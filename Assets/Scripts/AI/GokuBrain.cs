using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.AI
{
    /// <summary>
    /// Goku-specific AI combat brain.
    /// Fighting Style: Disciplined Martial Artist, Super Saiyan Powerhouse & Tactical Beam Zoner.
    /// - Gathers Ki efficiently through tactical meditation when at range (8.0 Ki/s).
    /// - Toggles Super Saiyan form strategically when closing in for combat (+25% speed, +66% light, +43% heavy).
    /// - Sustains Super Saiyan throughout combat, only dropping it when Ki is critically low (<= 12 Ki)
    ///   to avoid self-exhaustion in the Red Zone without triggering unnecessary 15-second lockout cooldowns.
    /// - Tactical Kamehameha: Recognizes beam range (2.5m - 6.5m) and fires from safe mid-to-long distance
    ///   instead of suicidally running into point-blank melee where the 2-second startup would be interrupted.
    /// </summary>
    public class GokuBrain : CpuFighterBrain
    {
        #region Character Overrides

        protected override float PreferredDistance => 1.65f;
        protected override float MeleeStrikeRange => 1.55f;
        protected override float SkillStrikeRange => 6.5f;     // Kamehameha reaches across the arena
        protected override float SkillCost => 0f;              // Super Saiyan is an active toggle (consumes 2.5 Ki/s)
        protected override float UltimateCost => myEnergy != null ? myEnergy.UltimateKiCost : 70f;         // Kamehameha cost: 70 Ki
        protected override float TargetKiHoard => myEnergy != null ? myEnergy.SafeUltimateThreshold : 80f; // Hoards to Safe Ultimate threshold (80 Ki)

        #endregion

        #region Unity Callbacks

        protected override void Update()
        {
            base.Update();

            // Tactical Super Saiyan Toggle Management (Checks cooldown & Red Zone safety)
            ManageSuperSaiyanState();
        }

        #endregion

        #region Super Saiyan State Management

        private void ManageSuperSaiyanState()
        {
            if (myController == null || myEnergy == null) return;
            if (opponent == null) return;

            float dist = Mathf.Abs(transform.position.x - opponent.position.x);
            float currentKi = myEnergy.CurrentKi;

            if (!myController.IsSuperSaiyan)
            {
                // To activate:
                // 1. Must NOT be on Skill cooldown (15s timer)
                // 2. Must have enough Ki buffer to avoid immediately entering Red Zone
                // 3. Close to opponent or in Desperation mode
                bool offCooldown = myController.SkillCooldownTimer <= 0f;
                bool healthyKi = currentKi >= SafeSkillThreshold; // Safe Skill marker (30 Ki buffer)
                bool inCombatRange = dist <= MeleeStrikeRange * 2.2f || isDesperate;
                bool canAct = myController.IsGrounded && (myController.CurrentState == FighterState.Idle || myController.CurrentState == FighterState.Walking);

                if (offCooldown && healthyKi && inCombatRange && canAct)
                {
                    TriggerSkill(); // Toggles ON Super Saiyan!
                }
            }
            else
            {
                // Already in Super Saiyan: Smart Toggle OFF to conserve Ki & prevent Red Zone
                // ONLY toggle off when Ki is dangerously low (near Red Zone) to avoid entering Red Zone exhaustion!
                // We do NOT toggle off on distance alone (dist > 4.2m) because toggling off incurs a 15-second cooldown
                // and strips Goku of his speed (+25%) and heavy attack (+43%) buffs mid-fight!
                float redZoneLimit = myEnergy != null ? myEnergy.RedZoneThreshold : 10f;
                bool lowKiWarning = currentKi <= (redZoneLimit + 2f);
                bool canAct = myController.SkillCooldownTimer <= 0f && myController.IsGrounded &&
                              (myController.CurrentState == FighterState.Idle || myController.CurrentState == FighterState.Walking);

                if (lowKiWarning && canAct)
                {
                    TriggerSkill(); // Toggles OFF Super Saiyan, cleanly reverting to Base form!
                }
            }
        }

        #endregion

        #region Tactical Ultimate & Beam Zoner Behavior

        /// <summary>
        /// Goku's Kamehameha has full-screen range (reaches 8.5m).
        /// Goku does NOT rush into point-blank melee when his Ultimate is ready;
        /// he holds mid-to-long spacing (2.5m - 6.5m) to fire without being interrupted.
        /// Only rushes if opponent is beyond maximum beam range (> 7.0m).
        /// </summary>
        protected override bool ShouldRushdownForUltimate(float dist)
        {
            return dist > 7.0f;
        }

        /// <summary>
        /// Tactical Kamehameha execution:
        /// - Mid/Long range (2.5m - 6.5m): Perfect beam spacing where opponent cannot punch Goku during startup!
        /// - Close range (< 2.0m): ONLY allowed if opponent is in HitStun or Exhausted (punish / super cancel).
        /// </summary>
        protected override bool TryTacticalUltimate(float dist)
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;
            bool ultOffCd = myController == null || myController.UltimateCooldownTimer <= 0f;
            if (currentKi < UltimateCost || !ultOffCd || !CanAttackGrounded()) return false;

            bool opponentVulnerable = IsOpponentVulnerable();

            // Case 1: Ideal Mid-to-Long Beam Spacing (2.5m - 6.5m)
            if (dist >= 2.5f && dist <= SkillStrikeRange)
            {
                // Standalone tactical decision: High chance if opponent is vulnerable, moderate in neutral
                float chance = opponentVulnerable ? 0.85f : 0.60f;
                if (Random.value < chance)
                {
                    TriggerUltimate();
                    attackCooldownTimer = 1.8f;
                    return true;
                }
            }
            // Case 2: Close-Range Punish (dist < 2.0m) ONLY when opponent is locked in HitStun / Exhaustion
            else if (dist < 2.0f && opponentVulnerable)
            {
                if (Random.value < 0.75f)
                {
                    TriggerUltimate();
                    attackCooldownTimer = 1.8f;
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

            // At point-blank, only fire Kamehameha if opponent is vulnerable/stunned so it cannot be interrupted!
            if (currentKi >= UltimateCost && ultOffCd && IsOpponentVulnerable() && Random.value < 0.75f)
            {
                TriggerUltimate();
                return;
            }

            // Single, distinct strikes with natural recovery frames (combos unlearned)
            if (Random.value < 0.60f)
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
