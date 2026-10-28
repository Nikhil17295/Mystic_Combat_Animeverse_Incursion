path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Fighter\FighterAnimator.cs"
with open(path, "r", encoding="utf-8") as f:
    code = f.read()

# Update PlayLoop and PlayOnce signatures and implementation
old_play = """        private void PlayLoop(List<Sprite> frames, float fps)
        {
            if (frames == null || frames.Count == 0) return;

            animTimer += Time.deltaTime;
            float frameDuration = 1f / fps;

            if (animTimer >= frameDuration)
            {
                animTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Count;
            }

            spriteRenderer.sprite = frames[currentFrameIndex];
        }

        private void PlayOnce(List<Sprite> frames, float fps)
        {
            if (frames == null || frames.Count == 0) return;

            animTimer += Time.deltaTime;
            float frameDuration = 1f / fps;

            if (animTimer >= frameDuration)
            {
                animTimer -= frameDuration;
                if (currentFrameIndex < frames.Count - 1)
                {
                    currentFrameIndex++;
                }
            }

            spriteRenderer.sprite = frames[currentFrameIndex];
        }"""

new_play = """        private void PlayLoop(List<Sprite> frames, float fps, bool unscaled = false)
        {
            if (frames == null || frames.Count == 0) return;

            float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            animTimer += dt;
            float frameDuration = 1f / fps;

            if (animTimer >= frameDuration)
            {
                animTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Count;
            }

            spriteRenderer.sprite = frames[currentFrameIndex];
        }

        private void PlayOnce(List<Sprite> frames, float fps, bool unscaled = false)
        {
            if (frames == null || frames.Count == 0) return;

            float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            animTimer += dt;
            float frameDuration = 1f / fps;

            if (animTimer >= frameDuration)
            {
                animTimer -= frameDuration;
                if (currentFrameIndex < frames.Count - 1)
                {
                    currentFrameIndex++;
                }
            }

            spriteRenderer.sprite = frames[currentFrameIndex];
        }"""

old_switch = """                case FighterState.LightAttack:
                    PlayOnce(GetFrames(ssjPunchFrames, punchFrames), lightAttackFps);
                    break;

                case FighterState.HeavyAttack:
                    var heavy = GetFrames(ssjHeavyFrames, heavyFrames);
                    PlayOnce(heavy.Count > 0 ? heavy : GetFrames(ssjPunchFrames, punchFrames), heavyAttackFps);
                    break;

                case FighterState.Skill:
                    var skill = GetFrames(ssjSkillFrames, skillFrames);
                    PlayOnce(skill.Count > 0 ? skill : GetFrames(ssjPunchFrames, punchFrames), skillFps);
                    break;

                case FighterState.Ultimate:
                    var ult = GetFrames(ssjUltimateFrames, ultimateFrames);
                    PlayOnce(ult.Count > 0 ? ult : GetFrames(ssjHeavyFrames, heavyFrames), ultimateFps);
                    break;

                case FighterState.HitStun:
                case FighterState.KnockBack:
                    var hurt = GetFrames(ssjHurtFrames, hurtFrames);
                    if (hurt.Count > 0) PlayOnce(hurt, hurtFps);
                    break;

                case FighterState.Knockout:
                    var ko = GetFrames(ssjKnockoutFrames, knockoutFrames);
                    if (ko.Count > 0) PlayOnce(ko, deathFps);
                    break;

                case FighterState.Victory:
                    var vic = GetFrames(ssjVictoryFrames, victoryFrames);
                    if (vic.Count > 0) PlayLoop(vic, victoryFps);
                    break;"""

new_switch = """                case FighterState.LightAttack:
                    var lightFrames = GetFrames(ssjPunchFrames, punchFrames);
                    float lightSpeed = lightFrames.Count > 0 ? Mathf.Max(6f, (float)lightFrames.Count / 0.44f) : lightAttackFps;
                    PlayOnce(lightFrames, lightSpeed);
                    break;

                case FighterState.HeavyAttack:
                    var heavy = GetFrames(ssjHeavyFrames, heavyFrames);
                    var heavyList = heavy.Count > 0 ? heavy : GetFrames(ssjPunchFrames, punchFrames);
                    float heavySpeed = heavyList.Count > 0 ? Mathf.Max(5f, (float)heavyList.Count / 0.88f) : heavyAttackFps;
                    PlayOnce(heavyList, heavySpeed);
                    break;

                case FighterState.Skill:
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
                    break;

                case FighterState.Victory:
                    var vic = GetFrames(ssjVictoryFrames, victoryFrames);
                    if (vic.Count > 0) PlayLoop(vic, victoryFps, unscaled: true);
                    break;"""

code_norm = code.replace("\r\n", "\n")
old_play_norm = old_play.replace("\r\n", "\n")
new_play_norm = new_play.replace("\r\n", "\n")
old_switch_norm = old_switch.replace("\r\n", "\n")
new_switch_norm = new_switch.replace("\r\n", "\n")

if old_play_norm in code_norm:
    code_norm = code_norm.replace(old_play_norm, new_play_norm, 1)
    print("Replaced PlayLoop/PlayOnce!")
else:
    print("Failed to find old_play_norm!")

if old_switch_norm in code_norm:
    code_norm = code_norm.replace(old_switch_norm, new_switch_norm, 1)
    print("Replaced switch cases!")
else:
    print("Failed to find old_switch_norm!")

with open(path, "w", encoding="utf-8") as f:
    f.write(code_norm)
print("Updated FighterAnimator.cs successfully!")
