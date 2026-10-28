using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

namespace MysticCombat.Network
{
    /// <summary>
    /// Singleton networking manager for Mystic Combat LAN play.
    /// Operates a high-performance, low-latency TCP socket connection (NoDelay enabled)
    /// between Host (PC 1) and Client (PC 2).
    /// </summary>
    public class LanNetworkManager : MonoBehaviour
    {
        #region Singleton

        private static bool isShuttingDown = false;
        private static LanNetworkManager instance;

        public static bool HasInstance => instance != null && !isShuttingDown;

        public static LanNetworkManager Instance
        {
            get
            {
                if (isShuttingDown) return null;

                if (instance == null)
                {
                    instance = FindAnyObjectByType<LanNetworkManager>();
                    if (instance == null && !isShuttingDown)
                    {
                        GameObject go = new GameObject("LanNetworkManager");
                        instance = go.AddComponent<LanNetworkManager>();
                    }
                }
                return instance;
            }
        }

        #endregion

        #region Enums

        public enum NetworkRole
        {
            None,
            Host,
            Client
        }

        #endregion

        #region Configuration

        [Header("LAN Settings")]
        [SerializeField] private int defaultPort = 7777;

        #endregion

        #region State

        private NetworkRole currentRole = NetworkRole.None;
        private bool isConnected = false;

        private TcpListener tcpListener;
        private TcpClient activeClient;
        private NetworkStream networkStream;
        private BinaryWriter streamWriter;
        private Thread receiveThread;
        private volatile bool isRunning = false;

        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        #endregion

        #region Events

        public event Action<bool> OnConnectionStateChanged;
        public event Action<CharSelectSyncPacket> OnCharSelectSyncReceived;
        public event Action<MatchStartPacket> OnMatchStartReceived;
        public event Action<FighterInputPacket> OnRemoteInputReceived;
        public event Action<MatchSnapshotPacket> OnMatchSnapshotReceived;
        public event Action OnRoundResetReceived;

        #endregion

        #region Properties

        public NetworkRole CurrentRole => currentRole;
        public bool IsConnected => isConnected;
        public bool IsHost => currentRole == NetworkRole.Host;
        public bool IsClient => currentRole == NetworkRole.Client;
        public int ActivePort => defaultPort;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            isShuttingDown = false;
            Application.runInBackground = true;

            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Drain network events safely onto Unity's main thread
            while (mainThreadActions.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
            Disconnect();
        }

        #endregion

        #region IP Discovery

        /// <summary>
        /// Retrieves the primary active local IPv4 address of this machine (e.g. 192.168.1.X).
        /// </summary>
        public string GetLocalIPv4Address()
        {
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    IPInterfaceProperties ipProps = ni.GetIPProperties();
                    foreach (UnicastIPAddressInformation unicast in ipProps.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return unicast.Address.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanNetworkManager] Failed to scan network interfaces: {ex.Message}");
            }

            return "127.0.0.1";
        }

        #endregion

        #region Host Management

