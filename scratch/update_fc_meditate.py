path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Fighter\FighterController.cs"
with open(path, "r", encoding="utf-8") as f:
    code = f.read()

# 1. Meditate guard in HandleGroundedTransitions
old_trans = "if (inputHandler.MeditateHeld && IsGrounded)"
new_trans = "if (inputHandler.MeditateHeld && IsGrounded && !isSuperSaiyan)"
if old_trans in code:
    code = code.replace(old_trans, new_trans, 1)
    print("Updated HandleGroundedTransitions meditate guard!")

# 2. Meditate cancel in HandleStateTransitions
old_med = "case FighterState.Meditating:\n                    if (!inputHandler.MeditateHeld || !IsGrounded)"
if old_med not in code:
    old_med = "case FighterState.Meditating:\r\n                    if (!inputHandler.MeditateHeld || !IsGrounded)"

new_med = "case FighterState.Meditating:\n                    if (!inputHandler.MeditateHeld || !IsGrounded || isSuperSaiyan)"
if old_med in code:
    code = code.replace(old_med, new_med, 1)
    print("Updated HandleStateTransitions meditate cancel!")

# 3. In ActivateSuperSaiyan, exit Meditating state if active
old_act = "superSaiyanTimer = duration;"
new_act = "superSaiyanTimer = duration;\n            if (CurrentState == FighterState.Meditating) TransitionTo(FighterState.Idle);"
if old_act in code:
    code = code.replace(old_act, new_act, 1)
    print("Updated ActivateSuperSaiyan!")

with open(path, "w", encoding="utf-8") as f:
    f.write(code)
print("Updated FighterController.cs successfully!")
