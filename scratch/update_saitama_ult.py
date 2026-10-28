fc_path = r"Assets\Scripts\Fighter\FighterController.cs"
with open(fc_path, "r", encoding="utf-8") as f:
    text = f.read()

t = """                        if (attackHitbox != null)
                        {
                            bool isGoku = gameObject.name.ToLower().Contains("goku");
                            Vector2 offset = isGoku ? new Vector2(1.8f, 1.0f) : new Vector2(1.2f, 1.0f);
                            Vector2 size = isGoku ? new Vector2(3.0f, 1.4f) : new Vector2(1.6f, 1.2f);
                            attackHitbox.ConfigureAttack(45, 14f, 15f, offset, size);
                        }
                        TransitionTo(FighterState.Ultimate);
                        return;"""

r = """                        bool isGoku = gameObject.name.ToLower().Contains("goku");
                        if (!isGoku)
                        {
                            // Saitama Serious Punch: 1-Hit KO with precision combat hitbox and super freeze
                            attackTimer = 1.60f;
                            attackStartupTimer = 0.55f;
                            attackActiveTimer = 0.45f;
                            if (attackHitbox != null)
                            {
                                attackHitbox.ConfigureAttack(100, 24f, 20f, new Vector2(1.25f, 0.95f), new Vector2(1.4f, 1.0f));
                            }
                            Combat.VFXManager.Instance?.TriggerCameraShake(0.35f, 0.30f);
                            Combat.VFXManager.Instance?.TriggerImpactFlash(0.04f);
                        }
                        else
                        {
                            if (attackHitbox != null)
                            {
                                attackHitbox.ConfigureAttack(45, 14f, 15f, new Vector2(1.8f, 1.0f), new Vector2(3.0f, 1.4f));
                            }
                        }
                        TransitionTo(FighterState.Ultimate);
                        return;"""

if t in text:
    text = text.replace(t, r, 1)
    with open(fc_path, "w", encoding="utf-8") as f:
        f.write(text)
    print("FighterController.cs updated with Saitama Serious Punch 1-Hit KO & camera effects!")
else:
    print("Failed to find target in FighterController.cs!")
