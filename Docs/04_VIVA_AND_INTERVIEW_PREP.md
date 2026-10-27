# 04. Project Viva & Jury Interview Defense Guide

> **Purpose:** Rehearsal guide for hackathon jury evaluations, technical interviews, and project presentations.
> **Tip for Teammates:** Read the bold answers aloud. You don't need to memorize every line of code—judges look for understanding of *why* choices were made.

---

## 1. The 60-Second Elevator Pitch

> *"Mystic Combat: Animeverse Incursion is a competitive 2D local multiplayer fighting game built in Unity 6 for offline Windows PC. Instead of getting bogged down in complex 3D modeling for a 5-day hackathon, our team prioritized rock-solid combat mechanics, 60-FPS physics, and a modular 'Grimoire' architecture where all frame data, stats, and moves are driven by external JSON files. Our signature feature is 'The Red Zone'—an exhaustion mechanic that punishes reckless energy expenditure and creates thrilling comeback opportunities."*

---

## 2. Core Viva Questions & Model Answers

### Q1: Why 2D pixel art over 3D for a 5-day hackathon?
**Answer:**
> *"In fighting games, frame precision and hitbox accuracy matter infinitely more than 3D polygon counts. By standardizing on Jump Ultimate Stars (JUS) 2D pixel art, we achieved three things:
> 1. Complete visual scale consistency across characters from different franchises (Goku, Saitama, Naruto).
> 2. Elimination of complex 3D rigging, skinning, and lighting overhead.
> 3. Zero frame drops—the physics and render loop stay locked at 60+ FPS even on low-end hardware."*

---

### Q2: What is your "Grimoire Architecture", and why did you choose JSON?
**Answer:**
> *"The Grimoire architecture decouples game logic from character balancing data. Instead of hardcoding numbers into C# scripts or Unity Inspector fields, each character's HP, speed, jump force, frame timings, and hitbox dimensions are stored in external JSON files (`goku.json`, `saitama.json`).
>
> **Why this matters to judges:**
> - A game designer can rebalance frame advantage or damage numbers live without touching code or waiting for Unity to recompile.
> - Adding a 3rd or 4th fighter requires zero C# code changes—just a new JSON file and a spritesheet.
> - Demonstrates data-driven architectural maturity, fulfilling the problem statement's bonus criteria."*

---

### Q3: Why do you read inputs in `Update()` but apply forces in `FixedUpdate()`?
**Answer:**
> *"This is a classic game physics requirement:
> - **Input must be read in `Update()`** because `Input.GetKeyDown()` is only true for a single render frame. If you poll input in `FixedUpdate()`, which runs on a separate clock, you will randomly miss button presses (dropped inputs).
> - **Physics forces must be applied in `FixedUpdate()`** because it runs on a rock-solid, fixed time step (50 ticks/sec). If you apply physics in `Update()`, movement speed fluctuates with screen refresh rates, causing inconsistent jump heights and floaty controls."*

---

### Q4: How do you prevent spaghetti code when multiple systems need to know about player health or Ki?
**Answer:**
> *"We use the **Observer Pattern** with C# `event Action` delegates.
> For example, `HealthSystem` does not know that `HUDController` or `MatchManager` exist. When a fighter takes damage, `HealthSystem` simply raises `OnHealthChanged?.Invoke(current, max);`.
> The HUD subscribes to this event and animates the health bar. If we delete the HUD, the health system never crashes. This strict separation of concerns makes our codebase modular and bug-resistant."*

---

### Q5: Explain "The Red Zone". What competitive fighting game problem does it solve?
**Answer:**
> *"In many fighting games, players spam special moves or camp in corners waiting for passive energy regeneration.
>
> The Red Zone solves this by adding high-stakes risk to energy management:
> - Ki has **zero passive regeneration**; you must earn it through aggressive strikes, taking hits, or risky standing meditation.
> - If your Ki falls below 10, you enter the **Red Zone**: a 5-second exhaustion penalty where you cannot attack and move at 35% speed.
> - Crucially, **comeback protection** is built in: every hit you suffer while exhausted shaves 0.1s off the timer, and meditation accelerates recovery by 1.5x. It turns exhaustion into a nail-biting mini-game."*

