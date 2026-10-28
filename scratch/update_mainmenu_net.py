with open('Assets/Scripts/UI/MainMenuController.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target1 = """        [Header("Options & Controls Modal")]
        [SerializeField] private GameObject optionsPanel;"""

replacement1 = """        [Header("Options & Controls Modal")]
        [SerializeField] private GameObject optionsPanel;

        [Header("LAN Multiplayer Modal")]
        [SerializeField] private LanLobbyModal lanLobbyModal;"""

target2 = """            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseOptions();
                }
                return;
            }"""

replacement2 = """            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseOptions();
                }
                return;
            }

            if (lanLobbyModal != null && lanLobbyModal.gameObject.activeSelf)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    lanLobbyModal.Close();
                }
                return;
            }

            // Quick Hotkey [L] for instant LAN Duel Lobby
            if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                OpenLanLobby();
                return;
            }"""

target3 = """        private void ExecuteSelection(int index)
        {
            if (enableMenuConfirmAudio)
            {
                PlaySelectAudio();
            }

            switch (index)
            {
                case 0:
                    StartGameMode(GameManager.GameMode.VsCpu);
                    break;
                case 1:
                    StartGameMode(GameManager.GameMode.LocalPvP);
                    break;
                case 2:
                    OpenOptions();
                    break;
                case 3:
                    QuitGame();
                    break;
            }
        }"""

replacement3 = """        private void ExecuteSelection(int index)
        {
            if (enableMenuConfirmAudio)
            {
                PlaySelectAudio();
            }

            if (menuItems.Count >= 5)
            {
                switch (index)
                {
                    case 0:
                        StartGameMode(GameManager.GameMode.VsCpu);
                        break;
                    case 1:
                        StartGameMode(GameManager.GameMode.LocalPvP);
                        break;
                    case 2:
                        OpenLanLobby();
                        break;
                    case 3:
                        OpenOptions();
                        break;
                    case 4:
                        QuitGame();
                        break;
                }
            }
            else
            {
                switch (index)
                {
                    case 0:
                        StartGameMode(GameManager.GameMode.VsCpu);
                        break;
                    case 1:
                        StartGameMode(GameManager.GameMode.LocalPvP);
                        break;
                    case 2:
                        OpenOptions();
                        break;
                    case 3:
                        QuitGame();
                        break;
                }
            }
        }

        /// <summary>
        /// Opens the LAN Lobby Modal to Host or Join local network battles.
        /// </summary>
        public void OpenLanLobby()
        {
            Debug.Log("<color=#00e676>[MainMenuController]</color> Opening LAN Multiplayer Lobby...");

            if (lanLobbyModal == null)
            {
                GameObject go = new GameObject("LanLobbyModal");
                go.transform.SetParent(transform, false);
                lanLobbyModal = go.AddComponent<LanLobbyModal>();
            }

            if (menuRoot != null) menuRoot.SetActive(false);
            lanLobbyModal.Open();
        }"""

text = text.replace('\r\n', '\n')
target1 = target1.replace('\r\n', '\n')
target2 = target2.replace('\r\n', '\n')
target3 = target3.replace('\r\n', '\n')
replacement1 = replacement1.replace('\r\n', '\n')
replacement2 = replacement2.replace('\r\n', '\n')
replacement3 = replacement3.replace('\r\n', '\n')

if target1 in text and target2 in text and target3 in text:
    text = text.replace(target1, replacement1).replace(target2, replacement2).replace(target3, replacement3)
    with open('Assets/Scripts/UI/MainMenuController.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("MainMenuController updated successfully.")
else:
    print("Targets not found in MainMenuController.")
