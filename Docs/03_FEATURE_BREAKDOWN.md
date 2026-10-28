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
1. **The In-Combat Pause Menu (`Escape`):** Freezes the match at any instant, halts character input buffering, and presents 4 clean navigation choices:
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
1. **`RushdownPressure` (Default):** Advances with intent and delivers relentless close-range pressure whenever within $1.55\text{m}$. Opens with Light jabs to bait reactions and confirm into combo finishers.
2. **`PunishOpening`:** Detects when an opponent whiffs a heavy attack or ultimate, or enters `HitStun` or `Exhausted` (Red Zone). Closes in instantly to punish with an Ultimate blast, Skill flurry, or heavy haymaker.
3. **`SpacingNeutral`:** Engages in dynamic footsies around $1.45\text{m} - 1.65\text{m}$. If the player steps into strike range, the bot counter-attacks immediately instead of retreating.
4. **`KiCharge`:** Disengages to safe distance ($> 3.0\text{m}$) and channels Meditation (`MeditateHeld = true`) to charge energy. The moment the player approaches within $2.4\text{m}$, meditation is cancelled instantly to defend.
5. **`ExhaustionRecovery`:** Triggered during Red Zone exhaustion. Utilizes accelerated meditation ($1.5\times$ speedup) at safe distance or defensive jumps/crouches when cornered.

### Micro Action Execution & Frame-Latched Virtual Input Pipeline
- **Continuous Inputs:** `MoveInput` (Vector2), `MeditateHeld` (bool), and `CrouchHeld` (bool) remain active as long as the tactical state demands them.
- **2-Frame Latched Button Triggers:** Attack buttons (`LightAttackPressed`, `HeavyAttackPressed`, `SkillPressed`, `UltimatePressed`) and `JumpPressed` are latched across 2 frames. This eliminates the race condition where `LateUpdate()` cleared button pulses before `FighterController.Update()` evaluated them, guaranteeing 100% actuation reliability regardless of Unity script execution ordering.
- **Hit-Confirm Chain-Canceling Engine:** The AI monitors `myController.HasHitTargetThisSwing` on every frame during `LightAttack`. If a hit lands, the bot cancels recovery directly into a Heavy Haymaker or special Skill based on its `comboChance`.

### Character-Specific Combat Personalities
1. **Goku (`GokuBrain.cs`):**
   - **Style:** Disciplined martial artist, Super Saiyan powerhouse & beam zoner.
   - **Spacing:** $1.65\text{m}$ melee range; full-screen beam sniping.
   - **Super Saiyan Ascension:** The instant Goku reaches $35\text{ Ki}$, he triggers `Skill` to ascend to Super Saiyan form ($+25\%$ speed, $+66\%$ light damage, $+43\%$ heavy damage).
   - **Special Move:** Snipes with full-screen *Kamehameha* beam (Ultimate / $80\text{ Ki}$ / $45\text{ dmg}$) from up to $6.5\text{m}$ away on vulnerable opponents.
2. **Saitama (`SaitamaBrain.cs`):**
   - **Style:** Relentless in-your-face rushdown brawler.
   - **Spacing:** Tight brawling ($1.30\text{m}$); refuses to back away in neutral.
   - **Visceral Combo Chain:** Light Jab ($4\text{ dmg}$) $\to$ Heavy Haymaker ($9\text{ dmg}$) $\to$ *Consecutive Normal Punches* ($15\text{ hits} / 30\text{ dmg}$).
   - **Serious Punch Finisher:** Hoards $80\text{ Ki}$ to march up and drop the round-ending *Serious Punch* ($90\text{ damage}$) with full cinematic camera shake and impact flash.

### Configurable Difficulty Knobs (`CpuBrainProfile.cs`)
- **`reactionDelay`:** Easy ($0.22\text{s}$), Hard ($0.10\text{s}$), Extreme ($0.04\text{s}$).
- **`mistakeChance`:** Easy ($15\%$), Hard ($4\%$), Extreme ($0\%$).
- **`comboChance`:** Easy ($60\%$), Hard ($88\%$), Extreme ($98\%$).
- **`healthMultiplier` & `damageMultiplier`:** Scaled to $1.35\times$ and $1.25\times$ on Extreme difficulty for tournament boss battles.
- **Character Select Integration:** Players can press `[TAB]` on the Character Select Screen when in `Vs CPU` mode to cycle dynamically between `[ CPU: EASY ]`, `[ CPU: HARD ]`, and `[ CPU: EXTREME ]`.

---

## 23. Runtime Fighter Deployment Pipeline (`PrototypeBootstrap.cs`)

