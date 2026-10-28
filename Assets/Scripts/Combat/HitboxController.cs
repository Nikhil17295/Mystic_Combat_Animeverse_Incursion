using System.Collections.Generic;
using UnityEngine;
using MysticCombat.Systems;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Controls attack hitboxes attached to fighters.
    /// Uses active Physics2D.OverlapBoxAll queries dynamically calculated in front of the fighter's
    /// facing direction. Completely immune to 2D trigger-on-trigger blindness or stationary overlap drops.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HitboxController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Hitbox Properties (Feel & Tuning)")]
        [Tooltip("Damage dealt on hit.")]
        [SerializeField] private int damage = 10;

        [Tooltip("Knockback force applied to target.")]
        [SerializeField] private float knockback = 4.5f;
        [SerializeField] private float knockbackY = 1.0f;

        [Tooltip("Ki gained by attacker on hit.")]
        [SerializeField] private float kiGainOnHit = 6f;

        [Tooltip("Offset relative to the fighter's pivot when facing right.")]
        public Vector2 hitboxOffset = new Vector2(0.85f, 0.9f);

        [Tooltip("Width and height of the attack reach.")]
        public Vector2 hitboxSize = new Vector2(1.1f, 0.85f);

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
        private SpriteRenderer ownerSr;
        private bool enableHitStop = true;
        private float shakeIntensityMultiplier = 1.0f;

        #endregion

        #region Properties

        public bool IsActive => isActive;
        public int Damage => damage;
        public float Knockback => knockback;
        public float KnockbackY => knockbackY;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            hitboxCollider = GetComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;
            if (owner != null)
            {
                ownerEnergy = owner.GetComponent<EnergySystem>();
                ownerSr = owner.GetComponent<SpriteRenderer>();
            }
            Deactivate();
        }

        private void Start()
        {
            if (owner != null)
            {
                if (ownerEnergy == null) ownerEnergy = owner.GetComponent<EnergySystem>();
                if (ownerSr == null) ownerSr = owner.GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!isActive) return;

            // Active physics frame query: guarantees hits register even if fighters are standing still!
            Vector2 center = GetHitboxWorldCenter();
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                ProcessHit(hits[i]);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProcessHit(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ProcessHit(other);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Vector3 center = GetHitboxWorldCenter();
            Vector3 size = new Vector3(hitboxSize.x, hitboxSize.y, 0.1f);

            if (isActive)
            {
                Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.65f);
                Gizmos.DrawCube(center, size);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.25f);
                Gizmos.DrawWireCube(center, size);
            }
        }

        #endregion

        #region Public Methods

        public void Setup(Transform ownerTransform, int hitDamage, float hitKnockback, float kiGain, float hitKnockbackY = 1.0f)
        {
            owner = ownerTransform;
            damage = hitDamage;
            knockback = hitKnockback;
            knockbackY = hitKnockbackY;
            kiGainOnHit = kiGain;
            if (owner != null)
            {
                ownerEnergy = owner.GetComponent<EnergySystem>();
                ownerSr = owner.GetComponent<SpriteRenderer>();
            }
        }

        public void ConfigureAttack(int hitDamage, float hitKnockback, float kiGain, Vector2 offset, Vector2 size, float hitKnockbackY = 1.0f, bool hitStop = true, float shakeMult = 1.0f)
        {
            damage = hitDamage;
            knockback = hitKnockback;
            knockbackY = hitKnockbackY;
            kiGainOnHit = kiGain;
            hitboxOffset = offset;
            hitboxSize = size;
            enableHitStop = hitStop;
            shakeIntensityMultiplier = shakeMult;

            if (hitboxCollider == null) hitboxCollider = GetComponent<BoxCollider2D>();
            if (hitboxCollider != null)
            {
                hitboxCollider.offset = offset;
                hitboxCollider.size = size;
            }
        }

        public void Activate()
        {
            hitTargetsThisSwing.Clear();
            isActive = true;
            if (hitboxCollider != null) hitboxCollider.enabled = true;
        }

        public void Deactivate()
        {
            isActive = false;
            hitTargetsThisSwing.Clear();
            if (hitboxCollider != null) hitboxCollider.enabled = false;
        }

        public void ResetHitTargets()
        {
            hitTargetsThisSwing.Clear();
        }

        #endregion

        #region Internal Hit Logic

        private Vector2 GetHitboxWorldCenter()
        {
            float facingDir = 1f;
            if (ownerSr != null && ownerSr.flipX)
            {
                facingDir = -1f;
            }
            else if (owner != null)
            {
                // Fallback check: if facing opponent to the left
                Fighter.FighterController fc = owner.GetComponent<Fighter.FighterController>();
                if (fc != null && fc.opponent != null && fc.opponent.position.x < owner.position.x)
                {
                    facingDir = -1f;
                }
            }

            Vector2 rootPos = owner != null ? (Vector2)owner.position : (Vector2)transform.position;
            return rootPos + new Vector2(facingDir * hitboxOffset.x, hitboxOffset.y);
        }

        private void ProcessHit(Collider2D other)
        {
            if (!isActive || other == null) return;

            HealthSystem targetHealth = other.GetComponentInParent<HealthSystem>();
            if (targetHealth == null) return;

            Transform target = targetHealth.transform;

            // Never hit self
            if (owner != null && (target == owner || target == owner.root)) return;

            // Prevent multiple hits in the same attack swing
            if (hitTargetsThisSwing.Contains(target)) return;
            hitTargetsThisSwing.Add(target);

            ResolveHit(targetHealth, target);
        }

        private void ResolveHit(HealthSystem targetHealth, Transform target)
        {
            // 1. Deal Damage
            targetHealth.TakeDamage(damage);

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
                float facingDir = 1f;
                if (ownerSr != null && ownerSr.flipX) facingDir = -1f;
                else if (target.position.x < owner.position.x) facingDir = -1f;

                targetRb.linearVelocity = new Vector2(facingDir * knockback, knockbackY);
            }

            // 5. Play Visceral Hit Impact Audio Feedback
            FighterAudio targetAudio = target.GetComponent<FighterAudio>();
            if (targetAudio == null && target.parent != null)
            {
                targetAudio = target.parent.GetComponent<FighterAudio>();
            }

            if (targetAudio != null)
            {
                if (damage >= 40)
                {
                    targetAudio.PlayUltimateHit();
                }
                else if (damage >= 20)
                {
                    targetAudio.PlaySkillHit();
                }
                else if (damage >= 15)
                {
                    targetAudio.PlayHeavyHit();
                }
                else
                {
                    targetAudio.PlayLightHit();
                }
            }

            // 6. Trigger Visual Combat Juice (Hit Sparks, Shockwaves, Camera Shake & Hitstop)
            Vector2 hitCenter = Vector2.Lerp(GetHitboxWorldCenter(), (Vector2)target.position + new Vector2(0f, 0.9f), 0.5f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayHitSpark(hitCenter, isHeavy: damage >= 15, triggerHitStop: enableHitStop, shakeMultiplier: shakeIntensityMultiplier);
                if (damage >= 35)
                {
                    VFXManager.Instance.TriggerImpactFlash();
                }
            }

            // 7. Apply Hitstun & Hurt Reaction
            Fighter.FighterController targetFc = target.GetComponent<Fighter.FighterController>();
            if (targetFc != null)
            {
                float stun = damage >= 15 ? 0.42f : 0.25f;
                targetFc.ApplyHit(damage, stun);
            }

            Debug.Log($"<color=#00e676>[COMBAT HIT RESOLVED]</color> {owner?.name} -> {target.name} | Damage: {damage} | New HP: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth} | Attacker Ki: +{kiGainOnHit}");
        }

        #endregion
    }
}
