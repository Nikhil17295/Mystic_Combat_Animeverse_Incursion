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

## 2. Bug Postmortems & Solutions

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

## 3. Team Best Practices Checklist

1. **Always check the Console (`Ctrl + Shift + C`):** Any red error or yellow warning must be resolved immediately before moving to the next feature.
2. **Never hardcode values:** If you need a new stat (e.g. projectile speed), put it in the character JSON file.
3. **Keep `FixedUpdate` clean:** Only physics velocities and impulses belong in `FixedUpdate`.
4. **Use `[SerializeField] private` instead of `public`:** Encapsulate variables; only expose what other scripts genuinely need to read.
