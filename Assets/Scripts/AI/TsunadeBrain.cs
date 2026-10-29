using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.AI
{
    /// <summary>
    /// Tsunade-specific AI combat brain.
    /// Fighting Style: Heavy-Hitter Brawler & Creation Rebirth Regeneration Tank.
    /// - Close Range:
    ///   * Pokes with fast 5-damage Light Head Flicks to interrupt enemy startup.
    ///   * Punishes grounded opponents with 10-damage Heavy Foot Stomps.
    /// - Special Skill (Ground Breaker):
    ///   * Spends 30 Ki (20s cooldown) to unleash a devastating 30-damage ground strike against grounded foes.
    /// - Ultimate (Katsuyu Summon & Creation Rebirth):
    ///   * Spends 60 Ki (20s cooldown) to launch Katsuyu (30 damage straight line).
    ///   * Activates 10-second Healing Stage (+2 HP/s, +2 Ki/s, 50% damage reduction).
    ///   * Plays hyper-aggressively while the Byakugou aura is active.
    /// </summary>
    public class TsunadeBrain : CpuFighterBrain
    {
        #region Character Overrides

        protected override float PreferredDistance => (myController != null && myController.IsHealingStage) ? 1.05f : 1.35f;
        protected override float MeleeStrikeRange => 1.55f;
        protected override float SkillStrikeRange => 2.40f;
        protected override float SkillCost => 30f;
        protected override float UltimateCost => 60f;
        protected override float TargetKiHoard => 60f;

        #endregion

        #region AI Logic Overrides

        protected override bool TryTacticalUltimate(float dist)
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;
            bool ultOffCd = myController == null || myController.UltimateCooldownTimer <= 0f;
            if (currentKi < UltimateCost || !ultOffCd || !CanAttackGrounded()) return false;

            if (dist <= SkillStrikeRange && Random.value < 0.70f)
            {
                TriggerUltimate();
                attackCooldownTimer = 1.2f;
                return true;
            }
            return false;
        }

        protected override void PerformCloseRangeAttack()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // 1. Try Ultimate (Katsuyu Summon) if ready
            if (currentKi >= UltimateCost && (myController == null || myController.UltimateCooldownTimer <= 0f) && CanAttackGrounded())
            {
                TriggerUltimate();
                return;
            }

            // 2. Try Skill (Ground Breaker) if off cooldown
            if (currentKi >= SkillCost && (myController == null || myController.SkillCooldownTimer <= 0f) && CanAttackGrounded())
            {
                TriggerSkill();
                return;
            }

            // 3. Try Heavy Foot Stomp
            if (Random.value < 0.45f)
            {
                TriggerHeavyAttack();
            }
            else
            {
                TriggerLightAttack();
            }
        }

        #endregion
    }
}
