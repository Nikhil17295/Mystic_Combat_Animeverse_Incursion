using UnityEngine;
using MysticCombat.Input;

namespace MysticCombat.Network
{
    /// <summary>
    /// Network-driven input provider that implements IFighterInput.
    /// Attached to Fighter 2 on the Host machine to execute moves streamed over LAN from Client.
    /// Completely interchangeable with PlayerInputHandler and CpuFighterBrain.
    /// </summary>
    public class NetworkFighterInput : MonoBehaviour, IFighterInput
    {
        #region State

        private Vector2 moveInput;
        private bool jumpPressed;
        private bool crouchHeld;
        private bool lightAttackPressed;
        private bool heavyAttackPressed;
        private bool skillPressed;
        private bool ultimatePressed;
        private bool meditateHeld;

        #endregion

        #region IFighterInput Implementation

        public Vector2 MoveInput => moveInput;
        public bool JumpPressed => jumpPressed;
        public bool CrouchHeld => crouchHeld;
        public bool LightAttackPressed => lightAttackPressed;
        public bool HeavyAttackPressed => heavyAttackPressed;
        public bool SkillPressed => skillPressed;
        public bool UltimatePressed => ultimatePressed;
        public bool MeditateHeld => meditateHeld;

        #endregion

        #region Input Reception

        /// <summary>
        /// Called on the Host whenever a FighterInputPacket arrives from the Client.
        /// </summary>
        public void FeedInput(FighterInputPacket packet)
        {
            moveInput = packet.MoveInput;
            crouchHeld = packet.CrouchHeld;
            meditateHeld = packet.MeditateHeld;

            // Latch edge-triggered inputs until the next frame consumption
            if (packet.JumpPressed) jumpPressed = true;
            if (packet.LightAttackPressed) lightAttackPressed = true;
            if (packet.HeavyAttackPressed) heavyAttackPressed = true;
            if (packet.SkillPressed) skillPressed = true;
            if (packet.UltimatePressed) ultimatePressed = true;
        }

        #endregion

        #region Unity Lifecycle

        private void LateUpdate()
        {
            // Clear single-frame impulse triggers after FighterController has run
            jumpPressed = false;
            lightAttackPressed = false;
            heavyAttackPressed = false;
            skillPressed = false;
            ultimatePressed = false;
        }

        #endregion
    }
}
