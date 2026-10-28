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
        public float reactionDelay = 0.35f;

        [Tooltip("Probability [0, 1] of hesitating or making a suboptimal spacing move.")]
        [Range(0f, 1f)] public float mistakeChance = 0.30f;

        [Header("Combat Tendencies")]
        [Tooltip("Probability [0, 1] of confirming a poke into Skill or Heavy attack.")]
        [Range(0f, 1f)] public float comboChance = 0.40f;

        [Tooltip("Distance variance allowed around preferred spacing.")]
        public float spacingTolerance = 0.6f;

        [Tooltip("Minimum distance from opponent to consider safe for Ki meditation.")]
        public float safeMeditateDistance = 3.2f;

        [Tooltip("Weight given to hoarding Ki for Ultimate move rather than spending on skills.")]
        [Range(0f, 1f)] public float kiHoardingBias = 0.5f;

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
                        reactionDelay = 0.40f, // Slower, humanized reaction
                        mistakeChance = 0.35f, // Frequent hesitation in neutral
                        comboChance = 0.30f,   // Rarely confirms long combos
                        spacingTolerance = 0.8f,
                        safeMeditateDistance = 3.5f,
                        kiHoardingBias = 0.3f,
                        healthMultiplier = 1.0f,
                        damageMultiplier = 1.0f
                    };

                case CpuDifficulty.Hard:
                    return new CpuBrainProfile
                    {
                        difficulty = CpuDifficulty.Hard,
                        reactionDelay = 0.14f, // Snappy arcade reflexes
                        mistakeChance = 0.08f, // Rarely hesitates
                        comboChance = 0.85f,   // Consistently combos
                        spacingTolerance = 0.35f,
                        safeMeditateDistance = 2.8f,
                        kiHoardingBias = 0.7f,
                        healthMultiplier = 1.0f,
                        damageMultiplier = 1.0f
                    };

                case CpuDifficulty.Extreme:
                    return new CpuBrainProfile
                    {
                        difficulty = CpuDifficulty.Extreme,
                        reactionDelay = 0.08f, // Razor-sharp tournament boss reflexes
                        mistakeChance = 0.02f, // Near-flawless execution
                        comboChance = 0.95f,   // Confirms almost all hits
                        spacingTolerance = 0.2f,
                        safeMeditateDistance = 2.5f,
                        kiHoardingBias = 0.85f,
                        healthMultiplier = 1.4f, // Boss stat boost
                        damageMultiplier = 1.25f
                    };

                default:
                    return new CpuBrainProfile();
            }
        }
    }
}
