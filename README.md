# 🥋 Mystic Combat: Animeverse Incursion

<div align="center">

![Unity 6](https://img.shields.io/badge/Unity%206-6000.6.7f1-000000?style=for-the-badge&logo=unity&logoColor=white)
![Render Pipeline](https://img.shields.io/badge/Pipeline-Universal%20RP%202D-1E88E5?style=for-the-badge)
![Language](https://img.shields.io/badge/Language-C%23%20%7C%20.NET%20Standard%202.1-512BD4?style=for-the-badge&logo=csharp&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%20PC%20x64-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Multiplayer](https://img.shields.io/badge/Netcode-Mirror%20LAN%20Networking-E53935?style=for-the-badge)
![Frame Target](https://img.shields.io/badge/Performance-Locked%2060%20FPS-43A047?style=for-the-badge)

<br/>

**A high-octane 16-bit arcade anime fighting game developed in Unity 6.**  
*Featuring authentic pixel art, sub-pixel combat physics, dynamic Doctor Strange mystic relics, adaptive tactical AI, and seamless local & LAN multiplayer.*

[🎮 Key Features](#-key-features) • [🕹️ Controls](#️-controls) • [👥 Character Roster](#-character-roster) • [🌐 Game Modes](#-game-modes) • [⚙️ Architecture](#-technical-architecture) • [🚀 Quick Start](#-quick-start)

---

</div>

## 🌌 Lore & Setting

When the Sorcerer Supreme attempts a forbidden temporal incantation to seal a fractured cosmic rift, spacetime destabilizes. Reality fractures across the multiverse, violently pulling two of the strongest martial forces in existence into the **Sanctum Sanctorum Mystic Arena**:

* **Son Goku** — Master of godly Ki, turtle school martial artist, driven by an unyielding spirit to push past every limit.
* **Saitama** — The caped hero who shattered his physical limiter, wielding incomprehensible kinetic power with nonchalant ease.

Bounded by ancient Taoist runic barriers and governed by the temporal authority of the **Emerald Time Stone**, only one warrior will emerge victorious.

---

## 🎮 Key Features

### 🥊 1. Responsive 16-Bit Arcade Combat Engine
* **Fluid Frame-Data Driven Combat:** Zero input lag, tight cancellation windows, and distinct Startup, Active, and Recovery states for high-level footsies, spacing, and whiff punishment.
* **Sub-Pixel Hitbox Precision:** Dedicated 2D trigger colliders on a deterministic physics layer matrix with automatic hit-target caches to eliminate duplicate hit registration during active swings.
* **Authentic Handcrafted Visuals:** Pixel-perfect animation loops covering idle breathing, walking, crouching guards, aerial jumps/falls, impact hitstuns, custom knockdown recoveries, and victory poses.

### ⚡ 2. Tactical Ki & Exhaustion Mechanics
* **Dynamic 3-Tier Energy System:** Ki governs special abilities and stamina.
  - **Safe Zone:** Above your threshold buffer, specials can be performed freely.
  - **Dynamic Threshold Markers:** Real-time HUD indicators calibrated to each fighter's exact move costs.
  - **Red Zone Exhaustion State:** Over-expending energy drops the fighter into exhaustion ($40\%$ slowed movement, disabled jumps, zero attack power, and vulnerability to punishes).
* **Active Meditation Stance:** Hold the Meditate key to channel raw spiritual energy ($+25\text{ Ki/s}$), creating high-risk, high-reward tactical resets during spacing neutral.

### 🔮 3. Doctor Strange Taoist Tournament HUD
* **Spinning Tao Spell Mandala:** Concentric runic green mandala continuously revolving at $22^\circ/\text{s}$ behind the central clock dial.
* **The Emerald Time Stone Relic:** Faceted, illuminated relic mounted directly atop the countdown dial, tracking round duration.
* **Championship Match Point Relic:** Central golden crest begins hollow during neutral rounds, dynamically igniting into a pulsing crystal when either fighter reaches Match Point.
* **Arcade Ghost Health Bars:** High-visibility 2-tone health bars with smoothly trailing warning-red ghost meters that visualize recent chip and combo damage.
* **Dual Sapphire Gem Indicators:** Socketed win indicators that illuminate upon round victories.

### 🧠 4. Adaptive AI with Tactical Spacing
* **Turn-Taking Pacing:** CPU bots execute single, readable strikes with full recovery windows, eliminating unfair frame-instant machine loops.
* **Intelligent Tactical Retreat:** Bots actively disengage and backpedal across the arena after exchanges to grant players breathing room to practice, position, and charge Ki.
* **Anti-Corner Edge Awareness:** AI monitors stage boundaries and executes athletic jumps toward center stage when backed against the arena wall.
* **3 Scaled Difficulty Presets:**
  - **Easy:** Accessible learning profile with generous retreat windows ($1.8\text{s} - 2.8\text{s}$) and relaxed reaction times ($0.45\text{s}$).
  - **Hard:** Competitive neutral spacing with balanced whiff punishing and tactical meditation.
  - **Extreme:** Tournament-master difficulty featuring rapid reaction ($0.04\text{s}$), reactive jump escapes, and crouch-ducking.

---

## 👥 Character Roster

```
<<<<<<< Updated upstream
====================================================================================================
  CHARACTER           SPECIAL SKILL                         CINEMATIC ULTIMATE
====================================================================================================
  Son Goku            Kaioken Ki Burst (30 Ki)              Kamehameha Wave (70 Ki)
                      Rapid horizontal body dash with       Iconic charge-up & full-screen energy
                      invulnerability startup frames.       beam, triggering Super Saiyan state!

  Saitama             Consecutive Normal Punches (30 Ki)    Serious Punch (80 Ki)
                      High-frequency multi-punch barrage    Cinematic dramatic camera cut-in, shockwave
                      shredding enemy guard.                impact, and screen-blackout finishing blow.
====================================================================================================
=======
Max Health: 200 HP  |  Move Speed: 5.2 (Fast)  |  Jump Force: 12.5  |  Gravity Scale: 3.0
Meditation Channel Rate: +8.0 Ki/s  |  Ki Gain on Damage Taken: 1.5 Ki per hit
>>>>>>> Stashed changes
```

### 🥋 Son Goku
* **Playstyle:** All-rounder martial artist with superior aerial mobility, long-range beam pressure, and Super Saiyan transformation.
* **Special Skill — Kaioken Burst:** Teleports forward in a red kinetic streak, closing gaps instantaneously.
* **Ultimate — Super Kamehameha:** Channels energy into a full-screen destructive beam. If Goku lands the beam, he ascends into **Super Saiyan** form, gaining radiant gold particle auras and increased mobility.

### 🥊 Saitama
* **Playstyle:** Heavy-impact rushdown powerhouse with unmatched single-strike devastation and heavy guard damage.
* **Special Skill — Consecutive Normal Punches:** Unleashes a rapid flurry of jabs dealing concentrated close-range damage.
* **Ultimate — Serious Series: Serious Punch:** Initiates a dramatic freeze-frame cinematic cut-in, executing a devastating punch that shakes the entire screen with cosmic kinetic force.

---

## 🕹️ Controls

<<<<<<< Updated upstream
The game supports simultaneous Dual Keyboard and XInput / DirectInput Gamepads.
=======
```
Max Health: 200 HP  |  Move Speed: 4.4 (Heavy)  |  Jump Force: 10.8  |  Gravity Scale: 3.0
Meditation Channel Rate: +5.0 Ki/s  |  Ki Gain on Damage Taken: 1.5 Ki per hit
```
>>>>>>> Stashed changes

| Action | Player 1 (Keyboard) | Player 2 (Keyboard) | Gamepad (Xbox / PlayStation) |
| :--- | :--- | :--- | :--- |
| **Move Left / Right** | <kbd>A</kbd> / <kbd>D</kbd> | <kbd>←</kbd> / <kbd>→</kbd> | Left Analog Stick / D-Pad |
| **Jump** | <kbd>W</kbd> | <kbd>↑</kbd> | Button South (<kbd>A</kbd> / <kbd>×</kbd>) |
| **Crouch / Guard** | <kbd>S</kbd> | <kbd>↓</kbd> | Down on Analog Stick / D-Pad |
| **Light Attack** | <kbd>J</kbd> | <kbd>Num 1</kbd> | Button West (<kbd>X</kbd> / <kbd>□</kbd>) |
| **Heavy Attack** | <kbd>K</kbd> | <kbd>Num 2</kbd> | Button North (<kbd>Y</kbd> / <kbd>△</kbd>) |
| **Special Skill** | <kbd>U</kbd> | <kbd>Num 4</kbd> | Right Shoulder (<kbd>RB</kbd> / <kbd>R1</kbd>) |
| **Ultimate Attack** | <kbd>I</kbd> | <kbd>Num 5</kbd> | Right Trigger (<kbd>RT</kbd> / <kbd>R2</kbd>) |
| **Meditate (Ki Charge)** | <kbd>L</kbd> | <kbd>Num 3</kbd> | Left Trigger (<kbd>LT</kbd> / <kbd>L2</kbd>) |
| **Pause / Menu** | <kbd>Escape</kbd> | <kbd>Escape</kbd> | <kbd>Start</kbd> / <kbd>Options</kbd> |

---

## 🌐 Game Modes

### 1. Solo Arcade Campaign
Challenge the CPU AI across **Easy**, **Hard**, or **Extreme** difficulties. Features adaptive spacing footsies, AI meditation management, and dynamic round scaling.

### 2. Local 2-Player Versus
Classic couch multiplayer on a shared screen. Both players select their fighters and battle under official tournament Best-of-3 round rules.

### 3. Local Area Network (LAN) Multiplayer
High-performance multiplayer powered by Mirror Networking:
* **Host Match:** Create a dedicated tournament server on your local Wi-Fi or Ethernet network.
* **Join Match:** Input the host's local IP address or use LAN Discovery to connect instantly.
* **Synchronized Best-of-3 Lifecycle:** Networked round timers, authoritative hit confirmation, health/Ki sync, and post-match rematch negotiation.

---

## ⚙️ Technical Architecture

* **Decoupled Grimoire JSON Balancing:** Character attributes (health, attack damage, walk velocity, jump arcs, Ki costs) are fully externalized in `StreamingAssets/Characters/*.json` for real-time balancing without recompilation.
* **Deterministic Event-Driven State Machines:** Fighters operate via decoupled hierarchical state machines communicating through strict C# delegates and events (`OnHealthChanged`, `OnEnergyChanged`, `OnThresholdsChanged`, `OnStateChanged`).
* **Automated Standalone Packaging Pipeline:** Integrated `IPostprocessBuildWithReport` build processor automatically sanitizes asset payloads upon build completion, ensuring standalone executables run out-of-the-box with zero dependency missing errors.

---

## 🚀 Quick Start

### Playing the Standalone Release
1. Extract `Mystic_Combat_Animeverse_Incursion_Playable.zip`.
2. Launch **`Mystic_Combat_Animeverse_Incursion.exe`**.
3. Use the keyboard or plug in up to two gamepads to start battling immediately!

### System Requirements
* **OS:** Windows 10 / 11 (64-bit)
* **Processor:** Intel Core i3 / AMD Ryzen 3 or equivalent
* **Memory:** 2 GB RAM
* **Graphics:** DirectX 11 / 12 compatible GPU
* **Storage:** 200 MB available space

---

<div align="center">

*Mystic Combat: Animeverse Incursion is developed for high-speed martial arts gaming.*  
**Created with Unity 6 & Universal Render Pipeline.**

</div>
