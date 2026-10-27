using UnityEngine;
using MysticCombat.Input;
using MysticCombat.Systems;
using MysticCombat.Combat;

namespace MysticCombat.Fighter
{
    /// <summary>
    /// Main fighter controller handling movement, jumping, crouching, auto-facing,
    /// meditation, attacks, and Red Zone exhaustion speed penalty.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class FighterController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("The input handler driving this fighter.")]
        [SerializeField] private PlayerInputHandler inputHandler;

        [Tooltip("The opposing fighter's transform for auto-facing.")]
        public Transform opponent;

        [Tooltip("Attack hitbox controller attached to this fighter.")]
        [SerializeField] private HitboxController attackHitbox;

        [Tooltip("Fighter audio controller for authentic combat sound effects.")]
        [SerializeField] private FighterAudio fighterAudio;

        [Header("Ground Check")]
        [Tooltip("Transform positioned at the fighter's feet for ground detection.")]
        [SerializeField] private Transform groundCheckPoint;

        [Tooltip("Radius of the ground-check overlap circle.")]
        [SerializeField] private float groundCheckRadius = 0.15f;

        [Tooltip("Layer mask for surfaces considered ground.")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Movement Tuning")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float gravityScale = 3f;

        [Header("Ki & Red Zone Tuning")]
        [SerializeField] private float meditateKiPerSecond = 15f;
        [SerializeField] private float redZoneSpeedMultiplier = 0.35f;

        #endregion

        #region Cached Components

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D bodyCollider;
        private EnergySystem energySystem;
        private float attackTimer;
        private float attackStartupTimer;
        private float attackActiveTimer;
        private float flurryHitTimer;

        #endregion

        #region State

        /// <summary>
        /// The fighter's current state.
        /// </summary>
        public FighterState CurrentState { get; private set; } = FighterState.Idle;

        /// <summary>
        /// Whether the fighter is currently touching the ground.
        /// </summary>
        public bool IsGrounded { get; private set; }

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            bodyCollider = GetComponent<BoxCollider2D>();
            energySystem = GetComponent<EnergySystem>();

            if (fighterAudio == null)
            {
                fighterAudio = GetComponent<FighterAudio>();
                if (fighterAudio == null)
                {
                    fighterAudio = gameObject.AddComponent<FighterAudio>();
                }
            }

            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
        }

        private void OnEnable()
        {
            if (energySystem != null)
            {
                energySystem.OnRedZoneEnter -= HandleRedZoneEnter;
                energySystem.OnRedZoneEnter += HandleRedZoneEnter;
            }
        }

        private void OnDisable()
        {
            if (energySystem != null)
            {
                energySystem.OnRedZoneEnter -= HandleRedZoneEnter;
            }
        }

        private void HandleRedZoneEnter()
        {
            fighterAudio?.PlayRedZoneWarning();
        }

        private void Start()
        {
            // Give fighters 25 initial Ki so players can immediately test special skills!
            if (energySystem != null && energySystem.CurrentKi < 25f)
            {
                energySystem.AddKi(25f);
            }
        }

