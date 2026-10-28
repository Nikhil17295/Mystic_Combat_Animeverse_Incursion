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
        private const string MAIN_MENU_BAKE_KEY = "MysticCombat_MainMenu_Bake_v13";

        static MainMenuSetupTool()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(MAIN_MENU_SCENE_PATH) || !EditorPrefs.GetBool(MAIN_MENU_BAKE_KEY, false))
                {
                    EditorPrefs.SetBool(MAIN_MENU_BAKE_KEY, true);
                    BakeMainMenuScene();
                }
            };
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
                "OPTIONS & MOVESET",
                "QUIT GAME"
            };

            List<MainMenuController.MenuItem> items = new List<MainMenuController.MenuItem>();
            float lineSpacing = 46f;
            float startY = 45f;

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
            skillTmp.text = "SKILL: Kamehameha [Ki: 20]";
            skillTmp.fontSize = 18;
            skillTmp.alignment = TextAlignmentOptions.Center;
            skillTmp.color = new Color(0.0f, 0.95f, 1.0f, 1f);
            card.skillValueText = skillTmp;

            // Ultimate Attack Line (Crimson Flame)
            GameObject ultObj = CreateUIObject("Line_Ultimate", statBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, statY - statSpacing * 3f), new Vector2(490f, 30f));
            TextMeshProUGUI ultTmp = ultObj.AddComponent<TextMeshProUGUI>();
            ultTmp.font = pressStartFont;
            ultTmp.text = "ULTIMATE: Spirit Bomb [Ki: 80]";
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

        private static GameObject CreateOptionsModal(Transform parent, TMP_FontAsset mainFont, TMP_FontAsset titleFont, MainMenuController controller)
        {
            GameObject modalRoot = CreateUIObject("Options_Modal_Overlay", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image modalDarkBg = modalRoot.AddComponent<Image>();
            modalDarkBg.color = new Color(0.02f, 0.01f, 0.05f, 0.94f);

            GameObject window = CreateUIObject("Window_Box", modalRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850f, 520f));
            Image winImg = window.AddComponent<Image>();
            winImg.color = new Color(0.06f, 0.07f, 0.12f, 0.98f);
            Outline winOutline = window.AddComponent<Outline>();
            winOutline.effectColor = new Color(1.0f, 0.85f, 0.1f, 1f); // Arcade Gold border
            winOutline.effectDistance = new Vector2(5f, -5f);

            GameObject titleObj = CreateUIObject("Modal_Title", window.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(0f, 50f));
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.font = titleFont;
            titleTmp.text = "OPTIONS & CONTROLS";
            titleTmp.fontSize = 28;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1.0f, 0.85f, 0.1f, 1f);

            GameObject bodyObj = CreateUIObject("Modal_Body", window.transform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyTmp.font = mainFont;
            bodyTmp.text = "PLAYER MOVESET CONFIGURATION\n\n" +
                           "• Player 1 (Goku):    WASD (Move/Jump/Crouch) | F (Light) | C (Heavy) | E (Skill) | Q (Ult)\n\n" +
                           "• Player 2 (Saitama): Arrows (Move/Jump/Crouch) | J (Light) | K (Heavy) | U (Skill) | I (Ult)\n\n" +
                           "[Custom player-preferred movesets and audio mixer will be configured here.]";
            bodyTmp.fontSize = 18;
            bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            bodyTmp.color = new Color(0.85f, 0.90f, 0.98f, 1f);

            GameObject closeBtn = CreateUIObject("Button_Back", window.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(280f, 48f));
            Image closeBtnImg = closeBtn.AddComponent<Image>();
            closeBtnImg.color = new Color(0.12f, 0.18f, 0.28f, 1f);
            Outline btnOutline = closeBtn.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.0f, 0.9f, 0.8f, 1f);
            btnOutline.effectDistance = new Vector2(3f, -3f);

            Button btnComp = closeBtn.AddComponent<Button>();
            btnComp.onClick.AddListener(() => controller.CloseOptions());

            GameObject closeTxtObj = CreateUIObject("Text", closeBtn.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI closeTxtTmp = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTxtTmp.font = mainFont;
            closeTxtTmp.text = "«  BACK (ESC)  «";
            closeTxtTmp.fontSize = 20;
            closeTxtTmp.alignment = TextAlignmentOptions.Center;
            closeTxtTmp.color = Color.white;

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

        private static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool hasMenu = false;
            bool hasCombat = false;

            foreach (var s in scenes)
            {
                if (s.path == MAIN_MENU_SCENE_PATH) hasMenu = true;
                if (s.path == COMBAT_SCENE_PATH) hasCombat = true;
            }

            if (!hasMenu) scenes.Insert(0, new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true));
            if (!hasCombat && File.Exists(COMBAT_SCENE_PATH)) scenes.Add(new EditorBuildSettingsScene(COMBAT_SCENE_PATH, true));

            EditorBuildSettings.scenes = scenes.ToArray();
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
    }
}
#endif
