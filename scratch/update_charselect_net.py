with open('Assets/Scripts/UI/CharacterSelectController.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target1 = """using MysticCombat.AI;"""

replacement1 = """using MysticCombat.AI;
using MysticCombat.Network;"""

target2 = """        private bool isTransitioning = false;

        #endregion"""

replacement2 = """        private bool isTransitioning = false;
        private bool isLanHostLocked = false;
        private bool isLanClientLocked = false;

        #endregion

        #region Unity Lifecycle Events

        private void OnEnable()
        {
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnCharSelectSyncReceived += HandleNetworkCharSelectSync;
                LanNetworkManager.Instance.OnMatchStartReceived += HandleNetworkMatchStart;
            }
        }

        private void OnDisable()
        {
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnCharSelectSyncReceived -= HandleNetworkCharSelectSync;
                LanNetworkManager.Instance.OnMatchStartReceived -= HandleNetworkMatchStart;
            }
        }"""

target3 = """            currentGameMode = mode;
            currentPhase = SelectionPhase.SelectingP1;
            p1SelectedIndex = 0;
            p2SelectedIndex = (currentGameMode == GameManager.GameMode.VsCpu) ? 1 : 1;
            isTransitioning = false;"""

replacement3 = """            currentGameMode = mode;
            isTransitioning = false;
            isLanHostLocked = false;
            isLanClientLocked = false;

            if (currentGameMode == GameManager.GameMode.LanPvP && GameManager.Instance != null && !GameManager.Instance.IsLanHost)
            {
                // Client starts directly on P2 selection!
                currentPhase = SelectionPhase.SelectingP2OrCpu;
                p1SelectedIndex = 0;
                p2SelectedIndex = 1;
            }
            else
            {
                currentPhase = SelectionPhase.SelectingP1;
                p1SelectedIndex = 0;
                p2SelectedIndex = (currentGameMode == GameManager.GameMode.VsCpu) ? 1 : 1;
            }"""

target4 = """        private void Navigate(int direction)
        {
            if (fighterCards == null || fighterCards.Count == 0) return;

            PlaySfx(navigateClip);

            if (currentPhase == SelectionPhase.SelectingP1)
            {
                p1SelectedIndex = (p1SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
            }
            else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
            {
                p2SelectedIndex = (p2SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
            }

            UpdateUI();
        }"""

replacement4 = """        private void Navigate(int direction)
        {
            if (fighterCards == null || fighterCards.Count == 0) return;

            PlaySfx(navigateClip);

            if (currentGameMode == GameManager.GameMode.LanPvP)
            {
                bool isHost = GameManager.Instance == null || GameManager.Instance.IsLanHost;
                if (isHost && !isLanHostLocked)
                {
                    p1SelectedIndex = (p1SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
                    if (LanNetworkManager.Instance != null)
                    {
                        LanNetworkManager.Instance.SendCharSelectSync(new CharSelectSyncPacket
                        {
                            PlayerIndex = 1,
                            CharacterId = fighterCards[p1SelectedIndex].characterId,
                            IsLockedIn = false
                        });
                    }
                }
                else if (!isHost && !isLanClientLocked)
                {
                    p2SelectedIndex = (p2SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
                    if (LanNetworkManager.Instance != null)
                    {
                        LanNetworkManager.Instance.SendCharSelectSync(new CharSelectSyncPacket
                        {
                            PlayerIndex = 2,
                            CharacterId = fighterCards[p2SelectedIndex].characterId,
                            IsLockedIn = false
                        });
                    }
                }
            }
            else
            {
                if (currentPhase == SelectionPhase.SelectingP1)
                {
                    p1SelectedIndex = (p1SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
                }
                else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
                {
                    p2SelectedIndex = (p2SelectedIndex + direction + fighterCards.Count) % fighterCards.Count;
                }
            }

            UpdateUI();
        }"""

target5 = """        public void ConfirmCurrentSelection()
        {
            PlaySfx(lockInClip);

            if (currentPhase == SelectionPhase.SelectingP1)
            {
                currentPhase = SelectionPhase.SelectingP2OrCpu;
                UpdateUI();
            }
            else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
            {
                currentPhase = SelectionPhase.ReadyLaunch;
                StartCoroutine(LaunchMatchSequence());
            }
        }"""

replacement5 = """        public void ConfirmCurrentSelection()
        {
            PlaySfx(lockInClip);

            if (currentGameMode == GameManager.GameMode.LanPvP)
            {
                bool isHost = GameManager.Instance == null || GameManager.Instance.IsLanHost;
                if (isHost)
                {
                    isLanHostLocked = true;
                    if (LanNetworkManager.Instance != null)
                    {
                        LanNetworkManager.Instance.SendCharSelectSync(new CharSelectSyncPacket
                        {
                            PlayerIndex = 1,
                            CharacterId = fighterCards[p1SelectedIndex].characterId,
                            IsLockedIn = true
                        });
                    }

                    if (isLanClientLocked)
                    {
                        if (LanNetworkManager.Instance != null)
                        {
                            LanNetworkManager.Instance.SendMatchStart(new MatchStartPacket
                            {
                                P1CharacterId = fighterCards[p1SelectedIndex].characterId,
                                P2CharacterId = fighterCards[p2SelectedIndex].characterId
                            });
                        }
                        currentPhase = SelectionPhase.ReadyLaunch;
                        StartCoroutine(LaunchMatchSequence());
                    }
                    else
                    {
                        if (selectionPromptText != null)
                        {
                            selectionPromptText.text = "<color=#ffd700>P1 LOCKED IN! WAITING FOR OPPONENT TO LOCK IN...</color>";
                        }
                    }
                }
                else // Client
                {
                    isLanClientLocked = true;
                    if (LanNetworkManager.Instance != null)
                    {
                        LanNetworkManager.Instance.SendCharSelectSync(new CharSelectSyncPacket
                        {
                            PlayerIndex = 2,
                            CharacterId = fighterCards[p2SelectedIndex].characterId,
                            IsLockedIn = true
                        });
                    }

                    if (selectionPromptText != null)
                    {
                        selectionPromptText.text = "<color=#00f5ff>P2 LOCKED IN! WAITING FOR HOST TO START MATCH...</color>";
                    }
                }
                return;
            }

            if (currentPhase == SelectionPhase.SelectingP1)
            {
                currentPhase = SelectionPhase.SelectingP2OrCpu;
                UpdateUI();
            }
            else if (currentPhase == SelectionPhase.SelectingP2OrCpu)
            {
                currentPhase = SelectionPhase.ReadyLaunch;
                StartCoroutine(LaunchMatchSequence());
            }
        }

        #region LAN Network Callbacks

        private void HandleNetworkCharSelectSync(CharSelectSyncPacket pkt)
        {
            int foundIdx = fighterCards.FindIndex(c => c.characterId.Equals(pkt.CharacterId, System.StringComparison.OrdinalIgnoreCase));
            if (foundIdx < 0) foundIdx = 0;

            if (pkt.PlayerIndex == 1)
            {
                p1SelectedIndex = foundIdx;
                isLanHostLocked = pkt.IsLockedIn;
            }
            else if (pkt.PlayerIndex == 2)
            {
                p2SelectedIndex = foundIdx;
                isLanClientLocked = pkt.IsLockedIn;
            }

            UpdateUI();

            bool isHost = GameManager.Instance == null || GameManager.Instance.IsLanHost;
            if (isHost && isLanHostLocked && isLanClientLocked && !isTransitioning)
            {
                if (LanNetworkManager.Instance != null)
                {
                    LanNetworkManager.Instance.SendMatchStart(new MatchStartPacket
                    {
                        P1CharacterId = fighterCards[p1SelectedIndex].characterId,
                        P2CharacterId = fighterCards[p2SelectedIndex].characterId
                    });
                }
                currentPhase = SelectionPhase.ReadyLaunch;
                StartCoroutine(LaunchMatchSequence());
            }
        }

        private void HandleNetworkMatchStart(MatchStartPacket pkt)
        {
            int idx1 = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P1CharacterId, System.StringComparison.OrdinalIgnoreCase));
            int idx2 = fighterCards.FindIndex(c => c.characterId.Equals(pkt.P2CharacterId, System.StringComparison.OrdinalIgnoreCase));
            if (idx1 >= 0) p1SelectedIndex = idx1;
            if (idx2 >= 0) p2SelectedIndex = idx2;

            currentPhase = SelectionPhase.ReadyLaunch;
            StartCoroutine(LaunchMatchSequence());
        }

        #endregion"""

text = text.replace('\r\n', '\n')
target1 = target1.replace('\r\n', '\n')
target2 = target2.replace('\r\n', '\n')
target3 = target3.replace('\r\n', '\n')
target4 = target4.replace('\r\n', '\n')
target5 = target5.replace('\r\n', '\n')

replacement1 = replacement1.replace('\r\n', '\n')
replacement2 = replacement2.replace('\r\n', '\n')
replacement3 = replacement3.replace('\r\n', '\n')
replacement4 = replacement4.replace('\r\n', '\n')
replacement5 = replacement5.replace('\r\n', '\n')

if all(t in text for t in [target1, target2, target3, target4, target5]):
    for t, r in zip([target1, target2, target3, target4, target5], [replacement1, replacement2, replacement3, replacement4, replacement5]):
        text = text.replace(t, r)
    with open('Assets/Scripts/UI/CharacterSelectController.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("CharacterSelectController updated successfully.")
else:
    print("Not all targets found in CharacterSelectController.")
