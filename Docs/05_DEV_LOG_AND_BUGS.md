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

### Bug 7: UI Font and Sprite Importer Configuration for Main Menu
- **Symptom:** Raw `.ttf` fonts and downloaded high-res `.png` textures dropped into `Assets/` cannot be directly assigned to UI `Image` or TextMeshPro components in Unity unless imported as Sprite (2D and UI) and converted to TMP Font Assets.
- **Root Cause:** By default, Unity imports new 2D images as `TextureType.Default` (standard 3D diffuse texture) rather than `TextureType.Sprite`. Similarly, TextMeshPro requires an SDF Font Asset rather than a raw TrueType font to render high-contrast arcade drop shadows and vertex gradients.
- **Solution:** 
  1. Built automated texture configuration in [`MainMenuSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/MainMenuSetupTool.cs) to ensure `menu_bg_multiverse.png` is imported as `Sprite (2D and UI)` with Bilinear filtering and no mipmaps.
  2. Created procedural fallback and automated `TMP_FontAsset.CreateFontAsset(font)` generation to preserve 16-bit arcade typography without manual editor clicks.
  3. Generated procedural 8-bit navigation and selection sound waves (`AudioClip.Create`) in [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs) to eliminate missing asset sound bugs.

---

### Bug 8: Sibling Namespace Collision with `UnityEngine.Input` (`error CS0234`)
- **Symptom:** Compiler throws `error CS0234: The type or namespace name 'GetKeyDown' does not exist in the namespace 'MysticCombat.Input'`.
- **Root Cause:** In C#, code declared inside `namespace MysticCombat.UI` resolves sibling namespaces within `MysticCombat` first. Since our project has a namespace `MysticCombat.Input` (containing `PlayerInputHandler.cs`), bare calls to `Input.GetKeyDown(...)` resolved `Input` to the namespace `MysticCombat.Input` instead of the static class `UnityEngine.Input`.
- **Solution:** Fully qualify all Unity input calls as `UnityEngine.Input.GetKeyDown(...)` and `UnityEngine.Input.GetAxisRaw(...)` inside [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs).

---

### Bug 9: Invisible TextMeshPro Text from Incomplete Sub-Asset Serialization (`m_Material: {fileID: 0}`)
- **Symptom:** TextMeshPro objects in the scene exist in the Hierarchy with text strings assigned, but render completely invisible in both Scene and Game views.
- **Root Cause:** Generating a `TMP_FontAsset` via script without explicitly creating its Distance Field Material and adding it as a child sub-asset (`AssetDatabase.AddObjectToAsset`) causes Unity to serialize an empty ~3 KB asset file where `m_Material` is saved as `{fileID: 0}`. Without a material and font atlas texture, the renderer draws zero vertices.
- **Solution:** 
  1. Configured [`MainMenuSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/MainMenuSetupTool.cs) to assign a valid `TextMeshPro/Distance Field` material, bind the atlas texture, and serialize both as sub-assets.
  2. Implemented guaranteed fallback to `LiberationSans SDF.asset` if any custom TrueType font fails to build an atlas, ensuring text is never invisible.

---

### Bug 10: Empty Character Table & Story Intro Screen Blocking Menu
- **Symptom:** Playing `MainMenuScene.unity` appeared stuck on a dark screen without showing the title marquee or menu options.
- **Root Cause:**
  1. Dynamic `TMP_FontAsset` generation created font assets with an empty character table (`m_Unicode: 95` only). Text with unbaked characters drew as blank spaces.
  2. The Story Intro overlay was enabled on startup by default, covering the entire screen with a 96% opacity dark veil for ~10 seconds while waiting for text lines that were invisible due to the font issue.
  3. `UnityEngine.UI.Shadow` components attached to TextMeshPro objects threw internal component warnings.
- **Solution:**
  1. Swapped all primary UI text to Unity's pre-baked `LiberationSans SDF.asset` (with bold uppercase styling, high letter spacing, and vertex gradients), guaranteeing 100% of characters render visibly with zero missing glyphs.
  2. Set `playIntroOnStart = false` and deactivated the story overlay by default, ensuring the Title Marquee and 4 arcade options are immediately visible and playable upon pressing Play.
  3. Removed legacy `Shadow` components from TextMeshPro objects.

---

### Bug 11: Main Menu UI Element Overlap & Negative Anchor Coordinate Collision
- **Symptom:** In `MainMenuScene.unity`, the bottom edge of the massive Title Logo collided directly with the top option (`1 PLAYER VS CPU`), obscuring the text. Additionally, flat monochromatic white text looked modern and non-arcade.
- **Root Cause:**
  1. Logo with height $546\text{px}$ positioned at $y = +160\text{px}$ had its bottom boundary at $160 - 273 = -113\text{px}$. The button group at $y = -180\text{px}$ had its first child offset by $+70\text{px}$, placing it at $-180 + 70 = -110\text{px}$, resulting in a $3\text{px}$ negative overlap.
  2. Unity's standard `TextMeshProUGUI.color` applies a single uniform tint, preventing the multi-color top-to-bottom character gradients characteristic of 90s Capcom / Technos CPS-1 arcade games.
- **Solution:**
  1. Recalculated coordinate bounds in [`MainMenuSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/MainMenuSetupTool.cs):
     - Logo: $y = +195\text{px}$, height $502\text{px}$ $\rightarrow$ bottom edge at $-56\text{px}$.
     - Button group: $y = -225\text{px}$, first item at $+45\text{px}$ $\rightarrow$ item 0 at $-180\text{px}$.
     - Creates a pristine $124\text{px}$ void clearance gap.
  2. Implemented 4-corner `VertexGradient` structures in [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs) using electric cyan/diamond silver/violet for inactive items and radiant sun gold/white/amber/crimson for active items, paired with dynamic color shimmer cycling.
  3. Added an automated title music discovery routine scanning `Assets/Audio/Music/` for `.mp3`, `.wav`, or `.ogg` tracks.

---

### Bug 12: Silent Title Screen Due to AssetDatabase Refresh Race & Serialization Timing
- **Symptom:** After downloading the audio file into `Assets/Audio/Music/`, pressing Play in the scene resulted in silence with no background music playing.
- **Root Cause:**
  1. Files added to the filesystem externally are not recognized by Unity's in-memory `AssetDatabase` until `AssetDatabase.Refresh()` is executed. In the initial bake pass, the scene file saved `titleMusicClip: {fileID: 0}` because the file was not yet in the editor index at the moment the scene was serialized.
  2. When hitting Play without re-baking, `MainMenuController` observed `titleMusicClip == null` and had no fallback mechanism to load the audio directly off disk.
- **Solution:**
  1. Implemented a dual-layer audio architecture in [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs):
     - *Primary:* Plays serialized `titleMusicClip` if assigned in Inspector / scene.
     - *Fallback:* If `titleMusicClip == null`, automatically launches `LoadTitleMusicFromDisk()` coroutine using `UnityWebRequestMultimedia.GetAudioClip("file://...")`, dynamically streaming the MP3 directly off disk at runtime.
  2. Serialized the direct asset GUID pointer (`{fileID: 8300000, guid: 99bbb0ea7d6b41cb8b7b33a81f6bab3d, type: 3}`) directly into [`MainMenuScene.unity`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scenes/MainMenuScene.unity).
  3. Added `AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport)` to [`MainMenuSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/MainMenuSetupTool.cs) to ensure editor indexes are always synchronized with the filesystem.

---

### Bug 13: Total Hardware Audio Silence Due to Missing `AudioListener`
- **Symptom:** In `MainMenuScene.unity`, neither background music nor procedural 8-bit sound effects (navigation blips, select confirm punch) produced any audible sound through the speakers, despite `AudioSource.isPlaying` returning true.
- **Root Cause:**
  1. In Unity, the `AudioListener` component acts as the virtual microphone/ears in the scene. Without exactly one active `AudioListener` in the scene, the Unity FMOD audio engine routes zero audio packets to the operating system's sound device.
  2. Scenes generated via `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene)` lack default camera rigs. When `MainMenuSetupTool.SetupCamera()` created the `Main Camera`, it attached a `Camera` component but omitted `camObj.AddComponent<AudioListener>()`.
- **Solution:**
  1. Added `camObj.AddComponent<AudioListener>()` in [`MainMenuSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/MainMenuSetupTool.cs).
  2. Serialized `AudioListener` (`!u!81 &462071808`) on `Main Camera` directly in [`MainMenuScene.unity`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scenes/MainMenuScene.unity).
  3. Implemented dynamic self-healing in [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs) during `Awake()`: if `Object.FindAnyObjectByType<AudioListener>() == null`, it dynamically attaches an `AudioListener` to the camera or canvas and sets `AudioListener.volume = 1f; AudioListener.pause = false;`.

---

### Bug 14: Alphabetical Audio Collision & Simultaneous Double Music Playback
- **Symptom:** When opening the game in the Main Menu, the combat battle music played alongside the title music, or two background tracks played simultaneously.
- **Root Cause:**
  1. `Directory.GetFiles()` returns filesystem entries in alphabetical order. Because `Double Dragon 2 - The Revenge (NES) Music - Final Battle 2.mp3` begins with uppercase 'D', while `double_dragon_ii_title.mp3` began with lowercase 'd' (or due to ASCII collation), unconstrained discovery loops `*.mp3` selected the combat battle track as `titleMusicClip`.
  2. Main menu music was not explicitly stopped before scene transitions (`StartGameMode`), risking overlap if scene unload timings staggered.
  3. Lack of strict state gating: combat BGM could trigger if state was uninitialized or additively loaded.
- **Solution:**
  1. Filter disk searches in [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs) and [`MainMenuSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/MainMenuSetupTool.cs) specifically for files containing `"title"` (`double_dragon_ii_title.mp3`), guaranteeing that battle tracks are never loaded as menu music.
  2. Added `musicSource.Stop()` in `MainMenuController.StartGameMode()` and `OnDestroy()` so title music cuts instantly when entering the arena.
  3. Hardened [`MatchAudioController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchAudioController.cs): battle music only plays when `MatchState == RoundInProgress` and `GameManager.GameState != MainMenu`.
  4. Streamlined battle music to a single unified track (`Double Dragon 2 - Final Battle 2`) across all 3 rounds to eliminate abrupt music cuts in Round 3.
  5. Implemented live Inspector volume sliders (`[SerializeField] [Range(0f, 1f)]`) updated in real time in `Update()`, with combat BGM set to `0.50`, victory fanfare to `0.85`, and title music to `0.70`.

---

### Bug 15: Audio Cutoff in Fast Combo Chains & Looping Channel Contention
- **Symptom:** In fighting games, firing rapid 3-hit punch combos abruptly truncated the sound effect of previous punches, causing a jarring "stuttering audio" sensation. Additionally, attempting to channel Ki meditation abruptly stopped landing sounds or whiff audio.
- **Root Cause:**
  1. Setting `audioSource.clip = clip; audioSource.Play();` replaces the currently playing audio buffer immediately. Any sound already in progress is brutally silenced.
  2. Forcing a single `AudioSource` to handle both transient one-shot sounds (punches, jumps, impacts) and sustained looping sounds (Ki meditation aura hum) creates channel contention where one sound always cancels the other.
- **Solution:**
  1. Standardized all transient sound playback on `AudioSource.PlayOneShot(clip, volume)`. `PlayOneShot` is non-destructive and allocates virtual audio voices, enabling overlapping punch tails and impact crunches.
  2. Implemented a dual-channel architecture in [`FighterAudio.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs):
     - `sfxSource`: Dedicated to one-shots (whiffs, hits, jumps, land, dash, red zone heartbeat).
     - `auraSource`: Dedicated secondary child GameObject with its own `AudioSource` strictly managing the seamless looping Ki meditation aura hum (`sfx_ki_charge_loop.wav`).

