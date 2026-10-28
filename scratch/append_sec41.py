import os

path = r"Docs\01_UNITY_FOR_BEGINNERS.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

section41 = """
---

## 41. Single-Turn Animation Pacing, Sprite Jumps & Particle Hygiene (Godot vs Unity)

### The Mistake: Looping Non-Loopable Celebrations
In fighting games, characters perform celebratory gestures upon winning a round (e.g., Goku's thumbs-up, Saitama's celebratory jump and fist pump).
A common rookie mistake is setting the animator to `PlayLoop` during a fixed duration hold (e.g., 4.0 seconds):
1. **The Pogo-Stick Trap:** If an animation contains a jump (squat $\\to$ leap $\\to$ land $\\to$ pose), running it in a loop causes the character to continuously jump, land, jump, land over and over like a pogo stick.
2. **The Stagnant Hold:** If the animation is frozen or looped endlessly, the pacing feels sluggish as players wait around doing nothing before the next round begins.

### The Solution: Single-Turn Animation Completion
1. **PlayOnce Architecture:** Set victory animations to `PlayOnce`. The sequence plays from frame $0$ through $N-1$, holding cleanly on the final triumphant frame.
2. **Dynamic Duration Derivation:** Derive the exact round-end transition duration dynamically:
   $$\\text{VictoryDuration} = \\frac{\\text{FrameCount}}{\\text{FPS}}$$
3. **Instant Next-Round Transition:** As soon as the animation completes its single turn, `MatchManager` immediately starts the next round. The flow is crisp, dynamic, and arcade-authentic.

### Sprite-Based Jump Offsets vs Physics Leaps
When a character jumps in a post-round celebration:
- **Rookie Approach (Physical Rigidbody Impulse):** Applying physics velocity (`rb.linearVelocity.y = 8f`) can cause the winner to land on top of the fallen opponent's collider, slide awkwardly down slopes, or jitter against boundaries.
- **Pro Approach (Mathematical Canvas Anchoring):** Anchor the sprite frames relative to the ground baseline ($Y = 135$ on a $160 \\times 160\\text{px}$ canvas):
  - **Squat windup:** Feet at $Y = 135$ (solid ground).
  - **Takeoff:** Feet launch to $Y = 120$ ($15\\text{px}$ above ground).
  - **Mid-air Apex:** Feet peak at $Y = 104$ ($31\\text{px}$ airborne) while the arm punches the sky.
  - **Landing & Pose:** Feet touch down at $Y = 135$ (ground impact) and hold.
  - *Result:* Zero physics collision bugs, perfect deterministic timing, and flawless landing.

### Particle Hygiene: When Pure Sprite Cocoon Trumps Particle Systems
Many beginners think adding more `ParticleSystem` emitters makes every effect look better. In retro pixel-art games:
- Particle emitters often emit blurred, unscaled particles that clash with razor-sharp pixel grids.
- Speed streaks floating around a fighter's body can obscure character silhouettes and readability.
- A well-crafted oscillating sprite cocoon (`dbz_spiky_aura.png`) with sine/cosine scale flickering and alpha ping-ponging produces authentic anime fire while keeping the scene visually clean and performant.

> **Godot Comparison:**
> In **Godot**, you achieve single-turn celebration pacing using `AnimatedSprite2D` or `AnimationPlayer`:
> ```gdscript
> # Play victory animation once without looping
> $AnimatedSprite2D.play("victory")
> await $AnimatedSprite2D.animation_finished
> # Next round starts immediately the moment the turn completes!
> MatchManager.start_next_round()
> ```
> Deleting the particle streaks corresponds to removing a `GPUParticles2D` child node and keeping only a `Sprite2D` flame aura with a custom shader or scale `Tween`.
"""

if "## 41. Single-Turn Animation Pacing" not in text:
    with open(path, "a", encoding="utf-8", newline="\n") as f:
        f.write(section41)
    print("Appended Section 41 to Docs/01_UNITY_FOR_BEGINNERS.md!")
else:
    print("Section 41 already present.")
