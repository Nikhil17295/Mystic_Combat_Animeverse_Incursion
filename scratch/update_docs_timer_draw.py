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

## 41. Round Timers, Sudden Death Overtime Extensions & Double-KO Arbitration (Godot vs Unity)

### The Beginner Trap: Resolving Fatal Events Immediately on the First Event Call
In fighting games, two fighters attacking at the same time can both hit and kill each other in the exact same physics frame.
If you wire an immediate callback:
```csharp
private void HandleP1Death() => EndRound(winner: 2);
private void HandleP2Death() => EndRound(winner: 1);
```
- **The Trap:** If Player 1's death event fires first by $1\\text{ millisecond}$ (due to internal collection iteration order), Player 2 is immediately declared the winner!
- Player 2's death event is either ignored because `currentMatchState` is already `RoundEnd`, or Player 2 gets awarded a win while collapsing into a corpse on the floor!

### The Professional Solution: End-of-Frame Arbitration
Never crown a winner on the very first death notification!
1. When either fighter dies, schedule a coroutine that waits for `yield return new WaitForEndOfFrame()`.
2. This allows all damage calculations, counter-hits, and health updates across both fighters during that physics frame to resolve cleanly.
3. At the end of the frame, inspect both fighters:
   - If **both** are dead $\\rightarrow$ Declare **Double K.O.!** Both fighters collapse into slow-mo knockout, and both players receive $1\\text{ point}$.
   - If only one is dead $\\rightarrow$ Award the round win to the surviving fighter.

### The Sudden Death Overtime Extension Pattern
- If neither fighter is knocked out when the $199\\text{s}$ round timer expires:
  - Do not arbitrarily hand a victory to higher HP!
  - Grant an **Overtime / Sudden Death Extension of $+100\\text{s}$**.
  - Flash the HUD timer in Warning Gold (`#FFD726`) and announce Sudden Death.
- If the extra $100\\text{s}$ expires and both fighters **still remain undefeated**:
  - The round ends in an honorable **Time-Out Draw**.
  - Both fighters receive $1\\text{ point}$ (`p1RoundWins++` and `p2RoundWins++`), neither collapses, and the match proceeds to the next round or sudden-death tiebreaker!

> **Godot Comparison:**
> In **Godot**, you achieve end-of-frame arbitration by using `call_deferred("_evaluate_round_end")` when a `died` signal is received, or checking both fighters' health states inside `_physics_process(delta)` after `move_and_slide()` and hit detection have run for all scene tree nodes.
"""

# 2. 02_ARCHITECTURE.md
doc2 = """
---

## 25. Match Manager Overtime Extension & Simultaneous Death Arbitration Architecture

```mermaid
flowchart TD
    Start["Round Start (199s Timer)"] --> Tick["Tick Round Timer (Update)"]
    Tick --> CheckDeath{"Fighter HP <= 0?"}
    
    CheckDeath -- "Yes (Death Event Fired)" --> WaitFrame["WaitForEndOfFrame() Arbitration"]
    WaitFrame --> BothDead{"Both HP <= 0?"}
    BothDead -- "Yes" --> DoubleKO["Double K.O.! Both Collapse\nP1 +1 Pt | P2 +1 Pt\nOnRoundEnd(0, round)"]
    BothDead -- "No" --> SingleWin["Single Winner Knockout\nWinner +1 Pt\nOnRoundEnd(winner, round)"]

    CheckDeath -- "No (Both Alive)" --> TimerZero{"roundTimer <= 0s?"}
    TimerZero -- "No" --> Tick
    TimerZero -- "Yes" --> CheckOT{"isOvertime == false?"}
    CheckOT -- "Yes (Regular Time Over)" --> GrantOT["Grant +100s Overtime!\nisOvertime = true\nVFX Flash & Camera Shake\nHUD Yellow Warning"]
    GrantOT --> Tick
    CheckOT -- "No (Overtime Expired)" --> TimeDraw["Time-Out Draw!\nNeither Collapses\nP1 +1 Pt | P2 +1 Pt\nOnRoundEnd(0, round)"]

    DoubleKO --> CheckWin{"CheckWinCondition()"}
    SingleWin --> CheckWin
    TimeDraw --> CheckWin

    CheckWin -- "Both >= 2 Wins & Tied (2-2)" --> Tiebreak["Sudden Death Final Round!\nStartRound()"]
    CheckWin -- "One Player >= 2 Wins" --> MatchOver["Tournament Champion Declared!\nOnMatchOver(winner)"]
    CheckWin -- "Neither >= 2 Wins (e.g. 1-1)" --> NextRound["Start Next Round\nStartRound()"]
```

