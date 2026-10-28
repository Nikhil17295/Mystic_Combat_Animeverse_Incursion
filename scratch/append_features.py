path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Docs\03_FEATURE_BREAKDOWN.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

new_sections = """

---

## 30. Dramatic 4-Phase Slow-Mo Knockout & Staggered Victory Flow (`MatchManager.cs`, `FighterAnimator.cs`)

### Feature Overview:
In arcade fighting classics like *Street Fighter III: 3rd Strike*, *Guilty Gear*, and *Dragon Ball FighterZ*, the final blow that finishes a round is never cut off instantly. It is given dramatic, cinematic weight.

### The 4-Phase Sequence:
1. **Phase 1: Cinematic Slow-Motion Collapse (1.8s Real-Time)**
   - Defeated character immediately enters `FighterState.Knockout`.
   - Engine slows down to slow motion (`Time.timeScale = 0.28f`).
   - Impact flash (white overlay for $0.12\\text{s}$) and camera shake ($0.40$ intensity).
   - Defeated fighter plays their full multi-frame knockdown animation (11 frames for Goku, 5 frames for Saitama) in slow motion as they fly back and collapse.
   - `FighterAnimator.cs` plays this using `unscaled: true` so the sprite frame rate remains fluid and uninterrupted over the 1.8-second slow-motion window.
2. **Phase 2: Floor Impact & Normalization**
   - The moment the character's body strikes the arena floor, `Time.timeScale` snaps back to $1.0\\text{f}$ normal speed.
   - A ground dust puff and secondary impact camera shake trigger at the landing coordinates.
3. **Phase 3: Staggered Victorious Celebration**
   - Winner transitions to `FighterState.Victory`.
   - Defeated character remains lying flat on the mat (pinned on their final knockout frame).
   - Victorious character performs their multi-frame victory loop (thumbs up for Goku, cape fluttering for Saitama).
4. **Phase 4: Dramatic Hold (2.8s Real-Time)**
   - The match state holds for $2.8$ seconds to allow players to soak in the victory before displaying Round 2/Final Round or Match Over.

---

## 31. DBZ Spiky Flame Cocoon & Speed-Streak Ki Aura System (`FighterKiAura.cs`)

### Feature Overview:
Replaces generic soft particles with the authentic Dragon Ball Z Super Saiyan flame cocoon matching Akira Toriyama's manga and anime aesthetic.

### Visual Architecture:
1. **Jagged Spiky Flame Cocoon:**
   - Custom sprite texture (`Assets/Sprites/Effects/dbz_spiky_aura.png`) featuring 26 jagged upward flame spires and an intense white-hot energy core.
   - Fluctuates and flickers rapidly ($24\\text{–}28\\text{ Hz}$) using dual Perlin oscillation to simulate turbulent licking fire.
   - Pulsing alpha energy envelope ($0.80\\text{–}0.98$).
2. **Rising Ki Speed-Streak Sub-Emitter:**
   - High-velocity vertical needle particles shooting upward through the cocoon from the fighter's feet to above their head.
3. **Dynamic Color Adaptation:**
   - **Super Saiyan Goku:** Radiant DBZ Gold (`Color(1.0f, 0.88f, 0.22f)`).
   - **Base Goku Meditation:** Electric Cyan (`Color(0.18f, 0.85f, 1.0f)`).
   - **Saitama Pressure:** Blazing Crimson (`Color(1.0f, 0.18f, 0.15f)`).

---

## 32. Kamehameha 16px Stray Rip Artifact Fix & Zero-Skate Foot Anchoring

### The Bug:
In `Frames_Raw/ultimate_1..3.png`, 6 stray pixels from the adjacent sprite on the original sprite sheet were ripped at $Y = 73..74$. Because the standard canvas tool aligned bounding-box bottoms to baseline $Y = 105$, these 6 pixels pushed Goku's actual feet (at $Y = 58$) up into the air by 16 pixels. During charge frames, Goku hopped into the sky and slammed down on frame 4. Furthermore, blind horizontal centering caused foot skating.

### The Fix:
1. Cropped all raw frames strictly at $Y < 60$, stripping the 16px bottom rip artifact completely.
2. Anchored the planted back foot to fixed canvas coordinates ($X = 41$, $Y = 105$ on a $128 \\times 128$ canvas).
3. Zero vertical hopping, zero horizontal foot skating across all 10 Kamehameha frames (`ultimate_0..9`).

---

## 33. Super Saiyan State Restrictions & 199s Round Timer Pacing

### Super Saiyan Meditation Guard:
- In anime canon, Super Saiyan is an explosive, raging combat form; Goku cannot calmly meditate to charge Ki while ascended.
- In [`FighterController.cs`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Scripts/Fighter/FighterController.cs), meditation input is guarded: `if (inputHandler.MeditateHeld && IsGrounded && !isSuperSaiyan)`.
- If meditation is active during transformation, the fighter immediately cancels meditation and transitions to Idle.

### 199s Round Timer Pacing:
- Set `roundDuration = 199f` in `MatchManager.cs`. Matches have ample time for deliberate neutral spacing, footsies, and strategic Ki management without artificial rushing.
"""

text = text.rstrip() + new_sections
with open(path, "w", encoding="utf-8") as f:
    f.write(text)
print("Successfully appended Sections 30-33 to Docs/03_FEATURE_BREAKDOWN.md")
