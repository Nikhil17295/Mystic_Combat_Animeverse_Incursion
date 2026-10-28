# 1. Update MatchManager.cs
mm_path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Core\MatchManager.cs"
with open(mm_path, "r", encoding="utf-8") as f:
    mm_code = f.read()

mm_code = mm_code.replace("Time.timeScale = 0.28f;", "Time.timeScale = 0.12f;")
mm_code = mm_code.replace("[SerializeField] private float roundEndPauseDuration = 2.8f;", "[SerializeField] private float roundEndPauseDuration = 4.0f;")

old_mm_seq = """        private System.Collections.IEnumerator RoundEndSequence(Fighter.FighterController winner, Fighter.FighterController loser)
        {
            // Phase 1: Let the knockout collapse animation complete in slow motion
            yield return new WaitForSecondsRealtime(1.8f);

            // Phase 2: The character hits the floor -> snap physics back to normal
            Time.timeScale = 1.0f;
            if (loser != null && Combat.VFXManager.Instance != null)
            {
                Combat.VFXManager.Instance.PlayDustPuff(loser.transform.position);
                Combat.VFXManager.Instance.TriggerCameraShake(0.20f, 0.20f);
            }

            // Phase 3: Winner starts their celebratory victory animation loop
            if (winner != null)
            {
                winner.TransitionTo(Fighter.FighterState.Victory);
            }

            // Phase 4: Hold for dramatic victory pause to let things sink in
            yield return new WaitForSeconds(roundEndPauseDuration);

            CheckWinCondition();
        }"""

new_mm_seq = """        private System.Collections.IEnumerator RoundEndSequence(Fighter.FighterController winner, Fighter.FighterController loser)
        {
            // Phase 1: Cinematic slow-motion collapse (4.0 seconds in real time, unhurried and dramatic)
            yield return new WaitForSecondsRealtime(4.0f);

            // Phase 2: Defeated fighter strikes the floor -> snap physics back to normal
            Time.timeScale = 1.0f;
            if (loser != null && Combat.VFXManager.Instance != null)
            {
                Combat.VFXManager.Instance.PlayDustPuff(loser.transform.position);
                Combat.VFXManager.Instance.TriggerCameraShake(0.28f, 0.28f);
            }

            // Dramatic floor hold: let the defeat register while the defeated fighter lies motionless
            yield return new WaitForSeconds(1.5f);

            // Phase 3: Victorious fighter transitions into their celebration victory loop
            if (winner != null)
            {
                winner.TransitionTo(Fighter.FighterState.Victory);
            }

            // Phase 4: Savor the victory celebration (4.0s hold) before transitioning
            yield return new WaitForSeconds(roundEndPauseDuration);

            CheckWinCondition();
        }"""

mm_norm = mm_code.replace("\r\n", "\n")
old_mm_norm = old_mm_seq.replace("\r\n", "\n")
new_mm_norm = new_mm_seq.replace("\r\n", "\n")
if old_mm_norm in mm_norm:
    mm_norm = mm_norm.replace(old_mm_norm, new_mm_norm, 1)
    print("Updated MatchManager sequence!")
else:
    print("Could not find old_mm_seq in MatchManager!")

with open(mm_path, "w", encoding="utf-8") as f:
    f.write(mm_norm)

# 2. Update FighterController.cs
fc_path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Fighter\FighterController.cs"
with open(fc_path, "r", encoding="utf-8") as f:
    fc_code = f.read()

# Replace Ultimate attack timings for Goku and Saitama
old_fc_ult = """                        attackTimer = 1.45f;
                        attackStartupTimer = 0.45f;
                        attackActiveTimer = 0.50f;
                        bool isGoku = gameObject.name.ToLower().Contains("goku");
                        if (!isGoku)
                        {
                            // Saitama Serious Punch: 1-Hit KO with precision combat hitbox and super freeze
                            attackTimer = 1.60f;
                            attackStartupTimer = 0.55f;
                            attackActiveTimer = 0.45f;"""

new_fc_ult = """                        bool isGoku = gameObject.name.ToLower().Contains("goku");
                        if (!isGoku)
                        {
                            // Saitama Serious Punch: Synced to voice audio (3.80s total)
                            attackTimer = 3.80f;
                            attackStartupTimer = 2.40f; // Chilling calm delivery: "Death... Serious Series..."
                            attackActiveTimer = 1.40f;  // Devastating punch on "SERIOUS PUNCH!" """

old_fc_goku_ult = """                        else
                        {
                            if (attackHitbox != null)
                            {
                                attackHitbox.ConfigureAttack(45, 14f, 15f, new Vector2(1.8f, 1.0f), new Vector2(3.0f, 1.4f));
                            }
                        }"""

new_fc_goku_ult = """                        else
                        {
                            // Goku Kamehameha: Synced to voice audio (3.36s total)
                            attackTimer = 3.36f;
                            attackStartupTimer = 2.15f; // Charging ki in cupped hands: "KA... ME... HA... ME..."
                            attackActiveTimer = 1.21f;  // Blasting beam on "HA-A-A-A!"
                            if (attackHitbox != null)
                            {
                                attackHitbox.ConfigureAttack(45, 14f, 15f, new Vector2(1.8f, 1.0f), new Vector2(3.0f, 1.4f));
                            }
                        }"""

# Replace Saitama Skill timing
old_fc_skill = """                        attackTimer = 0.85f;
                        attackStartupTimer = 0.10f;
                        attackActiveTimer = 0.60f;"""

new_fc_skill = """                        // Synced to audio: 2.70s for Saitama Consecutive Punches!
                        attackTimer = isGoku ? 0.85f : 2.70f;
                        attackStartupTimer = 0.15f;
                        attackActiveTimer = isGoku ? 0.60f : 2.50f;"""

