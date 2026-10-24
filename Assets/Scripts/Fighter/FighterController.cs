using UnityEngine;
using MysticCombat.Input;
using MysticCombat.Systems;

namespace MysticCombat.Fighter
{
    /// <summary>
    /// Main fighter controller handling movement, jumping, crouching, auto-facing,
    /// meditation, and Red Zone exhaustion speed penalty.
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

            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
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
            CurrentState = newState;
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
                    // Cannot move horizontally while crouching or meditating
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    break;
            }
        }

        #endregion

        #region Auto-Facing

        private void AutoFaceOpponent()
        {
            if (opponent == null || spriteRenderer == null) return;

            if (opponent.position.x > transform.position.x)
            {
                spriteRenderer.flipX = false; // Opponent is to the right — face right
            }
            else if (opponent.position.x < transform.position.x)
            {
                spriteRenderer.flipX = true; // Opponent is to the left — face left
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
