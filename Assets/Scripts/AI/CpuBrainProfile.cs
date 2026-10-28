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
    /// Drives the Hierarchical FSM's reaction speed, hesitation, combo rates, and stat scaling.
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
                        reactionDelay = 0.22f, // Humanized, responsive enough to actively fight
                        mistakeChance = 0.15f, // Occasional spacing error
                        comboChance = 0.60f,   // Can execute 2-hit combos
                        spacingTolerance = 0.55f,
                        safeMeditateDistance = 3.2f,
                        kiHoardingBias = 0.40f,
                        healthMultiplier = 1.0f,
                        damageMultiplier = 1.0f
                    };

                case CpuDifficulty.Hard:
                    return new CpuBrainProfile
                    {
                        difficulty = CpuDifficulty.Hard,
                        reactionDelay = 0.10f, // Snappy fighting game reflexes
                        mistakeChance = 0.04f, // Rarely drops combos
                        comboChance = 0.88f,   // Consistently confirms chain cancels
                        spacingTolerance = 0.30f,
                        safeMeditateDistance = 2.6f,
                        kiHoardingBias = 0.70f,
                        healthMultiplier = 1.0f,
                        damageMultiplier = 1.0f
                    };

                case CpuDifficulty.Extreme:
                    return new CpuBrainProfile
                    {
                        difficulty = CpuDifficulty.Extreme,
                        reactionDelay = 0.04f, // Tournament boss reflexes
                        mistakeChance = 0.00f, // Flawless execution
                        comboChance = 0.98f,   // Masterful combo confirms
                        spacingTolerance = 0.18f,
                        safeMeditateDistance = 2.2f,
                        kiHoardingBias = 0.85f,
                        healthMultiplier = 1.35f, // Boss stat boost
                        damageMultiplier = 1.25f
                    };

                default:
                    return new CpuBrainProfile();
            }
        }
    }
}
