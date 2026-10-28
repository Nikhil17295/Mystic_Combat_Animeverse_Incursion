using System.Collections;
using System.IO;
using UnityEngine;
using MysticCombat.Core;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Master Arena Background & Environmental VFX Controller.
    /// Manages 3-Round dynamic stage evolution:
    /// - Round 1: Grand Martial Arts Tournament (Sunset Sanctuary & Mountain Pagodas)
    /// - Round 2: Battle on the Moon (Lunar Crater Regolith, Earth, Space Rift)
    /// - Round 3: Multiverse Dimensional Cataclysm (Mirror Dimension Shatter, Tao Mandala, Saiyan Ki Auroras)
    /// Controls lively animated pixel flame braziers, ember particle emitters, and rotating Tao Mandala.
    /// </summary>
    public class ArenaBackgroundManager : MonoBehaviour
    {
        #region Singleton

        public static ArenaBackgroundManager Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [Header("Backdrop Renderers")]
        [Tooltip("Primary SpriteRenderer displaying the current round's stage.")]
        [SerializeField] private SpriteRenderer primaryBackdrop;

        [Tooltip("Secondary SpriteRenderer used for smooth alpha cross-fading between stages.")]
        [SerializeField] private SpriteRenderer crossfadeBackdrop;

        [Header("Stage Sprites (Optional Inspector Assignment)")]
        [SerializeField] private Sprite round1StageSprite;
        [SerializeField] private Sprite round2StageSprite;
        [SerializeField] private Sprite round3StageSprite;

        [Header("Brazier Settings")]
        [SerializeField] private Vector3 leftBrazierPos = new Vector3(-5.5f, -2.4f, 0f);
        [SerializeField] private Vector3 rightBrazierPos = new Vector3(5.5f, -2.4f, 0f);

        [Header("Doctor Strange Mandala")]
        [SerializeField] private Vector3 mandalaPos = new Vector3(0.5f, 0.7f, 0f);
        [SerializeField] private float mandalaRotationSpeed = 16f;

        #endregion

        #region Private Fields

        private BrazierFlameAnimator leftFlameAnim;
        private BrazierFlameAnimator rightFlameAnim;
        private ParticleSystem leftEmberPS;
        private ParticleSystem rightEmberPS;
        private GameObject mandalaObj;
        private SpriteRenderer mandalaRenderer;
        private ParticleSystem cosmicDustPS;

        private int activeRound = 1;
        private Coroutine activeCrossfadeCoroutine;
        private MatchManager matchManager;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureBackdropRenderers();
            LoadStageSpritesIfNull();
            BuildBraziers();
            BuildTaoMandala();
            BuildCosmicDustEmitter();

            // Set initial Round 1 stage
            ApplyRoundVisuals(1, instant: true);
        }

        private void OnEnable()
        {
            StartCoroutine(FindAndBindMatchManager());
        }

        private void OnDisable()
        {
            if (matchManager != null)
            {
                matchManager.OnRoundStart -= HandleRoundStart;
            }
        }

        private void Update()
        {
            // Rotate the Doctor Strange Tao Mandala in space/rift stages
            if (mandalaObj != null && mandalaObj.activeSelf)
            {
                mandalaObj.transform.Rotate(0f, 0f, -mandalaRotationSpeed * Time.deltaTime);
            }
        }

        #endregion

        #region MatchManager Event Handling

        private IEnumerator FindAndBindMatchManager()
        {
            // Yield until MatchManager is present in scene
            while (matchManager == null)
            {
                matchManager = Object.FindAnyObjectByType<MatchManager>();
                if (matchManager != null) break;
                yield return new WaitForSeconds(0.1f);
            }

            matchManager.OnRoundStart -= HandleRoundStart;
            matchManager.OnRoundStart += HandleRoundStart;

            // Sync with current round
            if (matchManager.CurrentRound > 0)
            {
                ApplyRoundVisuals(matchManager.CurrentRound, instant: true);
            }
        }

        private void HandleRoundStart(int roundNumber)
        {
            Debug.Log($"<color=#ffaa00>[ArenaBackgroundManager]</color> Transitioning arena to Round {roundNumber} atmosphere!");
            ApplyRoundVisuals(roundNumber, instant: false);
        }

        #endregion

        #region Stage Application & Cross-Fading

        /// <summary>
        /// Switches backdrop, flame palettes, particle emission, and mandala presence for the target round.
        /// </summary>
        public void ApplyRoundVisuals(int roundNumber, bool instant = false)
        {
            activeRound = roundNumber;
            Sprite targetSprite = GetStageSprite(roundNumber);

            if (targetSprite != null && primaryBackdrop != null)
            {
                if (instant)
                {
                    primaryBackdrop.sprite = targetSprite;
                    primaryBackdrop.color = Color.white;
                    if (crossfadeBackdrop != null) crossfadeBackdrop.color = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    if (activeCrossfadeCoroutine != null) StopCoroutine(activeCrossfadeCoroutine);
                    activeCrossfadeCoroutine = StartCoroutine(CrossfadeToSprite(targetSprite, 1.2f));
                }
            }

            // Update Braziers & VFX
            BrazierFlameAnimator.FlamePalette palette;
            Color emberColor;
            float emberRate;

            if (roundNumber == 1)
            {
                palette = BrazierFlameAnimator.FlamePalette.Orange;
                emberColor = new Color(1.0f, 0.65f, 0.2f, 0.9f);
                emberRate = 14f;
                if (mandalaObj != null) mandalaObj.SetActive(false);
                if (cosmicDustPS != null) cosmicDustPS.gameObject.SetActive(false);
            }
            else if (roundNumber == 2)
            {
                // Lunar Battle / Space Rift
                palette = BrazierFlameAnimator.FlamePalette.Blue;
                emberColor = new Color(0.35f, 0.8f, 1.0f, 0.95f);
                emberRate = 22f;
                if (mandalaObj != null)
                {
                    mandalaObj.SetActive(true);
                    if (mandalaRenderer != null) mandalaRenderer.color = new Color(1.0f, 0.75f, 0.2f, 0.45f); // Subtle amber/gold
                }
                if (cosmicDustPS != null)
                {
                    cosmicDustPS.gameObject.SetActive(true);
                    var main = cosmicDustPS.main;
                    main.startColor = new Color(0.6f, 0.8f, 1.0f, 0.5f);
                }
            }
            else
            {
                // Round 3+ : Cataclysm / Super Saiyan Golden Overdrive (No Doctor Strange Mandala)
                palette = BrazierFlameAnimator.FlamePalette.Gold;
                emberColor = new Color(1.0f, 0.9f, 0.3f, 1.0f);
                emberRate = 38f;
                if (mandalaObj != null)
                {
                    mandalaObj.SetActive(false); // Doctor Strange Mandala is disabled for Round 3
                }
                if (cosmicDustPS != null)
                {
                    cosmicDustPS.gameObject.SetActive(true);
                    var main = cosmicDustPS.main;
                    main.startColor = new Color(1.0f, 0.5f, 0.2f, 0.75f);
                }
            }

            if (leftFlameAnim != null) leftFlameAnim.SetPalette(palette);
            if (rightFlameAnim != null) rightFlameAnim.SetPalette(palette);

            SetEmberSettings(leftEmberPS, emberColor, emberRate);
            SetEmberSettings(rightEmberPS, emberColor, emberRate);
        }

        private IEnumerator CrossfadeToSprite(Sprite newSprite, float duration)
        {
            if (crossfadeBackdrop == null || primaryBackdrop == null) yield break;

            // Set crossfade to new sprite starting transparent
            crossfadeBackdrop.sprite = newSprite;
            crossfadeBackdrop.color = new Color(1f, 1f, 1f, 0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                crossfadeBackdrop.color = new Color(1f, 1f, 1f, t);
                yield return null;
            }

            // Swap: primary now holds the new sprite, crossfade fades back to invisible
            primaryBackdrop.sprite = newSprite;
            primaryBackdrop.color = Color.white;
            crossfadeBackdrop.color = new Color(1f, 1f, 1f, 0f);
            activeCrossfadeCoroutine = null;
        }

        private Sprite GetStageSprite(int roundNumber)
        {
            if (roundNumber == 1) return round1StageSprite;
            if (roundNumber == 2) return round2StageSprite;
            return round3StageSprite;
        }

        #endregion

        #region Public VFX Trigger

        /// <summary>
        /// Causes both braziers to dramatically surge when a major combat blow or ultimate explodes.
        /// </summary>
        public void TriggerBrazierSurge(float multiplier = 1.35f)
        {
            if (leftFlameAnim != null) leftFlameAnim.TriggerSurge(multiplier);
            if (rightFlameAnim != null) rightFlameAnim.TriggerSurge(multiplier);

            if (leftEmberPS != null) leftEmberPS.Emit(12);
            if (rightEmberPS != null) rightEmberPS.Emit(12);
        }

        #endregion

        #region Arena Construction & Auto-Setup

        private void EnsureBackdropRenderers()
        {
            // Find existing Backdrop_TournamentStage or create one
            Transform existingBg = transform.Find("Backdrop_TournamentStage");
            if (existingBg != null)
            {
                primaryBackdrop = existingBg.GetComponent<SpriteRenderer>();
            }

            if (primaryBackdrop == null)
            {
                GameObject bgObj = new GameObject("Backdrop_TournamentStage");
                bgObj.transform.SetParent(transform);
                bgObj.transform.localPosition = new Vector3(0f, -0.6f, 0f);
                bgObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                primaryBackdrop = bgObj.AddComponent<SpriteRenderer>();
                primaryBackdrop.sortingOrder = -10;
            }

            // Ensure secondary crossfade renderer
            Transform xfadeObj = transform.Find("Backdrop_Crossfade");
            if (xfadeObj != null)
            {
                crossfadeBackdrop = xfadeObj.GetComponent<SpriteRenderer>();
            }
            else
            {
                GameObject xf = new GameObject("Backdrop_Crossfade");
                xf.transform.SetParent(transform);
                xf.transform.localPosition = primaryBackdrop.transform.localPosition;
                xf.transform.localScale = primaryBackdrop.transform.localScale;
                crossfadeBackdrop = xf.AddComponent<SpriteRenderer>();
                crossfadeBackdrop.sortingOrder = -9;
                crossfadeBackdrop.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        private void LoadStageSpritesIfNull()
        {
            string arenaDir = Path.Combine(Application.dataPath, "Sprites", "Arena");
            if (round1StageSprite == null) round1StageSprite = LoadSpriteFile(Path.Combine(arenaDir, "arena_stage_round1.png"));
            if (round2StageSprite == null) round2StageSprite = LoadSpriteFile(Path.Combine(arenaDir, "arena_stage_round2.png"));
            if (round3StageSprite == null) round3StageSprite = LoadSpriteFile(Path.Combine(arenaDir, "arena_stage_round3.png"));

            // Fallback for round 1 if specific file missing
            if (round1StageSprite == null) round1StageSprite = LoadSpriteFile(Path.Combine(arenaDir, "arena_stage_background.png"));
        }

        private Sprite LoadSpriteFile(string path)
        {
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.LoadImage(bytes);

            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        private void BuildBraziers()
        {
            // Build Left and Right Braziers
            string standPath = Path.Combine(Application.dataPath, "Sprites", "Arena", "brazier_stand_pixel.png");
            Sprite standSprite = LoadSpriteFile(standPath);

            leftFlameAnim = CreateBrazierObject("Left_MysticBrazier", leftBrazierPos, standSprite, out leftEmberPS);
            rightFlameAnim = CreateBrazierObject("Right_MysticBrazier", rightBrazierPos, standSprite, out rightEmberPS);
        }

        private BrazierFlameAnimator CreateBrazierObject(string objName, Vector3 pos, Sprite standSprite, out ParticleSystem emberPS)
        {
            Transform existing = transform.Find(objName);
            if (existing != null) Destroy(existing.gameObject);

            GameObject root = new GameObject(objName);
            root.transform.SetParent(transform);
            root.transform.localPosition = pos;

            // 1. Stone Stand Base
            GameObject standObj = new GameObject("StandVisual");
            standObj.transform.SetParent(root.transform, false);
            standObj.transform.localPosition = Vector3.zero;
            SpriteRenderer standSR = standObj.AddComponent<SpriteRenderer>();
            standSR.sprite = standSprite;
            standSR.sortingOrder = -8; // In front of backdrop (-10), behind fighters (0)

            // 2. Animated Flame Sprite
            GameObject flameObj = new GameObject("FlameVisual");
            flameObj.transform.SetParent(root.transform, false);
            // Position atop bowl rim (stand is 64px tall @ 40 PPU = 1.6m, pivot centered = +0.8m top)
            flameObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            SpriteRenderer flameSR = flameObj.AddComponent<SpriteRenderer>();
            flameSR.sortingOrder = -7;
            BrazierFlameAnimator flameAnim = flameObj.AddComponent<BrazierFlameAnimator>();

            // 3. Upward Ember Particle System
            GameObject psObj = new GameObject("EmberParticles");
            psObj.transform.SetParent(root.transform, false);
            psObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            emberPS = SetupEmberParticleSystem(psObj);

            return flameAnim;
        }

        private ParticleSystem SetupEmberParticleSystem(GameObject targetObj)
        {
            ParticleSystem ps = targetObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(1.0f, 0.65f, 0.2f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.15f; // Gentle upward lift

            var emission = ps.emission;
            emission.rateOverTime = 14f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;

            var velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1.0f);
            curve.AddKey(1f, 0.1f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            ParticleSystemRenderer rend = targetObj.GetComponent<ParticleSystemRenderer>();
            rend.sortingOrder = -7;
            rend.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));

            return ps;
        }

        private void SetEmberSettings(ParticleSystem ps, Color color, float rate)
        {
            if (ps == null) return;
            var main = ps.main;
            main.startColor = color;
            var emission = ps.emission;
            emission.rateOverTime = rate;
        }

        private void BuildTaoMandala()
        {
            Transform existing = transform.Find("DoctorStrange_TaoMandala");
            if (existing != null) Destroy(existing.gameObject);

            mandalaObj = new GameObject("DoctorStrange_TaoMandala");
            mandalaObj.transform.SetParent(transform);
            mandalaObj.transform.localPosition = mandalaPos;
            mandalaObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            mandalaRenderer = mandalaObj.AddComponent<SpriteRenderer>();
            mandalaRenderer.sortingOrder = -9; // In front of backdrop (-10), behind braziers (-8) and fighters (0)

            string mandalaPath = Path.Combine(Application.dataPath, "Sprites", "UI", "time_stone_mandala.png");
            if (File.Exists(mandalaPath))
            {
                byte[] bytes = File.ReadAllBytes(mandalaPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.LoadImage(bytes);
                mandalaRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            mandalaRenderer.color = new Color(1.0f, 0.8f, 0.25f, 0.5f);
            mandalaObj.SetActive(false); // Only active in Round 2 and Round 3
        }

        private void BuildCosmicDustEmitter()
        {
            Transform existing = transform.Find("CosmicDustField");
            if (existing != null) Destroy(existing.gameObject);

            GameObject dustObj = new GameObject("CosmicDustField");
            dustObj.transform.SetParent(transform);
            dustObj.transform.localPosition = new Vector3(0f, 0f, 0f);

            cosmicDustPS = dustObj.AddComponent<ParticleSystem>();
            var main = cosmicDustPS.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor = new Color(0.6f, 0.8f, 1.0f, 0.4f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = cosmicDustPS.emission;
            emission.rateOverTime = 12f;

            var shape = cosmicDustPS.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 6f, 1f);

            ParticleSystemRenderer rend = dustObj.GetComponent<ParticleSystemRenderer>();
            rend.sortingOrder = -8;
            rend.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));

            dustObj.SetActive(false);
        }

        #endregion
    }
}
