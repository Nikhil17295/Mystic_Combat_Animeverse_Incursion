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
