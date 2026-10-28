with open('Assets/Scripts/UI/CharacterSelectController.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Fix 1: Add #endregion to OnDisable()
target1 = """        private void OnDisable()
        {
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnCharSelectSyncReceived -= HandleNetworkCharSelectSync;
                LanNetworkManager.Instance.OnMatchStartReceived -= HandleNetworkMatchStart;
            }
        }

        #region Unity Callbacks"""

replacement1 = """        private void OnDisable()
        {
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnCharSelectSyncReceived -= HandleNetworkCharSelectSync;
                LanNetworkManager.Instance.OnMatchStartReceived -= HandleNetworkMatchStart;
            }
        }

        #endregion

        #region Unity Callbacks"""

# Fix 2: Close Input Handling before LAN Network Callbacks, and close LAN Network Callbacks before SelectRandomCpuAndLaunch
target2 = """        #region LAN Network Callbacks

        private void HandleNetworkCharSelectSync(CharSelectSyncPacket pkt)"""

replacement2 = """        #endregion

        #region LAN Network Callbacks

        private void HandleNetworkCharSelectSync(CharSelectSyncPacket pkt)"""

target3 = """        private void HandleNetworkMatchStart(MatchStartPacket pkt)
        {
            int idx1 = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P1CharacterId, System.StringComparison.OrdinalIgnoreCase));
            int idx2 = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P2CharacterId, System.StringComparison.OrdinalIgnoreCase));
            if (idx1 >= 0) p1SelectedIndex = idx1;
            if (idx2 >= 0) p2SelectedIndex = idx2;

            currentPhase = SelectionPhase.ReadyLaunch;
            StartCoroutine(LaunchMatchSequence());
        }

        #endregion

        /// <summary>
        /// Instantly chooses a random character for the CPU and launches the match!
        /// </summary>"""

replacement3 = """        private void HandleNetworkMatchStart(MatchStartPacket pkt)
        {
            int idx1 = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P1CharacterId, System.StringComparison.OrdinalIgnoreCase));
            int idx2 = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P2CharacterId, System.StringComparison.OrdinalIgnoreCase));
            if (idx1 >= 0) p1SelectedIndex = idx1;
            if (idx2 >= 0) p2SelectedIndex = idx2;

            currentPhase = SelectionPhase.ReadyLaunch;
            StartCoroutine(LaunchMatchSequence());
        }

        #endregion

        #region Random & Difficulty Selection

        /// <summary>
        /// Instantly chooses a random character for the CPU and launches the match!
        /// </summary>"""

text = text.replace('\r\n', '\n')
target1 = target1.replace('\r\n', '\n')
target2 = target2.replace('\r\n', '\n')
target3 = target3.replace('\r\n', '\n')
replacement1 = replacement1.replace('\r\n', '\n')
replacement2 = replacement2.replace('\r\n', '\n')
replacement3 = replacement3.replace('\r\n', '\n')

if target1 in text and target2 in text and target3 in text:
    text = text.replace(target1, replacement1).replace(target2, replacement2).replace(target3, replacement3)
    with open('Assets/Scripts/UI/CharacterSelectController.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("CharacterSelectController regions fixed.")
else:
    print("Targets for regions not found in CharacterSelectController.")
