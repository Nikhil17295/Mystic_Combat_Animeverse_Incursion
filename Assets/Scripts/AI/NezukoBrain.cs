using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.AI
{
    /// <summary>
    /// Nezuko-specific AI combat brain.
    /// Fighting Style: Dual-Form Evasive Acrobat & Awakened Demon Striker.
    /// - Small Chibi State:
    ///   * Tiny hurtbox whiffs opponent's standing Light & Heavy attacks.
    ///   * Rapid movement (1.5x speed) and super jumps.
    ///   * Passively builds +1 Ki/s; meditates at range to hoard energy.
    ///   * Darts in for 0-damage hitstun interrupts to disrupt opponent combos.
    /// - Awakened Demon Form:
    ///   * Toggled via Skill when Ki >= 40.
    ///   * Closes in for fierce melee: 5-damage Light claws & 10-damage Heavy kicks.
    ///   * Continuously spends 2 Ki/s.
    ///   * Fires sure-hit Blood Demon Art: Exploding Blood (60 Ki / 30 burst + burn ticks) anywhere on screen!
    ///   * Strategically reverts back to Chibi form when Ki <= 15 to recharge and evade Red Zone exhaustion.
    /// </summary>
    public class NezukoBrain : CpuFighterBrain
    {
        #region Character Overrides

        protected override float PreferredDistance => (myController != null && myController.IsSmallChibi) ? 2.2f : 1.35f;
        protected override float MeleeStrikeRange => 1.50f;
        protected override float SkillStrikeRange => 3.0f;
        protected override float SkillCost => 0f; // Demon Form is an active toggle
        protected override float UltimateCost => 60f; // Blood Demon Art: 60 Ki
        protected override float TargetKiHoard => 60f;

        #endregion

        #region Unity Callbacks

        protected override void Update()
        {
            base.Update();

            ManageDemonFormState();
        }

        #endregion

        #region Demon Form Toggle Management

        private void ManageDemonFormState()
        {
            if (myController == null || myEnergy == null || opponent == null) return;

            float dist = Mathf.Abs(transform.position.x - opponent.position.x);
            float currentKi = myEnergy.CurrentKi;
            bool canAct = myController.IsGrounded && (myController.CurrentState == FighterState.Idle || myController.CurrentState == FighterState.Walking);

            if (!canAct) return;

            if (myController.IsSmallChibi)
            {
                // In Chibi form:
                // Transform into Demon form if:
                // 1. We have at least 40 Ki (enough to fight and potentially use Ultimate soon)
                // 2. We are closing in on opponent or in Desperation mode
                bool healthyKi = currentKi >= 40f;
                bool inCombatRange = dist <= 2.2f || isDesperate;

                if (healthyKi && inCombatRange)
                {
                    TriggerSkill(); // Toggles ON Demon Form!
                }
            }
            else if (myController.IsDemonForm)
            {
                // In Demon form:
                // Revert to Chibi form if Ki is critically low (<= 15 Ki) to prevent Red Zone exhaustion,
                // or if opponent is far away and we have low Ki.
                bool lowKiWarning = currentKi <= 15f;
                bool resetNeutral = dist > 4.5f && currentKi < 40f;

                if (lowKiWarning || resetNeutral)
                {
                    TriggerSkill(); // Toggles OFF Demon Form, reverting to Small Chibi!
                }
            }
        }

        #endregion

        #region Tactical Ultimate (Sure-Hit Blood Demon Art)

        /// <summary>
        /// Blood Demon Art is a sure-hit move anywhere across the arena.
        /// Does not need to close all the way to point-blank, but comfortable within 6.5m.
        /// </summary>
        protected override bool ShouldRushdownForUltimate(float dist)
        {
            return dist > 6.5f;
        }

        protected override bool TryTacticalUltimate(float dist)
        {
            if (myController == null || myController.IsSmallChibi) return false;

            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;
            bool ultOffCd = myController.UltimateCooldownTimer <= 0f;

            if (currentKi >= UltimateCost && ultOffCd && CanAttackGrounded())
            {
                // Sure hit move: guaranteed punish
                float chance = IsOpponentVulnerable() ? 0.95f : 0.70f;
                if (Random.value < chance)
                {
                    TriggerUltimate();
                    attackCooldownTimer = 1.0f;
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
            bool ultOffCd = myController == null || myController.UltimateCooldownTimer <= 0f;

            // In Demon Form: Check for Ultimate finisher first!
            if (myController != null && myController.IsDemonForm)
            {
                if (currentKi >= UltimateCost && ultOffCd && Random.value < 0.75f)
                {
                    TriggerUltimate();
                    return;
                }

                // Fierce claws (Light 5 dmg) or Heavy kicks (10 dmg)
                if (Random.value < 0.55f)
                {
                    TriggerLightAttack();
                }
                else
                {
                    TriggerHeavyAttack();
                }
            }
            else
            {
                // In Small Chibi form:
                // If we have built up 40+ Ki, transform into Demon form immediately!
                if (currentKi >= 40f)
                {
                    TriggerSkill();
                    return;
                }

                // Otherwise, use rapid strikes as interrupts to stagger opponent and build Ki
                if (Random.value < 0.65f)
                {
                    TriggerLightAttack();
                }
                else
                {
                    TriggerHeavyAttack();
                }
            }
        }

        #endregion
    }
}