---

### Bug 16: Input-Bound Audio Desync & Fragile Runtime Path Guessing
- **Symptom:** Pressing the jump key (`W`) while falling or in mid-air played the jump sound repeatedly. Mashing punch keys while in hit-stun or recovery played whoosh sounds even though no attack occurred. Character swapping was fragile, and runtime path guessing (`UnityWebRequest`) could load incorrect sounds behind the scenes.
- **Root Cause:**
  1. Triggering audio directly on raw keypresses (`if (inputHandler.JumpPressed) fighterAudio.PlayJump()`) tightly couples audio to user input instead of the physical state of the character.
  2. Bypassing Inspector serialization with disk-scraping runtime coroutines (`EnsureAudioClipsLoaded`) was opaque and error-prone.
  3. Lack of character audio profiles: Goku and Saitama shared identical hard-coded sounds rather than unique combat audio sets.
- **Solution:**
  1. **State-Driven Audio:** Moved all sound triggers out of `HandleGroundedTransitions()` into `FighterController.TransitionTo(FighterState newState)` and the grounded landing check. Audio only executes when the state machine enters the state.
  2. **Organized SFX Library:** Extracted over 50 audio clips into `Assets/Audio/SFX_Library/` (`Anime_Powers`, `Arcade_Bells`, `Hits_Punches`, `Swishes_Whiffs`) with full Inspector waveform auditioning.
  3. **Modular Character Audio Profiles:** Rebuilt `FighterAudio.cs` with explicit serialized `AudioClip` fields and character voiceover slots (`voiceAttackGruntClip`, `voiceHurtClip`, `voiceSkillClip`, `voiceUltimateClip`). Goku and Saitama now have distinct serialized audio profiles on their respective prefabs.

---

### Bug 17: Simultaneous Bell/Music Clutter, Missing K.O. Audio Pause, and Premature Skill Placeholders
- **Symptom:** At round start, the tournament bell and combat music fired simultaneously, drowning out the opening gong. On round K.O., battle BGM kept running continuously during defeat. Pressing Skill (`E`) or Ultimate (`Q`) played generic punch swooshes before unique VFX/animations were built.
- **Root Cause:**
  1. `MatchAudioController.HandleRoundStart()` called `PlayBattleMusic()` immediately without waiting for the bell audio to ring out.
  2. `HandleRoundEnd()` only dipped volume rather than stopping/pausing the battle track, leaving round transitions anticlimactic.
  3. `FighterAudio.PlaySkillAttack()` and `PlayUltimateAttack()` fell back to `PlayHeavyWhiff()` when clips were unassigned.
- **Solution:**
  1. Boosted `bellVolume` to `1.0` and implemented an asynchronous coroutine delay (`musicStartDelayAfterBell = 0.85f`). The bell strikes into silence, echoes, and then the BGM kicks in from `0:00`.
  2. Added `musicSource.Stop()` immediately on `HandleRoundEnd()` so the K.O. gong resonates over silence. When the next round starts after position reset, the BGM starts fresh from the beginning.
  3. Blanked out `skillAttackClip`, `ultimateAttackClip`, `skillHitClip`, and `ultimateHitClip` (`None`) on all character prefabs, and updated `FighterAudio.cs` to cleanly remain silent until custom VFX + SFX are designed.

---

### Bug 18: The 1-Second Pre-Bell BGM Blip & Unwanted Vocal Grunts on Basic Attacks
- **Symptom:** At match start, battle music played for 1 second, then abruptly stopped, the round bell rang, and music restarted from `0:00`. Additionally, basic punches and attacks triggered unwanted character vocal grunts instead of pure martial arts sound effects.
- **Root Cause:**
  1. `MatchAudioController.Start()` contained an eager `PlayBattleMusic()` invocation that fired immediately on frame 1, while `HandleRoundStart()` was running its $0.85\text{s}$ delay coroutine.
  2. `FighterController.TransitionTo()` and `FighterAudio.PlayLightHit()` were triggering voice grunts on normal jabs and hits.
- **Solution:**
  1. Removed `PlayBattleMusic()` from `Start()` in [`MatchAudioController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchAudioController.cs). Battle music is now strictly initiated by `HandleRoundStart()` after the bell echoes into silence.
  2. Removed vocal grunts from basic attacks and hits in [`FighterController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs) and [`FighterAudio.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs), reserving voice acting strictly for signature special dialogue quotes.
  3. Added `crouchClip` for stance transition sound effects.
  4. Sourced authentic Dragon Ball Super Saiyan aura (`dbz_super_saiyan_aura.mp3`), clean boots landing impact (`jumpland_clean.wav`), real martial arts light punch impact (`light_punch_real.mp3`), and signature voice lines (`goku_voice_kamehameha.mp3`, `goku_voice_kaioken.mp3`, `saitama_voice_serious_punch.mp3`).

---

### Bug 19: Auditory Fatigue from Ki Aura Hum & Combat Stalling in Anime Voice Lines
- **Symptom:**
  1. Holding the meditate key (`G`/`M`) to regenerate Ki produced an overpowering Super Saiyan aura hum (`dbz_super_saiyan_aura.mp3`) that masked punch impacts and fatigued the player's ears.
  2. Goku's Kamehameha audio clip had an excessive 2.4-second trailing reverb/silence tail after the shout *"HA!"*, lasting over 6.5 seconds and stalling combat flow.
  3. Saitama had no voice acting when unleashing his signature *Consecutive Normal Punches* (`E`) or *Serious Punch* (`Q`).
- **Root Cause:**
  1. Default `kiAuraVolume` was set to `0.75f` (75%), which is too aggressive for a continuous looping layer.
  2. Raw anime audio rips contain full scene background tails and cinematic pauses meant for television pacing rather than real-time fighting game combat.
- **Solution:**
  1. Reduced `kiAuraVolume` from `0.75f` down to `0.45f` (exactly 60% of original loudness) across [`FighterAudio.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs), [`Goku.prefab`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Prefabs/Fighters/Goku.prefab), and [`Saitama.prefab`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Prefabs/Fighters/Saitama.prefab).
  2. Sliced Goku's Kamehameha at 4.2s (end of *"HA!"*), added a 50ms linear fadeout, and sped up playback to $1.25\times$ ($3.36\text{s}$) to match dynamic beam execution speeds.
  3. Sourced and processed authentic anime voice lines for Saitama:
     - `saitama_voice_consecutive_punches.wav` ($2.20\text{s}$ clean *"Renzoku Futsuu no Panchi"* shout).
     - `saitama_voice_serious_punch.wav` ($3.17\text{s}$ at $1.20\times$ speed, *"Serious Series: Serious Punch"*).
  4. Wired clips directly to `voiceSkillClip` and `voiceUltimateClip` on `Goku.prefab` and `Saitama.prefab`.

---

### Bug 20: 15-Second Spoken Dialogue Bleed on Light Punches & Input Overrides from Secondary Keys
- **Symptom:**
  1. Hitting an opponent with a Light Attack (`F` / `H`) caused random spoken voices to play in the background instead of a pure martial arts knuckle slap.
  2. Pressing `Space` triggered P1 jumps unexpectedly, and pressing Arrow keys overrode P2 movement and corrupted directional state.
- **Root Cause:**
  1. `light_punch_real.mp3` was inadvertently downloaded from an online meme soundboard clip that was 15.65 seconds long and had talking dialogue in the background.
  2. `PlayerInputHandler.cs` mapped `Space` as an alternate P1 jump key alongside `W`, and mapped Arrow keys (`UpArrow`, `DownArrow`, `LeftArrow`, `RightArrow`) as alternate P2 movement keys alongside `I/J/K/L`, creating severe input cross-talk and overriding player inputs.
