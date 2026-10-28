using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using MysticCombat.Core;
using MysticCombat.Systems;

namespace MysticCombat.UI
{
    /// <summary>
    /// Controls the In-Combat Pause Menu and the Post-Match Victory/Rematch Screen.
    /// Preserves complete game physics/animation state across pause and resume,
    /// provides full keyboard (WASD/Arrows/Enter/ESC) and mouse navigation,
    /// and ensures retro 90s anime arcade tournament visual styling.
    /// </summary>
    public class CombatMenuController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI Root Panels")]
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private GameObject resultsMenuRoot;

        [Header("Pause Menu Elements")]
        [SerializeField] private List<Button> pauseButtons = new List<Button>();
        [SerializeField] private List<TextMeshProUGUI> pauseButtonTexts = new List<TextMeshProUGUI>();

        [Header("Results Menu Elements")]
        [SerializeField] private TextMeshProUGUI resultsTitleText;
        [SerializeField] private TextMeshProUGUI resultsWinnerText;
        [SerializeField] private TextMeshProUGUI resultsEpithetText;
        [SerializeField] private Image resultsAvatarImage;
        [SerializeField] private TextMeshProUGUI resultsScoreText;
        [SerializeField] private List<Button> resultsButtons = new List<Button>();
        [SerializeField] private List<TextMeshProUGUI> resultsButtonTexts = new List<TextMeshProUGUI>();

        [Header("Audio")]
        [SerializeField] private AudioSource menuAudioSource;

        [Header("System References")]
        [SerializeField] private MatchManager matchManager;

        #endregion

        #region State

        private bool isPaused;
        private bool isMatchOver;
        private int selectedPauseIndex;
        private int selectedResultsIndex;

        private AudioClip navigateSfx;
        private AudioClip selectSfx;
        private AudioClip pauseSfx;
        private AudioClip resumeSfx;
        private AudioClip victorySfx;

