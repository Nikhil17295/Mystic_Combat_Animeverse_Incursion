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
        [SerializeField] private int maxHealth = 200;

        private int currentHealth;
        private float fractionalHealthPool = 0f;
        private EnergySystem energySystem;
        private Fighter.FighterController fighterController;

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
        /// Whether the fighter is currently invulnerable (e.g. final 1.0s of ultimate casting).
        /// </summary>
        public bool IsInvulnerable => (fighterController != null && fighterController.IsInvulnerable);

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
            if (fighterController == null) fighterController = GetComponent<Fighter.FighterController>();
            if (fighterController != null && fighterController.IsInvulnerable)
            {
                Debug.Log($"<color=#38bdf8>[INVULNERABLE / SUPER ARMOR]</color> {gameObject.name} absorbed {amount} damage during ultimate climax!");
                return;
            }
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

            // Tsunade Creation Rebirth / Byakugou Healing Stage: Takes half damage (50% reduction)!
            if (fighterController != null && fighterController.IsHealingStage)
            {
                int baseAmount = amount;
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * 0.5f));
                Debug.Log($"<color=#10b981>[CREATION REBIRTH RESISTANCE]</color> {gameObject.name} in Healing Stage! Damage halved: {baseAmount} -> {amount}.");
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
        /// Restores health smoothly across frames (e.g. Tsunade +2 HP/s regen during Healing Stage).
        /// </summary>
        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            fractionalHealthPool += amount;
            if (fractionalHealthPool >= 1f)
            {
                int whole = Mathf.FloorToInt(fractionalHealthPool);
                fractionalHealthPool -= whole;
                Heal(whole);
            }
        }

        /// <summary>
        /// Resets health to maximum. Useful for starting a new round.
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Directly synchronizes health from an authoritative LAN snapshot.
        /// </summary>
        public void SetHealthDirect(int val)
        {
            currentHealth = Mathf.Clamp(val, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        #endregion
    }
}
