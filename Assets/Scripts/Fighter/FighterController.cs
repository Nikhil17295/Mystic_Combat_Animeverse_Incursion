using UnityEngine;

using MysticCombat.Input;

using MysticCombat.Systems;

using MysticCombat.Combat;



using MysticCombat.Data;



namespace MysticCombat.Fighter

{

    /// <summary>

    /// Main fighter controller handling movement, jumping, crouching, auto-facing,

    /// meditation, attacks, and Red Zone exhaustion speed penalty.

    /// </summary>

    [RequireComponent(typeof(Rigidbody2D))]

    [RequireComponent(typeof(Collider2D))]

    [RequireComponent(typeof(SpriteRenderer))]

    public class FighterController : MonoBehaviour

    {

        #region Serialized Fields



        [Header("References")]

        [Tooltip("The input handler driving this fighter (if pre-configured in Inspector).")]

        [SerializeField] private PlayerInputHandler legacyInputHandler;

        private IFighterInput inputHandler;



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







        [Header("Skill & Ultimate Cooldowns")]

        [Tooltip("Cooldown duration for special skill attacks (seconds).")]

        [SerializeField] private float skillCooldownDuration = 15f;

        [Tooltip("Cooldown duration for ultimate attacks (seconds).")]

        [SerializeField] private float ultimateCooldownDuration = 15f;

        private float skillCooldownTimer;

        private float ultimateCooldownTimer;



        [Header("Super Saiyan Transformation")]



        [Tooltip("Whether this fighter is currently in Super Saiyan form.")]



        [SerializeField] private bool isSuperSaiyan;







        [Tooltip("Remaining seconds in Super Saiyan transformation.")]



        [SerializeField] private float superSaiyanTimer;







        public bool IsSuperSaiyan => isSuperSaiyan;



        public float SuperSaiyanTimer => superSaiyanTimer;



        
        [Header("Nezuko Demon Form & Chibi State")]
        [Tooltip("Whether Nezuko is currently transformed into her awakened Demon Form.")]
        [SerializeField] private bool isDemonForm;

        public bool IsDemonForm => isDemonForm;
        public bool IsNezuko => (CharacterId == "nezuko") || gameObject.name.ToLower().Contains("nezuko");
        public bool IsSmallChibi => IsNezuko && !isDemonForm;

        [Header("Tsunade Healing Stage (Byakugou)")]
        [Tooltip("Whether Tsunade is currently in her Byakugou Healing Stage (+2 HP/s, +2 Ki/s, 50% damage reduction).")]
        [SerializeField] private bool isHealingStage;
        private float healingStageTimer;

        public bool IsHealingStage => isHealingStage;
        public float HealingStageTimer => healingStageTimer;
        public bool IsTsunade => (CharacterId == "tsunade") || gameObject.name.ToLower().Contains("tsunade");

        public void ActivateHealingStage(float duration = 10.0f)
        {
            isHealingStage = true;
            healingStageTimer = duration;
            Debug.Log($"<color=#10b981>[TSUNADE]</color> Byakugou Healing Stage activated for {duration} seconds!");
        }

        public void SetUltimateCooldown(float cd)
        {
            ultimateCooldownTimer = cd;
        }
#endregion



        #region Cached Components



        private Rigidbody2D rb;

        private SpriteRenderer spriteRenderer;

        private Collider2D bodyCollider;

        private EnergySystem energySystem;
        private HealthSystem healthSystem;

        private float attackTimer;

        private float attackStartupTimer;

        private float attackActiveTimer;

        private BoxCollider2D hurtboxCollider;

        private float flurryHitTimer;

        private int flurryHitsRemaining;

        private bool hasHitTargetThisSwing;

        [SerializeField] private Color baseTintColor = Color.white;



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



        public float AttackStartupTimer => attackStartupTimer;

        public float AttackActiveTimer => attackActiveTimer;

        public bool HasHitTargetThisSwing => hasHitTargetThisSwing;

        public Color BaseTintColor => baseTintColor;



        public float SkillCooldownTimer => skillCooldownTimer;

        public float UltimateCooldownTimer => ultimateCooldownTimer;

        public float SkillCooldownDuration => skillCooldownDuration;

        public float UltimateCooldownDuration => ultimateCooldownDuration;



        /// <summary>

        /// The loaded data profile for this fighter (Data-Driven Architecture).

        /// </summary>

        public CharacterData CharacterData { get; private set; }



        /// <summary>

        /// Unique character identifier (e.g. "goku", "saitama").

        /// </summary>

        public string CharacterId { get; private set; }



        /// <summary>

        /// Dynamic Ki cost for special skill derived from EnergySystem / CharacterData.

        /// </summary>

        public float SkillKiCost => energySystem != null ? energySystem.SkillKiCost : (gameObject.name.ToLower().Contains("goku") ? 0f : 30f);



        /// <summary>

        /// Dynamic Ki cost for ultimate ability derived from EnergySystem / CharacterData.

        /// </summary>

        public float UltimateKiCost => energySystem != null ? energySystem.UltimateKiCost : (gameObject.name.ToLower().Contains("goku") ? 70f : 80f);



        /// <summary>

        /// Whether the fighter is currently invulnerable (e.g. final 1.0s of ultimate casting and active release).

