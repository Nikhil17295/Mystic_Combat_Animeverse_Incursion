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

## 40. TimeScale Corruption, Nested Coroutines & Knockback Vectors (Godot vs Unity)

### The Beginner Trap: Storing `Time.timeScale` Inside an Interrupted Coroutine
In fighting games, "HitStop" (a micro-freeze of $0.05\\text{s}\\text{--}0.08\\text{s}$ upon hit impact) gives attacks visceral weight.
Developers typically implement this with a Unity coroutine:
```csharp
private IEnumerator DoHitStop(float duration) {
    float prevScale = Time.timeScale;
    Time.timeScale = 0.04f;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = prevScale;
}
```
**The Deadly Bug (Simultaneous Clash):**
1. Player 1 hits Player 2 $\\rightarrow$ Coroutine 1 starts. `prevScale` is recorded as `1.0f`. `Time.timeScale` becomes `0.04f`.
2. Player 2 hits Player 1 on the exact same frame or during the hitstop $\\rightarrow$ `StopCoroutine(activeHitstop)` cancels Coroutine 1.
3. Coroutine 2 starts. It executes:
   ```csharp
   float prevScale = Time.timeScale; // Time.timeScale is ALREADY 0.04f!
   ```
4. Coroutine 2 finishes and runs:
   ```csharp
   Time.timeScale = prevScale; // Sets Time.timeScale to 0.04f PERMANENTLY!
   ```
5. **Result:** The game runs at $4\\%$ speed forever!

### The Solution: Re-Entrant HitStop Management
Never sample `Time.timeScale` while a HitStop is already active!
- Maintain an `isHitStopActive` boolean and cache `originalTimeScale` (defaulting safely to `1.0f`).
- Only sample `originalTimeScale` when `!isHitStopActive` and `Time.timeScale > 0.1f`.
- If an overlapping hitstop arrives, refresh the timer without overwriting the clean baseline.
- Add an `OnDisable()` safeguard so scene transitions or component toggles always restore `Time.timeScale = 1.0f`.