---

### Q6: How does auto-facing work, and why is it inside `LateUpdate()`?
**Answer:**
> *"Auto-facing compares the fighter's X coordinate with the opponent's X coordinate:
> - If opponent.x > my.x, `spriteRenderer.flipX = false` (face right).
> - If opponent.x < my.x, `spriteRenderer.flipX = true` (face left).
>
> We put this in `LateUpdate()` because `LateUpdate()` executes after all `Update()` and `FixedUpdate()` movement has finished for that frame. Checking auto-facing earlier could result in 1-frame visual jitter if both players cross each other during the same tick."*

---

### Q7: How do you handle shared keyboard play without key ghosting?
**Answer:**
> *"We poll raw hardware keys (`Input.GetKey`) mapped to physically distinct zones of the keyboard:
> - Player 1 uses the left cluster (`W/A/S/D` + `G/H/J/K/T`).
> - Player 2 uses the right cluster (`Arrows` + `Numpad` or laptop-friendly keys `B/N/M/,//`).
> This avoids USB keyboard matrix ghosting that occurs when players press adjacent keys in the same scanning row."*

---

### Q8: How do you balance "canonically overpowered" characters like Saitama vs. Goku?
**Answer:**
> *"Our design philosophy is **competitive balance over canon lore**.
> In lore, Saitama defeats anyone in one punch. In Mystic Combat, Saitama's *Serious Punch* costs **100 Ki** (his entire bar), has a tiny hitbox, and a long startup animation. If it lands, it is indeed an instant KO; but if it misses, he is left completely drained at 0 Ki and plunged straight into the Red Zone! This honors the character's anime identity while preserving strict competitive fairness."*

---

### Q9: How did you solve sprite misalignment and foot jitter with variable-sized 2D frames?
**Answer:**
> *"In authentic fighting game spritesheets (like Jump Ultimate Stars), attacks have wider, taller bounding boxes than idle stances. If rendered with default center pivots, characters bounce up and down or sink into the floor.
> We built an automated normalization pipeline that pads every frame onto a unified square canvas ($128\times128$ for Goku, $160\times160$ for Saitama) where the lowest non-transparent pixel (feet) is anchored to the exact same baseline line ($y = 105$ and $y = 135$). By pairing this with calculated sub-pixel pivots in Unity, our fighters remain rock-solid on the arena floor across every attack, crouch, and walk pose."*

---

### Q10: How does your jump animation avoid looking floaty or desynced from physics?
**Answer:**
> *"Instead of looping a flat frame timer in the air, [`FighterAnimator.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterAnimator.cs) selects jump poses dynamically by querying the Rigidbody2D's vertical linear velocity (`rb.linearVelocity.y`):
> - Upward velocity $> 4\text{ m/s}$ plays the initial launch pose.
> - Upward velocity between $1\text{ and }4\text{ m/s}$ plays the rising tuck.
> - Near-zero velocity ($|\text{velocity}| \le 1\text{ m/s}$) displays the apex hangtime frame.
> - Negative downward velocity displays the falling descent and landing prep frames.
> This tightly couples visual animation directly to Unity 6's physics engine."*

---

### Q11: How do your hitboxes work, and how did you balance game feel vs physics accuracy?
**Answer:**
> *"In fighting games, realistic physics colliders fail because player expectation is all about 'feel'. If a fist misses by 1 pixel, the player feels cheated.
> We implemented separate trigger volumes:
> 1. **Hurtbox (Green):** Encapsulates the vulnerable body.
> 2. **Hitbox (Red):** Encapsulates attack reach and only turns active during the attack's strike window.
> 3. **Multi-hit suppression:** We track targets hit during each swing to prevent multi-hit bugs on single strikes.
> 4. **Visual Scene Gizmos:** Designers can click the fighter in the Scene view and visually drag the red hitbox handles over the punch animation frame to dial in the reach without recompiling code."*

---

