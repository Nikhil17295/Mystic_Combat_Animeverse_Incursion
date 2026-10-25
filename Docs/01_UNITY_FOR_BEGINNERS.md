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




