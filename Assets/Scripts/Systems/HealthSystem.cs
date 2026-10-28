using System;
using UnityEngine;

namespace MysticCombat.Systems
{
    /// <summary>
    /// Tracks and manages hit points for a single fighter.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        #region Fields

        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;

        private int currentHealth;
        private EnergySystem energySystem;

        #endregion

        #region Properties

        /// <summary>
        /// The fighter's maximum health.
        /// </summary>
        public int MaxHealth => maxHealth;

        /// <summary>
        /// The fighter's current health.
        /// </summary>
        public int CurrentHealth => currentHealth;

        /// <summary>
        /// Whether the fighter is still alive (health > 0).
        /// </summary>
        public bool IsAlive => currentHealth > 0;

        /// <summary>
        /// Current health as a 0-1 fraction of max health.
        /// </summary>
        public float HealthPercent => (float)currentHealth / maxHealth;

        #endregion

        #region Events

        /// <summary>
        /// Fired whenever health changes. Parameters: (currentHealth, maxHealth).
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>
        /// Fired when health reaches zero.
        /// </summary>
        public event Action OnDeath;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            currentHealth = maxHealth;
            energySystem = GetComponent<EnergySystem>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the health system with a specific max HP value.
        /// Sets current health to max.
        /// </summary>
        /// <param name="maxHp">The maximum health value.</param>
        public void Initialize(int maxHp)
        {
            maxHealth = maxHp;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Reduces current health by the given amount. Clamps to 0.
        /// Fires OnHealthChanged and OnDeath as appropriate.
        /// Accounts for Red Zone exhaustion (+35% extra damage vulnerability).
        /// </summary>
        /// <param name="amount">Damage to apply (positive value).</param>
        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;
            if (amount <= 0) return;

            if (energySystem == null)
            {
                energySystem = GetComponent<EnergySystem>();
            }

            // Red Zone Exhaustion Vulnerability: 35% increased damage taken!
            if (energySystem != null && energySystem.IsInRedZone)
            {
                int baseAmount = amount;
                amount = Mathf.RoundToInt(amount * 1.35f);
                Debug.Log($"<color=#ef4444>[HealthSystem] {gameObject.name} in RED ZONE! Vulnerability (+35%): {baseAmount} -> {amount} damage.</color>");
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"[HealthSystem] {gameObject.name} took {amount} damage. HP: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Debug.Log($"[HealthSystem] {gameObject.name} has been KO'd!");
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// Restores current health by the given amount. Clamps to max.
        /// </summary>
        /// <param name="amount">Health to restore (positive value).</param>
        public void Heal(int amount)
        {
            if (amount <= 0) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"[HealthSystem] {gameObject.name} healed {amount}. HP: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Resets health to maximum. Useful for starting a new round.
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        #endregion
    }
}
