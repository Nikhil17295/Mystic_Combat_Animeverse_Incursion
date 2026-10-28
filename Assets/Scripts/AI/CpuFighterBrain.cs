using UnityEngine;
using MysticCombat.Input;
using MysticCombat.Fighter;
using MysticCombat.Systems;

namespace MysticCombat.AI
{
    /// <summary>
    /// Base Hierarchical Finite State Machine (HFSM) controlling CPU fighters.
    /// Implements IFighterInput to emit virtual controller button presses and sticks.
    /// 
    /// Layer 1 (Macro): Tactical Posture (Neutral Spacing, Rushdown, Ki Hoarding, Defensive Retreat).
    /// Layer 2 (Micro): Concrete input actuation (Light, Heavy, Skill, Ultimate, Meditate, Jump, Crouch).
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class CpuFighterBrain : MonoBehaviour, IFighterInput
    {
        #region Macro States

        public enum MacroState
        {
            SpacingNeutral,
            AggressiveRushdown,
            KiHoarding,
            DefensiveRetreat,
            PunishOpening
        }

        #endregion

        #region Serialized Fields & Profile

        [Header("AI Profile & Tuning")]
        [SerializeField] protected CpuBrainProfile profile = new CpuBrainProfile();

        [Header("Current Tactical State (Debug View)")]
        [SerializeField] private MacroState currentMacroState = MacroState.SpacingNeutral;

        #endregion

        #region Cached References

        protected FighterController myController;
        protected HealthSystem myHealth;
        protected EnergySystem myEnergy;

        protected Transform opponent;
        protected FighterController opponentController;
        protected HealthSystem opponentHealth;
        protected EnergySystem opponentEnergy;

        #endregion

        #region Timers & Internal State

        private float reactionTimer;
        private float spacingOscillationTimer;
        private float spacingOscillationDir = 1f;
        private bool isHesitating;
        private float hesitationTimer;
        private float jumpCooldownTimer;
        private float attackCooldownTimer;
        private float crouchDefenseTimer;

        #endregion

        #region IFighterInput Implementation

        public Vector2 MoveInput { get; protected set; }
        public bool JumpPressed { get; protected set; }
        public bool CrouchHeld { get; protected set; }
        public bool LightAttackPressed { get; protected set; }
        public bool HeavyAttackPressed { get; protected set; }
        public bool SkillPressed { get; protected set; }
        public bool UltimatePressed { get; protected set; }
        public bool MeditateHeld { get; protected set; }

        #endregion

        #region Character-Specific Virtual Hooks (Customized by Subclasses)

        /// <summary>
        /// Preferred combat engagement distance from the opponent.
        /// </summary>
        protected virtual float PreferredDistance => 1.6f;

        /// <summary>
        /// Maximum horizontal distance for basic melee attacks (Light/Heavy).
        /// </summary>
        protected virtual float MeleeRange => 1.15f;

        /// <summary>
        /// Effective horizontal distance for special skill attacks.
        /// </summary>
        protected virtual float SkillRange => 2.2f;

        /// <summary>
        /// Ki required to activate special skill.
        /// </summary>
        protected virtual float SkillCost => 20f;

        /// <summary>
        /// Ki required to activate ultimate move.
        /// </summary>
        protected virtual float UltimateCost => 80f;

        /// <summary>
        /// Target Ki amount this character actively tries to build up and hoard.
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
            // Ensure opponent references are resolved
            if (opponent == null || opponentController == null)
            {
                FindOpponentReferences();
                if (opponent == null) return;
            }

            // Cooldown timers
            if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;
            if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
            if (crouchDefenseTimer > 0f)
            {
                crouchDefenseTimer -= Time.deltaTime;
                if (crouchDefenseTimer <= 0f) CrouchHeld = false;
            }

            // Humanized reaction tick timer
            reactionTimer -= Time.deltaTime;
            if (reactionTimer <= 0f)
            {
                reactionTimer = profile.reactionDelay * Random.Range(0.85f, 1.20f);
                EvaluateTacticalPosture();
            }

            // Execute tactical movements and virtual button actions
            ExecuteMacroState();
        }

        protected virtual void LateUpdate()
        {
            // Single-frame button pulses are cleared at the end of the frame
            // so FighterController sees them as a single clean key-press (just like wasPressedThisFrame).
            JumpPressed = false;
            LightAttackPressed = false;
            HeavyAttackPressed = false;
            SkillPressed = false;
            UltimatePressed = false;
        }

