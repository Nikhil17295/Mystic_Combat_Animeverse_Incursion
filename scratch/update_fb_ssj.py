lines = open('Docs/03_FEATURE_BREAKDOWN.md', 'r', encoding='utf-8').readlines()
new_sec = [
    "### 1. Goku's Pure Super Saiyan Transformation (Zero Damage & Direct-to-Idle)\n",
    "- **Role:** Skill button (`E` / `U`) triggers Goku's Super Saiyan power-up.\n",
    "- **Hitbox Deactivation:** `attackHitbox.Deactivate()` is executed immediately on state transition.\n",
    "- **Direct-to-Idle State Machine:** Rather than entering `FighterState.Skill` and triggering an attack duration lockout, `FighterController.cs` transitions directly to `FighterState.Idle`.\n",
    "- **Legacy Asset Purge:** Deleted legacy `skill_0..3.png` from `Assets/Sprites/Fighters/Goku/Frames/`.\n",
    "- **Zero Offensive Reach:** `attackStartupTimer = 0f` and `attackActiveTimer = 0f`. No hitbox activates, zero damage is inflicted, and forward surge speed is zeroed out (0.0 m/s), ensuring Goku plants his feet solidly during his transformation.\n",
    "- **Instantaneous SSJ Visuals:** Because `controller.IsSuperSaiyan` is asserted immediately, `FighterAnimator.cs` loads `ssjIdleFrames` on the exact same frame, seamlessly rendering Goku in his blazing Super Saiyan stance with 0ms visual delay.\n"
]
lines = lines[:1097] + new_sec + lines[1101:]
with open('Docs/03_FEATURE_BREAKDOWN.md', 'w', encoding='utf-8') as f:
    f.writelines(lines)
print('Updated lines in Docs/03_FEATURE_BREAKDOWN.md')
