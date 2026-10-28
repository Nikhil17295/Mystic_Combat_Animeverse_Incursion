path = r"Docs\04_VIVA_AND_INTERVIEW_PREP.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

old_q34 = """### Question 34: Why was Saitama jumping in his victory loop and how did you stabilize his vertical baseline?
**Answer for Judges:**
> *"When creating sprite animation loops from raw sprite sheets, rookie developers often include transition poses (e.g., a crouch takeoff or a landing squat) in the looping array.
>
> In our Saitama victory track, the raw slice included frames of him crouching down ($34\\text{px}$ tall) and leaping into the air ($60\\text{px}$ tall). Because the animator was set to `PlayLoop`, Saitama repeatedly collapsed to the floor and sprang into the air in an erratic loop.
>
> We fixed this by:
> 1. Standardizing the loop strictly to Saitama's heroic standing frames ($49\\text{px}$ tall, hands on hips with cape billowing).
> 2. Locking his head to $Y = 82$ and his feet to ground baseline $Y = 135$ across all frames.
> 3. This produces a rock-solid, grounded celebration with natural cape wind motion and zero vertical hopping."*"""

new_q34 = """### Question 34: Why was Saitama jumping erratically in his victory animation, and how did you implement his celebratory jump with single-turn round transitions?
**Answer for Judges:**
> *"When creating sprite animations from fighting game sprite sheets, celebratory animations often depict a dynamic gesture (e.g. crouching down, jumping high into the air with a victory punch, and landing).
>
> In our prototype, two flaws originally ruined this sequence:
> 1. **Looping Incompatible Keyframes:** Because `FighterAnimator` was set to `PlayLoop`, Saitama repeatedly collapsed to the floor and sprang into the air in an erratic, continuous pogo-stick cycle for 4 seconds.
> 2. **Rigid Round Delay:** The round manager paused for an arbitrary 4.0 seconds regardless of how long the celebration actually took.
>
> We resolved this through **Single-Turn Celebration & Mathematical Canvas Anchoring**:
> 1. In `FighterAnimator.cs`, victory celebrations play via `PlayOnce` rather than `PlayLoop`, completing exactly one turn ($1.50\\text{s}$ at 6 FPS) and cleanly holding the final proud pose.
> 2. In `MatchManager.cs`, `RoundEndSequence` waits for `winnerAnimator.VictoryDuration`. The instant that single turn concludes, `CheckWinCondition()` is called, immediately initiating the next round!
> 3. For the jump arc itself, we anchored the 9 frames mathematically on our $160 \\times 160\\text{px}$ canvas ($Y = 135 \\to 120 \\to 104 \\to 135$), delivering an authentic airborne leap ($31\\text{px}$ off the floor) and clean landing with zero physics jitter."*"""

text = text.replace(old_q34, new_q34)

q37 = """

---

### Question 37: Why did you remove particle effects from the Ki aura, and how does the pure sprite flame cocoon work?
**Answer for Judges:**
> *"In 2D retro fighting games, pixel-art readability is paramount. Standard Unity particle systems emit billboarded, non-pixelated textured quads that clash with the sharp 16-bit aesthetic and obscure character silhouettes during combat.
>
> We replaced particle streaks with a pure sprite-driven oscillating flame shell (`FighterKiAura.cs`):
> 1. A hand-drawn spiky flame texture (`dbz_spiky_aura.png`) wraps snugly around the fighter's silhouette, anchored at their feet.
> 2. In `Update()`, high-frequency sine/cosine oscillation ($16\\text{ Hz}$) subtly flickers the X and Y scale while alpha pulses between $0.80$ and $0.98$, perfectly simulating raging anime fire.
> 3. Dynamic single-pass color tinting adjusts the aura to Super Saiyan Gold, Base Goku Cyan, or Saitama Crimson without any particle emitter overhead or visual noise."*
"""

if "### Question 37:" not in text:
    text += q37

with open(path, "w", encoding="utf-8", newline="\n") as f:
    f.write(text)
print("Updated Docs/04_VIVA_AND_INTERVIEW_PREP.md successfully!")
