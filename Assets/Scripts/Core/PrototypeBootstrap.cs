using UnityEngine;
using UnityEngine.SceneManagement;
using MysticCombat.Fighter;
using MysticCombat.Input;
using MysticCombat.Systems;
using MysticCombat.Data;
using MysticCombat.Combat;
using MysticCombat.UI;
using MysticCombat.AI;
using MysticCombat.Network;

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
        private const float FIGHTER_COLLIDER_WIDTH = 1.10f;
        private const float FIGHTER_COLLIDER_HEIGHT = 0.90f;

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

        #region State & Execution Control

        private bool hasDeployedInCurrentScene = false;

        #endregion

        #region Runtime Auto-Bootstrap & Scene Watching

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeGlobalSceneWatcher()
        {
            SceneManager.sceneLoaded -= OnGlobalSceneLoaded;
            SceneManager.sceneLoaded += OnGlobalSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnInitialSceneLoad()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            CheckAndDeployScene(activeScene);
        }

        private static void OnGlobalSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckAndDeployScene(scene);
        }

        private static void CheckAndDeployScene(Scene scene)
        {
            // Do not deploy fighters inside the Main Menu!
            if (!scene.IsValid() || scene.name == "MainMenuScene") return;

            // When entering a combat scene, ensure PrototypeBootstrap exists and deploys fighters
            if (scene.name == "SampleScene" || Object.FindAnyObjectByType<MatchManager>() != null || GameObject.Find("Arena") != null)
            {
                SceneManager.SetActiveScene(scene);

                PrototypeBootstrap deployer = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    deployer = root.GetComponentInChildren<PrototypeBootstrap>(true);
                    if (deployer != null) break;
                }

                if (deployer == null)
                {
                    GameObject bootObj = new GameObject("[PrototypeBootstrap]");
                    SceneManager.MoveGameObjectToScene(bootObj, scene);
                    deployer = bootObj.AddComponent<PrototypeBootstrap>();
                }
                deployer.DeployMatchFighters();
            }
        }

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            // If spawned in MainMenuScene, do not deploy fighters
            if (gameObject.scene.name == "MainMenuScene")
            {
                return;
            }

            DeployMatchFighters();
        }

        #endregion

        #region Match Deployment

        public void DeployMatchFighters()
        {
            if (hasDeployedInCurrentScene) return;

            // Never deploy inside Main Menu
            if (gameObject.scene.name == "MainMenuScene") return;

            hasDeployedInCurrentScene = true;

            Debug.Log("<color=#00e676>[PrototypeBootstrap]</color> Deploying fighters for combat arena...");

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
            GameObject[] existingFighters = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in existingFighters)
            {
                if (go != null && (go.name.StartsWith("Fighter_P1") || go.name.StartsWith("Fighter_P2")))
                {
                    go.SetActive(false);
                    Destroy(go);
                }
            }

            string p1Name = "Fighter_P1_" + char.ToUpper(p1Char[0]) + p1Char.Substring(1);
            string p2Name = "Fighter_P2_" + char.ToUpper(p2Char[0]) + p2Char.Substring(1);

            GameObject fighter1 = DeployFighter(p1Name, p1SpawnPosition, PlayerInputHandler.PlayerIndex.P1, p1Char);
            GameObject fighter2 = DeployFighter(p2Name, p2SpawnPosition, PlayerInputHandler.PlayerIndex.P2, p2Char);

            // Wire cross-references (each fighter auto-faces the other)
            FighterController fc1 = fighter1.GetComponent<FighterController>();
            FighterController fc2 = fighter2.GetComponent<FighterController>();
            fc1.opponent = fighter2.transform;
            fc2.opponent = fighter1.transform;

            // Mirror Match Support (Player 2 Alternate Palette)
            if (p1Char == p2Char)
            {
                Color altColor = FighterController.GetAlternatePaletteColor(p2Char);
                if (fc2 != null)
                {
                    fc2.SetBaseTintColor(altColor);
                }
                else
                {
                    SpriteRenderer sr2 = fighter2.GetComponent<SpriteRenderer>();
                    if (sr2 != null) sr2.color = altColor;
                }
                Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Mirror Match detected ({p1Char} vs {p2Char})! Applied Alternate Palette ({altColor}) to Player 2.");
            }

            // Ensure VFXManager is bootstrapped
            if (Object.FindAnyObjectByType<Combat.VFXManager>() == null)
            {
                GameObject vfxObj = new GameObject("VFXManager");
                vfxObj.AddComponent<Combat.VFXManager>();
            }

            // Ensure dynamic Ki particle aura is present on both fighters
            if (fighter1.GetComponent<Combat.FighterKiAura>() == null) fighter1.AddComponent<Combat.FighterKiAura>();
            if (fighter2.GetComponent<Combat.FighterKiAura>() == null) fighter2.AddComponent<Combat.FighterKiAura>();

            // Ensure authentic Kamehameha Beam VFX is attached to Goku
            if (fighter1.name.ToLower().Contains("goku") && fighter1.GetComponent<Combat.KamehamehaBeamVFX>() == null) fighter1.AddComponent<Combat.KamehamehaBeamVFX>();
            if (fighter2.name.ToLower().Contains("goku") && fighter2.GetComponent<Combat.KamehamehaBeamVFX>() == null) fighter2.AddComponent<Combat.KamehamehaBeamVFX>();

            // Ensure cinematic Serious Punch (Death Punch) VFX is attached to Saitama
            if (fighter1.name.ToLower().Contains("saitama") && fighter1.GetComponent<Combat.SaitamaSeriousPunchCinematic>() == null) fighter1.AddComponent<Combat.SaitamaSeriousPunchCinematic>();
            if (fighter2.name.ToLower().Contains("saitama") && fighter2.GetComponent<Combat.SaitamaSeriousPunchCinematic>() == null) fighter2.AddComponent<Combat.SaitamaSeriousPunchCinematic>();

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

            // Wire HUDController if present
            HUDController hud = FindAnyObjectByType<HUDController>();
            if (hud != null)
            {
                hud.SetFighterReferences(
                    fighter1.GetComponent<HealthSystem>(),
                    fighter2.GetComponent<HealthSystem>(),
                    fighter1.GetComponent<EnergySystem>(),
                    fighter2.GetComponent<EnergySystem>()
                );
            }

            // Wire LAN NetworkMatchSynchronizer if in LAN mode
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameManager.GameMode.LanPvP)
            {
                GameObject syncObj = new GameObject("NetworkMatchSynchronizer");
                NetworkMatchSynchronizer synchronizer = syncObj.AddComponent<NetworkMatchSynchronizer>();
                synchronizer.Initialize(fc1, fc2, matchManager, GameManager.Instance.IsLanHost);
            }

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
            Debug.Log($"[PrototypeBootstrap] >>> FIGHTING MATCH READY: P1 ({p1Char}) vs P2 ({p2Char}) <<<");
            Debug.Log("=================================================================");
            Debug.Log("P1 (Goku):    [A/D] Move | [W / Space] Jump | [S] Crouch");
            Debug.Log("              [F] Light | [C] Heavy | [E] Skill | [Q] Ult | [X] Meditate");
            Debug.Log("P2 (Saitama): [J/L] Move | [I] Jump | [K] Crouch");
            Debug.Log("              [H] Light | [N] Heavy | [U] Skill | [O] Ult | [M] Meditate");
            Debug.Log("=================================================================");
        }

        #endregion

        #region Fighter Deployment & Creation

        private GameObject DeployFighter(string fighterName, Vector3 spawnPos, PlayerInputHandler.PlayerIndex playerIndex, string characterId)
        {
            string prefabName = characterId == "goku" ? "Goku" : "Saitama";
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/Fighters/{prefabName}");
            GameObject fighter = null;

            if (prefab != null)
            {
                fighter = Instantiate(prefab, spawnPos, Quaternion.identity);
                fighter.name = fighterName;
            }
            else
            {
                fighter = CreateFighterProcedural(fighterName, spawnPos, characterId);
            }

            if (fighter.scene != gameObject.scene)
            {
                SceneManager.MoveGameObjectToScene(fighter, gameObject.scene);
            }

            FighterController fc = fighter.GetComponent<FighterController>();
            if (fc == null) fc = fighter.AddComponent<FighterController>();

            // Input Provider Assignment (Decoupled: Human Keyboard vs Tactical CPU HFSM vs LAN Network)
            bool isLan = (GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameManager.GameMode.LanPvP);
            bool isHost = (GameManager.Instance == null || GameManager.Instance.IsLanHost);

            if (isLan)
            {
                if (playerIndex == PlayerInputHandler.PlayerIndex.P1)
                {
                    // On BOTH Host and Client: Player 1 (Left side) is ALWAYS the local human player!
                    PlayerInputHandler inputHandler = fighter.GetComponent<PlayerInputHandler>();
                    if (inputHandler == null) inputHandler = fighter.AddComponent<PlayerInputHandler>();
                    inputHandler.playerIndex = PlayerInputHandler.PlayerIndex.P1;
                    fc.SetInputHandler(inputHandler);
                    Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} as Local Human Player 1 (Left Side)!");
                }
                else // P2
                {
                    // On BOTH Host and Client: Player 2 (Right side) is ALWAYS the remote network opponent!
                    PlayerInputHandler existing = fighter.GetComponent<PlayerInputHandler>();
                    if (existing != null) Destroy(existing);

                    NetworkFighterInput netInput = fighter.GetComponent<NetworkFighterInput>();
                    if (netInput == null) netInput = fighter.AddComponent<NetworkFighterInput>();
                    fc.SetInputHandler(netInput);
                    Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} as Remote Network Player 2 (Right Side)!");
                }
            }
            else
            {
                bool isCpu = (playerIndex == PlayerInputHandler.PlayerIndex.P2) &&
                             (GameManager.Instance == null || GameManager.Instance.CurrentGameMode == GameManager.GameMode.VsCpu);

                if (isCpu)
                {
                    PlayerInputHandler existingInput = fighter.GetComponent<PlayerInputHandler>();
                    if (existingInput != null) Destroy(existingInput);

                    CpuDifficulty diff = (GameManager.Instance != null) ? GameManager.Instance.CurrentCpuDifficulty : CpuDifficulty.Easy;
                    CpuFighterBrain brain = characterId == "goku"
                        ? (CpuFighterBrain)fighter.AddComponent<GokuBrain>()
                        : (CpuFighterBrain)fighter.AddComponent<SaitamaBrain>();

                    brain.Configure(diff);
                    fc.SetInputHandler(brain);
                    Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} from {prefabName}.prefab as CPU ({diff})!");
                }
                else
                {
                    PlayerInputHandler inputHandler = fighter.GetComponent<PlayerInputHandler>();
                    if (inputHandler == null) inputHandler = fighter.AddComponent<PlayerInputHandler>();
                    inputHandler.playerIndex = playerIndex;
                    fc.SetInputHandler(inputHandler);
                    Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} from {prefabName}.prefab for Player {playerIndex}!");
                }
            }

            fc.SetGroundLayer(1 << GROUND_LAYER);

            Transform gc = fighter.transform.Find("GroundCheck");
            if (gc != null) fc.SetGroundCheckPoint(gc);

            FighterAudio fa = fighter.GetComponent<FighterAudio>();
            if (fa != null) fa.EnsureClipsLoaded();

            // Load and apply data from JSON (Data-Driven Architecture)
            HealthSystem health = fighter.GetComponent<HealthSystem>();
            EnergySystem energy = fighter.GetComponent<EnergySystem>();
            CharacterData data = CharacterDataLoader.Load(characterId);
            if (data != null)
            {
                fc.ApplyCharacterData(data);
                if (health != null) health.Initialize(data.maxHealth);
                if (energy != null)
                {
                    float redThresh = data.redZone != null ? data.redZone.threshold : 10f;
                    float redDur = data.redZone != null ? data.redZone.duration : 5f;
                    float redRec = data.redZone != null ? data.redZone.recoveryKi : 20f;
                    float skillCost = data.skill != null ? data.skill.kiCost : (characterId == "goku" ? 0f : 30f);
                    float ultCost = data.ultimate != null ? data.ultimate.kiCost : (characterId == "goku" ? 70f : 80f);
                    // For Goku, Safe Skill marker is 30 Ki (SSJ buffer); for Saitama it is redThresh + skillCost (10 + 30 = 40)
                    float safeSkill = characterId == "goku" ? 30f : (redThresh + skillCost);
                    float safeUlt = redThresh + ultCost; // 10 + 70 = 80 for Goku, 10 + 80 = 90 for Saitama

                    energy.Initialize(100f, redThresh, redDur, redRec, skillCost, ultCost, safeSkill, safeUlt);
                }
            }
            else
            {
                fc.ApplyStats(defaultMoveSpeed, defaultJumpForce, defaultGravityScale);
                if (health != null) health.Initialize(defaultMaxHealth);
                if (energy != null) energy.Initialize(100f, 10f, 5f, 20f);
            }

            return fighter;
        }

        private GameObject CreateFighterProcedural(string fighterName, Vector3 spawnPos, string characterId)
        {
            GameObject fighter = new GameObject(fighterName);
            fighter.transform.position = spawnPos;

            // Rigidbody2D
            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = defaultGravityScale;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Body Collider (Vertical Capsule with rounded dome to prevent stacking, wider for sparring gap)
            CapsuleCollider2D col = fighter.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(FIGHTER_COLLIDER_WIDTH, FIGHTER_COLLIDER_HEIGHT);
            col.offset = new Vector2(0f, FIGHTER_COLLIDER_HEIGHT / 2f);

            // SpriteRenderer
            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            // Ground check
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.05f, 0f);

            // Animator
            FighterAnimator animator = fighter.AddComponent<FighterAnimator>();
            string folderName = characterId == "goku" ? "Goku" : "Saitama";
            animator.LoadCharacterFrames(folderName);

            // Hurtbox
            GameObject hurtboxObj = new GameObject("Hurtbox");
            hurtboxObj.transform.SetParent(fighter.transform);
            hurtboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hurtboxCol = hurtboxObj.AddComponent<BoxCollider2D>();
            hurtboxCol.isTrigger = true;
            hurtboxCol.size = new Vector2(0.9f, 1.8f);
            hurtboxCol.offset = new Vector2(0f, 0.95f);
            HurtboxController hurtbox = hurtboxObj.AddComponent<HurtboxController>();
            hurtbox.SetOwner(fighter.transform);

            // Hitbox
            GameObject hitboxObj = new GameObject("Hitbox");
            hitboxObj.transform.SetParent(fighter.transform);
            hitboxObj.transform.localPosition = Vector3.zero;
            BoxCollider2D hitboxCol = hitboxObj.AddComponent<BoxCollider2D>();
            hitboxCol.isTrigger = true;
            hitboxCol.size = new Vector2(0.9f, 0.7f);
            hitboxCol.offset = new Vector2(0.8f, 0.9f);
            HitboxController hitbox = hitboxObj.AddComponent<HitboxController>();
            hitbox.Setup(fighter.transform, 10, 5f, 6f);

            // Audio
            FighterAudio fa = fighter.AddComponent<FighterAudio>();
            fa.EnsureClipsLoaded();

            // Systems
            fighter.AddComponent<HealthSystem>();
            fighter.AddComponent<EnergySystem>();

            return fighter;
        }

        #endregion

        #region Arena Setup

        private void EnsureArena()
        {
            GameObject arenaObj = GameObject.Find("Arena");
            if (arenaObj == null)
            {
                ArenaSetup existingArena = FindAnyObjectByType<ArenaSetup>();
                if (existingArena == null)
                {
                    arenaObj = new GameObject("Arena");
                    arenaObj.AddComponent<ArenaSetup>();
                }
                else
                {
                    arenaObj = existingArena.gameObject;
                }
            }

            if (arenaObj != null && arenaObj.GetComponent<Combat.ArenaBackgroundManager>() == null)
            {
                arenaObj.AddComponent<Combat.ArenaBackgroundManager>();
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
