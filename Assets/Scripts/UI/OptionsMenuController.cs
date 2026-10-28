using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;
using MysticCombat.Systems;

namespace MysticCombat.UI
{
    /// <summary>
    /// Interactive controller for the Game Options & Controls modal.
    /// Features:
    /// 1. Hierarchical Screen Architecture:
    ///    - Main Options Hub (Volume Sliders, Sub-Menu Launchers, Defaults & Exit).
    ///    - Single Player Sub-Screen (9 actions, spacious 24pt layout).
    ///    - 1v1 Local Duel Sub-Screen (18 keys in a 3-column combined table).
    /// 2. Obvious Visual Navigation:
    ///    - Blinking arcade cursor (►) preceding the selected item.
    ///    - Pulsing gold/white focus frame on the active element.
    ///    - Keyboard (W/S/A/D, Arrows, Enter, Space, Esc) and full Mouse hover/click support.
    /// 3. Persistent Audio & Keybinding settings via InputSettingsManager / PlayerPrefs.
    /// </summary>
    public class OptionsMenuController : MonoBehaviour
    {
        #region Enums & Sub-classes

        public enum OptionsView
        {
            MainHub,
            SinglePlayer,
            LocalDuel
        }

        [Serializable]
        public class SinglePlayerKeyRow
        {
            public FighterAction action;
            public string actionDisplayName;
            public Button button;
            public Outline buttonOutline;
            public TextMeshProUGUI actionText;
            public TextMeshProUGUI keyText;
        }

        [Serializable]
        public class LocalDuelKeyRow
        {
            public FighterAction action;
            public string actionDisplayName;
            public TextMeshProUGUI actionText;
            public Button p1Button;
            public Outline p1Outline;
            public TextMeshProUGUI p1Text;
            public Button p2Button;
            public Outline p2Outline;
            public TextMeshProUGUI p2Text;
        }

        #endregion

        #region Serialized Fields

        [Header("Main Menu Reference")]
        [SerializeField] private MainMenuController mainMenuController;

        [Header("View Panels")]
        [SerializeField] private GameObject mainHubPanel;
        [SerializeField] private GameObject singlePlayerPanel;
        [SerializeField] private GameObject localDuelPanel;

        [Header("Main Hub Elements")]
        [SerializeField] private Outline musicRowOutline;
        [SerializeField] private TextMeshProUGUI musicLabelText;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TextMeshProUGUI musicValText;

        [SerializeField] private Outline sfxRowOutline;
        [SerializeField] private TextMeshProUGUI sfxLabelText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI sfxValText;

        [SerializeField] private Button btnOpenSinglePlayer;
        [SerializeField] private Outline btnOpenSinglePlayerOutline;
        [SerializeField] private TextMeshProUGUI btnOpenSinglePlayerText;

        [SerializeField] private Button btnOpenLocalDuel;
        [SerializeField] private Outline btnOpenLocalDuelOutline;
        [SerializeField] private TextMeshProUGUI btnOpenLocalDuelText;

        [SerializeField] private Button btnMainResetDefaults;
        [SerializeField] private Outline btnMainResetDefaultsOutline;
        [SerializeField] private TextMeshProUGUI btnMainResetDefaultsText;

        [SerializeField] private Button btnMainSaveClose;
        [SerializeField] private Outline btnMainSaveCloseOutline;
        [SerializeField] private TextMeshProUGUI btnMainSaveCloseText;

        [Header("Single Player Sub-Menu Elements")]
        [SerializeField] private List<SinglePlayerKeyRow> singlePlayerRows = new List<SinglePlayerKeyRow>();
        [SerializeField] private Button spBackBtn;
        [SerializeField] private Outline spBackOutline;
        [SerializeField] private TextMeshProUGUI spBackText;

        [Header("Local Duel Sub-Menu Elements")]
        [SerializeField] private List<LocalDuelKeyRow> localDuelRows = new List<LocalDuelKeyRow>();
        [SerializeField] private Button ldBackBtn;
        [SerializeField] private Outline ldBackOutline;
        [SerializeField] private TextMeshProUGUI ldBackText;

