path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Docs\04_VIVA_AND_INTERVIEW_PREP.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

viva_new = """

---

### Question 30: How did you implement the dramatic slow-motion K.O. sequence without causing animations to stutter or skip frames?
**Answer for Judges:**
> *"In authentic arcade fighters (Street Fighter, Guilty Gear), the final knockout blow triggers cinematic slow motion to let the impact register before the next round begins.
>
> However, a classic Unity beginner trap is simply setting `Time.timeScale = 0.28f`. Because sprite animation scripts typically accumulate `Time.deltaTime`, slowing the physics engine by 72% makes the animation update 72% slower! An 11-frame knockout collapse that should look smooth turns into a choppy, slideshow-like 2 FPS crawl.
>
> We solved this through **Decoupled Animation Delta Time (`Time.unscaledDeltaTime`)**:
> 1. In `MatchManager.cs`, we orchestrate a 4-phase sequence:
>    - **Phase 1 (Slow-Mo Collapse):** Loser enters `Knockout`, `Time.timeScale` drops to $0.28\\text{f}$, triggering screen shake and an impact flash.
>    - **Phase 2 (Decoupled Playback):** In `FighterAnimator.cs`, `PlayOnce` uses `Time.unscaledDeltaTime` for knockout and victory states, dynamically adjusting FPS (`ko.Count / 1.7f`) so all 11 frames play smoothly across the 1.8-second slow-motion window.
>    - **Phase 3 (Floor Impact & Normalization):** When the loser hits the mat, `Time.timeScale` snaps back to $1.0\\text{f}$, triggering a floor dust puff.
>    - **Phase 4 (Staggered Celebration):** The winner then begins their celebratory victory loop while the loser stays pinned on the ground. A 2.8-second dramatic hold lets players absorb the win before advancing."*

---

### Question 31: Why did Goku's Kamehameha jump 16 pixels into the air, and how did you debug and fix the sprite sheet ripping artifact?
**Answer for Judges:**
> *"This was a fascinating pixel-level forensic bug:
> 1. **The Bug:** During Kamehameha charge frames (`ultimate_1..3`), Goku hopped 16 pixels straight up into the air, hung hovering, and then crashed down on frame 4.
> 2. **Root Cause Analysis:** We wrote a Python inspection script to analyze the alpha channels across all raw frames. We discovered that in frames 1, 2, and 3, there were 6 stray non-zero pixels from an adjacent sprite below Goku on the original rip sheet at $Y = 73..74$. Because our previous normalization script calculated the bounding box bottom and aligned it to baseline $Y = 105$, those 6 stray pixels were anchored to $Y = 105$, which pushed Goku's actual feet (at $Y = 58$) up to $Y = 89$ (16 pixels into the air!).
> 3. **The Solution:** We cropped all raw frames strictly at $Y < 60$ (discarding the rip artifact) and locked Goku's back planted foot to a fixed canvas coordinate ($X = 41$, $Y = 105$). This completely eliminated the vertical 16px hop and fixed horizontal stance skating."*

---

### Question 32: How did you create an authentic Dragon Ball Z spiky flame aura instead of generic particle puffs?
**Answer for Judges:**
> *"Authentic Dragon Ball Z Super Saiyan auras are not soft radial smoke puffs; they are violent, jagged, spiky golden flame cocoons that lick upward around the fighter's silhouette.
> We implemented this in `FighterKiAura.cs` using a **Dual-Layer Oscillating Shell**:
> 1. **Spiky Flame Cocoon:** A custom high-res sprite (`dbz_spiky_aura.png`) with 26 jagged upward flame spires and a white-hot inner core. In `Update()`, we apply high-frequency dual-Perlin noise ($24\\text{–}28\\text{ Hz}$) to scale and alpha, creating raging, licking fire flicker.
> 2. **Speed-Streak Sub-Emitter:** A particle system emitting vertical stretched needle particles shooting rapidly upward from the feet past the head.
> 3. **Dynamic Palette Tinting:** Because the base sprite is authored in master grayscale with alpha, Unity's `SpriteRenderer.color` tints it with 100% color vibrancy: Radiant Gold for Super Saiyan Goku, Electric Cyan for Base Goku meditation, and Blazing Crimson for Saitama."*
"""

text = text.rstrip() + viva_new
with open(path, "w", encoding="utf-8") as f:
    f.write(text)
print("Successfully appended Questions 30-32 to Docs/04_VIVA_AND_INTERVIEW_PREP.md")
