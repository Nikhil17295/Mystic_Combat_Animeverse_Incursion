using UnityEngine;

namespace MysticCombat.AI
{
    /// <summary>
    /// Goku-specific AI combat brain.
    /// Fighting Style: Disciplined Martial Artist & Mid-Range Zoner.
    /// - Maintains mid-range spacing (~2.2m) to bait whiffs.
    /// - Actively meditates at safe distance to build Ki.
    /// - Fires Kamehameha (Skill / E) from mid-range when Ki >= 20.
    /// - Hoards 80 Ki to unleash his Ultimate attack when the opponent is vulnerable or low HP.
    /// </summary>
    public class GokuBrain : CpuFighterBrain
    {
        #region Character-Specific Overrides

        protected override float PreferredDistance => 2.1f;
        protected override float MeleeRange => 1.15f;
        protected override float SkillRange => 2.7f;      // Kamehameha reaches across mid-distance
        protected override float SkillCost => 20f;
        protected override float UltimateCost => 80f;
        protected override float TargetKiHoard => 80f;

        #endregion

        #region Customized Combat Actions

        protected override void PerformMidRangeAction()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Goku signature: Kamehameha blast at mid-range!
            if (currentKi >= SkillCost && Random.value < (profile.comboChance * 0.85f))
            {
                SkillPressed = true;
                return;
            }

            // Occasional martial arts dash/hop
            if (Random.value < 0.20f)
            {
                JumpPressed = true;
            }
        }

        protected override void PerformMeleeCombo()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Ultimate check (Super Saiyan blast / Spirit Bomb)
            if (currentKi >= UltimateCost && Random.value < profile.comboChance)
            {
                UltimatePressed = true;
                return;
            }

            // Goku signature combo: Light jab canceled into Kamehameha flurry!
            if (currentKi >= SkillCost && Random.value < profile.comboChance)
            {
                SkillPressed = true;
                return;
            }

            // Disciplined punch-kick strings
            if (Random.value < 0.60f)
            {
                LightAttackPressed = true;
            }
            else
            {
                HeavyAttackPressed = true;
            }
        }

        #endregion
    }
}