        #endregion

        #region Initialization & Configuration

        /// <summary>
        /// Configures the brain with a specific difficulty preset.
        /// </summary>
        public void Configure(CpuDifficulty difficulty)
        {
            profile = CpuBrainProfile.GetPreset(difficulty);
            ApplyDifficultyStatScaling();
            Debug.Log($"<color=#00e676>[CpuFighterBrain]</color> Configured {gameObject.name} with {difficulty} difficulty.");
        }

        /// <summary>
        /// Sets a custom profile directly.
        /// </summary>
        public void SetProfile(CpuBrainProfile customProfile)
        {
            profile = customProfile ?? CpuBrainProfile.GetPreset(CpuDifficulty.Easy);
            ApplyDifficultyStatScaling();
        }

        private void ApplyDifficultyStatScaling()
        {
            if (profile == null) return;

            // Extreme / Boss mode stat modifications
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

            // Fallback scene search
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

        #region Layer 1: Macro Tactical HFSM

        private void EvaluateTacticalPosture()
        {
            if (opponent == null) return;

            // Hesitation logic (simulates human indecision on Easy/Medium)
            if (hesitationTimer > 0f)
            {
                hesitationTimer -= profile.reactionDelay;
                return;
            }

            if (Random.value < profile.mistakeChance)
            {
                isHesitating = true;
                hesitationTimer = Random.Range(0.15f, 0.40f);
                MoveInput = Vector2.zero;
                MeditateHeld = false;
                CrouchHeld = false;
                return;
            }
            isHesitating = false;

            float dist = Mathf.Abs(transform.position.x - opponent.position.x);
            float myHpPct = (myHealth != null && myHealth.MaxHealth > 0) ? (float)myHealth.CurrentHealth / myHealth.MaxHealth : 1f;
            float oppHpPct = (opponentHealth != null && opponentHealth.MaxHealth > 0) ? (float)opponentHealth.CurrentHealth / opponentHealth.MaxHealth : 1f;
            float myKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;
            bool isMeExhausted = myEnergy != null && myEnergy.IsInRedZone;
            bool isOpponentExhausted = opponentEnergy != null && opponentEnergy.IsInRedZone;

            // Strategy Rule 1: Opponent is in Red Zone or low HP -> AGGRESSIVE RUSHDOWN (Smell blood in the water)
            if (isOpponentExhausted || (oppHpPct < 0.25f && myHpPct > oppHpPct))
            {
                currentMacroState = MacroState.AggressiveRushdown;
                return;
            }

            // Strategy Rule 2: I am in Red Zone (Exhausted) -> DEFENSIVE RETREAT or MEDITATE
            if (isMeExhausted)
            {
                currentMacroState = (dist > profile.safeMeditateDistance) ? MacroState.KiHoarding : MacroState.DefensiveRetreat;
                return;
            }

            // Strategy Rule 3: Opponent is whiffing or in recovery -> PUNISH OPENING
            if (dist <= SkillRange && IsOpponentInVulnerableAnimation())
            {
                currentMacroState = MacroState.PunishOpening;
                return;
            }

            // Strategy Rule 4: Safe distance + Low Ki + Desires Ki Hoard -> KI HOARDING
            if (dist >= profile.safeMeditateDistance && myKi < TargetKiHoard && Random.value < profile.kiHoardingBias)
            {
                currentMacroState = MacroState.KiHoarding;
                return;
            }

            // Strategy Rule 5: I have high Ki or Opponent is Meditating -> AGGRESSIVE RUSHDOWN
            if (myKi >= UltimateCost || (opponentController != null && opponentController.CurrentState == FighterState.Meditating))
            {
                currentMacroState = MacroState.AggressiveRushdown;
                return;
            }

            // Default State: SPACING NEUTRAL (Footsies & Spacing)
            currentMacroState = MacroState.SpacingNeutral;
        }

        private bool IsOpponentInVulnerableAnimation()
        {
            if (opponentController == null) return false;
            FighterState s = opponentController.CurrentState;
            return s == FighterState.HeavyAttack || s == FighterState.Ultimate || s == FighterState.Exhausted;
        }

        #endregion

        #region Layer 2: Micro Action Execution

        private void ExecuteMacroState()
        {
            if (opponent == null || isHesitating) return;

            float dist = Mathf.Abs(transform.position.x - opponent.position.x);
            float dirToOpponent = (opponent.position.x > transform.position.x) ? 1f : -1f;

            switch (currentMacroState)
            {
                case MacroState.SpacingNeutral:
                    ExecuteSpacingNeutral(dist, dirToOpponent);
                    break;

                case MacroState.AggressiveRushdown:
                    ExecuteAggressiveRushdown(dist, dirToOpponent);
                    break;

                case MacroState.KiHoarding:
                    ExecuteKiHoarding(dist, dirToOpponent);
                    break;

                case MacroState.DefensiveRetreat:
                    ExecuteDefensiveRetreat(dist, dirToOpponent);
                    break;

                case MacroState.PunishOpening:
                    ExecutePunishOpening(dist, dirToOpponent);
                    break;
            }
        }

        private void ExecuteSpacingNeutral(float dist, float dirToOpponent)
        {
            MeditateHeld = false;

            // Defensive Crouch Duck: occasional duck under opponent attack
            if (opponentController != null && 
               (opponentController.CurrentState == FighterState.LightAttack || opponentController.CurrentState == FighterState.HeavyAttack) &&
               dist <= 1.4f && crouchDefenseTimer <= 0f && Random.value < (0.45f * (1f - profile.mistakeChance)))
            {
                CrouchHeld = true;
                crouchDefenseTimer = Random.Range(0.25f, 0.45f);
                MoveInput = Vector2.zero;
                return;
            }

            // Weave back and forth around preferred distance
            float distanceError = dist - PreferredDistance;

            if (Mathf.Abs(distanceError) > profile.spacingTolerance)
            {
                // Move towards preferred spacing
                float moveX = Mathf.Sign(distanceError) * dirToOpponent;
                MoveInput = new Vector2(moveX, 0f);
            }
            else
            {
                // Opponent-relative footsies oscillation (step in to threaten, step back to bait)
                spacingOscillationTimer -= Time.deltaTime;
                if (spacingOscillationTimer <= 0f)
                {
                    spacingOscillationTimer = Random.Range(0.35f, 0.65f);
                    spacingOscillationDir *= -1f;
                }
                float relativeStep = (spacingOscillationDir > 0f) ? dirToOpponent * 0.45f : -dirToOpponent * 0.35f;
                MoveInput = new Vector2(relativeStep, 0f);

                // In spacing range, attack with humanized rhythm
                if (attackCooldownTimer <= 0f)
                {
                    if (dist <= MeleeRange)
                    {
                        PerformMeleePoke();
                        attackCooldownTimer = profile.reactionDelay * Random.Range(1.3f, 2.0f);
                    }
                    else if (dist <= SkillRange)
                    {
                        PerformMidRangeAction();
                        attackCooldownTimer = profile.reactionDelay * Random.Range(1.5f, 2.4f);
                    }
                }
            }
        }

        private void ExecuteAggressiveRushdown(float dist, float dirToOpponent)
        {
            MeditateHeld = false;
            CrouchHeld = false;

            if (dist > MeleeRange)
            {
                // Sprint / Walk forward
                MoveInput = new Vector2(dirToOpponent, 0f);

                // Tactical jump-in with strict cooldown gating
                if (jumpCooldownTimer <= 0f && dist > 1.8f && dist < 3.2f && Random.value < 0.25f)
                {
                    JumpPressed = true;
                    jumpCooldownTimer = Random.Range(2.0f, 3.2f);
                }
            }
            else
            {
                // Close range: execute combo pressure with natural cadence
                MoveInput = new Vector2(dirToOpponent * 0.2f, 0f);
                if (attackCooldownTimer <= 0f)
                {
                    PerformMeleeCombo();
                    attackCooldownTimer = profile.reactionDelay * Random.Range(0.8f, 1.4f);
                }
            }
        }

        private void ExecuteKiHoarding(float dist, float dirToOpponent)
        {
            bool isCornered = (dirToOpponent > 0 && transform.position.x < -7.2f) || (dirToOpponent < 0 && transform.position.x > 7.2f);

            if (isCornered || dist < profile.safeMeditateDistance)
            {
                // Cannot retreat further into wall or opponent is too close!
                MeditateHeld = false;
                if (isCornered)
                {
                    // Escape jump over opponent if ready, else fight out of corner
                    if (jumpCooldownTimer <= 0f)
                    {
                        MoveInput = new Vector2(dirToOpponent, 0f);
                        JumpPressed = true;
                        jumpCooldownTimer = 2.5f;
                    }
                    else
                    {
                        currentMacroState = MacroState.AggressiveRushdown;
                    }
                }
                else
                {
                    MoveInput = new Vector2(-dirToOpponent, 0f);
                }
            }
            else
            {
                // Safe distance: Hold meditation!
                MoveInput = Vector2.zero;
                MeditateHeld = true;

                // Stop meditating if Ki reached target
                if (myEnergy != null && myEnergy.CurrentKi >= TargetKiHoard)
                {
                    MeditateHeld = false;
                    currentMacroState = MacroState.SpacingNeutral;
                }
            }
        }

        private void ExecuteDefensiveRetreat(float dist, float dirToOpponent)
        {
            MeditateHeld = false;
            bool isCornered = (dirToOpponent > 0 && transform.position.x < -7.2f) || (dirToOpponent < 0 && transform.position.x > 7.2f);

            if (isCornered)
            {
                // If pinned to the boundary wall, jump out or punish!
                if (jumpCooldownTimer <= 0f)
                {
                    MoveInput = new Vector2(dirToOpponent, 0f);
                    JumpPressed = true;
                    jumpCooldownTimer = 2.5f;
                }
                else
                {
                    currentMacroState = MacroState.PunishOpening;
                }
                return;
            }

            // Move away from opponent
            MoveInput = new Vector2(-dirToOpponent, 0f);

            // Occasional defensive escape jump with cooldown gate
            if (jumpCooldownTimer <= 0f && dist < 1.4f && Random.value < 0.35f)
            {
                JumpPressed = true;
                jumpCooldownTimer = Random.Range(2.0f, 3.0f);
            }
        }

        private void ExecutePunishOpening(float dist, float dirToOpponent)
        {
            MeditateHeld = false;
            CrouchHeld = false;

            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // If in lethal range and have Ki for Ultimate, PUNISH WITH ULTIMATE!
            if (currentKi >= UltimateCost && dist <= SkillRange && attackCooldownTimer <= 0f)
            {
                UltimatePressed = true;
                attackCooldownTimer = 1.0f;
                currentMacroState = MacroState.SpacingNeutral;
                return;
            }

            // If have Ki for Skill, punish with Skill!
            if (currentKi >= SkillCost && dist <= SkillRange && attackCooldownTimer <= 0f)
            {
                SkillPressed = true;
                attackCooldownTimer = 0.8f;
                currentMacroState = MacroState.SpacingNeutral;
                return;
            }

            // Melee range punish with Heavy Attack
            if (dist <= MeleeRange && attackCooldownTimer <= 0f)
            {
                HeavyAttackPressed = true;
                attackCooldownTimer = 0.6f;
                currentMacroState = MacroState.SpacingNeutral;
                return;
            }

            // Otherwise, close in fast
            MoveInput = new Vector2(dirToOpponent, 0f);
        }

        #endregion

        #region Attack Routines & Virtual Hooks

        protected virtual void PerformMeleePoke()
        {
            // Light jab poke
            if (Random.value < 0.65f)
            {
                LightAttackPressed = true;
            }
            else
            {
                HeavyAttackPressed = true;
            }
        }

        protected virtual void PerformMeleeCombo()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Ultimate check (if affordable and confirmed)
            if (currentKi >= UltimateCost && Random.value < profile.comboChance)
            {
                UltimatePressed = true;
                return;
            }

            // Skill check (if affordable and confirmed)
            if (currentKi >= SkillCost && Random.value < profile.comboChance)
            {
                SkillPressed = true;
                return;
            }

            // Regular hit chain
            if (Random.value < 0.5f)
            {
                LightAttackPressed = true;
            }
            else
            {
                HeavyAttackPressed = true;
            }
        }

        protected virtual void PerformMidRangeAction()
        {
            // Base behavior: poke if affordable
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;
            if (currentKi >= SkillCost && Random.value < (profile.comboChance * 0.7f))
            {
                SkillPressed = true;
            }
        }

        #endregion
    }
}