### Architectural Principles:
1. **Deferred Death Arbitration (`WaitForEndOfFrame`):**
   - Eliminates execution-order bias during trading blows.
   - Accurately captures simultaneous double-deaths within the same frame tick.
2. **Re-Entrant Draw Representation (`winnerIndex = 0`):**
   - The event signature `OnRoundEnd?.Invoke(winnerPlayerIndex, currentRound)` utilizes `0` to denote a Draw.
   - Listeners ([`HUDController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/HUDController.cs), [`CombatMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/CombatMenuController.cs)) seamlessly adapt by lighting both players' score gems and presenting Draw Match victory banners.
3. **Pacing Continuity:**
   - Overtime extensions do not reset health or fighter positions; combat flows uninterrupted with fresh clock pressure.
"""

# 3. 03_FEATURE_BREAKDOWN.md
doc3 = """
---

## 35. 199s Round Timer, +100s Sudden Death Overtime & Double K.O. Draw Scoring

### 1. 199s Canonical Round Timer
- **Scene Serialization Synchronization:** Resolved discrepancy where [`SampleScene.unity`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scenes/SampleScene.unity) had `roundDuration: 99`. Serialized value updated to `199` to match [`MatchManager.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchManager.cs).
- **HUD Synchronization:** Fallback timers in [`HUDController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/HUDController.cs) updated from `99f` to `199f`.

### 2. Sudden Death Overtime Extension (+100s)
- **Trigger:** Regular round timer expires ($0\\text{s}$) while both fighters are still standing (`p1Health.CurrentHealth > 0 && p2Health.CurrentHealth > 0`) and `!isOvertime`.
- **Action:**
  - `roundTimer = 100f`
  - `isOvertime = true`
  - Screen impact flash ($0.08\\text{s}$) + camera rumble ($0.30\\text{ intensity}$).
  - HUD timer font shifts to Warning Gold (`#FFD726`) and header updates to `★ OVERTIME • SUDDEN DEATH (+100s) ★`.

### 3. Time-Out Survival Draw (+1 Point to Each)
- **Trigger:** Timer expires ($0\\text{s}$) after +100s overtime has elapsed and both fighters remain unbeaten.
- **Action:**
  - `p1RoundWins++` and `p2RoundWins++`.
  - Both fighters remain standing in pride (neither collapses).
  - Both player score gems light up on HUD.
  - Check win conditions or proceed to sudden-death round.

### 4. Double K.O. Simultaneous Death (+1 Point to Each)
- **Trigger:** Both fighters suffer fatal damage on the same frame or within trading windows.
- **Action:**
  - Both `FighterController` components transition to `Knockout` simultaneously.
  - Cinematic slow-motion collapse ($0.12\\times$ speed for $4.0\\text{s}$).
  - `p1RoundWins++` and `p2RoundWins++`.
  - Both player score gems light up on HUD.

### 5. Sudden Death Tiebreaker Progression
- If a Double K.O. or Draw pushes both players to match point simultaneously (e.g., tied 2-2 in a Best of 3):
  - Instead of ending abruptly, `CheckWinCondition()` triggers a **Sudden Death Final Round** via `StartRound()`.
  - If a player holds a point advantage (e.g., 2-1), that player is crowned Tournament Champion.
"""

# 4. 04_VIVA_AND_INTERVIEW_PREP.md
doc4 = """
---

### Question 37: How does your tournament match system handle simultaneous Double K.O.s and Time-Out Draws without awarding an unfair win to whoever died one millisecond later?
**Answer for Judges:**
> *"In fighting games, simultaneous fatal hits ('trades') are common when both fighters attack at once. If your event system immediately grants the round win to whoever's death event fires first, you create an unfair race condition where Player 2 wins simply because Player 1's script happened to execute a millisecond earlier.
>
> We solved this through **Deferred End-of-Frame Arbitration** in `MatchManager.cs`:
> 1. When a fighter's health reaches zero, we do not end the round immediately. Instead, we initiate a coroutine that waits for `yield return new WaitForEndOfFrame()`.
> 2. This guarantees that all hitboxes, physics collisions, and damage events within that frame have fully resolved.
> 3. We then inspect both fighters' health states:
>    - If **both** are at 0 HP, we declare a **Double K.O.**: both fighters collapse into slow-mo collapse simultaneously, and both players are awarded 1 point (`p1RoundWins++`, `p2RoundWins++`).
>    - If only one is dead, the surviving player is awarded the win.
>
> For time-outs where neither fighter is defeated:
> - When the 199s timer expires, we grant a **+100s Sudden Death Overtime extension**.
> - If both fighters survive overtime, both receive 1 point as an honorable Time-Out Draw, and the tournament proceeds to a sudden-death tiebreaker if tied at match point."*