### Q12: How do you prevent fighters from running off-screen, and how did you balance relative visual proportions?
**Answer:**
> *"1. **Invisible Boundary Walls:** We placed vertical `BoxCollider2D` barriers at $x = \pm 5.7$, exactly aligned with the 16:9 camera boundaries (Orthographic size 3.2). Fighters physically cannot leave the camera frame.
> 2. **Proportional Visual Scaling:** Jump Ultimate Stars sprites have varied widths—Goku's spiky hair and martial arts gi made him look wider than Saitama. By tuning root local scale ($0.88\times$ for Goku, $1.12\times$ for Saitama), Saitama has a taller, heroic presence while Goku stays athletic and stocky, automatically scaling their hitboxes and hurtboxes proportionally."*

---

### Q13: Why did you decouple the HUD from the fighters using C# events instead of polling in Update()?
**Answer:**
> *"Polling in `Update()` (e.g. `p1HealthBar.fillAmount = fighter.health / 100f` every frame) creates unnecessary CPU overhead and tight coupling.
> Instead, we adopted the **Observer Pattern**:
> 1. `HealthSystem` and `EnergySystem` broadcast C# `Action` events (`OnHealthChanged`, `OnKiChanged`) ONLY when values actually mutate.
> 2. `HUDController` subscribes to these events and updates the visual fill amounts reactively.
> 3. If the UI is disabled or destroyed, gameplay logic runs with zero dependency on the view. This mirrors Godot's Signal architecture (`health_changed.connect(...)`)."*

---

### Q14: How does your damage ghost bar and retro round win tracker work?
**Answer:**
> *"1. **Damage Ghost Bar:** When an attack lands, the primary yellow/orange health fill drops immediately, but the red ghost bar stays frozen for 0.35s before sliding down smoothly via `Mathf.MoveTowards()`. This gives critical visual feedback on burst damage dealt.
> 2. **Symmetrical Win Trackers:** P1's golden dragon gem and 'WINS: 0/1' sit under P1's Ki bar on the left, while P2's ruby gem sits under P2's Ki bar on the right. An empty socket fills when a player takes a round, maintaining clean arcade clarity without overcrowding the central timer."*

---

### Q15: How did you implement the Godot-style scene workflow in Unity so non-programmers can edit characters and UI visually?
**Answer:**
> *"In Godot, every character and UI element is an independent `.tscn` scene that can be opened and edited in isolation.
> In Unity, the exact counterpart is the **Prefab System**:
> 1. We converted Goku, Saitama, and the HUD into connected Prefabs (`Assets/Prefabs/Fighters/Goku.prefab`, `Saitama.prefab`, and `Assets/Prefabs/UI/HUD_Canvas.prefab`).
> 2. By using `PrefabUtility.SaveAsPrefabAssetAndConnect()`, instances in the scene turn blue in the Hierarchy. Non-programmers can simply double-click any prefab to enter **Prefab Mode** (isolated scene editing), tweak colliders or UI layouts visually, and have those changes automatically apply everywhere.
> 3. We also created `Assets/Scenes/HUDScene.unity`, a dedicated sandbox scene where UI artists can test and style the HUD without loading fighters or physics."*

---

### Q16: Why did you choose a 16-bit retro arcade aesthetic for the HUD over modern flat minimalist UI?
**Answer:**
> *"Modern minimalist UI often feels sterile, corporate, and detached from the high-energy spirit of anime fighting games.
> Classic 16-bit arcade fighters (like *Street Fighter II*, *Alpha*, and *DBZ Hyper Dimension*) succeeded because their UI had tactile weight:
> 1. Double-line square portrait frames give characters distinct fighting game presence.
> 2. 2-tone specular health fills (lemon top, orange bottom) maximize contrast against detailed arena backgrounds.
> 3. Point filtering (`FilterMode.Point`) ensures pixel-perfect crispness without blurry bilinear smoothing.
> 4. The Time Stone sits directly inside the clock housing, marrying the mystical Marvel lore with retro arcade countdown typography."*

---