        /// </summary>

        public bool IsInvulnerable => CurrentState == FighterState.Ultimate && attackStartupTimer <= 1.0f;



        /// <summary>

        /// Configures the base palette tint (e.g. Player 2 mirror match alternate color).

        /// </summary>

        public void SetBaseTintColor(Color tint)

        {

            baseTintColor = tint;

            if (spriteRenderer != null)

            {

                spriteRenderer.color = tint;

            }

        }



        /// <summary>

        /// Forces fighter state from an authoritative network snapshot (used by Client).

        /// </summary>

        public void ForceNetworkState(FighterState state)

        {

            CurrentState = state;

        }



        /// <summary>

        /// Sets horizontal facing flip directly from network state.

        /// </summary>

        public void SetFacing(bool flipX)

        {

            if (spriteRenderer != null)

            {

                spriteRenderer.flipX = flipX;

            }

        }



        /// <summary>

        /// Canonical alternate palette color for mirror matches.

        /// </summary>

        public static Color GetAlternatePaletteColor(string characterId)

        {

            if (string.IsNullOrEmpty(characterId)) return new Color(0.45f, 0.75f, 1.0f, 1.0f);

            if (characterId.ToLower().Contains("goku"))
            {
                return new Color(0.45f, 0.75f, 1.0f, 1.0f); // Cool Ice/Cyan Alternate Gi
            }
            else if (characterId.ToLower().Contains("nezuko"))
            {
                return new Color(0.85f, 0.45f, 0.95f, 1.0f); // Mystic Lavender Demon Kimono
            }
            else
            {
                return new Color(1.0f, 0.50f, 0.45f, 1.0f); // Blazing Coral/Crimson Alternate Suit
            }

        }



        #endregion



        #region Unity Callbacks



        private void Awake()

        {

            rb = GetComponent<Rigidbody2D>();

            spriteRenderer = GetComponent<SpriteRenderer>();

            bodyCollider = GetComponent<Collider2D>();

            energySystem = GetComponent<EnergySystem>();
            healthSystem = GetComponent<HealthSystem>();



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



            if (inputHandler == null)

            {

                if (legacyInputHandler != null) inputHandler = legacyInputHandler;

                else inputHandler = GetComponent<IFighterInput>();

            }



            if (hurtboxCollider == null)

            {

                Combat.HurtboxController hb = GetComponentInChildren<Combat.HurtboxController>(true);

                if (hb != null) hurtboxCollider = hb.GetComponent<BoxCollider2D>();

            }



            if (gameObject.name.ToLower().Contains("goku") && GetComponent<Combat.KamehamehaBeamVFX>() == null)

            {

                gameObject.AddComponent<Combat.KamehamehaBeamVFX>();

            }

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

            // Initialize asymmetric character meditation rates if at default

            bool isGokuChar = (CharacterId == "goku") || gameObject.name.ToLower().Contains("goku");

            if (meditateKiPerSecond >= 10f)
            {
                meditateKiPerSecond = isGokuChar ? 8.0f : (IsNezuko ? 7.0f : 5.0f);
            }

            if (IsNezuko)
            {
                ToggleDemonForm(false);
            }

        }



        private void Update()

        {

            if (skillCooldownTimer > 0f) skillCooldownTimer -= Time.deltaTime;

            if (ultimateCooldownTimer > 0f) ultimateCooldownTimer -= Time.deltaTime;



            CheckGround();

            HandleSuperSaiyanTimer();

            HandleNezukoKi();
            HandleTsunadeHealingStage();
            HandleRedZoneVisuals();

            HandleStateTransitions();

            HandleKiGeneration();

        }



        private void FixedUpdate()

        {

            ApplyMovement();

            ApplyKineticPhysics();

        }



        private void LateUpdate()

        {

            AutoFaceOpponent();

        }



        #endregion



        #region Initialization



        /// <summary>

        /// Assigns the input handler (hardware or CPU virtual brain) for this fighter.

        /// </summary>

        public void SetInputHandler(IFighterInput handler)

        {

            inputHandler = handler;

        }



        /// <summary>

        /// Gets the active input handler.

        /// </summary>

        public IFighterInput GetInputHandler() => inputHandler;

        public IFighterInput InputHandler => inputHandler;



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



            if (hurtboxCollider != null)

            {

                hurtboxCollider.size = new Vector2(0.85f, 1.80f);

                hurtboxCollider.offset = new Vector2(0f, 0.95f);

            }



            if (spriteRenderer != null)

            {

                spriteRenderer.flipX = !faceRight;

                spriteRenderer.color = baseTintColor;

            }



            FighterAnimator anim = GetComponent<FighterAnimator>();

            if (anim != null)

            {

                anim.ResetToIdle();

            }

            RevertSuperSaiyan();



            if (IsNezuko)
            {
                ToggleDemonForm(false);
            }
            Debug.Log($"<color=#00e676>[FighterController]</color> {gameObject.name} reset for new round at {spawnPosition} (Facing: {(faceRight ? "Right" : "Left")})");

        }



        /// <summary>

        /// Called by HitboxController when an attack successfully connects with the enemy.

        /// Enables chain-canceling during recovery frames.

