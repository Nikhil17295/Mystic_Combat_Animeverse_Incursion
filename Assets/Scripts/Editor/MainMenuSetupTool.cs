#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using MysticCombat.UI;
using MysticCombat.Core;
using MysticCombat.Systems;

namespace MysticCombat.Editor
{
    /// <summary>
    /// Bakes the authentic Double Dragon II / 90s Arcade Brawler Main Menu (MainMenuScene.unity):
    /// - Stage 1: Dramatic line-by-line Story Prologue ("IN THE YEAR 20XX...") with color/size animation
    /// - Stage 2: Color-cycling Title Screen ("MYSTIC COMBAT") with rotating Doctor Strange mandala
    /// - Stage 3: Pure retro arcade options (NO modern container boxes! Bold typography, flashing ► cursor)
    /// - Blinking [ PUSH START BUTTON ] footer
    /// - Robust font handling guaranteeing text is always 100% visible
    /// </summary>
    [InitializeOnLoad]
    public static class MainMenuSetupTool
    {
        private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenuScene.unity";
        private const string COMBAT_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const string MAIN_MENU_BAKE_KEY = "MysticCombat_MainMenu_Bake_v18";
        private const string PREF_PLAY_FROM_MAIN_MENU = "MysticCombat_PlayFromMainMenu";

        static MainMenuSetupTool()
        {
            EditorApplication.delayCall += () =>
            {
                // Enforce Build Settings (MainMenuScene = Index 0, SampleScene = Index 1)
                UpdateBuildSettings();

                // Enforce Play Mode Start Scene so pressing Play boots directly into MainMenu
                ApplyPlayModeStartScene();

                if (!File.Exists(MAIN_MENU_SCENE_PATH) || !EditorPrefs.GetBool(MAIN_MENU_BAKE_KEY, false))
                {
                    EditorPrefs.SetBool(MAIN_MENU_BAKE_KEY, true);
                    BakeMainMenuScene();
                }
            };
        }

        public static void ApplyPlayModeStartScene()
        {
            bool playFromMenu = EditorPrefs.GetBool(PREF_PLAY_FROM_MAIN_MENU, true);
            if (playFromMenu)
            {
                SceneAsset menuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MAIN_MENU_SCENE_PATH);
                if (menuScene != null)
                {
                    EditorSceneManager.playModeStartScene = menuScene;
                }
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }

        [MenuItem("Mystic Combat/Play Mode/Always Start From Main Menu", false, 100)]
        public static void SetPlayModeStartMainMenu()
        {
            EditorPrefs.SetBool(PREF_PLAY_FROM_MAIN_MENU, true);
            ApplyPlayModeStartScene();
            Debug.Log("<color=#00e676>[Mystic Combat]</color> Play Mode will ALWAYS start from MainMenuScene.unity (Primary Scene)!");
        }

        [MenuItem("Mystic Combat/Play Mode/Always Start From Main Menu", true)]
        public static bool ValidateSetPlayModeStartMainMenu()
        {
            Menu.SetChecked("Mystic Combat/Play Mode/Always Start From Main Menu", EditorPrefs.GetBool(PREF_PLAY_FROM_MAIN_MENU, true));
            return true;
        }

        [MenuItem("Mystic Combat/Play Mode/Start From Active Scene (Debug)", false, 101)]
        public static void SetPlayModeStartActiveScene()
        {
            EditorPrefs.SetBool(PREF_PLAY_FROM_MAIN_MENU, false);
            ApplyPlayModeStartScene();
            Debug.Log("<color=#ffeb3b>[Mystic Combat]</color> Play Mode will start from whichever scene is currently open in Hierarchy!");
        }

        [MenuItem("Mystic Combat/Play Mode/Start From Active Scene (Debug)", true)]
        public static bool ValidateSetPlayModeStartActiveScene()
        {
            Menu.SetChecked("Mystic Combat/Play Mode/Start From Active Scene (Debug)", !EditorPrefs.GetBool(PREF_PLAY_FROM_MAIN_MENU, true));
            return true;
        }

        [MenuItem("Mystic Combat/Bake Main Menu Scene (MainMenuScene.unity)")]
        public static void BakeMainMenuScene()
        {
            Debug.Log("<color=#00e676>[MainMenuSetupTool]</color> Baking Double Dragon II Arcade Main Menu Scene...");

            // Ensure AssetDatabase is synchronized with disk
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 1. Ensure texture import settings
            SetupMenuTextures();

            // 2. Load or Create Scene
            Scene scene;
            if (File.Exists(MAIN_MENU_SCENE_PATH))
            {
                scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Clean up any existing hierarchy on re-bake
            GameObject existingCanvas = GameObject.Find("MainMenu_Canvas");
            if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);
            GameObject existingCam = GameObject.Find("Main Camera");
            if (existingCam != null) Object.DestroyImmediate(existingCam);

            // 3. Setup Camera
            SetupCamera();

            // 4. Load Sprites & Reliable Fonts
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/MainMenu/menu_bg_multiverse.png");
            Sprite mandalaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/time_stone_mandala.png");
            Sprite titleLogoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/MainMenu/mystic_combat_title_logo.png");

            // Guaranteed reliable pre-baked font (Zero missing glyphs, 100% visible)
            TMP_FontAsset defaultTmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            TMP_FontAsset arcadeFontAsset = defaultTmpFont;
            TMP_FontAsset retroGamingFontAsset = defaultTmpFont;
            TMP_FontAsset pressStartFontAsset = defaultTmpFont;

            // 5. Setup Canvas
            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            MainMenuController menuController = canvasObj.AddComponent<MainMenuController>();

            // EventSystem
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // ----------------------------------------------------
            // 6. FULLSCREEN BACKGROUND (Cosmic Multiverse Dimension)
            // ----------------------------------------------------
            GameObject bgObj = CreateUIObject("Cosmic_Background", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = bgObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.color = new Color(0.72f, 0.72f, 0.85f, 1f); // Vibrant cosmic multiverse colors
            }
            else
            {
                bgImg.color = new Color(0.04f, 0.02f, 0.08f, 1f);
            }

            // Dark Multiverse Vignette (Ensures Logo and Arcade Text pop with crisp contrast while showing stars/void)
            GameObject overlayObj = CreateUIObject("Vignette_Overlay", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(0.02f, 0.01f, 0.06f, 0.45f);

            // ----------------------------------------------------
            // 7. MENU ROOT (Active during Title & Option Selection)
            // ----------------------------------------------------
            GameObject menuRoot = CreateUIObject("Menu_Root", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Double Dragon II Style Hero Title Logo Image (Transparent PNG, No Blue Box, No Green Mandala)
            GameObject logoObj = CreateUIObject("Title_Hero_Logo", menuRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(900f, 502f));
            RectTransform logoRt = logoObj.GetComponent<RectTransform>();
            Image logoImg = logoObj.AddComponent<Image>();
            if (titleLogoSprite != null)
            {
                logoImg.sprite = titleLogoSprite;
                logoImg.preserveAspect = true;
            }

            // ----------------------------------------------------
            // 8. TRUE RETRO ARCADE OPTIONS (Lowered with 124px clearance below Logo)
            // ----------------------------------------------------
            GameObject optionsGroupObj = CreateUIObject("Arcade_Options_Group", menuRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -225f), new Vector2(750f, 200f));

            string[] optionTitles = new string[]
            {
                "1 PLAYER  VS  CPU",
                "1 VS 1  LOCAL DUEL",
                "1 VS 1  LAN MULTIPLAYER",
                "OPTIONS & MOVESET",
                "QUIT GAME"
            };

            List<MainMenuController.MenuItem> items = new List<MainMenuController.MenuItem>();
            float lineSpacing = 42f;
            float startY = 60f;

            for (int i = 0; i < optionTitles.Length; i++)
            {
                float posY = startY - i * lineSpacing;

                GameObject itemObj = CreateUIObject($"Item_{i}", optionsGroupObj.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, posY), new Vector2(700f, 42f));

                RectTransform itemRt = itemObj.GetComponent<RectTransform>();

                TextMeshProUGUI itemTmp = itemObj.AddComponent<TextMeshProUGUI>();
                itemTmp.font = retroGamingFontAsset;
                itemTmp.fontSize = 28;
                itemTmp.fontStyle = FontStyles.Bold;
                itemTmp.alignment = TextAlignmentOptions.Center;
                itemTmp.characterSpacing = 3f;

                // Multi-color vertex gradients (No monochromatic text!)
                itemTmp.enableVertexGradient = true;
                if (i == 0)
                {
                    itemTmp.text = $"►  {optionTitles[i]}";
                    itemTmp.colorGradient = new VertexGradient(
                        new Color(1.0f, 0.95f, 0.40f, 1f),  // Radiant Sun Gold
                        new Color(1.0f, 1.0f, 1.0f, 1f),    // Sunburst White
                        new Color(1.0f, 0.50f, 0.0f, 1f),   // Blazing Amber
                        new Color(1.0f, 0.15f, 0.25f, 1f)   // Fiery Crimson
                    );
                }
                else
                {
                    itemTmp.text = $"    {optionTitles[i]}";
                    itemTmp.colorGradient = new VertexGradient(
                        new Color(0.0f, 0.95f, 1.0f, 1f),   // Electric Cyan
                        new Color(0.85f, 0.96f, 1.0f, 1f),  // Diamond Silver
                        new Color(0.50f, 0.0f, 1.0f, 1f),   // Neon Violet
                        new Color(0.25f, 0.10f, 0.85f, 1f)  // Deep Indigo
                    );
                }

                // Pointer Hook
                MainMenuButton pointerBtn = itemObj.AddComponent<MainMenuButton>();
                pointerBtn.Initialize(i, menuController);

                items.Add(new MainMenuController.MenuItem
                {
                    title = optionTitles[i],
                    itemRect = itemRt,
                    labelText = itemTmp
                });
            }

            // Blinking PUSH START BUTTON Prompt (Lowered)
            GameObject pushStartObj = CreateUIObject("Text_PushStart", menuRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -365f), new Vector2(700f, 36f));
            TextMeshProUGUI pushStartTmp = pushStartObj.AddComponent<TextMeshProUGUI>();
            pushStartTmp.font = pressStartFontAsset;
            pushStartTmp.text = "[ PUSH ENTER OR SPACE TO START ]";
            pushStartTmp.fontSize = 17;
            pushStartTmp.alignment = TextAlignmentOptions.Center;
            pushStartTmp.color = new Color(1.0f, 0.88f, 0.3f, 1f);