### Knockback Vectors: Why 2D Fighters Need Decoupled X and Y Velocities
- If an attack engine sets `linearVelocity = new Vector2(knockbackX, 3.2f)`, every light jab pops the opponent into the sky with $3.2\\text{ m/s}$ vertical lift.
- Light jabs should flinch horizontally ($2.2\\text{f}$) with minimal lift ($0.8\\text{f}$).
- Multi-hit flurry moves (like Saitama's Consecutive Normal Punches) must have low horizontal knockback ($1.2\\text{f}$) and gentle lift ($0.4\\text{f}$) so the opponent stays trapped inside the flurry rather than rocketing out on hit 1.

> **Godot Comparison:**
> In **Godot**, you modify `Engine.time_scale` for hitstop, and create a realtime timer via `await get_tree().create_timer(duration, true, false, true).timeout` (`ignore_time_scale = true`). Just like in Unity, if multiple timers overlap, overwriting a local `var prev_scale = Engine.time_scale` causes permanent slow-mo. Godot developers maintain a stateful `HitStopManager` Autoload that preserves the clean baseline scale.
"""

# 2. 02_ARCHITECTURE.md
doc2 = """
---

## 24. Re-Entrant HitStop Management & 2D Vector Knockback Architecture

```mermaid
sequenceDiagram
    autonumber
    participant P1 as Player 1 Hitbox
    participant P2 as Player 2 Hitbox
    participant FC1 as Fighter 1 Controller
    participant FC2 as Fighter 2 Controller
    participant VFX as VFXManager
    participant Time as Unity Time Engine

    Note over P1,P2: Simultaneous Attack Clash (Both land within 1 frame)
    P1->>FC2: ProcessHit() deals damage + knockback
    P1->>VFX: TriggerHitStop(0.065s)
    Note over VFX: isHitStopActive was FALSE -> originalTimeScale = 1.0f
    VFX->>Time: Time.timeScale = 0.04f (Physics micro-freeze)

    P2->>FC1: ProcessHit() deals damage + knockback
    P2->>VFX: TriggerHitStop(0.065s) (Re-entrant Call!)
    Note over VFX: isHitStopActive is TRUE! Do NOT overwrite originalTimeScale!
    VFX->>Time: Refresh HitStop timer, keep originalTimeScale = 1.0f

    FC1->>FC1: TransitionTo(HitStun) -> attackHitbox.Deactivate()
    FC2->>FC2: TransitionTo(HitStun) -> attackHitbox.Deactivate()

    Note over VFX: Realtime duration completes
    VFX->>Time: Time.timeScale = originalTimeScale (1.0f)
    Note over VFX,Time: TimeScale safely restored! Zero permanent slow-mo!
```

### Architectural Pillars:
1. **Re-Entrant HitStop Guard (`VFXManager.cs`):**
   - Encapsulates `isHitStopActive` state.
   - Prevents nested coroutines from reading an already slowed `Time.timeScale` ($0.04\\text{f}$) as the "normal" scale.
   - Restores physics speed safely on completion, interruption, or component disable.
2. **2D Decoupled Knockback (`HitboxController.cs`):**
   - Replaced monolithic vertical lift with parameterized `knockbackY` in `ConfigureAttack(damage, knockbackX, kiGain, offset, size, knockbackY)`.
   - Allows fine-grained control: jabs flinch without floating, heavy strikes send targets reeling, and multi-hit flurries trap opponents in place.
3. **Counter-Hit Attack Interruption (`FighterController.cs`):**
   - In `TransitionTo(newState)`, if entering `HitStun`, `KnockBack`, or `Knockout`, `attackHitbox.Deactivate()` is executed immediately.
   - Prevents lingering hitboxes from hitting after a fighter has already been interrupted.
"""

# 3. 03_FEATURE_BREAKDOWN.md
doc3 = """
---

## 34. Tuned Combat Knockback & Saitama Flurry Speed Normalization

### 1. Knockback Rebalancing Across All Moves
To prevent fighters from rocketing across the 17-unit arena floor into invisible boundary walls, horizontal and vertical knockback velocities were balanced:

| Move | Character | Old Knockback (X, Y) | Tuned Knockback (X, Y) | Combat Feel / Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **Light Attack** | Both | $4.5\\text{f},\\ 3.2\\text{f}$ | $\\mathbf{2.2f},\\ \\mathbf{0.8f}$ | Fast, snappy jab; flinches opponent without launching them into air. |
| **Heavy Attack** | Both | $8.0\\text{f},\\ 3.2\\text{f}$ | $\\mathbf{5.0f},\\ \\mathbf{2.0f}$ | Crushing blow; creates solid spacing and wall stagger. |
| **Consecutive Punches** | Saitama (Skill) | $6.0\\text{f},\\ 3.2\\text{f}$ (per hit!) | $\\mathbf{1.2f},\\ \\mathbf{0.4f}$ | Traps the opponent in a sustained juggle barrage for all 15 punches. |
| **Kamehameha** | Goku (Ultimate) | $14.0\\text{f},\\ 3.2\\text{f}$ | $\\mathbf{8.5f},\\ \\mathbf{2.2f}$ | High-impact energy blast pushing opponent to arena edge. |
| **Serious Punch** | Saitama (Ultimate) | $24.0\\text{f},\\ 3.2\\text{f}$ | $\\mathbf{12.0f},\\ \\mathbf{3.0f}$ | Devastating screen-clearing knockout punch staying within bounds. |

### 2. Saitama Forward Speed in Consecutive Normal Punches
- **Previous Behavior:** Moved forward at `1.5f` velocity continuously for $2.70\\text{s}$ ($4.05\\text{ units}$ of distance), causing Saitama to skate past opponents.
- **Tuned Behavior:** Forward flurry velocity tuned to `0.60f` ($1.62\\text{ units}$ total), giving Saitama an intimidating, steady anime martial artist advance.
"""

# 4. 04_VIVA_AND_INTERVIEW_PREP.md
doc4 = """
---

### Question 35: What happens when two fighters hit each other at the exact same frame (Double-Hit Clash), and how did you prevent the game from getting permanently frozen in slow-motion?
**Answer for Judges:**
> *"When two fighters trade hits simultaneously in fighting games, both hitboxes trigger HitStop (a micro-freeze that drops `Time.timeScale` to $0.04\\times$ physics speed for visceral impact).
>
> In an unshielded coroutine implementation, Hit 1 sets `Time.timeScale = 0.04f` and records `prevScale = 1.0f`. When Hit 2 lands a fraction of a second later, it cancels Hit 1's coroutine and starts a new one. The new coroutine reads `Time.timeScale`, which is ALREADY $0.04\\text{f}$! When Hit 2 finishes, it restores `Time.timeScale` back to $0.04\\text{f}$, trapping the entire game in slow-motion forever.
>
> We solved this through **Re-Entrant HitStop Management** in `VFXManager.cs`:
> 1. We maintain an `isHitStopActive` state boolean.
> 2. We only sample `originalTimeScale` when entering hitstop from normal gameplay (`!isHitStopActive` and `Time.timeScale > 0.1f`).
> 3. If a second hit lands while hitstop is active, we refresh the freeze timer without overwriting the cached baseline scale.
> 4. On completion, `Time.timeScale` safely restores to `1.0f`. We also added an `OnDisable()` safeguard to ensure timeScale never leaks across scenes."*

---

### Question 36: Why were knockbacks sending fighters flying off the stage, and how did you balance vertical lift across different attack types?
**Answer for Judges:**
> *"Our fighting arena is 17 world units wide ($X = -8.5$ to $+8.5$). Originally, our hitbox controller applied a monolithic vertical velocity of $3.2\\text{ m/s}$ to every hit, alongside massive horizontal velocities ($24\\text{f}$ on Serious Punch, $6\\text{f}$ on each punch of Saitama's 15-hit flurry).
>
> This caused two gameplay issues:
> 1. Simple light jabs launched opponents into the sky, breaking combo opportunities.
> 2. Saitama's Consecutive Normal Punches blasted the target up and out of the flurry on punch #1, making the remaining 14 punches whiff completely.
>
> We resolved this by decoupling knockback into distinct horizontal and vertical components (`knockbackX` and `knockbackY` in `ConfigureAttack`):
> - **Light Jab:** Tuned to $2.2\\text{f}$ horizontal, $0.8\\text{f}$ vertical for responsive ground flinching.
> - **Saitama Flurry:** Tuned to $1.2\\text{f}$ horizontal, $0.4\\text{f}$ vertical. This traps and juggles the target directly in front of Saitama throughout all 15 punches.
> - **Heavy & Ultimates:** Scaled appropriately ($5.0\\text{f}$ Heavy, $8.5\\text{f}$ Kamehameha, $12.0\\text{f}$ Serious Punch), preserving cinematic impact without blasting fighters off-screen."*
"""

# 5. 05_DEV_LOG_AND_BUGS.md
doc5 = """
---

### Bug 29: Simultaneous Hit Double-HitStop TimeScale Freeze & Knockback Launch Overshoot

- **Symptom:**
  1. When both fighters attacked and hit each other at the same time (or during Saitama's rapid punch flurry), the game slowed down to a crawl ($0.04\\times$ speed) and never sped back up.
  2. Knockbacks on simple attacks sent fighters flying across the entire screen; light jabs popped opponents high into the air.
  3. During Saitama's Consecutive Normal Punches, Saitama skated forward too fast ($1.5\\text{f}$ speed) and pushed the opponent across the arena, while the opponent was knocked into orbit on punch #1.
- **Root Cause:**
  1. **Nested HitStop TimeScale Leak:** In `VFXManager.DoHitStop`, `float prevScale = Time.timeScale;` was sampled inside the coroutine. When Hit 2 interrupted Hit 1, `Time.timeScale` was already $0.04\\text{f}$. Hit 2 captured `prevScale = 0.04f` and restored it to $0.04\\text{f}$ upon completion, permanently corrupting the game clock.
  2. **Hardcoded Monolithic Vertical Lift:** `HitboxController.cs` applied `targetRb.linearVelocity = new Vector2(facingDir * knockback, 3.2f);` on every hit. Even light jabs applied $3.2\\text{f}$ vertical lift.
  3. **Excessive Forward Flurry Speed:** In `FighterController.cs`, `FighterState.Skill` assigned `rb.linearVelocity = new Vector2(surgeDir * 1.5f, rb.linearVelocity.y);` every frame for $2.7\\text{s}$, moving Saitama over 4 world units forward.
- **Resolution:**
  1. **Re-Entrant Safe HitStop:** In `VFXManager.cs`, added `isHitStopActive` and `originalTimeScale`. Baseline time scale is only captured when hitstop is not already running. When overlapping hits arrive, the timer is refreshed without overwriting `originalTimeScale`. Added `OnDisable()` safeguard.
  2. **Attack Interruption on Counter-Hit:** In `FighterController.TransitionTo()`, entering `HitStun`, `KnockBack`, or `Knockout` immediately deactivates `attackHitbox`.
  3. **Decoupled 2D Knockback Tuning:** Added `knockbackY` to `HitboxController.Setup()` and `ConfigureAttack()`. Rebalanced all attack velocities:
     - Light: $2.2\\text{f}$ X, $0.8\\text{f}$ Y (was $4.5\\text{f},\\ 3.2\\text{f}$).
     - Heavy: $5.0\\text{f}$ X, $2.0\\text{f}$ Y (was $8.0\\text{f},\\ 3.2\\text{f}$).
     - Saitama Flurry: $1.2\\text{f}$ X, $0.4\\text{f}$ Y (was $6.0\\text{f},\\ 3.2\\text{f}$).
     - Kamehameha: $8.5\\text{f}$ X, $2.2\\text{f}$ Y (was $14.0\\text{f},\\ 3.2\\text{f}$).
     - Serious Punch: $12.0\\text{f}$ X, $3.0\\text{f}$ Y (was $24.0\\text{f},\\ 3.2\\text{f}$).
  4. **Controlled Flurry Forward Advance:** Reduced Saitama's `surgeSpeed` during `FighterState.Skill` from `1.5f` to `0.60f`, delivering a tight, menacing forward step into the punch barrage.
"""

append_to_file("Docs/01_UNITY_FOR_BEGINNERS.md", doc1)
append_to_file("Docs/02_ARCHITECTURE.md", doc2)
append_to_file("Docs/03_FEATURE_BREAKDOWN.md", doc3)
append_to_file("Docs/04_VIVA_AND_INTERVIEW_PREP.md", doc4)
append_to_file("Docs/05_DEV_LOG_AND_BUGS.md", doc5)