        private readonly string[] pauseLabels = { "RESUME", "RESTART MATCH", "CHARACTER SELECT", "MAIN MENU" };
        private readonly string[] resultsLabels = { "REMATCH", "CHARACTER SELECT", "MAIN MENU" };

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            SetupAudio();
            EnsureUIHierarchyBuilt();
            SanitizeAndHealUIHierarchy();
        }

        private void Start()
        {
            if (matchManager == null)
            {
                matchManager = FindAnyObjectByType<MatchManager>();
            }

            if (matchManager != null)
            {
                matchManager.OnMatchOver -= HandleMatchOver;
                matchManager.OnMatchOver += HandleMatchOver;
            }

            // Ensure menus start hidden and world is unpaused
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
            if (resultsMenuRoot != null) resultsMenuRoot.SetActive(false);
            SanitizeAndHealUIHierarchy();
        }

        private void OnDestroy()
        {
            if (matchManager != null)
            {
                matchManager.OnMatchOver -= HandleMatchOver;
            }

            // Guarantee time scale is never stuck at 0 if destroyed
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // 1. In-Combat Pause Toggle (ESC or P)
            if (!isMatchOver)
            {
                if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
                {
                    if (isPaused)
                    {
                        ResumeGame();
                    }
                    else
                    {
                        PauseGame();
                    }
                    return;
                }
            }

            // 2. Menu Navigation when Modal is Open
            if (isPaused)
            {
                HandlePauseInput(kb);
            }
            else if (isMatchOver && resultsMenuRoot != null && resultsMenuRoot.activeSelf)
            {
                HandleResultsInput(kb);
            }
        }

        #endregion

        #region Pause & Resume Mechanics

        /// <summary>
        /// Freezes game physics and clocks while keeping UI navigation interactive.
        /// Pauses in-game combat audio while allowing UI sounds to play.
        /// </summary>
        public void PauseGame()
        {
            if (isMatchOver) return;

            isPaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            PlaySound(pauseSfx, 0.65f);

            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(true);
            }

            SanitizeAndHealUIHierarchy();
            selectedPauseIndex = 0;
            UpdatePauseSelectionVisuals();
            Debug.Log("<color=#00e676>[CombatMenuController]</color> Game PAUSED. Physics and clocks frozen.");
        }

        /// <summary>
        /// Unfreezes the game, restoring all active animations, physics velocities, and battle audio.
        /// </summary>
        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;

            PlaySound(resumeSfx, 0.6f);

            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(false);
            }

            Debug.Log("<color=#00e676>[CombatMenuController]</color> Game RESUMED. Combat physics and audio restored.");
        }

        private void HandlePauseInput(Keyboard kb)
        {
            int prev = selectedPauseIndex;

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                selectedPauseIndex = (selectedPauseIndex - 1 + pauseLabels.Length) % pauseLabels.Length;
            }
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            {
                selectedPauseIndex = (selectedPauseIndex + 1) % pauseLabels.Length;
            }

            if (selectedPauseIndex != prev)
            {
                PlaySound(navigateSfx, 0.22f);
                UpdatePauseSelectionVisuals();
            }

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                ExecutePauseSelection(selectedPauseIndex);
            }
        }

        private void UpdatePauseSelectionVisuals()
        {
            for (int i = 0; i < pauseButtons.Count && i < pauseButtonTexts.Count; i++)
            {
                bool isSel = (i == selectedPauseIndex);
                TextMeshProUGUI label = pauseButtonTexts[i];
                Image bg = pauseButtons[i].GetComponent<Image>();

                if (isSel)
                {
                    label.text = $">  {pauseLabels[i]}  <";
                    label.color = new Color(1f, 0.88f, 0.25f, 1f); // Solar Gold
                    if (bg != null) bg.color = new Color(0.18f, 0.32f, 0.52f, 0.95f); // Glowing Accent Slate
                    pauseButtons[i].transform.localScale = new Vector3(1.04f, 1.04f, 1f);
                }
                else
                {
                    label.text = pauseLabels[i];
                    label.color = new Color(0.85f, 0.92f, 1f, 0.85f); // Crisp Ice
                    if (bg != null) bg.color = new Color(0.10f, 0.14f, 0.22f, 0.85f); // Deep Arcade Navy
                    pauseButtons[i].transform.localScale = Vector3.one;
                }
            }
        }

        private void ExecutePauseSelection(int index)
        {
            PlaySound(selectSfx, 0.7f);

            switch (index)
            {
                case 0: // Resume
                    ResumeGame();
                    break;

                case 1: // Restart Match
                    RestartCurrentMatch();
                    break;

                case 2: // Character Select
                    NavigateToCharacterSelect();
                    break;

                case 3: // Main Menu
                    NavigateToMainMenu();
                    break;
            }
        }

        #endregion

        #region Results / Match Over Mechanics

        private void HandleMatchOver(int winnerPlayerIndex)
        {
            isMatchOver = true;
            StartCoroutine(ShowResultsSequence(winnerPlayerIndex));
        }

        private IEnumerator ShowResultsSequence(int winnerPlayerIndex)
        {
            // 3-Second Dramatic Pause after final K.O. to let victory sink in and animation conclude
            yield return new WaitForSecondsRealtime(3.0f);

            // Determine winner character ID
            string p1Char = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.P1SelectedCharacterId))
                ? GameManager.Instance.P1SelectedCharacterId.ToLower()
                : "goku";
            string p2Char = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.P2SelectedCharacterId))
                ? GameManager.Instance.P2SelectedCharacterId.ToLower()
                : "saitama";

            string winnerChar = winnerPlayerIndex == 1 ? p1Char : (winnerPlayerIndex == 2 ? p2Char : p1Char);
            string winnerName = winnerChar == "goku" ? "GOKU" : "SAITAMA";
            bool isVsCpu = GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameManager.GameMode.VsCpu;

            if (winnerPlayerIndex == 0)
            {
                if (resultsWinnerText != null)
                {
                    resultsWinnerText.text = "DRAW MATCH!";
                    resultsWinnerText.color = new Color(1f, 0.85f, 0.2f, 1f); // Solar Gold
                }
                if (resultsEpithetText != null)
                {
                    resultsEpithetText.text = "HONORABLE DRAW | UNSTOPPABLE FORCE MET IMMOVABLE OBJECT";
                }
            }
            else
            {
                string titlePrefix = (winnerPlayerIndex == 2 && isVsCpu) ? "CPU " : $"PLAYER {winnerPlayerIndex}: ";
                if (resultsWinnerText != null)
                {
                    resultsWinnerText.text = $"{titlePrefix}{winnerName} WINS!";
                    resultsWinnerText.color = winnerChar == "goku"
                        ? new Color(0f, 0.96f, 1f, 1f) // Electric Cyan
                        : new Color(1f, 0.88f, 0.25f, 1f); // Solar Gold
                }

                if (resultsEpithetText != null)
                {
                    resultsEpithetText.text = winnerChar == "goku"
                        ? "HOPE OF THE UNIVERSE | TOURNAMENT VICTOR"
                        : "HERO FOR FUN | ONE PUNCH CHAMPION";
                }
            }

            if (resultsScoreText != null && matchManager != null)
            {
                resultsScoreText.text = $"FINAL SCORE:  {matchManager.P1RoundWins} - {matchManager.P2RoundWins}";
            }

            // Load Heroic Avatar Portrait
            if (resultsAvatarImage != null)
            {
                string avatarPath = Path.Combine(Application.dataPath, "Sprites", "UI", $"{winnerChar}_avatar_heroic.png");
                if (File.Exists(avatarPath))
                {
                    byte[] bytes = File.ReadAllBytes(avatarPath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.filterMode = FilterMode.Bilinear;
                    if (tex.LoadImage(bytes))
                    {
                        resultsAvatarImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
            }

            // Activate Results Root and smoothly slide the card down from above
            if (resultsMenuRoot != null)
            {
                resultsMenuRoot.SetActive(true);
                RectTransform modalBox = resultsMenuRoot.transform.Find("Results_ModalBox") as RectTransform;
                if (modalBox != null)
                {
                    StartCoroutine(SlideInResultsCard(modalBox, 0.65f));
                }
            }

            SanitizeAndHealUIHierarchy();
            PlaySound(victorySfx, 0.75f);

            selectedResultsIndex = 0;
            UpdateResultsSelectionVisuals();
            Debug.Log($"<color=#00e676>[CombatMenuController]</color> Displaying Results Screen: {winnerName} won tournament! Slid down to center.");
        }

        private IEnumerator SlideInResultsCard(RectTransform boxRt, float duration)
        {
            if (boxRt == null) yield break;

            float elapsed = 0f;
            Vector2 startPos = new Vector2(0f, 950f);
            Vector2 targetPos = Vector2.zero;
            boxRt.anchoredPosition = startPos;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Cubic ease-out: 1 - (1 - t)^3
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                boxRt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                yield return null;
            }

            boxRt.anchoredPosition = targetPos;
        }

        private void HandleResultsInput(Keyboard kb)
        {
            int prev = selectedResultsIndex;

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                selectedResultsIndex = (selectedResultsIndex - 1 + resultsLabels.Length) % resultsLabels.Length;
            }
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            {
                selectedResultsIndex = (selectedResultsIndex + 1) % resultsLabels.Length;
            }

            if (selectedResultsIndex != prev)
            {
                PlaySound(navigateSfx, 0.22f);
                UpdateResultsSelectionVisuals();
            }

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                ExecuteResultsSelection(selectedResultsIndex);
            }
            else if (kb.escapeKey.wasPressedThisFrame)
            {
                ExecuteResultsSelection(2); // Quick Escape to Main Menu
            }
        }

        private void UpdateResultsSelectionVisuals()
        {
            for (int i = 0; i < resultsButtons.Count && i < resultsButtonTexts.Count; i++)
            {
                bool isSel = (i == selectedResultsIndex);
                TextMeshProUGUI label = resultsButtonTexts[i];
                Image bg = resultsButtons[i].GetComponent<Image>();

                if (isSel)
                {
                    label.text = $">  {resultsLabels[i]}  <";
                    if (i == 0)
                    {
                        label.color = new Color(1f, 0.88f, 0.25f, 1f); // Solar Gold
                        if (bg != null) bg.color = new Color(0.35f, 0.28f, 0.05f, 0.95f); // Gold Glow
                    }
                    else if (i == 1)
                    {
                        label.color = new Color(0f, 0.96f, 1f, 1f); // Electric Cyan
                        if (bg != null) bg.color = new Color(0.06f, 0.28f, 0.42f, 0.95f); // Cyan Glow
                    }
                    else
                    {
                        label.color = new Color(1f, 0.42f, 0.42f, 1f); // Ruby Crimson
                        if (bg != null) bg.color = new Color(0.38f, 0.10f, 0.14f, 0.95f); // Crimson Glow
                    }
                    resultsButtons[i].transform.localScale = new Vector3(1.04f, 1.04f, 1f);
                }
                else
                {
                    label.text = resultsLabels[i];
                    label.color = new Color(0.85f, 0.92f, 1f, 0.85f);
                    if (bg != null) bg.color = new Color(0.10f, 0.14f, 0.22f, 0.88f);
                    resultsButtons[i].transform.localScale = Vector3.one;
                }
            }
        }

        private void ExecuteResultsSelection(int index)
        {
            PlaySound(selectSfx, 0.7f);

            switch (index)
            {
                case 0: // Rematch
                    RestartCurrentMatch();
                    break;

                case 1: // Character Select
                    NavigateToCharacterSelect();
                    break;

                case 2: // Main Menu
                    NavigateToMainMenu();
                    break;
            }
        }

        #endregion

        #region Navigation Actions

        public void RestartCurrentMatch()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
            if (resultsMenuRoot != null) resultsMenuRoot.SetActive(false);

            isPaused = false;
            isMatchOver = false;

            if (matchManager != null)
            {
                matchManager.RestartMatch();
            }
            else
            {
                // Fallback scene reload
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            Debug.Log("<color=#00e676>[CombatMenuController]</color> Match RESTARTED fresh.");
        }

        public void NavigateToCharacterSelect()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.CharacterSelect);
            }

            SceneManager.LoadScene("MainMenuScene");
        }

        public void NavigateToMainMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.MainMenu);
            }

            SceneManager.LoadScene("MainMenuScene");
        }

        #endregion

        #region UI Auto-Building (Self-Sustaining Runtime Fallback)

        /// <summary>
        /// Ensures that Pause Menu and Results Menu hierarchies exist under HUD_Canvas.
        /// If not already serialized from editor baking, builds them dynamically.
        /// </summary>
        public void EnsureUIHierarchyBuilt()
        {
            TMP_FontAsset arcadeFont = LoadFont("ArcadeClassic_SDF");
            TMP_FontAsset retroFont = LoadFont("RetroGaming_SDF");

            // 1. Pause Menu Root
            if (pauseMenuRoot == null)
            {
                Transform existing = transform.Find("PauseMenu_Root");
                if (existing != null)
                {
                    pauseMenuRoot = existing.gameObject;
                }
                else
                {
                    pauseMenuRoot = BuildPauseMenuHierarchy(arcadeFont, retroFont);
                }
            }

            // Auto-wire Pause Menu elements if unassigned
            if (pauseButtons.Count == 0 || pauseButtonTexts.Count == 0)
            {
                pauseButtons.Clear();
                pauseButtonTexts.Clear();
                if (pauseMenuRoot != null)
                {
                    string[] btnNames = { "Btn_Pause_Resume", "Btn_Pause_Restart", "Btn_Pause_CharSelect", "Btn_Pause_MainMenu" };
                    foreach (string bName in btnNames)
                    {
                        Transform t = pauseMenuRoot.transform.Find($"Pause_ModalBox/{bName}");
                        if (t != null)
                        {
                            Button btn = t.GetComponent<Button>();
                            if (btn != null) pauseButtons.Add(btn);
                            TextMeshProUGUI txt = t.GetComponentInChildren<TextMeshProUGUI>(true);
                            if (txt != null) pauseButtonTexts.Add(txt);
                        }
                    }
                }
            }

            // 2. Results Menu Root
            if (resultsMenuRoot == null)
            {
                Transform existing = transform.Find("ResultsMenu_Root");
                if (existing != null)
                {
                    resultsMenuRoot = existing.gameObject;
                }
                else
                {
                    resultsMenuRoot = BuildResultsMenuHierarchy(arcadeFont, retroFont);
                }
            }

            // Auto-wire Results Menu elements if unassigned
            if (resultsButtons.Count == 0 || resultsButtonTexts.Count == 0)
            {
                resultsButtons.Clear();
                resultsButtonTexts.Clear();
                if (resultsMenuRoot != null)
                {
                    Transform box = resultsMenuRoot.transform.Find("Results_ModalBox");
                    if (box != null)
                    {
                        if (resultsTitleText == null) resultsTitleText = box.Find("Results_Title")?.GetComponent<TextMeshProUGUI>();
                        if (resultsWinnerText == null) resultsWinnerText = box.Find("Results_Winner")?.GetComponent<TextMeshProUGUI>();
                        if (resultsEpithetText == null) resultsEpithetText = box.Find("Results_Epithet")?.GetComponent<TextMeshProUGUI>();
                        if (resultsScoreText == null) resultsScoreText = box.Find("Results_Score")?.GetComponent<TextMeshProUGUI>();
                        if (resultsAvatarImage == null) resultsAvatarImage = box.Find("Results_AvatarCard/Avatar_Image")?.GetComponent<Image>();

                        string[] rBtnNames = { "Btn_Results_Rematch", "Btn_Results_CharSelect", "Btn_Results_MainMenu" };
                        foreach (string bName in rBtnNames)
                        {
                            Transform t = box.Find(bName);
                            if (t != null)
                            {
                                Button btn = t.GetComponent<Button>();
                                if (btn != null) resultsButtons.Add(btn);
                                TextMeshProUGUI txt = t.GetComponentInChildren<TextMeshProUGUI>(true);
                                if (txt != null) resultsButtonTexts.Add(txt);
                            }
                        }
                    }
                }
            }

            // Bind Button Click Events
            BindPauseButtonEvents();
            BindResultsButtonEvents();
            SanitizeAndHealUIHierarchy();
        }

        private TMP_FontAsset LoadFont(string fontName)
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>($"Fonts/{fontName}");
            if (font == null)
            {
                foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                {
                    if (f.name == fontName)
                    {
                        font = f;
                        break;
                    }
                }
            }

            TMP_FontAsset defaultFallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;

            if (font != null)
            {
                EnsureFontIntegrity(font, defaultFallback);
            }
            else
            {
                font = defaultFallback;
            }

            return font;
        }

        /// <summary>
        /// Scans all TextMeshProUGUI components in the pause and results menus,
        /// verifies font material texture bindings, replaces missing glyphs,
        /// sanitizes Unicode characters to standard ASCII, and ensures no blocks can ever render.
        /// </summary>
        public void SanitizeAndHealUIHierarchy()
        {
            TMP_FontAsset defaultFallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFallback == null)
            {
                defaultFallback = TMP_Settings.defaultFontAsset;
            }

            List<GameObject> roots = new List<GameObject>();
            if (pauseMenuRoot != null) roots.Add(pauseMenuRoot);
            if (resultsMenuRoot != null) roots.Add(resultsMenuRoot);

            foreach (var root in roots)
            {
                TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var label in labels)
                {
                    if (label == null) continue;

                    // 1. Sanitize text string characters
                    if (!string.IsNullOrEmpty(label.text))
                    {
                        string sanitized = label.text
                            .Replace('>', '>')
                            .Replace('<', '<')
                            .Replace('*', '*')
                            .Replace('-', '-')
                            .Replace('|', '|');

                        if (sanitized != label.text)
                        {
                            label.text = sanitized;
                        }
                    }

                    // 2. Ensure font asset integrity
                    TMP_FontAsset font = label.font;
                    if (font == null)
                    {
                        if (defaultFallback != null) label.font = defaultFallback;
                        continue;
                    }

                    EnsureFontIntegrity(font, defaultFallback);

                    // 3. Check Material texture connection
                    Material mat = label.fontSharedMaterial;
                    if (mat != null)
                    {
                        Texture mainTex = mat.GetTexture("_MainTex");
                        if (mainTex == null && font.atlasTexture != null)
                        {
                            mat.SetTexture("_MainTex", font.atlasTexture);
                            label.fontSharedMaterial = mat;
                        }
                    }

                    label.SetAllDirty();
                    label.ForceMeshUpdate();
                }
            }
        }

        private void EnsureFontIntegrity(TMP_FontAsset font, TMP_FontAsset defaultFallback)
        {
            if (font == null) return;

            // Reconnect _MainTex on material if null
            if (font.material != null)
            {
                Texture currentTex = font.material.GetTexture("_MainTex");
                if (currentTex == null && font.atlasTexture != null)
                {
                    font.material.SetTexture("_MainTex", font.atlasTexture);
                }
            }

            // Ensure fallback font table has a robust full-character fallback
            if (defaultFallback != null && font != defaultFallback)
            {
                if (font.fallbackFontAssetTable == null)
                {
                    font.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }
                if (!font.fallbackFontAssetTable.Contains(defaultFallback))
                {
                    font.fallbackFontAssetTable.Add(defaultFallback);
                }
            }
        }

        private GameObject BuildPauseMenuHierarchy(TMP_FontAsset titleFont, TMP_FontAsset bodyFont)
        {
            GameObject root = new GameObject("PauseMenu_Root");
            root.transform.SetParent(transform, false);

            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            // Semi-transparent cosmic scrim
            Image scrim = root.AddComponent<Image>();
            scrim.color = new Color(0.03f, 0.03f, 0.07f, 0.82f);

            // Centered Modal Window
            GameObject box = new GameObject("Pause_ModalBox");
            box.transform.SetParent(root.transform, false);
            RectTransform boxRt = box.AddComponent<RectTransform>();
            boxRt.sizeDelta = new Vector2(500f, 440f);
            Image boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.08f, 0.11f, 0.18f, 0.96f);

            // Neon Cyan Header Glow
            GameObject header = new GameObject("Header_Glow");
            header.transform.SetParent(box.transform, false);
            RectTransform headerRt = header.AddComponent<RectTransform>();
            headerRt.anchoredPosition = new Vector2(0f, 195f);
            headerRt.sizeDelta = new Vector2(460f, 4f);
            Image headerImg = header.AddComponent<Image>();
            headerImg.color = new Color(0f, 0.96f, 1f, 1f);

            // Title
            GameObject titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(box.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0f, 150f);
            titleRt.sizeDelta = new Vector2(460f, 55f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "PAUSED";
            if (titleFont != null) titleTmp.font = titleFont;
            titleTmp.fontSize = 42f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.88f, 0.25f, 1f); // Solar Gold

            // Subtitle
            GameObject subObj = new GameObject("Subtitle_Text");
            subObj.transform.SetParent(box.transform, false);
            RectTransform subRt = subObj.AddComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0f, 115f);
            subRt.sizeDelta = new Vector2(460f, 25f);
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "- COMBAT FROZEN -";
            if (bodyFont != null) subTmp.font = bodyFont;
            subTmp.fontSize = 13f;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0f, 0.96f, 1f, 0.85f);

            // Buttons Container
            pauseButtons.Clear();
            pauseButtonTexts.Clear();
            float startY = 55f;
            float stepY = 54f;

            for (int i = 0; i < pauseLabels.Length; i++)
            {
                int index = i;
                GameObject btnObj = new GameObject($"Btn_Pause_{i}");
                btnObj.transform.SetParent(box.transform, false);
                RectTransform btnRt = btnObj.AddComponent<RectTransform>();
                btnRt.anchoredPosition = new Vector2(0f, startY - (i * stepY));
                btnRt.sizeDelta = new Vector2(380f, 44f);

                Image btnBg = btnObj.AddComponent<Image>();
                btnBg.color = new Color(0.12f, 0.16f, 0.25f, 0.85f);

                Button btn = btnObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
                btn.colors = cb;

                // Add Hover Trigger
                EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener((_) => {
                    selectedPauseIndex = index;
                    PlaySound(navigateSfx, 0.22f);
                    UpdatePauseSelectionVisuals();
                });
                trigger.triggers.Add(entry);

                // Label
                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(btnObj.transform, false);
                RectTransform lblRt = lblObj.AddComponent<RectTransform>();
                lblRt.sizeDelta = btnRt.sizeDelta;
                TextMeshProUGUI lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
                lblTmp.text = pauseLabels[i];
                if (bodyFont != null) lblTmp.font = bodyFont;
                lblTmp.fontSize = 18f;
                lblTmp.alignment = TextAlignmentOptions.Center;
                lblTmp.color = Color.white;

                pauseButtons.Add(btn);
                pauseButtonTexts.Add(lblTmp);
            }

            // Footer Hints
            GameObject footerObj = new GameObject("Footer_Hints");
            footerObj.transform.SetParent(box.transform, false);
            RectTransform footerRt = footerObj.AddComponent<RectTransform>();
            footerRt.anchoredPosition = new Vector2(0f, -185f);
            footerRt.sizeDelta = new Vector2(460f, 30f);
            TextMeshProUGUI footerTmp = footerObj.AddComponent<TextMeshProUGUI>();
            footerTmp.text = "[W/S or ARROWS] Navigate   |   [ENTER] Select   |   [ESC] Resume";
            if (bodyFont != null) footerTmp.font = bodyFont;
            footerTmp.fontSize = 11f;
            footerTmp.alignment = TextAlignmentOptions.Center;
            footerTmp.color = new Color(0.6f, 0.65f, 0.75f, 0.85f);

            root.SetActive(false);
            return root;
        }

        private GameObject BuildResultsMenuHierarchy(TMP_FontAsset titleFont, TMP_FontAsset bodyFont)
        {
            GameObject root = new GameObject("ResultsMenu_Root");
            root.transform.SetParent(transform, false);

            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            // 100% Transparent Scrim: Arena, fighters, and lighting remain fully bright!
            Image scrim = root.AddComponent<Image>();
            scrim.color = new Color(0f, 0f, 0f, 0f);
            scrim.raycastTarget = true;

            // Centered Modal Window - Starts at Y = +950 for smooth sliding entrance animation
            GameObject box = new GameObject("Results_ModalBox");
            box.transform.SetParent(root.transform, false);
            RectTransform boxRt = box.AddComponent<RectTransform>();
            boxRt.sizeDelta = new Vector2(620f, 600f);
            boxRt.anchoredPosition = new Vector2(0f, 950f); // Off-screen top
            Image boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.06f, 0.08f, 0.15f, 0.98f); // Royal Obsidian Navy

            // Ornate Outer Gold Border Top
            GameObject borderTop = new GameObject("Border_Top");
            borderTop.transform.SetParent(box.transform, false);
            RectTransform btRt = borderTop.AddComponent<RectTransform>();
            btRt.anchoredPosition = new Vector2(0f, 290f);
            btRt.sizeDelta = new Vector2(590f, 5f);
            Image btImg = borderTop.AddComponent<Image>();
            btImg.color = new Color(1f, 0.85f, 0.2f, 1f); // Solar Gold

            // Ornate Cyan Trim Top Line
            GameObject trimTop = new GameObject("Trim_Top");
            trimTop.transform.SetParent(box.transform, false);
            RectTransform ttRt = trimTop.AddComponent<RectTransform>();
            ttRt.anchoredPosition = new Vector2(0f, 283f);
            ttRt.sizeDelta = new Vector2(560f, 2f);
            Image ttImg = trimTop.AddComponent<Image>();
            ttImg.color = new Color(0f, 0.96f, 1f, 0.9f); // Electric Cyan

            // Ornate Outer Gold Border Bottom
            GameObject borderBottom = new GameObject("Border_Bottom");
            borderBottom.transform.SetParent(box.transform, false);
            RectTransform bbRt = borderBottom.AddComponent<RectTransform>();
            bbRt.anchoredPosition = new Vector2(0f, -290f);
            bbRt.sizeDelta = new Vector2(590f, 5f);
            Image bbImg = borderBottom.AddComponent<Image>();
            bbImg.color = new Color(1f, 0.85f, 0.2f, 1f);

            // Micro Header Tournament Tag
            GameObject microObj = new GameObject("Micro_Header");
            microObj.transform.SetParent(box.transform, false);
            RectTransform microRt = microObj.AddComponent<RectTransform>();
            microRt.anchoredPosition = new Vector2(0f, 260f);
            microRt.sizeDelta = new Vector2(580f, 20f);
            TextMeshProUGUI microTmp = microObj.AddComponent<TextMeshProUGUI>();
            microTmp.text = "* ANIMEVERSE MARTIAL ARTS TOURNAMENT *";
            if (bodyFont != null) microTmp.font = bodyFont;
            microTmp.fontSize = 11.5f;
            microTmp.alignment = TextAlignmentOptions.Center;
            microTmp.color = new Color(0f, 0.96f, 1f, 0.95f); // Electric Cyan

            // Champion Banner Title
            GameObject titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(box.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0f, 225f);
            titleRt.sizeDelta = new Vector2(580f, 48f);
            resultsTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            resultsTitleText.text = "* WORLD CHAMPION *";
            if (titleFont != null) resultsTitleText.font = titleFont;
            resultsTitleText.fontSize = 34f;
            resultsTitleText.alignment = TextAlignmentOptions.Center;
            resultsTitleText.color = new Color(1f, 0.88f, 0.25f, 1f); // Radiant Sun Gold

            // Winner Heroic Avatar Medal Box
            GameObject avatarBox = new GameObject("Avatar_Frame");
            avatarBox.transform.SetParent(box.transform, false);
            RectTransform abRt = avatarBox.AddComponent<RectTransform>();
            abRt.anchoredPosition = new Vector2(0f, 130f);
            abRt.sizeDelta = new Vector2(114f, 114f);
            Image abImg = avatarBox.AddComponent<Image>();
            abImg.color = new Color(1f, 0.85f, 0.2f, 1f); // Solid gold bevel border

            GameObject avatarInner = new GameObject("Avatar_InnerGlow");
            avatarInner.transform.SetParent(avatarBox.transform, false);
            RectTransform aiInnerRt = avatarInner.AddComponent<RectTransform>();
            aiInnerRt.sizeDelta = new Vector2(108f, 108f);
            Image aiInnerImg = avatarInner.AddComponent<Image>();
            aiInnerImg.color = new Color(0.12f, 0.18f, 0.32f, 1f); // Rich navy backing

            GameObject avatarImgObj = new GameObject("Avatar_Image");
            avatarImgObj.transform.SetParent(avatarInner.transform, false);
            RectTransform aiRt = avatarImgObj.AddComponent<RectTransform>();
            aiRt.sizeDelta = new Vector2(104f, 104f);
            resultsAvatarImage = avatarImgObj.AddComponent<Image>();
            resultsAvatarImage.color = Color.white;

            // Winner Name Text
            GameObject winnerObj = new GameObject("Winner_Text");
            winnerObj.transform.SetParent(box.transform, false);
            RectTransform winnerRt = winnerObj.AddComponent<RectTransform>();
            winnerRt.anchoredPosition = new Vector2(0f, 52f);
            winnerRt.sizeDelta = new Vector2(580f, 40f);
            resultsWinnerText = winnerObj.AddComponent<TextMeshProUGUI>();
            resultsWinnerText.text = "GOKU WINS!";
            if (titleFont != null) resultsWinnerText.font = titleFont;
            resultsWinnerText.fontSize = 30f;
            resultsWinnerText.alignment = TextAlignmentOptions.Center;
            resultsWinnerText.color = new Color(0f, 0.96f, 1f, 1f);

            // Epithet Text
            GameObject epithetObj = new GameObject("Epithet_Text");
            epithetObj.transform.SetParent(box.transform, false);
            RectTransform epRt = epithetObj.AddComponent<RectTransform>();
            epRt.anchoredPosition = new Vector2(0f, 22f);
            epRt.sizeDelta = new Vector2(580f, 24f);
            resultsEpithetText = epithetObj.AddComponent<TextMeshProUGUI>();
            resultsEpithetText.text = "HOPE OF THE UNIVERSE | TOURNAMENT VICTOR";
            if (bodyFont != null) resultsEpithetText.font = bodyFont;
            resultsEpithetText.fontSize = 13.5f;
            resultsEpithetText.alignment = TextAlignmentOptions.Center;
            resultsEpithetText.color = new Color(1f, 0.85f, 0.3f, 0.95f);

            // Score Badge Plaque
            GameObject scorePlaque = new GameObject("Score_Plaque");
            scorePlaque.transform.SetParent(box.transform, false);
            RectTransform spRt = scorePlaque.AddComponent<RectTransform>();
            spRt.anchoredPosition = new Vector2(0f, -15f);
            spRt.sizeDelta = new Vector2(360f, 32f);
            Image spImg = scorePlaque.AddComponent<Image>();
            spImg.color = new Color(0.10f, 0.16f, 0.28f, 0.9f);

            GameObject scoreObj = new GameObject("Score_Text");
            scoreObj.transform.SetParent(scorePlaque.transform, false);
            RectTransform scRt = scoreObj.AddComponent<RectTransform>();
            scRt.sizeDelta = spRt.sizeDelta;
            resultsScoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            resultsScoreText.text = "FINAL SCORE:  2 - 1";
            if (bodyFont != null) resultsScoreText.font = bodyFont;
            resultsScoreText.fontSize = 15f;
            resultsScoreText.alignment = TextAlignmentOptions.Center;
            resultsScoreText.color = new Color(0.85f, 0.95f, 1f, 1f);

            // Buttons Container
            resultsButtons.Clear();
            resultsButtonTexts.Clear();
            float startY = -72f;
            float stepY = 56f;

            for (int i = 0; i < resultsLabels.Length; i++)
            {
                int index = i;
                GameObject btnObj = new GameObject($"Btn_Results_{i}");
                btnObj.transform.SetParent(box.transform, false);
                RectTransform btnRt = btnObj.AddComponent<RectTransform>();
                btnRt.anchoredPosition = new Vector2(0f, startY - (i * stepY));
                btnRt.sizeDelta = new Vector2(420f, 48f);

                Image btnBg = btnObj.AddComponent<Image>();
                btnBg.color = new Color(0.10f, 0.15f, 0.25f, 0.9f);

                Button btn = btnObj.AddComponent<Button>();
                EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener((_) => {
                    selectedResultsIndex = index;
                    PlaySound(navigateSfx, 0.22f);
                    UpdateResultsSelectionVisuals();
                });
                trigger.triggers.Add(entry);

                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(btnObj.transform, false);
                RectTransform lblRt = lblObj.AddComponent<RectTransform>();
                lblRt.sizeDelta = btnRt.sizeDelta;
                TextMeshProUGUI lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
                lblTmp.text = resultsLabels[i];
                if (bodyFont != null) lblTmp.font = bodyFont;
                lblTmp.fontSize = 19f;
                lblTmp.alignment = TextAlignmentOptions.Center;
                lblTmp.color = Color.white;

                resultsButtons.Add(btn);
                resultsButtonTexts.Add(lblTmp);
            }

            // Footer Hints
            GameObject footerObj = new GameObject("Footer_Hints");
            footerObj.transform.SetParent(box.transform, false);
            RectTransform footerRt = footerObj.AddComponent<RectTransform>();
            footerRt.anchoredPosition = new Vector2(0f, -260f);
            footerRt.sizeDelta = new Vector2(580f, 30f);
            TextMeshProUGUI footerTmp = footerObj.AddComponent<TextMeshProUGUI>();
            footerTmp.text = "[W/S or ARROWS] Navigate   |   [ENTER] Confirm   |   [ESC] Main Menu";
            if (bodyFont != null) footerTmp.font = bodyFont;
            footerTmp.fontSize = 11.5f;
            footerTmp.alignment = TextAlignmentOptions.Center;
            footerTmp.color = new Color(0.65f, 0.72f, 0.85f, 0.85f);

            root.SetActive(false);
            return root;
        }

        private void BindPauseButtonEvents()
        {
            if (pauseButtons.Count >= 4)
            {
                pauseButtons[0].onClick.RemoveAllListeners();
                pauseButtons[0].onClick.AddListener(ResumeGame);

                pauseButtons[1].onClick.RemoveAllListeners();
                pauseButtons[1].onClick.AddListener(RestartCurrentMatch);

                pauseButtons[2].onClick.RemoveAllListeners();
                pauseButtons[2].onClick.AddListener(NavigateToCharacterSelect);

                pauseButtons[3].onClick.RemoveAllListeners();
                pauseButtons[3].onClick.AddListener(NavigateToMainMenu);
            }
        }

        private void BindResultsButtonEvents()
        {
            if (resultsButtons.Count >= 3)
            {
                resultsButtons[0].onClick.RemoveAllListeners();
                resultsButtons[0].onClick.AddListener(RestartCurrentMatch);

                resultsButtons[1].onClick.RemoveAllListeners();
                resultsButtons[1].onClick.AddListener(NavigateToCharacterSelect);

                resultsButtons[2].onClick.RemoveAllListeners();
                resultsButtons[2].onClick.AddListener(NavigateToMainMenu);
            }
        }

        #endregion

        #region Audio Setup

        private void SetupAudio()
        {
            if (menuAudioSource == null)
            {
                menuAudioSource = GetComponent<AudioSource>();
                if (menuAudioSource == null)
                {
                    menuAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            menuAudioSource.playOnAwake = false;
            menuAudioSource.spatialBlend = 0f;
            // Crucial: UI sounds must ignore listener pause so they play while Time.timeScale = 0!
            menuAudioSource.ignoreListenerPause = true;

            GenerateProceduralAudio();
        }

        private void GenerateProceduralAudio()
        {
            // 1. Navigation Blip (Square 580 Hz, 0.04s with smooth decay envelope)
            navigateSfx = CreateToneClip("MenuNav", 0.04f, (t) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 580f * t)) * (1f - (t / 0.04f)));

            // 2. Select Chime (Sine 880 -> 1200 Hz ascending sweep, 0.12s)
            selectSfx = CreateToneClip("MenuSelect", 0.12f, (t) => {
                float freq = Mathf.Lerp(880f, 1200f, t / 0.12f);
                return Mathf.Sin(2f * Mathf.PI * freq * t);
            });

            // 3. Pause Chime (Sine 880 -> 440 Hz descending, 0.15s)
            pauseSfx = CreateToneClip("MenuPause", 0.15f, (t) => {
                float freq = Mathf.Lerp(880f, 440f, t / 0.15f);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (t / 0.15f));
            });

            // 4. Resume Chime (Sine 440 -> 880 Hz ascending, 0.12s)
            resumeSfx = CreateToneClip("MenuResume", 0.12f, (t) => {
                float freq = Mathf.Lerp(440f, 880f, t / 0.12f);
                return Mathf.Sin(2f * Mathf.PI * freq * t);
            });

            // 5. Victory Fanfare (Arcade arpeggio, 0.45s)
            victorySfx = CreateToneClip("MenuVictory", 0.45f, (t) => {
                float freq = t < 0.12f ? 523.25f : (t < 0.24f ? 659.25f : 783.99f); // C5 -> E5 -> G5
                return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - (t / 0.45f) * 0.5f);
            });
        }

        private AudioClip CreateToneClip(string clipName, float duration, System.Func<float, float> waveform)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = waveform(t) * 0.6f;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (menuAudioSource != null && clip != null)
            {
                float sfxMaster = InputSettingsManager.SFXVolume;
                menuAudioSource.PlayOneShot(clip, volume * sfxMaster);
            }
        }

        #endregion
    }
}
