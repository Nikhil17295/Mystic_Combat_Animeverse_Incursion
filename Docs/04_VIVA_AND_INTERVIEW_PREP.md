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
> 3. **Hierarchical FSM vs Flat FSM or Behavior Trees:** Behavior Trees add unnecessary traversal overhead and node boilerplate for a fast 2D fighter with instant 60 FPS frame data. Conversely, a flat FSM easily gets tangled in transition spaghetti. A Hierarchical FSM cleanly splits decision-making into two decoupled layers:
>    - **Layer 1 (Macro):** Strategic Posture (`SpacingNeutral`, `AggressiveRushdown`, `KiHoarding`, `DefensiveRetreat`, `PunishOpening`).
>    - **Layer 2 (Micro):** Concrete input actuation (virtual button triggers, pulse clearing in `LateUpdate()`)."*

---

### Q53: How do you prevent CPU AI from feeling like an unfair 'input-reading' cheat while still keeping Hard difficulty challenging?
**Answer:**
> *"Fighting game AI feels unfair when it exhibits $0\text{ms}$ frame-reading reactions (e.g. parrying every light attack on frame 1). We eliminated this through three humanization mechanics:
> 1. **Perceptual Reaction Tick ($\Delta t \approx 0.14\text{s} - 0.40\text{s}$):** Instead of evaluating the battlefield every single frame, the CPU brain evaluates decisions on a throttled timer. On Easy difficulty, this is $0.40\text{s}$, giving the player a large window to land mix-ups and jump-ins. On Hard, it is $0.14\text{s}$, giving snappy arcade reflexes that mimic a skilled human tournament player.
> 2. **Mistake & Hesitation Probability:** On Easy mode, the bot has a $35\%$ mistake rate where it hesitates or pauses in neutral, simulating human uncertainty. On Hard, hesitation drops to $8\%$.
> 3. **Combo Confirmation Logic:** Rather than instantly inputting optimal combos with mathematical perfection, the bot rolls against a `comboChance` attribute ($30\%$ on Easy vs $85\%$ on Hard) to decide whether to confirm a Light attack poke into a special skill or heavy attack."*

---

### Q54: How did you implement distinct anime-accurate fighting personalities (Goku vs. Saitama) in the AI codebase?
**Answer:**
> *"Rather than using a generic bot with different sprite skins, we subclassed `CpuFighterBrain` into character-specific classes with distinct strategic traits:
> 1. **`GokuBrain.cs` (Disciplined Martial Artist & Zoner):**
>    - **Spacing Bias:** Maintains mid-range distance ($\approx 2.1\text{m}$).
>    - **Ki Management:** Actively disengages at safe distance ($> 3.0\text{m}$) to channel meditation and charge energy.
>    - **Mid-Range Blast:** Emits Kamehameha (`SkillPressed`) from $1.4\text{m} - 2.7\text{m}$ when Ki $\ge 20$.
>    - **Hoarding Goal:** Saves up $80\text{ Ki}$ to punish vulnerable opponents with his Ultimate move.
> 2. **`SaitamaBrain.cs` (Relentless Rushdown Brawler):**
>    - **Spacing Bias:** Closes distance relentlessly ($< 1.0\text{m}$); never backs away in neutral.
>    - **Visceral Close Combat:** Pokes with heavy punches and chains into *Consecutive Normal Punches* (`SkillPressed`) point-blank.
>    - **100-Ki Lethality:** Hoards the entire $100\text{ Ki}$ bar to march up and drop the game-ending *Serious Punch* (`UltimatePressed`)."*