        #endregion

        #region Navigation State

        private OptionsView currentView = OptionsView.MainHub;

        // Navigation Indices
        private int mainNavIndex = 0; // 0: Music, 1: SFX, 2: Open SP, 3: Open Duel, 4: Reset, 5: Save/Back
        private int spNavIndex = 0;   // 0..8: Key rows, 9: Back button
        private int ldRowIndex = 0;   // 0..8: Key rows, 9: Back button
        private int ldColIndex = 0;   // 0: Player 1, 1: Player 2

        private float lastNavInputTime = 0f;
        private const float NAV_REPEAT_DELAY = 0.20f;

        // Rebinding state
        private bool isListeningForRebind = false;
        private FighterAction activeRebindAction;
        private bool activeRebindIsPlayer2 = false;
        private bool activeRebindIsLocalDuel = false;
        private TextMeshProUGUI activeRebindLabel;
        private Key originalKeyBeforeRebind = Key.None;
        private float rebindBlinkTimer = 0f;

        // Colors
        private static readonly Color FocusGold = new Color(1.0f, 0.85f, 0.15f, 1f);
        private static readonly Color FocusWhite = new Color(1.0f, 1.0f, 1.0f, 1f);
        private static readonly Color NormalBorder = new Color(0.22f, 0.28f, 0.40f, 0.8f);
        private static readonly Color NormalText = new Color(0.85f, 0.90f, 0.98f, 1f);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            HookAllListeners();
        }

        private void OnEnable()
        {
            InputSettingsManager.EnsureInitialized();
            isListeningForRebind = false;

            // Load sliders
            if (musicSlider != null)
            {
                musicSlider.value = InputSettingsManager.MusicVolume;
                UpdateMusicLabel(InputSettingsManager.MusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = InputSettingsManager.SFXVolume;
                UpdateSFXLabel(InputSettingsManager.SFXVolume);
            }

            // Always open to Main Hub
            SwitchView(OptionsView.MainHub);
            mainNavIndex = 0;
            RefreshAllKeyLabels();
            UpdateVisualFocus();
        }

        private void Update()
        {
            if (isListeningForRebind)
            {
                HandleRebindListening();
                return;
            }

            HandleNavigationInput();
            AnimateFocusPulse();
        }

        #endregion

        #region View Switching

        public void SwitchView(OptionsView view)
        {
            if (isListeningForRebind)
            {
                CancelActiveRebind();
            }

            currentView = view;

            if (mainHubPanel != null) mainHubPanel.SetActive(view == OptionsView.MainHub);
            if (singlePlayerPanel != null) singlePlayerPanel.SetActive(view == OptionsView.SinglePlayer);
            if (localDuelPanel != null) localDuelPanel.SetActive(view == OptionsView.LocalDuel);

            if (view == OptionsView.MainHub)
            {
                // Reset or preserve index
                if (mainNavIndex < 0 || mainNavIndex > 5) mainNavIndex = 0;
            }
            else if (view == OptionsView.SinglePlayer)
            {
                spNavIndex = 0;
            }
            else if (view == OptionsView.LocalDuel)
            {
                ldRowIndex = 0;
                ldColIndex = 0;
            }

            RefreshAllKeyLabels();
            UpdateVisualFocus();
        }

        #endregion

        #region Navigation Input Handling

        private void HandleNavigationInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // Escape Handling
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (currentView == OptionsView.MainHub)
                {
                    SaveAndClose();
                }
                else
                {
                    SwitchView(OptionsView.MainHub);
                }
                return;
            }

            float now = Time.unscaledTime;
            bool canNavigate = (now - lastNavInputTime) >= NAV_REPEAT_DELAY;

            // Directional detection
            bool up = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            bool down = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
            bool left = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
            bool right = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
            bool confirm = kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