        /// </summary>

        public void OnAttackHitConfirmed()

        {

            hasHitTargetThisSwing = true;

        }



        /// <summary>

        /// Applies movement and Ki tuning values (from character JSON data).

        /// </summary>

        /// <summary>

        /// Puts the fighter into HitStun / Hurt reaction state or Knockout if defeated.

        /// Accounts for Red Zone exhaustion (+0.12s longer hitstun vulnerability).

        /// </summary>

        public void ApplyHit(int damage, float stunDuration = 0.35f)

        {

            if (CurrentState == FighterState.Knockout) return;

            // Super Armor / Uninterruptible: Fighters cannot be knocked into HitStun during Ultimate
            if (CurrentState == FighterState.Ultimate || IsInvulnerable) return;



            HealthSystem health = GetComponent<HealthSystem>();

            if (health != null && health.CurrentHealth <= 0)

            {

                fighterAudio?.PlayVoiceHurt();

                TransitionTo(FighterState.Knockout);

                return;

            }



            // Red Zone Exhaustion Vulnerability: Stun duration extended by +0.12s

            if (energySystem != null && energySystem.IsInRedZone)

            {

                stunDuration += 0.12f;

            }



            attackTimer = stunDuration;

            fighterAudio?.PlayVoiceHurt();

            TransitionTo(FighterState.HitStun);

        }



        /// <summary>

        /// Binds full CharacterData directly to this fighter, initializing movement stats and identity.

        /// </summary>

        public void ApplyCharacterData(CharacterData data)

        {

            if (data == null) return;

            CharacterData = data;

            CharacterId = data.characterId;

            ApplyStats(

                data.moveSpeed, 

                data.jumpForce, 

                data.gravityScale, 

                data.meditateKiPerSecond > 0f ? data.meditateKiPerSecond : (CharacterId == "goku" ? 8.0f : 5.0f)

            );

        }



        public void ApplyStats(float speed, float jump, float gravity, float meditateRate = 8f, float redZoneSpeedMult = 0.35f)

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

                    if (!inputHandler.MeditateHeld || !IsGrounded || isSuperSaiyan)

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

                    attackTimer -= Time.deltaTime;



                    if (attackStartupTimer > 0f)

                    {

                        attackStartupTimer -= Time.deltaTime;

                        if (attackStartupTimer <= 0f && attackHitbox != null)

                        {

                            attackHitbox.Activate();

                        }

                    }

                    else if (attackActiveTimer > 0f)

                    {

                        attackActiveTimer -= Time.deltaTime;

                        if (attackActiveTimer <= 0f && attackHitbox != null)

                        {

                            attackHitbox.Deactivate();

                        }

                    }



                    // Chain-Canceling: If Light Attack lands on opponent, can chain-cancel into Heavy or Skill!

                    if (hasHitTargetThisSwing && !isExhausted)

                    {

                        if (inputHandler.HeavyAttackPressed)

                        {

                            ExecuteHeavyAttack();

                            break;

                        }

                        else if (inputHandler.SkillPressed)

                        {

                            if (ExecuteSkillAttack()) break;

                        }

                    }



                    if (attackTimer <= 0f)

                    {

                        if (attackHitbox != null) attackHitbox.Deactivate();

                        TransitionTo(isExhausted ? FighterState.Exhausted : FighterState.Idle);

                    }

                    break;



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

                            if (IsTsunade)

                            {

                                attackHitbox.Activate();

                                // Devastating Earth Shaker Ground Impact VFX!

                                if (Combat.VFXManager.Instance != null)

                                {

                                    float facing = transform.localScale.x > 0 ? 1f : -1f;

                                    Vector3 impactPos = transform.position + new Vector3(facing * 1.05f, -0.6f, 0f);

                                    Combat.VFXManager.Instance.PlayShockwave(impactPos);

                                    Combat.VFXManager.Instance.PlayDustPuff(impactPos);

                                    Combat.VFXManager.Instance.PlayDustPuff(impactPos + new Vector3(facing * 0.4f, 0f, 0f));

                                    Combat.VFXManager.Instance.PlayDustPuff(transform.position);

                                    Combat.VFXManager.Instance.TriggerCameraShake(0.65f, 0.40f);

                                    Combat.VFXManager.Instance.TriggerImpactFlash(0.18f);

                                }

                                fighterAudio?.PlayHeavyHit();

                            }

                            else if (flurryHitsRemaining > 0)