- **Solution:**
  1. Completely retired and deleted `light_punch_real.mp3`.
  2. Assigned [`arcade_hit_01.ogg`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Audio/SFX_Library/Hits_Punches/arcade_hit_01.ogg) (a snappy $0.27	ext{s}$ foley punch impact with zero voice interference) to `lightHitClip` on both [`Goku.prefab`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Prefabs/Fighters/Goku.prefab) and [`Saitama.prefab`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Prefabs/Fighters/Saitama.prefab).
  3. Cleaned up legacy unused audio files in `Assets/Audio/SFX/`.
  4. Updated [`PlayerInputHandler.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Input/PlayerInputHandler.cs):
     - Removed `Space` key: P1 Jump is strictly **`W`**.
     - Removed all 4 Arrow Keys: P2 Movement is strictly **`I/J/K/L`** (`I` = Jump, `K` = Crouch, `J` = Left, `L` = Right).

---

### Postmortem: The "Double Delta" Physics Velocity Pitfall
- **Incident:** A developer attempts to make movement frame-rate independent by writing `rb.linearVelocity = new Vector2(speed * Time.deltaTime, 0)`, but the character crawls at an imperceptible snail's pace.
- **Root Cause:** In physics simulation, velocity is already expressed in **units per second**. Unity's 2D physics engine (`Box2D`) automatically integrates velocity over time by multiplying it by `Time.fixedDeltaTime` ($0.02\text{s}$) in its internal solver loop. Multiplying by `Time.deltaTime` in user code applies delta time twice ($5 \times 0.02 \times 0.02 = 0.002\text{ m/tick}$), effectively nullifying movement.
- **Resolution:**
  - When setting Rigidbody velocities, assign speeds directly in meters/second: `rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);`.
  - Reserve `Time.deltaTime` exclusively for direct `Transform` translations (`transform.position += dir * speed * Time.deltaTime`) and custom countdown timers.

---

### Postmortem: The Missing Inspector Variables & Child Selection Mistake
- **Incident:** A teammate opens Unity, selects a fighter in the Hierarchy, but cannot find the `moveSpeed`, `jumpForce`, or combat tuning fields in the Inspector, assuming serialized fields failed to compile.
- **Root Cause:** 
  1. In Unity, GameObjects follow a multi-component stack architecture rather than Godot's "One Node = One Script" model. A fighter has 8 distinct components (`Transform`, `SpriteRenderer`, `Rigidbody2D`, `BoxCollider2D`, etc.). `FighterController` is positioned lower in the stack, requiring scrolling or collapsing earlier components.
  2. Additionally, selecting child GameObjects (`Hitbox`, `Hurtbox`, `GroundCheckPoint`) displays only that child's isolated trigger colliders, not the root controller.
- **Resolution:**
  1. Always select the root entity (`Fighter_P1_Goku` or `Fighter_P2_Saitama`) in the Hierarchy.
  2. Collapse standard engine components (`Transform`, `SpriteRenderer`, `Rigidbody2D`) to bring gameplay logic to the top.
  3. Utilize the Inspector Search bar (`Ctrl + F` or typing `speed` into the 🔍 field) to instantly isolate target variables.

---

## 3. Team Best Practices Checklist

1. **Always check the Console (`Ctrl + Shift + C`):** Any red error or yellow warning must be resolved immediately before moving to the next feature.
2. **Never hardcode values:** If you need a new stat (e.g. projectile speed), put it in the character JSON file.
3. **Keep `FixedUpdate` clean:** Only physics velocities and impulses belong in `FixedUpdate`.
4. **Use `[SerializeField] private` instead of `public`:** Encapsulate variables; only expose what other scripts genuinely need to read.
---

### Postmortem: Preventing Input Leakage Between Swapped UI Panels
- **Incident:** When the player navigated to the Character Select screen and pressed `Escape` or arrow keys, both the Character Select menu and the Main Menu options responded simultaneously, causing audio double-triggering and unexpected menu resets.
- **Root Cause:** Both [`MainMenuController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs) and [`CharacterSelectController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/CharacterSelectController.cs) were polling `Input.GetKeyDown()` in their respective `Update()` methods without checking if the sibling UI panel was currently active.
- **Resolution:**
  1. In `MainMenuController.HandleNavigationInput()`, added an immediate guard clause:
     ```csharp
     if (characterSelectRoot != null && characterSelectRoot.activeSelf)
     {
         return; // Yield all input control to CharacterSelectController!
     }
     ```
  2. When returning from Character Select (`ReturnToMainMenuFromCharSelect()`), the main menu restores its visuals cleanly and resumes navigation without restarting the background music.

---

### Postmortem: Audio Bleed During Scene Transition
- **Incident:** When transitioning from the Character Select screen into the fighting arena (`SampleScene`), the Title Screen BGM briefly overlapped with the arena's opening bell sound.
- **Root Cause:** In the original prototype, `musicSource.Stop()` was called during `StartGameMode()`. Because Character Select was inserted as a pre-match screen, stopping the music on menu option selection cut the music too early; leaving it unmanaged caused it to bleed across scene loading.
- **Resolution:**
  1. Title music now plays uninterrupted through the entire Character Selection phase.
  2. `CharacterSelectController.LaunchMatchSequence()` calls `mainMenuController.StopTitleMusic()` right at the end of the `VERSUS!` countdown, ensuring total acoustic silence so the arena's opening gong can ring cleanly into silence.

---

### Bug 13: UI Coordinate Collision & Overlap in Character Select Cards
- **Symptom:** In MainMenuScene.unity's Character Select screen, the bottom of the hero title/epithet ("HOPE OF THE UNIVERSE" and "HERO FOR FUN") was visibly sliced through and covered by the blue stat box below it.
- **Root Cause (Bounding Box Math Collision):**
  1. The hero epithet label (Text_Epithet) was anchored at  = -76\text{px}$ with height \text{px}$, placing its bottom edge at:
     -76 - 17 = -93\text{px}
  2. The blue stat panel (Stat_Panel) was anchored at  = -205\text{px}$ with height \text{px}$, placing its top edge at:
     -205 + 122.5 = -82.5\text{px}
  3. Because $-82.5\text{px}$ is higher than $-93\text{px}$, the stat panel's top border and dark background collided .5\text{px}$ directly into the title text!
- **Godot vs Unity Architectural Concept:**
  > In **Godot**, placing UI nodes inside a **VBoxContainer** or **MarginContainer** automatically calculates child bounding rectangles and enforces vertical separation margins without manual pixel math.
  > In **Unity**, when using raw **RectTransform** components (without a VerticalLayoutGroup), every element uses absolute local pixel offsets (nchoredPosition and sizeDelta). If the developer or setup tool specifies overlapping coordinate bounds, Unity renders them directly on top of each other.
- **Solution:**
  1. Recalculated the card's vertical rhythm inside [MainMenuSetupTool.cs](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/MainMenuSetupTool.cs):
     - Card Root: Expanded height to \text{px}$.
     - Epithet Label: Center  = -52\text{px}$, height \text{px}$, font size \text{pt}$ $\rightarrow$ bottom edge sits at $-68\text{px}$.
     - Stat Box: Center  = -195\text{px}$, height \text{px}$ $\rightarrow$ top edge sits at $-82.5\text{px}$.
     - **Result:** A clean **.5\text{px}$ void clearance gap**, completely eliminating the overlap.
  2. Applied franchise-specific title color signatures:
     - Goku: Super Saiyan Radiant Gold (#FFE620) with Ki Amber outline (#401E00).
     - Saitama: Serious Punch Crimson (#FF4D4D) with Dark Maroon outline (#360808).

---

### Postmortem: Dynamic Runtime Audio Binding & Fallback Architecture (EnsureClipsLoaded)
- **Incident:** When testing match transitions from Character Select into SampleScene, fighters dynamically spawned by [PrototypeBootstrap.cs](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs) executed skills and ultimates in complete silence.
- **Root Cause:** 
  1. SceneSetupTool.cs wires AudioClip references into the pre-baked scene fighters using AssetDatabase.LoadAssetAtPath().
  2. However, at runtime, PrototypeBootstrap.CreateFighter() instantiated components via gameObject.AddComponent<FighterAudio>().
  3. In Unity, AddComponent<T>() instantiates a blank component with all serialized [SerializeField] fields initialized to 
ull. Because AssetDatabase is stripped from runtime player builds, runtime scripts cannot use AssetDatabase.LoadAssetAtPath().
- **Resolution:**
  1. Created a dedicated runtime audio directory: Assets/Resources/Audio/Anime_Powers/, Swishes_Whiffs/, and Hits_Punches/.
  2. Implemented EnsureClipsLoaded() in [FighterAudio.cs](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs):
     `csharp
     if (isSaitama)
     {
         if (voiceSkillClip == null) voiceSkillClip = Resources.Load<AudioClip>("Audio/Anime_Powers/consecutive_normal_punches");
         if (voiceUltimateClip == null) voiceUltimateClip = Resources.Load<AudioClip>("Audio/Anime_Powers/serious_punch");
     }
     `
  3. In PrototypeBootstrap.CreateFighter(), explicitly called a.EnsureClipsLoaded() right after attaching FighterAudio.
  4. Now, regardless of whether fighters are baked in the scene, instantiated from prefabs, or created on the fly in mirror matches, all voices, whiffs, and hits are 100% guaranteed to play!

---

### Bug 14: Mirror Match Notice Coordinate Collision ($Y = -315\text{px}$)
- **Symptom:** In `MainMenuScene.unity`, when Player 1 and Player 2/CPU selected the same character (Mirror Match), the warning banner (`★ MIRROR MATCH: BOTH FIGHTERS ACTIVE ★`) appeared directly slicing through the bottom corners of both character cards.
- **Root Cause:** Character cards sit at $Y = +15\text{px}$ with a height of $660\text{px}$, placing their bottom edge exactly at:
  $$15 - 330 = -315\text{px}$$
  The mirror match banner was centered at $Y = -315\text{px}$ with a width of $900\text{px}$ ($X = -450$ to $+450$). Because the cards span from $-660$ to $-100$ on the left and $+100$ to $+660$ on the right, the banner's outer $350\text{px}$ segments crossed directly over both cards.
- **Solution:** Repositioned `Mirror_Match_Notice` to $Y = -345\text{px}$ in [`MainMenuSetupTool.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/MainMenuSetupTool.cs), placing it cleanly into the void clearance gap between the cards (bottom at $-315\text{px}$) and the instructions footer (top at $-367\text{px}$).

---

### Bug 15: Versus Screen P2 Badge Showing `[ PLAYER 2 ]` in CPU Mode
- **Symptom:** During the cinematic `VERSUS!` screen fly-in, Player 2's card displayed `[ PLAYER 2 ]` even when the player selected "1 PLAYER VS CPU" mode and was fighting a computer opponent.
- **Root Cause:** While the name text updated to `"CPU SAITAMA"` or `"CPU GOKU"`, the top badge `p2VsBadgeTmp` was never serialized or updated in `CharacterSelectController.cs`, leaving its static default string unaltered.
- **Solution:** Serialized `versusP2BadgeText` and `versusP2CardOutline` on [`CharacterSelectController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/CharacterSelectController.cs) and dynamically assigned:
  ```csharp
  versusP2BadgeText.text = (currentGameMode == GameManager.GameMode.VsCpu) ? "[ CPU ]" : "[ PLAYER 2 ]";
  versusP2BadgeText.color = (currentGameMode == GameManager.GameMode.VsCpu) ? cpuThemeColor : p2ThemeColor;
  ```

---

### Postmortem: The "Zombie" Ki Meditation Volume Trap (`kiAuraVolume`)
- **Incident:** Despite previous attempts to balance the meditation sound, the Ki aura hum was still overwhelmingly loud during gameplay, drowning out battle audio.
- **Root Cause Analysis (Why the sound didn't decrease previously):**
  1. **Field Serialization in Prefabs:** While documentation noted volume calibration, the serialized YAML in `Assets/Prefabs/Fighters/Goku.prefab` and `Saitama.prefab` remained at `kiAuraVolume: 0.45`! Unity prefabs serialize their own values and override script field default values unless explicitly re-baked.
  2. **Raw Master Audio Loudness:** The audio asset `dbz_super_saiyan_aura.mp3` is mastered at $0\text{ dBFS}$ peak (full normalized scale) with heavy low-frequency sub-bass. In game audio, looping sounds with continuous $0\text{ dB}$ RMS perceive as substantially louder than transient punch hits. Even at $45\%$ ($0.45$), it overwhelmed the soundscape.
  3. **Runtime Spawner Override:** In `PrototypeBootstrap.cs`, dynamically added `FighterAudio` components inherited the old $0.45$ default from `FighterAudio.cs`.
- **Resolution (4-Tier Enforcement):**
  1. Updated `FighterAudio.cs` C# default: `kiAuraVolume = 0.12f;` (soft 12% ambient hum).
  2. Updated `SceneSetupTool.cs` baking logic to enforce `0.12f`.
  3. Updated YAML directly in `Goku.prefab` and `Saitama.prefab` to `kiAuraVolume: 0.12`.
  4. Enforced in `PrototypeBootstrap.CreateFighter()`: `fa.KiAuraVolume = 0.12f;`.

---

### Postmortem: Audio PlayOneShot Volume Ceiling & Vocal Amplification
- **Incident:** Special move vocal shouts (Saitama's "Consecutive Normal Punches" and "Serious Punch") sounded too quiet and lacked punchiness.
- **Root Cause:**
  1. `FighterAudio.PlaySound()` previously executed:
     `sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));`
     `Mathf.Clamp01` forcibly clipped the volume multiplier to a maximum of $1.0$, preventing any amplification boost above unity gain.
  2. All audio (whiffs, hits, jumps, voice) shared a single `sfxSource` `AudioSource`. During multi-hit flurries, punch impacts competed for amplitude on the same channel.
- **Resolution:**
  1. Added a dedicated child `AudioSource` (`Voice_AudioSource`) specifically for high-priority vocal cries.
  2. Replaced `Mathf.Clamp01(volume)` with `Mathf.Clamp(volume, 0f, 2.5f)`, allowing volume scale multipliers above $1.0$.
  3. Implemented move-specific volume amplification:
     - Skill Attack (`consecutive_normal_punches.mp3`): **$1.45\times$ volume multiplier**.
     - Ultimate Finisher (`serious_punch.mp3`): **$1.70\times$ volume multiplier**.

---

### Bug 16: Saitama Consecutive Normal Punches Luminance Blending & Perimeter Outline Vanishing

- **Symptom:**
  During Saitama's Special Skill ("Consecutive Normal Punches", activated via `[U]`), the rapid flurry of punches sometimes blended into the background tiles or enemy fighters, causing the punch outlines to disappear and rendering the attack as a muddy, indistinct red blob.
- **Forensic Pixel Diagnosis:**
  1. **Border Color Collision:** The outer boundary pixels across all 12 animation frames (`skill_0.png` through `skill_11.png`) were colored `(20, 5, 5, 255)` (`#140505`), which has a relative luminance of just **$9.5$** out of $255$.
  2. **Context-Dependent Contrast Failure:**
     - In the center of the arena where the stage spotlight shines (`#FBA782`, luminance $\sim 190$), the dark `#140505` outline had sufficient contrast against the floor.
     - However, when Saitama moved towards the left or right arena flanks or punched against Goku's sprite, the backdrop tiles are dark slate/navy (`#1A1F25` and `#141937`, luminance **$26\text{–}30$**) and Goku's hair/sash are black (`#000000`, luminance $0.0$).
     - Against these dark surfaces, the `#140505` boundary had a contrast ratio of **$< 1.3:1$**, causing human visual perception to merge the outline with the background.
  3. **Unshielded Perimeter Gaps (171 Pixels):** Frames 0, 2, 3, 4, 6, and 7 contained 171 perimeter glove pixels where raw red (`#F40000` / `#B70000`) touched transparent background alpha directly with zero outline.
  4. **Internal Fist Merging:** Overlapping punches lacked leading kinetic shockwave highlights or internal separators, causing the rapid flurry to lose 3D form.
