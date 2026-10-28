path = r"Docs\02_ARCHITECTURE.md"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

# 1. Update line 861
text = text.replace(
    'K -- Yes --> M["Winner enters FighterState.Victory -> Plays victory_*.png loop"]',
    'K -- Yes --> M["Winner enters FighterState.Victory -> Plays victory_*.png single turn"]'
)

# 2. Update sequence diagram lines 902-907
old_seq = """    Note over Win,Anim: Phase 3: Winner starts victory pose loop
    MM->>Win: TransitionTo(FighterState.Victory)
    Anim->>Win: PlayLoop(victory_0..8, fps: 6.0, unscaled: true)
    Note over MM: Phase 4: Dramatic Hold & Let Things Sink In
    MM->>MM: WaitForSeconds(2.8s)
    MM->>MM: CheckWinCondition() / StartRound()"""

new_seq = """    Note over Win,Anim: Phase 3: Winner performs single-turn victory celebration
    MM->>Win: TransitionTo(FighterState.Victory)
    Anim->>Win: PlayOnce(victory_0..8, fps: 6.0, unscaled: true)
    Note over MM: Phase 4: Wait for exactly 1 turn of victory animation (VictoryDuration)
    MM->>MM: WaitForSeconds(VictoryDuration)
    Note over MM: Phase 5: Start next round immediately!
    MM->>MM: CheckWinCondition() / StartRound()"""

text = text.replace(old_seq, new_seq)

# 3. Update aura description in section 21
old_aura = """2. **Dual-Layer Oscillating DBZ Aura (`FighterKiAura.cs`):**
   - **Layer 1 (Spiky Flame Cocoon):** A high-frequency oscillating sprite shell (`dbz_spiky_aura.png`, 26 jagged flame spires) fluctuating at $24–28\\text{ Hz}$ with dynamic Perlin scaling and alpha pulsation.
   - **Layer 2 (Speed-Streak Sub-Emitter):** Vertical stretched needle particles shooting rapidly upward through the cocoon from the feet past the fighter's head.
   - **Dynamic Color Tinting:** Single-pass tinting allows the grayscale master sprite to project Radiant DBZ Gold (Super Saiyan Goku), Electric Cyan (Base Goku meditation), or Blazing Crimson (Saitama)."""

new_aura = """2. **Pure Sprite Oscillating Flame Aura (`FighterKiAura.cs`):**
   - **Spiky Flame Shell Cocoon:** A high-frequency oscillating sprite shell (`dbz_spiky_aura.png`, 7 fluid anime fire tongues) fluctuating with rapid sine/cosine scaling and alpha pulsation ($16\\text{ Hz}$), perfectly conforming around the fighter's silhouette.
   - **Zero-Emitter Pixel-Art Clarity:** Particle streaks were intentionally removed to eliminate visual clutter and keep 100% focus on the hand-crafted fighting sprites and clean combat hitboxes.
   - **Dynamic Single-Pass Tinting:** The grayscale flame cocoon is dynamically tinted to Radiant DBZ Gold (Super Saiyan Goku), Electric Cyan (Base Goku meditation), or Blazing Crimson (Saitama)."""

text = text.replace(old_aura, new_aura)

with open(path, "w", encoding="utf-8", newline="\n") as f:
    f.write(text)
print("Updated Docs/02_ARCHITECTURE.md successfully!")
