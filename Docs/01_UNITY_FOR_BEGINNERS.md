# 01. Unity & Game Development for Beginners

> **Who this guide is for:** Anyone on the team who is completely new to game development, or who is transitioning from Godot to Unity. No prior game engine experience required!

---

## 1. What Actually Happens When a Game Runs?

A game is not a static program like a calculator or a website. A game is an **infinite loop** running at 60 times per second (or more).

Each turn of that loop is called a **Frame**:

```
        ┌──────────────────────────────────────────────────┐
        │                 THE GAME LOOP                    │
        │                                                  │
        │   1. Read Player Input  (Did player press 'W'?)  │
        │   2. Update Game Logic  (Calculate new speed)    │
        │   3. Simulate Physics   (Apply gravity, bumps)   │
        │   4. Draw to Screen     (Render sprites)         │
        └─────────────────────────┬────────────────────────┘
                                  │ Repeat 60x / second
                                  ▼
```

### Why Delta Time Matters (`Time.deltaTime`)
Computers run at different speeds. One PC might run at 120 frames per second (FPS), another at 30 FPS.
If you say `position.x += 5;` every frame:
- The 120 FPS PC moves the character at $5 \times 120 = 600$ units per second.
- The 30 FPS PC moves the character at $5 \times 30 = 150$ units per second.

That is broken! To fix it, games multiply speed by `Time.deltaTime` (the fraction of a second that passed since the last frame). This guarantees that movement is **frame-rate independent**—the character moves at the exact same physical speed on any machine.

---

## 2. Unity's Core Philosophy: GameObjects & Components

In Unity, everything in your game world follows one fundamental rule:

> **An entity is a `GameObject`. What it can do is defined by its `Components`.**

Think of a `GameObject` as an empty cardboard box. By itself, it does nothing and is completely invisible. You give it properties by dropping **Components** into the box:

```
┌────────────────────────────────────────────────────────┐
│  GameObject: "Fighter_P1_Goku"                         │
│                                                        │
│  ├── Transform         (Where is it in 2D space? X, Y) │
│  ├── SpriteRenderer    (What picture does it show?)    │
│  ├── Rigidbody2D       (Gravity, physics velocity)     │
│  ├── BoxCollider2D     (Physical body that can bump)   │
│  ├── PlayerInputHandler(Reads keyboard keys)           │
│  ├── FighterController (Controls jumping, walking)     │
│  ├── HealthSystem      (Tracks 100 HP, handles damage) │
│  └── EnergySystem      (Tracks Ki and Red Zone)        │
└────────────────────────────────────────────────────────┘
```

If you remove the `Rigidbody2D` component, Goku instantly stops experiencing gravity. If you remove `SpriteRenderer`, he turns invisible but can still walk and bump into walls.

---

## 3. Godot vs. Unity: The "Rosetta Stone"

If you know Godot, Unity is doing the exact same jobs under different names:

| Godot Concept | Unity Equivalent | Plain-English Explanation |
| :--- | :--- | :--- |
| **`Node`** | **`GameObject`** | A thing in the scene hierarchy. |
| **`Node2D` / `CharacterBody2D`** | **`GameObject` + `Transform` + `Rigidbody2D`** | In Godot, a node has built-in type capabilities. In Unity, a GameObject gets capabilities by adding components. |
| **One script per Node** | **Multiple scripts per GameObject** | In Godot, you attach one script to a node. In Unity, you can attach 5 different C# scripts to the same GameObject! |
| **`PackedScene` (`.tscn`)** | **`Prefab` (`.prefab`)** | A saved blueprint of an object that you can instantiate many times. |
| **`_ready()`** | **`Start()`** / **`Awake()`** | Code that runs once when the object is created. |
| **`_process(delta)`** | **`Update()`** | Code that runs every visual frame. |
| **`_physics_process(delta)`** | **`FixedUpdate()`** | Code that runs on a rock-solid, fixed time interval for physics. |
| **Signals (`my_signal.emit()`)** | **C# Events (`event Action`)** | How objects talk to each other without messy direct coupling. |
| **Custom Resource (`.tres`)** | **`ScriptableObject` or JSON** | Pure data containers (like our `goku.json`). |

---

## 4. The 4 Essential Unity Script Lifecycles

Every C# script that inherits from `MonoBehaviour` has special built-in methods that Unity calls automatically:

### 1. `Awake()` — "I am born!"
Runs the split-second the object is created, even before the game officially starts.
- **Use for:** Finding components on the same object (`GetComponent<Rigidbody2D>()`).

### 2. `Start()` — "The match is starting!"
Runs on the very first frame before anything is rendered.
- **Use for:** Setting up relationships with *other* objects (e.g. finding your opponent).

### 3. `Update()` — "What happened this visual frame?"
Runs once per frame (60–144+ times a second).
- **Use for:** Reading player input (`Input.GetKey`), checking timers, non-physics animations.
- **Never put physics forces here** because frame rates fluctuate!

### 4. `FixedUpdate()` — "Physics tick"
Runs at a strictly fixed interval (by default, exactly 50 times per second, independent of visual FPS).
- **Use for:** Applying movement velocity, jumping force, gravity calculations.

### 5. `LateUpdate()` — "After everyone else has moved"
Runs after all `Update()` methods across all scripts have finished.
- **Use for:** Auto-facing (turning towards opponent) and camera following.

---

## 5. How 2D Physics Works in Unity

### A. Rigidbody2D
Gives a GameObject physical presence in the physics simulation.
- **Dynamic:** Subject to gravity, velocity, and forces (Used for our Fighters).
- **Static:** Unmovable, zero gravity (Used for the Arena floor and walls).
- **In Unity 6:** Movement velocity is set using `rb.linearVelocity` (not the older `rb.velocity`).

### B. BoxCollider2D
Defines the invisible boundary of an object for collision.
- **Solid Collider:** Stops things from passing through (e.g., floor prevents fighters from falling into the void).
- **Trigger Collider (`isTrigger = true`):** Does not physically block movement, but fires an alert (`OnTriggerEnter2D`) when something overlaps. (This is how **Hitboxes** and **Hurtboxes** detect punches without acting like brick walls!).

### C. Layers & Tags
- **Tag:** A label string (e.g., `"Ground"`, `"Hurtbox"`) used to quickly identify what an object is.
- **Layer:** A number (0–31) used for physics filtering. For example, Layer 6 is `"Ground"`, so our fighters can ask: *"Am I touching something on Layer 6? If yes, I am allowed to jump."*

---

## 6. Real Example: Tracing a Single Jump in Mystic Combat

Here is the exact journey of what happens when Player 1 presses `W`:

1. **Input Polling (`PlayerInputHandler.cs` inside `Update`):**
   ```csharp
   JumpPressed = Input.GetKeyDown(KeyCode.W);
   ```
   Unity detects the `W` key went down on this frame.

2. **Ground Check (`FighterController.cs`):**
   ```csharp
   IsGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, 0.15f, groundLayer);
   ```
   Unity casts a tiny invisible circle at Goku's feet. It detects the Floor on Layer 6 (`Ground`). `IsGrounded` becomes `true`.

3. **State Transition & Impulse (`FighterController.cs`):**
   ```csharp
   if (inputHandler.JumpPressed && IsGrounded)
   {
       rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
       TransitionTo(FighterState.Jumping);
   }
   ```
   Vertical velocity is set to `12` (loaded from `goku.json`). State changes from `Idle` to `Jumping`.

4. **In the Air:**
   Gravity naturally pulls vertical velocity down. When `rb.linearVelocity.y < 0`, state switches to `Falling`.

5. **Landing:**
   When Goku's feet touch the floor again, `IsGrounded` becomes `true`, and state returns to `Idle`.

---

## 7. Why Does C# Look More Complicated Than GDScript? (The Honest Truth)

If you feel C# looks much heavier than Godot's GDScript, **you are 100% right**. 

Here is why:

### 1. In Godot, You Usually Write "Monolithic" Scripts
In Godot, a typical character has a single `player.gd` file that handles movement, input, health, and animations all in one place:
```
Godot:  [ Player Node ] ──── Attached to ───► player.gd (All logic in 1 file)
```
In commercial Unity games, we follow the **Component Architecture** (Single Responsibility Principle). We split logic into modular components:
```
Unity:  [ Fighter GameObject ]
             ├── PlayerInputHandler.cs   (Only reads keys)
             ├── FighterController.cs    (Only handles states & physics)
             ├── FighterAnimator.cs      (Only handles sprites & FPS)
             ├── HealthSystem.cs         (Only tracks HP)
             └── EnergySystem.cs         (Only tracks Ki & Red Zone)
```
* **Why did we do this?** Because if you change how input works, you can never accidentally break the health system! It is modular and jury-friendly. But it does mean you see 6 files instead of 1.

### 2. C# "Ceremony" (Boilerplate)
GDScript cuts away all ceremony. C# requires strict definitions:
* **Brackets `{ }` and Semicolons `;`:** GDScript uses indentation (like Python); C# uses explicit brackets and semicolons.
* **Access Modifiers (`public` / `private`):** In C#, you must declare whether other scripts are allowed to see a variable.
* **Type Declarations (`int`, `float`, `string`, `Vector2`):** In GDScript you write `var speed = 5.0`. In C# you write `float speed = 5f;`.

---

## 8. The GDScript to C# "Cheat Sheet": Side-by-Side Comparison

Look at how identical the logic really is once you strip away the syntax brackets:

