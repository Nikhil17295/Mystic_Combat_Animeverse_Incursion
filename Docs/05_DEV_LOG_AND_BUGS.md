# 05. Dev Log, Common Mistakes & Bug Postmortems

> **Purpose:** A living record of technical pitfalls, rookie traps in Unity, bugs we encountered, and how we resolved them. Essential reading for viva defense and project postmortems!

---

## 1. Common Unity Pitfalls & Rookie Traps

### Trap 1: `rb.velocity` vs. `rb.linearVelocity` (Unity 6 Breaking Change)
- **The Trap:** Decades of Unity tutorials online teach: `rb.velocity = new Vector2(...)`.
- **The Bug in Unity 6:** In Unity 6 (`6000.x+`), Unity deprecated `rb.velocity` and replaced it with `rb.linearVelocity` for 2D Rigidbodies (to distinguish linear velocity from angular velocity). Using `rb.velocity` throws compiler warnings or errors in modern builds.
- **Our Fix:** We strictly use `rb.linearVelocity` across all scripts.

---

### Trap 2: Sliced Sprite Draw Mode Without 9-Slice Borders
- **The Trap:** Setting `SpriteRenderer.drawMode = SpriteDrawMode.Sliced` on a procedurally generated sprite texture without specifying border margins.
- **The Error:** Unity spams the console with:
  `"Cannot generate sliced sprite data for sprite without border."`
- **Our Fix:** In [`ArenaSetup.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/ArenaSetup.cs), we use `SpriteDrawMode.Simple` and scale the GameObject's `transform.localScale` to match dimensions, generating zero warnings.

---

### Trap 3: Unassigned Layers in Physics Queries
- **The Trap:** Calling `Physics2D.OverlapCircle(point, radius, 1 << 6)` when Layer 6 is unnamed or unassigned in `TagManager.asset`.
- **The Bug:** `IsGrounded` permanently returns `false`, causing fighters to be unable to jump or walk properly.
- **Our Fix:** We explicitly configured Layer 6 as `"Ground"` in `ProjectSettings/TagManager.asset` and added defense fallback code in [`PrototypeBootstrap.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs) that verifies the layer name at startup.

---

### Trap 4: Input Ghosting on Shared Keyboards (Laptop Keyboards)
- **The Trap:** Mapping Player 2 controls exclusively to the numeric keypad (`Keypad1`, `Keypad2`, etc.).
- **The Problem:** Many modern laptops (and 60%/75% mechanical keyboards) do not have a physical numpad! A teammate testing on a laptop would be completely unable to trigger Player 2's actions.
- **Our Fix:** [`PlayerInputHandler.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Input/PlayerInputHandler.cs) supports dual bindings:
  - Numpad cluster: `Keypad1 / Keypad2 / Keypad3 / Keypad5 / Keypad0`
  - Laptop row: `B / N / M / , / /` (Slash) or `Right Control`

---

### Trap 5: Symmetrical Sprites Make Auto-Facing Invisible
- **The Trap:** Testing `spriteRenderer.flipX = true` on a uniform colored square.
- **The Problem:** A solid colored square flipped horizontally looks 100% identical. You cannot tell if auto-facing is working or broken!
- **Our Fix:** In [`PrototypeBootstrap.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs), we procedurally generate a 16x24 pixel texture with a **cyan eye/visor** on the right side. When the fighter turns left, the eye visibly flips to the left.

---

### Trap 6: Unity 6 New Input System Blocks Legacy `Input.GetKey()`
- **The Trap:** Using `UnityEngine.Input.GetKey()` in Unity 6 when the project is configured for the New Input System package.
- **The Error:** Unity throws:
  `"InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package in Player Settings."`
  All keyboard inputs are completely blocked.
