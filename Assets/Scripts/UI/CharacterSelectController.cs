using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysticCombat.Core;
using MysticCombat.Data;
using MysticCombat.AI;

namespace MysticCombat.UI
{
    /// <summary>
    /// Controls the authentic 90s Arcade Character Selection Screen.
    /// Seamlessly replaces the Main Menu foreground while retaining cosmic background and BGM.
    /// Features:
    /// - 1P vs CPU (Manual CPU choice or [R] Instant Random CPU)
    /// - 2P Local Duel (Step-by-step P1 & P2 lock-in)
    /// - Dynamic stat loading directly from goku.json and saitama.json (Grimoire architecture)
    /// - Mirror match support with untouched, 100% original heroic portraits
    /// - Cinematic sliding VS clash sequence with flying avatar frames
    /// - High-readability arcade typography throughout
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        #region Sub-classes

        [System.Serializable]
        public class FighterCard
        {
            public string characterId = "goku";
            public string displayName = "GOKU";
            public string epithet = "HOPE OF THE UNIVERSE";

            [Header("Card Components")]
            public RectTransform cardRoot;
            public Image outlineImage;
            public Image portraitImage;
            public Image portraitBackdrop;

            [Header("Indicators")]
            public GameObject p1Indicator;
            public GameObject p2Indicator;
            public TextMeshProUGUI p2IndicatorText; // Displays "[ PLAYER 2 ]" or "[ CPU ]"

            [Header("Stats UI Elements")]
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI epithetText;
            public TextMeshProUGUI hpValueText;
            public TextMeshProUGUI speedValueText;
            public TextMeshProUGUI attackValueText;
            public TextMeshProUGUI skillValueText;
            public TextMeshProUGUI ultimateValueText;
        }

        public enum SelectionPhase
        {
            SelectingP1,
            SelectingP2OrCpu,
            ReadyLaunch
        }

        #endregion

        #region Serialized Fields

        [Header("Character Cards")]
        [SerializeField] private List<FighterCard> fighterCards = new List<FighterCard>();

        [Header("Header & Instruction Prompts")]
        [SerializeField] private TextMeshProUGUI mainHeaderTitle;
        [SerializeField] private TextMeshProUGUI selectionPromptText;
        [SerializeField] private TextMeshProUGUI instructionsFooterText;
        [SerializeField] private GameObject mirrorMatchNotice;

        [Header("Cinematic Versus Overlay")]
        [SerializeField] private GameObject versusOverlay;
        [SerializeField] private RectTransform versusP1CardRect;
        [SerializeField] private Image versusP1AvatarImage;
        [SerializeField] private TextMeshProUGUI versusP1NameText;
        [SerializeField] private RectTransform versusP2CardRect;
        [SerializeField] private Image versusP2AvatarImage;
        [SerializeField] private TextMeshProUGUI versusP2NameText;
        [SerializeField] private TextMeshProUGUI versusP2BadgeText;
        [SerializeField] private Outline versusP2CardOutline;
        [SerializeField] private RectTransform versusCenterBadgeRect;
        [SerializeField] private TextMeshProUGUI versusBannerText;

        [Header("Action Buttons")]
        [SerializeField] private GameObject randomCpuButton;

        [Header("Main Menu Reference")]
        [SerializeField] private MainMenuController mainMenuController;

        [Header("Scene Loading")]
        [SerializeField] private string combatSceneName = "SampleScene";

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip navigateClip;
        [SerializeField] private AudioClip lockInClip;
        [SerializeField] private AudioClip versusLaunchClip;

        [Header("P1 / P2 Arcade Neon Colors")]
        [SerializeField] private Color p1ThemeColor = new Color(0.0f, 0.95f, 1.0f, 1f); // Electric Cyan
        [SerializeField] private Color p2ThemeColor = new Color(1.0f, 0.25f, 0.15f, 1f); // Fiery Crimson
        [SerializeField] private Color cpuThemeColor = new Color(1.0f, 0.85f, 0.20f, 1f); // Solar Gold
        [SerializeField] private Color neutralBorderColor = new Color(0.20f, 0.25f, 0.35f, 0.8f);

        #endregion

        #region State

        private GameManager.GameMode currentGameMode = GameManager.GameMode.VsCpu;
        private SelectionPhase currentPhase = SelectionPhase.SelectingP1;

        private int p1SelectedIndex = 0; // 0 = Goku, 1 = Saitama
        private int p2SelectedIndex = 1; // 0 = Goku, 1 = Saitama

