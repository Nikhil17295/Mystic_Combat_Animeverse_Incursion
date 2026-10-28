using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysticCombat.Systems;
using MysticCombat.Core;

namespace MysticCombat.UI
{
    /// <summary>
    /// Majestic 16-bit Anime Tournament HUD:
    /// - 140x140 heroic bust portrait cards with dynamic anime burst backgrounds
    /// - Heavy 46px 2-tone arcade health bars with trailing red damage ghost bars
    /// - 24px electric cyan Ki energy bars
    /// - Doctor Strange / MCU Time Stone surrounded by a rotating emerald spell mandala (No box!)
    /// - Shiny Blue Point Gems for scored rounds and Hollow Sockets when unearned
    /// - Big Golden Gem in the center that ignites and pulses at Match Point (1-1 tie)
    /// - Automatic runtime reference discovery ensuring real-time event updates
    /// - Dynamic fighter avatar and name binding reflecting character select choices
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Player 1 (Goku) Elements")]
        [Tooltip("P1 active health bar fill (Filled, Horizontal, Left).")]
        [SerializeField] private Image p1HealthBar;

        [Tooltip("P1 trailing damage ghost bar.")]
        [SerializeField] private Image p1GhostHealthBar;

        [Tooltip("P1 Ki energy bar fill.")]
        [SerializeField] private Image p1KiBar;

        [Header("Tactical Segmented Ki Meter Elements")]
        [SerializeField] private Color kiRedZoneColor = new Color(1.00f, 0.22f, 0.22f, 1f);       // Danger Red
        [SerializeField] private Color kiSafeZoneColor = new Color(0.20f, 0.95f, 0.35f, 1f);      // Safe Green
        [SerializeField] private Color kiSkillSafeColor = new Color(0.12f, 0.75f, 1.00f, 1f);     // Skill-Ready Blue
        [SerializeField] private Color kiSurplusColor = new Color(1.00f, 0.88f, 0.18f, 1f);       // Ultimate Surplus Gold
        [SerializeField] private Color kiNotchColor = new Color(0.92f, 0.95f, 1.00f, 0.95f);       // Metallic Divider Notch

        [Header("Player 1 Ki Segments & Marks")]
        [SerializeField] private Image p1KiRed;
        [SerializeField] private Image p1KiGreen;
        [SerializeField] private Image p1KiBlue;
        [SerializeField] private Image p1KiYellow;
        [SerializeField] private RectTransform p1MarkRedZone;
        [SerializeField] private RectTransform p1MarkSkillSafe;
        [SerializeField] private RectTransform p1MarkUltSafe;

        [Header("Player 1 Round Point Gems")]
        [Tooltip("P1 round point gem 1 (Hollow when 0, Shiny Blue when 1).")]
        [SerializeField] private Image p1PointGem1;

        [Tooltip("P1 round point gem 2 (Hollow when unearned, Shiny Blue when won 2 rounds).")]
        [SerializeField] private Image p1PointGem2;

        [Header("Player 2 (Saitama) Elements")]
        [Tooltip("P2 active health bar fill (Filled, Horizontal, Right).")]
        [SerializeField] private Image p2HealthBar;

        [Tooltip("P2 trailing damage ghost bar.")]
        [SerializeField] private Image p2GhostHealthBar;

        [Tooltip("P2 Ki energy bar fill.")]
        [SerializeField] private Image p2KiBar;

        [Header("Player 2 Ki Segments & Marks")]
        [SerializeField] private Image p2KiRed;
        [SerializeField] private Image p2KiGreen;
        [SerializeField] private Image p2KiBlue;
        [SerializeField] private Image p2KiYellow;
        [SerializeField] private RectTransform p2MarkRedZone;
        [SerializeField] private RectTransform p2MarkSkillSafe;
        [SerializeField] private RectTransform p2MarkUltSafe;

        [Header("Player 2 Round Point Gems")]
        [Tooltip("P2 round point gem 1 (Hollow when 0, Shiny Blue when 1).")]
        [SerializeField] private Image p2PointGem1;

        [Tooltip("P2 round point gem 2 (Hollow when unearned, Shiny Blue when won 2 rounds).")]
        [SerializeField] private Image p2PointGem2;

        [Header("Avatar & Name Displays")]
        [SerializeField] private Image p1AvatarCard;
        [SerializeField] private Image p2AvatarCard;
        [SerializeField] private TextMeshProUGUI p1NameText;
        [SerializeField] private TextMeshProUGUI p2NameText;

        [Header("Center Time Stone & Magic Mandala")]
        [Tooltip("Doctor Strange style rotating green spell mandala rect.")]
        [SerializeField] private RectTransform timeStoneMandala;

        [Tooltip("Emerald Time Stone gem image.")]
        [SerializeField] private Image timeStoneGem;

