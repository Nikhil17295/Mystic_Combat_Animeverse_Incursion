using UnityEngine;
using MysticCombat.Fighter;
using MysticCombat.Input;
using MysticCombat.Systems;
using MysticCombat.Data;
using MysticCombat.Combat;
using MysticCombat.UI;
using MysticCombat.AI;

namespace MysticCombat.Core
{
    /// <summary>
    /// Bootstraps the fighting scene by creating fighters with real pixel art animation frames,
    /// setting up the arena backdrop, and wiring up all combat systems.
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        #region Constants

        private const int GROUND_LAYER = 6;
        private const float FIGHTER_COLLIDER_WIDTH = 0.8f;
        private const float FIGHTER_COLLIDER_HEIGHT = 1.9f;

        #endregion

        #region Serialized Fields

        [Header("Spawn Positions")]
        [SerializeField] private Vector3 p1SpawnPosition = new Vector3(-2.2f, -3.0f, 0f);
        [SerializeField] private Vector3 p2SpawnPosition = new Vector3(2.2f, -3.0f, 0f);

        [Header("Fallback Fighter Stats")]
        [SerializeField] private float defaultMoveSpeed = 5f;
        [SerializeField] private float defaultJumpForce = 12f;
        [SerializeField] private float defaultGravityScale = 3f;
        [SerializeField] private int defaultMaxHealth = 100;

        #endregion

