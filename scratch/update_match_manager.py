import re

path = r"b:\Unity\Projects\Mystic_Combat_Animeverse_Incursion\Assets\Scripts\Core\MatchManager.cs"
with open(path, "r", encoding="utf-8") as f:
    code = f.read()

# 1. Update roundDuration and roundEndPauseDuration
code = code.replace(
    "[SerializeField] private float roundDuration = 99f;",
    "[SerializeField] private float roundDuration = 199f;"
)
code = code.replace(
    "[SerializeField] private float roundEndPauseDuration = 1.8f;",
    "[SerializeField] private float roundEndPauseDuration = 2.8f;"
)

# 2. In StartRound, ensure Time.timeScale is reset to 1f
target_start_round = "public void StartRound()\n        {"
if target_start_round not in code:
    target_start_round = "public void StartRound()\r\n        {"

replacement_start_round = target_start_round + "\n            Time.timeScale = 1.0f;"
code = code.replace(target_start_round, replacement_start_round, 1)

# 3. Update EndRound and RoundEndSequence
old_end_seq = """        public void EndRound(int winnerPlayerIndex)
        {
            if (currentMatchState == MatchState.RoundEnd || currentMatchState == MatchState.MatchOver) return;

            currentMatchState = MatchState.RoundEnd;

            if (winnerPlayerIndex == 1)
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

            Debug.Log($"<color=#ffaa00>[MatchManager]</color> Round {currentRound} WON by Player {winnerPlayerIndex}! (Score: P1={p1RoundWins} | P2={p2RoundWins})");

            OnRoundEnd?.Invoke(winnerPlayerIndex, currentRound);

            StartCoroutine(RoundEndSequence(winnerPlayerIndex));
        }

        private System.Collections.IEnumerator RoundEndSequence(int winnerPlayerIndex)
        {
            // Dramatic pause for K.O. celebration and readability
            yield return new WaitForSeconds(roundEndPauseDuration);

            CheckWinCondition();
        }"""

new_end_seq = """        public void EndRound(int winnerPlayerIndex)
        {
            if (currentMatchState == MatchState.RoundEnd || currentMatchState == MatchState.MatchOver) return;

            currentMatchState = MatchState.RoundEnd;

            if (winnerPlayerIndex == 1)
                p1RoundWins++;
            else
                p2RoundWins++;

            Fighter.FighterController winner = winnerPlayerIndex == 1 ? p1Controller : p2Controller;
            Fighter.FighterController loser = winnerPlayerIndex == 1 ? p2Controller : p1Controller;

            // Phase 1: Loser enters Knockout immediately to trigger full multi-frame collapse animation
            if (loser != null) loser.TransitionTo(Fighter.FighterState.Knockout);

            // Dramatic Slow-Motion Knockout effect (0.28x physics speed)
            Time.timeScale = 0.28f;

            // Visceral K.O. Impact Flash & Screen Shake
            if (Combat.VFXManager.Instance != null)
            {
                Combat.VFXManager.Instance.TriggerImpactFlash(0.12f);
                Combat.VFXManager.Instance.TriggerCameraShake(0.40f, 0.40f);
            }

            Debug.Log($"<color=#ffaa00>[MatchManager]</color> Round {currentRound} WON by Player {winnerPlayerIndex}! (Score: P1={p1RoundWins} | P2={p2RoundWins})");

            OnRoundEnd?.Invoke(winnerPlayerIndex, currentRound);

            StartCoroutine(RoundEndSequence(winner, loser));
        }

        private System.Collections.IEnumerator RoundEndSequence(Fighter.FighterController winner, Fighter.FighterController loser)
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

# Normalize CRLF for replacement
code_normalized = code.replace("\r\n", "\n")
old_normalized = old_end_seq.replace("\r\n", "\n")
new_normalized = new_end_seq.replace("\r\n", "\n")

if old_normalized in code_normalized:
    code_normalized = code_normalized.replace(old_normalized, new_normalized, 1)
    print("Replaced EndRound and RoundEndSequence successfully!")
else:
    print("Could not find old_normalized! Check exact text.")

with open(path, "w", encoding="utf-8") as f:
    f.write(code_normalized)
print("Updated MatchManager.cs")
