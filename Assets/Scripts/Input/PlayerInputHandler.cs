using UnityEngine;

namespace MysticCombat.Input
{
    /// <summary>
    /// Reads raw keyboard input for a specific player (P1 or P2).
    /// Uses Input.GetKey / GetKeyDown for shared-keyboard reliability.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Identifies which player this handler reads input for.
        /// </summary>
        public enum PlayerIndex
        {
            P1,
            P2
        }

        #endregion

        #region Serialized Fields

        [Header("Player Assignment")]
        [Tooltip("Which player's controls this handler reads.")]
        public PlayerIndex playerIndex = PlayerIndex.P1;

        #endregion

        #region Public Properties

        /// <summary>
        /// Horizontal and vertical movement input. X: -1/0/1 (left/right). Y: 1 (jump) / -1 (crouch) / 0.
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// True on the frame the jump key is pressed down.
        /// </summary>
        public bool JumpPressed { get; private set; }

        /// <summary>
        /// True while the crouch key is held.
        /// </summary>
        public bool CrouchHeld { get; private set; }

        /// <summary>
        /// True on the frame the light attack key is pressed down.
        /// </summary>
        public bool LightAttackPressed { get; private set; }

        /// <summary>
        /// True on the frame the heavy attack key is pressed down.
        /// </summary>
        public bool HeavyAttackPressed { get; private set; }

        /// <summary>
        /// True on the frame the skill key is pressed down.
        /// </summary>
        public bool SkillPressed { get; private set; }

        /// <summary>
        /// True on the frame the ultimate key is pressed down.
        /// </summary>
        public bool UltimatePressed { get; private set; }

        /// <summary>
        /// True while the meditate key is held.
        /// </summary>
        public bool MeditateHeld { get; private set; }

        #endregion

        #region Unity Callbacks

        private void Update()
        {
            if (playerIndex == PlayerIndex.P1)
            {
                ReadP1Input();
            }
            else
            {
                ReadP2Input();
            }
        }

        #endregion

        #region Private Methods

        private void ReadP1Input()
        {
            // Movement
            float horizontal = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) horizontal += 1f;

            float vertical = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.W)) vertical += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) vertical -= 1f;

            MoveInput = new Vector2(horizontal, vertical);

            // Buttons
            JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.W);
            CrouchHeld = UnityEngine.Input.GetKey(KeyCode.S);
            LightAttackPressed = UnityEngine.Input.GetKeyDown(KeyCode.G);
            HeavyAttackPressed = UnityEngine.Input.GetKeyDown(KeyCode.H);
            SkillPressed = UnityEngine.Input.GetKeyDown(KeyCode.J);
            UltimatePressed = UnityEngine.Input.GetKeyDown(KeyCode.K);
            MeditateHeld = UnityEngine.Input.GetKey(KeyCode.T);
        }

        private void ReadP2Input()
        {
            // Movement
            float horizontal = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;

            float vertical = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;

            MoveInput = new Vector2(horizontal, vertical);

            // Buttons
            JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.UpArrow);
            CrouchHeld = UnityEngine.Input.GetKey(KeyCode.DownArrow);
            LightAttackPressed = UnityEngine.Input.GetKeyDown(KeyCode.Keypad1);
            HeavyAttackPressed = UnityEngine.Input.GetKeyDown(KeyCode.Keypad2);
            SkillPressed = UnityEngine.Input.GetKeyDown(KeyCode.Keypad3);
            UltimatePressed = UnityEngine.Input.GetKeyDown(KeyCode.Keypad5);
            MeditateHeld = UnityEngine.Input.GetKey(KeyCode.Keypad0);
        }

        #endregion
    }
}
