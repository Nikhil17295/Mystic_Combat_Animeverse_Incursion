using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysticCombat.Core;
using MysticCombat.Systems;

namespace MysticCombat.UI
{
    /// <summary>
    /// Controls the authentic Double Dragon II / 90s Arcade Brawler Main Menu:
    /// - Stage 1: Dramatic line-by-line Story Prologue ("IN THE YEAR 20XX...") with color/size animation
    /// - Stage 2: Arcade Title Screen with cycling neon gradient ("MYSTIC COMBAT")
    /// - Stage 3: Pure retro arcade menu options (No modern container boxes!)
    ///   - Blinking arcade cursor: ►
    ///   - Color-cycling active selection (Flashing Yellow & White)
    ///   - Classic blinking [ PUSH START BUTTON ] footer
    /// - Full Keyboard (W/S, Up/Down, Enter/Space), Gamepad, and Mouse support
    /// - Procedural 8-bit sound effects (square-wave navigation blip and punch strike)
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        #region Sub-classes

        [System.Serializable]
        public class MenuItem
        {
            public string title = "OPTION";
            public RectTransform itemRect;
            public TextMeshProUGUI labelText;
        }

        #endregion

        #region Serialized Fields

        [Header("Menu State")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject introRoot;
        [SerializeField] private bool playIntroOnStart = false; // Start directly at Title & Menu for instant arcade responsiveness

        [Header("Story Intro Elements (Double Dragon II Style)")]
        [SerializeField] private List<TextMeshProUGUI> introLines = new List<TextMeshProUGUI>();
        [SerializeField] private TextMeshProUGUI introSkipPrompt;
        [SerializeField] private float lineRevealDelay = 1.6f;

        [Header("Title Marquee")]
        [SerializeField] private RectTransform titleLogoRect;
        [SerializeField] private TextMeshProUGUI mainTitleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private RectTransform mysticMandala;
        [SerializeField] private float mandalaRotationSpeed = 18f;

        [Header("Arcade Menu Items (4 Options)")]
        [Tooltip("0: Vs CPU, 1: Local Duel, 2: Options, 3: Quit")]
        [SerializeField] private List<MenuItem> menuItems = new List<MenuItem>();
        [SerializeField] private TextMeshProUGUI pushStartPrompt;

        [Header("Colors & Arcade Palette")]
        [SerializeField] private Color normalTextColor = new Color(0.60f, 0.68f, 0.78f, 1f); // Metallic Arcade Silver
        [SerializeField] private Color selectedTextColorA = new Color(1.0f, 0.90f, 0.10f, 1f); // Sun Yellow
        [SerializeField] private Color selectedTextColorB = new Color(1.0f, 1.0f, 1.0f, 1f); // Pure White

        [Header("Audio & SFX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool enableProceduralAudio = true;
        [SerializeField] private bool enableButtonChangeAudio = false; // Muted: title music is enough
        [SerializeField] private bool enableEntranceImpactAudio = false; // Muted: clean start without click/twik
        [SerializeField] private bool enableMenuConfirmAudio = true; // Confirmation punch when launching match

        [Header("Music / BGM")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip titleMusicClip;
        [Tooltip("Title screen background music volume. Adjustable live in Inspector during Play mode.")]
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 1.00f;

        [Header("Options & Controls Modal")]
        [SerializeField] private GameObject optionsPanel;

        [Header("LAN Multiplayer Modal")]
        [SerializeField] private LanLobbyModal lanLobbyModal;

        [Header("Character Selection Screen")]
        [SerializeField] private GameObject characterSelectRoot;
        [SerializeField] private CharacterSelectController characterSelectController;

        [Header("Scene Loading")]
        [SerializeField] private string combatSceneName = "SampleScene";

        #endregion

        #region State

        private int currentIndex = 0;
        private float lastNavInputTime;
        private const float NAV_REPEAT_DELAY = 0.22f;

        private bool isIntroPlaying = false;
        private bool isEntranceAnimating = false;
        private Coroutine introCoroutine;

        // Procedural Audio Clips
        private AudioClip navigateSfx;
        private AudioClip selectSfx;

        // Authentic 90s Multi-Color Vertex Gradients
        // Inactive / Normal: Electric Cyan -> Diamond Silver -> Neon Violet -> Cobalt Indigo
        private readonly VertexGradient inactiveGradient = new VertexGradient(
            new Color(0.0f, 0.95f, 1.0f, 1f),   // Electric Cyan
            new Color(0.85f, 0.96f, 1.0f, 1f),  // Diamond Silver
            new Color(0.50f, 0.0f, 1.0f, 1f),   // Neon Violet
            new Color(0.25f, 0.10f, 0.85f, 1f)  // Deep Indigo
        );

        // Active / Selected: Radiant Gold -> Sunburst White -> Blazing Amber -> Fiery Crimson
        private readonly VertexGradient activeGradient = new VertexGradient(
            new Color(1.0f, 0.95f, 0.40f, 1f),  // Radiant Sun Gold
            new Color(1.0f, 1.0f, 1.0f, 1f),    // Sunburst White
            new Color(1.0f, 0.50f, 0.0f, 1f),   // Blazing Amber
            new Color(1.0f, 0.15f, 0.25f, 1f)   // Fiery Crimson
        );

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            // Ensure AudioListener exists in scene (Player Ears)
            if (Object.FindAnyObjectByType<AudioListener>() == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                }
                else
                {
                    gameObject.AddComponent<AudioListener>();
                }
                Debug.Log("<color=#00e676>[MainMenuController]</color> Attached AudioListener (Player Ears) to Scene.");
            }
            AudioListener.volume = 1.0f;
            AudioListener.pause = false;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.mute = false;

            // Setup Music Player
            InputSettingsManager.EnsureInitialized();
            musicVolume = InputSettingsManager.MusicVolume;

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;
            musicSource.mute = false;

            if (titleMusicClip != null)
            {
                musicSource.clip = titleMusicClip;
                musicSource.volume = musicVolume;
                musicSource.loop = true;
                musicSource.Play();
                Debug.Log($"<color=#00e676>[MainMenuController]</color> Playing Serialized Title Music: {titleMusicClip.name} ({titleMusicClip.length:F1}s)");
            }
            else
            {
                // Runtime disk fallback: auto-stream from Audio/Music so title music plays even before baking!
                StartCoroutine(LoadTitleMusicFromDisk());
            }

            GenerateProceduralAudio();
        }

        private void Start()
        {
            // If returning from combat directly to Character Select, bypass title screen
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.CharacterSelect)
            {
                SkipIntro();
                if (characterSelectController != null && characterSelectRoot != null)
                {
                    if (menuRoot != null) menuRoot.SetActive(false);
                    characterSelectRoot.SetActive(true);
                    characterSelectController.Open(GameManager.Instance.CurrentGameMode);
                    return;
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.MainMenu);
            }

            if (playIntroOnStart && introRoot != null && introLines.Count > 0)
            {
                StartIntroSequence();
            }
            else
            {
                SkipIntro();
                StartEntranceAnimation();
            }
        }

        private void Update()
        {
            if (isIntroPlaying)
            {
                HandleIntroInput();
                AnimateSkipPrompt();
                return;
            }

            AnimateTitleLogo();
            AnimateMandala();
            AnimateTitleColorCycling();
            AnimateActiveSelection();
            AnimatePushStartPrompt();
            HandleNavigationInput();

            // Enable live real-time slider adjustments in the Inspector during play
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.volume = musicVolume;
            }
        }