---

### Question 38: Why was the round timer showing 99 even when the script had 199, and how did you implement the +100s Sudden Death Overtime extension?
**Answer for Judges:**
> *"This was an example of **Unity Scene Serialization Overriding Code Defaults**:
> 1. In Unity, when a script field is serialized in the Inspector, Unity stores that value directly inside the `.unity` scene YAML file. Even if a developer updates the C# script default (`roundDuration = 199f`), loading the scene deserializes the scene's saved value (`roundDuration: 99`).
> 2. We fixed this by synchronizing `roundDuration: 199` directly in `SampleScene.unity` and updating fallback defaults in `HUDController.cs`.
>
> For the **Overtime Extension**:
> - We added an `isOvertime` state boolean in `MatchManager.cs`.
> - In `TickRoundTimer()`, if the timer reaches 0 and both fighters have $> 0\\text{ HP}$, we check `!isOvertime`.
> - We set `roundTimer = 100f`, set `isOvertime = true`, fire `OnOvertimeStarted`, and trigger a screen flash and camera shake.
> - The HUD controller catches this event, changes the timer digits to Warning Gold (`#FFD726`), and updates the round title to 'OVERTIME • SUDDEN DEATH'."*
"""

# 5. 05_DEV_LOG_AND_BUGS.md
doc5 = """
---

### Bug 30: Scene Serialization Timer Desync (99s vs 199s) & Simultaneous Death Single-Winner Race Condition

- **Symptom:**
  1. Round timer in `SampleScene` loaded with 99 seconds instead of the intended 199 seconds per round.
  2. When both fighters killed each other simultaneously with heavy attacks, only one fighter was awarded the round win; the other fighter collapsed with zero points awarded.
  3. When the round timer expired with both fighters alive, the game arbitrarily declared a winner based on slight health percentage rather than granting overtime or an honorable draw.
- **Root Cause:**
  1. **Scene YAML Override:** `SampleScene.unity` had `roundDuration: 99` serialized on the `MatchManager` GameObject, overriding the C# field default.
  2. **First-Come Single-Winner Race Condition:** `HandleP1Death` immediately called `EndRound(2)` and locked `currentMatchState` to `RoundEnd`. Player 2's simultaneous death on the same frame was dropped.
  3. **Premature Time-Up Resolution:** `TimeUp()` immediately compared health percentages without supporting overtime extensions or draw scoring.
- **Resolution:**
  1. **Scene & HUD Timer Synchronization:** Updated `roundDuration` in `SampleScene.unity` to `199` and adjusted HUD fallback timers.
  2. **Deferred End-of-Frame Arbitration:** Replaced immediate death resolution with `EvaluateFighterDefeats()` coroutine (`yield return new WaitForEndOfFrame()`). If both fighters have $\\le 0\\text{ HP}$, `EndRoundDraw(isDoubleKO: true)` is triggered, collapsing both fighters in slow motion and awarding 1 point to each.
  3. **+100s Sudden Death Overtime:** Added `isOvertime` flag. Expiration of regular 199s time grants +100 seconds with audio/visual feedback. If overtime expires and both remain alive, `EndRoundDraw(isDoubleKO: false)` awards 1 point each with zero collapse.
  4. **Sudden Death Tiebreaker Progression:** In `CheckWinCondition()`, tied match point scores (e.g. 2-2) trigger a Sudden Death Final Round (`StartRound()`).
"""

append_to_file("Docs/01_UNITY_FOR_BEGINNERS.md", doc1)
append_to_file("Docs/02_ARCHITECTURE.md", doc2)
append_to_file("Docs/03_FEATURE_BREAKDOWN.md", doc3)
append_to_file("Docs/04_VIVA_AND_INTERVIEW_PREP.md", doc4)
append_to_file("Docs/05_DEV_LOG_AND_BUGS.md", doc5)
