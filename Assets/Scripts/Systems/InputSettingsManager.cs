using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticCombat.Systems
{
    /// <summary>
    /// Fighter actions that can be bound to keyboard keys.
    /// </summary>
    public enum FighterAction
    {
        Left,
        Right,
        Jump,
        Crouch,
        LightAttack,
        HeavyAttack,
        Skill,
        Ultimate,
        Meditate
    }

    /// <summary>
    /// Central persistent manager for audio volume levels and game-mode-specific keybindings.
    /// Supports:
    /// 1. Single Player Mode (Vs CPU & LAN): 9 keys with full keyboard freedom.
    /// 2. Local Duel Mode (1v1 Shared Keyboard): 18 non-overlapping keys in a combined P1/P2 table.
    /// 3. Permanent persistence across sessions via Unity PlayerPrefs.
    /// </summary>
    public static class InputSettingsManager
    {
        #region Constants & PlayerPrefs Keys

        private const string PREF_MUSIC_VOL = "MC_Setting_MusicVolume";
        private const string PREF_SFX_VOL = "MC_Setting_SFXVolume";

        private const string PREF_PREFIX_SP = "MC_Bind_SP_";
        private const string PREF_PREFIX_DUEL_P1 = "MC_Bind_DuelP1_";
        private const string PREF_PREFIX_DUEL_P2 = "MC_Bind_DuelP2_";

        public const float DEFAULT_MUSIC_VOL = 1.00f;
        public const float DEFAULT_SFX_VOL = 1.00f;

        #endregion

        #region State

        private static bool isInitialized = false;

        public static float MusicVolume { get; private set; } = DEFAULT_MUSIC_VOL;
        public static float SFXVolume { get; private set; } = DEFAULT_SFX_VOL;

        private static readonly Dictionary<FighterAction, Key> singlePlayerKeys = new Dictionary<FighterAction, Key>();
        private static readonly Dictionary<FighterAction, Key> localDuelP1Keys = new Dictionary<FighterAction, Key>();
        private static readonly Dictionary<FighterAction, Key> localDuelP2Keys = new Dictionary<FighterAction, Key>();

        #endregion

        #region Initialization & Loading

        static InputSettingsManager()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (isInitialized) return;
            LoadAllSettings();
            isInitialized = true;
        }

        public static void LoadAllSettings()
        {
            // 1. Audio Volumes (Migrate previous 0.70f default to 1.00f so user experiences full BGM)
            float storedMusic = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, -1f);
            if (storedMusic < 0f || Mathf.Approximately(storedMusic, 0.70f))
            {
                MusicVolume = DEFAULT_MUSIC_VOL;
                PlayerPrefs.SetFloat(PREF_MUSIC_VOL, MusicVolume);
            }
            else
            {
                MusicVolume = storedMusic;
            }
            SFXVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, DEFAULT_SFX_VOL);

            // 2. Single Player Profile Defaults (WASD + Ergonomic Action Cluster)
            singlePlayerKeys[FighterAction.Left] = LoadKey(PREF_PREFIX_SP, FighterAction.Left, Key.A);
            singlePlayerKeys[FighterAction.Right] = LoadKey(PREF_PREFIX_SP, FighterAction.Right, Key.D);
            singlePlayerKeys[FighterAction.Jump] = LoadKey(PREF_PREFIX_SP, FighterAction.Jump, Key.W);
            singlePlayerKeys[FighterAction.Crouch] = LoadKey(PREF_PREFIX_SP, FighterAction.Crouch, Key.S);
            singlePlayerKeys[FighterAction.LightAttack] = LoadKey(PREF_PREFIX_SP, FighterAction.LightAttack, Key.F);
            singlePlayerKeys[FighterAction.HeavyAttack] = LoadKey(PREF_PREFIX_SP, FighterAction.HeavyAttack, Key.C);
            singlePlayerKeys[FighterAction.Skill] = LoadKey(PREF_PREFIX_SP, FighterAction.Skill, Key.E);
            singlePlayerKeys[FighterAction.Ultimate] = LoadKey(PREF_PREFIX_SP, FighterAction.Ultimate, Key.Q);
            singlePlayerKeys[FighterAction.Meditate] = LoadKey(PREF_PREFIX_SP, FighterAction.Meditate, Key.X);

            // 3. Local Duel Profile (Left-Half for P1, Right-Half for P2)
            localDuelP1Keys[FighterAction.Left] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.Left, Key.A);
            localDuelP1Keys[FighterAction.Right] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.Right, Key.D);
            localDuelP1Keys[FighterAction.Jump] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.Jump, Key.W);
            localDuelP1Keys[FighterAction.Crouch] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.Crouch, Key.S);
            localDuelP1Keys[FighterAction.LightAttack] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.LightAttack, Key.F);
            localDuelP1Keys[FighterAction.HeavyAttack] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.HeavyAttack, Key.C);
            localDuelP1Keys[FighterAction.Skill] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.Skill, Key.E);
            localDuelP1Keys[FighterAction.Ultimate] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.Ultimate, Key.Q);
            localDuelP1Keys[FighterAction.Meditate] = LoadKey(PREF_PREFIX_DUEL_P1, FighterAction.Meditate, Key.X);

            localDuelP2Keys[FighterAction.Left] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.Left, Key.J);
            localDuelP2Keys[FighterAction.Right] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.Right, Key.L);
            localDuelP2Keys[FighterAction.Jump] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.Jump, Key.I);
            localDuelP2Keys[FighterAction.Crouch] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.Crouch, Key.K);
            localDuelP2Keys[FighterAction.LightAttack] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.LightAttack, Key.H);
            localDuelP2Keys[FighterAction.HeavyAttack] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.HeavyAttack, Key.N);
            localDuelP2Keys[FighterAction.Skill] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.Skill, Key.U);
            localDuelP2Keys[FighterAction.Ultimate] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.Ultimate, Key.O);
            localDuelP2Keys[FighterAction.Meditate] = LoadKey(PREF_PREFIX_DUEL_P2, FighterAction.Meditate, Key.M);
        }

        private static Key LoadKey(string prefix, FighterAction action, Key defaultKey)
        {
            string prefKey = prefix + action.ToString();
            if (PlayerPrefs.HasKey(prefKey))
            {
                string keyName = PlayerPrefs.GetString(prefKey);
                if (Enum.TryParse(keyName, out Key parsedKey))
                {
                    return parsedKey;
                }
            }
            return defaultKey;
        }

        public static void SaveAllSettings()
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, MusicVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, SFXVolume);

            foreach (var kvp in singlePlayerKeys)
            {
                PlayerPrefs.SetString(PREF_PREFIX_SP + kvp.Key.ToString(), kvp.Value.ToString());
            }

            foreach (var kvp in localDuelP1Keys)
            {
                PlayerPrefs.SetString(PREF_PREFIX_DUEL_P1 + kvp.Key.ToString(), kvp.Value.ToString());
            }

            foreach (var kvp in localDuelP2Keys)
            {
                PlayerPrefs.SetString(PREF_PREFIX_DUEL_P2 + kvp.Key.ToString(), kvp.Value.ToString());
            }

            PlayerPrefs.Save();
            Debug.Log("<color=#00e676>[InputSettingsManager]</color> All settings & custom keybindings saved to disk.");
        }

        public static void ResetToDefaults()
        {
            MusicVolume = DEFAULT_MUSIC_VOL;
            SFXVolume = DEFAULT_SFX_VOL;

            // Clear PlayerPrefs for keys
            foreach (FighterAction action in Enum.GetValues(typeof(FighterAction)))
            {
                PlayerPrefs.DeleteKey(PREF_PREFIX_SP + action.ToString());
                PlayerPrefs.DeleteKey(PREF_PREFIX_DUEL_P1 + action.ToString());
                PlayerPrefs.DeleteKey(PREF_PREFIX_DUEL_P2 + action.ToString());
            }
            PlayerPrefs.DeleteKey(PREF_MUSIC_VOL);
            PlayerPrefs.DeleteKey(PREF_SFX_VOL);

            isInitialized = false;
            EnsureInitialized();
            SaveAllSettings();
            Debug.Log("<color=#ffaa00>[InputSettingsManager]</color> Reset all keybindings and volumes to original defaults.");
        }

        #endregion

        #region Public Getters & Setters

        public static void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
        }

        public static void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
        }

        public static Key GetSinglePlayerKey(FighterAction action)
        {
            EnsureInitialized();
            return singlePlayerKeys.TryGetValue(action, out Key k) ? k : Key.None;
        }

        public static void SetSinglePlayerKey(FighterAction action, Key newKey)
        {
            EnsureInitialized();
            singlePlayerKeys[action] = newKey;
        }

        public static Key GetLocalDuelP1Key(FighterAction action)
        {
            EnsureInitialized();
            return localDuelP1Keys.TryGetValue(action, out Key k) ? k : Key.None;
        }

        public static void SetLocalDuelP1Key(FighterAction action, Key newKey)
        {
            EnsureInitialized();
            localDuelP1Keys[action] = newKey;
        }

        public static Key GetLocalDuelP2Key(FighterAction action)
        {
            EnsureInitialized();
            return localDuelP2Keys.TryGetValue(action, out Key k) ? k : Key.None;
        }

        public static void SetLocalDuelP2Key(FighterAction action, Key newKey)
        {
            EnsureInitialized();
            localDuelP2Keys[action] = newKey;
        }

        /// <summary>
        /// Unified runtime key lookup: automatically provides the right key based on
        /// whether the player is Player 1 or Player 2, and whether playing Single Player (Vs CPU / LAN)
        /// or 1v1 Local Duel (Shared Keyboard).
        /// </summary>
        public static Key GetRuntimeKey(FighterAction action, bool isPlayer2, bool isLocalDuel)
        {
            EnsureInitialized();

            if (isPlayer2)
            {
                return GetLocalDuelP2Key(action);
            }
            else
            {
                return isLocalDuel ? GetLocalDuelP1Key(action) : GetSinglePlayerKey(action);
            }
        }

        #endregion
    }
}
