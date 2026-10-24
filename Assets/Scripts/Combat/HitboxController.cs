using UnityEngine;
using MysticCombat.Systems;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Controls a hitbox trigger collider attached to a fighter.
    /// Day 1 stub — detects hits and logs them; full damage integration comes later.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HitboxController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Hitbox Settings")]
        [Tooltip("Damage dealt by this hitbox on contact.")]
        [SerializeField] private int damage = 10;

        [Tooltip("Knockback force applied on hit.")]
        [SerializeField] private float knockback = 5f;

        [Tooltip("The fighter that owns this hitbox (prevents self-hits).")]
        [SerializeField] private Transform owner;

        #endregion

        #region State

        private bool isActive;
        private BoxCollider2D hitboxCollider;

        #endregion

        #region Properties

        /// <summary>
        /// Whether this hitbox is currently active and can register hits.
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// The damage this hitbox deals.
        /// </summary>
        public int Damage => damage;

        /// <summary>
        /// The knockback force this hitbox applies.
        /// </summary>
        public float Knockback => knockback;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            hitboxCollider = GetComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;
            Deactivate(); // Start inactive
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            // Only react to objects tagged "Hurtbox"
            if (!other.CompareTag("Hurtbox")) return;

            HurtboxController hurtbox = other.GetComponent<HurtboxController>();
            if (hurtbox == null) return;

            // Don't hit ourselves
            if (hurtbox.Owner == owner) return;

            // Try to deal damage
            HealthSystem targetHealth = hurtbox.Owner.GetComponent<HealthSystem>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
                Debug.Log($"[HitboxController] {owner.name} hit {hurtbox.Owner.name} for {damage} damage!");
            }
            else
            {
                Debug.Log($"[HitboxController] {owner.name} hit {hurtbox.Owner.name} (no HealthSystem found).");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up this hitbox with an owner, damage, and knockback.
        /// </summary>
        /// <param name="ownerTransform">The fighter that owns this hitbox.</param>
        /// <param name="hitDamage">Damage on hit.</param>
        /// <param name="hitKnockback">Knockback force on hit.</param>
        public void Setup(Transform ownerTransform, int hitDamage, float hitKnockback)
        {
            owner = ownerTransform;
            damage = hitDamage;
            knockback = hitKnockback;
        }

        /// <summary>
        /// Activates the hitbox so it can register hits.
        /// </summary>
        public void Activate()
        {
            isActive = true;
            hitboxCollider.enabled = true;
        }

        /// <summary>
        /// Deactivates the hitbox, preventing it from registering hits.
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = false;
            }
        }

        #endregion
    }
}
