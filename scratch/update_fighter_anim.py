anim_path = r"Assets\Scripts\Fighter\FighterAnimator.cs"
with open(anim_path, "r", encoding="utf-8") as f:
    text = f.read()

# 1. Frames declaration
t1 = """        private readonly List<Sprite> meditateFrames = new List<Sprite>();
        private Sprite crouchSprite;"""

r1 = """        private readonly List<Sprite> meditateFrames = new List<Sprite>();
        private readonly List<Sprite> hurtFrames = new List<Sprite>();
        private readonly List<Sprite> knockoutFrames = new List<Sprite>();
        private readonly List<Sprite> victoryFrames = new List<Sprite>();
        private Sprite crouchSprite;

        // Super Saiyan Animation Sequences (Goku)
        private readonly List<Sprite> ssjIdleFrames = new List<Sprite>();
        private readonly List<Sprite> ssjWalkFrames = new List<Sprite>();
        private readonly List<Sprite> ssjPunchFrames = new List<Sprite>();
        private readonly List<Sprite> ssjHeavyFrames = new List<Sprite>();
        private readonly List<Sprite> ssjSkillFrames = new List<Sprite>();
        private readonly List<Sprite> ssjUltimateFrames = new List<Sprite>();
        private readonly List<Sprite> ssjJumpFrames = new List<Sprite>();
        private readonly List<Sprite> ssjMeditateFrames = new List<Sprite>();
        private readonly List<Sprite> ssjHurtFrames = new List<Sprite>();
        private readonly List<Sprite> ssjKnockoutFrames = new List<Sprite>();
        private readonly List<Sprite> ssjVictoryFrames = new List<Sprite>();
        private Sprite ssjCrouchSprite;"""

# 2. Loading sequences
t2 = """            // Load multi-frame sequences
            LoadSequence(framesDir, "idle", idleFrames);
            LoadSequence(framesDir, "walk", walkFrames);
            LoadSequence(framesDir, "punch", punchFrames);
            LoadSequence(framesDir, "heavy", heavyFrames);
            LoadSequence(framesDir, "skill", skillFrames);
            LoadSequence(framesDir, "ultimate", ultimateFrames);
            LoadSequence(framesDir, "jump", jumpFrames);
            LoadSequence(framesDir, "meditate", meditateFrames);

            // Load crouch pose
            crouchSprite = LoadSingleOrFirst(framesDir, "crouch");"""

r2 = """            // Load multi-frame sequences
            LoadSequence(framesDir, "idle", idleFrames);
            LoadSequence(framesDir, "walk", walkFrames);
            LoadSequence(framesDir, "punch", punchFrames);
            LoadSequence(framesDir, "heavy", heavyFrames);
            LoadSequence(framesDir, "skill", skillFrames);
            LoadSequence(framesDir, "ultimate", ultimateFrames);
            LoadSequence(framesDir, "jump", jumpFrames);
            LoadSequence(framesDir, "meditate", meditateFrames);
            LoadSequence(framesDir, "hurt", hurtFrames);
            LoadSequence(framesDir, "knockout", knockoutFrames);
            if (knockoutFrames.Count == 0) LoadSequence(framesDir, "ko", knockoutFrames);
            LoadSequence(framesDir, "victory", victoryFrames);

            // Load crouch pose
            crouchSprite = LoadSingleOrFirst(framesDir, "crouch");

            // Load Super Saiyan frames if available (for Goku)
            if (characterFolder.ToLower().Contains("goku"))
            {
                string ssjDir = Path.Combine(Application.dataPath, "Sprites", "Fighters", "Goku", "Frames_SSJ");
                if (Directory.Exists(ssjDir))
                {
                    LoadSequence(ssjDir, "idle", ssjIdleFrames);
                    LoadSequence(ssjDir, "walk", ssjWalkFrames);
                    LoadSequence(ssjDir, "punch", ssjPunchFrames);
                    LoadSequence(ssjDir, "heavy", ssjHeavyFrames);
                    LoadSequence(ssjDir, "skill", ssjSkillFrames);
                    LoadSequence(ssjDir, "ultimate", ssjUltimateFrames);
                    LoadSequence(ssjDir, "jump", ssjJumpFrames);
                    LoadSequence(ssjDir, "meditate", ssjMeditateFrames);
                    LoadSequence(ssjDir, "hurt", ssjHurtFrames);
                    LoadSequence(ssjDir, "knockout", ssjKnockoutFrames);
                    if (ssjKnockoutFrames.Count == 0) LoadSequence(ssjDir, "ko", ssjKnockoutFrames);
                    LoadSequence(ssjDir, "victory", ssjVictoryFrames);
                    ssjCrouchSprite = LoadSingleOrFirst(ssjDir, "crouch");

                    Debug.Log($"[FighterAnimator] Loaded Goku SSJ: {ssjIdleFrames.Count} idle, {ssjWalkFrames.Count} walk, " +
                              $"{ssjPunchFrames.Count} punch, {ssjHeavyFrames.Count} heavy, {ssjSkillFrames.Count} skill frames.");
                }
            }"""

