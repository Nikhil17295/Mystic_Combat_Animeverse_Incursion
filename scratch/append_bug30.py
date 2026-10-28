path = r"Docs\05_DEV_LOG_AND_BUGS.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

bug30 = """

---

### Bug 30: Saitama Celebratory Jump Arc Restoration, Single-Turn Round Transition & Ki Aura Particle De-Cluttering

- **Symptom:**
  1. In a previous fix, Saitama's victory animation was flattened to a static standing pose because the previous loop caused repetitive "pogo-sticking." However, Saitama's authentic celebratory gesture was supposed to include his dynamic jump.
  2. Round end pauses were locked to a hardcoded 4.0-second delay in `MatchManager`, forcing players to wait through repetitive animation loops before Round 2 or Final Round began.
  3. Rising vertical Ki particle streaks in `FighterKiAura.cs` cluttered character silhouettes and clashed with the clean, razor-sharp 16-bit pixel-art presentation.
- **Root Cause:**
  1. **Premature Jump Removal:** Rather than running the victory celebration as a single-turn sequence, the previous fix simply stripped the jumping frames from `victory_*.png`.
  2. **Looping Non-Loopable Keyframes:** `FighterAnimator.cs` executed `FighterState.Victory` via `PlayLoop`. Because victory animations contain transition poses (squat $\\to$ leap $\\to$ land $\\to$ pose), looping them continuously caused the character to bounce up and down endlessly until the timer ran out.
  3. **Decoupled Round Reset Timer:** `MatchManager.cs` used a rigid serialized delay (`roundEndPauseDuration = 4.0f`) instead of observing the victor's actual single-turn animation completion.
  4. **Over-Engineering with Particle Systems:** Spawning a runtime `ParticleSystem` (`Aura_Rising_Streaks`) overlaid unscaled particle textures on top of hand-drawn pixel sprites.
- **Resolution:**
  1. **9-Frame Mathematical Jump Arc (`victory_0..8.png`):**
     - Sliced and standardized all 9 authentic frames on a $160 \\times 160\\text{px}$ canvas.
     - Anchored frames 0–2 (squat) at $Y = 135$, frame 3 (takeoff) at $Y = 120$ ($15\\text{px}$ lift), frame 4 (apex mid-air punch) at $Y = 104$ ($31\\text{px}$ airborne), and frames 5–8 (landing impact, cape settling, and triumphant fist pump) at ground baseline $Y = 135$.
  2. **Single-Turn Animation Pacing (`FighterAnimator.cs`):**
     - Switched `FighterState.Victory` to `PlayOnce(vic, victoryFps, unscaled: true)`.
     - Exposed `VictoryDuration => (float)vic.Count / victoryFps;` ($1.50\\text{s}$ at 6.0 FPS).
  3. **Immediate Next-Round Transition (`MatchManager.cs`):**
     - Updated `RoundEndSequence` to yield exactly `winnerAnim.VictoryDuration`.
     - The instant that single turn concludes, `CheckWinCondition()` immediately fires `StartRound()` without any awkward pause or repetitive bouncing.
  4. **Pure Sprite Anime Fire Cocoon (`FighterKiAura.cs`):**
     - Completely removed the `ParticleSystem` component, particle renderer, and speed-streak texture logic.
     - Preserved the high-frequency ($16\\text{ Hz}$) oscillating spiky flame shell cocoon (`dbz_spiky_aura.png`), ensuring pristine pixel-art readability.
  5. **State-Entry Audio Trigger (`FighterController.cs`):**
     - Added `fighterAudio?.PlayJump()` to `FighterState.Victory` for Saitama, providing a crisp audio whoosh as he leaps into the air.
  6. **Compiler Verification:** Clean compilation with **0 errors** across both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.
"""

if "### Bug 30:" not in text:
    text += bug30

with open(path, "w", encoding="utf-8", newline="\n") as f:
    f.write(text)
print("Updated Docs/05_DEV_LOG_AND_BUGS.md successfully!")
