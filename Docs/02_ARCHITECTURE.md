# 02. System Architecture & Design Patterns

> **Audience:** Core team developers, code reviewers, and viva / project defense evaluators.

---

## 1. High-Level Architectural Diagram

Mystic Combat is designed around **decoupled, modular components** communicating via **Data Contracts** and **C# Events**. No monolithic "Player" script exists.

```mermaid
graph TD
    subgraph Data Layer ["Data Layer (Grimoire)"]
        JSON["goku.json / saitama.json<br/>(External JSON Files)"]
        Loader["CharacterDataLoader.cs"]
        DataClass["CharacterData.cs<br/>(Plain C# POCO)"]
        JSON --> Loader --> DataClass
    end

    subgraph Input Layer ["Input Layer"]
        Hardware["Shared Keyboard"]
        InputHandler["PlayerInputHandler.cs<br/>(Reads P1 / P2 keys)"]
        Hardware --> InputHandler
    end

    subgraph Fighter Entity ["Fighter GameObject"]
        FC["FighterController.cs<br/>(State Machine & Movement)"]
        Health["HealthSystem.cs<br/>(HP & Death)"]
        Energy["EnergySystem.cs<br/>(Ki & Red Zone)"]
        Hurtbox["HurtboxController.cs<br/>(Damage Receiver)"]
        Hitbox["HitboxController.cs<br/>(Damage Dealer)"]
    end

    subgraph Core & UI ["Core & UI Layer"]
        Match["MatchManager.cs<br/>(Rounds & Timer)"]
        HUD["HUDController.cs<br/>(Health & Ki Bars)"]
    end

    DataClass -->|Applies stats on spawn| FC
    DataClass -->|Initializes Max HP| Health
    DataClass -->|Sets Red Zone tuning| Energy

    InputHandler -->|MoveInput, Jump, Crouch, Meditate| FC

    Health -->|Event: OnHealthChanged| HUD
    Health -->|Event: OnDeath| Match
    Energy -->|Event: OnKiChanged, OnRedZone| HUD
    Energy -->|IsInRedZone status| FC

    Hitbox -.->|Trigger overlap| Hurtbox
    Hurtbox -->|Calls TakeDamage| Health
```

---

## 2. Key Architecture Decisions

### Decision 1: The "Grimoire Architecture" (Data-Driven JSON)
**The Problem:** In traditional hackathon games, stats (HP, speed, damage, frame timings) are hardcoded directly into C# scripts or Inspector fields. Modifying balance requires editing code and waiting for Unity to recompile.
**Our Solution:** Character stats are stored in standalone external JSON files (`Assets/Data/Characters/*.json` and `StreamingAssets/Characters/*.json`).
- **Advantage 1:** Designers can rebalance characters (e.g. nerf Saitama's speed from `4.5` to `4.2`) without touching a single line of C#.
- **Advantage 2:** New characters can be added to the game simply by dropping a new `.json` file and a spritesheet into the project.
- **Advantage 3:** Explicit scoring bonus in the hackathon criteria for external data-driven architectures with runtime validation.

### Decision 2: Single Responsibility Principle (Separation of Concerns)
Instead of a giant 2,000-line `Player.cs`, responsibilities are strictly separated:

| Component | What it DOES | What it DOES NOT DO |
| :--- | :--- | :--- |
| **`PlayerInputHandler`** | Reads keyboard keys (`W/A/S/D` or `Arrows`). | Does not move the player or know what an attack is. |
| **`FighterController`** | Manages movement physics, states, jumping, and auto-facing. | Does not calculate damage or update UI. |
| **`HealthSystem`** | Stores HP, handles `TakeDamage()` and `Heal()`. | Does not know what attack hit the player. |
| **`EnergySystem`** | Stores Ki (0–100), handles meditation, manages 5-second Red Zone timer. | Does not draw UI or move the player. |
| **`HUDController`** | Updates UI bars and text. | Does not store or modify game variables. |

### Decision 3: Event-Driven Communication (Observer Pattern)
Components communicate through C# `event Action` rather than direct hard references.

**Example: When Goku takes damage:**
1. `HealthSystem.TakeDamage(15)` reduces HP from 100 to 85.
2. `HealthSystem` fires `OnHealthChanged?.Invoke(85, 100);`.
3. `HUDController` is listening to this event, so it slides the health bar down to 85%.
4. `HealthSystem` has **zero reference** to `HUDController`. If the HUD is disabled or deleted, the game will never crash!

---

## 3. Finite State Machine (FSM)

Fighting games are defined by strict state rules (e.g., *you cannot jump while being knocked back, and you cannot attack while exhausted*).

### State Transition Diagram:

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Walking : Move input != 0
    Walking --> Idle : Move input == 0

    Idle --> Jumping : Jump pressed & Grounded
    Walking --> Jumping : Jump pressed & Grounded
    Jumping --> Falling : Y velocity < 0
    Falling --> Idle : Hits ground

    Idle --> Crouching : Crouch held & Grounded
    Crouching --> Idle : Crouch released

    Idle --> Meditating : Meditate held & Grounded
    Meditating --> Idle : Meditate released

    Idle --> Exhausted : Ki <= 10 (Red Zone entered)
    Walking --> Exhausted : Ki <= 10 (Red Zone entered)
    Exhausted --> Meditating : Meditate held (Faster recovery!)
    Exhausted --> Idle : Red Zone timer expires (Recovers 20 Ki)
```

### Why FSM is Critical for Fighting Games:
- Prevents invalid moves (e.g., cannot air-dash or jump when already in the air).
- Controls animation synchronization.
- Enforces the **Red Zone penalty**: when in `Exhausted` state, attack inputs are ignored, and horizontal velocity is clamped to $35\%$ of normal move speed.
