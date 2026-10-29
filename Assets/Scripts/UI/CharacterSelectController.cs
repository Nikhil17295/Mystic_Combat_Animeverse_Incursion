using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.UI;

using TMPro;

using MysticCombat.Core;

using MysticCombat.Data;

using MysticCombat.AI;

using MysticCombat.Network;



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



        [Header("Sliding 2-Card Carousel Tuning")]
        [Tooltip("Horizontal offset for the Left visible card slot.")]
        [SerializeField] private float leftSlotX = -380f;
        [Tooltip("Horizontal offset for the Right visible card slot.")]
        [SerializeField] private float rightSlotX = 380f;
        [Tooltip("Off-screen staging distance for hidden cards.")]
        [SerializeField] private float offscreenX = 1300f;
        [Tooltip("Interpolation speed for sliding cards.")]
        [SerializeField] private float carouselSlideSpeed = 14f;

        private int carouselWindowStart = 0;

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

        private bool isLanHostLocked = false;

        private bool isLanClientLocked = false;



        #endregion



        #region Unity Lifecycle Events



        private void OnEnable()

        {

            if (LanNetworkManager.Instance != null)

            {

                LanNetworkManager.Instance.OnCharSelectSyncReceived += HandleNetworkCharSelectSync;

                LanNetworkManager.Instance.OnMatchStartReceived += HandleNetworkMatchStart;

            }

        }



        private void OnDisable()

        {

            if (LanNetworkManager.Instance != null)

            {

                LanNetworkManager.Instance.OnCharSelectSyncReceived -= HandleNetworkCharSelectSync;

                LanNetworkManager.Instance.OnMatchStartReceived -= HandleNetworkMatchStart;

            }

        }



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
            EnsureNezukoCardLoaded();
            EnsureTsunadeCardLoaded();
            LoadCharacterStatsFromJson();
            SnapCarouselPositions();
        }



        private void Update()

        {

            if (isTransitioning) return;



            HandleInput();
            UpdateCarouselPositions();
            AnimateSelectionBadges();

        }



        #endregion



        #region Public Methods



        /// <summary>

        /// Activates the character selection screen for the specified game mode.

        /// </summary>

        public void Open(GameManager.GameMode mode)
        {
            EnsureNezukoCardLoaded();
            currentGameMode = mode;
            isTransitioning = false;
            isLanHostLocked = false;
            isLanClientLocked = false;

            if (currentGameMode == GameManager.GameMode.LanPvP && GameManager.Instance != null && !GameManager.Instance.IsLanHost)
            {
                // Client starts directly on P2 selection!
                currentPhase = SelectionPhase.SelectingP2OrCpu;
                AdjustCarouselWindow(p2SelectedIndex, 1);
                UpdateUI();
            }
            else
            {
                currentPhase = SelectionPhase.SelectingP1;
                p1SelectedIndex = 0;
                p2SelectedIndex = (currentGameMode == GameManager.GameMode.VsCpu) ? 1 : 1;
                AdjustCarouselWindow(p1SelectedIndex, 0);
            }

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

            int targetIndex = 0;

            if (currentGameMode == GameManager.GameMode.LanPvP)
            {
                bool isHost = GameManager.Instance == null || GameManager.Instance.IsLanHost;
                if (isHost && !isLanHostLocked)
                {
                    p1SelectedIndex = (p1SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
                    targetIndex = p1SelectedIndex;
                    AdjustCarouselWindow(targetIndex, direction);
                    if (LanNetworkManager.Instance != null)
                    {
                        LanNetworkManager.Instance.SendCharSelectSync(new CharSelectSyncPacket
                        {
                            PlayerIndex = 1,
                            CharacterId = fighterCards[p1SelectedIndex].characterId,
                            IsLockedIn = false
                        });
                    }
                }
                else if (!isHost && !isLanClientLocked)
                {
                    p2SelectedIndex = (p2SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
                    targetIndex = p2SelectedIndex;
                    AdjustCarouselWindow(targetIndex, direction);
                    if (LanNetworkManager.Instance != null)
                    {
                        LanNetworkManager.Instance.SendCharSelectSync(new CharSelectSyncPacket
                        {
                            PlayerIndex = 2,
                            CharacterId = fighterCards[p2SelectedIndex].characterId,
                            IsLockedIn = false
                        });
                    }
                }
            }
            else
            {
                if (currentPhase == SelectionPhase.SelectingP1)
                {
                    p1SelectedIndex = (p1SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
                    targetIndex = p1SelectedIndex;
                    AdjustCarouselWindow(targetIndex, direction);
                }
                else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
                {
                    p2SelectedIndex = (p2SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
                    targetIndex = p2SelectedIndex;
                    AdjustCarouselWindow(targetIndex, direction);
                }
            }

            UpdateUI();
        }



        /// <summary>

        /// Confirms the current player or CPU selection.

        /// </summary>

        public void ConfirmCurrentSelection()

        {

            PlaySfx(lockInClip);



            if (currentGameMode == GameManager.GameMode.LanPvP)

            {

                bool isHost = GameManager.Instance == null || GameManager.Instance.IsLanHost;

                if (isHost)

                {

                    isLanHostLocked = true;

                    if (LanNetworkManager.Instance != null)

                    {

                        LanNetworkManager.Instance.SendCharSelectSync(new CharSelectSyncPacket

                        {

                            PlayerIndex = 1,

                            CharacterId = fighterCards[p1SelectedIndex].characterId,

                            IsLockedIn = true

                        });

                    }



                    if (isLanClientLocked)

                    {

                        if (LanNetworkManager.Instance != null)

                        {

                            LanNetworkManager.Instance.SendMatchStart(new MatchStartPacket

                            {

                                P1CharacterId = fighterCards[p1SelectedIndex].characterId,

                                P2CharacterId = fighterCards[p2SelectedIndex].characterId

                            });

                        }

                        currentPhase = SelectionPhase.ReadyLaunch;

                        StartCoroutine(LaunchMatchSequence());

                    }

                    else

                    {

                        if (selectionPromptText != null)

                        {

                            selectionPromptText.text = "<color=#ffd700>P1 LOCKED IN! WAITING FOR OPPONENT TO LOCK IN...</color>";

                        }

                    }

                }

                else // Client

                {

                    isLanClientLocked = true;

                    if (LanNetworkManager.Instance != null)

                    {

                        LanNetworkManager.Instance.SendCharSelectSync(new CharSelectSyncPacket

                        {

                            PlayerIndex = 2,

                            CharacterId = fighterCards[p2SelectedIndex].characterId,

                            IsLockedIn = true

                        });

                    }



                    if (selectionPromptText != null)

                    {

                        selectionPromptText.text = "<color=#00f5ff>P2 LOCKED IN! WAITING FOR HOST TO START MATCH...</color>";

                    }

                }

                return;

            }



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



        #endregion



        #region LAN Network Callbacks



        private void HandleNetworkCharSelectSync(CharSelectSyncPacket pkt)

        {

            int foundIdx = fighterCards.FindIndex(c => c.characterId.Equals(pkt.CharacterId, System.StringComparison.OrdinalIgnoreCase));

            if (foundIdx < 0) foundIdx = 0;



            if (pkt.PlayerIndex == 1)

            {

                p1SelectedIndex = foundIdx;

                isLanHostLocked = pkt.IsLockedIn;

            }

            else if (pkt.PlayerIndex == 2)

            {

                p2SelectedIndex = foundIdx;

                isLanClientLocked = pkt.IsLockedIn;

            }



            UpdateUI();



            bool isHost = GameManager.Instance == null || GameManager.Instance.IsLanHost;

            if (isHost && isLanHostLocked && isLanClientLocked && !isTransitioning)

            {

                if (LanNetworkManager.Instance != null)

                {

                    LanNetworkManager.Instance.SendMatchStart(new MatchStartPacket

                    {

                        P1CharacterId = fighterCards[p1SelectedIndex].characterId,

                        P2CharacterId = fighterCards[p2SelectedIndex].characterId

                    });

                }

                currentPhase = SelectionPhase.ReadyLaunch;

                StartCoroutine(LaunchMatchSequence());

            }

        }



        private void HandleNetworkMatchStart(MatchStartPacket pkt)

        {

            bool isHost = GameManager.Instance == null || GameManager.Instance.IsLanHost;

            if (!isHost)

            {

                // On Client: Local Player is always Player 1 (Left), Remote Host is Player 2 (Right)!

                int clientCharIdx = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P2CharacterId, System.StringComparison.OrdinalIgnoreCase));

                int hostCharIdx = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P1CharacterId, System.StringComparison.OrdinalIgnoreCase));

                p1SelectedIndex = clientCharIdx >= 0 ? clientCharIdx : 0;

                p2SelectedIndex = hostCharIdx >= 0 ? hostCharIdx : 0;

            }

            else

            {

                int idx1 = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P1CharacterId, System.StringComparison.OrdinalIgnoreCase));

                int idx2 = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P2CharacterId, System.StringComparison.OrdinalIgnoreCase));

                if (idx1 >= 0) p1SelectedIndex = idx1;

                if (idx2 >= 0) p2SelectedIndex = idx2;

            }



            currentPhase = SelectionPhase.ReadyLaunch;

            StartCoroutine(LaunchMatchSequence());

        }



        #endregion



        #region Random & Difficulty Selection



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

            AdjustCarouselWindow(index, 0);



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

                        // Strict [ PLAYER 2 ] and [ CPU: DIFFICULTY ] bracket formatting with Alternate Palette detection

                        if (isP1 && isP2 && currentPhase != SelectionPhase.SelectingP1)

                        {

                            string diffBadge = (GameManager.Instance != null) ? $"[ CPU: {GameManager.Instance.CurrentCpuDifficulty.ToString().ToUpper()} (ALT) ]" : "[ CPU: ALT PALETTE ]";

                            card.p2IndicatorText.text = (currentGameMode == GameManager.GameMode.VsCpu) ? diffBadge : "[ P2: ALT PALETTE ]";

                        }

                        else

                        {

                            string diffBadge = (GameManager.Instance != null) ? $"[ CPU: {GameManager.Instance.CurrentCpuDifficulty.ToString().ToUpper()} ]" : "[ CPU ]";

                            card.p2IndicatorText.text = (currentGameMode == GameManager.GameMode.VsCpu) ? diffBadge : "[ PLAYER 2 ]";

                        }

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



                #region Sliding 2-Card Carousel Mechanics

        /// <summary>
        /// Guarantees Nezuko's FighterCard exists in the selection roster at runtime,
        /// even if the scene was baked with only Goku and Saitama.
        /// </summary>
        public void EnsureNezukoCardLoaded()
        {
            if (fighterCards == null) fighterCards = new List<FighterCard>();

            bool hasNezuko = fighterCards.Exists(c => c != null && c.characterId.Equals("nezuko", System.StringComparison.OrdinalIgnoreCase));
            if (!hasNezuko && fighterCards.Count >= 2)
            {
                FighterCard template = fighterCards[1];
                if (template != null && template.cardRoot != null)
                {
                    GameObject clonedObj = Instantiate(template.cardRoot.gameObject, template.cardRoot.parent);
                    clonedObj.name = "Card_NEZUKO";
                    RectTransform clonedRt = clonedObj.GetComponent<RectTransform>();
                    clonedRt.anchoredPosition = new Vector2(offscreenX, 15f);

                    FighterCard nezukoCard = new FighterCard();
                    nezukoCard.characterId = "nezuko";
                    nezukoCard.displayName = "NEZUKO";
                    nezukoCard.epithet = "DEMON OF KINDNESS";
                    nezukoCard.cardRoot = clonedRt;

                    nezukoCard.outlineImage = clonedObj.GetComponent<Image>();

                    Transform portraitFrame = clonedObj.transform.Find("Portrait_Frame");
                    if (portraitFrame != null)
                    {
                        nezukoCard.portraitBackdrop = portraitFrame.GetComponent<Image>();
                        Transform avatarImg = portraitFrame.Find("Avatar_Sprite"); if (avatarImg == null) avatarImg = portraitFrame.Find("Avatar_Image");
                        if (avatarImg != null)
                        {
                            nezukoCard.portraitImage = avatarImg.GetComponent<Image>();
                            Sprite nezukoSp = Resources.Load<Sprite>("Sprites/UI/nezuko_avatar_heroic");
                            if (nezukoSp == null)
                            {
                                string path = System.IO.Path.Combine(Application.dataPath, "Sprites", "UI", "nezuko_avatar_heroic.png");
                                if (System.IO.File.Exists(path))
                                {
                                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                                    tex.filterMode = FilterMode.Bilinear;
                                    tex.LoadImage(bytes);
                                    nezukoSp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                                }
                            }
                            if (nezukoSp != null && nezukoCard.portraitImage != null)
                            {
                                nezukoCard.portraitImage.sprite = nezukoSp;
                            }
                        }
                    }

                    Transform b1 = clonedObj.transform.Find("Badge_P1");
                    if (b1 != null) nezukoCard.p1Indicator = b1.gameObject;

                    Transform b2 = clonedObj.transform.Find("Badge_P2");
                    if (b2 != null)
                    {
                        nezukoCard.p2Indicator = b2.gameObject;
                        Transform b2t = b2.Find("Text");
                        if (b2t != null) nezukoCard.p2IndicatorText = b2t.GetComponent<TextMeshProUGUI>();
                    }

                    Transform nameObj = clonedObj.transform.Find("Text_Name");
                    if (nameObj != null)
                    {
                        nezukoCard.nameText = nameObj.GetComponent<TextMeshProUGUI>();
                        nezukoCard.nameText.text = "NEZUKO";
                    }

                    Transform epiObj = clonedObj.transform.Find("Text_Epithet");
                    if (epiObj != null)
                    {
                        nezukoCard.epithetText = epiObj.GetComponent<TextMeshProUGUI>();
                        nezukoCard.epithetText.text = "DEMON OF KINDNESS";
                        nezukoCard.epithetText.color = new Color(1.0f, 0.45f, 0.65f, 1f);
                    }

                    Transform statsBox = clonedObj.transform.Find("Stats_Box");
                    if (statsBox != null)
                    {
                        Transform hp = statsBox.Find("Stat_HP");
                        if (hp != null) nezukoCard.hpValueText = hp.GetComponent<TextMeshProUGUI>();

                        Transform atk = statsBox.Find("Stat_Attack");
                        if (atk != null) nezukoCard.attackValueText = atk.GetComponent<TextMeshProUGUI>();

                        Transform skl = statsBox.Find("Stat_Skill");
                        if (skl != null) nezukoCard.skillValueText = skl.GetComponent<TextMeshProUGUI>();

                        Transform ult = statsBox.Find("Stat_Ultimate");
                        if (ult != null) nezukoCard.ultimateValueText = ult.GetComponent<TextMeshProUGUI>();
                    }

                    CharacterSelectButton btn = clonedObj.GetComponent<CharacterSelectButton>();
                    if (btn != null)
                    {
                        btn.Initialize(CharacterSelectButton.ButtonType.SelectCard, 1, this);
                    }

                    // Insert Nezuko between Goku and Saitama: [0: Goku, 1: Nezuko, 2: Saitama]
                    fighterCards.Insert(1, nezukoCard);

                    // Re-index Saitama's button to 2
                    if (fighterCards.Count > 2 && fighterCards[2].cardRoot != null)
                    {
                        CharacterSelectButton sBtn = fighterCards[2].cardRoot.GetComponent<CharacterSelectButton>();
                        if (sBtn != null) sBtn.Initialize(CharacterSelectButton.ButtonType.SelectCard, 2, this);
                    }

                    Debug.Log("<color=#00e676>[CharacterSelectController]</color> Successfully injected Nezuko into 3-fighter sliding carousel!");
                }
            }
        }
        /// <summary>
        /// Guarantees Tsunade's FighterCard exists in the selection roster at runtime,
        /// even if the scene was baked with only earlier characters.
        /// </summary>
        public void EnsureTsunadeCardLoaded()
        {
            if (fighterCards == null) fighterCards = new List<FighterCard>();

            bool hasTsunade = fighterCards.Exists(c => c != null && c.characterId.Equals("tsunade", System.StringComparison.OrdinalIgnoreCase));
            if (!hasTsunade && fighterCards.Count >= 2)
            {
                FighterCard template = fighterCards[fighterCards.Count - 1];
                if (template != null && template.cardRoot != null)
                {
                    GameObject clonedObj = Instantiate(template.cardRoot.gameObject, template.cardRoot.parent);
                    clonedObj.name = "Card_TSUNADE";
                    RectTransform clonedRt = clonedObj.GetComponent<RectTransform>();
                    clonedRt.anchoredPosition = new Vector2(offscreenX, 15f);

                    FighterCard tsunadeCard = new FighterCard();
                    tsunadeCard.characterId = "tsunade";
                    tsunadeCard.displayName = "TSUNADE";
                    tsunadeCard.epithet = "THE LEGENDARY SANNIN";
                    tsunadeCard.cardRoot = clonedRt;

                    tsunadeCard.outlineImage = clonedObj.GetComponent<Image>();

                    Transform portraitFrame = clonedObj.transform.Find("Portrait_Frame");
                    if (portraitFrame != null)
                    {
                        tsunadeCard.portraitBackdrop = portraitFrame.GetComponent<Image>();
                        Transform avatarImg = portraitFrame.Find("Avatar_Sprite"); if (avatarImg == null) avatarImg = portraitFrame.Find("Avatar_Image");
                        if (avatarImg != null)
                        {
                            tsunadeCard.portraitImage = avatarImg.GetComponent<Image>();
                            Sprite tsunadeSp = Resources.Load<Sprite>("Sprites/UI/tsunade_avatar_heroic");
                            if (tsunadeSp == null)
                            {
                                string p = System.IO.Path.Combine(Application.dataPath, "Sprites", "UI", "tsunade_avatar_heroic.png");
                                if (System.IO.File.Exists(p))
                                {
                                    byte[] bytes = System.IO.File.ReadAllBytes(p);
                                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                                    tex.filterMode = FilterMode.Bilinear;
                                    tex.LoadImage(bytes);
                                    tsunadeSp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                                }
                            }
                            if (tsunadeSp != null && tsunadeCard.portraitImage != null)
                            {
                                tsunadeCard.portraitImage.sprite = tsunadeSp;
                            }
                        }
                    }

                    Transform b1 = clonedObj.transform.Find("Badge_P1");
                    if (b1 != null) tsunadeCard.p1Indicator = b1.gameObject;

                    Transform b2 = clonedObj.transform.Find("Badge_P2");
                    if (b2 != null)
                    {
                        tsunadeCard.p2Indicator = b2.gameObject;
                        Transform b2t = b2.Find("Text");
                        if (b2t != null) tsunadeCard.p2IndicatorText = b2t.GetComponent<TextMeshProUGUI>();
                    }

                    Transform nameObj = clonedObj.transform.Find("Text_Name");
                    if (nameObj != null)
                    {
                        tsunadeCard.nameText = nameObj.GetComponent<TextMeshProUGUI>();
                        tsunadeCard.nameText.text = "TSUNADE";
                    }

                    Transform epiObj = clonedObj.transform.Find("Text_Epithet");
                    if (epiObj != null)
                    {
                        tsunadeCard.epithetText = epiObj.GetComponent<TextMeshProUGUI>();
                        tsunadeCard.epithetText.text = "THE LEGENDARY SANNIN";
                        tsunadeCard.epithetText.color = new Color(0.10f, 0.95f, 0.45f, 1f);
                    }

                    Transform statsBox = clonedObj.transform.Find("Stats_Box");
                    if (statsBox != null)
                    {
                        Transform hp = statsBox.Find("Stat_HP");
                        if (hp != null) tsunadeCard.hpValueText = hp.GetComponent<TextMeshProUGUI>();

                        Transform atk = statsBox.Find("Stat_Attack");
                        if (atk != null) tsunadeCard.attackValueText = atk.GetComponent<TextMeshProUGUI>();

                        Transform skl = statsBox.Find("Stat_Skill");
                        if (skl != null) tsunadeCard.skillValueText = skl.GetComponent<TextMeshProUGUI>();

                        Transform ult = statsBox.Find("Stat_Ultimate");
                        if (ult != null) tsunadeCard.ultimateValueText = ult.GetComponent<TextMeshProUGUI>();
                    }

                    // Insert Tsunade before Saitama if Saitama is present, else add
                    int saitamaIdx = fighterCards.FindIndex(c => c != null && c.characterId.Equals("saitama", System.StringComparison.OrdinalIgnoreCase));
                    if (saitamaIdx >= 0)
                    {
                        fighterCards.Insert(saitamaIdx, tsunadeCard);
                    }
                    else
                    {
                        fighterCards.Add(tsunadeCard);
                    }

                    // Re-index all card buttons to match their list indices
                    for (int i = 0; i < fighterCards.Count; i++)
                    {
                        if (fighterCards[i] != null && fighterCards[i].cardRoot != null)
                        {
                            CharacterSelectButton btn = fighterCards[i].cardRoot.GetComponent<CharacterSelectButton>();
                            if (btn != null) btn.Initialize(CharacterSelectButton.ButtonType.SelectCard, i, this);
                        }
                    }

                    Debug.Log("<color=#00e676>[CharacterSelectController]</color> Successfully injected Tsunade into sliding carousel!");
                }
            }
        }


        /// <summary>
        /// Adjusts the 2-card visible carousel window when moving left or right,
        /// sliding characters into and out of the viewport.
        /// </summary>
        private void AdjustCarouselWindow(int selectedIdx, int direction)
        {
            if (fighterCards == null || fighterCards.Count <= 2)
            {
                carouselWindowStart = 0;
                return;
            }

            int count = fighterCards.Count;
            int leftSlotIdx = carouselWindowStart;
            int rightSlotIdx = (carouselWindowStart + 1) % count;

            if (direction > 0)
            {
                // Moving right: if selected card is not in the visible window, advance window!
                if (selectedIdx != leftSlotIdx && selectedIdx != rightSlotIdx)
                {
                    carouselWindowStart = (selectedIdx - 1 + count) % count;

                    // Teleport incoming right card offscreen right so it slides in cleanly from the right edge
                    var incoming = fighterCards[selectedIdx];
                    if (incoming != null && incoming.cardRoot != null)
                    {
                        if (incoming.cardRoot.anchoredPosition.x < 0f)
                        {
                            incoming.cardRoot.anchoredPosition = new Vector2(offscreenX, 15f);
                        }
                        incoming.cardRoot.gameObject.SetActive(true);
                    }
                }
            }
            else if (direction < 0)
            {
                // Moving left: if selected card is not in the visible window, rewind window!
                if (selectedIdx != leftSlotIdx && selectedIdx != rightSlotIdx)
                {
                    carouselWindowStart = selectedIdx;

                    // Teleport incoming left card offscreen left so it slides in cleanly from the left edge
                    var incoming = fighterCards[selectedIdx];
                    if (incoming != null && incoming.cardRoot != null)
                    {
                        if (incoming.cardRoot.anchoredPosition.x > 0f)
                        {
                            incoming.cardRoot.anchoredPosition = new Vector2(-offscreenX, 15f);
                        }
                        incoming.cardRoot.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                // Direct selection (mouse click or random CPU)
                if (selectedIdx != leftSlotIdx && selectedIdx != rightSlotIdx)
                {
                    carouselWindowStart = selectedIdx;
                }
            }
        }

        /// <summary>
        /// Smoothly interpolates the positions of all cards in the carousel,
        /// keeping exactly 2 cards in the active viewport (leftSlotX and rightSlotX),
        /// while smoothly sliding other cards off-screen.
        /// </summary>
        private void UpdateCarouselPositions()
        {
            if (fighterCards == null || fighterCards.Count == 0) return;

            int count = fighterCards.Count;
            int leftSlotIdx = carouselWindowStart;
            int rightSlotIdx = (carouselWindowStart + 1) % count;

            for (int i = 0; i < count; i++)
            {
                var card = fighterCards[i];
                if (card == null || card.cardRoot == null) continue;

                Vector2 targetPos;
                bool inView = false;

                if (i == leftSlotIdx)
                {
                    targetPos = new Vector2(leftSlotX, 15f);
                    inView = true;
                }
                else if (i == rightSlotIdx)
                {
                    targetPos = new Vector2(rightSlotX, 15f);
                    inView = true;
                }
                else
                {
                    int forwardDist = (i - leftSlotIdx + count) % count;
                    float tx = (forwardDist > 1) ? offscreenX : -offscreenX;
                    targetPos = new Vector2(tx, 15f);
                    inView = false;
                }

                card.cardRoot.anchoredPosition = Vector2.Lerp(
                    card.cardRoot.anchoredPosition,
                    targetPos,
                    Time.deltaTime * carouselSlideSpeed
                );

                if (Vector2.Distance(card.cardRoot.anchoredPosition, targetPos) < 1f)
                {
                    card.cardRoot.anchoredPosition = targetPos;
                }

                bool isSliding = Vector2.Distance(card.cardRoot.anchoredPosition, targetPos) > 3f;
                card.cardRoot.gameObject.SetActive(inView || isSliding);
            }
        }

        /// <summary>
        /// Instantly snaps all cards into their initial carousel slot positions with zero lerp lag.
        /// </summary>
        private void SnapCarouselPositions()
        {
            if (fighterCards == null || fighterCards.Count == 0) return;

            int count = fighterCards.Count;
            int leftSlotIdx = carouselWindowStart;
            int rightSlotIdx = (carouselWindowStart + 1) % count;

            for (int i = 0; i < count; i++)
            {
                var card = fighterCards[i];
                if (card == null || card.cardRoot == null) continue;

                if (i == leftSlotIdx)
                {
                    card.cardRoot.anchoredPosition = new Vector2(leftSlotX, 15f);
                    card.cardRoot.gameObject.SetActive(true);
                }
                else if (i == rightSlotIdx)
                {
                    card.cardRoot.anchoredPosition = new Vector2(rightSlotX, 15f);
                    card.cardRoot.gameObject.SetActive(true);
                }
                else
                {
                    int forwardDist = (i - leftSlotIdx + count) % count;
                    float tx = (forwardDist > 1) ? offscreenX : -offscreenX;
                    card.cardRoot.anchoredPosition = new Vector2(tx, 15f);
                    card.cardRoot.gameObject.SetActive(false);
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

                    card.epithet = "DRAGON BALL Z  ★  SUPER SAIYAN";

                    if (card.epithetText != null)

                    {

                        card.epithetText.text = card.epithet;

                        card.epithetText.color = new Color(1.0f, 0.90f, 0.12f, 1f); // Super Saiyan Radiant Gold

                    }

                }

                else if (card.characterId.ToLower() == "saitama")

                {

                    card.epithet = "ONE PUNCH MAN  ★  HERO FOR FUN";

                    if (card.epithetText != null)

                    {

                        card.epithetText.text = card.epithet;

                        card.epithetText.color = new Color(1.0f, 0.30f, 0.30f, 1f); // Serious Punch Crimson

                    }

                }
                else if (card.characterId.ToLower() == "nezuko")
                {
                    card.epithet = "DEMON SLAYER  ★  DEMON OF KINDNESS";
                    if (card.epithetText != null)
                    {
                        card.epithetText.text = card.epithet;
                        card.epithetText.color = new Color(1.0f, 0.45f, 0.65f, 1f); // Demon Blossom Pink
                    }
                }
                else if (card.characterId.ToLower() == "tsunade")
                {
                    card.epithet = "NARUTO SHIPPUDEN  ★  THE LEGENDARY SANNIN";
                    if (card.epithetText != null)
                    {
                        card.epithetText.text = card.epithet;
                        card.epithetText.color = new Color(0.10f, 0.95f, 0.45f, 1f); // Medical Ninjutsu Emerald
                    }
                }

                else if (card.epithetText != null)

                {

                    card.epithetText.text = card.epithet;

                }



                if (card.hpValueText != null)
                {
                    if (card.characterId.ToLower() == "nezuko")
                    {
                        card.hpValueText.text = $"HP: {data.maxHealth} (Takes 1.5x Dmg in Chibi)  |  PASSIVE: +1 Ki/s";
                    }
                    else
                    {
                        card.hpValueText.text = $"HP: {data.maxHealth}     SPD: {data.moveSpeed:F1} ({(data.moveSpeed >= 5.0f ? "FAST" : "BALANCED")})";
                    }
                }



                if (card.speedValueText != null)

                {

                    card.speedValueText.text = $"MOVE SPEED: {data.moveSpeed:F1}";

                }



                if (card.attackValueText != null)
                {
                    card.attackValueText.enableAutoSizing = true;
                    card.attackValueText.fontSizeMin = 13f;
                    card.attackValueText.fontSizeMax = 18f;
                    int lDmg = data.lightAttack != null ? data.lightAttack.damage : 8;
                    int hDmg = data.heavyAttack != null ? data.heavyAttack.damage : 15;
                    if (card.characterId.ToLower() == "tsunade")
                    {
                        card.attackValueText.text = $"ATTACK: Light {lDmg} (Head Flick)  |  Heavy {hDmg} (Foot Stomp, Grounded Only)";
                    }
                    else if (card.characterId.ToLower() == "nezuko")
                    {
                        card.attackValueText.text = $"ATTACK: Light 5 / Heavy 10 (Demon Form)  |  0 Dmg Interrupts (Chibi Form)";
                    }
                    else if (card.characterId.ToLower() == "goku")
                    {
                        card.attackValueText.text = $"ATTACK: Light {lDmg} (Dragon Jab)  |  Heavy {hDmg} (Saiyan Kick)";
                    }
                    else if (card.characterId.ToLower() == "saitama")
                    {
                        card.attackValueText.text = $"ATTACK: Light {lDmg} (Normal Punch)  |  Heavy {hDmg} (Serious Stomp)";
                    }
                    else
                    {
                        card.attackValueText.text = $"ATTACK: Light {lDmg}  |  Heavy {hDmg}";
                    }
                }



                if (card.skillValueText != null && data.skill != null)

                {

                    card.skillValueText.enableAutoSizing = true;

                    card.skillValueText.fontSizeMin = 13f;

                    card.skillValueText.fontSizeMax = 18f;



                    if (data.characterId == "goku" || data.skill.name.ToLower().Contains("saiyan"))

                    {

                        card.skillValueText.text = $"SKILL: Super Saiyan (Toggle) [-2.5 Ki/s] (+25% Spd, +Atk Buffs)";

                    }
                    else if (data.characterId == "nezuko")
                    {
                        card.skillValueText.text = $"SKILL: Demon Form (Toggle) [-2 Ki/s] (Full Attack, Crouch & Ult)";
                    }
                    else if (data.characterId == "tsunade")
                    {
                        card.skillValueText.text = $"SKILL: Earth Shaker [30 Ki / 20s CD] (30 Dmg, Grounded Only)";
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
                    card.ultimateValueText.enableAutoSizing = true;
                    card.ultimateValueText.fontSizeMin = 12f;
                    card.ultimateValueText.fontSizeMax = 18f;

                    if (card.characterId.ToLower() == "tsunade")
                    {
                        card.ultimateValueText.text = $"ULTIMATE: Summoning Katsuyu [60 Ki / 20s CD] (30 Dmg Slug + 10s Healing: +2HP/s, +2Ki/s, Half Damage)";
                    }
                    else if (card.characterId.ToLower() == "nezuko")
                    {
                        card.ultimateValueText.text = $"ULTIMATE: Blood Demon Art [60 Ki / 10s CD] (30 Dmg Sure-Hit Fire, Burns + 20 Ki Back)";
                    }
                    else
                    {
                        card.ultimateValueText.text = $"ULTIMATE: {data.ultimate.name}  [Ki: {data.ultimate.kiCost:F0}]";
                    }

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



                // Set P2 details (Applying Alternate Palette if Mirror Match)

                if (versusP2AvatarImage != null && p2Card.portraitImage != null)

                {

                    versusP2AvatarImage.sprite = p2Card.portraitImage.sprite;

                    if (p1Id == p2Id)

                    {

                        versusP2AvatarImage.color = Fighter.FighterController.GetAlternatePaletteColor(p2Id);

                    }

                    else

                    {

                        versusP2AvatarImage.color = Color.white;

                    }

                }

                if (versusP2NameText != null)

                {

                    string prefix = (currentGameMode == GameManager.GameMode.VsCpu) ? "CPU " : "";

                    string altTag = (p1Id == p2Id) ? " [ALT]" : "";

                    versusP2NameText.text = prefix + p2Card.displayName.ToUpper() + altTag;

                }



                if (versusP2BadgeText != null)

                {

                    string diffBadge = (GameManager.Instance != null) ? $"[ CPU: {GameManager.Instance.CurrentCpuDifficulty.ToString().ToUpper()} ]" : "[ CPU ]";

                    string badgeText = (currentGameMode == GameManager.GameMode.VsCpu) ? diffBadge : "[ PLAYER 2 ]";

                    if (p1Id == p2Id)

                    {

                        badgeText = (currentGameMode == GameManager.GameMode.VsCpu) ? $"[ CPU: {GameManager.Instance.CurrentCpuDifficulty.ToString().ToUpper()} (ALT) ]" : "[ PLAYER 2 (ALT) ]";

                    }

                    versusP2BadgeText.text = badgeText;

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