        [Tooltip("Countdown timer text (e.g. '99').")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Tooltip("Round title banner (e.g. 'ROUND 1').")]
        [SerializeField] private TextMeshProUGUI roundTitleText;

        [Header("Center Big Gold Gem")]
        [Tooltip("Big golden gem seated in the center below the Time Stone.")]
        [SerializeField] private Image centerBigGoldGem;

        [Header("Gem Sprites")]
        [Tooltip("Vibrant shiny blue gem awarded for a won round.")]
        [SerializeField] private Sprite gemShinyBlue;

        [Tooltip("Hollow metallic socket when round is unearned.")]
        [SerializeField] private Sprite gemHollowSocket;

        [Tooltip("Big golden gem sprite for the center relic.")]
        [SerializeField] private Sprite gemBigGold;

        [Tooltip("Hollow golden relic socket sprite when match point is not reached.")]
        [SerializeField] private Sprite gemBigGoldHollow;

        [Header("Tuning")]
        [SerializeField] private float ghostLagDelay = 0.35f;
        [SerializeField] private float ghostDrainSpeed = 0.45f;
        [SerializeField] private float mandalaRotationSpeed = 22f;

        [Header("System References")]
        [SerializeField] private HealthSystem p1Health;
        [SerializeField] private HealthSystem p2Health;
        [SerializeField] private EnergySystem p1Energy;
        [SerializeField] private EnergySystem p2Energy;
        [SerializeField] private MatchManager matchManager;

        #endregion

        #region State

        private float p1GhostTimer;
        private float p2GhostTimer;
        private float p1TargetHp = 1f;
        private float p2TargetHp = 1f;
        private bool isSubscribed;
        private bool isMatchPoint;
        private Vector3 initialGoldGemScale = Vector3.one;

        private float p1T1 = 10f;
        private float p1T2 = 45f;
        private float p1T3 = 90f;
        private float p2T1 = 10f;
        private float p2T2 = 30f;
        private float p2T3 = 90f;
        private Sprite cachedWhiteKiFillSprite;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (centerBigGoldGem != null)
            {
                initialGoldGemScale = centerBigGoldGem.transform.localScale;
            }
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            SyncInitialState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SyncInitialState();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (!IsInActiveScene(p1Health) || !IsInActiveScene(p2Health))
            {
                ResolveReferences();
            }

