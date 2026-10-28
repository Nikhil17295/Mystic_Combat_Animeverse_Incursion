using UnityEngine;
using MysticCombat.Core;
using MysticCombat.Fighter;
using MysticCombat.Systems;
using MysticCombat.Combat;

namespace MysticCombat.Network
{
    /// <summary>
    /// Synchronizes combat arena state over LAN.
    /// - On Host: Streams 60Hz MatchSnapshotPackets to Client and receives P2 FighterInputPackets.
    /// - On Client: Captures local P2 keyboard inputs, streams them to Host, and mirrors Host match state.
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
            }

            if (isHost)
            {
                // Host receives remote inputs from Client and feeds into P2
                remoteP2Input = p2Controller != null ? p2Controller.GetComponent<NetworkFighterInput>() : null;
                if (LanNetworkManager.Instance != null)
                {
                    LanNetworkManager.Instance.OnRemoteInputReceived += OnRemoteInputReceived;
                }
            }
            else
            {
                // Client receives snapshots from Host
                if (LanNetworkManager.Instance != null)
                {
                    LanNetworkManager.Instance.OnMatchSnapshotReceived += OnMatchSnapshotReceived;
                }

                // Make Client fighters kinematic to prevent local physics drift
                if (p1Controller != null)
                {
                    Rigidbody2D rb1 = p1Controller.GetComponent<Rigidbody2D>();
                    if (rb1 != null) rb1.bodyType = RigidbodyType2D.Kinematic;
                }
                if (p2Controller != null)
                {
                    Rigidbody2D rb2 = p2Controller.GetComponent<Rigidbody2D>();
                    if (rb2 != null) rb2.bodyType = RigidbodyType2D.Kinematic;
                }

                // Disable local damaging hitboxes on client: Host is 100% authoritative
                HitboxController[] hitboxes = FindObjectsByType<HitboxController>(FindObjectsSortMode.None);
                for (int i = 0; i < hitboxes.Length; i++)
                {
                    hitboxes[i].enabled = false;
                }
            }

            isInitialized = true;
            Debug.Log($"<color=#00e676>[NetworkMatchSynchronizer]</color> Initialized. Role: {(isHost ? "HOST (Authoritative Engine)" : "CLIENT (Screen & Input Mirror)")}");
        }

        private void OnDestroy()
        {
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnRemoteInputReceived -= OnRemoteInputReceived;
                LanNetworkManager.Instance.OnMatchSnapshotReceived -= OnMatchSnapshotReceived;
            }
        }

        #endregion

        #region Unity Loop

        private void Update()
        {
            if (!isInitialized) return;

            if (isClient)
            {
                // Capture local inputs on Client and transmit to Host
                SendClientInputToHost();
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized) return;

            if (isHost)
            {
                // Broadcast authoritative simulation snapshot to Client at fixed 60Hz
                BroadcastSnapshotToClient();
            }
        }

        #endregion

        #region Client Input Gathering

        private void SendClientInputToHost()
        {
            if (LanNetworkManager.Instance == null || !LanNetworkManager.Instance.IsConnected) return;

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            FighterInputPacket pkt = new FighterInputPacket();

            // Client controls Player 2 on their own machine using comfortable solo keys (WASD or Arrows)
            float mx = 0f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) mx += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) mx -= 1f;
            pkt.MoveInput = new Vector2(mx, 0f);

            pkt.JumpPressed = kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            pkt.CrouchHeld = kb.sKey.isPressed || kb.downArrowKey.isPressed;
            pkt.LightAttackPressed = kb.fKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;
            pkt.HeavyAttackPressed = kb.cKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame;
            pkt.SkillPressed = kb.eKey.wasPressedThisFrame || kb.uKey.wasPressedThisFrame;
            pkt.UltimatePressed = kb.qKey.wasPressedThisFrame || kb.oKey.wasPressedThisFrame;
            pkt.MeditateHeld = kb.gKey.isPressed || kb.mKey.isPressed;

            LanNetworkManager.Instance.SendInput(pkt);
        }

        #endregion

        #region Host Snapshot Broadcasting

        private void BroadcastSnapshotToClient()
        {
            if (LanNetworkManager.Instance == null || !LanNetworkManager.Instance.IsConnected) return;
            if (p1Controller == null || p2Controller == null) return;

            MatchSnapshotPacket snap = new MatchSnapshotPacket();
            if (matchManager != null)
            {
                snap.CurrentRound = (byte)matchManager.CurrentRound;
                snap.RoundTimer = matchManager.RoundTimer;
                snap.MatchState = (byte)matchManager.CurrentMatchState;
                snap.P1RoundWins = (byte)matchManager.P1RoundWins;
                snap.P2RoundWins = (byte)matchManager.P2RoundWins;
            }

            snap.P1 = BuildFighterSnapshot(p1Controller, p1Health, p1Energy);
            snap.P2 = BuildFighterSnapshot(p2Controller, p2Health, p2Energy);

            LanNetworkManager.Instance.SendMatchSnapshot(snap);
        }

        private FighterSnapshot BuildFighterSnapshot(FighterController fc, HealthSystem hp, EnergySystem en)
        {
            FighterSnapshot s = new FighterSnapshot();
            s.PositionX = fc.transform.position.x;
            s.PositionY = fc.transform.position.y;

            Rigidbody2D rb = fc.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                s.VelocityX = rb.linearVelocity.x;
                s.VelocityY = rb.linearVelocity.y;
            }

            s.Health = (short)(hp != null ? hp.CurrentHealth : 100);
            s.Ki = (short)(en != null ? en.CurrentKi : 0);
            s.State = fc.CurrentState;

            SpriteRenderer sr = fc.GetComponent<SpriteRenderer>();
            s.FlipX = sr != null && sr.flipX;

            return s;
        }

        #endregion

        #region Event Callbacks

        private void OnRemoteInputReceived(FighterInputPacket packet)
        {
            if (remoteP2Input != null)
            {
                remoteP2Input.FeedInput(packet);
            }
        }

        private void OnMatchSnapshotReceived(MatchSnapshotPacket snap)
        {
            if (!isClient) return;

            // Apply P1 state
            if (p1Controller != null)
            {
                p1Controller.transform.position = new Vector3(snap.P1.PositionX, snap.P1.PositionY, 0f);
                p1Controller.SetFacing(snap.P1.FlipX);
                p1Controller.ForceNetworkState(snap.P1.State);
                if (p1Health != null && p1Health.CurrentHealth != snap.P1.Health)
                {
                    p1Health.SetHealthDirect(snap.P1.Health);
                }
                if (p1Energy != null && (int)p1Energy.CurrentKi != snap.P1.Ki)
                {
                    p1Energy.SetKiDirect(snap.P1.Ki);
                }
            }

            // Apply P2 state
            if (p2Controller != null)
            {
                p2Controller.transform.position = new Vector3(snap.P2.PositionX, snap.P2.PositionY, 0f);
                p2Controller.SetFacing(snap.P2.FlipX);
                p2Controller.ForceNetworkState(snap.P2.State);
                if (p2Health != null && p2Health.CurrentHealth != snap.P2.Health)
                {
                    p2Health.SetHealthDirect(snap.P2.Health);
                }
                if (p2Energy != null && (int)p2Energy.CurrentKi != snap.P2.Ki)
                {
                    p2Energy.SetKiDirect(snap.P2.Ki);
                }
            }

            // Synchronize Round clock, state, and score
            if (matchManager != null)
            {
                matchManager.SyncFromNetwork(snap.CurrentRound, snap.RoundTimer, (MatchManager.MatchState)snap.MatchState, snap.P1RoundWins, snap.P2RoundWins);
            }
        }

        #endregion
    }
}