        /// <summary>
        /// Starts hosting a match on the specified port.
        /// </summary>
        public bool StartHost(int port = 7777)
        {
            Disconnect();

            defaultPort = port;
            currentRole = NetworkRole.Host;

            try
            {
                tcpListener = new TcpListener(IPAddress.Any, defaultPort);
                tcpListener.Start();
                isRunning = true;

                Debug.Log($"<color=#00e676>[LanNetworkManager]</color> Host listening on port {defaultPort}. Local IP: {GetLocalIPv4Address()}");

                // Begin waiting asynchronously for challenger
                tcpListener.BeginAcceptTcpClient(OnClientAccepted, null);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LanNetworkManager] Failed to start host: {ex.Message}");
                Disconnect();
                return false;
            }
        }

        private void OnClientAccepted(IAsyncResult ar)
        {
            if (!isRunning || tcpListener == null) return;

            try
            {
                activeClient = tcpListener.EndAcceptTcpClient(ar);
                activeClient.NoDelay = true; // Minimum ping (turns off Nagle's algorithm)
                activeClient.ReceiveBufferSize = 8192;
                activeClient.SendBufferSize = 8192;

                networkStream = activeClient.GetStream();
                streamWriter = new BinaryWriter(networkStream);
                isConnected = true;

                Debug.Log("<color=#00e676>[LanNetworkManager]</color> Challenger connected successfully to Host!");

                // Start reader thread
                StartReceiveThread();

                mainThreadActions.Enqueue(() =>
                {
                    OnConnectionStateChanged?.Invoke(true);
                });
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    Debug.LogError($"[LanNetworkManager] Error accepting client: {ex.Message}");
                }
            }
        }

        #endregion

        #region Client Management

        /// <summary>
        /// Connects to a remote Host on the given IP and port.
        /// </summary>
        public void ConnectToHost(string hostIp, int port = 7777)
        {
            Disconnect();

            if (string.IsNullOrWhiteSpace(hostIp)) hostIp = "127.0.0.1";
            defaultPort = port;
            currentRole = NetworkRole.Client;
            isRunning = true;

            Debug.Log($"<color=#00e676>[LanNetworkManager]</color> Connecting to Host at {hostIp}:{port}...");

            try
            {
                activeClient = new TcpClient();
                activeClient.NoDelay = true; // Sub-millisecond latency
                activeClient.ReceiveBufferSize = 8192;
                activeClient.SendBufferSize = 8192;

                activeClient.BeginConnect(hostIp, defaultPort, OnConnectedCallback, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LanNetworkManager] Connect error: {ex.Message}");
                Disconnect();
                mainThreadActions.Enqueue(() => OnConnectionStateChanged?.Invoke(false));
            }
        }

        private void OnConnectedCallback(IAsyncResult ar)
        {
            if (!isRunning || activeClient == null) return;

            try
            {
                activeClient.EndConnect(ar);
                networkStream = activeClient.GetStream();
                streamWriter = new BinaryWriter(networkStream);
                isConnected = true;

                Debug.Log("<color=#00e676>[LanNetworkManager]</color> Successfully connected to Host!");

                StartReceiveThread();

                mainThreadActions.Enqueue(() =>
                {
                    OnConnectionStateChanged?.Invoke(true);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LanNetworkManager] Connection failed: {ex.Message}");
                Disconnect();
                mainThreadActions.Enqueue(() => OnConnectionStateChanged?.Invoke(false));
            }
        }

        #endregion

        #region Packet Transmission

        /// <summary>
        /// Serializes and sends a discrete framed packet: [1 byte Type][2 bytes Length][Payload].
        /// </summary>
        public void SendPacket(PacketType type, Action<BinaryWriter> writePayload)
        {
            if (!isConnected || streamWriter == null || networkStream == null) return;

            try
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    writePayload?.Invoke(bw);
                    byte[] payload = ms.ToArray();
                    short length = (short)payload.Length;

                    lock (streamWriter)
                    {
                        streamWriter.Write((byte)type);
                        streamWriter.Write(length);
                        if (length > 0)
                        {
                            streamWriter.Write(payload);
                        }
                        streamWriter.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanNetworkManager] Send error: {ex.Message}");
                Disconnect();
            }
        }

        public void SendInput(FighterInputPacket packet)
        {
            SendPacket(PacketType.FighterInput, (writer) => packet.Serialize(writer));
        }

        public void SendMatchSnapshot(MatchSnapshotPacket packet)
        {
            SendPacket(PacketType.MatchSnapshot, (writer) => packet.Serialize(writer));
        }

        public void SendCharSelectSync(CharSelectSyncPacket packet)
        {
            SendPacket(PacketType.CharSelectSync, (writer) => packet.Serialize(writer));
        }

        public void SendMatchStart(MatchStartPacket packet)
        {
            SendPacket(PacketType.MatchStart, (writer) => packet.Serialize(writer));
        }

        public void SendRoundReset()
        {
            SendPacket(PacketType.RoundReset, null);
        }

        #endregion

        #region Packet Reception Loop

        private void StartReceiveThread()
        {
            receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "LanNetworkReader"
            };
            receiveThread.Start();
        }

        private void ReceiveLoop()
        {
            BinaryReader reader = new BinaryReader(networkStream);

            try
            {
                while (isRunning && isConnected && networkStream != null)
                {
                    // 1. Read Packet Header
                    byte typeByte = reader.ReadByte();
                    short length = reader.ReadInt16();

                    byte[] payload = length > 0 ? reader.ReadBytes(length) : Array.Empty<byte>();

                    PacketType packetType = (PacketType)typeByte;
                    ProcessIncomingPacket(packetType, payload);
                }
            }
            catch (Exception)
            {
                // Stream closed or disconnected
            }
            finally
            {
                mainThreadActions.Enqueue(() =>
                {
                    if (isConnected)
                    {
                        Debug.LogWarning("[LanNetworkManager] Connection terminated.");
                        Disconnect();
                        OnConnectionStateChanged?.Invoke(false);
                    }
                });
            }
        }

        private void ProcessIncomingPacket(PacketType type, byte[] payload)
        {
            using (MemoryStream ms = new MemoryStream(payload))
            using (BinaryReader r = new BinaryReader(ms))
            {
                switch (type)
                {
                    case PacketType.FighterInput:
                        FighterInputPacket inputPacket = FighterInputPacket.Deserialize(r);
                        mainThreadActions.Enqueue(() => OnRemoteInputReceived?.Invoke(inputPacket));
                        break;

                    case PacketType.MatchSnapshot:
                        MatchSnapshotPacket snapshotPacket = MatchSnapshotPacket.Deserialize(r);
                        mainThreadActions.Enqueue(() => OnMatchSnapshotReceived?.Invoke(snapshotPacket));
                        break;

                    case PacketType.CharSelectSync:
                        CharSelectSyncPacket syncPacket = CharSelectSyncPacket.Deserialize(r);
                        mainThreadActions.Enqueue(() => OnCharSelectSyncReceived?.Invoke(syncPacket));
                        break;

                    case PacketType.MatchStart:
                        MatchStartPacket startPacket = MatchStartPacket.Deserialize(r);
                        mainThreadActions.Enqueue(() => OnMatchStartReceived?.Invoke(startPacket));
                        break;

                    case PacketType.RoundReset:
                        mainThreadActions.Enqueue(() => OnRoundResetReceived?.Invoke());
                        break;

                    case PacketType.Disconnect:
                        mainThreadActions.Enqueue(Disconnect);
                        break;
                }
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanly shuts down active sockets and streams.
        /// </summary>
        public void Disconnect()
        {
            isRunning = false;
            isConnected = false;

            try
            {
                if (streamWriter != null)
                {
                    streamWriter.Close();
                    streamWriter = null;
                }
                if (networkStream != null)
                {
                    networkStream.Close();
                    networkStream = null;
                }
                if (activeClient != null)
                {
                    activeClient.Close();
                    activeClient = null;
                }
                if (tcpListener != null)
                {
                    tcpListener.Stop();
                    tcpListener = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanNetworkManager] Cleanup warning: {ex.Message}");
            }

            currentRole = NetworkRole.None;
        }

        #endregion
    }
}
