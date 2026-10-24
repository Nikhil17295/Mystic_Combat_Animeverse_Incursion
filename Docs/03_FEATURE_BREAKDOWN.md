# 03. Feature Breakdown & Implementation Details

> **Audience:** Developers and reviewers seeking a deep-dive into how individual game mechanics are coded and executed.

---

## 1. Dynamic Auto-Facing

### The Problem
In 2D fighting games (Street Fighter, Guilty Gear, Smash Bros), fighters must always face each other horizontally, automatically flipping around when one jumps over the other.

### How It Is Implemented
- Located in: [`FighterController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs) inside `LateUpdate()`.
- **Why `LateUpdate()`?** `Update()` and `FixedUpdate()` move the fighters. If we check auto-facing in `LateUpdate()`, we guarantee both fighters have finished moving for this frame before we decide which way they face.
- **Code Logic:**
  ```csharp
  private void AutoFaceOpponent()
  {
      if (opponent == null || spriteRenderer == null) return;

      if (opponent.position.x > transform.position.x)
          spriteRenderer.flipX = false; // Opponent is to the right -> face right
      else if (opponent.position.x < transform.position.x)
          spriteRenderer.flipX = true;  // Opponent is to the left -> face left
  }
  ```

---

## 2. The Ki System & The Signature "Red Zone"

### What is Ki?
Ki is the mystic combat energy (0–100) required to execute special skills and ultimates.
- **No passive regeneration:** Players cannot camp or run away to recharge Ki.
- **How to gain Ki:**
  1. Hitting the opponent (+5 to +10 Ki per strike).
  2. Taking damage (comeback mechanic: +3 to +4 Ki per hit taken).
  3. **Meditation:** Holding the meditate key roots the fighter in place to channel Ki at a rate of 12–15 Ki/sec.

### The Red Zone (Exhaustion Mechanic)
- **Trigger:** If a fighter's Ki drops into the $0 \le \text{Ki} \le 10$ danger range.
- **Penalties:**
  - 5-second locked timer.
  - Movement speed heavily reduced (to $35\%$ of base speed).
  - Cannot execute attacks.
- **Actions Allowed:**
  - Can move slowly.
  - **Can channel Meditation:** Meditating accelerates the exhaustion countdown by $1.5\times$!
- **Comeback Protection:**
  - Every hit taken while in the Red Zone reduces the remaining exhaustion timer by $0.1\text{s}$, helping the player recover faster when getting pummeled.
- **Exit Condition:**
  - When the 5-second timer reaches 0, the fighter exits the Red Zone and immediately recovers **+20 Ki**.

### Code Implementation (`EnergySystem.cs`)
```csharp
public void SpendKi(float amount)
{
    currentKi = Mathf.Max(0f, currentKi - amount);
    OnKiChanged?.Invoke(currentKi, maxKi);

    if (!isInRedZone && currentKi <= redZoneThreshold)
    {
        EnterRedZone();
    }
}

private void TickRedZone()
{
    redZoneTimer -= Time.deltaTime;
    if (redZoneTimer <= 0f)
    {
        ExitRedZone();
    }
}
```

---

## 3. Shared Keyboard Input System

### The Problem
Unity's default input system often experiences key ghosting or missed events when two players simultaneously press multiple keys on a single shared keyboard.

### How It Is Implemented
- Located in: [`PlayerInputHandler.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Input/PlayerInputHandler.cs).
- Uses `UnityEngine.InputSystem.Keyboard.current` for direct raw polling.
- Maps distinct ergonomic physical zones of the keyboard:
  - **Player 1 (Goku):** `W/A/S/D` for movement (`Space` or `W` to jump, `S` to crouch):
    - `F`: Light Attack
    - `C`: Heavy Attack
    - `E`: Skill (Special Attack)
    - `Q`: Ultimate
    - `G`: Meditate (Ki charge)
  - **Player 2 (Saitama):** `I/J/K/L` for movement (`I` to jump, `K` to crouch, `J/L` to move):
    - `H`: Light Attack
    - `N`: Heavy Attack
    - `U`: Skill (Consecutive Normal Punches)
    - `O`: Ultimate (Serious Punch)
    - `M`: Meditate (Ki charge)

---

## 4. Grimoire JSON Loader with Dual Fallback Paths

### The Problem
During development, files are loaded from `Application.dataPath + "/Data/Characters/"`. But when the game is built into a standalone `.exe` release, `Application.dataPath` changes structure and is read-only.