                            {

                                attackHitbox.Activate();

                                flurryHitTimer = 0.35f;

                            }

                        }

                    }

                    else if (attackActiveTimer > 0f)

                    {

                        attackActiveTimer -= Time.deltaTime;

                        if (flurryHitsRemaining > 0)

                        {

                            flurryHitTimer -= Time.deltaTime;

                            if (flurryHitTimer <= 0f)

                            {

                                flurryHitTimer = 0.35f;

                                flurryHitsRemaining--;

                                if (attackHitbox != null)

                                {

                                    if (flurryHitsRemaining > 0)

                                    {

                                        attackHitbox.ResetHitTargets();

                                    }

                                    else

                                    {

                                        attackHitbox.Deactivate();

                                    }

                                }

                            }

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



                case FighterState.HitStun:

                case FighterState.KnockBack:

                    attackTimer -= Time.deltaTime;

                    if (attackTimer <= 0f)

                    {

                        TransitionTo(isExhausted ? FighterState.Exhausted : FighterState.Idle);

                    }

                    break;



                case FighterState.Knockout:

                case FighterState.Victory:

                    // Stays locked in Knockout or Victory pose until round resets!

                    break;

            }

        }



        private void HandleGroundedTransitions(bool isExhausted)

        {

            // Meditate can be channeled at any time when grounded (even in Red Zone)

            bool cannotMeditate = isSuperSaiyan || (IsNezuko && isDemonForm);
            if (inputHandler.MeditateHeld && IsGrounded && !cannotMeditate)

            {

                TransitionTo(FighterState.Meditating);

                return;

            }



            // Jump takes priority (exhausted fighters have weaker jump or regular)

            if (inputHandler.JumpPressed && IsGrounded)

            {

                float effectiveJump = isExhausted ? jumpForce * 0.75f : (IsSmallChibi ? jumpForce * 1.05f : jumpForce);

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, effectiveJump);

                Combat.VFXManager.Instance?.PlayDustPuff(transform.position);

                TransitionTo(FighterState.Jumping);

                return;

            }



            // Attacks (allowed only when NOT exhausted)

            if (!isExhausted && IsGrounded)

            {

                if (inputHandler.UltimatePressed && ExecuteUltimateAttack())

                {

                    return;

                }



                if (inputHandler.SkillPressed && ExecuteSkillAttack())

                {

                    return;

                }



                if (inputHandler.HeavyAttackPressed)

                {

                    ExecuteHeavyAttack();

                    return;

                }



                if (inputHandler.LightAttackPressed)

                {

                    ExecuteLightAttack();

                    return;

                }

            }



            // Crouch

            if (inputHandler.CrouchHeld && IsGrounded)
            {
                if (!IsSmallChibi)
                {
                    TransitionTo(FighterState.Crouching);
                    return;
                }
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



        public void TransitionTo(FighterState newState)

        {

            if (CurrentState == newState) return;



            FighterState oldState = CurrentState;

            CurrentState = newState;

            hasHitTargetThisSwing = false;



            // Attack Interruption & Cleanup: Deactivate hitboxes if taking a hit or leaving attack states

            if (attackHitbox != null && (newState == FighterState.HitStun || newState == FighterState.KnockBack || newState == FighterState.Knockout || (IsAttackState(oldState) && !IsAttackState(newState))))

            {

                attackHitbox.Deactivate();

            }



            // Voice Cancellation on Interruption: Immediately cut off active voice lines if interrupted or KO'd

            if (newState == FighterState.HitStun || newState == FighterState.KnockBack || newState == FighterState.Knockout)

            {

                fighterAudio?.StopVoice();

            }



            // Dynamic Hurtbox Height: Ducking lowers vulnerable body area under high & heavy attacks!

            // (Physical body collider is NEVER resized at runtime to prevent Box2D depenetration impulses and infinite jumping)

            if (hurtboxCollider != null)

            {

                if (newState == FighterState.Crouching)

                {

                    hurtboxCollider.size = new Vector2(0.75f, 0.65f);

                    hurtboxCollider.offset = new Vector2(0f, 0.35f);

                }

                else if (oldState == FighterState.Crouching)
                {
                    if (IsSmallChibi)
                    {
                        hurtboxCollider.size = new Vector2(0.70f, 0.60f);
                        hurtboxCollider.offset = new Vector2(0f, 0.30f);
                    }
                    else
                    {
                        hurtboxCollider.size = new Vector2(0.85f, 1.80f);
                        hurtboxCollider.offset = new Vector2(0f, 0.95f);
                    }
                }

            }



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



                case FighterState.Victory:

                    if (gameObject.name.ToLower().Contains("saitama"))

                    {

                        fighterAudio?.PlayJump();

                    }

                    break;

            }



            // --- Landing Audio & Dust VFX Trigger (Airborne to Grounded) ---

            if ((oldState == FighterState.Falling || oldState == FighterState.Jumping) &&

                (newState == FighterState.Idle || newState == FighterState.Walking || newState == FighterState.Exhausted))

            {

                fighterAudio?.PlayLand();

                Combat.VFXManager.Instance?.PlayDustPuff(transform.position);

            }



            // --- State-Exit Audio Triggers ---

            if (oldState == FighterState.Meditating && newState != FighterState.Meditating)

            {

                fighterAudio?.StopKiMeditationAura();

            }

        }



        private bool IsAttackState(FighterState state)

        {

            return state == FighterState.LightAttack ||

                   state == FighterState.HeavyAttack ||

                   state == FighterState.Skill ||

                   state == FighterState.Ultimate;

        }



        #endregion



        #region Ki Generation & Meditation



        #region Tsunade Healing Stage & Mechanics

        private void HandleTsunadeHealingStage()
        {
            if (!isHealingStage) return;

            healingStageTimer -= Time.deltaTime;

            // +2 HP/s regeneration
            if (healthSystem != null)
            {
                healthSystem.Heal(2.0f * Time.deltaTime);
            }

            // +2 Ki/s generation
            if (energySystem != null)
            {
                energySystem.AddKi(2.0f * Time.deltaTime);
            }

            if (healingStageTimer <= 0f)
            {
                isHealingStage = false;
                Debug.Log("<color=#10b981>[TSUNADE]</color> Byakugou Healing Stage expired.");
            }
        }

        #endregion

        #region Nezuko Demon Form & Chibi Mechanics

        private void HandleNezukoKi()
        {
            if (!IsNezuko || energySystem == null) return;

            if (IsSmallChibi)
            {
                // User requirement: "in this state she will gain 1 ki/s , even if she do nothing"
                energySystem.AddKi(1.0f * Time.deltaTime);
            }
            else if (isDemonForm)
            {
                // User requirement: "she will spend 2 ki/s"
                energySystem.SpendKi(2.0f * Time.deltaTime);
                if (energySystem.CurrentKi <= 0f)
                {
                    ToggleDemonForm(false);
                }
            }
        }

        public void ToggleDemonForm(bool activate)
        {
            isDemonForm = activate;
            if (isDemonForm)
            {
                // Full scale Awakened Demon Form (100% normal fighter scale)
                transform.localScale = new Vector3(1.0f, 1.0f, 1f);

                fighterAudio?.PlaySkillAttack();
                Combat.VFXManager.Instance?.TriggerImpactFlash(0.08f);

                if (hurtboxCollider != null)
                {
                    hurtboxCollider.size = new Vector2(0.85f, 1.80f);
                    hurtboxCollider.offset = new Vector2(0f, 0.95f);
                }
                if (bodyCollider is CapsuleCollider2D cap)
                {
                    cap.size = new Vector2(0.70f, 1.60f);
                    cap.offset = new Vector2(0f, 0.80f);
                }
                Debug.Log($"<color=#f43f5e>[FighterController]</color> {gameObject.name} AWAKENED DEMON FORM! Scale: 100%, Consuming 2 Ki/s.");
            }
            else
            {
                // User requirement: "her small form , it should be visibly smaller too , like make her about 60% of the size she currently is , in her small form"
                transform.localScale = new Vector3(0.60f, 0.60f, 1f);

                if (hurtboxCollider != null)
                {
                    hurtboxCollider.size = new Vector2(0.85f, 1.00f);
                    hurtboxCollider.offset = new Vector2(0f, 0.50f);
                }
                if (bodyCollider is CapsuleCollider2D cap)
                {
                    cap.size = new Vector2(0.70f, 1.00f);
                    cap.offset = new Vector2(0f, 0.50f);
                }
                Debug.Log($"<color=#38bdf8>[FighterController]</color> {gameObject.name} returned to Small Chibi Form (Visibly 60% Scale, +1 Ki/s passive).");
            }
        }

        #endregion

        #region Super Saiyan Transformation







        public void ActivateSuperSaiyan(float duration = 0f)

        {

            isSuperSaiyan = true;

            superSaiyanTimer = duration;

            if (CurrentState == FighterState.Meditating) TransitionTo(FighterState.Idle);



            fighterAudio?.StartKiMeditationAura();



            Debug.Log($"<color=#fbbf24>[FighterController] {gameObject.name} ASCENDED TO SUPER SAIYAN! Consuming 2.5 Ki/s.</color>");

        }



        public void RevertSuperSaiyan()

        {

            if (!isSuperSaiyan) return;



            isSuperSaiyan = false;

            superSaiyanTimer = 0f;

            skillCooldownTimer = skillCooldownDuration; // 15s Skill cooldown begins upon reverting



            fighterAudio?.StopKiMeditationAura();



            Debug.Log($"[FighterController] {gameObject.name} reverted to Base form. 15s Skill cooldown started.");

        }



        private void HandleSuperSaiyanTimer()

        {

            if (!isSuperSaiyan) return;



            // Drains 2.5 Ki/sec while in Super Saiyan form

            if (energySystem != null)

            {

                energySystem.SpendKi(2.5f * Time.deltaTime);

                if (energySystem.CurrentKi <= 0f)

                {

                    RevertSuperSaiyan();

                }

            }

        }







        #endregion







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

            float speedMult = isSuperSaiyan ? 1.25f : (IsSmallChibi ? 1.18f : 1.0f);
            float currentSpeed = moveSpeed * (isExhausted ? redZoneSpeedMultiplier : speedMult);



            switch (CurrentState)

            {

                case FighterState.Idle:

                case FighterState.Walking:

                case FighterState.Exhausted:

                case FighterState.Jumping:

                case FighterState.Falling:

                    float horizontalVelocity = inputHandler.MoveInput.x * currentSpeed;



                    // Proximity Buffer & Push Prevention:

                    // When GROUNDED, moving towards the opponent, and already within the sparring gap (1.15m), lock forward movement!

                    // (Airborne jumpers are never clamped so they can freely jump over each other!)

                    if (IsGrounded && opponent != null)

                    {

                        float dx = opponent.position.x - transform.position.x;

                        float moveX = inputHandler.MoveInput.x;

                        bool movingTowardsOpponent = (dx > 0f && moveX > 0f) || (dx < 0f && moveX < 0f);

                        if (movingTowardsOpponent && Mathf.Abs(dx) <= 1.15f)

                        {

                            horizontalVelocity = 0f;

                        }

                    }



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

                    // Anime forward step into punch flurry! (Saitama steady flurry advance tuned to 0.60f)

                    float surgeDir = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;

                    float surgeSpeed = gameObject.name.ToLower().Contains("goku") ? 0.0f : 0.60f;



                    // Halt flurry forward surge if already touching opponent

                    if (opponent != null)

                    {

                        float fdx = opponent.position.x - transform.position.x;

                        bool surgingTowards = (fdx > 0f && surgeDir > 0f) || (fdx < 0f && surgeDir < 0f);

                        if (surgingTowards && Mathf.Abs(fdx) <= 1.15f)

                        {

                            surgeSpeed = 0f;

                        }

                    }



                    rb.linearVelocity = new Vector2(surgeDir * surgeSpeed, rb.linearVelocity.y);

                    break;

            }

        }



        #endregion



        #region Attack Execution Helpers



        private void ExecuteLightAttack()
        {
            if (IsTsunade)
            {
                // Tsunade: Light flick on the head (normal damage, 5 per hit)
                int tsunadeDmg = 5;
                attackTimer = 0.35f;
                attackStartupTimer = 0.06f;
                attackActiveTimer = 0.18f;
                if (attackHitbox != null)
                {
                    Vector2 offset = new Vector2(0.85f, 0.95f);
                    Vector2 size = new Vector2(0.85f, 0.50f);
                    attackHitbox.ConfigureAttack(tsunadeDmg, 2.2f, 4.0f, offset, size, 0.5f, attackType: AttackType.Light, requiresGrounded: false);
                }
                TransitionTo(FighterState.LightAttack);
                return;
            }
            bool isGoku = gameObject.name.ToLower().Contains("goku");
            int damage = 4;
            if (isGoku)
            {
                damage = isSuperSaiyan ? 5 : 3;
            }
            else if (IsNezuko)
            {
                // In Demon form: 5 dmg; in Small Chibi form: 0 dmg (interrupts only)
                damage = isDemonForm ? 5 : 0;
            }
            float knockbackX = isGoku ? 2.0f : (IsNezuko ? 1.8f : 2.2f);

            attackTimer = 0.38f;
            attackStartupTimer = 0.08f;
            attackActiveTimer = 0.20f;
            if (attackHitbox != null)
            {
                float kiGain = isGoku ? 4.0f : (IsNezuko ? 3.5f : 2.5f);
                Vector2 offset = IsSmallChibi ? new Vector2(0.85f, 0.60f) : new Vector2(0.80f, 1.05f);
                Vector2 size = IsSmallChibi ? new Vector2(0.85f, 0.55f) : new Vector2(0.75f, 0.45f);
                attackHitbox.ConfigureAttack(damage, knockbackX, kiGain, offset, size, 0.5f, attackType: AttackType.Light);
            }

            // User requirement: Nezuko voice lines only on ultimate and attacking when she is in her demon form
            if (IsNezuko && isDemonForm)
            {
                fighterAudio?.PlayVoiceAttackGrunt();
            }

            TransitionTo(FighterState.LightAttack);
        }



        private void ExecuteHeavyAttack()
        {
            if (IsTsunade)
            {
                // Tsunade: Foot stomp on the ground (10 damage to grounded opponent!)
                int tsunadeDmg = 10;
                attackTimer = 0.70f;
                attackStartupTimer = 0.18f;
                attackActiveTimer = 0.30f;
                if (attackHitbox != null)
                {
                    Vector2 offset = new Vector2(0.90f, 0.40f);
                    Vector2 size = new Vector2(1.10f, 0.70f);
                    attackHitbox.ConfigureAttack(tsunadeDmg, 5.5f, 7.0f, offset, size, 0.5f, attackType: AttackType.Heavy, requiresGrounded: true);
                }
                TransitionTo(FighterState.HeavyAttack);
                return;
            }
            bool isGoku = gameObject.name.ToLower().Contains("goku");
            int damage = 9;
            if (isGoku)
            {
                damage = isSuperSaiyan ? 10 : 7;
            }
            else if (IsNezuko)
            {
                // In Demon form: 10 dmg; in Small Chibi form: 0 dmg (interrupts only)
                damage = isDemonForm ? 10 : 0;
            }
            float knockbackX = isGoku ? 5.0f : (IsNezuko ? 4.5f : 5.5f);

            attackTimer = 0.75f;
            attackStartupTimer = 0.18f;
            attackActiveTimer = 0.32f;
            if (attackHitbox != null)
            {
                float kiGain = isGoku ? 7.0f : (IsNezuko ? 5.5f : 5.0f);
                Vector2 offset = IsSmallChibi ? new Vector2(0.90f, 0.65f) : new Vector2(0.84f, 1.05f);
                Vector2 size = IsSmallChibi ? new Vector2(0.90f, 0.60f) : new Vector2(0.82f, 0.48f);
                attackHitbox.ConfigureAttack(damage, knockbackX, kiGain, offset, size, 1.8f, attackType: AttackType.Heavy);
            }

            // User requirement: Nezuko voice lines only on ultimate and attacking when she is in her demon form
            if (IsNezuko && isDemonForm)
            {
                fighterAudio?.PlayVoiceAttackGrunt();
            }

            TransitionTo(FighterState.HeavyAttack);
        }



        private bool ExecuteSkillAttack()

        {

            if (skillCooldownTimer > 0f) return false;



                        if (IsTsunade)
            {
                // Tsunade Skill: Devastating Ground Breaker (30 damage to grounded opponent, 30 Ki cost, 20s cooldown)
                float cost = 30f;
                if (energySystem != null && !energySystem.CanAfford(cost)) return false;

                if (energySystem != null) energySystem.SpendKi(cost);
                skillCooldownTimer = 20.0f; // 20-second cooldown per user instruction

                attackTimer = 1.05f;
                attackStartupTimer = 0.35f; // Windup matching ground slam animation
                attackActiveTimer = 0.45f;

                if (attackHitbox != null)
                {
                    Vector2 offset = new Vector2(1.05f, 0.35f);
                    Vector2 size = new Vector2(1.40f, 0.90f);
                    int damage = 30; // 30 damage to grounded opponent
                    attackHitbox.ConfigureAttack(damage, 7.0f, 0f, offset, size, 2.5f, hitStop: true, shakeMult: 2.2f, attackType: AttackType.Skill, requiresGrounded: true);
                }

                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayDustPuff(transform.position);
                }

                TransitionTo(FighterState.Skill);
                return true;
            }