            // Copyright / Arcade Footer
            GameObject footerObj = CreateUIObject("Footer_Copyright", menuRoot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(1000f, 30f));
            TextMeshProUGUI footerTmp = footerObj.AddComponent<TextMeshProUGUI>();
            footerTmp.font = pressStartFontAsset;
            footerTmp.text = "© 1988-2026 MYSTIC COMBAT ARCADE CORP.   •   ALL RIGHTS RESERVED";
            footerTmp.fontSize = 13;
            footerTmp.alignment = TextAlignmentOptions.Center;
            footerTmp.color = new Color(0.70f, 0.45f, 0.90f, 0.85f); // Classic purple copyright tint

            // ----------------------------------------------------
            // 9. DOUBLE DRAGON II STORY INTRO SEQUENCE (Attract Overlay - Disabled by default)
            // ----------------------------------------------------
            GameObject introRoot = CreateUIObject("Story_Intro_Overlay", canvasObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image introDarkBg = introRoot.AddComponent<Image>();
            introDarkBg.color = new Color(0.02f, 0.01f, 0.05f, 0.96f);

            GameObject storyBox = CreateUIObject("Story_Text_Container", introRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1100f, 400f));

            string[] introSentences = new string[]
            {
                "IN THE YEAR 20XX...",
                "A CATASTROPHIC MULTIVERSE RIFT SHATTERED REALITY.",
                "WARRIORS FROM PARALLEL ANIME REALMS ARE DRAWN INTO BATTLE.",
                "ONLY THE STRONGEST CAN HOLD THE LINE!",
                "LET'S GO MYSTIC WARRIORS!"
            };

            List<TextMeshProUGUI> lineTmps = new List<TextMeshProUGUI>();
            float introLineSpacing = 58f;
            float introStartY = 120f;

            for (int s = 0; s < introSentences.Length; s++)
            {
                GameObject lineObj = CreateUIObject($"IntroLine_{s}", storyBox.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, introStartY - s * introLineSpacing), new Vector2(1050f, 48f));

                TextMeshProUGUI lineTmp = lineObj.AddComponent<TextMeshProUGUI>();
                lineTmp.font = retroGamingFontAsset;
                lineTmp.text = introSentences[s];
                lineTmp.fontSize = (s == 0 || s == 4) ? 30 : 22;
                lineTmp.alignment = TextAlignmentOptions.Center;
                lineTmp.fontStyle = FontStyles.Bold;
                lineTmp.characterSpacing = 3f;

                lineTmps.Add(lineTmp);
            }

            GameObject skipObj = CreateUIObject("Intro_SkipPrompt", introRoot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(800f, 36f));
            TextMeshProUGUI skipTmp = skipObj.AddComponent<TextMeshProUGUI>();
            skipTmp.font = pressStartFontAsset;
            skipTmp.text = "►  PRESS ANY KEY OR CLICK TO SKIP  ◄";
            skipTmp.fontSize = 15;
            skipTmp.alignment = TextAlignmentOptions.Center;
            skipTmp.color = new Color(0.7f, 0.8f, 1.0f, 1f);

            introRoot.SetActive(false); // Inactive so menu shows immediately!

            // ----------------------------------------------------
            // 10. OPTIONS & CONTROLS MODAL
            // ----------------------------------------------------
            GameObject optionsModal = CreateOptionsModal(canvasObj.transform, retroGamingFontAsset, pressStartFontAsset, menuController);
            optionsModal.SetActive(false);

            // ----------------------------------------------------
            // 11. CHARACTER SELECTION SCREEN (Arcade Roster)
            // ----------------------------------------------------
            GameObject charSelectMenu = CreateCharSelectMenu(canvasObj.transform, retroGamingFontAsset, retroGamingFontAsset, pressStartFontAsset, menuController);

            // ----------------------------------------------------
            // 12. WIRE PROPERTIES ON MAINMENUCONTROLLER
            // ----------------------------------------------------
            SerializedObject so = new SerializedObject(menuController);
            so.FindProperty("menuRoot").objectReferenceValue = menuRoot;
            so.FindProperty("introRoot").objectReferenceValue = introRoot;
            so.FindProperty("characterSelectRoot").objectReferenceValue = charSelectMenu;
            so.FindProperty("characterSelectController").objectReferenceValue = charSelectMenu.GetComponent<CharacterSelectController>();
            so.FindProperty("playIntroOnStart").boolValue = false; // Start directly at Title Screen

            SerializedProperty linesProp = so.FindProperty("introLines");
            linesProp.ClearArray();
            for (int l = 0; l < lineTmps.Count; l++)
            {
                linesProp.InsertArrayElementAtIndex(l);
                linesProp.GetArrayElementAtIndex(l).objectReferenceValue = lineTmps[l];
            }
            so.FindProperty("introSkipPrompt").objectReferenceValue = skipTmp;

            so.FindProperty("titleLogoRect").objectReferenceValue = logoRt;
            so.FindProperty("mainTitleText").objectReferenceValue = null;
            so.FindProperty("subtitleText").objectReferenceValue = null;
            so.FindProperty("mysticMandala").objectReferenceValue = null;
            so.FindProperty("pushStartPrompt").objectReferenceValue = pushStartTmp;

            // Explicitly link authentic Double Dragon II Title Screen BGM
            AudioClip titleMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/double_dragon_ii_title.mp3");
            if (titleMusic == null)
            {
                string musicDir = "Assets/Audio/Music";
                if (Directory.Exists(musicDir))
                {
                    string[] musicFiles = Directory.GetFiles(musicDir, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (string mf in musicFiles)
                    {
                        string fileName = Path.GetFileName(mf).ToLower();
                        string ext = Path.GetExtension(mf).ToLower();
                        if (fileName.Contains("title") && (ext == ".mp3" || ext == ".wav" || ext == ".ogg"))
                        {
                            string normPath = mf.Replace("\\", "/");
                            titleMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(normPath);
                            if (titleMusic != null) break;
                        }
                    }
                }
            }
            if (titleMusic != null)
            {
                so.FindProperty("titleMusicClip").objectReferenceValue = titleMusic;
                Debug.Log($"<color=#00e676>[MainMenuSetupTool]</color> Linked title music track: {titleMusic.name}");
            }
            so.FindProperty("musicVolume").floatValue = 0.70f;

            SerializedProperty itemsProp = so.FindProperty("menuItems");
            itemsProp.ClearArray();
            for (int i = 0; i < items.Count; i++)
            {
                itemsProp.InsertArrayElementAtIndex(i);
                SerializedProperty elem = itemsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("title").stringValue = items[i].title;
                elem.FindPropertyRelative("itemRect").objectReferenceValue = items[i].itemRect;
                elem.FindPropertyRelative("labelText").objectReferenceValue = items[i].labelText;
            }

            so.FindProperty("optionsPanel").objectReferenceValue = optionsModal;

            // 11. LAN LOBBY MODAL CREATION (Full Arcade Hierarchy)
            GameObject lanModalObj = CreateLanLobbyModal(
                canvasObj.transform,
                arcadeFontAsset,
                retroGamingFontAsset,
                pressStartFontAsset,
                menuController,
                charSelectMenu.GetComponent<CharacterSelectController>()
            );
            LanLobbyModal lanModal = lanModalObj.GetComponent<LanLobbyModal>();
            so.FindProperty("lanLobbyModal").objectReferenceValue = lanModal;
            so.FindProperty("combatSceneName").stringValue = "SampleScene";
            so.FindProperty("enableButtonChangeAudio").boolValue = false;
            so.FindProperty("enableEntranceImpactAudio").boolValue = false;
            so.ApplyModifiedProperties();

            // ----------------------------------------------------
            // 12. ENSURE GAMEMANAGER
            // ----------------------------------------------------
            if (Object.FindAnyObjectByType<GameManager>() == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // Save Scene & Build Settings
            EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE_PATH);
            UpdateBuildSettings();

            Debug.Log("<color=#00e676>[MainMenuSetupTool] SUCCESS!</color> Double Dragon II Retro Main Menu baked to " + MAIN_MENU_SCENE_PATH);
        }

        private static GameObject CreateCharSelectMenu(
            Transform canvasTransform,
            TMP_FontAsset titleFont,
            TMP_FontAsset retroFont,
            TMP_FontAsset pressStartFont,
            MainMenuController menuController)
        {
            GameObject root = CreateUIObject("CharacterSelect_Root", canvasTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CharacterSelectController charController = root.AddComponent<CharacterSelectController>();

            // Dark semi-transparent background overlay behind cards (so cosmic stars still shine through)
            GameObject backdrop = CreateUIObject("Backdrop_Dim", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image dimImg = backdrop.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.01f, 0.06f, 0.70f);

            // 1. Header Banner (Large Arcade Typography)
            GameObject headerObj = CreateUIObject("Header_Title", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 440f), new Vector2(1100f, 75f));
            TextMeshProUGUI headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
            headerTmp.font = titleFont;
            headerTmp.text = "CHOOSE YOUR FIGHTER";
            headerTmp.fontSize = 50;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.color = new Color(1.0f, 0.88f, 0.20f, 1f);

            // Sub-header prompt (e.g. "PLAYER 1: SELECT YOUR HERO")
            GameObject promptObj = CreateUIObject("Selection_Prompt", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 370f), new Vector2(1000f, 50f));
            TextMeshProUGUI promptTmp = promptObj.AddComponent<TextMeshProUGUI>();
            promptTmp.font = pressStartFont;
            promptTmp.text = "▶ PLAYER 1: SELECT YOUR HERO ◀";
            promptTmp.fontSize = 24;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.color = new Color(0.0f, 0.95f, 1.0f, 1f);

            // 2. Character Cards
            Sprite gokuAvatar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/goku_avatar_heroic.png");
            Sprite saitamaAvatar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/saitama_avatar_heroic.png");
            Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/portrait_frame_retro.png");

            List<CharacterSelectController.FighterCard> cards = new List<CharacterSelectController.FighterCard>();

            // Setup Card 0 (Goku) and Card 1 (Saitama) with expanded width and high-visibility stats
            CharacterSelectController.FighterCard cardGoku = CreateFighterCard(
                root.transform, 0, "goku", "GOKU", "HOPE OF THE UNIVERSE",
                new Vector2(-380f, 15f), gokuAvatar, frameSprite,
                retroFont, pressStartFont, charController
            );
            cards.Add(cardGoku);