- **Resolution:**
  1. **Zero-Touch Backup:** Created bit-for-bit pristine backups of original frames in `Assets/Sprites/Fighters/Saitama/Original_Skill_Backup/` and scratch (`revert_skill_frames.py` available for instant 1-second reversion if needed).
  2. **Perimeter Sealing & Deep Inking:** Sealed all 171 unshielded gaps with a solid 1px dark perimeter contour (`#080404` / `#000000`), darkening outer boundaries to eliminate transparency bleeding while preserving 100% of Saitama's body, cape, and suit pixels untouched.
  3. **Anime Kinetic Shockwave Rim Highlight:** Added high-contrast anime impact highlights on all leading knuckle strike faces:
     - **Strike Face (Front):** Blazing impact white (`#FFFFFF`, luminance **$255.0$**).
     - **Kinetic Transition (1px behind):** Solar anime gold highlight (`#FFD76E`, luminance **$215.0$**).
     - **Warm Body Blend (2px behind):** Energetic red-orange (`#FF5A32`, luminance **$134.8$**).
  4. **Contrast Verification:** The knuckle front now boasts an **$>8:1$ contrast ratio** against dark arena tiles and **$>20:1$** against black sprites, ensuring razor-sharp punch readability at 60 FPS everywhere in the arena.

---

### Bug 17: Input Buffering & Audio Bleed during Pause (The Time.timeScale Trap)

- **Symptom:**
  In many fighting games, setting `Time.timeScale = 0f` to pause the game causes two classic bugs:
  1. **Input Buffering on Unpause:** While navigating the pause menu with `W/S` or `Space`, the player's fighter buffers the keystrokes and immediately executes a jump or light attack upon resuming the match.
  2. **Looping Audio Bleed & Silent Menus:** Either looping combat SFX (like Ki charging hums) keep droning on while paused, OR if `AudioListener.pause = true` is used, the pause menu's own button clicks and navigation blips become completely muted.
- **Root Cause:**
  1. `PlayerInputHandler` evaluates hardware keys (`Keyboard.current`) inside `Update()`. In Unity, `Update()` still executes every frame even when `Time.timeScale = 0f`! If `Time.timeScale` is not checked, input properties remain populated.
  2. `AudioListener.pause = true` halts all active sound channels globally unless an `AudioSource` explicitly opts out via `ignoreListenerPause`.
- **Resolution:**
  1. **Zero Input Buffering:** In [`PlayerInputHandler.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Input/PlayerInputHandler.cs), added an early exit guard:
     ```csharp
     if (Time.timeScale == 0f)
     {
         MoveInput = Vector2.zero;
         JumpPressed = false;
         LightAttackPressed = false;
         // ... zeroes all action flags ...
         return;
     }
     ```
     This guarantees that physics velocities, animation frames, and positions are frozen and completely preserved across pause/resume without accidental action execution.
  2. **Selective UI Audio Execution:** In [`CombatMenuController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/CombatMenuController.cs), configured `menuAudioSource.ignoreListenerPause = true;`. When the game is paused, `AudioListener.pause = true;` halts combat auras and hits, while the menu's retro navigation bleeps and confirmation chimes play with zero disruption.

---

### Bug 18: Post-K.O. Premature Winner UI & Stray Input Bleed (Pacing & Lifecycle Fix)

- **Symptom:**
  1. Immediately upon landing the final K.O. strike, the winning player could still press attack keys, causing their character to punch and jump into empty air while the opponent lay defeated.
  2. The victory screen popped up too quickly ($1.25\text{s}$), interrupting the impact of the final strike and obscuring the fighters before players could absorb the end of the battle.
  3. The victory screen's dark background scrim dimmed the entire arena, hiding the arena backdrop and characters.
- **Root Cause:**
  1. `PlayerInputHandler` only checked `Time.timeScale == 0f`, but did not evaluate `MatchManager.CurrentMatchState`. As a result, during `RoundEnd`, `MatchOver`, and before the opening round bell (`WaitingToStart`), input hardware events were still passed to `FighterController`.
  2. The results modal relied on a static `SetActive(true)` with an opaque dark scrim rather than a dynamic presentation respecting the game world.
- **Resolution:**
  1. **Lifecycle Input Gating:** In [`PlayerInputHandler.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Input/PlayerInputHandler.cs), added a `CanFight()` gate checking `matchManager.CurrentMatchState == MatchState.RoundInProgress`. All movement and attack properties are zeroed out before the match begins, during round-end pauses, and after match over.
  2. **3.0s Cinematic Pacing:** Increased the post-match delay to $3.0\text{s}$ in `CombatMenuController.cs`, providing ample time for K.O. reverberation, defeat reactions, and upcoming victory animations.
  3. **Sliding Entrance Animation:** Animated the modal box using a cubic ease-out coroutine in unscaled time ($Y = +950\text{px} \to 0\text{px}$ over $0.65\text{s}$).
  4. **Transparent Scrim & Prestigious Card Styling:** Cleared the dark scrim to `Color.clear` (keeping arena and characters 100% bright) and styled the modal as an ornate Animeverse Championship plaque with gold borders, cyan trim, and ruby studs.

---

### Bug 19: Tight Coupling of Fighter Physics to Hardware Keyboards & AI Input Cheating

- **Symptom:**
  1. [`FighterController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs) held a hard direct reference to `PlayerInputHandler` (`[SerializeField] private PlayerInputHandler inputHandler`), which read physical keyboard strokes (`Keyboard.current`).
  2. Spawning a CPU bot required either giving it a fake keyboard device or bypassing `FighterController` by directly mutating physics and animations, which breaks frame data, Ki spending rules, and recovery windows.
  3. Continuous AI inputs (like holding Meditate) and discrete single-frame triggers (like Light Attack press) required distinct lifecycle handling to avoid spamming an attack every single frame.
- **Root Cause:**
  - Violation of the **Dependency Inversion Principle (DIP)**: `FighterController` (a high-level combat execution module) depended on a low-level concrete hardware input reader (`PlayerInputHandler`) instead of an abstraction.
- **Resolution:**
  1. **Input Interface Extraction:** Created [`IFighterInput.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Input/IFighterInput.cs), defining properties for `MoveInput`, `JumpPressed`, `LightAttackPressed`, `HeavyAttackPressed`, `SkillPressed`, `UltimatePressed`, `MeditateHeld`, and `CrouchHeld`.
  2. **Decoupled Injection in FighterController:** Modified `FighterController` to reference `IFighterInput inputHandler`, with backward-compatible fallback for pre-configured serialized inspector components.
  3. **Virtual Action Pulse Clearing:** In [`CpuFighterBrain.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/AI/CpuFighterBrain.cs), single-frame button pulses (`JumpPressed`, `LightAttackPressed`, etc.) are reset to `false` in `LateUpdate()`, guaranteeing they act as true one-frame events (`wasPressedThisFrame`) without duplicate hits.
  4. **Character-Specific Brains & Difficulty Knobs:** Created [`GokuBrain.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/AI/GokuBrain.cs) (mid-range Kamehameha zoner), [`SaitamaBrain.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/AI/SaitamaBrain.cs) (relentless rushdown brawler), and [`CpuBrainProfile.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/AI/CpuBrainProfile.cs) with Easy, Hard, and Extreme difficulty tiers.
  5. **Dynamic Character Select Switching:** Added `[TAB]` keyboard cycling in [`CharacterSelectController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/CharacterSelectController.cs) to switch CPU difficulty on the fly.

---

### Bug 20: Character Select Lock-In Disconnect: Hardcoded Scene GameObjects & Deferred `Destroy()` Race Condition

- **Symptom:**
  1. In the Character Selection screen, when Player 1 locked in Saitama and CPU locked in Goku, the HUD updated names and portraits correctly, but the physical combatants on the stage were always locked to Goku (P1) vs. Saitama (P2).
  2. If Saitama was chosen for both players (Mirror Match), the match failed to load Player 2 or inverted health and Ki bar event listeners.
- **Root Cause:**
  1. **Static Scene Prefab Instances:** [`SampleScene.unity`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scenes/SampleScene.unity) had static baked GameObject instances (`Fighter_P1_Goku` and `Fighter_P2_Saitama`) serialized directly into the scene hierarchy.
  2. **Deferred `Destroy()` Execution:** When `PrototypeBootstrap` called `Destroy(existingFighter)`, Unity deferred object removal until the end of the frame. In `Awake()`, `MatchManager` and `HUDController` executed `GameObject.Find("Fighter_P1_Goku")` and bound to the obsolete fighters before they were removed.
  3. **Brittle String Queries:** Both `MatchManager.cs` and `HUDController.cs` explicitly searched for `"Fighter_P1_Goku"` and `"Fighter_P2_Saitama"`. Any other character combination failed to bind.
- **Resolution:**
  1. **Clean Arena Architecture:** Stripped static fighter instances from `SampleScene.unity` and updated [`SceneSetupTool.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/SceneSetupTool.cs) to treat the arena as an empty stage.
  2. **Resources Prefab Encapsulation:** Saved canonical fighter prefabs into `Assets/Resources/Prefabs/Fighters/` (`Goku.prefab`, `Saitama.prefab`).
  3. **Dynamic Runtime Deployer:** In [`PrototypeBootstrap.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs), `DeployFighter()` dynamically instantiates the chosen character prefab via `Resources.Load<GameObject>()` based on `GameManager.Instance.P1SelectedCharacterId` and `P2SelectedCharacterId`.
  4. **Direct Reference Injection:** Implemented `SetFighterReferences(p1Hp, p2Hp, p1Ki, p2Ki)` on both `MatchManager` and `HUDController`, allowing the deployer to inject the newly spawned fighter references directly, eliminating all brittle scene search queries.
  5. **Immediate Deactivation Sweep:** Any residual scene fighters are deactivated with `go.SetActive(false)` immediately before calling `Destroy(go)`, preventing lifecycle race conditions during frame 0.

---

### Bug 21: Non-Persistent Editor Lambda Listeners & Static Controls Modal

- **Symptom:**
  1. The Main Menu's "Options & Moveset" button opened a static, read-only placeholder box with text stating `[Custom player-preferred movesets and audio mixer will be configured here.]`, providing no actual volume sliders or key customization.
  2. When procedurally generating UI in Unity Editor baking scripts (`MainMenuSetupTool.cs`), attaching C# lambda listeners (e.g. `btn.onClick.AddListener(() => OnClick())`) failed to persist after saving the scene file (`EditorSceneManager.SaveScene`).
- **Root Cause:**
  1. **UnityEvent Serialization Mechanics:** Standard `AddListener` calls register transient, non-serialized C# delegates in memory. When Unity saves a scene, only persistent listeners serialized in YAML are retained.
  2. **Single Global Keymap Constraint:** If a single global keymap was used, remapping Player 1's keys for solo play against the CPU would inadvertently scramble the shared keyboard layout required for 2-player 1v1 local duels.
