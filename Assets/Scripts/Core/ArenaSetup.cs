using UnityEngine;

namespace MysticCombat.Core
{
    /// <summary>
    /// Programmatically creates the Day 1 sandbox arena (floor, walls, ceiling)
    /// if no child objects already exist. Attach to an empty "Arena" GameObject.
    /// </summary>
    public class ArenaSetup : MonoBehaviour
    {
        #region Constants

        // Arena dimensions
        private const float FLOOR_WIDTH = 20f;
        private const float FLOOR_HEIGHT = 1f;
        private const float FLOOR_Y = -4f;

        private const float WALL_WIDTH = 1f;
        private const float WALL_HEIGHT = 12f;
        private const float WALL_X = 10f;

        private const float CEILING_WIDTH = 20f;
        private const float CEILING_HEIGHT = 1f;
        private const float CEILING_Y = 6f;

        // Layer index for Ground (use layer 6 — typically the first user layer)
        // NOTE: You must manually add "Ground" as Layer 6 in Edit > Project Settings > Tags and Layers.
        private const int GROUND_LAYER_INDEX = 6;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            // Only build the arena if this object has no children (avoid double-creation)
            if (transform.childCount > 0)
            {
                Debug.Log("[ArenaSetup] Arena children already exist, skipping procedural setup.");
                return;
            }

            BuildArena();
            LogCameraSetupNote();
        }

        #endregion

        #region Arena Construction

        private void BuildArena()
        {
            Debug.Log("[ArenaSetup] Building procedural arena...");

            CreateFloor();
            CreateWall("LeftWall", -WALL_X);
            CreateWall("RightWall", WALL_X);
            CreateCeiling();

            Debug.Log("[ArenaSetup] Arena construction complete.");
        }

        /// <summary>
        /// Creates the floor platform.
        /// </summary>
        private void CreateFloor()
        {
            GameObject floor = CreateArenaBlock(
                "Floor",
                new Vector3(0f, FLOOR_Y, 0f),
                new Vector2(FLOOR_WIDTH, FLOOR_HEIGHT),
                new Color(0.25f, 0.25f, 0.25f, 1f) // Dark gray
            );

            // Set to Ground layer
            floor.layer = GROUND_LAYER_INDEX;
            // Also tag it for convenience
            floor.tag = "Ground";

            Debug.Log($"[ArenaSetup] Floor created at y={FLOOR_Y}, layer={GROUND_LAYER_INDEX}. " +
                      $"Ensure 'Ground' is set as Layer {GROUND_LAYER_INDEX} in Project Settings.");
        }

        /// <summary>
        /// Creates a wall at the specified X position.
        /// </summary>
        private void CreateWall(string wallName, float xPos)
        {
            CreateArenaBlock(
                wallName,
                new Vector3(xPos, (FLOOR_Y + CEILING_Y) / 2f, 0f),
                new Vector2(WALL_WIDTH, WALL_HEIGHT),
                new Color(0.3f, 0.3f, 0.3f, 1f) // Slightly lighter gray
            );
        }

        /// <summary>
        /// Creates the ceiling.
        /// </summary>
        private void CreateCeiling()
        {
            CreateArenaBlock(
                "Ceiling",
                new Vector3(0f, CEILING_Y, 0f),
                new Vector2(CEILING_WIDTH, CEILING_HEIGHT),
                new Color(0.2f, 0.2f, 0.2f, 1f) // Darker gray
            );
        }

        /// <summary>
        /// Helper: creates a child GameObject with SpriteRenderer and BoxCollider2D.
        /// </summary>
        private GameObject CreateArenaBlock(string blockName, Vector3 position, Vector2 size, Color color)
        {
            GameObject block = new GameObject(blockName);
            block.transform.SetParent(transform);
            block.transform.localPosition = position;

            // Sprite renderer (uses the built-in white square sprite)
            SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite();
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = -10; // Behind fighters

            // Collider
            BoxCollider2D col = block.AddComponent<BoxCollider2D>();
            col.size = size;

            return block;
        }

        /// <summary>
        /// Creates a simple 1x1 white pixel sprite for use as a placeholder.
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

        #region Camera Notes

        private void LogCameraSetupNote()
        {
            Debug.Log("[ArenaSetup] CAMERA SETUP NOTE: " +
                      "Main Camera should be Orthographic, Size ~6, positioned at (0, 0, -10). " +
                      "Adjust as needed for your resolution.");

            // Auto-configure if a main camera exists
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 6f;
                mainCam.transform.position = new Vector3(0f, 0f, -10f);
                mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f); // Dark blue-ish background
                Debug.Log("[ArenaSetup] Main Camera auto-configured.");
            }
        }

        #endregion
    }
}
