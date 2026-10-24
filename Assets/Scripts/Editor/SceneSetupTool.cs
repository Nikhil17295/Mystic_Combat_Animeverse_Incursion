#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MysticCombat.Fighter;
using MysticCombat.Input;
using MysticCombat.Systems;
using MysticCombat.Combat;
using MysticCombat.Core;

namespace MysticCombat.Editor
{
    /// <summary>
    /// Bakes the complete 2D WYSIWYG Arena and Fighters directly into SampleScene.unity.
    /// Provides the authentic Godot-like visual editing experience where background,
    /// floor, boundary walls, and characters are visible and editable in 2D mode 24/7.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSetupTool
    {
        private const int GROUND_LAYER = 6;

        static SceneSetupTool()
        {
            EditorApplication.delayCall += () =>
            {
                // Auto-bake if scene is currently missing the arena or fighters
                if (GameObject.Find("Arena") == null || GameObject.Find("Fighter_P1_Goku") == null)
                {
                    BakeGodotStyleScene();
                }
            };
        }

        [MenuItem("Mystic Combat/Bake 2D Visual Scene (Godot-Style)")]
        public static void BakeGodotStyleScene()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            Debug.Log($"<color=#ffaa00>[SceneSetupTool]</color> Baking visual 2D fighting scene into {scene.name}...");

            // 1. Ensure Background texture is imported as Sprite
            SetupBackgroundTexture();

            // 2. Setup Main Camera
            SetupCamera();

            // 3. Setup Arena Hierarchy (Backdrop, Floor, Invisible Boundary Walls)
            SetupArena();

            // 4. Setup Fighters with Balanced Scales
            GameObject goku = SetupGoku();
            GameObject saitama = SetupSaitama();

            // 5. Wire Auto-Facing Opponent References
            FighterController gokuFc = goku.GetComponent<FighterController>();
            FighterController saitamaFc = saitama.GetComponent<FighterController>();
            gokuFc.opponent = saitama.transform;
            saitamaFc.opponent = goku.transform;

            // 6. Setup MatchManager
            SetupMatchManager(goku, saitama);

            // 7. Snap SceneView to 2D Mode
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.Repaint();
            }

            // 8. Save the Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#00e676>[SceneSetupTool] SUCCESS!</color> Visual 2D Scene baked into SampleScene.unity! Goku, Saitama, Arena, and Invisible Walls are now fully visible on screen.");
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

