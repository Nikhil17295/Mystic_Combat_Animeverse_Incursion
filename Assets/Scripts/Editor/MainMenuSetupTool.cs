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
        private const string MAIN_MENU_BAKE_KEY = "MysticCombat_MainMenu_Bake_v8";

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
            // 11. WIRE PROPERTIES ON MAINMENUCONTROLLER
            // ----------------------------------------------------
            SerializedObject so = new SerializedObject(menuController);
            so.FindProperty("menuRoot").objectReferenceValue = menuRoot;
            so.FindProperty("introRoot").objectReferenceValue = introRoot;
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
