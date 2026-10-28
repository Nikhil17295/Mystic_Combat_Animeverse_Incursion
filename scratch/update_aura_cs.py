path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Combat\FighterKiAura.cs"
with open(path, "r", encoding="utf-8") as f:
    code = f.read()

code = code.replace(
    "private Vector3 baseScale = new Vector3(1.1f, 1.15f, 1f);",
    "private Vector3 baseScale = new Vector3(0.82f, 0.88f, 1f);"
)

code = code.replace(
    "auraShellObj.transform.localPosition = new Vector3(0f, 0.42f, 0.05f); // Centered on torso",
    "auraShellObj.transform.localPosition = new Vector3(0f, 0.05f, 0.05f); // Rest on floor at feet level"
)

code = code.replace(
    "new Vector2(0.5f, 0.35f),",
    "new Vector2(0.5f, 0.04f), // Pivot at bottom ground line"
)

code = code.replace(
    "float flickerX = 1f + Mathf.Sin(Time.time * 28f) * 0.05f + Mathf.Cos(Time.time * 19f) * 0.03f;\n                float flickerY = 1f + Mathf.Cos(Time.time * 24f) * 0.06f + Mathf.Sin(Time.time * 16f) * 0.035f;",
    "float flickerX = 1f + Mathf.Sin(Time.time * 16f) * 0.035f + Mathf.Cos(Time.time * 10f) * 0.02f;\n                float flickerY = 1f + Mathf.Cos(Time.time * 14f) * 0.045f + Mathf.Sin(Time.time * 9f) * 0.025f;"
)

with open(path, "w", encoding="utf-8") as f:
    f.write(code)
print("Updated FighterKiAura.cs successfully!")
