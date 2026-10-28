fc_path = r"Assets\Scripts\Fighter\FighterController.cs"
with open(fc_path, "r", encoding="utf-8") as f:
    fc = f.read()

# 1. State machine cases
t1 = """                    if (attackTimer <= 0f)
                    {
                        if (attackHitbox != null) attackHitbox.Deactivate();
                        TransitionTo(isExhausted ? FighterState.Exhausted : FighterState.Idle);
                    }
                    break;
            }"""

r1 = """                    if (attackTimer <= 0f)
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
            }"""

# 2. Add ApplyHit method
t2 = """        public void ApplyStats(float speed, float jump, float gravity, float meditateRate = 15f, float redZoneSpeedMult = 0.35f)"""
r2 = """        /// <summary>
        /// Puts the fighter into HitStun / Hurt reaction state or Knockout if defeated.
        /// </summary>
        public void ApplyHit(int damage, float stunDuration = 0.35f)
        {
            if (CurrentState == FighterState.Knockout) return;

            HealthSystem health = GetComponent<HealthSystem>();
            if (health != null && health.CurrentHealth <= 0)
            {
                TransitionTo(FighterState.Knockout);
                return;
            }

            attackTimer = stunDuration;
            TransitionTo(FighterState.HitStun);
        }

        public void ApplyStats(float speed, float jump, float gravity, float meditateRate = 15f, float redZoneSpeedMult = 0.35f)"""

# 3. Landing dust puff
t3 = """            // --- Landing Audio Trigger (Airborne to Grounded) ---
            if ((oldState == FighterState.Falling || oldState == FighterState.Jumping) &&
                (newState == FighterState.Idle || newState == FighterState.Walking || newState == FighterState.Exhausted))
            {
                fighterAudio?.PlayLand();
            }"""

r3 = """            // --- Landing Audio & Dust VFX Trigger (Airborne to Grounded) ---
            if ((oldState == FighterState.Falling || oldState == FighterState.Jumping) &&
                (newState == FighterState.Idle || newState == FighterState.Walking || newState == FighterState.Exhausted))
            {
                fighterAudio?.PlayLand();
                Combat.VFXManager.Instance?.PlayDustPuff(transform.position);
            }"""

# 4. Jump takeoff dust puff
t4 = """                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    TransitionTo(FighterState.Jumping);"""

r4 = """                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    Combat.VFXManager.Instance?.PlayDustPuff(transform.position);
                    TransitionTo(FighterState.Jumping);"""

for idx, (t, r) in enumerate([(t1, r1), (t2, r2), (t3, r3), (t4, r4)], 1):
    if t in fc:
        fc = fc.replace(t, r, 1)
        print(f"FC Target {idx} replaced!")
    else:
        print(f"FC Target {idx} NOT found!")

with open(fc_path, "w", encoding="utf-8") as f:
    f.write(fc)
print("FighterController.cs updated with Hitstun, Knockout, and Dust triggers!")
