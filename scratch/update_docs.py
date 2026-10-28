sec20 = """
---

## 20. Dual-Tier Character Transformation & Fallback Architecture (Super Saiyan Mode)

Mystic Combat implements a decoupled, high-resilience transformation system allowing fighters to morph into elevated power forms (such as Goku ascending to Super Saiyan) without duplicating character prefabs, corrupting game loops, or causing missing-sprite crashes.

```mermaid
flowchart TD
    A["Player presses Skill (U key)"] --> B{"Is Fighter Goku and Has >= 35 Ki?"}
    B -- No --> C["Standard Skill / Flurry Attack"]
    B -- Yes --> D["Consume 35 Ki"]
    D --> E["ActivateSuperSaiyan(15s)"]
    E --> F["Set isSuperSaiyan = true and Timer = 15.0s"]
    F --> G["FighterAudio.StartKiMeditationAura() (DBZ Aura Loop)"]
    F --> H["Apply Movement and Damage Multipliers (+25%)"]
    F --> I["FighterAnimator Dynamic Resolution: GetFrames(ssjList, baseList)"]
    I --> J{"SSJ Frame Sequence Sliced?"}
    J -- Yes --> K["Render Golden Hair Super Saiyan Sprite"]
    J -- No --> L["Graceful Fallback: Render Base Goku Sprite (Zero Crashes)"]
    F --> M["Timer reaches 0s OR Round Over"]
    M --> N["RevertSuperSaiyan(): Reset Speed, Damage, and Sprite Sets"]
```

### Architectural Highlights:
1. **Zero-Crash Graceful Fallback (`GetFrames` Pattern):**
   - In traditional sprite systems, switching form sets can throw index out-of-range or null pointer exceptions if an artist hasn't yet exported every single animation for the transformed state (e.g., crouching or jumping).
   - `FighterAnimator.GetFrames(ssjList, baseList)` inspects if `isSuperSaiyan` is true and `ssjList.Count > 0`. If any SSJ sequence is missing, it dynamically falls back to the Base sequence. The game remains 100% playable at all times.
2. **Stat Multiplier Overlay:**
   - Rather than hardcoding separate Super Saiyan stat blocks, `FighterController` uses an operational multiplier (`isSuperSaiyan ? 1.25f : 1.0f`) on linear velocity calculations and hitbox configuration calls.
3. **Round Reset Auto-Reversion:**
   - `FighterController.ResetForNewRound()` explicitly calls `RevertSuperSaiyan()`, guaranteeing fighters spawn in base form at the start of each round.
4. **Godot vs Unity Analogy:**
   - **Godot:** In Godot, you might swap an `AnimatedSprite2D`'s `SpriteFrames` resource or flip an `AnimationTree` blend state.
   - **Unity:** In Unity, `FighterAnimator` dynamically routes frame requests through the cached `Frames_SSJ` list pointers, avoiding asset reloads or disk reads during live 60 FPS combat.
"""

with open(r"Docs\02_ARCHITECTURE.md", "r", encoding="utf-8") as f:
    orig = f.read().rstrip()

with open(r"Docs\02_ARCHITECTURE.md", "w", encoding="utf-8") as f:
    f.write(orig + "\n" + sec20 + "\n")

print("02_ARCHITECTURE.md updated with Section 20!")


sec21 = """
---

## 21. Super Saiyan Transformation Mode (Goku Special Skill)

### Feature Overview:
In *Mystic Combat: Animeverse Incursion*, Goku's special skill (`U` key / `E` key) is not a generic projectile — it is a mid-match transformation into **Super Saiyan**!

### Combat Parameters & Tuning:
- **Activation Cost:** 35 Ki (requires at least 35% of energy gauge).
- **Transformation Startup:** 0.65s roar pose with golden Ki aura activation (`dbz_super_saiyan_aura.mp3`).
- **Form Duration:** 15.0 seconds.
- **Combat Multipliers:**
  - **Movement Speed:** $+25\%$ linear velocity multiplier.
  - **Damage:** $+25\%$ damage output on all punches, kicks, and strikes.
  - **Visual:** Golden spiky hair and pulsing Ki aura.
- **Auto-Reversion:**
  - Automatically reverts to Base Goku when the 15-second timer expires.
  - Automatically reverts if a round ends (via `ResetForNewRound`).
"""

with open(r"Docs\03_FEATURE_BREAKDOWN.md", "r", encoding="utf-8") as f:
    orig2 = f.read().rstrip()

with open(r"Docs\03_FEATURE_BREAKDOWN.md", "w", encoding="utf-8") as f:
    f.write(orig2 + "\n" + sec21 + "\n")

print("03_FEATURE_BREAKDOWN.md updated with Section 21!")