- **Resolution:**
  1. **Persistent Manager (`InputSettingsManager.cs`):** Manages `MusicVolume`, `SFXVolume`, `SinglePlayerKeys` (9 actions for Vs CPU / LAN), and `LocalDuelP1/P2Keys` (18 actions for shared keyboard), persisted via `PlayerPrefs`.
  2. **Runtime Lifecycle Event Hooking (`OptionsMenuController.cs`):**
     - Instead of relying on fragile Editor-time lambda serialization, `OptionsMenuController` stores serialized lists of rows (`singlePlayerRows` and `localDuelRows`).
     - In `Awake()`, `HookAllListeners()` binds runtime `onClick` and `onValueChanged` delegates to all sliders, tab switches, and key buttons.
  3. **3-Column Combined Table Layout:**
     - The 1v1 Local Duel panel presents all 9 actions in a unified 3-column table:
       `[ ACTION NAME ] | [ PLAYER 1 (LEFT SIDE) ] | [ PLAYER 2 (RIGHT SIDE) ]`
     - This allows both players to audit their keys side-by-side and avoid mapping overlaps before combat starts.
  4. **Interactive Key Rebinding Loop:**
     - Clicking any key button transitions to `[ PRESS KEY ]` with gold pulse animation.
     - `Update()` listens to `Keyboard.current.allKeys`, intercepts the pressed key, updates the button label, and writes to `PlayerPrefs` immediately. ESC cleanly cancels without modifying the key.
  5. **Compiler Verification & CS0103 Fix:**
     - Executed a full project compilation check across both assemblies (`Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`) using Unity's official Roslyn compiler (`csc.dll` via .NET 8 SDK).
     - Fixed a refactoring typo in `OptionsMenuController.InitializeReferences()` where a legacy call to `HookButtonEvents()` was resolved to `HookAllListeners()`.
     - Verified clean compilation with **0 errors** across all 34 C# source files and 393 reference assemblies.

---

### Bug 22: High Information Density & Illegible Font Scaling in Unified Options Screen

- **Symptom:**
  1. Font sizes in the initial Controls & Options screen were cramped down to $12\text{pt}\text{–}14\text{pt}$, making key labels and descriptions hard to read on 1080p and higher displays.
  2. Displaying all 18 key rows simultaneously below the audio sliders caused visual clutter and sensory overload.
  3. The menu lacked an obvious arcade-style navigation focus indicator, making keyboard/controller navigation ambiguous.
- **Root Cause:** Squeezing audio controls, mode descriptors, and 18 interactive key rows onto a single $1060 \times 720\text{px}$ canvas left insufficient vertical clearance ($< 32\text{px}$ per row), artificially restricting text scale.
- **Resolution:**
  1. **$80\%$ Typography Scale-Up:** Scaled fonts across the board: Titles to $36\text{pt}$, Subtitles to $20\text{pt}$, Action Labels & Buttons to $23\text{pt}\text{–}25\text{pt}$, and Percentage Indicators to $26\text{pt}$.
  2. **Hierarchical 3-Screen Architecture:**
     - **Main Hub:** Audio sliders, launcher cards (`[ 👤 CONFIGURE SINGLE PLAYER KEYS » ]` and `[ ⚔️ CONFIGURE 1v1 LOCAL DUEL KEYS » ]`), and side-by-side lower buttons (`Reset Defaults` and `Save & Back`).
     - **Single Player Sub-Screen:** Full window dedicated to 9 spacious action rows ($54\text{px}$ row height).
     - **1v1 Local Duel Sub-Screen:** Full $1440\text{px}$ width dedicated to the 3-column combined table (`ACTION` | `PLAYER 1` | `PLAYER 2`).
  3. **Obvious Visual Navigation System:**
     - Implemented dynamic `►` cursor assignment and real-time pulsing Solar Gold (`#FFD54F`) focus frame.
     - Dual-axis input handling: Vertical ($W/S$, Up/Down) shifts active rows; horizontal ($A/D$, Left/Right) nudges volume sliders $\pm 5\%$ or toggles between P1 and P2 columns.
     - Full synchronization between keyboard, D-pad, and mouse hover/click interactions.








---

### Bug 23: Empty Arena & Null Fighter Deployment via `RuntimeInitializeOnLoadMethod` Single-Execution & Scene Transition Trap

- **Symptom:**
  1. After cleaning up static pre-baked fighters from `SampleScene.unity` to allow dynamic Character Selection, playing the game from `MainMenuScene` loaded an empty combat arena with zero fighters on stage.
  2. The round timer did not start, HUD portraits showed null bars, and keyboard inputs had no characters to control.
- **Root Cause:**
  1. **`RuntimeInitializeOnLoadMethod` Lifecycle Trap:** Attributes with `RuntimeInitializeLoadType.AfterSceneLoad` fire **only once** upon initial application launch. When starting the game from `MainMenuScene`, the method ran inside `MainMenuScene` and aborted because fighters should not spawn in the menu. When the player transitioned into `SampleScene`, that method was never invoked again!
  2. **The `SceneManager.sceneLoaded` Active Scene Gotcha:** In Unity, when `SceneManager.sceneLoaded` fires, `SceneManager.GetActiveScene()` is **not yet switched** to the newly loaded scene. When `PrototypeBootstrap.DeployMatchFighters()` ran:
     ```csharp
     if (SceneManager.GetActiveScene().name == "MainMenuScene") return;
     ```
     `SceneManager.GetActiveScene()` was still `"MainMenuScene"`, so the deployment aborted immediately!
  3. **Premature `MatchManager.Start()`:** `MatchManager.Start()` immediately called `StartMatch()` regardless of whether fighters were present, expecting static scene fighters.
- **Resolution:**
  1. **Target Scene Context Guard:** In [`PrototypeBootstrap.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs), replaced `SceneManager.GetActiveScene().name` checks with `gameObject.scene.name` and the loaded `scene.name`.
  2. **Explicit Scene Migration (`SceneManager.MoveGameObjectToScene`):** Called `SceneManager.SetActiveScene(scene)` and moved instantiated fighters explicitly into `scene` to prevent them from belonging to the unloading scene.
  3. **Dual Execution Hooks:** Wired both `SceneManager.sceneLoaded` (for scene changes from Main Menu) and `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` (for direct Play Mode debugging inside `SampleScene`).
  4. **Stateful Readiness Handshake (`MatchState.WaitingToStart`):**
     - In [`MatchManager.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchManager.cs), default state is `MatchState.WaitingToStart`.
     - In `Start()`: If fighters are missing, logs a waiting notice and does not start the round timer.
     - In `SetFighterReferences()` and `Update()`: When valid fighters are supplied/discovered, transitions `WaitingToStart -> RoundInProgress` and calls `StartMatch()`.
  5. **Auto-Bake Integration:** In [`SceneSetupTool.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/SceneSetupTool.cs), added an explicit `[PrototypeBootstrap]` GameObject to `SampleScene.unity` and bumped `BAKE_VERSION_KEY` to `v27`.

---

### Bug 24: Overly Loud Menu Navigation SFX, Move-Slot Semantic Disconnect & Vocal Gain Calibration

- **Symptom:**
  1. Navigating choices inside the in-match Pause Menu and Victory Menu triggered an abrasive, loud sound effect that drowned out background music.
  2. Goku's Kamehameha shout was extremely loud and did not drop in volume when pressing Skill (`E`), while Saitama's vocal callouts ("Consecutive Normal Punches" and "Serious Punch") were drowned out by punch impact sounds.
  3. Default background music volume was set to `0.70f` ($70\%$), preventing new players from experiencing full dynamic range unless manually bumped up in Options.
- **Root Cause:**
  1. In `CombatMenuController.cs`, `navigateSfx` was triggered with an unscaled volume of `0.45f` and a raw square-wave without an amplitude decay envelope, producing high-frequency transient clicks.
  2. **Move-Slot Disconnect:** In `FighterAudio.cs` and `SceneSetupTool.cs`, `goku_voice_kamehameha` was attached to `voiceUltimateClip` (Ultimate `Q`), while `voiceSkillClip` (Skill `E`, where Kamehameha actually lives in `goku.json`) was left as `null`. When players pressed `E` to fire Kamehameha, no voice played; when pressing `Q`, the voice played at uncalibrated levels.
  3. **Acoustic Mastering Disparity:** `goku_voice_kamehameha.wav` was mastered at near-peak ceiling (Peak `0.906`, RMS `0.262`). Even with a $30\%$ drop ($0.84\times$), it remained deafening. In contrast, Saitama's MP3 stems had softer recording gain and were eclipsed by physical hit SFX.
  4. In `InputSettingsManager.cs`, `DEFAULT_MUSIC_VOL` was hardcoded to `0.70f`, and returning users had `0.70f` cached in `PlayerPrefs`.
- **Resolution:**
  1. **Menu Navigation SFX Volume Halved:** Reduced `navigateSfx` playback volume from `0.45f` down to `0.22f` ($51\%$ reduction) across keyboard and mouse triggers in `CombatMenuController.cs`. Added linear decay envelope `(1f - t/duration)` to procedural audio to remove clicks.
  2. **Goku Kamehameha Wired as Ultimate (`Q`) & Gain-Calibrated:**
     - In `FighterAudio.cs`, mapped `goku_voice_kamehameha` to `voiceUltimateClip` and set `voiceSkillClip = null` (Goku has no skill voice; his animation and ultimate finisher is Kamehameha).
     - Calibrated Goku's `voiceUltimateMultiplier` down to `0.50f`, and added an explicit clamp in `PlayVoiceSound()`: `tunedVolume = Mathf.Min(tunedVolume, 0.50f);` for Kamehameha.
  3. **Saitama Vocal Shouts Amplified:**
     - Boosted Saitama's `voiceSkillMultiplier` to **`2.20f`** (Consecutive Normal Punches).
     - Boosted Saitama's `voiceUltimateMultiplier` to **`2.50f`** (Serious Punch).
  4. **Master BGM Default to 100% with Migration:**
     - Updated `DEFAULT_MUSIC_VOL = 1.00f;` in `InputSettingsManager.cs`.
     - In `LoadAllSettings()`, added an auto-upgrade branch: if stored `PlayerPrefs` was the legacy default (`0.70f`) or unset, it auto-migrates to `1.00f`.
     - Updated serialized defaults in `MainMenuController.cs` and bumped `MAIN_MENU_BAKE_KEY` to `v16` in `MainMenuSetupTool.cs`.
  5. **Master Audio Bus Synchronization (Single-Pass Scaling):** Linked `MatchAudioController.cs` and `FighterAudio.cs` directly to `InputSettingsManager.MusicVolume` and `SFXVolume` using single-pass multiplication in `PlayOneShot()`.
  6. **Clean Compilation:** Re-verified with official Roslyn compiler (`csc.dll` via .NET 8 SDK): **0 errors** across both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.

---

### Bug 25: Animation Bouncing, Static Collider Disconnect & 60-FPS AI Pogo-Sticking

- **Symptom:**
  1. Goku bounced $20\text{px}$ into the air whenever he initiated his Kamehameha skill animation, popping up and snapping back down.
  2. Crouching changed the character sprite to ducking, but opponents' high melee strikes still connected because the hurtbox remained at standing height.
  3. Goku's Kamehameha beam whiffed into thin air when standing at his preferred zoning distance ($2.1\text{m}$).
  4. CPU bots felt erratic and "super dumb": pogo-stick jumping 9 times every second, pacing back and forth blindly on an arbitrary world timer, and getting stuck running against the arena boundary wall while trying to retreat and meditate.
- **Root Cause:**
  1. **Canvas Size & Baseline Disparity:** Standard Goku frames were $128 \times 128\text{px}$ with a $23\text{px}$ bottom padding (`y = 104`). In contrast, `skill_0..3.png` were sliced at $90 \times 79\text{px}$ with variable paddings ($14\text{–}20\text{px}$), causing `FighterAnimator.cs` to skip its standard pivot formula and lift Goku's feet $20\text{px}$ into the air.
  2. **Static Hurtbox Collider:** `Hurtbox` was a static child `BoxCollider2D` ($1.8\text{m}$ tall) that never resized during `FighterState.Crouching`.
  3. **Hitbox Reach Disconnect:** In `FighterController.cs`, the skill attack hitbox was configured with a width of only $1.3\text{m}$, far shorter than the visual beam and Goku's $2.1\text{m}$ preferred distance. Additionally, `HitboxController.ConfigureAttack()` updated internal fields without synchronizing `hitboxCollider.offset` and `size` on the physical component.
  4. **The 60 FPS Probability Trap & World-Space Oscillation:**
     - Evaluating `Random.value < 0.15f` every frame in `Update()` caused bots to jump 9 times per second.
     - Neutral footsies oscillation used world space (`spacingOscillationDir * 0.4f`) rather than opponent-relative vectors.
     - `ExecuteKiHoarding` lacked stage boundary checks; when backed against the wall ($|x| > 7.2\text{m}$), bots kept trying to walk backward into the wall.
- **Resolution:**
  1. **$128 \times 128$ Canvas Standardization:** Re-canvased all 44 Goku frames onto uniform $128 \times 128\text{px}$ PNGs with feet locked at `y = 104` and stance center at $x = 64$.
  2. **Dynamic Hurtbox Resizing:** In `FighterController.TransitionTo()`, scaled `hurtboxCollider.size` to $0.95\text{m}$ ($y = 0.48\text{m}$) on entering `Crouching` and restored to $1.8\text{m}$ on exit.
  3. **Kamehameha Reach & Component Sync:** Expanded beam reach to $3.6\text{m}$ (`offset = (2.1f, 0.9f)`, `size = (3.6f, 0.85f)`) and synchronized `hitboxCollider.offset` and `size` in `ConfigureAttack()`.
  4. **Tactical HFSM Overhaul:**
     - Added `jumpCooldownTimer` ($2.0\text{s}\text{–}3.2\text{s}$) to prevent pogo-sticking.
     - Added `attackCooldownTimer` ($0.25\text{s}\text{–}0.50\text{s}$) for human-like attack rhythm.
     - Converted neutral footsies to opponent-relative stepping (`dirToOpponent * 0.45f` in, `-dirToOpponent * 0.35f` out).
     - Added corner awareness near walls ($|x| > 7.2\text{m}$): bots now jump over or fight their way out instead of running into the wall.
     - Added defensive crouch ducking when opponents attack in close range.
  5. **Compiler Verification:** Clean compilation with **0 errors** across both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.

---

### Bug 26: Silent Round 1 Combat BGM via Scene Load Event Race & Stale MainMenu Guard

- **Symptom:**
  1. When starting combat from Character Select or launching directly into `SampleScene`, Round 1 played in complete silence without background music.
  2. Upon reaching Round 2, the combat BGM (*Double Dragon 2 - Final Battle 2*) suddenly kicked in and played normally.
- **Root Cause:**
  1. **Stale `GameState.MainMenu` Guard:** In `MatchAudioController.cs`, `HandleRoundStart()` and `PlayBattleMusic()` checked `if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.MainMenu) return;`. In `GameManager.cs`, `currentState` defaulted to `GameState.MainMenu` and `MatchManager` never explicitly set it to `GameState.InMatch` on round start.
  2. **Scene Load Event Race Condition:** `PrototypeBootstrap` deployed fighters dynamically during scene loading, triggering `MatchManager.StartRound(1)` and firing `OnRoundStart?.Invoke(1)` before `MatchAudioController` had completed its event subscription.
  3. **Empty `Start()` Recovery:** `MatchAudioController.Start()` was completely empty, assuming future events would fire. Because Round 1's event was already spent, the controller stayed idle until Round 2 fired a new event.
- **Resolution:**
  1. **Scene-Context Filtering:** In [`MatchAudioController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchAudioController.cs), replaced the brittle `CurrentState == GameState.MainMenu` check with `if (gameObject.scene.name == "MainMenuScene") return;`.
  2. **Stateful Self-Healing in `Start()`:** In `Start()`, inspected whether `matchManager.CurrentMatchState == MatchState.RoundInProgress`. If Round 1 has already begun and `!musicSource.isPlaying`, calls `PlayBattleMusic(resetToStart: true)` immediately.
  3. **Continuous Failsafe in `Update()`:** Added an automatic playback recovery check in `Update()` if a round is in progress and the music source is silent.
  4. **State Synchronization in `MatchManager`:** In `MatchManager.StartMatch()`, explicitly calls `GameManager.Instance.SetState(GameState.InMatch)`.
  5. **Compiler Verification:** Clean compilation with **0 errors** across both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.

