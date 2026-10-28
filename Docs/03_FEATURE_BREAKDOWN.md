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
Ki is the mystic combat energy (0â€“100) required to execute special skills and ultimates.
- **No passive regeneration:** Players cannot camp or run away to recharge Ki.
- **How to gain Ki:**
  1. Hitting the opponent (+5 to +10 Ki per strike).
  2. Taking damage (comeback mechanic: +3 to +4 Ki per hit taken).
  3. **Meditation:** Holding the meditate key roots the fighter in place to channel Ki at a rate of 12â€“15 Ki/sec.

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
- In classic 1v1 arcade fighting games (Street Fighter III, Guilty Gear), fighters occupy **40%â€“50% of the vertical screen height** so hits and expressions feel impactful.
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

---

## 8. Visual Hitbox & Hurtbox Scene Gizmos (The Feel Factor)

### Why Fighting Games Require Visual Hitbox Tuning
A punch that misses by 2 pixels feels unfair; a punch that hits from 3 feet away feels cheap. Combat tuning is about visual reach and player expectations.

### How It Is Implemented
- **Hurtbox (Green Box):** Managed by [`HurtboxController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/HurtboxController.cs). Draws a translucent green volume over the fighter's body.
- **Hitbox (Red Box):** Managed by [`HitboxController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/HitboxController.cs). Draws a bright red volume representing attack reach.
- **Auto-Mirroring:** When the fighter turns left (`spriteRenderer.flipX = true`), the Hitbox's local scale is automatically mirrored:
  ```csharp
  attackHitbox.transform.localScale = new Vector3(faceLeft ? -1f : 1f, 1f, 1f);
  ```
- **Inspector Adjustment:** Designers can adjust `Hitbox Offset` and `Hitbox Size` directly in the Unity Inspector and watch the red box expand or retract in real-time in the Scene view without touching code.

---

## 9. Arena Containment & Invisible Boundary Walls

### The Problem
In 2D fighting games, fighters must never run off-screen or escape the camera's field of view.

### How It Is Implemented
- The Main Camera uses an Orthographic Size of `3.2` at position `(0, -1.2, -10)`. At a 16:9 aspect ratio, the camera viewport spans from $x = -5.69$ to $x = 5.69$.
- We placed solid invisible `BoxCollider2D` boundary walls directly at the camera borders:
  - **Left Wall:** `x = -5.7`, size `(1, 14)`
  - **Right Wall:** `x = 5.7`, size `(1, 14)`
  - **Floor:** `y = -3.5`, size `(24, 1)` (top surface at $y = -3.0$)
  - **Ceiling:** `y = 3.2`, size `(16, 1)`
- **Result:** Fighters are cleanly contained within the camera frame with zero off-screen escaping.

---

## 10. Character Visual Proportions & Scale Normalization

### The Visual Scale Challenge
In Jump Ultimate Stars, Goku's sprite is wide and muscular (large hair and broad dogi), while Saitama's sprite is slender. Rendered at identical scale, Goku inadvertently dwarfed Saitama.

### The Scale Multiplication Fix
To give Saitama heroic, towering anime presence while keeping Goku stocky and athletic:
- **Goku Root Scale:** `(0.88, 0.88, 1.0)`
- **Saitama Root Scale:** `(1.12, 1.12, 1.0)`
- **Proportional Inheritance:** Scaling the root GameObject automatically multiplies the child `SpriteRenderer`, `BoxCollider2D`, `Hurtbox`, and `Hitbox` proportionally, ensuring visual reach matches physical collisions without manual re-tuning.

---

## 11. Godot-Style 2D WYSIWYG Scene Baking (`SceneSetupTool.cs`)

