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

        [Header("Tactical Ki Economy & Action Costs")]
        [Tooltip("Ki consumed or recommended buffer for fighter's special skill.")]
        [SerializeField] private float skillKiCost = 20f;

        [Tooltip("Ki consumed by the fighter's ultimate ability.")]
        [SerializeField] private float ultimateKiCost = 80f;

        [Tooltip("Optional custom safe skill threshold (e.g. 30 Ki for Goku SSJ buffer). If <= 0, defaults to redZoneThreshold + skillKiCost.")]
        [SerializeField] private float safeSkillThreshold = -1f;

        [Tooltip("Optional custom safe ultimate threshold. If <= 0, defaults to redZoneThreshold + ultimateKiCost.")]
        [SerializeField] private float safeUltimateThreshold = -1f;

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

        /// <summary>
        /// The Red Zone danger threshold below which exhaustion occurs.
        /// </summary>
        public float RedZoneThreshold => redZoneThreshold;

        /// <summary>
        /// The Ki cost or recommended safety buffer for the fighter's Skill.
        /// </summary>
        public float SkillKiCost => skillKiCost;

        /// <summary>
        /// The Ki cost for the fighter's Ultimate ability.
        /// </summary>
        public float UltimateKiCost => ultimateKiCost;

        /// <summary>
        /// Safe Skill Threshold.
        /// Reaching this threshold guarantees that using skill leaves fighter safe from Red Zone.
        /// For Goku, calibrated to 30 Ki (SSJ buffer); for standard fighters: redZoneThreshold + skillKiCost.
        /// </summary>
        public float SafeSkillThreshold => safeSkillThreshold > 0f 
            ? safeSkillThreshold 
            : (redZoneThreshold + skillKiCost);

        /// <summary>
        /// Safe Ultimate Threshold = RedZoneThreshold + UltimateKiCost.
        /// Reaching this threshold guarantees that firing Ultimate leaves fighter safe from Red Zone.
        /// Any Ki above this threshold fills the Yellow surplus zone.
        /// </summary>
        public float SafeUltimateThreshold => safeUltimateThreshold > 0f 
            ? safeUltimateThreshold 
            : Mathf.Min(maxKi, redZoneThreshold + ultimateKiCost);

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

        /// <summary>
        /// Fired whenever tactical thresholds or energy costs are updated.
        /// </summary>
        public event Action OnThresholdsChanged;

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
        /// <summary>
        /// Configures tactical thresholds directly from deployed fighter stats.
        /// </summary>
        public void ConfigureThresholds(float redThreshold, float skillCost, float ultCost, float customSafeSkill = -1f, float customSafeUlt = -1f)
        {
            redZoneThreshold = redThreshold;
            skillKiCost = skillCost;
            ultimateKiCost = ultCost;
            safeSkillThreshold = customSafeSkill;
            safeUltimateThreshold = customSafeUlt;
            OnThresholdsChanged?.Invoke();
        }

        public void Initialize(float max, float threshold, float duration, float recovery, float skillCost = 20f, float ultCost = 80f, float customSafeSkill = -1f, float customSafeUlt = -1f)
        {
            maxKi = max;
            redZoneThreshold = threshold;
            redZoneDuration = duration;
            recoveryKi = recovery;
            skillKiCost = skillCost;
            ultimateKiCost = ultCost;
            safeSkillThreshold = customSafeSkill;
            safeUltimateThreshold = customSafeUlt;
            currentKi = 0f;
            isInRedZone = false;
            redZoneTimer = 0f;
            OnKiChanged?.Invoke(currentKi, maxKi);
            OnThresholdsChanged?.Invoke();
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

        /// <summary>
        /// Directly synchronizes Ki from an authoritative LAN snapshot.
        /// </summary>
        public void SetKiDirect(float val)
        {
            currentKi = Mathf.Clamp(val, 0f, maxKi);
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