        private void OnDestroy()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        #endregion

        #region Story Intro Sequence

        public void StartIntroSequence()
        {
            isIntroPlaying = true;
            if (introRoot != null) introRoot.SetActive(true);
            if (menuRoot != null) menuRoot.SetActive(false);

            // Hide all lines initially
            foreach (var line in introLines)
            {
                if (line != null)
                {
                    line.gameObject.SetActive(false);
                    line.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            introCoroutine = StartCoroutine(StoryIntroRoutine());
        }

        private IEnumerator StoryIntroRoutine()
        {
            yield return new WaitForSeconds(0.4f);

            for (int i = 0; i < introLines.Count; i++)
            {
                var line = introLines[i];
                if (line != null)
                {
                    line.gameObject.SetActive(true);

                    // Dramatic typewriter / size pulse reveal
                    float elapsed = 0f;
                    float duration = 0.5f;
                    Vector3 baseScale = Vector3.one;

                    // Play subtle text click SFX
                    PlayNavigateAudio();

                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / duration);

                        // Pulse from 1.25x down to 1.0x with glowing color
                        float scale = Mathf.Lerp(1.22f, 1.0f, t);
                        line.transform.localScale = baseScale * scale;

                        // Shift color from vibrant cyan/gold to crisp white
                        Color pulseColor = Color.Lerp(new Color(0.0f, 0.95f, 0.85f, 1f), Color.white, t);
                        pulseColor.a = t;
                        line.color = pulseColor;

                        yield return null;
                    }

                    line.transform.localScale = baseScale;
                    line.color = Color.white;
                }

                yield return new WaitForSeconds(lineRevealDelay);
            }

            yield return new WaitForSeconds(1.2f);
            SkipIntro();
        }

