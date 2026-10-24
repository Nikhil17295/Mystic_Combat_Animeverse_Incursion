using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysticCombat.Systems;

namespace MysticCombat.UI
{
    /// <summary>
    /// Manages the in-match HUD: health bars, Ki bars, timer, round score, and Red Zone indicators.
    /// Day 1: provides the public API and subscribes to HealthSystem/EnergySystem events.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Health Bars")]
        [Tooltip("P1 health bar fill image (Image.fillAmount will be set).")]
        [SerializeField] private Image p1HealthBar;

        [Tooltip("P2 health bar fill image.")]
        [SerializeField] private Image p2HealthBar;

        [Header("Ki Bars")]
        [Tooltip("P1 Ki bar fill image.")]
        [SerializeField] private Image p1KiBar;

        [Tooltip("P2 Ki bar fill image.")]
        [SerializeField] private Image p2KiBar;

        [Header("Timer")]
        [Tooltip("Text displaying the round timer.")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Round Score")]
        [Tooltip("Text displaying the current round score.")]
        [SerializeField] private TextMeshProUGUI roundScoreText;

        [Header("Red Zone Indicators")]
        [Tooltip("P1 Red Zone warning indicator GameObject.")]
        [SerializeField] private GameObject p1RedZoneIndicator;

        [Tooltip("P2 Red Zone warning indicator GameObject.")]
        [SerializeField] private GameObject p2RedZoneIndicator;

        [Header("System References")]
        [Tooltip("Player 1's HealthSystem.")]
        [SerializeField] private HealthSystem p1Health;

        [Tooltip("Player 2's HealthSystem.")]
        [SerializeField] private HealthSystem p2Health;

        [Tooltip("Player 1's EnergySystem.")]
        [SerializeField] private EnergySystem p1Energy;

        [Tooltip("Player 2's EnergySystem.")]
        [SerializeField] private EnergySystem p2Energy;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Binds the HUD to the specified fighter systems. Call this if fighters are created at runtime.
        /// </summary>
        /// <param name="p1Hp">Player 1's HealthSystem.</param>
        /// <param name="p2Hp">Player 2's HealthSystem.</param>
        /// <param name="p1Ki">Player 1's EnergySystem.</param>
        /// <param name="p2Ki">Player 2's EnergySystem.</param>
        public void BindSystems(HealthSystem p1Hp, HealthSystem p2Hp, EnergySystem p1Ki, EnergySystem p2Ki)
        {
            UnsubscribeEvents();

            p1Health = p1Hp;
            p2Health = p2Hp;
            p1Energy = p1Ki;
            p2Energy = p2Ki;

            SubscribeEvents();

            // Initialize bars to current values
            if (p1Health != null) UpdateHealthBar(0, p1Health.HealthPercent);
            if (p2Health != null) UpdateHealthBar(1, p2Health.HealthPercent);
            if (p1Energy != null) UpdateKiBar(0, p1Energy.KiPercent);
            if (p2Energy != null) UpdateKiBar(1, p2Energy.KiPercent);
        }

        /// <summary>
        /// Updates a player's health bar fill amount.
        /// </summary>
        /// <param name="playerIndex">0 for P1, 1 for P2.</param>
        /// <param name="percent">Health percentage (0 to 1).</param>
        public void UpdateHealthBar(int playerIndex, float percent)
        {
            Image bar = playerIndex == 0 ? p1HealthBar : p2HealthBar;
            if (bar != null)
            {
                bar.fillAmount = Mathf.Clamp01(percent);
            }
        }

        /// <summary>
        /// Updates a player's Ki bar fill amount.
        /// </summary>
        /// <param name="playerIndex">0 for P1, 1 for P2.</param>
        /// <param name="percent">Ki percentage (0 to 1).</param>
        public void UpdateKiBar(int playerIndex, float percent)
        {
            Image bar = playerIndex == 0 ? p1KiBar : p2KiBar;
            if (bar != null)
            {
                bar.fillAmount = Mathf.Clamp01(percent);
            }
        }

        /// <summary>
        /// Updates the round timer display.
        /// </summary>
        /// <param name="time">Remaining time in seconds.</param>
        public void UpdateTimer(float time)
        {
            if (timerText != null)
            {
                int displayTime = Mathf.CeilToInt(Mathf.Max(0f, time));
                timerText.text = displayTime.ToString();
            }
        }

        /// <summary>
        /// Updates the round score display.
        /// </summary>
        /// <param name="p1Wins">Player 1's round wins.</param>
        /// <param name="p2Wins">Player 2's round wins.</param>
        public void UpdateRoundScore(int p1Wins, int p2Wins)
        {
            if (roundScoreText != null)
            {
                roundScoreText.text = $"{p1Wins} - {p2Wins}";
            }
        }

        /// <summary>
        /// Shows or hides the Red Zone indicator for a player.
        /// </summary>
        /// <param name="playerIndex">0 for P1, 1 for P2.</param>
        /// <param name="show">True to show, false to hide.</param>
        public void ShowRedZoneIndicator(int playerIndex, bool show)
        {
            GameObject indicator = playerIndex == 0 ? p1RedZoneIndicator : p2RedZoneIndicator;
            if (indicator != null)
            {
                indicator.SetActive(show);
            }
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeEvents()
        {
            if (p1Health != null)
                p1Health.OnHealthChanged += OnP1HealthChanged;
            if (p2Health != null)
                p2Health.OnHealthChanged += OnP2HealthChanged;
            if (p1Energy != null)
            {
                p1Energy.OnKiChanged += OnP1KiChanged;
                p1Energy.OnRedZoneEnter += OnP1RedZoneEnter;
                p1Energy.OnRedZoneExit += OnP1RedZoneExit;
            }
            if (p2Energy != null)
            {
                p2Energy.OnKiChanged += OnP2KiChanged;
                p2Energy.OnRedZoneEnter += OnP2RedZoneEnter;
                p2Energy.OnRedZoneExit += OnP2RedZoneExit;
            }
        }

        private void UnsubscribeEvents()
        {
            if (p1Health != null)
                p1Health.OnHealthChanged -= OnP1HealthChanged;
            if (p2Health != null)
                p2Health.OnHealthChanged -= OnP2HealthChanged;
            if (p1Energy != null)
            {
                p1Energy.OnKiChanged -= OnP1KiChanged;
                p1Energy.OnRedZoneEnter -= OnP1RedZoneEnter;
                p1Energy.OnRedZoneExit -= OnP1RedZoneExit;
            }
            if (p2Energy != null)
            {
                p2Energy.OnKiChanged -= OnP2KiChanged;
                p2Energy.OnRedZoneEnter -= OnP2RedZoneEnter;
                p2Energy.OnRedZoneExit -= OnP2RedZoneExit;
            }
        }

        #endregion

        #region Event Handlers

        private void OnP1HealthChanged(int current, int max) =>
            UpdateHealthBar(0, (float)current / max);

        private void OnP2HealthChanged(int current, int max) =>
            UpdateHealthBar(1, (float)current / max);

        private void OnP1KiChanged(float current, float max) =>
            UpdateKiBar(0, max > 0f ? current / max : 0f);

        private void OnP2KiChanged(float current, float max) =>
            UpdateKiBar(1, max > 0f ? current / max : 0f);

        private void OnP1RedZoneEnter() => ShowRedZoneIndicator(0, true);
        private void OnP1RedZoneExit() => ShowRedZoneIndicator(0, false);
        private void OnP2RedZoneEnter() => ShowRedZoneIndicator(1, true);
        private void OnP2RedZoneExit() => ShowRedZoneIndicator(1, false);

        #endregion
    }
}
