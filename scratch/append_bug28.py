path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Docs\05_DEV_LOG_AND_BUGS.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

bug28 = """

---

### Bug 28: Voice-Line Animation Truncation, Victory Pogo-Sticking & Dedicated Slicer Architecture

- **Symptom:**
  1. Goku returned to his idle breathing stance only halfway through shouting his Kamehameha chant, walking around while the audio still blasted "ME... HA!".
  2. Saitama's victory animation looked like erratic jumping/pogo-sticking rather than a stable, proud victory celebration.
  3. The round-end K.O. sequence felt rushed, cutting to victory before the defeat settled in.
  4. The aura had too many rigid spikes and extended below the fighter's feet through the floor.
  5. Fighters occupied ~2.0 world units, making the stage feel cramped with little room for spacing.
  6. The sprite slicer tool suffered from browser caching when attempting to toggle between Base and Super Saiyan modes.
- **Root Cause:**
  1. **Audio/State Duration Disconnect:** `goku_voice_kamehameha.wav` is $3.36\\text{s}$ long, while `FighterController.attackTimer` was hardcoded to $1.45\\text{s}$. The state machine exited the `Ultimate` state and resumed `Idle` before the audio finished.
  2. **Looping Incompatible Poses:** The victory animation frames contained crouch frames ($34\\text{px}$), mid-air leaps ($60\\text{px}$), and standing poses ($49\\text{px}$). Looping these in a cycle made Saitama jump up and down by $30\\text{px}$ continuously.
  3. **Premature TimeScale Restoration:** The knockout slow-motion sequence lasted only $1.8\\text{s}$ in real time, rushing the physics and collapse.
  4. **Rigid Spike Geometry:** Generating 26 mathematical polygon spikes produced a rigid cardboard shell rather than translucent, licking anime fire.
  5. **Shared Single-Page State Mismatch:** Trying to swap sheets and characters inside a single HTML page caused browser caching collisions.
- **Resolution:**
  1. **Audio-Synchronized Attack Timings:**
     - Goku Kamehameha: $3.36\\text{s}$ total ($2.15\\text{s}$ charging chant + $1.21\\text{s}$ active blast).
     - Saitama Consecutive Punches: $2.70\\text{s}$ continuous looping flurry.
     - Saitama Serious Punch: $3.80\\text{s}$ ($2.40\\text{s}$ chilling windup + $1.40\\text{s}$ devastating punch).
  2. **Zero-Jumping Victory Normalization:**
     - Standardized Saitama's victory animation to his clean, heroic standing victory frames (hands on hips, cape fluttering).
     - Feet locked to $Y = 135$ and head to $Y = 82$ across all frames. Zero jumping.
  3. **9.5s Unhurried Cinematic Knockout:**
     - $4.0\\text{s}$ ultra slow-motion collapse (`Time.timeScale = 0.12f`).
     - $1.5\\text{s}$ motionless floor pause.
     - $4.0\\text{s}$ victory celebration hold.
  4. **Fluid Translucent Anime Fire:**
     - Redrew [`dbz_spiky_aura.png`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Assets/Sprites/Effects/dbz_spiky_aura.png) with a rounded ground base that never extends below the feet ($Y=0$) and 7 fluid, soft fire tongues.
  5. **Fighter Scale Normalization:**
     - Scaled PPU to 29.5 (Goku) and 34.5 (Saitama), reducing height by $18\\%$ ($1.58\\text{m}$), opening up the stage for spacing.
  6. **Dedicated Standalone Slicer (`slicer_ssj.html`):**
     - Created [`Tools/SpriteSlicer/slicer_ssj.html`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Tools/SpriteSlicer/slicer_ssj.html) and [`launch_ssj_slicer.bat`](file:///B:/Unity/Projects/Mystic_Combat_Animeverse_Incursion/Tools/SpriteSlicer/launch_ssj_slicer.bat) dedicated 100% to Super Saiyan slicing with zero character swap buttons.
"""

text = text.rstrip() + bug28
with open(path, "w", encoding="utf-8") as f:
    f.write(text)
print("Successfully appended Bug 28 to Docs/05_DEV_LOG_AND_BUGS.md")
