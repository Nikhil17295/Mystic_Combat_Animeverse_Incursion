using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Master Visual Effects & Combat Juice Engine for Mystic Combat.
    /// Manages pooled particle hit sparks, starbursts, expanding shockwaves,
    /// dynamic dust puffs, hitstop micro-pauses, camera shake, and impact flashes.
    /// Completely self-bootstrapping and GC-allocation-free during combat.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        #region Singleton

        public static VFXManager Instance { get; private set; }

        #endregion

        #region Serialized Tuning

        [Header("Camera Shake & Hitstop")]
        [Tooltip("Default camera shake intensity for heavy hits.")]
        [SerializeField] private float defaultShakeIntensity = 0.22f;

        [Tooltip("Default hitstop duration for visceral impact feel (seconds).")]
        [SerializeField] private float defaultHitstopSeconds = 0.065f;

        #endregion

        #region Internal Resources & Pools

        private Texture2D hitSparkStarTex;
        private Texture2D hitSparkOrbTex;
        private Texture2D shockwaveRingTex;
        private Texture2D dustPuffTex;

        private Material particleAdditiveMat;
        private Material particleAlphaBlendMat;

        private readonly Queue<ParticleSystem> lightSparkPool = new Queue<ParticleSystem>();
        private readonly Queue<ParticleSystem> heavySparkPool = new Queue<ParticleSystem>();
        private readonly Queue<ParticleSystem> dustPool = new Queue<ParticleSystem>();
        private readonly Queue<GameObject> shockwavePool = new Queue<GameObject>();

        private Transform poolRoot;
        private Camera targetCamera;
        private Vector3 initialCamPos;
        private Coroutine activeShakeCoroutine;
        private Coroutine activeHitstopCoroutine;
        private bool isHitStopActive;
        private float originalTimeScale = 1.0f;
        private Canvas flashCanvas;
        private UnityEngine.UI.Image flashOverlayImage;

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

            targetCamera = Camera.main;
            if (targetCamera != null)
            {
                initialCamPos = targetCamera.transform.position;
            }

            poolRoot = new GameObject("VFX_Pool_Root").transform;
            poolRoot.SetParent(transform);

            LoadTextures();
            CreateMaterials();
            BuildImpactFlashCanvas();
            PrewarmPools();
        }

        private void OnDisable()
        {
            if (isHitStopActive)
            {
                Time.timeScale = originalTimeScale > 0.1f ? originalTimeScale : 1.0f;
                isHitStopActive = false;
                activeHitstopCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (isHitStopActive)
            {
                Time.timeScale = originalTimeScale > 0.1f ? originalTimeScale : 1.0f;
                isHitStopActive = false;
            }
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Texture & Material Setup

        private void LoadTextures()
        {
            string fxDir = Path.Combine(Application.dataPath, "Sprites", "Effects");
            hitSparkStarTex = LoadTextureFromFile(Path.Combine(fxDir, "hit_spark_star.png"));
            hitSparkOrbTex = LoadTextureFromFile(Path.Combine(fxDir, "hit_spark_orb.png"));
            shockwaveRingTex = LoadTextureFromFile(Path.Combine(fxDir, "shockwave_ring.png"));
            dustPuffTex = LoadTextureFromFile(Path.Combine(fxDir, "dust_puff.png"));
        }

        private Texture2D LoadTextureFromFile(string path)
        {
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.LoadImage(bytes);
            return tex;
        }

        private void CreateMaterials()
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null) spriteShader = Shader.Find("Unlit/Transparent");

            particleAdditiveMat = new Material(spriteShader) { color = Color.white };
            particleAlphaBlendMat = new Material(spriteShader) { color = Color.white };
        }

        private void BuildImpactFlashCanvas()
        {
            GameObject canvasObj = new GameObject("VFX_Flash_Canvas");
            canvasObj.transform.SetParent(transform);
            flashCanvas = canvasObj.AddComponent<Canvas>();
            flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            flashCanvas.sortingOrder = 999; // Above HUD and combat

            GameObject imgObj = new GameObject("Flash_Overlay");
            imgObj.transform.SetParent(canvasObj.transform, false);
            flashOverlayImage = imgObj.AddComponent<UnityEngine.UI.Image>();
            flashOverlayImage.color = new Color(1f, 1f, 1f, 0f); // Invisible initially

            RectTransform rt = imgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        #endregion

        #region Prewarming Pools

        private void PrewarmPools()
        {
            for (int i = 0; i < 8; i++)
            {
                lightSparkPool.Enqueue(CreateParticleEmitter("LightSpark_Emitter", isHeavy: false));
                heavySparkPool.Enqueue(CreateParticleEmitter("HeavySpark_Emitter", isHeavy: true));
                dustPool.Enqueue(CreateDustEmitter("Dust_Emitter"));
                shockwavePool.Enqueue(CreateShockwaveObject("Shockwave_Object"));
            }
        }

        private ParticleSystem CreateParticleEmitter(string emitterName, bool isHeavy)
        {
            GameObject obj = new GameObject(emitterName);
            obj.transform.SetParent(poolRoot);
            obj.SetActive(false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = isHeavy ? 0.32f : 0.20f;
            main.startSpeed = isHeavy ? new ParticleSystem.MinMaxCurve(6f, 14f) : new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = isHeavy ? new ParticleSystem.MinMaxCurve(0.45f, 0.9f) : new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor = isHeavy
                ? new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.4f, 1f), new Color(1f, 0.35f, 0.05f, 1f))
                : new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 1f), new Color(1f, 0.85f, 0.2f, 1f));
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, isHeavy ? (short)24 : (short)14) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.material = particleAdditiveMat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (hitSparkStarTex != null)
            {
                renderer.material.mainTexture = hitSparkStarTex;
            }

            return ps;
        }

        private ParticleSystem CreateDustEmitter(string emitterName)
        {
            GameObject obj = new GameObject(emitterName);
            obj.transform.SetParent(poolRoot);
            obj.SetActive(false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startColor = new Color(0.9f, 0.88f, 0.82f, 0.65f);
            main.gravityModifier = -0.2f; // Soft float up

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.5f);
            curve.AddKey(0.4f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.material = particleAlphaBlendMat;
            if (dustPuffTex != null)
            {
                renderer.material.mainTexture = dustPuffTex;
            }

            return ps;
        }

        private GameObject CreateShockwaveObject(string objName)
        {
            GameObject obj = new GameObject(objName);
            obj.transform.SetParent(poolRoot);
            obj.SetActive(false);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            if (shockwaveRingTex != null)
            {
                sr.sprite = Sprite.Create(shockwaveRingTex, new Rect(0, 0, shockwaveRingTex.width, shockwaveRingTex.height), new Vector2(0.5f, 0.5f), 64f);
            }
            sr.color = new Color(1f, 0.9f, 0.5f, 0.85f);
            return obj;
        }

        #endregion

        #region Public API — Spawning & Triggers

        /// <summary>
        /// Plays an authentic fighting game hit spark at the collision point.
        /// </summary>
        public void PlayHitSpark(Vector2 worldPos, bool isHeavy = false, bool triggerHitStop = true, float shakeMultiplier = 1f)
        {
            var pool = isHeavy ? heavySparkPool : lightSparkPool;
            if (pool.Count == 0) return;

            ParticleSystem ps = pool.Dequeue();
            ps.transform.position = worldPos;
            ps.gameObject.SetActive(true);
            ps.Play();

            StartCoroutine(RecycleParticle(ps, pool, isHeavy ? 0.35f : 0.22f));

            if (isHeavy)
            {
                PlayShockwave(worldPos, scale: 1.25f);
                if (shakeMultiplier > 0f) TriggerCameraShake(defaultShakeIntensity * 1.5f * shakeMultiplier, 0.22f);
                if (triggerHitStop) TriggerHitStop(defaultHitstopSeconds * 1.4f);
                ArenaBackgroundManager.Instance?.TriggerBrazierSurge(1.4f);
            }
            else
            {
                if (shakeMultiplier > 0f) TriggerCameraShake(defaultShakeIntensity * 0.6f * shakeMultiplier, 0.12f);
                if (triggerHitStop) TriggerHitStop(defaultHitstopSeconds);
            }
        }

        /// <summary>
        /// Spawns stylized anime dust puff at world coordinate (e.g. feet on jump or land).
        /// </summary>
        public void PlayDustPuff(Vector2 worldPos)
        {
            if (dustPool.Count == 0) return;

            ParticleSystem ps = dustPool.Dequeue();
            ps.transform.position = worldPos;
            ps.gameObject.SetActive(true);
            ps.Play();

            StartCoroutine(RecycleParticle(ps, dustPool, 0.40f));
        }

        /// <summary>
        /// Spawns an expanding shockwave ring at world coordinate.
        /// </summary>
        public void PlayShockwave(Vector2 worldPos, float scale = 1.0f)
        {
            if (shockwavePool.Count == 0) return;

            GameObject obj = shockwavePool.Dequeue();
            obj.transform.position = worldPos;
            obj.transform.localScale = Vector3.one * 0.2f;
            obj.SetActive(true);

            StartCoroutine(AnimateShockwave(obj, scale));
        }

        /// <summary>
        /// Triggers classic fighting game hitstop (micro-freeze on impact).
        /// Re-entrant safe: prevents concurrent or clashing hits from permanently locking Time.timeScale.
        /// </summary>
        public void TriggerHitStop(float durationSeconds)
        {
            if (!isHitStopActive)
            {
                // Only capture baseline timeScale when hitstop is not already running!
                // If the game was already in an abnormal low scale (<0.1f), default safely to 1.0f.
                originalTimeScale = Time.timeScale > 0.1f ? Time.timeScale : 1.0f;
            }

            if (activeHitstopCoroutine != null) StopCoroutine(activeHitstopCoroutine);
            activeHitstopCoroutine = StartCoroutine(DoHitStop(durationSeconds));
        }

        /// <summary>
        /// Triggers a screen-shake on the Main Camera.
        /// </summary>
        public void TriggerCameraShake(float intensity, float duration)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            if (activeShakeCoroutine != null) StopCoroutine(activeShakeCoroutine);
            activeShakeCoroutine = StartCoroutine(DoCameraShake(intensity, duration));
        }

        /// <summary>
        /// Triggers a 2-frame fullscreen white impact frame for crushing blows and K.O.s.
        /// </summary>
        public void TriggerImpactFlash(float duration = 0.055f)
        {
            ArenaBackgroundManager.Instance?.TriggerBrazierSurge(1.6f);
            if (flashOverlayImage == null) return;
            StartCoroutine(DoImpactFlash(duration));
        }

        #endregion

        #region Coroutines

        private IEnumerator RecycleParticle(ParticleSystem ps, Queue<ParticleSystem> pool, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                pool.Enqueue(ps);
            }
        }

        private IEnumerator AnimateShockwave(GameObject obj, float maxScale)
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            float elapsed = 0f;
            float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentScale = Mathf.Lerp(0.2f, maxScale * 2.5f, t);
                obj.transform.localScale = new Vector3(currentScale, currentScale, 1f);

                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(0.85f, 0f, t * t);
                    sr.color = c;
                }
                yield return null;
            }

            obj.SetActive(false);
            shockwavePool.Enqueue(obj);
        }

        private IEnumerator DoHitStop(float duration)
        {
            isHitStopActive = true;
            Time.timeScale = 0.04f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalTimeScale > 0.1f ? originalTimeScale : 1.0f;
            isHitStopActive = false;
            activeHitstopCoroutine = null;
        }

        private IEnumerator DoCameraShake(float intensity, float duration)
        {
            float elapsed = 0f;
            initialCamPos = targetCamera.transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentIntensity = Mathf.Lerp(intensity, 0f, elapsed / duration);
                Vector2 offset = Random.insideUnitCircle * currentIntensity;
                targetCamera.transform.position = new Vector3(initialCamPos.x + offset.x, initialCamPos.y + offset.y, initialCamPos.z);
                yield return null;
            }

            targetCamera.transform.position = initialCamPos;
            activeShakeCoroutine = null;
        }

        private IEnumerator DoImpactFlash(float duration)
        {
            flashOverlayImage.color = new Color(1f, 1f, 1f, 0.85f);
            yield return new WaitForSecondsRealtime(duration);
            flashOverlayImage.color = new Color(1f, 1f, 1f, 0f);
        }

        #endregion
    }
}