        private void HandleIntroInput()
        {
            // Any key or mouse click instantly skips into the Title Marquee
            if (UnityEngine.Input.anyKeyDown || UnityEngine.Input.GetMouseButtonDown(0))
            {
                SkipIntro();
            }
        }

        public void SkipIntro()
        {
            if (introCoroutine != null)
            {
                StopCoroutine(introCoroutine);
                introCoroutine = null;
            }

            isIntroPlaying = false;
            if (introRoot != null) introRoot.SetActive(false);
            if (menuRoot != null) menuRoot.SetActive(true);

            if (enableEntranceImpactAudio)
            {
                PlaySelectAudio();
            }
            UpdateSelectionVisuals(playSfx: false);
            StartEntranceAnimation();
        }

        private void AnimateSkipPrompt()
        {
            if (introSkipPrompt != null)
            {
                // Classic arcade 2 Hz flashing prompt
                float alpha = Mathf.PingPong(Time.time * 2.5f, 1f) > 0.35f ? 1f : 0.15f;
                introSkipPrompt.color = new Color(0.7f, 0.8f, 1.0f, alpha);
            }
        }

        #endregion

        #region Navigation & Execution

        private void HandleNavigationInput()
        {
            if (characterSelectRoot != null && characterSelectRoot.activeSelf)
            {
                return;
            }

            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseOptions();
                }
                return;
            }

            if (lanLobbyModal != null && lanLobbyModal.gameObject.activeSelf)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    lanLobbyModal.Close();
                }
                return;
            }

            // Quick Hotkey [L] for instant LAN Duel Lobby
            if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                OpenLanLobby();
                return;
            }

            float vAxis = UnityEngine.Input.GetAxisRaw("Vertical");
            bool upPressed = UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.W) || (vAxis > 0.5f && Time.time - lastNavInputTime > NAV_REPEAT_DELAY);
            bool downPressed = UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.S) || (vAxis < -0.5f && Time.time - lastNavInputTime > NAV_REPEAT_DELAY);

            if (upPressed)
            {
                Navigate(-1);
                lastNavInputTime = Time.time;
            }
            else if (downPressed)
            {
                Navigate(1);
                lastNavInputTime = Time.time;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter) || UnityEngine.Input.GetKeyDown(KeyCode.F))
            {
                ExecuteSelection(currentIndex);
            }
        }

        public void Navigate(int direction)
        {
            if (menuItems == null || menuItems.Count == 0) return;

            currentIndex = (currentIndex + direction + menuItems.Count) % menuItems.Count;
            UpdateSelectionVisuals(playSfx: true);
        }

        public void OnPointerHoverItem(int index)
        {
            if (index >= 0 && index < menuItems.Count && index != currentIndex)
            {
                currentIndex = index;
                UpdateSelectionVisuals(playSfx: true);
            }
        }

        public void OnPointerClickItem(int index)
        {
            if (index >= 0 && index < menuItems.Count)
            {
                currentIndex = index;
                ExecuteSelection(index);
            }
        }

        private void ExecuteSelection(int index)
        {
            if (enableMenuConfirmAudio)
            {
                PlaySelectAudio();
            }

            if (menuItems.Count >= 5)
            {
                switch (index)
                {
                    case 0:
                        StartGameMode(GameManager.GameMode.VsCpu);
                        break;
                    case 1:
                        StartGameMode(GameManager.GameMode.LocalPvP);
                        break;
                    case 2:
                        OpenLanLobby();
                        break;
                    case 3:
                        OpenOptions();
                        break;
                    case 4:
                        QuitGame();
                        break;
                }
            }
            else
            {
                switch (index)
                {
                    case 0:
                        StartGameMode(GameManager.GameMode.VsCpu);
                        break;
                    case 1:
                        StartGameMode(GameManager.GameMode.LocalPvP);
                        break;
                    case 2:
                        OpenOptions();
                        break;
                    case 3:
                        QuitGame();
                        break;
                }
            }
        }

        /// <summary>
        /// Opens the LAN Lobby Modal to Host or Join local network battles.
        /// </summary>
        public void OpenLanLobby()
        {
            Debug.Log("<color=#00e676>[MainMenuController]</color> Opening LAN Multiplayer Lobby...");

            if (lanLobbyModal == null)
            {
                GameObject go = new GameObject("LanLobbyModal");
                go.transform.SetParent(transform, false);
                lanLobbyModal = go.AddComponent<LanLobbyModal>();
            }

            if (menuRoot != null) menuRoot.SetActive(false);
            lanLobbyModal.Open();
        }

        private void StartGameMode(GameManager.GameMode mode)
        {
            Debug.Log($"<color=#00e676>[MainMenuController]</color> Opening Character Select for: {mode}");

            // If Character Select UI exists, transition seamlessly without stopping background music!
            if (characterSelectController != null && characterSelectRoot != null)
            {
                if (menuRoot != null) menuRoot.SetActive(false);
                characterSelectRoot.SetActive(true);
                characterSelectController.Open(mode);
            }
            else
            {
                // Fallback direct launch if CharacterSelect is not baked yet
                if (musicSource != null)
                {
                    musicSource.Stop();
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetGameMode(mode);
                    GameManager.Instance.SetState(GameManager.GameState.InMatch);
                    GameManager.Instance.LoadScene(combatSceneName);
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(combatSceneName);
                }
            }
        }

        /// <summary>
        /// Called when the player cancels character selection and returns to Title.
        /// Retains playing background music without restarting or hitching.
        /// </summary>
        public void ReturnToMainMenuFromCharSelect()
        {
            if (characterSelectRoot != null) characterSelectRoot.SetActive(false);
            if (menuRoot != null) menuRoot.SetActive(true);
            UpdateSelectionVisuals(playSfx: false);
        }

        /// <summary>
        /// Stops title background music cleanly when the match transition commences.
        /// </summary>
        public void StopTitleMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void OpenOptions()
        {
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
            }
        }

        public void CloseOptions()
        {
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }
        }

        public void QuitGame()
        {
            Debug.Log("[MainMenuController] Quitting Mystic Combat.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Visual Presentation & Retro Arcade Animation

        private void StartEntranceAnimation()
        {
            if (isEntranceAnimating) return;
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ArcadeEntranceRoutine());
            }
        }

        private IEnumerator ArcadeEntranceRoutine()
        {
            isEntranceAnimating = true;

            // 1. Initial Impact Setup
            if (titleLogoRect != null)
            {
                titleLogoRect.localScale = new Vector3(1.35f, 1.35f, 1f);
            }

            // Offset items downward with zero alpha
            List<Vector2> basePositions = new List<Vector2>();
            for (int i = 0; i < menuItems.Count; i++)
            {
                if (menuItems[i].itemRect != null)
                {
                    basePositions.Add(menuItems[i].itemRect.anchoredPosition);
                    menuItems[i].itemRect.anchoredPosition = basePositions[i] - new Vector2(0f, 30f);
                    if (menuItems[i].labelText != null)
                    {
                        menuItems[i].labelText.alpha = 0f;
                    }
                }
            }

            if (pushStartPrompt != null)
            {
                pushStartPrompt.alpha = 0f;
            }

            // 2. Logo Punch Impact (Elastic slam)
            float elapsed = 0f;
            float punchDuration = 0.38f;
            while (elapsed < punchDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / punchDuration);

                // Quick drop to 0.94x then spring settle to 1.0x
                float scale;
                if (t < 0.7f)
                {
                    scale = Mathf.Lerp(1.35f, 0.94f, t / 0.7f);
                }
                else
                {
                    scale = Mathf.Lerp(0.94f, 1.0f, (t - 0.7f) / 0.3f);
                }

                if (titleLogoRect != null)
                {
                    titleLogoRect.localScale = new Vector3(scale, scale, 1f);
                }

                yield return null;
            }

            if (titleLogoRect != null)
            {
                titleLogoRect.localScale = Vector3.one;
            }

            if (enableEntranceImpactAudio)
            {
                PlaySelectAudio();
            }

            yield return new WaitForSeconds(0.06f);

            // 3. Staggered Cascade of Arcade Options
            for (int i = 0; i < menuItems.Count; i++)
            {
                var item = menuItems[i];
                if (item.itemRect != null && i < basePositions.Count)
                {
                    StartCoroutine(SlideItemIn(item.itemRect, basePositions[i], item.labelText, 0.18f));
                    if (enableButtonChangeAudio)
                    {
                        PlayNavigateAudio();
                    }
                }
                yield return new WaitForSeconds(0.06f);
            }

            yield return new WaitForSeconds(0.12f);

            if (pushStartPrompt != null)
            {
                pushStartPrompt.alpha = 1f;
            }

            isEntranceAnimating = false;
            UpdateSelectionVisuals(playSfx: false);
        }

        private IEnumerator SlideItemIn(RectTransform rt, Vector2 targetPos, TextMeshProUGUI tmp, float duration)
        {
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f); // Smooth ease out

                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                if (tmp != null)
                {
                    tmp.alpha = ease;
                }

                yield return null;
            }

            rt.anchoredPosition = targetPos;
            if (tmp != null) tmp.alpha = 1f;
        }

        private void UpdateSelectionVisuals(bool playSfx)
        {
            if (playSfx && enableButtonChangeAudio)
            {
                PlayNavigateAudio();
            }

            for (int i = 0; i < menuItems.Count; i++)
            {
                var item = menuItems[i];
                bool isSelected = (i == currentIndex);

                if (item.labelText != null)
                {
                    // Double Dragon Style: Classic flashing arrow cursor "►"
                    string prefix = isSelected ? "►  " : "    ";
                    item.labelText.text = $"{prefix}{item.title}";

                    // Apply multi-color vertex gradients (No monochromatic text!)
                    item.labelText.enableVertexGradient = true;
                    item.labelText.colorGradient = isSelected ? activeGradient : inactiveGradient;
                }

                if (item.itemRect != null)
                {
                    item.itemRect.localScale = isSelected ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
                }
            }
        }

        private void AnimateActiveSelection()
        {
            if (menuItems == null || currentIndex < 0 || currentIndex >= menuItems.Count) return;

            var item = menuItems[currentIndex];
            if (item.labelText != null)
            {
                // Dynamic shimmer cycling on active option: Radiant Gold/White to Solar Amber/Crimson
                float cycle = Mathf.PingPong(Time.time * 4.5f, 1f);
                Color tl = Color.Lerp(new Color(1.0f, 0.95f, 0.40f, 1f), new Color(1.0f, 1.0f, 1.0f, 1f), cycle);
                Color tr = Color.Lerp(new Color(1.0f, 1.0f, 1.0f, 1f), new Color(1.0f, 0.85f, 0.20f, 1f), cycle);
                Color bl = Color.Lerp(new Color(1.0f, 0.50f, 0.0f, 1f), new Color(1.0f, 0.20f, 0.10f, 1f), cycle);
                Color br = Color.Lerp(new Color(1.0f, 0.15f, 0.25f, 1f), new Color(1.0f, 0.60f, 0.0f, 1f), cycle);

                item.labelText.enableVertexGradient = true;
                item.labelText.colorGradient = new VertexGradient(tl, tr, bl, br);
            }

            if (item.itemRect != null)
            {
                float subtlePulse = 1.05f + 0.02f * Mathf.Sin(Time.time * 9f);
                item.itemRect.localScale = new Vector3(subtlePulse, subtlePulse, 1f);
            }
        }

        private void AnimateTitleColorCycling()
        {
            if (mainTitleText == null) return;

            // Authentic 90s arcade palette cycling: Crimson -> Amber -> Gold -> White -> Cyan
            float t = (Time.time * 1.5f) % 4f;
            Color cTopLeft, cTopRight, cBottomLeft, cBottomRight;

            if (t < 1f)
            {
                float k = t;
                cTopLeft = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(1f, 0.85f, 0.1f), k);
                cTopRight = Color.Lerp(new Color(1f, 0.6f, 0.1f), new Color(0.2f, 1f, 0.9f), k);
                cBottomLeft = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(1f, 0.4f, 0.0f), k);
                cBottomRight = Color.Lerp(new Color(1f, 0.3f, 0.0f), new Color(0.9f, 0.1f, 0.3f), k);
            }
            else if (t < 2f)
            {
                float k = t - 1f;
                cTopLeft = Color.Lerp(new Color(1f, 0.85f, 0.1f), new Color(0.2f, 1f, 0.9f), k);
                cTopRight = Color.Lerp(new Color(0.2f, 1f, 0.9f), Color.white, k);
                cBottomLeft = Color.Lerp(new Color(1f, 0.4f, 0.0f), new Color(0.8f, 0.1f, 0.4f), k);
                cBottomRight = Color.Lerp(new Color(0.9f, 0.1f, 0.3f), new Color(1f, 0.8f, 0.2f), k);
            }
            else if (t < 3f)
            {
                float k = t - 2f;
                cTopLeft = Color.Lerp(new Color(0.2f, 1f, 0.9f), Color.white, k);
                cTopRight = Color.Lerp(Color.white, new Color(1f, 0.2f, 0.2f), k);
                cBottomLeft = Color.Lerp(new Color(0.8f, 0.1f, 0.4f), new Color(1f, 0.4f, 0.0f), k);
                cBottomRight = Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.8f, 0.1f, 0.1f), k);
            }
            else
            {
                float k = t - 3f;
                cTopLeft = Color.Lerp(Color.white, new Color(1f, 0.2f, 0.2f), k);
                cTopRight = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(1f, 0.6f, 0.1f), k);
                cBottomLeft = Color.Lerp(new Color(1f, 0.4f, 0.0f), new Color(0.8f, 0.1f, 0.1f), k);
                cBottomRight = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(1f, 0.3f, 0.0f), k);
            }

            mainTitleText.enableVertexGradient = true;
            mainTitleText.colorGradient = new VertexGradient(cTopLeft, cTopRight, cBottomLeft, cBottomRight);

            // Subtle breathing scale on the title marquee
            float titleBounce = 1f + 0.015f * Mathf.Sin(Time.time * 5f);
            mainTitleText.transform.localScale = new Vector3(titleBounce, titleBounce, 1f);
        }

        private void AnimateTitleLogo()
        {
            if (isEntranceAnimating) return;

            if (titleLogoRect != null)
            {
                float breathe = 1f + 0.015f * Mathf.Sin(Time.time * 3f);
                titleLogoRect.localScale = new Vector3(breathe, breathe, 1f);
            }
        }

        private void AnimatePushStartPrompt()
        {
            if (isEntranceAnimating) return;

            if (pushStartPrompt != null)
            {
                // Authentic blinking at 2 Hz
                float blink = Mathf.PingPong(Time.time * 2.8f, 1f) > 0.4f ? 1f : 0f;
                pushStartPrompt.color = new Color(1f, 0.88f, 0.3f, blink);
            }
        }

        private void AnimateMandala()
        {
            if (mysticMandala != null)
            {
                mysticMandala.Rotate(0f, 0f, -mandalaRotationSpeed * Time.deltaTime);
            }
        }

        public void SetTitleMusic(AudioClip clip)
        {
            titleMusicClip = clip;
            if (musicSource != null && clip != null)
            {
                musicSource.clip = clip;
                musicSource.volume = musicVolume;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        private IEnumerator LoadTitleMusicFromDisk()
        {
            string musicFolder = System.IO.Path.Combine(Application.dataPath, "Audio/Music");
            if (!System.IO.Directory.Exists(musicFolder)) yield break;

            string[] files = System.IO.Directory.GetFiles(musicFolder);
            string targetPath = null;
            AudioType audioType = AudioType.MPEG;

            // Strictly filter for title theme track so battle music is never accidentally picked
            foreach (var file in files)
            {
                string fileName = System.IO.Path.GetFileName(file).ToLower();
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (fileName.Contains("title") && (ext == ".mp3" || ext == ".wav" || ext == ".ogg"))
                {
                    targetPath = file;
                    if (ext == ".mp3") audioType = AudioType.MPEG;
                    else if (ext == ".wav") audioType = AudioType.WAV;
                    else if (ext == ".ogg") audioType = AudioType.OGGVORBIS;
                    break;
                }
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                Debug.LogWarning("[MainMenuController] No audio tracks (.mp3, .wav, .ogg) found in Assets/Audio/Music.");
                yield break;
            }

            string url = new System.Uri(targetPath).AbsoluteUri;
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        clip.name = "DoubleDragonII_Title_Stream";
                        SetTitleMusic(clip);
                        Debug.Log($"<color=#00e676>[MainMenuController]</color> Title music stream loaded successfully from {targetPath}!");
                    }
                    else
                    {
                        Debug.LogWarning("[MainMenuController] Audio content decoded as null from " + url);
                    }
                }
                else
                {
                    Debug.LogWarning($"[MainMenuController] Failed to stream audio via UnityWebRequest: {www.error}");
                }
            }
        }

        #endregion

        #region Procedural Retro Audio Generation

        private void GenerateProceduralAudio()
        {
            if (!enableProceduralAudio) return;

            // Authentic 8-bit navigation blip (Square wave ramp)
            navigateSfx = CreateToneClip("Sfx_MenuNav", 0.05f, (t) =>
            {
                float freq = Mathf.Lerp(480f, 720f, t);
                float val = Mathf.Sin(2f * Mathf.PI * freq * t) > 0f ? 0.15f : -0.15f;
                return val * (1f - t);
            });

            // Double Dragon style punch confirmation chime
            selectSfx = CreateToneClip("Sfx_MenuSelect", 0.20f, (t) =>
            {
                float freq = Mathf.Lerp(320f, 960f, t);
                float square = Mathf.Sin(2f * Mathf.PI * freq * t) > 0f ? 0.22f : -0.22f;
                float bass = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.35f;
                return (square + bass) * (1f - Mathf.Pow(t, 0.6f));
            });
        }

        private AudioClip CreateToneClip(string clipName, float duration, System.Func<float, float> waveform)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                samples[i] = Mathf.Clamp(waveform(t), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlayNavigateAudio()
        {
            if (audioSource != null && navigateSfx != null)
            {
                audioSource.PlayOneShot(navigateSfx, 0.45f);
            }
        }

        private void PlaySelectAudio()
        {
            if (audioSource != null && selectSfx != null)
            {
                audioSource.PlayOneShot(selectSfx, 0.65f);
            }
        }

        #endregion
    }
}
