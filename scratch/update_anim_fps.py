anim_path = r"Assets\Scripts\Fighter\FighterAnimator.cs"
with open(anim_path, "r", encoding="utf-8") as f:
    text = f.read()

t1 = """        [Tooltip("Ki meditation channel loop speed.")]
        [SerializeField] private float meditateFps = 7f;"""

r1 = """        [Tooltip("Ki meditation channel loop speed.")]
        [SerializeField] private float meditateFps = 7f;

        [Tooltip("Hurt reaction playback speed (snappy read).")]
        [SerializeField] private float hurtFps = 8.5f;

        [Tooltip("Knockout collapse speed.")]
        [SerializeField] private float deathFps = 6f;

        [Tooltip("Victory celebratory loop speed.")]
        [SerializeField] private float victoryFps = 6f;"""

t2 = """                case FighterState.HitStun:
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
                    break;"""

r2 = """                case FighterState.HitStun:
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

if t1 in text and t2 in text:
    text = text.replace(t1, r1, 1).replace(t2, r2, 1)
    with open(anim_path, "w", encoding="utf-8") as f:
        f.write(text)
    print("FighterAnimator.cs updated with configurable FPS fields!")
else:
    print("Failed to find targets in FighterAnimator.cs!")
