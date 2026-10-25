# 🛡️ Mystic Combat — Internal Team Knowledge Base & Viva Defense Hub

Welcome to the internal engineering and study documentation for **Mystic Combat: Animeverse Incursion**.

> [!IMPORTANT]
> **Two-Tier Documentation System:**
> * **Tier 1 (This Folder — `Docs/`):** **Internal Team Master Guide.** Written specifically for you and your teammate to study, master, and defend the codebase. Contains beginner-friendly explanations, Godot-to-Unity conversions, deep architectural diagrams, and 27 comprehensive viva/interview answers.
> * **Tier 2 (Repository Root — `README.md`):** **External & Judge Presentation.** Written for hackathon evaluators, judges, recruiters, and players downloading the game. Focuses on the elevator pitch, lore, gameplay showcases, move lists, and technical highlights.

---

## 📚 Document Index & Curriculum

| Document | Core Topics | Why You Should Read It |
| :--- | :--- | :--- |
| [**01. Unity for Beginners**](01_UNITY_FOR_BEGINNERS.md) | Game Loops, GameObjects vs Components, 2D Rigidbody Physics, Coroutines vs `await`, and **Godot $\rightarrow$ Unity translation**. | Teammates new to game development or transitioning from Godot. Explains *how games actually work* in plain language without jargon. |
| [**02. System Architecture**](02_ARCHITECTURE.md) | MVC Architecture, Model Grimoire JSON, Finite State Machine, Event-Driven Decoupled HUD, Round State Diagram. | Technical system flow. Read this to understand how all scripts connect and talk to each other without spaghetti code. |
| [**03. Feature Breakdown**](03_FEATURE_BREAKDOWN.md) | Hitbox calculations, animation speed tuning, Doctor Strange rotating Tao Mandala, dual-gem Best-of-3 score indicators, match point relic pulse. | Feature-by-feature review. Read this to explain what every single feature does and how it was constructed. |
| [**04. Viva & Interview Prep**](04_VIVA_AND_INTERVIEW_PREP.md) | **27 Comprehensive Jury Defense Q&As**, technical justifications, architecture trade-offs, and a **Live Judge Demo Script**. | **Your secret weapon during hackathon evaluation.** Practice these questions with your teammate so you can answer any professor or judge confidently. |
| [**05. Dev Log & Bug Postmortems**](05_DEV_LOG_AND_BUGS.md) | Real-world bug postmortems (0ms round reset freeze, Unity prefab disk pointer trap, Unity 6 TMP deprecation, white halo chroma-keying). | Proves to judges that you didn't just write code—you debugged complex engine architectural pitfalls like seasoned engineers. |

---

## 🧭 Teammate Onboarding & Study Roadmap (Zero to Hero)

If your teammate has never worked on this codebase before, follow this 3-day study plan:

### 📅 Day 1: Foundations & Mental Model
1. Read [**01_UNITY_FOR_BEGINNERS.md**](01_UNITY_FOR_BEGINNERS.md) from start to finish.
2. Focus on:
   - What `Update()` vs `FixedUpdate()` does.
   - Why `Rigidbody2D` velocities must only be changed in `FixedUpdate`.
   - How Unity components correspond to Godot nodes.
   - Why Coroutines (`IEnumerator`) are necessary for dramatic fighting game pauses (1.8s K.O. freeze).

### 📅 Day 2: Code Architecture & Visual HUD
1. Read [**02_ARCHITECTURE.md**](02_ARCHITECTURE.md) and [**03_FEATURE_BREAKDOWN.md**](03_FEATURE_BREAKDOWN.md).
2. Trace the data flow:
   - How `goku.json` and `saitama.json` get loaded by `CharacterDataLoader.cs`.
   - How `FighterController` moves between `Idle`, `Walking`, `Jumping`, `Attacking`, and `Meditating`.
   - How `HealthSystem.OnHealthChanged` notifies `HUDController` via C# events with 0ms delay.
   - How the central Match Point relic ignites into the authentic golden crystal on match point.

### 📅 Day 3: Bug Postmortems & Mock Viva Defense
1. Read [**05_DEV_LOG_AND_BUGS.md**](05_DEV_LOG_AND_BUGS.md):
   - Understand the **0ms Round Reset bug** and why the 1.8s coroutine solved it.
   - Understand the **Prefab Disk Pointer bug** and how `IsInActiveScene()` resolved it.
2. Rehearse with [**04_VIVA_AND_INTERVIEW_PREP.md**](04_VIVA_AND_INTERVIEW_PREP.md):
   - Have one teammate ask the questions while the other answers.
   - Walk through the **5-step Live Judge Demonstration Script** in Unity Play Mode!

---

## 🎯 Key Hackathon Presentation Principles

1. **Be Proud of the Data-Driven Design:** When judges ask how difficult it is to add a 3rd character (like Vegeta or Genos), tell them: *"It requires 0 code changes. We just create a `vegeta.json` file in `Assets/Data/Characters/` with stats and frame references."*
2. **Highlight the Decoupled Architecture:** Emphasize that the HUD doesn't poll health every frame; it uses C# event actions (`Signals` in Godot terminology) for sub-millisecond reaction times.
3. **Showcase the Autonomous Round State Machine:** Demonstrate Round 1 $\rightarrow$ 1.8s pause $\rightarrow$ Round 2 $\rightarrow$ 1-1 tie $\rightarrow$ Round 3 (Final Round Match Point) with live visual feedback from the dual gem indicators and pulsing center golden relic.