---

### Bug 27: Kamehameha 16px Stray Rip Artifact, Skated Footing & Instant Round-End Visual Cut

- **Symptom:**
  1. Goku's Kamehameha charge (`ultimate_1..3`) hopped 16 pixels straight up into the air, hung briefly, and then slammed back down on frame 4. Furthermore, his stance skated horizontally while lunging forward.
  2. When a fighter's HP reached 0, the round ended abruptly with no dramatic weight; the loser's 11-frame collapse animation was cut short, the winner had no time to perform their victory pose, and the round restarted almost instantly.
  3. Aura visuals felt like soft generic smoke/dust clouds rather than authentic Dragon Ball Z jagged spiky flame energy.
  4. Goku was able to charge Ki via meditation while in Super Saiyan mode, which contradicted authentic anime mechanics.
  5. The default round timer of 99s caused matches to feel unnaturally rushed for a deliberate tactical fighting game.
- **Root Cause:**
  1. **Stray Sprite Rip Artifact at $Y = 73..74$:** In `Frames_Raw/ultimate_1..3.png`, 6 stray pixels from the adjacent sprite beneath it on the ripped sheet existed at $Y = 73..74$. Because the standard canvas script aligned bounding-box bottoms to baseline $Y = 105$, those 6 stray pixels were anchored to $Y = 105$, which lifted Goku's actual feet (at $Y = 58$) up to $Y = 89$ (16 pixels into the air). In frames 0 and 4..9, there was no bottom artifact ($Y \le 58$), so Goku's feet snapped back to the floor. Additionally, the ripped frames drifted horizontally by 1px per frame ($X = 13 	o 4$), causing foot skating when centered blindly.
  2. **Coupled TimeScale & Premature Round Restart:** `Time.deltaTime` scaled animation updates directly with physics timescale. When slow-motion was triggered, animations crawled or skipped frames, and `RoundEndSequence` waited only 1.8 seconds in game time without letting the loser hit the floor before the winner started celebrating.
  3. **Particle-Only Aura:** Soft radial particles cannot reproduce the iconic spiky golden flame cocoon of Akira Toriyama's Super Saiyan aesthetic.
- **Resolution:**
  1. **$Y < 60$ Crop & Fixed Foot Anchoring:**
     - In `scratch/realign_kamehameha.py`, cropped all raw frames strictly at $Y < 60$, completely eliminating the 16px bottom stray artifact.
     - Anchored Goku's planted back foot to a fixed canvas position ($X = 41$, $Y = 105$). Zero vertical hopping, zero horizontal foot skating.
  2. **4-Phase Dramatic Slow-Mo K.O. State Machine:**
     - In [`MatchManager.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchManager.cs):
       - **Phase 1 (Slow-Mo Collapse):** Loser enters `FighterState.Knockout`, `Time.timeScale = 0.28f`, camera shake ($0.40$), impact flash ($0.12\text{s}$). Waits $1.8\text{s}$ in real time (`WaitForSecondsRealtime(1.8f)`).
       - **Phase 2 (Floor Impact & Normalization):** Loser hits the mat, ground dust puff triggers, and `Time.timeScale` snaps back to $1.0\text{f}$.
       - **Phase 3 (Staggered Victory Celebration):** Winner transitions to `FighterState.Victory` to perform their full victory animation loop.
       - **Phase 4 (Dramatic Hold):** Holds for $2.8\text{s}$ in real time before calling `CheckWinCondition()`.
     - In [`FighterAnimator.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterAnimator.cs): Added `unscaled: true` to `PlayOnce` and `PlayLoop` for `Knockout` and `Victory` states, ensuring all 11 knockout frames and 9 victory frames play smoothly at full framerate regardless of timescale.
  3. **Authentic DBZ Dual-Layer Spiky Flame Aura:**
     - Created `Assets/Sprites/Effects/dbz_spiky_aura.png` with 26 jagged flame spires, elongated upward flare, and bright inner core.
     - In [`FighterKiAura.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterKiAura.cs), built a dual-layer system:
       - Oscillating spiky shell ($24\text{–}28\text{ Hz}$ flame vibration, alpha pulse).
       - Sub-emitter vertical needle speed streaks shooting upward from feet.
       - Dynamic color tinting: Radiant Gold for SSJ Goku, Electric Cyan for Base Goku meditation, Blazing Crimson for Saitama.
  4. **Super Saiyan Meditation Guard:** In [`FighterController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs), blocked meditation while `isSuperSaiyan` is active, and automatically reverted to Idle if meditation was active during transformation.
  5. **Round Timer Set to 199s:** Set `roundDuration = 199f` in `MatchManager.cs` for deliberate, authentic arcade fighting pacing.
  6. **Sprite Slicer Default to SSJ:** Updated `Tools/SpriteSlicer/index.html` to default to `goku_ssj` and cleared `Assets/Sprites/Fighters/Goku/Frames_SSJ/` so the user has a clean slate for manual slicing.
  7. **Compiler Verification:** Clean compilation with **0 errors** across both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.

---

### Bug 28: Voice-Line Animation Truncation, Victory Pogo-Sticking & Dedicated Slicer Architecture

- **Symptom:**
  1. Goku returned to his idle breathing stance only halfway through shouting his Kamehameha chant, walking around while the audio still blasted "ME... HA!".
  2. Saitama's victory animation looked like erratic jumping/pogo-sticking rather than a stable, proud victory celebration.
  3. The round-end K.O. sequence felt rushed, cutting to victory before the defeat settled in.
  4. The aura had too many rigid spikes and extended below the fighter's feet through the floor.
  5. Fighters occupied ~2.0 world units, making the stage feel cramped with little room for spacing.
  6. The sprite slicer tool suffered from browser caching when attempting to toggle between Base and Super Saiyan modes.
- **Root Cause:**
  1. **Audio/State Duration Disconnect:** `goku_voice_kamehameha.wav` is $3.36\text{s}$ long, while `FighterController.attackTimer` was hardcoded to $1.45\text{s}$. The state machine exited the `Ultimate` state and resumed `Idle` before the audio finished.
  2. **Looping Incompatible Poses:** The victory animation frames contained crouch frames ($34\text{px}$), mid-air leaps ($60\text{px}$), and standing poses ($49\text{px}$). Looping these in a cycle made Saitama jump up and down by $30\text{px}$ continuously.
  3. **Premature TimeScale Restoration:** The knockout slow-motion sequence lasted only $1.8\text{s}$ in real time, rushing the physics and collapse.
  4. **Rigid Spike Geometry:** Generating 26 mathematical polygon spikes produced a rigid cardboard shell rather than translucent, licking anime fire.
  5. **Shared Single-Page State Mismatch:** Trying to swap sheets and characters inside a single HTML page caused browser caching collisions.