### A. Creating an Inspector Variable
| Godot (GDScript) | Unity (C#) | What it Means |
| :--- | :--- | :--- |
| `@export var move_speed: float = 5.0` | `[SerializeField] private float moveSpeed = 5f;` | Shows up as an editable slider in the Inspector panel. |

### B. Finding a Child or Sibling Component
| Godot (GDScript) | Unity (C#) | What it Means |
| :--- | :--- | :--- |
| `$Sprite2D` or `get_node("Sprite2D")` | `GetComponent<SpriteRenderer>()` | Grabs the visual sprite component on this object. |
| `$CollisionShape2D` | `GetComponent<BoxCollider2D>()` | Grabs the collider component on this object. |

### C. The Lifecycle Functions
| Godot (GDScript) | Unity (C#) | What it Means |
| :--- | :--- | :--- |
| `func _ready():` | `private void Start()` | Runs once when game starts. |
| `func _process(delta):` | `private void Update()` | Runs every frame (for input & animations). |
| `func _physics_process(delta):` | `private void FixedUpdate()` | Runs 50x/sec (for physics & movement). |

### D. Reading Movement Input
| Godot (GDScript) | Unity (C#) | What it Means |
| :--- | :--- | :--- |
| `Input.is_action_pressed("move_right")` | `kb.dKey.isPressed` | True as long as key is held down. |
| `Input.is_action_just_pressed("jump")` | `kb.wKey.wasPressedThisFrame` | True ONLY for the single frame the key was tapped. |

### E. Moving the Character
| Godot (GDScript) | Unity (C#) | What it Means |
| :--- | :--- | :--- |
| `velocity.x = input * speed`<br/>`move_and_slide()` | `rb.linearVelocity = new Vector2(input * speed, rb.linearVelocity.y);` | Sets horizontal physics speed, letting gravity handle Y. |

### F. Signals vs Events
| Godot (GDScript) | Unity (C#) | What it Means |
| :--- | :--- | :--- |
| `signal health_changed(new_hp)` | `public event Action<int> OnHealthChanged;` | Declares a radio broadcast. |
| `health_changed.emit(current_hp)` | `OnHealthChanged?.Invoke(currentHp);` | Broadcasts the message to all listeners (HUD, UI). |

---

## 9. How to View Scripts Easily Beside the Editor in Unity

In Godot, the script editor is embedded right in the center window. In Unity, you have two great options:

1. **The Inspector Quick-Preview (Built-in!):**
   * In Unity's **Project** panel (`Assets/Scripts/...`), single-click any `.cs` file.
   * Look at the **Inspector on the right side**: Unity displays a **full, color-coded preview of the script** right there beside your scene! You don't even have to open VS Code just to read it.
2. **Side-by-Side Windows (The Pro Setup):**
   * Snap Unity to the right half of your monitor (`Windows Key + Right Arrow`).
   * Snap VS Code / Visual Studio to the left half (`Windows Key + Left Arrow`).
   * Now your code is sitting right beside your 2D scene, exactly like Godot!

---

## 10. Unity Prefabs vs Godot `.tscn` Scenes: The Big Mystery Solved!

If you come from Godot, you are used to the golden rule: **"Everything is a Scene."**
* In Godot, `Goku` is a scene (`Goku.tscn`). `Saitama` is a scene (`Saitama.tscn`). The HUD is a scene (`HUD.tscn`).
* You instance `Goku.tscn` into `Main.tscn`.
* In Godot's Scene Tree, you can click on Goku, click the little movie clapper icon (or double-click), and Godot opens a tab showing **ONLY Goku**, where you can visually tweak his colliders and sprite.

### How Unity Does the Exact Same Thing: **Prefabs**
In Unity, the equivalent of a `.tscn` scene file is a **`.prefab` asset**:
* `Assets/Prefabs/Fighters/Goku.prefab` = `Goku.tscn`
* `Assets/Prefabs/Fighters/Saitama.prefab` = `Saitama.tscn`
* `Assets/Prefabs/UI/HUD_Canvas.prefab` = `HUD.tscn`

### How to Tell if an Object is a Real Prefab in Unity:
1. Look at the **Hierarchy** window:
   * **Black / Grey Text with White Cube:** It's just a raw GameObject created in that one scene. If you delete it, it's gone.
   * **Blue Text with a Blue Cube Icon:** It is a **Live Prefab Instance**!
2. **How to Open It in Isolation (Godot-Style Scene Mode):**
   * In the Project tab, double-click `Assets/Prefabs/Fighters/Goku.prefab`.
   * **OR** in the Hierarchy, click the tiny arrow **`>`** next to `Fighter_P1_Goku`.
   * Unity's Scene view will dim into **Prefab Mode** (with a dark blue environment bar at the top). You are now editing *only* Goku in complete isolation, exactly like opening `Goku.tscn` in Godot!
   * Any change you make here automatically updates the game scene, your test scenes, and your teammate's scenes!

---

## 11. TextMeshPro 3D Typography & Vertex Gradients (Godot vs Unity)

In 2D fighting games, flat, single-color text feels dull and amateur. High-impact arcade fighters (like *Dragon Ball FighterZ* and *Street Fighter*) use **multi-color gradient fills** and **3D bevels**.

### How Godot Does It:
In Godot, you attach a `LabelSettings` resource to a `Label` node:
- You configure `font_color`, `outline_color`, `outline_size`, and `shadow_color`.
- To get multi-color gradients in Godot, you typically attach a custom `ShaderMaterial` to the CanvasItem.

### How Unity Does It: TextMeshPro `VertexGradient`
In Unity, `TextMeshProUGUI` has native hardware vertex color blending built directly into the text mesh:

```csharp
TextMeshProUGUI p1Name = GetComponent<TextMeshProUGUI>();
p1Name.enableVertexGradient = true;
p1Name.colorGradient = new VertexGradient(
    new Color(1f, 0.96f, 0.45f, 1f),  // Top-Left: Radiant Sun Gold
    new Color(1f, 0.85f, 0.20f, 1f),  // Top-Right: Pure Super Saiyan Gold
    new Color(1f, 0.45f, 0.05f, 1f),  // Bottom-Left: Blazing Amber Orange
    new Color(0.85f, 0.15f, 0.00f, 1f) // Bottom-Right: Deep Dragon Crimson
);
```

### Why Vertex Gradients are Better than Images for Text:
1. **Zero Texture Memory:** The gradient is computed on 4 vertices per character letter dynamically by the GPU.
2. **Infinite Scaling:** When the screen resolution changes from 1080p to 4K, the text and gradient never pixelate or blur.
3. **Dynamic Animation:** You can pulse or shift the colors in C# code during special states (like Red Zone or Super Attacks).

---

## 12. 2D Sprite Alpha Cutouts & Nearest-Neighbor Scaling (Godot vs Unity)

### Godot Concept vs Unity Concept
- In **Godot**, a `Sprite2D` or `TextureRect` uses a `CanvasItem` material. If an imported image has a white rectangular background, developers sometimes try to use a shader with `discard` or chroma-keying.
- In **Unity**, 2D sprites rely on an unmultiplied or premultiplied **Alpha Channel (RGBA)**. The `TextureImporter` requires:
  - `Texture Type: Sprite (2D and UI)`
  - `Alpha is Transparency: Enabled`
  - `Filter Mode: Point (no filter)` for crisp retro anime pixel art.

### The White-Background Screenshot Dilemma:
When raw screenshots of gems or items are dragged directly into a project, they often contain compression artifacts or near-white boundary pixels (`#F4F4F4` to `#FFFFFF`):
1. **Chroma-Keying Thresholds:** A simple single-color key (`r == 255 && g == 255 && b == 255`) leaves jagged white halos (fringing) around dark facet borders.
2. **Clean Distance Keying:** To make the user's emerald Time Stone look majestic, the background cutout evaluates color variance (`abs(r-g) < 15 && abs(g-b) < 15 && r > 170`) to discard near-white compression artifacts while strictly preserving the deep emerald greens (`#167F00`, `#083A01`) and specular glints.
3. **Point (Nearest-Neighbor) Scaling:** When scaling pixel art gems from 34px to 96px, bilinear filtering turns crisp diamond facets into blurry mush. Point filtering keeps every crystalline facet edge razor-sharp.

---

## 13. Coroutines (`IEnumerator`) vs Godot `await get_tree().create_timer()`

### Godot Concept vs Unity Concept
- In **Godot**, when you want to pause execution for a dramatic K.O. moment or delay before starting the next round, you write:
  ```gdscript
  await get_tree().create_timer(1.8).timeout
  start_next_round()
  ```
- In **Unity C#**, standard methods run synchronously from start to finish on the main thread in a single frame. Calling `System.Threading.Thread.Sleep()` would freeze the entire game window!
  Instead, Unity uses **Coroutines** via the `IEnumerator` return type:
  ```csharp
  StartCoroutine(RoundEndSequence(winnerIndex));

  private IEnumerator RoundEndSequence(int winnerIndex)
  {
      // 1.8 second dramatic pause while game renders and particles play
      yield return new WaitForSeconds(1.8f);

      // Check win condition and start next round
      CheckWinCondition();
  }
  ```

### Why 0ms Instant Round Transitions Break Fighting Games:
If a round ends and immediately resets health on the very same frame:
1. **The Attacker's Hitbox Is Still Active:** The winning punch has not finished its swing, so the resetting opponent immediately gets hit again on frame 1 of the new round!
2. **Knockback Physics Glitch:** Rigidbody velocities applied on the fatal hit launch the newly respawned fighter across the arena.
3. **Player Perception:** Human reaction time requires at least $1.5\text{s}$ to recognize a K.O., see the score update, and mentally reset for the next round. Coroutines give the engine the exact temporal control needed for arcade game flow.

---

## 14. Dual Round Indicators & Match Point Relics (UI Score Systems in Godot vs Unity)

### Godot Concept vs Unity Concept
- In **Godot**, to track rounds in a fighting game, you create an `HBoxContainer` containing two `TextureRect` nodes for each fighter. When a round win signal is received, you update `texture = preload("res://shiny_blue_gem.png")`.
- In **Unity UI (uGUI)**, we instantiate two `Image` components per player (`P1_PointGem_1`, `P1_PointGem_2`, `P2_PointGem_1`, `P2_PointGem_2`) positioned beside their avatar cards. We assign `gem_socket_hollow` (unearned) and `gem_shiny_blue` (earned) directly through serialized `Sprite` references on the [`HUDController`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/HUDController.cs).

### Why 2 Discrete Gem Sockets Beat Plain Text:
1. **Immediate Visual Reading:** In the heat of combat, players can glance peripherally at the HUD and count glowing gems without reading textual numerals like `"WINS: 1"`.
2. **Arcade Authenticity:** Classic fighting games (Street Fighter, Tekken, Guilty Gear) use 2 round sockets for Best of 3 tournaments. Empty sockets create visual anticipation, while filled gems create visual satisfaction.
3. **The Center Championship Relic:** Having an overarching 68×68 center gem that ignites into radiant gold with an active breathing sine-wave pulse ($1.0\times \rightarrow 1.15\times$) during Match Point dynamically communicates high-stakes tension to spectators and players alike.

---

## 15. `[SerializeField]` vs `public` vs `private`: What Serialization Actually Means

### The Diary Metaphor
Think of a C# script as a private diary:
- **`private` variables** are locked inside the diary. No other script can read or modify them, and **Unity's Inspector cannot see them either**.
- **`public` variables** leave the diary wide open on the table. Any other script in the entire game can tamper with them, violating Object-Oriented Programming (OOP) encapsulation.

### What is "Serialization"?
In computer science, **serialization** means converting an active object from computer memory (RAM) into a formatted stream of bytes that can be saved to disk (like in a `.unity` scene or `.prefab` asset file) and reconstructed later when the game boots.

### The Solution: `[SerializeField]`
```csharp
[SerializeField] private float moveSpeed = 6.5f;
```
- **To the C# Compiler:** The variable remains strictly `private` (protected from external scripts).
- **To the Unity Engine:** The `[SerializeField]` attribute instructs Unity's serializer: *"Save this variable to the scene file and draw an editable UI field for it in the Inspector window!"*

> **Godot Equivalent:** In Godot 4, this is the exact same as writing `@export var move_speed: float = 6.5`.

---

## 16. The Unity Inspector Mental Map & Decluttering Guide

### Why Unity Feels Cluttered Compared to Godot
In **Godot**, the architecture follows **"One Node = One Clean Script"**. Clicking a character node displays a neat list of `@export` variables at the top of the Inspector.

In **Unity**, the architecture follows **"One GameObject = A Stack of Many Components"**. Clicking `Fighter_P1_Goku` renders a tall scrolling column containing:
1. `Transform` (Position, Rotation, Scale)
2. `SpriteRenderer` (Sprite graphic, sorting layer)
3. `Rigidbody2D` (Mass, gravity, physics velocities)
4. `BoxCollider2D` (Solid physical boundary)
5. `PlayerInputHandler` (Keyboard polling)
6. `FighterController` (State machine brain)
7. `FighterAnimator` (Sprite flipbook animator)
8. `HealthSystem` & `EnergySystem` (Vitals and Ki tracking)

### 3 Tips to Declutter the Inspector:
1. **Collapse Components:** Click the small triangle **▼** next to `Transform`, `SpriteRenderer`, `Rigidbody2D`, and colliders. They shrink to thin 1-line strips, bringing your gameplay scripts straight to the top.
2. **Use the Inspector Search Bar:** Type `speed` or `health` into the magnifying glass 🔍 at the top of the Inspector. Unity instantly hides all unrelated components and highlights only matching fields!
3. **The Root vs. Child Trap:** If you click a child GameObject (like `Hitbox` or `GroundCheckPoint`), the Inspector will only show that child's components. Always ensure you click the root `Fighter_P1_Goku` or `Fighter_P2_Saitama` to edit main controller variables.

---

## 17. The Math Behind the `f` Suffix in C# (`5.0f` vs `double`)

### Why Do We Need the `f`?
In C#, any literal number written with a decimal point (like `3.14`, `0.5`, or `5.0`) is treated as a **`double`** (a 64-bit precision floating point number) by default.

However, in real-time game engines (Unity, Unreal, Godot), graphic cards (GPUs) and physics calculations (`Vector2`, `Vector3`, `Time.deltaTime`) operate on **`float`** (a 32-bit single-precision number).

### Why Do Game Engines Use 32-bit `float` Instead of 64-bit `double`?
1. **GPU Speed:** Modern graphics hardware processes 32-bit `float` mathematical vectors **twice as fast** as 64-bit `double`.
2. **Memory Footprint:** A `float` consumes 4 bytes, whereas a `double` consumes 8 bytes. In physics engines calculating thousands of collision vectors, 32-bit floats cut memory bandwidth in half.

### The C# Compiler Rule:
```csharp
float speed = 5.5;   // ❌ COMPILE ERROR: Cannot implicitly convert 'double' to 'float'
float speed = 5.5f;  // ✅ CORRECT: The 'f' suffix explicitly marks it as a 32-bit float!
```

---

## 18. `Time.deltaTime` vs. `Time.fixedDeltaTime` & The "Double Delta" Trap

### Why Does Delta Time Exist?
If you move a character with `position.x += 5;`:
- On a **30 FPS laptop**: The character travels $5 \times 30 =$ **150 units per second**.
- On a **120 FPS gaming PC**: The character travels $5 \times 120 =$ **600 units per second** (4x faster!).

Multiplying by `Time.deltaTime` ("the fraction of a second since the last frame") guarantees that movement is **frame-rate independent**—traveling the exact same physical distance in 1 real-world second on any hardware.

### Why Must Physics Use `Time.fixedDeltaTime`? (The Tunneling Disaster)
Physics math (friction, momentum, collision resolution) cannot tolerate fluctuating frame rates:
- If a sudden frame drop occurs (`deltaTime` spikes to $0.2\text{s}$), a high-speed projectile moving at $100\text{ m/s}$ will leap $20\text{ meters}$ in a single step, **tunneling straight through a 2-meter solid wall** without colliding.
- To prevent this, Unity runs physics on a separate, locked clock in `FixedUpdate()` using **`Time.fixedDeltaTime`** (default $0.02\text{s}$ or 50 ticks per second). Every physics tick is identical, guaranteeing rock-solid, reproducible collisions.

### ⚠️ The "Double Delta" Rookie Mistake:
```csharp
// ❌ WRONG: Multiplying velocity by deltaTime
rb.linearVelocity = new Vector2(speed * Time.deltaTime, 0f);
```
**Why this breaks physics:**
Velocity already represents **meters per second**. Unity's physics engine automatically multiplies velocity by `Time.fixedDeltaTime` inside its internal integration loop. If you multiply by `Time.deltaTime` in your script, you multiply by delta **twice**, reducing your character's speed to an imperceptible crawl!

```csharp
// ✅ CORRECT: Assign speed directly in meters per second
rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
```

---

## 19. How to Display Information in Unity (Console vs Gizmos vs UI Text)

Depending on the context, Unity provides 3 distinct display channels:

| Display Channel | Code Example | Where it Appears | Purpose |
| :--- | :--- | :--- | :--- |
| **Developer Console** | `Debug.Log("Health: " + hp);` | Unity Console Window (bottom bar) | Diagnosing logic bugs and printing state transitions. |
| **Scene Gizmos** | `void OnDrawGizmos() { Gizmos.DrawWireCube(...); }` | Scene View (Editor only) | Visualizing invisible boundaries (hitboxes, hurtboxes, ground check circles). |
| **In-Game Text** | `scoreText.text = "HP: " + hp;` (via `TMP_Text`) | On-screen Canvas overlay | Crisp, high-resolution vector text rendered for the player. |

### Adding Text with TextMeshPro (TMP):
1. **Hierarchy:** Right-click $\rightarrow$ **UI** $\rightarrow$ **Text - TextMeshPro** (automatically creates Canvas and EventSystem).
2. **C# Script:**
   ```csharp
   using UnityEngine;
   using TMPro; // Always import the TextMeshPro namespace!

   public class HealthDisplay : MonoBehaviour
   {
       [SerializeField] private TMP_Text healthLabel;

       public void UpdateHealth(int current)
       {
           healthLabel.text = "HP: " + current;
       }
   }
   ```
> **Godot Comparison:** In Godot, you add a `CanvasLayer` node with a `Label` child, and update it in GDScript via `$Label.text = "HP: " + str(current)`.

---

## 20. How Audio Works in Unity (Ears vs Speakers vs Sound Tapes)

Sound in Unity revolves around three essential building blocks:

```
┌─────────────────┐        ┌──────────────────┐        ┌───────────────────┐
│    AudioClip    │ -----> │   AudioSource    │ -----> │   AudioListener   │
│ (The Audio Tape)│        │  (The Loudspeaker)│       │(The Player's Ears)│
└─────────────────┘        └──────────────────┘        └───────────────────┘
```

1. **`AudioClip` (The Sound File):**
   - The actual audio file imported on disk (`.wav`, `.mp3`, `.ogg`).
   - *Analogy:* A cassette tape or record sitting on a shelf. By itself, it produces zero noise until put into a player.

2. **`AudioSource` (The Loudspeaker):**
   - A component attached to any GameObject in the scene that plays audio into the world.
   - You can have dozens of `AudioSource` components simultaneously (e.g. one on Goku, one on Saitama, one on the Camera for BGM).

3. **`AudioListener` (The Player's Ears):**
   - The virtual microphone that receives sounds from all active `AudioSource` components and pipes them to your physical PC speakers or headphones.
   - **Golden Rule:** There must be **exactly ONE** `AudioListener` active in the scene (almost always attached to the `Main Camera`). If missing, your game will be 100% silent!

### Two Different Ways to Play Sounds in Code:

```csharp
// 1. One-Shot Playback (For quick, overlapping Sound Effects: punches, jumps, impacts)
// Multiple punches fired in rapid succession will NOT cut each other off!
audioSource.PlayOneShot(punchHitClip, 1.0f);

// 2. State Playback (For continuous Background Music or looping Ki Meditation Aura)
audioSource.clip = battleMusicClip;
audioSource.loop = true;
audioSource.Play();
```

> **Godot Comparison:**
> - In Godot, an `AudioStreamPlayer` or `AudioStreamPlayer2D` node plays an `AudioStream` resource.
> - In Godot, there is no explicit `AudioListener` component needed for 2D—the engine automatically routes 2D audio listeners to the active viewport camera. In Unity, the `AudioListener` component must explicitly exist on the camera.

---

## 21. State-Driven Audio vs Input-Bound Audio (Fighting Game Audio Decoupling)

A common rookie pitfall in fighting game development is triggering sound effects whenever the player presses an input key:

```csharp
// ❌ ROOKIE MISTAKE: Triggering audio on raw button press
if (Input.GetKeyDown(KeyCode.W))
{
    audioSource.PlayOneShot(jumpSound);
}
```

### Why Raw Key-Press Audio is Buggy:
1. **Airborne Key Mashing:** If the player mashes `W` while falling or in mid-air, the jump sound replays continuously even though the fighter cannot jump again.
2. **Hit-Stun / Frozen Desync:** If the player is frozen, knocked down, or reeling in hit-stun, pressing attack keys will fire punch whooshes even though no punch can physically occur.
3. **Breaks AI & Network Replays:** If the character is controlled by a CPU AI bot, a second player (P2), or a network netcode replay, they do not trigger keyboard inputs, leaving the character completely silent!

### The Solution: State-Driven Audio Architecture
Audio should be triggered **exclusively by state changes** inside the character's State Machine:

```csharp
// ✅ PROPER FIGHTING GAME PATTERN: State-Driven Audio
private void TransitionTo(FighterState newState)
{
    if (CurrentState == newState) return;
    FighterState oldState = CurrentState;
    CurrentState = newState;

    switch (newState)
    {
        case FighterState.Jumping:
            fighterAudio?.PlayJump(); // Only plays when jump physically executes!
            break;
        case FighterState.LightAttack:
            fighterAudio?.PlayLightWhiff();
            break;
        case FighterState.Meditating:
            fighterAudio?.StartKiMeditationAura(); // Starts continuous loop
            break;
    }

    // Landing sound: Triggered when airborne state transitions back to ground
    if ((oldState == FighterState.Falling || oldState == FighterState.Jumping) &&
        (newState == FighterState.Idle || newState == FighterState.Walking))
    {
        fighterAudio?.PlayLand();
    }
}
```

> **Godot Comparison:**
> In Godot, you attach an `AnimationPlayer` or `StateMachine` node. When the state changes to `"Jump"` or an animation event is fired, the node emits a signal that triggers `$AudioStreamPlayer2D.play()`. Input in `_unhandled_input()` only requests state transitions, never audio directly.

---

## 22. Auditioning & Swapping Sounds in the Unity Inspector

Unity includes a built-in waveform player for every imported audio clip:
1. In the **Project Window**, navigate to `Assets/Audio/SFX_Library/`.
2. Select any `.wav` or `.ogg` file.
3. Look at the bottom-right corner of the **Inspector Window**—a waveform viewer with a **Play button** ($\blacktriangleright$) will appear.
4. Click **Play** to listen to the audio directly in the Editor without running the game!
5. To change a fighter's sound, select their prefab in `Assets/Prefabs/Fighters/` (or in the scene Hierarchy) and simply drag-and-drop the chosen audio clip into the corresponding slot on the `FighterAudio` component.

---

## 23. Seamless UI Screen Swapping & Cross-Scene Singletons

### Why Not Create a Separate Scene for Character Select?
In beginner game tutorials, developers often create a new Unity scene for every single screen:
`MainMenuScene.unity` ➔ `CharacterSelectScene.unity` ➔ `CombatScene.unity`.

**The Problem:** Calling `SceneManager.LoadScene()` unloads everything in the current scene. This causes:
1. A noticeable freeze or frame hitch while textures reload.
2. Background music immediately cuts off or restarts from the beginning, breaking arcade continuity.
3. Background graphics (like our cosmic multiverse animation) vanish during the transition.

### The Industry Standard: Single-Canvas Panel Swapping
In professional fighting games (like *Street Fighter* or *Tekken*), the Title Screen and Character Select Screen live inside the **exact same Canvas**:

```
┌─────────────────────────────────────────────────────────┐
│ MainMenu_Canvas (Screen Space Overlay)                  │
│                                                         │
│ ├── Cosmic_Background (Always visible & animated)       │
│ ├── Vignette_Overlay  (Always visible)                  │
│ ├── Menu_Root         (Title logo & 4 arcade options)   │
│ └── CharacterSelect_Root (Fighter cards & stat panels)  │
└─────────────────────────────────────────────────────────┘
```

When the player selects **"1 PLAYER VS CPU"** or **"2 PLAYER LOCAL DUEL"**:
- `menuRoot.SetActive(false);`
- `characterSelectRoot.SetActive(true);`

**Result:** The background and title music continue uninterrupted! The player experiences zero loading screen.

### How Do Selected Characters Travel to the Fight Scene?
When the battle begins, we load `SampleScene.unity`. Since scenes unload regular GameObjects, how does the fight scene know who Player 1 and Player 2 chose?

**The Solution: The Persistent Singleton (`GameManager.cs`)**
Using Unity's `DontDestroyOnLoad(gameObject)`, the `GameManager` survives scene transitions:

```csharp
// Inside CharacterSelectController.cs
GameManager.Instance.P1SelectedCharacterId = "goku";
GameManager.Instance.P2SelectedCharacterId = "saitama";
GameManager.Instance.LoadScene("SampleScene");

// Inside PrototypeBootstrap.cs (in SampleScene)
string p1 = GameManager.Instance.P1SelectedCharacterId; // "goku"
string p2 = GameManager.Instance.P2SelectedCharacterId; // "saitama"
CreateFighter("Fighter_P1_" + p1, p1Spawn, PlayerIndex.P1, p1);
```

> **Godot Comparison:**
> This is identical to Godot's **Autoload Singletons** (e.g. `GameManager.gd` registered in Project Settings). Autoload nodes sit in the root viewport above the current scene, persisting variables across `get_tree().change_scene_to_file()`.







---

## 24. RectTransform Coordinate Math vs. Godot Containers & Runtime Audio

### Why Did UI Elements Overlap in Unity?
In Unity's UI system (`uGUI`), each element is governed by a **`RectTransform`**:
- **Anchors (`min`, `max`):** Define the reference point on the parent container (e.g. `(0.5, 0.5)` for center).
- **`anchoredPosition`:** The (X, Y) offset from that anchor.
- **`sizeDelta`:** The (Width, Height) of the element.

If you place an element at Y = -76 with height 34, its bottom edge is at -76 - 17 = -93px.
If you place the box below it at Y = -205 with height 245, its top edge is at -205 + 122.5 = -82.5px.
Because -82.5 > -93, the top of the lower box penetrates 10.5px right into the upper text!

> **Godot Analogy:**
> In **Godot**, you rarely position UI by raw pixel arithmetic. Instead, you put elements inside a **`VBoxContainer`** or **`MarginContainer`**. Godot automatically reads each child's `custom_minimum_size` and stacks them with automatic separation margins, preventing accidental coordinate collisions.
> In **Unity**, when you manually code or bake `RectTransform` layouts without a `VerticalLayoutGroup`, you must calculate bounding box intervals (Center +/- Height/2) to guarantee a clean void gap!

---

### The `AddComponent` Trap: Why Was Audio Silent?
When you write `fighter.AddComponent<FighterAudio>()` at runtime in C#:
1. Unity creates a clean, default instance of the component.
2. All `[SerializeField]` fields (like `voiceSkillClip`, `jumpClip`, `lightHitClip`) start as **`null`**!
3. If the script relies only on values dragged into the Inspector during edit time, dynamically spawned GameObjects will have no sound clips assigned and will fail silently.

### The Solution: Unity's `Resources` Folder
In Unity, any asset placed inside a folder named **`Resources/`** (anywhere in your `Assets/` tree) is compiled into a special runtime archive:
```csharp
// Loads consecutive_normal_punches.mp3 at runtime without needing Inspector drag-and-drop!
AudioClip clip = Resources.Load<AudioClip>("Audio/Anime_Powers/consecutive_normal_punches");
```
By adding `EnsureClipsLoaded()` into `FighterAudio.cs`, the audio component automatically falls back to `Resources.Load()` if serialized inspector slots are empty, guaranteeing that fighters spawned from Character Select or during Mirror Matches always have their voices and hits ready to play!

---

## 25. Pixel Art Contrast, Sprites, and Background Readability (Why Do Outlines Blend In?)

### What is Relative Luminance?
In digital games, human eyes perceive color brightness primarily through **luminance** ($Y$):
$$Y = 0.299 \cdot R + 0.587 \cdot G + 0.114 \cdot B$$
Notice that green contributes $\sim 59\%$ to brightness, red $\sim 30\%$, and blue $\sim 11\%$.

### Why Did Saitama's Flurry Punches Disappear "Sometimes"?
- **The Center of the Arena:** The arena stage floor has a warm spotlight (`#FBA782`, luminance $\sim 190$).
- **The Outer Flanks of the Arena:** The stage floor tiles and background walls are dark slate and navy (`#1A1F25`, luminance $\approx 28$).
- **Saitama's Original Outlines:** Were colored dark charcoal-red (`#140505`, luminance $\approx 9.5$).

When Saitama punched in the spotlight, a dark outline against a bright floor created an easy $20:1$ contrast ratio. But when he moved toward the dark edge of the screen, a luminance of $9.5$ against a background of $28$ yielded a contrast ratio of less than $1.3:1$! The human eye physically cannot distinguish the boundary, so the outline seemed to vanish into thin air.

### The Fix: Dual-Tier Anime Cel-Shading
In retro fighting games and anime fighters:
1. **Perimeter Inking:** Outlines bordering transparent space must be solid, pure dark ink (`#080404` / `#000000`) so they never bleed into background pixels.
2. **Kinetic Knuckle Highlights:** The leading front edge of every striking fist gets an explosive anime white/gold flash (`#FFFFFF` and `#FFD76E`). Because white has a maximum luminance of $255$, it creates an $>8:1$ contrast ratio against ANY dark tile, ensuring punches pop with cinematic energy wherever the fighter stands.

> **Godot Comparison:**
> In **Godot**, developers frequently face the same issue when 2D pixel art sprites are placed in front of dark `TileMapLayer` or `ParallaxBackground` nodes. Godot users often solve this using a `CanvasItemMaterial` with a custom fragment shader that samples 4-neighbor pixels to create an outline or outer glow, or by baking high-contrast pixel art highlights directly into `SpriteFrames` of an `AnimatedSprite2D`.

---

## 26. Pausing Games, Time.timeScale, and Unscaled Input in Unity vs Godot

### How Does Pausing Work in Unity?
To pause a game in Unity, developers set:
```csharp
Time.timeScale = 0f;
```
When `Time.timeScale = 0f`:
1. **`FixedUpdate()` Physics Stop:** Rigidbodies stop moving, collisions stop calculating, and velocity is completely preserved.
2. **`Time.deltaTime` Becomes `0.0`:** Any movement code multiplying by `Time.deltaTime` automatically halts.
3. **`WaitForSeconds()` Coroutines Freeze:** Any coroutine waiting on `WaitForSeconds` is paused until `Time.timeScale` is restored to `1.0`.

### The Two Classic Rookie Traps:
1. **`Update()` Keeps Running:**
   - Unity's `Update()` function **still executes every frame** even when `Time.timeScale = 0`!
   - If your player script checks `Keyboard.current.spaceKey.wasPressedThisFrame` in `Update()`, pressing Space to select an option in the pause menu will register as a jump attack when unpaused!
   - *The Fix:* Add an early exit in your input handler: `if (Time.timeScale == 0f) return;`.
   - *Lifecycle Input Gating:* In addition to pause, fighters should not accept inputs before the match starts or after K.O. Gating input with `MatchManager.CurrentMatchState == RoundInProgress` guarantees clean finish poses.
2. **Audio Stops or Stutters:**
   - Calling `AudioListener.pause = true` pauses all audio globally.
   - If your pause menu needs to play retro button clicks or navigation bleeps, set `audioSource.ignoreListenerPause = true;` on the menu's `AudioSource`.

### Sliding UI Animations in Pure C# (No Plugins Needed)
To slide a menu from off-screen ($Y = 950\text{px}$) into the center ($Y = 0\text{px}$):
```csharp
float t = Mathf.Clamp01(elapsed / duration);
// Cubic Ease-Out: 1 - (1 - t)^3
float ease = 1f - Mathf.Pow(1f - t, 3f);
rectTransform.anchoredPosition = Vector2.Lerp(new Vector2(0, 950), Vector2.zero, ease);
```
Using `Time.unscaledDeltaTime` ensures the tween runs smoothly at 60 FPS even when `Time.timeScale` is frozen!

> **Godot Comparison:**
> In **Godot**, pausing is handled via `get_tree().paused = true`.
> Every Node in Godot has a **`process_mode`** property:
> - `PROCESS_MODE_INHERIT` / `PAUSABLE`: The node stops processing when paused (like fighters in Unity).
> - `PROCESS_MODE_ALWAYS`: The node continues processing even when paused (used for Pause Menus and UI in Godot).
> For UI slide animations, Godot developers use `create_tween().tween_property(panel, "position:y", 0.0, 0.65).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)`. In Unity, our custom coroutine provides the exact same high-performance tween with 0 external dependencies!


---

## 27. Game AI Fundamentals: Decoupling Input and Hierarchical State Machines (HFSM)

### Why Can't the AI Just "Tell the Character What to Do"?
In beginner tutorials, people often write AI by directly calling character functions:
```csharp
// ❌ ROOKIE MISTAKE: AI directly overrides physics and bypasses game rules!
if (playerNearby) {
    fighter.transform.position = player.position; // Teleport!
    fighter.PlayAttackAnimation(); // Cheats attack cooldowns!
}
```
This causes major game-feel bugs:
1. **Unfair "Input Reading" & Cheating:** The AI attacks without obeying recovery frames, startup delays, or Ki costs.
2. **Coupling Nightmare:** If you change how walking or jumping works in `FighterController`, you have to rewrite the AI from scratch.

### The Professional Game Dev Solution: The "Virtual Controller"
Instead of letting the AI bypass rules, we create an **Input Interface** (`IFighterInput`):
- `MoveInput`
- `JumpPressed`
- `LightAttackPressed`
- `HeavyAttackPressed`
- `SkillPressed`
- `UltimatePressed`
- `MeditateHeld`

Both **Humans** and **CPUs** must feed through this exact same interface!
```
[Physical Keyboard]  ──▶ [PlayerInputHandler] ──┐
                                                 ├──▶ [IFighterInput] ──▶ [FighterController]
[Tactical CPU Brain] ──▶ [CpuFighterBrain]   ──┘
```
The `FighterController` does not even know if it is being played by a human or an AI. The AI has to obey the exact same gravity, startup frames, hitbox ranges, and Ki exhaustion rules as the player.

### What is a Hierarchical Finite State Machine (HFSM)?
A flat state machine only has one layer (`Idle`, `Walk`, `Punch`). In fighting games, this feels robotic.
A **Hierarchical FSM** splits thinking into two layers:
1. **Layer 1: Macro Strategy (High-Level Posture):**
   - *"Am I winning or losing? Should I rush down, keep my distance, or back off to charge Ki?"*
2. **Layer 2: Micro Actions (Concrete Button Pressing):**
   - *"I decided to rush down; now I press [D] to walk forward, and when within 1.0m, I tap [Light Attack]."*

### Humanizing the Bot (No Frame-0 Cheats)
If a computer reacts in $0\text{ms}$, players feel cheated. We introduce:
1. **Reaction Delay Tick ($\Delta t \approx 0.14\text{s} - 0.40\text{s}$):** The AI only evaluates strategic decisions every few hundred milliseconds, matching human reflex windows.
2. **Hesitation / Mistake Rate:** A percentage chance on Easy difficulty where the bot pauses in neutral or drops a combo confirm.

> **Godot Comparison:**
> In **Godot**, you achieve this by giving your `CharacterBody2D` a generic child node or Resource for `input_provider`. In 2P mode, it points to a script reading `Input.get_vector()`. In Single-Player mode, you swap that node for an `AIController.gd` node that emits virtual vectors and action flags into `_physics_process()`.

---

## 28. Dynamic Prefab Instantiation vs. Scene Hardcoding (The Fighting Game Stage Pattern)

### The Rookie Trap: Leaving Characters in the Scene
When prototyping in Unity, developers often drag `Goku` and `Saitama` directly into the scene hierarchy.
Why this breaks fighting games:
1. **Broken Character Select:** If Player 1 selects Saitama in the menu, but the scene already contains a baked `Goku`, the game will either spawn duplicates or completely ignore the player's choice!
2. **The Deferred `Destroy()` Race Condition:** Calling `Destroy(oldFighter)` in Unity does **not** destroy the object immediately—it flags it to be removed at the end of the frame. Any script calling `GameObject.Find()` in `Awake()` or `Start()` will still find the old, discarded fighter!

### The Solution: Clean Arena + Dynamic Deployment
An arena scene (`SampleScene`) should be a clean stage: floor, backdrop, boundary walls, lights, camera, HUD, and referee (`MatchManager`).
When the scene loads:
1. The **Deployer** reads `GameManager.Instance.P1SelectedCharacterId` and `P2SelectedCharacterId`.
2. It calls `Resources.Load<GameObject>("Prefabs/Fighters/" + charId)`.
3. It spawns the exact selected prefabs at the spawn markers:
   ```csharp
   GameObject p1 = Instantiate(p1Prefab, p1SpawnPos, Quaternion.identity);
   p1.name = "Fighter_P1";
   ```
4. It immediately hands the instantiated references directly to `MatchManager` and `HUDController`:
   ```csharp
   matchManager.SetFighterReferences(p1Health, p2Health, p1Energy, p2Energy);
   hudController.SetFighterReferences(p1Health, p2Health, p1Energy, p2Energy);
   ```

> **Godot Comparison:**
> In **Godot**, this is identical to keeping `Arena.tscn` free of hardcoded players. Instead, `Arena.gd` reads `GameManager.p1_character` in `_ready()`, calls:
> ```gdscript
> var p1_node = load("res://Fighters/" + char_id + ".tscn").instantiate()
> $SpawnP1.add_child(p1_node)
> ```
> and directly connects the child node's signals to the HUD.

---

## 29. Dynamic Key Rebinding, Combined Local Duel Tables & Audio Mixer (Godot vs Unity)

### The Problem: Hardcoded Keyboard Keys & Shared Keyboard Chaos
In PC fighting games, players have very different setups:
1. **Solo Player (Vs CPU & Future LAN):** The player has the entire keyboard to themselves and wants comfortable single-hand or split layouts.
2. **Local 1v1 Duel (Shared Keyboard):** Two players sit side-by-side on the same keyboard. If Player 1 uses `W/A/S/D` and Player 2 also uses keys that collide, neither can fight.
3. **Hardcoding Rookie Mistake:** Writing `if (Keyboard.current.aKey.isPressed)` directly in fighter scripts locks players into one fixed control scheme.

### The Solution in Mystic Combat:
1. **`InputSettingsManager.cs`**:
   - Stores two distinct profiles:
     - **Single Player Profile (9 Keys):** Full keyboard freedom (Vs CPU & LAN).
     - **1v1 Local Duel Profile (18 Keys):** Split keyboard across Player 1 (Left) and Player 2 (Right).
   - Saved persistently to disk via `PlayerPrefs` (`MC_Bind_SP_*`, `MC_Bind_DuelP1_*`, `MC_Bind_DuelP2_*`).
2. **The Combined Table UI Pattern:**
   - Rather than dividing the screen into two disconnected boxes for P1 and P2, we use a **3-column combined table**:
     `[ Fighter Action ] | [ Player 1 Key ] | [ Player 2 Key ]`
   - Keeps both players' mappings vertically aligned so conflicts can be spotted instantly.
3. **Interactive Key Interception:**
   - Clicking a key button switches it to `[ PRESS KEY ]`.
   - In `Update()`, the controller monitors `Keyboard.current.allKeys`, binds the first key pressed, and saves it immediately.
4. **Top Audio Sliders:**
   - Volume sliders (`Music BGM` and `SFX Sound`) live directly at the top of the modal for instant adjustment with live `%` readouts and real-time audio playback update.

> **Godot Comparison:**
> In **Godot**, actions are registered in `ProjectSettings` (`input/*`). Runtime rebinding is done with:
> ```gdscript
> InputMap.action_erase_events(action_name)
> var new_ev = InputEventKey.new()
> new_ev.physical_keycode = key
> InputMap.action_add_event(action_name, new_ev)
> ```
> Settings are saved to `user://settings.cfg` using `ConfigFile`. In Unity, `InputSettingsManager` maps `FighterAction` enum to `UnityEngine.InputSystem.Key` controls and persists them via `PlayerPrefs`.

---

## 30. Hierarchical Sub-Menu Flow & Arcade Visual Navigation (Godot vs Unity)

### The Beginner Trap: Squeezing Everything Onto One Static Screen
When beginners build menus in fighting games, they often try to fit every slider, checkbox, and keybinding into one single view.
- **The Consequence:** To fit volume sliders, descriptions, and 18 key rows simultaneously, UI text must be shrunk down to $12\text{pt}\text{–}14\text{pt}$. On standard 1080p and 4K displays, players literally have to squint to read their own key bindings!
- **The Rookie Mistake on Navigation:** Forgetting visual focus indicators, then pasting ugly text like *"Use W/S to move, Enter to choose, Esc to leave"* across the screen.

### The Solution: Hierarchical Sub-Menus + Obvious Visual Navigation
1. **Window Enlargement:** Expanded the Options window to **$1440 \times 880\text{px}$** with an **$80\%$ font size increase** ($24\text{pt}\text{–}36\text{pt}$ typography everywhere).
2. **Hierarchical Screen Flow:**
   - **Screen 1 (Main Hub):** Contains only the big volume sliders, sub-menu entry buttons, and lower reset/exit buttons.
   - **Screen 2 (Single Player Sub-Screen):** Opens full-window when selected, dedicating the entire screen to 9 large, readable action rows ($24\text{pt}$).
   - **Screen 3 (1v1 Local Duel Sub-Screen):** Opens full-window when selected, providing massive horizontal space for the 3-column combined table.
3. **Making Navigation Obvious (No Prompt Clutter):**
   - **Blinking Arcade Cursor (`►`):** Precedes the active item (`►  MUSIC BGM`, `►  CONFIGURE SINGLE PLAYER KEYS`).
   - **Pulsing Neon Gold Focus Frame:** Uses a sine wave (`Mathf.Sin(Time.unscaledTime * 7f)`) to smoothly pulse the active element's `Outline` component between Radiant Gold (`#FFD54F`) and Pure White (`#FFFFFF`).
   - Any player looking at the screen instantly knows what is selected without reading a single tutorial prompt.

> **Godot Comparison:**
> In **Godot**, you manage focus using `focus_mode = FOCUS_ALL`, `StyleBoxFlat` with custom border color on focus, and the `focus_entered` signal. You toggle sub-menus by hiding `MainPanel.visible = false` and setting `SubPanel.visible = true`. In Unity, `OptionsMenuController` manages active indices, updates the `►` text prefix, and pulses the `Outline` component in `Update()`.

---

## 31. Dynamic Fighter Deployment vs. Scene Transition Lifecycles (Godot vs Unity)

### The Beginner Trap: Static Scene Instances vs. Dynamic Character Spawning
When beginners create fighting games, they often place characters directly into the scene editor (`SampleScene.unity`).
- **The Consequence:** When you build a Character Selection screen (Goku vs. Saitama), your selection never works in the actual match! Because `SampleScene.unity` already has pre-baked fighters serialized on disk, the game ignores your Character Select choices and always spawns the hardcoded characters.
- **The Follow-Up Rookie Mistake:** Beginners delete the static fighters from the scene file, but then enter Play Mode and discover an **empty arena** with no fighters at all!

### Why Did the Arena Load Empty? (The Scene Transition Lifecycle Trap)
In Unity, understanding *when* scripts execute across scenes is critical:
1. **`[RuntimeInitializeOnLoadMethod]` Only Fires Once:**
   - Many developers use `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` to auto-spawn fighters.
   - **The Trap:** This attribute runs **exactly once** when the game starts. If your game begins in `MainMenuScene`, the method fires while in the main menu (where fighters shouldn't spawn). When the player later selects a character and loads `SampleScene`, that method **never fires again**!
2. **The `SceneManager.sceneLoaded` Active Scene Gotcha:**
   - When transitioning scenes via `SceneManager.LoadScene("SampleScene")`, Unity triggers the `sceneLoaded` callback.
   - **The Trap:** When `sceneLoaded` fires, `SceneManager.GetActiveScene()` is **still pointing to `MainMenuScene`** until Unity finishes processing all scene callbacks! If your deployer script checks `if (SceneManager.GetActiveScene().name == "MainMenuScene") return;`, it erroneously aborts, thinking it is still inside the main menu!
3. **Instantiating Into the Wrong Scene:**
   - Calling `Instantiate(prefab)` without specifying a parent or scene drops the GameObject into whatever scene Unity currently considers active (`SceneManager.GetActiveScene()`). If `MainMenuScene` is still technically active during the transition, the new fighters are spawned into `MainMenuScene` right before `MainMenuScene` is destroyed and unloaded from memory!

### The Solution in Mystic Combat:
1. **Listen to `SceneManager.sceneLoaded` & `AfterSceneLoad`:**
   - Listen to `SceneManager.sceneLoaded` for transitions (e.g. Main Menu $\to$ Arena).
   - Also listen to `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` so developers can press Play directly inside `SampleScene` during debugging without going through the Main Menu.
2. **Explicit Scene Migration (`SceneManager.MoveGameObjectToScene`):**
   - When instantiating the deployer or fighter prefabs, explicitly set the active scene and move objects into the target combat scene:
     ```csharp
     SceneManager.SetActiveScene(scene);
     SceneManager.MoveGameObjectToScene(fighterInstance, scene);
     ```
3. **`MatchManager` Readiness Handshake (`MatchState.WaitingToStart`):**
   - In `MatchManager.Start()`, if fighters are not yet in the scene, the match enters `MatchState.WaitingToStart`.
   - As soon as the deployer spawns the fighters and calls `SetFighterReferences(p1Hp, p2Hp, p1Ki, p2Ki)`, `MatchManager` smoothly transitions to starting Round 1.

> **Godot Comparison:**
> In **Godot**, scene changing is handled by `get_tree().change_scene_to_file("res://Scenes/Arena.tscn")`. To spawn dynamic characters:
> ```gdscript
> var fighter_scene = load("res://Prefabs/Goku.tscn")
> var fighter = fighter_scene.instantiate()
> arena_node.add_child(fighter)
> ```
> In Godot, `add_child()` explicitly dictates which branch of the SceneTree receives the node, completely avoiding Unity's `GetActiveScene()` ambiguity. In Unity, `SceneManager.MoveGameObjectToScene()` provides that exact explicit placement.

---

## 37. Audio Mixing: Linear vs Logarithmic Volume, Envelopes & Dynamic Range

Balancing audio in a 2D fighting game is critical for game feel. If menu clicks or vocal shouts are too loud, they drown out the background music (BGM) and cause instant ear fatigue.

### 1. Unity Volume vs Godot Decibels
- **Unity AudioSource:** Uses a normalized **linear amplitude scale** from `0.0f` (silent) to `1.0f` (full volume).
  - Passing `volumeScale = 0.50f` to `audioSource.PlayOneShot(clip, volumeScale)` directly cuts the sound wave amplitude in half (a $50\%$ drop).
  - Passing `volumeScale = 0.70f` produces a $30\%$ drop.
- **Godot Audio Buses:** Godot uses a **decibel scale (dB)** where $0\text{ dB}$ is full volume and $-80\text{ dB}$ is silence.
  - In Godot GDScript, you convert between scales using `linear_to_db(0.5)` or `db_to_linear(-6.0)`.

```
Unity Linear:  0.0f ───────────── 0.50f ───────────── 1.00f (Max Headroom)
Godot Decibel: -80 dB ─────────── -6 dB  ──────────── 0 dB (Unity 1.0f Equivalent)
```

### 2. Eliminating Audio "Pops" and "Clicks" with Envelopes
When generating procedural sound effects (like retro 8-bit blips or menu boops), instantly cutting a waveform to zero creates an abrupt discontinuity in speaker voltage, which our ears hear as a harsh **"pop"** or **"click"**.
- **The Solution (Amplitude Decay Envelope):** Multiply the waveform by a linear decay factor:
  ```csharp
  // Without envelope: harsh click at end of sound
  sample = Mathf.Sign(Mathf.Sin(frequency * t));

  // With linear decay envelope: smooth fade to silence
  float decay = 1.0f - (t / duration);
  sample = Mathf.Sign(Mathf.Sin(frequency * t)) * decay;
  ```

### 3. Master Hierarchy & Per-Sound Calibration
In Mystic Combat:
1. **Master BGM:** Defaults to $100\%$ (`1.00f`) so the 90s arcade music delivers maximum dynamic range without being artificially muffled.
2. **Menu Navigation SFX:** Reduced from `0.45f` down to `0.22f` ($51\%$ volume reduction) and shaped with a smooth decay envelope, keeping menu interactions crisp without piercing the player's ears.
3. **Character Move-Slot Audio Routing:**
   - **Goku:** Has no special skill; his signature **Ultimate Finisher** (`Q` / `O`, Ki: 80) is **Kamehameha**, animated via `ultimate_0..9` and voiced via `voiceUltimateClip` (`goku_voice_kamehameha`).
   - **Saitama:** Has Consecutive Normal Punches on **Skill** (`E` / `U`, `voiceSkillClip`) and Serious Punch on **Ultimate** (`Q` / `O`, `voiceUltimateClip`).
4. **Vocal Acoustic Gain Calibration:**
   - **Goku Kamehameha (`goku_voice_kamehameha.wav`):** Has an extremely high raw digital recording ceiling (Peak `0.906`, RMS `0.262`). To prevent ear fatigue, its multiplier is calibrated to `0.50f` with a hard safety ceiling.
   - **Saitama Special Shouts (`consecutive_normal_punches.mp3` & `serious_punch.mp3`):** Recorded with softer vocal gain. Their multipliers are boosted to `2.20f` (Skill) and `2.50f` (Ultimate) so they punch through loud physical hit impacts!
5. **Clean One-Shot Master Scaling (No Double Multiplication):**
   - In Unity, `audioSource.PlayOneShot(clip, volumeScale)` produces `audioSource.volume * volumeScale`.
   - By keeping the underlying `AudioSource.volume` at `1.0f` and applying `InputSettingsManager.SFXVolume` directly inside `PlayOneShot()`, the system avoids accidental cascading attenuation.

---

## 38. Sprite Baselines, Dynamic Hurtbox Scaling & AI Action Cadence (Godot vs Unity)

### 1. The Sprite Bouncing Trap: Baseline Alignment
In 2D fighting games, characters switch between different animation states: `Idle`, `Walk`, `Punch`, `Skill`, and `Ultimate`.
- **The Rookie Mistake:** Cropping each animation frame tightly around the character's bounding box to save file size.
  - For example, if an idle sprite is $47 \times 47\text{px}$, but a special blast sprite is $90 \times 79\text{px}$ with $20\text{px}$ of empty canvas below the feet:
  - When the game switches to the blast animation, the character's feet pop $20\text{px}$ upward into the air! They appear to bounce like a pogo stick.
- **The Professional Solution:** Standardize all frames onto a fixed canvas ($128 \times 128\text{px}$) with the ground contact point locked to an exact baseline row (`y = 104`, bottom padding $23\text{px}$). The pivot `(0.5f, 23/128)` then applies uniformly to every single frame, guaranteeing zero jumping or jittering.

### 2. Dynamic Hurtbox Scaling (Why Ducking Must Lower the Collider)
In fighting games, crouching is used to duck under high attacks.
- If your `Hurtbox` child GameObject has a static `BoxCollider2D` that stays $1.8\text{m}$ tall, crouching changes the visual sprite but leaves the vulnerable zone standing up in the air!
- In `FighterController.TransitionTo()`, whenever a fighter enters `FighterState.Crouching`, the hurtbox height is scaled from $1.8\text{m}$ down to $0.95\text{m}$ ($y = 0.48\text{m}$). Returning to `Idle` restores it to $1.8\text{m}$. Ducking now physically dodges high strikes!

### 3. The 60 FPS AI Probability Trap
In `Update()`, code runs 60 times every second.
- **The Trap:** Writing `if (Random.value < 0.15f) JumpPressed = true;` inside `Update()`.
  - Beginners assume this means a $15\%$ chance to jump.
  - In reality, evaluating a $15\%$ chance 60 times a second produces an expected **9 jumps every second**! The bot jumps constantly like a maniac.
- **The Solution:** Use explicit cooldown timers (`jumpCooldownTimer`, `attackCooldownTimer`). The AI evaluates actions at controlled intervals ($1.8\text{s}\text{–}3.0\text{s}$ between tactical jumps; $0.25\text{s}\text{–}0.50\text{s}$ between attack strikes), producing human-like arcade pacing.

> **Godot Comparison:**
> In **Godot**, you handle dynamic hurtboxes by adding a track for `CollisionShape2D:shape:size` directly inside the `AnimationPlayer` resource, adjusting the collider shape frame-by-frame on the timeline. In Unity, scripts update `BoxCollider2D.size` and `offset` during state transitions. For AI cadence, Godot typically uses a `Timer` node or a `yield(get_tree().create_timer(delay), "timeout")` pattern.

---

## 39. Event Race Conditions & Audio Self-Healing in Scene Transitions (Godot vs Unity)

### The Beginner Trap: Relying 100% on Events Without State Recovery
In Unity, many systems communicate using C# events:
- `MatchManager.OnRoundStart += HandleRoundStart;`
- **The Trap:** When transitioning scenes dynamically, `PrototypeBootstrap` deploys fighters and immediately starts Round 1.
  - If `OnRoundStart?.Invoke(1)` fires during the scene load step, `MatchAudioController` might not have finished its `OnEnable()` subscription yet.
  - The event fires into empty air. `MatchAudioController` misses the notification completely!
  - If `MatchAudioController.Start()` is empty, it assumes an event will arrive in the future. Result: Round 1 has **zero background music**, but Round 2 works because the listener is now subscribed.

### The Professional Solution: Self-Healing Handshake
Never rely solely on an event if the underlying system has a persistent state!
1. In `Start()`: Inspect the current state of `MatchManager`:
   ```csharp
   if (matchManager.CurrentMatchState == MatchState.RoundInProgress && !musicSource.isPlaying)
   {
       PlayBattleMusic(resetToStart: true);
   }
   ```
2. In `Update()`: Add a failsafe check that recovers playback if music was stopped or missed.
3. This guarantees that whether the event fires early, on time, or late, the music **always plays**.

> **Godot Comparison:**
> In **Godot**, background music is almost always placed on an **Autoload Singleton** (e.g., `BGMPlayer.tscn`). Because Autoload nodes live in the root `/root/` SceneTree, they are **never destroyed or reloaded** when `change_scene_to_file()` runs. In Unity, scene-local audio controllers must implement self-healing checks in `Start()` to bridge asynchronous transition timing gaps.











---

## 40. TimeScale Corruption, Nested Coroutines & Knockback Vectors (Godot vs Unity)

### The Beginner Trap: Storing `Time.timeScale` Inside an Interrupted Coroutine
In fighting games, "HitStop" (a micro-freeze of $0.05\text{s}\text{--}0.08\text{s}$ upon hit impact) gives attacks visceral weight.
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
1. Player 1 hits Player 2 $\rightarrow$ Coroutine 1 starts. `prevScale` is recorded as `1.0f`. `Time.timeScale` becomes `0.04f`.
2. Player 2 hits Player 1 on the exact same frame or during the hitstop $\rightarrow$ `StopCoroutine(activeHitstop)` cancels Coroutine 1.
3. Coroutine 2 starts. It executes:
   ```csharp
   float prevScale = Time.timeScale; // Time.timeScale is ALREADY 0.04f!
   ```
4. Coroutine 2 finishes and runs:
   ```csharp
   Time.timeScale = prevScale; // Sets Time.timeScale to 0.04f PERMANENTLY!
   ```
5. **Result:** The game runs at $4\%$ speed forever!

### The Solution: Re-Entrant HitStop Management
Never sample `Time.timeScale` while a HitStop is already active!
- Maintain an `isHitStopActive` boolean and cache `originalTimeScale` (defaulting safely to `1.0f`).
- Only sample `originalTimeScale` when `!isHitStopActive` and `Time.timeScale > 0.1f`.
- If an overlapping hitstop arrives, refresh the timer without overwriting the clean baseline.
- Add an `OnDisable()` safeguard so scene transitions or component toggles always restore `Time.timeScale = 1.0f`.

### Knockback Vectors: Why 2D Fighters Need Decoupled X and Y Velocities
- If an attack engine sets `linearVelocity = new Vector2(knockbackX, 3.2f)`, every light jab pops the opponent into the sky with $3.2\text{ m/s}$ vertical lift.
- Light jabs should flinch horizontally ($2.2\text{f}$) with minimal lift ($0.8\text{f}$).
- Multi-hit flurry moves (like Saitama's Consecutive Normal Punches) must have low horizontal knockback ($1.2\text{f}$) and gentle lift ($0.4\text{f}$) so the opponent stays trapped inside the flurry rather than rocketing out on hit 1.

> **Godot Comparison:**
> In **Godot**, you modify `Engine.time_scale` for hitstop, and create a realtime timer via `await get_tree().create_timer(duration, true, false, true).timeout` (`ignore_time_scale = true`). Just like in Unity, if multiple timers overlap, overwriting a local `var prev_scale = Engine.time_scale` causes permanent slow-mo. Godot developers maintain a stateful `HitStopManager` Autoload that preserves the clean baseline scale.

---

## 41. Single-Turn Animation Pacing, Sprite Jumps & Particle Hygiene (Godot vs Unity)

### The Mistake: Looping Non-Loopable Celebrations
In fighting games, characters perform celebratory gestures upon winning a round (e.g., Goku's thumbs-up, Saitama's celebratory jump and fist pump).
A common rookie mistake is setting the animator to `PlayLoop` during a fixed duration hold (e.g., 4.0 seconds):
1. **The Pogo-Stick Trap:** If an animation contains a jump (squat $\to$ leap $\to$ land $\to$ pose), running it in a loop causes the character to continuously jump, land, jump, land over and over like a pogo stick.
2. **The Stagnant Hold:** If the animation is frozen or looped endlessly, the pacing feels sluggish as players wait around doing nothing before the next round begins.

### The Solution: Single-Turn Animation Completion
1. **PlayOnce Architecture:** Set victory animations to `PlayOnce`. The sequence plays from frame $0$ through $N-1$, holding cleanly on the final triumphant frame.
2. **Dynamic Duration Derivation:** Derive the exact round-end transition duration dynamically:
   $$\text{VictoryDuration} = \frac{\text{FrameCount}}{\text{FPS}}$$
3. **Instant Next-Round Transition:** As soon as the animation completes its single turn, `MatchManager` immediately starts the next round. The flow is crisp, dynamic, and arcade-authentic.

### Sprite-Based Jump Offsets vs Physics Leaps
When a character jumps in a post-round celebration:
- **Rookie Approach (Physical Rigidbody Impulse):** Applying physics velocity (`rb.linearVelocity.y = 8f`) can cause the winner to land on top of the fallen opponent's collider, slide awkwardly down slopes, or jitter against boundaries.
- **Pro Approach (Mathematical Canvas Anchoring):** Anchor the sprite frames relative to the ground baseline ($Y = 135$ on a $160 \times 160\text{px}$ canvas):
  - **Squat windup:** Feet at $Y = 135$ (solid ground).
  - **Takeoff:** Feet launch to $Y = 120$ ($15\text{px}$ above ground).
  - **Mid-air Apex:** Feet peak at $Y = 104$ ($31\text{px}$ airborne) while the arm punches the sky.
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


---

## 41. Round Timers, Sudden Death Overtime Extensions & Double-KO Arbitration (Godot vs Unity)

### The Beginner Trap: Resolving Fatal Events Immediately on the First Event Call
In fighting games, two fighters attacking at the same time can both hit and kill each other in the exact same physics frame.
If you wire an immediate callback:
```csharp
private void HandleP1Death() => EndRound(winner: 2);
private void HandleP2Death() => EndRound(winner: 1);
```
- **The Trap:** If Player 1's death event fires first by $1\text{ millisecond}$ (due to internal collection iteration order), Player 2 is immediately declared the winner!
- Player 2's death event is either ignored because `currentMatchState` is already `RoundEnd`, or Player 2 gets awarded a win while collapsing into a corpse on the floor!

### The Professional Solution: End-of-Frame Arbitration
Never crown a winner on the very first death notification!
1. When either fighter dies, schedule a coroutine that waits for `yield return new WaitForEndOfFrame()`.
2. This allows all damage calculations, counter-hits, and health updates across both fighters during that physics frame to resolve cleanly.
3. At the end of the frame, inspect both fighters:
   - If **both** are dead $\rightarrow$ Declare **Double K.O.!** Both fighters collapse into slow-mo knockout, and both players receive $1\text{ point}$.
   - If only one is dead $\rightarrow$ Award the round win to the surviving fighter.

### The Sudden Death Overtime Extension Pattern
- If neither fighter is knocked out when the $199\text{s}$ round timer expires:
  - Do not arbitrarily hand a victory to higher HP!
  - Grant an **Overtime / Sudden Death Extension of $+100\text{s}$**.
  - Flash the HUD timer in Warning Gold (`#FFD726`) and announce Sudden Death.
- If the extra $100\text{s}$ expires and both fighters **still remain undefeated**:
  - The round ends in an honorable **Time-Out Draw**.
  - Both fighters receive $1\text{ point}$ (`p1RoundWins++` and `p2RoundWins++`), neither collapses, and the match proceeds to the next round or sudden-death tiebreaker!

> **Godot Comparison:**
> In **Godot**, you achieve end-of-frame arbitration by using `call_deferred("_evaluate_round_end")` when a `died` signal is received, or checking both fighters' health states inside `_physics_process(delta)` after `move_and_slide()` and hit detection have run for all scene tree nodes.


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
- When an attack beam stays on screen for $1.21\text{ seconds}$ (over 70 frames at 60 FPS), an unshielded trigger collider will deal damage 70 times, instantly annihilating any fighter in its path.
- **Single-Hit AoE Guarantee:** In `HitboxController.cs`, we register hit targets in a `HashSet<Transform> hitTargetsThisSwing`. When an opponent touches any part of the $8.5\text{m}$ beam, they take the $45\text{ damage}$ once, receive normal knockback ($8.5\text{f}, 2.2\text{f}$), and enter hitstun. Subsequent frames ignore them because their transform is already in the set!

> **Godot Comparison:**
> In **Godot**, a beam is commonly built using a `Line2D` with a `Gradient` texture and texture mode `TILE` or `STRETCH`, or an `Area2D` containing child `Sprite2D` nodes with `offset` set to `(width / 2, 0)` to emulate left-aligned pivoting. Single-hit detection is handled by tracking hit bodies in an `Array` during the `body_entered` signal.


---

## 33. TextMeshPro, Font Assets, and the "Solid Block" Bug (Why Text Turned Into Filled Boxes)

### How TextMeshPro Actually Draws Text (Signed Distance Fields - SDF)
Unlike traditional 2D game text which draws low-resolution bitmaps that get blurry when scaled up, modern engines use **Signed Distance Field (SDF)** rendering:
1. When you bake a font in Unity, it doesn't store standard pixels. Instead, it generates a high-resolution gradient texture where each pixel's alpha channel stores the **distance to the nearest edge of the letter**.
2. Inside the letter, distance is high ($1.0$); outside the letter, distance is low ($0.0$). The exact contour/edge of the letter is precisely at $0.5$.
3. When rendering on screen, a special GPU pixel shader samples this texture and applies a threshold function:
   $$\text{alpha} = \text{smoothstep}(0.5 - \text{smoothing}, 0.5 + \text{smoothing}, \text{distance})$$
   Because it's mathematical distance rather than raw pixels, text remains razor-sharp at 4K resolutions or when zoomed in!

### The Three-Part Font Asset Structure
Inside Unity, a TextMeshPro font asset file (e.g., `ArcadeClassic_SDF.asset`) contains **three separate sub-assets** bundled together:
1. **`TMP_FontAsset` (Class 114):** The metadata table containing character codes, glyph sizes, UV coordinates, and line heights.
2. **`Texture2D` (Class 28):** The 1024x1024 grayscale distance-field bitmap.
3. **`Material` (Class 21):** The material running the `TextMeshPro/Distance Field` shader.

### The Rookie Trap: The Disconnected `_MainTex`
When font assets are created or copied improperly, the internal YAML link between the `Material` and the `Texture2D` can break, setting:
```yaml
- _MainTex:
    m_Texture: {fileID: 0}   # Null reference!
```
- When `_MainTex` is null, Unity supplies a default solid white $1 \times 1$ pixel dummy texture.
- Every single pixel sampled by the GPU returns a distance value of $1.0$.
- Because $1.0 > 0.5$, the shader calculates that **every single pixel inside the character's bounding quad is solid ink**.
- **Result:** Instead of rendering the outline of letters like 'P', 'A', 'U', 'S', 'E', 'D', TextMeshPro draws **solid, filled colored rectangles**!

### Why Missing Glyphs Turn Into "Tofu" Boxes
Many retro arcade fonts (like `Arcade Classic` or `Retro Gaming`) only contain uppercase English letters and a few punctuation marks:
- If a menu tries to print characters not present in the font atlas (such as special Unicode symbols like `▶`, `◀`, `★`, `—`, `•`, or missing lowercase letters), TextMeshPro cannot find the glyph.
- If the font's `m_FallbackFontAssetTable` is empty, TMP draws the "missing character" glyph (a square tofu box).
- Combined with a broken material texture, that square tofu box is rendered as a solid colored block!

> **Godot Comparison:**
> In **Godot Engine**, font handling is managed through `FontFile` and `FontVariation` resources. Godot automatically binds MSDF (Multi-channel Signed Distance Field) textures under the hood inside `CanvasItem` without exposing disconnected sub-asset materials. In Godot, you configure fallback fonts using `FontFile.fallbacks = [fallback_font]`. If a glyph is missing, Godot automatically walks down the array to find the character in the fallback font.
