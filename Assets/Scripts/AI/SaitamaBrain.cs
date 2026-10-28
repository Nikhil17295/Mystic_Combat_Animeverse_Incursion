using UnityEngine;

namespace MysticCombat.AI
{
    /// <summary>
    /// Saitama-specific AI combat brain.
    /// Fighting Style: Overwhelming In-Your-Face Rushdown Brawler.
    /// - Closes distance relentlessly (< 1.0m); refuses to back away in neutral.
    /// - Loves close-range Light jabs into Heavy gut punches.
    /// - Chains into Consecutive Normal Punches (Skill / U) point-blank.
    /// - Hoards 100 Ki to march up and drop the lethal Serious Punch (Ultimate / O).
    /// </summary>
    public class SaitamaBrain : CpuFighterBrain
    {
        #region Character-Specific Overrides

        protected override float PreferredDistance => 0.95f;   // Pure in-your-face rushdown
        protected override float MeleeRange => 1.20f;
        protected override float SkillRange => 1.35f;          // Consecutive Normal Punches are point-blank
        protected override float SkillCost => 20f;
        protected override float UltimateCost => 100f;         // Serious Punch costs entire 100 Ki bar!
        protected override float TargetKiHoard => 100f;        // Hoards every bit of Ki for Serious Punch

        #endregion

        #region Customized Combat Actions

        protected override void PerformMidRangeAction()
        {
            // Saitama doesn't zone with projectiles; he immediately charges forward!
            float dirToOpponent = (opponent != null && opponent.position.x > transform.position.x) ? 1f : -1f;
            MoveInput = new Vector2(dirToOpponent, 0f);

            // Occasional hero hop to close distance
            if (Random.value < 0.25f)
            {
                JumpPressed = true;
            }
        }

        protected override void PerformMeleePoke()
        {
            // Heavy punches and jabs up close
            if (Random.value < 0.50f)
            {
                HeavyAttackPressed = true;
            }
            else
            {
                LightAttackPressed = true;
            }
        }

        protected override void PerformMeleeCombo()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Serious Punch: 100-Ki instant lethal KO strike!
            if (currentKi >= UltimateCost)
            {
                // Saitama unleashes Serious Punch when in range!
                UltimatePressed = true;
                return;
            }

            // Consecutive Normal Punches (Skill flurry)
            if (currentKi >= SkillCost && Random.value < profile.comboChance)
            {
                SkillPressed = true;
                return;
            }

            // Heavy haymaker into light attack
            if (Random.value < 0.55f)
            {
                HeavyAttackPressed = true;
            }
            else
            {
                LightAttackPressed = true;
            }
        }

        #endregion
    }
}