- **Resolution:**
  1. **Audio-Synchronized Attack Timings:**
     - Goku Kamehameha: $3.36\text{s}$ total ($2.15\text{s}$ charging chant + $1.21\text{s}$ active blast).
     - Saitama Consecutive Punches: $2.70\text{s}$ continuous looping flurry.
     - Saitama Serious Punch: $3.80\text{s}$ ($2.40\text{s}$ chilling windup + $1.40\text{s}$ devastating punch).
  2. **Zero-Jumping Victory Normalization:**
     - Standardized Saitama's victory animation to his clean, heroic standing victory frames (hands on hips, cape fluttering).
     - Feet locked to $Y = 135$ and head to $Y = 82$ across all frames. Zero jumping.
  3. **9.5s Unhurried Cinematic Knockout:**
     - $4.0\text{s}$ ultra slow-motion collapse (`Time.timeScale = 0.12f`).
     - $1.5\text{s}$ motionless floor pause.
     - $4.0\text{s}$ victory celebration hold.
  4. **Fluid Translucent Anime Fire:**
     - Redrew [`dbz_spiky_aura.png`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Sprites/Effects/dbz_spiky_aura.png) with a rounded ground base that never extends below the feet ($Y=0$) and 7 fluid, soft fire tongues.
  5. **Fighter Scale Normalization:**
  6. **Consolidation & Native Slicer Integration (index.html):**
     - Deleted temporary redundant files (slicer_ssj.html and launch_ssj_slicer.bat) to eliminate tooling fragmentation.
     - Fully integrated Super Saiyan Goku directly into canonical [Tools/SpriteSlicer/index.html](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Tools/SpriteSlicer/index.html) as the primary default character and sheet.
     - Preserved the exact canonical UI, controls, toolbar, and canvas layout that developers expect.
     - Updated [launch_slicer.bat](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Tools/SpriteSlicer/launch_slicer.bat) with cache-busting timestamping (http://localhost:8085/index.html?t=%RANDOM%) to guarantee the latest tool loads in any browser.
     - Added destination folder selector to /api/export in the slicer UI for seamless exporting directly into either Frames (in-game) or Frames_SSJ (alternate).


---

### Bug 29: Simultaneous Hit Double-HitStop TimeScale Freeze & Knockback Launch Overshoot

- **Symptom:**
  1. When both fighters attacked and hit each other at the same time (or during Saitama's rapid punch flurry), the game slowed down to a crawl ($0.04\times$ speed) and never sped back up.
  2. Knockbacks on simple attacks sent fighters flying across the entire screen; light jabs popped opponents high into the air.
  3. During Saitama's Consecutive Normal Punches, Saitama skated forward too fast ($1.5\text{f}$ speed) and pushed the opponent across the arena, while the opponent was knocked into orbit on punch #1.
- **Root Cause:**
  1. **Nested HitStop TimeScale Leak:** In `VFXManager.DoHitStop`, `float prevScale = Time.timeScale;` was sampled inside the coroutine. When Hit 2 interrupted Hit 1, `Time.timeScale` was already $0.04\text{f}$. Hit 2 captured `prevScale = 0.04f` and restored it to $0.04\text{f}$ upon completion, permanently corrupting the game clock.
  2. **Hardcoded Monolithic Vertical Lift:** `HitboxController.cs` applied `targetRb.linearVelocity = new Vector2(facingDir * knockback, 3.2f);` on every hit. Even light jabs applied $3.2\text{f}$ vertical lift.
  3. **Excessive Forward Flurry Speed:** In `FighterController.cs`, `FighterState.Skill` assigned `rb.linearVelocity = new Vector2(surgeDir * 1.5f, rb.linearVelocity.y);` every frame for $2.7\text{s}$, moving Saitama over 4 world units forward.
- **Resolution:**
  1. **Re-Entrant Safe HitStop:** In `VFXManager.cs`, added `isHitStopActive` and `originalTimeScale`. Baseline time scale is only captured when hitstop is not already running. When overlapping hits arrive, the timer is refreshed without overwriting `originalTimeScale`. Added `OnDisable()` safeguard.
  2. **Attack Interruption on Counter-Hit:** In `FighterController.TransitionTo()`, entering `HitStun`, `KnockBack`, or `Knockout` immediately deactivates `attackHitbox`.
  3. **Decoupled 2D Knockback Tuning:** Added `knockbackY` to `HitboxController.Setup()` and `ConfigureAttack()`. Rebalanced all attack velocities:
     - Light: $2.2\text{f}$ X, $0.8\text{f}$ Y (was $4.5\text{f},\ 3.2\text{f}$).
     - Heavy: $5.0\text{f}$ X, $2.0\text{f}$ Y (was $8.0\text{f},\ 3.2\text{f}$).
     - Saitama Flurry: $1.2\text{f}$ X, $0.4\text{f}$ Y (was $6.0\text{f},\ 3.2\text{f}$).
     - Kamehameha: $8.5\text{f}$ X, $2.2\text{f}$ Y (was $14.0\text{f},\ 3.2\text{f}$).
     - Serious Punch: $12.0\text{f}$ X, $3.0\text{f}$ Y (was $24.0\text{f},\ 3.2\text{f}$).
  4. **Controlled Flurry Forward Advance:** Reduced Saitama's `surgeSpeed` during `FighterState.Skill` from `1.5f` to `0.60f`, delivering a tight, menacing forward step into the punch barrage.

---

### Bug 29: Super Saiyan Frame Aspect Ratio Skew, Transparent Margin Jitter & Jump Sprite Regression

- **Symptom:**
  1. Exported Super Saiyan sprite slices had non-uniform dimensions ( \times 90$,  \times 86$,  \times 71$) with bottom empty transparent margins ranging between \text{px}$ and \text{px}$. Without baseline correction, transitioning between Idle, Walk, and Attack caused the character to hop \text{–}15\text{px}$ up and down.
  2. In UpdateJumpPose(), while all other states used GetFrames(ssj..., base...), the jump velocity evaluator checked jumpFrames.Count and assigned jumpFrames[frameIdx] (Base Goku), causing Goku's hair to flash back to black whenever he jumped in Super Saiyan form.
  3. Directional lunge attacks (Punch combo) suffered from horizontal skating when centered blindly.
- **Root Cause:**
  1. **Atlas Crop Origin Disparity:** Sprite slicing from an unevenly spaced sheet captures tight bounding boxes rather than standardized canvas origins. Because LoadFrame() used pivot = (0.5f, 0.0f) for non-128px textures, variable bottom margins shifted the character's ground contact plane on every state transition.
  2. **Variable Shadowing / Typo in UpdateJumpPose():** UpdateJumpPose() evaluated jumpFrames (Base Goku field) rather than the localized jFrames variable returned by GetFrames(ssjJumpFrames, jumpFrames).
  3. **Foot-Anchoring vs Bounding-Box Centering:** In Goku's punch combo, frames 3 and 4 extend his fist 19 pixels forward. Centering by image width pushed his planted back foot backward, creating visual foot slippage.
- **Resolution:**
  1. ** \times 128$ Canvas Standardization Pipeline:**
     - Backed up raw slices to Assets/Sprites/Fighters/Goku/Frames_SSJ_Raw/.
     - Standardized all 44 frames onto uniform  \times 128\text{px}$ transparent RGBA canvases with the bottom foot strictly locked to baseline  = 105$.
     - Mirrored all 12 knockout_*.png frames to death_*.png for bidirectional sequence loader compatibility.
  2. **Fixed Back-Foot Anchoring:**
     - For lunging attack frames (punch_0..5), anchored the planted back foot to  = 42$ while allowing the punching fist to extend freely to  = 105$, eliminating horizontal foot skating.
  3. **UpdateJumpPose() Parameter Repair:**
     - Replaced jumpFrames references with jFrames, cleanly interpolating Goku's 4 Super Saiyan jump frames across vertical physics velocity ($).
  4. **Dynamic Duration FPS Calibration:**
     - Configured LightAttack (.6\text{ FPS}$ over .44\text{s}$), HeavyAttack (.0\text{ FPS}$ over .88\text{s}$), and Knockout (.15\text{ FPS}$ over .8\text{s}$), maintaining exact frame duration synchronization.
  5. **Compiler Verification:** Clean compilation with **0 errors** across both Assembly-CSharp.dll and Assembly-CSharp-Editor.dll.


---

### Bug 30: Saitama Celebratory Jump Arc Restoration, Single-Turn Round Transition & Ki Aura Particle De-Cluttering

- **Symptom:**
  1. In a previous fix, Saitama's victory animation was flattened to a static standing pose because the previous loop caused repetitive "pogo-sticking." However, Saitama's authentic celebratory gesture was supposed to include his dynamic jump.
  2. Round end pauses were locked to a hardcoded 4.0-second delay in `MatchManager`, forcing players to wait through repetitive animation loops before Round 2 or Final Round began.
  3. Rising vertical Ki particle streaks in `FighterKiAura.cs` cluttered character silhouettes and clashed with the clean, razor-sharp 16-bit pixel-art presentation.
- **Root Cause:**
  1. **Premature Jump Removal:** Rather than running the victory celebration as a single-turn sequence, the previous fix simply stripped the jumping frames from `victory_*.png`.
  2. **Looping Non-Loopable Keyframes:** `FighterAnimator.cs` executed `FighterState.Victory` via `PlayLoop`. Because victory animations contain transition poses (squat $\to$ leap $\to$ land $\to$ pose), looping them continuously caused the character to bounce up and down endlessly until the timer ran out.
  3. **Decoupled Round Reset Timer:** `MatchManager.cs` used a rigid serialized delay (`roundEndPauseDuration = 4.0f`) instead of observing the victor's actual single-turn animation completion.
  4. **Over-Engineering with Particle Systems:** Spawning a runtime `ParticleSystem` (`Aura_Rising_Streaks`) overlaid unscaled particle textures on top of hand-drawn pixel sprites.
- **Resolution:**
  1. **9-Frame Mathematical Jump Arc (`victory_0..8.png`):**
     - Sliced and standardized all 9 authentic frames on a $160 \times 160\text{px}$ canvas.
     - Anchored frames 0–2 (squat) at $Y = 135$, frame 3 (takeoff) at $Y = 120$ ($15\text{px}$ lift), frame 4 (apex mid-air punch) at $Y = 104$ ($31\text{px}$ airborne), and frames 5–8 (landing impact, cape settling, and triumphant fist pump) at ground baseline $Y = 135$.
  2. **Single-Turn Animation Pacing (`FighterAnimator.cs`):**
     - Switched `FighterState.Victory` to `PlayOnce(vic, victoryFps, unscaled: true)`.
     - Exposed `VictoryDuration => (float)vic.Count / victoryFps;` ($1.50\text{s}$ at 6.0 FPS).
  3. **Immediate Next-Round Transition (`MatchManager.cs`):**
     - Updated `RoundEndSequence` to yield exactly `winnerAnim.VictoryDuration`.
     - The instant that single turn concludes, `CheckWinCondition()` immediately fires `StartRound()` without any awkward pause or repetitive bouncing.
  4. **Pure Sprite Anime Fire Cocoon (`FighterKiAura.cs`):**
     - Completely removed the `ParticleSystem` component, particle renderer, and speed-streak texture logic.
     - Preserved the high-frequency ($16\text{ Hz}$) oscillating spiky flame shell cocoon (`dbz_spiky_aura.png`), ensuring pristine pixel-art readability.
  5. **State-Entry Audio Trigger (`FighterController.cs`):**
     - Added `fighterAudio?.PlayJump()` to `FighterState.Victory` for Saitama, providing a crisp audio whoosh as he leaps into the air.
  6. **Compiler Verification:** Clean compilation with **0 errors** across both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.


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
  2. **Deferred End-of-Frame Arbitration:** Replaced immediate death resolution with `EvaluateFighterDefeats()` coroutine (`yield return new WaitForEndOfFrame()`). If both fighters have $\le 0\text{ HP}$, `EndRoundDraw(isDoubleKO: true)` is triggered, collapsing both fighters in slow motion and awarding 1 point to each.
  3. **+100s Sudden Death Overtime:** Added `isOvertime` flag. Expiration of regular 199s time grants +100 seconds with audio/visual feedback. If overtime expires and both remain alive, `EndRoundDraw(isDoubleKO: false)` awards 1 point each with zero collapse.
  4. **Sudden Death Tiebreaker Progression:** In `CheckWinCondition()`, tied match point scores (e.g. 2-2) trigger a Sudden Death Final Round (`StartRound()`).

---

### Bug 30: Multi-Hit Hitstop Engine Freeze, Screen Shake Saturation & Transformation Hitbox Overlap

- **Symptom:**
  1. Activating Goku's Skill (Super Saiyan ascension) dealt damage to nearby opponents even though it is purely a transformation power-up rather than a melee attack.
  2. Saitama's Consecutive Normal Punches caused the entire game engine to stutter and freeze.
  3. Camera shake escalated into a disorienting, exponential tremor during the flurry.
- **Root Cause:**
  1. **Transformation / Attack Hitbox Coupling:** In `FighterController.cs`, `TransitionTo(FighterState.Skill)` entered the generic skill state without deactivating `attackHitbox`. If a previous attack had configured the hitbox, or if Goku was already in Super Saiyan, `ConfigureAttack()` configured 26 damage and 3.6m reach.
  2. **Re-Entrant Hitstop Freezing (`Time.timeScale = 0.04f`):** Every registered punch called `VFXManager.TriggerHitStop(0.065s)`. Across rapid multi-hits, the physics engine was forced into near-zero time scale ($4\%$) repeatedly, virtually halting the game loop.
  3. **Uncapped Camera Shake Accumulation:** Every hit triggered default camera shake ($0.132$), which stacked continuously over the 2.70-second flurry.
- **Resolution:**
  1. **Strict Non-Damaging Transformation Pipeline:**
     - For Goku, `attackHitbox.Deactivate()` is immediately called on Skill activation.
     - `attackStartupTimer` and `attackActiveTimer` set to `0.0f`, ensuring zero hitboxes activate during transformation.
     - Goku's forward surge speed during transformation is zeroed out, preventing unwanted sliding while roaring.
  2. **Selective Hitstop Bypassing (`enableHitStop`):**
     - Added `hitStop = true` and `shakeMult = 1.0f` parameters to `HitboxController.ConfigureAttack()` and `VFXManager.PlayHitSpark()`.
     - For Consecutive Normal Punches, `hitStop` is set to `false`, keeping `Time.timeScale = 1.0f` (100% full speed) with zero engine stutter.
  3. **Attenuated Shake & Micro-Knockback:**
     - Set `shakeMult = 0.20f` for a subtle, high-frequency vibration rather than a violent screen jolt.
     - Configured knockback to $X = 0.35	ext{m/s}, Y = 0.0	ext{m/s}$, keeping the opponent neatly inside the flurry.
  4. **Exactly 10 Hits @ 2 Damage:**
     - Added `flurryHitsRemaining = 10` counter and `flurryHitTimer = 0.23f`.
     - Delivers exactly 10 hits ($10 	imes 2 = 20$ damage) across the 2.30s active window, then cleanly deactivates the hitbox while the vocal shout completes.
  5. **Compiler Verification:** Clean compilation with **0 errors** across both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.


---

### Bug 31: Full-Screen Beam Hitbox Alignment & Multi-Hit Recursion in Long-Duration Beams

- **Symptom:**
  1. Goku's Kamehameha lacked visual energy effects; Goku played his charging pose and beam stance, but no actual energy beam or charge orb appeared on screen.
  2. The previous attack hitbox was only 3.0m wide, failing to cover the visual reach of a beam blast across the arena.
  3. Long-duration beam attacks risked multi-hitting opponents on every physics frame, instantly draining 100% health in less than a second.
- **Root Cause:**
  1. **Missing Beam VFX Assembly:** No dedicated sprite hierarchy existed to render the charge orb, muzzle flare, and beam body.
  2. **Hitbox Mismatch:** The hitbox offset ($1.8\text{m}$) and size ($3.0\text{m}$) were scaled for a short melee thrust rather than a full-screen energy projectile.
  3. **Multi-Hit Vulnerability:** Attacks that stay active across multiple seconds must strictly guard against clearing hit targets unless designed as rapid flurries.
- **Resolution:**
  1. **Modular VFX Component (`KamehamehaBeamVFX.cs`):** Created a self-contained component driving the 2.15s Ki charge orb gathering and 1.21s solid light-blue energy beam with high-frequency plasma breathing ($26\text{ Hz}$).
  2. **Generated 4 DBZ Effect Textures:** Programmatically generated `kamehameha_charge_orb.png`, `kamehameha_beam_body.png`, `kamehameha_beam_head.png`, and `kamehameha_muzzle_flare.png` in `Assets/Sprites/Effects/`.
  3. **True AoE Hitbox Synchronization:** Configured hitbox offset to `new Vector2(4.6f, 0.95f)` and size to `new Vector2(8.5f, 1.6f)`, matching the full $8.5\text{m}$ visual beam.
  4. **Single-Hit Guarantee:** Maintained `hitTargetsThisSwing` set persistence across the entire blast duration, guaranteeing exactly one hit per target.

---

### Bug 32: Goku Legacy Skill Animation Cleanup & Instant Direct-to-SSJ Idle Transition

- **Symptom:**
  1. When triggering Goku's Skill (Key `E` / `U`), Goku played an outdated charge/strike animation before transforming, rather than ascending directly into his Super Saiyan Idle stance.
  2. Leftover skill sprite files (`skill_0..3.png`) lingered in Goku's runtime `Frames/` directory, creating technical debt and confusing the automatic sequence loader.
- **Root Cause:**
  1. **Residual Prototype Slices:** Goku's `Assets/Sprites/Fighters/Goku/Frames/` directory still contained leftover `skill_0.png` through `skill_3.png` and their corresponding `.meta` files from early prototype combat passes.
  2. **Loader Sequence Auto-Discovery:** `FighterAnimator.cs` dynamically loaded all indexed `skill_*.png` files into `skillFrames`. If the fighter transitioned to `FighterState.Skill`, the animator played this old sequence.
  3. **State Transition Lockout:** `FighterController.cs` originally assigned an `attackTimer = 0.65f` and called `TransitionTo(FighterState.Skill)`, which locked Goku into an attack state for over half a second before allowing idle.
- **Resolution:**
  1. **Purged Legacy Sprite Assets:** Deleted `skill_0..3.png` and `skill_0..3.png.meta` from `Assets/Sprites/Fighters/Goku/Frames/` (backed up in `Frames_Backup/`).
  2. **Direct Super Saiyan Idle Transition:** In `FighterController.cs`, Goku's Skill pathway was rewritten to immediately deactivate `attackHitbox`, zero out all attack timers (`attackTimer = 0f`, `attackStartupTimer = 0f`, `attackActiveTimer = 0f`), and immediately call `TransitionTo(FighterState.Idle)`.
  3. **Zero-Delay Visual Transformation:** Because `controller.IsSuperSaiyan` is asserted true before `TransitionTo(FighterState.Idle)`, `FighterAnimator.UpdateAnimation()` queries `ssjIdleFrames` on that exact frame, seamlessly snapping Goku directly into his iconic Super Saiyan golden idle stance with zero intermediate frames, zero hitbox latency, and zero damage dealt.
  4. **Project Assembly Alignment:** Added missing `KamehamehaBeamVFX.cs` compile item to `Assembly-CSharp.csproj`, ensuring automated verification tools and IDE build systems pass with **0 errors**.


---

### Bug 31: TextMeshPro Menu Text Rendering as Solid Rectangles / Colored Blocks (The Missing _MainTex & Empty Fallback Bug)

- **Date:** October 28, 2026
- **Severity:** High (UI Visual & Readability Defect)
- **Component:** `Assets/Fonts/TMP_Assets/`, `Assets/Prefabs/UI/HUD_Canvas.prefab`, `CombatMenuController.cs`

#### Symptom:
When pausing combat (`ESC` / `P`) or concluding a tournament match, all text elements across the modal window—including `"PAUSED"`, selection buttons (`"RESUME"`, `"RESTART MATCH"`), and post-match victory cards—rendered as solid colored rectangles/blocks of color rather than legible letterforms.

#### Root Causes:
1. **Disconnected `_MainTex` on Font Asset Materials:**
   In `ArcadeClassic_SDF.asset`, `RetroGaming_SDF.asset`, and `PressStart2P_SDF.asset`, the embedded Material sub-assets had:
   ```yaml
   - _MainTex:
       m_Texture: {fileID: 0}
   ```
   When `_MainTex` is null, the TextMeshPro Distance Field pixel shader samples Unity's default $1 \times 1$ white texture, returning a distance value of $1.0$ across the entire glyph quad. Because $1.0 > 0.5$ threshold, every pixel in the quad is rendered at 100% opacity, creating solid blocks.
2. **Missing Glyphs in Retro Atlases:**
   `ArcadeClassic_SDF` only contained 25 uppercase characters; `RetroGaming_SDF` only contained 44 characters (missing `D`, `G`, `K`, `X`, `Y`, `Q` and digits `3-9`). Both had `m_FallbackFontAssetTable: []` empty.
3. **Non-ASCII Unicode Symbols:**
   Code and prefabs contained Unicode symbols (`▶`, `◀`, `★`, `—`, `•`) that did not exist in the retro font character sets.
4. **Missing Resources Directory:**
   `CombatMenuController.LoadFont()` attempted `Resources.Load<TMP_FontAsset>("Fonts/...")`, but `Assets/Resources/Fonts/` did not exist.

#### Resolution:
1. **Reconnected `_MainTex` in YAML:**
   - In `ArcadeClassic_SDF.asset`: Set `_MainTex` to `{fileID: -3925478913968619439}`.
   - In `RetroGaming_SDF.asset`: Set `_MainTex` to `{fileID: -1603636736672497938}`.
   - In `PressStart2P_SDF.asset`: Set `_MainTex` to `{fileID: -6318304193973382029}`.
2. **Configured Fallback Chaining:**
   Added `LiberationSans SDF` (`guid: 8f586378b4e144a9851e7b34d9b748ee`) to `m_FallbackFontAssetTable` in both retro font assets.
3. **Created `Assets/Resources/Fonts/`:**
   Populated `Assets/Resources/Fonts/` with font assets so `Resources.Load<TMP_FontAsset>()` succeeds reliably.
4. **Sanitized Typography to Universal ASCII:**
   Replaced all Unicode symbols in `HUD_Canvas.prefab` and `CombatMenuController.cs`:
   - `▶ ... ◀` -> `> ... <`
   - `★` -> `*`
   - `—` -> `-`
   - `•` -> `|`
5. **Implemented Runtime Self-Healing (`CombatMenuController.cs`):**
   - Added `EnsureFontIntegrity()` to re-link material textures dynamically if missing.
   - Added `SanitizeAndHealUIHierarchy()` called during `Awake()`, `Start()`, `PauseGame()`, and `ShowResultsSequence()` to validate all labels and enforce mesh updates.
   - Added hierarchy auto-wiring in `EnsureUIHierarchyBuilt()` to automatically bind buttons and labels if unassigned in scene prefabs.

#### Verification:
- All assemblies compiled cleanly with **ZERO errors** via `verify_compilation.py`.
- Verified font asset `_MainTex` points to Texture2D sub-assets.
- Verified pure ASCII encoding across `CombatMenuController.cs` and `HUD_Canvas.prefab`.
