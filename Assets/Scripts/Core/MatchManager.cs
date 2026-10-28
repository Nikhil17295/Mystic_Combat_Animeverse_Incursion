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
        [SerializeField] private float roundDuration = 199f;
        [SerializeField] private float roundEndPauseDuration = 1.5f;

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
        private bool isOvertime;
        private Coroutine evaluateDefeatCoroutine;

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

        /// <summary>
        /// Whether the current round is in +100s overtime (sudden death).
        /// </summary>
        public bool IsOvertime => isOvertime;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a round starts. Parameter: round number.
        /// </summary>
        public event System.Action<int> OnRoundStart;

        /// <summary>
        /// Fired when a round ends. Parameters: (winner player index 1 or 2, or 0 for Draw, round number).
        /// </summary>
        public event System.Action<int, int> OnRoundEnd;

        /// <summary>
        /// Fired when the match is over. Parameter: winner player index (1 or 2, or 0 for Draw).
        /// </summary>
        public event System.Action<int> OnMatchOver;

        /// <summary>
        /// Fired every frame the timer updates during a round.
        /// </summary>
        public event System.Action<float> OnTimerUpdate;

        /// <summary>
        /// Fired when regular time expires and +100s overtime is granted.
        /// </summary>
        public event System.Action OnOvertimeStarted;

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
            if (p1Health != null && p2Health != null && currentMatchState == MatchState.WaitingToStart)
            {
                StartMatch();
            }
            else if (currentMatchState == MatchState.WaitingToStart)
            {
                Debug.Log("<color=#ffaa00>[MatchManager]</color> Waiting for fighters to be deployed before starting match...");
            }
        }

        private void Update()
        {
            if (p1Health == null || p2Health == null || !IsInActiveScene(p1Health) || !IsInActiveScene(p2Health))
            {
                ResolveFighters();
                if (currentMatchState == MatchState.WaitingToStart && p1Health != null && p2Health != null)
                {
                    StartMatch();
                }
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
        /// Searches for Fighter_P1* and Fighter_P2* regardless of character identity!
        /// </summary>
        public void ResolveFighters()
        {
            bool p1Invalid = !IsInActiveScene(p1Health) || !IsInActiveScene(p1Energy);
            bool p2Invalid = !IsInActiveScene(p2Health) || !IsInActiveScene(p2Energy);

            if (p1Invalid || p2Invalid)
            {
                var allFighters = FindObjectsByType<Fighter.FighterController>(FindObjectsInactive.Exclude);
                foreach (var fc in allFighters)
                {
                    if (fc == null) continue;
                    string n = fc.gameObject.name;
                    if (p1Invalid && (n == "Fighter_P1" || n.StartsWith("Fighter_P1_")))
                    {
                        p1Health = fc.GetComponent<HealthSystem>();
                        p1Energy = fc.GetComponent<EnergySystem>();
                        p1Controller = fc;
                    }
                    else if (p2Invalid && (n == "Fighter_P2" || n.StartsWith("Fighter_P2_")))
                    {
                        p2Health = fc.GetComponent<HealthSystem>();
                        p2Energy = fc.GetComponent<EnergySystem>();
                        p2Controller = fc;
                    }
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

            if (currentMatchState == MatchState.WaitingToStart && p1Health != null && p2Health != null)
            {
                StartMatch();
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

            if (currentMatchState == MatchState.WaitingToStart && p1Health != null && p2Health != null)
            {
                StartMatch();
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

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.InMatch);
            }

            Debug.Log("[MatchManager] Match started!");
            StartRound();
        }

        /// <summary>
        /// Starts a new round anew. Completely resets positions, states, health, and timers.
        /// </summary>
        public void StartRound()
        {
            Time.timeScale = 1.0f;
            currentRound++;
            roundTimer = roundDuration;
            isOvertime = false;
            evaluateDefeatCoroutine = null;

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

            Fighter.FighterController winner = winnerPlayerIndex == 1 ? p1Controller : p2Controller;
            Fighter.FighterController loser = winnerPlayerIndex == 1 ? p2Controller : p1Controller;

            // Phase 1: Loser enters Knockout immediately to trigger full multi-frame collapse animation
            if (loser != null) loser.TransitionTo(Fighter.FighterState.Knockout);

            // Dramatic Slow-Motion Knockout effect (0.28x physics speed)
            Time.timeScale = 0.12f;

            // Visceral K.O. Impact Flash & Screen Shake
            if (Combat.VFXManager.Instance != null)
            {
                Combat.VFXManager.Instance.TriggerImpactFlash(0.12f);
                Combat.VFXManager.Instance.TriggerCameraShake(0.40f, 0.40f);
            }

            Debug.Log($"<color=#ffaa00>[MatchManager]</color> Round {currentRound} WON by Player {winnerPlayerIndex}! (Score: P1={p1RoundWins} | P2={p2RoundWins})");

            OnRoundEnd?.Invoke(winnerPlayerIndex, currentRound);

            StartCoroutine(RoundEndSequence(winner, loser));
        }

        private System.Collections.IEnumerator RoundEndSequence(Fighter.FighterController winner, Fighter.FighterController loser)
        {
            // Phase 1: Cinematic slow-motion collapse (4.0 seconds in real time, unhurried and dramatic)
            yield return new WaitForSecondsRealtime(4.0f);

            // Phase 2: Defeated fighter strikes the floor -> snap physics back to normal
            Time.timeScale = 1.0f;
            if (loser != null && Combat.VFXManager.Instance != null)
            {
                Combat.VFXManager.Instance.PlayDustPuff(loser.transform.position);
                Combat.VFXManager.Instance.TriggerCameraShake(0.28f, 0.28f);
            }

            // Dramatic floor hold: let the defeat register while the defeated fighter lies motionless
            yield return new WaitForSeconds(1.5f);

            // Phase 3: Victorious fighter transitions into their celebration victory pose
            float victoryDuration = roundEndPauseDuration;
            if (winner != null)
            {
                winner.TransitionTo(Fighter.FighterState.Victory);
                var anim = winner.GetComponent<Fighter.FighterAnimator>();
                if (anim != null)
                {
                    victoryDuration = anim.VictoryDuration;
                }
            }

            // Phase 4: Wait for the victory animation to complete exactly one turn before starting next round
            yield return new WaitForSeconds(victoryDuration);

            // Phase 5: Start next round immediately (or show match over)
            CheckWinCondition();
        }

        /// <summary>
        /// Ends the round in a Draw (either Double K.O. or Overtime time-out survival),
        /// awarding 1 point to each player.
        /// </summary>
        public void EndRoundDraw(bool isDoubleKO)
        {
            if (currentMatchState == MatchState.RoundEnd || currentMatchState == MatchState.MatchOver) return;

            currentMatchState = MatchState.RoundEnd;

            p1RoundWins++;
            p2RoundWins++;

            Debug.Log($"<color=#ffaa00>[MatchManager]</color> Round {currentRound} DRAW! {(isDoubleKO ? "DOUBLE K.O.!" : "TIME-OUT DRAW (OVERTIME EXPIRED)!")} Both players awarded 1 point! (Score: P1={p1RoundWins} | P2={p2RoundWins})");

            OnRoundEnd?.Invoke(0, currentRound);

            StartCoroutine(RoundEndDrawSequence(isDoubleKO));
        }

        private System.Collections.IEnumerator RoundEndDrawSequence(bool isDoubleKO)
        {
            if (isDoubleKO)
            {
                // Both defeated fighters enter Knockout simultaneously
                if (p1Controller != null) p1Controller.TransitionTo(Fighter.FighterState.Knockout);
                if (p2Controller != null) p2Controller.TransitionTo(Fighter.FighterState.Knockout);

                Time.timeScale = 0.12f;

                if (Combat.VFXManager.Instance != null)
                {
                    Combat.VFXManager.Instance.TriggerImpactFlash(0.12f);
                    Combat.VFXManager.Instance.TriggerCameraShake(0.40f, 0.40f);
                }

                yield return new WaitForSecondsRealtime(4.0f);

                Time.timeScale = 1.0f;

                if (Combat.VFXManager.Instance != null)
                {
                    if (p1Controller != null) Combat.VFXManager.Instance.PlayDustPuff(p1Controller.transform.position);
                    if (p2Controller != null) Combat.VFXManager.Instance.PlayDustPuff(p2Controller.transform.position);
                    Combat.VFXManager.Instance.TriggerCameraShake(0.28f, 0.28f);
                }

                yield return new WaitForSeconds(roundEndPauseDuration);
            }
            else
            {
                // Time-Out Draw: Both fighters survived overtime! Neither collapses.
                if (Combat.VFXManager.Instance != null)
                {
                    Combat.VFXManager.Instance.TriggerCameraShake(0.25f, 0.25f);
                }

                yield return new WaitForSeconds(roundEndPauseDuration);
            }

            CheckWinCondition();
        }

        /// <summary>
        /// Checks if either player has won enough rounds to win the match, handling tiebreakers cleanly.
        /// </summary>
        public void CheckWinCondition()
        {
            if (p1RoundWins >= roundsToWin && p2RoundWins >= roundsToWin)
            {
                if (p1RoundWins == p2RoundWins)
                {
                    // Tied at match point (e.g. 2-2) -> Final Sudden Death Round!
                    Debug.Log("<color=#ffaa00>[MatchManager] MATCH TIED AT MATCH POINT! PROCEEDING TO FINAL SUDDEN DEATH ROUND!</color>");
                    StartRound();
                }
                else if (p1RoundWins > p2RoundWins)
                {
                    currentMatchState = MatchState.MatchOver;
                    Debug.Log("<color=#00e676>[MatchManager] MATCH OVER — Player 1 (Goku) WINS THE TOURNAMENT!</color>");
                    OnMatchOver?.Invoke(1);
                }
                else
                {
                    currentMatchState = MatchState.MatchOver;
                    Debug.Log("<color=#00e676>[MatchManager] MATCH OVER — Player 2 (Saitama) WINS THE TOURNAMENT!</color>");
                    OnMatchOver?.Invoke(2);
                }
            }
            else if (p1RoundWins >= roundsToWin)
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

        /// <summary>
        /// Completely restarts the match from Round 1, restoring full health, energy,
        /// positions, score (0-0), and unpauses the engine.
        /// </summary>
        public void RestartMatch()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            StopAllCoroutines();
            currentMatchState = MatchState.WaitingToStart;

            StartMatch();
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
            if (currentMatchState != MatchState.RoundInProgress) return;

            bool p1Alive = p1Health != null && p1Health.CurrentHealth > 0;
            bool p2Alive = p2Health != null && p2Health.CurrentHealth > 0;

            if (p1Alive && p2Alive)
            {
                if (!isOvertime)
                {
                    // Regular round time expired with neither fighter knocked out -> Grant +100s Overtime!
                    isOvertime = true;
                    roundTimer = 100f;

                    if (Combat.VFXManager.Instance != null)
                    {
                        Combat.VFXManager.Instance.TriggerImpactFlash(0.08f);
                        Combat.VFXManager.Instance.TriggerCameraShake(0.30f, 0.25f);
                    }

                    Debug.Log("<color=#ffdd00>[MatchManager] REGULAR TIME EXPIRED! Both fighters standing! OVERTIME EXTENSION: +100 SECONDS GRANTED (SUDDEN DEATH)!</color>");
                    OnOvertimeStarted?.Invoke();
                    return;
                }
                else
                {
                    // Overtime expired as well and both still remain unbeaten -> Draw! Award 1 point to each!
                    Debug.Log("<color=#ffaa00>[MatchManager] OVERTIME EXPIRED! Both fighters remain unbeaten! ROUND ENDS IN A DRAW!</color>");
                    EndRoundDraw(isDoubleKO: false);
                    return;
                }
            }

            // If one or both fighters were at 0 HP when time expired
            if (!p1Alive && !p2Alive)
            {
                EndRoundDraw(isDoubleKO: true);
            }
            else if (!p1Alive)
            {
                EndRound(2);
            }
            else
            {
                EndRound(1);
            }
        }

        private void OnFighterDefeated(int defeatedPlayerIndex)
        {
            if (currentMatchState != MatchState.RoundInProgress) return;
            if (evaluateDefeatCoroutine != null) return;
            evaluateDefeatCoroutine = StartCoroutine(EvaluateFighterDefeats());
        }

        private System.Collections.IEnumerator EvaluateFighterDefeats()
        {
            // Yield until end of frame so simultaneous hits / trades in the same physics tick register
            yield return new WaitForEndOfFrame();
            evaluateDefeatCoroutine = null;

            if (currentMatchState != MatchState.RoundInProgress) yield break;

            bool p1Dead = p1Health == null || p1Health.CurrentHealth <= 0;
            bool p2Dead = p2Health == null || p2Health.CurrentHealth <= 0;

            if (p1Dead && p2Dead)
            {
                // Double K.O.! Both defeated at the same time
                EndRoundDraw(isDoubleKO: true);
            }
            else if (p1Dead)
            {
                EndRound(2);
            }
            else if (p2Dead)
            {
                EndRound(1);
            }
        }

        #endregion
    }
}
