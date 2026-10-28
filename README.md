# 🥋 Mystic Combat: Animeverse Incursion

<div align="center">

![Unity 6](https://img.shields.io/badge/Unity%206-6000.6.7f1-000000?style=for-the-badge&logo=unity&logoColor=white)
![Render Pipeline](https://img.shields.io/badge/Pipeline-Universal%20RP%202D-1E88E5?style=for-the-badge)
![Language](https://img.shields.io/badge/Language-C%23%20%7C%20.NET%20Standard%202.1-512BD4?style=for-the-badge&logo=csharp&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%20PC%20x64-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Developer](https://img.shields.io/badge/Developer-Nikhil%20Bansal-FF6F00?style=for-the-badge&logo=github&logoColor=white)
![Netcode](https://img.shields.io/badge/Netcode-Symmetric%20LAN%20Input%20Exchange-E53935?style=for-the-badge)
![Target FPS](https://img.shields.io/badge/Performance-Locked%2060%20FPS-43A047?style=for-the-badge)

<br/>

**A tournament-grade, 16-bit arcade anime fighting game developed from scratch in Unity 6.**  
*Featuring authentic pixel art, sub-pixel hitbox physics, Doctor Strange mystic relics, data-driven balance, adaptive footsies AI, and lag-free symmetric LAN multiplayer.*

[🌌 Lore](#-lore--narrative) • [🕹️ Exact Controls](#️-controls--input-mappings) • [👥 Character Roster & Frame Data](#-character-roster--exact-move-data) • [🔮 Unique Mechanics](#-unique-game-mechanics) • [🌐 Game Modes & Netcode](#-game-modes--network-architecture) • [🚀 Quick Start](#-quick-start)

---

</div>

## 🌌 Lore & Narrative

When Doctor Strange attempts a forbidden temporal incantation to seal an eldritch multiversal tear, the spell destabilizes the fabric of space and time. Dimensional rifts violently rupture across the Animeverse, pulling two supreme champions into the ancient **Sanctum Sanctorum Tournament Arena**:

* **Son Goku** (Dragon Ball) — The Saiyan master of godlike Ki, relentless martial artist with transcendent speed, beam zoning, and the golden Super Saiyan transformation.
* **Saitama** (One-Punch Man) — The hero who shattered his physical limiter, bringing nonchalant demeanor backed by catastrophic, near-instantaneous kinetic power.

Encircled by ancient Taoist runic barriers and governed by the temporal authority of the **Emerald Time Stone**, only one warrior can claim championship victory and return to their home dimension!

---

## 🕹️ Controls & Input Mappings

The game features **100% exact, verified input profiles** configured through the central `InputSettingsManager`. All keybindings persist across sessions and can be reconfigured directly in the in-game Options menu.

### 1. Single Player Mode (Solo vs CPU & LAN Multiplayer)
*Optimized for ergonomic one-hand movement and adjacent action triggers:*

| Action | Primary Key | Description |
| :--- | :---: | :--- |
| **Move Left** | <kbd>A</kbd> | Walk backward / retreat |
| **Move Right** | <kbd>D</kbd> | Walk forward / advance |
| **Jump** | <kbd>W</kbd> | Physics jump arc |
| **Crouch / Low Guard** | <kbd>S</kbd> | Duck high attacks and crouch |
| **Light Attack** | <kbd>F</kbd> | Fast frame jab (builds Ki on hit) |
| **Heavy Attack** | <kbd>C</kbd> | Heavy strike with high knockback |
| **Special Skill** | <kbd>E</kbd> | Character-specific ability (Super Saiyan Toggle / Consecutive Punches) |
| **Ultimate Attack** | <kbd>Q</kbd> | Screen-clearing cinematic finisher |
| **Meditate (Ki Charge)** | <kbd>X</kbd> | Channel spiritual energy to regenerate Ki |
| **Pause / Return** | <kbd>Escape</kbd> | Open in-game menu or back out |

---

### 2. Local 2-Player Duel Mode (Shared Single Keyboard)
*A balanced, non-overlapping dual-layout allowing two players to fight on the same keyboard:*

| Action | Player 1 (Left Keyboard Half) | Player 2 (Right Keyboard Half) | Gamepad (Xbox / PlayStation) |
| :--- | :---: | :---: | :---: |
| **Move Left** | <kbd>A</kbd> | <kbd>J</kbd> | Left Analog Stick / D-Pad Left |
| **Move Right** | <kbd>D</kbd> | <kbd>L</kbd> | Left Analog Stick / D-Pad Right |
| **Jump** | <kbd>W</kbd> | <kbd>I</kbd> | Button South (<kbd>A</kbd> / <kbd>×</kbd>) |
| **Crouch** | <kbd>S</kbd> | <kbd>K</kbd> | Down on Analog Stick / D-Pad Down |
| **Light Attack** | <kbd>F</kbd> | <kbd>H</kbd> | Button West (<kbd>X</kbd> / <kbd>□</kbd>) |
| **Heavy Attack** | <kbd>C</kbd> | <kbd>N</kbd> | Button North (<kbd>Y</kbd> / <kbd>△</kbd>) |
| **Special Skill** | <kbd>E</kbd> | <kbd>U</kbd> | Right Shoulder (<kbd>RB</kbd> / <kbd>R1</kbd>) |
| **Ultimate Attack** | <kbd>Q</kbd> | <kbd>O</kbd> | Right Trigger (<kbd>RT</kbd> / <kbd>R2</kbd>) |
| **Meditate** | <kbd>X</kbd> | <kbd>M</kbd> | Left Trigger (<kbd>LT</kbd> / <kbd>L2</kbd>) |

---

## 👥 Character Roster & Exact Move Data

Every attribute and move frame is externalized in data-driven JSON profiles (`StreamingAssets/Characters/*.json`).

### 🥋 Son Goku (Martial Arts & Ki Transformation Specialist)

```
Max Health: 100 HP  |  Move Speed: 5.2 (Fast)  |  Jump Force: 12.5  |  Gravity Scale: 3.0
Meditation Channel Rate: +8.0 Ki/s  |  Ki Gain on Damage Taken: 1.5 Ki per hit
```

| Move | Command | Damage | Ki Cost | Frame Data (Startup / Active / Recovery) | Knockback | Properties & Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **Light Jab** | <kbd>F</kbd> | **3** | $0$ | 3 / 4 / 6 frames | $2.0$ | Fast poking jab. Generates $+4\text{ Ki}$ on hit. |
| **Fierce Roundhouse** | <kbd>C</kbd> | **7** | $0$ | 6 / 5 / 10 frames | $4.5$ | Heavy turning kick. Generates $+7\text{ Ki}$ on hit. |
| **Super Saiyan (Toggle)** | <kbd>E</kbd> | Buff | $0$ to activate<br/>(Drains $2.5\text{ Ki/s}$) | Instant Toggle | — | **Ascends to Super Saiyan!** Golden radiant hair & aura burst. Buffs: **$+25\%$ Move Speed**, Light Attack dmg boosted to **$5$** ($+66\%$), Heavy Attack dmg boosted to **$10$** ($+43\%$). Toggles off when deactivated or when Ki reaches zero. |
| **Super Kamehameha** | <kbd>Q</kbd> | **45** | $70\text{ Ki}$ | 30 / 15 / 25 frames | $8.0$ | Iconic voiced charge-up (*"KA-ME-HA-ME-HA!"*) launching a colossal, screen-spanning energy beam across the arena that obliterates everything in its path. |

---

### 🥊 Saitama (Devastating Bruiser & Kinetic Powerhouse)

```
Max Health: 100 HP  |  Move Speed: 4.4 (Heavy)  |  Jump Force: 10.8  |  Gravity Scale: 3.0
Meditation Channel Rate: +5.0 Ki/s  |  Ki Gain on Damage Taken: 1.5 Ki per hit
```

| Move | Command | Damage | Ki Cost | Frame Data (Startup / Active / Recovery) | Knockback | Properties & Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **Light Slap** | <kbd>F</kbd> | **4** | $0$ | **2** / 3 / 5 frames | $2.2$ | Ultra-fast 2-frame startup strike. Generates $+2.5\text{ Ki}$ on hit. |
| **Heavy Smash** | <kbd>C</kbd> | **9** | $0$ | 5 / 4 / 9 frames | $5.5$ | Brutal kinetic strike. Generates $+5\text{ Ki}$ on hit. |
| **Consecutive Normal Punches** | <kbd>E</kbd> | **20**<br/>($5\text{ hits} \times 4$) | $30\text{ Ki}$ | 4 / 12 / 8 frames | $3.5$ | A blinding close-range flurry of 5 rapid punches that shreds enemy guard and locks opponent in multi-hit stun. |
| **Serious Series: Serious Punch** | <kbd>Q</kbd> | **90** | $80\text{ Ki}$ | 20 / 3 / 30 frames | $12.0$ | **Cinematic Finisher:** Dramatic freeze-frame cut-in with menacing death-glare artwork, earth-shaking shockwave, and full-screen blackout blow that deals $90\text{ damage}$ (near one-hit KO). High risk: $30\text{ frames}$ recovery on whiff! |

---

## 🔮 Unique Game Mechanics

### 1. Dynamic 3-Tier Energy & Safe-Threshold Markers
Ki governs special abilities and stamina. Unlike games with static meters, our HUD features **real-time dynamic threshold indicators** calibrated to each fighter:
* **Marker 1 — Red Zone Exhaustion Threshold ($10\text{ Ki}$):**
  If a fighter's Ki drops below $10$, they enter **Red Zone Exhaustion**:
  - Movement speed drops by **$70\%$** ($0.3\times$ speed multiplier).
  - Jumping and dashing are disabled.
  - Attacks cannot be initiated for $5\text{ seconds}$ until recovering to $20\text{ Ki}$.
* **Marker 2 — Dynamic Safe Skill Threshold:**
  - **Goku:** Drawn at **$30\text{ Ki}$** (warning indicator before transformation stamina drain).
  - **Saitama:** Drawn dynamically at **$40\text{ Ki}$** ($10\text{ Red Zone} + 30\text{ Skill Cost}$). Activating Consecutive Normal Punches at or above $40\text{ Ki}$ guarantees you will not self-exhaust into the Red Zone!
* **Marker 3 — Dynamic Safe Ultimate Threshold:**
  - **Goku:** Drawn at **$80\text{ Ki}$** ($10 + 70\text{ Kamehameha Cost}$).
  - **Saitama:** Drawn at **$90\text{ Ki}$** ($10 + 80\text{ Serious Punch Cost}$).

### 2. Active Meditation Stance
Holding the Meditate key puts the fighter into an active spiritual charging stance:
- Goku recovers $+8.0\text{ Ki/s}$.
- Saitama recovers $+5.0\text{ Ki/s}$.
- Creates tactical footsie resets during neutral spacing: risk taking a hit while charging, or build Ki to unlock game-ending ultimates.

### 3. Doctor Strange Taoist Magic Relics
* **Spinning Tao Spell Mandala:** A $160\times 160\text{px}$ concentric runic green mandala continuously spinning at $22^\circ/\text{s}$ behind the match clock.
* **The Emerald Time Stone Relic:** A faceted, illuminated diamond-specular green jewel mounted directly on top of the round countdown dial.
* **Dual Sapphire Win Sockets:** Two discrete gem sockets per fighter that light up with radiant blue crystal gems upon winning rounds.
* **Pulsing Golden Championship Relic:** Sits as an empty golden socket during neutral rounds, but dynamically ignites into a glowing golden crystal that pulses with an active breathing sine wave ($1.0\times \rightarrow 1.15\times$) as soon as either player reaches Match Point!
* **Round 3 Decider Music:** If fighters tie $1-1$, Round 3 triggers the dramatic final showdown track (`Final Battle.mp3`) with heightened tension!

### 4. Adaptive AI with Tactical Spacing & Footsies
* **Combo Unlearning for Fairness:** Frame-instant Gatling cancels were intentionally removed from the AI. The computer throws single, readable attacks with full recovery windows, guaranteeing players have opportunities to counter-attack.
* **Tactical Retreat State Machine:** After launching an attack, the AI rolls a retreat check ($90\%$ on Easy, $70\%$ on Hard, $40\%$ on Extreme). On Easy mode, the bot actively walks backward for **$1.8\text{s} - 2.8\text{s}$**, opening up the stage so players can breathe, meditate, and practice combos.
* **Anti-Corner Edge Awareness:** If backed against the arena wall ($|X| \ge 6.8\text{m}$), the AI automatically executes an athletic forward jump toward center stage to avoid corner traps.

---

## 🌐 Game Modes & Network Architecture

### 1. Solo Arcade Mode
Fight against the adaptive AI across **Easy**, **Hard**, and **Extreme** difficulty tiers. Features tactical spacing footsies, AI self-preservation, and dynamic round scaling.

### 2. Local 2-Player Versus
Classic couch multiplayer on a shared screen. Both players select their fighters and battle under official tournament Best-of-3 round rules.

### 3. Symmetric LAN Multiplayer Architecture
High-performance local area network play powered by low-latency TCP sockets with `NoDelay` enabled:
* **True Symmetric Screen Perspective:**
  - On **Host's screen**, Host is Player 1 on the Left; Client is Player 2 on the Right.
  - On **Client's screen**, Client is Player 1 on the Left; Host is Player 2 on the Right!
  - Both players enjoy **0 ms local input latency** controlling their hero on the left side of their own display.
* **Full Local 60 FPS Simulation:**
  - Neither machine freezes physics to Kinematic.
  - Neither machine disables hitboxes.
  - Both computers simulate full 60 FPS physics, jump curves, animations, and particle bursts locally.
* **9-Byte Input Streaming with 3-Frame Buffering:**
  - Transmits compact 9-byte packets (`MoveInput.x`, `MoveInput.y`, and 7-button bitmask).
  - Horizontal movement is automatically inverted for arena symmetry (`MoveInput.x = -packet.MoveInput.x`).
  - Button presses are latched into a 3-frame buffer consumed on read, guaranteeing zero dropped attacks or jumps.
* **Synchronized Round Resets:**
  - When the Host initiates `StartRound()`, a `RoundReset` packet triggers identical synchronized restarts on both screens.

---

## 🚀 Quick Start

### Running the Standalone Release
1. Extract **`Mystic_Combat_Animeverse_Incursion_Playable.zip`**.
2. Run **`Mystic_Combat_Animeverse_Incursion.exe`**.
3. Use the keyboard (Single Player: `WASD` + `F, C, E, Q, X`) or connect up to two gamepads.

### Minimum System Requirements
* **OS:** Windows 10 / 11 (64-bit)
* **Processor:** Intel Core i3 / AMD Ryzen 3 or equivalent
* **Memory:** 2 GB RAM
* **Graphics:** DirectX 11 / 12 compatible GPU
* **Storage:** 200 MB available space

---

<div align="center">

**Developed by [Nikhil Bansal](https://github.com/Nikhil17295) and [Madhav Gaba]**  
*Built with Unity 6 & Universal Render Pipeline 2D*

</div>