### Transitioning from Procedural Code to Live 2D Editing
Rather than generating the game world dynamically in code on Play (`PrototypeBootstrap.cs`), [`SceneSetupTool.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/SceneSetupTool.cs) bakes the entire visual game directly into `SampleScene.unity`:
1. The anime tournament backdrop is placed at sorting order `-10`.
2. The floor and invisible walls are visible in the scene hierarchy.
3. Goku and Saitama are placed in their starting positions with their real idle sprites rendered in the editor via `[ExecuteAlways]`.
4. The Scene View automatically locks to 2D Mode (`in2DMode = true`).
5. **Result:** Developers experience the exact same live WYSIWYG visual editing as Godot's 2D workspace!

---

## 12. Grand 20% Anime Tournament HUD & Doctor Strange Time Mandala

### 1. 20% Screen Coverage & Majestic Arcade Presence
In competitive fighting games, the HUD cannot be a flimsy, miniature overlay. At 1080p, Mystic Combat's HUD spans the top **216px** (exactly $20\%$ of the screen height), creating an authoritative, commanding visual header:
- **Heroic Bust Portrait Cards (160Ã—160px with Designer Dragon Corners):**
  - **Saitama:** Full bust portrait (head, intense combat stare, white cape, yellow collar) set against a high-contrast crimson solar burst with kinetic speed streaks (`saitama_avatar_heroic.png`). Mirrored inward toward center stage and framed with ornate 8px gold filigree and dragon corner brackets.
  - **Goku:** Official *Extreme Butoden* combat bust set against a multi-stop golden Super Saiyan dragon ki sunburst (`goku_avatar_heroic.png`).
- **Heavy 2-Tone Arcade Health Bars (650Ã—52px) with Golden Dragon Crawlers:**
  - Framed in `health_bar_grand_bg.png` with 8px beveled gold rims and charcoal shadow.
  - **Golden Dragon Crawler (`dragon_crawler_p1.png` & `p2.png` 650Ã—44px):** A sinuous Eastern lung dragon with golden scales, whiskers, serrated dorsal spines, and sharp claws crawling along the top outer rim of the health bar, facing toward the central Time Stone.
  - 2-tone fill (`health_bar_grand_fill.png`): bright lemon-yellow top half, rich amber bottom half.
  - Red damage ghost bar (`health_bar_grand_ghost.png`): holds position for `0.35s` before sliding down at `0.45/s` via `Mathf.MoveTowards()`.
- **Electric Cyan Ki Gauges (520Ã—24px):**
  - High-visibility energy meters (`ki_bar_grand_bg.png` & `ki_bar_grand_fill.png`) that fill as fighters meditate or land hits.
- **Bold 3D Multi-Color Gradient Typography:**
  - **Goku Name ("GOKU"):** 36pt bold font with Super Saiyan 4-corner vertex gradient (Radiant Sun Gold `#FFF573` $\rightarrow$ Fiery Dragon Crimson `#D92600`).
  - **Saitama Name ("SAITAMA"):** 36pt bold font with One-Punch Hero vertex gradient (Blazing White-Gold `#FFF0E8` $\rightarrow$ Deep Hero Maroon `#8C001E`).
  - **Countdown Timer:** 50pt bold numerals with MCU Emerald-Jade gradient (`#E0FFF8` $\rightarrow$ `#00B359`).
  - **Round Title:** 20pt bold header with Antique Tournament Gold gradient (`#FFF9E0` $\rightarrow$ `#D98200`).

### 2. Doctor Strange / MCU Mystic Time Stone & Spell Mandala (No Box!)
- **No Generic Clock Box:** The rectangular clock housing was completely discarded. In Marvel lore, the Time Stone is an ancient cosmic infinity stone harnessed through green eldritch magic.
- **Mystic Runic Spell Banner (`mystic_runic_banner.png` 960Ã—36px):**
  - Repositioned along the top border (`y = -18`) with high-luminescence Taoist eldritch mandalas and runic bridges framing the entire combat cluster.
- **Rotating Time Mandala (`time_stone_mandala.png`):**
  - A 160Ã—160 circular emerald spell mandala (Tao dial) featuring concentric runic rings, sacred geometric interlaced triangles, and 24 outer eldritch tick marks.
  - Continuously rotates at $22^\circ/\text{s}$ in `Update()`, giving the physical impression of active temporal sorcery.
- **Emerald Time Stone (`timestone_gem_emerald.png` enlarged to 56Ã—56):**
  - Isolated from the user's authentic artifact screenshot, cleaned of white halos, upscaled with point filtering, and enhanced with diamond specular facets, an inner pulsing Infinity Core (`#00FFA3`), and an Eye of Agamotto bronze/gold bezel.
- **Radiant Emerald Countdown Numerals:**
  - Big 50pt numerals in electric emerald-white glowing in the center of the spell dial.

### 3. Tactical Dual-Gem Point System & Central Golden Trophy Relic
- **Dual Round Gems per Fighter (Two 56Ã—56 Sockets per Player):**
  - **Zero Text Clutter:** Removed clunky `"WINS: 0"` and `"WINS: 1"`. The dual gems alone provide immediate, tactile visual clarity:
    - **0 Rounds Won:** Both Gem 1 and Gem 2 are dark hollow metallic sockets (`gem_socket_hollow.png` 56Ã—56).
    - **1st Round Won:** Gem 1 ignites into the vibrant **Shiny Blue Gem** (`gem_shiny_blue.png` 56Ã—56).
    - **2nd Round Won:** Gem 2 ignites into the second **Shiny Blue Gem**, clinching the match.
- **Center Big Gold Gem (`gem_big_gold.png` & `gem_big_gold_hollow.png` enlarged to 68Ã—68):**
  - Positioned directly beneath the Time Stone mandala as the grand tournament championship relic.
  - **Round Hollow Socket Pre-Match:** Starts as a circular hollow golden socket (`gem_big_gold_hollow.png`), matching the round point gems.
  - **Match Point Mode:** When either fighter reaches match point (`p1Wins >= 1 || p2Wins >= 1`), the relic ignites into the radiant **authentic round golden gem** ([`gem_big_gold.png`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Sprites/UI/gem_big_gold.png), extracted directly from the multi-gem screenshot `Screenshot 2026-10-25 112541.png`) with a breathing golden pulse ($1.0\times \rightarrow 1.15\times$ sine wave), and the title banner updates to `â˜… ROUND X â€¢ MATCH POINT â˜…` (or `â˜… FINAL ROUND â€¢ MATCH POINT â˜…` on 1â€“1 ties).
- **Single-Line Bold Header Typography (600Ã—36px):**
  - Expanded container from 340px to 600px with `textWrappingMode = TextWrappingModes.NoWrap` and bold 24pt 3D vertex gradient text, guaranteeing crisp, unclipped single-line titles across all match states.
- **Vibrant Heroic Avatar Cards (160Ã—160):**
  - **Goku:** Deep cosmic indigo night sky with a 12-ray blazing Super Saiyan golden ki sunburst behind his spiky hair, framed in an 8px chiseled gold arcade border.
  - **Saitama (Full Bust):** Extracted in full proportion from the user's authentic reference (`Screenshot 2026-10-25 112023.png`) displaying his entire heroic bust (bald head, white cape, yellow collar/suit, and clenched red gloved fist) over a deep anime purple/magenta vignette, mirrored inward toward the ring.

### 4. Autonomous Round Transitions & Round 3 Decider
- **1.8s K.O. Pause:** Combat smoothly pauses on final hit to display the K.O. impact before score incrementing.
- **Round Reset (`ResetForNewRound`):** Fighters snap back to spawn positions (`-2.2` and `+2.2`), velocities zeroed, states reset to Idle, and health/ghost bars immediately refilled to 100%.
- **Round 3 Decider:** If P1 wins Round 1 and P2 wins Round 2, the match automatically advances to Round 3 (Final Round Match Point), where the winner takes the tournament.

---

## 13. General-Purpose Dev Sprite Slicer & Animation Playground (`Tools/SpriteSlicer/`)

### Purpose & Architecture
When developing a 2D fighting game, evaluating candidate sprite sheets (e.g. testing different anime characters) directly in Unity can pollute the Asset Database with duplicate meta files, unneeded textures, and editor re-import lag. 

The interactive Sprite Slicer web application was upgraded into a **general-purpose dev testing tool & animation playground**:
1. **Zero-Clutter In-Browser Loading:**
   - Supports opening any image via the **"ðŸ“ Open Image from PC"** file dialog or dragging & dropping directly onto the canvas.
   - Utilizes HTML5 `URL.createObjectURL(file)` to load textures 100% into browser memory (RAM).
   - **Zero files are written, copied, or uploaded to `Assets/Sprites/`** during testing.
2. **Dynamic Animation Tracks:**
   - Rather than being locked to hardcoded categories, developers can click **"âž• New Animation"** and define custom tracks on the fly (e.g., `dash`, `taunt`, `fireball`, `teleport`).
   - Animation tracks can be deleted (`âœ•`), frames can be cleared in bulk, and frame counts are visually badged on each tab.
3. **Live 60 FPS Canvas Preview:**
   - Real-time frame loop with variable FPS slider (2â€“24 FPS), frame pause/play toggle, and pixel-perfect rendering.
4. **Offline Coordinate Backup (`.json`):**
   - Developers can click **"ðŸ’¾ Download Slices (JSON)"** to export their exact frame coordinates `(x, y, w, h)` to disk without touching the Unity project.
5. **Safe Project Export:**
   - If a spritesheet is officially committed to the game project (e.g. Goku or Saitama), clicking **"âš¡ EXPORT & APPLY TO UNITY GAME"** automatically crops, normalizes transparency, and exports sprite assets directly into `Assets/Sprites/Fighters/{Name}/Frames/`.

### Godot vs. Unity Architectural Mapping
| Concept | Godot Equivalent | Unity Equivalent | In Sprite Slicer Dev Playground |
| :--- | :--- | :--- | :--- |
| **Animation Container** | `SpriteFrames` Resource | `AnimationClip` / `AnimatorController` | `characterData[char].animations` dictionary |
| **Custom Animation Track** | Adding track in `SpriteFrames` (`"idle"`, `"dash"`) | Creating `.anim` clip in Project window | Clicking `+ New Animation` and typing track name |
| **Frame Region** | `AtlasTexture` Region `(Rect2)` | `Sprite.Create(rect, pivot)` / Sprite Editor slicing | Green bounding box `(x, y, w, h)` |
| **Live Playback** | `AnimatedSprite2D` editor preview | `FighterAnimator.cs` `[ExecuteAlways]` | `<canvas id="previewCanvas">` preview loop |

---

## 14. Retro Arcade Main Menu & Double Dragon II Story Prologue (`MainMenuScene.unity`)

### Overview & Visual Design
The start page delivers an authentic 90s retro arcade brawler experience directly inspired by ***Double Dragon II: The Revenge*** (NES & Arcade) and fused with the Doctor Strange cosmic multiverse theme:

1. **Stage 1: Dramatic Story Prologue (Attract Sequence):**
   - High-contrast void backdrop with sequential line-by-line narrative reveal:
     - *"IN THE YEAR 20XX..."*
     - *"A CATASTROPHIC MULTIVERSE RIFT SHATTERED REALITY."*
     - *"WARRIORS FROM PARALLEL ANIME REALMS ARE DRAWN INTO BATTLE."*
     - *"ONLY THE STRONGEST CAN HOLD THE LINE!"*
     - *"LET'S GO MYSTIC WARRIORS!"*
   - Each line pops in with dynamic color shift (dim cyan $\rightarrow$ radiant gold $\rightarrow$ crisp white) and scale zoom.
   - Blinking `â–º PRESS ANY KEY OR CLICK TO SKIP â—„` prompt allowing instant transition to the title screen.

2. **Stage 2: Double Dragon II Style Hero Title Logo Sprite:**
   - Handcrafted 16-bit arcade title logo ([`mystic_combat_title_logo.png`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Sprites/UI/MainMenu/mystic_combat_title_logo.png)) mirroring Technos Japan's 1988 *Double Dragon II* arcade composition:
     - Giant chiseled gold Doctor Strange mystic mandala relic in the background.
     - Extruded metallic chrome "MYSTIC COMBAT" typography with sky-blue and white horizontal reflection.
     - Electric royal-blue anime kanji calligraphy in the center with gunmetal 3D drop shadow.
     - Fiery crimson brush-script "~ Animeverse Incursion ~" subtitle slanted across the bottom.
     - **Clean Alpha Pipeline:** Background box alpha-keyed to zero transparency (`alphaIsTransparency = 1`), seamlessly floating the gold mandala and chrome typography over the cosmic void without blue bounding boxes or secondary green mandala clutter.
     - Subtle breathing/heartbeat scaling animation ($1.0\times$ to $1.015\times$) at 3 Hz.

3. **Stage 3: Pure Retro Arcade Menu Typography & Multi-Color Vertex Gradients:**
   - Eliminated modern monochromatic white text and rounded Web2 boxes.
   - TextMeshPro 4-corner `VertexGradient` rendering:
     - **Active Selection:** Top-Left Radiant Gold (`#FFF066`), Top-Right Sunburst White (`#FFFFFF`), Bottom-Left Blazing Amber (`#FF8C00`), Bottom-Right Fiery Crimson (`#FF2244`), enhanced with dynamic gold-amber shimmer cycling at 4.5 Hz.
     - **Inactive Items:** Top-Left Electric Cyan (`#00F2FE`), Top-Right Diamond Silver (`#E0F7FA`), Bottom-Left Neon Violet (`#7F00FF`), Bottom-Right Deep Cobalt (`#3F51B5`).
   - Classic flashing retro cursor: `â–º` that tracks the selected option.
   - Classic blinking `[ PUSH ENTER OR SPACE TO START ]` arcade prompt at 2 Hz.
   - 124px vertical clearance gap between the bottom of the logo ($y = -56\text{px}$) and Item 0 ($y = -180\text{px}$).
   - Keyboard (`W/S`, `Up/Down`), Gamepad, and Mouse pointer hover/click support.

4. **Stage 4: Arcade Entrance Animation & Audio Pipeline:**
   - **Logo Impact Punch:** On start, title logo zooms from $1.35\times$ down to $1.0\times$ with an elastic arcade spring bounce ($0.94\times \rightarrow 1.0\times$) smoothly settling into place.
   - **Staggered Option Cascade:** The 4 arcade options slide up into position sequentially with a $0.06\text{s}$ interval.
   - **Selective SFX Muting for Music Showcase:** Procedural entrance "twik" impact sounds and button navigation clicks are muted (`enableEntranceImpactAudio = false`, `enableButtonChangeAudio = false`) to isolate the authentic Double Dragon II title track without conflicting synth blips.
   - **Title Screen BGM Player:** Built-in looping audio architecture in [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs) (`musicSource` + `titleMusicClip`) with automated discovery in `Assets/Audio/Music/`.
   - Fullscreen cosmic multiverse backdrop ([`menu_bg_multiverse.png`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Sprites/UI/MainMenu/menu_bg_multiverse.png)) showcasing deep space stars, nebulae, and dimensional rift colors.

---

## 15. Dynamic Combat Audio Architecture & Real-Time Inspector Volume Mixing (`MatchAudioController.cs`)

### The Problem
In fighting games, abrupt mid-match music changes between rounds can feel disjointed and break combat immersion. Furthermore, if combat background music is mixed at full volume (1.0), it drowns out critical tactical gameplay audio cues: punch and kick impacts, hurt grunts, whiff sounds, Ki meditation charging, and character voice clips. Additionally, developers and tournament organizers need the ability to balance audio levels in real time without restarting the scene or recompiling code.

### How It Is Implemented
- Located in: [`MatchAudioController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchAudioController.cs) attached to the `MatchManager` GameObject in [`SampleScene.unity`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scenes/SampleScene.unity).
- **Event-Driven Observer Pattern:** Subscribes to `MatchManager.OnRoundStart`, `MatchManager.OnRoundEnd`, and `MatchManager.OnMatchOver`.

### Audio Flow & Tournament Flow
1. **Continuous Tournament Combat Theme (All 3 Rounds):**
   - Plays **`Double Dragon 2 - Final Battle 2.mp3`** on loop across Rounds 1, 2, and 3.
   - High-energy, driving 8-bit drumline provides seamless combat momentum across the entire match without abrupt track changes, maintaining franchise consistency with the main menu title screen.
2. **Subtle KO Round-End Volume Dip:**
   - On `OnRoundEnd`, an asynchronous coroutine (`FadeVolume`) smoothly dips the battle music volume to $50\%$ over $0.4\text{s}$ during the dramatic K.O. pause, emphasizing the defeat impact before returning to full battle volume when the next round commences.
3. **Match Over (3-Second Victory Fanfare):**
   - When the final round K.O. lands and `OnMatchOver` fires:
     - The combat music is immediately halted (`musicSource.Stop()`).
     - Plays **`Double Dragon 3 - Mission Clear.mp3`** (~3 seconds) via a dedicated secondary `AudioSource` at full celebratory presence (`0.85`) as the official victory fanfare while the winning fighter celebrates!

### Round Sequencing & Bell Pacing Architecture
- **Round Start:**
  1. The round start bell rings at full volume (`bellVolume = 1.0f`).
  2. The battle music does NOT play immediately; it waits for a dramatic $0.85\text{s}$ pause while the gong resonates across the arena.
  3. The combat BGM kicks in with full energy from the very start (`time = 0.0s`).
- **Round Defeat (K.O.):**
  1. The final blow triggers `HandleRoundEnd()`.
  2. The battle BGM is **immediately halted** (`musicSource.Stop()`).
  3. The K.O. bell rings out alone over the arena silence, providing maximum dramatic weight to the knockout.
- **Between Rounds:**
  1. Fighters reset to their spawn positions.
  2. The next round start bell rings.
  3. After the $0.85\text{s}$ delay, the battle BGM restarts fresh from `0:00`.
- **Menu & Combat Isolation:** Battle music is strictly barred from ever playing in the Main Menu (`GameManager.Instance.CurrentState != GameState.MainMenu`).
- **Round-Start Gating:** Battle music only begins when round combat actually starts (`matchManager.CurrentMatchState == MatchState.RoundInProgress`).
- **Scene Exit Handshake:** In [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs), the title music (`double_dragon_ii_title.mp3`) is explicitly stopped upon selecting a game mode (`StartGameMode`) and during `OnDestroy()`, ensuring zero track bleed or simultaneous playback.

### Live Inspector Volume Sliders (Godot `@export_range` vs Unity `[Range]`)
Both [`MainMenuController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/MainMenuController.cs) and [`MatchAudioController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchAudioController.cs) expose real-time Inspector sliders:
- **Title Music:** `[SerializeField] [Range(0f, 1f)] private float musicVolume = 0.70f;`
- **Combat BGM:** `[SerializeField] [Range(0f, 1f)] private float battleMusicVolume = 0.50f;`
- **Victory Fanfare:** `[SerializeField] [Range(0f, 1f)] private float victoryJingleVolume = 0.85f;`

By synchronizing `musicSource.volume` in `Update()`, developers, sound engineers, and evaluators can drag the volume sliders in the Unity Inspector live during Play mode and hear instant volume changes without restarting the match!

---

## 16. State-Driven Fighter Audio System & SFX Library (`FighterAudio.cs`)

### The Problem
Fighting games feel lifeless and floaty without immediate acoustic feedback. However, binding audio directly to raw button presses (`Input.GetKeyDown`) creates severe bugs: airborne mashing spams jump sounds, and players in hit-stun fire phantom punch whooshes. Furthermore, hardcoding audio to "P1" or "Goku" prevents character swapping and breaks CPU AI / replay systems.

### The Solution: State-Driven, Fighter-General Audio Architecture
1. **Decoupled from Player Slots & Input:**
   - Audio is triggered **only when the character's state machine enters a state** (`TransitionTo(newState)`).
   - Works identically whether the fighter is driven by P1 keyboard, P2 controller, CPU AI, or network replay.
2. **Character-Specific Audio Profiles:**
   - [`FighterAudio.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs) is a modular component attached to each fighter prefab (`Goku.prefab`, `Saitama.prefab`).
   - Every fighter has individual, serialized `AudioClip` slots visible in the Inspector:
     - **Agility & Movement:** `crouchClip` (`swish_05.wav`), `jumpClip` (`sfx_jump.wav`), `landClip` (`jumpland_clean.wav`), `dashClip` (`sfx_dash.wav`)
     - **Attack Whiffs:** `lightWhiffClip` (`swish_01.wav`), `heavyWhiffClip` (`swish_04.wav`)
     - **Hit Impacts:** `lightHitClip` (`arcade_hit_01.ogg`), `heavyHitClip` (`arcade_hit_04.ogg` / `saitama_normal_punch.mp3`)
     - **Special Attacks & Hits (Blanked until Custom VFX):** `skillAttackClip`, `ultimateAttackClip`, `skillHitClip`, `ultimateHitClip` (`None`)
     - **Energy & Exhaustion:** `kiChargeLoopClip` (`dbz_super_saiyan_aura.mp3` at 60% volume / `0.45`), `redZoneWarningClip` (`sfx_redzone_warning.wav`)
     - **Signature Voice Lines & Dialogue (Dialogue Specials Only):**
       - **Goku Ultimate Finisher (`voiceUltimateClip`):** `goku_voice_kamehameha.wav` ($3.36\text{s}$ at $1.25\times$ speed, trimmed dead-air tail).
       - **Saitama Special Skill (`voiceSkillClip`):** `saitama_voice_consecutive_punches.wav` ($2.20\text{s}$, *"Renzoku Futsuu no Panchi"*).
       - **Saitama Ultimate Finisher (`voiceUltimateClip`):** `saitama_voice_serious_punch.wav` ($3.17\text{s}$ at $1.20\times$ speed, *"Serious Series: Serious Punch"*).
       - Basic attacks and reactions strictly use pure martial arts sound effects with zero voice clutter.
3. **Character Customization (Goku vs Saitama):**
   - **Goku:** Authentic DBZ Super Saiyan Ki charging aura loop (`dbz_super_saiyan_aura.mp3`), crisp crisp arcade martial arts punch impacts (`arcade_hit_01.ogg`), clean boots landing impact (`jumpland_clean.wav`), and heavy arcade impact (`arcade_hit_04.ogg`).
   - **Saitama:** Real Saitama punch hit impact (`saitama_normal_punch.mp3`), clean boots landing impact (`jumpland_clean.wav`), stance crouch sound (`swish_05.wav`), and Ki aura loop (`dbz_super_saiyan_aura.mp3`).

### Multi-Tier Hit Impact Scaling (`HitboxController.cs`)
Inside [`HitboxController.cs.ResolveHit()`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/HitboxController.cs#L240-L270), the impact audio dynamically scales with damage:
- **$\text{Damage} \ge 40$ (Ultimate):** Plays `ultimateHitClip` (or falls back if unassigned).
- **$20 \le \text{Damage} < 40$ (Skill):** Plays `skillHitClip` (or falls back if unassigned).
- **$15 \le \text{Damage} < 20$ (Heavy Attack):** Plays `heavyHitClip` (e.g., `saitama_normal_punch.mp3` or `arcade_hit_04.ogg`).
- **$\text{Damage} < 15$ (Light Attack):** Plays `lightHitClip` (`arcade_hit_01.ogg`).

### Curated SFX Library (`Assets/Audio/SFX_Library/`)
Over 50 audio clips are organized into four dedicated folders for in-Editor auditioning:
1. `Assets/Audio/SFX_Library/Hits_Punches/`: 18 distinct arcade hits (`impact_hit_01.ogg` through `18.ogg`) + real martial arts light punch (`light_punch_real.mp3`) + Saitama normal punch impact (`saitama_normal_punch.mp3`).
2. `Assets/Audio/SFX_Library/Swishes_Whiffs/`: 13 martial arts air displacements (`swish_01.wav` through `13.wav`).
3. `Assets/Audio/SFX_Library/Anime_Powers/`: Authentic DBZ Super Saiyan aura loop (`dbz_super_saiyan_aura.mp3`), clean boots landing (`jumpland_clean.wav`), DBZ teleport dash (`dbz_teleport_dash.mp3`), and iconic anime dialogue quotes (`goku_voice_kamehameha.mp3`, `goku_voice_kaioken.mp3`, `saitama_voice_serious_punch.mp3`).
4. `Assets/Audio/SFX_Library/Arcade_Bells/`: Tournament match start gongs and KO bells (`gong_01.ogg`, `gong_02.ogg`, `bell_01.ogg`â€“`03.ogg`).

Any clip can be auditioned via the Inspector's built-in waveform player and drag-and-dropped into any fighter prefab without touching code.

---

## 17. Round Sequencing & Bell Pacing Architecture (`MatchAudioController.cs`)

### The Problem
In standard video game implementations, music and sound effects often start simultaneously. If the battle soundtrack and the round opening bell play at frame 0, the bell is masked and drowned out by guitars and drums. Furthermore, if music plays during round defeat, the emotional weight of a K.O. is completely lost.

### The Solution: Paced Round Audio Flow
1. **Round 1 Start:**
   - The round bell strikes at maximum volume (`bellVolume = 1.0`) into complete silence.
   - An asynchronous delay coroutine waits $0.85\text{s}$ for the bell's metallic resonance to echo.
   - The battle music track (`musicSource`) then starts playing fresh from `0:00`.
   - **Bug Fix:** Removed premature `PlayBattleMusic()` in `Start()` that previously generated a 1-second pre-bell music blip.
2. **Round Defeat (K.O.):**
   - On `OnRoundEnd`, `musicSource.Stop()` immediately terminates combat music.
   - The heavy tournament K.O. gong reverberates alone over total silence.
3. **Round Reset & Round 2 Start:**
   - Fighters reset to their neutral starting positions.
   - The round bell rings into silence.
   - After the $0.85\text{s}$ delay, battle music restarts cleanly from the very beginning (`0:00`).






---

## 18. Authentic 90s Character Select Menu (`CharacterSelectController.cs`)

### Overview & Philosophy
The Character Select Menu represents the pivotal transition between the attract mode / title marquee and high-stakes combat. Rather than relying on static generic buttons or jarring scene reloads, Mystic Combat implements a **seamless in-canvas screen replacement** with dynamic JSON data binding, retro neon aesthetics, and versatile input handling.

### Visual & Interactive Elements
1. **Header & Dynamic Step Prompts:**
   - Bold arcade title banner: `CHOOSE YOUR FIGHTER`.
   - Dynamic prompt guiding the player through the flow:
     - Phase 1: `â–¶ PLAYER 1: SELECT YOUR HERO â—€` (Electric Cyan `#00F5FF`)
     - Phase 2 (Local PvP): `â–¶ PLAYER 2: SELECT YOUR HERO â—€` (Fiery Crimson `#FF3B30`)
     - Phase 2 (Vs CPU): `â–¶ CHOOSE CPU OPPONENT  or  PRESS [R] FOR RANDOM â—€` (Solar Gold `#FFD700`)
     - Phase 3 (Lock In): `âš¡ GET READY FOR BATTLE! âš¡` with dramatic `VERSUS!` full-screen overlay.
2. **Heroic Character Cards (Goku & Saitama):**
   - **Heroic Avatar Portraits:** High-resolution bust art (`goku_avatar_heroic.png` & `saitama_avatar_heroic.png`) set inside authentic arcade bevel frames.
   - **Hero Epithets & Titles:**
     - Goku: **`HOPE OF THE UNIVERSE`** in bold 21pt Radiant Sun Gold (`#FFE040`) with dark drop shadow.
     - Saitama: **`HERO FOR FUN`** in bold 21pt Radiant Sun Gold.
   - **Dynamic Grimoire Stats (Enlarged 19-20pt High-Visibility Palette):** Real-time data read from `StreamingAssets/Characters/*.json`:
     - Health & Speed: `HP: 100  |  SPD: 5.0 (FAST)` (20pt Neon Emerald `#40FF70`)
     - Normal Attacks: `ATTACK: Light 8  |  Heavy 15` (20pt Blazing Amber `#FFA834`)
     - Special Skill: `SKILL: Kamehameha [Ki: 20]` (19pt Electric Aqua `#00F5FF`)
     - Ultimate Ability: `ULTIMATE: Spirit Bomb [Ki: 80]` (19pt Crimson Flame `#FF4D4D`)
   - **Floating Selection Indicators:**
     - Player 1 badge `[ PLAYER 1 ]` in Electric Cyan with real-time breathing luminescence.
     - Player 2 badge `[ PLAYER 2 ]` or `[ CPU ]` in Fiery Crimson or Gold.
     - Scale pop ($1.03\times$) and glowing border highlighting the active selection.
3. **Versatile Control Schemes:**
   - **Keyboard & Gamepad:**
     - Navigation: `A / D` or Left / Right Arrows (or `J / L` for P2).
     - Confirmation: `Enter`, `Space`, `Keypad Enter`, `F` (P1 Light Attack), or `H` (P2 Light Attack).
     - Quick Random CPU: `[R]` key immediately rolls a random opponent and triggers match start.
     - Cancellation / Back: `[Escape]` or `[Backspace]` returns from P2 selection to P1, or from P1 back to Title Screen without cutting music.
   - **Mouse & Touch Support:**
     - Clicking any card selects or confirms that hero.
     - Interactive footer buttons: `Â« BACK (ESC)`, `âš¡ RANDOM CPU (R)`, and `LOCK IN (ENTER) Â»`.
4. **Mirror Match System (P1 vs P2 Same Fighter):**
   - Full support for Goku vs Goku and Saitama vs Saitama duels.
   - Avatars remain 100% original full-color portraits in all screens (no portrait tinting).
   - In-arena mirror matches maintain distinct identity without compromising heroic portrait fidelity.
5. **Cinematic "VS" Screen with Sliding Avatar Frames:**
   - Upon final confirmation, triggers a dedicated $2.35\text{s}$ pre-match cinematic clash:
     - Player 1's avatar card rockets in from far off-screen left (`X = -1500` âž” `-420`) with smooth quintic ease-out deceleration.
     - Player 2 / CPU's avatar card rockets in from far off-screen right (`X = +1500` âž” `+420`) simultaneously.
     - A giant 96pt neon **`VS`** drops into the center with heavy impact strike audio.
     - Screen holds for $1.8\text{s}$ allowing players to absorb the clash before seamlessly launching the battle arena.
   - Title music stops cleanly right as the combat arena loads, eliminating audio bleed.

---

## 19. Saitama Signature Audio Integration & Multi-Tier Audio Pipeline

### Overview & Vocal Delivery
Saitama's character in *One-Punch Man* is iconic for his contrasting attitudes: casual nonchalance versus earth-shattering seriousness. To bring this dynamism into combat, custom vocal audio files were integrated for his signature moves:
- **Skill Attack (oiceSkillClip):** consecutive_normal_punches.mp3 triggers when activating his 20-Ki multi-strike flurry ([U]).
- **Ultimate Finisher (oiceUltimateClip):** serious_punch.mp3 delivers the devastating full vocal scream when triggering his 100-Ki KO punch ([O]).

### Multi-Tier Fallback Loading Architecture (EnsureClipsLoaded)
Because fighters can be instantiated through multiple pathways (pre-baked in SampleScene.unity, saved as Prefab assets, or dynamically spawned via PrototypeBootstrap.cs), relying solely on Inspector serialization risks null references during dynamic runtime instantiation.

To solve this, [FighterAudio.cs](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs) implements a robust multi-tier asset resolution pipeline:
1. **Tier 1 (Serialized Prefab Wiring):** [SceneSetupTool.cs](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/SceneSetupTool.cs) binds consecutive_normal_punches.mp3 and serious_punch.mp3 directly into Saitama.prefab via AssetDatabase.LoadAssetAtPath.
2. **Tier 2 (Runtime Resource Auto-Fallback):** In FighterAudio.Awake(), EnsureClipsLoaded() evaluates whether oiceSkillClip or oiceUltimateClip are null. If null, it dynamically loads them from Resources/Audio/Anime_Powers/ using Unity's runtime Resources.Load<AudioClip>().
3. **Tier 3 (Runtime Bootstrap Synchronization):** In [PrototypeBootstrap.cs](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs), when fighters are spawned from the Character Select menu, a.EnsureClipsLoaded() is invoked immediately after component instantiation.

### Project Repository Hygiene
To prevent asset bloat:
- The temporary ingestion folder Assets/Audio/processing/ and its .meta file were deleted.
- Deprecated uncompressed .wav voice iterations were cleaned up, reducing repository storage footprint by over $1.5\text{MB}$.

---

## 20. Anime Kinetic Silhouette & High-Contrast Punch Flurry Architecture

### Overview
In 2D fighting games, multi-hit special attacks (such as E. Honda's *Hundred Hand Slap*, Chun-Li's *Hyakuretsukyaku*, or Saitama's *Consecutive Normal Punches*) require distinct visual framing. Because the attack consists of rapid cycling across 12 animation frames at 10 FPS, inadequate silhouette contrast causes the barrage to blur into an amorphous blob or blend into dark background tiles.

### Contrast Analysis & The Luminance Trap
1. **Luminance Comparison:**
   - Original Perimeter Outline: `#140505` (luminance $9.5$).
   - Stage Spotlight (Center): `#FBA782` (luminance $190.0$). Contrast Ratio $\approx 20:1$ (Visible).
   - Stage Floor/Backdrop Tiles (Sides): `#1A1F25` / `#141937` (luminance $26\text{–}30$). Contrast Ratio $< 1.3:1$ (**Blending Failure!**).
   - Goku's Hair & Gi Shadows: `#000000` / `#00009C` (luminance $0.0$). Contrast Ratio $\approx 1:1$ (**Total Outline Disappearance!**).
2. **Missing Outlines:** 171 perimeter punch pixels across frames 0, 2, 3, 4, 6, and 7 directly bordered transparency without an outline stroke.

### Dual-Contour Cel-Shading Enhancement
1. **Solid 1px Perimeter Inking (`#080404`):**
   - Sealed all 171 unshielded perimeter gaps with a 1px solid dark boundary.
   - Deepened outer boundary pixels so silhouettes maintain 100% opacity against any background tile.
2. **Kinetic Knuckle Impact Rim Lighting:**
   - **Strike Face (Front):** Blazing impact white (`#FFFFFF`, luminance $255.0$).
   - **Kinetic Transition (1px behind):** Solar anime gold highlight (`#FFD76E`, luminance $215.0$).
   - **Warm Body Blend (2px behind):** Energetic red-orange (`#FF5A32`, luminance $134.8$).
3. **Safety & Zero-Destruction Architecture:**
   - 100% of Saitama's body, cape, suit, face, and belt pixels are strictly preserved with 0 differences.
   - A bit-for-bit pristine backup is maintained in `Assets/Sprites/Fighters/Saitama/Original_Skill_Backup/` with a turnkey restoration script (`revert_skill_frames.py`) enabling instant 1-second reversion at any time.

---

## 21. In-Combat Pause Menu & Post-Match Victory/Rematch System (`CombatMenuController.cs`)

### Overview
To transition Mystic Combat from a combat prototype into a complete arcade fighting game loop, we introduced two dedicated modal systems in `SampleScene`:
1. **The In-Combat Pause Menu (`Escape` / `P`):** Freezes the match at any instant, halts character input buffering, and presents 4 clean navigation choices:
   - **`RESUME`**: Unfreezes game physics and restores battle audio.
   - **`RESTART MATCH`**: Restarts the match from Round 1 with full health and zeroed scores.
   - **`CHARACTER SELECT`**: Directly loads `MainMenuScene` with state `GameState.CharacterSelect`, immediately opening Hero Selection.
   - **`MAIN MENU`**: Exits to the title marquee.
2. **The Post-Match Victory & Rematch Screen:** Automatically fires after a tournament winner wins 2 rounds (`OnMatchOver`), celebrating the victory with:
   - **`★ TOURNAMENT CHAMPION ★`** Radiant Gold header banner.
   - Winner Heroic Avatar bust portrait (`goku_avatar_heroic.png` or `saitama_avatar_heroic.png`).
   - Winner Declaration (`GOKU WINS!` / `SAITAMA WINS!`) and Hero Epithet (*Hope of the Universe* / *Hero for Fun*).
   - Match Final Score (`FINAL SCORE: 2 - 1`).
   - Three immediate action buttons: **`⚡ REMATCH`**, **`👥 CHARACTER SELECT`**, and **`🏠 MAIN MENU`**.

### Navigation & Input Ergonomics
- **Dual Input Architecture:** Supports keyboard navigation (`W/S`, `Up/Down`, `Enter`, `Space`, `Escape`) and mouse interaction (pointer hover highlights and pointer clicks).
- **Procedural 8-Bit/16-Bit Audio:** Synthesizes dedicated arcade sound clips via `AudioClip.Create` for navigation blips (580 Hz), affirmative select chimes (880-1200 Hz sweep), pause tones, and victory fanfare.
- **Audio Bus Decoupling:** `CombatMenuController` sets `menuAudioSource.ignoreListenerPause = true;`. When `AudioListener.pause = true;` freezes looping combat Ki auras, UI feedback continues to play cleanly.

### Pacing, Presentation & Input Lifecycle Refinements
1. **Match-Lifecycle Input Gating:** `PlayerInputHandler` evaluates `MatchManager.CurrentMatchState == RoundInProgress`. Before the round opening bell (`WaitingToStart`), during the K.O. strike pause (`RoundEnd`), and after the match ends (`MatchOver`), all player movement and attack vectors are zeroed out.
2. **3.0-Second Dramatic Pacing:** The post-match victory screen wait is tuned to a full $3.0\text{s}$ realtime window, allowing the tournament K.O. gong to echo, the defeated fighter to hit the mat, and the victor's stance to sink in before UI elements enter.
3. **Cubic Ease-Out Slide Entrance:** The champion modal card dynamically animates from off-screen ($Y = +950\text{px}$) down to screen center ($Y = 0\text{px}$) over $0.65\text{s}$ using pure unscaled cubic interpolation: $y(t) = \text{Lerp}(950, 0, 1 - (1 - t)^3)$.
4. **Vibrant World Preservation (No Dark Scrim):** The full-screen background scrim is configured as transparent (`Color.clear`), keeping the arena stage, spotlights, and fighters fully visible and bright behind the modal card.
5. **Regal World Championship Styling:**
   - Ornate double gold & cyan borders with ruby corner studs.
   - Dual micro-tag: `★ ANIMEVERSE TOURNAMENT CHAMPIONSHIP ★` and `★ WORLD CHAMPION ★`.
   - Centered 114x114px gold medal avatar frame showcasing the victor's bust portrait.
   - Distinct color-coded buttons: Solar Gold (`REMATCH`), Electric Cyan (`CHANGE HEROES`), and Ruby Crimson (`MAIN MENU`).

---

## 22. Single-Player CPU Bot & Hierarchical FSM AI (`CpuFighterBrain.cs`, `GokuBrain.cs`, `SaitamaBrain.cs`, `CpuBrainProfile.cs`)

### Overview
To transform Mystic Combat into a complete single-player arcade title, we introduced a fully autonomous, anime-accurate CPU opponent. The AI is built on a **Hierarchical Finite State Machine (HFSM)** that interfaces with the character entity via a virtual controller contract (`IFighterInput`), guaranteeing 100% fair play without game-rule cheats or unnatural frame-0 teleports.

### Macro Strategy Layer (Tactical Postures)
The CPU continuously perceives the battlefield through sensory queries:
- $\text{Distance} = |x_{\text{self}} - x_{\text{opp}}|$
- Direction to opponent: $\text{Sign}(x_{\text{opp}} - x_{\text{self}})$
- Current Health percentages ($\text{HP}_{\text{self}} / \text{MaxHP}$ vs $\text{HP}_{\text{opp}} / \text{MaxHP}$)
- Ki reserves ($\text{Ki}_{\text{self}}$ vs $\text{Ki}_{\text{opp}}$)
- Exhaustion states ($\text{IsInRedZone}$)
- Opponent animation states ($\text{IsOpponentInRecovery}$, $\text{IsOpponentMeditating}$)

The AI transitions dynamically between 5 macro postures:
1. **`SpacingNeutral`:** Maintains optimal footsies spacing, weaving forward and backward to bait human whiffs while looking for poke openings.
2. **`AggressiveRushdown`:** Sprints into close melee range ($< 1.0\text{m}$) to chain attack combos. Fired whenever the opponent is exhausted in the Red Zone, under low HP, or meditating.
3. **`KiHoarding`:** Disengages to safe distance ($> 3.0\text{m}$) and channels Meditation (`MeditateHeld = true`) to charge energy or accelerate Red Zone recovery.
4. **`DefensiveRetreat`:** Backpedals and uses evasive hops when low on health or suffering from Red Zone exhaustion.
5. **`PunishOpening`:** Detects when an opponent misses a high-recovery move (Heavy/Ultimate) and immediately commits to a counter-strike.

### Micro Action Execution & Virtual Input Pipeline
- **Continuous Inputs:** `MoveInput` (Vector2), `MeditateHeld` (bool), and `CrouchHeld` (bool) remain active as long as the macro state demands them.
- **Single-Frame Button Pulses:** Attack buttons (`LightAttackPressed`, `HeavyAttackPressed`, `SkillPressed`, `UltimatePressed`) and `JumpPressed` are pulsed for a single frame and automatically reset in `LateUpdate()`. This ensures `FighterController` detects them identically to human hardware key presses (`wasPressedThisFrame`).

### Character-Specific Combat Personalities
1. **Goku (`GokuBrain.cs`):**
   - **Style:** Disciplined martial artist & mid-range zoner.
   - **Spacing:** Maintains $\approx 2.1\text{m}$ distance.
   - **Special Move:** Fires *Kamehameha* (`[E]`) from mid-distance ($1.4\text{m} - 2.7\text{m}$) when $\text{Ki} \ge 20$.
   - **Ki Target:** Hoards $80\text{ Ki}$ for his Ultimate blast.
2. **Saitama (`SaitamaBrain.cs`):**
   - **Style:** Relentless rushdown brawler.
   - **Spacing:** Point-blank ($< 1.0\text{m}$); never retreats in neutral.
   - **Special Move:** Unloads *Consecutive Normal Punches* (`[U]`) in close quarters.
   - **Ki Target:** Hoards the entire $100\text{ Ki}$ bar to march up and drop the lethal *Serious Punch* (`[O]`).

### Configurable Difficulty Knobs (`CpuBrainProfile.cs`)
- **`reactionDelay`:** Easy ($0.40\text{s}$), Hard ($0.14\text{s}$), Extreme ($0.08\text{s}$).
- **`mistakeChance`:** Easy ($35\%$), Hard ($8\%$), Extreme ($2\%$).
- **`comboChance`:** Easy ($30\%$), Hard ($85\%$), Extreme ($95\%$).
- **`healthMultiplier` & `damageMultiplier`:** Scaled to $1.4\times$ and $1.25\times$ on Extreme difficulty for tournament boss battles.
- **Character Select Integration:** Players can press `[TAB]` on the Character Select Screen when in `Vs CPU` mode to cycle dynamically between `[ CPU: EASY ]`, `[ CPU: HARD ]`, and `[ CPU: EXTREME ]`.