### Q17: How did you solve the Unity Prefab reference stripping problem when connecting UI events to fighters?
**Answer:**
> *"In Unity, saving a Canvas as a Prefab Asset (`HUD_Canvas.prefab`) automatically strips references to scene instances (like `Fighter_P1_Goku`) because assets on disk cannot point to transient scene objects.
> If left unhandled, `HUDController` wakes up with null references, making the UI completely unresponsive to combat events.
> We resolved this via **Self-Healing Reference Discovery**:
> In `HUDController.Awake()`, `Start()`, and `OnEnable()`, if any fighter or match manager reference is null, it queries the scene dynamically, binds the C# action events (`OnHealthChanged`, `OnKiChanged`, `OnTimerUpdate`), and synchronizes the current state immediately. This gives us both modular prefab isolation and runtime responsiveness."*

---

### Q18: Why does the Time Stone use a Doctor Strange style magic mandala instead of a standard digital clock box?
**Answer:**
> *"In Marvel lore (*Doctor Strange*, *Infinity War*), the Time Stone is an ancient cosmic infinity stone harnessed through green eldritch sorcery. Placing it inside a generic plastic clock box felt immersion-breaking and bland.
> Instead, we designed a circular 160×160 green Time Mandala inspired by the Tao Mandalas Doctor Strange conjures to manipulate time.
> The mandala continuously rotates at 22°/s with concentric geometric rings, radiating emerald ki rays, and glowing emerald-white countdown numerals in the center. This marries authentic Marvel aesthetic with clear arcade fighting game visibility."*

---

### Q19: Why did 2D trigger-on-trigger collisions fail in Unity Box2D, and how did you resolve hit detection?
**Answer:**
> *"By default in Box2D (Unity's 2D physics engine), two `isTrigger = true` colliders (sensors) never generate contact events between each other.
> Initially, both `Hitbox` and `Hurtbox` were triggers, causing `OnTriggerEnter2D` to be silently skipped.
> We solved this robustly by upgrading `HitboxController`:
> 1. It queries `other.GetComponentInParent<HealthSystem>()`, detecting any collider belonging to the opponent (body colliders, hurtboxes, or roots).
> 2. It implements both `OnTriggerEnter2D` and `OnTriggerStay2D` so point-blank strikes register without requiring fresh movement.
> 3. It filters out self-hits and multi-hits within the same strike cycle, guaranteeing 100% reliable combat damage."*

---

### Q20: Why is attack frame rate (FPS) tuning critical for fighting game weight and 'game feel'?
**Answer:**
> *"In fighting games, uniform high-speed playback makes all moves feel like flimsy paper.
> To give attacks bone-crushing physical weight:
> 1. Light jabs run at 9.5 FPS for snappy, immediate pokes.
> 2. Heavy strikes and Ultimates run at 6 FPS, holding the anticipation and impact frames longer so players feel the massive kinetic force.
> 3. Flurry skills (Consecutive Normal Punches) run at 10 FPS with multi-hit re-arming, giving a rhythmic, satisfying barrage instead of an unrecognizable blur."*

---

### Q21: How do TextMeshPro 4-corner vertex gradients create authentic 3D arcade typography in Unity?
**Answer:**
> *"Rather than relying on flat monochrome font colors or pre-rendered text bitmap images that blur when scaled, TextMeshProUGUI enables GPU-accelerated vertex gradients via `VertexGradient(topLeft, topRight, bottomLeft, bottomRight)`.
> By assigning bright specular highlights to the top vertices (e.g. `#FFF573` for Goku, `#FFF0E8` for Saitama) and deep rich tones to the bottom vertices (e.g. `#D92600` crimson, `#8C001E` maroon), each glyph dynamically renders with a 3D beveled lighting gradient at zero runtime texture allocation."*

---

### Q22: How does the multi-layer HUD architecture prevent ornate decorative elements (like the Golden Dragon Crawler) from clipping or breaking gameplay?
**Answer:**
> *"In fighting game UIs, heavy decorative frames risk obscuring active fill bars or intercepting UI click events.
> We solve this through clean architectural separation:
> 1. The health bar container manages the 650×52 dimensions and 8px beveled frame.
> 2. The fill bars (`GhostFill` and `HealthFill`) are inset with negative padding (`offsetMin = 8, 6`, `offsetMax = -8, -6`), ensuring health values never bleed over the border.
> 3. The `DragonCrawler` overlay sits on the top rail as an independent image with `raycastTarget = false`. It delivers majestic anime visual aesthetic while completely avoiding any interference with UI events or hit tracking."*

