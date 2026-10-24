using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MysticCombat.Fighter
{
    /// <summary>
    /// Animates a fighter by dynamically playing real multi-frame pixel art sprites based on FighterState.
    /// Supports Idle loops, Walk cycles, Jump curves, Crouch, Light/Heavy Attacks, Skills, Ultimates, and Meditation.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(FighterController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class FighterAnimator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Animation Speeds (Frames Per Second)")]
        [Tooltip("Breathing idle loop speed.")]
        [SerializeField] private float idleFps = 6f;

        [Tooltip("Walk cycle speed.")]
        [SerializeField] private float walkFps = 10f;

        [Tooltip("Light punch speed (snappy).")]
        [SerializeField] private float lightAttackFps = 14f;

        [Tooltip("Heavy attack windup & strike speed.")]
        [SerializeField] private float heavyAttackFps = 11f;

        [Tooltip("Skill flurry speed (e.g. Saitama Consecutive Normal Punches).")]
        [SerializeField] private float skillFps = 18f;

        [Tooltip("Ultimate speed (Spirit Bomb / Serious Punch).")]
        [SerializeField] private float ultimateFps = 10f;

        [Tooltip("Ki meditation channel loop speed.")]
        [SerializeField] private float meditateFps = 8f;

        [Header("Sprite Scaling")]
        [Tooltip("Pixels per unit for crisp pixel scaling. Lower = bigger on screen.")]
        [SerializeField] private float pixelsPerUnit = 24f;

        #endregion

        #region Cached Components

        private SpriteRenderer spriteRenderer;
        private FighterController controller;
        private Rigidbody2D rb;

        #endregion

        #region Animation Frames

        private readonly List<Sprite> idleFrames = new List<Sprite>();
        private readonly List<Sprite> walkFrames = new List<Sprite>();
        private readonly List<Sprite> punchFrames = new List<Sprite>();
        private readonly List<Sprite> heavyFrames = new List<Sprite>();
        private readonly List<Sprite> skillFrames = new List<Sprite>();
        private readonly List<Sprite> ultimateFrames = new List<Sprite>();
        private readonly List<Sprite> jumpFrames = new List<Sprite>();
        private readonly List<Sprite> meditateFrames = new List<Sprite>();
        private Sprite crouchSprite;

        private float animTimer;
        private int currentFrameIndex;
        private FighterState lastState = FighterState.Idle;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            controller = GetComponent<FighterController>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (idleFrames.Count == 0) return;

            UpdateAnimation();
        }

        #endregion

        #region Frame Loading

        /// <summary>
        /// Loads all sprite frames from the character's Frames directory.
        /// </summary>
        /// <param name="characterFolder">Subfolder name (e.g. 'Goku' or 'Saitama').</param>
        public void LoadCharacterFrames(string characterFolder)
        {
            string framesDir = Path.Combine(Application.dataPath, "Sprites", "Fighters", characterFolder, "Frames");
            if (!Directory.Exists(framesDir))
            {
                Debug.LogWarning($"[FighterAnimator] Frames folder not found: {framesDir}");
                return;
            }

            // Adjust PPU so both fighters occupy ~2 world units height
            if (characterFolder.ToLower().Contains("saitama"))
            {
                pixelsPerUnit = 28f; // 55px body / 28 = ~1.96 world units
            }
            else
            {
                pixelsPerUnit = 24f; // 47px body / 24 = ~1.96 world units
            }

            // Load multi-frame sequences
            LoadSequence(framesDir, "idle", idleFrames);
            LoadSequence(framesDir, "walk", walkFrames);
            LoadSequence(framesDir, "punch", punchFrames);
            LoadSequence(framesDir, "heavy", heavyFrames);
            LoadSequence(framesDir, "skill", skillFrames);
            LoadSequence(framesDir, "ultimate", ultimateFrames);
            LoadSequence(framesDir, "jump", jumpFrames);
            LoadSequence(framesDir, "meditate", meditateFrames);

            // Load crouch pose
            crouchSprite = LoadSingleOrFirst(framesDir, "crouch");

            // Set initial sprite
            if (idleFrames.Count > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }

            Debug.Log($"[FighterAnimator] Loaded {characterFolder}: {idleFrames.Count} idle, {walkFrames.Count} walk, " +
                      $"{jumpFrames.Count} jump, {punchFrames.Count} punch, {heavyFrames.Count} heavy, " +
                      $"{skillFrames.Count} skill, {ultimateFrames.Count} ult frames.");
        }

        private void LoadSequence(string dir, string prefix, List<Sprite> targetList)
        {
            targetList.Clear();
            int idx = 0;
            while (true)
            {
                string path = Path.Combine(dir, $"{prefix}_{idx}.png");
                if (!File.Exists(path)) break;

                Sprite sp = LoadFrame(path);
                if (sp != null) targetList.Add(sp);
                idx++;
            }

            // If no indexed files, check for single prefix.png
            if (targetList.Count == 0)
            {
                string singlePath = Path.Combine(dir, $"{prefix}.png");
                if (File.Exists(singlePath))
                {
                    Sprite sp = LoadFrame(singlePath);
                    if (sp != null) targetList.Add(sp);
                }
            }
        }

        private Sprite LoadSingleOrFirst(string dir, string prefix)
        {
            string singlePath = Path.Combine(dir, $"{prefix}.png");
            if (File.Exists(singlePath)) return LoadFrame(singlePath);

            string firstIndexed = Path.Combine(dir, $"{prefix}_0.png");
            if (File.Exists(firstIndexed)) return LoadFrame(firstIndexed);

            return null;
        }

        private Sprite LoadFrame(string path)
        {
            if (!File.Exists(path)) return null;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; // Razor sharp pixel art!
            tex.LoadImage(bytes);

            // Calculate exact baseline pivot so feet rest perfectly on ground plane
            Vector2 pivot = new Vector2(0.5f, 0.0f);
            if (tex.width == 128 && tex.height == 128)
            {
                // Goku standardized baseline at y = 105
                pivot = new Vector2(0.5f, (128f - 105f) / 128f);
            }
            else if (tex.width == 160 && tex.height == 160)
            {
                // Saitama standardized baseline at y = 135
                pivot = new Vector2(0.5f, (160f - 135f) / 160f);
            }

            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                pivot,
                pixelsPerUnit
            );
        }

        #endregion

        #region Animation Playback

        private void UpdateAnimation()
        {
            FighterState currentState = controller.CurrentState;

            // Reset frame index if state changed
            if (currentState != lastState)
            {
                currentFrameIndex = 0;
                animTimer = 0f;
                lastState = currentState;
            }

            switch (currentState)
            {
                case FighterState.Idle:
                case FighterState.Exhausted:
                    PlayLoop(idleFrames, idleFps);
                    break;

                case FighterState.Walking:
                    PlayLoop(walkFrames, walkFps);
                    break;

                case FighterState.Jumping:
                case FighterState.Falling:
                    UpdateJumpPose();
                    break;

                case FighterState.Crouching:
                    if (crouchSprite != null) spriteRenderer.sprite = crouchSprite;
                    break;

                case FighterState.Meditating:
                    if (meditateFrames.Count > 0)
                        PlayLoop(meditateFrames, meditateFps);
                    break;

                case FighterState.LightAttack:
                    PlayOnce(punchFrames, lightAttackFps);
                    break;

                case FighterState.HeavyAttack:
                    PlayOnce(heavyFrames.Count > 0 ? heavyFrames : punchFrames, heavyAttackFps);
                    break;

                case FighterState.Skill:
                    PlayOnce(skillFrames.Count > 0 ? skillFrames : punchFrames, skillFps);
                    break;

                case FighterState.Ultimate:
                    PlayOnce(ultimateFrames.Count > 0 ? ultimateFrames : heavyFrames, ultimateFps);
                    break;
            }
        }

        /// <summary>
        /// Selects the jump sprite dynamically based on vertical physics velocity.
        /// </summary>
        private void UpdateJumpPose()
        {
            if (jumpFrames == null || jumpFrames.Count == 0) return;

            float vy = rb != null ? rb.linearVelocity.y : 0f;

            if (jumpFrames.Count >= 5)
            {
                if (vy > 4.0f)
                {
                    spriteRenderer.sprite = jumpFrames[0]; // Launch
                }
                else if (vy > 1.0f)
                {
                    spriteRenderer.sprite = jumpFrames[1]; // Ascending
                }
                else if (Mathf.Abs(vy) <= 1.0f)
                {
                    spriteRenderer.sprite = jumpFrames[2]; // Apex hangtime
                }
                else if (vy >= -5.0f)
                {
                    spriteRenderer.sprite = jumpFrames[3]; // Descent
                }
                else
                {
                    spriteRenderer.sprite = jumpFrames[jumpFrames.Count - 1]; // Fast descent
                }
            }
            else
            {
                // Smooth normalized mapping for any count of jump frames
                float t = Mathf.InverseLerp(12f, -12f, vy);
                int frameIdx = Mathf.Clamp(Mathf.FloorToInt(t * jumpFrames.Count), 0, jumpFrames.Count - 1);
                spriteRenderer.sprite = jumpFrames[frameIdx];
            }
        }

        private void PlayLoop(List<Sprite> frames, float fps)
        {
            if (frames == null || frames.Count == 0) return;

            animTimer += Time.deltaTime;
            float frameDuration = 1f / fps;

            if (animTimer >= frameDuration)
            {
                animTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Count;
            }

            spriteRenderer.sprite = frames[currentFrameIndex];
        }

        private void PlayOnce(List<Sprite> frames, float fps)
        {
            if (frames == null || frames.Count == 0) return;

            animTimer += Time.deltaTime;
            float frameDuration = 1f / fps;

            if (animTimer >= frameDuration)
            {
                animTimer -= frameDuration;
                if (currentFrameIndex < frames.Count - 1)
                {
                    currentFrameIndex++;
                }
            }

            spriteRenderer.sprite = frames[currentFrameIndex];
        }

        #endregion
    }
}