if (IsNezuko)
            {
                // Nezuko Skill: Active toggle transforming between Small Chibi and Demon Form
                ToggleDemonForm(!isDemonForm);
                if (attackHitbox != null) attackHitbox.Deactivate();
                attackTimer = 0.25f;
                attackStartupTimer = 0.0f;
                attackActiveTimer = 0.0f;
                flurryHitsRemaining = 0;
                TransitionTo(FighterState.Skill);
                return true;
            }

            bool isGoku = gameObject.name.ToLower().Contains("goku");

            if (isGoku)

            {

                // Goku Skill: Active Toggle between Base and Super Saiyan (drains 2.5 Ki/s)

                if (!isSuperSaiyan)

                {

                    if (energySystem == null || energySystem.CurrentKi < 2.5f) return false;

                    ActivateSuperSaiyan();

                }

                else

                {

                    RevertSuperSaiyan();

                }



                if (attackHitbox != null) attackHitbox.Deactivate();

                attackTimer = 0.0f;

                attackStartupTimer = 0.0f;

                attackActiveTimer = 0.0f;

                flurryHitsRemaining = 0;

                TransitionTo(FighterState.Idle);

                return true;

            }



            // Saitama Skill (Consecutive Normal Punches - 5 hits @ 6 damage = 30 total damage, 30 Ki cost)

            float skillCost = SkillKiCost;

            if (energySystem != null && !energySystem.CanAfford(skillCost)) return false;



            if (energySystem != null && skillCost > 0f) energySystem.SpendKi(skillCost);

            skillCooldownTimer = skillCooldownDuration; // 15-second cooldown



            attackTimer = 2.00f;

            attackStartupTimer = 0.15f;

            attackActiveTimer = 1.80f;

            flurryHitsRemaining = 5;

            flurryHitTimer = 0.35f; // Spaced punches give opponent windows to counter-hit and interrupt!

            if (attackHitbox != null)

            {

                Vector2 offset = new Vector2(1.05f, 0.90f);

                Vector2 size = new Vector2(1.4f, 0.90f);

                int damage = 4; // 4 damage per punch x 5 hits = 20 damage total (rebalanced from 6)

                float knockbackX = 0.35f;

                float knockbackY = 0.0f;

                attackHitbox.ConfigureAttack(damage, knockbackX, 0.5f, offset, size, knockbackY, hitStop: false, shakeMult: 0.20f);

            }

            TransitionTo(FighterState.Skill);

            return true;

        }



        private bool ExecuteUltimateAttack()

        {

            if (ultimateCooldownTimer > 0f) return false;



                        if (IsTsunade)
            {
                // Tsunade Ultimate: Katsuyu Summon & Creation Rebirth (60 Ki cost, 20s cooldown)
                if (energySystem != null && !energySystem.CanAfford(60f)) return false;

                if (energySystem != null) energySystem.SpendKi(60f);
                ultimateCooldownTimer = 20.0f; // 20-second cooldown

                attackTimer = 1.10f;
                attackStartupTimer = 0.35f;
                attackActiveTimer = 0.65f;

                Combat.TsunadeKatsuyuCinematic katsuyu = GetComponent<Combat.TsunadeKatsuyuCinematic>();
                if (katsuyu != null)
                {
                    katsuyu.TriggerKatsuyuSummon(opponent);
                }

                TransitionTo(FighterState.Ultimate);
                return true;
            }

if (IsNezuko)
            {
                // User: "she can't use ulimate in his form"
                if (IsSmallChibi) return false;

                // User: "consume about 60 ki ... short windup , ddeals 30 damage and is a sure hit , and has a 10 sec cooldown"
                if (energySystem != null && !energySystem.CanAfford(60f)) return false;

                if (energySystem != null) energySystem.SpendKi(60f);
                ultimateCooldownTimer = 10f; // 10-second cooldown

                attackTimer = 1.0f;
                attackStartupTimer = 0.35f;
                attackActiveTimer = 0.65f;

                NezukoBloodDemonArtCinematic bda = GetComponent<NezukoBloodDemonArtCinematic>();
                if (bda != null)
                {
                    bda.TriggerBloodDemonArt(opponent);
                }

                TransitionTo(FighterState.Ultimate);
                return true;
            }

            bool isGoku = (CharacterId == "goku") || gameObject.name.ToLower().Contains("goku");

            float ultCost = UltimateKiCost;

            if (energySystem != null && !energySystem.CanAfford(ultCost)) return false;



            if (energySystem != null) energySystem.SpendKi(ultCost);

            ultimateCooldownTimer = ultimateCooldownDuration; // 15-second cooldown



            if (!isGoku)

            {

                // Saitama Serious Punch: Synced to voice audio (3.80s total)

                attackTimer = 3.80f;

                attackStartupTimer = 2.40f; // Chilling calm delivery: "Death... Serious Series..."

                attackActiveTimer = 1.40f;  // Devastating punch on "SERIOUS PUNCH!" (90 heavy decisive damage)

                if (attackHitbox != null)

                {

                    attackHitbox.ConfigureAttack(90, 12f, 20f, new Vector2(1.25f, 0.95f), new Vector2(1.4f, 1.0f), 3.0f);

                }

                Combat.VFXManager.Instance?.TriggerCameraShake(0.35f, 0.30f);

                Combat.VFXManager.Instance?.TriggerImpactFlash(0.04f);

            }

            else

            {

                // Goku Kamehameha: Synced to voice audio (3.36s total, 45 beam damage)

                attackTimer = 3.36f;

                attackStartupTimer = 2.15f; // Charging ki in cupped hands: "KA... ME... HA... ME..."

                attackActiveTimer = 1.21f;  // Blasting beam on "HA-A-A-A!"

                if (attackHitbox != null)

                {

                    attackHitbox.ConfigureAttack(45, 8.5f, 15f, new Vector2(4.6f, 0.95f), new Vector2(8.5f, 1.6f), 2.2f);

                }

            }

            TransitionTo(FighterState.Ultimate);

            return true;

        }



        #endregion



        #region Kinetic Physics & Visuals



        private void HandleRedZoneVisuals()

        {

            if (spriteRenderer == null) return;



            if (energySystem != null && energySystem.IsInRedZone)

            {

                // Visual exhaustion pulse: throb between base tint and danger amber-red

                float pulse = Mathf.PingPong(Time.time * 5f, 1f);

                Color dangerColor = new Color(1f, 0.40f, 0.40f, 1f);

                spriteRenderer.color = Color.Lerp(baseTintColor, dangerColor, pulse);

            }

            else if (spriteRenderer.color != baseTintColor)

            {

                spriteRenderer.color = baseTintColor;

            }

        }



        private void ApplyKineticPhysics()

        {

            if (rb == null) return;



            // 1. Grounded knockback friction / deceleration: prevents endless sliding on hits

            if ((CurrentState == FighterState.HitStun || CurrentState == FighterState.KnockBack) && IsGrounded)

            {

                float decayedVx = Mathf.MoveTowards(rb.linearVelocity.x, 0f, 14f * Time.fixedDeltaTime);

                rb.linearVelocity = new Vector2(decayedVx, rb.linearVelocity.y);

            }



            // 2. Snappy fall gravity: 1.35x heavier gravity during downward fall arc for authentic fighting game weight

            if (!IsGrounded && rb.linearVelocity.y < 0f)

            {

                rb.gravityScale = gravityScale * 1.35f;

            }

            else

            {

                rb.gravityScale = gravityScale;

            }



            // 3. Anti-Stacking Slip Vector: Prevents balancing or resting on the opponent's head

            if (!IsGrounded && opponent != null)

            {

                float dx = transform.position.x - opponent.position.x;

                float dy = transform.position.y - opponent.position.y;

                // Only if directly centered over opponent's dome and descending

                if (dy > 0.40f && dy < 1.05f && Mathf.Abs(dx) < 0.45f && rb.linearVelocity.y <= 0f)

                {

                    float slipDir = Mathf.Abs(rb.linearVelocity.x) > 0.1f ? Mathf.Sign(rb.linearVelocity.x) : (dx >= 0f ? 1f : -1f);

                    rb.linearVelocity = new Vector2(slipDir * 2.5f, rb.linearVelocity.y);

                }

            }

        }



        private void OnCollisionEnter2D(Collision2D collision)

        {

            if (collision.gameObject.name.ToLower().Contains("wall"))

            {

                // High-velocity impact against boundary wall triggers impact crunch dust & micro shake

                if (rb != null && (Mathf.Abs(rb.linearVelocity.x) > 3.0f || Mathf.Abs(collision.relativeVelocity.x) > 3.0f))

                {

                    Vector2 contactPt = collision.contacts.Length > 0 ? collision.contacts[0].point : (Vector2)transform.position;

                    Combat.VFXManager.Instance?.PlayDustPuff(contactPt);

                    Combat.VFXManager.Instance?.TriggerCameraShake(0.18f, 0.15f);

                }

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

