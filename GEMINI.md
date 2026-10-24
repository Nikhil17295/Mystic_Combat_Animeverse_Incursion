# Antigravity Pair-Programming & Documentation Rules
Project: Mystic Combat (~ Animeverse Incursion ~)

## Core Working Protocol

1. **Discuss Before Implementing:**
   - Always explain *what* will be built, *why*, and *how* it works before writing any code.
   - Always get user confirmation before modifying or adding scripts.
   - Relate Unity patterns to Godot concepts whenever relevant (e.g., Nodes vs Components, Signals vs C# Events).

2. **Git Control:**
   - Never run `git commit` or `git push` automatically. The user manages Git commits themselves.

3. **Continuous Documentation & Viva Prep:**
   - Whenever a feature, bug fix, or architectural change is introduced, update the corresponding file in `Docs/`:
     - `Docs/01_UNITY_FOR_BEGINNERS.md` (Game dev & Unity basics for beginners)
     - `Docs/02_ARCHITECTURE.md` (System design & architecture)
     - `Docs/03_FEATURE_BREAKDOWN.md` (Detailed feature documentation)
     - `Docs/04_VIVA_AND_INTERVIEW_PREP.md` (Hackathon defense & viva questions/answers)
     - `Docs/05_DEV_LOG_AND_BUGS.md` (Pitfalls, common rookie mistakes, bug postmortems)
   - Ensure explanations are beginner-friendly so that non-developer teammates can read and explain the project confidently during evaluations/vivas.
