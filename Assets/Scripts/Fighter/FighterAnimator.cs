using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MysticCombat.Fighter
{
    /// <summary>
    /// Animates a fighter by dynamically playing real multi-frame pixel art sprites based on FighterState.
    /// Supports Idle loops, Walk cycles, Jump curves, Crouch, Light/Heavy Attacks, Skills, Ultimates, and Meditation.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(FighterController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class FighterAnimator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Animation Speeds (Frames Per Second)")]
        [Tooltip("Breathing idle loop speed.")]
        [SerializeField] private float idleFps = 5f;

        [Tooltip("Walk cycle speed.")]
        [SerializeField] private float walkFps = 8f;

        [Tooltip("Light punch speed (comfortable read).")]
        [SerializeField] private float lightAttackFps = 9.5f;

        [Tooltip("Heavy attack windup & strike speed (slowed down for weight & impact!).")]
        [SerializeField] private float heavyAttackFps = 6f;

        [Tooltip("Skill flurry speed (e.g. Saitama Consecutive Normal Punches).")]
        [SerializeField] private float skillFps = 10f;

        [Tooltip("Ultimate speed (Spirit Bomb / Serious Punch) - heavy cinematic windup.")]
        [SerializeField] private float ultimateFps = 6f;

        [Tooltip("Ki meditation channel loop speed.")]
        [SerializeField] private float meditateFps = 7f;

        [Tooltip("Hurt reaction playback speed (snappy read).")]
        [SerializeField] private float hurtFps = 8.5f;

        [Tooltip("Knockout collapse speed.")]
        [SerializeField] private float deathFps = 6f;

        [Tooltip("Victory celebratory loop speed.")]
        [SerializeField] private float victoryFps = 6f;

        [Header("Sprite Scaling")]
        [Tooltip("Pixels per unit for crisp pixel scaling. Lower = bigger on screen.")]
        [SerializeField] private float pixelsPerUnit = 29.5f;

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
        private readonly List<Sprite> hurtFrames = new List<Sprite>();
        private readonly List<Sprite> knockoutFrames = new List<Sprite>();
        private readonly List<Sprite> victoryFrames = new List<Sprite>();
        private Sprite crouchSprite;

        // Super Saiyan Animation Sequences (Goku)
        private readonly List<Sprite> ssjIdleFrames = new List<Sprite>();
        private readonly List<Sprite> ssjWalkFrames = new List<Sprite>();
        private readonly List<Sprite> ssjPunchFrames = new List<Sprite>();
        private readonly List<Sprite> ssjHeavyFrames = new List<Sprite>();
        private readonly List<Sprite> ssjSkillFrames = new List<Sprite>();
        private readonly List<Sprite> ssjUltimateFrames = new List<Sprite>();
        private readonly List<Sprite> ssjJumpFrames = new List<Sprite>();
        private readonly List<Sprite> ssjMeditateFrames = new List<Sprite>();
        private readonly List<Sprite> ssjHurtFrames = new List<Sprite>();
        private readonly List<Sprite> ssjKnockoutFrames = new List<Sprite>();
        private readonly List<Sprite> ssjVictoryFrames = new List<Sprite>();
        private Sprite ssjCrouchSprite;

        private float animTimer;
        private int currentFrameIndex;
        private FighterState lastState = FighterState.Idle;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (controller == null) controller = GetComponent<FighterController>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();

            // Auto-load idle sprite in Edit Mode so fighter is visible on the 2D scene canvas
            if (idleFrames.Count == 0)
            {
                string charName = "Goku";
                if (gameObject.name.ToLower().Contains("saitama")) charName = "Saitama";
                else if (gameObject.name.ToLower().Contains("nezuko")) charName = "Nezuko";
                else if (gameObject.name.ToLower().Contains("tsunade")) charName = "Tsunade";
                LoadCharacterFrames(charName);
            }
        }

        private void Update()
        {
            if (idleFrames.Count == 0) return;

            // In Edit Mode, keep the initial idle pose visible
            if (!Application.isPlaying)
            {
                if (spriteRenderer != null && idleFrames.Count > 0 && spriteRenderer.sprite == null)
                {
                    spriteRenderer.sprite = idleFrames[0];
                }
                return;
            }

            UpdateAnimation();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Total real duration of one complete turn of the character's victory animation.
        /// </summary>
        public float VictoryDuration
        {
            get
            {
                var vic = GetFrames(ssjVictoryFrames, victoryFrames);
                return (vic != null && vic.Count > 0) ? (float)vic.Count / victoryFps : 1.5f;
            }
        }

        /// <summary>
        /// Returns true if the victory animation has completed all frames of its single turn.
        /// </summary>
        public bool IsVictoryFinished
        {
            get
            {
                var vic = GetFrames(ssjVictoryFrames, victoryFrames);
                return controller != null && controller.CurrentState == FighterState.Victory &&
                       vic != null && vic.Count > 0 && currentFrameIndex >= vic.Count - 1;
            }
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
                pixelsPerUnit = 34.5f; // 55px body / 28 = ~1.96 world units
            }
            else if (characterFolder.ToLower().Contains("nezuko"))
            {
                pixelsPerUnit = 28.0f; // compact chibi & proportional demon
            }
            else if (characterFolder.ToLower().Contains("tsunade"))
            {
                pixelsPerUnit = 33.0f; // 70px body / 33 = 2.12 world units
            }
            else
            {
                pixelsPerUnit = 29.5f; // 47px body / 24 = ~1.96 world units
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
            LoadSequence(framesDir, "hurt", hurtFrames);
            LoadSequence(framesDir, "death", knockoutFrames);
            if (knockoutFrames.Count == 0) LoadSequence(framesDir, "knockout", knockoutFrames);
            if (knockoutFrames.Count == 0) LoadSequence(framesDir, "ko", knockoutFrames);
            LoadSequence(framesDir, "victory", victoryFrames);

            // Check for new_ultimate frames (e.g. Saitama serious punch windup)
            List<Sprite> newUlt = new List<Sprite>();
            LoadSequence(framesDir, "new_ultimate", newUlt);
            if (newUlt.Count > 0)
            {
                ultimateFrames.InsertRange(0, newUlt);
            }

            if (characterFolder.ToLower().Contains("tsunade"))
            {
                // Tsunade's ultimate is Katsuyu (spawned by TsunadeKatsuyuCinematic).
                // For Tsunade's own body during ultimate, keep her in her focused summoning stance!
                ultimateFrames.Clear();
                if (meditateFrames.Count > 0)
                {
                    ultimateFrames.AddRange(meditateFrames);
                }
                else if (idleFrames.Count > 0)
                {
                    ultimateFrames.AddRange(idleFrames);
                }
            }

            // Load crouch pose
            crouchSprite = LoadSingleOrFirst(framesDir, "crouch");

            // Load Demon Form frames if Nezuko
            if (characterFolder.ToLower().Contains("nezuko"))
            {
                string demonDir = Path.Combine(Application.dataPath, "Sprites", "Fighters", "Nezuko", "Frames_Demon");
                if (Directory.Exists(demonDir))
                {
                    LoadSequence(demonDir, "idle", ssjIdleFrames);
                    LoadSequence(demonDir, "walk", ssjWalkFrames);
                    LoadSequence(demonDir, "punch", ssjPunchFrames);
                    LoadSequence(demonDir, "heavy", ssjHeavyFrames);
                    LoadSequence(demonDir, "skill", ssjSkillFrames);
                    LoadSequence(demonDir, "ultimate", ssjUltimateFrames);
                    LoadSequence(demonDir, "jump", ssjJumpFrames);
                    LoadSequence(demonDir, "meditate", ssjMeditateFrames);
                    LoadSequence(demonDir, "hurt", ssjHurtFrames);
                    LoadSequence(demonDir, "death", ssjKnockoutFrames);
                    if (ssjKnockoutFrames.Count == 0) LoadSequence(demonDir, "knockout", ssjKnockoutFrames);
                    if (ssjKnockoutFrames.Count == 0) LoadSequence(demonDir, "ko", ssjKnockoutFrames);
                    LoadSequence(demonDir, "victory", ssjVictoryFrames);
                    ssjCrouchSprite = LoadSingleOrFirst(demonDir, "crouch");

                    Debug.Log($"[FighterAnimator] Loaded Nezuko Demon Form: {ssjIdleFrames.Count} idle, {ssjWalkFrames.Count} walk, " +
                              $"{ssjPunchFrames.Count} punch, {ssjHeavyFrames.Count} heavy, {ssjUltimateFrames.Count} ult frames.");
                }
            }

            // Load Super Saiyan frames if available (for Goku)
            if (characterFolder.ToLower().Contains("goku"))
            {
                string ssjDir = Path.Combine(Application.dataPath, "Sprites", "Fighters", "Goku", "Frames_SSJ");
                if (Directory.Exists(ssjDir))
                {
                    LoadSequence(ssjDir, "idle", ssjIdleFrames);
                    LoadSequence(ssjDir, "walk", ssjWalkFrames);
                    LoadSequence(ssjDir, "punch", ssjPunchFrames);
                    LoadSequence(ssjDir, "heavy", ssjHeavyFrames);
                    LoadSequence(ssjDir, "skill", ssjSkillFrames);
                    LoadSequence(ssjDir, "ultimate", ssjUltimateFrames);
                    LoadSequence(ssjDir, "jump", ssjJumpFrames);
                    LoadSequence(ssjDir, "meditate", ssjMeditateFrames);
                    LoadSequence(ssjDir, "hurt", ssjHurtFrames);
                    LoadSequence(ssjDir, "death", ssjKnockoutFrames);
                    if (ssjKnockoutFrames.Count == 0) LoadSequence(ssjDir, "knockout", ssjKnockoutFrames);
                    if (ssjKnockoutFrames.Count == 0) LoadSequence(ssjDir, "ko", ssjKnockoutFrames);
                    LoadSequence(ssjDir, "victory", ssjVictoryFrames);
                    ssjCrouchSprite = LoadSingleOrFirst(ssjDir, "crouch");

                    Debug.Log($"[FighterAnimator] Loaded Goku SSJ: {ssjIdleFrames.Count} idle, {ssjWalkFrames.Count} walk, " +
                              $"{ssjPunchFrames.Count} punch, {ssjHeavyFrames.Count} heavy, {ssjSkillFrames.Count} skill frames.");
                }
            }

            // Set initial sprite
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && idleFrames.Count > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }

            Debug.Log($"[FighterAnimator] Loaded {characterFolder}: {idleFrames.Count} idle, {walkFrames.Count} walk, " +
                      $"{jumpFrames.Count} jump, {punchFrames.Count} punch, {heavyFrames.Count} heavy, " +
                      $"{skillFrames.Count} skill, {ultimateFrames.Count} ult frames.");
        }

        /// <summary>
        /// Instantly resets frame index, animation timer, and displays the first idle frame.
        /// Useful when starting a new round or recovering from hitstun.
        /// </summary>
        public void ResetToIdle()
        {
            currentFrameIndex = 0;
            animTimer = 0f;
            lastState = FighterState.Idle;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && idleFrames.Count > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
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

            bool isSSJ = controller != null && controller.IsSuperSaiyan;

            switch (currentState)
            {
                case FighterState.Idle:
                case FighterState.Exhausted:
                    PlayLoop(GetFrames(ssjIdleFrames, idleFrames), idleFps);
                    break;

                case FighterState.Walking:
                    PlayLoop(GetFrames(ssjWalkFrames, walkFrames), walkFps);
                    break;

                case FighterState.Jumping:
                case FighterState.Falling:
                    UpdateJumpPose();
                    break;

                case FighterState.Crouching:
                    bool isTransformed = controller != null && (controller.IsSuperSaiyan || controller.IsDemonForm);
                    Sprite cSprite = (isTransformed && ssjCrouchSprite != null) ? ssjCrouchSprite : crouchSprite;
                    if (cSprite != null) spriteRenderer.sprite = cSprite;
                    break;

                case FighterState.Meditating:
                    var med = GetFrames(ssjMeditateFrames, meditateFrames);
                    if (med.Count > 0) PlayLoop(med, meditateFps);
                    break;

                case FighterState.LightAttack:
                    var lightFrames = GetFrames(ssjPunchFrames, punchFrames);
                    float lightSpeed = lightFrames.Count > 0 ? Mathf.Max(6f, (float)lightFrames.Count / 0.44f) : lightAttackFps;
                    PlayOnce(lightFrames, lightSpeed);
                    break;

                case FighterState.HeavyAttack:
                    var heavy = GetFrames(ssjHeavyFrames, heavyFrames);
                    var heavyList = heavy.Count > 0 ? heavy : GetFrames(ssjPunchFrames, punchFrames);
                    float heavySpeed = heavyList.Count > 0 ? Mathf.Max(5f, (float)heavyList.Count / 0.88f) : heavyAttackFps;
                    PlayOnce(heavyList, heavySpeed);
                    break;

                case FighterState.Skill:
                    if (gameObject.name.ToLower().Contains("saitama"))
                    {
                        var skill = GetFrames(ssjSkillFrames, skillFrames);
                        var skillList = skill.Count > 0 ? skill : punchFrames;
                        PlayLoop(skillList, 11f);
                    }
                    else if (gameObject.name.ToLower().Contains("nezuko"))
                    {
                        var skill = GetFrames(ssjSkillFrames, skillFrames);
                        PlayOnce(skill, 8f);
                    }
                    else if (gameObject.name.ToLower().Contains("tsunade"))
                    {
                        var skill = GetFrames(ssjSkillFrames, skillFrames);
                        PlayOnce(skill, 8.5f);
                    }
                    else
                    {
                        var idle = GetFrames(ssjIdleFrames, idleFrames);
                        PlayLoop(idle, idleFps);
                    }
                    break;

                case FighterState.Ultimate:
                    var ult = GetFrames(ssjUltimateFrames, ultimateFrames);
                    var ultList = ult.Count > 0 ? ult : GetFrames(ssjHeavyFrames, heavyFrames);
                    // Match animation progression to exact voice audio duration!
                    float ultDuration = 3.80f;
                    if (gameObject.name.ToLower().Contains("goku")) ultDuration = 3.36f;
                    else if (gameObject.name.ToLower().Contains("nezuko")) ultDuration = 1.0f;
                    else if (gameObject.name.ToLower().Contains("tsunade")) ultDuration = 1.10f;
                    float ultSpeed = ultList.Count > 0 ? Mathf.Max(2.5f, (float)ultList.Count / ultDuration) : ultimateFps;
                    PlayOnce(ultList, ultSpeed);
                    break;

                case FighterState.HitStun:
                case FighterState.KnockBack:
                    var hurt = GetFrames(ssjHurtFrames, hurtFrames);
                    if (hurt.Count > 0) PlayOnce(hurt, hurtFps);
                    break;

                case FighterState.Knockout:
                    var ko = GetFrames(ssjKnockoutFrames, knockoutFrames);
                    if (ko.Count > 0)
                    {
                        // Unhurried cinematic collapse over 3.8 real seconds
                        float koSpeed = Mathf.Max(2.5f, (float)ko.Count / 3.8f);
                        PlayOnce(ko, koSpeed, unscaled: true);
                    }
                    break;

                case FighterState.Victory:
                    var vic = GetFrames(ssjVictoryFrames, victoryFrames);
                    if (vic.Count > 0) PlayOnce(vic, victoryFps, unscaled: true);
                    break;
            }
        }

        private List<Sprite> GetFrames(List<Sprite> ssjList, List<Sprite> baseList)
        {
            bool isTransformed = controller != null && (controller.IsSuperSaiyan || controller.IsDemonForm);
            return (isTransformed && ssjList != null && ssjList.Count > 0) ? ssjList : baseList;
        }

        /// <summary>
        /// Selects the jump sprite dynamically based on vertical physics velocity.
        /// </summary>
        private void UpdateJumpPose()
        {
            var jFrames = GetFrames(ssjJumpFrames, jumpFrames);
            if (jFrames == null || jFrames.Count == 0) return;

            float vy = rb != null ? rb.linearVelocity.y : 0f;

            if (jFrames.Count >= 5)
            {
                if (vy > 4.0f)
                {
                    spriteRenderer.sprite = jFrames[0]; // Launch
                }
                else if (vy > 1.0f)
                {
                    spriteRenderer.sprite = jFrames[1]; // Ascending
                }
                else if (Mathf.Abs(vy) <= 1.0f)
                {
                    spriteRenderer.sprite = jFrames[2]; // Apex hangtime
                }
                else if (vy >= -5.0f)
                {
                    spriteRenderer.sprite = jFrames[3]; // Descent
                }
                else
                {
                    spriteRenderer.sprite = jFrames[jFrames.Count - 1]; // Fast descent
                }
            }
            else
            {
                // Smooth normalized mapping for any count of jump frames
                float t = Mathf.InverseLerp(12f, -12f, vy);
                int frameIdx = Mathf.Clamp(Mathf.FloorToInt(t * jFrames.Count), 0, jFrames.Count - 1);
                spriteRenderer.sprite = jFrames[frameIdx];
            }
        }

        private void PlayLoop(List<Sprite> frames, float fps, bool unscaled = false)
        {
            if (frames == null || frames.Count == 0) return;

            float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            animTimer += dt;
            float frameDuration = 1f / fps;

            if (animTimer >= frameDuration)
            {
                animTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Count;
            }

            spriteRenderer.sprite = frames[currentFrameIndex];
        }

        private void PlayOnce(List<Sprite> frames, float fps, bool unscaled = false)
        {
            if (frames == null || frames.Count == 0) return;

            float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            animTimer += dt;
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
