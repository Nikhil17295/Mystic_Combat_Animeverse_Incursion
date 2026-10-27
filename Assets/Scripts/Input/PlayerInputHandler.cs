using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticCombat.Input
{
    /// <summary>
    /// Reads keyboard input for P1 or P2 using Unity's modern Input System (Keyboard.current).
    /// Fallback supported for legacy input systems.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        #region Enums

        public enum PlayerIndex
        {
            P1,
            P2
        }

        #endregion

        #region Serialized Fields

        [Header("Player Assignment")]
        public PlayerIndex playerIndex = PlayerIndex.P1;

        #endregion

        #region Public Properties

        public Vector2 MoveInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool LightAttackPressed { get; private set; }
        public bool HeavyAttackPressed { get; private set; }
        public bool SkillPressed { get; private set; }
        public bool UltimatePressed { get; private set; }
        public bool MeditateHeld { get; private set; }

        #endregion

        #region Unity Callbacks

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (playerIndex == PlayerIndex.P1)
            {
                ReadP1Input(kb);
            }
            else
            {
                ReadP2Input(kb);
            }
        }

        #endregion

        #region Input Reading (Modern Input System)

        private void ReadP1Input(Keyboard kb)
        {
            // Movement (W/A/S/D) + Space for Jump
            float horizontal = 0f;
            if (kb.aKey.isPressed) horizontal -= 1f;
            if (kb.dKey.isPressed) horizontal += 1f;

            float vertical = 0f;
            if (kb.wKey.isPressed) vertical += 1f;
            if (kb.sKey.isPressed) vertical -= 1f;

            MoveInput = new Vector2(horizontal, vertical);

            // Action Buttons (Custom Ergonomic Layout: F = Light, C = Heavy, E = Skill, Q = Ultimate, X = Meditate)
            // Jump strictly mapped to W (Space removed to prevent overriding inputs)
            JumpPressed = kb.wKey.wasPressedThisFrame;
            CrouchHeld = kb.sKey.isPressed;
            LightAttackPressed = kb.fKey.wasPressedThisFrame;
            HeavyAttackPressed = kb.cKey.wasPressedThisFrame;
            SkillPressed = kb.eKey.wasPressedThisFrame;
            UltimatePressed = kb.qKey.wasPressedThisFrame;
            MeditateHeld = kb.xKey.isPressed;
        }

        private void ReadP2Input(Keyboard kb)
        {
            // Movement (I / J / K / L strictly - Arrow Keys removed to prevent overriding inputs)
            float horizontal = 0f;
            if (kb.jKey.isPressed) horizontal -= 1f;
            if (kb.lKey.isPressed) horizontal += 1f;

            float vertical = 0f;
            if (kb.iKey.isPressed) vertical += 1f;
            if (kb.kKey.isPressed) vertical -= 1f;

            MoveInput = new Vector2(horizontal, vertical);

            // Action Buttons (Custom Ergonomic Layout: H = Light, N = Heavy, U = Skill, O = Ultimate, M = Meditate)
            JumpPressed = kb.iKey.wasPressedThisFrame;
            CrouchHeld = kb.kKey.isPressed;

            LightAttackPressed = kb.hKey.wasPressedThisFrame;
            HeavyAttackPressed = kb.nKey.wasPressedThisFrame;
            SkillPressed = kb.uKey.wasPressedThisFrame;
            UltimatePressed = kb.oKey.wasPressedThisFrame;
            MeditateHeld = kb.mKey.isPressed;
        }

        #endregion
    }
}
