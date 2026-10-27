using UnityEngine;
using MysticCombat.Systems;

namespace MysticCombat.Core
{
    /// <summary>
    /// Manages the match flow: rounds, timer, and win conditions.
    /// Lives in the Match scene.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// States of the match round flow.
        /// </summary>
        public enum MatchState
        {
            WaitingToStart,
            RoundInProgress,
            RoundEnd,
            MatchOver
        }

        #endregion

        #region Serialized Fields

        [Header("Fighter References")]
        [Tooltip("Player 1's HealthSystem component.")]
        [SerializeField] private HealthSystem p1Health;

        [Tooltip("Player 2's HealthSystem component.")]
        [SerializeField] private HealthSystem p2Health;

        [Tooltip("Player 1's EnergySystem component.")]
        [SerializeField] private EnergySystem p1Energy;

        [Tooltip("Player 2's EnergySystem component.")]
        [SerializeField] private EnergySystem p2Energy;

        [Header("Match Settings")]
        [SerializeField] private int roundsToWin = 2;
        [SerializeField] private float roundDuration = 99f;
        [SerializeField] private float roundEndPauseDuration = 1.8f;

        [Header("Spawn Positions")]
        [SerializeField] private Vector3 p1SpawnPosition = new Vector3(-2.2f, -3.0f, 0f);
        [SerializeField] private Vector3 p2SpawnPosition = new Vector3(2.2f, -3.0f, 0f);

        #endregion

        #region State

        private MatchState currentMatchState = MatchState.WaitingToStart;
        private int p1RoundWins;
        private int p2RoundWins;
        private float roundTimer;
        private int currentRound;

        private Fighter.FighterController p1Controller;
        private Fighter.FighterController p2Controller;

        #endregion

        #region Properties

        /// <summary>
        /// The current match state.
        /// </summary>
        public MatchState CurrentMatchState => currentMatchState;

        /// <summary>
        /// Player 1's total round wins this match.
        /// </summary>
        public int P1RoundWins => p1RoundWins;

        /// <summary>
        /// Player 2's total round wins this match.
        /// </summary>
        public int P2RoundWins => p2RoundWins;

        /// <summary>
        /// The current round timer value.
        /// </summary>
        public float RoundTimer => roundTimer;

        /// <summary>
        /// The current round number (1-indexed).
        /// </summary>
        public int CurrentRound => currentRound;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a round starts. Parameter: round number.
        /// </summary>
        public event System.Action<int> OnRoundStart;

        /// <summary>
        /// Fired when a round ends. Parameters: (winner player index 1 or 2, round number).
        /// </summary>
        public event System.Action<int, int> OnRoundEnd;

        /// <summary>
        /// Fired when the match is over. Parameter: winner player index (1 or 2).
        /// </summary>
        public event System.Action<int> OnMatchOver;