---

### Q23: Why did health bars fail to decrease when attacks landed, and how does `IsInActiveScene()` resolve the Prefab Asset listener bug?
**Answer:**
> *"When a UI Canvas is saved as a Prefab Asset, Unity cannot serialize pointers to live scene GameObjects. Instead, Unity automatically remaps those references to the uninstantiated Prefab Assets stored on disk in `Assets/Prefabs/Fighters/`.
> At runtime, standard null checks (`if (p1Health == null)`) fail to trigger because the field is not null—it holds a valid pointer to the disk asset!
> Consequently, `HUDController` attached its event delegates to the inert disk asset while the live fighters in the active scene took hits in isolation.
> We solved this by implementing `IsInActiveScene(Component comp) => comp != null && comp.gameObject.scene.IsValid() && comp.gameObject.scene.isLoaded;`.
> If a reference points to a disk asset, the script discards it and dynamically binds to the active scene instances (`Fighter_P1_Goku`, `Fighter_P2_Saitama`), ensuring 100% reliable real-time health bar tracking."*

### Q24: How does the Match Point indicator system work dynamically in Mystic Combat, and why use dual round gems and a hollow relic socket instead of simple text?
**Answer:**
> *"In competitive fighting games, match tension peaks when a fighter is one round away from victory. Instead of generic text like 'WINS: 1', Mystic Combat uses an environmental visual relic system:
> 1. **Dual Player Round Gems:** Each fighter has two 56×56 point sockets (`p1PointGem1`, `p1PointGem2`, `p2PointGem1`, `p2PointGem2`). At 0 wins, both remain dark hollow sockets (`gem_socket_hollow.png`). On winning Round 1, Gem 1 turns into a glowing sapphire (`gem_shiny_blue.png`). On winning Round 2, Gem 2 ignites and clinches tournament victory.
> 2. **The Hollow Relic Socket:** The central 68×68 championship relic begins the match as an empty, dark golden socket (`gem_big_gold_hollow.png`). This signals that the championship trophy is unearned.
> 3. **Dynamic Event Evaluation:** In `HUDController.UpdateRoundScore(p1Wins, p2Wins)`, the game evaluates `isMatchPoint = (p1Wins >= 1 || p2Wins >= 1)`. 
> 4. **Active Ignition:** When either fighter reaches match point, `centerBigGoldGem.sprite` swaps to the radiant [`gem_big_gold.png`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Sprites/UI/gem_big_gold.png) (extracted directly from the user's multi-gem screenshot `Screenshot 2026-10-25 112541.png`), triggering a breathing golden sine-wave pulse ($1.0\times \rightarrow 1.15\times$). This gives instant, dramatic feedback without crowding the screen."*

### Q25: How did you transform a low-resolution user-provided sprite into a cinematic game-ready UI element with clean alpha blending and specular lighting?
**Answer:**
> *"When integrating raw user assets (such as the emerald Time Stone screenshot), three critical processing steps are applied:
> 1. **Color Distance Keying:** Rather than an exact RGB match that leaves jagged white halos, we discard pixels within a variance threshold (`abs(r-g) < 15 && abs(g-b) < 15 && r > 170`) to strip compression artifacts while preserving emerald facets and highlights.
> 2. **Point-Filtered Upscaling:** Scaling pixel art using bilinear filtering results in muddy, blurry edges. We upscale using nearest-neighbor (`Point`) filtering to keep crystalline facet boundaries razor-sharp.
> 3. **Compositing Specular Facets & Cosmic Accents:** We embed the clean gem inside a golden Eye of Agamotto bezel, add top-left diamond glints, and place it atop an active 160×160 Doctor Strange Tao mandala rotating at $22^\circ/\text{s}$."*

### Q26: How does `MatchManager` manage round transitions, and why is an asynchronous coroutine pause necessary between rounds in a 2D fighting game?
**Answer:**
> *"In fighting games, instant 0-millisecond round resets break gameplay because the fatal attack's active hitbox and knockback physics are still resolving on the frame health reaches zero.
> We implemented an asynchronous coroutine `RoundEndSequence(winnerIndex)` in `MatchManager`:
> 1. When a fighter's HP drops to 0, `currentMatchState` transitions to `RoundEnd`, freezing combat for 1.8 seconds so players see the K.O.
> 2. `ResetForNewRound()` snaps both fighters back to their starting spawn coordinates (`-2.2` and `+2.2`), resets velocities to zero, clears attack hitboxes, and transitions the state machine back to `Idle`.
> 3. If the match is tied 1–1 (P1 won Round 1, P2 won Round 2), the system automatically initiates Round 3 as the Final Round Match Point with all gems, health bars, and ghost bars cleanly refreshed."*

### Q27: Why did TextMeshPro text shrink to microscopic sizes on long strings, and how does expanding RectTransform width and disabling word wrapping fix it?
**Answer:**
> *"TextMeshProUGUI components with narrow RectTransforms (such as our original 340px header) suffer from text distortion when long match point strings are assigned. If auto-sizing is active or container bounds are constrained, TMP either forces word-wrapping or scales font size down to fit.
> We resolved this by:
> 1. Expanding the `RoundTitle` RectTransform sizeDelta to `600×36px`, providing ample horizontal clearance across 1080p and 4K viewports.
> 2. Setting `textWrappingMode = TextWrappingModes.NoWrap` (replacing obsolete `enableWordWrapping` in Unity 6) and `overflowMode = TextOverflowModes.Overflow`, guaranteeing that match point titles remain strictly on a single line at bold 24pt with 3D antique gold vertex gradients."*

---

### Q28: What is `[SerializeField]` in Unity, and why is it preferred over making variables `public`?
**Answer:**
> *"In C#, making a variable `public` exposes it in the Unity Inspector, but it violates Object-Oriented Programming (OOP) encapsulation because any external script can inadvertently overwrite it during runtime.
> `[SerializeField] private` achieves the best of both worlds:
> 1. It keeps the variable strictly `private` in code, protecting it from external tampering.
> 2. It instructs Unity's serializer to save the value to the scene/prefab file and draw an editable UI field in the Inspector.
> In Godot, this is the direct equivalent of the `@export` keyword."*

---

### Q29: Why does Unity separate `Update()` and `FixedUpdate()`? What happens if physics runs on variable `Time.deltaTime`?
**Answer:**
> *"`Update()` executes once per render frame, which fluctuates based on GPU load and display refresh rates (e.g., 30 FPS vs. 144 FPS).
> `FixedUpdate()` executes on a rock-solid, deterministic time step (default 50 ticks/sec or `0.02s`).
> If physics ran inside `Update()` with variable delta time, sudden lag spikes would cause 'tunneling'—a high-speed projectile moving at 100 m/s would jump 20 meters in a single lag frame and pass completely through solid walls without triggering collision detection. A fixed time step guarantees identical, reproducible physics simulation on all hardware."*

---

### Q30: Why shouldn't you multiply `Rigidbody2D.linearVelocity` by `Time.deltaTime`?
**Answer:**
> *"Velocity in physics is defined as distance per second (meters/second).
> Unity's physics engine (`Box2D`) automatically integrates velocity over time by multiplying it by `Time.fixedDeltaTime` under the hood.
> If a developer writes `rb.linearVelocity = new Vector2(speed * Time.deltaTime, 0)`, they are multiplying by delta time twice (the 'Double Delta' error), which shrinks the character's movement velocity down to an imperceptible crawl."*

---

### Q31: Why do we append the `f` suffix to decimal numbers in Unity C# (e.g. `5.5f`)?
**Answer:**
> *"In C#, any literal decimal number (such as `5.5`) is treated as a 64-bit `double` by default.
> However, game engines, graphics cards (GPUs), and physics systems (`Vector2`, `Vector3`, `Time.deltaTime`) operate on 32-bit single-precision `float` numbers because GPUs process 32-bit floats twice as fast with half the memory footprint.
> Appending `f` explicitly instructs the compiler to treat the number as a 32-bit float, avoiding implicit type conversion errors."*

---

### Q32: How is text rendered in modern Unity, and how does TextMeshPro differ from legacy UI Text?
**Answer:**
> *"Legacy Unity UI Text relied on rasterized bitmap fonts, which blur and pixelate when scaled or viewed on high-resolution displays.
> Modern Unity uses TextMeshPro (TMP), which renders text using Signed Distance Field (SDF) vector shaders. SDF fonts remain mathematically razor-sharp at any zoom level, screen resolution, or perspective angle, while supporting GPU-accelerated vertex gradients, drop shadows, and glowing outlines at zero runtime texture allocation overhead."*

---

### Q33: Can you explain your character's architecture in 60 seconds?
**Answer:**
> *"Our fighter architecture follows a clean 4-stage pipeline that separates input, logic, animation, and combat:
> 1. **The Senses (`PlayerInputHandler.cs`):** Polls raw hardware keys for P1 (WASD) and P2 (IJKL) and exposes clean input flags.
> 2. **The Brain (`FighterController.cs`):** Acts as the central state machine. It evaluates physics conditions (grounded checks, knockback, Red Zone exhaustion) and updates fighter state.
> 3. **The Artist (`FighterAnimator.cs`):** Reactively monitors the controller's current state and animates calibrated sprite flipbooks (idle at 5 FPS, punch at 9 FPS, flurry at 14 FPS).
> 4. **The Combat Layer (`HitboxController.cs` & `HurtboxController.cs`):** Activates collision triggers during attack frames. When a hitbox overlaps an opponent's hurtbox, it fires events that apply damage, physics knockback impulse, and Ki generation without coupling the two fighters directly."*

---

### Q34: How did you design and implement the Retro Arcade Main Menu, and how does the game ensure authentic 90s aesthetic without compromising modern UI responsiveness?
**Answer:**
> *"We fused Technos Japan's 1988 Double Dragon II arcade aesthetic with the Doctor Strange cosmic multiverse theme:
> 1. **Handcrafted Title Logo & Alpha Cutout:** An extruded metallic chrome 'MYSTIC COMBAT' title with embedded Doctor Strange gold mandala, alpha-keyed to zero transparency to float seamlessly across the cosmic multiverse backdrop without boxy borders.
> 2. **Arcade Entrance Coroutine:** On launch, the title logo performs an elastic punch impact ($1.35\times \rightarrow 0.94\times \rightarrow 1.0\times$) accompanied by an 8-bit sound confirmation, followed by a staggered cascade of the 4 menu options ($0.06\text{s}$ interval).
> 3. **Multi-Color Vertex Gradients:** Instead of flat modern monochromatic text, options render with GPU-accelerated 4-corner TextMeshPro vertex gradients (active: Radiant Gold/White to Blazing Amber/Crimson with dynamic shimmer; inactive: Electric Cyan/Silver to Neon Violet/Indigo).
> 4. **Automated BGM Pipeline:** Built-in looping audio architecture in `MainMenuController.cs` that dynamically discovers and streams audio tracks placed in `Assets/Audio/Music/`."*


---

## 3. Live Demo Checklist for Judges

When demonstrating the game to evaluators:
1. **Show the Console:** Point out `[CharacterDataLoader]` loading `goku.json` and `saitama.json` at startup to prove the data-driven architecture.
2. **Show Auto-Facing:** Jump over Saitama with Goku and show both characters flipping smoothly.
3. **Show Meditation:** Hold the meditate key (`G` for P1, `M` for P2) and show the fighter rooting in place to channel mystic Ki.
4. **Show Arena Colliders:** Run into the arena boundary walls to show solid 2D collision resolution.
5. **Show Damage Ghost Bar & Time Stone:** Land a heavy strike to show the instant health drop followed by the smooth trailing ghost bar, and point out the rotating energy aura behind the central Time Stone.
