using UnityEngine;
using MysticCombat.Input;

namespace MysticCombat.Network
{
    /// <summary>
    /// Network-driven input provider that implements IFighterInput.
    /// Attached to Fighter 2 on both Host and Client to execute moves streamed over LAN.
    /// Uses frame-buffered latches so no button presses are ever missed across frame boundaries.
    /// </summary>
    public class NetworkFighterInput : MonoBehaviour, IFighterInput
    {
        #region State

        private Vector2 moveInput;
        private bool crouchHeld;
        private bool meditateHeld;

        // Buffers with frame expiration so fast triggers are never dropped
        private int jumpBuffer;
        private int lightAttackBuffer;
        private int heavyAttackBuffer;
        private int skillBuffer;
        private int ultimateBuffer;

        #endregion

        #region IFighterInput Implementation

        public Vector2 MoveInput => moveInput;
        public bool CrouchHeld => crouchHeld;
        public bool MeditateHeld => meditateHeld;

        public bool JumpPressed => ConsumeBuffer(ref jumpBuffer);
        public bool LightAttackPressed => ConsumeBuffer(ref lightAttackBuffer);
        public bool HeavyAttackPressed => ConsumeBuffer(ref heavyAttackBuffer);
        public bool SkillPressed => ConsumeBuffer(ref skillBuffer);
        public bool UltimatePressed => ConsumeBuffer(ref ultimateBuffer);

        #endregion

        #region Input Reception

        /// <summary>
        /// Called whenever a FighterInputPacket arrives from the remote peer.
        /// </summary>
        public void FeedInput(FighterInputPacket packet)
        {
            moveInput = packet.MoveInput;
            crouchHeld = packet.CrouchHeld;
            meditateHeld = packet.MeditateHeld;

            // Buffer single-frame triggers for up to 3 frames until FighterController consumes them
            if (packet.JumpPressed) jumpBuffer = 3;
            if (packet.LightAttackPressed) lightAttackBuffer = 3;
            if (packet.HeavyAttackPressed) heavyAttackBuffer = 3;
            if (packet.SkillPressed) skillBuffer = 3;
            if (packet.UltimatePressed) ultimateBuffer = 3;
        }

        private bool ConsumeBuffer(ref int buffer)
        {
            if (buffer > 0)
            {
                buffer = 0;
                return true;
            }
            return false;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Age out unconsumed buffers
            if (jumpBuffer > 0) jumpBuffer--;
            if (lightAttackBuffer > 0) lightAttackBuffer--;
            if (heavyAttackBuffer > 0) heavyAttackBuffer--;
            if (skillBuffer > 0) skillBuffer--;
            if (ultimateBuffer > 0) ultimateBuffer--;
        }

        #endregion
    }
}
