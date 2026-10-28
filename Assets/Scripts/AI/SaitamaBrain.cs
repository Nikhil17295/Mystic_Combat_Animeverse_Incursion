using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.AI
{
    /// <summary>
    /// Saitama-specific AI combat brain.
    /// Fighting Style: Relentless In-Your-Face Rushdown Brawler.
    /// - Closes distance without hesitation; refuses to backpedal in neutral.
    /// - Chains fast Light jabs into Heavy haymakers and Consecutive Normal Punches (Skill / 30 Ki / 5 hits @ 4 dmg = 20 total).
    /// - Checks 15-second cooldown timers and Red Zone energy buffers before expending Ki.
    /// - If he builds 100 Ki, unleashes the devastating Serious Punch (Ultimate / 100 Ki / 90 damage)
    ///   at point-blank range (dist <= 1.7m) to decisively end the round!
    /// </summary>
    public class SaitamaBrain : CpuFighterBrain
    {
        #region Character Overrides

        protected override float PreferredDistance => 1.30f;   // Tight close-quarters brawling
        protected override float MeleeStrikeRange => 1.55f;
        protected override float SkillStrikeRange => 1.85f;    // Consecutive Normal Punches
        protected override float SkillCost => myEnergy != null ? myEnergy.SkillKiCost : 30f;             // Rebalanced: 30 Ki (5 punches @ 4 dmg)
        protected override float UltimateCost => myEnergy != null ? myEnergy.UltimateKiCost : 80f;         // Serious Punch cost: 80 Ki
        protected override float TargetKiHoard => myEnergy != null ? myEnergy.SafeUltimateThreshold : 90f; // Hoards to Safe Ultimate threshold (90 Ki)

        #endregion

        #region Tactical Ultimate & Brawler Spacing

        /// <summary>
        /// Serious Punch is a devastating point-blank melee haymaker (dist <= 1.7m).
        /// Saitama must aggressively rush down the opponent when his Ultimate is ready!
        /// </summary>
        protected override bool ShouldRushdownForUltimate(float dist)
        {
            return dist > 1.60f;
        }

        /// <summary>
        /// Tactical Serious Punch execution:
        /// Connects at point-blank range (dist <= 1.70m) when 100 Ki is ready and off cooldown.
        /// Devastating punish if opponent is vulnerable, whiffing, or recovering.
        /// </summary>
        protected override bool TryTacticalUltimate(float dist)
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;
            bool ultOffCd = myController == null || myController.UltimateCooldownTimer <= 0f;
            if (currentKi < UltimateCost || !ultOffCd || !CanAttackGrounded()) return false;

            if (dist <= 1.70f)
            {
                bool opponentVulnerable = IsOpponentVulnerable();
                float chance = opponentVulnerable ? 0.90f : 0.65f;
                if (Random.value < chance)
                {
                    TriggerUltimate();
                    attackCooldownTimer = 1.5f;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Customized Combat Actions

        protected override void PerformCloseRangeAttack()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;
            float dist = (opponent != null) ? Mathf.Abs(transform.position.x - opponent.position.x) : 99f;
            bool ultOffCd = myController == null || myController.UltimateCooldownTimer <= 0f;
            bool skillOffCd = myController == null || myController.SkillCooldownTimer <= 0f;

            // Serious Punch Finisher: If Ultimate Ki is available, off cooldown, and in point-blank range (dist <= 1.7m)!
            if (currentKi >= UltimateCost && ultOffCd && dist <= 1.70f && Random.value < 0.70f)
            {
                TriggerUltimate();
                return;
            }

            // Consecutive Normal Punches: Standalone special skill (30 Ki / 20 damage)
            bool safeForSkill = isDesperate || (currentKi >= SafeSkillThreshold);
            if (currentKi >= SkillCost && skillOffCd && safeForSkill && dist <= SkillStrikeRange && Random.value < 0.60f)
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
