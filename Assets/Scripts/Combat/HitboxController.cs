using System.Collections.Generic;
using UnityEngine;
using MysticCombat.Systems;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Controls a hitbox trigger collider attached to an attacking fighter.
    /// Manages damage resolution, physics knockback, Ki generation, and draws
    /// visual red Scene Gizmos so designers can visually adjust hitboxes.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HitboxController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Hitbox Properties (Feel & Tuning)")]
        [Tooltip("Damage dealt on hit.")]
        [SerializeField] private int damage = 10;

        [Tooltip("Knockback force applied to target.")]
        [SerializeField] private float knockback = 6f;

        [Tooltip("Ki gained by attacker on hit.")]
        [SerializeField] private float kiGainOnHit = 6f;

        [Tooltip("Offset relative to the fighter's pivot.")]
        public Vector2 hitboxOffset = new Vector2(0.8f, 0.9f);

        [Tooltip("Width and height of the attack reach.")]
        public Vector2 hitboxSize = new Vector2(0.9f, 0.7f);

        [Header("Owner")]
        [Tooltip("The root transform of the attacking fighter.")]
        [SerializeField] private Transform owner;

        [Header("Gizmo Visualization")]
        [Tooltip("Whether to always draw the red hitbox gizmo in the Scene view.")]
        [SerializeField] private bool showGizmo = true;

        #endregion

        #region State

        private bool isActive;
        private BoxCollider2D hitboxCollider;
        private readonly HashSet<Transform> hitTargetsThisSwing = new HashSet<Transform>();
        private EnergySystem ownerEnergy;

        #endregion

        #region Properties

        public bool IsActive => isActive;
        public int Damage => damage;
        public float Knockback => knockback;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            hitboxCollider = GetComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;
            ApplyColliderGeometry();
            Deactivate();
        }

        private void Start()
        {
            if (owner != null)
            {
                ownerEnergy = owner.GetComponent<EnergySystem>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            // Only collide with Hurtboxes
            if (!other.CompareTag("Hurtbox")) return;

            HurtboxController hurtbox = other.GetComponent<HurtboxController>();
            if (hurtbox == null || hurtbox.Owner == null) return;

            // Never hit self
            if (hurtbox.Owner == owner) return;

            // Prevent hitting the same opponent multiple times in one swing
            if (hitTargetsThisSwing.Contains(hurtbox.Owner)) return;
            hitTargetsThisSwing.Add(hurtbox.Owner);

            ResolveHit(hurtbox);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            // Bright red when active, dim red outline when inactive
            Vector3 center = transform.position + (Vector3)hitboxOffset;
            Vector3 size = new Vector3(hitboxSize.x, hitboxSize.y, 0.1f);

            if (isActive)
            {
                Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.55f);
                Gizmos.DrawCube(center, size);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
                Gizmos.DrawWireCube(center, size);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the owner and initial stats for this hitbox.
        /// </summary>
        public void Setup(Transform ownerTransform, int hitDamage, float hitKnockback, float kiGain)
        {
            owner = ownerTransform;
            damage = hitDamage;
            knockback = hitKnockback;
            kiGainOnHit = kiGain;
            if (owner != null)
            {
                ownerEnergy = owner.GetComponent<EnergySystem>();
            }
        }

        /// <summary>
        /// Configures the hitbox geometry and stats for a specific attack (Light, Heavy, Skill, Ult).
        /// </summary>
        public void ConfigureAttack(int hitDamage, float hitKnockback, float kiGain, Vector2 offset, Vector2 size)
        {
            damage = hitDamage;
            knockback = hitKnockback;
            kiGainOnHit = kiGain;
            hitboxOffset = offset;
            hitboxSize = size;
            ApplyColliderGeometry();
        }

        /// <summary>
        /// Activates the hitbox for an attack swing. Clears previously hit targets.
        /// </summary>
        public void Activate()
        {
            hitTargetsThisSwing.Clear();
            ApplyColliderGeometry();
            isActive = true;
            if (hitboxCollider != null) hitboxCollider.enabled = true;
        }

        /// <summary>
        /// Deactivates the hitbox after an attack swing ends.
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            hitTargetsThisSwing.Clear();
            if (hitboxCollider != null) hitboxCollider.enabled = false;
        }

        #endregion

        #region Internal Hit Logic

        private void ApplyColliderGeometry()
        {
            if (hitboxCollider == null) hitboxCollider = GetComponent<BoxCollider2D>();
            if (hitboxCollider != null)
            {
                hitboxCollider.offset = hitboxOffset;
                hitboxCollider.size = hitboxSize;
            }
        }

        private void ResolveHit(HurtboxController hurtbox)
        {
            Transform target = hurtbox.Owner;

            // 1. Deal Damage
            HealthSystem targetHealth = target.GetComponent<HealthSystem>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }

            // 2. Grant Ki to Attacker
            if (ownerEnergy != null)
            {
                ownerEnergy.AddKi(kiGainOnHit);
            }

            // 3. Grant Comeback Ki to Target
            EnergySystem targetEnergy = target.GetComponent<EnergySystem>();
            if (targetEnergy != null)
            {
                targetEnergy.AddKi(3f);
            }

            // 4. Apply Knockback Physics
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                // Calculate push direction based on attacker facing
                float facingDir = 1f;
                SpriteRenderer sr = owner.GetComponent<SpriteRenderer>();
                if (sr != null && sr.flipX)
                {
                    facingDir = -1f;
                }
                else if (target.position.x < owner.position.x)
                {
                    facingDir = -1f;
                }

                targetRb.linearVelocity = new Vector2(facingDir * knockback, 3.2f);
            }

            Debug.Log($"<color=#ff4444>[COMBAT]</color> {owner.name} LANDED HIT on {target.name}! Damage: {damage}, Ki Gain: +{kiGainOnHit}");
        }

        #endregion
    }
}
