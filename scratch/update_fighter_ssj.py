fc_path = r"Assets\Scripts\Fighter\FighterController.cs"
with open(fc_path, "r", encoding="utf-8") as f:
    content = f.read()

# 3. Update method
t3 = """        private void Update()
        {
            CheckGround();
            HandleStateTransitions();
            HandleKiGeneration();
        }"""
r3 = """        private void Update()
        {
            CheckGround();
            HandleSuperSaiyanTimer();
            HandleStateTransitions();
            HandleKiGeneration();
        }"""

# 4. ResetToIdle
t4 = """            if (anim != null)
            {
                anim.ResetToIdle();
            }"""
r4 = """            if (anim != null)
            {
                anim.ResetToIdle();
            }
            RevertSuperSaiyan();"""

# 5. Skill pressed
t5 = """                // Skill Attack (E / U)
                if (inputHandler.SkillPressed)
                {
                    float skillCost = 20f;
                    if (energySystem == null || energySystem.CanAfford(skillCost))
                    {
                        if (energySystem != null) energySystem.SpendKi(skillCost);
                        attackTimer = 0.85f;
                        attackStartupTimer = 0.10f;
                        attackActiveTimer = 0.60f;
                        if (attackHitbox != null)
                        {
                            bool isGoku = gameObject.name.ToLower().Contains("goku");
                            Vector2 offset = isGoku ? new Vector2(2.1f, 0.9f) : new Vector2(1.05f, 0.9f);
                            Vector2 size = isGoku ? new Vector2(3.6f, 0.85f) : new Vector2(1.4f, 0.9f);
                            attackHitbox.ConfigureAttack(20, 6f, 8f, offset, size);
                        }
                        TransitionTo(FighterState.Skill);
                        return;
                    }
                }"""
r5 = """                // Skill Attack / Super Saiyan (E / U)
                if (inputHandler.SkillPressed)
                {
                    bool isGoku = gameObject.name.ToLower().Contains("goku");
                    float skillCost = isGoku ? 35f : 20f;
                    if (energySystem == null || energySystem.CanAfford(skillCost))
                    {
                        if (energySystem != null) energySystem.SpendKi(skillCost);

                        if (isGoku && !isSuperSaiyan)
                        {
                            ActivateSuperSaiyan(15f);
                            attackTimer = 0.65f;
                            attackStartupTimer = 0.0f;
                            attackActiveTimer = 0.0f;
                            TransitionTo(FighterState.Skill);
                            return;
                        }

                        attackTimer = 0.85f;
                        attackStartupTimer = 0.10f;
                        attackActiveTimer = 0.60f;
                        if (attackHitbox != null)
                        {
                            Vector2 offset = isGoku ? new Vector2(2.1f, 0.9f) : new Vector2(1.05f, 0.9f);
                            Vector2 size = isGoku ? new Vector2(3.6f, 0.85f) : new Vector2(1.4f, 0.9f);
                            int damage = isGoku ? (isSuperSaiyan ? 26 : 20) : 20;
                            attackHitbox.ConfigureAttack(damage, 6f, 8f, offset, size);
                        }
                        TransitionTo(FighterState.Skill);
                        return;
                    }
                }"""

for idx, (t, r) in enumerate([(t3, r3), (t4, r4), (t5, r5)], 3):
    if t in content:
        content = content.replace(t, r, 1)
        print(f"Target {idx} replaced successfully!")
    else:
        print(f"Target {idx} failed!")

with open(fc_path, "w", encoding="utf-8") as f:
    f.write(content)
print("Updated remaining FighterController targets.")