        private void Update()
        {
            CheckGround();
            HandleStateTransitions();
            HandleKiGeneration();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        private void LateUpdate()
        {
            AutoFaceOpponent();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Assigns the input handler for this fighter.
        /// </summary>
        public void SetInputHandler(PlayerInputHandler handler)
        {
            inputHandler = handler;
        }

        /// <summary>
        /// Assigns the ground check transform used for ground detection.
        /// </summary>
        public void SetGroundCheckPoint(Transform point)
        {
            groundCheckPoint = point;
        }

        /// <summary>
        /// Sets the ground layer mask for ground detection.
        /// </summary>
        public void SetGroundLayer(LayerMask mask)
        {
            groundLayer = mask;
        }

        /// <summary>
        /// Assigns the attack hitbox controller for this fighter.
        /// </summary>
        public void SetHitbox(HitboxController hitbox)
        {
            attackHitbox = hitbox;
        }

        /// <summary>
        /// Completely resets the fighter's state, physics velocity, hitboxes, and position for a clean new round.
        /// </summary>
        public void ResetForNewRound(Vector3 spawnPosition, bool faceRight)
        {
            transform.position = spawnPosition;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            TransitionTo(FighterState.Idle);

            attackTimer = 0f;
            attackStartupTimer = 0f;
            attackActiveTimer = 0f;

            if (attackHitbox != null)
            {
                attackHitbox.Deactivate();
                attackHitbox.ResetHitTargets();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !faceRight;
            }

            FighterAnimator anim = GetComponent<FighterAnimator>();
            if (anim != null)
            {
                anim.ResetToIdle();
            }

            Debug.Log($"<color=#00e676>[FighterController]</color> {gameObject.name} reset for new round at {spawnPosition} (Facing: {(faceRight ? "Right" : "Left")})");
        }

        /// <summary>
        /// Applies movement and Ki tuning values (from character JSON data).
        /// </summary>
        public void ApplyStats(float speed, float jump, float gravity, float meditateRate = 15f, float redZoneSpeedMult = 0.35f)
        {
            moveSpeed = speed;
            jumpForce = jump;
            gravityScale = gravity;
            meditateKiPerSecond = meditateRate;
            redZoneSpeedMultiplier = redZoneSpeedMult;

            if (rb != null) rb.gravityScale = gravityScale;
        }

        #endregion

        #region Ground Check

        private void CheckGround()
        {
            if (groundCheckPoint == null)
            {
                IsGrounded = true; // Fallback: assume grounded if no check point assigned.
                return;
            }

            IsGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        }

        #endregion

        #region State Machine

        private void HandleStateTransitions()
        {
            if (inputHandler == null) return;

            // Check if fighter is in Red Zone (Exhausted)
            bool isExhausted = energySystem != null && energySystem.IsInRedZone;

            switch (CurrentState)
            {
                case FighterState.Idle:
                case FighterState.Walking:
                case FighterState.Exhausted:
                    HandleGroundedTransitions(isExhausted);
                    break;

                case FighterState.Meditating:
                    if (!inputHandler.MeditateHeld || !IsGrounded)
                    {
                        TransitionTo(isExhausted ? FighterState.Exhausted : FighterState.Idle);
                    }
                    break;

                case FighterState.Jumping:
                    if (rb.linearVelocity.y < 0f)
                    {
                        TransitionTo(FighterState.Falling);
                    }
                    break;

                case FighterState.Falling:
                    if (IsGrounded)
                    {
                        TransitionTo(isExhausted ? FighterState.Exhausted : FighterState.Idle);
                    }
                    break;

                case FighterState.Crouching:
                    if (!inputHandler.CrouchHeld || !IsGrounded)
                    {
                        TransitionTo(isExhausted ? FighterState.Exhausted : FighterState.Idle);
                    }
                    break;

                case FighterState.LightAttack:
                case FighterState.HeavyAttack:
                case FighterState.Ultimate:
                    attackTimer -= Time.deltaTime;

                    // Startup phase countdown
                    if (attackStartupTimer > 0f)
                    {
                        attackStartupTimer -= Time.deltaTime;
                        if (attackStartupTimer <= 0f && attackHitbox != null)
                        {
                            attackHitbox.Activate();
                        }
                    }
                    // Active hitbox window countdown
                    else if (attackActiveTimer > 0f)
                    {
                        attackActiveTimer -= Time.deltaTime;
                        if (attackActiveTimer <= 0f && attackHitbox != null)
                        {
                            attackHitbox.Deactivate();
                        }
                    }

                    if (attackTimer <= 0f)
                    {
                        if (attackHitbox != null) attackHitbox.Deactivate();
                        TransitionTo(isExhausted ? FighterState.Exhausted : FighterState.Idle);
                    }
                    break;

                case FighterState.Skill:
                    attackTimer -= Time.deltaTime;

                    if (attackStartupTimer > 0f)
                    {
                        attackStartupTimer -= Time.deltaTime;
                        if (attackStartupTimer <= 0f && attackHitbox != null)
                        {
                            attackHitbox.Activate();
                            flurryHitTimer = 0.16f;
                        }
                    }
                    else if (attackActiveTimer > 0f)
                    {
                        attackActiveTimer -= Time.deltaTime;
                        flurryHitTimer -= Time.deltaTime;
                        if (flurryHitTimer <= 0f)
                        {
                            flurryHitTimer = 0.16f;
                            if (attackHitbox != null) attackHitbox.ResetHitTargets();
                        }

                        if (attackActiveTimer <= 0f && attackHitbox != null)
                        {
                            attackHitbox.Deactivate();
                        }
                    }

                    if (attackTimer <= 0f)
                    {
                        if (attackHitbox != null) attackHitbox.Deactivate();
                        TransitionTo(isExhausted ? FighterState.Exhausted : FighterState.Idle);
                    }
                    break;
            }
        }

        private void HandleGroundedTransitions(bool isExhausted)
        {
            // Meditate can be channeled at any time when grounded (even in Red Zone)
            if (inputHandler.MeditateHeld && IsGrounded)
            {
                TransitionTo(FighterState.Meditating);
                return;
            }

            // Jump takes priority (exhausted fighters have weaker jump or regular)
            if (inputHandler.JumpPressed && IsGrounded)
            {
                float effectiveJump = isExhausted ? jumpForce * 0.75f : jumpForce;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, effectiveJump);
                TransitionTo(FighterState.Jumping);
                return;
            }

            // Attacks (allowed only when NOT exhausted)
            if (!isExhausted && IsGrounded)
            {
                // Ultimate Attack (Q / O)
                if (inputHandler.UltimatePressed)
                {
                    float ultCost = 80f;
                    if (energySystem == null || energySystem.CanAfford(ultCost))
                    {
                        if (energySystem != null) energySystem.SpendKi(ultCost);
                        attackTimer = 1.45f;
                        attackStartupTimer = 0.45f;
                        attackActiveTimer = 0.50f;
                        if (attackHitbox != null)
                        {
                            attackHitbox.ConfigureAttack(45, 14f, 15f, new Vector2(1.1f, 1.0f), new Vector2(1.5f, 1.2f));
                        }
                        TransitionTo(FighterState.Ultimate);
                        return;
                    }
                }

                // Skill Attack (E / U)
                if (inputHandler.SkillPressed)
                {
                    float skillCost = 20f;
                    if (energySystem == null || energySystem.CanAfford(skillCost))
                    {
                        if (energySystem != null) energySystem.SpendKi(skillCost);
                        attackTimer = 0.85f;
                        attackStartupTimer = 0.10f;
                        attackActiveTimer = 0.60f;
                        if (attackHitbox != null)
                        {
                            attackHitbox.ConfigureAttack(20, 6f, 8f, new Vector2(1.0f, 0.9f), new Vector2(1.3f, 0.9f));
                        }
                        TransitionTo(FighterState.Skill);
                        return;
                    }
                }

                // Heavy Attack (C / N)
                if (inputHandler.HeavyAttackPressed)
                {
                    attackTimer = 0.88f;
                    attackStartupTimer = 0.18f;
                    attackActiveTimer = 0.35f;
                    if (attackHitbox != null)
                    {
                        attackHitbox.ConfigureAttack(16, 8f, 10f, new Vector2(0.95f, 0.85f), new Vector2(1.1f, 0.75f));
                    }
                    TransitionTo(FighterState.HeavyAttack);
                    return;
                }

                // Light Attack (F / H)
                if (inputHandler.LightAttackPressed)
                {
                    attackTimer = 0.44f;
                    attackStartupTimer = 0.08f;
                    attackActiveTimer = 0.24f;
                    if (attackHitbox != null)
                    {
                        attackHitbox.ConfigureAttack(10, 4.5f, 6f, new Vector2(0.8f, 0.85f), new Vector2(0.85f, 0.65f));
                    }
                    TransitionTo(FighterState.LightAttack);
                    return;
                }
            }

            // Crouch
            if (inputHandler.CrouchHeld && IsGrounded)
            {
                TransitionTo(FighterState.Crouching);
                return;
            }

            // Falling off edge
            if (!IsGrounded)
            {
                TransitionTo(FighterState.Falling);
                return;
            }

            // Ground movement / idle / exhaustion
            if (isExhausted)
            {
                TransitionTo(FighterState.Exhausted);
            }
            else if (Mathf.Abs(inputHandler.MoveInput.x) > 0.01f)
            {
                TransitionTo(FighterState.Walking);
            }
            else
            {
                TransitionTo(FighterState.Idle);
            }
        }

        private void TransitionTo(FighterState newState)
        {
            if (CurrentState == newState) return;

            FighterState oldState = CurrentState;
            CurrentState = newState;

            // --- State-Entry Audio Triggers (State-Driven Architecture) ---
            switch (newState)
            {
                case FighterState.Jumping:
                    fighterAudio?.PlayJump();
                    break;

                case FighterState.Crouching:
                    fighterAudio?.PlayCrouch();
                    break;

                case FighterState.LightAttack:
                    fighterAudio?.PlayLightWhiff();
                    break;

                case FighterState.HeavyAttack:
                    fighterAudio?.PlayHeavyWhiff();
                    break;

                case FighterState.Skill:
                    fighterAudio?.PlaySkillAttack();
                    break;

                case FighterState.Ultimate:
                    fighterAudio?.PlayUltimateAttack();
                    break;

                case FighterState.Meditating:
                    fighterAudio?.StartKiMeditationAura();
                    break;
            }

            // --- Landing Audio Trigger (Airborne to Grounded) ---
            if ((oldState == FighterState.Falling || oldState == FighterState.Jumping) &&
                (newState == FighterState.Idle || newState == FighterState.Walking || newState == FighterState.Exhausted))
            {
                fighterAudio?.PlayLand();
            }

            // --- State-Exit Audio Triggers ---
            if (oldState == FighterState.Meditating && newState != FighterState.Meditating)
            {
                fighterAudio?.StopKiMeditationAura();
            }
        }

        #endregion

        #region Ki Generation & Meditation

        private void HandleKiGeneration()
        {
            if (CurrentState == FighterState.Meditating && energySystem != null)
            {
                // Charge Ki while meditating
                energySystem.AddKi(meditateKiPerSecond * Time.deltaTime);

                // If in Red Zone, meditating accelerates exhaustion recovery
                if (energySystem.IsInRedZone)
                {
                    energySystem.ReduceRedZoneTimer(Time.deltaTime * 1.5f);
                }
            }
        }

        #endregion

        #region Movement

        private void ApplyMovement()
        {
            if (inputHandler == null) return;

            bool isExhausted = energySystem != null && energySystem.IsInRedZone;
            float currentSpeed = moveSpeed * (isExhausted ? redZoneSpeedMultiplier : 1f);

            switch (CurrentState)
            {
                case FighterState.Idle:
                case FighterState.Walking:
                case FighterState.Exhausted:
                case FighterState.Jumping:
                case FighterState.Falling:
                    float horizontalVelocity = inputHandler.MoveInput.x * currentSpeed;
                    rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
                    break;

                case FighterState.Crouching:
                case FighterState.Meditating:
                case FighterState.LightAttack:
                case FighterState.HeavyAttack:
                case FighterState.Ultimate:
                    // Cannot move horizontally while crouching, meditating, or regular attacking
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    break;

                case FighterState.Skill:
                    // Anime forward step into punch flurry!
                    float surgeDir = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
                    rb.linearVelocity = new Vector2(surgeDir * 1.5f, rb.linearVelocity.y);
                    break;
            }
        }

        #endregion

        #region Auto-Facing

        private void AutoFaceOpponent()
        {
            if (opponent == null || spriteRenderer == null) return;

            bool faceLeft = opponent.position.x < transform.position.x;
            spriteRenderer.flipX = faceLeft;

            // Mirror Hitbox X scale so attack reach always extends toward opponent
            if (attackHitbox != null)
            {
                Vector3 currentScale = attackHitbox.transform.localScale;
                attackHitbox.transform.localScale = new Vector3(faceLeft ? -Mathf.Abs(currentScale.x) : Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
            }
        }

        #endregion
    }
}
