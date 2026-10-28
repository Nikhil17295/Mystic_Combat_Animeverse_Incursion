using UnityEngine;
using MysticCombat.Core;
using MysticCombat.Fighter;
using MysticCombat.Systems;

namespace MysticCombat.Network
{
    /// <summary>
    /// Synchronizes combat arena over LAN using symmetric input exchange.
    /// - Both Host and Client simulate full local physics, animations, and hitboxes.
    /// - Local player is always Player 1 (Left); remote player is always Player 2 (Right).
    /// - Every frame, local P1 inputs are transmitted to the remote peer's Player 2.
    /// - Host broadcasts lightweight periodic snapshots (timer, round state, health reconciliation).
    /// </summary>
    public class NetworkMatchSynchronizer : MonoBehaviour
    {
        #region State

        private FighterController p1Controller;
        private FighterController p2Controller;
        private HealthSystem p1Health;
        private HealthSystem p2Health;
        private EnergySystem p1Energy;
        private EnergySystem p2Energy;
        private MatchManager matchManager;
        private NetworkFighterInput remoteP2Input;

        private bool isHost;
        private bool isClient;
        private bool isInitialized = false;

        #endregion

        #region Initialization

        public void Initialize(FighterController fc1, FighterController fc2, MatchManager mm, bool asHost)
        {
            p1Controller = fc1;
            p2Controller = fc2;
            matchManager = mm;
            isHost = asHost;
            isClient = !asHost;

            if (p1Controller != null)
            {
                p1Health = p1Controller.GetComponent<HealthSystem>();
                p1Energy = p1Controller.GetComponent<EnergySystem>();
            }
            if (p2Controller != null)
            {
                p2Health = p2Controller.GetComponent<HealthSystem>();
                p2Energy = p2Controller.GetComponent<EnergySystem>();
                remoteP2Input = p2Controller.GetComponent<NetworkFighterInput>();
            }

            // Both Host and Client subscribe to incoming remote inputs
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnRemoteInputReceived += OnRemoteInputReceived;
                LanNetworkManager.Instance.OnRoundResetReceived += OnRoundResetReceived;

                if (isClient)
                {
                    LanNetworkManager.Instance.OnMatchSnapshotReceived += OnMatchSnapshotReceived;
                }
            }

            isInitialized = true;
            Debug.Log($"<color=#00e676>[NetworkMatchSynchronizer]</color> Initialized Symmetric Input-Exchange Netcode. Role: {(isHost ? "HOST" : "CLIENT")}. Full local 60 FPS simulation active!");
        }

        private void OnDestroy()
        {
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnRemoteInputReceived -= OnRemoteInputReceived;
                LanNetworkManager.Instance.OnRoundResetReceived -= OnRoundResetReceived;

                if (isClient)
                {
                    LanNetworkManager.Instance.OnMatchSnapshotReceived -= OnMatchSnapshotReceived;
                }
            }
        }

        #endregion

        #region Unity Loop

        private void Update()
        {
            if (!isInitialized || LanNetworkManager.Instance == null || !LanNetworkManager.Instance.IsConnected) return;

            // Stream local P1 inputs to remote peer's Player 2
            if (p1Controller != null && p1Controller.InputHandler != null)
            {
                var input = p1Controller.InputHandler;
                FighterInputPacket pkt = new FighterInputPacket
                {
                    MoveInput = input.MoveInput,
                    JumpPressed = input.JumpPressed,
                    CrouchHeld = input.CrouchHeld,
                    LightAttackPressed = input.LightAttackPressed,
                    HeavyAttackPressed = input.HeavyAttackPressed,
                    SkillPressed = input.SkillPressed,
                    UltimatePressed = input.UltimatePressed,
                    MeditateHeld = input.MeditateHeld
                };

                LanNetworkManager.Instance.SendInput(pkt);
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized || !isHost || LanNetworkManager.Instance == null || !LanNetworkManager.Instance.IsConnected) return;

            // Host broadcasts lightweight snapshot every 6 fixed ticks (~10 Hz) for soft clock/health sync
            if (Time.frameCount % 6 == 0)
            {
                BroadcastSnapshotToClient();
            }
        }

        #endregion

        #region Snapshot Broadcasting & Synchronization

        private void BroadcastSnapshotToClient()
        {
            MatchSnapshotPacket snap = new MatchSnapshotPacket();
            if (matchManager != null)
            {
                snap.CurrentRound = (byte)matchManager.CurrentRound;
                snap.RoundTimer = matchManager.RoundTimer;
                snap.MatchState = (byte)matchManager.CurrentMatchState;
                snap.P1RoundWins = (byte)matchManager.P1RoundWins;
                snap.P2RoundWins = (byte)matchManager.P2RoundWins;
            }

            snap.P1 = new FighterSnapshot
            {
                Health = (short)(p1Health != null ? p1Health.CurrentHealth : 100),
                Ki = (short)(p1Energy != null ? p1Energy.CurrentKi : 0)
            };

            snap.P2 = new FighterSnapshot
            {
                Health = (short)(p2Health != null ? p2Health.CurrentHealth : 100),
                Ki = (short)(p2Energy != null ? p2Energy.CurrentKi : 0)
            };

            LanNetworkManager.Instance.SendMatchSnapshot(snap);
        }

        private void OnRemoteInputReceived(FighterInputPacket packet)
        {
            if (remoteP2Input != null)
            {
                // Invert X because the remote player is on the opposite side of the arena (facing center)
                packet.MoveInput = new Vector2(-packet.MoveInput.x, packet.MoveInput.y);
                remoteP2Input.FeedInput(packet);
            }
        }

        private void OnMatchSnapshotReceived(MatchSnapshotPacket snap)
        {
            if (!isClient) return;

            // On Client: Client is P1 (Left), Host is P2 (Right).
            // Host's P1 corresponds to Client's P2, and Host's P2 corresponds to Client's P1.
            if (matchManager != null)
            {
                matchManager.SyncFromNetwork(snap.CurrentRound, snap.RoundTimer, (MatchManager.MatchState)snap.MatchState, snap.P2RoundWins, snap.P1RoundWins);
            }

            // Soft health & Ki reconciliation if slight drift occurred
            if (p1Health != null && Mathf.Abs(p1Health.CurrentHealth - snap.P2.Health) > 3)
            {
                p1Health.SetHealthDirect(snap.P2.Health);
            }
            if (p2Health != null && Mathf.Abs(p2Health.CurrentHealth - snap.P1.Health) > 3)
            {
                p2Health.SetHealthDirect(snap.P1.Health);
            }
            if (p1Energy != null && Mathf.Abs((int)p1Energy.CurrentKi - snap.P2.Ki) > 5)
            {
                p1Energy.SetKiDirect(snap.P2.Ki);
            }
            if (p2Energy != null && Mathf.Abs((int)p2Energy.CurrentKi - snap.P1.Ki) > 5)
            {
                p2Energy.SetKiDirect(snap.P1.Ki);
            }
        }

        private void OnRoundResetReceived()
        {
            if (isClient && matchManager != null)
            {
                Debug.Log("<color=#00e676>[NetworkMatchSynchronizer]</color> Received Round Reset from Host. Syncing new round locally!");
                matchManager.StartRound();
            }
        }

        #endregion
    }
}