            AnimateGhostBars();
            AnimateTimeMandala();
            AnimateMatchPointPulse();
        }

        #endregion

        #region Reference Resolution & Event Binding

        private static bool IsInActiveScene(Component comp)
        {
            if (comp == null) return false;
            return comp.gameObject.scene.IsValid() && comp.gameObject.scene.isLoaded;
        }

        /// <summary>
        /// Auto-discovers live scene references dynamically.
        /// Replaces any dead references pointing to prefab assets on disk with live scene fighter instances.
        /// </summary>
        public void ResolveReferences()
        {
            // 1. Resolve UI references if unassigned or lost
            if (p1HealthBar == null)
            {
                Transform t = transform.Find("P1_HealthBar_Container/HealthFill");
                if (t != null) p1HealthBar = t.GetComponent<Image>();
            }
            if (p1GhostHealthBar == null)
            {
                Transform t = transform.Find("P1_HealthBar_Container/GhostFill");
                if (t != null) p1GhostHealthBar = t.GetComponent<Image>();
            }
            if (p1KiBar == null)
            {
                Transform t = transform.Find("P1_KiBar_Container/KiFill");
                if (t != null) p1KiBar = t.GetComponent<Image>();
            }
            if (p2HealthBar == null)
            {
                Transform t = transform.Find("P2_HealthBar_Container/HealthFill");
                if (t != null) p2HealthBar = t.GetComponent<Image>();
            }
            if (p2GhostHealthBar == null)
            {
                Transform t = transform.Find("P2_HealthBar_Container/GhostFill");
                if (t != null) p2GhostHealthBar = t.GetComponent<Image>();
            }
            if (p2KiBar == null)
            {
                Transform t = transform.Find("P2_KiBar_Container/KiFill");
                if (t != null) p2KiBar = t.GetComponent<Image>();
            }

            EnsureKiMeterHierarchyBuilt();

            // Resolve Avatar cards and Name text elements
            if (p1AvatarCard == null)
            {
                Transform t = transform.Find("P1_AvatarCard");
                if (t != null) p1AvatarCard = t.GetComponent<Image>();
            }
            if (p2AvatarCard == null)
            {
                Transform t = transform.Find("P2_AvatarCard");
                if (t != null) p2AvatarCard = t.GetComponent<Image>();
            }
            if (p1NameText == null)
            {
                Transform t = transform.Find("P1_Name");
                if (t != null) p1NameText = t.GetComponent<TextMeshProUGUI>();
            }
            if (p2NameText == null)
            {
                Transform t = transform.Find("P2_Name");
                if (t != null) p2NameText = t.GetComponent<TextMeshProUGUI>();
            }

            // 2. Resolve Player 1 - Must be in active scene!
            if (!IsInActiveScene(p1Health) || !IsInActiveScene(p1Energy))
            {
                p1Health = null;
                p1Energy = null;

                foreach (var fc in Object.FindObjectsByType<Fighter.FighterController>(FindObjectsInactive.Exclude))
                {
                    if (fc == null) continue;
                    string n = fc.gameObject.name;
                    if (n == "Fighter_P1" || n.StartsWith("Fighter_P1_"))
                    {
                        p1Health = fc.GetComponent<HealthSystem>();
                        p1Energy = fc.GetComponent<EnergySystem>();
                        break;
                    }
                }
            }

            // 3. Resolve Player 2 - Must be in active scene!
            if (!IsInActiveScene(p2Health) || !IsInActiveScene(p2Energy))
            {
                p2Health = null;
                p2Energy = null;

                foreach (var fc in Object.FindObjectsByType<Fighter.FighterController>(FindObjectsInactive.Exclude))
                {
                    if (fc == null) continue;
                    string n = fc.gameObject.name;
                    if (n == "Fighter_P2" || n.StartsWith("Fighter_P2_"))
                    {
                        p2Health = fc.GetComponent<HealthSystem>();
                        p2Energy = fc.GetComponent<EnergySystem>();
                        break;
                    }
                }
            }

            // 4. Resolve MatchManager
            if (!IsInActiveScene(matchManager))
            {
                matchManager = Object.FindAnyObjectByType<MatchManager>();
            }

            SubscribeEvents();
        }

        public void BindSystems(HealthSystem p1Hp, HealthSystem p2Hp, EnergySystem p1Ki, EnergySystem p2Ki)
        {
            UnsubscribeEvents();
            p1Health = p1Hp;
            p2Health = p2Hp;
            p1Energy = p1Ki;
            p2Energy = p2Ki;
            SubscribeEvents();
            SyncInitialState();
        }

        public void BindMatchManager(MatchManager mm)
        {
            if (matchManager != null)
            {
                matchManager.OnTimerUpdate -= UpdateTimer;
                matchManager.OnRoundEnd -= OnRoundFinished;
                matchManager.OnRoundStart -= OnRoundStarted;
                matchManager.OnOvertimeStarted -= HandleOvertimeStarted;
            }
            matchManager = mm;
            SubscribeMatchEvents();
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (p1Health != null && IsInActiveScene(p1Health))
            {
                p1Health.OnHealthChanged += OnP1HealthChanged;
                Debug.Log($"<color=#00e676>[HUDController]</color> Subscribed to P1 Health: {p1Health.gameObject.name} (HP: {p1Health.CurrentHealth}/{p1Health.MaxHealth})");
            }

            if (p2Health != null && IsInActiveScene(p2Health))
            {
                p2Health.OnHealthChanged += OnP2HealthChanged;
                Debug.Log($"<color=#00e676>[HUDController]</color> Subscribed to P2 Health: {p2Health.gameObject.name} (HP: {p2Health.CurrentHealth}/{p2Health.MaxHealth})");
            }

            if (p1Energy != null && IsInActiveScene(p1Energy))
            {
                p1Energy.OnKiChanged += OnP1KiChanged;
                p1Energy.OnThresholdsChanged += OnP1ThresholdsChanged;
                CalibrateKiThresholdsFromEnergySystem(0);
            }

            if (p2Energy != null && IsInActiveScene(p2Energy))
            {
                p2Energy.OnKiChanged += OnP2KiChanged;
                p2Energy.OnThresholdsChanged += OnP2ThresholdsChanged;
                CalibrateKiThresholdsFromEnergySystem(1);
            }

            SubscribeMatchEvents();
            isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!isSubscribed) return;

            if (p1Health != null) p1Health.OnHealthChanged -= OnP1HealthChanged;
            if (p2Health != null) p2Health.OnHealthChanged -= OnP2HealthChanged;

            if (p1Energy != null)
            {
                p1Energy.OnKiChanged -= OnP1KiChanged;
                p1Energy.OnThresholdsChanged -= OnP1ThresholdsChanged;
            }
            if (p2Energy != null)
            {
                p2Energy.OnKiChanged -= OnP2KiChanged;
                p2Energy.OnThresholdsChanged -= OnP2ThresholdsChanged;
            }

            if (matchManager != null)
            {
                matchManager.OnTimerUpdate -= UpdateTimer;
                matchManager.OnRoundEnd -= OnRoundFinished;
                matchManager.OnRoundStart -= OnRoundStarted;
                matchManager.OnOvertimeStarted -= HandleOvertimeStarted;
            }

            isSubscribed = false;
        }

        private void SubscribeMatchEvents()
        {
            if (matchManager == null) return;

            matchManager.OnTimerUpdate -= UpdateTimer;
            matchManager.OnRoundEnd -= OnRoundFinished;
            matchManager.OnRoundStart -= OnRoundStarted;
            matchManager.OnOvertimeStarted -= HandleOvertimeStarted;

            matchManager.OnTimerUpdate += UpdateTimer;
            matchManager.OnRoundEnd += OnRoundFinished;
            matchManager.OnRoundStart += OnRoundStarted;
            matchManager.OnOvertimeStarted += HandleOvertimeStarted;

            UpdateRoundScore(matchManager.P1RoundWins, matchManager.P2RoundWins);
            UpdateTimer(matchManager.RoundTimer > 0f ? matchManager.RoundTimer : 199f);
        }

        /// <summary>
        /// Explicitly binds fighter health and energy systems at runtime.
        /// Immediately synchronizes health bars, Ki meters, and heroic avatar portraits.
        /// </summary>
        public void SetFighterReferences(HealthSystem p1Hp, HealthSystem p2Hp, EnergySystem p1Ki, EnergySystem p2Ki)
        {
            UnsubscribeEvents();

            p1Health = p1Hp;
            p2Health = p2Hp;
            p1Energy = p1Ki;
            p2Energy = p2Ki;

            SyncInitialState();
            SubscribeEvents();
            Debug.Log($"<color=#00e676>[HUDController]</color> Bound fighter references: P1: {p1Health?.gameObject.name} | P2: {p2Health?.gameObject.name}");
        }

        private void SyncInitialState()
        {
            SyncFighterAvatarsAndNames();

            if (p1Health != null) UpdateHealthBar(0, p1Health.HealthPercent);
            else UpdateHealthBar(0, 1f);

            if (p2Health != null) UpdateHealthBar(1, p2Health.HealthPercent);
            else UpdateHealthBar(1, 1f);

            if (p1Energy != null) UpdateKiBar(0, p1Energy.CurrentKi, p1Energy.MaxKi);
            else UpdateKiBar(0, 50f, 100f);

            if (p2Energy != null) UpdateKiBar(1, p2Energy.CurrentKi, p2Energy.MaxKi);
            else UpdateKiBar(1, 50f, 100f);

            if (matchManager != null)
            {
                UpdateRoundScore(matchManager.P1RoundWins, matchManager.P2RoundWins);
                UpdateTimer(matchManager.RoundTimer);
            }
            else
            {
                UpdateRoundScore(0, 0);
                UpdateTimer(199f);
            }
        }

        private void SyncFighterAvatarsAndNames()
        {
            string p1Id = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.P1SelectedCharacterId))
                ? GameManager.Instance.P1SelectedCharacterId.ToLower()
                : "goku";
            string p2Id = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.P2SelectedCharacterId))
                ? GameManager.Instance.P2SelectedCharacterId.ToLower()
                : "saitama";

            if (p1Energy != null) CalibrateKiThresholdsFromEnergySystem(0);
            else CalibrateKiThresholds(0, p1Id);

            if (p2Energy != null) CalibrateKiThresholdsFromEnergySystem(1);
            else CalibrateKiThresholds(1, p2Id);

            if (p1NameText != null)
            {
                p1NameText.text = p1Id == "goku" ? "GOKU" : "SAITAMA";
            }

            if (p2NameText != null)
            {
                string prefix = (GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameManager.GameMode.VsCpu) ? "CPU " : "";
                p2NameText.text = prefix + (p2Id == "goku" ? "GOKU" : "SAITAMA");
            }

            UpdateAvatarCard(p1AvatarCard, p1Id, false);
            UpdateAvatarCard(p2AvatarCard, p2Id, p1Id == p2Id);
        }

        private void UpdateAvatarCard(Image img, string charId, bool isMirrorP2)
        {
            if (img == null) return;

            string spritePath = charId == "goku"
                ? System.IO.Path.Combine(Application.dataPath, "Sprites", "UI", "goku_avatar_heroic.png")
                : System.IO.Path.Combine(Application.dataPath, "Sprites", "UI", "saitama_avatar_heroic.png");

            if (System.IO.File.Exists(spritePath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(spritePath);
                Texture2D tex = new Texture2D(2, 2);
                tex.filterMode = FilterMode.Bilinear;
                if (tex.LoadImage(bytes))
                {
                    img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            // If Player 2 in a Mirror Match, apply the matching Alternate Palette!
            if (isMirrorP2)
            {
                img.color = Fighter.FighterController.GetAlternatePaletteColor(charId);
            }
            else
            {
                img.color = Color.white;
            }
        }

        #endregion

        #region Public Visual Updaters

        public void UpdateHealthBar(int playerIndex, float percent)
        {
            float target = Mathf.Clamp01(percent);
            Debug.Log($"<color=#ff00ff>[HUDController.UpdateHealthBar]</color> Player {playerIndex} target HP: {target:F2} | Bar assigned: {(playerIndex == 0 ? p1HealthBar != null : p2HealthBar != null)}");

            if (playerIndex == 0)
            {
                p1TargetHp = target;
                if (p1HealthBar != null) p1HealthBar.fillAmount = target;

                if (p1GhostHealthBar != null && p1GhostHealthBar.fillAmount < target)
                {
                    p1GhostHealthBar.fillAmount = target;
                }
                else
                {
                    p1GhostTimer = ghostLagDelay;
                }
            }
            else
            {
                p2TargetHp = target;
                if (p2HealthBar != null) p2HealthBar.fillAmount = target;

                if (p2GhostHealthBar != null && p2GhostHealthBar.fillAmount < target)
                {
                    p2GhostHealthBar.fillAmount = target;
                }
                else
                {
                    p2GhostTimer = ghostLagDelay;
                }
            }
        }

        public void UpdateKiBar(int playerIndex, float currentKi, float maxKi)
        {
            float t1 = playerIndex == 0 ? p1T1 : p2T1;
            float t2 = playerIndex == 0 ? p1T2 : p2T2;
            float t3 = playerIndex == 0 ? p1T3 : p2T3;

            float cur = Mathf.Max(0f, currentKi);
            float max = Mathf.Max(1f, maxKi);

            // Calculate fill percentages for each of the 4 zones
            float redFill = Mathf.Clamp01(cur / t1);
            float greenFill = cur <= t1 ? 0f : Mathf.Clamp01((cur - t1) / Mathf.Max(0.001f, t2 - t1));
            float blueFill = cur <= t2 ? 0f : Mathf.Clamp01((cur - t2) / Mathf.Max(0.001f, t3 - t2));
            float yellowFill = cur <= t3 ? 0f : Mathf.Clamp01((cur - t3) / Mathf.Max(0.001f, max - t3));

            if (playerIndex == 0)
            {
                if (p1KiRed != null) p1KiRed.fillAmount = redFill;
                if (p1KiGreen != null) p1KiGreen.fillAmount = greenFill;
                if (p1KiBlue != null) p1KiBlue.fillAmount = blueFill;
                if (p1KiYellow != null) p1KiYellow.fillAmount = yellowFill;
                if (p1KiBar != null && p1KiBar.gameObject.activeSelf) p1KiBar.fillAmount = Mathf.Clamp01(cur / max);
            }
            else
            {
                if (p2KiRed != null) p2KiRed.fillAmount = redFill;
                if (p2KiGreen != null) p2KiGreen.fillAmount = greenFill;
                if (p2KiBlue != null) p2KiBlue.fillAmount = blueFill;
                if (p2KiYellow != null) p2KiYellow.fillAmount = yellowFill;
                if (p2KiBar != null && p2KiBar.gameObject.activeSelf) p2KiBar.fillAmount = Mathf.Clamp01(cur / max);
            }
        }

        public void UpdateKiBar(int playerIndex, float percent)
        {
            float maxKi = 100f;
            UpdateKiBar(playerIndex, Mathf.Clamp01(percent) * maxKi, maxKi);
        }

        #region Segmented Ki Meter Hierarchy & Calibration

        private void EnsureKiMeterHierarchyBuilt()
        {
            EnsurePlayerKiMeter(0);
            EnsurePlayerKiMeter(1);
        }

        private Sprite GetWhiteKiFillSprite()
        {
            if (cachedWhiteKiFillSprite != null) return cachedWhiteKiFillSprite;

            cachedWhiteKiFillSprite = Resources.Load<Sprite>("Sprites/UI/ki_bar_grand_fill_white");
            if (cachedWhiteKiFillSprite != null) return cachedWhiteKiFillSprite;

            string path = System.IO.Path.Combine(Application.dataPath, "Sprites", "UI", "ki_bar_grand_fill_white.png");
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                tex.filterMode = FilterMode.Point;
                if (tex.LoadImage(bytes))
                {
                    cachedWhiteKiFillSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    return cachedWhiteKiFillSprite;
                }
            }

            return null;
        }

        private void EnsurePlayerKiMeter(int playerIndex)
        {
            string containerName = playerIndex == 0 ? "P1_KiBar_Container" : "P2_KiBar_Container";
            Transform container = transform.Find(containerName);
            if (container == null) return;

            // Deactivate legacy single-color KiFill if present
            Transform legacyFill = container.Find("KiFill");
            if (legacyFill != null)
            {
                legacyFill.gameObject.SetActive(false);
            }

            // Find or create SegmentContainer
            Transform segTrans = container.Find("SegmentContainer");
            GameObject segObj;
            RectTransform segRt;
            if (segTrans == null)
            {
                segObj = new GameObject("SegmentContainer", typeof(RectTransform));
                segObj.transform.SetParent(container, false);
                segRt = segObj.GetComponent<RectTransform>();
                segRt.anchorMin = Vector2.zero;
                segRt.anchorMax = Vector2.one;
                segRt.offsetMin = new Vector2(3f, 3f);
                segRt.offsetMax = new Vector2(-3f, -3f);
            }
            else
            {
                segObj = segTrans.gameObject;
                segRt = segObj.GetComponent<RectTransform>();
            }

            Sprite whiteFill = GetWhiteKiFillSprite();
            Image.OriginHorizontal origin = playerIndex == 0 ? Image.OriginHorizontal.Left : Image.OriginHorizontal.Right;

            // Get or create 4 colored fill segments
            Image red = GetOrCreateSegment(segObj.transform, "KiFill_Red", whiteFill, kiRedZoneColor, origin);
            Image green = GetOrCreateSegment(segObj.transform, "KiFill_Green", whiteFill, kiSafeZoneColor, origin);
            Image blue = GetOrCreateSegment(segObj.transform, "KiFill_Blue", whiteFill, kiSkillSafeColor, origin);
            Image yellow = GetOrCreateSegment(segObj.transform, "KiFill_Yellow", whiteFill, kiSurplusColor, origin);

            // Get or create 3 divider notches (Mark 1, 2, 3)
            RectTransform mark1 = GetOrCreateMark(segObj.transform, "Mark_RedZone");
            RectTransform mark2 = GetOrCreateMark(segObj.transform, "Mark_SkillSafe");
            RectTransform mark3 = GetOrCreateMark(segObj.transform, "Mark_UltimateSafe");

            if (playerIndex == 0)
            {
                p1KiRed = red;
                p1KiGreen = green;
                p1KiBlue = blue;
                p1KiYellow = yellow;
                p1MarkRedZone = mark1;
                p1MarkSkillSafe = mark2;
                p1MarkUltSafe = mark3;
            }
            else
            {
                p2KiRed = red;
                p2KiGreen = green;
                p2KiBlue = blue;
                p2KiYellow = yellow;
                p2MarkRedZone = mark1;
                p2MarkSkillSafe = mark2;
                p2MarkUltSafe = mark3;
            }

            ApplyKiSegmentAnchors(playerIndex);
        }

        private Image GetOrCreateSegment(Transform parent, string name, Sprite sprite, Color tint, Image.OriginHorizontal origin)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            Image img;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(parent, false);
                img = obj.GetComponent<Image>();
            }
            else
            {
                obj = existing.gameObject;
                img = obj.GetComponent<Image>() ?? obj.AddComponent<Image>();
            }

            if (sprite != null) img.sprite = sprite;
            img.color = tint;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)origin;
            return img;
        }

        private RectTransform GetOrCreateMark(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            RectTransform rt;
            Image img;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(parent, false);
                rt = obj.GetComponent<RectTransform>();
                img = obj.GetComponent<Image>();
                Outline outline = obj.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
                outline.effectDistance = new Vector2(1f, 0f);
            }
            else
            {
                obj = existing.gameObject;
                rt = obj.GetComponent<RectTransform>();
                img = obj.GetComponent<Image>() ?? obj.AddComponent<Image>();
                if (obj.GetComponent<Outline>() == null)
                {
                    Outline outline = obj.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
                    outline.effectDistance = new Vector2(1f, 0f);
                }
            }

            img.color = kiNotchColor;
            return rt;
        }

        public void CalibrateKiThresholdsFromEnergySystem(int playerIndex)
        {
            EnergySystem energy = playerIndex == 0 ? p1Energy : p2Energy;
            if (energy != null)
            {
                float t1 = energy.RedZoneThreshold;
                float t2 = energy.SafeSkillThreshold;
                float t3 = energy.SafeUltimateThreshold;

                if (playerIndex == 0)
                {
                    p1T1 = t1;
                    p1T2 = t2;
                    p1T3 = t3;
                }
                else
                {
                    p2T1 = t1;
                    p2T2 = t2;
                    p2T3 = t3;
                }

                ApplyKiSegmentAnchors(playerIndex);
                UpdateKiBar(playerIndex, energy.CurrentKi, energy.MaxKi);
                Debug.Log($"<color=#38bdf8>[HUDController]</color> Deployed P{(playerIndex + 1)} Ki Thresholds Synced: M1(Red)={t1:F0}, M2(Skill)={t2:F0}, M3(Ult)={t3:F0}");
            }
            else
            {
                string charId = playerIndex == 0 ? (GameManager.Instance?.P1SelectedCharacterId ?? "goku") : (GameManager.Instance?.P2SelectedCharacterId ?? "saitama");
                CalibrateKiThresholds(playerIndex, charId);
            }
        }

        public void CalibrateKiThresholds(int playerIndex, string charId)
        {
            string id = !string.IsNullOrEmpty(charId) ? charId.ToLower() : (playerIndex == 0 ? "goku" : "saitama");
            float t1 = 10f; // Red Zone danger threshold (10 Ki)
            // For Goku, Safe Skill marker is set to 30 Ki (SSJ buffer); for Saitama it is 40 Ki (10 RedZone + 30 SkillCost)
            float t2 = id == "goku" ? 30f : 40f;
            // For Goku, Safe Ultimate threshold is 80 Ki (10 RedZone + 70 UltCost); for Saitama it is 90 Ki (10 RedZone + 80 UltCost)
            float t3 = id == "goku" ? 80f : 90f;

            if (playerIndex == 0)
            {
                p1T1 = t1;
                p1T2 = t2;
                p1T3 = t3;
            }
            else
            {
                p2T1 = t1;
                p2T2 = t2;
                p2T3 = t3;
            }

            ApplyKiSegmentAnchors(playerIndex);
        }

        private void OnP1ThresholdsChanged() => CalibrateKiThresholdsFromEnergySystem(0);
        private void OnP2ThresholdsChanged() => CalibrateKiThresholdsFromEnergySystem(1);

        private void ApplyKiSegmentAnchors(int playerIndex)
        {
            float t1 = playerIndex == 0 ? p1T1 : p2T1;
            float t2 = playerIndex == 0 ? p1T2 : p2T2;
            float t3 = playerIndex == 0 ? p1T3 : p2T3;

            float p1 = Mathf.Clamp01(t1 / 100f);
            float p2 = Mathf.Clamp01(t2 / 100f);
            float p3 = Mathf.Clamp01(t3 / 100f);

            Image red = playerIndex == 0 ? p1KiRed : p2KiRed;
            Image green = playerIndex == 0 ? p1KiGreen : p2KiGreen;
            Image blue = playerIndex == 0 ? p1KiBlue : p2KiBlue;
            Image yellow = playerIndex == 0 ? p1KiYellow : p2KiYellow;

            RectTransform m1 = playerIndex == 0 ? p1MarkRedZone : p2MarkRedZone;
            RectTransform m2 = playerIndex == 0 ? p1MarkSkillSafe : p2MarkSkillSafe;
            RectTransform m3 = playerIndex == 0 ? p1MarkUltSafe : p2MarkUltSafe;

            if (playerIndex == 0)
            {
                // Player 1: Fills Left to Right [0 -> 1]
                SetSegmentRect(red, 0f, p1);
                SetSegmentRect(green, p1, p2);
                SetSegmentRect(blue, p2, p3);
                SetSegmentRect(yellow, p3, 1f);

                SetMarkPosition(m1, p1);
                SetMarkPosition(m2, p2);
                SetMarkPosition(m3, p3);
            }
            else
            {
                // Player 2: Mirrored Right to Left [1 -> 0]
                SetSegmentRect(red, 1f - p1, 1f);
                SetSegmentRect(green, 1f - p2, 1f - p1);
                SetSegmentRect(blue, 1f - p3, 1f - p2);
                SetSegmentRect(yellow, 0f, 1f - p3);

                SetMarkPosition(m1, 1f - p1);
                SetMarkPosition(m2, 1f - p2);
                SetMarkPosition(m3, 1f - p3);
            }
        }

        private void SetSegmentRect(Image img, float minX, float maxX)
        {
            if (img == null) return;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(minX, 0f);
            rt.anchorMax = new Vector2(maxX, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void SetMarkPosition(RectTransform rt, float xAnchor)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(xAnchor, 0.5f);
            rt.anchorMax = new Vector2(xAnchor, 0.5f);
            rt.sizeDelta = new Vector2(3f, 26f);
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsLastSibling();
        }

        #endregion

        public void UpdateTimer(float time)
        {
            if (timerText != null)
            {
                int displayTime = Mathf.CeilToInt(Mathf.Max(0f, time));
                timerText.text = displayTime.ToString();
            }
        }

        public void UpdateRoundScore(int p1Wins, int p2Wins)
        {
            // Point Gems: Hollow when unearned, Shiny Blue when earned
            if (p1PointGem1 != null) p1PointGem1.sprite = (p1Wins >= 1) ? gemShinyBlue : gemHollowSocket;
            if (p1PointGem2 != null) p1PointGem2.sprite = (p1Wins >= 2) ? gemShinyBlue : gemHollowSocket;

            if (p2PointGem1 != null) p2PointGem1.sprite = (p2Wins >= 1) ? gemShinyBlue : gemHollowSocket;
            if (p2PointGem2 != null) p2PointGem2.sprite = (p2Wins >= 2) ? gemShinyBlue : gemHollowSocket;

            // Big Gold Gem & Match Point tension: Center relic is hollow until Match Point!
            // In Best of 3, Match Point is reached when either player has 1 win (or tied 1-1)
            isMatchPoint = (p1Wins >= 1 || p2Wins >= 1);

            if (centerBigGoldGem != null)
            {
                centerBigGoldGem.sprite = isMatchPoint ? gemBigGold : (gemBigGoldHollow != null ? gemBigGoldHollow : gemHollowSocket);
            }

            if (roundTitleText != null)
            {
                if (p1Wins == 1 && p2Wins == 1)
                {
                    roundTitleText.text = "★ FINAL ROUND • MATCH POINT ★";
                    roundTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
                }
                else if (isMatchPoint)
                {
                    roundTitleText.text = $"★ ROUND {(matchManager != null ? matchManager.CurrentRound : 2)} • MATCH POINT ★";
                    roundTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
                }
                else if (matchManager != null)
                {
                    roundTitleText.text = $"ROUND {matchManager.CurrentRound}";
                    roundTitleText.color = new Color(0.95f, 0.95f, 0.85f, 1f);
                }
                else
                {
                    roundTitleText.text = "ROUND 1";
                }
            }
        }

        #endregion

        #region Animation Updates

        private void AnimateGhostBars()
        {
            if (p1GhostHealthBar != null)
            {
                if (p1GhostTimer > 0f)
                {
                    p1GhostTimer -= Time.deltaTime;
                }
                else if (p1GhostHealthBar.fillAmount > p1TargetHp)
                {
                    p1GhostHealthBar.fillAmount = Mathf.MoveTowards(
                        p1GhostHealthBar.fillAmount,
                        p1TargetHp,
                        ghostDrainSpeed * Time.deltaTime
                    );
                }
            }

            if (p2GhostHealthBar != null)
            {
                if (p2GhostTimer > 0f)
                {
                    p2GhostTimer -= Time.deltaTime;
                }
                else if (p2GhostHealthBar.fillAmount > p2TargetHp)
                {
                    p2GhostHealthBar.fillAmount = Mathf.MoveTowards(
                        p2GhostHealthBar.fillAmount,
                        p2TargetHp,
                        ghostDrainSpeed * Time.deltaTime
                    );
                }
            }
        }

        private void AnimateTimeMandala()
        {
            if (timeStoneMandala != null)
            {
                timeStoneMandala.Rotate(0f, 0f, -mandalaRotationSpeed * Time.deltaTime);
            }
        }

        private void AnimateMatchPointPulse()
        {
            if (centerBigGoldGem == null) return;

            if (isMatchPoint)
            {
                float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 6f);
                centerBigGoldGem.transform.localScale = initialGoldGemScale * pulse;
            }
            else
            {
                centerBigGoldGem.transform.localScale = initialGoldGemScale;
            }
        }

        #endregion

        #region Event Handlers

        private void OnP1HealthChanged(int current, int max)
        {
            Debug.Log($"<color=#00e676>[HUDController]</color> P1 Health event received: {current}/{max}");
            UpdateHealthBar(0, max > 0 ? (float)current / max : 0f);
        }

        private void OnP2HealthChanged(int current, int max)
        {
            Debug.Log($"<color=#00e676>[HUDController]</color> P2 Health event received: {current}/{max}");
            UpdateHealthBar(1, max > 0 ? (float)current / max : 0f);
        }

        private void OnP1KiChanged(float current, float max) =>
            UpdateKiBar(0, current, max);

        private void OnP2KiChanged(float current, float max) =>
            UpdateKiBar(1, current, max);

        /// <summary>
        /// Immediately resets both active and ghost health bars to 100% full with zero ghost lag.
        /// </summary>
        public void ResetHealthBarsInstantly()
        {
            p1TargetHp = 1f;
            if (p1HealthBar != null) p1HealthBar.fillAmount = 1f;
            if (p1GhostHealthBar != null) p1GhostHealthBar.fillAmount = 1f;
            p1GhostTimer = 0f;

            p2TargetHp = 1f;
            if (p2HealthBar != null) p2HealthBar.fillAmount = 1f;
            if (p2GhostHealthBar != null) p2GhostHealthBar.fillAmount = 1f;
            p2GhostTimer = 0f;
        }

        private void OnRoundStarted(int round)
        {
            ResetHealthBarsInstantly();

            if (timerText != null)
            {
                timerText.color = Color.white;
            }

            if (matchManager != null)
            {
                UpdateRoundScore(matchManager.P1RoundWins, matchManager.P2RoundWins);
            }
        }

        private void HandleOvertimeStarted()
        {
            if (timerText != null)
            {
                timerText.color = new Color(1f, 0.85f, 0.15f, 1f); // Warning Golden Yellow
            }
            if (roundTitleText != null)
            {
                roundTitleText.text = "★ OVERTIME • SUDDEN DEATH (+100s) ★";
            }
        }

        private void OnRoundFinished(int winnerIndex, int round)
        {
            if (matchManager != null)
            {
                UpdateRoundScore(matchManager.P1RoundWins, matchManager.P2RoundWins);
            }
        }

        #endregion
    }
}
