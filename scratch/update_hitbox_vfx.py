# Update HitboxController.cs to trigger VFXManager and FighterController.ApplyHit
hbc_path = r"Assets\Scripts\Combat\HitboxController.cs"
with open(hbc_path, "r", encoding="utf-8") as f:
    hbc = f.read()

target_hbc = """            Debug.Log($"<color=#00e676>[COMBAT HIT RESOLVED]</color> {owner?.name} -> {target.name} | Damage: {damage} | New HP: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth} | Attacker Ki: +{kiGainOnHit}");
        }"""

replacement_hbc = """            // 6. Trigger Visual Combat Juice (Hit Sparks, Shockwaves, Camera Shake & Hitstop)
            Vector2 hitCenter = Vector2.Lerp(GetHitboxWorldCenter(), (Vector2)target.position + new Vector2(0f, 0.9f), 0.5f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayHitSpark(hitCenter, isHeavy: damage >= 15);
                if (damage >= 35)
                {
                    VFXManager.Instance.TriggerImpactFlash();
                }
            }

            // 7. Apply Hitstun & Hurt Reaction
            Fighter.FighterController targetFc = target.GetComponent<Fighter.FighterController>();
            if (targetFc != null)
            {
                float stun = damage >= 15 ? 0.42f : 0.25f;
                targetFc.ApplyHit(damage, stun);
            }

            Debug.Log($"<color=#00e676>[COMBAT HIT RESOLVED]</color> {owner?.name} -> {target.name} | Damage: {damage} | New HP: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth} | Attacker Ki: +{kiGainOnHit}");
        }"""

if target_hbc in hbc:
    hbc = hbc.replace(target_hbc, replacement_hbc, 1)
    with open(hbc_path, "w", encoding="utf-8") as f:
        f.write(hbc)
    print("HitboxController.cs updated with VFX & Hitstun triggers!")
else:
    print("HitboxController target NOT found!")
