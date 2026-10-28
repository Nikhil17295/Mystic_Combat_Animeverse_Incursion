using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using MysticCombat.Fighter;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Master Arcade Super Freeze & Cinematic Cut-In Engine for Saitama's "Serious Punch".
    /// 1. In-World Buildup (0.0s - 2.4s): Saitama coils low in the arena with normal lighting ("Hissatsu... Maji Series...").
    /// 2. Black Screen Freeze (~2.4s on "SERIOUS PUNCH!"): The entire screen instantly snaps to 100% solid pitch black.
    ///    The enlarged Big Punch cut-in (creepy shadowed face + colossal thrusting red fist with speed lines) flashes
    ///    against the black void for a punchy 0.28s window, oriented toward center-screen based on facing direction.
    /// 3. Immediate Snap Back: The black void vanishes instantly! Visual control returns to the normal arena where
    ///    Saitama continues his normal character punch animation (ultimate_7..13) with no big sprites attached to his body.
    /// 4. Impact Cataclysm: Small short melee hitbox (1.4m x 1.0m) connects, triggering camera shake, impact flash, and brazier surge.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class SaitamaSeriousPunchCinematic : MonoBehaviour
    {
        #region Serialized Tuning

        [Header("Cinematic Transition Settings")]
        [Tooltip("Duration in seconds that the solid black screen freeze and big punch cut-in stay visible.")]
        [SerializeField] private float freezeDuration = 0.28f;

        [Tooltip("Pixel width of the enlarged punch cut-in on screen (native 234x204 scaled up).")]
        [SerializeField] private float cutInWidth = 720f;

        [Tooltip("Pixel height of the enlarged punch cut-in on screen.")]
        [SerializeField] private float cutInHeight = 628f;

        [Tooltip("Horizontal offset fallback from screen center towards Saitama's side.")]
        [SerializeField] private float horizontalOffset = 120f;

        [Tooltip("If true, shows punch fist only (saitama_death_fist.png). If false, shows combo with creepy face (saitama_death_combo.png).")]
        [SerializeField] private bool useFistOnly = false;

        #endregion

        #region Internal Components & UI

        private FighterController controller;
        private SpriteRenderer fighterSr;

        // UI Canvas Hierarchy (ScreenSpaceOverlay)
        private GameObject cinematicCanvasObj;
        private Canvas cinematicCanvas;
        private CanvasScaler canvasScaler;
        private GameObject blackPanelObj;
        private Image blackPanelImage;
        private GameObject cutInObj;
        private Image cutInImage;
        private RectTransform cutInRect;

        // Sprites
        private Sprite deathComboSprite;
        private Sprite deathFistSprite;

        private bool wasInUltimate;
        private bool hasTriggeredFreeze;
        private Coroutine activeFreezeCoroutine;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            controller = GetComponent<FighterController>();
            fighterSr = GetComponent<SpriteRenderer>();

            // Only activate for Saitama
            if (!gameObject.name.ToLower().Contains("saitama"))
            {
                enabled = false;
                return;
            }

            LoadSprites();
            BuildCinematicCanvas();
        }

        private void Update()
        {
            if (controller == null) return;

            bool isUltimate = controller.CurrentState == FighterState.Ultimate;

            if (!isUltimate)
            {
                if (wasInUltimate)
                {
                    ResetCinematic();
                }
                wasInUltimate = false;
                return;
            }

            wasInUltimate = true;

            // Trigger the black screen super freeze exactly when startup reaches 0.0s (the strike transition on "SERIOUS PUNCH!")
            float startupRemaining = controller.AttackStartupTimer;
            if (startupRemaining <= 0.05f && !hasTriggeredFreeze)
            {
                hasTriggeredFreeze = true;
                bool faceLeft = fighterSr != null && fighterSr.flipX;
                if (activeFreezeCoroutine != null) StopCoroutine(activeFreezeCoroutine);
                activeFreezeCoroutine = StartCoroutine(DoSuperFreeze(faceLeft));
            }
        }

        private void OnDisable()
        {
            ResetCinematic();
        }

        #endregion

        #region Cinematic Sequence

        private IEnumerator DoSuperFreeze(bool faceLeft)
        {
            if (blackPanelObj == null || cutInObj == null) yield break;

            // 1. Orient and position the cut-in
            float dir = faceLeft ? -1f : 1f;

            // Pick sprite based on toggle
            Sprite activeSprite = (useFistOnly && deathFistSprite != null) ? deathFistSprite : deathComboSprite;
            if (cutInImage != null && activeSprite != null)
            {
                cutInImage.sprite = activeSprite;
            }

            // Find Saitama's screen position in 1920x1080 canvas coordinates
            float saitamaCanvasX = -horizontalOffset * dir;
            if (Camera.main != null)
            {
                Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);
                saitamaCanvasX = (vp.x - 0.5f) * 1920f;
            }

            // Calculate cut-in X position: biased towards screen center (0), never exceeding Saitama's backward X
            float halfWidth = cutInWidth * 0.5f;
            float targetX;
            if (dir > 0)
            {
                // Facing right: left edge must be >= saitamaCanvasX (never behind Saitama), right edge <= 920 (on screen)
                float minTarget = saitamaCanvasX + halfWidth;
                float maxTarget = 920f - halfWidth;
                targetX = Mathf.Max(minTarget, Mathf.Min(0f, maxTarget));
                if (targetX > maxTarget) targetX = maxTarget;
            }
            else
            {
                // Facing left: right edge must be <= saitamaCanvasX, left edge >= -920
                float minTarget = -920f + halfWidth;
                float maxTarget = saitamaCanvasX - halfWidth;
                targetX = Mathf.Min(maxTarget, Mathf.Max(0f, minTarget));
                if (targetX < minTarget) targetX = minTarget;
            }

            if (cutInRect != null)
            {
                cutInRect.localScale = new Vector3(dir, 1f, 1f);
                cutInRect.anchoredPosition = new Vector2(targetX, 20f);
            }

            // 2. Snap the screen to 100% solid pitch black!
            blackPanelObj.SetActive(true);
            cutInObj.SetActive(true);

            // Subtle micro-timeScale slowdown for classic fighting game hit freeze
            float prevTimeScale = Time.timeScale;

            // Wait for the transition freeze duration (realtime to prevent physics freezing permanently)
            float elapsed = 0f;
            while (elapsed < freezeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 3. Vanish the black screen instantly! Snap back to the normal arena!
            blackPanelObj.SetActive(false);
            cutInObj.SetActive(false);

            // 4. Trigger in-world combat juice on the strike impact
            VFXManager.Instance?.TriggerImpactFlash(0.055f);
            VFXManager.Instance?.TriggerCameraShake(0.38f, 0.32f);
            ArenaBackgroundManager.Instance?.TriggerBrazierSurge(1.5f);

            activeFreezeCoroutine = null;
        }

        private void ResetCinematic()
        {
            hasTriggeredFreeze = false;
            if (activeFreezeCoroutine != null)
            {
                StopCoroutine(activeFreezeCoroutine);
                activeFreezeCoroutine = null;
            }

            if (blackPanelObj != null) blackPanelObj.SetActive(false);
            if (cutInObj != null) cutInObj.SetActive(false);
        }

        #endregion

        #region Setup & Asset Loading

        private void LoadSprites()
        {
            string comboPath = Path.Combine(Application.dataPath, "Sprites", "Effects", "saitama_death_combo.png");
            if (File.Exists(comboPath))
            {
                deathComboSprite = LoadSpriteFile(comboPath);
            }

            string fistPath = Path.Combine(Application.dataPath, "Sprites", "Effects", "saitama_death_fist.png");
            if (File.Exists(fistPath))
            {
                deathFistSprite = LoadSpriteFile(fistPath);
            }
        }

        private Sprite LoadSpriteFile(string path)
        {
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

        private void BuildCinematicCanvas()
        {
            // Fullscreen ScreenSpaceOverlay Canvas at high sorting order (above stage and characters)
            cinematicCanvasObj = new GameObject("Saitama_SuperFreeze_Canvas");
            cinematicCanvasObj.transform.SetParent(transform, false);

            cinematicCanvas = cinematicCanvasObj.AddComponent<Canvas>();
            cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cinematicCanvas.sortingOrder = 950; // Above fighters and stage, just beneath top HUD headers

            canvasScaler = cinematicCanvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            cinematicCanvasObj.AddComponent<GraphicRaycaster>();

            // 1. Solid Pitch-Black Fullscreen Panel (covers 100% of the screen)
            blackPanelObj = new GameObject("SolidBlack_Void");
            blackPanelObj.transform.SetParent(cinematicCanvasObj.transform, false);
            blackPanelImage = blackPanelObj.AddComponent<Image>();
            blackPanelImage.color = new Color(0f, 0f, 0f, 1.0f); // 100% solid black!

            RectTransform blackRt = blackPanelObj.GetComponent<RectTransform>();
            blackRt.anchorMin = Vector2.zero;
            blackRt.anchorMax = Vector2.one;
            blackRt.sizeDelta = Vector2.zero;
            blackRt.anchoredPosition = Vector2.zero;

            // 2. Enlarged Big Punch Cut-In Image (Box 3)
            cutInObj = new GameObject("BigPunch_CutIn");
            cutInObj.transform.SetParent(blackPanelObj.transform, false);
            cutInImage = cutInObj.AddComponent<Image>();
            cutInImage.sprite = deathComboSprite;
            cutInImage.preserveAspect = true;

            cutInRect = cutInObj.GetComponent<RectTransform>();
            cutInRect.anchorMin = new Vector2(0.5f, 0.5f);
            cutInRect.anchorMax = new Vector2(0.5f, 0.5f);
            cutInRect.pivot = new Vector2(0.5f, 0.5f);
            cutInRect.sizeDelta = new Vector2(cutInWidth, cutInHeight);
            cutInRect.anchoredPosition = Vector2.zero;

            // Initially inactive
            blackPanelObj.SetActive(false);
            cutInObj.SetActive(false);
        }

        #endregion
    }
}
