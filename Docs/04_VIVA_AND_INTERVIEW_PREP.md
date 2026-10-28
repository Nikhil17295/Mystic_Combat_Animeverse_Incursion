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

### Q35: How does the combat audio architecture work, and how did you prevent audio bleed between the menu and arena?
**Answer:**
> *"Combat audio is managed by `MatchAudioController.cs` observing `MatchManager` events:
> 1. **Seamless Tournament Theme:** All 3 rounds play 'Double Dragon 2 - Final Battle 2' continuously, eliminating abrupt mid-match jarring track switches and keeping arcade combat flow steady.
> 2. **3-Second Victory Fanfare:** On final match victory (`OnMatchOver`), combat BGM stops and 'Double Dragon 3 - Mission Clear' triggers for 3 seconds while the victor celebrates.
> 3. **Strict State Isolation:** Battle music only triggers when round combat begins (`RoundInProgress`) and is strictly barred if `GameManager.GameState == MainMenu`. Additionally, `MainMenuController.cs` cuts the title music (`double_dragon_ii_title.mp3`) immediately when launching combat (`StartGameMode`) or unloading, preventing simultaneous double music.
> 4. **Bell-First Dramatic Sequencing:** At round start, the tournament bell strikes alone at full volume (`1.0`). Battle BGM waits a deliberate $0.85\text{s}$ pause for the bell to ring out before kicking in fresh from `0:00`. On round defeat/K.O., battle BGM halts instantly so the K.O. gong echoes over arena silence."*

---

### Q36: How do you achieve real-time live audio volume mixing in the Unity Inspector during Play mode, and how does it compare to Godot?
**Answer:**
> *"In Unity, we declare serialized volume fields bounded with range sliders:
> `[SerializeField] [Range(0f, 1f)] private float battleMusicVolume = 0.50f;`
> In `Update()`, we evaluate:
> `if (musicSource != null && musicSource.isPlaying) musicSource.volume = battleMusicVolume;`
>
> **Godot vs Unity Analogy:**
> - In Godot, you write `@export_range(0.0, 1.0) var battle_music_volume: float = 0.50` on an `AudioStreamPlayer` node.
> - In Unity, we use `[SerializeField] [Range(0f, 1f)]` on an `AudioSource` component.
>
> **Why this matters for judges:**
> Audio balancing cannot be judged accurately from a static editor view. By linking the slider in `Update()`, judges and audio testers can drag the slider in the Inspector during live gameplay to find the sweet spot (e.g. 0.50 for combat BGM vs 1.0 for punches/kicks) without restarting or recompiling."*

---

### Q37: How did you design the combat sound effects architecture, and how does your hit audio scale with attack damage?
**Answer:**
> *"Every fighter has a dedicated `FighterAudio.cs` component with two separate audio sources:
> 1. **One-Shot Source:** Uses `AudioSource.PlayOneShot()` so rapid attack combos never cut off previous audio tails.
> 2. **Looping Channeling Source:** Plays a seamless 110Hz harmonic aura hum while holding the Meditation key, stopping smoothly upon release.
> 3. **Dynamic Impact Scaling:** When `HitboxController.cs` resolves an attack hit, it checks the strike's damage value and fires proportional audio:
>    - Light attacks (<15 damage): Snappy knuckle slap crack (`sfx_hit_light.wav`).
>    - Heavy attacks (15–19 damage): Deep crunchy bass crunch (`sfx_hit_heavy.wav`).
>    - Skill attacks (20–39 damage): Energy-blast resonant zap (`sfx_hit_skill.wav`).
>    - Ultimates (40+ damage): Cinematic sub-bass explosion (`sfx_hit_ultimate.wav`).
> 4. **Audio Balancing Hierarchy:** SFX are mixed at 1.0 (100%), while background music is mixed at 0.50 (50%), ensuring punches, jumps, and Ki charging cut through cleanly without drowning in the soundtrack."*

---


---

### Q38: Why is binding audio triggers to raw button presses an anti-pattern in fighting games, and how does your state-driven architecture solve this across P1, P2, and CPU AI?
**Answer:**
> *"Triggering audio on raw input events (e.g. `if (Input.GetKeyDown(KeyCode.W)) PlayJump()`) is a classic rookie anti-pattern for three reasons:
> 1. **Input Spamming:** Pressing `W` repeatedly while already airborne would trigger multiple jump sounds mid-air.
> 2. **State & Hit-Stun Desync:** Pressing attack keys while frozen or knocked down would play punch swishes even though the character cannot physically execute the move.
> 3. **AI & Multi-Player Failure:** CPU bots, second players, or replay systems do not generate keyboard input events, making characters silent.
>
> **Our Solution: State-Driven Fighter Audio:**
> Audio is triggered strictly inside `FighterController.TransitionTo(newState)` upon state entry, and landing audio is tied to ground detection transitions. Furthermore, `FighterAudio.cs` is attached to character prefabs with serialized `AudioClip` slots, allowing each character (Goku, Saitama, Naruto, Vegeta) to have their own unique audio profile, dialogue quotes, and grunts regardless of whether they are P1, P2, or a CPU bot."*

---

### Q39: Why can't you use raw anime audio clips directly in a fighting game, and how did you balance continuous aura loops vs one-shot punch impacts?
**Answer:**
> *"There are two major audio pitfalls when porting anime assets into a real-time fighting game:
> 1. **Cinematic Dead-Air & Pacing Desync:** Anime episodes feature dramatic pauses and 2-to-3-second reverb tails meant for TV screen pacing. In a fast fighting game, a 6.5-second Kamehameha or a 9-second Serious Punch creates sluggish gameplay. We trimmed the dead-air tails right after the vocal shout and resampled the playback to $1.20\times - 1.25\times$ speed (~3.1–3.3s), ensuring the audio matches the crisp strike animation.
> 2. **Acoustic Masking & Ear Fatigue:** Continuous looping sounds (like Ki meditation charging) naturally tire the player's auditory cortex if played at full volume. We balanced the continuous Ki aura loop at 45% (`0.45`), background music at 50% (`0.50`), and instantaneous impact strikes at 100% (`1.0`). This ensures punches retain visceral physical weight while charging feels ambient rather than deafening."*

---

### Q40: How do you prevent input conflicts and ghost keypresses in local 2-player fighting games?
**Answer:**
> *"In local single-keyboard multiplayer games, multiple key bindings for the same action (e.g. binding both `W` and `Space` to P1 Jump, or binding both `I/J/K/L` and `Arrow Keys` to P2 movement) frequently cause input cross-talk and ghost state transitions. For example, pressing Arrow keys for custom user macros can inadvertently move P2, and tapping `Space` while attempting an attack can cause accidental jump-takeoffs.
> 
> **Our Solution: Dedicated Key Isolation (`PlayerInputHandler.cs`):**
> 1. **P1 Layout:** Strict `W/A/S/D` for movement (`W` exclusively for Jump, `Space` removed) + `F/C/E/Q/X` for combat.
> 2. **P2 Layout:** Strict `I/J/K/L` for movement (`I` exclusively for Jump, Arrow keys removed) + `H/N/U/O/M` for combat.
> This guarantees 100% key isolation with zero accidental input overrides."*

---

## 3. Live Demo Checklist for Judges

When demonstrating the game to evaluators:
1. **Show the Console:** Point out `[CharacterDataLoader]` loading `goku.json` and `saitama.json` at startup to prove the data-driven architecture.
2. **Show Auto-Facing:** Jump over Saitama with Goku and show both characters flipping smoothly.
3. **Show Meditation:** Hold the meditate key (`G` for P1, `M` for P2) and show the fighter rooting in place to channel mystic Ki.
4. **Show Arena Colliders:** Run into the arena boundary walls to show solid 2D collision resolution.
5. **Show Damage Ghost Bar & Time Stone:** Land a heavy strike to show the instant health drop followed by the smooth trailing ghost bar, and point out the rotating energy aura behind the central Time Stone.
---

### Q41: Why is the Character Select menu hosted inside the Main Menu Canvas rather than in a separate Unity scene?
**Answer:**
> *"In fighting games, screen transitions must feel instantaneous, fluid, and cinematic. If we loaded a separate Unity scene (`SceneManager.LoadScene("CharacterSelect")`), Unity would discard all memory, reload assets, cause a visible frame freeze, and abruptly cut the title music.
> 
> By hosting both screens on the same Canvas (`MainMenu_Canvas`) and toggling between `Menu_Root` and `CharacterSelect_Root`, the cosmic multiverse background animation and the title theme continue streaming seamlessly. The player perceives zero loading latency, matching the authentic arcade experience of Capcom and SNK classics."*

---

### Q42: How does the Character Select UI adhere to your 'Grimoire Architecture' rather than hardcoded text?
**Answer:**
> *"Instead of statically typing 'HP: 100' or 'Kamehameha' into TextMeshPro labels in the Unity Editor, [`CharacterSelectController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/CharacterSelectController.cs) queries [`CharacterDataLoader.Load()`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Data/CharacterDataLoader.cs) when opened.
> 
> It parses `goku.json` and `saitama.json` directly from `StreamingAssets/Characters/`. If a designer or game balancer changes Goku's move speed from `5.0` to `5.5` or raises Spirit Bomb damage in the JSON file, the Character Select card updates its displayed statistics automatically without needing code changes or project re-baking."*

---

### Q43: How does your fighting game resolve 'Mirror Matches' when both players pick the same character?
**Answer:**
> *"In competitive fighting games, both players must be allowed to pick their best character (e.g. Goku vs Goku or Saitama vs Saitama).
> 
> 1. **UI Notification:** When `p1SelectedIndex == p2SelectedIndex`, `CharacterSelectController` activates a dynamic notice: `★ MIRROR MATCH: PLAYER 2 USES ALTERNATE PALETTE ★`.
> 2. **Sprite Tinting:** During scene initialization in [`PrototypeBootstrap.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs), the engine compares the two character IDs. If they match, Player 2's `SpriteRenderer.color` is tinted with a distinct tournament palette (Cool Ice Cyan for Goku or Solar Amber for Saitama).
> 3. **HUD Consistency:** [`HUDController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/HUDController.cs) applies the matching alternate tint to Player 2's bust card in the top-right header, ensuring complete visual clarity for both players and spectators."*

---

### Q44: How does CPU opponent selection work for single-player mode?
**Answer:**
> *"We provide a hybrid system accommodating both tactical play and quick play:
> 1. **Manual Selection:** In 1P vs CPU mode, after P1 locks in their hero, the prompt updates to `▶ CHOOSE CPU OPPONENT  or  PRESS [R] FOR RANDOM ◀`. The player can browse and hand-pick whoever they want to fight against.
> 2. **Instant Random:** If the player wants immediate action without browsing, they can press the `[R]` shortcut or click the `[ ⚡ RANDOM CPU ]` button. The system immediately rolls a random valid fighter and executes the match launch sequence."*

---

