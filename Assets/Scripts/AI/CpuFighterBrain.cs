using UnityEngine;
using MysticCombat.Input;
using MysticCombat.Fighter;
using MysticCombat.Systems;
using MysticCombat.Core;

namespace MysticCombat.AI
{
    /// <summary>
    /// Advanced Hierarchical Finite State Machine (HFSM) controlling CPU fighters.
    /// Implements IFighterInput to emit frame-reliable virtual controller button presses.
    /// 
    /// Layer 1: Tactical Postures (Rushdown, Punish, Spacing, KiCharge, ExhaustionRecovery).
    /// Layer 2: Frame-latched virtual input actuator (guarantees inputs are never dropped
    ///          regardless of Unity component execution order).
    /// Layer 3: Dynamic combo engine utilizing hit-confirms and chain-canceling.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class CpuFighterBrain : MonoBehaviour, IFighterInput
    {
        #region Tactical States

        public enum TacticalState
        {
            RushdownPressure,
            PunishOpening,
            SpacingNeutral,
            KiCharge,
            ExhaustionRecovery
        }

        #endregion

        #region Serialized Fields & Profiles

        [Header("AI Profile & Tuning")]
        [SerializeField] protected CpuBrainProfile profile = new CpuBrainProfile();

        [Header("Current Tactical State (Debug View)")]
        [SerializeField] private TacticalState currentState = TacticalState.RushdownPressure;

        #endregion

        #region Cached References

        protected FighterController myController;
        protected HealthSystem myHealth;
        protected EnergySystem myEnergy;

        protected Transform opponent;
        protected FighterController opponentController;
        protected HealthSystem opponentHealth;
        protected EnergySystem opponentEnergy;

        private MatchManager cachedMatchManager;

        #endregion

        #region Input Latching (Guarantees Zero Dropped Inputs)

        private int jumpLatch;
        private int lightLatch;
        private int heavyLatch;
        private int skillLatch;
        private int ultimateLatch;

        public Vector2 MoveInput { get; protected set; }
        public bool JumpPressed => jumpLatch > 0;
        public bool CrouchHeld { get; protected set; }
        public bool LightAttackPressed => lightLatch > 0;
        public bool HeavyAttackPressed => heavyLatch > 0;
        public bool SkillPressed => skillLatch > 0;
        public bool UltimatePressed => ultimateLatch > 0;
        public bool MeditateHeld { get; protected set; }

        #endregion

        #region Timers & Internal Counters

        private float reactionTimer;
        private float decisionTimer;
        private float attackCooldownTimer;
        private float jumpCooldownTimer;
        private float crouchDefenseTimer;
        private float footsiesTimer;
        private float footsiesDir = 1f;

        #endregion

        #region Character Overrides (Customized by GokuBrain & SaitamaBrain)

        /// <summary>
        /// Ideal neutral combat spacing.
        /// </summary>
        protected virtual float PreferredDistance => 1.45f;

        /// <summary>
        /// Maximum range where basic melee attacks reliably land (matches actual hitbox overlap).
        /// </summary>
        protected virtual float MeleeStrikeRange => 1.55f;

        /// <summary>
        /// Maximum horizontal range for special skills.
        /// </summary>
        protected virtual float SkillStrikeRange => 2.2f;

        /// <summary>
        /// Ki required for special skill.
        /// </summary>
        protected virtual float SkillCost => 20f;

        /// <summary>
        /// Ki required for ultimate finisher.
        /// </summary>
        protected virtual float UltimateCost => 80f;

        /// <summary>
        /// Target Ki amount before looking for ultimate finish.
        /// </summary>
        protected virtual float TargetKiHoard => 80f;

        #endregion

        #region Unity Callbacks

        protected virtual void Awake()
        {
            myController = GetComponent<FighterController>();
            myHealth = GetComponent<HealthSystem>();
            myEnergy = GetComponent<EnergySystem>();
        }

        protected virtual void Start()
        {
            FindOpponentReferences();
            ApplyDifficultyStatScaling();
        }

        protected virtual void Update()
        {
            // Decrement input latch counters frame by frame
            // Latching for 2 frames guarantees FighterController.Update sees the button press
            // whether FighterController runs before or after CpuFighterBrain.
            if (jumpLatch > 0) jumpLatch--;
            if (lightLatch > 0) lightLatch--;
            if (heavyLatch > 0) heavyLatch--;
            if (skillLatch > 0) skillLatch--;
            if (ultimateLatch > 0) ultimateLatch--;

            // Timers
            if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
            if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;
            if (crouchDefenseTimer > 0f)
            {
                crouchDefenseTimer -= Time.deltaTime;
                if (crouchDefenseTimer <= 0f) CrouchHeld = false;
            }

            // Ensure opponent references are valid
            if (opponent == null || opponentController == null)
            {
                FindOpponentReferences();
                if (opponent == null) return;
            }

            // Verify match is actively running
            if (!IsRoundActive())
            {
                ClearAllInputs();
                return;
            }

            // High-frequency hit-confirm chain canceling (Checked every frame!)
            CheckHitConfirmComboChain();

            // Tactical decision evaluation tick
            reactionTimer -= Time.deltaTime;
            if (reactionTimer <= 0f)
            {
                reactionTimer = profile.reactionDelay * Random.Range(0.85f, 1.15f);
                EvaluateTacticalState();
            }

            // Execute tactical state movements & actuation
            ExecuteTacticalState();
        }

        #endregion

        #region Match State Tracking

        private bool IsRoundActive()
        {
            if (Time.timeScale == 0f) return false;

            if (cachedMatchManager == null)
            {
                cachedMatchManager = Object.FindAnyObjectByType<MatchManager>();
            }

            if (cachedMatchManager != null)
            {
                return cachedMatchManager.CurrentMatchState == MatchManager.MatchState.RoundInProgress;
            }

            return true;
        }

        private void ClearAllInputs()
        {
            MoveInput = Vector2.zero;
            CrouchHeld = false;
            MeditateHeld = false;
            jumpLatch = 0;
            lightLatch = 0;
            heavyLatch = 0;
            skillLatch = 0;
            ultimateLatch = 0;
        }

        #endregion

        #region Initialization & Scaling

        public void Configure(CpuDifficulty difficulty)
        {
            profile = CpuBrainProfile.GetPreset(difficulty);
            ApplyDifficultyStatScaling();
            Debug.Log($"<color=#00e676>[CpuFighterBrain]</color> Configured {gameObject.name} with {difficulty} difficulty.");
        }

        public void SetProfile(CpuBrainProfile customProfile)
        {
            profile = customProfile ?? CpuBrainProfile.GetPreset(CpuDifficulty.Easy);
            ApplyDifficultyStatScaling();
        }

        private void ApplyDifficultyStatScaling()
        {
            if (profile == null) return;

            if (profile.healthMultiplier > 1.01f && myHealth != null)
            {
                int scaledMaxHp = Mathf.RoundToInt(myHealth.MaxHealth * profile.healthMultiplier);
                myHealth.Initialize(scaledMaxHp);
            }
        }

        private void FindOpponentReferences()
        {
            if (myController != null && myController.opponent != null)
            {
                opponent = myController.opponent;
                opponentController = opponent.GetComponent<FighterController>();
                opponentHealth = opponent.GetComponent<HealthSystem>();
                opponentEnergy = opponent.GetComponent<EnergySystem>();
                return;
            }

            FighterController[] allFighters = FindObjectsByType<FighterController>(FindObjectsInactive.Exclude);
            foreach (var f in allFighters)
            {
                if (f != null && f.gameObject != this.gameObject)
                {
                    opponent = f.transform;
                    opponentController = f;
                    opponentHealth = f.GetComponent<HealthSystem>();
                    opponentEnergy = f.GetComponent<EnergySystem>();
                    if (myController != null) myController.opponent = opponent;
                    break;
                }
            }
        }

        #endregion

        #region Tactical HFSM State Evaluation

        private void EvaluateTacticalState()
        {
            if (opponent == null) return;

            float dist = Mathf.Abs(transform.position.x - opponent.position.x);
            float myKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;
            bool isMeExhausted = myEnergy != null && myEnergy.IsInRedZone;
            bool isOpponentExhausted = opponentEnergy != null && opponentEnergy.IsInRedZone;
            bool isOpponentVulnerable = IsOpponentVulnerable();

            // Priority 1: Self Exhausted in Red Zone
            if (isMeExhausted)
            {
                currentState = TacticalState.ExhaustionRecovery;
                return;
            }

            // Priority 2: Opponent is in Red Zone, in HitStun, or Whiffing -> PUNISH OPENING!
            if (isOpponentExhausted || isOpponentVulnerable)
            {
                currentState = TacticalState.PunishOpening;
                return;
            }

            // Priority 3: Safe distance + Low Ki -> Meditate to charge Ki
            if (dist >= profile.safeMeditateDistance && myKi < TargetKiHoard && Random.value < profile.kiHoardingBias)
            {
                currentState = TacticalState.KiCharge;
                return;
            }

            // Priority 4: Default combat posture is AGGRESSIVE RUSHDOWN
            // The bot actively seeks out combat rather than backing away
            if (dist > MeleeStrikeRange * 1.15f || myKi >= UltimateCost)
            {
                currentState = TacticalState.RushdownPressure;
                return;
            }

            // Priority 5: Close-range neutral footsies
            currentState = TacticalState.SpacingNeutral;
        }

        private bool IsOpponentVulnerable()
        {
            if (opponentController == null) return false;
            FighterState s = opponentController.CurrentState;
            return s == FighterState.HitStun || 
                   s == FighterState.KnockBack || 
                   s == FighterState.Exhausted ||
                   s == FighterState.HeavyAttack || 
                   s == FighterState.Ultimate || 
                   s == FighterState.Meditating;
        }

        #endregion

        #region Tactical State Execution

        private void ExecuteTacticalState()
        {
            if (opponent == null) return;

            float dx = opponent.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            float dirToOpponent = (dx >= 0f) ? 1f : -1f;

            // Defensive crouch reaction to incoming heavy/ultimate attacks
            TryDefensiveCrouchDuck(dist);

            switch (currentState)
            {
                case TacticalState.RushdownPressure:
                    ExecuteRushdownPressure(dist, dirToOpponent);
                    break;

                case TacticalState.PunishOpening:
                    ExecutePunishOpening(dist, dirToOpponent);
                    break;

                case TacticalState.SpacingNeutral:
                    ExecuteSpacingNeutral(dist, dirToOpponent);
                    break;

                case TacticalState.KiCharge:
                    ExecuteKiCharge(dist, dirToOpponent);
                    break;

                case TacticalState.ExhaustionRecovery:
                    ExecuteExhaustionRecovery(dist, dirToOpponent);
                    break;
            }
        }

        private void ExecuteRushdownPressure(float dist, float dirToOpponent)
        {
            MeditateHeld = false;
            CrouchHeld = false;

            if (dist > MeleeStrikeRange)
            {
                // Advance forward with intent
                MoveInput = new Vector2(dirToOpponent, 0f);

                // Tactical jump-in when closing medium distance
                if (jumpCooldownTimer <= 0f && dist > 2.0f && dist < 3.5f && Random.value < 0.20f)
                {
                    TriggerJump();
                    jumpCooldownTimer = Random.Range(2.5f, 4.0f);
                }
            }
            else
            {
                // In close strike range: attack relentlessly!
                MoveInput = new Vector2(dirToOpponent * 0.15f, 0f);

                if (attackCooldownTimer <= 0f && CanAttackGrounded())
                {
                    PerformCloseRangeAttack();
                    attackCooldownTimer = profile.reactionDelay * Random.Range(0.6f, 1.1f);
                }
            }
        }

        private void ExecutePunishOpening(float dist, float dirToOpponent)
        {
            MeditateHeld = false;
            CrouchHeld = false;

            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Close in fast to punish
            if (dist > MeleeStrikeRange)
            {
                MoveInput = new Vector2(dirToOpponent, 0f);
            }
            else
            {
                MoveInput = Vector2.zero;
            }

            if (attackCooldownTimer <= 0f && CanAttackGrounded())
            {
                // Punish with Ultimate if affordable and in range
                if (currentKi >= UltimateCost && dist <= SkillStrikeRange)
                {
                    TriggerUltimate();
                    attackCooldownTimer = 1.0f;
                    currentState = TacticalState.RushdownPressure;
                    return;
                }

                // Punish with Skill if affordable
                if (currentKi >= SkillCost && dist <= SkillStrikeRange)
                {
                    TriggerSkill();
                    attackCooldownTimer = 0.8f;
                    currentState = TacticalState.RushdownPressure;
                    return;
                }

                // Melee punish with Heavy attack or Light jab
                if (dist <= MeleeStrikeRange)
                {
                    if (Random.value < 0.70f)
                    {
                        TriggerHeavyAttack();
                    }
                    else
                    {
                        TriggerLightAttack();
                    }
                    attackCooldownTimer = 0.5f;
                    currentState = TacticalState.RushdownPressure;
                }
            }
        }

        private void ExecuteSpacingNeutral(float dist, float dirToOpponent)
        {
            MeditateHeld = false;

            // If player walks right up into melee range, counter-attack immediately!
            if (dist <= MeleeStrikeRange && attackCooldownTimer <= 0f && CanAttackGrounded())
            {
                PerformCloseRangeAttack();
                attackCooldownTimer = profile.reactionDelay * Random.Range(0.7f, 1.2f);
                return;
            }

            // Footsies: step back and forward around PreferredDistance
            footsiesTimer -= Time.deltaTime;
            if (footsiesTimer <= 0f)
            {
                footsiesTimer = Random.Range(0.25f, 0.55f);
                footsiesDir *= -1f;
            }

            float offsetFromPreferred = dist - PreferredDistance;
            if (Mathf.Abs(offsetFromPreferred) > profile.spacingTolerance)
            {
                MoveInput = new Vector2(Mathf.Sign(offsetFromPreferred) * dirToOpponent, 0f);
            }
            else
            {
                MoveInput = new Vector2(footsiesDir * dirToOpponent * 0.35f, 0f);
            }
        }

        private void ExecuteKiCharge(float dist, float dirToOpponent)
        {
            // If opponent gets too close, immediately drop meditation!
            if (dist < 2.4f)
            {
                MeditateHeld = false;
                currentState = TacticalState.RushdownPressure;
                return;
            }

            // Safe distance: hold meditation
            MoveInput = Vector2.zero;
            MeditateHeld = true;

            // Stop when Ki reached target
            if (myEnergy != null && myEnergy.CurrentKi >= TargetKiHoard)
            {
                MeditateHeld = false;
                currentState = TacticalState.RushdownPressure;
            }
        }

        private void ExecuteExhaustionRecovery(float dist, float dirToOpponent)
        {
            // Meditating while exhausted accelerates Red Zone recovery by 1.5x!
            if (dist >= 2.2f)
            {
                MoveInput = Vector2.zero;
                MeditateHeld = true;
            }
            else
            {
                // Opponent is close: retreat or defend
                MeditateHeld = false;
                bool isCornered = (dirToOpponent > 0 && transform.position.x < -7.0f) || (dirToOpponent < 0 && transform.position.x > 7.0f);

                if (isCornered && jumpCooldownTimer <= 0f)
                {
                    MoveInput = new Vector2(dirToOpponent, 0f);
                    TriggerJump();
                    jumpCooldownTimer = 2.5f;
                }
                else
                {
                    MoveInput = new Vector2(-dirToOpponent, 0f);
                }
            }

            // Recovered from Red Zone!
            if (myEnergy != null && !myEnergy.IsInRedZone)
            {
                MeditateHeld = false;
                currentState = TacticalState.RushdownPressure;
            }
        }

        #endregion

        #region Hit-Confirm & Chain-Canceling Engine

        /// <summary>
        /// Checked every frame: If currently in LightAttack and a hit is confirmed on the opponent,
        /// immediately chain-cancel into Heavy or Skill before the opponent recovers!
        /// </summary>
        private void CheckHitConfirmComboChain()
        {
            if (myController == null) return;
            if (myController.CurrentState != FighterState.LightAttack) return;

            bool hitConfirmed = myController.HasHitTargetThisSwing || 
                                (opponentController != null && opponentController.CurrentState == FighterState.HitStun);

            if (!hitConfirmed) return;

            // Decide whether to chain-cancel based on combo profile
            if (Random.value < profile.comboChance)
            {
                float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

                // Chain-cancel into Skill if affordable
                if (currentKi >= SkillCost && Random.value < 0.65f)
                {
                    TriggerSkill();
                }
                else
                {
                    // Chain-cancel into Heavy attack
                    TriggerHeavyAttack();
                }
            }
        }

        #endregion

        #region Virtual Input Trigger Helpers (Guaranteed 2-Frame Latch)

        protected void TriggerLightAttack()
        {
            lightLatch = 2;
        }

        protected void TriggerHeavyAttack()
        {
            heavyLatch = 2;
        }

        protected void TriggerSkill()
        {
            skillLatch = 2;
        }

        protected void TriggerUltimate()
        {
            ultimateLatch = 2;
        }

        protected void TriggerJump()
        {
            jumpLatch = 2;
        }

        private bool CanAttackGrounded()
        {
            if (myController == null || !myController.IsGrounded) return false;
            FighterState s = myController.CurrentState;
            return s == FighterState.Idle || s == FighterState.Walking;
        }

        private void TryDefensiveCrouchDuck(float dist)
        {
            if (opponentController == null || dist > 1.6f) return;

            FighterState os = opponentController.CurrentState;
            if ((os == FighterState.HeavyAttack || os == FighterState.Ultimate) && 
                crouchDefenseTimer <= 0f && 
                Random.value < (0.50f * (1f - profile.mistakeChance)))
            {
                CrouchHeld = true;
                crouchDefenseTimer = Random.Range(0.25f, 0.45f);
                MoveInput = Vector2.zero;
            }
        }

        #endregion

        #region Virtual Attack Hooks (Overridden by GokuBrain & SaitamaBrain)

        protected virtual void PerformCloseRangeAttack()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // If we have Ultimate Ki and high combo chance, strike with Ultimate!
            if (currentKi >= UltimateCost && Random.value < profile.comboChance)
            {
                TriggerUltimate();
                return;
            }

            // Open with fast Light jab (which triggers chain-canceling on hit!)
            if (Random.value < 0.65f)
            {
                TriggerLightAttack();
            }
            else
            {
                TriggerHeavyAttack();
            }
        }

        #endregion
    }
}
