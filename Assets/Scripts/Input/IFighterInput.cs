using UnityEngine;

namespace MysticCombat.Input
{
    /// <summary>
    /// Common input abstraction contract for all fighters.
    /// Implemented by PlayerInputHandler (hardware keyboard/gamepad)
    /// and CpuFighterBrain (tactical AI state machine).
    /// </summary>
    public interface IFighterInput
    {
        /// <summary>
        /// Directional movement input: X in [-1, 1], Y in [-1, 1].
        /// </summary>
        Vector2 MoveInput { get; }

        /// <summary>
        /// True on the exact frame jump is triggered.
        /// </summary>
        bool JumpPressed { get; }

        /// <summary>
        /// True while crouch is actively held down.
        /// </summary>
        bool CrouchHeld { get; }

        /// <summary>
        /// True on the exact frame light attack is pressed.
        /// </summary>
        bool LightAttackPressed { get; }

        /// <summary>
        /// True on the exact frame heavy attack is pressed.
        /// </summary>
        bool HeavyAttackPressed { get; }

        /// <summary>
        /// True on the exact frame special skill attack is pressed.
        /// </summary>
        bool SkillPressed { get; }

        /// <summary>
        /// True on the exact frame ultimate attack is pressed.
        /// </summary>
        bool UltimatePressed { get; }

        /// <summary>
        /// True while meditation (Ki charging / Red Zone recovery) is actively held.
        /// </summary>
        bool MeditateHeld { get; }
    }
}
