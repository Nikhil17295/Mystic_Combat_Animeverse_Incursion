path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Docs\04_VIVA_AND_INTERVIEW_PREP.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

viva_new = """

---

### Question 33: How do you synchronize attack animations and hitboxes with voice audio clips so moves don't finish before the character stops speaking?
**Answer for Judges:**
> *"In fighting games, a common immersion-breaking bug is when a character finishes their special move animation and returns to their idle stance while their voice line is still shouting in the background (e.g. Goku walking around while still screaming 'ME... HA!').
>
> We resolved this through **Audio-Calibrated State Timers**:
> 1. We inspected the exact duration of each vocal asset (e.g., `goku_voice_kamehameha.wav` is $3.36\\text{s}$, `consecutive_normal_punches.mp3` is $2.70\\text{s}$).
> 2. In `FighterController.cs`, we matched `attackTimer` directly to the clip duration ($3.36\\text{s}$).
> 3. We partitioned the attack into exact acoustic phases:
>    - **Startup Phase ($2.15\\text{s}$):** Goku holds his charging pose in cupped hands while shouting the chant ('KA... ME... HA... ME...').
>    - **Active Phase ($1.21\\text{s}$):** Hitbox activates and beam fires exactly as 'HA!' explodes.
> 4. In `FighterAnimator.cs`, the 10-frame animation playback rate is mathematically derived from `(float)frames.Count / attackDuration`, ensuring the sprite sequence and audio finish at the exact same millisecond."*

---

### Question 34: Why was Saitama jumping in his victory loop and how did you stabilize his vertical baseline?
**Answer for Judges:**
> *"When creating sprite animation loops from raw sprite sheets, rookie developers often include transition poses (e.g., a crouch takeoff or a landing squat) in the looping array.
>
> In our Saitama victory track, the raw slice included frames of him crouching down ($34\\text{px}$ tall) and leaping into the air ($60\\text{px}$ tall). Because the animator was set to `PlayLoop`, Saitama repeatedly collapsed to the floor and sprang into the air in an erratic loop.
>
> We fixed this by:
> 1. Standardizing the loop strictly to Saitama's heroic standing frames ($49\\text{px}$ tall, hands on hips with cape billowing).
> 2. Locking his head to $Y = 82$ and his feet to ground baseline $Y = 135$ across all frames.
> 3. This produces a rock-solid, grounded celebration with natural cape wind motion and zero vertical hopping."*
"""

text = text.rstrip() + viva_new
with open(path, "w", encoding="utf-8") as f:
    f.write(text)
print("Successfully appended Questions 33-34 to Docs/04_VIVA_AND_INTERVIEW_PREP.md")
