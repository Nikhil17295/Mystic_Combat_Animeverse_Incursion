using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.AI
{
    /// <summary>
    /// Saitama-specific AI combat brain.
    /// Fighting Style: Relentless In-Your-Face Rushdown Brawler.
    /// - Closes distance without hesitation; refuses to backpedal in neutral.
    /// - Chains fast Light jabs into Heavy haymakers and Consecutive Normal Punches (Skill / 20 Ki).
    /// - If he builds 80 Ki, unleashes the devastating Serious Punch (Ultimate / 80 Ki / 90 damage)
    ///   to decisively end the round!
    /// </summary>
    public class SaitamaBrain : CpuFighterBrain
    {
        #region Character Overrides

        protected override float PreferredDistance => 1.30f;   // Tight close-quarters brawling
        protected override float MeleeStrikeRange => 1.55f;
        protected override float SkillStrikeRange => 1.85f;    // Consecutive Normal Punches
        protected override float SkillCost => 20f;
        protected override float UltimateCost => 80f;         // Serious Punch costs 80 Ki
        protected override float TargetKiHoard => 80f;

        #endregion

        #region Customized Combat Actions

        protected override void PerformCloseRangeAttack()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Serious Punch Finisher: If 80 Ki is available, drop the lethal 90-damage strike!
            if (currentKi >= UltimateCost && Random.value < (profile.comboChance * 1.1f))
            {
                TriggerUltimate();
                return;
            }

            // Consecutive Normal Punches: 15 rapid-fire punches advancing forward (30 damage)
            if (currentKi >= SkillCost && Random.value < profile.comboChance)
            {
                TriggerSkill();
                return;
            }

            // Heavy haymaker or fast Light jab opener
            if (Random.value < 0.55f)
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
