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

        #endregion

        #region State

        private MatchState currentMatchState = MatchState.WaitingToStart;
        private int p1RoundWins;
        private int p2RoundWins;
        private float roundTimer;
        private int currentRound;

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

        private void Start()
        {
            StartMatch();
        }

        private void Update()
        {
            if (currentMatchState == MatchState.RoundInProgress)
            {
                TickRoundTimer();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up fighter references. Call this if fighters are created at runtime.
        /// </summary>
        /// <param name="p1Hp">Player 1's HealthSystem.</param>
        /// <param name="p2Hp">Player 2's HealthSystem.</param>
        /// <param name="p1Ki">Player 1's EnergySystem.</param>
        /// <param name="p2Ki">Player 2's EnergySystem.</param>
        public void SetFighterReferences(HealthSystem p1Hp, HealthSystem p2Hp, EnergySystem p1Ki, EnergySystem p2Ki)
        {
            p1Health = p1Hp;
            p2Health = p2Hp;
            p1Energy = p1Ki;
            p2Energy = p2Ki;

            // Subscribe to death events
            if (p1Health != null) p1Health.OnDeath += () => OnFighterDefeated(1);
            if (p2Health != null) p2Health.OnDeath += () => OnFighterDefeated(2);
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
        /// Starts a new round.
        /// </summary>
        public void StartRound()
        {
            currentRound++;
            roundTimer = roundDuration;
            currentMatchState = MatchState.RoundInProgress;

            // Reset fighter health and ki for the new round
            if (p1Health != null) p1Health.ResetHealth();
            if (p2Health != null) p2Health.ResetHealth();
            if (p1Energy != null) p1Energy.ResetKi();
            if (p2Energy != null) p2Energy.ResetKi();

            Debug.Log($"[MatchManager] Round {currentRound} START!");
            OnRoundStart?.Invoke(currentRound);
        }

        /// <summary>
        /// Ends the current round, awarding a win to the specified player.
        /// </summary>
        /// <param name="winnerPlayerIndex">1 for P1, 2 for P2.</param>
        public void EndRound(int winnerPlayerIndex)
        {
            currentMatchState = MatchState.RoundEnd;

            if (winnerPlayerIndex == 1)
                p1RoundWins++;
            else
                p2RoundWins++;

            Debug.Log($"[MatchManager] Round {currentRound} WON by P{winnerPlayerIndex}. " +
                      $"Score: P1={p1RoundWins} / P2={p2RoundWins}");

            OnRoundEnd?.Invoke(winnerPlayerIndex, currentRound);

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
                Debug.Log("[MatchManager] MATCH OVER — Player 1 WINS!");
                OnMatchOver?.Invoke(1);
            }
            else if (p2RoundWins >= roundsToWin)
            {
                currentMatchState = MatchState.MatchOver;
                Debug.Log("[MatchManager] MATCH OVER — Player 2 WINS!");
                OnMatchOver?.Invoke(2);
            }
            else
            {
                // Start next round after a short delay (Day 1: immediate)
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
