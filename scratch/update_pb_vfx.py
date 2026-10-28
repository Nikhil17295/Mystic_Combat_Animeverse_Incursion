pb_path = r"Assets\Scripts\Core\PrototypeBootstrap.cs"
with open(pb_path, "r", encoding="utf-8") as f:
    text = f.read()

t = """            // Wire cross-references (each fighter auto-faces the other)
            FighterController fc1 = fighter1.GetComponent<FighterController>();
            FighterController fc2 = fighter2.GetComponent<FighterController>();
            fc1.opponent = fighter2.transform;
            fc2.opponent = fighter1.transform;"""

r = """            // Wire cross-references (each fighter auto-faces the other)
            FighterController fc1 = fighter1.GetComponent<FighterController>();
            FighterController fc2 = fighter2.GetComponent<FighterController>();
            fc1.opponent = fighter2.transform;
            fc2.opponent = fighter1.transform;

            // Ensure VFXManager is bootstrapped
            if (Object.FindAnyObjectByType<Combat.VFXManager>() == null)
            {
                GameObject vfxObj = new GameObject("VFXManager");
                vfxObj.AddComponent<Combat.VFXManager>();
            }

            // Ensure dynamic Ki particle aura is present on both fighters
            if (fighter1.GetComponent<Combat.FighterKiAura>() == null) fighter1.AddComponent<Combat.FighterKiAura>();
            if (fighter2.GetComponent<Combat.FighterKiAura>() == null) fighter2.AddComponent<Combat.FighterKiAura>();"""

if t in text:
    text = text.replace(t, r, 1)
    with open(pb_path, "w", encoding="utf-8") as f:
        f.write(text)
    print("PrototypeBootstrap.cs updated with VFXManager and KiAura bootstrapping!")
else:
    print("Target not found in PrototypeBootstrap.cs!")
