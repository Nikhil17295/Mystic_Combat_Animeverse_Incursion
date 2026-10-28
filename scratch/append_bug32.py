bug32 = r"""
---

### Bug 32: Goku Legacy Skill Animation Cleanup & Instant Direct-to-SSJ Idle Transition

- **Symptom:**
  1. When triggering Goku's Skill (Key `E` / `U`), Goku played an outdated charge/strike animation before transforming, rather than ascending directly into his Super Saiyan Idle stance.
  2. Leftover skill sprite files (`skill_0..3.png`) lingered in Goku's runtime `Frames/` directory, creating technical debt and confusing the automatic sequence loader.
- **Root Cause:**
  1. **Residual Prototype Slices:** Goku's `Assets/Sprites/Fighters/Goku/Frames/` directory still contained leftover `skill_0.png` through `skill_3.png` and their corresponding `.meta` files from early prototype combat passes.
  2. **Loader Sequence Auto-Discovery:** `FighterAnimator.cs` dynamically loaded all indexed `skill_*.png` files into `skillFrames`. If the fighter transitioned to `FighterState.Skill`, the animator played this old sequence.
  3. **State Transition Lockout:** `FighterController.cs` originally assigned an `attackTimer = 0.65f` and called `TransitionTo(FighterState.Skill)`, which locked Goku into an attack state for over half a second before allowing idle.
- **Resolution:**
  1. **Purged Legacy Sprite Assets:** Deleted `skill_0..3.png` and `skill_0..3.png.meta` from `Assets/Sprites/Fighters/Goku/Frames/` (backed up in `Frames_Backup/`).
  2. **Direct Super Saiyan Idle Transition:** In `FighterController.cs`, Goku's Skill pathway was rewritten to immediately deactivate `attackHitbox`, zero out all attack timers (`attackTimer = 0f`, `attackStartupTimer = 0f`, `attackActiveTimer = 0f`), and immediately call `TransitionTo(FighterState.Idle)`.
  3. **Zero-Delay Visual Transformation:** Because `controller.IsSuperSaiyan` is asserted true before `TransitionTo(FighterState.Idle)`, `FighterAnimator.UpdateAnimation()` queries `ssjIdleFrames` on that exact frame, seamlessly snapping Goku directly into his iconic Super Saiyan golden idle stance with zero intermediate frames, zero hitbox latency, and zero damage dealt.
  4. **Project Assembly Alignment:** Added missing `KamehamehaBeamVFX.cs` compile item to `Assembly-CSharp.csproj`, ensuring automated verification tools and IDE build systems pass with **0 errors**.
"""

with open('Docs/05_DEV_LOG_AND_BUGS.md', 'a', encoding='utf-8') as f:
    f.write(bug32)
print('Appended Bug 32 to Docs/05_DEV_LOG_AND_BUGS.md')