        #region Runtime Auto-Bootstrap

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (FindAnyObjectByType<PrototypeBootstrap>() == null)
            {
                GameObject bootObj = new GameObject("[PrototypeBootstrap]");
                bootObj.AddComponent<PrototypeBootstrap>();
            }
        }

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            Debug.Log("[PrototypeBootstrap] Initializing Mystic Combat Arcade View...");

            SetupLayers();
            EnsureArena();

            // Determine active character IDs from GameManager (defaults: Goku vs Saitama)
            string p1Char = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.P1SelectedCharacterId))
                ? GameManager.Instance.P1SelectedCharacterId.ToLower()
                : "goku";

            string p2Char = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.P2SelectedCharacterId))
                ? GameManager.Instance.P2SelectedCharacterId.ToLower()
                : "saitama";

            // Clean up pre-existing baked fighters in scene to guarantee correct characters spawn
            GameObject[] existingFighters = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in existingFighters)
            {
                if (go != null && (go.name.StartsWith("Fighter_P1_") || go.name.StartsWith("Fighter_P2_")))
                {
                    Destroy(go);
                }
            }

            string p1Name = "Fighter_P1_" + char.ToUpper(p1Char[0]) + p1Char.Substring(1);
            string p2Name = "Fighter_P2_" + char.ToUpper(p2Char[0]) + p2Char.Substring(1);

            GameObject fighter1 = CreateFighter(p1Name, p1SpawnPosition, PlayerInputHandler.PlayerIndex.P1, p1Char);
            GameObject fighter2 = CreateFighter(p2Name, p2SpawnPosition, PlayerInputHandler.PlayerIndex.P2, p2Char);

            // Mirror Match Support (Player 2 Alternate Palette)
            if (p1Char == p2Char)
            {
                SpriteRenderer sr2 = fighter2.GetComponent<SpriteRenderer>();
                if (sr2 != null)
                {
                    if (p2Char == "goku")
                    {
                        sr2.color = new Color(0.70f, 0.85f, 1.25f, 1.0f); // Cool Cyan/Ice Alternate Gi
                    }
                    else
                    {
                        sr2.color = new Color(1.15f, 0.85f, 0.70f, 1.0f); // Solar Amber Alternate Suit
                    }
                }
            }

            // Wire cross-references (each fighter auto-faces the other)
            FighterController fc1 = fighter1.GetComponent<FighterController>();
            FighterController fc2 = fighter2.GetComponent<FighterController>();
            fc1.opponent = fighter2.transform;
            fc2.opponent = fighter1.transform;

            // Wire MatchManager if present
            MatchManager matchManager = FindAnyObjectByType<MatchManager>();
            if (matchManager == null)
            {
                GameObject mmObj = new GameObject("MatchManager");
                matchManager = mmObj.AddComponent<MatchManager>();
            }

            matchManager.SetFighterReferences(
                fighter1.GetComponent<HealthSystem>(),
                fighter2.GetComponent<HealthSystem>(),
                fighter1.GetComponent<EnergySystem>(),
                fighter2.GetComponent<EnergySystem>()
            );

            // Ensure CombatMenuController is active and ready on HUD_Canvas
            CombatMenuController menuCtrl = Object.FindAnyObjectByType<CombatMenuController>();
            if (menuCtrl == null)
            {
                GameObject canvasObj = GameObject.Find("HUD_Canvas");
                if (canvasObj != null)
                {
                    menuCtrl = canvasObj.AddComponent<CombatMenuController>();
                }
            }
            if (menuCtrl != null)
            {
                menuCtrl.EnsureUIHierarchyBuilt();
            }

            Debug.Log("=================================================================");
            Debug.Log("[PrototypeBootstrap] >>> FIGHTING GAME PROTOTYPE READY <<<");
            Debug.Log("=================================================================");
            Debug.Log("P1 (Goku):    [A/D] Move | [W / Space] Jump | [S] Crouch");
            Debug.Log("              [F] Light | [C] Heavy | [E] Skill | [Q] Ult | [X] Meditate");
            Debug.Log("P2 (Saitama): [J/L] Move | [I] Jump | [K] Crouch");
            Debug.Log("              [H] Light | [N] Heavy | [U] Skill | [O] Ult | [M] Meditate");
            Debug.Log("=================================================================");
        }

        #endregion

        #region Fighter Creation

        private GameObject CreateFighter(string fighterName, Vector3 spawnPos, PlayerInputHandler.PlayerIndex playerIndex, string characterId)
        {
            GameObject fighter = new GameObject(fighterName);
            fighter.transform.position = spawnPos;

            // Rigidbody2D
            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = defaultGravityScale;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Body Collider (Bottom is at y=0, height ~1.9, center at 0.95)
            BoxCollider2D col = fighter.AddComponent<BoxCollider2D>();
            col.size = new Vector2(FIGHTER_COLLIDER_WIDTH, FIGHTER_COLLIDER_HEIGHT);
            col.offset = new Vector2(0f, FIGHTER_COLLIDER_HEIGHT / 2f);

            // SpriteRenderer
            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            // Input Provider (Decoupled: Human Keyboard vs Tactical CPU HFSM)
            bool isCpu = (playerIndex == PlayerInputHandler.PlayerIndex.P2) &&
                         (GameManager.Instance == null || GameManager.Instance.CurrentGameMode == GameManager.GameMode.VsCpu);

            FighterController fc = fighter.AddComponent<FighterController>();

            if (isCpu)
            {
                CpuDifficulty diff = (GameManager.Instance != null) ? GameManager.Instance.CurrentCpuDifficulty : CpuDifficulty.Easy;
                CpuFighterBrain brain = null;
                if (characterId == "goku")
                {
                    brain = fighter.AddComponent<GokuBrain>();
                }
                else
                {
                    brain = fighter.AddComponent<SaitamaBrain>();
                }

                brain.Configure(diff);
                fc.SetInputHandler(brain);
                Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Spawned {fighterName} as CPU with {diff} difficulty!");
            }
            else
            {
                PlayerInputHandler inputHandler = fighter.AddComponent<PlayerInputHandler>();
                inputHandler.playerIndex = playerIndex;
                fc.SetInputHandler(inputHandler);
            }

            // Ground check point at the fighter's feet
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            fc.SetGroundCheckPoint(groundCheck.transform);
            fc.SetGroundLayer(1 << GROUND_LAYER);

            // FighterAnimator (Loads real multi-frame pixel art)
            FighterAnimator animator = fighter.AddComponent<FighterAnimator>();
            string folderName = characterId == "goku" ? "Goku" : "Saitama";
            animator.LoadCharacterFrames(folderName);

            // Hurtbox Child GameObject (vulnerable area - green gizmo)
            GameObject hurtboxObj = new GameObject("Hurtbox");
            hurtboxObj.transform.SetParent(fighter.transform);
            hurtboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hurtboxCol = hurtboxObj.AddComponent<BoxCollider2D>();
            hurtboxCol.isTrigger = true;
            hurtboxCol.size = new Vector2(0.9f, 1.8f);
            hurtboxCol.offset = new Vector2(0f, 0.95f);
            HurtboxController hurtbox = hurtboxObj.AddComponent<HurtboxController>();
            hurtbox.SetOwner(fighter.transform);

            // Hitbox Child GameObject (attack reach - red gizmo)
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

            // Fighter Audio (Voice shouts, martial hits, anime powers)
            FighterAudio fa = fighter.AddComponent<FighterAudio>();
            fa.EnsureClipsLoaded();
            fa.KiAuraVolume = 0.12f;
            fa.VoiceVolume = 1.0f;
            fa.VoiceSkillMultiplier = 1.45f;
            fa.VoiceUltimateMultiplier = 1.70f;

            // Systems
            HealthSystem health = fighter.AddComponent<HealthSystem>();
            EnergySystem energy = fighter.AddComponent<EnergySystem>();

            // Load and apply data from JSON (Data-Driven Architecture)
            CharacterData data = CharacterDataLoader.Load(characterId);
            if (data != null)
            {
                fc.ApplyStats(data.moveSpeed, data.jumpForce, data.gravityScale);
                health.Initialize(data.maxHealth);
                if (data.redZone != null)
                {
                    energy.Initialize(100f, data.redZone.threshold, data.redZone.duration, data.redZone.recoveryKi);
                }
                else
                {
                    energy.Initialize(100f, 10f, 5f, 20f);
                }
                Debug.Log($"[PrototypeBootstrap] Loaded data for '{data.displayName}': HP={data.maxHealth}, Speed={data.moveSpeed}, Jump={data.jumpForce}");
            }
            else
            {
                fc.ApplyStats(defaultMoveSpeed, defaultJumpForce, defaultGravityScale);
                health.Initialize(defaultMaxHealth);
                energy.Initialize(100f, 10f, 5f, 20f);
            }

            return fighter;
        }

        #endregion

        #region Arena Setup

        private void EnsureArena()
        {
            if (GameObject.Find("Arena") != null) return;

            ArenaSetup existingArena = FindAnyObjectByType<ArenaSetup>();
            if (existingArena == null)
            {
                GameObject arenaObj = new GameObject("Arena");
                arenaObj.AddComponent<ArenaSetup>();
            }
        }

        #endregion

        #region Layer Setup

        private void SetupLayers()
        {
            string layerName = LayerMask.LayerToName(GROUND_LAYER);
            if (string.IsNullOrEmpty(layerName))
            {
                Debug.LogWarning($"[PrototypeBootstrap] Layer {GROUND_LAYER} is not named! Ensure Layer {GROUND_LAYER} is set to 'Ground'.");
            }
        }

        #endregion
    }
}
