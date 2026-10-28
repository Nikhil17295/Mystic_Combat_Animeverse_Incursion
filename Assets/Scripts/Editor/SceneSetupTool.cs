#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using MysticCombat.Fighter;
using MysticCombat.Input;
using MysticCombat.Systems;
using MysticCombat.Combat;
using MysticCombat.Core;
using MysticCombat.UI;

namespace MysticCombat.Editor
{
    /// <summary>
    /// Bakes the complete 2D WYSIWYG Arena, Fighters, and Grand 20% Tournament Arcade HUD directly into SampleScene.unity and HUDScene.unity.
    /// Provides the authentic Godot-like visual editing experience where background,
    /// floor, boundary walls, characters, and the glowing Time Stone HUD are visible and editable in 2D mode 24/7.
    /// Converts fighters and HUD into true Unity Prefab Instances so developers can double-click them to edit in isolation.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSetupTool
    {
        private const int GROUND_LAYER = 6;
        private const string BAKE_VERSION_KEY = "MysticCombat_GrandHUD_Bake_v28";

        static SceneSetupTool()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(BAKE_VERSION_KEY, false) || GameObject.Find("HUD_Canvas") == null)
                {
                    EditorPrefs.SetBool(BAKE_VERSION_KEY, true);
                    BakeGodotStyleScene();
                }
            };
        }

        [MenuItem("Mystic Combat/Bake Complete Project (All Scenes & Prefabs)")]
        public static void BakeAll()
        {
            BakeDedicatedHUDScene();
            BakeGodotStyleScene();
        }

        [MenuItem("Mystic Combat/Bake 2D Visual Scene (Godot-Style)")]
        public static void BakeGodotStyleScene()
        {
            // 1. Ensure Textures are imported as Sprites
            SetupBackgroundTexture();
            SetupUITextures();
            SetupFighterTextures();

            string sampleScenePath = "Assets/Scenes/SampleScene.unity";
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != sampleScenePath)
            {
                if (File.Exists(sampleScenePath))
                {
                    scene = EditorSceneManager.OpenScene(sampleScenePath, OpenSceneMode.Single);
                }
            }

            Debug.Log($"<color=#ffaa00>[SceneSetupTool]</color> Baking visual 2D fighting scene and Grand Anime HUD into {scene.name}...");

            // 2. Setup Main Camera
            SetupCamera();

            // 3. Setup Arena Hierarchy (Backdrop, Floor, Invisible Boundary Walls)
            SetupArena();

            // 4. Ensure Standalone Fighter Prefabs are Generated in Assets/Prefabs and Assets/Resources
            FighterPrefabGenerator.GeneratePrefabs();

            // 5. Clean up any static baked fighters in the scene so dynamic character selection deploys cleanly!
            GameObject existingGoku = GameObject.Find("Fighter_P1_Goku");
            if (existingGoku != null) Object.DestroyImmediate(existingGoku);
            GameObject existingSaitama = GameObject.Find("Fighter_P2_Saitama");
            if (existingSaitama != null) Object.DestroyImmediate(existingSaitama);
            GameObject existingP1 = GameObject.Find("Fighter_P1");
            if (existingP1 != null) Object.DestroyImmediate(existingP1);
            GameObject existingP2 = GameObject.Find("Fighter_P2");
            if (existingP2 != null) Object.DestroyImmediate(existingP2);

            // 6. Setup MatchManager
            MatchManager mm = SetupMatchManager(null, null);

            // 7. Setup Grand Tournament Arcade HUD (Health, Ki, Heroic Avatars, Time Stone Mandala, Gems)
            GameObject hudCanvas = SetupHUD(null, null, mm);

            // 8. Ensure PrototypeBootstrap exists in the scene for dynamic fighter deployment
            GameObject bootstrap = GameObject.Find("[PrototypeBootstrap]");
            if (bootstrap == null)
            {
                bootstrap = new GameObject("[PrototypeBootstrap]");
                bootstrap.AddComponent<PrototypeBootstrap>();
            }

            // 9. Snap SceneView to 2D Mode
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.Repaint();
            }

            // 10. Save the Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#00e676>[SceneSetupTool] SUCCESS!</color> Grand 20% Anime HUD and Fighters baked into SampleScene.unity! Live Prefab Instances active.");
        }

        [MenuItem("Mystic Combat/Bake Dedicated HUD Scene (HUDScene.unity)")]
        public static void BakeDedicatedHUDScene()
        {
            Debug.Log("<color=#ffaa00>[SceneSetupTool]</color> Baking dedicated visual HUDScene.unity...");

            SetupUITextures();

            string scenePath = "Assets/Scenes/HUDScene.unity";
            Scene hudScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);

            // Add dimmed backdrop preview
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Arena/arena_stage_background.png");
            if (bgSprite != null)
            {
                GameObject bgObj = new GameObject("Backdrop_Preview");
                SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
                sr.sprite = bgSprite;
                sr.color = new Color(0.35f, 0.35f, 0.35f, 1f);
                bgObj.transform.position = new Vector3(0f, 0f, 10f);
            }

            // Setup the HUD
            SetupHUD(null, null, null);

            // Save Scene
            EditorSceneManager.SaveScene(hudScene, scenePath);
            Debug.Log("<color=#00e676>[SceneSetupTool] SUCCESS!</color> Dedicated HUD Scene created at Assets/Scenes/HUDScene.unity!");
        }

        private static void SetupFighterTextures()
        {
            if (!Directory.Exists("Assets/Sprites/Fighters")) return;

            string[] fighterFiles = Directory.GetFiles("Assets/Sprites/Fighters", "*.png", SearchOption.AllDirectories);
            bool changed = false;

            foreach (string path in fighterFiles)
            {
                string normalized = path.Replace("\\", "/");
                TextureImporter importer = AssetImporter.GetAtPath(normalized) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                    changed = true;
                }
            }

            if (changed)
            {
                AssetDatabase.Refresh();
            }
        }

        private static void SetupBackgroundTexture()
        {
            string bgPath = "Assets/Sprites/Arena/arena_stage_background.png";
            TextureImporter importer = AssetImporter.GetAtPath(bgPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static void SetupUITextures()
        {
            if (!Directory.Exists("Assets/Sprites/UI")) return;

            string[] uiFiles = Directory.GetFiles("Assets/Sprites/UI", "*.png");
            bool changed = false;

            foreach (string path in uiFiles)
            {
                string normalized = path.Replace("\\", "/");
                TextureImporter importer = AssetImporter.GetAtPath(normalized) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.spritePixelsPerUnit = 100;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                    changed = true;
                }
            }

            if (changed)
            {
                AssetDatabase.Refresh();
            }
        }

        private static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 3.2f;
            cam.transform.position = new Vector3(0f, -1.2f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.12f, 1f);
        }

        private static void SetupArena()
        {
            GameObject arena = GameObject.Find("Arena");
            if (arena != null) Object.DestroyImmediate(arena);

            arena = new GameObject("Arena");
            arena.transform.position = Vector3.zero;

            // A. Backdrop Sprite
            GameObject backdrop = new GameObject("Backdrop_TournamentStage");
            backdrop.transform.SetParent(arena.transform);
            backdrop.transform.position = new Vector3(0f, -0.6f, 0f);
            SpriteRenderer bgSr = backdrop.AddComponent<SpriteRenderer>();
            bgSr.sortingOrder = -10;

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Arena/arena_stage_background.png");
            if (bgSprite != null)
            {
                bgSr.sprite = bgSprite;
                backdrop.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            }

            // B. Floor (Layer 6 = Ground, Tag = Ground)
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(arena.transform);
            floor.transform.position = new Vector3(0f, -3.5f, 0f);
            floor.layer = GROUND_LAYER;
            try { floor.tag = "Ground"; } catch { }

            BoxCollider2D floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(24f, 1.0f);

            GameObject floorVisual = new GameObject("FloorVisual");
            floorVisual.transform.SetParent(floor.transform);
            floorVisual.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            SpriteRenderer floorSr = floorVisual.AddComponent<SpriteRenderer>();
            floorSr.sortingOrder = -5;
            floorSr.color = new Color(0.12f, 0.14f, 0.20f, 0.9f);
            floorVisual.transform.localScale = new Vector2(24f, 0.1f);

            // C. Invisible Boundary Walls
            GameObject leftWall = new GameObject("Left_InvisibleWall");
            leftWall.transform.SetParent(arena.transform);
            leftWall.transform.position = new Vector3(-5.7f, 0f, 0f);
            BoxCollider2D lwCol = leftWall.AddComponent<BoxCollider2D>();
            lwCol.size = new Vector2(1f, 14f);

            GameObject rightWall = new GameObject("Right_InvisibleWall");
            rightWall.transform.SetParent(arena.transform);
            rightWall.transform.position = new Vector3(5.7f, 0f, 0f);
            BoxCollider2D rwCol = rightWall.AddComponent<BoxCollider2D>();
            rwCol.size = new Vector2(1f, 14f);

            GameObject ceiling = new GameObject("Ceiling_InvisibleWall");
            ceiling.transform.SetParent(arena.transform);
            ceiling.transform.position = new Vector3(0f, 3.2f, 0f);
            BoxCollider2D cCol = ceiling.AddComponent<BoxCollider2D>();
            cCol.size = new Vector2(16f, 1f);
        }

        private static GameObject SetupGoku()
        {
            GameObject existing = GameObject.Find("Fighter_P1_Goku");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject fighter = new GameObject("Fighter_P1_Goku");
            fighter.transform.position = new Vector3(-2.2f, -3.0f, 0f);
            fighter.transform.localScale = new Vector3(0.88f, 0.88f, 1f);

            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = fighter.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 1.9f);
            col.offset = new Vector2(0f, 0.95f);

            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            Sprite idleGoku = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Fighters/Goku/Frames/idle_0.png");
            if (idleGoku != null) sr.sprite = idleGoku;

            PlayerInputHandler input = fighter.AddComponent<PlayerInputHandler>();
            input.playerIndex = PlayerInputHandler.PlayerIndex.P1;

            FighterController fc = fighter.AddComponent<FighterController>();
            fc.SetInputHandler(input);
            fc.SetGroundLayer(1 << GROUND_LAYER);

            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            fc.SetGroundCheckPoint(groundCheck.transform);

            FighterAnimator anim = fighter.AddComponent<FighterAnimator>();
            anim.LoadCharacterFrames("Goku");

            fighter.AddComponent<HealthSystem>();
            fighter.AddComponent<EnergySystem>();

            GameObject hurtboxObj = new GameObject("Hurtbox");
            hurtboxObj.transform.SetParent(fighter.transform);
            hurtboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hurtboxCol = hurtboxObj.AddComponent<BoxCollider2D>();
            hurtboxCol.isTrigger = true;
            hurtboxCol.size = new Vector2(0.85f, 1.8f);
            hurtboxCol.offset = new Vector2(0f, 0.95f);
            HurtboxController hurtbox = hurtboxObj.AddComponent<HurtboxController>();
            hurtbox.SetOwner(fighter.transform);

            GameObject hitboxObj = new GameObject("Hitbox");
            hitboxObj.transform.SetParent(fighter.transform);
            hitboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hitboxCol = hitboxObj.AddComponent<BoxCollider2D>();
            hitboxCol.isTrigger = true;
            hitboxCol.size = new Vector2(0.9f, 0.7f);
            hitboxCol.offset = new Vector2(0.8f, 0.85f);
            HitboxController hitbox = hitboxObj.AddComponent<HitboxController>();
            hitbox.Setup(fighter.transform, 10, 5f, 6f);
            fc.SetHitbox(hitbox);

            // Fighter Audio Component (State-driven authentic audio: whiffs, visceral hits, ki meditation hum)
            FighterAudio fa = SetupFighterAudio(fighter, "Goku");
            SerializedObject fcSo = new SerializedObject(fc);
            fcSo.FindProperty("fighterAudio").objectReferenceValue = fa;
            fcSo.ApplyModifiedProperties();

            // Connect as live Prefab
            string prefabDir = "Assets/Prefabs/Fighters";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/Goku.prefab";
            GameObject prefabInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(fighter, prefabPath, InteractionMode.AutomatedAction);
            return prefabInstance != null ? prefabInstance : fighter;
        }

        private static GameObject SetupSaitama()
        {
            GameObject existing = GameObject.Find("Fighter_P2_Saitama");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject fighter = new GameObject("Fighter_P2_Saitama");
            fighter.transform.position = new Vector3(2.2f, -3.0f, 0f);
            fighter.transform.localScale = new Vector3(1.12f, 1.12f, 1f);

            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = fighter.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 1.9f);
            col.offset = new Vector2(0f, 0.95f);

            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            Sprite idleSaitama = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Fighters/Saitama/Frames/idle_0.png");
            if (idleSaitama != null) sr.sprite = idleSaitama;

            PlayerInputHandler input = fighter.AddComponent<PlayerInputHandler>();
            input.playerIndex = PlayerInputHandler.PlayerIndex.P2;

            FighterController fc = fighter.AddComponent<FighterController>();
            fc.SetInputHandler(input);
            fc.SetGroundLayer(1 << GROUND_LAYER);

            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            fc.SetGroundCheckPoint(groundCheck.transform);

            FighterAnimator anim = fighter.AddComponent<FighterAnimator>();
            anim.LoadCharacterFrames("Saitama");

            fighter.AddComponent<HealthSystem>();
            fighter.AddComponent<EnergySystem>();

            GameObject hurtboxObj = new GameObject("Hurtbox");
            hurtboxObj.transform.SetParent(fighter.transform);
            hurtboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hurtboxCol = hurtboxObj.AddComponent<BoxCollider2D>();
            hurtboxCol.isTrigger = true;
            hurtboxCol.size = new Vector2(0.85f, 1.8f);
            hurtboxCol.offset = new Vector2(0f, 0.95f);
            HurtboxController hurtbox = hurtboxObj.AddComponent<HurtboxController>();
            hurtbox.SetOwner(fighter.transform);

            GameObject hitboxObj = new GameObject("Hitbox");
            hitboxObj.transform.SetParent(fighter.transform);
            hitboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hitboxCol = hitboxObj.AddComponent<BoxCollider2D>();
            hitboxCol.isTrigger = true;
            hitboxCol.size = new Vector2(0.9f, 0.7f);
            hitboxCol.offset = new Vector2(0.8f, 0.85f);
            HitboxController hitbox = hitboxObj.AddComponent<HitboxController>();
            hitbox.Setup(fighter.transform, 10, 5f, 6f);
            fc.SetHitbox(hitbox);

            // Fighter Audio Component (State-driven authentic audio: whiffs, visceral hits, ki meditation hum)
            FighterAudio fa = SetupFighterAudio(fighter, "Saitama");
            SerializedObject fcSo = new SerializedObject(fc);
            fcSo.FindProperty("fighterAudio").objectReferenceValue = fa;
            fcSo.ApplyModifiedProperties();

            // Connect as live Prefab
            string prefabDir = "Assets/Prefabs/Fighters";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = $"{prefabDir}/Saitama.prefab";
            GameObject prefabInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(fighter, prefabPath, InteractionMode.AutomatedAction);
            return prefabInstance != null ? prefabInstance : fighter;
        }

        private static MatchManager SetupMatchManager(GameObject goku, GameObject saitama)
        {
            MatchManager mm = Object.FindAnyObjectByType<MatchManager>();
            if (mm == null)
            {
                GameObject mmObj = new GameObject("MatchManager");
                mm = mmObj.AddComponent<MatchManager>();
            }

            if (goku != null && saitama != null)
            {
                mm.SetFighterReferences(
                    goku.GetComponent<HealthSystem>(),
                    saitama.GetComponent<HealthSystem>(),
                    goku.GetComponent<EnergySystem>(),
                    saitama.GetComponent<EnergySystem>()
                );
            }

            // Setup MatchAudioController (Unified Battle Theme across all 3 rounds & 3s Victory Fanfare)
            MatchAudioController mac = mm.GetComponent<MatchAudioController>();
            if (mac == null)
            {
                mac = mm.gameObject.AddComponent<MatchAudioController>();
            }

            AudioClip battleBgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/Double Dragon 2 - The Revenge (NES) Music - Final Battle 2.mp3");
            AudioClip winJingle = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/Double Dragon 3 - The Sacred Stone (NES) Music - Mission Clear.mp3");
            AudioClip roundStartBell = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_round_start_bell.ogg");
            if (roundStartBell == null) roundStartBell = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_round_start_bell.wav");
            AudioClip koBell = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_ko_bell.ogg");
            if (koBell == null) koBell = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_ko_bell.wav");

            SerializedObject macSo = new SerializedObject(mac);
            macSo.FindProperty("matchManager").objectReferenceValue = mm;
            macSo.FindProperty("battleMusic").objectReferenceValue = battleBgm;
            macSo.FindProperty("victoryJingle").objectReferenceValue = winJingle;
            macSo.FindProperty("roundStartBellClip").objectReferenceValue = roundStartBell;
            macSo.FindProperty("koBellClip").objectReferenceValue = koBell;
            macSo.FindProperty("battleMusicVolume").floatValue = 0.50f;
            macSo.FindProperty("victoryJingleVolume").floatValue = 0.85f;
            macSo.FindProperty("bellVolume").floatValue = 1.0f;
            macSo.FindProperty("musicStartDelayAfterBell").floatValue = 0.85f;
            macSo.ApplyModifiedProperties();

            return mm;
        }

        private static FighterAudio SetupFighterAudio(GameObject fighter, string characterName)
        {
            FighterAudio fa = fighter.GetComponent<FighterAudio>();
            if (fa == null)
            {
                fa = fighter.AddComponent<FighterAudio>();
            }

            SerializedObject so = new SerializedObject(fa);

            // Common / Shared Agility Clips
            AudioClip jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Swishes_Whiffs/swish_01.wav");
            AudioClip landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Anime_Powers/jumpland_clean.wav");
            AudioClip crouchClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Swishes_Whiffs/swish_05.wav");
            AudioClip dashClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Anime_Powers/teleport_dash.wav");
            AudioClip kiAuraClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Anime_Powers/dbz_super_saiyan_aura.mp3");
            AudioClip redZoneClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Hits_Punches/punch_foley_punch_1.wav");

            so.FindProperty("jumpClip").objectReferenceValue = jumpClip;
            so.FindProperty("landClip").objectReferenceValue = landClip;
            so.FindProperty("crouchClip").objectReferenceValue = crouchClip;
            so.FindProperty("dashClip").objectReferenceValue = dashClip;
            so.FindProperty("kiChargeLoopClip").objectReferenceValue = kiAuraClip;
            so.FindProperty("redZoneWarningClip").objectReferenceValue = redZoneClip;

            if (characterName == "Goku")
            {
                // Goku Audio Profile: Martial arts swishes, real martial arts light punch, and heavy smash
                so.FindProperty("lightWhiffClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Swishes_Whiffs/swish_13.wav");
                so.FindProperty("heavyWhiffClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Swishes_Whiffs/swish_09.wav");
                so.FindProperty("skillAttackClip").objectReferenceValue = null;
                so.FindProperty("ultimateAttackClip").objectReferenceValue = null;
                so.FindProperty("lightHitClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Hits_Punches/arcade_hit_01.ogg");
                so.FindProperty("heavyHitClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Hits_Punches/arcade_hit_04.ogg");
                so.FindProperty("skillHitClip").objectReferenceValue = null;
                so.FindProperty("ultimateHitClip").objectReferenceValue = null;
                // Goku has NO skill voice
                so.FindProperty("voiceSkillClip").objectReferenceValue = null;
                // Ultimate (Q / O): Kamehameha!
                so.FindProperty("voiceUltimateClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Anime_Powers/goku_voice_kamehameha.wav");
                so.FindProperty("voiceSkillMultiplier").floatValue = 1.0f;
                so.FindProperty("voiceUltimateMultiplier").floatValue = 0.50f;
            }
            else // Saitama
            {
                // Saitama Audio Profile: Fast flurry swishes, crisp punch smack, and normal punch smash
                so.FindProperty("lightWhiffClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Swishes_Whiffs/swish_11.wav");
                so.FindProperty("heavyWhiffClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Swishes_Whiffs/swish_06.wav");
                so.FindProperty("skillAttackClip").objectReferenceValue = null;
                so.FindProperty("ultimateAttackClip").objectReferenceValue = null;
                so.FindProperty("lightHitClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Hits_Punches/arcade_hit_01.ogg");
                so.FindProperty("heavyHitClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Hits_Punches/saitama_normal_punch.mp3");
                so.FindProperty("skillHitClip").objectReferenceValue = null;
                so.FindProperty("ultimateHitClip").objectReferenceValue = null;
                // Skill (E / U): Consecutive Normal Punches
                so.FindProperty("voiceSkillClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Anime_Powers/consecutive_normal_punches.mp3");
                // Ultimate (Q / O): Serious Punch
                so.FindProperty("voiceUltimateClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Library/Anime_Powers/serious_punch.mp3");
                so.FindProperty("voiceSkillMultiplier").floatValue = 2.20f;
                so.FindProperty("voiceUltimateMultiplier").floatValue = 2.50f;
            }

            so.FindProperty("sfxVolume").floatValue = 1.0f;
            so.FindProperty("whiffVolume").floatValue = 0.65f;
            so.FindProperty("impactVolume").floatValue = 1.0f;
            so.FindProperty("voiceVolume").floatValue = 1.0f;
            so.FindProperty("kiAuraVolume").floatValue = 0.12f;
            so.ApplyModifiedProperties();
            return fa;
        }

        private static GameObject SetupHUD(GameObject goku, GameObject saitama, MatchManager mm)
        {
            // 1. Destroy existing HUD Canvas
            GameObject existingCanvas = GameObject.Find("HUD_Canvas");
            if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);

            // 2. Setup Canvas
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 3. Load UI Sprites
            Sprite avatarGoku = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/goku_avatar_heroic.png");
            Sprite avatarSaitama = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/saitama_avatar_heroic.png");

            Sprite healthBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/health_bar_grand_bg.png");
            Sprite healthFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/health_bar_grand_fill.png");
            Sprite healthGhost = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/health_bar_grand_ghost.png");

            Sprite runicBanner = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/mystic_runic_banner.png");

            Sprite kiBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/ki_bar_grand_bg.png");
            Sprite kiFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/ki_bar_grand_fill.png");

            Sprite timeMandala = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/time_stone_mandala.png");
            Sprite timeGem = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/timestone_gem_emerald.png");

            Sprite gemBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/gem_shiny_blue.png");
            Sprite gemSocket = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/gem_socket_hollow.png");
            Sprite gemBigGold = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/gem_big_gold.png");
            Sprite gemBigGoldHollow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/gem_big_gold_hollow.png");

            // 4. Create InGameHUD Root
            GameObject hudRoot = new GameObject("InGameHUD");
            hudRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform hudRt = hudRoot.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.sizeDelta = Vector2.zero;
            HUDController hudCtrl = hudRoot.AddComponent<HUDController>();

            // Mystic Doctor Strange Spell Banner spanning center top
            if (runicBanner != null)
            {
                // Positioned visibly across the top header border
                GameObject bannerObj = CreateUIObject("Mystic_Runic_Banner", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(960f, 36f));
                Image bannerImg = bannerObj.AddComponent<Image>();
                bannerImg.sprite = runicBanner;
                bannerImg.raycastTarget = false;
            }

            // ----------------------------------------------------
            // 5. PLAYER 1 (GOKU) - TOP LEFT CLUSTER (~20% SCREEN COVERAGE)
            // ----------------------------------------------------
            // P1 Heroic Bust Card (160x160 with Designer Dragon Filigree & Anime Burst)
            GameObject p1CardObj = CreateUIObject("P1_AvatarCard", hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(100f, -100f), new Vector2(160f, 160f));
            Image p1CardImg = p1CardObj.AddComponent<Image>();
            p1CardImg.sprite = avatarGoku;

            // P1 Name Tag ("GOKU") with 3D Super Saiyan Flame Gradient
            GameObject p1NameObj = CreateUIObject("P1_Name", hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(195f, -38f), new Vector2(300f, 40f), new Vector2(0f, 0.5f));
            TextMeshProUGUI p1NameTmp = p1NameObj.AddComponent<TextMeshProUGUI>();
            p1NameTmp.text = "GOKU";
            p1NameTmp.fontSize = 36;
            p1NameTmp.fontStyle = FontStyles.Bold;
            p1NameTmp.alignment = TextAlignmentOptions.Left;
            p1NameTmp.enableVertexGradient = true;
            p1NameTmp.colorGradient = new VertexGradient(
                new Color(1f, 0.96f, 0.45f, 1f),  // Top-Left: Radiant Sun Gold
                new Color(1f, 0.85f, 0.20f, 1f),  // Top-Right: Pure Super Saiyan Gold
                new Color(1f, 0.45f, 0.05f, 1f),  // Bottom-Left: Blazing Amber Orange
                new Color(0.85f, 0.15f, 0.00f, 1f) // Bottom-Right: Deep Dragon Crimson
            );

            // P1 Heavy Health Bar Container (650x52 with 8px Beveled Rim)
            GameObject p1HpContainer = CreateUIObject("P1_HealthBar_Container", hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(195f, -80f), new Vector2(650f, 52f), new Vector2(0f, 0.5f));
            Image p1HpBg = p1HpContainer.AddComponent<Image>();
            p1HpBg.sprite = healthBg;

            // P1 Ghost Damage Bar
            GameObject p1GhostObj = CreateUIObject("GhostFill", p1HpContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform p1GhostRt = p1GhostObj.GetComponent<RectTransform>();
            p1GhostRt.offsetMin = new Vector2(8f, 6f);
            p1GhostRt.offsetMax = new Vector2(-8f, -6f);
            Image p1GhostImg = p1GhostObj.AddComponent<Image>();
            p1GhostImg.sprite = healthGhost;
            p1GhostImg.type = Image.Type.Filled;
            p1GhostImg.fillMethod = Image.FillMethod.Horizontal;
            p1GhostImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            p1GhostImg.fillAmount = 1f;

            // P1 Active Health Bar
            GameObject p1HpObj = CreateUIObject("HealthFill", p1HpContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform p1HpRt = p1HpObj.GetComponent<RectTransform>();
            p1HpRt.offsetMin = new Vector2(8f, 6f);
            p1HpRt.offsetMax = new Vector2(-8f, -6f);
            Image p1HpImg = p1HpObj.AddComponent<Image>();
            p1HpImg.sprite = healthFill;
            p1HpImg.type = Image.Type.Filled;
            p1HpImg.fillMethod = Image.FillMethod.Horizontal;
            p1HpImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            p1HpImg.fillAmount = 1f;

            // P1 Ki Bar Container (520x24)
            GameObject p1KiContainer = CreateUIObject("P1_KiBar_Container", hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(195f, -126f), new Vector2(520f, 24f), new Vector2(0f, 0.5f));
            Image p1KiBg = p1KiContainer.AddComponent<Image>();
            p1KiBg.sprite = kiBg;

            // P1 Ki Fill
            GameObject p1KiObj = CreateUIObject("KiFill", p1KiContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform p1KiRt = p1KiObj.GetComponent<RectTransform>();
            p1KiRt.offsetMin = new Vector2(3f, 3f);
            p1KiRt.offsetMax = new Vector2(-3f, -3f);
            Image p1KiImg = p1KiObj.AddComponent<Image>();
            p1KiImg.sprite = kiFill;
            p1KiImg.type = Image.Type.Filled;
            p1KiImg.fillMethod = Image.FillMethod.Horizontal;
            p1KiImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            p1KiImg.fillAmount = 0.5f;

            // P1 Round Point Gem Sockets (Tactical Visual Point Tracker - 2 Sockets, 56x56 each)
            GameObject p1PointGem1Obj = CreateUIObject("P1_PointGem_1", hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(215f, -168f), new Vector2(56f, 56f));
            Image p1PointGem1Img = p1PointGem1Obj.AddComponent<Image>();
            p1PointGem1Img.sprite = gemSocket;

            GameObject p1PointGem2Obj = CreateUIObject("P1_PointGem_2", hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -168f), new Vector2(56f, 56f));
            Image p1PointGem2Img = p1PointGem2Obj.AddComponent<Image>();
            p1PointGem2Img.sprite = gemSocket;

            // ----------------------------------------------------
            // 6. PLAYER 2 (SAITAMA) - TOP RIGHT CLUSTER (MIRRORED)
            // ----------------------------------------------------
            // P2 Heroic Bust Card (160x160 with Designer Dragon Filigree & Kinetic Burst)
            GameObject p2CardObj = CreateUIObject("P2_AvatarCard", hudRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-100f, -100f), new Vector2(160f, 160f));
            Image p2CardImg = p2CardObj.AddComponent<Image>();
            p2CardImg.sprite = avatarSaitama;

            // P2 Name Tag ("SAITAMA") with 3D Hero Crimson Gradient
            GameObject p2NameObj = CreateUIObject("P2_Name", hudRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-195f, -38f), new Vector2(300f, 40f), new Vector2(1f, 0.5f));
            TextMeshProUGUI p2NameTmp = p2NameObj.AddComponent<TextMeshProUGUI>();
            p2NameTmp.text = "SAITAMA";
            p2NameTmp.fontSize = 36;
            p2NameTmp.fontStyle = FontStyles.Bold;
            p2NameTmp.alignment = TextAlignmentOptions.Right;
            p2NameTmp.enableVertexGradient = true;
            p2NameTmp.colorGradient = new VertexGradient(
                new Color(1f, 0.94f, 0.94f, 1f),  // Top-Left: Hero White-Gold Glow
                new Color(1f, 0.75f, 0.75f, 1f),  // Top-Right: Radiant Highlight
                new Color(1f, 0.20f, 0.30f, 1f),  // Bottom-Left: Punch Crimson
                new Color(0.55f, 0.00f, 0.12f, 1f) // Bottom-Right: Deep Hero Maroon
            );

            // P2 Heavy Health Bar Container (650x52 with 8px Beveled Rim)
            GameObject p2HpContainer = CreateUIObject("P2_HealthBar_Container", hudRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-195f, -80f), new Vector2(650f, 52f), new Vector2(1f, 0.5f));
            Image p2HpBg = p2HpContainer.AddComponent<Image>();
            p2HpBg.sprite = healthBg;

            // P2 Ghost Damage Bar
            GameObject p2GhostObj = CreateUIObject("GhostFill", p2HpContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform p2GhostRt = p2GhostObj.GetComponent<RectTransform>();
            p2GhostRt.offsetMin = new Vector2(8f, 6f);
            p2GhostRt.offsetMax = new Vector2(-8f, -6f);
            Image p2GhostImg = p2GhostObj.AddComponent<Image>();
            p2GhostImg.sprite = healthGhost;
            p2GhostImg.type = Image.Type.Filled;
            p2GhostImg.fillMethod = Image.FillMethod.Horizontal;
            p2GhostImg.fillOrigin = (int)Image.OriginHorizontal.Right;
            p2GhostImg.fillAmount = 1f;

            // P2 Active Health Bar
            GameObject p2HpObj = CreateUIObject("HealthFill", p2HpContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform p2HpRt = p2HpObj.GetComponent<RectTransform>();
            p2HpRt.offsetMin = new Vector2(8f, 6f);
            p2HpRt.offsetMax = new Vector2(-8f, -6f);
            Image p2HpImg = p2HpObj.AddComponent<Image>();
            p2HpImg.sprite = healthFill;
            p2HpImg.type = Image.Type.Filled;
            p2HpImg.fillMethod = Image.FillMethod.Horizontal;
            p2HpImg.fillOrigin = (int)Image.OriginHorizontal.Right;
            p2HpImg.fillAmount = 1f;

            // P2 Ki Bar Container (520x24)
            GameObject p2KiContainer = CreateUIObject("P2_KiBar_Container", hudRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-195f, -126f), new Vector2(520f, 24f), new Vector2(1f, 0.5f));
            Image p2KiBg = p2KiContainer.AddComponent<Image>();
            p2KiBg.sprite = kiBg;

            // P2 Ki Fill
            GameObject p2KiObj = CreateUIObject("KiFill", p2KiContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform p2KiRt = p2KiObj.GetComponent<RectTransform>();
            p2KiRt.offsetMin = new Vector2(3f, 3f);
            p2KiRt.offsetMax = new Vector2(-3f, -3f);
            Image p2KiImg = p2KiObj.AddComponent<Image>();
            p2KiImg.sprite = kiFill;
            p2KiImg.type = Image.Type.Filled;
            p2KiImg.fillMethod = Image.FillMethod.Horizontal;
            p2KiImg.fillOrigin = (int)Image.OriginHorizontal.Right;
            p2KiImg.fillAmount = 0.5f;

            // P2 Round Point Gem Sockets (Tactical Visual Point Tracker - 2 Sockets, 56x56 each)
            GameObject p2PointGem1Obj = CreateUIObject("P2_PointGem_1", hudRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-215f, -168f), new Vector2(56f, 56f));
            Image p2PointGem1Img = p2PointGem1Obj.AddComponent<Image>();
            p2PointGem1Img.sprite = gemSocket;

            GameObject p2PointGem2Obj = CreateUIObject("P2_PointGem_2", hudRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-280f, -168f), new Vector2(56f, 56f));
            Image p2PointGem2Img = p2PointGem2Obj.AddComponent<Image>();
            p2PointGem2Img.sprite = gemSocket;

            // ----------------------------------------------------
            // 7. CENTER DOCTOR STRANGE TIME MANDALA & BIG GOLD GEM (NO BOX!)
            // ----------------------------------------------------
            // Rotating Green Mystic Time Mandala (160x160)
            GameObject mandalaObj = CreateUIObject("TimeStone_Mandala", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -85f), new Vector2(160f, 160f));
            Image mandalaImg = mandalaObj.AddComponent<Image>();
            mandalaImg.sprite = timeMandala;
            RectTransform mandalaRt = mandalaObj.GetComponent<RectTransform>();

            // Radiant Emerald Time Stone at Mandala Apex (Enlarged 56x56)
            GameObject timeStoneObj = CreateUIObject("TimeStone_Gem", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(56f, 56f));
            Image timeStoneImg = timeStoneObj.AddComponent<Image>();
            timeStoneImg.sprite = timeGem;

            // Radiant Countdown Numerals ("99") with 3D Emerald Gradient
            GameObject timerTextObj = CreateUIObject("TimerText", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(140f, 54f));
            TextMeshProUGUI timerTmp = timerTextObj.AddComponent<TextMeshProUGUI>();
            timerTmp.text = "99";
            timerTmp.fontSize = 50;
            timerTmp.fontStyle = FontStyles.Bold;
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.enableVertexGradient = true;
            timerTmp.colorGradient = new VertexGradient(
                new Color(0.92f, 1f, 0.98f, 1f),  // Top-Left: Emerald Ice
                new Color(0.80f, 1f, 0.92f, 1f),  // Top-Right: Mystic Mint
                new Color(0.00f, 0.95f, 0.55f, 1f), // Bottom-Left: Agamotto Green
                new Color(0.00f, 0.70f, 0.35f, 1f)  // Bottom-Right: Deep Jade
            );

            // Center Big Gem (Round socket, ignites into round gold gem on Match Point - Enlarged 68x68)
            GameObject goldGemObj = CreateUIObject("Center_BigGoldGem", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -168f), new Vector2(68f, 68f));
            Image goldGemImg = goldGemObj.AddComponent<Image>();
            goldGemImg.sprite = gemBigGoldHollow != null ? gemBigGoldHollow : gemSocket;

            // Round Title ("ROUND 1") with 3D Antique Gold Gradient (Wider 600px, single-line, bold 24pt)
            GameObject roundLabelObj = CreateUIObject("RoundTitle", hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -216f), new Vector2(600f, 36f));
            TextMeshProUGUI roundTmp = roundLabelObj.AddComponent<TextMeshProUGUI>();
            roundTmp.text = "ROUND 1";
            roundTmp.fontSize = 24;
            roundTmp.fontStyle = FontStyles.Bold;
            roundTmp.alignment = TextAlignmentOptions.Center;
            roundTmp.textWrappingMode = TextWrappingModes.NoWrap;
            roundTmp.overflowMode = TextOverflowModes.Overflow;
            roundTmp.enableVertexGradient = true;
            roundTmp.colorGradient = new VertexGradient(
                new Color(1f, 0.98f, 0.88f, 1f),
                new Color(1f, 0.92f, 0.70f, 1f),
                new Color(1f, 0.75f, 0.15f, 1f),
                new Color(0.85f, 0.50f, 0.05f, 1f)
            );

            // ----------------------------------------------------
            // 8. WIRE SERIALIZED PROPERTIES ON HUD CONTROLLER
            // ----------------------------------------------------
            SerializedObject so = new SerializedObject(hudCtrl);
            so.FindProperty("p1HealthBar").objectReferenceValue = p1HpImg;
            so.FindProperty("p1GhostHealthBar").objectReferenceValue = p1GhostImg;
            so.FindProperty("p1KiBar").objectReferenceValue = p1KiImg;
            so.FindProperty("p1PointGem1").objectReferenceValue = p1PointGem1Img;
            so.FindProperty("p1PointGem2").objectReferenceValue = p1PointGem2Img;

            so.FindProperty("p2HealthBar").objectReferenceValue = p2HpImg;
            so.FindProperty("p2GhostHealthBar").objectReferenceValue = p2GhostImg;
            so.FindProperty("p2KiBar").objectReferenceValue = p2KiImg;
            so.FindProperty("p2PointGem1").objectReferenceValue = p2PointGem1Img;
            so.FindProperty("p2PointGem2").objectReferenceValue = p2PointGem2Img;

            so.FindProperty("timeStoneMandala").objectReferenceValue = mandalaRt;
            so.FindProperty("timeStoneGem").objectReferenceValue = timeStoneImg;
            so.FindProperty("timerText").objectReferenceValue = timerTmp;
            so.FindProperty("roundTitleText").objectReferenceValue = roundTmp;
            so.FindProperty("centerBigGoldGem").objectReferenceValue = goldGemImg;

            so.FindProperty("gemShinyBlue").objectReferenceValue = gemBlue;
            so.FindProperty("gemHollowSocket").objectReferenceValue = gemSocket;
            so.FindProperty("gemBigGold").objectReferenceValue = gemBigGold;
            so.FindProperty("gemBigGoldHollow").objectReferenceValue = gemBigGoldHollow;

            // Keep scene fighter fields NULL in the prefab asset to prevent saving dead asset references
            so.FindProperty("p1Health").objectReferenceValue = null;
            so.FindProperty("p1Energy").objectReferenceValue = null;
            so.FindProperty("p2Health").objectReferenceValue = null;
            so.FindProperty("p2Energy").objectReferenceValue = null;
            so.FindProperty("matchManager").objectReferenceValue = null;

            so.ApplyModifiedProperties();

            // ----------------------------------------------------
            // 8b. ATTACH COMBAT MENU CONTROLLER (PAUSE & RESULTS)
            // ----------------------------------------------------
            CombatMenuController menuCtrl = canvasObj.GetComponent<CombatMenuController>();
            if (menuCtrl == null) menuCtrl = canvasObj.AddComponent<CombatMenuController>();
            menuCtrl.EnsureUIHierarchyBuilt();

            // ----------------------------------------------------
            // 9. SAVE AS PREFAB AND CONNECT AS LIVE INSTANCE
            // ----------------------------------------------------
            string uiDir = "Assets/Prefabs/UI";
            if (!Directory.Exists(uiDir)) Directory.CreateDirectory(uiDir);
            string hudPrefabPath = $"{uiDir}/HUD_Canvas.prefab";
            GameObject canvasInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObj, hudPrefabPath, InteractionMode.AutomatedAction);

            // On the LIVE SCENE INSTANCE, bind directly to the active scene fighters and record prefab overrides
            HUDController liveCtrl = canvasInstance != null ? canvasInstance.GetComponentInChildren<HUDController>() : hudCtrl;
            if (liveCtrl != null && goku != null && saitama != null)
            {
                SerializedObject sceneSo = new SerializedObject(liveCtrl);
                sceneSo.FindProperty("p1Health").objectReferenceValue = goku.GetComponent<HealthSystem>();
                sceneSo.FindProperty("p1Energy").objectReferenceValue = goku.GetComponent<EnergySystem>();
                sceneSo.FindProperty("p2Health").objectReferenceValue = saitama.GetComponent<HealthSystem>();
                sceneSo.FindProperty("p2Energy").objectReferenceValue = saitama.GetComponent<EnergySystem>();
                if (mm != null) sceneSo.FindProperty("matchManager").objectReferenceValue = mm;
                sceneSo.ApplyModifiedProperties();

                PrefabUtility.RecordPrefabInstancePropertyModifications(liveCtrl);
                liveCtrl.ResolveReferences();
                EditorUtility.SetDirty(liveCtrl);
            }

            // Wire CombatMenuController matchManager reference on live instance
            CombatMenuController liveMenu = canvasInstance != null ? canvasInstance.GetComponent<CombatMenuController>() : menuCtrl;
            if (liveMenu != null && mm != null)
            {
                SerializedObject menuSo = new SerializedObject(liveMenu);
                menuSo.FindProperty("matchManager").objectReferenceValue = mm;
                menuSo.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(liveMenu);
                EditorUtility.SetDirty(liveMenu);
            }

            return canvasInstance != null ? canvasInstance : canvasObj;
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2? pivot = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (pivot.HasValue) rt.pivot = pivot.Value;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }
    }
}
#endif