# 3. Helper and UpdateAnimation
t3 = """            switch (currentState)
            {
                case FighterState.Idle:
                case FighterState.Exhausted:
                    PlayLoop(idleFrames, idleFps);
                    break;

                case FighterState.Walking:
                    PlayLoop(walkFrames, walkFps);
                    break;

                case FighterState.Jumping:
                case FighterState.Falling:
                    UpdateJumpPose();
                    break;

                case FighterState.Crouching:
                    if (crouchSprite != null) spriteRenderer.sprite = crouchSprite;
                    break;

                case FighterState.Meditating:
                    if (meditateFrames.Count > 0)
                        PlayLoop(meditateFrames, meditateFps);
                    break;

                case FighterState.LightAttack:
                    PlayOnce(punchFrames, lightAttackFps);
                    break;

                case FighterState.HeavyAttack:
                    PlayOnce(heavyFrames.Count > 0 ? heavyFrames : punchFrames, heavyAttackFps);
                    break;

                case FighterState.Skill:
                    PlayOnce(skillFrames.Count > 0 ? skillFrames : punchFrames, skillFps);
                    break;

                case FighterState.Ultimate:
                    PlayOnce(ultimateFrames.Count > 0 ? ultimateFrames : heavyFrames, ultimateFps);
                    break;
            }"""

r3 = """            bool isSSJ = controller != null && controller.IsSuperSaiyan;

            switch (currentState)
            {
                case FighterState.Idle:
                case FighterState.Exhausted:
                    PlayLoop(GetFrames(ssjIdleFrames, idleFrames), idleFps);
                    break;

                case FighterState.Walking:
                    PlayLoop(GetFrames(ssjWalkFrames, walkFrames), walkFps);
                    break;

                case FighterState.Jumping:
                case FighterState.Falling:
                    UpdateJumpPose();
                    break;

                case FighterState.Crouching:
                    Sprite cSprite = (isSSJ && ssjCrouchSprite != null) ? ssjCrouchSprite : crouchSprite;
                    if (cSprite != null) spriteRenderer.sprite = cSprite;
                    break;

                case FighterState.Meditating:
                    var med = GetFrames(ssjMeditateFrames, meditateFrames);
                    if (med.Count > 0) PlayLoop(med, meditateFps);
                    break;

                case FighterState.LightAttack:
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
                    if (hurt.Count > 0) PlayOnce(hurt, 8f);
                    break;

                case FighterState.Knockout:
                    var ko = GetFrames(ssjKnockoutFrames, knockoutFrames);
                    if (ko.Count > 0) PlayOnce(ko, 6f);
                    break;

                case FighterState.Victory:
                    var vic = GetFrames(ssjVictoryFrames, victoryFrames);
                    if (vic.Count > 0) PlayLoop(vic, 6f);
                    break;
            }"""

# 4. Helper method and UpdateJumpPose
t4 = """        /// <summary>
        /// Selects the jump sprite dynamically based on vertical physics velocity.
        /// </summary>
        private void UpdateJumpPose()
        {
            if (jumpFrames == null || jumpFrames.Count == 0) return;"""

r4 = """        private List<Sprite> GetFrames(List<Sprite> ssjList, List<Sprite> baseList)
        {
            bool isSSJ = controller != null && controller.IsSuperSaiyan;
            return (isSSJ && ssjList != null && ssjList.Count > 0) ? ssjList : baseList;
        }

        /// <summary>
        /// Selects the jump sprite dynamically based on vertical physics velocity.
        /// </summary>
        private void UpdateJumpPose()
        {
            var jFrames = GetFrames(ssjJumpFrames, jumpFrames);
            if (jFrames == null || jFrames.Count == 0) return;"""

# Replace in jump pose references to jumpFrames with jFrames
t5 = """                if (vy > 4.0f)
                {
                    spriteRenderer.sprite = jumpFrames[0]; // Launch
                }
                else if (vy > 1.0f)
                {
                    spriteRenderer.sprite = jumpFrames[1]; // Ascending
                }
                else if (Mathf.Abs(vy) <= 1.0f)
                {
                    spriteRenderer.sprite = jumpFrames[2]; // Apex hangtime
                }
                else if (vy >= -5.0f)
                {
                    spriteRenderer.sprite = jumpFrames[3]; // Descent
                }
                else
                {
                    spriteRenderer.sprite = jumpFrames[jumpFrames.Count - 1]; // Fast descent
                }"""

r5 = """                if (vy > 4.0f)
                {
                    spriteRenderer.sprite = jFrames[0]; // Launch
                }
                else if (vy > 1.0f)
                {
                    spriteRenderer.sprite = jFrames[1]; // Ascending
                }
                else if (Mathf.Abs(vy) <= 1.0f)
                {
                    spriteRenderer.sprite = jFrames[2]; // Apex hangtime
                }
                else if (vy >= -5.0f)
                {
                    spriteRenderer.sprite = jFrames[3]; // Descent
                }
                else
                {
                    spriteRenderer.sprite = jFrames[jFrames.Count - 1]; // Fast descent
                }"""

targets = [t1, t2, t3, t4, t5]
replacements = [r1, r2, r3, r4, r5]

for idx, (t, r) in enumerate(zip(targets, replacements), 1):
    if t in text:
        text = text.replace(t, r, 1)
        print(f"Target {idx} replaced!")
    else:
        print(f"Target {idx} FAILED!")

with open(anim_path, "w", encoding="utf-8") as f:
    f.write(text)
print("FighterAnimator.cs update finished!")