        /// <summary>
        /// Fired every frame the timer updates during a round.
        /// </summary>
        public event System.Action<float> OnTimerUpdate;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            ResolveFighters();
            if (GetComponent<MatchAudioController>() == null)
            {
                gameObject.AddComponent<MatchAudioController>();
            }
        }

        private void Start()
        {
            ResolveFighters();
            StartMatch();
        }

        private void Update()
        {
            if (p1Health == null || p2Health == null || !IsInActiveScene(p1Health) || !IsInActiveScene(p2Health))
            {
                ResolveFighters();
            }

            if (currentMatchState == MatchState.RoundInProgress)
            {
                TickRoundTimer();
            }
        }

        #endregion

        #region Public Methods

        private static bool IsInActiveScene(Component comp)
        {
            if (comp == null) return false;
            return comp.gameObject.scene.IsValid() && comp.gameObject.scene.isLoaded;
        }

        /// <summary>
        /// Auto-discovers fighters in the scene if not explicitly serialized or if pointing to assets on disk.
        /// </summary>
        public void ResolveFighters()
        {
            bool p1Invalid = !IsInActiveScene(p1Health) || !IsInActiveScene(p1Energy);
            bool p2Invalid = !IsInActiveScene(p2Health) || !IsInActiveScene(p2Energy);

            if (p1Invalid)
            {
                p1Health = null;
                p1Energy = null;
                p1Controller = null;
                GameObject goku = GameObject.Find("Fighter_P1_Goku");
                if (goku != null)
                {
                    p1Health = goku.GetComponent<HealthSystem>();
                    p1Energy = goku.GetComponent<EnergySystem>();
                    p1Controller = goku.GetComponent<Fighter.FighterController>();
                }
            }

            if (p2Invalid)
            {
                p2Health = null;
                p2Energy = null;
                p2Controller = null;
                GameObject saitama = GameObject.Find("Fighter_P2_Saitama");
                if (saitama != null)
                {
                    p2Health = saitama.GetComponent<HealthSystem>();
                    p2Energy = saitama.GetComponent<EnergySystem>();
                    p2Controller = saitama.GetComponent<Fighter.FighterController>();
                }
            }

            if (p1Health != null)
            {
                p1Health.OnDeath -= HandleP1Death;
                p1Health.OnDeath += HandleP1Death;
            }
            if (p2Health != null)
            {
                p2Health.OnDeath -= HandleP2Death;
                p2Health.OnDeath += HandleP2Death;
            }
        }

        private void HandleP1Death() => OnFighterDefeated(1);
        private void HandleP2Death() => OnFighterDefeated(2);

        /// <summary>
        /// Sets up fighter references. Call this if fighters are created at runtime.
        /// </summary>
        public void SetFighterReferences(HealthSystem p1Hp, HealthSystem p2Hp, EnergySystem p1Ki, EnergySystem p2Ki)
        {
            p1Health = p1Hp;
            p2Health = p2Hp;
            p1Energy = p1Ki;
            p2Energy = p2Ki;

            if (p1Health != null) p1Controller = p1Health.GetComponent<Fighter.FighterController>();
            if (p2Health != null) p2Controller = p2Health.GetComponent<Fighter.FighterController>();

            if (p1Health != null)
            {
                p1Health.OnDeath -= HandleP1Death;
                p1Health.OnDeath += HandleP1Death;
            }
            if (p2Health != null)
            {
                p2Health.OnDeath -= HandleP2Death;
                p2Health.OnDeath += HandleP2Death;
            }
        }

        /// <summary>
        /// Begins the match, starting at round 1.
        /// </summary>
        public void StartMatch()
        {
            p1RoundWins = 0;
            p2RoundWins = 0;
            currentRound = 0;

            Debug.Log("[MatchManager] Match started!");
            StartRound();
        }

        /// <summary>
        /// Starts a new round anew. Completely resets positions, states, health, and timers.
        /// </summary>
        public void StartRound()
        {
            currentRound++;
            roundTimer = roundDuration;

            // 1. Reset fighters to starting positions and Idle state
            if (p1Controller != null) p1Controller.ResetForNewRound(p1SpawnPosition, true);
            else if (p1Health != null) p1Health.transform.position = p1SpawnPosition;

            if (p2Controller != null) p2Controller.ResetForNewRound(p2SpawnPosition, false);
            else if (p2Health != null) p2Health.transform.position = p2SpawnPosition;

            // 2. Reset health and ki
            if (p1Health != null) p1Health.ResetHealth();
            if (p2Health != null) p2Health.ResetHealth();
            if (p1Energy != null) p1Energy.ResetKi();
            if (p2Energy != null) p2Energy.ResetKi();

            currentMatchState = MatchState.RoundInProgress;

            Debug.Log($"<color=#00e676>[MatchManager] Round {currentRound} START! (Score: P1={p1RoundWins} | P2={p2RoundWins})</color>");
            OnRoundStart?.Invoke(currentRound);
        }

        /// <summary>
        /// Ends the current round, awarding a win to the specified player.
        /// </summary>
        /// <param name="winnerPlayerIndex">1 for P1, 2 for P2.</param>
        public void EndRound(int winnerPlayerIndex)
        {
            if (currentMatchState == MatchState.RoundEnd || currentMatchState == MatchState.MatchOver) return;

            currentMatchState = MatchState.RoundEnd;

            if (winnerPlayerIndex == 1)
                p1RoundWins++;
            else
                p2RoundWins++;

            Debug.Log($"<color=#ffaa00>[MatchManager]</color> Round {currentRound} WON by Player {winnerPlayerIndex}! (Score: P1={p1RoundWins} | P2={p2RoundWins})");

            OnRoundEnd?.Invoke(winnerPlayerIndex, currentRound);

            StartCoroutine(RoundEndSequence(winnerPlayerIndex));
        }

        private System.Collections.IEnumerator RoundEndSequence(int winnerPlayerIndex)
        {
            // Dramatic pause for K.O. celebration and readability
            yield return new WaitForSeconds(roundEndPauseDuration);

            CheckWinCondition();
        }

        /// <summary>
        /// Checks if either player has won enough rounds to win the match.
        /// </summary>
        public void CheckWinCondition()
        {
            if (p1RoundWins >= roundsToWin)
            {
                currentMatchState = MatchState.MatchOver;
                Debug.Log("<color=#00e676>[MatchManager] MATCH OVER — Player 1 (Goku) WINS THE TOURNAMENT!</color>");
                OnMatchOver?.Invoke(1);
            }
            else if (p2RoundWins >= roundsToWin)
            {
                currentMatchState = MatchState.MatchOver;
                Debug.Log("<color=#00e676>[MatchManager] MATCH OVER — Player 2 (Saitama) WINS THE TOURNAMENT!</color>");
                OnMatchOver?.Invoke(2);
            }
            else
            {
                // Start next round cleanly (Round 2 or Round 3 Final Round)
                StartRound();
            }
        }

        #endregion

        #region Private Methods

        private void TickRoundTimer()
        {
            roundTimer -= Time.deltaTime;
            OnTimerUpdate?.Invoke(roundTimer);

            if (roundTimer <= 0f)
            {
                roundTimer = 0f;
                TimeUp();
            }
        }

        private void TimeUp()
        {
            Debug.Log("[MatchManager] TIME UP!");

            // Determine winner by remaining health percentage
            float p1Percent = p1Health != null ? p1Health.HealthPercent : 0f;
            float p2Percent = p2Health != null ? p2Health.HealthPercent : 0f;

            if (p1Percent >= p2Percent)
            {
                EndRound(1);
            }
            else
            {
                EndRound(2);
            }
        }

        private void OnFighterDefeated(int defeatedPlayerIndex)
        {
            if (currentMatchState != MatchState.RoundInProgress) return;

            // The other player wins the round
            int winner = defeatedPlayerIndex == 1 ? 2 : 1;
            EndRound(winner);
        }

        #endregion
    }
}
