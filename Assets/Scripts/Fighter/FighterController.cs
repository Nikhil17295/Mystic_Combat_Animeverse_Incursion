using UnityEngine;
using MysticCombat.Input;

namespace MysticCombat.Fighter
{
    /// <summary>
    /// Main fighter controller handling movement, jumping, crouching, and auto-facing.
    /// Day 1 prototype — movement only; combat actions will be added later.
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

        #endregion

        #region Cached Components

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D bodyCollider;

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

            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
        }

        private void Update()
        {
            CheckGround();
            HandleStateTransitions();
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
        /// <param name="handler">The PlayerInputHandler to read input from.</param>
        public void SetInputHandler(PlayerInputHandler handler)
        {
            inputHandler = handler;
        }

        /// <summary>
        /// Assigns the ground check transform used for ground detection.
        /// </summary>
        /// <param name="point">A transform positioned at the fighter's feet.</param>
        public void SetGroundCheckPoint(Transform point)
        {
            groundCheckPoint = point;
        }

        /// <summary>
        /// Sets the ground layer mask for ground detection.
        /// </summary>
        /// <param name="mask">The LayerMask representing ground surfaces.</param>
        public void SetGroundLayer(LayerMask mask)
        {
            groundLayer = mask;
        }

        /// <summary>
        /// Applies movement tuning values (e.g., from character JSON data).
        /// </summary>
        /// <param name="speed">Horizontal move speed.</param>
        /// <param name="jump">Vertical jump force.</param>
        /// <param name="gravity">Rigidbody gravity scale.</param>
        public void ApplyStats(float speed, float jump, float gravity)
        {
            moveSpeed = speed;
            jumpForce = jump;
            gravityScale = gravity;
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

            switch (CurrentState)
            {
                case FighterState.Idle:
                case FighterState.Walking:
                    HandleGroundedTransitions();
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
                        TransitionTo(FighterState.Idle);
                    }
                    break;

                case FighterState.Crouching:
                    if (!inputHandler.CrouchHeld || !IsGrounded)
                    {
                        TransitionTo(FighterState.Idle);
                    }
                    break;
            }
        }

        private void HandleGroundedTransitions()
        {
            // Jump takes priority
            if (inputHandler.JumpPressed && IsGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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

            // Walk or idle
            if (Mathf.Abs(inputHandler.MoveInput.x) > 0.01f)
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

        #region Movement

        private void ApplyMovement()
        {
            if (inputHandler == null) return;

            // Only allow horizontal movement in Idle, Walking, Jumping, Falling states
            switch (CurrentState)
            {
                case FighterState.Idle:
                case FighterState.Walking:
                case FighterState.Jumping:
                case FighterState.Falling:
                    float horizontalVelocity = inputHandler.MoveInput.x * moveSpeed;
                    rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
                    break;

                case FighterState.Crouching:
                    // No horizontal movement while crouching
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    break;
            }
        }

        #endregion

        #region Auto-Facing

        private void AutoFaceOpponent()
        {
            if (opponent == null) return;

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
