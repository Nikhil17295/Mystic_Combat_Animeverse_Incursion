using System;
using UnityEngine;

namespace MysticCombat.Systems
{
    /// <summary>
    /// Tracks and manages Ki (energy) for a single fighter, including Red Zone mechanics.
    /// </summary>
    public class EnergySystem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Ki Settings")]
        [SerializeField] private float maxKi = 100f;
        [SerializeField] private float currentKi = 0f;

        [Header("Red Zone Settings")]
        [SerializeField] private float redZoneThreshold = 10f;
        [SerializeField] private float redZoneDuration = 5f;
        [SerializeField] private float recoveryKi = 20f;

        #endregion

        #region State

        private float redZoneTimer;
        private bool isInRedZone;

        #endregion

        #region Properties

        /// <summary>
        /// The fighter's maximum Ki.
        /// </summary>
        public float MaxKi => maxKi;

        /// <summary>
        /// The fighter's current Ki.
        /// </summary>
        public float CurrentKi => currentKi;

        /// <summary>
        /// Current Ki as a 0-1 fraction of max Ki.
        /// </summary>
        public float KiPercent => maxKi > 0f ? currentKi / maxKi : 0f;

        /// <summary>
        /// Whether the fighter is currently in Red Zone (low Ki danger state).
        /// </summary>
        public bool IsInRedZone => isInRedZone && redZoneTimer > 0f;

        /// <summary>
        /// Remaining time in the Red Zone state.
        /// </summary>
        public float RedZoneTimeRemaining => redZoneTimer;

        #endregion

        #region Events

        /// <summary>
        /// Fired whenever Ki changes. Parameters: (currentKi, maxKi).
        /// </summary>
        public event Action<float, float> OnKiChanged;

        /// <summary>
        /// Fired when the fighter enters Red Zone.
        /// </summary>
        public event Action OnRedZoneEnter;

        /// <summary>
        /// Fired when the fighter exits Red Zone (timer expired, Ki recovered).
        /// </summary>
        public event Action OnRedZoneExit;

        #endregion

        #region Unity Callbacks

        private void Update()
        {
            if (isInRedZone)
            {
                TickRedZone();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the energy system with specific values.
        /// </summary>
        /// <param name="max">Maximum Ki capacity.</param>
        /// <param name="threshold">Ki level at or below which Red Zone activates.</param>
        /// <param name="duration">Duration of the Red Zone state in seconds.</param>
        /// <param name="recovery">Ki restored when exiting Red Zone.</param>
        public void Initialize(float max, float threshold, float duration, float recovery)
        {
            maxKi = max;
            redZoneThreshold = threshold;
            redZoneDuration = duration;
            recoveryKi = recovery;
            currentKi = 0f;
            isInRedZone = false;
            redZoneTimer = 0f;
            OnKiChanged?.Invoke(currentKi, maxKi);
        }

        /// <summary>
        /// Adds Ki energy. Clamped to max.
        /// </summary>
        /// <param name="amount">Amount of Ki to add (positive value).</param>
        public void AddKi(float amount)
        {
            if (amount <= 0f) return;

            currentKi = Mathf.Min(maxKi, currentKi + amount);
            OnKiChanged?.Invoke(currentKi, maxKi);
        }

        /// <summary>
        /// Spends Ki energy. Clamped to 0. May trigger Red Zone if Ki drops to/below threshold.
        /// </summary>
        /// <param name="amount">Amount of Ki to spend (positive value).</param>
        public void SpendKi(float amount)
        {
            if (amount <= 0f) return;

            currentKi = Mathf.Max(0f, currentKi - amount);
            OnKiChanged?.Invoke(currentKi, maxKi);

            // Check if we should enter Red Zone
            if (!isInRedZone && currentKi <= redZoneThreshold)
            {
                EnterRedZone();
            }
        }

        /// <summary>
        /// Checks whether the fighter can afford a Ki cost.
        /// </summary>
        /// <param name="cost">The Ki cost to check.</param>
        /// <returns>True if currentKi >= cost.</returns>
        public bool CanAfford(float cost)
        {
            return currentKi >= cost;
        }

        /// <summary>
        /// Reduces the Red Zone timer by a specified amount (comeback hit mechanic).
        /// If the timer reaches 0, Red Zone exits early.
        /// </summary>
        /// <param name="amount">Seconds to subtract from the Red Zone timer.</param>
        public void ReduceRedZoneTimer(float amount)
        {
            if (!isInRedZone) return;
            if (amount <= 0f) return;

            redZoneTimer = Mathf.Max(0f, redZoneTimer - amount);

            if (redZoneTimer <= 0f)
            {
                ExitRedZone();
            }
        }

        /// <summary>
        /// Resets Ki to zero and clears Red Zone. Useful for starting a new round.
        /// </summary>
        public void ResetKi()
        {
            currentKi = 0f;
            isInRedZone = false;
            redZoneTimer = 0f;
            OnKiChanged?.Invoke(currentKi, maxKi);
        }

        #endregion

        #region Red Zone Logic

        private void EnterRedZone()
        {
            isInRedZone = true;
            redZoneTimer = redZoneDuration;
            Debug.Log($"[EnergySystem] {gameObject.name} entered RED ZONE! Duration: {redZoneDuration}s");
            OnRedZoneEnter?.Invoke();
        }

        private void TickRedZone()
        {
            redZoneTimer -= Time.deltaTime;

            if (redZoneTimer <= 0f)
            {
                ExitRedZone();
            }
        }

        private void ExitRedZone()
        {
            isInRedZone = false;
            redZoneTimer = 0f;

            // Recover Ki on exit
            currentKi = Mathf.Min(maxKi, currentKi + recoveryKi);
            Debug.Log($"[EnergySystem] {gameObject.name} exited RED ZONE. Recovered {recoveryKi} Ki. Current Ki: {currentKi}");

            OnKiChanged?.Invoke(currentKi, maxKi);
            OnRedZoneExit?.Invoke();
        }

        #endregion
    }
}
