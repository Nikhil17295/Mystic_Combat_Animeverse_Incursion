using UnityEngine;
using System.IO;

namespace MysticCombat.Core
{
    /// <summary>
    /// Programmatically creates the fighting game arena (stage backdrop, floor, walls, ceiling)
    /// and configures the camera for close-up fighting game zoom (~45% character screen coverage).
    /// </summary>
    public class ArenaSetup : MonoBehaviour
    {
        #region Constants

        private const float FLOOR_WIDTH = 20f;
        private const float FLOOR_HEIGHT = 1f;
        private const float FLOOR_Y = -3.5f;

        private const float WALL_WIDTH = 1f;
        private const float WALL_HEIGHT = 12f;
        private const float WALL_X = 8.5f;

        private const float CEILING_WIDTH = 20f;
        private const float CEILING_HEIGHT = 1f;
        private const float CEILING_Y = 6f;

        private const int GROUND_LAYER_INDEX = 6;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (transform.childCount > 0)
            {
                Debug.Log("[ArenaSetup] Arena children already exist, skipping procedural setup.");
                return;
            }

            BuildArena();
            ConfigureCamera();
        }

        #endregion

        #region Arena Construction

        private void BuildArena()
        {
            Debug.Log("[ArenaSetup] Building procedural anime fighting stage...");

            CreateStageBackground();
            CreateFloor();
            CreateWall("LeftWall", -WALL_X);
            CreateWall("RightWall", WALL_X);
            CreateCeiling();

            Debug.Log("[ArenaSetup] Arena construction complete.");
        }

        private void CreateStageBackground()
        {
            string bgPath = Path.Combine(Application.dataPath, "Sprites", "Arena", "arena_stage_background.png");
            if (!File.Exists(bgPath)) return;

            byte[] bytes = File.ReadAllBytes(bgPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.filterMode = FilterMode.Point;
            tex.LoadImage(bytes);

            GameObject bgObj = new GameObject("StageBackground");
            bgObj.transform.SetParent(transform);
            bgObj.transform.localPosition = new Vector3(0f, -0.5f, 5f);

            SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sr.sortingOrder = -20;
            bgObj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        }

        private void CreateFloor()
        {
            GameObject floor = CreateArenaBlock(
                "Floor",
                new Vector3(0f, FLOOR_Y, 0f),
                new Vector2(FLOOR_WIDTH, FLOOR_HEIGHT),
                new Color(0.22f, 0.22f, 0.25f, 1f)
            );

            floor.layer = GROUND_LAYER_INDEX;
            floor.tag = "Ground";
        }

        private void CreateWall(string wallName, float xPos)
        {
            // Invisible boundary wall
            GameObject wall = new GameObject(wallName);
            wall.transform.SetParent(transform);
            wall.transform.localPosition = new Vector3(xPos, 0f, 0f);

            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            col.size = new Vector2(WALL_WIDTH, WALL_HEIGHT);
        }

        private void CreateCeiling()
        {
            GameObject ceiling = new GameObject("Ceiling");
            ceiling.transform.SetParent(transform);
            ceiling.transform.localPosition = new Vector3(0f, CEILING_Y, 0f);

            BoxCollider2D col = ceiling.AddComponent<BoxCollider2D>();
            col.size = new Vector2(CEILING_WIDTH, CEILING_HEIGHT);
        }

        private GameObject CreateArenaBlock(string blockName, Vector3 position, Vector2 size, Color color)
        {
            GameObject block = new GameObject(blockName);
            block.transform.SetParent(transform);
            block.transform.localPosition = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite();
            sr.color = color;
            sr.sortingOrder = -5;

            BoxCollider2D col = block.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            return block;
        }

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
                4f
            );
        }

        #endregion

        #region Camera Configuration

        private void ConfigureCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 3.2f; // Close-up fighting game zoom (~45% screen coverage)!
                mainCam.transform.position = new Vector3(0f, -1.2f, -10f); // Centered on fighters standing on floor
                mainCam.backgroundColor = new Color(0.06f, 0.06f, 0.1f, 1f);
                Debug.Log("[ArenaSetup] Main Camera zoomed to arcade view (Size 3.2).");
            }
        }

        #endregion
    }
}