### Overview
To enable free character selection across all combinations without baking characters into `SampleScene.unity`, [`PrototypeBootstrap.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs) functions as the **Runtime Fighter Deployer**.

### Execution Pipeline (Scene Load):
1. **Scene Sweep:** Detects and immediately flags for destruction any static or legacy fighters left in the scene (`Fighter_P1*`, `Fighter_P2*`), setting them inactive instantly to prevent lifecycle race conditions.
2. **Session Identification:** Reads `P1SelectedCharacterId` and `P2SelectedCharacterId` from `GameManager.Instance`.
3. **Resource Prefab Instantiation:**
   - Calls `Resources.Load<GameObject>("Prefabs/Fighters/" + charName)`.
   - Spawns Player 1 at $(-2.2, -3.0, 0)$ as `Fighter_P1`.
   - Spawns Player 2 at $(+2.2, -3.0, 0)$ as `Fighter_P2`.
4. **Input Decoupling Injection:**
   - `Fighter_P1`: Injected with `PlayerInputHandler` (`PlayerIndex.P1`).
   - `Fighter_P2`:
     - If `CurrentGameMode == GameMode.VsCpu`: Removes human input and attaches the character-specific brain (`GokuBrain` or `SaitamaBrain`), configured to `CurrentCpuDifficulty`.
     - If `CurrentGameMode == GameMode.LocalPvP`: Injected with `PlayerInputHandler` (`PlayerIndex.P2`).
5. **Opponent Auto-Facing:** Cross-wires `fc1.opponent = fc2.transform` and `fc2.opponent = fc1.transform`.
6. **Direct Reference Injection:** Calls `MatchManager.SetFighterReferences()` and `HUDController.SetFighterReferences()`, binding health and Ki systems directly and eliminating all brittle `GameObject.Find()` calls.
7. **Mirror Match Resolution:** If `p1Char == p2Char`, Player 2 receives an alternate palette tint on its `SpriteRenderer` (Cool Cyan for Goku or Solar Amber for Saitama).

---

## 24. Interactive Options & Controls Modal, Combined Local Duel Table & Audio Mixer

### Overview & Philosophy
The **Options & Controls** screen transforms the previously static placeholder into a full-featured, interactive configuration hub for sound mixing and dynamic key rebinding. It solves the classic PC local multiplayer dilemma by cleanly separating **Single Player** input freedom from **1v1 Local Duel** shared keyboard management.

### Feature Specifications
1. **Direct Top Audio Sliders:**
   - Positioned prominently across the top of the modal window for immediate access without switching tabs.
   - **Music BGM Slider:** Ranges from $0\%$ to $100\%$, updating title music and match BGM volume live as the slider is dragged.
   - **SFX Sound Slider:** Adjusts hit, attack, and UI impact audio volume.
   - Live percentage readouts (`70%`, `100%`) update dynamically on every value change.
2. **Profile Selection Tabs:**
   - `[ 👤 SINGLE PLAYER (Vs CPU & Future LAN) ]`: Active tab highlighted with glowing gold border and navy fill.
   - `[ ⚔️ 1v1 LOCAL DUEL (Shared Keyboard) ]`: Inactive tab displayed in muted slate styling.
3. **Single Player Profile (9 Customizable Actions):**
   - Configured for solo play against the CPU and future LAN networked gameplay where each player has their own machine.
   - Full keyboard freedom across all 9 actions: `Move Left`, `Move Right`, `Jump`, `Crouch / Guard`, `Light Attack`, `Heavy Attack`, `Special Skill`, `Ultimate Move`, `Ki Meditate`.
   - Canonical Defaults: `WASD` for movement, `F` (Light), `C` (Heavy), `E` (Skill), `Q` (Ultimate), `X` (Meditate).
4. **1v1 Local Duel Combined Table (18 Customizable Keys):**
   - Designed specifically for two human players sharing a single keyboard on one PC.
   - **3-Column Unified Layout:**
     `[ FIGHTER ACTION ] | [ PLAYER 1 (LEFT SIDE) ] | [ PLAYER 2 (RIGHT SIDE) ]`
   - Keeps both players' mappings side-by-side on 9 rows, allowing instant comparison and preventing key collisions.
   - Canonical Defaults:
     - Player 1 (Left Half): `A` / `D` / `W` / `S` (Movement) + `F` (Light), `C` (Heavy), `E` (Skill), `Q` (Ult), `X` (Ki).
     - Player 2 (Right Half): `J` / `L` / `I` / `K` (Movement) + `H` (Light), `N` (Heavy), `U` (Skill), `O` (Ult), `M` (Ki).
5. **Interactive Key Rebinding Mechanism:**
   - Clicking any button sets that slot into **Listening Mode**: button text changes to `[ PRESS KEY ]` with an animated gold/white pulse.
   - In `Update()`, `OptionsMenuController` scans `Keyboard.current.allKeys` for `wasPressedThisFrame`.
   - The first pressed key is bound to the slot, formatted cleanly (e.g. arrow keys render as `←`, `→`, `↑`, `↓`), and written to `PlayerPrefs`.
   - Pressing `[Escape]` cancels rebinding cleanly without modifying the existing key.
6. **Reset to Defaults & Persistence:**
   - **`[ ↺ RESET TO DEFAULTS ]`**: Reverts volume sliders to $70\% / 100\%$ and restores all 27 key assignments (9 SP + 18 Duel) to their original factory defaults.
   - **`[ ✔ SAVE & CLOSE (ESC) ]`**: Commits all modified keys and volume values to disk via `PlayerPrefs.Save()` and dismisses the modal.

---

## 25. Large-Scale High-Visibility Typography & Arcade Navigation Hierarchy

### Overview & Visual Philosophy
To ensure maximum readability across varying monitor distances and resolutions, the Controls & Options architecture was redesigned with an **$80\%$ font size scale-up** and a clean **3-screen hierarchy**.

### Key System Details
1. **Window Dimensions:** Expanded to $1440 \times 880\text{px}$ on standard 1080p reference canvas.
2. **Typography Scaling:**
   - Screen Title: $36\text{pt}$ Bold Arcade Gold (`#FFD54F`).
   - Screen Subtitles: $20\text{pt}$ Metallic Cyan (`#90CAF9`).
   - Volume Labels & Menu Buttons: $24\text{pt}\text{–}25\text{pt}$ Bold.
   - Action Titles: $23\text{pt}$ High-Contrast White.
   - Bound Key Buttons: $24\text{pt}$ Bold.
   - Volume Percentage Readouts: $26\text{pt}$ Bold.
3. **Screen Hierarchy Flow:**
   - **Main Hub:** Contains the Music & SFX sliders, the two sub-menu launcher buttons (`[ 👤 CONFIGURE SINGLE PLAYER KEYS » ]` and `[ ⚔️ CONFIGURE 1v1 LOCAL DUEL KEYS » ]`), and side-by-side bottom buttons.
   - **Single Player Sub-Screen:** Devotes the full window to 9 large action rows with ample spacing ($54\text{px}$ row height).
   - **1v1 Local Duel Sub-Screen:** Devotes the full $1440\text{px}$ window to the 3-column combined table (`ACTION` | `PLAYER 1` | `PLAYER 2`), allowing side-by-side comparison with zero visual crowding.
4. **Obvious Arcade Navigation System:**
   - **`►` Cursor Prefixing:** Automatically attaches to the focused element's label text (`►  MUSIC BGM`, `►  MOVE LEFT`).
   - **Pulsing Neon Gold Outline:** Dynamically blends between Solar Gold and Pure White using `Mathf.Sin(Time.unscaledTime * 7f)`.
   - **Dual-Axis Input:**
     - Vertical (`W/S` or `Up/Down`): Moves item focus with a $0.20\text{s}$ repeat gate.
     - Horizontal (`A/D` or `Left/Right`): Nudges sliders $\pm 5\%$ in Main Hub, or toggles between P1 and P2 columns in the Local Duel table.
     - Action (`Enter/Space`): Activates buttons or initiates key listening mode.
     - Back/Cancel (`Escape`): Cancels active rebind, returns to Main Hub from sub-screens, or saves and exits to Title from the Main Hub.
   - **Mouse Integration:** Hovering moves the cursor and outline immediately to that item; clicking triggers it directly.

---

## 26. Runtime Dynamic Character Deployer & Match Lifecycle Synchronization

### Overview
To completely eliminate hardcoded combatants from the stage and support true arbitrary fighter matchups (Goku vs. Saitama, Saitama vs. Goku, Goku vs. Goku mirror, Saitama vs. Saitama mirror), Mystic Combat employs a dedicated runtime deployment manager: [`PrototypeBootstrap.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/PrototypeBootstrap.cs).

### Core Deployer Responsibilities
1. **Prefab Resolution from Resources:**
   - Character prefabs are maintained in `Assets/Resources/Prefabs/Fighters/` (`Goku.prefab`, `Saitama.prefab`).
   - `DeployFighter()` dynamically instantiates the correct prefab based on `GameManager.Instance.P1SelectedCharacterId` and `P2SelectedCharacterId`.
2. **Decoupled Input Provider Assignment (`IFighterInput`):**
   - **Player 1:** Assigned `PlayerInputHandler` bound to Player 1 key mappings.
   - **Player 2 (Human vs. Human):** Assigned `PlayerInputHandler` bound to Player 2 key mappings.
   - **Player 2 (Human vs. CPU):** In `GameMode.VsCpu`, the deployer removes `PlayerInputHandler` and attaches the character-specific HFSM brain (`GokuBrain` or `SaitamaBrain`), configuring its reaction times, mistake chances, and stat multipliers via `GameManager.Instance.CurrentCpuDifficulty`.
3. **Mirror Match Alternate Visual Palette:**
   - If Player 1 and Player 2 select the same character (e.g. Goku vs. Goku), Player 2 is assigned an alternate anime color tint:
     - Goku P2 Alternate: Cool Cyan / Ice Gi (`#B2D9FF`).
     - Saitama P2 Alternate: Solar Amber Suit (`#FFD9B2`).
4. **Cross-Fighter Target Wiring:**
   - Automatically cross-links each fighter's `opponent` transform (`fc1.opponent = fc2.transform; fc2.opponent = fc1.transform;`) so characters naturally face each other and track spacing distances.
5. **Direct Reference Injection into Systems:**
   - Automatically pushes references into both `MatchManager.SetFighterReferences()` and `HUDController.SetFighterReferences()`, bypassing scene search queries.
6. **Scene Lifecycle Auto-Bootstrap:**
   - Hooks both `SceneManager.sceneLoaded` and `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`.
   - Uses `SceneManager.MoveGameObjectToScene` to guarantee new instances are parented to `SampleScene` regardless of transition timing.

---

## 27. Audio Mixing Tuning, Kamehameha Voice Calibration & Dynamic SFX Scaling

### Overview
To ensure an authentic, balanced retro arcade audio landscape, audio playback levels across the UI, combat arena, and fighter vocals were calibrated to prevent audio clipping, ear fatigue, and volume dominance.

### Key Adjustments Implemented:

1. **Default Background Music (BGM) Set to 100%:**
   - In [`InputSettingsManager.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Systems/InputSettingsManager.cs), updated `DEFAULT_MUSIC_VOL = 1.00f;`.
   - In `LoadAllSettings()`, incorporated an automatic migration routine: if stored volume in `PlayerPrefs` was the old default (`0.70f`) or uninitialized, it automatically upgrades to `1.00f`, granting maximum headroom immediately.
   - Updated serialized defaults in `MainMenuController.cs` and `MainMenuSetupTool.cs` (bake key `v16`).

2. **Pause & Victory Menu Navigation SFX Volume Halved:**
   - In [`CombatMenuController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/CombatMenuController.cs):
     - Reduced navigation sound volume from `0.45f` down to `0.22f` ($51\%$ volume drop) across keyboard inputs and mouse hover callbacks.
     - Implemented an amplitude decay envelope in procedural audio synthesis:
       `Mathf.Sign(Mathf.Sin(frequency * t)) * (1f - (t / 0.04f))`
       which eliminates the harsh high-frequency click on audio cutoffs.
     - Coupled playback to master `InputSettingsManager.SFXVolume`.

3. **Goku's Kamehameha Corrected as Ultimate (`Q`) & Volume Calibrated:**
   - In [`FighterAudio.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs) and [`SceneSetupTool.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Editor/SceneSetupTool.cs):
     - Goku has no special skill; his `voiceSkillClip` is `null`.
     - Goku's signature Ultimate (Ki: 80, `Q` / `O`) is **Kamehameha**, animated via `ultimate_0..9` and voiced via `voiceUltimateClip` (`goku_voice_kamehameha.wav`).
     - Calibrated Goku's `voiceUltimateMultiplier` down to `0.50f` to compensate for the extreme digital mastering gain of `goku_voice_kamehameha.wav` (Peak `0.906`, RMS `0.262`).
     - In `PlayVoiceSound()`, added a hard safety ceiling: `tunedVolume = Mathf.Min(tunedVolume, 0.50f);` whenever the Kamehameha clip triggers, preventing ear-piercing audio peaks.

4. **Saitama Skill & Ultimate Vocal Shouts Amplified:**
   - In [`FighterAudio.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterAudio.cs):
     - Increased Saitama's `voiceSkillMultiplier` to **`2.20f`** so "Consecutive Normal Punches" cuts through loud impact sounds.
     - Increased Saitama's `voiceUltimateMultiplier` to **`2.50f`** so "Serious Punch" rings out with climactic anime weight.

5. **Single-Pass Master Audio Bus Scaling:**
   - Kept one-shot audio sources unscaled (`volume = 1.0f`) and applied `InputSettingsManager.SFXVolume` as the final scale factor inside `PlaySound()` and `PlayVoiceSound()`, avoiding double-multiplication bugs.
   - Looping meditation aura scales via `kiAuraVolume * sfxVolume * InputSettingsManager.SFXVolume`.

---

## 28. Standardized Animation Baselines, Dynamic Hurtbox Sizing & Tactical AI Rhythm

### Overview
This feature update resolves visual animation bouncing, aligns physical combat colliders with on-screen actions, and upgrades CPU Bot combat behavior from random input spam into tactical arcade martial arts.

### 1. Goku 128x128 Animation Baseline Standardization
- **Root Problem:** While standard Goku frames were $128 \times 128\text{px}$, `skill_0..3` were sliced at $90 \times 79\text{px}$ with varying bottom padding ($14\text{–}20\text{px}$), causing Goku's sprite pivot to lift him $20\text{px}$ above the arena floor whenever Kamehameha fired.
- **Resolution:**
  - Standardized all 44 Goku animation frames (`crouch`, `idle`, `walk`, `punch`, `heavy`, `skill`, `ultimate`, `meditate`, `jump`) onto uniform **$128 \times 128\text{px}$** canvases.
  - Locked ground foot contact baseline to `y = 104` ($23\text{px}$ bottom padding) and stance center to $x = 64$.
  - In [`FighterAnimator.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterAnimator.cs), `pivot = new Vector2(0.5f, 23f / 128f)` now applies universally, completely eliminating visual bouncing.

### 2. Dynamic Hurtbox Sizing on Crouch
- In [`FighterController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs), when transitioning into `FighterState.Crouching`:
  - The `Hurtbox` `BoxCollider2D.size` dynamically scales down from $1.8\text{m}$ to $0.95\text{m}$, with `offset` lowered to $0.48\text{m}$.
  - Exiting crouch restores the standing $1.8\text{m}$ box.
  - This allows ducking fighters to physically evade high-altitude melee attacks.

### 3. Extended Kamehameha Beam Hitbox Reach
- Goku's Skill attack was expanded from a tiny $1.3\text{m}$ box into an authentic full-screen beam:
  `hitboxOffset = new Vector2(2.1f, 0.9f)`, `hitboxSize = new Vector2(3.6f, 0.85f)`.
- In [`HitboxController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/HitboxController.cs), `ConfigureAttack()` synchronizes both internal fields and the physical `hitboxCollider.offset` and `hitboxCollider.size`.
- The beam now cleanly strikes targets standing across the stage at mid/long distance ($0.3\text{m}\text{–}3.9\text{m}$).

### 4. Tactical HFSM & Humanized Cadence
- In [`CpuFighterBrain.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/AI/CpuFighterBrain.cs):
  - **Jump Cooldown Gate:** Added `jumpCooldownTimer` ($2.0\text{s}\text{–}3.2\text{s}$), stopping the 60 FPS pogo-sticking bug.
  - **Relative Footsies Spacing:** Neutral spacing oscillation now steps forward and back relative to the opponent (`dirToOpponent`), creating believable boxing footsies.
  - **Corner & Wall Awareness:** Near boundary walls ($|x| > 7.2\text{m}$), the bot refuses to walk into the wall; it either initiates an escape jump-over or fights out with aggressive rushdown.
  - **Attack Cadence:** Regulated by `attackCooldownTimer` ($0.25\text{s}\text{–}0.50\text{s}$), allowing natural attack pacing and combo hit-confirmation.

---

## 29. Combat BGM First-Round Self-Healing & Scene Guarding

### Overview
Ensures that the arena combat soundtrack (*Double Dragon 2 - Final Battle 2*) begins playing seamlessly from 0:00 on Round 1 under all scene-loading conditions, eliminating silent opening rounds.

### Key Enhancements Implemented
1. **Scene-Context Guarding:**
   - In [`MatchAudioController.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchAudioController.cs), replaced the brittle `CurrentState == GameState.MainMenu` flag check with `gameObject.scene.name == "MainMenuScene"`.
   - This prevents battle music from playing in the Main Menu, while ensuring that launching directly into `SampleScene` in the Editor never erroneously silences combat music.
2. **First-Round State Self-Healing:**
   - In `MatchAudioController.Start()`:
     ```csharp
     if (matchManager != null && matchManager.CurrentMatchState == MatchManager.MatchState.RoundInProgress)
     {
         if (musicSource != null && !musicSource.isPlaying)
         {
             PlayBattleMusic(resetToStart: true);
         }
     }
     ```
   - If `OnRoundStart(1)` fired before `MatchAudioController` finished its subscription during scene load, `Start()` immediately detects the in-progress round and kicks off the battle track with zero frame delay.
3. **Continuous Playback Failsafe:**
   - In `Update()`, if `matchManager.CurrentMatchState == RoundInProgress` and `!musicSource.isPlaying`, the controller initiates playback automatically.
4. **Explicit State Setting in MatchManager:**
   - In [`MatchManager.cs`](file:///b:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchManager.cs), `StartMatch()` explicitly sets `GameManager.Instance.SetState(GameState.InMatch)` to keep game state synchronized across the engine.

---

## 21. Super Saiyan Transformation Mode (Goku Special Skill)

### Feature Overview:
In *Mystic Combat: Animeverse Incursion*, Goku's special skill (`U` key / `E` key) is not a generic projectile — it is a mid-match transformation into **Super Saiyan**!

### Combat Parameters & Tuning:
- **Activation Cost:** 35 Ki (requires at least 35% of energy gauge).
- **Transformation Startup:** 0.65s roar pose with golden Ki aura activation (`dbz_super_saiyan_aura.mp3`).
- **Form Duration:** 15.0 seconds.
- **Combat Multipliers:**
  - **Movement Speed:** $+25\%$ linear velocity multiplier.
  - **Damage:** $+25\%$ damage output on all punches, kicks, and strikes.
  - **Visual:** Golden spiky hair and pulsing Ki aura.
- **Auto-Reversion:**
  - Automatically reverts to Base Goku when the 15-second timer expires.
  - Automatically reverts if a round ends (via `ResetForNewRound`).

---

## 30. Dramatic 4-Phase Slow-Mo Knockout & Staggered Victory Flow (`MatchManager.cs`, `FighterAnimator.cs`)

### Feature Overview:
In arcade fighting classics like *Street Fighter III: 3rd Strike*, *Guilty Gear*, and *Dragon Ball FighterZ*, the final blow that finishes a round is never cut off instantly. It is given dramatic, cinematic weight.

### The 4-Phase Sequence:
1. **Phase 1: Cinematic Slow-Motion Collapse (1.8s Real-Time)**
   - Defeated character immediately enters `FighterState.Knockout`.
   - Engine slows down to slow motion (`Time.timeScale = 0.28f`).
   - Impact flash (white overlay for $0.12\text{s}$) and camera shake ($0.40$ intensity).
   - Defeated fighter plays their full multi-frame knockdown animation (11 frames for Goku, 5 frames for Saitama) in slow motion as they fly back and collapse.
   - `FighterAnimator.cs` plays this using `unscaled: true` so the sprite frame rate remains fluid and uninterrupted over the 1.8-second slow-motion window.
2. **Phase 2: Floor Impact & Normalization**
   - The moment the character's body strikes the arena floor, `Time.timeScale` snaps back to $1.0\text{f}$ normal speed.
   - A ground dust puff and secondary impact camera shake trigger at the landing coordinates.
3. **Phase 3: Staggered Victorious Celebration (Single Turn)**
   - Winner transitions to `FighterState.Victory`.
   - Defeated character remains lying flat on the mat (pinned on their final knockout frame).
   - Victorious character performs their full celebratory animation once (thumbs up for Goku, 9-frame jump celebration for Saitama).
4. **Phase 4: Exact Single-Turn Duration Hold (`VictoryDuration`)**
   - The engine waits exactly for the character's single-turn victory animation to complete ($1.50\text{s}$ for Saitama, $1.00\text{s}$ for Goku) via `anim.VictoryDuration`.
   - The instant that turn finishes, `MatchManager` immediately initiates the next round (or advances to the match-over screen if tournament is clinched).

---

## 31. DBZ Spiky Flame Cocoon & Speed-Streak Ki Aura System (`FighterKiAura.cs`)

### Feature Overview:
Replaces generic soft particles with the authentic Dragon Ball Z Super Saiyan flame cocoon matching Akira Toriyama's manga and anime aesthetic.

### Visual Architecture:
1. **Jagged Spiky Flame Cocoon:**
   - Custom sprite texture (`Assets/Sprites/Effects/dbz_spiky_aura.png`) featuring 26 jagged upward flame spires and an intense white-hot energy core.
   - Fluctuates and flickers rapidly ($24\text{–}28\text{ Hz}$) using dual Perlin oscillation to simulate turbulent licking fire.
   - Pulsing alpha energy envelope ($0.80\text{–}0.98$).
2. **Clean Pure-Sprite Anime Cocoon (Zero Emitter Clutter):**
   - Particle streaks were removed to eliminate visual clutter and ensure hand-crafted pixel art fighters remain crisp and legible at all times.
   - Ground baseline rests cleanly at the fighter's feet without dipping below the floor plane.
3. **Dynamic Color Adaptation:**
   - **Super Saiyan Goku:** Radiant DBZ Gold (`Color(1.0f, 0.88f, 0.22f)`).
   - **Base Goku Meditation:** Electric Cyan (`Color(0.18f, 0.85f, 1.0f)`).
   - **Saitama Pressure:** Blazing Crimson (`Color(1.0f, 0.18f, 0.15f)`).

---

## 32. Kamehameha 16px Stray Rip Artifact Fix & Zero-Skate Foot Anchoring

### The Bug:
In `Frames_Raw/ultimate_1..3.png`, 6 stray pixels from the adjacent sprite on the original sprite sheet were ripped at $Y = 73..74$. Because the standard canvas tool aligned bounding-box bottoms to baseline $Y = 105$, these 6 pixels pushed Goku's actual feet (at $Y = 58$) up into the air by 16 pixels. During charge frames, Goku hopped into the sky and slammed down on frame 4. Furthermore, blind horizontal centering caused foot skating.

### The Fix:
1. Cropped all raw frames strictly at $Y < 60$, stripping the 16px bottom rip artifact completely.
2. Anchored the planted back foot to fixed canvas coordinates ($X = 41$, $Y = 105$ on a $128 \times 128$ canvas).
3. Zero vertical hopping, zero horizontal foot skating across all 10 Kamehameha frames (`ultimate_0..9`).

---

## 33. Super Saiyan State Restrictions & 199s Round Timer Pacing

### Super Saiyan Meditation Guard:
- In anime canon, Super Saiyan is an explosive, raging combat form; Goku cannot calmly meditate to charge Ki while ascended.
- In [`FighterController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs), meditation input is guarded: `if (inputHandler.MeditateHeld && IsGrounded && !isSuperSaiyan)`.
- If meditation is active during transformation, the fighter immediately cancels meditation and transitions to Idle.

### 199s Round Timer Pacing:
- Set `roundDuration = 199f` in `MatchManager.cs`. Matches have ample time for deliberate neutral spacing, footsies, and strategic Ki management without artificial rushing.


---

## 34. Tuned Combat Knockback & Saitama Flurry Speed Normalization

### 1. Knockback Rebalancing Across All Moves
To prevent fighters from rocketing across the 17-unit arena floor into invisible boundary walls, horizontal and vertical knockback velocities were balanced:

| Move | Character | Old Knockback (X, Y) | Tuned Knockback (X, Y) | Combat Feel / Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **Light Attack** | Both | $4.5\text{f},\ 3.2\text{f}$ | $\mathbf{2.2f},\ \mathbf{0.8f}$ | Fast, snappy jab; flinches opponent without launching them into air. |
| **Heavy Attack** | Both | $8.0\text{f},\ 3.2\text{f}$ | $\mathbf{5.0f},\ \mathbf{2.0f}$ | Crushing blow; creates solid spacing and wall stagger. |
| **Consecutive Punches** | Saitama (Skill) | $6.0\text{f},\ 3.2\text{f}$ (per hit!) | $\mathbf{1.2f},\ \mathbf{0.4f}$ | Traps the opponent in a sustained juggle barrage for all 15 punches. |
| **Kamehameha** | Goku (Ultimate) | $14.0\text{f},\ 3.2\text{f}$ | $\mathbf{8.5f},\ \mathbf{2.2f}$ | High-impact energy blast pushing opponent to arena edge. |
| **Serious Punch** | Saitama (Ultimate) | $24.0\text{f},\ 3.2\text{f}$ | $\mathbf{12.0f},\ \mathbf{3.0f}$ | Devastating screen-clearing knockout punch staying within bounds. |

### 2. Saitama Forward Speed in Consecutive Normal Punches
- **Previous Behavior:** Moved forward at `1.5f` velocity continuously for $2.70\text{s}$ ($4.05\text{ units}$ of distance), causing Saitama to skate past opponents.
- **Tuned Behavior:** Forward flurry velocity tuned to `0.60f` ($1.62\text{ units}$ total), giving Saitama an intimidating, steady anime martial artist advance.


---

## 34. Saitama Celebratory Jump Arc & Single-Turn Round Transition (`FighterAnimator.cs`, `MatchManager.cs`)

### Feature Overview:
Restores Saitama's authentic 9-frame celebration sequence where he squats, springs into the air with a triumphant punch into the sky, lands on the mat, and raises his fist in victory—tuned to play for exactly one turn and immediately trigger the next round.

### The 9-Frame Mathematical Jump Arc:
All frames are normalized on a $160 \times 160\text{px}$ canvas with the ground baseline at $Y = 135$:
1. **Frames 0–2 (Squat Windup):** Feet anchored to $Y = 135$. Saitama coils low into a compact spring ($H = 34\text{px} \to 25\text{px}$).
2. **Frame 3 (Ascending Leap):** Springs upward with feet leaving the ground at $Y = 120$ ($15\text{px}$ above baseline).
3. **Frame 4 (Apex Mid-Air Punch):** Peak airborne frame! Knees bent in mid-air with feet at $Y = 104$ ($31\text{px}$ above ground), right gloved fist thrusting high into the sky ($Y = 57$).
4. **Frame 5 (Impact Landing):** Lands firmly back on the ground canvas ($Y = 135$) with cape billowing upward from kinetic momentum.
5. **Frames 6–7 (Cape Settling):** Settles gracefully into stance as the cape flutters ($Y = 135$).
6. **Frame 8 (Triumphant Fist Pump):** Stays standing proudly in his heroic victory pose with fist raised ($Y = 135$).

### The Single-Turn Arcade Pacing:
- In `FighterAnimator.cs`, `FighterState.Victory` executes via `PlayOnce` rather than `PlayLoop`.
- In `MatchManager.cs`, `RoundEndSequence` waits for `anim.VictoryDuration` (derived as $\frac{\text{FrameCount}}{\text{FPS}} = \frac{9}{6.0} = 1.50\text{s}$).
- The instant that single turn concludes, `CheckWinCondition()` immediately fires `StartRound()` without any awkward pause or repetitive bouncing.

---

## 35. Super Saiyan Complete Animation Suite & Zero-Jitter Baseline Synchronization (`Frames_SSJ/`)

### Feature Overview
Integrates a complete 44-frame custom pixel-art animation suite for Goku's Super Saiyan form, standardizing all frames onto $128 \times 128\text{px}$ transparent canvases with a strict ground plane baseline at $Y = 105$.

### Track-by-Track Mathematical Synchronization:
1. **Idle Cycle (`idle_0..3.png` - 4 Frames @ 5 FPS):**
   - Height: $63\text{–}65\text{px}$, Width: $34\text{–}37\text{px}$.
   - Stance center balanced at $X = 64$.
   - Ground contact pinned at $Y = 105$. Breathing cycle oscillates at 5 FPS matching Base Goku's breathing rhythm.

2. **Walk/Dash Cycle (`walk_0..5.png` - 6 Frames @ 8 FPS):**
   - Height: $39\text{–}43\text{px}$, Width: $53\text{–}63\text{px}$.
   - Goku's iconic low-profile forward ninja sprint from Jump Ultimate Stars.
   - Feet anchored to $Y = 105$. Runs at 8 FPS smoothly synchronized with horizontal physics velocity.

3. **Light Punch Flurry (`punch_0..5.png` - 6 Frames @ 13.6 FPS):**
   - Height: $50\text{–}54\text{px}$.
   - **Fixed Back-Foot Anchoring:** Planted back foot locked to $X = 42$ while the front punching fist extends forward from $X = 85$ to $X = 105$. Zero horizontal ice-skating.
   - Duration: $0.44\text{s}$ total.

4. **Heavy Spin Kick Combo (`heavy_0..6.png` - 7 Frames @ 8.0 FPS):**
   - Height: $46\text{–}58\text{px}$, Width: $35\text{–}63\text{px}$.
   - Startup charging pose $\to$ rising roundhouse $\to$ spinning kick $\to$ follow-through finish.
   - Ground baseline locked to $Y = 105$, stance center balanced at $X = 64$.
   - Duration: $0.88\text{s}$ total.

5. **Physics Velocity Jump Curve (`jump_0..3.png` - 4 Frames):**
   - In `FighterAnimator.cs`, `UpdateJumpPose()` dynamically evaluates vertical physics velocity ($vy$):
     - $vy > 1.0\text{ m/s}$: Launch & Ascending (`jump_0`, `jump_1` with tucked knees 11px off baseline).
     - $|vy| \le 1.0\text{ m/s}$: Apex hangtime (`jump_2`).
     - $vy < -1.0\text{ m/s}$: Fast descent (`jump_3`).

6. **Crouch Duck (`crouch.png` - 1 Frame):**
   - Height: $40\text{px}$, Width: $47\text{px}$.
   - Ground contact pinned at $Y = 105$. Dynamically shrinks hurtbox height to $0.78\text{m}$.

7. **Hurt Flinch (`hurt_0..3.png` - 4 Frames @ 10 FPS):**
   - Height: $50\text{–}56\text{px}$.
   - Ground contact pinned at $Y = 105$. Plays during `FighterState.HitStun` and `KnockBack`.

8. **Dramatic Slow-Mo Knockout (`knockout_0..11.png` - 12 Frames @ 3.15 FPS):**
   - 12-stage collapse sequence perfectly mapped across the 3.8-second unscaled slow-motion window:
     - Frames 0–4: Stumble backward and reel from impact (baseline $Y = 105$).
     - Frames 5–6: Knees buckle (baseline $Y = 105$).
     - Frame 7: Airborne arc falling backward (natural 9px air elevation above ground).
     - Frame 8: Impact on the mat ($Y = 105$).
     - Frame 9: 3px elastic mat bounce ($Y = 102$).
     - Frames 10–11: Motionless collapse flat on the arena floor ($Y = 105$).


---

## 35. 199s Round Timer, +100s Sudden Death Overtime & Double K.O. Draw Scoring

### 1. 199s Canonical Round Timer
- **Scene Serialization Synchronization:** Resolved discrepancy where [`SampleScene.unity`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scenes/SampleScene.unity) had `roundDuration: 99`. Serialized value updated to `199` to match [`MatchManager.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchManager.cs).
- **HUD Synchronization:** Fallback timers in [`HUDController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/UI/HUDController.cs) updated from `99f` to `199f`.

### 2. Sudden Death Overtime Extension (+100s)
- **Trigger:** Regular round timer expires ($0\text{s}$) while both fighters are still standing (`p1Health.CurrentHealth > 0 && p2Health.CurrentHealth > 0`) and `!isOvertime`.
- **Action:**
  - `roundTimer = 100f`
  - `isOvertime = true`
  - Screen impact flash ($0.08\text{s}$) + camera rumble ($0.30\text{ intensity}$).
  - HUD timer font shifts to Warning Gold (`#FFD726`) and header updates to `★ OVERTIME • SUDDEN DEATH (+100s) ★`.

### 3. Time-Out Survival Draw (+1 Point to Each)
- **Trigger:** Timer expires ($0\text{s}$) after +100s overtime has elapsed and both fighters remain unbeaten.
- **Action:**
  - `p1RoundWins++` and `p2RoundWins++`.
  - Both fighters remain standing in pride (neither collapses).
  - Both player score gems light up on HUD.
  - Check win conditions or proceed to sudden-death round.

### 4. Double K.O. Simultaneous Death (+1 Point to Each)
- **Trigger:** Both fighters suffer fatal damage on the same frame or within trading windows.
- **Action:**
  - Both `FighterController` components transition to `Knockout` simultaneously.
  - Cinematic slow-motion collapse ($0.12\times$ speed for $4.0\text{s}$).
  - `p1RoundWins++` and `p2RoundWins++`.
  - Both player score gems light up on HUD.

### 5. Sudden Death Tiebreaker Progression
- If a Double K.O. or Draw pushes both players to match point simultaneously (e.g., tied 2-2 in a Best of 3):
  - Instead of ending abruptly, `CheckWinCondition()` triggers a **Sudden Death Final Round** via `StartRound()`.
  - If a player holds a point advantage (e.g., 2-1), that player is crowned Tournament Champion.

---

## 36. 10-Hit Calibrated Flurry & Non-Damaging Transformation Pipeline (`FighterController.cs`, `HitboxController.cs`)

### 1. Goku's Pure Super Saiyan Transformation (Zero Damage & Direct-to-Idle)
- **Role:** Skill button (`E` / `U`) triggers Goku's Super Saiyan power-up.
- **Hitbox Deactivation:** `attackHitbox.Deactivate()` is executed immediately on state transition.
- **Direct-to-Idle State Machine:** Rather than entering `FighterState.Skill` and triggering an attack duration lockout, `FighterController.cs` transitions directly to `FighterState.Idle`.
- **Legacy Asset Purge:** Deleted legacy `skill_0..3.png` from `Assets/Sprites/Fighters/Goku/Frames/`.
- **Zero Offensive Reach:** `attackStartupTimer = 0f` and `attackActiveTimer = 0f`. No hitbox activates, zero damage is inflicted, and forward surge speed is zeroed out (0.0 m/s), ensuring Goku plants his feet solidly during his transformation.
- **Instantaneous SSJ Visuals:** Because `controller.IsSuperSaiyan` is asserted immediately, `FighterAnimator.cs` loads `ssjIdleFrames` on the exact same frame, seamlessly rendering Goku in his blazing Super Saiyan stance with 0ms visual delay.

### 2. Saitama's Calibrated 10-Hit Flurry (Consecutive Normal Punches)
- **Hit Budget:** Exactly **10 punches** delivered across the 2.30s active window ($0.23	ext{s}$ interval between hits).
- **Damage per Punch:** Exactly **2 damage** ($10 	imes 2 = 20	ext{ total damage}$).
- **Engine HitStop Disabled:** `enableHitStop = false` bypasses the `Time.timeScale = 0.04f` freeze. The game engine runs at 100% speed throughout the entire move with zero frame stutter.
- **Attenuated Camera Vibration:** Screen shake intensity scaled down to $20\%$ (`shakeMult = 0.20f`), preventing exponential camera oscillation over 10 rapid strikes.
- **Micro-Knockback:** $X = 0.35	ext{m/s}, Y = 0.0	ext{m/s}$. Keeps the opponent neatly trapped in the flurry without launching them away.
- **Hitbox Expiration:** The moment the 10th punch connects, `attackHitbox.Deactivate()` shuts down offensive collision, allowing Saitama to conclude his vocal shout cleanly.


---

## 36. Authentic Anime Kamehameha VFX & AoE Beam Specification

### 1. Visual & Acoustic Specifications
- **Total Duration:** $3.36\text{ seconds}$ (100% synchronized with `goku_voice_kamehameha.wav`).
- **Phase 1: Gathering Ki Charge Orb ($2.15\text{s}$):**
  - Texture: `Assets/Sprites/Effects/kamehameha_charge_orb.png` ($128 \times 128$).
  - Position: Cupped hands at hip ($(X = -0.35\text{m}, Y = 0.88\text{m})$ relative to pivot when facing right).
  - Animation: Non-linear expansion from $0.20\text{m} \rightarrow 0.88\text{m}$ with $30\text{ Hz}$ sine vibration.
- **Phase 2: Solid Light-Blue Energy Beam Blast ($1.21\text{s}$):**
  - Muzzle Flare: `kamehameha_muzzle_flare.png` at $(X = +0.55\text{m}, Y = 0.95\text{m})$.
  - Beam Body: `kamehameha_beam_body.png` ($7.2\text{m}$ length, Left-Center pivot).
  - Beam Head: `kamehameha_beam_head.png` ($1.3\text{m}$ length, aerodynamic energy dome).
  - Total Beam Span: **$8.5\text{ world units}$** (covers half the entire tournament arena).
  - Roaring Plasma Oscillation: $1.35\text{m} \pm 0.08\text{m}$ vertical breathing at $26\text{ Hz}$.
  - Screen Shake: $0.30\text{ intensity}, 0.38\text{s}$ duration on eruption.
  - Impact Flash: 2-frame fullscreen white flash ($0.04\text{s}$).

### 2. Combat Mechanics
- **Damage:** $45\text{ HP}$ (Ultimate attack tier).
- **Knockback Vector:** $8.5\text{f}$ horizontal, $2.2\text{f}$ vertical lift ("normal knockback" that pushes target to wall without launching off-screen).
- **Single-Hit AoE:** Opponent is hit exactly once upon touching the beam and cannot be multi-hit by the same beam blast.
- **Ki Cost:** $80\text{ Ki}$.


---

## 42. In-Combat Pause Menu & Post-Match Results Screen Text Architecture

### Overview
The combat menu system (`CombatMenuController.cs`) governs two critical modal states:
1. **In-Combat Pause Menu (`PauseMenu_Root`):** Triggered solely by `[ESCAPE]`. Freezes `Time.timeScale = 0f` and `AudioListener.pause = true` while keeping UI interactive (polling for `P` removed to prevent collision with right-hand arcade clusters).
2. **Post-Match Results Screen (`ResultsMenu_Root`):** Triggered after the final round K.O. Slides down from off-screen ($Y=+950\text{px}$) with winner portrait, tournament epithet, final round score, and rematch options. Small textual elements have been scaled up ($15\text{pt} - 22\text{pt}$) for arcade and couch readability.

### Visual & Typography Specifications

| Element | Primary Font | Fallback Font | Font Size | Display Text | Color Styling |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Pause Header** | `ArcadeClassic_SDF` | `LiberationSans SDF` | `32pt` | `PAUSED` | Electric Cyan (`#00F5FF`) |
| **Pause Subtitle** | `RetroGaming_SDF` | `LiberationSans SDF` | `15pt` | `- COMBAT FROZEN -` | Crisp Ice (`#D9EBFF`) |
| **Pause Selection** | `RetroGaming_SDF` | `LiberationSans SDF` | `20pt` | `>  RESUME  <` | Solar Gold (`#FFE040`) |
| **Pause Navigation**| `RetroGaming_SDF` | `LiberationSans SDF` | `15pt` | `[W/S or ARROWS] Navigate \| [ENTER] Select \| [ESC] Resume` | Muted Silver |
| **Victory Tag** | `RetroGaming_SDF` | `LiberationSans SDF` | `16pt` | `* ANIMEVERSE MARTIAL ARTS TOURNAMENT *` | Electric Cyan (`#00F5FF`) |
| **Victory Header** | `ArcadeClassic_SDF` | `LiberationSans SDF` | `34pt` | `* WORLD CHAMPION *` | Solar Gold (`#FFE040`) |
| **Winner Title** | `ArcadeClassic_SDF` | `LiberationSans SDF` | `30pt` | `PLAYER 1: GOKU WINS!` | Cyan (`#00F5FF`) or Gold |
| **Winner Epithet** | `RetroGaming_SDF` | `LiberationSans SDF` | `18.5pt` | `HOPE OF THE UNIVERSE \| TOURNAMENT VICTOR` | Solar Gold Tint |
| **Final Score** | `RetroGaming_SDF` | `LiberationSans SDF` | `21pt` | `FINAL SCORE:  2 - 1` | Crisp Ice (`#D9EBFF`) |
| **Results Buttons** | `RetroGaming_SDF` | `LiberationSans SDF` | `22pt` | `REMATCH \| CHAR SELECT \| MAIN MENU` | Crisp White on Navy |
| **Results Hints** | `RetroGaming_SDF` | `LiberationSans SDF` | `15pt` | `[W/S or ARROWS] Navigate \| [ENTER] Confirm \| [ESC] Main Menu` | Muted Silver |

### Zero-Block Guarantee
1. **Direct YAML `_MainTex` Connection:** `ArcadeClassic_SDF.asset` and `RetroGaming_SDF.asset` have their Material `_MainTex` connected directly to their sub-asset `Texture2D` fileIDs.
2. **Automatic Fallback Chaining:** All characters missing from the retro font atlas automatically fall back to `LiberationSans SDF`.
3. **Pure ASCII Strings:** All typography uses universal ASCII characters (`>`, `<`, `*`, `-`, `|`), eliminating missing glyph tofu errors.
4. **Runtime Self-Healing:** Every call to `Awake()`, `Start()`, `PauseGame()`, and `ShowResultsSequence()` triggers `SanitizeAndHealUIHierarchy()`, ensuring text meshes never enter a corrupted state.


---

## 37. Dynamic 3-Round Arena System, Interactive Animated Flame Braziers & Multiverse Incursion VFX

### Overview
To transform the combat arena from a static image into a living, breathing stage, `ArenaBackgroundManager.cs` introduces a **3-Round Stage Evolution System** paired with **interactive pixel-art flame braziers**, **ember particle emitters**, and a **Doctor Strange rotating Tao Mandala**.

### 1. The Three Evolving Round Stages

| Round | Stage Atmosphere | Key Visual Elements | Environmental VFX |
| :--- | :--- | :--- | :--- |
| **Round 1** | **Grand Martial Arts Tournament** *(Dusk / Twilight)* | Ancient carved dragon stone floor, temple pagodas, decorative banners, sunset mountain silhouettes. | Warm orange brazier flames, drifting gentle embers (14/s), peaceful pre-match air. |
| **Round 2** | **Battle on the Moon** *(Saitama vs Boros Homage)* | Moon regolith crater floor, dark cosmic starry void, radiant blue-and-white Planet Earth in the sky. | Eerie blue/cyan plasma flames, Doctor Strange Tao Mandala rotating in space rift, low-gravity cosmic dust (12/s). |
| **Round 3** | **Multiverse Dimensional Cataclysm** *(Mirror Dimension x Saiyan Ki)* | Shattered floating stone platforms, cosmic reality tear, blinding solar gold & crimson nebulae (Clean composition with zero circular mandalas). | Blazing Super Saiyan golden ki flames, lightning fractures, intense ember surges (38/s). |

### 2. Interactive Animated Flame Braziers (`BrazierFlameAnimator.cs`)
- **Twin Flanking Pedestals:** Located at $X = -5.5$ and $X = +5.5$, $Y = -2.4$, perfectly framing the 16:9 arcade camera zoom while keeping the main sparring lane clear.
- **Carved Dragon Stone Stand (`brazier_stand_pixel.png`):** Handcrafted $40 \times 64$ retro arcade stone plinth with gold runic collar and ruby eye gem.
- **8-Frame Looping Pixel Fire:**
  - `Orange` (Tournament): $10\text{ FPS}$ breathing warm flame loop with white-yellow core and curling vermillion tongues.
  - `Blue` (Moon / Space): Cold incandescent cyan core tapering into violet eldritch flame tips.
  - `Gold` (Saiyan Overdrive): Towering $115\%$ height divine ki flame loop with surging solar aura borders.
- **Interactive Combat Surge:**
  When a heavy attack, Kamehameha beam, or Serious Punch connects:
  $$\text{surgeScale} = 1.4\times, \quad \text{extraEmbers} = +12$$
  The braziers visually flare up and shower the arena with sparks, instantly grounding combat hits into the physical world!

### 3. Doctor Strange Rotating Tao Mandala (Featured in Round 2)
- Rendered from `time_stone_mandala.png` at sorting order $-9$ (behind braziers and fighters).
- Positioned inside the dimensional space fissure at $(X = 0.5, Y = 0.7)$ during the Round 2 Moon Battle.
- Rotates continuously at $16^\circ/\text{s}$ in `Update()`, creating mystical sorcery motion in the background.
- **Explicitly Disabled in Round 3:** In the final round, the mandala is completely omitted, allowing the raw, unobstructed Super Saiyan ki auroras, lightning fractures, and floating temple debris to command the screen.
- Rotates continuously at $16^\circ/\text{s}$ in `Update()`, creating dynamic movement in the background while players fight.


---

## 38. Comprehensive Combat Attributes, Frame Data, Kinetic Physics & Red Zone Mechanics

### Overview
To transition Mystic Combat into an authentic, esports-ready fighting experience, the combat engine features calibrated low poke damages, decisive high-risk Ultimates, Gatling chain cancels, and a high-stakes Red Zone exhaustion mechanic.

### 1. Calibrated Attack & Damage Matrix

| Attack Move | Character | Damage (Normal) | Damage (Red Zone Target) | Startup | Active | Total Duration | Knockback $(X, Y)$ | Attack Level / Target |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Light Attack** | Goku | $3\text{ HP}$ ($5$ SSJ) | $4\text{ HP}$ ($7$ SSJ) | $0.08\text{s}$ | $0.20\text{s}$ | $0.38\text{s}$ | $(2.0\text{f}, 0.5\text{f})$ | High (Duckable) |
| **Light Attack** | Saitama | $4\text{ HP}$ | $5\text{ HP}$ | $0.08\text{s}$ | $0.20\text{s}$ | $0.38\text{s}$ | $(2.2\text{f}, 0.5\text{f})$ | High (Duckable) |
| **Heavy Attack** | Goku | $7\text{ HP}$ ($10$ SSJ) | $9\text{ HP}$ ($14$ SSJ) | $0.18\text{s}$ | $0.32\text{s}$ | $0.75\text{s}$ | $(5.0\text{f}, 1.8\text{f})$ | Mid (Hits crouching) |
| **Heavy Attack** | Saitama | $9\text{ HP}$ | $12\text{ HP}$ | $0.18\text{s}$ | $0.32\text{s}$ | $0.75\text{s}$ | $(5.5\text{f}, 2.0\text{f})$ | Mid (Hits crouching) |
| **Skill (Flurry)** | Saitama | $2 \times 15 = 30\text{ HP}$ | $3 \times 15 = 45\text{ HP}$ | $0.15\text{s}$ | $2.30\text{s}$ | $2.70\text{s}$ | $(0.30\text{f}, 0.0\text{f})$ | Mid (Traps opponent) |
| **Skill (SSJ)** | Goku | Buff ($0\text{ Dmg}$) | N/A | $0.00\text{s}$ | $15.0\text{s}$ | Instant | N/A | Ki Power-Up ($+25\%$ Spd) |
| **Ultimate (Kamehameha)**| Goku | $45\text{ HP}$ | $61\text{ HP}$ | $2.15\text{s}$ | $1.21\text{s}$ | $3.36\text{s}$ | $(8.5\text{f}, 2.2\text{f})$ | Full-Screen AoE Beam |
| **Ultimate (Serious Punch)**| Saitama | $90\text{ HP}$ | **$122\text{ HP}$ (Instant KO!)** | $2.40\text{s}$ | $1.40\text{s}$ | $3.80\text{s}$ | $(12.0\text{f}, 3.0\text{f})$ | Melee Cataclysm |

### 2. Character Asymmetry & Passive Attributes

| Attribute | Goku (Agile Martial Artist) | Saitama (Immovable Titan) |
| :--- | :--- | :--- |
| **Move Speed** | $5.2\text{ m/s}$ ($6.5\text{ m/s}$ in Super Saiyan) | $4.4\text{ m/s}$ |
| **Jump Force** | $12.5\text{ m/s}$ (High aerial mobility & anti-air control) | $10.8\text{ m/s}$ (Heavy, grounded leap) |
| **Knockback Resistance**| Standard ($100\%$ knockback applied) | **Dense Mass: $20\%$ passive knockback reduction** ($80\%$ knockback applied) |
| **Special Utility** | $15\text{s}$ Super Saiyan transformation with continuous Ki aura | $15\text{-hit}$ multi-punch flurry trapping opponents |
| **Ki Economy** | Fast meditation ($15\text{ Ki/s}$), Kamehameha costs $80\text{ Ki}$ | Steady meditation ($12\text{ Ki/s}$), Serious Punch costs $80\text{ Ki}$ |

### 3. Red Zone Mechanics (High-Risk Exhaustion)
- **Damage Vulnerability (+35%):** Exhausted fighters receive $+35\%$ damage from all attacks.
- **Extended Hitstun (+0.12s):** Base hitstun is extended from $0.22\text{s}$ to $0.34\text{s}$ (light) and $0.38\text{s}$ to $0.50\text{s}$ (heavy).
- **Visual Danger Pulse:** `SpriteRenderer.color` pulses between white and danger vermillion (`#FF6666`) at 5 Hz.
- **Movement Penalty:** Ground movement speed is reduced to $35\%$.
- **Recovery Acceleration:** Meditating while in the Red Zone increases exhaustion recovery speed by $1.5\times$.

### 4. Kinetic Physics & Hitstop Tuning
- **Grounded Friction:** Knockback decelerates at $14\text{ m/s}^2$ on ground contact, stopping slippery sliding.
- **Asymmetrical Gravity:** Falling speed accelerates at $1.35\times$ base gravity scale (`3.0 -> 4.05`), ensuring snappy landings.
- **Wall Impact Crunch:** Boundary collisions at $|V_x| > 3.0\text{ m/s}$ spawn dust puffs and camera shake (`0.18f, 0.15f`).
- **Gatling Chain Canceling:** Light attacks unlock immediate cancellation into Heavy Attack or Skill upon hit confirmation (`OnAttackHitConfirmed`).


---

## 39. Capsule Physics Colliders, Anti-Stacking Geometry & Duckable Attack Clearance

### Overview
To eliminate clumsy character interactions—such as standing on an opponent's head or shoving them across the floor by running—the physics and hitbox subsystems feature rounded vertical capsule bodies, a proximity push-lock, and calibrated vertical hitboxes.

### 1. Physical Collider Specification

| Parameter | Previous Value | New Calibrated Value | Gameplay Impact |
| :--- | :--- | :--- | :--- |
| **Collider Type** | `BoxCollider2D` (Flat edges) | **`CapsuleCollider2D` (Vertical)** | Hemispherical dome top naturally deflects descending jumpers. |
| **Collider Size** | $0.80\text{m} \times 1.90\text{m}$ | **$1.10\text{m} \times 1.45\text{m}$** | Compact vertical profile ($1.45\text{m}$) prevents aerial clipping; wider width ($1.10\text{m}$) enforces sparring gap. |
| **Collider Offset**| $(0.00\text{m}, 0.95\text{m})$ | **$(0.00\text{m}, 0.725\text{m})$** | Anchored cleanly between ground level and chest height. |
| **Push Behavior** | Dynamic friction shove | **Immovable Proximity Lock** | Forward velocity clamped to $0$ when moving towards opponent at gap $\le 1.15\text{m}$. |
| **Aerial Stacking**| Supported horizontal balance | **Anti-Stacking Slip Vector** | Descending fighters over opponent ($dy > 0.70\text{m}$) are pushed horizontally at $3.0\text{ m/s}$. |

### 2. Vertical Attack Hitbox & Ducking Hurtbox Table

| State / Attack | Hurtbox / Hitbox Size | Offset $(X, Y)$ | Vertical Span $(Y_{\text{min}} \to Y_{\text{max}})$ | Interaction with Ducking |
| :--- | :--- | :--- | :--- | :--- |
| **Standing Hurtbox** | $0.85\text{m} \times 1.80\text{m}$ | $(0.00\text{m}, 0.95\text{m})$ | $0.050\text{m} \to 1.850\text{m}$ | Full-body vulnerable target |
| **Crouching Hurtbox** | $0.75\text{m} \times 0.65\text{m}$ | $(0.00\text{m}, 0.35\text{m})$ | $0.025\text{m} \to 0.675\text{m}$ | **Low profile target ($Y_{\text{top}} = 0.675\text{m}$)** |
| **Light Attack (Poke)** | $0.75\text{m} \times 0.45\text{m}$ | $(0.80\text{m}, 1.05\text{m})$ | $0.825\text{m} \to 1.275\text{m}$ | **Clean Whiff** ($+0.150\text{m}$ clearance) |
| **Heavy Attack (Strike)**| $0.82\text{m} \times 0.48\text{m}$ | $(0.84\text{m}, 1.05\text{m})$ | $0.810\text{m} \to 1.290\text{m}$ | **Clean Whiff** ($+0.135\text{m}$ clearance) |
| **Saitama Skill (Flurry)**| $1.40\text{m} \times 0.90\text{m}$ | $(1.05\text{m}, 0.70\text{m})$ | $0.250\text{m} \to 1.150\text{m}$ | **Hits Ducking** (Punishes crouching) |
| **Goku Ult (Kamehameha)**| $8.50\text{m} \times 1.60\text{m}$ | $(4.60\text{m}, 0.95\text{m})$ | $0.150\text{m} \to 1.750\text{m}$ | **Hits Ducking** (Un-duckable AoE beam) |
| **Saitama Ult (Serious Punch)**| $1.40\text{m} \times 1.00\text{m}$ | $(1.25\text{m}, 0.95\text{m})$ | $0.450\text{m} \to 1.450\text{m}$ | **Hits Standing / Close** (Short melee reach) |


---

## 40. Waist-Level Push Boxes, Calibrated Close-Range Heavy Attacks & Strict Hurtbox Filtering

### Overview
This update eliminates the root cause of the ducking dodge failure by strictly filtering hit registrations to hurtboxes, replaces tall body colliders with short waist-high capsules ($0.90\text{m}$) for free cross-over jumps, keeps the physical capsule static at runtime to prevent Box2D depenetration jumping, and scales the Heavy Attack hitbox down to a compact, close-range strike ($10\%$ larger than Light Attack).

### 1. Attack Hitbox & Collision Profile

| Parameter | Light Attack (Jab) | Heavy Attack (Old) | Heavy Attack (New Calibrated) | Ratio (New Heavy vs Light) |
| :--- | :--- | :--- | :--- | :--- |
| **Hitbox Size** | $0.75\text{m} \times 0.45\text{m}$ | $1.05\text{m} \times 0.55\text{m}$ | **$0.82\text{m} \times 0.48\text{m}$** | **$+9.3\%$ width, $+6.7\%$ height (~10%)** |
| **Hitbox Offset** | $(0.80\text{m}, 1.05\text{m})$ | $(0.95\text{m}, 1.00\text{m})$ | **$(0.84\text{m}, 1.05\text{m})$** | $+0.04\text{m}$ forward reach |
| **Maximum Reach**| $1.175\text{m}$ | $1.475\text{m}$ (Excessive) | **$1.250\text{m}$ (Close commitment)**| $+0.075\text{m}$ over light jab |
| **Vertical Span** | $0.825\text{m} \to 1.275\text{m}$ | $0.725\text{m} \to 1.275\text{m}$ | **$0.810\text{m} \to 1.290\text{m}$** | $Y_{\text{min}} = 0.810\text{m}$ |
| **Clearance over Duck**| $+0.150\text{m}$ (Clean Whiff) | $+0.170\text{m}$ | **$+0.135\text{m}$ (Clean Whiff)** | Guaranteed whiff over $Y_{\text{top}} = 0.675\text{m}$ |

### 2. Physical Body Collider Calibration

| Property | Value (Standing & Crouching) | Impact |
| :--- | :--- | :--- |
| **Capsule Dimensions** | $1.10\text{m} \times 0.90\text{m}$ (Waist-high) | Leaves upper body free for aerial cross-overs |
| **Capsule Offset** | $(0.00\text{m}, 0.45\text{m})$ | Anchored at lower torso and legs |
| **Runtime Resizing** | **STATIC (Never Resized)** | Prevents Box2D floor depenetration impulses and "infinite jumping" |
| **Grounded Push Lock** | Active at gap $\le 1.15\text{m}$ | Bulldozing prevented on the ground |
| **Airborne Jumps** | Full horizontal speed | Cross-overs completely unobstructed |


---

## 48. Saitama Serious Punch Cinematic Engine, Re-Ordered Physical Frames & Fair Hitbox Tuning

### Overview
To give Saitama's Ultimate attack the terrifying, psychological dread of the anime's iconic "DEATH PUNCH" (Episode 5 vs. Genos), `SaitamaSeriousPunchCinematic.cs` integrates the high-detail big box frames from `saitama_jus__by_arzeer_de00xcg.png` while strictly preserving the 2.40s physical buildup, voice line synchronization, and short, fair hitbox.

### 1. Frame Re-Ordering & Transitional Pose Correction
In the original sprite exports, the standing forward frame (`ultimate_0`) was erroneously placed at index 0, causing Saitama to stand still before awkwardly dropping into a crouch.
- **The Correct Physical Sequencing:**
  - `ultimate_0.png` through `ultimate_5.png`: Drop into low crouch and coil right fist backward into deep shadow.
  - `ultimate_6.png`: **The Transition Frame** (the re-positioned standing braced frame), held during the final milliseconds before the punch launches!
  - `ultimate_7.png` through `ultimate_10.png`: Forward punch eruption and kinetic release.
  - `ultimate_11.png` through `ultimate_13.png`: Wind deceleration, stepping up, and recovery.

### 2. The 3-Phase Arcade Super Combo Freeze

| Phase | Duration / Time | Character Pose & Audio | Visual Overlays & FX | Damaging Hitbox |
| :--- | :--- | :--- | :--- | :--- |
| **1. Coiled Buildup** | $0.0\text{s} - 2.4\text{s}$ | Stance drop (`ultimate_0..5`) and braced transition (`ultimate_6`). Voice: *"Hissatsu... Maji Series..."* | Normal arena lighting. Saitama coils with kinetic tension. | Inactive (Defender can jump or back away) |
| **2. Pitch-Black Super Freeze** | $2.40\text{s} - 2.68\text{s}$ ($0.28\text{s}$ window) | Vocal release: **"SERIOUS PUNCH!"** | Screen snaps to **100% solid pitch-black void** (`sortingOrder = 950`). **Big Punch Cut-In ($720 \times 628\text{px}$)** flashes against the void, dynamically positioned near screen center without exceeding Saitama's $X$ coordinate backwards. | Inactive (Cinematic transition freeze) |
| **3. Instant Snap-Back & Strike** | $2.68\text{s} - 3.20\text{s}$ | Physical punch release (`ultimate_7..10`). | Black screen vanishes instantly! Arena returns. Saitama continues his normal character punch animation. White impact flash, heavy camera shake, and brazier surge erupt! | **Active: Short Melee Hitbox ($1.4\text{m} \times 1.0\text{m}$)**. Deals 90 damage only at point-blank range. |
| **4. Recovery** | $3.20\text{s} - 3.80\text{s}$ | Stance recovery (`ultimate_11..13`). Wind dissipates. | Normal gameplay flow resumes. | Inactive |

### 3. Hitbox Discipline & Competitive Fairness
- Damaging Hitbox Size: `Vector2(1.4f, 1.0f)`
- Damaging Hitbox Offset: `Vector2(1.25f, 0.95f)`
- **Competitive Rule:** Even though the Big Punch cut-in is visually massive during the super freeze, the damaging hitbox is strictly confined to close melee range. A defender who anticipates the 2.40s telegraphed buildup and spaces backward or neutral-jumps will safely avoid all damage!


---

## 49. Mirror Match Alternate Palette Engine & Multi-Screen Synchronization

### Overview
In fighting game tournament play, mirror matches (Goku vs. Goku or Saitama vs. Saitama) require unambiguous visual differentiation between Player 1 and Player 2/CPU across every game state. The Mirror Match Engine provides high-contrast alternate palettes, persistent baseline state tracking (`baseTintColor`), and multi-screen visual synchronization.

### 1. Alternate Palette Specifications

| Character | Base Identity | P1 Palette (Original) | P2 Alternate Palette (Mirror) | RGB Multiplier | Visual Presentation |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Son Goku** | Turtle School Dogi | Classic Orange / Indigo | **Cool Ice / Cyan Gi** | `RGBA(0.45, 0.75, 1.0, 1.0)` | Multiplies across orange gi into an electrifying cobalt/cyan dogi, preserving deep black hair and martial arts belt. |
| **Saitama** | Hero for Fun Suit | Yellow Suit / Red Gloves / White Cape | **Fiery Sunset / Crimson Suit** | `RGBA(1.0, 0.50, 0.45, 1.0)` | Multiplies across yellow suit into a bold blazing coral/crimson combat uniform with warm cape highlights. |

### 2. Multi-Screen Synchronization Table

| Stage / System | Visual Component | Mirror Match Behavior (`p1 == p2`) | Normal Match Behavior (`p1 != p2`) |
| :--- | :--- | :--- | :--- |
| **Character Select** | P2 Indicator Badge | Updates to **`[ P2: ALT PALETTE ]`** or **`[ CPU: ALT PALETTE ]`** | Displays standard `[ PLAYER 2 ]` or `[ CPU: DIFFICULTY ]` |
| **Character Select** | Mirror Match Banner | Activates `Mirror_Match_Notice`: `? MIRROR MATCH: P2 RECEIVES ALTERNATE PALETTE ?` | Hidden |
| **Versus Screen** | `versusP2AvatarImage` | Multiplied by `GetAlternatePaletteColor(p2Id)` | Multiplied by `Color.white` (Original) |
| **Versus Screen** | P2 Name / Badge | Appends **`[ALT]`** to character name and **`(ALT PALETTE)`** to badge | Standard name and badge |
| **Arena Combat** | In-World `SpriteRenderer` | Set to `baseTintColor` via `SetBaseTintColor()`. Red Zone pulses relative to this tint. | Normal `Color.white` |
| **Combat HUD** | `p2AvatarCard` | Top-right avatar bust card tinted with `GetAlternatePaletteColor(p2Id)` | Normal `Color.white` |
| **Round Reset** | Reset Subsystem | `ResetForNewRound()` explicitly restores `spriteRenderer.color = baseTintColor` | Restores `Color.white` |

### 3. Architecture & Bug Resolution
- **Root Cause of Missing Tint:** Previous implementation set `sr2.color` once at instantiation in `PrototypeBootstrap`, but `FighterController.HandleRedZoneVisuals()` in `Update()` executed `else if (spriteRenderer.color != Color.white) spriteRenderer.color = Color.white;`, instantly clobbering the alternate tint on Frame 1.
- **The State Pattern Solution:** `FighterController` now stores `baseTintColor`. Runtime effects pulse between `baseTintColor` and effect colors (e.g. Red Zone danger red), and non-exhausted states cleanly restore `baseTintColor`, guaranteeing that Player 2's alternate palette persists throughout the entire match.


---

# 38. Fighter Rebalance, Asymmetric Ki Economy, SSJ Toggle & Climax Invulnerability

### Combat Tuning Matrix

| Parameter / Mechanic | Son Goku (Ki Martial Artist) | Saitama (Physical Brawler) | Design Rationale |
| :--- | :--- | :--- | :--- |
| **Meditation Ki Rate** | **$8.0\text{ Ki/s}$** | **$5.0\text{ Ki/s}$** | Goku has formal spiritual training; Saitama lacks ki control. |
| **Light Attack Ki Gain** | **$+4.0\text{ Ki}$** | **$+2.5\text{ Ki}$** | Fast Ki accumulation for Goku; slower for Saitama. |
| **Heavy Attack Ki Gain** | **$+7.0\text{ Ki}$** | **$+5.0\text{ Ki}$** | Rewards heavy commitment with higher energy return. |
| **Target Comeback Ki** | $+1.5\text{ Ki}$ (both fighters) | $+1.5\text{ Ki}$ (both fighters) | Nerfed from $3.0\text{ Ki}$ to slow down unearned supers. |
| **Starting Ki** | **$0\text{ Ki}$** | **$0\text{ Ki}$** | Free initial $25\text{ Ki}$ removed; players must earn specials. |
| **Skill Mechanism** | **Super Saiyan (Active Toggle)** | **Consecutive Normal Punches** | Strategic buff toggle vs instant multi-hit melee strike. |
| **Skill Ki Cost** | **$2.5\text{ Ki/s}$ continuous drain** | **$30\text{ Ki}$ upfront cost** | Goku burns stamina over time; Saitama expends upfront energy. |
| **Skill Cooldown** | **$15.0\text{s}$** (begins upon deactivation) | **$15.0\text{s}$** (begins on cast) | Anti-spam discipline prevents back-to-back activation. |
| **Skill Payload** | $+25\%$ Spd, $+66\%$ Light Dmg, $+43\%$ Heavy Dmg | **5 hits @ 6 damage = 30 dmg** ($0.35\text{s}$ hit spacing) | Spaced punches allow defender to counter-hit or interrupt! |
| **Ultimate Mechanism** | Kamehameha Beam ($45\text{ dmg}$) | Serious Punch ($90\text{ decisive dmg}$) | Long-range beam vs point-blank execution blow. |
| **Ultimate Ki Cost** | **$70\text{ Ki}$** | **$100\text{ Ki}$** (Full meter required) | Reflects payload disparity ($45\text{ dmg}$ vs $90\text{ dmg}$). |
| **Ultimate Cooldown** | **$15.0\text{s}$** | **$15.0\text{s}$** | Ensures fair neutral resets after super attacks. |
| **Climax Invulnerability** | **Active during final $1.0\text{s}$ & beam** | **Active during final $1.0\text{s}$ & strike** | Prevents cheap late-startup interruptions. |
| **Voice Interruption** | Voice cuts off on `HitStun` / `Knockout` | Voice cuts off on `HitStun` / `Knockout` | Stops dangling vocal scream when flinching. |

---

### Saitama Skill Rework: Interruption & Defense Windows
Previously, Consecutive Normal Punches delivered 15 micro-hits @ 2 damage ($15 \times 2 = 30$), which locked the opponent in continuous hitstun with zero counterplay.
- **New Tuning:** 5 deliberate punches @ 6 damage ($5 \times 6 = 30$ total damage).
- **Hit Interval:** $0.35\text{s}$ between punches.
- **Defensive Counterplay:** If the opponent is hit by punch 1 or blocks, there are small recovery windows between subsequent punches where a fast jab or armored move can strike Saitama.
- **Interruption Consequence:** If Saitama is struck, his flurry immediately halts, his hitbox deactivates, and his battle shout (*"Consecutive Normal Punches!"*) is instantly stopped.

---

## 50. Option A 4-Tier Segmented Ki Meter: Dynamic Threshold Calibration & Visual Economy

### 1. Functional Specification

| Segment Zone | Fill Color | Hex Code | Ki Interval | Tactical Meaning & In-Game Value |
| :--- | :--- | :--- | :--- | :--- |
| **Zone 1: Red Zone** | Crimson Scarlet | `#FF3838` | $0 \to \text{Mark 1}$ ($10\text{ Ki}$) | **Danger Zone:** Fighter is vulnerable to or inside Red Zone Exhaustion ($30\%$ move speed, no attack canceling). |
| **Zone 2: Safe Buffer** | Neon Mint Green | `#33F259` | $\text{Mark 1} \to \text{Mark 2}$ | **Safe Neutral Zone:** Fighter is safe from exhaustion, but activating Skill will consume Ki below Mark 1 into the Red Zone. |
| **Zone 3: Skill-Safe** | Sky Electric Blue | `#1FC0FF` | $\text{Mark 2} \to \text{Mark 3}$ | **Skill-Ready Buffer:** Reaching Mark 2 ensures that activating Skill leaves the fighter safely above the Red Zone! |
| **Zone 4: Ultimate Surplus** | Solar Gold | `#FFE02E` | $\text{Mark 3} \to \text{Max 100}$ | **Ultimate Buffer:** Reaching Mark 3 ensures that firing an Ultimate leaves the fighter safely above the Red Zone. Above Mark 3 is pure surplus/overcharge. |

### 2. Fighter-Specific Threshold Calibration

| Threshold Mark | Visual Geometry | Son Goku Calibration | Saitama Calibration | Rationale |
| :--- | :--- | :--- | :--- | :--- |
| **Mark 1 (Red Zone)** | 3px x 26px Platinum Divider | **$10\text{ Ki}$ ($10\%$)** | **$10\text{ Ki}$ ($10\%$)** | Identical exhaustion threshold across both characters. |
| **Mark 2 (Safe Skill)** | 3px x 26px Platinum Divider | **$45\text{ Ki}$ ($45\%$)** | **$30\text{ Ki}$ ($30\%$)** | Calculated as $\text{RedZone} + \text{SkillCost}$. For Goku: $10 + 35 = 45$. For Saitama: $10 + 20 = 30$. |
| **Mark 3 (Safe Ultimate)** | 3px x 26px Platinum Divider | **$90\text{ Ki}$ ($90\%$)** | **$90\text{ Ki}$ ($90\%$)** | Calculated as $\text{RedZone} + \text{UltimateCost}$ ($10 + 80 = 90$). Reaching this mark allows unleasing super attacks safely. |

### 3. Visual Polish & Symmetry
- **Grayscale Specular Texture:** Created `ki_bar_grand_fill_white.png` so colors tint with $100\%$ saturation and zero hue contamination while retaining the 3D beveled rim highlight.
- **Divider Notches:** 3px wide metallic notches with 1px black outline drop-shadows positioned directly on the threshold lines, extending 1px above and below the bar frame to evoke physical arcade bezel brackets.
- **Mirrored Tournament P2:** Inverted coordinate anchors ensure Player 2 fills cleanly from the right border inward toward the center timer.



---

## 51. LAN Multiplayer Duel System: Peer Discovery, Socket Streaming & Synchronized Roster Selection

### Overview
The LAN Multiplayer Duel System enables two separate computers on the same local Wi-Fi or Ethernet network to compete head-to-head in tournament-grade fighting game combat. It eliminates shared keyboard cramping while providing instant, zero-desync gameplay through a Host-Authoritative binary protocol.

### 1. Functional System Flow

```
[ Main Menu ]
      │
      ├── Select "1 VS 1 LAN MULTIPLAYER" or Press [L]
      │
      ▼
[ LanLobbyModal ]
      │
      ├── [ HOST MATCH ] ──► Binds Port 7777, Discovers IPv4 ──► "Waiting for Challenger..."
      │                                                                  │
      └── [ JOIN MATCH ] ──► Enters Host IP ──► Connects to Port 7777 ───┘
                                                                         │
                                                                         ▼
                                                       [ Connection Handshake Success ]
                                                                         │
                                                                         ▼
                                                       [ Character Select Screen (Synced) ]
                                                       • Host controls P1 Selection
                                                       • Client controls P2 Selection
                                                       • Both Lock In ──► Cinematic Fly-In
                                                                         │
                                                                         ▼
                                                       [ Combat Arena (SampleScene) ]
                                                       • Host runs physics & hitboxes
                                                       • Client streams buttons & mirrors view
```

### 2. Detailed Component Breakdown

#### A. `LanLobbyModal.cs` (Lobby UI & Automatic IP Discovery)
- **Automatic IP Scanning:** Calls `LanNetworkManager.Instance.GetLocalIPv4Address()` which traverses `NetworkInterface.GetAllNetworkInterfaces()`, automatically filtering for operational, non-loopback IPv4 addresses (e.g. `192.168.1.45`).
- **One-Click Host:** Instantly spins up a `TcpListener` on port `7777`, configures `GameManager.Instance.IsLanHost = true`, and displays instructions for the joining player.
- **Client Connect:** Provides an arcade-styled text field with pre-filled default `127.0.0.1` (enabling single-machine two-window testing) and a Connect button.
- **Resilient Fallback Generation:** If UI elements are not pre-baked in the scene, `EnsureUIBuilt()` procedurally constructs all panels, buttons, and TextMeshPro labels at runtime!

#### B. `CharacterSelectController.cs` (Network Synchronization)
- **Bidirectional Packet Sync:** Whenever either player presses Left/Right to browse characters, `SendCharSelectSync()` transmits a `CharSelectSyncPacket` containing `(PlayerIndex, CharacterId, IsLockedIn)`.
- **Decoupled Roles:**
  - Host acts as Player 1 (`currentPhase = SelectingP1`).
  - Client acts as Player 2 (`currentPhase = SelectingP2OrCpu`).
- **Coordinated Launch:** When both players confirm their fighters, the Host dispatches a `MatchStartPacket` containing the finalized character IDs, triggering the dramatic `VERSUS!` slide animation on both screens simultaneously!

#### C. `NetworkMatchSynchronizer.cs` (In-Combat Real-Time Engine)
- **Host Role:**
  - Deploys Fighter 1 with local keyboard controls.
  - Deploys Fighter 2 with `NetworkFighterInput`.
  - In `FixedUpdate()`, packages a 38-byte `MatchSnapshotPacket` containing positions, velocities, health, Ki, active states, and round clocks, broadcasting to the Client at 60Hz.
  - Receives `FighterInputPacket` from Client and pipes directly into Fighter 2's input buffer.
- **Client Role:**
  - Reads local keyboard using comfortable single-player ergonomic mappings (WASD or Arrow keys).
  - Sends a 9-byte `FighterInputPacket` with bitmasked actions to the Host every frame.
  - Receives the Host's 60Hz snapshot and smoothly positions both fighters, mirrors facing direction, updates animation states, and keeps the tournament HUD in perfect synchronization.
  - Automatically disables local physics simulation (`Rigidbody2D.bodyType = Kinematic`) and attack hitboxes, completely preventing local physics drift or duplicate damage calculations.