            CharacterSelectController.FighterCard cardSaitama = CreateFighterCard(
                root.transform, 1, "saitama", "SAITAMA", "HERO FOR FUN",
                new Vector2(380f, 15f), saitamaAvatar, frameSprite,
                retroFont, pressStartFont, charController
            );
            cards.Add(cardSaitama);

            // 3. Mirror Match Notice (Repositioned at Y=-345 to guarantee ZERO overlap with cards ending at Y=-315)
            GameObject mirrorObj = CreateUIObject("Mirror_Match_Notice", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -345f), new Vector2(900f, 32f));
            TextMeshProUGUI mirrorTmp = mirrorObj.AddComponent<TextMeshProUGUI>();
            mirrorTmp.font = pressStartFont;
            mirrorTmp.text = "★ MIRROR MATCH: BOTH FIGHTERS ACTIVE ★";
            mirrorTmp.fontSize = 15;
            mirrorTmp.alignment = TextAlignmentOptions.Center;
            mirrorTmp.color = new Color(1.0f, 0.85f, 0.25f, 1f);
            mirrorObj.SetActive(false);

            // 4. Instructions Footer (Clear & Crisp at Y=-385)
            GameObject footerObj = CreateUIObject("Instructions_Footer", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -385f), new Vector2(1100f, 36f));
            TextMeshProUGUI footerTmp = footerObj.AddComponent<TextMeshProUGUI>();
            footerTmp.font = pressStartFont;
            footerTmp.text = "[A/D] MOVE   [ENTER/SPACE] CONFIRM   [R] RANDOM CPU   [ESC] BACK";
            footerTmp.fontSize = 16;
            footerTmp.alignment = TextAlignmentOptions.Center;
            footerTmp.color = new Color(0.75f, 0.85f, 1.0f, 0.95f);

            // 5. Interactive Action Buttons (at Y=-450)
            CreateActionButton(root.transform, "Btn_Back", "« BACK (ESC)", new Vector2(-440f, -450f), new Vector2(220f, 50f),
                new Color(0.15f, 0.20f, 0.30f, 1f), new Color(0.6f, 0.7f, 0.8f, 1f), pressStartFont,
                CharacterSelectButton.ButtonType.Back, 0, charController);

            GameObject btnRandom = CreateActionButton(root.transform, "Btn_RandomCpu", "⚡ RANDOM CPU (R)", new Vector2(0f, -450f), new Vector2(300f, 50f),
                new Color(0.25f, 0.20f, 0.05f, 1f), new Color(1.0f, 0.85f, 0.20f, 1f), pressStartFont,
                CharacterSelectButton.ButtonType.RandomCpu, 0, charController);

            CreateActionButton(root.transform, "Btn_LockIn", "LOCK IN (ENTER) »", new Vector2(440f, -450f), new Vector2(260f, 50f),
                new Color(0.05f, 0.22f, 0.25f, 1f), new Color(0.0f, 0.95f, 1.0f, 1f), pressStartFont,
                CharacterSelectButton.ButtonType.LockIn, 0, charController);

            // ----------------------------------------------------
            // 6. CINEMATIC VERSUS! OVERLAY (SLIDING AVATAR FRAMES)
            // ----------------------------------------------------
            GameObject versusObj = CreateUIObject("Versus_Overlay", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image versusDark = versusObj.AddComponent<Image>();
            versusDark.color = new Color(0.01f, 0.01f, 0.04f, 0.96f);

            // Left Side: P1 Cinematic Avatar Card (Flies in from off-screen left)
            GameObject p1VsCard = CreateUIObject("P1_VS_Card", versusObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 0f), new Vector2(380f, 500f));
            Image p1VsBg = p1VsCard.AddComponent<Image>();
            p1VsBg.color = new Color(0.05f, 0.08f, 0.16f, 0.95f);
            Outline p1VsOutline = p1VsCard.AddComponent<Outline>();
            p1VsOutline.effectColor = new Color(0.0f, 0.95f, 1.0f, 1f); // Electric Cyan
            p1VsOutline.effectDistance = new Vector2(5f, -5f);

            GameObject p1VsBadge = CreateUIObject("P1_Badge", p1VsCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 215f), new Vector2(260f, 38f));
            TextMeshProUGUI p1VsBadgeTmp = p1VsBadge.AddComponent<TextMeshProUGUI>();
            p1VsBadgeTmp.font = pressStartFont;
            p1VsBadgeTmp.text = "[ PLAYER 1 ]";
            p1VsBadgeTmp.fontSize = 16;
            p1VsBadgeTmp.alignment = TextAlignmentOptions.Center;
            p1VsBadgeTmp.color = new Color(0.0f, 0.95f, 1.0f, 1f);

            GameObject p1VsFrame = CreateUIObject("Avatar_Frame", p1VsCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(240f, 240f));
            Image p1VsFrameBg = p1VsFrame.AddComponent<Image>();
            p1VsFrameBg.color = new Color(0.08f, 0.12f, 0.22f, 1f);

            GameObject p1VsAvatar = CreateUIObject("Avatar_Image", p1VsFrame.transform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-12f, -12f));
            Image p1VsAvatarImg = p1VsAvatar.AddComponent<Image>();
            p1VsAvatarImg.sprite = gokuAvatar;
            p1VsAvatarImg.preserveAspect = true;

            GameObject p1VsName = CreateUIObject("Text_Name", p1VsCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -135f), new Vector2(360f, 50f));
            TextMeshProUGUI p1VsNameTmp = p1VsName.AddComponent<TextMeshProUGUI>();
            p1VsNameTmp.font = retroFont;
            p1VsNameTmp.text = "GOKU";
            p1VsNameTmp.fontSize = 42;
            p1VsNameTmp.fontStyle = FontStyles.Bold;
            p1VsNameTmp.alignment = TextAlignmentOptions.Center;
            p1VsNameTmp.color = new Color(0.0f, 0.95f, 1.0f, 1f);

            // Right Side: P2 Cinematic Avatar Card (Flies in from off-screen right)
            GameObject p2VsCard = CreateUIObject("P2_VS_Card", versusObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 0f), new Vector2(380f, 500f));
            Image p2VsBg = p2VsCard.AddComponent<Image>();
            p2VsBg.color = new Color(0.14f, 0.06f, 0.06f, 0.95f);
            Outline p2VsOutline = p2VsCard.AddComponent<Outline>();
            p2VsOutline.effectColor = new Color(1.0f, 0.25f, 0.15f, 1f); // Fiery Crimson
            p2VsOutline.effectDistance = new Vector2(5f, -5f);

            GameObject p2VsBadge = CreateUIObject("P2_Badge", p2VsCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 215f), new Vector2(260f, 38f));
            TextMeshProUGUI p2VsBadgeTmp = p2VsBadge.AddComponent<TextMeshProUGUI>();
            p2VsBadgeTmp.font = pressStartFont;
            p2VsBadgeTmp.text = "[ PLAYER 2 ]";
            p2VsBadgeTmp.fontSize = 16;
            p2VsBadgeTmp.alignment = TextAlignmentOptions.Center;
            p2VsBadgeTmp.color = new Color(1.0f, 0.25f, 0.15f, 1f);

            GameObject p2VsFrame = CreateUIObject("Avatar_Frame", p2VsCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(240f, 240f));
            Image p2VsFrameBg = p2VsFrame.AddComponent<Image>();
            p2VsFrameBg.color = new Color(0.20f, 0.08f, 0.08f, 1f);

            GameObject p2VsAvatar = CreateUIObject("Avatar_Image", p2VsFrame.transform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-12f, -12f));
            Image p2VsAvatarImg = p2VsAvatar.AddComponent<Image>();
            p2VsAvatarImg.sprite = saitamaAvatar;
            p2VsAvatarImg.preserveAspect = true;

            GameObject p2VsName = CreateUIObject("Text_Name", p2VsCard.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -135f), new Vector2(360f, 50f));
            TextMeshProUGUI p2VsNameTmp = p2VsName.AddComponent<TextMeshProUGUI>();
            p2VsNameTmp.font = retroFont;
            p2VsNameTmp.text = "SAITAMA";
            p2VsNameTmp.fontSize = 42;
            p2VsNameTmp.fontStyle = FontStyles.Bold;
            p2VsNameTmp.alignment = TextAlignmentOptions.Center;
            p2VsNameTmp.color = new Color(1.0f, 0.35f, 0.25f, 1f);

            // Center Clash VS Emblem
            GameObject vsCenterObj = CreateUIObject("VS_Center_Emblem", versusObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 180f));
            TextMeshProUGUI vsTmp = vsCenterObj.AddComponent<TextMeshProUGUI>();
            vsTmp.font = titleFont;
            vsTmp.text = "VS";
            vsTmp.fontSize = 96;
            vsTmp.fontStyle = FontStyles.Bold;
            vsTmp.alignment = TextAlignmentOptions.Center;
            vsTmp.enableVertexGradient = true;
            vsTmp.colorGradient = new VertexGradient(
                new Color(1.0f, 0.95f, 0.40f, 1f),
                new Color(1.0f, 1.0f, 1.0f, 1f),
                new Color(1.0f, 0.40f, 0.0f, 1f),
                new Color(1.0f, 0.15f, 0.20f, 1f)
            );
            Outline vsOutline = vsCenterObj.AddComponent<Outline>();
            vsOutline.effectColor = new Color(0.6f, 0.0f, 0.0f, 1f);
            vsOutline.effectDistance = new Vector2(6f, -6f);
            versusObj.SetActive(false);

            // 7. Wire Serialized Properties on CharacterSelectController
            SerializedObject cso = new SerializedObject(charController);
            cso.FindProperty("mainHeaderTitle").objectReferenceValue = headerTmp;
            cso.FindProperty("selectionPromptText").objectReferenceValue = promptTmp;
            cso.FindProperty("instructionsFooterText").objectReferenceValue = footerTmp;
            cso.FindProperty("mirrorMatchNotice").objectReferenceValue = mirrorObj;
            cso.FindProperty("versusOverlay").objectReferenceValue = versusObj;
            cso.FindProperty("versusP1CardRect").objectReferenceValue = p1VsCard.GetComponent<RectTransform>();
            cso.FindProperty("versusP1AvatarImage").objectReferenceValue = p1VsAvatarImg;
            cso.FindProperty("versusP1NameText").objectReferenceValue = p1VsNameTmp;
            cso.FindProperty("versusP2CardRect").objectReferenceValue = p2VsCard.GetComponent<RectTransform>();
            cso.FindProperty("versusP2AvatarImage").objectReferenceValue = p2VsAvatarImg;
            cso.FindProperty("versusP2NameText").objectReferenceValue = p2VsNameTmp;
            cso.FindProperty("versusP2BadgeText").objectReferenceValue = p2VsBadgeTmp;
            cso.FindProperty("versusP2CardOutline").objectReferenceValue = p2VsOutline;
            cso.FindProperty("versusCenterBadgeRect").objectReferenceValue = vsCenterObj.GetComponent<RectTransform>();
            cso.FindProperty("versusBannerText").objectReferenceValue = vsTmp;
            cso.FindProperty("randomCpuButton").objectReferenceValue = btnRandom;
            cso.FindProperty("mainMenuController").objectReferenceValue = menuController;
            cso.FindProperty("combatSceneName").stringValue = "SampleScene";

            AudioClip navAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Swishes_Whiffs/swish_01.wav");
            AudioClip lockAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Hits_Punches/impact_hit_01.ogg");
            AudioClip launchAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Anime_Powers/dbz_aura_burst.mp3");

            if (navAudio != null) cso.FindProperty("navigateClip").objectReferenceValue = navAudio;
            if (lockAudio != null) cso.FindProperty("lockInClip").objectReferenceValue = lockAudio;
            if (launchAudio != null) cso.FindProperty("versusLaunchClip").objectReferenceValue = launchAudio;

            SerializedProperty cardsProp = cso.FindProperty("fighterCards");
            cardsProp.ClearArray();
            for (int c = 0; c < cards.Count; c++)
            {
                cardsProp.InsertArrayElementAtIndex(c);
                SerializedProperty cardElem = cardsProp.GetArrayElementAtIndex(c);
                cardElem.FindPropertyRelative("characterId").stringValue = cards[c].characterId;
                cardElem.FindPropertyRelative("displayName").stringValue = cards[c].displayName;
                cardElem.FindPropertyRelative("epithet").stringValue = cards[c].epithet;
                cardElem.FindPropertyRelative("cardRoot").objectReferenceValue = cards[c].cardRoot;
                cardElem.FindPropertyRelative("outlineImage").objectReferenceValue = cards[c].outlineImage;
                cardElem.FindPropertyRelative("portraitImage").objectReferenceValue = cards[c].portraitImage;
                cardElem.FindPropertyRelative("portraitBackdrop").objectReferenceValue = cards[c].portraitBackdrop;
                cardElem.FindPropertyRelative("p1Indicator").objectReferenceValue = cards[c].p1Indicator;
                cardElem.FindPropertyRelative("p2Indicator").objectReferenceValue = cards[c].p2Indicator;
                cardElem.FindPropertyRelative("p2IndicatorText").objectReferenceValue = cards[c].p2IndicatorText;
                cardElem.FindPropertyRelative("nameText").objectReferenceValue = cards[c].nameText;
                cardElem.FindPropertyRelative("epithetText").objectReferenceValue = cards[c].epithetText;
                cardElem.FindPropertyRelative("hpValueText").objectReferenceValue = cards[c].hpValueText;
                cardElem.FindPropertyRelative("speedValueText").objectReferenceValue = cards[c].speedValueText;
                cardElem.FindPropertyRelative("attackValueText").objectReferenceValue = cards[c].attackValueText;
                cardElem.FindPropertyRelative("skillValueText").objectReferenceValue = cards[c].skillValueText;
                cardElem.FindPropertyRelative("ultimateValueText").objectReferenceValue = cards[c].ultimateValueText;
            }
            cso.ApplyModifiedProperties();

            root.SetActive(false); // Inactive until user picks 1P or 2P
            return root;
        }

        private static CharacterSelectController.FighterCard CreateFighterCard(
            Transform parent,
            int cardIndex,
            string charId,
            string dispName,
            string epithet,
            Vector2 position,
            Sprite avatarSprite,
            Sprite frameSprite,
            TMP_FontAsset retroFont,
            TMP_FontAsset pressStartFont,
            CharacterSelectController charController)
        {
            var card = new CharacterSelectController.FighterCard();
            card.characterId = charId;
            card.displayName = dispName;
            card.epithet = epithet;

            // Card Root Container (Enlarged 560x660 for clear vertical rhythm)
            GameObject cardObj = CreateUIObject($"Card_{dispName}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(560f, 660f));
            card.cardRoot = cardObj.GetComponent<RectTransform>();

            // Background Card Backing & Border Outline
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.06f, 0.08f, 0.14f, 0.92f); // Deep dark multiverse slate

            Outline cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.20f, 0.25f, 0.35f, 0.8f);
            cardOutline.effectDistance = new Vector2(4f, -4f);
            card.outlineImage = cardBg;

            // Add Clickable button component to card
            CharacterSelectButton cardBtn = cardObj.AddComponent<CharacterSelectButton>();
            cardBtn.Initialize(CharacterSelectButton.ButtonType.SelectCard, cardIndex, charController);

            // Indicators Row (Top)
            // P1 Indicator
            GameObject p1Badge = CreateUIObject("Badge_P1", cardObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-125f, 270f), new Vector2(210f, 38f));
            Image p1BadgeBg = p1Badge.AddComponent<Image>();
            p1BadgeBg.color = new Color(0.0f, 0.35f, 0.45f, 0.95f);
            GameObject p1Txt = CreateUIObject("Text", p1Badge.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI p1Tmp = p1Txt.AddComponent<TextMeshProUGUI>();
            p1Tmp.font = pressStartFont;
            p1Tmp.text = "[ PLAYER 1 ]";
            p1Tmp.fontSize = 15;
            p1Tmp.alignment = TextAlignmentOptions.Center;
            p1Tmp.color = new Color(0.0f, 0.95f, 1.0f, 1f);
            card.p1Indicator = p1Badge;
            p1Badge.SetActive(false);

            // P2 / CPU Indicator
            GameObject p2Badge = CreateUIObject("Badge_P2", cardObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(125f, 270f), new Vector2(210f, 38f));
            Image p2BadgeBg = p2Badge.AddComponent<Image>();
            p2BadgeBg.color = new Color(0.45f, 0.20f, 0.05f, 0.95f);
            GameObject p2Txt = CreateUIObject("Text", p2Badge.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI p2Tmp = p2Txt.AddComponent<TextMeshProUGUI>();
            p2Tmp.font = pressStartFont;
            p2Tmp.text = "[ CPU ]";
            p2Tmp.fontSize = 15;
            p2Tmp.alignment = TextAlignmentOptions.Center;
            p2Tmp.color = new Color(1.0f, 0.85f, 0.20f, 1f);
            card.p2Indicator = p2Badge;
            card.p2IndicatorText = p2Tmp;
            p2Badge.SetActive(false);

            // Portrait Avatar Frame (Enlarged 220x220)
            GameObject portraitBox = CreateUIObject("Portrait_Frame", cardObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(220f, 220f));
            Image frameBg = portraitBox.AddComponent<Image>();
            frameBg.color = new Color(0.10f, 0.12f, 0.22f, 1f);
            Outline portraitOutline = portraitBox.AddComponent<Outline>();
            portraitOutline.effectColor = new Color(0.35f, 0.45f, 0.65f, 0.8f);
            portraitOutline.effectDistance = new Vector2(3f, -3f);
            card.portraitBackdrop = frameBg;

            // Avatar Heroic Sprite (Always 100% original full color)
            GameObject avatarObj = CreateUIObject("Avatar_Sprite", portraitBox.transform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-12f, -12f));
            Image avatarImg = avatarObj.AddComponent<Image>();
            if (avatarSprite != null)
            {
                avatarImg.sprite = avatarSprite;
                avatarImg.preserveAspect = true;
            }
            avatarImg.color = Color.white;
            card.portraitImage = avatarImg;

            // Name & Title Banner (Large Arcade Typography)
            GameObject nameObj = CreateUIObject("Text_Name", cardObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(520f, 44f));
            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.font = retroFont;
            nameTmp.text = dispName;
            nameTmp.fontSize = 40;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.characterSpacing = 3f;
            nameTmp.color = new Color(1.0f, 0.95f, 0.4f, 1f);
            card.nameText = nameTmp;

            // Hero Epithet / Title with distinct hero color signatures & clean separation
            GameObject epithetObj = CreateUIObject("Text_Epithet", cardObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(520f, 32f));
            TextMeshProUGUI epithetTmp = epithetObj.AddComponent<TextMeshProUGUI>();
            epithetTmp.font = pressStartFont;
            epithetTmp.text = epithet;
            epithetTmp.fontSize = 19;
            epithetTmp.fontStyle = FontStyles.Bold;
            epithetTmp.alignment = TextAlignmentOptions.Center;

            Outline epOutline = epithetObj.AddComponent<Outline>();
            if (charId.ToLower() == "goku")
            {
                // Goku: Super Saiyan Radiant Gold with Ki Amber Outline
                epithetTmp.color = new Color(1.0f, 0.90f, 0.12f, 1f);
                epOutline.effectColor = new Color(0.25f, 0.12f, 0.0f, 0.95f);
            }
            else
            {
                // Saitama: Serious Punch Crimson with Deep Maroon Outline
                epithetTmp.color = new Color(1.0f, 0.30f, 0.30f, 1f);
                epOutline.effectColor = new Color(0.22f, 0.04f, 0.04f, 0.95f);
            }
            epOutline.effectDistance = new Vector2(2f, -2f);
            card.epithetText = epithetTmp;

            // Dynamic Stats Panel (Positioned at Y=-195 with 225px height -> top edge is at -82.5, giving >14px clearance)
            GameObject statBox = CreateUIObject("Stat_Panel", cardObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -195f), new Vector2(520f, 225f));
            Image statBg = statBox.AddComponent<Image>();
            statBg.color = new Color(0.03f, 0.04f, 0.08f, 0.88f);
            Outline statOutline = statBox.AddComponent<Outline>();
            statOutline.effectColor = new Color(0.20f, 0.25f, 0.35f, 0.6f);
            statOutline.effectDistance = new Vector2(2f, -2f);

            // Stat lines: HP & Speed, Attack, Skill, Ultimate (Font size 18pt)
            float statY = 60f;
            float statSpacing = 42f;

            // Health & Speed Line (Neon Emerald)
            GameObject hpObj = CreateUIObject("Line_HP_Speed", statBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, statY), new Vector2(490f, 30f));
            TextMeshProUGUI hpTmp = hpObj.AddComponent<TextMeshProUGUI>();
            hpTmp.font = pressStartFont;
            hpTmp.text = "HP: 100     SPD: 5.0 (FAST)";
            hpTmp.fontSize = 18;
            hpTmp.alignment = TextAlignmentOptions.Center;
            hpTmp.color = new Color(0.25f, 1.0f, 0.45f, 1f);
            card.hpValueText = hpTmp;

            // Attack Line (Blazing Amber)
            GameObject atkObj = CreateUIObject("Line_Attack", statBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, statY - statSpacing), new Vector2(490f, 30f));
            TextMeshProUGUI atkTmp = atkObj.AddComponent<TextMeshProUGUI>();
            atkTmp.font = pressStartFont;
            atkTmp.text = "ATTACK: Light 8  |  Heavy 15";
            atkTmp.fontSize = 18;
            atkTmp.alignment = TextAlignmentOptions.Center;
            atkTmp.color = new Color(1.0f, 0.75f, 0.20f, 1f);
            card.attackValueText = atkTmp;

            // Special Skill Line (Electric Aqua)
            GameObject skillObj = CreateUIObject("Line_Skill", statBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, statY - statSpacing * 2f), new Vector2(490f, 30f));
            TextMeshProUGUI skillTmp = skillObj.AddComponent<TextMeshProUGUI>();
            skillTmp.font = pressStartFont;
            skillTmp.text = "SKILL: None";
            skillTmp.fontSize = 18;
            skillTmp.alignment = TextAlignmentOptions.Center;
            skillTmp.color = new Color(0.0f, 0.95f, 1.0f, 1f);
            card.skillValueText = skillTmp;

            // Ultimate Attack Line (Crimson Flame)
            GameObject ultObj = CreateUIObject("Line_Ultimate", statBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, statY - statSpacing * 3f), new Vector2(490f, 30f));
            TextMeshProUGUI ultTmp = ultObj.AddComponent<TextMeshProUGUI>();
            ultTmp.font = pressStartFont;
            ultTmp.text = "ULTIMATE: Kamehameha [Ki: 80]";
            ultTmp.fontSize = 18;
            ultTmp.alignment = TextAlignmentOptions.Center;
            ultTmp.color = new Color(1.0f, 0.30f, 0.30f, 1f);
            card.ultimateValueText = ultTmp;

            return card;
        }

        private static GameObject CreateActionButton(
            Transform parent,
            string btnName,
            string label,
            Vector2 pos,
            Vector2 size,
            Color bgColor,
            Color borderColor,
            TMP_FontAsset font,
            CharacterSelectButton.ButtonType btnType,
            int cardIndex,
            CharacterSelectController charController)
        {
            GameObject btnObj = CreateUIObject(btnName, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            Image bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            CharacterSelectButton csb = btnObj.AddComponent<CharacterSelectButton>();
            csb.Initialize(btnType, cardIndex, charController);

            GameObject txtObj = CreateUIObject("Text", btnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = borderColor;

            return btnObj;
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 pos, Vector2 size, float minVal, float maxVal, float defaultVal, Color fillColor, Color handleColor)
        {
            GameObject sliderObj = CreateUIObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.value = defaultVal;

            // Background track
            GameObject bgObj = CreateUIObject("Background", sliderObj.transform, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.15f, 0.22f, 1f);

            // Fill Area
            GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(-5f, 0f), new Vector2(-15f, 0f));
            GameObject fill = CreateUIObject("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            slider.fillRect = fill.GetComponent<RectTransform>();

            // Handle Area
            GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-15f, 0f));
            GameObject handle = CreateUIObject("Handle", handleArea.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 24f));
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = handleColor;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImg;

            return slider;
        }

        private static GameObject CreateOptionsModal(Transform parent, TMP_FontAsset mainFont, TMP_FontAsset titleFont, MainMenuController controller)
        {
            GameObject modalRoot = CreateUIObject("Options_Modal_Overlay", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image modalDarkBg = modalRoot.AddComponent<Image>();
            modalDarkBg.color = new Color(0.02f, 0.01f, 0.05f, 0.94f);

            OptionsMenuController optionsCtrl = modalRoot.AddComponent<OptionsMenuController>();

            // Window Box (Spacious 1440x880)
            GameObject window = CreateUIObject("Window_Box", modalRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1440f, 880f));
            Image winImg = window.AddComponent<Image>();
            winImg.color = new Color(0.05f, 0.07f, 0.12f, 0.98f);
            Outline winOutline = window.AddComponent<Outline>();
            winOutline.effectColor = new Color(1.0f, 0.85f, 0.15f, 1f); // Arcade Gold border
            winOutline.effectDistance = new Vector2(5f, -5f);

            // 1. Header (Title & Subtitle - Enlarged by 80%)
            GameObject titleObj = CreateUIObject("Modal_Title", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 395f), new Vector2(1380f, 54f));
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.font = titleFont;
            titleTmp.text = "OPTIONS & CONTROLS";
            titleTmp.fontSize = 36;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1.0f, 0.85f, 0.15f, 1f);

            GameObject subObj = CreateUIObject("Modal_Subtitle", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 355f), new Vector2(1380f, 32f));
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.font = mainFont;
            subTmp.text = "CUSTOMIZE AUDIO MIXER & INDEPENDENT KEYBOARD PROFILES";
            subTmp.fontSize = 20;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.65f, 0.78f, 0.92f, 0.9f);

            // =========================================================================
            // VIEW 1: MAIN OPTIONS HUB
            // =========================================================================
            GameObject hubPanel = CreateUIObject("Panel_MainHub", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(1380f, 700f));

            // Music BGM Row Box
            GameObject musicRowObj = CreateUIObject("Music_Row_Box", hubPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1300f, 80f));
            Image musicRowImg = musicRowObj.AddComponent<Image>();
            musicRowImg.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            Outline musicOutline = musicRowObj.AddComponent<Outline>();
            musicOutline.effectColor = new Color(0.22f, 0.28f, 0.40f, 0.8f);
            musicOutline.effectDistance = new Vector2(2f, -2f);

            GameObject musicLblObj = CreateUIObject("Music_Label", musicRowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 0f), new Vector2(360f, 50f));
            TextMeshProUGUI musicLblTmp = musicLblObj.AddComponent<TextMeshProUGUI>();
            musicLblTmp.font = mainFont;
            musicLblTmp.text = "►  MUSIC BGM";
            musicLblTmp.fontSize = 25;
            musicLblTmp.fontStyle = FontStyles.Bold;
            musicLblTmp.alignment = TextAlignmentOptions.Left;
            musicLblTmp.color = new Color(1f, 0.85f, 0.2f, 1f);

            Slider musicSlider = CreateSlider("Music_Slider", musicRowObj.transform, new Vector2(20f, 0f), new Vector2(520f, 26f), 0f, 1f, InputSettingsManager.MusicVolume, new Color(1f, 0.85f, 0.2f, 1f), new Color(1f, 0.95f, 0.5f, 1f));

            GameObject musicValObj = CreateUIObject("Music_Val", musicRowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380f, 0f), new Vector2(120f, 50f));
            TextMeshProUGUI musicValTmp = musicValObj.AddComponent<TextMeshProUGUI>();
            musicValTmp.font = mainFont;
            musicValTmp.text = $"{Mathf.RoundToInt(InputSettingsManager.MusicVolume * 100f)}%";
            musicValTmp.fontSize = 26;
            musicValTmp.fontStyle = FontStyles.Bold;
            musicValTmp.alignment = TextAlignmentOptions.Center;
            musicValTmp.color = Color.white;

            // SFX Sound Row Box
            GameObject sfxRowObj = CreateUIObject("SFX_Row_Box", hubPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(1300f, 80f));
            Image sfxRowImg = sfxRowObj.AddComponent<Image>();
            sfxRowImg.color = new Color(0.08f, 0.11f, 0.18f, 0.95f);
            Outline sfxOutline = sfxRowObj.AddComponent<Outline>();
            sfxOutline.effectColor = new Color(0.22f, 0.28f, 0.40f, 0.8f);
            sfxOutline.effectDistance = new Vector2(2f, -2f);

            GameObject sfxLblObj = CreateUIObject("SFX_Label", sfxRowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, 0f), new Vector2(360f, 50f));
            TextMeshProUGUI sfxLblTmp = sfxLblObj.AddComponent<TextMeshProUGUI>();
            sfxLblTmp.font = mainFont;
            sfxLblTmp.text = "   SFX SOUND";
            sfxLblTmp.fontSize = 25;
            sfxLblTmp.fontStyle = FontStyles.Bold;
            sfxLblTmp.alignment = TextAlignmentOptions.Left;
            sfxLblTmp.color = new Color(0f, 0.9f, 1f, 1f);

            Slider sfxSlider = CreateSlider("SFX_Slider", sfxRowObj.transform, new Vector2(20f, 0f), new Vector2(520f, 26f), 0f, 1f, InputSettingsManager.SFXVolume, new Color(0f, 0.9f, 1f, 1f), new Color(0.7f, 1f, 1f, 1f));

            GameObject sfxValObj = CreateUIObject("SFX_Val", sfxRowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380f, 0f), new Vector2(120f, 50f));
            TextMeshProUGUI sfxValTmp = sfxValObj.AddComponent<TextMeshProUGUI>();
            sfxValTmp.font = mainFont;
            sfxValTmp.text = $"{Mathf.RoundToInt(InputSettingsManager.SFXVolume * 100f)}%";
            sfxValTmp.fontSize = 26;
            sfxValTmp.fontStyle = FontStyles.Bold;
            sfxValTmp.alignment = TextAlignmentOptions.Center;
            sfxValTmp.color = Color.white;

            // Single Player Key Sub-Menu Button
            GameObject btnSpObj = CreateUIObject("Btn_Open_SinglePlayer", hubPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(1300f, 80f));
            Image btnSpImg = btnSpObj.AddComponent<Image>();
            btnSpImg.color = new Color(0.10f, 0.14f, 0.24f, 1f);
            Outline btnSpOutline = btnSpObj.AddComponent<Outline>();
            btnSpOutline.effectColor = new Color(0.22f, 0.28f, 0.40f, 0.8f);
            btnSpOutline.effectDistance = new Vector2(2f, -2f);
            Button btnSp = btnSpObj.AddComponent<Button>();

            GameObject btnSpTxtObj = CreateUIObject("Text", btnSpObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(1260f, 36f));
            TextMeshProUGUI btnSpTmp = btnSpTxtObj.AddComponent<TextMeshProUGUI>();
            btnSpTmp.font = mainFont;
            btnSpTmp.text = "   CONFIGURE SINGLE PLAYER KEYS (Vs CPU / LAN)  »";
            btnSpTmp.fontSize = 24;
            btnSpTmp.fontStyle = FontStyles.Bold;
            btnSpTmp.alignment = TextAlignmentOptions.Center;
            btnSpTmp.color = Color.white;

            GameObject btnSpDescObj = CreateUIObject("Desc", btnSpObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1260f, 26f));
            TextMeshProUGUI btnSpDescTmp = btnSpDescObj.AddComponent<TextMeshProUGUI>();
            btnSpDescTmp.font = mainFont;
            btnSpDescTmp.text = "Full keyboard control for Player 1. Used against CPU and LAN matches.";
            btnSpDescTmp.fontSize = 16;
            btnSpDescTmp.alignment = TextAlignmentOptions.Center;
            btnSpDescTmp.color = new Color(0.5f, 0.85f, 1f, 1f);

            // Local Duel Key Sub-Menu Button
            GameObject btnLdObj = CreateUIObject("Btn_Open_LocalDuel", hubPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -65f), new Vector2(1300f, 80f));
            Image btnLdImg = btnLdObj.AddComponent<Image>();
            btnLdImg.color = new Color(0.10f, 0.14f, 0.24f, 1f);
            Outline btnLdOutline = btnLdObj.AddComponent<Outline>();
            btnLdOutline.effectColor = new Color(0.22f, 0.28f, 0.40f, 0.8f);
            btnLdOutline.effectDistance = new Vector2(2f, -2f);
            Button btnLd = btnLdObj.AddComponent<Button>();

            GameObject btnLdTxtObj = CreateUIObject("Text", btnLdObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(1260f, 36f));
            TextMeshProUGUI btnLdTmp = btnLdTxtObj.AddComponent<TextMeshProUGUI>();
            btnLdTmp.font = mainFont;
            btnLdTmp.text = "   CONFIGURE 1v1 LOCAL DUEL KEYS (SHARED KEYBOARD)  »";
            btnLdTmp.fontSize = 24;
            btnLdTmp.fontStyle = FontStyles.Bold;
            btnLdTmp.alignment = TextAlignmentOptions.Center;
            btnLdTmp.color = Color.white;

            GameObject btnLdDescObj = CreateUIObject("Desc", btnLdObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1260f, 26f));
            TextMeshProUGUI btnLdDescTmp = btnLdDescObj.AddComponent<TextMeshProUGUI>();
            btnLdDescTmp.font = mainFont;
            btnLdDescTmp.text = "Shared keyboard combined table for 2 players battling on one PC.";
            btnLdDescTmp.fontSize = 16;
            btnLdDescTmp.alignment = TextAlignmentOptions.Center;
            btnLdDescTmp.color = new Color(1f, 0.82f, 0.5f, 1f);

            // Lower Side-by-Side Action Buttons
            GameObject resetBtnObj = CreateUIObject("Button_Reset", hubPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -265f), new Vector2(580f, 66f));
            Image resetBtnImg = resetBtnObj.AddComponent<Image>();
            resetBtnImg.color = new Color(0.26f, 0.12f, 0.08f, 1f);
            Outline resetOutline = resetBtnObj.AddComponent<Outline>();
            resetOutline.effectColor = new Color(1.0f, 0.55f, 0.1f, 1f);
            resetOutline.effectDistance = new Vector2(2f, -2f);
            Button resetBtn = resetBtnObj.AddComponent<Button>();

            GameObject resetTxtObj = CreateUIObject("Text", resetBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI resetTxtTmp = resetTxtObj.AddComponent<TextMeshProUGUI>();
            resetTxtTmp.font = mainFont;
            resetTxtTmp.text = "   [ ↺ RESET ALL TO DEFAULTS ]";
            resetTxtTmp.fontSize = 23;
            resetTxtTmp.fontStyle = FontStyles.Bold;
            resetTxtTmp.alignment = TextAlignmentOptions.Center;
            resetTxtTmp.color = new Color(1.0f, 0.65f, 0.15f, 1f);

            GameObject closeBtn = CreateUIObject("Button_SaveClose", hubPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(330f, -265f), new Vector2(580f, 66f));
            Image closeBtnImg = closeBtn.AddComponent<Image>();
            closeBtnImg.color = new Color(0.08f, 0.24f, 0.16f, 1f);
            Outline btnOutline = closeBtn.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.0f, 0.9f, 0.5f, 1f);
            btnOutline.effectDistance = new Vector2(2f, -2f);
            Button saveCloseBtn = closeBtn.AddComponent<Button>();

            GameObject closeTxtObj = CreateUIObject("Text", closeBtn.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI closeTxtTmp = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTxtTmp.font = mainFont;
            closeTxtTmp.text = "   [ « SAVE & BACK TO TITLE (ESC) ]";
            closeTxtTmp.fontSize = 23;
            closeTxtTmp.fontStyle = FontStyles.Bold;
            closeTxtTmp.alignment = TextAlignmentOptions.Center;
            closeTxtTmp.color = new Color(0.4f, 0.95f, 0.68f, 1f);

            // =========================================================================
            // VIEW 2: SINGLE PLAYER KEY BINDINGS SUB-SCREEN
            // =========================================================================
            GameObject spPanel = CreateUIObject("Panel_SinglePlayer", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(1380f, 700f));

            GameObject spGuideObj = CreateUIObject("Guide_Text", spPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 315f), new Vector2(1300f, 36f));
            TextMeshProUGUI spGuideTmp = spGuideObj.AddComponent<TextMeshProUGUI>();
            spGuideTmp.font = mainFont;
            spGuideTmp.text = "SINGLE PLAYER PROFILE: FULL KEYBOARD MAPPING";
            spGuideTmp.fontSize = 22;
            spGuideTmp.fontStyle = FontStyles.Bold;
            spGuideTmp.alignment = TextAlignmentOptions.Center;
            spGuideTmp.color = new Color(0.5f, 0.85f, 1f, 1f);

            FighterAction[] actions = new FighterAction[]
            {
                FighterAction.Left,
                FighterAction.Right,
                FighterAction.Jump,
                FighterAction.Crouch,
                FighterAction.LightAttack,
                FighterAction.HeavyAttack,
                FighterAction.Skill,
                FighterAction.Ultimate,
                FighterAction.Meditate
            };

            string[] actionLabels = new string[]
            {
                "MOVE LEFT",
                "MOVE RIGHT",
                "JUMP",
                "CROUCH / GUARD",
                "LIGHT ATTACK",
                "HEAVY ATTACK",
                "SPECIAL SKILL",
                "ULTIMATE MOVE",
                "KI MEDITATE"
            };

            float spStartY = 250f;
            float spStepY = -54f;
            for (int i = 0; i < actions.Length; i++)
            {
                float rowY = spStartY + i * spStepY;
                GameObject rowObj = CreateUIObject($"Row_{actions[i]}", spPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, rowY), new Vector2(1260f, 48f));
                Image rowImg = rowObj.AddComponent<Image>();
                rowImg.color = (i % 2 == 0) ? new Color(0.08f, 0.10f, 0.16f, 0.7f) : new Color(0.12f, 0.15f, 0.22f, 0.7f);

                GameObject actLblObj = CreateUIObject("ActionName", rowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, 0f), new Vector2(580f, 42f));
                TextMeshProUGUI actLblTmp = actLblObj.AddComponent<TextMeshProUGUI>();
                actLblTmp.font = mainFont;
                actLblTmp.text = $"   {actionLabels[i]}";
                actLblTmp.fontSize = 23;
                actLblTmp.fontStyle = FontStyles.Bold;
                actLblTmp.alignment = TextAlignmentOptions.Left;
                actLblTmp.color = new Color(0.85f, 0.90f, 0.95f, 1f);

                GameObject keyBtnObj = CreateUIObject("Button_Key", rowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(330f, 0f), new Vector2(320f, 42f));
                Image keyBtnImg = keyBtnObj.AddComponent<Image>();
                keyBtnImg.color = new Color(0.12f, 0.18f, 0.28f, 1f);
                Outline keyBtnOutline = keyBtnObj.AddComponent<Outline>();
                keyBtnOutline.effectColor = new Color(0.0f, 0.8f, 0.9f, 0.7f);
                keyBtnOutline.effectDistance = new Vector2(1f, -1f);
                Button keyBtn = keyBtnObj.AddComponent<Button>();

                GameObject keyTxtObj = CreateUIObject("Text", keyBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                TextMeshProUGUI keyTxtTmp = keyTxtObj.AddComponent<TextMeshProUGUI>();
                keyTxtTmp.font = mainFont;
                keyTxtTmp.text = OptionsMenuController.FormatKeyName(InputSettingsManager.GetSinglePlayerKey(actions[i]));
                keyTxtTmp.fontSize = 24;
                keyTxtTmp.fontStyle = FontStyles.Bold;
                keyTxtTmp.alignment = TextAlignmentOptions.Center;
                keyTxtTmp.color = Color.white;

                optionsCtrl.AddSinglePlayerRow(actions[i], actionLabels[i], keyBtn, keyBtnOutline, actLblTmp, keyTxtTmp);
            }

            // Single Player Sub-Menu Back Button
            GameObject spBackObj = CreateUIObject("Button_SpBack", spPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -285f), new Vector2(660f, 62f));
            Image spBackImg = spBackObj.AddComponent<Image>();
            spBackImg.color = new Color(0.12f, 0.18f, 0.28f, 1f);
            Outline spBackOutline = spBackObj.AddComponent<Outline>();
            spBackOutline.effectColor = new Color(0.0f, 0.8f, 0.9f, 0.8f);
            spBackOutline.effectDistance = new Vector2(2f, -2f);
            Button spBackBtn = spBackObj.AddComponent<Button>();

            GameObject spBackTxtObj = CreateUIObject("Text", spBackObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI spBackTmp = spBackTxtObj.AddComponent<TextMeshProUGUI>();
            spBackTmp.font = mainFont;
            spBackTmp.text = "   [ « BACK TO OPTIONS (ESC) ]";
            spBackTmp.fontSize = 23;
            spBackTmp.fontStyle = FontStyles.Bold;
            spBackTmp.alignment = TextAlignmentOptions.Center;
            spBackTmp.color = Color.white;

            // =========================================================================
            // VIEW 3: 1v1 LOCAL DUEL COMBINED TABLE SUB-SCREEN
            // =========================================================================
            GameObject ldPanel = CreateUIObject("Panel_LocalDuel", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -25f), new Vector2(1380f, 700f));

            GameObject ldGuideObj = CreateUIObject("Guide_Text", ldPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 318f), new Vector2(1320f, 36f));
            TextMeshProUGUI ldGuideTmp = ldGuideObj.AddComponent<TextMeshProUGUI>();
            ldGuideTmp.font = mainFont;
            ldGuideTmp.text = "1v1 LOCAL DUEL PROFILE: SHARED KEYBOARD COMBINED TABLE";
            ldGuideTmp.fontSize = 22;
            ldGuideTmp.fontStyle = FontStyles.Bold;
            ldGuideTmp.alignment = TextAlignmentOptions.Center;
            ldGuideTmp.color = new Color(1f, 0.82f, 0.5f, 1f);

            // Table Header Row
            GameObject headerObj = CreateUIObject("Table_Header", ldPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 272f), new Vector2(1320f, 44f));
            Image headerImg = headerObj.AddComponent<Image>();
            headerImg.color = new Color(0.14f, 0.18f, 0.26f, 1f);
            Outline headerOutline = headerObj.AddComponent<Outline>();
            headerOutline.effectColor = new Color(0.35f, 0.45f, 0.60f, 0.8f);
            headerOutline.effectDistance = new Vector2(1f, -1f);

            GameObject thAct = CreateUIObject("TH_Action", headerObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 0f), new Vector2(480f, 38f));
            TextMeshProUGUI thActTmp = thAct.AddComponent<TextMeshProUGUI>();
            thActTmp.font = mainFont;
            thActTmp.text = "   FIGHTER ACTION";
            thActTmp.fontSize = 22;
            thActTmp.fontStyle = FontStyles.Bold;
            thActTmp.alignment = TextAlignmentOptions.Left;
            thActTmp.color = new Color(1f, 0.85f, 0.2f, 1f);

            GameObject thP1 = CreateUIObject("TH_P1", headerObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), new Vector2(340f, 38f));
            TextMeshProUGUI thP1Tmp = thP1.AddComponent<TextMeshProUGUI>();
            thP1Tmp.font = mainFont;
            thP1Tmp.text = "PLAYER 1 (LEFT SIDE)";
            thP1Tmp.fontSize = 22;
            thP1Tmp.fontStyle = FontStyles.Bold;
            thP1Tmp.alignment = TextAlignmentOptions.Center;
            thP1Tmp.color = new Color(0f, 0.9f, 1f, 1f);

            GameObject thP2 = CreateUIObject("TH_P2", headerObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(450f, 0f), new Vector2(340f, 38f));
            TextMeshProUGUI thP2Tmp = thP2.AddComponent<TextMeshProUGUI>();
            thP2Tmp.font = mainFont;
            thP2Tmp.text = "PLAYER 2 (RIGHT SIDE)";
            thP2Tmp.fontSize = 22;
            thP2Tmp.fontStyle = FontStyles.Bold;
            thP2Tmp.alignment = TextAlignmentOptions.Center;
            thP2Tmp.color = new Color(1f, 0.35f, 0.35f, 1f);

            // 9 Combined Rows
            float ldStartY = 220f;
            float ldStepY = -52f;
            for (int i = 0; i < actions.Length; i++)
            {
                float rowY = ldStartY + i * ldStepY;
                GameObject rowObj = CreateUIObject($"Row_{actions[i]}", ldPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, rowY), new Vector2(1320f, 46f));
                Image rowImg = rowObj.AddComponent<Image>();
                rowImg.color = (i % 2 == 0) ? new Color(0.08f, 0.10f, 0.16f, 0.7f) : new Color(0.12f, 0.15f, 0.22f, 0.7f);

                // Column 1: Action Name
                GameObject actLblObj = CreateUIObject("ActionName", rowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 0f), new Vector2(480f, 40f));
                TextMeshProUGUI actLblTmp = actLblObj.AddComponent<TextMeshProUGUI>();
                actLblTmp.font = mainFont;
                actLblTmp.text = $"   {actionLabels[i]}";
                actLblTmp.fontSize = 22;
                actLblTmp.fontStyle = FontStyles.Bold;
                actLblTmp.alignment = TextAlignmentOptions.Left;
                actLblTmp.color = new Color(0.85f, 0.90f, 0.95f, 1f);

                // Column 2: P1 Key Button
                GameObject p1BtnObj = CreateUIObject("Button_P1Key", rowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), new Vector2(310f, 40f));
                Image p1BtnImg = p1BtnObj.AddComponent<Image>();
                p1BtnImg.color = new Color(0.10f, 0.18f, 0.26f, 1f);
                Outline p1Outline = p1BtnObj.AddComponent<Outline>();
                p1Outline.effectColor = new Color(0.0f, 0.8f, 0.9f, 0.7f);
                p1Outline.effectDistance = new Vector2(1f, -1f);
                Button p1Btn = p1BtnObj.AddComponent<Button>();

                GameObject p1TxtObj = CreateUIObject("Text", p1BtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                TextMeshProUGUI p1TxtTmp = p1TxtObj.AddComponent<TextMeshProUGUI>();
                p1TxtTmp.font = mainFont;
                p1TxtTmp.text = OptionsMenuController.FormatKeyName(InputSettingsManager.GetLocalDuelP1Key(actions[i]));
                p1TxtTmp.fontSize = 23;
                p1TxtTmp.fontStyle = FontStyles.Bold;
                p1TxtTmp.alignment = TextAlignmentOptions.Center;
                p1TxtTmp.color = Color.white;

                // Column 3: P2 Key Button
                GameObject p2BtnObj = CreateUIObject("Button_P2Key", rowObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(450f, 0f), new Vector2(310f, 40f));
                Image p2BtnImg = p2BtnObj.AddComponent<Image>();
                p2BtnImg.color = new Color(0.24f, 0.12f, 0.16f, 1f);
                Outline p2Outline = p2BtnObj.AddComponent<Outline>();
                p2Outline.effectColor = new Color(1.0f, 0.35f, 0.35f, 0.7f);
                p2Outline.effectDistance = new Vector2(1f, -1f);
                Button p2Btn = p2BtnObj.AddComponent<Button>();

                GameObject p2TxtObj = CreateUIObject("Text", p2BtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                TextMeshProUGUI p2TxtTmp = p2TxtObj.AddComponent<TextMeshProUGUI>();
                p2TxtTmp.font = mainFont;
                p2TxtTmp.text = OptionsMenuController.FormatKeyName(InputSettingsManager.GetLocalDuelP2Key(actions[i]));
                p2TxtTmp.fontSize = 23;
                p2TxtTmp.fontStyle = FontStyles.Bold;
                p2TxtTmp.alignment = TextAlignmentOptions.Center;
                p2TxtTmp.color = Color.white;

                optionsCtrl.AddLocalDuelRow(actions[i], actionLabels[i], actLblTmp, p1Btn, p1Outline, p1TxtTmp, p2Btn, p2Outline, p2TxtTmp);
            }

            // Local Duel Sub-Menu Back Button
            GameObject ldBackObj = CreateUIObject("Button_LdBack", ldPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -285f), new Vector2(660f, 62f));
            Image ldBackImg = ldBackObj.AddComponent<Image>();
            ldBackImg.color = new Color(0.12f, 0.18f, 0.28f, 1f);
            Outline ldBackOutline = ldBackObj.AddComponent<Outline>();
            ldBackOutline.effectColor = new Color(0.0f, 0.8f, 0.9f, 0.8f);
            ldBackOutline.effectDistance = new Vector2(2f, -2f);
            Button ldBackBtn = ldBackObj.AddComponent<Button>();

            GameObject ldBackTxtObj = CreateUIObject("Text", ldBackObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI ldBackTmp = ldBackTxtObj.AddComponent<TextMeshProUGUI>();
            ldBackTmp.font = mainFont;
            ldBackTmp.text = "   [ « BACK TO OPTIONS (ESC) ]";
            ldBackTmp.fontSize = 23;
            ldBackTmp.fontStyle = FontStyles.Bold;
            ldBackTmp.alignment = TextAlignmentOptions.Center;
            ldBackTmp.color = Color.white;

            // Connect references to controller
            optionsCtrl.InitializeReferences(
                controller,
                hubPanel, spPanel, ldPanel,
                musicOutline, musicLblTmp, musicSlider, musicValTmp,
                sfxOutline, sfxLblTmp, sfxSlider, sfxValTmp,
                btnSp, btnSpOutline, btnSpTmp,
                btnLd, btnLdOutline, btnLdTmp,
                resetBtn, resetOutline, resetTxtTmp,
                saveCloseBtn, btnOutline, closeTxtTmp,
                spBackBtn, spBackOutline, spBackTmp,
                ldBackBtn, ldBackOutline, ldBackTmp
            );

            // Default view state: Hub active, sub-panels inactive
            hubPanel.SetActive(true);
            spPanel.SetActive(false);
            ldPanel.SetActive(false);

            EditorUtility.SetDirty(optionsCtrl);
            return modalRoot;
        }

        private static void SetupCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.01f, 0.05f, 1f);
            camObj.AddComponent<AudioListener>(); // Essential player ears for all game sound!
        }

        private static void SetupMenuTextures()
        {
            string bgPath = "Assets/Sprites/UI/MainMenu/menu_bg_multiverse.png";
            TextureImporter importer = AssetImporter.GetAtPath(bgPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            string logoPath = "Assets/Sprites/UI/MainMenu/mystic_combat_title_logo.png";
            TextureImporter logoImporter = AssetImporter.GetAtPath(logoPath) as TextureImporter;
            if (logoImporter != null)
            {
                logoImporter.textureType = TextureImporterType.Sprite;
                logoImporter.spriteImportMode = SpriteImportMode.Single;
                logoImporter.alphaIsTransparency = true;
                logoImporter.mipmapEnabled = false;
                logoImporter.filterMode = FilterMode.Bilinear;
                logoImporter.maxTextureSize = 2048;
                logoImporter.SaveAndReimport();
            }
        }

        private static TMP_FontAsset GetOrCreateTMPFont(string fontPath, string assetName, TMP_FontAsset fallbackFont)
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (ttf == null) return fallbackFont;

            string targetDir = "Assets/Fonts/TMP_Assets";
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            string assetPath = $"{targetDir}/{assetName}.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

            // If existing asset has a valid material, use it!
            if (existing != null && existing.material != null)
            {
                return existing;
            }

            try
            {
                // Create with Dynamic population mode so glyphs are dynamically rendered
                TMP_FontAsset newAsset = TMP_FontAsset.CreateFontAsset(
                    ttf,
                    90,
                    9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic
                );

                if (newAsset != null)
                {
                    Shader distanceFieldShader = Shader.Find("TextMeshPro/Distance Field");
                    if (distanceFieldShader != null && (newAsset.material == null || existing == null))
                    {
                        Material mat = new Material(distanceFieldShader);
                        mat.name = ttf.name + " Material";
                        newAsset.material = mat;
                    }

                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(newAsset, existing);
                        AssetDatabase.SaveAssets();
                        return existing;
                    }

                    AssetDatabase.CreateAsset(newAsset, assetPath);
                    if (newAsset.material != null)
                    {
                        AssetDatabase.AddObjectToAsset(newAsset.material, newAsset);
                    }
                    if (newAsset.atlasTexture != null)
                    {
                        AssetDatabase.AddObjectToAsset(newAsset.atlasTexture, newAsset);
                    }
                    AssetDatabase.SaveAssets();
                    return newAsset;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MainMenuSetupTool] TMP Font creation warning for {fontPath}: {ex.Message}. Using guaranteed fallback font.");
            }

            return fallbackFont != null ? fallbackFont : existing;
        }

        public static void UpdateBuildSettings()
        {
            // Enforce MainMenuScene strictly at Index 0 (Primary Boot Scene) and SampleScene at Index 1
            var newScenes = new List<EditorBuildSettingsScene>();
            newScenes.Add(new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true));
            if (File.Exists(COMBAT_SCENE_PATH))
            {
                newScenes.Add(new EditorBuildSettingsScene(COMBAT_SCENE_PATH, true));
            }

            // Retain any other custom scenes after the core game scenes
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path != MAIN_MENU_SCENE_PATH && s.path != COMBAT_SCENE_PATH)
                {
                    newScenes.Add(s);
                }
            }

            EditorBuildSettings.scenes = newScenes.ToArray();
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private static GameObject CreateLanLobbyModal(
            Transform canvasTransform,
            TMP_FontAsset titleFont,
            TMP_FontAsset retroFont,
            TMP_FontAsset pressStartFont,
            MainMenuController menuController,
            CharacterSelectController charController)
        {
            // 1. Fullscreen Semi-Transparent Backdrop Dim
            GameObject root = CreateUIObject("Modal_LanLobby", canvasTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.02f, 0.01f, 0.06f, 0.88f);

            LanLobbyModal lanModal = root.AddComponent<LanLobbyModal>();

            // 2. Center Arcade Window Panel
            GameObject window = CreateUIObject("Lan_Window_Frame", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 680f));
            Image winImg = window.AddComponent<Image>();
            winImg.color = new Color(0.06f, 0.09f, 0.18f, 0.98f); // Cosmic dark arcade navy
            Outline winOutline = window.AddComponent<Outline>();
            winOutline.effectColor = new Color(0.0f, 0.95f, 1.0f, 0.85f); // Electric Cyan neon frame
            winOutline.effectDistance = new Vector2(3f, -3f);

            // 3. Header Title
            GameObject titleObj = CreateUIObject("Title", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 255f), new Vector2(900f, 60f));
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.font = titleFont != null ? titleFont : retroFont;
            titleTmp.text = "⚡ LAN MULTIPLAYER COMBAT ⚡";
            titleTmp.fontSize = 40;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1.0f, 0.88f, 0.20f, 1f); // Solar Gold

            // 4. Local IP Banner
            GameObject ipObj = CreateUIObject("Text_LocalIp", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(900f, 40f));
            TextMeshProUGUI ipTmp = ipObj.AddComponent<TextMeshProUGUI>();
            ipTmp.font = pressStartFont;
            ipTmp.text = "YOUR LOCAL LAN IP: 127.0.0.1  |  PORT: 7777";
            ipTmp.fontSize = 20;
            ipTmp.fontStyle = FontStyles.Bold;
            ipTmp.alignment = TextAlignmentOptions.Center;
            ipTmp.color = new Color(0.0f, 0.95f, 1.0f, 1f); // Electric Cyan

            // 5. Status Prompt Banner
            GameObject statusObj = CreateUIObject("Text_Status", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(900f, 50f));
            TextMeshProUGUI statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
            statusTmp.font = retroFont;
            statusTmp.text = "SELECT HOST OR JOIN TO COMMENCE LAN COMBAT";
            statusTmp.fontSize = 22;
            statusTmp.alignment = TextAlignmentOptions.Center;
            statusTmp.color = new Color(0.85f, 0.95f, 1.0f, 1f);

            // 6. Section 1: Host Button
            GameObject hostBtnObj = CreateUIObject("Btn_Host", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(680f, 75f));
            Image hostImg = hostBtnObj.AddComponent<Image>();
            hostImg.color = new Color(0.12f, 0.20f, 0.35f, 1f);
            Outline hostOutline = hostBtnObj.AddComponent<Outline>();
            hostOutline.effectColor = new Color(0.0f, 0.95f, 1f, 0.9f);
            hostOutline.effectDistance = new Vector2(2f, -2f);
            Button hostBtn = hostBtnObj.AddComponent<Button>();

            GameObject hostTxtObj = CreateUIObject("Text", hostBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI hostTxtTmp = hostTxtObj.AddComponent<TextMeshProUGUI>();
            hostTxtTmp.font = retroFont;
            hostTxtTmp.text = "⚔  HOST MATCH (WAIT FOR OPPONENT)";
            hostTxtTmp.fontSize = 24;
            hostTxtTmp.fontStyle = FontStyles.Bold;
            hostTxtTmp.alignment = TextAlignmentOptions.Center;
            hostTxtTmp.color = Color.white;

            // 7. Subtle Divider
            GameObject divObj = CreateUIObject("Divider", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(750f, 2f));
            Image divImg = divObj.AddComponent<Image>();
            divImg.color = new Color(0.2f, 0.3f, 0.5f, 0.6f);

            // 8. Section 2: Join by IP
            GameObject joinLabelObj = CreateUIObject("Label_JoinPrompt", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(680f, 32f));
            TextMeshProUGUI joinLblTmp = joinLabelObj.AddComponent<TextMeshProUGUI>();
            joinLblTmp.font = retroFont;
            joinLblTmp.text = "OR ENTER OPPONENT'S HOST IP ADDRESS:";
            joinLblTmp.fontSize = 20;
            joinLblTmp.alignment = TextAlignmentOptions.Center;
            joinLblTmp.color = new Color(0.7f, 0.85f, 1.0f, 1f);

            // Input Field Container
            GameObject inputObj = CreateUIObject("Input_IP", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-155f, -125f), new Vector2(350f, 62f));
            Image inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.08f, 0.12f, 0.22f, 1f);
            Outline inputOutline = inputObj.AddComponent<Outline>();
            inputOutline.effectColor = new Color(0.0f, 0.8f, 0.9f, 0.8f);
            inputOutline.effectDistance = new Vector2(1f, -1f);

            GameObject inputTxtObj = CreateUIObject("Text", inputObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI inputTmp = inputTxtObj.AddComponent<TextMeshProUGUI>();
            inputTmp.font = pressStartFont;
            inputTmp.text = "127.0.0.1";
            inputTmp.fontSize = 22;
            inputTmp.alignment = TextAlignmentOptions.Center;
            inputTmp.color = Color.white;

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textComponent = inputTmp;
            inputField.text = "127.0.0.1";

            // Join Button
            GameObject joinBtnObj = CreateUIObject("Btn_Join", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(205f, -125f), new Vector2(310f, 62f));
            Image joinImg = joinBtnObj.AddComponent<Image>();
            joinImg.color = new Color(0.12f, 0.25f, 0.18f, 1f); // Emerald tone
            Outline joinOutline = joinBtnObj.AddComponent<Outline>();
            joinOutline.effectColor = new Color(0.2f, 1.0f, 0.4f, 0.85f);
            joinOutline.effectDistance = new Vector2(2f, -2f);
            Button joinBtn = joinBtnObj.AddComponent<Button>();

            GameObject joinTxtObj = CreateUIObject("Text", joinBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI joinTxtTmp = joinTxtObj.AddComponent<TextMeshProUGUI>();
            joinTxtTmp.font = retroFont;
            joinTxtTmp.text = "⚡ CONNECT & JOIN";
            joinTxtTmp.fontSize = 22;
            joinTxtTmp.fontStyle = FontStyles.Bold;
            joinTxtTmp.alignment = TextAlignmentOptions.Center;
            joinTxtTmp.color = Color.white;

            // 9. Back Button
            GameObject backBtnObj = CreateUIObject("Btn_Back", window.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -235f), new Vector2(400f, 54f));
            Image backImg = backBtnObj.AddComponent<Image>();
            backImg.color = new Color(0.18f, 0.12f, 0.16f, 1f);
            Outline backOutline = backBtnObj.AddComponent<Outline>();
            backOutline.effectColor = new Color(1.0f, 0.4f, 0.4f, 0.8f);
            backOutline.effectDistance = new Vector2(2f, -2f);
            Button backBtn = backBtnObj.AddComponent<Button>();

            GameObject backTxtObj = CreateUIObject("Text", backBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI backTxtTmp = backTxtObj.AddComponent<TextMeshProUGUI>();
            backTxtTmp.font = retroFont;
            backTxtTmp.text = "◄ BACK TO MAIN MENU (ESC)";
            backTxtTmp.fontSize = 20;
            backTxtTmp.fontStyle = FontStyles.Bold;
            backTxtTmp.alignment = TextAlignmentOptions.Center;
            backTxtTmp.color = Color.white;

            // Wire all references
            GameObject charRoot = charController != null ? charController.gameObject : null;
            lanModal.InitializeReferences(
                root, titleTmp, ipTmp, statusTmp,
                hostBtn, hostTxtTmp,
                inputField,
                joinBtn, joinTxtTmp,
                backBtn,
                charController, charRoot, menuController
            );

            root.SetActive(false);
            return root;
        }
    }
}

#endif
