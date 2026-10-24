using UnityEngine;
using MysticCombat.Fighter;
using MysticCombat.Input;
using MysticCombat.Systems;

namespace MysticCombat.Core
{
    /// <summary>
    /// Bootstraps the Day 1 sandbox scene by creating fighters, arena, and wiring everything up.
    /// Attach to a "Bootstrap" GameObject in the scene for rapid prototyping without prefabs.
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Layer index used for ground detection.
        /// Must match the "Ground" layer set in Edit > Project Settings > Tags and Layers.
        /// Typically Layer 6 is the first available user-defined layer.
        /// </summary>
        private const int GROUND_LAYER = 6;

        private const float FIGHTER_SCALE_X = 1f;
        private const float FIGHTER_SCALE_Y = 1.5f;

        #endregion

        #region Serialized Fields

        [Header("Spawn Positions")]
        [SerializeField] private Vector3 p1SpawnPosition = new Vector3(-3f, 0f, 0f);
        [SerializeField] private Vector3 p2SpawnPosition = new Vector3(3f, 0f, 0f);

        [Header("Fighter Stats (Defaults)")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float gravityScale = 3f;
        [SerializeField] private int maxHealth = 100;

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            Debug.Log("[PrototypeBootstrap] Initializing Day 1 sandbox...");

            SetupLayers();
            EnsureArena();

            // Create fighters
            GameObject fighter1 = CreateFighter("Fighter_P1", p1SpawnPosition, Color.blue, PlayerInputHandler.PlayerIndex.P1);
            GameObject fighter2 = CreateFighter("Fighter_P2", p2SpawnPosition, Color.red, PlayerInputHandler.PlayerIndex.P2);

            // Wire cross-references (each fighter faces the other)
            FighterController fc1 = fighter1.GetComponent<FighterController>();
            FighterController fc2 = fighter2.GetComponent<FighterController>();
            fc1.opponent = fighter2.transform;
            fc2.opponent = fighter1.transform;

            // Set up MatchManager if present
            MatchManager matchManager = FindAnyObjectByType<MatchManager>();
            if (matchManager != null)
            {
                matchManager.SetFighterReferences(
                    fighter1.GetComponent<HealthSystem>(),
                    fighter2.GetComponent<HealthSystem>(),
                    fighter1.GetComponent<EnergySystem>(),
                    fighter2.GetComponent<EnergySystem>()
                );
            }

            Debug.Log("==============================================");
            Debug.Log("[PrototypeBootstrap] DAY 1 PROTOTYPE READY!");
            Debug.Log("==============================================");
            Debug.Log("P1 Controls: W=Jump, A=Left, D=Right, S=Crouch");
            Debug.Log("             G=Light, H=Heavy, J=Skill, K=Ult, T=Meditate");
            Debug.Log("P2 Controls: Arrows=Move, NumPad1=Light, NumPad2=Heavy");
            Debug.Log("             NumPad3=Skill, NumPad5=Ult, NumPad0=Meditate");
            Debug.Log("==============================================");
        }

        #endregion

        #region Fighter Creation

        /// <summary>
        /// Creates a fully-configured fighter GameObject with all required components.
        /// </summary>
        /// <param name="fighterName">Name for the GameObject.</param>
        /// <param name="spawnPos">World-space spawn position.</param>
        /// <param name="color">Placeholder sprite color.</param>
        /// <param name="playerIndex">Which player controls this fighter.</param>
        /// <returns>The created fighter GameObject.</returns>
        private GameObject CreateFighter(string fighterName, Vector3 spawnPos, Color color, PlayerInputHandler.PlayerIndex playerIndex)
        {
            GameObject fighter = new GameObject(fighterName);
            fighter.transform.position = spawnPos;

            // --- SpriteRenderer (colored placeholder square) ---
            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite();
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(FIGHTER_SCALE_X, FIGHTER_SCALE_Y);
            sr.sortingOrder = 1;

            // --- Rigidbody2D ---
            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // --- BoxCollider2D (body) ---
            BoxCollider2D col = fighter.AddComponent<BoxCollider2D>();
            col.size = new Vector2(FIGHTER_SCALE_X, FIGHTER_SCALE_Y);

            // --- PlayerInputHandler ---
            PlayerInputHandler inputHandler = fighter.AddComponent<PlayerInputHandler>();
            inputHandler.playerIndex = playerIndex;

            // --- FighterController ---
            FighterController fc = fighter.AddComponent<FighterController>();
            fc.SetInputHandler(inputHandler);
            fc.ApplyStats(moveSpeed, jumpForce, gravityScale);

            // Ground check point (child transform at the fighter's feet)
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -(FIGHTER_SCALE_Y / 2f) - 0.05f, 0f);
            fc.SetGroundCheckPoint(groundCheck.transform);

            // Set ground layer mask
            fc.SetGroundLayer(1 << GROUND_LAYER);

            // --- HealthSystem ---
            HealthSystem health = fighter.AddComponent<HealthSystem>();
            health.Initialize(maxHealth);

            // --- EnergySystem ---
            EnergySystem energy = fighter.AddComponent<EnergySystem>();
            energy.Initialize(100f, 10f, 5f, 20f);

            Debug.Log($"[PrototypeBootstrap] Created {fighterName} at {spawnPos} (Player: {playerIndex})");

            return fighter;
        }

        #endregion

        #region Arena Setup

        /// <summary>
        /// Ensures an ArenaSetup exists in the scene to create the arena.
        /// </summary>
        private void EnsureArena()
        {
            ArenaSetup existingArena = FindAnyObjectByType<ArenaSetup>();
            if (existingArena == null)
            {
                GameObject arenaObj = new GameObject("Arena");
                arenaObj.AddComponent<ArenaSetup>();
                Debug.Log("[PrototypeBootstrap] Created Arena with ArenaSetup.");
            }
            else
            {
                Debug.Log("[PrototypeBootstrap] ArenaSetup already exists in scene.");
            }
        }

        #endregion

        #region Layer Setup

        /// <summary>
        /// Logs a reminder about required layer setup.
        /// Unity layer names can only be set via the Editor (Tags and Layers settings).
        /// </summary>
        private void SetupLayers()
        {
            string layerName = LayerMask.LayerToName(GROUND_LAYER);
            if (string.IsNullOrEmpty(layerName))
            {
                Debug.LogWarning($"[PrototypeBootstrap] Layer {GROUND_LAYER} is not named! " +
                                 $"Please go to Edit > Project Settings > Tags and Layers and set Layer {GROUND_LAYER} to 'Ground'. " +
                                 $"Ground detection will not work until this is configured.");
            }
            else
            {
                Debug.Log($"[PrototypeBootstrap] Ground layer ({GROUND_LAYER}) is named '{layerName}'.");
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Creates a simple 1x1 white pixel sprite for fighter placeholders.
        /// </summary>
        private Sprite CreatePlaceholderSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f),
                4f // 4 pixels per unit → 1x1 unit sprite
            );
        }

        #endregion
    }
}