- **Our Fix:** Upgraded [`PlayerInputHandler.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Input/PlayerInputHandler.cs) to use `UnityEngine.InputSystem.Keyboard.current` (e.g. `kb.aKey.isPressed`, `kb.wKey.wasPressedThisFrame`) and set `activeInputHandler: 2` (Both) in `ProjectSettings.asset`.

---

### Trap 7: The "Play Mode Revert" Trap (Godot vs. Unity Inspector)
- **The Trap:** Modifying serialized variables in the Unity Inspector while Play Mode is active.
- **The Bug:** Unity silently discards every change made during Play Mode the exact second you press Stop (`Ctrl + P`), frustrating developers coming from Godot.
- **Our Fix:** 
  1. We built standalone **Prefabs** (`Goku.prefab`, `Saitama.prefab`) in `Assets/Prefabs/Fighters/`. Double-clicking a Prefab opens **Prefab Mode** (the direct equivalent of a Godot `.tscn` scene), where all Inspector changes are permanently saved.
  2. If you do test balance changes during Play Mode, right-click the component header $\rightarrow$ **Copy Component**, stop Play mode, right-click $\rightarrow$ **Paste Component Values**.

---

### Trap 8: The "Sterile Math UI" Trap vs. Authentic Visual Scenes
- **The Trap:** Generating UI primitives solely through script math and basic rectangular drawing algorithms.
- **The Problem:** The resulting UI looks sterile, corporate, and bland. Fighting games require crisp pixel borders, high-contrast 2-tone fills, bold typography, and authentic fighter portraits. Furthermore, generating UI purely in code robs developers and artists of the ability to visually manipulate layout, drag anchors, and tune colors in Unity's Scene view.
- **Our Fix:**
  1. Built a dedicated visual scene (`Assets/Scenes/HUDScene.unity`) and modular prefab (`Assets/Prefabs/UI/HUD_Canvas.prefab`) allowing live WYSIWYG editing in Unity Editor.
  2. Hand-processed authentic 16-bit arcade assets: square double-bordered portrait frames with *Extreme Butoden* Goku and inward-facing Saitama, a dark slate retro clock housing with the green Time Stone crystal, 2-tone health fills, and warning red damage ghost bars.
  3. Cleaned out all obsolete procedural PIL images to keep `Assets/Sprites/UI/` organized and lightweight.

---

### Trap 9: The "Disconnected Scene GameObject" Trap (Why Unity Needs Connected Prefabs)
- **The Trap:** Instantiating GameObjects in the scene purely from an Editor script (`new GameObject(...)`) without saving them as Prefabs and connecting them.
- **The Problem:** In Godot, everything in a scene is an instance of a `.tscn`. In Unity, raw GameObjects appear in black/white text in the Hierarchy with no link to a disk asset. Developers coming from Godot cannot double-click them to enter an isolated "scene editor", and any Inspector adjustments made to a raw GameObject cannot be reused across other scenes.
- **Our Fix:** In [`SceneSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/SceneSetupTool.cs), after assembling Goku, Saitama, and the HUD Canvas, we call `PrefabUtility.SaveAsPrefabAssetAndConnect()`. This writes `.prefab` files to `Assets/Prefabs/` and links the scene instances directly to them (turning them blue in the Hierarchy). Double-clicking any prefab opens **Prefab Mode** (the direct equivalent of Godot's scene tab).

---

### Trap 10: The Prefab Asset Disk Reference Trap (The "Dead Listener" Bug)
- **The Trap:** Checking `if (p1Health == null)` in `ResolveReferences()` to determine if scene fighters need to be found.
- **The Problem:** When a Canvas containing `HUDController` is saved as a Prefab Asset (`Assets/Prefabs/UI/HUD_Canvas.prefab`), Unity cannot store references to scene objects. Instead of leaving them `null`, Unity's serializer automatically maps them to the **Prefab Asset on disk** (`type: 3`, e.g. `Assets/Prefabs/Fighters/Goku.prefab`).
  At runtime, `p1Health != null` evaluates to **`true`**! However, because it points to the disk asset, `p1Health.gameObject.scene.IsValid()` is **`false`**.
  `HUDController` attached its event listeners (`OnHealthChanged`) to the inactive asset in the project folder, while the live scene instance was punched in the ring. The HUD never received health change events, leaving the health bar completely static!
- **Our Fix:** Implemented `IsInActiveScene(Component comp) => comp != null && comp.gameObject.scene.IsValid() && comp.gameObject.scene.isLoaded;`.
  If `p1Health` is not in the active scene, `HUDController` discards the dead disk reference, dynamically discovers the live `Fighter_P1_Goku` and `Fighter_P2_Saitama` scene instances, and binds to their live C# events. Also added an `Update()` watchdog check so late-spawning fighters are immediately linked.

---

### Trap 11: The Box2D Trigger-on-Trigger Blindness Trap (Why Combat Attacks Dealt Zero Damage)
- **The Trap:** Designing both the attacking `Hitbox` and the defending `Hurtbox` as 2D trigger colliders (`isTrigger = true`).
- **The Problem:** In Box2D (Unity's 2D physics backend), sensors never generate contact events when overlapping other sensors. As a result, `OnTriggerEnter2D` never fired between Hitbox and Hurtbox! Furthermore, `HitboxController` checked `if (!other.CompareTag("Hurtbox")) return;`, which caused it to discard overlaps with the opponent's non-trigger body collider.
- **Our Fix:** Replaced string tag checks with component queries: `other.GetComponentInParent<HealthSystem>()`. If the hit target is the opponent, damage, knockback, and Ki are awarded immediately. Also added `OnTriggerStay2D` so point-blank attacks land without requiring fresh motion.

---

## 2. Bug Postmortems & Solutions

### Postmortem: Backwards Hitbox Offset On Facing Inversion
- **Incident:** Player attacks (such as Saitama's punch) failed to hit the opponent even when standing point-blank and facing each other.
- **Root Cause:** In [`HitboxController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/HitboxController.cs), the hitbox position offset was hardcoded to `hitboxOffset = new Vector2(0.8f, 0.85f)`. When Saitama was positioned on the right facing left towards Goku, an offset of $+0.8\text{f}$ placed the attack hitbox behind Saitama (towards the right boundary wall), punching completely into thin air!
- **Resolution:** Added facing-direction awareness to hitbox positioning:
  ```csharp
  float facingDir = 1f;
  if (spriteRenderer != null && spriteRenderer.flipX) facingDir = -1f;
  else if (opponent != null && opponent.position.x < transform.position.x) facingDir = -1f;
  Vector2 center = (Vector2)ownerTransform.position + new Vector2(hitboxOffset.x * facingDir, hitboxOffset.y);
  ```
  Additionally, added `Physics2D.OverlapBoxAll(center, hitboxSize, 0f)` queries during active attack frames so stationary overlaps trigger damage even without continuous Rigidbody motion.

---

### Postmortem: Saitama Skill Sprite Pivot Jitter & Foot Alignment
- **Incident:** Triggering Saitama's skill ("Consecutive Normal Punches") caused his character sprite to violently jitter and shift 30 pixels backwards.
- **Root Cause:** In the sprite sheet rip, Saitama's punch flurry had red after-image effects extending to the right, causing the bounding box foot center to shift from $x = 80$ (in Idle) down to $x = 50$ (in Skill frames 0–7), then back to $x = 80$ (in frame 10). Because the sprite pivot was set to $0.5$ ($x = 80$), Saitama's body teleported backwards by 30 pixels whenever the skill started.
- **Resolution:** Re-aligned all 12 skill frames in Python so that Saitama's ground foot contact point remains locked at $x = 80.0$ across every single frame. Saitama now stands completely solid while his punch barrage extends forward naturally toward the opponent.

---

### Postmortem: Unity Editor Lockfile & Script Compilation
- **Incident:** Attempting to run Unity command-line batchmode (`-batchmode -quit`) to execute scene baking failed with exit code 1.
- **Root Cause:** The Unity Editor was already open on the desktop, creating `Temp/UnityLockfile`. Unity strictly enforces single-process access per project directory to prevent database corruption.
- **Resolution:** Rather than relying on external batchmode scripts, we embedded a versioned auto-bake check inside `[InitializeOnLoad]` in [`SceneSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/SceneSetupTool.cs). When script files are edited, Unity compiles them upon taking focus and automatically triggers `BakeGodotStyleScene()` via `EditorApplication.delayCall`. We also kept the top-bar menu item (`Mystic Combat -> Bake 2D Visual Scene (Godot-Style)`) for instant one-click manual baking.

---

### Postmortem: JSON Schema Alignment
- **Incident:** Initial C# class definition used nested structures (`data.baseStats.moveSpeed`), while the design document JSON files used a flat structure (`data.moveSpeed`).
- **Root Cause:** Unity's built-in `JsonUtility.FromJson<T>` is fast and lightweight, but it is **strictly field-name matched**. If JSON has `"moveSpeed": 5.0` but the C# class expects `baseStats.moveSpeed`, `JsonUtility` silently leaves the field at default `0` without throwing an error!
- **Resolution:** Re-architected [`CharacterData.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Data/CharacterData.cs) to match the JSON key schema 1-to-1, and created [`Vector2Data`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Data/CharacterData.cs#L145) helper class since `JsonUtility` cannot parse Unity's native `Vector2` directly from JSON objects.

---

### Postmortem: Asset Slicer URL Encoding & Variant Sourcing
- **Incident:** Goku spritesheet returned HTTP 404 in local slicer tool, and Saitama loaded a different variant sheet than the preferred artist rip.
- **Root Cause:** Web browsers encode spaces as `%20` (`DS%20_%20DSi...`). When Python's HTTP server looked for this path on Windows disk without `urllib.parse.unquote()`, the file was not found. Additionally, having multiple candidate rips for fighters requires runtime variant selection.
- **Resolution:** Applied `urllib.parse.unquote` to incoming asset request URLs and added a dynamic sheet variant dropdown to [index.html](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Tools/SpriteSlicer/index.html) with direct resolution badges.

---

### Postmortem: Sliced Frame Indexed Filenames vs. Single-Frame Assumptions
- **Incident:** Jump and meditate animations failed to display in-game, freezing on whatever prior frame was active.
- **Root Cause:** `FighterAnimator.cs` expected static single filenames (`jump.png`, `meditate.png`), but multi-frame exports from the slicer tool generated numbered index files (`jump_0.png`, `jump_1.png`...). The loader returned `null` silently.
- **Resolution:** Upgraded `LoadSequence()` to recursively collect all indexed frames (`jump_0` through `jump_n`), and added physics-driven pose selection based on `rb.linearVelocity.y`.

---

### Postmortem: Visual Over-Clutter & The Removal of Decorative Crawlers
- **Incident:** Golden dragon sprites crawling along the upper rim of the P1 and P2 health bars created visual noise and obscured the clean reading of combat health.
- **Root Cause:** In arcade fighting games, health bars require instantaneous readability ($< 50\text{ms}$ player glance time). Sinuous overlapping decorative sprites disrupted the clean horizontal silhouette of the health fill.
- **Resolution:** Removed the dragon crawlers completely. Shifted the mystical aesthetic to an ambient header frieze (`Mystic_Runic_Banner` at `y = -18`) spanning above the bars, leaving the health containers razor-sharp and unobstructed.

### Postmortem: White-Background Screenshot Cutout Artifacts
- **Incident:** Importing raw screenshots of items (like the emerald Time Stone) produced white rectangular blocks and jagged compression fringes in the HUD.
- **Root Cause:** Raw screenshot formats (JPEG/PNG without alpha) bake background colors into border pixels via anti-aliasing. A naive single-color chroma-key (`#FFFFFF`) left noticeable light halos around dark emerald facet edges.
- **Resolution:** Implemented distance-threshold keying in our Python asset pipeline (`abs(r-g) < 15 && abs(g-b) < 15 && r > 170`), cleanly discarding compression artifacts while preserving crystalline emerald facets, and upscaled using nearest-neighbor `Point` filtering to maintain retro anime sharpness.

### Postmortem: The Synchronous 0ms Round Transition Bug (Why Round 3 Never Started)
- **Incident:** When Player 1 won Round 1, the match failed to cleanly transition to Round 2; if Player 2 won Round 2, Round 3 never started, leaving the fighters stuck in lingering physics knockback.
- **Root Cause:** In `MatchManager.cs`, when a fighter's HP reached 0, `EndRound()` called `CheckWinCondition()` and `StartRound()` synchronously on the same frame in the middle of resolving the fatal attack's hit callback. Fighters never reset their positions, velocities, or state machines, so Saitama was knocked back with lingering velocities while his health snapped back to 100 on frame 1 of Round 2.
- **Resolution:** Replaced the instant snap with a 1.8s `RoundEndSequence` coroutine. Added `FighterController.ResetForNewRound(spawnPos, faceRight)`, resetting positions (`-2.2` and `+2.2`), velocities, and hitboxes. Upgraded `HUDController.ResetHealthBarsInstantly()` to wipe trailing red ghost bars on round start.

### Postmortem: Truncated Character Busts & Multi-Point Asset Composition
- **Incident:** The Saitama avatar card only showed a tight crop of his face, cutting off his iconic red fist and yellow suit.
- **Root Cause:** Using an unproportional square face crop instead of isolating his full upper bust from the uploaded reference sheet (`Screenshot 2026-10-25 112023.png`).
- **Resolution:** Keyed out the uniform `#8C4682` purple background across the entire 936×913 bust, preserving his bald head, white cape, yellow collar/suit, and clenched red fist in the foreground, scaled down proportionally into the 160×160 card and mirrored inward.

---

### Postmortem: C# Compiler Errors on Round-Reset Hookup & Unity 6 TextMeshPro API Breakage
- **Incident:** Unity Editor blocked Play Mode and scene baking with `error CS1061: 'FighterAnimator' does not contain a definition for 'PlayAnimation'` and `error CS0619: 'TMP_Text.enableWordWrapping' is obsolete (treated as error in Unity 6)`.
- **Root Cause:**
  1. `FighterAnimator` was designed as a state-reactive polling component (it reads `controller.CurrentState` during `UpdateAnimation()`). Calling an imperative string-based `anim.PlayAnimation("idle")` from `FighterController.ResetForNewRound()` broke because that method didn't exist.
  2. In Unity 6 (`6000.x`), TextMeshPro deprecated `enableWordWrapping` with an error attribute `[Obsolete(..., true)]`, requiring `textWrappingMode = TextWrappingModes.NoWrap`.
- **Resolution:**
  1. Added a first-class `public void ResetToIdle()` method on [`FighterAnimator.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterAnimator.cs) that immediately resets frame counters, timers, and snaps the sprite renderer to the first idle frame.
  2. Updated [`FighterController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs) to call `anim.ResetToIdle()`.
  3. Replaced `enableWordWrapping = false` with `textWrappingMode = TextWrappingModes.NoWrap` in [`SceneSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/SceneSetupTool.cs).
  4. Updated deprecated `FindObjectsByType<T>(FindObjectsSortMode)` calls in [`HUDController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/HUDController.cs) to modern Unity 6 syntax `FindObjectsByType<T>(FindObjectsInactive.Exclude)`. Both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll` now compile with 0 errors and 0 warnings.

---

### Postmortem: HUD Gem Sizing, Dual Point Sockets, & Layout Clearance
- **Incident:** Single 32px/48px point gems felt dwarfed by the heavy 650px health bars, and displaying only 1 gem failed to communicate Best-of-3 tournament progression (where 2 round wins are required).
- **Root Cause:** In arcade fighting game UX, round indicators should mirror the exact victory threshold ($N = 2$). A single gem could only show binary state (0 or $\ge 1$), which failed to capture when a player was leading 1–0 vs winning 2–0. Furthermore, small sizes got lost against dark backgrounds.
- **Resolution:**
  1. Upgraded each player to **two 56×56px point sockets** (`P1_PointGem_1`, `P1_PointGem_2`, `P2_PointGem_1`, `P2_PointGem_2`) spaced $65\text{px}$ apart center-to-center.
  2. Scaled the center championship relic up to **68×68px**, dynamically igniting from `gem_big_gold_hollow.png` into the authentic [`gem_big_gold.png`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Sprites/UI/gem_big_gold.png) (extracted directly from the multi-gem reference `Screenshot 2026-10-25 112541.png`) with a breathing sine-wave pulse ($1.0\times \rightarrow 1.15\times$) when match point is triggered.
  3. Scaled the emerald Time Stone up to **56×56px** atop the 160×160 rotating Tao mandala, creating clear visual hierarchy across the entire top header.

---

### Postmortem: Candidate Character Sprite Asset Clutter vs. In-Memory Dev Slicing
- **Incident:** Rapidly prototyping and testing candidate anime spritesheets (e.g. testing walk cycles, jump frames, attack bounding boxes for unconfirmed roster characters) directly inside the Unity Editor generated dozens of unneeded `.png.meta` files and triggered full Unity asset database re-imports on every test.
- **Root Cause:** Slicing tools were originally coupled directly to disk export paths (`Assets/Sprites/Fighters/{Name}/Frames/`). When developers just wanted to evaluate whether a candidate sheet was viable, copying files to disk cluttered the project repository.
- **Resolution:**
  1. Upgraded [`Tools/SpriteSlicer/index.html`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Tools/SpriteSlicer/index.html) and [`B:\Unity\GameDev_Toolbox\SpriteSlicer\index.html`](file:///B:/Unity/GameDev_Toolbox/SpriteSlicer/index.html) to decouple previewing from project disk storage.
  2. Implemented HTML5 `URL.createObjectURL(file)` via the file picker and canvas drag-and-drop. Images are ingested 100% in browser RAM.
  3. Replaced rigid static animation category buttons with a dynamic dictionary model and an interactive `➕ New Animation` creator, allowing developers to test arbitrary moves (`dash`, `taunt`, `fireball`).
  4. Added a client-side `💾 Download Slices (JSON)` coordinate backup, allowing developers to save bounding boxes locally without polluting project folders until a character is officially approved for the roster.

---

## 3. Team Best Practices Checklist

1. **Always check the Console (`Ctrl + Shift + C`):** Any red error or yellow warning must be resolved immediately before moving to the next feature.
2. **Never hardcode values:** If you need a new stat (e.g. projectile speed), put it in the character JSON file.
3. **Keep `FixedUpdate` clean:** Only physics velocities and impulses belong in `FixedUpdate`.
4. **Use `[SerializeField] private` instead of `public`:** Encapsulate variables; only expose what other scripts genuinely need to read.
