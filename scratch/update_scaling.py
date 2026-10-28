# 1. FighterAnimator.cs
anim_path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Fighter\FighterAnimator.cs"
with open(anim_path, "r", encoding="utf-8") as f:
    anim_code = f.read()

anim_code = anim_code.replace("pixelsPerUnit = 28f;", "pixelsPerUnit = 34.5f;")
anim_code = anim_code.replace("pixelsPerUnit = 24f;", "pixelsPerUnit = 29.5f;")

with open(anim_path, "w", encoding="utf-8") as f:
    f.write(anim_code)
print("Updated FighterAnimator.cs PPU")

# 2. FighterController.cs
fc_path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Fighter\FighterController.cs"
with open(fc_path, "r", encoding="utf-8") as f:
    fc_code = f.read()

fc_code = fc_code.replace(
    "hurtboxCollider.size = new Vector2(0.85f, 1.8f);\n                hurtboxCollider.offset = new Vector2(0f, 0.95f);",
    "hurtboxCollider.size = new Vector2(0.70f, 1.50f);\n                hurtboxCollider.offset = new Vector2(0f, 0.78f);"
)

fc_code = fc_code.replace(
    "hurtboxCollider.size = new Vector2(0.85f, 0.95f);\n                    hurtboxCollider.offset = new Vector2(0f, 0.48f);",
    "hurtboxCollider.size = new Vector2(0.70f, 0.78f);\n                    hurtboxCollider.offset = new Vector2(0f, 0.40f);"
)

with open(fc_path, "w", encoding="utf-8") as f:
    f.write(fc_code)
print("Updated FighterController.cs hurtbox sizes")
