#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using MysticCombat.Fighter;
using MysticCombat.Input;
using MysticCombat.Systems;
using MysticCombat.Combat;

namespace MysticCombat.Editor
{
    /// <summary>
    /// Generates standalone Goku.prefab and Saitama.prefab assets into Assets/Prefabs/Fighters/.
    /// Gives teammates the exact Godot-like '.tscn' Inspector experience to permanently adjust
    /// Hitboxes, Hurtboxes, stats, and animation frame rates before running the game.
    /// </summary>
    [InitializeOnLoad]
    public static class FighterPrefabGenerator
    {
        private const string PREFAB_DIR = "Assets/Prefabs/Fighters";
        private const string GOKU_PREFAB_PATH = "Assets/Prefabs/Fighters/Goku.prefab";
        private const string SAITAMA_PREFAB_PATH = "Assets/Prefabs/Fighters/Saitama.prefab";

                private const string NEZUKO_PREFAB_PATH = "Assets/Prefabs/Fighters/Nezuko.prefab";
        private const string TSUNADE_PREFAB_PATH = "Assets/Prefabs/Fighters/Tsunade.prefab";
private const string RESOURCES_DIR = "Assets/Resources/Prefabs/Fighters";
        private const string GOKU_RESOURCE_PATH = "Assets/Resources/Prefabs/Fighters/Goku.prefab";
        private const string SAITAMA_RESOURCE_PATH = "Assets/Resources/Prefabs/Fighters/Saitama.prefab";

                private const string NEZUKO_RESOURCE_PATH = "Assets/Resources/Prefabs/Fighters/Nezuko.prefab";
        private const string TSUNADE_RESOURCE_PATH = "Assets/Resources/Prefabs/Fighters/Tsunade.prefab";
static FighterPrefabGenerator()
        {
            EditorApplication.delayCall += EnsurePrefabsExist;
        }

        private static void EnsurePrefabsExist()
        {
            if (!File.Exists(GOKU_PREFAB_PATH) || !File.Exists(SAITAMA_PREFAB_PATH) || !File.Exists(NEZUKO_PREFAB_PATH) || !File.Exists(TSUNADE_PREFAB_PATH))
            {
                GeneratePrefabs();
            }
        }

        [MenuItem("Mystic Combat/Generate Fighter Prefabs")]
        public static void GeneratePrefabs()
        {
            if (!Directory.Exists(PREFAB_DIR))
            {
                Directory.CreateDirectory(PREFAB_DIR);
            }
            if (!Directory.Exists(RESOURCES_DIR))
            {
                Directory.CreateDirectory(RESOURCES_DIR);
            }

            BuildFighterPrefab("Goku", PlayerInputHandler.PlayerIndex.P1, GOKU_PREFAB_PATH);
            BuildFighterPrefab("Saitama", PlayerInputHandler.PlayerIndex.P2, SAITAMA_PREFAB_PATH);

            BuildFighterPrefab("Goku", PlayerInputHandler.PlayerIndex.P1, GOKU_RESOURCE_PATH);
            BuildFighterPrefab("Saitama", PlayerInputHandler.PlayerIndex.P2, SAITAMA_RESOURCE_PATH);

                        BuildFighterPrefab("Nezuko", PlayerInputHandler.PlayerIndex.P1, NEZUKO_PREFAB_PATH);
            BuildFighterPrefab("Nezuko", PlayerInputHandler.PlayerIndex.P1, NEZUKO_RESOURCE_PATH);
            BuildFighterPrefab("Tsunade", PlayerInputHandler.PlayerIndex.P1, TSUNADE_PREFAB_PATH);
            BuildFighterPrefab("Tsunade", PlayerInputHandler.PlayerIndex.P1, TSUNADE_RESOURCE_PATH);
AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00e676>[FighterPrefabGenerator]</color> Generated Goku.prefab and Saitama.prefab in Assets/Prefabs/Fighters/ and Assets/Resources/Prefabs/Fighters/!");
        }

