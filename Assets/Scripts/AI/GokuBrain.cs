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

        protected override float PreferredDistance => 2.2f;
        protected override float MeleeRange => 1.20f;
        protected override float SkillRange => 3.5f;      // Kamehameha reaches 3.6m across mid/long-distance!
        protected override float SkillCost => 20f;
        protected override float UltimateCost => 80f;
        protected override float TargetKiHoard => 80f;

        #endregion

        #region Customized Combat Actions

        protected override void PerformMidRangeAction()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Goku signature: Kamehameha Ultimate blast across mid/long-range!
            if (currentKi >= UltimateCost && Random.value < (profile.comboChance * 0.90f))
            {
                UltimatePressed = true;
                return;
            }

            // Step in slightly to contest neutral
            float dirToOpponent = (opponent != null && opponent.position.x > transform.position.x) ? 1f : -1f;
            MoveInput = new Vector2(dirToOpponent * 0.4f, 0f);
        }

        protected override void PerformMeleeCombo()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Ultimate check (Kamehameha blast!)
            if (currentKi >= UltimateCost && Random.value < profile.comboChance)
            {
                UltimatePressed = true;
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