### How It Is Implemented
- Located in: [`CharacterDataLoader.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Data/CharacterDataLoader.cs).
- First checks `Application.streamingAssetsPath + "/Characters/"` (standard path for exported Windows builds).
- If not found, falls back to `Application.dataPath + "/Data/Characters/"` (standard path inside the Unity Editor).
- Uses `JsonUtility.FromJson<CharacterData>()` to deserialize the data directly into strong C# types.

---

## 5. Procedural Sandbox & Arcade View

### Arcade Camera & Character Presence
- In classic 1v1 arcade fighting games (Street Fighter III, Guilty Gear), fighters occupy **40%–50% of the vertical screen height** so hits and expressions feel impactful.
- In [`ArenaSetup.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/ArenaSetup.cs), the Orthographic Camera size is set to `3.2` and centered at `(0, -1.2, -10)`. With fighters standing ~2.0 units tall, they occupy ~45% of the screen height.
- Attached the `arena_stage_background.png` behind the arena as the backdrop.

---

## 6. Multi-Frame Animation System & Foot Baseline Normalization (`FighterAnimator.cs`)

### The Foot Jitter Problem in 2D Fighting Games
When extracting frames from non-uniform spritesheets, each bounding box has different dimensions and vertical padding. If rendered with default center or bottom pivots, the character's feet constantly bounce up and down, sinking into the ground during attacks.

### The Mathematical Fix: Foot Baseline Normalization
Every exported frame is padded onto a standardized canvas with its lowest non-transparent pixel (feet) anchored to an identical baseline:
$$\text{paste\_y} = \text{Baseline\_Y} - \text{Feet\_Y}_{\text{frame}}$$
$$\text{paste\_x} = \frac{\text{Canvas\_Width}}{2} - \text{Center\_X}_{\text{frame}}$$
- **Goku:** Unified $128 \times 128$ canvas, Baseline $y = 105$. Pivot: `(0.5, 0.1797)`.
- **Saitama:** Unified $160 \times 160$ canvas, Baseline $y = 135$. Pivot: `(0.5, 0.1562)`.
Result: 100% zero foot jitter, zero floor sinking, and perfectly grounded physics.

### Distinct Animation Timing Curves (FPS)
Fighting games require specialized frame rates per action rather than a flat global speed:
- **Idle (`6 FPS`):** Calm breathing loop.
- **Walk (`10 FPS`):** Snappy athletic stride.
- **Light Attack (`14 FPS`):** Snappy, instant jab feedback.
- **Heavy Attack (`11 FPS`):** Anticipation windup followed by strong strike.
- **Skill (`18 FPS`):** Blazing fast anime flurry (Saitama's 12-frame Consecutive Normal Punches!).
- **Ultimate (`10 FPS`):** Dramatic slow-motion buildup (Spirit Bomb / Serious Punch).
- **Meditate (`8 FPS`):** Rhythmic Ki aura pulsation.

### Physics-Driven Jump Pose Selection
Instead of playing a looping timer while airborne, jump frames are selected dynamically by checking `rb.linearVelocity.y`:
- $\text{Velocity}_y > 4.0\text{ m/s} \rightarrow$ **Launch frame** (`jump_0`)
- $1.0\text{ m/s} < \text{Velocity}_y \le 4.0\text{ m/s} \rightarrow$ **Ascending leap** (`jump_1`)
- $|\text{Velocity}_y| \le 1.0\text{ m/s} \rightarrow$ **Apex hangtime** (`jump_2`)
- $-5.0\text{ m/s} \le \text{Velocity}_y < -1.0\text{ m/s} \rightarrow$ **Descent** (`jump_3`)
- $\text{Velocity}_y < -5.0\text{ m/s} \rightarrow$ **Fast fall / landing prep** (`jump_4`)

---

## 7. Zero-Setup Rapid Prototyping (`PrototypeBootstrap.cs`)

### Why Build This?
Normally in Unity, testing a game requires:
1. Creating sprites in Photoshop/Aseprite.
2. Slicing textures.
3. Assembling GameObjects in the scene.
4. Dragging components into the Inspector one by one.
5. Saving as prefabs.

`PrototypeBootstrap.cs` automates this entire setup programmatically via `[RuntimeInitializeOnLoadMethod]`:
- Spawns both fighters with proper colliders, rigidbodies, and stats.
- Generates 16x24 pixel-art placeholder textures with a colored body, dark outline, belt, and cyan directional visor.
- Procedurally builds the arena floor (with `Ground` layer), side collision walls, and ceiling.
- Auto-configures the Main Camera to 2D Orthographic, size 6, centered at `(0, 0, -10)`.
- Wire cross-references between the fighters for auto-facing.

**Result:** Any teammate can open any blank scene in Unity, press Play, and instantly test the game without any setup friction!
