path = r"Docs\03_FEATURE_BREAKDOWN.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

# Update Section 30 Phase 3 and 4
old_s30 = """3. **Phase 3: Staggered Victorious Celebration**
   - Winner transitions to `FighterState.Victory`.
   - Defeated character remains lying flat on the mat (pinned on their final knockout frame).
   - Victorious character performs their multi-frame victory loop (thumbs up for Goku, cape fluttering for Saitama).
4. **Phase 4: Dramatic Hold (2.8s Real-Time)**
   - The match state holds for $2.8$ seconds to allow players to soak in the victory before displaying Round 2/Final Round or Match Over."""

new_s30 = """3. **Phase 3: Staggered Victorious Celebration (Single Turn)**
   - Winner transitions to `FighterState.Victory`.
   - Defeated character remains lying flat on the mat (pinned on their final knockout frame).
   - Victorious character performs their full celebratory animation once (thumbs up for Goku, 9-frame jump celebration for Saitama).
4. **Phase 4: Exact Single-Turn Duration Hold (`VictoryDuration`)**
   - The engine waits exactly for the character's single-turn victory animation to complete ($1.50\\text{s}$ for Saitama, $1.00\\text{s}$ for Goku) via `anim.VictoryDuration`.
   - The instant that turn finishes, `MatchManager` immediately initiates the next round (or advances to the match-over screen if tournament is clinched)."""

text = text.replace(old_s30, new_s30)

# Update Section 31 to note particle removal
old_s31_sub = """2. **Rising Ki Speed-Streak Sub-Emitter:**
   - High-velocity vertical needle particles shooting upward through the cocoon from the fighter's feet to above their head."""

new_s31_sub = """2. **Clean Pure-Sprite Anime Cocoon (Zero Emitter Clutter):**
   - Particle streaks were removed to eliminate visual clutter and ensure hand-crafted pixel art fighters remain crisp and legible at all times.
   - Ground baseline rests cleanly at the fighter's feet without dipping below the floor plane."""

text = text.replace(old_s31_sub, new_s31_sub)

# Add Section 34 at the end
sec34 = """

---

## 34. Saitama Celebratory Jump Arc & Single-Turn Round Transition (`FighterAnimator.cs`, `MatchManager.cs`)

### Feature Overview:
Restores Saitama's authentic 9-frame celebration sequence where he squats, springs into the air with a triumphant punch into the sky, lands on the mat, and raises his fist in victory—tuned to play for exactly one turn and immediately trigger the next round.

### The 9-Frame Mathematical Jump Arc:
All frames are normalized on a $160 \\times 160\\text{px}$ canvas with the ground baseline at $Y = 135$:
1. **Frames 0–2 (Squat Windup):** Feet anchored to $Y = 135$. Saitama coils low into a compact spring ($H = 34\\text{px} \\to 25\\text{px}$).
2. **Frame 3 (Ascending Leap):** Springs upward with feet leaving the ground at $Y = 120$ ($15\\text{px}$ above baseline).
3. **Frame 4 (Apex Mid-Air Punch):** Peak airborne frame! Knees bent in mid-air with feet at $Y = 104$ ($31\\text{px}$ above ground), right gloved fist thrusting high into the sky ($Y = 57$).
4. **Frame 5 (Impact Landing):** Lands firmly back on the ground canvas ($Y = 135$) with cape billowing upward from kinetic momentum.
5. **Frames 6–7 (Cape Settling):** Settles gracefully into stance as the cape flutters ($Y = 135$).
6. **Frame 8 (Triumphant Fist Pump):** Stays standing proudly in his heroic victory pose with fist raised ($Y = 135$).

### The Single-Turn Arcade Pacing:
- In `FighterAnimator.cs`, `FighterState.Victory` executes via `PlayOnce` rather than `PlayLoop`.
- In `MatchManager.cs`, `RoundEndSequence` waits for `anim.VictoryDuration` (derived as $\\frac{\\text{FrameCount}}{\\text{FPS}} = \\frac{9}{6.0} = 1.50\\text{s}$).
- The instant that single turn concludes, `CheckWinCondition()` immediately fires `StartRound()` without any awkward pause or repetitive bouncing.
"""

if "## 34. Saitama Celebratory Jump Arc" not in text:
    text += sec34

with open(path, "w", encoding="utf-8", newline="\n") as f:
    f.write(text)
print("Updated Docs/03_FEATURE_BREAKDOWN.md successfully!")
