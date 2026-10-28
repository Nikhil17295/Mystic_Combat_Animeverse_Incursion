anim_path = r"Assets\Scripts\Fighter\FighterAnimator.cs"
with open(anim_path, "r", encoding="utf-8") as f:
    text = f.read()

t1 = """            LoadSequence(framesDir, "hurt", hurtFrames);
            LoadSequence(framesDir, "knockout", knockoutFrames);
            if (knockoutFrames.Count == 0) LoadSequence(framesDir, "ko", knockoutFrames);
            LoadSequence(framesDir, "victory", victoryFrames);"""

r1 = """            LoadSequence(framesDir, "hurt", hurtFrames);
            LoadSequence(framesDir, "death", knockoutFrames);
            if (knockoutFrames.Count == 0) LoadSequence(framesDir, "knockout", knockoutFrames);
            if (knockoutFrames.Count == 0) LoadSequence(framesDir, "ko", knockoutFrames);
            LoadSequence(framesDir, "victory", victoryFrames);

            // Check for new_ultimate frames (e.g. Saitama serious punch windup)
            List<Sprite> newUlt = new List<Sprite>();
            LoadSequence(framesDir, "new_ultimate", newUlt);
            if (newUlt.Count > 0)
            {
                ultimateFrames.InsertRange(0, newUlt);
            }"""

t2 = """                    LoadSequence(ssjDir, "hurt", ssjHurtFrames);
                    LoadSequence(ssjDir, "knockout", ssjKnockoutFrames);
                    if (ssjKnockoutFrames.Count == 0) LoadSequence(ssjDir, "ko", ssjKnockoutFrames);"""

r2 = """                    LoadSequence(ssjDir, "hurt", ssjHurtFrames);
                    LoadSequence(ssjDir, "death", ssjKnockoutFrames);
                    if (ssjKnockoutFrames.Count == 0) LoadSequence(ssjDir, "knockout", ssjKnockoutFrames);
                    if (ssjKnockoutFrames.Count == 0) LoadSequence(ssjDir, "ko", ssjKnockoutFrames);"""

if t1 in text and t2 in text:
    text = text.replace(t1, r1, 1).replace(t2, r2, 1)
    with open(anim_path, "w", encoding="utf-8") as f:
        f.write(text)
    print("FighterAnimator.cs updated with death and new_ultimate loading!")
else:
    print("Failed to find targets in FighterAnimator.cs!")
