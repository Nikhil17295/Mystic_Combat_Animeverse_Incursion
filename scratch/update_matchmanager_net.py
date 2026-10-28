with open('Assets/Scripts/Core/MatchManager.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = """        public void RestartMatch()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            StopAllCoroutines();
            currentMatchState = MatchState.WaitingToStart;

            StartMatch();
        }"""

replacement = """        public void RestartMatch()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            StopAllCoroutines();
            currentMatchState = MatchState.WaitingToStart;

            StartMatch();
        }

        /// <summary>
        /// Synchronizes the round clock, round number, and score from an authoritative LAN host.
        /// </summary>
        public void SyncFromNetwork(int round, float timer, MatchState state, int p1Wins, int p2Wins)
        {
            currentRound = round;
            roundTimer = timer;
            currentMatchState = state;
            p1RoundWins = p1Wins;
            p2RoundWins = p2Wins;
            OnTimerUpdate?.Invoke(roundTimer);
        }"""

text = text.replace('\r\n', '\n')
target = target.replace('\r\n', '\n')
replacement = replacement.replace('\r\n', '\n')

if target in text:
    text = text.replace(target, replacement)
    with open('Assets/Scripts/Core/MatchManager.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("MatchManager updated successfully.")
else:
    print("Target not found in MatchManager.")
