using UnityEngine;
using UnityEngine.InputSystem;
using MysticCombat.Core;
using MysticCombat.Systems;

namespace MysticCombat.Input
{
    /// <summary>
    /// Reads keyboard input for P1 or P2 using Unity's modern Input System (Keyboard.current).
    /// Fallback supported for legacy input systems.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour, IFighterInput
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

        #region Match State Tracking

        private MatchManager cachedMatchManager;

        /// <summary>
        /// Fighters can only receive inputs when a round is actively in progress.
        /// Inputs are completely cut off before the round begins, during K.O./round-end delays,
        /// after match over, and during pause.
        /// </summary>
        private bool CanFight()
        {
            if (Time.timeScale == 0f) return false;

            if (cachedMatchManager == null)
            {
                cachedMatchManager = Object.FindAnyObjectByType<MatchManager>();
            }

            if (cachedMatchManager != null)
            {
                if (cachedMatchManager.CurrentMatchState != MatchManager.MatchState.RoundInProgress)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Unity Callbacks

        private void Update()
        {
            if (!CanFight())
            {
                MoveInput = Vector2.zero;
                JumpPressed = false;
                CrouchHeld = false;
                LightAttackPressed = false;
                HeavyAttackPressed = false;
                SkillPressed = false;
                UltimatePressed = false;
                MeditateHeld = false;
                return;
            }

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

        #region Input Reading (Modern Input System with Dynamic Keybindings)

        private void ReadP1Input(Keyboard kb)
        {
            bool isLocalDuel = GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameManager.GameMode.LocalPvP;

            float horizontal = 0f;
            if (IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Left, isPlayer2: false, isLocalDuel))) horizontal -= 1f;
            if (IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Right, isPlayer2: false, isLocalDuel))) horizontal += 1f;

            float vertical = 0f;
            if (IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Jump, isPlayer2: false, isLocalDuel))) vertical += 1f;
            if (IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Crouch, isPlayer2: false, isLocalDuel))) vertical -= 1f;

            MoveInput = new Vector2(horizontal, vertical);

            JumpPressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Jump, isPlayer2: false, isLocalDuel));
            CrouchHeld = IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Crouch, isPlayer2: false, isLocalDuel));
            LightAttackPressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.LightAttack, isPlayer2: false, isLocalDuel));
            HeavyAttackPressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.HeavyAttack, isPlayer2: false, isLocalDuel));
            SkillPressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Skill, isPlayer2: false, isLocalDuel));
            UltimatePressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Ultimate, isPlayer2: false, isLocalDuel));
            MeditateHeld = IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Meditate, isPlayer2: false, isLocalDuel));
        }

        private void ReadP2Input(Keyboard kb)
        {
            // P2 is strictly active in Local Duel mode
            float horizontal = 0f;
            if (IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Left, isPlayer2: true, isLocalDuel: true))) horizontal -= 1f;
            if (IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Right, isPlayer2: true, isLocalDuel: true))) horizontal += 1f;

            float vertical = 0f;
            if (IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Jump, isPlayer2: true, isLocalDuel: true))) vertical += 1f;
            if (IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Crouch, isPlayer2: true, isLocalDuel: true))) vertical -= 1f;

            MoveInput = new Vector2(horizontal, vertical);

            JumpPressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Jump, isPlayer2: true, isLocalDuel: true));
            CrouchHeld = IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Crouch, isPlayer2: true, isLocalDuel: true));
            LightAttackPressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.LightAttack, isPlayer2: true, isLocalDuel: true));
            HeavyAttackPressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.HeavyAttack, isPlayer2: true, isLocalDuel: true));
            SkillPressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Skill, isPlayer2: true, isLocalDuel: true));
            UltimatePressed = WasPressedThisFrame(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Ultimate, isPlayer2: true, isLocalDuel: true));
            MeditateHeld = IsPressed(kb, InputSettingsManager.GetRuntimeKey(FighterAction.Meditate, isPlayer2: true, isLocalDuel: true));
        }

        private static bool IsPressed(Keyboard kb, Key key)
        {
            if (kb == null || key == Key.None) return false;
            try
            {
                var control = kb[key];
                return control != null && control.isPressed;
            }
            catch
            {
                return false;
            }
        }

        private static bool WasPressedThisFrame(Keyboard kb, Key key)
        {
            if (kb == null || key == Key.None) return false;
            try
            {
                var control = kb[key];
                return control != null && control.wasPressedThisFrame;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