        private float lastInputTime;
        private const float NAV_REPEAT_COOLDOWN = 0.20f;
        private bool isTransitioning = false;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null)
                {
                    sfxSource = gameObject.AddComponent<AudioSource>();
                }
            }
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            // Load procedural fallback clips if inspector clips are missing
            if (navigateClip == null) navigateClip = GenerateBlipAudio(600f, 0.05f);
            if (lockInClip == null) lockInClip = GeneratePunchAudio();
        }

        private void Start()
        {
            LoadCharacterStatsFromJson();
        }

        private void Update()
        {
            if (isTransitioning) return;

            HandleInput();
            AnimateSelectionBadges();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Activates the character selection screen for the specified game mode.
        /// </summary>
        public void Open(GameManager.GameMode mode)
        {
            currentGameMode = mode;
            currentPhase = SelectionPhase.SelectingP1;
            p1SelectedIndex = 0;
            p2SelectedIndex = (currentGameMode == GameManager.GameMode.VsCpu) ? 1 : 1;
            isTransitioning = false;

            if (versusOverlay != null)
            {
                versusOverlay.SetActive(false);
            }

            gameObject.SetActive(true);
            LoadCharacterStatsFromJson();
            UpdateUI();

            Debug.Log($"<color=#00e676>[CharacterSelectController]</color> Opened in mode: {mode}");
        }

        /// <summary>
        /// Cancels and returns to the Main Menu Title screen without restarting music.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (isTransitioning) return;

            Debug.Log("<color=#00e676>[CharacterSelectController]</color> Returning to Title Screen.");
            PlaySfx(navigateClip);

            gameObject.SetActive(false);
            if (mainMenuController != null)
            {
                mainMenuController.ReturnToMainMenuFromCharSelect();
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Cancel / Back key (Escape or Backspace)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.Backspace))
            {
                if (currentPhase == SelectionPhase.SelectingP2OrCpu)
                {
                    // Step back to P1 selection
                    currentPhase = SelectionPhase.SelectingP1;
                    PlaySfx(navigateClip);
                    UpdateUI();
                }
                else
                {
                    // Return to Title Menu
                    ReturnToMainMenu();
                }
                return;
            }

            // Quick / Random CPU Selection shortcut [R]
            if (currentGameMode == GameManager.GameMode.VsCpu && UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                SelectRandomCpuAndLaunch();
                return;
            }

            // Cycle CPU Difficulty shortcut [Tab]
            if (currentGameMode == GameManager.GameMode.VsCpu && UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                CycleCpuDifficulty();
            }

            float hAxis = UnityEngine.Input.GetAxisRaw("Horizontal");
            bool leftPressed = UnityEngine.Input.GetKeyDown(KeyCode.A) || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || (hAxis < -0.5f && Time.time - lastInputTime > NAV_REPEAT_COOLDOWN);
            bool rightPressed = UnityEngine.Input.GetKeyDown(KeyCode.D) || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || (hAxis > 0.5f && Time.time - lastInputTime > NAV_REPEAT_COOLDOWN);

            // In 2P Local mode during P2's turn, also accept P2's designated keys (J/L)
            if (currentPhase == SelectionPhase.SelectingP2OrCpu && currentGameMode == GameManager.GameMode.LocalPvP)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.J)) leftPressed = true;
                if (UnityEngine.Input.GetKeyDown(KeyCode.L)) rightPressed = true;
            }

            if (leftPressed)
            {
                Navigate(-1);
                lastInputTime = Time.time;
            }
            else if (rightPressed)
            {
                Navigate(1);
                lastInputTime = Time.time;
            }

            // Confirm Keys: Enter, Space, Keypad Enter, F (P1 Light), H (P2 Light)
            bool confirmPressed = UnityEngine.Input.GetKeyDown(KeyCode.Return) ||
                                 UnityEngine.Input.GetKeyDown(KeyCode.Space) ||
                                 UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) ||
                                 UnityEngine.Input.GetKeyDown(KeyCode.F);

            if (currentPhase == SelectionPhase.SelectingP2OrCpu && currentGameMode == GameManager.GameMode.LocalPvP)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.H)) confirmPressed = true;
            }

            if (confirmPressed)
            {
                ConfirmCurrentSelection();
            }
        }

        private void Navigate(int direction)
        {
            if (fighterCards == null || fighterCards.Count == 0) return;

            PlaySfx(navigateClip);

            if (currentPhase == SelectionPhase.SelectingP1)
            {
                p1SelectedIndex = (p1SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
            }
            else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
            {
                p2SelectedIndex = (p2SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
            }

            UpdateUI();
        }

        /// <summary>
        /// Confirms the current player or CPU selection.
        /// </summary>
        public void ConfirmCurrentSelection()
        {
            PlaySfx(lockInClip);

            if (currentPhase == SelectionPhase.SelectingP1)
            {
                currentPhase = SelectionPhase.SelectingP2OrCpu;
                UpdateUI();
            }
            else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
            {
                currentPhase = SelectionPhase.ReadyLaunch;
                StartCoroutine(LaunchMatchSequence());
            }
        }

        /// <summary>
        /// Instantly chooses a random character for the CPU and launches the match!
        /// </summary>
        public void SelectRandomCpuAndLaunch()
        {
            if (fighterCards == null || fighterCards.Count == 0 || isTransitioning) return;

            PlaySfx(lockInClip);
            p2SelectedIndex = Random.Range(0, fighterCards.Count);
            currentPhase = SelectionPhase.ReadyLaunch;
            UpdateUI();
            StartCoroutine(LaunchMatchSequence());
        }

        /// <summary>
        /// Cycles CPU difficulty between Easy, Hard, and Extreme when in VsCpu mode.
        /// </summary>
        public void CycleCpuDifficulty()
        {
            if (GameManager.Instance == null) return;
            CpuDifficulty current = GameManager.Instance.CurrentCpuDifficulty;
            CpuDifficulty next = current switch
            {
                CpuDifficulty.Easy => CpuDifficulty.Hard,
                CpuDifficulty.Hard => CpuDifficulty.Extreme,
                _ => CpuDifficulty.Easy
            };
            GameManager.Instance.SetCpuDifficulty(next);
            PlaySfx(navigateClip);
            UpdateUI();
        }

        #endregion

        #region Pointer / Click Handlers (Mouse / Touch Support)

        public void OnPointerClickCard(int index)
        {
            if (isTransitioning || index < 0 || index >= fighterCards.Count) return;

            if (currentPhase == SelectionPhase.SelectingP1)
            {
                if (p1SelectedIndex == index)
                {
                    ConfirmCurrentSelection();
                }
                else
                {
                    p1SelectedIndex = index;
                    PlaySfx(navigateClip);
                    UpdateUI();
                }
            }
            else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
            {
                if (p2SelectedIndex == index)
                {
                    ConfirmCurrentSelection();
                }
                else
                {
                    p2SelectedIndex = index;
                    PlaySfx(navigateClip);
                    UpdateUI();
                }
            }
        }

        public void OnClickRandomCpuButton()
        {
            SelectRandomCpuAndLaunch();
        }

        public void OnClickBackButton()
        {
            ReturnToMainMenu();
        }

        public void OnClickLockInButton()
        {
            ConfirmCurrentSelection();
        }

        #endregion

        #region UI Updates & Visual Animations

        private void UpdateUI()
        {
            if (fighterCards == null || fighterCards.Count == 0) return;

            // 1. Update Header & Sub-header prompt
            if (mainHeaderTitle != null)
            {
                mainHeaderTitle.text = "CHOOSE YOUR FIGHTER";
            }

            if (selectionPromptText != null)
            {
                if (currentPhase == SelectionPhase.SelectingP1)
                {
                    selectionPromptText.text = "<color=#00f5ff>▶ PLAYER 1: SELECT YOUR HERO ◀</color>";
                }
                else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
                {
                    if (currentGameMode == GameManager.GameMode.VsCpu)
                    {
                        selectionPromptText.text = "<color=#ffd700>▶ CHOOSE CPU OPPONENT  or  PRESS [R] FOR RANDOM ◀</color>";
                    }
                    else
                    {
                        selectionPromptText.text = "<color=#ff3b30>▶ PLAYER 2: SELECT YOUR HERO ◀</color>";
                    }
                }
                else
                {
                    selectionPromptText.text = "<color=#ffffff>⚡ GET READY FOR BATTLE! ⚡</color>";
                }
            }

            // 2. Update Instructions Footer
            if (instructionsFooterText != null)
            {
                if (currentGameMode == GameManager.GameMode.VsCpu)
                {
                    string diffStr = (GameManager.Instance != null) ? GameManager.Instance.CurrentCpuDifficulty.ToString().ToUpper() : "EASY";
                    instructionsFooterText.text = $"[A/D] MOVE   [ENTER/SPACE] CONFIRM   [TAB] CPU: {diffStr}   [R] RANDOM CPU   [ESC] BACK";
                }
                else
                {
                    instructionsFooterText.text = "[A/D or J/L] MOVE   [ENTER/SPACE/F/H] CONFIRM   [ESC] BACK";
                }
            }

            // 3. Update Cards Highlights & Indicators
            for (int i = 0; i < fighterCards.Count; i++)
            {
                var card = fighterCards[i];
                if (card == null) continue;

                bool isP1 = (p1SelectedIndex == i);
                bool isP2 = (p2SelectedIndex == i && currentPhase != SelectionPhase.SelectingP1);

                if (card.p1Indicator != null)
                {
                    card.p1Indicator.SetActive(isP1);
                }

                if (card.p2Indicator != null)
                {
                    card.p2Indicator.SetActive(isP2);
                    if (card.p2IndicatorText != null)
                    {
                        // Strict [ PLAYER 2 ] and [ CPU: DIFFICULTY ] bracket formatting
                        string diffBadge = (GameManager.Instance != null) ? $"[ CPU: {GameManager.Instance.CurrentCpuDifficulty.ToString().ToUpper()} ]" : "[ CPU ]";
                        card.p2IndicatorText.text = (currentGameMode == GameManager.GameMode.VsCpu) ? diffBadge : "[ PLAYER 2 ]";
                        card.p2IndicatorText.color = (currentGameMode == GameManager.GameMode.VsCpu) ? cpuThemeColor : p2ThemeColor;
                    }
                }

                // Avatars remain 100% original full color in all cases!
                if (card.portraitImage != null)
                {
                    card.portraitImage.color = Color.white;
                }

                // Outline color styling
                if (card.outlineImage != null)
                {
                    if (isP1 && isP2)
                    {
                        card.outlineImage.color = Color.Lerp(p1ThemeColor, (currentGameMode == GameManager.GameMode.VsCpu ? cpuThemeColor : p2ThemeColor), 0.5f);
                    }
                    else if (isP1)
                    {
                        card.outlineImage.color = p1ThemeColor;
                    }
                    else if (isP2)
                    {
                        card.outlineImage.color = (currentGameMode == GameManager.GameMode.VsCpu) ? cpuThemeColor : p2ThemeColor;
                    }
                    else
                    {
                        card.outlineImage.color = neutralBorderColor;
                    }
                }

                // Scale pulse on selected card
                if (card.cardRoot != null)
                {
                    bool isHighlighted = (currentPhase == SelectionPhase.SelectingP1 && isP1) ||
                                         (currentPhase == SelectionPhase.SelectingP2OrCpu && isP2);
                    card.cardRoot.localScale = isHighlighted ? new Vector3(1.03f, 1.03f, 1f) : Vector3.one;
                }
            }

            // 4. Mirror Match Notice
            if (mirrorMatchNotice != null)
            {
                bool isMirror = (currentPhase != SelectionPhase.SelectingP1 && p1SelectedIndex == p2SelectedIndex);
                mirrorMatchNotice.SetActive(isMirror);
            }

            // 5. Random CPU Action Button (Only visible in VsCpu mode)
            if (randomCpuButton != null)
            {
                randomCpuButton.SetActive(currentGameMode == GameManager.GameMode.VsCpu);
            }
        }

        private void AnimateSelectionBadges()
        {
            // Subtle neon breathing pulse on the active phase marker
            float pulse = Mathf.PingPong(Time.time * 3f, 1f);

            if (fighterCards != null && fighterCards.Count > 0)
            {
                if (currentPhase == SelectionPhase.SelectingP1 && p1SelectedIndex < fighterCards.Count)
                {
                    var p1Card = fighterCards[p1SelectedIndex];
                    if (p1Card != null && p1Card.outlineImage != null)
                    {
                        Color c = Color.Lerp(p1ThemeColor * 0.7f, p1ThemeColor * 1.3f, pulse);
                        p1Card.outlineImage.color = c;
                    }
                }
                else if (currentPhase == SelectionPhase.SelectingP2OrCpu && p2SelectedIndex < fighterCards.Count)
                {
                    var p2Card = fighterCards[p2SelectedIndex];
                    if (p2Card != null && p2Card.outlineImage != null)
                    {
                        Color targetColor = (currentGameMode == GameManager.GameMode.VsCpu) ? cpuThemeColor : p2ThemeColor;
                        Color c = Color.Lerp(targetColor * 0.7f, targetColor * 1.3f, pulse);
                        p2Card.outlineImage.color = c;
                    }
                }
            }
        }

        #endregion

        #region Grimoire Data Loading (Dynamic JSON Stats)

        private void LoadCharacterStatsFromJson()
        {
            foreach (var card in fighterCards)
            {
                if (card == null || string.IsNullOrEmpty(card.characterId)) continue;

                CharacterData data = CharacterDataLoader.Load(card.characterId.ToLower());
                if (data == null) continue;

                if (card.nameText != null)
                {
                    card.nameText.text = (data.displayName ?? card.characterId).ToUpper();
                }

                if (card.characterId.ToLower() == "goku")
                {
                    card.epithet = "HOPE OF THE UNIVERSE";
                    if (card.epithetText != null)
                    {
                        card.epithetText.text = card.epithet;
                        card.epithetText.color = new Color(1.0f, 0.90f, 0.12f, 1f); // Super Saiyan Radiant Gold
                    }
                }
                else if (card.characterId.ToLower() == "saitama")
                {
                    card.epithet = "HERO FOR FUN";
                    if (card.epithetText != null)
                    {
                        card.epithetText.text = card.epithet;
                        card.epithetText.color = new Color(1.0f, 0.30f, 0.30f, 1f); // Serious Punch Crimson
                    }
                }
                else if (card.epithetText != null)
                {
                    card.epithetText.text = card.epithet;
                }

                if (card.hpValueText != null)
                {
                    card.hpValueText.text = $"HP: {data.maxHealth}     SPD: {data.moveSpeed:F1} ({(data.moveSpeed >= 5.0f ? "FAST" : "BALANCED")})";
                }

                if (card.speedValueText != null)
                {
                    card.speedValueText.text = $"MOVE SPEED: {data.moveSpeed:F1}";
                }

                if (card.attackValueText != null)
                {
                    int lDmg = data.lightAttack != null ? data.lightAttack.damage : 8;
                    int hDmg = data.heavyAttack != null ? data.heavyAttack.damage : 15;
                    card.attackValueText.text = $"ATTACK: Light {lDmg}  |  Heavy {hDmg}";
                }

                if (card.skillValueText != null && data.skill != null)
                {
                    card.skillValueText.enableAutoSizing = true;
                    card.skillValueText.fontSizeMin = 13f;
                    card.skillValueText.fontSizeMax = 18f;

                    if (data.characterId == "goku" || data.skill.name.ToLower().Contains("saiyan"))
                    {
                        card.skillValueText.text = $"SKILL: Super Saiyan [Ki: {data.skill.kiCost:F0}] (+25% Spd, +Atk Buffs)";
                    }
                    else
                    {
                        card.skillValueText.text = (data.skill.name == "None" || data.skill.kiCost <= 0)
                            ? "SKILL: None"
                            : $"SKILL: {data.skill.name}  [Ki: {data.skill.kiCost:F0}]";
                    }
                }

                if (card.ultimateValueText != null && data.ultimate != null)
                {
                    card.ultimateValueText.text = $"ULTIMATE: {data.ultimate.name}  [Ki: {data.ultimate.kiCost:F0}]";
                }
            }
        }

        #endregion

        #region Cinematic Match Launch Sequence

        private IEnumerator LaunchMatchSequence()
        {
            isTransitioning = true;

            var p1Card = fighterCards[p1SelectedIndex];
            var p2Card = fighterCards[p2SelectedIndex];

            string p1Id = p1Card.characterId;
            string p2Id = p2Card.characterId;

            Debug.Log($"<color=#00e676>[CharacterSelectController]</color> LOCK IN! P1: {p1Id} VS {(currentGameMode == GameManager.GameMode.VsCpu ? "CPU" : "P2")}: {p2Id}");

            // Store in persistent GameManager singleton
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameMode(currentGameMode);
                GameManager.Instance.P1SelectedCharacterId = p1Id;
                GameManager.Instance.P2SelectedCharacterId = p2Id;
                GameManager.Instance.SetState(GameManager.GameState.InMatch);
            }

            // Setup Cinematic Versus Screen
            if (versusOverlay != null)
            {
                versusOverlay.SetActive(true);

                // Set P1 details
                if (versusP1AvatarImage != null && p1Card.portraitImage != null)
                {
                    versusP1AvatarImage.sprite = p1Card.portraitImage.sprite;
                    versusP1AvatarImage.color = Color.white; // Keep 100% normal
                }
                if (versusP1NameText != null)
                {
                    versusP1NameText.text = p1Card.displayName.ToUpper();
                }

                // Set P2 details
                if (versusP2AvatarImage != null && p2Card.portraitImage != null)
                {
                    versusP2AvatarImage.sprite = p2Card.portraitImage.sprite;
                    versusP2AvatarImage.color = Color.white; // Keep 100% normal
                }
                if (versusP2NameText != null)
                {
                    string prefix = (currentGameMode == GameManager.GameMode.VsCpu) ? "CPU " : "";
                    versusP2NameText.text = prefix + p2Card.displayName.ToUpper();
                }

                if (versusP2BadgeText != null)
                {
                    string diffBadge = (GameManager.Instance != null) ? $"[ CPU: {GameManager.Instance.CurrentCpuDifficulty.ToString().ToUpper()} ]" : "[ CPU ]";
                    versusP2BadgeText.text = (currentGameMode == GameManager.GameMode.VsCpu) ? diffBadge : "[ PLAYER 2 ]";
                    versusP2BadgeText.color = (currentGameMode == GameManager.GameMode.VsCpu) ? cpuThemeColor : p2ThemeColor;
                }

                if (versusP2CardOutline != null)
                {
                    versusP2CardOutline.effectColor = (currentGameMode == GameManager.GameMode.VsCpu) ? cpuThemeColor : p2ThemeColor;
                }

                if (versusBannerText != null)
                {
                    versusBannerText.text = "VS";
                }

                // Animate cinematic fly-in from off-screen into battle positions!
                Vector2 p1StartPos = new Vector2(-1500f, 0f);
                Vector2 p1TargetPos = new Vector2(-420f, 0f);

                Vector2 p2StartPos = new Vector2(1500f, 0f);
                Vector2 p2TargetPos = new Vector2(420f, 0f);

                Vector3 vsStartScale = new Vector3(3.5f, 3.5f, 1f);
                Vector3 vsTargetScale = Vector3.one;

                if (versusP1CardRect != null) versusP1CardRect.anchoredPosition = p1StartPos;
                if (versusP2CardRect != null) versusP2CardRect.anchoredPosition = p2StartPos;
                if (versusCenterBadgeRect != null) versusCenterBadgeRect.localScale = vsStartScale;

                PlaySfx(lockInClip);

                // Slide animation: 0.55s with smooth quintic ease-out deceleration
                float elapsed = 0f;
                float animDuration = 0.55f;
                while (elapsed < animDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / animDuration);
                    float ease = 1f - Mathf.Pow(1f - t, 4f); // Ease-out quintic

                    if (versusP1CardRect != null)
                    {
                        versusP1CardRect.anchoredPosition = Vector2.LerpUnclamped(p1StartPos, p1TargetPos, ease);
                    }
                    if (versusP2CardRect != null)
                    {
                        versusP2CardRect.anchoredPosition = Vector2.LerpUnclamped(p2StartPos, p2TargetPos, ease);
                    }
                    if (versusCenterBadgeRect != null)
                    {
                        versusCenterBadgeRect.localScale = Vector3.LerpUnclamped(vsStartScale, vsTargetScale, ease);
                    }

                    yield return null;
                }

                // Snap to final positions
                if (versusP1CardRect != null) versusP1CardRect.anchoredPosition = p1TargetPos;
                if (versusP2CardRect != null) versusP2CardRect.anchoredPosition = p2TargetPos;
                if (versusCenterBadgeRect != null) versusCenterBadgeRect.localScale = vsTargetScale;

                // Heavy impact strike sound as fighters slam in!
                PlaySfx(versusLaunchClip != null ? versusLaunchClip : lockInClip);

                // Hold screen for 1.8s so player can take in the epic cinematic showdown (~2.35s total)
                yield return new WaitForSeconds(1.8f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            // Stop menu background music right as match scene loads
            if (mainMenuController != null)
            {
                mainMenuController.StopTitleMusic();
            }

            // Load combat arena scene
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(combatSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(combatSceneName);
            }
        }

        #endregion

        #region Audio Helpers

        private void PlaySfx(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        private AudioClip GenerateBlipAudio(float frequency, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t)) * (1f - (float)i / samples) * 0.25f;
            }

            AudioClip clip = AudioClip.Create("Select_Blip", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GeneratePunchAudio()
        {
            int sampleRate = 44100;
            float duration = 0.18f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(180f, 40f, t / duration);
                float env = 1f - (t / duration);
                float noise = (Random.value * 2f - 1f) * 0.35f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f + noise) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("LockIn_Strike", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }
}
