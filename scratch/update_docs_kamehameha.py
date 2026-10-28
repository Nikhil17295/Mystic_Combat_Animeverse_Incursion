import os

def append_to_file(path, content):
    with open(path, "r", encoding="utf-8") as f:
        original = f.read()
    if content.strip() in original:
        print(f"Already in {path}")
        return
    with open(path, "a", encoding="utf-8") as f:
        f.write("\n\n" + content.strip() + "\n")
    print(f"Appended to {path}")

# 1. 01_UNITY_FOR_BEGINNERS.md
doc1 = """
---

## 42. Layered Anime Energy Beams, Multi-Sprite Assemblies & Single-Hit AoE Colliders (Godot vs Unity)

### The Multi-Sprite Beam Assembly Pattern
In 2D fighting games (such as *Dragon Ball FighterZ* and *Jump Ultimate Stars*), a cinematic energy beam cannot be rendered as a simple stretched rectangle. To achieve authentic anime weight, it is constructed from four coordinated sprite layers:
1. **The Charge Orb:** A radial Ki sphere (`kamehameha_charge_orb.png`) gathering at the attacker's cupped palms during the voice chant ("KA... ME... HA... ME...").
2. **The Muzzle Flare:** A bright starburst flare (`kamehameha_muzzle_flare.png`) anchored at the palm release point.
3. **The Beam Shaft:** A horizontal energy cylinder (`kamehameha_beam_body.png`) with an ultra-white core encased in electric cyan plasma, scaled along the $X$-axis.
4. **The Leading Impact Head:** An aerodynamic energy dome (`kamehameha_beam_head.png`) capping the forward tip of the beam.

### The Left-Center Pivot Trick (`Vector2(0.0f, 0.5f)`)
- **The Beginner Trap:** By default, Unity sprites use a center pivot `(0.5f, 0.5f)`. If you scale a beam with a center pivot, the beam expands equally backwards through the fighter's back as it does forwards!
- **The Solution:** Set the sprite pivot to **Left-Center (`0.0f, 0.5f`)**:
  - The left edge remains permanently locked to the fighter's hands.
  - Scaling $X$ directly extends the beam forward across the stage.
  - Multiplying scale $X$ by `facingDir` ($-1$ or $+1$) automatically mirrors the entire beam when facing left or right with zero math overhead.

### Single-Hit AoE vs Multi-Hit Drilling
- When an attack beam stays on screen for $1.21\\text{ seconds}$ (over 70 frames at 60 FPS), an unshielded trigger collider will deal damage 70 times, instantly annihilating any fighter in its path.
- **Single-Hit AoE Guarantee:** In `HitboxController.cs`, we register hit targets in a `HashSet<Transform> hitTargetsThisSwing`. When an opponent touches any part of the $8.5\\text{m}$ beam, they take the $45\\text{ damage}$ once, receive normal knockback ($8.5\\text{f}, 2.2\\text{f}$), and enter hitstun. Subsequent frames ignore them because their transform is already in the set!

> **Godot Comparison:**
> In **Godot**, a beam is commonly built using a `Line2D` with a `Gradient` texture and texture mode `TILE` or `STRETCH`, or an `Area2D` containing child `Sprite2D` nodes with `offset` set to `(width / 2, 0)` to emulate left-aligned pivoting. Single-hit detection is handled by tracking hit bodies in an `Array` during the `body_entered` signal.
"""