        private static void BuildFighterPrefab(string fighterName, PlayerInputHandler.PlayerIndex playerIndex, string assetPath)
        {
            GameObject fighter = new GameObject(fighterName);
            if (fighterName == "Goku") fighter.transform.localScale = new Vector3(0.88f, 0.88f, 1f);
            else if (fighterName == "Saitama") fighter.transform.localScale = new Vector3(1.12f, 1.12f, 1f);
            else if (fighterName == "Nezuko") fighter.transform.localScale = new Vector3(0.60f, 0.60f, 1f);
            else fighter.transform.localScale = Vector3.one;

            // Rigidbody2D
            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // CapsuleCollider2D (World collision - vertical dome prevents stacking)
            CapsuleCollider2D col = fighter.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            if (fighterName == "Nezuko")
            {
                col.size = new Vector2(0.75f, 0.60f);
                col.offset = new Vector2(0f, 0.30f);
            }
            else
            {
                col.size = new Vector2(1.10f, 0.90f);
                col.offset = new Vector2(0f, 0.45f);
            }

            // SpriteRenderer
            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            // PlayerInputHandler
            PlayerInputHandler input = fighter.AddComponent<PlayerInputHandler>();
            input.playerIndex = playerIndex;

            // FighterController
            FighterController fc = fighter.AddComponent<FighterController>();
            fc.SetInputHandler(input);
            fc.SetGroundLayer(1 << 6); // Layer 6 = Ground

            // Ground check child
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            fc.SetGroundCheckPoint(groundCheck.transform);

            // FighterAnimator
            FighterAnimator anim = fighter.AddComponent<FighterAnimator>();
            anim.LoadCharacterFrames(fighterName);

            // Systems
            HealthSystem health = fighter.AddComponent<HealthSystem>();
            EnergySystem energy = fighter.AddComponent<EnergySystem>();

            // Audio
            FighterAudio fa = fighter.AddComponent<FighterAudio>();
            fa.EnsureClipsLoaded();

            // Hurtbox child (Green Scene Gizmo)
            GameObject hurtboxObj = new GameObject("Hurtbox");
            hurtboxObj.transform.SetParent(fighter.transform);
            hurtboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hurtboxCol = hurtboxObj.AddComponent<BoxCollider2D>();
            hurtboxCol.isTrigger = true;
            if (fighterName == "Nezuko")
            {
                hurtboxCol.size = new Vector2(0.70f, 0.60f);
                hurtboxCol.offset = new Vector2(0f, 0.30f);
            }
            else
            {
                hurtboxCol.size = new Vector2(0.9f, 1.8f);
                hurtboxCol.offset = new Vector2(0f, 0.95f);
            }
            HurtboxController hurtbox = hurtboxObj.AddComponent<HurtboxController>();
            hurtbox.SetOwner(fighter.transform);

            // Hitbox child (Red Scene Gizmo)
            GameObject hitboxObj = new GameObject("Hitbox");
            hitboxObj.transform.SetParent(fighter.transform);
            hitboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hitboxCol = hitboxObj.AddComponent<BoxCollider2D>();
            hitboxCol.isTrigger = true;
            hitboxCol.size = new Vector2(0.9f, 0.7f);
            hitboxCol.offset = new Vector2(0.8f, 0.9f);
            HitboxController hitbox = hitboxObj.AddComponent<HitboxController>();
            hitbox.Setup(fighter.transform, 10, 5f, 6f);
            fc.SetHitbox(hitbox);

            if (fighterName == "Nezuko")
            {
                fighter.AddComponent<NezukoBloodDemonArtCinematic>();
            }
            else if (fighterName == "Tsunade")
            {
                fighter.AddComponent<TsunadeKatsuyuCinematic>();
            }

            // Save to asset
            PrefabUtility.SaveAsPrefabAsset(fighter, assetPath);
            Object.DestroyImmediate(fighter);
        }
    }
}
#endif