fc_norm = fc_code.replace("\r\n", "\n")
old_fc_ult_norm = old_fc_ult.replace("\r\n", "\n")
new_fc_ult_norm = new_fc_ult.replace("\r\n", "\n")
old_fc_goku_ult_norm = old_fc_goku_ult.replace("\r\n", "\n")
new_fc_goku_ult_norm = new_fc_goku_ult.replace("\r\n", "\n")
old_fc_skill_norm = old_fc_skill.replace("\r\n", "\n")
new_fc_skill_norm = new_fc_skill.replace("\r\n", "\n")

if old_fc_ult_norm in fc_norm:
    fc_norm = fc_norm.replace(old_fc_ult_norm, new_fc_ult_norm, 1)
    print("Updated FC ult timings!")
else:
    print("Could not find old_fc_ult!")

if old_fc_goku_ult_norm in fc_norm:
    fc_norm = fc_norm.replace(old_fc_goku_ult_norm, new_fc_goku_ult_norm, 1)
    print("Updated FC Goku ult!")
else:
    print("Could not find old_fc_goku_ult!")

if old_fc_skill_norm in fc_norm:
    fc_norm = fc_norm.replace(old_fc_skill_norm, new_fc_skill_norm, 1)
    print("Updated FC skill timing!")
else:
    print("Could not find old_fc_skill!")

with open(fc_path, "w", encoding="utf-8") as f:
    f.write(fc_norm)

# 3. Update FighterAnimator.cs
anim_path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Fighter\FighterAnimator.cs"
with open(anim_path, "r", encoding="utf-8") as f:
    anim_code = f.read()

old_anim_cases = """                case FighterState.Skill:
                    var skill = GetFrames(ssjSkillFrames, skillFrames);
                    var skillList = skill.Count > 0 ? skill : GetFrames(ssjPunchFrames, punchFrames);
                    float skillSpeed = skillList.Count > 0 ? Mathf.Max(6f, (float)skillList.Count / 0.85f) : skillFps;
                    PlayOnce(skillList, skillSpeed);
                    break;

                case FighterState.Ultimate:
                    var ult = GetFrames(ssjUltimateFrames, ultimateFrames);
                    var ultList = ult.Count > 0 ? ult : GetFrames(ssjHeavyFrames, heavyFrames);
                    float ultDuration = gameObject.name.ToLower().Contains("goku") ? 1.45f : 1.60f;
                    float ultSpeed = ultList.Count > 0 ? Mathf.Max(5f, (float)ultList.Count / ultDuration) : ultimateFps;
                    PlayOnce(ultList, ultSpeed);
                    break;

                case FighterState.HitStun:
                case FighterState.KnockBack:
                    var hurt = GetFrames(ssjHurtFrames, hurtFrames);
                    if (hurt.Count > 0) PlayOnce(hurt, hurtFps);
                    break;

                case FighterState.Knockout:
                    var ko = GetFrames(ssjKnockoutFrames, knockoutFrames);
                    if (ko.Count > 0)
                    {
                        // Ensure all frames play fluidly over the 1.7s slow-motion collapse window
                        float koSpeed = Mathf.Max(3f, (float)ko.Count / 1.7f);
                        PlayOnce(ko, koSpeed, unscaled: true);
                    }
                    break;"""

new_anim_cases = """                case FighterState.Skill:
                    var skill = GetFrames(ssjSkillFrames, skillFrames);
                    var skillList = skill.Count > 0 ? skill : GetFrames(ssjPunchFrames, punchFrames);
                    if (gameObject.name.ToLower().Contains("saitama"))
                    {
                        // Rapid looping punch flurry for the entire 2.70s shout
                        PlayLoop(skillList, 11f);
                    }
                    else
                    {
                        float skillSpeed = skillList.Count > 0 ? Mathf.Max(5f, (float)skillList.Count / 0.85f) : skillFps;
                        PlayOnce(skillList, skillSpeed);
                    }
                    break;

                case FighterState.Ultimate:
                    var ult = GetFrames(ssjUltimateFrames, ultimateFrames);
                    var ultList = ult.Count > 0 ? ult : GetFrames(ssjHeavyFrames, heavyFrames);
                    // Match animation progression to exact voice audio duration!
                    float ultDuration = gameObject.name.ToLower().Contains("goku") ? 3.36f : 3.80f;
                    float ultSpeed = ultList.Count > 0 ? Mathf.Max(2.5f, (float)ultList.Count / ultDuration) : ultimateFps;
                    PlayOnce(ultList, ultSpeed);
                    break;

                case FighterState.HitStun:
                case FighterState.KnockBack:
                    var hurt = GetFrames(ssjHurtFrames, hurtFrames);
                    if (hurt.Count > 0) PlayOnce(hurt, hurtFps);
                    break;

                case FighterState.Knockout:
                    var ko = GetFrames(ssjKnockoutFrames, knockoutFrames);
                    if (ko.Count > 0)
                    {
                        // Unhurried cinematic collapse over 3.8 real seconds
                        float koSpeed = Mathf.Max(2.5f, (float)ko.Count / 3.8f);
                        PlayOnce(ko, koSpeed, unscaled: true);
                    }
                    break;"""

anim_norm = anim_code.replace("\r\n", "\n")
old_anim_cases_norm = old_anim_cases.replace("\r\n", "\n")
new_anim_cases_norm = new_anim_cases.replace("\r\n", "\n")

if old_anim_cases_norm in anim_norm:
    anim_norm = anim_norm.replace(old_anim_cases_norm, new_anim_cases_norm, 1)
    print("Updated FighterAnimator cases!")
else:
    print("Could not find old_anim_cases!")

with open(anim_path, "w", encoding="utf-8") as f:
    f.write(anim_norm)
print("Updated all scripts successfully!")