# 2. 02_ARCHITECTURE.md
doc2 = """
---

## 26. Kamehameha State Machine Lifecycle & Full-Screen AoE Blast Architecture

```mermaid
sequenceDiagram
    autonumber
    participant Input as Player Input
    participant FC as Goku FighterController
    participant VFX as KamehamehaBeamVFX
    participant Hit as HitboxController
    participant Cam as VFXManager (Camera)
    participant Opp as Opponent Fighter

    Input->>FC: Ultimate Pressed (Q / O, 80 Ki)
    FC->>FC: TransitionTo(FighterState.Ultimate)
    Note over FC: attackTimer = 3.36s | startup = 2.15s | active = 1.21s

    loop Phase 1: Startup Charge (2.15s)
        VFX->>VFX: Position charge orb in cupped hands (-0.35m, 0.88m)
        VFX->>VFX: Swell orb scale (0.20m -> 0.88m) + 30Hz Ki pulse
        Note over FC: Audio: "KA... ME... HA... ME..."
    end

    Note over FC,VFX: Startup Timer reaches 0s -> Eruption!
    FC->>Hit: Activate Hitbox (Offset: 4.6m, Size: 8.5m x 1.6m)
    VFX->>Cam: TriggerCameraShake(0.30, 0.38s) + ImpactFlash(0.04s)
    VFX->>VFX: Deactivate Orb -> Activate Muzzle Flare + Beam Body + Beam Head

    loop Phase 2: Beam Blast (1.21s)
        VFX->>VFX: Roaring plasma oscillation (1.35m - 1.55m thickness)
        Hit->>Opp: Overlap detected! Deal 45 Dmg, 8.5f X / 2.2f Y Knockback
        Note over Hit: Opponent added to hitTargetsThisSwing -> SINGLE HIT!
        Note over FC: Audio: "HA-A-A-A!"
    end

    Note over VFX: activeTimer < 0.20s -> Fade beam alpha (1.0 -> 0.0)
    FC->>Hit: Deactivate Hitbox
    VFX->>VFX: StopAllVFX()
    FC->>FC: TransitionTo(Idle)
```

### Architectural Principles:
1. **Decoupled Visual Component (`KamehamehaBeamVFX.cs`):**
   - Implements a self-contained visual controller attached directly to Goku.
   - Monitors `controller.CurrentState`, `AttackStartupTimer`, and `AttackActiveTimer` to drive visual phases without polluting core movement or state transition logic.
2. **Instant Interruption Cleanup:**
   - If Goku takes damage and transitions to `HitStun` or `Knockout`, `KamehamehaBeamVFX.Update()` detects `CurrentState != Ultimate` and shuts down all active sprites on the exact same frame.
3. **True AoE Beam Alignment:**
   - Visual beam body ($7.2\\text{m}$) and leading head ($1.3\\text{m}$) yield an $8.5\\text{m}$ total reach, perfectly synchronized with `HitboxController.ConfigureAttack(45, 8.5f, 15f, new Vector2(4.6f, 0.95f), new Vector2(8.5f, 1.6f), 2.2f)`.
"""

# 3. 03_FEATURE_BREAKDOWN.md
doc3 = """
---

## 36. Authentic Anime Kamehameha VFX & AoE Beam Specification

### 1. Visual & Acoustic Specifications
- **Total Duration:** $3.36\\text{ seconds}$ (100% synchronized with `goku_voice_kamehameha.wav`).
- **Phase 1: Gathering Ki Charge Orb ($2.15\\text{s}$):**
  - Texture: `Assets/Sprites/Effects/kamehameha_charge_orb.png` ($128 \\times 128$).
  - Position: Cupped hands at hip ($(X = -0.35\\text{m}, Y = 0.88\\text{m})$ relative to pivot when facing right).
  - Animation: Non-linear expansion from $0.20\\text{m} \\rightarrow 0.88\\text{m}$ with $30\\text{ Hz}$ sine vibration.
- **Phase 2: Solid Light-Blue Energy Beam Blast ($1.21\\text{s}$):**
  - Muzzle Flare: `kamehameha_muzzle_flare.png` at $(X = +0.55\\text{m}, Y = 0.95\\text{m})$.
  - Beam Body: `kamehameha_beam_body.png` ($7.2\\text{m}$ length, Left-Center pivot).
  - Beam Head: `kamehameha_beam_head.png` ($1.3\\text{m}$ length, aerodynamic energy dome).
  - Total Beam Span: **$8.5\\text{ world units}$** (covers half the entire tournament arena).
  - Roaring Plasma Oscillation: $1.35\\text{m} \\pm 0.08\\text{m}$ vertical breathing at $26\\text{ Hz}$.
  - Screen Shake: $0.30\\text{ intensity}, 0.38\\text{s}$ duration on eruption.
  - Impact Flash: 2-frame fullscreen white flash ($0.04\\text{s}$).

### 2. Combat Mechanics
- **Damage:** $45\\text{ HP}$ (Ultimate attack tier).
- **Knockback Vector:** $8.5\\text{f}$ horizontal, $2.2\\text{f}$ vertical lift ("normal knockback" that pushes target to wall without launching off-screen).
- **Single-Hit AoE:** Opponent is hit exactly once upon touching the beam and cannot be multi-hit by the same beam blast.
- **Ki Cost:** $80\\text{ Ki}$.
"""

