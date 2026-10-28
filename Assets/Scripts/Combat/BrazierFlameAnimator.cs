using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Animates a lively multi-frame pixel art fire loop on arena braziers.
    /// Supports dynamic palette switching (Tournament Orange, Lunar Blue, Saiyan Gold)
    /// and interactive surge flares when heavy hits or ultimates connect.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BrazierFlameAnimator : MonoBehaviour
    {
        public enum FlamePalette
        {
            Orange, // Round 1: Martial Arts Tournament
            Blue,   // Round 2: Battle on the Moon / Space Rift
            Gold    // Round 3: Multiverse Cataclysm / Super Saiyan
        }

        #region Serialized Fields

        [Header("Animation Settings")]
        [Tooltip("Playback speed of the flame loop in frames per second.")]
        [SerializeField] private float fps = 10f;

        [Tooltip("Active color palette for the brazier fire.")]
        [SerializeField] private FlamePalette currentPalette = FlamePalette.Orange;

        [Tooltip("Baseline scale of the flame.")]
        [SerializeField] private Vector3 baseScale = Vector3.one;

        #endregion

        #region Private Fields

        private SpriteRenderer spriteRenderer;
        private readonly List<Sprite> orangeFrames = new List<Sprite>();
        private readonly List<Sprite> blueFrames = new List<Sprite>();
        private readonly List<Sprite> goldFrames = new List<Sprite>();

        private float timer;
        private int currentFrame;
        private float surgeScale = 1.0f;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadAllPalettes();
        }

        private void Update()
        {
            List<Sprite> activeFrames = GetActiveFrames();
            if (activeFrames.Count == 0) return;

            // Advance frame animation
            timer += Time.deltaTime;
            float frameInterval = 1f / Mathf.Max(1f, fps);
            if (timer >= frameInterval)
            {
                timer -= frameInterval;
                currentFrame = (currentFrame + 1) % activeFrames.Count;
                spriteRenderer.sprite = activeFrames[currentFrame];
            }

            // Smooth surge scale decay
            if (surgeScale > 1.0f)
            {
                surgeScale = Mathf.MoveTowards(surgeScale, 1.0f, Time.deltaTime * 2.5f);
                transform.localScale = baseScale * surgeScale;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Switches the active flame palette (Orange -> Blue -> Gold) to match the match round.
        /// </summary>
        public void SetPalette(FlamePalette palette)
        {
            currentPalette = palette;
            currentFrame = 0;
            timer = 0f;

            List<Sprite> frames = GetActiveFrames();
            if (frames.Count > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        /// <summary>
        /// Triggers an interactive flame flare/surge when a heavy attack or ultimate blast erupts.
        /// </summary>
        public void TriggerSurge(float intensityMultiplier = 1.35f)
        {
            surgeScale = Mathf.Max(surgeScale, intensityMultiplier);
        }

        #endregion

        #region Frame Loading

        private List<Sprite> GetActiveFrames()
        {
            switch (currentPalette)
            {
                case FlamePalette.Blue: return blueFrames;
                case FlamePalette.Gold: return goldFrames;
                default: return orangeFrames;
            }
        }

        private void LoadAllPalettes()
        {
            string baseDir = Path.Combine(Application.dataPath, "Sprites", "Arena", "Flames");
            LoadFolder(Path.Combine(baseDir, "Orange"), orangeFrames);
            LoadFolder(Path.Combine(baseDir, "Blue"), blueFrames);
            LoadFolder(Path.Combine(baseDir, "Gold"), goldFrames);

            List<Sprite> initial = GetActiveFrames();
            if (initial.Count > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = initial[0];
            }
        }

        private void LoadFolder(string folderPath, List<Sprite> targetList)
        {
            targetList.Clear();
            if (!Directory.Exists(folderPath)) return;

            for (int i = 0; i < 16; i++)
            {
                string path = Path.Combine(folderPath, $"fire_{i}.png");
                if (!File.Exists(path)) break;

                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.LoadImage(bytes);

                Sprite sp = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.0f), // Bottom pivot so flame scales upwards from bowl
                    40f
                );
                targetList.Add(sp);
            }
        }

        #endregion
    }
}
