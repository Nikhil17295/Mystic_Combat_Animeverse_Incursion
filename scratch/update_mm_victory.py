mm_path = r"Assets\Scripts\Core\MatchManager.cs"
with open(mm_path, "r", encoding="utf-8") as f:
    text = f.read()

t = """            if (winnerPlayerIndex == 1)
                p1RoundWins++;
            else
                p2RoundWins++;

            Debug.Log($"<color=#ffaa00>[MatchManager]</color> Round {currentRound} WON by Player {winnerPlayerIndex}! (Score: P1={p1RoundWins} | P2={p2RoundWins})");"""

r = """            if (winnerPlayerIndex == 1)
                p1RoundWins++;
            else
                p2RoundWins++;

            // Set winner to Victory state and loser to Knockout state
            Fighter.FighterController winner = winnerPlayerIndex == 1 ? p1Controller : p2Controller;
            Fighter.FighterController loser = winnerPlayerIndex == 1 ? p2Controller : p1Controller;

            if (winner != null) winner.TransitionTo(Fighter.FighterState.Victory);
            if (loser != null) loser.TransitionTo(Fighter.FighterState.Knockout);

            // Visceral K.O. Impact Flash & Screen Shake
            if (Combat.VFXManager.Instance != null)
            {
                Combat.VFXManager.Instance.TriggerImpactFlash(0.08f);
                Combat.VFXManager.Instance.TriggerCameraShake(0.35f, 0.35f);
            }

            Debug.Log($"<color=#ffaa00>[MatchManager]</color> Round {currentRound} WON by Player {winnerPlayerIndex}! (Score: P1={p1RoundWins} | P2={p2RoundWins})");"""

if t in text:
    text = text.replace(t, r, 1)
    with open(mm_path, "w", encoding="utf-8") as f:
        f.write(text)
    print("MatchManager.cs updated with Victory/Knockout transitions and K.O. flash!")
else:
    print("Failed to find target in MatchManager.cs!")
