using UnityEngine;

namespace MysticCombat.AI
{
    /// <summary>
    /// Supported CPU difficulty levels.
    /// </summary>
    public enum CpuDifficulty
    {
        Easy,
        Hard,
        Extreme
    }

    /// <summary>
    /// Configurable tactical parameters for a CPU bot.
    /// Drives the Hierarchical FSM's reaction speed, hesitation, combo rates, pacing, and stat scaling.
    /// </summary>
    [System.Serializable]
    public class CpuBrainProfile
    {
        [Header("Difficulty Level")]
        public CpuDifficulty difficulty = CpuDifficulty.Easy;

        [Header("Reaction & Perception")]
        [Tooltip("Evaluation tick interval in seconds (lower = faster reaction).")]
        public float reactionDelay = 0.25f;

        [Tooltip("Probability [0, 1] of hesitating or making a suboptimal spacing move.")]
        [Range(0f, 1f)] public float mistakeChance = 0.15f;

        [Header("Combat Tendencies")]
        [Tooltip("Probability [0, 1] of confirming a poke into Skill or Heavy attack.")]
        [Range(0f, 1f)] public float comboChance = 0.55f;

        [Tooltip("Distance variance allowed around preferred spacing.")]
        public float spacingTolerance = 0.5f;

        [Tooltip("Minimum distance from opponent to consider safe for Ki meditation.")]
        public float safeMeditateDistance = 3.2f;

        [Tooltip("Weight given to hoarding Ki for Ultimate move rather than spending on skills.")]
        [Range(0f, 1f)] public float kiHoardingBias = 0.45f;

        [Header("Pacing & Rest Windows (Anti-Mindless Rushdown)")]
        [Tooltip("Minimum duration bot pauses / resets to neutral after an attack engagement (seconds).")]
        public float restDurationMin = 0.4f;

        [Tooltip("Maximum duration bot pauses / resets to neutral after an attack engagement (seconds).")]
        public float restDurationMax = 0.8f;

        [Tooltip("Probability of retreating to meditate when opponent is knocked down or far away.")]
        [Range(0f, 1f)] public float tacticalMeditationChance = 0.50f;

        [Header("Tactical Retreat & Disengagement (Anti-Smothering)")]
        [Tooltip("Probability [0, 1] of backing away and disengaging after an attack engagement.")]
        [Range(0f, 1f)] public float retreatChance = 0.80f;

        [Tooltip("Minimum duration bot actively walks backward away from player (seconds).")]
        public float retreatDurationMin = 1.5f;

        [Tooltip("Maximum duration bot actively walks backward away from player (seconds).")]
        public float retreatDurationMax = 2.5f;

        [Header("Tactical Defense & Red Zone Awareness")]
        [Tooltip("Probability [0, 1] of crouch-ducking under high / heavy attacks.")]
        [Range(0f, 1f)] public float crouchDuckChance = 0.50f;

        [Tooltip("Ki threshold below which the bot stops voluntary Ki spending to avoid entering Red Zone.")]
        public float redZoneSafeThreshold = 15f;

        [Header("Desperation Mode (< 30% HP)")]
        [Tooltip("HP percentage threshold to trigger fierce desperation mode.")]
        public float desperationThreshold = 0.30f;

        [Tooltip("Aggression and attack frequency multiplier when desperate.")]
        public float desperationAggressionBoost = 1.35f;

        [Header("Stat Multipliers (Extreme Mode)")]
        public float healthMultiplier = 1.0f;
        public float damageMultiplier = 1.0f;

        /// <summary>
        /// Generates a tuned preset profile for the specified difficulty.
        /// </summary>
        public static CpuBrainProfile GetPreset(CpuDifficulty diff)
        {
            switch (diff)
            {
                case CpuDifficulty.Easy:
                    return new CpuBrainProfile
                    {
                        difficulty = CpuDifficulty.Easy,
                        reactionDelay = 0.45f,              // Generous humanized reaction window
                        mistakeChance = 0.50f,              // Frequently hesitates, whiffs & misjudges spacing
                        comboChance = 0.00f,                // Combos unlearned! Single, readable strikes only
                        spacingTolerance = 0.75f,
                        safeMeditateDistance = 3.8f,
                        kiHoardingBias = 0.30f,
                        restDurationMin = 1.20f,            // Natural resting interval
                        restDurationMax = 2.00f,
                        retreatChance = 0.90f,              // 90% chance to retreat after attacking!
                        retreatDurationMin = 1.80f,         // Backs up across stage for 1.8s - 2.8s
                        retreatDurationMax = 2.80f,         // Gives player massive room to attack/breathe
                        tacticalMeditationChance = 0.15f,
                        crouchDuckChance = 0.05f,
                        redZoneSafeThreshold = 5f,
                        desperationThreshold = 0.25f,
                        desperationAggressionBoost = 1.10f,
                        healthMultiplier = 1.0f,
                        damageMultiplier = 0.85f
                    };

                case CpuDifficulty.Hard:
                    return new CpuBrainProfile
                    {
                        difficulty = CpuDifficulty.Hard,
                        reactionDelay = 0.22f,              // Realistic human reaction time
                        mistakeChance = 0.18f,
                        comboChance = 0.00f,                // Combos unlearned! Distinct single strikes
                        spacingTolerance = 0.40f,
                        safeMeditateDistance = 2.8f,
                        kiHoardingBias = 0.65f,
                        restDurationMin = 0.60f,
                        restDurationMax = 1.00f,
                        retreatChance = 0.70f,              // 70% chance to retreat after attacking
                        retreatDurationMin = 1.00f,         // Steps back for 1.0s - 1.8s to reset footsies
                        retreatDurationMax = 1.80f,
                        tacticalMeditationChance = 0.50f,
                        crouchDuckChance = 0.30f,
                        redZoneSafeThreshold = 15f,
                        desperationThreshold = 0.30f,
                        desperationAggressionBoost = 1.25f,
                        healthMultiplier = 1.0f,
                        damageMultiplier = 1.0f
                    };

                case CpuDifficulty.Extreme:
                    return new CpuBrainProfile
                    {
                        difficulty = CpuDifficulty.Extreme,
                        reactionDelay = 0.04f,              // Frame-tight tournament reflexes
                        mistakeChance = 0.00f,              // Flawless execution
                        comboChance = 0.00f,                // Combos unlearned! Distinct, heavy blows
                        spacingTolerance = 0.18f,
                        safeMeditateDistance = 2.2f,
                        kiHoardingBias = 0.85f,
                        restDurationMin = 0.05f,
                        restDurationMax = 0.15f,
                        retreatChance = 0.40f,              // 40% chance for tactical spacing reset
                        retreatDurationMin = 0.40f,
                        retreatDurationMax = 0.80f,
                        tacticalMeditationChance = 0.90f,
                        crouchDuckChance = 0.85f,
                        redZoneSafeThreshold = 18f,
                        desperationThreshold = 0.35f,
                        desperationAggressionBoost = 1.50f,
                        healthMultiplier = 1.35f,
                        damageMultiplier = 1.25f
                    };

                default:
                    return new CpuBrainProfile();
            }
        }
    }
}
