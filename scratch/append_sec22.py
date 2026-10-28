path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Docs\02_ARCHITECTURE.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

sec22 = """

---

## 22. Dramatic Slow-Motion K.O. State Machine & Authentic DBZ Flame Architecture

To achieve authentic arcade fighting game weight and cinematic punch, Mystic Combat replaces abrupt round transitions with a 4-phase decoupled K.O. sequence and an oscillating dual-layer anime flame shell.

```mermaid
sequenceDiagram
    autonumber
    actor HP as Fighter Health
    participant MM as MatchManager
    participant Phys as Physics (Box2D)
    participant VFX as VFXManager
    participant Anim as FighterAnimator
    participant Def as Defeated Fighter
    participant Win as Victorious Fighter

    HP->>MM: OnDeath (Health reaches 0)
    MM->>Def: TransitionTo(FighterState.Knockout)
    MM->>Phys: Time.timeScale = 0.28f (Slow Motion Physics)
    MM->>VFX: TriggerImpactFlash(0.12s) + CameraShake(0.40)
    Note over Def,Anim: Phase 1: Loser plays 11-frame collapse animation in slow-mo
    Anim->>Def: PlayOnce(death_0..10, fps: 6.5, unscaled: true)
    MM->>MM: WaitForSecondsRealtime(1.8s)
    Note over MM,Phys: Phase 2: Loser hits floor -> snap physics back to normal
    MM->>Phys: Time.timeScale = 1.0f
    MM->>VFX: PlayDustPuff(floor) + Impact Camera Shake(0.20)
    Note over Win,Anim: Phase 3: Winner starts victory pose loop
    MM->>Win: TransitionTo(FighterState.Victory)
    Anim->>Win: PlayLoop(victory_0..8, fps: 6.0, unscaled: true)
    Note over MM: Phase 4: Dramatic Hold & Let Things Sink In
    MM->>MM: WaitForSeconds(2.8s)
    MM->>MM: CheckWinCondition() / StartRound()
```

### Architectural Principles:
1. **Decoupled Delta Time (`unscaled: true`):**
   - **The Problem:** Scaling animations with `Time.deltaTime` during a slow-motion knockdown causes frame rate to drop from 60 FPS to 15 FPS, resulting in choppy 3-frame slideshow animations.
   - **The Solution:** In [`FighterAnimator.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterAnimator.cs), `PlayOnce` and `PlayLoop` support `unscaled: true` using `Time.unscaledDeltaTime`. Physics (gravity, knockback recoil, debris) moves at $0.28\\times$ cinematic speed, while the 11-frame pixel-art collapse plays at a full, fluid framerate over exactly 1.7 real seconds.
2. **Dual-Layer Oscillating DBZ Aura (`FighterKiAura.cs`):**
   - **Layer 1 (Spiky Flame Cocoon):** A high-frequency oscillating sprite shell (`dbz_spiky_aura.png`, 26 jagged flame spires) fluctuating at $24\\text{–}28\\text{ Hz}$ with dynamic Perlin scaling and alpha pulsation.
   - **Layer 2 (Speed-Streak Sub-Emitter):** Vertical stretched needle particles shooting rapidly upward through the cocoon from the feet past the fighter's head.
   - **Dynamic Color Tinting:** Single-pass tinting allows the grayscale master sprite to project Radiant DBZ Gold (Super Saiyan Goku), Electric Cyan (Base Goku meditation), or Blazing Crimson (Saitama).
3. **Super Saiyan State Guards:**
   - In [`FighterController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs), meditation is strictly blocked while `isSuperSaiyan` is true (`!isSuperSaiyan`), preventing Goku from generating Ki while ascended.
4. **Pacing Standardization (199s Round Timer):**
   - Set `roundDuration = 199f` in `MatchManager.cs` to eliminate rushed play and provide ample time for footsies, spacing, and dramatic momentum swings.
5. **Godot vs Unity Analogy:**
   - **Godot:** In Godot, you'd set `Engine.time_scale = 0.28`, and mark `process_mode = PROCESS_MODE_ALWAYS` or use `_process(get_process_delta_time())` on an `AnimatedSprite2D` to avoid timescale slowdown.
   - **Unity:** In Unity, `Time.timeScale = 0.28f` governs the physics loop and `Time.deltaTime`, while `Time.unscaledDeltaTime` drives UI and decoupled celebratory sprite sequences.
"""

text = text.rstrip() + sec22
with open(path, "w", encoding="utf-8") as f:
    f.write(text)
print("Successfully appended Section 22 to Docs/02_ARCHITECTURE.md")
