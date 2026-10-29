# 🥋 Mystic Combat: Animeverse Incursion

<div align="center">

![Unity 6](https://img.shields.io/badge/Unity%206-6000.6.7f1-000000?style=for-the-badge&logo=unity&logoColor=white)
![Render Pipeline](https://img.shields.io/badge/Pipeline-Universal%20RP%202D-1E88E5?style=for-the-badge)
![Language](https://img.shields.io/badge/Language-C%23%20%7C%20.NET%20Standard%202.1-512BD4?style=for-the-badge&logo=csharp&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%20PC%20x64-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Netcode](https://img.shields.io/badge/Netcode-Mirror%20LAN%20Networking-E53935?style=for-the-badge)
![Frame Target](https://img.shields.io/badge/Performance-Locked%2060%20FPS-43A047?style=for-the-badge)

<br/>

**A high-octane 16-bit arcade anime fighting game developed in Unity 6.**  
*Featuring authentic pixel art, sub-pixel combat physics, dynamic Doctor Strange mystic relics, adaptive tactical AI, and seamless local & LAN multiplayer.*

[🎮 Key Features](#-key-features) • [👥 Character Roster](#-character-roster) • [🕹️ Controls](#️-controls) • [⚡ Combat Systems](#-combat-systems) • [🌐 Game Modes](#-game-modes) • [🚀 Quick Start](#-quick-start)

---

</div>

## 🌌 Lore & Premise

When the Sorcerer Supreme attempts a forbidden temporal incantation to seal a fractured cosmic rift, spacetime destabilizes. Reality shatters across the anime multiverse, pulling four legendary warriors across dimensions into the sacred **Sanctum Sanctorum Mystic Arena**:

* **Son Goku** (*Dragon Ball*) — Master of godly Ki, turtle school prodigy, driven by an unyielding spirit to transcend every mortal limit.
* **Saitama** (*One Punch Man*) — The caped hero who broke his physical limiter, wielding incomprehensible kinetic might with casual ease.
* **Nezuko Kamado** (*Demon Slayer*) — The demon-awakened sister wielding pyrokinesis and shifting between evasive agility and demonic ferocity.
* **Tsunade Senju** (*Naruto*) — The Fifth Hokage and Legendary Sannin, summoning the titan slug Katsuyu and mastering immortal mitotic regeneration.

Bounded by ancient Taoist runic barriers and governed by the temporal authority of the **Emerald Time Stone**, only one champion will conquer the Incursion!

---

## 👥 Character Roster

Every fighter is built with distinct physical statistics, hitbox geometry, custom pixel animations, unique special skills, and devastating cinematic ultimates. All fighters share a standardized **200 Max HP** balance pool.

```
========================================================================================================================
 CHARACTER          ARCHETYPE                   SPECIAL SKILL (Ki Cost)                 CINEMATIC ULTIMATE (Ki Cost)
========================================================================================================================
 Son Goku           Aerial Rushdown & Blaster   Super Saiyan Form (Active Toggle)       Super Kamehameha (70 Ki)
                                                Golden aura, faster speed & attack      Full-screen destructive beam
                                                draining 2.5 Ki/sec.                    barrage across the arena.

 Saitama            Heavyweight Juggernaut      Consecutive Normal Punches (30 Ki)      Serious Punch (80 Ki)
                                                Rapid 5-hit close-range flurry          Cosmic kinetic strike with
                                                dealing 30 burst damage.                full-screen shockwave cut-in.

 Nezuko Kamado      Stance Shifter & Pyromancer Awakened Demon Form (Active Toggle)     Blood Demon Art: Bakketsu (60 Ki)
                                                Toggles between 60% Chibi dodge-form    Sure-hit blood flame eruption dealing
                                                and 100% Awakened Demon claws.          30 burst + 6 burn dmg; refunds 20 Ki.

 Tsunade Senju      Tank Healer & Earth Shaker  Devastating Ground Breaker (30 Ki)      Katsuyu Summon & Byakugou (60 Ki)
                                                Visceral ground-crack slam dealing      Uninterruptible summon; slow rolling
                                                30 damage to grounded foes.             tidal wave + 10s 50% damage reduction.
========================================================================================================================
```

---

### 🥋 Son Goku (`goku`)
> *"I am the answer to all living things that cry out for peace!"*

* **Combat Profile:** All-around martial artist with superior jump arcs, high-speed footsies, and long-range beam dominance.
* **Base Statistics:**
  * **Max Health:** 200 HP
  * **Movement Speed:** 5.2 (High mobility)
  * **Jump Force:** 12.5 | Gravity Scale: 3.0
  * **Meditation Channel Rate:** +8.0 Ki/s
* **Special Skill — Super Saiyan Form (Active Toggle):**
  * Consumes 2.5 Ki per second to sustain.
  * Emits a radiant golden energy aura with active pixel sparks.
  * Enhances movement speed and adds bonus impact power to all physical strikes.
* **Cinematic Ultimate — Super Kamehameha (70 Ki):**
  * Goku braces into his signature stance, chanting as azure Ki condenses.
  * Fires a full-screen energy beam that engulfs the battlefield, inflicting 70 heavy damage and pushing the opponent back.

---

### 🥊 Saitama (`saitama`)
> *"I'm just a guy who's a hero for fun."*

* **Combat Profile:** Immovable powerhouse who dominates close-quarters neutral. Possesses natural mass conferring 20% passive knockback resistance.
* **Base Statistics:**
  * **Max Health:** 200 HP
  * **Movement Speed:** 4.8 | Jump Force: 12.0
  * **Passive Trait:** **Limiter Shattered** — 20% innate knockback resistance against all enemy attacks.
  * **Meditation Channel Rate:** +5.0 Ki/s
* **Special Skill — Consecutive Normal Punches (30 Ki, 15s Cooldown):**
  * Delivers a lightning-fast barrage of 5 jabs in rapid succession (6 damage per hit = 30 total damage).
  * Shreds enemy spacing and applies heavy hitstop on connection.
* **Cinematic Ultimate — Serious Series: Serious Punch (80 Ki):**
  * Triggers a dramatic freeze-frame cinematic camera zoom with black-and-white color flash.
  * Saitama steps forward and delivers a singular cosmic punch, creating a colossal shockwave across the stage dealing 80 decisive damage.

---

### 🌸 Nezuko Kamado (`nezuko`)
> *"Mmm-hmm!"*

* **Combat Profile:** Dual-stance dynamic brawler able to toggle between miniature evasion and awakened demonic rushdown, finished with sure-hit pyrokinesis.
* **Base Statistics:**
  * **Max Health:** 200 HP
  * **Base Form Scale:** 60% Miniature Chibi (Low hurtbox)
  * **Demon Form Scale:** 100% Full Fighter (High offensive reach)
  * **Movement Speed:** 5.4 (Chibi) / 5.0 (Demon)
* **Special Skill — Awakened Demon Form (Active Toggle):**
  * **Small Chibi Form (Default):** Compressed 60% hitbox allows high attacks and tall projectiles to cleanly whiff overhead. Ideal for dodging and neutral maneuvering.
  * **Demon Form (Awakened):** Grows to full height, developing her demonic horn and vein markings. Enhances claw reach, increases melee damage from 5 to 10 on heavy strikes, and consumes 2 Ki/s.
* **Cinematic Ultimate — Blood Demon Art: Exploding Blood / Bakketsu (60 Ki, 10s Cooldown):**
  * **Sure-Hit Accuracy:** Guaranteed tracking impact anywhere across the arena floor; opponent cannot simply walk under or run away.
  * Nezuko clenches her fist, igniting her demonic blood in a massive erupting crimson-pink pyrotechnic column directly over the opponent.
  * Inflicts **30 burst damage** upon detonation, followed by **3 lingering burn ticks** (2 damage/tick = 6 burn damage).
  * **Ki Refund:** Immediately refunds **+20 Ki** upon successful connection!

---

### 🍃 Tsunade Senju (`tsunade`)
> *"People become stronger because they have memories they can't forget!"*

* **Combat Profile:** Legendary Sannin brawler combining ground-shattering chakra strikes with unmatched survivability and persistent battlefield summon control.
* **Base Statistics:**
  * **Max Health:** 200 HP
  * **Movement Speed:** 4.9 | Jump Force: 12.0
  * **Meditation Channel Rate:** +5.0 Ki/s
* **Special Skill — Devastating Ground Breaker / Earth Shaker (30 Ki, 20s Cooldown):**
  * Tsunade leaps into the air and hammers her chakra-infused fist directly into the floor.
  * Triggers a violent ground shockwave, bursting dual dust fissures, intense camera shake, and impact flash.
  * Deals **30 damage** to grounded opponents within the fissure zone.
* **Cinematic Ultimate — Katsuyu Summon & Creation Rebirth / Byakugou (60 Ki, 20s Cooldown):**
  * **Uninterruptible Casting (Super Armor):** Immune to hitstun and knockback during her summon invocation stance.
  * **Katsuyu Summon (Tidal Wave):** Summons a colossal 2.4x scale slug companion that slowly and majestically crawls forward across the entire arena (speed 1.6f, lifetime 8.5s). Katsuyu hits for 30 damage with heavy hitstop, but **does not dissolve**—she rolls straight through the enemy like a tidal wave across the stage!
  * **Byakugou Healing Stage (10 Seconds):** Instantly envelops Tsunade in a radiant emerald medical chakra aura:
    * Regenerates **+2 HP per second**.
    * Generates **+2 Ki per second**.
    * Confers **50% incoming damage reduction** against all enemy strikes.

---

## 🕹️ Controls

The game supports single-keyboard local play, dual-keyboard split play, and native USB/Bluetooth gamepads (Xbox, PlayStation, 8BitDo).

| Action | Player 1 (Keyboard) | Player 2 (Numpad / Arrow Keys) | Gamepad (Xbox / PS) |
| :--- | :--- | :--- | :--- |
| **Move Left / Right** | `A` / `D` | `Left Arrow` / `Right Arrow` | `D-Pad` or `Left Stick` |
| **Jump** | `W` | `Up Arrow` | `Button South` (`A` / `Cross`) |
| **Crouch / Guard** | `S` | `Down Arrow` | `D-Pad Down` or `Stick Down` |
| **Light Attack** | `J` | `Numpad 1` | `Button West` (`X` / `Square`) |
| **Heavy Attack** | `K` | `Numpad 2` | `Button North` (`Y` / `Triangle`) |
| **Special Skill** | `U` | `Numpad 4` | `Right Bumper` (`RB` / `R1`) |
| **Cinematic Ultimate** | `I` | `Numpad 5` | `Right Trigger` (`RT` / `R2`) |
| **Meditate (Charge Ki)**| `L` | `Numpad 3` | `Left Bumper` (`LB` / `L1`) |
| **Pause / Menu** | `Escape` | `Escape` | `Start` / `Options` |

---

## ⚡ Combat Systems

### 1. 200 Max HP & Arcade Ghost Health Bars
All fighters enter the arena with **200 Max HP**, providing longer, deeper competitive exchanges with ample opportunity for comebacks, chain cancels, and defensive spacing. Health bars feature dynamic 2-tone rendering with a warning-red ghost bar visualizing recent burst combo damage.

### 2. 3-Tier Ki Energy & Red Zone Exhaustion
* **Ki Gauge (0 - 100 Ki):** Powers special skills, transformations, and cinematic ultimates.
* **Safe Zone:** Above skill threshold, abilities can be cast freely.
* **Active Meditation Stance:** Holding the Meditate key allows fighters to channel raw spiritual energy (+8 Ki/s for Goku, +5 Ki/s for others).
* **Red Zone Exhaustion State:** Over-expending energy drops the fighter into exhaustion ($40\%$ slowed movement, disabled jumps, zero attack power, and +0.12s longer hitstun vulnerability).

### 3. Visceral Combat Juice & Frame Advantage
* **Hitstop Freeze Frames:** Dynamic micro-pauses (0.05s - 0.12s) freeze the attacker and defender on contact to deliver tangible arcade weight.
* **Impact Sparks & Flash:** Directional particle bursts and whole-screen contrast flashes emphasize heavy attacks and counter-hits.
* **Screen Shake:** Multi-tier camera impulse shaking calibrated to each move's kinetic power.
* **Chain Canceling:** Landing a Light Attack opens an immediate cancellation window into Heavy Attack or Special Skill.

---

## 🌐 Game Modes

1. **Arcade Mode (1P vs CPU):**
   * Battle against an adaptive CPU opponent featuring intelligent spacing, tactical meditation, whiff punishment, and boundary awareness.
   * Scaled across 3 distinct AI difficulty presets: **Easy**, **Hard**, and **Extreme**.
2. **Local Versus (1P vs 2P):**
   * Classic couch versus on a shared screen supporting split keyboard or mixed keyboard + controller.
3. **LAN Multiplayer (Mirror Netcode):**
   * Built on Mirror networking for high-speed local area network matches.
   * Host a lobby or connect directly via local IP address.

---

## 🚀 Quick Start

### Running in Unity Editor
1. Open **Unity Hub** and add the project folder:  
   `Unity Version: 6000.6.7f1 (6000.6.7f1)`
2. Ensure the active build target is set to **PC, Mac & Linux Standalone (Windows x64)**.
3. Open `Assets/Scenes/MainMenuScene.unity` and press **Play** in the editor.

---

<div align="center">

**Developed by [Nikhil Bansal](https://github.com/Nikhil17295) and [Madhav Gaba]**  
*Built with Unity 6 & Universal Render Pipeline 2D*

<b>🥋 Mystic Combat: Animeverse Incursion © 2026. Built with passion for competitive anime arcade fighting.</b>

</div>