        private static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
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
                // Scale 1376x768 texture to fit camera
                backdrop.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            }

            // B. Floor (Layer 6 = Ground, Tag = Ground)
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(arena.transform);
            floor.transform.position = new Vector3(0f, -3.5f, 0f);
            floor.layer = GROUND_LAYER;
            try { floor.tag = "Ground"; } catch { }

            BoxCollider2D floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(24f, 1.0f); // Top of floor at y = -3.0

            // Dark floor line visual
            GameObject floorVisual = new GameObject("FloorVisual");
            floorVisual.transform.SetParent(floor.transform);
            floorVisual.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            SpriteRenderer floorSr = floorVisual.AddComponent<SpriteRenderer>();
            floorSr.sortingOrder = -5;
            floorSr.color = new Color(0.12f, 0.14f, 0.20f, 0.9f);
            floorVisual.transform.localScale = new Vector2(24f, 0.1f);

            // C. Invisible Boundary Walls (Contain fighters within 16:9 view)
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
            fighter.transform.position = new Vector3(-2.0f, -3.0f, 0f);
            // Compact Goku athletic scale (multiplies sprite & hitboxes)
            fighter.transform.localScale = new Vector3(0.88f, 0.88f, 1f);

            // Rigidbody2D
            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // BoxCollider2D (World collision)
            BoxCollider2D col = fighter.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 1.9f);
            col.offset = new Vector2(0f, 0.95f);

            // SpriteRenderer
            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            // Input
            PlayerInputHandler input = fighter.AddComponent<PlayerInputHandler>();
            input.playerIndex = PlayerInputHandler.PlayerIndex.P1;

            // Controller
            FighterController fc = fighter.AddComponent<FighterController>();
            fc.SetInputHandler(input);
            fc.SetGroundLayer(1 << GROUND_LAYER);

            // GroundCheck
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            fc.SetGroundCheckPoint(groundCheck.transform);

            // Animator (Loads Goku frames live in editor)
            FighterAnimator anim = fighter.AddComponent<FighterAnimator>();
            anim.LoadCharacterFrames("Goku");

            // Systems
            fighter.AddComponent<HealthSystem>();
            fighter.AddComponent<EnergySystem>();

            // Hurtbox child (Green Scene Gizmo)
            GameObject hurtboxObj = new GameObject("Hurtbox");
            hurtboxObj.transform.SetParent(fighter.transform);
            hurtboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hurtboxCol = hurtboxObj.AddComponent<BoxCollider2D>();
            hurtboxCol.isTrigger = true;
            hurtboxCol.size = new Vector2(0.85f, 1.8f);
            hurtboxCol.offset = new Vector2(0f, 0.95f);
            HurtboxController hurtbox = hurtboxObj.AddComponent<HurtboxController>();
            hurtbox.SetOwner(fighter.transform);

            // Hitbox child (Red Scene Gizmo)
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

            return fighter;
        }

        private static GameObject SetupSaitama()
        {
            GameObject existing = GameObject.Find("Fighter_P2_Saitama");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject fighter = new GameObject("Fighter_P2_Saitama");
            fighter.transform.position = new Vector3(2.0f, -3.0f, 0f);
            // Heroic tall Saitama scale (multiplies sprite & hitboxes)
            fighter.transform.localScale = new Vector3(1.12f, 1.12f, 1f);

            // Rigidbody2D
            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // BoxCollider2D (World collision)
            BoxCollider2D col = fighter.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 1.9f);
            col.offset = new Vector2(0f, 0.95f);

            // SpriteRenderer
            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            // Input
            PlayerInputHandler input = fighter.AddComponent<PlayerInputHandler>();
            input.playerIndex = PlayerInputHandler.PlayerIndex.P2;

            // Controller
            FighterController fc = fighter.AddComponent<FighterController>();
            fc.SetInputHandler(input);
            fc.SetGroundLayer(1 << GROUND_LAYER);

            // GroundCheck
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            fc.SetGroundCheckPoint(groundCheck.transform);

            // Animator (Loads Saitama frames live in editor)
            FighterAnimator anim = fighter.AddComponent<FighterAnimator>();
            anim.LoadCharacterFrames("Saitama");

            // Systems
            fighter.AddComponent<HealthSystem>();
            fighter.AddComponent<EnergySystem>();

            // Hurtbox child (Green Scene Gizmo)
            GameObject hurtboxObj = new GameObject("Hurtbox");
            hurtboxObj.transform.SetParent(fighter.transform);
            hurtboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hurtboxCol = hurtboxObj.AddComponent<BoxCollider2D>();
            hurtboxCol.isTrigger = true;
            hurtboxCol.size = new Vector2(0.85f, 1.8f);
            hurtboxCol.offset = new Vector2(0f, 0.95f);
            HurtboxController hurtbox = hurtboxObj.AddComponent<HurtboxController>();
            hurtbox.SetOwner(fighter.transform);

            // Hitbox child (Red Scene Gizmo)
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

            return fighter;
        }

        private static void SetupMatchManager(GameObject goku, GameObject saitama)
        {
            MatchManager mm = Object.FindAnyObjectByType<MatchManager>();
            if (mm == null)
            {
                GameObject mmObj = new GameObject("MatchManager");
                mm = mmObj.AddComponent<MatchManager>();
            }

            mm.SetFighterReferences(
                goku.GetComponent<HealthSystem>(),
                saitama.GetComponent<HealthSystem>(),
                goku.GetComponent<EnergySystem>(),
                saitama.GetComponent<EnergySystem>()
            );
        }
    }
}
#endif