### Q45: How did you diagnose and eliminate the UI element overlap bug in the Character Select cards, and how does Unity's UI layout system compare to Godot?
**Answer:**
> *"In Unity, UI elements are positioned via their RectTransform component relative to anchors:
> 1. **The Math Collision:** Our fighter title label was anchored at  = -76\text{px}$ with a height of \text{px}$ (bottom boundary at $-93\text{px}$). The blue stat panel was anchored at  = -205\text{px}$ with a height of \text{px}$ (top boundary at $-82.5\text{px}$). Because $-82.5 > -93$, the stat box physically intersected the title text by .5\text{px}$.
> 2. **Godot vs Unity Architectural Difference:**
>    - In **Godot**, UI nodes usually live inside container nodes like VBoxContainer or MarginContainer. Godot automatically computes child bounding rectangles and applies separation margins, making manual pixel tracking unnecessary.
>    - In **Unity**, when working with raw RectTransform offsets without a VerticalLayoutGroup, every element uses absolute local coordinates. If the bounding intervals intersect, Unity draws them on top of each other.
> 3. **The Solution:** We recalculated the vertical hierarchy in MainMenuSetupTool.cs (Title center at $-52\text{px}$, bottom at $-68\text{px}$; Stat panel center at $-195\text{px}$, top at $-82.5\text{px}$), creating an explicit **.5\text{px}$ clear margin** with zero overlap. We also gave Goku and Saitama custom title colors (Super Saiyan Radiant Gold #FFE620 vs Serious Punch Crimson #FF4D4D)."*

---

### Q46: How does your audio architecture handle character-specific vocal cries (like Saitama's 'Consecutive Normal Punches' and 'Serious Punch') across dynamic scene transitions and runtime instantiation?
**Answer:**
> *"Fighting games require audio to be instant, robust, and state-synced:
> 1. **Multi-Tier Resolution Pipeline:**
>    - **Tier 1 (Prefab Serialization):** Our editor baking tool (SceneSetupTool.cs) assigns consecutive_normal_punches.mp3 and serious_punch.mp3 directly to Saitama's prefab slots (oiceSkillClip and oiceUltimateClip).
>    - **Tier 2 (Runtime Resource Fallback):** When matches start from Character Select, [PrototypeBootstrap.cs](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs) spawns fighters dynamically via code (AddComponent<FighterAudio>). In Unity, AddComponent creates empty serialized fields, and AssetDatabase is unavailable at runtime. We solved this by implementing EnsureClipsLoaded() in [FighterAudio.cs](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs), which dynamically falls back to Resources.Load<AudioClip>() from Assets/Resources/Audio/.
> 2. **State-Machine Driven Vocal Triggering:** When Saitama enters his Skill attack state ([U]), ighterAudio.PlaySkillAttack() fires PlayVoiceSkill(). When he triggers his 100-Ki Ultimate ([O]), ighterAudio.PlayUltimateAttack() fires PlayVoiceUltimate(), playing his signature shouts in perfect sync with his visual animation frames."*

---

### Q47: Why did the Ki meditation sound remain loud despite previous code changes, and how does Unity handle serialized prefab overrides vs C# default values?
**Answer:**
> *"This illustrates one of the most common pitfalls in Unity development:
> 1. **Prefab Serialization Overrides Script Defaults:** When a GameObject is saved as a Prefab (`Goku.prefab`, `Saitama.prefab`), Unity serializes every field's current value directly into the prefab's YAML file. Even if a developer changes `kiAuraVolume = 0.45f` to `0.12f` in the C# script, existing prefabs in `Assets/Prefabs/` will continue using their serialized `0.45` value because prefab data takes precedence over code defaults.
> 2. **Continuous RMS Loudness of Loop Assets:** Unlike transient punch impacts that last $0.15\text{s}$, `dbz_super_saiyan_aura.mp3` is a continuous normalized $0\text{ dBFS}$ sub-bass loop. To the human ear, continuous pink/brown noise sounds dramatically louder than punch hits at the same linear volume.
> 3. **The Multi-Tier Fix:** We enforced `0.12f` (12% volume) across all 4 tiers simultaneously:
>    - The C# field default in `FighterAudio.cs`.
>    - The scene baker `SceneSetupTool.cs`.
>    - The serialized prefab YAML files (`Goku.prefab` and `Saitama.prefab`).
>    - The runtime spawning logic in `PrototypeBootstrap.cs`."*

---

### Q48: How does audio volume scaling work in Unity's AudioSource.PlayOneShot, and how did you boost vocal shouts without distorting normal SFX?
**Answer:**
> *"In Unity, `AudioSource.PlayOneShot(AudioClip clip, float volumeScale)` accepts a `volumeScale` parameter that scales output amplitude.
> 1. **The Clamp Trap:** Beginner code often writes `Mathf.Clamp01(volume)`, which locks volume scaling to a maximum of $1.0$ ($100\%$). If a voice line has a quiet recording gain, it cannot be amplified.
> 2. **Dedicated Voice Channel:** In `FighterAudio.cs`, we introduced a dedicated `voiceSource` (`Voice_AudioSource`) separate from the one-shot `sfxSource`. This prevents punch impacts from ducking or clipping the voice shouts.
> 3. **Amplification Multipliers:** By removing the `0..1` clamp in favor of `Mathf.Clamp(volume, 0f, 2.5f)`, we applied tailored multipliers:
>    - Skill Flurry (`consecutive_normal_punches.mp3`): **$1.45\times$**.
>    - Ultimate KO Punch (`serious_punch.mp3`): **$1.70\times$**.
> This guarantees vocal cries punch through the combat mix clearly without altering the baseline volume of normal hits."*

---

### Q49: How did you diagnose and solve the visual blending bug where Saitama's flurry punch outlines disappeared against certain backgrounds?
**Answer:**
> *"This was a critical lesson in fighting game visual readability and human perception:
> 1. **Relative Luminance & Contextual Contrast:** In fighting games, character attacks must be readable anywhere on stage. We discovered that Saitama's flurry punch outlines were drawn with `#140505` (a dark charcoal with a relative luminance of just $9.5$). When Saitama was in the center of the ring under the stage spotlight (luminance $\sim 190$), the dark outline was visible. But when moving toward the arena edges, the background tiles have luminance $26\text{–}30$ (`#1A1F25`). Against dark tiles and Goku's black gi shadows, the contrast ratio collapsed below $1.3:1$, causing the boundary to visually vanish.
> 2. **Unshielded Transparency Gaps:** In 6 of the 12 frames, 171 perimeter punch pixels had no dark outline at all and directly touched the transparent background.
> 3. **The Dual-Contour Anime Solution:** Rather than relying on computationally heavy post-processing or fragment outline shaders, we baked a dual-tier cel-shading architecture directly into the sprite frames:
>    - **Crisp Perimeter Inking:** Sealed all unshielded gaps with a 1px solid dark contour (`#080404`).
>    - **Kinetic Shockwave Knuckle Highlights:** Along the leading front edge of each fist, we introduced a 3-stage anime rim highlight: blazing white strike face (`#FFFFFF`, luminance $255.0$), solar gold transition (`#FFD76E`), and warm red-orange blend (`#FF5A32`).
> 4. **The Result:** The punches now have an $>8:1$ contrast ratio against dark arena tiles and $>20:1$ against black sprites, giving the move the signature high-speed anime flurry look while maintaining 100% of the original character body art."*

---

### Q50: How does pausing work in Unity (`Time.timeScale`), and how did you preserve combat state and prevent input buffering across pause and rematch?
**Answer:**
> *"Pausing in fighting games requires careful handling of physics, audio, input, and coroutines:
> 1. **Physics & Animation Freezing:** Setting `Time.timeScale = 0f` stops `FixedUpdate()` physics updates, which preserves Rigidbody velocities, active jump trajectories, and attack frames without corrupting state.
> 2. **Input Buffering Prevention:** In Unity, `Update()` still executes every frame even when `Time.timeScale = 0f`. If not guarded, menu navigation keypresses (like `W` or `Space`) bleed into the fighter's input handler. We added a zero-allocation guard in `PlayerInputHandler` that clears all action flags (`JumpPressed = false`, `LightAttackPressed = false`) when `Time.timeScale == 0f`, guaranteeing a clean resumption.
> 3. **Audio Bus Separation:** We invoked `AudioListener.pause = true` to freeze looping battle music and combat auras, but set `ignoreListenerPause = true` on the UI `AudioSource`. This allows retro menu navigation bleeps and confirmation chimes to play while the match is frozen.
> 4. **Zero-Reload Rematch Architecture:** Calling `MatchManager.RestartMatch()` restores full health and Ki, zeroes wins to 0-0, resets round timers, and positions fighters at their starting spawn anchors without requiring a heavy scene reload, giving players instantaneous arcade rematching."*

---

### Q51: How did you implement match-lifecycle input gating and cinematic UI slide transitions in Unity?
**Answer:**
> *"In competitive fighting games, input handling and round pacing are fundamental to game feel:
> 1. **Lifecycle Input Gating:** In `PlayerInputHandler`, we implemented a `CanFight()` gate checking `MatchManager.CurrentMatchState == MatchState.RoundInProgress`. Before the round opening bell (`WaitingToStart`), during the K.O. strike reverberation (`RoundEnd`), and after the match ends (`MatchOver`), all movement and attack vectors are zeroed out. This prevents accidental attacks before the bell and keeps fighters in clean finish poses after K.O.
> 2. **3-Second Pacing Window:** We paced the post-match victory reveal with a $3.0\text{s}$ realtime delay so the final hit reverberates, allowing the defeated fighter to hit the canvas and the victor's pose to sink in.
> 3. **Cubic Ease-Out Tweening in Code:** Rather than relying on external tweening libraries or Animator state machines, we coded a pure mathematical ease-out coroutine:
>    $$y(t) = \text{Lerp}\left(950, 0, 1 - (1 - t)^3\right)$$
>    Running on `Time.unscaledDeltaTime`, the victory card rockets down from off-screen ($Y = 950\text{px}$) and settles gracefully in the screen center at 60 FPS.
> 4. **Maintaining Scene Brightness:** By setting the full-screen modal backdrop to `Color.clear`, the arena lighting, stage backdrop, and characters stay fully illuminated instead of being masked in shadow."*

---

### Q52: Why did you choose a Hierarchical Finite State Machine (HFSM) with an Input Abstraction layer for CPU AI instead of Behavior Trees or direct controller calls?
**Answer:**
> *"We chose this architecture for three crucial software engineering and game-feel reasons:
> 1. **Zero-Cheating Virtual Controller Contract (`IFighterInput`):** In amateur fighting games, AI scripts directly override character physics (e.g. teleporting, skipping startup frames, or ignoring Ki costs). In Mystic Combat, `FighterController` only accepts an `IFighterInput` interface. The CPU AI (`CpuFighterBrain`) must press virtual buttons (`MoveInput`, `LightAttackPressed`, `MeditateHeld`) just like a human keyboard player. It obeys the exact same gravity, recovery windows, and Red Zone exhaustion rules.
> 2. **Decoupling Brain from Body:** Because the AI generates inputs rather than controlling physics directly, any future combat feature we add to `FighterController` (e.g., parries, air dashes, or counter-hits) works automatically for both human and CPU fighters without rewriting the AI.
> 3. **Hierarchical FSM vs Flat FSM or Behavior Trees:** Behavior Trees add unnecessary traversal overhead and node boilerplate for a fast 2D fighter with instant 60 FPS frame data. Conversely, a flat FSM easily gets tangled in transition spaghetti. A Hierarchical FSM cleanly splits decision-making into three decoupled layers:
>    - **Layer 1 (Macro):** Tactical Posture (`RushdownPressure`, `PunishOpening`, `SpacingNeutral`, `KiCharge`, `ExhaustionRecovery`).
>    - **Layer 2 (Combat Intelligence):** Hit-confirm combo chain-canceling and Goku Super Saiyan ascension checks.
>    - **Layer 3 (Micro):** 2-Frame latched virtual input actuation (eliminates `LateUpdate()` frame drops regardless of component execution order)."*

---

### Q53: How do you prevent CPU AI from feeling like an unfair 'input-reading' cheat while still keeping Hard difficulty challenging?
**Answer:**
> *"Fighting game AI feels unfair when it exhibits $0\text{ms}$ frame-reading reactions (e.g. parrying every light attack on frame 1). We eliminated this through three humanization mechanics:
> 1. **Perceptual Reaction Tick ($\Delta t \approx 0.04\text{s} - 0.22\text{s}$):** Instead of evaluating the battlefield every single frame, the CPU brain evaluates tactical decisions on a throttled timer. On Easy difficulty, this is $0.22\text{s}$, giving the player a large window to land mix-ups and jump-ins. On Hard, it is $0.10\text{s}$, giving snappy arcade reflexes that mimic a skilled human tournament player.
> 2. **Mistake & Spacing Probability:** On Easy mode, the bot has a $15\%$ mistake rate where it misjudges spacing or pauses in neutral, simulating human uncertainty. On Hard, mistake rate drops to $4\%$.
> 3. **Hit-Confirm Confirmation Logic:** Rather than instantly inputting optimal combos with mathematical perfection, the bot rolls against a `comboChance` attribute ($60\%$ on Easy vs $88\%$ on Hard, $98\%$ on Extreme) to decide whether to confirm a landed Light attack poke into a special skill or heavy attack."*

---

### Q54: How did you implement distinct anime-accurate fighting personalities (Goku vs. Saitama) in the AI codebase?
**Answer:**
> *"Rather than using a generic bot with different sprite skins, we subclassed `CpuFighterBrain` into character-specific classes with distinct strategic traits:
> 1. **`GokuBrain.cs` (Disciplined Martial Artist, SSJ Powerhouse & Zoner):**
>    - **Spacing Bias:** Maintains $1.65\text{m}$ melee range; capable of full-screen beam zoning.
>    - **Super Saiyan Transformation:** The instant Goku accumulates $35\text{ Ki}$, he triggers `Skill` to ascend into Super Saiyan form ($+25\%$ speed, $+66\%$ light damage, $+43\%$ heavy damage).
>    - **Beam Sniper:** Unloads full-screen *Kamehameha* (`UltimatePressed` / $80\text{ Ki}$ / $45\text{ dmg}$) from up to $6.5\text{m}$ away on vulnerable opponents.
>    - **Disengage Ki Charging:** Backs up to safe range ($> 3.0\text{m}$) and channels meditation to build Ki for beam attacks.
> 2. **`SaitamaBrain.cs` (Relentless Rushdown Brawler):**
>    - **Spacing Bias:** Closes distance relentlessly ($1.30\text{m}$); refuses to back away in neutral.
>    - **Visceral Punch Chains:** Pokes with Light jabs and chain-cancels into Heavy haymakers and *Consecutive Normal Punches* (`SkillPressed` / $15\text{ hits} / 30\text{ dmg}$) point-blank.
>    - **80-Ki Lethality:** Hoards $80\text{ Ki}$ to march up and drop the round-ending *Serious Punch* (`UltimatePressed` / $90\text{ damage}$). All-in brawler personality."*

---

### Q55: Why did Character Selection fail to change the actual fighters on stage despite updating the HUD, and how did you resolve it?
**Answer:**
> *"This was caused by a combination of scene hardcoding and name-brittle object queries:
> 1. **Static Scene Hardcoding:** `SampleScene.unity` had static baked GameObject instances (`Fighter_P1_Goku` and `Fighter_P2_Saitama`) saved directly in the scene YAML.
> 2. **Name-Brittle Queries in Systems:** `MatchManager.cs` and `HUDController.cs` contained hardcoded `GameObject.Find("Fighter_P1_Goku")` and `GameObject.Find("Fighter_P2_Saitama")` calls. Even when Character Select chose Saitama as Player 1, `MatchManager` and the HUD explicitly hunted for Goku as P1 and Saitama as P2!
> 3. **The Solution (The Deployer Pattern):**
>    - We stripped static character instances from the arena scene, treating `SampleScene` as an empty stage.
>    - We encapsulated characters as standalone prefabs in `Assets/Resources/Prefabs/Fighters/` (`Goku.prefab`, `Saitama.prefab`).
>    - On scene load, [`PrototypeBootstrap.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs) reads session character IDs from `GameManager`, instantiates the selected prefabs as `Fighter_P1` and `Fighter_P2`, and injects their references directly into `MatchManager.SetFighterReferences()` and `HUDController.SetFighterReferences()`. All combinations (Goku vs. Saitama, Saitama vs. Goku, and Mirror matches) now deploy with 100% fidelity."*

---

### Q56: How does your dynamic fighter deployment pipeline prevent race conditions between `Destroy()` and `Awake()`?
**Answer:**
> *"In Unity, calling `Destroy(gameObject)` is deferred until the end of the current frame.
> 1. **The Race Condition:** If an old fighter exists in a scene, calling `Destroy(oldFighter)` in `Start()` means the object is still active in the hierarchy for that entire frame. If other scripts run `FindObjectsByType` or `GameObject.Find` in `Awake()`, they will discover the obsolete fighter that was scheduled for deletion.
> 2. **The Prevention Mechanism:**
>    - **Immediate Deactivation:** When `PrototypeBootstrap` sweeps the scene for legacy fighters, it calls `go.SetActive(false);` immediately before `Destroy(go);`. This excludes them from active physics, trigger collisions, and `FindObjectsInactive.Exclude` queries.
>    - **Direct Dependency Injection:** Rather than having `MatchManager` and `HUDController` query the scene tree using brittle string names, `PrototypeBootstrap` explicitly passes the newly instantiated GameObject references directly via `SetFighterReferences(p1Health, p2Health, p1Energy, p2Energy)`, completely bypassing the need for scene searches."*

---

### Q57: How did you implement real-time dynamic key re-binding in Unity's new Input System, and how does it compare to Godot's `InputMap`?
**Answer:**
> *"In Unity's modern Input System (`UnityEngine.InputSystem`), input devices are represented by hardware device classes (`Keyboard.current`).
> 1. **Data Architecture:** We constructed `InputSettingsManager.cs`, a persistent static manager that maps our `FighterAction` enum to `Key` enums.
> 2. **Key Interception Pipeline:**
>    - When a player clicks a key binding button, `OptionsMenuController` enters a 'listening' state, pulsing the button label with `[ PRESS KEY ]`.
>    - In `Update()`, the controller iterates through `Keyboard.current.allKeys` to detect which key triggered `wasPressedThisFrame`.
>    - Once detected (and verified not to be `Escape`), the new key is assigned, saved immediately to `PlayerPrefs`, and formatted with readable typography (e.g. arrows display as `←`, `→`, `↑`, `↓`).
> 3. **Godot vs Unity Architectural Comparison:**
>    - In **Godot**, actions are registered in `ProjectSettings` (`input/*`). Runtime rebinding is achieved using `InputMap.action_erase_events(action)` and `InputMap.action_add_event(action, new_event)`, then saved to `user://settings.cfg` via `ConfigFile`.
>    - In **Unity**, since actions are read via C# code querying `Keyboard.current[key]`, we manage key mapping in our centralized `InputSettingsManager` and serialize values via `PlayerPrefs`."*

---

### Q58: Why did you separate Single Player (9 keys) and 1v1 Local Duel (18 keys) into two distinct profiles instead of a single global keymap?
**Answer:**
> *"This directly addresses how input ergonomics differ between solo play and shared-keyboard play:
> 1. **Solo Player Ergonomics (Vs CPU & Future LAN):** In Single Player (or future networked LAN matches), Player 1 has full ownership of the entire keyboard. They can spread their hands out comfortably across WASD and the right side of the board without worrying about colliding with another human hand.
> 2. **Shared Keyboard Constraint (1v1 Local Duel):** When two humans play locally on the exact same keyboard, Player 1 must stay strictly on the left half (`WASD + FCEQX`) while Player 2 sits on the right half (`JLIK + HNUOM`).
> 3. **The Solution:** By maintaining two independent profiles (`SinglePlayerKeys` and `LocalDuelP1/P2Keys`), players can customize their solo setup however they prefer without corrupting or breaking their 2-player local battle setup. `PlayerInputHandler` checks `GameManager.Instance.CurrentGameMode` at runtime and automatically routes to the appropriate profile."*

---

### Q59: Why did you design the Local Duel options as a 3-column combined table rather than two separate boxes, and how does this prevent player input collisions?
**Answer:**
> *"When configuring two players on one keyboard, separate columns or disjointed panels create cognitive overload:
> 1. **The Collision Trap:** In a shared keyboard fighting game, if Player 1 maps an attack to `U` while Player 2 has `U` mapped to their special skill, pressing `U` during combat will trigger both players' actions simultaneously.
> 2. **The Combined Table UX:** By structuring the screen as a unified 3-column table:
>    `[ ACTION ] | [ PLAYER 1 (LEFT SIDE) ] | [ PLAYER 2 (RIGHT SIDE) ]`
>    both players can see their mappings aligned horizontally on the exact same row. It allows players to visually audit keys in seconds and eliminates cross-talk conflicts before the match starts."*

---

### Q60: How does your UI architecture achieve clear, intuitive navigation without displaying cluttered button instructions on screen?
**Answer:**
> *"In authentic arcade design, good UI communicates state purely through visual feedback rather than instruction labels:
> 1. **Blinking Arcade Cursor (`►`):** The focused element dynamically receives a leading `►` symbol. When focus shifts via W/S or arrow keys, the prefix is stripped from the old element and assigned to the new one.
> 2. **Pulsing Neon Gold Focus Frame:** Rather than a static box, the selected element's `Outline` component continuously blends between Solar Gold (`#FFD54F`) and Pure White (`#FFFFFF`) using a sine-wave function (`Mathf.Sin(Time.unscaledTime * 7f)`).
> 3. **Directional Multiplexing:** Vertical inputs (`W/S`, `Up/Down`) shift the menu row index; horizontal inputs (`A/D`, `Left/Right`) nudge volume sliders by $\pm 5\%$ or swap between Player 1 and Player 2 columns.
> 4. **Mouse/Keyboard Harmony:** Hovering with the mouse immediately syncs the focus index, allowing seamless switching between controller, keyboard, and mouse without desynchronizing visual focus."*

---

### Q61: Why did you move key bindings into dedicated sub-screens instead of keeping them permanently open beneath the audio sliders?
**Answer:**
> *"This was a critical UI/UX refactor driven by information density and visual ergonomics:
> 1. **The Single-Screen Density Trap:** Squeezing volume sliders, two profile tabs, and 9 to 18 key rows onto one static screen forced typography down to $12\text{pt}\text{–}14\text{pt}$, creating visual clutter and eye strain.
> 2. **The 3-Tier Hierarchy:** By breaking the system into a Main Options Hub (audio sliders and sub-menu launchers) and dedicated sub-screens for Single Player and 1v1 Local Duel, each view has ample screen estate.
> 3. **The Result:** We expanded the window to $1440 \times 880\text{px}$ and increased font sizes by **$80\%$** ($22\text{pt}\text{–}36\text{pt}$ across all labels and buttons). The interface is now instantly legible from across the room, perfectly matching 90s arcade clarity."*

---

### Q62: How did you solve the empty arena bug when transitioning from Character Select into the combat stage?
**Answer:**
> *"This was a fascinating multi-scene Unity engine lifecycle bug with three root causes:
> 1. **`RuntimeInitializeOnLoadMethod` Limitation:** Attributes like `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` only fire **once** when the game starts up. Because our game boots into `MainMenuScene`, the method ran inside the main menu (where it safely aborted). When the player picked characters and loaded `SampleScene`, that method never executed again!
> 2. **The `SceneManager.sceneLoaded` Active Scene Gotcha:** We subscribed to `SceneManager.sceneLoaded`, but Unity does not update `SceneManager.GetActiveScene()` until after all event listeners finish running. When our deployer checked `SceneManager.GetActiveScene().name == 'MainMenuScene'`, it evaluated to true, falsely believing it was still inside the main menu!
> 3. **The Fix:** We updated the guard to check the actual loaded scene (`scene.name`) and the deployer's own scene (`gameObject.scene.name`). We explicitly called `SceneManager.SetActiveScene(scene)` and `SceneManager.MoveGameObjectToScene()`, ensuring that `[PrototypeBootstrap]` and all spawned fighter GameObjects belong strictly to the new combat arena scene."*

---

### Q63: How does `MatchManager` avoid race conditions when fighters are instantiated dynamically at runtime?
**Answer:**
> *"In amateur games, match managers often assume fighters exist statically in the scene file and start round timers inside `Start()` or `Awake()`. If fighters are spawned dynamically, this throws `NullReferenceException` or starts Round 1 before characters even appear.
> 
> We solved this through a stateful readiness handshake:
> 1. **Default `WaitingToStart` State:** `MatchManager` initializes its state to `MatchState.WaitingToStart`.
> 2. **Conditional Round Kickoff:** In `MatchManager.Start()`, it inspects whether both fighters are discovered. If null, it logs a waiting notice and does not tick the round timer.
> 3. **Direct Injection Handshake:** When `PrototypeBootstrap` instantiates the fighters, it calls `MatchManager.SetFighterReferences(p1Hp, p2Hp, p1Ki, p2Ki)`. `MatchManager` binds the death events and transitions `WaitingToStart -> RoundInProgress`, kicking off Round 1 cleanly with zero frame delay."*

---

### Q64: How did you handle audio mixing, dynamic range, and vocal calibration across music, menu sounds, and character combat shouts?
**Answer:**
> *"Audio balancing in fighting games is notorious for loudness wars where vocal shouts and menu sounds overpower the soundtrack. We addressed this through a four-tier mixing pipeline:
> 1. **Full Headroom for BGM ($1.00\text{f} / 100\%$):** We set the default background music to full amplitude ($1.00\text{f}$) so players experience the full dynamic range of our retro arcade soundtrack right out of the box.
> 2. **Anti-Ear-Fatigue Menu SFX (Reduced by $\sim 50\%$):** Rapidly cycling through pause or victory menu items with loud sounds causes immediate ear fatigue. We dropped navigation SFX volume from $0.45\text{f}$ down to $0.22\text{f}$ and applied a linear amplitude decay envelope (`Mathf.Sign(...) * (1f - (t / duration))`) to eliminate the harsh click on audio cutoffs.
> 3. **Move-Slot Semantic Correctness & Acoustic Balancing:**
>    - Goku has no special skill (`voiceSkillClip = null`); his signature Ultimate (Ki: 80, `Q`/`O`) is **Kamehameha**, mapped to `voiceUltimateClip`. Because the raw WAV recording has a near-clipping digital ceiling (peak `0.906`), its multiplier was calibrated down to `0.50f` with an explicit safety clamp.
> 4. **Equalizing Softer Vocal Stems (Saitama Boost):** Saitama's vocal recordings (`consecutive_normal_punches.mp3` and `serious_punch.mp3`) have softer vocal gain that previously got swallowed by heavy hit effects. We boosted his multipliers to `2.20f` (Skill) and `2.50f` (Ultimate) so both fighters have balanced vocal presence in the match.
> 5. **Single-Pass Master Audio Bus Scaling:** We maintain one-shot `AudioSource.volume` at `1.0f` and apply `InputSettingsManager.SFXVolume` strictly inside `PlayOneShot()`, eliminating the double-multiplication bugs common in game audio."*

---

### Q65: How did you fix character sprite bouncing during special move animations in 2D pixel art?
**Answer:**
> *"In 2D fighting games, sprite bouncing occurs when animation frames have different image dimensions or irregular transparent padding below the character's feet.
> 
> 1. **The Root Cause:** Goku's walking and punching sprites were exported onto a $128 \times 128\text{px}$ canvas with feet at baseline row $y = 104$ ($23\text{px}$ bottom padding). However, his Kamehameha skill frames were exported onto an irregular $90 \times 79\text{px}$ canvas with only $14\text{–}20\text{px}$ padding. Because the pivot was calculated based on the $128\text{px}$ assumption, Goku's sprite shifted $20\text{px}$ upwards into the air during the attack, making him bounce on and off the floor.
> 2. **The Fix:** We wrote a batch asset processor in Python to standardize all 44 Goku frames onto identical $128 \times 128\text{px}$ canvases, aligning the ground contact point to row $104$ and the stance center to column $64$. In `FighterAnimator.cs`, the baseline pivot `Vector2(0.5f, 23/128)` now applies uniformly to every single frame, ensuring rock-solid foot planting."*

---

### Q66: How do you prevent fighting game AI from spamming actions at 60 FPS while keeping difficulty challenging?
**Answer:**
> *"In Unity, code in `Update()` runs 60 times every second. A rookie mistake in AI development is writing probabilistic rolls like `if (Random.value < 0.15f) JumpPressed = true;` inside `Update()`. While $15\%$ sounds modest, evaluating it 60 times a second guarantees roughly 9 jumps every single second, turning the CPU bot into an erratic pogo-stick!
> 
> We resolved this by implementing **explicit action cadence timers**:
> 1. **Jump Cooldown Gate:** Added a `jumpCooldownTimer` enforced at $2.0\text{s}\text{–}3.2\text{s}$, ensuring jumps only happen with deliberate tactical intent (e.g. jumping over an incoming projectile or corner escape).
> 2. **Attack Cadence & Rhythm:** Throttled basic attacks with an `attackCooldownTimer` ($0.25\text{s}\text{–}0.50\text{s}$) that simulates human reaction cadence and allows for natural hit-confirm combos.
> 3. **Opponent-Relative Footsies:** Neutral spacing shifts back and forth relative to the opponent (`dirToOpponent`) rather than oscillating in absolute world coordinates, creating authentic boxing footsies."*

---

### Q67: Why did the combat background music fail to play during Round 1, and how did you resolve the scene transition race condition?
**Answer:**
> *"This was a classic event-timing race condition during asynchronous scene loading:
> 1. **The Root Cause:** When transitioning from Character Select into the combat stage, `PrototypeBootstrap` deployed the fighters and invoked `MatchManager.StartRound(1)`. This fired the `OnRoundStart(1)` event immediately. However, `MatchAudioController` in the scene had not yet completed its `OnEnable()` subscription or was blocked by a stale `CurrentState == GameState.MainMenu` guard. Because `MatchAudioController.Start()` was completely empty, it sat in silence during the entire first round until Round 2 fired a new event.
> 2. **The Self-Healing Solution:** We implemented a two-part fix:
>    - **Scene-Aware Guarding:** Replaced the brittle `GameState.MainMenu` flag with a direct scene check (`gameObject.scene.name == 'MainMenuScene'`).
>    - **Stateful Self-Healing in `Start()`:** In `MatchAudioController.Start()`, we check `if (matchManager.CurrentMatchState == MatchState.RoundInProgress && !musicSource.isPlaying) PlayBattleMusic();`. If the event was missed during scene load, `Start()` catches the in-progress round and immediately kicks off the battle soundtrack with zero frame silence."*

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
> - Every frame is normalized to a fixed canvas ($128 \times 128$ for Goku, $160 \times 160$ for Saitama).
> - The lowest foot pixel is strictly anchored to a standardized baseline coordinate ($Y = 105$ for Goku, $Y = 135$ for Saitama).
> This guarantees that regardless of whether Goku is idling, walking, flinching in hitstun, or transforming into Super Saiyan, his feet stay locked to the arena floor."*

---

### Question 30: How did you implement the dramatic slow-motion K.O. sequence without causing animations to stutter or skip frames?
**Answer for Judges:**
> *"In authentic arcade fighters (Street Fighter, Guilty Gear), the final knockout blow triggers cinematic slow motion to let the impact register before the next round begins.
>
> However, a classic Unity beginner trap is simply setting `Time.timeScale = 0.28f`. Because sprite animation scripts typically accumulate `Time.deltaTime`, slowing the physics engine by 72% makes the animation update 72% slower! An 11-frame knockout collapse that should look smooth turns into a choppy, slideshow-like 2 FPS crawl.
>
> We solved this through **Decoupled Animation Delta Time (`Time.unscaledDeltaTime`)**:
> 1. In `MatchManager.cs`, we orchestrate a 4-phase sequence:
>    - **Phase 1 (Slow-Mo Collapse):** Loser enters `Knockout`, `Time.timeScale` drops to $0.28\text{f}$, triggering screen shake and an impact flash.
>    - **Phase 2 (Decoupled Playback):** In `FighterAnimator.cs`, `PlayOnce` uses `Time.unscaledDeltaTime` for knockout and victory states, dynamically adjusting FPS (`ko.Count / 1.7f`) so all 11 frames play smoothly across the 1.8-second slow-motion window.
>    - **Phase 3 (Floor Impact & Normalization):** When the loser hits the mat, `Time.timeScale` snaps back to $1.0\text{f}$, triggering a floor dust puff.
>    - **Phase 4 (Staggered Celebration):** The winner then begins their celebratory victory loop while the loser stays pinned on the ground. A 2.8-second dramatic hold lets players absorb the win before advancing."*

---

### Question 31: Why did Goku's Kamehameha jump 16 pixels into the air, and how did you debug and fix the sprite sheet ripping artifact?
**Answer for Judges:**
> *"This was a fascinating pixel-level forensic bug:
> 1. **The Bug:** During Kamehameha charge frames (`ultimate_1..3`), Goku hopped 16 pixels straight up into the air, hung hovering, and then crashed down on frame 4.
> 2. **Root Cause Analysis:** We wrote a Python inspection script to analyze the alpha channels across all raw frames. We discovered that in frames 1, 2, and 3, there were 6 stray non-zero pixels from an adjacent sprite below Goku on the original rip sheet at $Y = 73..74$. Because our previous normalization script calculated the bounding box bottom and aligned it to baseline $Y = 105$, those 6 stray pixels were anchored to $Y = 105$, which pushed Goku's actual feet (at $Y = 58$) up to $Y = 89$ (16 pixels into the air!).
> 3. **The Solution:** We cropped all raw frames strictly at $Y < 60$ (discarding the rip artifact) and locked Goku's back planted foot to a fixed canvas coordinate ($X = 41$, $Y = 105$). This completely eliminated the vertical 16px hop and fixed horizontal stance skating."*

---

### Question 32: How did you create an authentic Dragon Ball Z spiky flame aura instead of generic particle puffs?
**Answer for Judges:**
> *"Authentic Dragon Ball Z Super Saiyan auras are not soft radial smoke puffs; they are violent, jagged, spiky golden flame cocoons that lick upward around the fighter's silhouette.
> We implemented this in `FighterKiAura.cs` using a **Dual-Layer Oscillating Shell**:
> 1. **Spiky Flame Cocoon:** A custom high-res sprite (`dbz_spiky_aura.png`) with 26 jagged upward flame spires and a white-hot inner core. In `Update()`, we apply high-frequency dual-Perlin noise ($24\text{–}28\text{ Hz}$) to scale and alpha, creating raging, licking fire flicker.
> 2. **Speed-Streak Sub-Emitter:** A particle system emitting vertical stretched needle particles shooting rapidly upward from the feet past the head.
> 3. **Dynamic Palette Tinting:** Because the base sprite is authored in master grayscale with alpha, Unity's `SpriteRenderer.color` tints it with 100% color vibrancy: Radiant Gold for Super Saiyan Goku, Electric Cyan for Base Goku meditation, and Blazing Crimson for Saitama."*

---

### Question 33: How do you synchronize attack animations and hitboxes with voice audio clips so moves don't finish before the character stops speaking?
**Answer for Judges:**
> *"In fighting games, a common immersion-breaking bug is when a character finishes their special move animation and returns to their idle stance while their voice line is still shouting in the background (e.g. Goku walking around while still screaming 'ME... HA!').
>
> We resolved this through **Audio-Calibrated State Timers**:
> 1. We inspected the exact duration of each vocal asset (e.g., `goku_voice_kamehameha.wav` is $3.36\text{s}$, `consecutive_normal_punches.mp3` is $2.70\text{s}$).
> 2. In `FighterController.cs`, we matched `attackTimer` directly to the clip duration ($3.36\text{s}$).
> 3. We partitioned the attack into exact acoustic phases:
>    - **Startup Phase ($2.15\text{s}$):** Goku holds his charging pose in cupped hands while shouting the chant ('KA... ME... HA... ME...').
>    - **Active Phase ($1.21\text{s}$):** Hitbox activates and beam fires exactly as 'HA!' explodes.
> 4. In `FighterAnimator.cs`, the 10-frame animation playback rate is mathematically derived from `(float)frames.Count / attackDuration`, ensuring the sprite sequence and audio finish at the exact same millisecond."*

---

### Question 34: Why was Saitama jumping erratically in his victory animation, and how did you implement his celebratory jump with single-turn round transitions?
**Answer for Judges:**
> *"When creating sprite animations from fighting game sprite sheets, celebratory animations often depict a dynamic gesture (e.g. crouching down, jumping high into the air with a victory punch, and landing).
>
> In our prototype, two flaws originally ruined this sequence:
> 1. **Looping Incompatible Keyframes:** Because `FighterAnimator` was set to `PlayLoop`, Saitama repeatedly collapsed to the floor and sprang into the air in an erratic, continuous pogo-stick cycle for 4 seconds.
> 2. **Rigid Round Delay:** The round manager paused for an arbitrary 4.0 seconds regardless of how long the celebration actually took.
>
> We resolved this through **Single-Turn Celebration & Mathematical Canvas Anchoring**:
> 1. In `FighterAnimator.cs`, victory celebrations play via `PlayOnce` rather than `PlayLoop`, completing exactly one turn ($1.50\text{s}$ at 6 FPS) and cleanly holding the final proud pose.
> 2. In `MatchManager.cs`, `RoundEndSequence` waits for `winnerAnimator.VictoryDuration`. The instant that single turn concludes, `CheckWinCondition()` is called, immediately initiating the next round!
> 3. For the jump arc itself, we anchored the 9 frames mathematically on our $160 \times 160\text{px}$ canvas ($Y = 135 \to 120 \to 104 \to 135$), delivering an authentic airborne leap ($31\text{px}$ off the floor) and clean landing with zero physics jitter."*


---

### Question 35: What happens when two fighters hit each other at the exact same frame (Double-Hit Clash), and how did you prevent the game from getting permanently frozen in slow-motion?
**Answer for Judges:**
> *"When two fighters trade hits simultaneously in fighting games, both hitboxes trigger HitStop (a micro-freeze that drops `Time.timeScale` to $0.04\times$ physics speed for visceral impact).
>
> In an unshielded coroutine implementation, Hit 1 sets `Time.timeScale = 0.04f` and records `prevScale = 1.0f`. When Hit 2 lands a fraction of a second later, it cancels Hit 1's coroutine and starts a new one. The new coroutine reads `Time.timeScale`, which is ALREADY $0.04\text{f}$! When Hit 2 finishes, it restores `Time.timeScale` back to $0.04\text{f}$, trapping the entire game in slow-motion forever.
>
> We solved this through **Re-Entrant HitStop Management** in `VFXManager.cs`:
> 1. We maintain an `isHitStopActive` state boolean.
> 2. We only sample `originalTimeScale` when entering hitstop from normal gameplay (`!isHitStopActive` and `Time.timeScale > 0.1f`).
> 3. If a second hit lands while hitstop is active, we refresh the freeze timer without overwriting the cached baseline scale.
> 4. On completion, `Time.timeScale` safely restores to `1.0f`. We also added an `OnDisable()` safeguard to ensure timeScale never leaks across scenes."*

---

### Question 36: Why were knockbacks sending fighters flying off the stage, and how did you balance vertical lift across different attack types?
**Answer for Judges:**
> *"Our fighting arena is 17 world units wide ($X = -8.5$ to $+8.5$). Originally, our hitbox controller applied a monolithic vertical velocity of $3.2\text{ m/s}$ to every hit, alongside massive horizontal velocities ($24\text{f}$ on Serious Punch, $6\text{f}$ on each punch of Saitama's 15-hit flurry).
>
> This caused two gameplay issues:
> 1. Simple light jabs launched opponents into the sky, breaking combo opportunities.
> 2. Saitama's Consecutive Normal Punches blasted the target up and out of the flurry on punch #1, making the remaining 14 punches whiff completely.
>
> We resolved this by decoupling knockback into distinct horizontal and vertical components (`knockbackX` and `knockbackY` in `ConfigureAttack`):
> - **Light Jab:** Tuned to $2.2\text{f}$ horizontal, $0.8\text{f}$ vertical for responsive ground flinching.
> - **Saitama Flurry:** Tuned to $1.2\text{f}$ horizontal, $0.4\text{f}$ vertical. This traps and juggles the target directly in front of Saitama throughout all 15 punches.
> - **Heavy & Ultimates:** Scaled appropriately ($5.0\text{f}$ Heavy, $8.5\text{f}$ Kamehameha, $12.0\text{f}$ Serious Punch), preserving cinematic impact without blasting fighters off-screen."*

---

### Question 35: How does the game guarantee that switching between Base Goku and Super Saiyan does not cause sprite popping, floating feet, or visual foot sliding?

**Answer:**
1. **Shared 128x128 Canvas & Baseline Standard (Y = 105):**
   - In 2D pixel-art fighting games, raw sprite crops vary wildly in dimensions (e.g. an idle pose might be 34x63px, a crouch 47x40px, and a kick 63x54px).
   - If imported with naive center/bottom pivots, the character's feet will hop vertically by 10-20px on every state change.
   - We normalize every single frame across both Base and Super Saiyan forms onto a uniform 128x128px canvas where the lowest grounded foot pixel is pinned strictly at baseline $Y = 105$.
   - In `FighterAnimator.cs`, `pivot` is set to `(0.5f, (128f - 105f) / 128f) = (0.5f, 0.1797f)`. This guarantees the character's feet touch the exact same world-space floor coordinate ($Y = 0$) across all animations and form transformations.

2. **Fixed Back-Foot Anchoring for Directional Attacks:**
   - When Goku executes his punch combo, his fist lunges forward by 19 pixels.
   - Centering by image width causes the entire sprite—including the planted foot—to shift backward, causing "ice-skating".
   - Instead, the planted back foot is locked to $X = 42$, allowing the punch to reach outward to $X = 105$ without the grounded foot budging.

3. **Dynamic Frame-Rate Normalization:**
   - Attack animations adapt their playback speed dynamically using:
     $$\text{speed} = \frac{\text{frameCount}}{\text{moveDuration}}$$
   - A 6-frame Super Saiyan punch flurry completes in $0.44\text{s}$ ($13.6\text{ FPS}$), while a 7-frame heavy kick sequence plays at $8.0\text{ FPS}$ over $0.88\text{s}$.
   - The 12-frame knockout sequence unhurriedly unfolds across the full $3.8\text{s}$ slow-motion collapse window ($3.15\text{ FPS}$), perfectly synchronized with hit impacts and camera shake.

4. **Godot Equivalent:**
   - In Godot, this is equivalent to configuring an `AtlasTexture` with uniform region cell sizes or adjusting the `offset` vector on an `AnimatedSprite2D` to align the feet of each frame with the node's origin `(0, 0)`.


---

### Question 37: Why did you remove particle effects from the Ki aura, and how does the pure sprite flame cocoon work?
**Answer for Judges:**
> *"In 2D retro fighting games, pixel-art readability is paramount. Standard Unity particle systems emit billboarded, non-pixelated textured quads that clash with the sharp 16-bit aesthetic and obscure character silhouettes during combat.
>
> We replaced particle streaks with a pure sprite-driven oscillating flame shell (`FighterKiAura.cs`):
> 1. A hand-drawn spiky flame texture (`dbz_spiky_aura.png`) wraps snugly around the fighter's silhouette, anchored at their feet.
> 2. In `Update()`, high-frequency sine/cosine oscillation ($16\text{ Hz}$) subtly flickers the X and Y scale while alpha pulses between $0.80$ and $0.98$, perfectly simulating raging anime fire.
> 3. Dynamic single-pass color tinting adjusts the aura to Super Saiyan Gold, Base Goku Cyan, or Saitama Crimson without any particle emitter overhead or visual noise."*


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
> - In `TickRoundTimer()`, if the timer reaches 0 and both fighters have $> 0\text{ HP}$, we check `!isOvertime`.
> - We set `roundTimer = 100f`, set `isOvertime = true`, fire `OnOvertimeStarted`, and trigger a screen flash and camera shake.
> - The HUD controller catches this event, changes the timer digits to Warning Gold (`#FFD726`), and updates the round title to 'OVERTIME • SUDDEN DEATH'."*

---

### Question 36: Why can traditional fighting game hitstop (freeze frames) break rapid multi-hit moves like flurry punches, and how did you resolve it?

**Answer:**
1. **The Multi-Hit Hitstop Trap:**
   - In 2D fighting games, **HitStop** (impact pause) momentarily drops `Time.timeScale` to near-zero (e.g., $0.04	ext{f}$ for $0.065	ext{s}$) to give single heavy strikes weight and impact crunch.
   - However, when applied to a rapid-fire flurry (like Saitama's 10-punch Consecutive Normal Punches), triggering hitstop on every single punch causes $10 	imes 0.065	ext{s} = 0.65	ext{s}$ of freeze time. Because the physics simulation is virtually paused, the game engine stutters violently and feels unresponsive.
2. **Selective Hitstop Parameterization:**
   - In `HitboxController.cs` and `VFXManager.cs`, we parameterized `enableHitStop` and `shakeMultiplier`.
   - Single heavy blows (e.g., Serious Punch, Kamehameha) retain full hitstop ($0.065	ext{s}	ext{–}0.09	ext{s}$) and camera shake.
   - Rapid-fire flurries explicitly set `enableHitStop = false`, keeping `Time.timeScale = 1.0f` (100% full speed) while triggering light visual sparks and an attenuated 20% camera vibration.
3. **Discrete Hit Budgeting via Staggered Timers:**
   - Rather than relying on continuous collider overlap, `FighterController.cs` maintains a strict hit budget (`flurryHitsRemaining = 10`) and pulse timer (`flurryHitTimer = 0.23f`).
   - Every $0.23	ext{s}$, `HitboxController.ResetHitTargets()` allows exactly one new punch to register ($2	ext{ damage}$, $0.35	ext{m/s}$ micro-knockback), cleanly shutting down the hitbox once the 10-punch quota is met.
4. **Godot Equivalent:**
   - In Godot, you avoid calling `Engine.time_scale` inside the `area_entered` callback for multi-hit flurries, using an animated `MethodCallTrack` or repeating `Timer` to pulse `Area2D.monitoring` for $N$ discrete damage ticks.


---

### Question 39: How did you implement Goku's Kamehameha beam to ensure it looks like authentic DBZ anime and hits targets once across the entire screen without infinitely grinding their health?
**Answer for Judges:**
> *"In fighting games, implementing full-screen beam attacks requires solving two distinct challenges: visual authenticity and hitbox arbitration.
>
> 1. **Visual Authenticity (Two-Phase Anime Staging):**
>    - **Charge Phase (2.15s):** Goku draws his cupped hands back to his hip while chanting 'KA... ME... HA... ME...'. We spawn a pulsing Ki sphere (`kamehameha_charge_orb.png`) that expands from a spark to a crackling electric cyan sphere at his palms.
>    - **Blast Phase (1.21s):** On 'HA!', the orb erupts into a solid light-blue energy beam composed of three coordinated layers: an origin muzzle flare, a thick horizontal energy cylinder (`kamehameha_beam_body.png`) with an ultra-white core and cyan outer plasma, and an aerodynamic leading dome head (`kamehameha_beam_head.png`). We add camera rumble (0.30 intensity) and a 2-frame impact flash.
> 2. **Single-Hit AoE Arbitration:**
>    - Because the beam persists on screen for 1.21 seconds, a standard trigger would deal damage every frame (over 70 hits!).
>    - In `HitboxController.cs`, we register struck targets in a `HashSet<Transform> hitTargetsThisSwing`.
>    - When the opponent enters any part of the 8.5m beam, they take the full 45 damage and normal knockback (8.5f X / 2.2f Y) exactly once. They are immediately recorded in the set, preventing multi-frame hit recursion."*

---

### Question 40: Why did you use Left-Center sprite pivots for the beam shaft, and how does the interruption-safe architecture work?
**Answer for Judges:**
> *"Unity sprites default to a center pivot `(0.5f, 0.5f)`. If you scale a center-pivoted beam horizontally, it expands in both directions, protruding through the back of the character.
> By authoring the sprite with a **Left-Center pivot (`0.0f, 0.5f`)**:
> - The origin is anchored to Goku's palms.
> - Setting scale $X$ directly extends the beam forward across the arena.
> - Flipping direction is as simple as multiplying scale $X$ by `facingDir` (-1 or +1), automatically adapting to Goku facing left or right.
>
> For **Interruption Safety**:
> In `KamehamehaBeamVFX.cs`, the component continuously inspects `FighterController.CurrentState`. If Goku takes a counter-hit or gets knocked down while charging or firing, `CurrentState` changes to `HitStun` or `Knockout`. The VFX component detects this on the exact same frame and instantly deactivates all beam and charge sprites, ensuring no floating beams remain on stage."*

---

### Question 41: How did you implement Goku's Super Saiyan transformation to ensure zero delay, zero accidental damage, and immediate transition to the Super Saiyan Idle stance?

**Answer:**
1. **The Fighting Game Problem:**
   In anime and competitive fighting games, transformation mechanics (like Dragon Ball Z's Super Saiyan or Guilty Gear's Dragon Install) serve as power-ups rather than offensive strikes. If implemented through a standard `FighterState.Skill` state machine branch, two critical issues arise:
   - **Unintended Hitbox Damage:** Standard skill states often configure offensive hitboxes. Goku could damage an opponent simply by powering up in place, conflicting with the canonical nature of the move.
   - **Animation Lockout & Asset Drift:** If old prototype animation frames (`skill_0..3.png`) linger in the directory, the animator plays an outdated attack sequence before transitioning, causing a jarring visual delay before the player sees the Super Saiyan form.

2. **The Direct Ascension Solution in `FighterController.cs`:**
   When Goku presses Skill (`E` / `U`), the transformation pipeline executes an atomic transition:
   ```csharp
   if (isGoku)
   {
       if (!isSuperSaiyan) ActivateSuperSaiyan(15f);
       else { superSaiyanTimer = 15f; fighterAudio?.StartKiMeditationAura(); }

       if (attackHitbox != null) attackHitbox.Deactivate();
       attackTimer = 0.0f;
       attackStartupTimer = 0.0f;
       attackActiveTimer = 0.0f;
       flurryHitsRemaining = 0;
       TransitionTo(FighterState.Idle);
       return;
   }
   ```
   - **`attackHitbox.Deactivate()` & Zeroed Timers:** Guarantees zero damage and zero active hitboxes.
   - **`TransitionTo(FighterState.Idle)`:** Instead of locking into an attack animation, Goku directly enters `Idle`.
   - **Same-Frame Visual Switch in `FighterAnimator.cs`:** Because `isSuperSaiyan` is immediately set to `true`, `UpdateAnimation()` evaluates `GetFrames(ssjIdleFrames, idleFrames)`. On that very same frame, the renderer swaps to `ssjIdleFrames[0]`, presenting Goku instantly in his golden Super Saiyan stance with the crackling aura with zero ms delay.
   - **Asset Cleanliness:** All legacy `skill_*.png` frames were purged from Goku's runtime directory, eliminating redundant disk overhead and preventing sprite loader fallbacks.

3. **Comparison to Godot:**
   In Godot, this is equivalent to an `AnimationTree` with a blend state or a state machine where entering `Ascend` immediately updates an exported boolean `is_super_saiyan = true` and transitions the state machine back to `Idle` on the same frame, while calling `get_node("Hitbox").monitoring = false`.


---

## Question 38: Why was the text in the Pause and Victory menus rendering as solid colored rectangles/blocks, and how does TextMeshPro's Distance Field shader work?

### Short Answer (30-second elevator pitch):
"TextMeshPro uses Signed Distance Field (SDF) shaders where each pixel in the font atlas represents the distance to the letter's edge. Our retro font asset materials had their `_MainTex` property disconnected (`fileID: 0`), so Unity bound a default white 1x1 dummy texture. When the shader sampled this dummy texture, every pixel returned maximum distance ($1.0 > 0.5$), making the shader evaluate that every point inside the character quad was solid ink. We solved this by reconnecting `_MainTex` to the atlas sub-asset in YAML, configuring `LiberationSans SDF` as an automatic fallback for missing glyphs, sanitizing all strings to ASCII, and adding a runtime self-healing validation method in `CombatMenuController`."

### Detailed Technical Explanation:

1. **How SDF Font Rendering Works:**
   - In traditional bitmap fonts, characters are pixel grids that become blurry or pixelated when enlarged.
   - TextMeshPro pre-computes a mathematical distance transform for every character. Pixels inside the glyph have positive distance values; pixels outside have negative distance values.
   - The GPU fragment shader evaluates:
     ```hlsl
     float distance = tex2D(_MainTex, i.uv).a;
     float alpha = smoothstep(0.5 - smoothing, 0.5 + smoothing, distance);
     ```
   - If distance $> 0.5$, the pixel is inside the glyph contour and rendered opaque.

2. **The Root Cause of the Solid Block Bug:**
   - TextMeshPro font assets store the `TMP_FontAsset`, `Texture2D`, and `Material` as sub-assets inside the `.asset` file.
   - In `ArcadeClassic_SDF.asset` and `RetroGaming_SDF.asset`, the Material's `m_TexEnvs._MainTex.m_Texture` was set to `{fileID: 0}`.
   - When Unity renders a material with a null `_MainTex`, it passes an internal $1 \times 1$ white texture where $\alpha = 1.0$.
   - Because distance is evaluated as $1.0 > 0.5$ across 100% of the character quad, the shader renders the **entire bounding rectangle** of each letter as solid color.

3. **Incomplete Font Atlases & Missing Glyphs:**
   - Retro fonts like `Arcade Classic` only bake uppercase letters (`!12:ACDEGHIKLMNOPRSTUWY_`) and lack digits `0, 3-9` and symbols.
   - When a font has no fallback configured in `m_FallbackFontAssetTable`, missing characters render as tofu boxes.
   - We resolved this by chaining `LiberationSans SDF` as an explicit fallback in YAML and C#, ensuring missing glyphs always render cleanly.

4. **Comparison to Godot:**
   In Godot, font rendering is handled by `FontFile` and `FontVariation` resources. Godot manages MSDF textures and shader parameters under the hood without exposing sub-asset materials that can lose their texture pointers. Fallbacks in Godot are set directly via `FontFile.fallbacks = [fallback_font]`, allowing Godot to traverse the fallback chain automatically.


---

## Question 39: How does the dynamic 3-Round Arena Background system work, and how does your architecture prevent performance drops while rendering animated pixel fire and particle VFX?

### Short Answer (30-second elevator pitch):
"Instead of a static background image, our arena dynamically evolves across the 3 rounds: Round 1 is the Grand Martial Arts Tournament at sunset, Round 2 is the iconic Moon Battle with Earth in deep space and an active Doctor Strange Tao Mandala in the space rift, and Round 3 is the Multiverse Dimensional Cataclysm with pure Super Saiyan golden ki auroras, lightning fractures, and floating temple debris (with no mandala overlay, keeping the climax clean and impactful). The system uses an event-driven architecture subscribing to `MatchManager.OnRoundStart` to trigger a $1.2\text{s}$ dual-renderer alpha crossfade. Twin stone braziers flank the arena running an 8-frame pixel fire loop at 10 FPS and emitting upward ember particles. On heavy hits, `VFXManager` triggers an interactive flame surge ($140\%$ scale and $+12$ embers). All frames are pre-loaded in memory, resulting in zero runtime garbage collection and steady 60 FPS performance."

### Detailed Technical Explanation:

1. **Event-Driven Decoupled Synchronization:**
   - Instead of checking `MatchManager` every frame in `Update()`, `ArenaBackgroundManager` subscribes to `MatchManager.OnRoundStart` during `OnEnable()`.
   - When a new round begins, the manager executes `ApplyRoundVisuals(roundNumber)`.
   - In Godot terms, this is identical to connecting a script to a `signal round_started(round_number)`.

2. **Dual-Renderer Cross-Fading:**
   - Cutting directly between distinct stage backdrops causes jarring visual pops.
   - We utilize two overlapping `SpriteRenderer` components: `primaryBackdrop` (sorting order $-10$) and `crossfadeBackdrop` (sorting order $-9$).
   - When a round advances, `crossfadeBackdrop` takes the new stage texture, fades in over $1.2\text{s}$ via a coroutine, and then swaps onto `primaryBackdrop` before resetting to transparent.

3. **Interactive Combat Coupling:**
   - Environmental props in traditional games often feel detached from combat.
   - We wired `VFXManager.cs` to call `ArenaBackgroundManager.Instance?.TriggerBrazierSurge(1.4f)` on heavy hitsparks, screen shakes, and impact flashes.
   - Both braziers temporarily flare up by $140\%$ and burst $12$ glowing ember particles, creating the sensation that the arena is reacting to the fighters' destructive energy.

4. **Zero-GC Performance & GPU Batching:**
   - Flame frames (`Orange`, `Blue`, `Gold`) and brazier stands are loaded into pre-allocated memory lists during `Awake()`.
   - Cycling animation frames updates the sprite pointer on the existing `SpriteRenderer` without instantiating new objects.
   - Particle systems use fixed pre-allocated particle buffers (`ParticleSystemSimulationSpace.World`), preventing memory re-allocations during gameplay.

5. **Comparison to Godot:**
   - In **Godot**: You would create a `Node2D` named `Arena`, child nodes `Sprite2D` (Backdrop), `AnimatedSprite2D` (Brazier Flames), and `GPUParticles2D` (Embers). Communication is handled via `MatchManager.round_started.connect(_on_round_started)`.
   - In **Unity**: The `Arena` GameObject contains `SpriteRenderer`, `ParticleSystem`, and custom C# components (`ArenaBackgroundManager`, `BrazierFlameAnimator`), subscribing to C# events: `matchManager.OnRoundStart += HandleRoundStart;`.


---

## Question 40: How does your damage scaling and chain-canceling (Gatling combo) architecture create fairer, longer, and more strategic fighting game matches?

### Short Answer (30-second elevator pitch):
"Instead of allowing high-damage basic attacks that end rounds in 4 or 5 hits, we calibrated Light attacks to $3\text{--}4$ damage and Heavy attacks to $7\text{--}9$ damage, preserving Ultimates ($45\text{--}90$ damage) as decisive high-stakes finishers. To reward aggressive play, we implemented a decoupled Gatling chain-cancel pipeline: when a Light attack connects with the enemy hurtbox, `HitboxController` invokes `ownerFc.OnAttackHitConfirmed()`, unlocking a cancellation window that lets the player seamlessly abort recovery frames directly into a Heavy strike or Special Skill. If the player misses into air, they remain locked in recovery frames, punishing mindless button mashing."

### Detailed Technical Explanation:
1. **Prolonged Neutral & Spacing Importance:**
   - In a $100\text{ HP}$ health system, low poke damage ($3\text{--}4\%$) means neutral game interactions, spacing, and footsies dominate the match.
   - Players cannot win by simply spamming fast light jabs; they must use light pokes to confirm hits into heavy strikes or build Ki for decisive finishers.
2. **Decoupled Hit Confirmation (`OnAttackHitConfirmed`):**
   - Traditional fighting games implement complex input buffer rings.
   - Mystic Combat achieves this via a clean decoupled architecture: `HitboxController.ResolveHit()` explicitly notifies the attacking fighter via `OnAttackHitConfirmed()`.
   - `FighterController` evaluates `hasHitTargetThisSwing && !isExhausted` during `LightAttack` to permit immediate transitions to `HeavyAttack` or `Skill`.
   - On any state change, `TransitionTo()` resets `hasHitTargetThisSwing = false`, preventing stale input execution.
3. **Decisive Ultimates (The Comeback Dynamic):**
   - Goku's Kamehameha deals $45\text{ damage}$ across an $8.5\text{m}$ beam AoE.
   - Saitama's Serious Punch deals $90\text{ damage}$ with a $2.40\text{s}$ telegraphed voice chant. If landed on an exhausted enemy, the $+35\%$ Red Zone multiplier turns it into $122\text{ damage}$, guaranteeing an instant one-hit knockout.
4. **Comparison to Godot:**
   - In **Godot**, chain cancels are implemented using `AnimationTree` with `AnimationNodeStateMachinePlayback`. When the `Area2D` hitbox detects an `area_entered` signal on the hurtbox, it fires a `hit_confirmed` signal that switches a boolean condition in the state machine transition graph from `LightAttack -> HeavyAttack`. In Unity, we manage this through our custom C# state machine inside `FighterController.HandleStateTransitions()`.


---

## Question 41: How does the "Red Zone" exhaustion mechanic shift player psychology, and how does your 2D physics engine prevent frictionless gliding on knockbacks?

### Short Answer (30-second elevator pitch):
"The Red Zone triggers when a fighter expends all Ki, inflicting a $+35\%$ incoming damage vulnerability multiplier, $+0.12\text{s}$ longer hitstun, and a 5 Hz visual danger pulse. This completely transforms combat psychology: players cannot mindlessly spam special moves without risking instant death from a punish combo. For physics, we resolved the classic 'frictionless gliding' bug by applying an explicit ground deceleration vector in `FixedUpdate()` using `Mathf.MoveTowards(linearVelocity.x, 0f, 14f * Time.fixedDeltaTime)`, while using asymmetrical gravity ($1.35\times$ downward acceleration) to give snappy, responsive aerial descents."

### Detailed Technical Explanation:
1. **Centralized Vulnerability in `HealthSystem`:**
   - Instead of checking Red Zone states across every attack hitbox, vulnerability is calculated centrally inside `HealthSystem.TakeDamage()`:
     ```csharp
     if (energySystem != null && energySystem.IsInRedZone)
     {
         amount = Mathf.RoundToInt(amount * 1.35f);
     }
     ```
   - This ensures all incoming damage (beams, strikes, flurries) automatically scales up by $+35\%$ with zero logic duplication.
2. **Stun Extension for Punish Combos:**
   - In `FighterController.ApplyHit()`, exhausted targets receive $+0.12\text{s}$ additional hitstun ($0.22\text{s} \to 0.34\text{s}$ on light hits; $0.38\text{s} \to 0.50\text{s}$ on heavy hits).
   - This allows opponents to convert basic pokes into full multi-hit punish combos that would normally be blocked or escaped.
3. **Kinetic Friction vs Rigid Physics:**
   - Default Unity 2D Physics materials without friction cause high-velocity knockbacks to slide indefinitely across flat colliders.
   - We implemented active kinematic friction in `FixedUpdate()`: when grounded in `HitStun` or `KnockBack`, horizontal velocity decays to zero at $14\text{ m/s}^2$, snapping fighters to a crisp halt.
4. **Asymmetrical Gravity for Aerial Weight:**
   - In arcade fighters, floaty jumps feel unresponsive. We dynamically scale `rb.gravityScale`: when airborne and moving downward ($V_y < 0$), gravity increases by $1.35\times$ ($3.0 \to 4.05$). Takeoff remains floaty and majestic, while falling is snappy and decisive.
5. **Comparison to Godot:**
   - In **Godot**, you implement kinetic ground deceleration directly in `_physics_process()` using `velocity.x = move_toward(velocity.x, 0.0, 14.0 * delta)` and asymmetrical gravity by checking `if velocity.y > 0: velocity.y += gravity * 1.35 * delta` before executing `move_and_slide()`.


---

## Question 42: How does your 2D physics architecture prevent characters from stacking on top of each other's heads or bulldozing each other across the arena floor?

### Short Answer (30-second elevator pitch):
"To eliminate clumsy arcade physics bugs, we replaced flat `BoxCollider2D` components with vertical `CapsuleCollider2D` shapes ($1.10\text{m} \times 1.45\text{m}$). The hemispherical dome top of the capsule provides an angled normal surface, which is paired with an active anti-stacking slip velocity: whenever an airborne character is directly above the opponent, they are pushed outward at $3.0\text{ m/s}$ down to the floor, making head-standing physically impossible. To stop running fighters from bulldozing defenders, `FighterController.ApplyMovement()` checks proximity: if a fighter moves toward the opponent and the distance is $\le 1.15\text{m}$, forward velocity is clamped to zero, creating an immovable sparring boundary."

### Detailed Technical Explanation:
1. **Capsule Dome Deflection vs Flat Box Equilibrium:**
   - A box collider creates a flat horizontal surface normal $(0, 1)$ at head height ($Y = 1.9\text{m}$), allowing jumping characters to treat the opponent's head as a solid platform.
   - A vertical `CapsuleCollider2D` ($1.10\text{m} \times 1.45\text{m}$) caps the character with a rounded semicircle. Gravity and downward velocity generate lateral sliding forces against the curved surface.
2. **Deterministic Anti-Stacking Slip Vector:**
   - In `FighterController.ApplyKineticPhysics()`, we monitor relative spatial vectors:
     $$\Delta x = x_{\text{self}} - x_{\text{opp}}, \quad \Delta y = y_{\text{self}} - y_{\text{opp}}$$
   - When airborne and $\Delta y > 0.70\text{m}$ with $|\Delta x| < 0.95\text{m}$, the engine sets:
     $$V_x = \text{sign}(\Delta x) \times 3.0\text{ m/s}$$
   - This deflects the airborne jumper off the opponent's shoulder immediately down onto the stage floor.
3. **Proximity Bulldoze Prevention:**
   - Rather than relying on physics material friction (which can cause jitter or sluggish movement), we implement kinematic proximity clamping:
     ```csharp
     bool movingTowards = (dx > 0 && moveX > 0) || (dx < 0 && moveX < 0);
     if (movingTowards && Mathf.Abs(dx) <= 1.15f)
     {
         horizontalVelocity = 0f;
     }
     ```
   - The attacking player stops cleanly at the boundary without displacing the defender. Backward movement away from the defender remains $100\%$ responsive.
4. **Comparison to Godot:**
   - In **Godot Engine**, `CharacterBody2D` with a `CapsuleShape2D` is standard. Godot's `move_and_slide()` automatically slides along curved capsule surfaces. Bulldozing in Godot is prevented by checking collision slide normals or calculating distance to opponent before assigning `velocity.x`.


---

## Question 43: Why did ducking initially fail to dodge attacks, why did dynamic collider resizing cause 'infinite jumping', and how do waist-high push-boxes enable cross-overs?

### Short Answer (30-second elevator pitch):
"Two major physics and collision bugs were uncovered during combat tuning. First, `HitboxController.ProcessHit()` queried `other.GetComponentInParent<HealthSystem>()`, catching the fighter's root physical body collider (`CapsuleCollider2D`) instead of requiring `HurtboxController`. Because the physical body collider remained tall, attacks hit the body capsule and dealt damage even when the hurtbox was shrunk. Second, dynamically resizing `CapsuleCollider2D` while resting on the floor caused Box2D to generate explosive vertical depenetration impulses against the floor plane, catapulting the fighter into the air and creating an infinite jump-land loop! We solved both by strictly filtering attacks to `HurtboxController`, keeping the physical body capsule static at waist height ($0.90\text{m}$) at all times, and only dynamically resizing the non-physical hurtbox trigger ($Y_{\text{top}} = 0.675\text{m}$). Restricting bulldoze clamping to `IsGrounded` completes the setup, allowing smooth aerial cross-overs."

### Detailed Technical Explanation:
1. **The Hitbox/Hurtbox vs Physical Body Architecture:**
   - In 2D game architecture, hitboxes (damage dealers) must never intersect with physical movement colliders (world obstacles).
   - If a hitbox queries `HealthSystem` from any collider in the parent hierarchy, it treats the character's physical body as a hurtbox.
   - We corrected this by requiring `HurtboxController hurtbox = other.GetComponent<HurtboxController>(); if (hurtbox == null) return;`.
2. **The Box2D Depenetration Impulse on Dynamic Resizing ("Infinite Jumping"):**
   - When a dynamic `Rigidbody2D` resting on a static floor has its physical collider resized at runtime, Box2D detects an instantaneous geometric interpenetration with the floor.
   - To resolve the overlap, Box2D applies a massive upward velocity impulse, launching the fighter into the air.
   - Being airborne sets `IsGrounded = false`, which aborts the crouch state into `Falling`/`Idle`. Upon landing, holding down triggers the crouch again, repeating the cycle endlessly.
   - **Fix:** Keep the physical body capsule fixed and static ($1.10\text{m} \times 0.90\text{m}$) at all times. Dynamic crouching strictly modifies the trigger `hurtboxCollider` (`size = (0.75f, 0.65f)`, `offset = (0f, 0.35f)`, $Y_{\text{top}} = 0.675\text{m}$), which creates zero physical impulses.
3. **Waist-High Push-Box & Clean Cross-Over Jump-Ins:**
   - Push-boxes cover only waist height ($Y = 0 \to 0.90\text{m}$).
   - This leaves the upper torso and head open in physical space, allowing jumpers ($1.98\text{m}\text{--}2.65\text{m}$) to easily cross over.
   - Forward proximity push-locking is strictly restricted to `IsGrounded`, preserving full horizontal velocity in mid-air.
4. **Comparison to Godot:**
   - In **Godot Engine**, this separation is maintained natively via Collision Layers: `Area2D` hitboxes have their `collision_mask` set strictly to the Hurtbox layer (Layer 3), while the `CharacterBody2D` resides on the World layer (Layer 1). The physics engine never generates signals between hitboxes and bodies. In Unity, our C# component check (`other.GetComponent<HurtboxController>()`) enforces the exact same guarantee.
   - For depenetration avoidance, Godot's `CharacterBody2D` handles shapes differently via `move_and_slide()`, but altering a `CollisionShape2D` on a rigid body while resting on ground produces the same depenetration jitter. Keeping the movement shape static and animating only the `Area2D` hurtbox is standard best practice in both engines.


---

## Question 44: How does Saitama's 'Serious Punch' (Death Punch) achieve cinematic anime psychological dread while remaining competitively balanced and fair?

### Short Answer (30-second elevator pitch):
"We designed Saitama's Serious Punch as a high-risk, high-reward execution move using classic fighting game **Arcade Super Combo Freeze** architecture (*Street Fighter*, *Guilty Gear*, *Dragon Ball FighterZ*). The move is synchronized to the 3.80-second Japanese vocal line: during the 2.40-second startup buildup ('Hissatsu... Maji Series...'), Saitama coils low in the arena. At 2.40 seconds, on the vocal shout 'SERIOUS PUNCH!', the entire screen instantly plunges into a 100% solid pitch-black void (`sortingOrder = 950`) where an enlarged $720 \times 628\text{px}$ cut-in of his colossal thrusting punch flashes for a punchy 0.28-second window. The cut-in is dynamically framed towards screen center and mathematically clamped so it never exceeds Saitama's $X$ coordinate backwards. After 0.28 seconds, the black void vanishes immediately, snapping visual control back to the arena where Saitama continues his normal character sprites (`ultimate_7..13`). Crucially, to maintain competitive fairness, the damaging hitbox is strictly constrained to close melee reach ($1.4\text{m} \times 1.0\text{m}$); if the defender spaces away or neutral-jumps during the 2.40-second telegraphed windup, the 90-damage punch harmlessly whiffs!"

### Detailed Technical Explanation:
1. **The Arcade Super Freeze Pipeline:**
   - **Startup Buildup ($0.0\text{s} - 2.4\text{s}$):** Physical martial arts stance drop (`ultimate_0..5`) and braced transition frame (`ultimate_6`). Vocal line builds: *"Hissatsu... Maji Series..."*
   - **Pitch-Black Screen Freeze ($t = 2.40\text{s}$ on *"SERIOUS PUNCH!"*):** The entire screen snaps to a 100% solid pitch-black overlay canvas (`sortingOrder = 950`). The enlarged Big Punch cut-in ($720 \times 628\text{px}$) flashes in the void for $0.28\text{s}$.
   - **Dynamic Screen-Space Clamping:** Using `Camera.main.WorldToViewportPoint()`, the cut-in is oriented towards screen center in Saitama's facing direction. Its back edge is clamped so it **never exceeds Saitama's $X$ coordinate backwards** and stays comfortably within viewport bounds.
   - **Instant Snap-Back to Arena:** After $0.28\text{s}$, the black void vanishes instantly! Saitama continues his normal character punch animation (`ultimate_7..13`) in the arena with no big sprites attached to his physical body.
2. **Hitbox Discipline vs Visual Spectacle:**
   - A common rookie mistake in fighting games is scaling the physical collision box to match giant visual cut-in effects, creating unfair off-screen or anti-air hits.
   - We decoupled the visual presentation from the physics: the cut-in appears exclusively as a UI canvas overlay, while `attackHitbox` remains strictly pinned to `size = (1.4f, 1.0f)` at torso height ($Y = 0.95\text{m}$). The move can only hit opponents standing directly in point-blank melee range.
3. **Correcting the Misplaced Frame 0:**
   - The standing braced frame was originally placed at index 0 by mistake, breaking the windup flow.
   - We re-ordered the sequence so frames 0–5 represent the crouch and deep fist coil, while the standing braced frame sits at index 6 as the pre-transition pose, holding the kinetic energy just before the strike erupts.
4. **Comparison to Godot:**
   - In **Godot Engine**, this sequence would be created in an `AnimationPlayer` controlling a `CanvasLayer` (`layer = 10`) with a solid black `ColorRect` and a centered `TextureRect` for the punch cut-in, toggled via `get_tree().create_timer(0.28)`.
   - In **Unity**, `SaitamaSeriousPunchCinematic.cs` monitors `FighterController`'s `attackStartupTimer` ($2.40\text{s}$) and manages the `ScreenSpaceOverlay` canvas, micro-freeze timing, camera shake, and brazier surge.

---

## Question 45: Why was the in-game Pause key isolated to Escape only, and how did you resolve character select metadata mismatches?

### Short Answer (30-second elevator pitch):
"In competitive fighting games, binding `P` to Pause is an ergonomic hazard because Player 2's right-hand layout rests directly near `P` (on keys `I/J/K/L/U/O`), causing accidental freezes during heated exchanges. We isolated Pause strictly to `[ESCAPE]`. Furthermore, we resolved a data disconnect where Son Goku's character select card displayed `SKILL: None`: although the combat engine implemented Goku's 15-second Super Saiyan transformation (granting $+25\%$ speed, $+66\%$ light damage, $+43\%$ heavy damage), the underlying JSON files (`goku.json`) had not been updated. We updated the JSON files and implemented dynamic font auto-sizing on the select card to showcase `SKILL: Super Saiyan [Ki: 35] (+25% Spd, +Atk Buffs)`. On the Victory screen, we upgraded sub-15pt typography to $18.5\text{pt}\text{--}22\text{pt}$ for optimal couch and arcade legibility."

### Detailed Technical Explanation:
1. **Ergonomic Key Isolation:**
   - In `CombatMenuController.cs`, we eliminated `kb.pKey.wasPressedThisFrame` from the update loop. Pause is now solely bound to `Keyboard.escapeKey.wasPressedThisFrame`.
2. **Synchronizing JSON Data-Driven Schemas:**
   - In fighting game UI design, character select screens should be data-driven rather than hardcoded.
   - When Goku's Super Saiyan transformation was built in `FighterController.cs`, `goku.json` still held legacy prototype data (`"skill": { "name": "None" }`).
   - Updating `Assets/Data/Characters/goku.json` and `Assets/StreamingAssets/Characters/goku.json` ensures that any system reading character profiles receives accurate stats, Ki costs, and active buffs.
3. **Typography Scaling for Distant Viewing:**
   - In arcade cabinets and living room setups, players view screens from $2\text{m}\text{--}3\text{m}$ away.
   - The Victory screen's micro-header, epithet text, score badge, and buttons were increased to $16.0\text{pt}\text{--}22.0\text{pt}$, ensuring instant legibility upon match conclusion.
4. **Comparison to Godot:**
   - In **Godot Engine**, input actions are configured in the Project Settings Input Map (`ui_cancel` mapped to `KEY_ESCAPE`). Removing `KEY_P` from `ui_cancel` achieves the same isolation without hardcoding keys in scripts. For UI text, Godot developers configure `theme_override_font_sizes` or use a Theme resource to ensure uniform typography scale across scenes.