            // Held repetition for sliders
            if (canNavigate)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) left = true;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) right = true;
            }

            if (up || down || left || right || confirm)
            {
                lastNavInputTime = now;
            }

            switch (currentView)
            {
                case OptionsView.MainHub:
                    HandleMainHubInput(up, down, left, right, confirm);
                    break;
                case OptionsView.SinglePlayer:
                    HandleSinglePlayerInput(up, down, left, right, confirm);
                    break;
                case OptionsView.LocalDuel:
                    HandleLocalDuelInput(up, down, left, right, confirm);
                    break;
            }
        }

        private void HandleMainHubInput(bool up, bool down, bool left, bool right, bool confirm)
        {
            if (up)
            {
                mainNavIndex = (mainNavIndex - 1 + 6) % 6;
                UpdateVisualFocus();
            }
            else if (down)
            {
                mainNavIndex = (mainNavIndex + 1) % 6;
                UpdateVisualFocus();
            }

            // Slider nudging on Left / Right
            if (mainNavIndex == 0) // Music
            {
                if (left && musicSlider != null)
                {
                    musicSlider.value = Mathf.Max(0f, musicSlider.value - 0.05f);
                }
                else if (right && musicSlider != null)
                {
                    musicSlider.value = Mathf.Min(1f, musicSlider.value + 0.05f);
                }
            }
            else if (mainNavIndex == 1) // SFX
            {
                if (left && sfxSlider != null)
                {
                    sfxSlider.value = Mathf.Max(0f, sfxSlider.value - 0.05f);
                }
                else if (right && sfxSlider != null)
                {
                    sfxSlider.value = Mathf.Min(1f, sfxSlider.value + 0.05f);
                }
            }

            if (confirm)
            {
                switch (mainNavIndex)
                {
                    case 2: // Open Single Player
                        SwitchView(OptionsView.SinglePlayer);
                        break;
                    case 3: // Open Local Duel
                        SwitchView(OptionsView.LocalDuel);
                        break;
                    case 4: // Reset Defaults
                        ResetToDefaults();
                        break;
                    case 5: // Save & Close
                        SaveAndClose();
                        break;
                }
            }
        }

        private void HandleSinglePlayerInput(bool up, bool down, bool left, bool right, bool confirm)
        {
            int total = singlePlayerRows.Count + 1; // 9 rows + Back button
            if (up)
            {
                spNavIndex = (spNavIndex - 1 + total) % total;
                UpdateVisualFocus();
            }
            else if (down)
            {
                spNavIndex = (spNavIndex + 1) % total;
                UpdateVisualFocus();
            }

            if (confirm)
            {
                if (spNavIndex < singlePlayerRows.Count)
                {
                    var row = singlePlayerRows[spNavIndex];
                    OnKeyButtonClicked(row.action, isPlayer2: false, isLocalDuel: false, row.keyText);
                }
                else
                {
                    // Back button
                    SwitchView(OptionsView.MainHub);
                }
            }
        }

        private void HandleLocalDuelInput(bool up, bool down, bool left, bool right, bool confirm)
        {
            int total = localDuelRows.Count + 1; // 9 rows + Back button
            if (up)
            {
                ldRowIndex = (ldRowIndex - 1 + total) % total;
                UpdateVisualFocus();
            }
            else if (down)
            {
                ldRowIndex = (ldRowIndex + 1) % total;
                UpdateVisualFocus();
            }

            // Left / Right switches between P1 and P2 on rows 0..8
            if (ldRowIndex < localDuelRows.Count)
            {
                if (left)
                {
                    ldColIndex = 0;
                    UpdateVisualFocus();
                }
                else if (right)
                {
                    ldColIndex = 1;
                    UpdateVisualFocus();
                }
            }

            if (confirm)
            {
                if (ldRowIndex < localDuelRows.Count)
                {
                    var row = localDuelRows[ldRowIndex];
                    if (ldColIndex == 0)
                    {
                        OnKeyButtonClicked(row.action, isPlayer2: false, isLocalDuel: true, row.p1Text);
                    }
                    else
                    {
                        OnKeyButtonClicked(row.action, isPlayer2: true, isLocalDuel: true, row.p2Text);
                    }
                }
                else
                {
                    // Back button
                    SwitchView(OptionsView.MainHub);
                }
            }
        }

        #endregion

        #region Visual Focus & Pulse Animation

        private void UpdateVisualFocus()
        {
            if (currentView == OptionsView.MainHub)
            {
                SetRowFocus(musicRowOutline, musicLabelText, "MUSIC BGM", mainNavIndex == 0);
                SetRowFocus(sfxRowOutline, sfxLabelText, "SFX SOUND", mainNavIndex == 1);
                SetRowFocus(btnOpenSinglePlayerOutline, btnOpenSinglePlayerText, "CONFIGURE SINGLE PLAYER KEYS (Vs CPU / LAN)  »", mainNavIndex == 2);
                SetRowFocus(btnOpenLocalDuelOutline, btnOpenLocalDuelText, "CONFIGURE 1v1 LOCAL DUEL KEYS (SHARED KEYBOARD)  »", mainNavIndex == 3);
                SetRowFocus(btnMainResetDefaultsOutline, btnMainResetDefaultsText, "[ ↺ RESET ALL TO DEFAULTS ]", mainNavIndex == 4);
                SetRowFocus(btnMainSaveCloseOutline, btnMainSaveCloseText, "[ « SAVE & BACK TO TITLE (ESC) ]", mainNavIndex == 5);
            }
            else if (currentView == OptionsView.SinglePlayer)
            {
                for (int i = 0; i < singlePlayerRows.Count; i++)
                {
                    var r = singlePlayerRows[i];
                    bool isFocused = (spNavIndex == i);
                    if (r.actionText != null)
                    {
                        r.actionText.text = isFocused ? $"►  {r.actionDisplayName}" : $"   {r.actionDisplayName}";
                        r.actionText.color = isFocused ? FocusGold : NormalText;
                    }
                    if (r.buttonOutline != null)
                    {
                        r.buttonOutline.effectColor = isFocused ? FocusGold : NormalBorder;
                        r.buttonOutline.effectDistance = isFocused ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
                    }
                }

                SetRowFocus(spBackOutline, spBackText, "[ « BACK TO OPTIONS (ESC) ]", spNavIndex == singlePlayerRows.Count);
            }
            else if (currentView == OptionsView.LocalDuel)
            {
                for (int i = 0; i < localDuelRows.Count; i++)
                {
                    var r = localDuelRows[i];
                    bool rowFocused = (ldRowIndex == i);

                    if (r.actionText != null)
                    {
                        r.actionText.text = rowFocused ? $"►  {r.actionDisplayName}" : $"   {r.actionDisplayName}";
                        r.actionText.color = rowFocused ? FocusGold : NormalText;
                    }

                    bool p1Focused = rowFocused && (ldColIndex == 0);
                    bool p2Focused = rowFocused && (ldColIndex == 1);

                    if (r.p1Outline != null)
                    {
                        r.p1Outline.effectColor = p1Focused ? FocusGold : NormalBorder;
                        r.p1Outline.effectDistance = p1Focused ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
                    }

                    if (r.p2Outline != null)
                    {
                        r.p2Outline.effectColor = p2Focused ? FocusGold : NormalBorder;
                        r.p2Outline.effectDistance = p2Focused ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
                    }
                }

                SetRowFocus(ldBackOutline, ldBackText, "[ « BACK TO OPTIONS (ESC) ]", ldRowIndex == localDuelRows.Count);
            }
        }

        private void SetRowFocus(Outline outline, TextMeshProUGUI label, string baseText, bool isFocused)
        {
            if (label != null)
            {
                label.text = isFocused ? $"►  {baseText}" : $"   {baseText}";
                label.color = isFocused ? FocusGold : NormalText;
            }

            if (outline != null)
            {
                outline.effectColor = isFocused ? FocusGold : NormalBorder;
                outline.effectDistance = isFocused ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
            }
        }

        private void AnimateFocusPulse()
        {
            float t = (Mathf.Sin(Time.unscaledTime * 7f) + 1f) * 0.5f;
            Color pulseCol = Color.Lerp(FocusGold, FocusWhite, t);

            if (currentView == OptionsView.MainHub)
            {
                Outline activeOutline = null;
                switch (mainNavIndex)
                {
                    case 0: activeOutline = musicRowOutline; break;
                    case 1: activeOutline = sfxRowOutline; break;
                    case 2: activeOutline = btnOpenSinglePlayerOutline; break;
                    case 3: activeOutline = btnOpenLocalDuelOutline; break;
                    case 4: activeOutline = btnMainResetDefaultsOutline; break;
                    case 5: activeOutline = btnMainSaveCloseOutline; break;
                }
                if (activeOutline != null) activeOutline.effectColor = pulseCol;
            }
            else if (currentView == OptionsView.SinglePlayer)
            {
                if (spNavIndex < singlePlayerRows.Count)
                {
                    var r = singlePlayerRows[spNavIndex];
                    if (r.buttonOutline != null) r.buttonOutline.effectColor = pulseCol;
                }
                else
                {
                    if (spBackOutline != null) spBackOutline.effectColor = pulseCol;
                }
            }
            else if (currentView == OptionsView.LocalDuel)
            {
                if (ldRowIndex < localDuelRows.Count)
                {
                    var r = localDuelRows[ldRowIndex];
                    if (ldColIndex == 0 && r.p1Outline != null) r.p1Outline.effectColor = pulseCol;
                    if (ldColIndex == 1 && r.p2Outline != null) r.p2Outline.effectColor = pulseCol;
                }
                else
                {
                    if (ldBackOutline != null) ldBackOutline.effectColor = pulseCol;
                }
            }
        }

        #endregion

        #region Setup & Event Hooking

        public void InitializeReferences(
            MainMenuController mmc,
            GameObject hubPanel, GameObject spPanel, GameObject ldPanel,
            Outline mOutline, TextMeshProUGUI mLbl, Slider mSlider, TextMeshProUGUI mVal,
            Outline sOutline, TextMeshProUGUI sLbl, Slider sSlider, TextMeshProUGUI sVal,
            Button btnSp, Outline btnSpOutline, TextMeshProUGUI btnSpTxt,
            Button btnLd, Outline btnLdOutline, TextMeshProUGUI btnLdTxt,
            Button btnReset, Outline btnResetOutline, TextMeshProUGUI btnResetTxt,
            Button btnClose, Outline btnCloseOutline, TextMeshProUGUI btnCloseTxt,
            Button spBack, Outline spBOutline, TextMeshProUGUI spBTxt,
            Button ldBack, Outline ldBOutline, TextMeshProUGUI ldBTxt)
        {
            mainMenuController = mmc;
            mainHubPanel = hubPanel;
            singlePlayerPanel = spPanel;
            localDuelPanel = ldPanel;

            musicRowOutline = mOutline;
            musicLabelText = mLbl;
            musicSlider = mSlider;
            musicValText = mVal;

            sfxRowOutline = sOutline;
            sfxLabelText = sLbl;
            sfxSlider = sSlider;
            sfxValText = sVal;

            btnOpenSinglePlayer = btnSp;
            btnOpenSinglePlayerOutline = btnSpOutline;
            btnOpenSinglePlayerText = btnSpTxt;

            btnOpenLocalDuel = btnLd;
            btnOpenLocalDuelOutline = btnLdOutline;
            btnOpenLocalDuelText = btnLdTxt;

            btnMainResetDefaults = btnReset;
            btnMainResetDefaultsOutline = btnResetOutline;
            btnMainResetDefaultsText = btnResetTxt;

            btnMainSaveClose = btnClose;
            btnMainSaveCloseOutline = btnCloseOutline;
            btnMainSaveCloseText = btnCloseTxt;

            spBackBtn = spBack;
            spBackOutline = spBOutline;
            spBackText = spBTxt;

            ldBackBtn = ldBack;
            ldBackOutline = ldBOutline;
            ldBackText = ldBTxt;

            HookAllListeners();
        }

        public void AddSinglePlayerRow(FighterAction action, string name, Button btn, Outline outline, TextMeshProUGUI actText, TextMeshProUGUI keyText)
        {
            int index = singlePlayerRows.Count;
            singlePlayerRows.Add(new SinglePlayerKeyRow
            {
                action = action,
                actionDisplayName = name,
                button = btn,
                buttonOutline = outline,
                actionText = actText,
                keyText = keyText
            });

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    spNavIndex = index;
                    UpdateVisualFocus();
                    OnKeyButtonClicked(action, isPlayer2: false, isLocalDuel: false, keyText);
                });
            }
        }

        public void AddLocalDuelRow(FighterAction action, string name, TextMeshProUGUI actText, Button p1Btn, Outline p1Out, TextMeshProUGUI p1Txt, Button p2Btn, Outline p2Out, TextMeshProUGUI p2Txt)
        {
            int index = localDuelRows.Count;
            localDuelRows.Add(new LocalDuelKeyRow
            {
                action = action,
                actionDisplayName = name,
                actionText = actText,
                p1Button = p1Btn,
                p1Outline = p1Out,
                p1Text = p1Txt,
                p2Button = p2Btn,
                p2Outline = p2Out,
                p2Text = p2Txt
            });

            if (p1Btn != null)
            {
                p1Btn.onClick.RemoveAllListeners();
                p1Btn.onClick.AddListener(() =>
                {
                    ldRowIndex = index;
                    ldColIndex = 0;
                    UpdateVisualFocus();
                    OnKeyButtonClicked(action, isPlayer2: false, isLocalDuel: true, p1Txt);
                });
            }

            if (p2Btn != null)
            {
                p2Btn.onClick.RemoveAllListeners();
                p2Btn.onClick.AddListener(() =>
                {
                    ldRowIndex = index;
                    ldColIndex = 1;
                    UpdateVisualFocus();
                    OnKeyButtonClicked(action, isPlayer2: true, isLocalDuel: true, p2Txt);
                });
            }
        }

        public void HookAllListeners()
        {
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
            }

            if (btnOpenSinglePlayer != null)
            {
                btnOpenSinglePlayer.onClick.RemoveAllListeners();
                btnOpenSinglePlayer.onClick.AddListener(() => SwitchView(OptionsView.SinglePlayer));
            }

            if (btnOpenLocalDuel != null)
            {
                btnOpenLocalDuel.onClick.RemoveAllListeners();
                btnOpenLocalDuel.onClick.AddListener(() => SwitchView(OptionsView.LocalDuel));
            }

            if (btnMainResetDefaults != null)
            {
                btnMainResetDefaults.onClick.RemoveAllListeners();
                btnMainResetDefaults.onClick.AddListener(ResetToDefaults);
            }

            if (btnMainSaveClose != null)
            {
                btnMainSaveClose.onClick.RemoveAllListeners();
                btnMainSaveClose.onClick.AddListener(SaveAndClose);
            }

            if (spBackBtn != null)
            {
                spBackBtn.onClick.RemoveAllListeners();
                spBackBtn.onClick.AddListener(() => SwitchView(OptionsView.MainHub));
            }

            if (ldBackBtn != null)
            {
                ldBackBtn.onClick.RemoveAllListeners();
                ldBackBtn.onClick.AddListener(() => SwitchView(OptionsView.MainHub));
            }

            for (int i = 0; i < singlePlayerRows.Count; i++)
            {
                var r = singlePlayerRows[i];
                int idx = i;
                if (r.button != null)
                {
                    r.button.onClick.RemoveAllListeners();
                    r.button.onClick.AddListener(() =>
                    {
                        spNavIndex = idx;
                        UpdateVisualFocus();
                        OnKeyButtonClicked(r.action, isPlayer2: false, isLocalDuel: false, r.keyText);
                    });
                }
            }

            for (int i = 0; i < localDuelRows.Count; i++)
            {
                var r = localDuelRows[i];
                int idx = i;
                if (r.p1Button != null)
                {
                    r.p1Button.onClick.RemoveAllListeners();
                    r.p1Button.onClick.AddListener(() =>
                    {
                        ldRowIndex = idx;
                        ldColIndex = 0;
                        UpdateVisualFocus();
                        OnKeyButtonClicked(r.action, isPlayer2: false, isLocalDuel: true, r.p1Text);
                    });
                }

                if (r.p2Button != null)
                {
                    r.p2Button.onClick.RemoveAllListeners();
                    r.p2Button.onClick.AddListener(() =>
                    {
                        ldRowIndex = idx;
                        ldColIndex = 1;
                        UpdateVisualFocus();
                        OnKeyButtonClicked(r.action, isPlayer2: true, isLocalDuel: true, r.p2Text);
                    });
                }
            }
        }

        #endregion

        #region Audio Handlers

        private void OnMusicSliderChanged(float val)
        {
            InputSettingsManager.SetMusicVolume(val);
            UpdateMusicLabel(val);

            if (mainMenuController != null)
            {
                mainMenuController.SetMusicVolume(val);
            }
        }

        private void OnSFXSliderChanged(float val)
        {
            InputSettingsManager.SetSFXVolume(val);
            UpdateSFXLabel(val);
        }

        private void UpdateMusicLabel(float val)
        {
            if (musicValText != null)
            {
                musicValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void UpdateSFXLabel(float val)
        {
            if (sfxValText != null)
            {
                sfxValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        #endregion

        #region Key Rebinding Logic

        private void OnKeyButtonClicked(FighterAction action, bool isPlayer2, bool isLocalDuel, TextMeshProUGUI label)
        {
            if (isListeningForRebind)
            {
                CancelActiveRebind();
            }

            activeRebindAction = action;
            activeRebindIsPlayer2 = isPlayer2;
            activeRebindIsLocalDuel = isLocalDuel;
            activeRebindLabel = label;

            originalKeyBeforeRebind = InputSettingsManager.GetRuntimeKey(action, isPlayer2, isLocalDuel);

            isListeningForRebind = true;
            rebindBlinkTimer = 0f;

            if (activeRebindLabel != null)
            {
                activeRebindLabel.text = "[ PRESS KEY ]";
                activeRebindLabel.color = FocusGold;
            }
        }

        private void HandleRebindListening()
        {
            rebindBlinkTimer += Time.unscaledDeltaTime * 6f;
            if (activeRebindLabel != null)
            {
                float t = (Mathf.Sin(rebindBlinkTimer) + 1f) * 0.5f;
                activeRebindLabel.color = Color.Lerp(FocusGold, FocusWhite, t);
            }

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                CancelActiveRebind();
                return;
            }

            foreach (KeyControl keyControl in kb.allKeys)
            {
                if (keyControl.wasPressedThisFrame)
                {
                    Key pressed = keyControl.keyCode;

                    if (pressed == Key.None || pressed == Key.Escape)
                    {
                        continue;
                    }

                    CommitRebind(pressed);
                    break;
                }
            }
        }

        private void CommitRebind(Key newKey)
        {
            if (!activeRebindIsLocalDuel)
            {
                InputSettingsManager.SetSinglePlayerKey(activeRebindAction, newKey);
            }
            else if (activeRebindIsPlayer2)
            {
                InputSettingsManager.SetLocalDuelP2Key(activeRebindAction, newKey);
            }
            else
            {
                InputSettingsManager.SetLocalDuelP1Key(activeRebindAction, newKey);
            }

            InputSettingsManager.SaveAllSettings();

            if (activeRebindLabel != null)
            {
                activeRebindLabel.text = FormatKeyName(newKey);
                activeRebindLabel.color = Color.white;
            }

            isListeningForRebind = false;
            activeRebindLabel = null;
        }

        private void CancelActiveRebind()
        {
            if (activeRebindLabel != null)
            {
                activeRebindLabel.text = FormatKeyName(originalKeyBeforeRebind);
                activeRebindLabel.color = Color.white;
            }

            isListeningForRebind = false;
            activeRebindLabel = null;
        }

        #endregion

        #region Refresh & Formatting

        public void RefreshAllKeyLabels()
        {
            foreach (SinglePlayerKeyRow row in singlePlayerRows)
            {
                if (row.keyText != null)
                {
                    Key k = InputSettingsManager.GetSinglePlayerKey(row.action);
                    row.keyText.text = FormatKeyName(k);
                    row.keyText.color = Color.white;
                }
            }

            foreach (LocalDuelKeyRow row in localDuelRows)
            {
                if (row.p1Text != null)
                {
                    Key k1 = InputSettingsManager.GetLocalDuelP1Key(row.action);
                    row.p1Text.text = FormatKeyName(k1);
                    row.p1Text.color = Color.white;
                }

                if (row.p2Text != null)
                {
                    Key k2 = InputSettingsManager.GetLocalDuelP2Key(row.action);
                    row.p2Text.text = FormatKeyName(k2);
                    row.p2Text.color = Color.white;
                }
            }
        }

        public static string FormatKeyName(Key key)
        {
            switch (key)
            {
                case Key.LeftArrow: return "←";
                case Key.RightArrow: return "→";
                case Key.UpArrow: return "↑";
                case Key.DownArrow: return "↓";
                case Key.Space: return "SPACE";
                case Key.Enter: return "ENTER";
                case Key.LeftShift: return "L-SHIFT";
                case Key.RightShift: return "R-SHIFT";
                case Key.LeftCtrl: return "L-CTRL";
                case Key.RightCtrl: return "R-CTRL";
                case Key.LeftAlt: return "L-ALT";
                case Key.RightAlt: return "R-ALT";
                case Key.Tab: return "TAB";
                case Key.Numpad0: return "NUM 0";
                case Key.Numpad1: return "NUM 1";
                case Key.Numpad2: return "NUM 2";
                case Key.Numpad3: return "NUM 3";
                case Key.Numpad4: return "NUM 4";
                case Key.Numpad5: return "NUM 5";
                case Key.Numpad6: return "NUM 6";
                case Key.Numpad7: return "NUM 7";
                case Key.Numpad8: return "NUM 8";
                case Key.Numpad9: return "NUM 9";
                default:
                    return key.ToString().ToUpper();
            }
        }

        #endregion

        #region Global Actions (Reset & Save)

        public void ResetToDefaults()
        {
            if (isListeningForRebind)
            {
                CancelActiveRebind();
            }

            InputSettingsManager.ResetToDefaults();

            if (musicSlider != null)
            {
                musicSlider.value = InputSettingsManager.MusicVolume;
                UpdateMusicLabel(InputSettingsManager.MusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = InputSettingsManager.SFXVolume;
                UpdateSFXLabel(InputSettingsManager.SFXVolume);
            }

            if (mainMenuController != null)
            {
                mainMenuController.SetMusicVolume(InputSettingsManager.MusicVolume);
            }

            RefreshAllKeyLabels();
            UpdateVisualFocus();
        }

        public void SaveAndClose()
        {
            if (isListeningForRebind)
            {
                CancelActiveRebind();
            }

            InputSettingsManager.SaveAllSettings();

            if (mainMenuController != null)
            {
                mainMenuController.CloseOptions();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
