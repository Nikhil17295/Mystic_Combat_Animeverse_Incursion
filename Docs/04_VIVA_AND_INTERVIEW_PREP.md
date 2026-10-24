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

## 3. Live Demo Checklist for Judges

When demonstrating the game to evaluators:
1. **Show the Console:** Point out `[CharacterDataLoader]` loading `goku.json` and `saitama.json` at startup to prove the data-driven architecture.
2. **Show Auto-Facing:** Jump over Saitama with Goku and show both characters flipping smoothly.
3. **Show Meditation:** Hold the meditate key (`T`) and show the fighter rooting in place to channel mystic Ki.
4. **Show Arena Colliders:** Run into the arena boundary walls to show solid 2D collision resolution.
