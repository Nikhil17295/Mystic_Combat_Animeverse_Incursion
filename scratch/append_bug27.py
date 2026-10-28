path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Docs\05_DEV_LOG_AND_BUGS.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

bug27 = """

---

### Bug 27: Kamehameha 16px Stray Rip Artifact, Skated Footing & Instant Round-End Visual Cut

- **Symptom:**
  1. Goku's Kamehameha charge (`ultimate_1..3`) hopped 16 pixels straight up into the air, hung briefly, and then slammed back down on frame 4. Furthermore, his stance skated horizontally while lunging forward.
  2. When a fighter's HP reached 0, the round ended abruptly with no dramatic weight; the loser's 11-frame collapse animation was cut short, the winner had no time to perform their victory pose, and the round restarted almost instantly.
  3. Aura visuals felt like soft generic smoke/dust clouds rather than authentic Dragon Ball Z jagged spiky flame energy.
  4. Goku was able to charge Ki via meditation while in Super Saiyan mode, which contradicted authentic anime mechanics.
  5. The default round timer of 99s caused matches to feel unnaturally rushed for a deliberate tactical fighting game.
- **Root Cause:**
  1. **Stray Sprite Rip Artifact at $Y = 73..74$:** In `Frames_Raw/ultimate_1..3.png`, 6 stray pixels from the adjacent sprite beneath it on the ripped sheet existed at $Y = 73..74$. Because the standard canvas script aligned bounding-box bottoms to baseline $Y = 105$, those 6 stray pixels were anchored to $Y = 105$, which lifted Goku's actual feet (at $Y = 58$) up to $Y = 89$ (16 pixels into the air). In frames 0 and 4..9, there was no bottom artifact ($Y \le 58$), so Goku's feet snapped back to the floor. Additionally, the ripped frames drifted horizontally by 1px per frame ($X = 13 \to 4$), causing foot skating when centered blindly.
  2. **Coupled TimeScale & Premature Round Restart:** `Time.deltaTime` scaled animation updates directly with physics timescale. When slow-motion was triggered, animations crawled or skipped frames, and `RoundEndSequence` waited only 1.8 seconds in game time without letting the loser hit the floor before the winner started celebrating.
  3. **Particle-Only Aura:** Soft radial particles cannot reproduce the iconic spiky golden flame cocoon of Akira Toriyama's Super Saiyan aesthetic.
- **Resolution:**
  1. **$Y < 60$ Crop & Fixed Foot Anchoring:**
     - In `scratch/realign_kamehameha.py`, cropped all raw frames strictly at $Y < 60$, completely eliminating the 16px bottom stray artifact.
     - Anchored Goku's planted back foot to a fixed canvas position ($X = 41$, $Y = 105$). Zero vertical hopping, zero horizontal foot skating.
  2. **4-Phase Dramatic Slow-Mo K.O. State Machine:**
     - In [`MatchManager.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Core/MatchManager.cs):
       - **Phase 1 (Slow-Mo Collapse):** Loser enters `FighterState.Knockout`, `Time.timeScale = 0.28f`, camera shake ($0.40$), impact flash ($0.12\\text{s}$). Waits $1.8\\text{s}$ in real time (`WaitForSecondsRealtime(1.8f)`).
       - **Phase 2 (Floor Impact & Normalization):** Loser hits the mat, ground dust puff triggers, and `Time.timeScale` snaps back to $1.0\\text{f}$.
       - **Phase 3 (Staggered Victory Celebration):** Winner transitions to `FighterState.Victory` to perform their full victory animation loop.
       - **Phase 4 (Dramatic Hold):** Holds for $2.8\\text{s}$ in real time before calling `CheckWinCondition()`.
     - In [`FighterAnimator.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterAnimator.cs): Added `unscaled: true` to `PlayOnce` and `PlayLoop` for `Knockout` and `Victory` states, ensuring all 11 knockout frames and 9 victory frames play smoothly at full framerate regardless of timescale.
  3. **Authentic DBZ Dual-Layer Spiky Flame Aura:**
     - Created `Assets/Sprites/Effects/dbz_spiky_aura.png` with 26 jagged flame spires, elongated upward flare, and bright inner core.
     - In [`FighterKiAura.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Combat/FighterKiAura.cs), built a dual-layer system:
       - Oscillating spiky shell ($24\\text{–}28\\text{ Hz}$ flame vibration, alpha pulse).
       - Sub-emitter vertical needle speed streaks shooting upward from feet.
       - Dynamic color tinting: Radiant Gold for SSJ Goku, Electric Cyan for Base Goku meditation, Blazing Crimson for Saitama.
  4. **Super Saiyan Meditation Guard:** In [`FighterController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs), blocked meditation while `isSuperSaiyan` is active, and automatically reverted to Idle if meditation was active during transformation.
  5. **Round Timer Set to 199s:** Set `roundDuration = 199f` in `MatchManager.cs` for deliberate, authentic arcade fighting pacing.
  6. **Sprite Slicer Default to SSJ:** Updated `Tools/SpriteSlicer/index.html` to default to `goku_ssj` and cleared `Assets/Sprites/Fighters/Goku/Frames_SSJ/` so the user has a clean slate for manual slicing.
  7. **Compiler Verification:** Clean compilation with **0 errors** across both `Assembly-CSharp.dll` and `Assembly-CSharp-Editor.dll`.
"""

text = text.rstrip() + bug27
with open(path, "w", encoding="utf-8") as f:
    f.write(text)
print("Successfully appended Bug 27 to Docs/05_DEV_LOG_AND_BUGS.md")
