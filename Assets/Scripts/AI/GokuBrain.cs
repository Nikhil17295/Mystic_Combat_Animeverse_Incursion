using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.AI
{
    /// <summary>
    /// Goku-specific AI combat brain.
    /// Fighting Style: Disciplined Martial Artist, Super Saiyan Powerhouse & Beam Zoner.
    /// - Gathers Ki efficiently through tactical meditation when at range.
    /// - The moment Ki reaches 35, ascends into Super Saiyan form (+25% speed, +66% light attack damage, +43% heavy damage).
    /// - Closes in with high-velocity SSJ movement and punishes with crisp Light -> Heavy chain cancels.
    /// - Fires full-screen Kamehameha beam (Ultimate / 80 Ki) when the opponent whiffs or enters vulnerability.
    /// </summary>
    public class GokuBrain : CpuFighterBrain
    {
        #region Character Overrides

        protected override float PreferredDistance => 1.65f;
        protected override float MeleeStrikeRange => 1.55f;
        protected override float SkillStrikeRange => 6.5f;     // Kamehameha reaches across the arena
        protected override float SkillCost => 35f;            // Super Saiyan ascension cost
        protected override float UltimateCost => 80f;         // Kamehameha cost
        protected override float TargetKiHoard => 80f;

        #endregion

        #region Unity Callbacks

        protected override void Update()
        {
            base.Update();

            // Tactical Super Saiyan Check:
            // If Goku has >= 35 Ki and is NOT in Super Saiyan, transform immediately to dominate neutral!
            CheckSuperSaiyanAscension();
        }

        #endregion

        #region Super Saiyan Ascension

        private void CheckSuperSaiyanAscension()
        {
            if (myController == null || myEnergy == null) return;

            // Only ascend if grounded, in movable state, not already SSJ, and have Ki
            if (!myController.IsSuperSaiyan && 
                myEnergy.CanAfford(SkillCost) && 
                myController.IsGrounded &&
                (myController.CurrentState == FighterState.Idle || myController.CurrentState == FighterState.Walking))
            {
                TriggerSkill();
            }
        }

        #endregion

        #region Customized Combat Actions

        protected override void PerformCloseRangeAttack()
        {
            float currentKi = (myEnergy != null) ? myEnergy.CurrentKi : 0f;

            // Point-blank Kamehameha if affordable and confirmed
            if (currentKi >= UltimateCost && Random.value < profile.comboChance)
            {
                TriggerUltimate();
                return;
            }

            // Disciplined punch-kick strings (Light jab into Heavy chain cancel)
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
