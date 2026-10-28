# Update 02_ARCHITECTURE.md with Section 21
sec21_arch = """
---

## 21. Master VFX & Combat Juice Architecture (`VFXManager.cs` & `FighterKiAura.cs`)

Mystic Combat implements a zero-allocation, pooled Visual Effects engine specifically designed for 60 FPS fighting game responsiveness.

```mermaid
flowchart TD
    A["Attack Connects in HitboxController"] --> B["Calculate hitCenter = Lerp(HitboxCenter, HurtboxCenter, 0.5)"]
    B --> C["VFXManager.Instance.PlayHitSpark(hitCenter, isHeavy)"]
    C --> D["Recycle from Pre-warmed Object Pool (Zero GC Stutter)"]
    C --> E["Apply Hitstop Micro-Pause: Time.timeScale = 0.04f (0.065s)"]
    C --> F["Trigger Camera Shake on Camera.main"]
    A --> G{"Damage >= 35?"}
    G -- Yes --> H["TriggerImpactFlash: 2-Frame White Overlay Flash"]
    A --> I["Target.ApplyHit(damage, stunDuration)"]
    I --> J["Target enters FighterState.HitStun -> Plays hurt_*.png"]
    I --> K{"Health <= 0?"}
    K -- Yes --> L["Target enters FighterState.Knockout -> Plays death_*.png"]
    K -- Yes --> M["Winner enters FighterState.Victory -> Plays victory_*.png loop"]
```

### Key Architectural Pillars:
1. **Pre-warmed Object Pooling:** In a fast fighter, attacks land 3–5 times per second. Spawning `Instantiate()` and `Destroy()` triggers Garbage Collection (GC) pauses that drop frames. `VFXManager` pre-warms 32 particle emitters and shockwave GameObjects in `Awake()`, cycling them through FIFO queues with `SetActive(true/false)`.
2. **Hitstop (Juice / Impact Freeze):** A hallmark of Capcom and Arc System Works fighting games. Freezing the world for 65 milliseconds on heavy impact gives attacks real physical weight and lets the human eye register the collision.
3. **Dynamic Ki Particle Aura (`FighterKiAura.cs`):** A custom particle emitter attached to each fighter that continuously evaluates state:
   - Goku Super Saiyan: Blazing golden flames and rising particles.
   - Base Goku Meditation: Electric cyan Ki flames.
   - Saitama: Menacing crimson pressure aura.
4. **Godot vs Unity Analogy:**
   - **Godot:** In Godot, you'd emit signals to a global `VFX` autoload or instantiate packed scenes (`GPUParticles2D`) and call `queue_free()`.
   - **Unity:** In Unity, `VFXManager.Instance` coordinates pre-warmed `ParticleSystem` components and screen-shake coroutines directly on the main scene camera.
"""

with open(r"Docs\02_ARCHITECTURE.md", "r", encoding="utf-8") as f:
    arch = f.read().rstrip()

with open(r"Docs\02_ARCHITECTURE.md", "w", encoding="utf-8") as f:
    f.write(arch + "\n" + sec21_arch + "\n")
print("02_ARCHITECTURE.md updated with Section 21!")

# Update 04_VIVA_AND_INTERVIEW_PREP.md with Viva Question 28 & 29
viva_qa = """
---

### Question 28: How does your game handle hit feedback and "Game Feel" without causing Garbage Collection frame drops?
**Answer for Judges:**
> *"In fighting games, game feel—or 'juice'—is what separates a stiff prototype from an authentic arcade fighter. We built a dedicated `VFXManager` that combines four synchronized layers on every hit:
> 1. **Hitstop:** A 65ms micro-pause (`Time.timeScale = 0.04f`) on impact that gives punches visceral physical weight.
> 2. **Camera Shake:** A decay-curve orthographic camera shake on the Main Camera.
> 3. **Hit Sparks & Shockwaves:** High-contrast particle starbursts spawned at the exact intersection between the attacking Hitbox and defending Hurtbox.
> 4. **Pre-warmed Object Pooling:** Spawning and destroying GameObjects at 60 FPS causes GC stutter. We pre-warm 32 reusable emitters in memory during scene load, cycling them with zero allocations during live combat."*

---

### Question 29: How did you ensure sprite animations don't jitter or hop when switching between Base, Super Saiyan, Hurt, and Knockout states?
**Answer for Judges:**
> *"Different action poses have different bounding box sizes (e.g., crouching is 45px tall while jumping is 60px tall). If you crop sprites to their tightest bounding box and use center pivots, the character's feet will visibly hop and jitter off the ground.
> We solved this through **Standardized Baseline Canvas Anchoring**:
> - Every frame is normalized to a fixed canvas ($128 \\times 128$ for Goku, $160 \\times 160$ for Saitama).
> - The lowest foot pixel is strictly anchored to a standardized baseline coordinate ($Y = 105$ for Goku, $Y = 135$ for Saitama).
> This guarantees that regardless of whether Goku is idling, walking, flinching in hitstun, or transforming into Super Saiyan, his feet stay locked to the arena floor."*
"""

with open(r"Docs\04_VIVA_AND_INTERVIEW_PREP.md", "r", encoding="utf-8") as f:
    viva = f.read().rstrip()

with open(r"Docs\04_VIVA_AND_INTERVIEW_PREP.md", "w", encoding="utf-8") as f:
    f.write(viva + "\n" + viva_qa + "\n")
print("04_VIVA_AND_INTERVIEW_PREP.md updated with Questions 28 & 29!")
