with open(r'Assets\Scripts\Fighter\FighterController.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = """                case FighterState.Meditating:
                    fighterAudio?.StartKiMeditationAura();
                    break;"""

rep = """                case FighterState.Meditating:
                    fighterAudio?.StartKiMeditationAura();
                    break;

                case FighterState.Victory:
                    if (gameObject.name.ToLower().Contains("saitama"))
                    {
                        fighterAudio?.PlayJump();
                    }
                    break;"""

if target in text:
    new_text = text.replace(target, rep)
elif target.replace('\n', '\r\n') in text:
    new_text = text.replace(target.replace('\n', '\r\n'), rep.replace('\n', '\r\n'))
else:
    new_text = None

if new_text:
    with open(r'Assets\Scripts\Fighter\FighterController.cs', 'w', encoding='utf-8', newline='') as f:
        f.write(new_text)
    print("Updated FighterController.cs successfully!")
else:
    print("Target not found")
