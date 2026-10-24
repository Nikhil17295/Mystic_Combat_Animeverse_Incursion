using UnityEngine;
using MysticCombat.Fighter;
using MysticCombat.Input;
using MysticCombat.Systems;
using MysticCombat.Data;

namespace MysticCombat.Core
{
    /// <summary>
    /// Bootstraps the Day 1 sandbox scene by creating fighters, arena, and wiring everything up.
    /// Runs automatically upon scene load via [RuntimeInitializeOnLoadMethod], or can be attached to a scene GameObject.
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Layer index used for ground detection.
        /// Configured as Layer 6 "Ground" in ProjectSettings/TagManager.asset.
        /// </summary>
        private const int GROUND_LAYER = 6;

        private const float FIGHTER_SCALE_X = 1f;
        private const float FIGHTER_SCALE_Y = 1.6f;

        #endregion

        #region Serialized Fields

        [Header("Spawn Positions")]
        [SerializeField] private Vector3 p1SpawnPosition = new Vector3(-3.5f, -2.5f, 0f);
        [SerializeField] private Vector3 p2SpawnPosition = new Vector3(3.5f, -2.5f, 0f);

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
            Debug.Log("[PrototypeBootstrap] Initializing Mystic Combat Day 1 Sandbox...");

            SetupLayers();
            EnsureArena();

            // Create Fighters: Goku (P1) and Saitama (P2)
            Color gokuOrange = new Color(1.0f, 0.45f, 0.1f, 1f);
            Color saitamaYellow = new Color(1.0f, 0.85f, 0.2f, 1f);

            GameObject fighter1 = CreateFighter("Fighter_P1_Goku", p1SpawnPosition, gokuOrange, PlayerInputHandler.PlayerIndex.P1, "goku");
            GameObject fighter2 = CreateFighter("Fighter_P2_Saitama", p2SpawnPosition, saitamaYellow, PlayerInputHandler.PlayerIndex.P2, "saitama");

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

            Debug.Log("=================================================================");
            Debug.Log("[PrototypeBootstrap] >>> DAY 1 PROTOTYPE ACTIVE & READY <<<");
            Debug.Log("=================================================================");
            Debug.Log("P1 (Goku - Orange):   [A/D] Move | [W] Jump | [S] Crouch");
            Debug.Log("                      [G] Light | [H] Heavy | [J] Skill | [K] Ult | [T] Meditate");
            Debug.Log("P2 (Saitama - Yellow):[Left/Right] Move | [Up] Jump | [Down] Crouch");
            Debug.Log("                      [Numpad 1] Light | [Numpad 2] Heavy | [Numpad 3] Skill | [Numpad 5] Ult | [Numpad 0] Meditate");
            Debug.Log("=================================================================");
        }

        #endregion

        #region Fighter Creation

        /// <summary>
        /// Creates a fully-configured fighter GameObject with all required components, loading stats from JSON.
        /// </summary>
        private GameObject CreateFighter(string fighterName, Vector3 spawnPos, Color bodyColor, PlayerInputHandler.PlayerIndex playerIndex, string characterId)
        {
            GameObject fighter = new GameObject(fighterName);
            fighter.transform.position = spawnPos;

            // Rigidbody2D
            Rigidbody2D rb = fighter.AddComponent<Rigidbody2D>();
            rb.gravityScale = defaultGravityScale;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Body Collider
            BoxCollider2D col = fighter.AddComponent<BoxCollider2D>();
            col.size = new Vector2(FIGHTER_SCALE_X, FIGHTER_SCALE_Y);

            // SpriteRenderer (asymmetrical texture so auto-facing is visible)
            SpriteRenderer sr = fighter.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFighterSprite(bodyColor);
            sr.sortingOrder = 2;

            // PlayerInputHandler
            PlayerInputHandler inputHandler = fighter.AddComponent<PlayerInputHandler>();
            inputHandler.playerIndex = playerIndex;

            // FighterController
            FighterController fc = fighter.AddComponent<FighterController>();
            fc.SetInputHandler(inputHandler);

            // Ground check point at the fighter's feet
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(fighter.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -(FIGHTER_SCALE_Y / 2f) - 0.05f, 0f);
            fc.SetGroundCheckPoint(groundCheck.transform);
            fc.SetGroundLayer(1 << GROUND_LAYER);

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
                Debug.LogWarning($"[PrototypeBootstrap] Character data for '{characterId}' not found. Using defaults.");
            }

            return fighter;
        }

        #endregion

        #region Arena Setup

        private void EnsureArena()
        {
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

        #region Sprite Generation

        /// <summary>
        /// Creates an asymmetrical 16x24 sprite with a visible eye/visor on the right edge.
        /// This ensures auto-facing (flipX) is clearly visible when fighters change direction.
        /// </summary>
        private Sprite CreateFighterSprite(Color baseColor)
        {
            int width = 16;
            int height = 24;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; // Crisp pixel look

            Color outlineColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            Color visorColor = new Color(0.1f, 0.95f, 1.0f, 1f); // Bright cyan eye/visor
            Color beltColor = new Color(0.15f, 0.15f, 0.15f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Border outline
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        tex.SetPixel(x, y, outlineColor);
                    }
                    // Eye / Visor facing right (x between 10 and 14, y between 16 and 19)
                    else if (x >= 10 && x <= 14 && y >= 16 && y <= 19)
                    {
                        tex.SetPixel(x, y, visorColor);
                    }
                    // Belt / sash around middle
                    else if (y >= 10 && y <= 12)
                    {
                        tex.SetPixel(x, y, beltColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, baseColor);
                    }
                }
            }

            tex.Apply();

            // Pivot at center, sized so it matches FIGHTER_SCALE_X by FIGHTER_SCALE_Y
            return Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                width / FIGHTER_SCALE_X
            );
        }

        #endregion
    }
}