# 4. 04_VIVA_AND_INTERVIEW_PREP.md
doc4 = """
---

### Question 39: How did you implement Goku's Kamehameha beam to ensure it looks like authentic DBZ anime and hits targets once across the entire screen without infinitely grinding their health?
**Answer for Judges:**
> *"In fighting games, implementing full-screen beam attacks requires solving two distinct challenges: visual authenticity and hitbox arbitration.
>
> 1. **Visual Authenticity (Two-Phase Anime Staging):**
>    - **Charge Phase (2.15s):** Goku draws his cupped hands back to his hip while chanting 'KA... ME... HA... ME...'. We spawn a pulsing Ki sphere (`kamehameha_charge_orb.png`) that expands from a spark to a crackling electric cyan sphere at his palms.
>    - **Blast Phase (1.21s):** On 'HA!', the orb erupts into a solid light-blue energy beam composed of three coordinated layers: an origin muzzle flare, a thick horizontal energy cylinder (`kamehameha_beam_body.png`) with an ultra-white core and cyan outer plasma, and an aerodynamic leading dome head (`kamehameha_beam_head.png`). We add camera rumble (0.30 intensity) and a 2-frame impact flash.
> 2. **Single-Hit AoE Arbitration:**
>    - Because the beam persists on screen for 1.21 seconds, a standard trigger would deal damage every frame (over 70 hits!).
>    - In `HitboxController.cs`, we register struck targets in a `HashSet<Transform> hitTargetsThisSwing`.
>    - When the opponent enters any part of the 8.5m beam, they take the full 45 damage and normal knockback (8.5f X / 2.2f Y) exactly once. They are immediately recorded in the set, preventing multi-frame hit recursion."*

---

### Question 40: Why did you use Left-Center sprite pivots for the beam shaft, and how does the interruption-safe architecture work?
**Answer for Judges:**
> *"Unity sprites default to a center pivot `(0.5f, 0.5f)`. If you scale a center-pivoted beam horizontally, it expands in both directions, protruding through the back of the character.
> By authoring the sprite with a **Left-Center pivot (`0.0f, 0.5f`)**:
> - The origin is anchored to Goku's palms.
> - Setting scale $X$ directly extends the beam forward across the arena.
> - Flipping direction is as simple as multiplying scale $X$ by `facingDir` (-1 or +1), automatically adapting to Goku facing left or right.
>
> For **Interruption Safety**:
> In `KamehamehaBeamVFX.cs`, the component continuously inspects `FighterController.CurrentState`. If Goku takes a counter-hit or gets knocked down while charging or firing, `CurrentState` changes to `HitStun` or `Knockout`. The VFX component detects this on the exact same frame and instantly deactivates all beam and charge sprites, ensuring no floating beams remain on stage."*
"""

# 5. 05_DEV_LOG_AND_BUGS.md
doc5 = """
---

### Bug 31: Full-Screen Beam Hitbox Alignment & Multi-Hit Recursion in Long-Duration Beams

- **Symptom:**
  1. Goku's Kamehameha lacked visual energy effects; Goku played his charging pose and beam stance, but no actual energy beam or charge orb appeared on screen.
  2. The previous attack hitbox was only 3.0m wide, failing to cover the visual reach of a beam blast across the arena.
  3. Long-duration beam attacks risked multi-hitting opponents on every physics frame, instantly draining 100% health in less than a second.
- **Root Cause:**
  1. **Missing Beam VFX Assembly:** No dedicated sprite hierarchy existed to render the charge orb, muzzle flare, and beam body.
  2. **Hitbox Mismatch:** The hitbox offset ($1.8\\text{m}$) and size ($3.0\\text{m}$) were scaled for a short melee thrust rather than a full-screen energy projectile.
  3. **Multi-Hit Vulnerability:** Attacks that stay active across multiple seconds must strictly guard against clearing hit targets unless designed as rapid flurries.
- **Resolution:**
  1. **Modular VFX Component (`KamehamehaBeamVFX.cs`):** Created a self-contained component driving the 2.15s Ki charge orb gathering and 1.21s solid light-blue energy beam with high-frequency plasma breathing ($26\\text{ Hz}$).
  2. **Generated 4 DBZ Effect Textures:** Programmatically generated `kamehameha_charge_orb.png`, `kamehameha_beam_body.png`, `kamehameha_beam_head.png`, and `kamehameha_muzzle_flare.png` in `Assets/Sprites/Effects/`.
  3. **True AoE Hitbox Synchronization:** Configured hitbox offset to `new Vector2(4.6f, 0.95f)` and size to `new Vector2(8.5f, 1.6f)`, matching the full $8.5\\text{m}$ visual beam.
  4. **Single-Hit Guarantee:** Maintained `hitTargetsThisSwing` set persistence across the entire blast duration, guaranteeing exactly one hit per target.
"""

append_to_file("Docs/01_UNITY_FOR_BEGINNERS.md", doc1)
append_to_file("Docs/02_ARCHITECTURE.md", doc2)
append_to_file("Docs/03_FEATURE_BREAKDOWN.md", doc3)
append_to_file("Docs/04_VIVA_AND_INTERVIEW_PREP.md", doc4)
append_to_file("Docs/05_DEV_LOG_AND_BUGS.md", doc5)
