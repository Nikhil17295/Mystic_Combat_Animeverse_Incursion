with open('Assets/Scripts/Editor/MainMenuSetupTool.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target1 = """            string[] optionTitles = new string[]
            {
                "1 PLAYER  VS  CPU",
                "1 VS 1  LOCAL DUEL",
                "OPTIONS & MOVESET",
                "QUIT GAME"
            };

            List<MainMenuController.MenuItem> items = new List<MainMenuController.MenuItem>();
            float lineSpacing = 46f;
            float startY = 45f;"""

replacement1 = """            string[] optionTitles = new string[]
            {
                "1 PLAYER  VS  CPU",
                "1 VS 1  LOCAL DUEL",
                "1 VS 1  LAN MULTIPLAYER",
                "OPTIONS & MOVESET",
                "QUIT GAME"
            };

            List<MainMenuController.MenuItem> items = new List<MainMenuController.MenuItem>();
            float lineSpacing = 42f;
            float startY = 60f;"""

target2 = """            so.FindProperty("optionsPanel").objectReferenceValue = optionsModal;"""

replacement2 = """            so.FindProperty("optionsPanel").objectReferenceValue = optionsModal;

            // LAN Lobby Modal Creation
            GameObject lanModalObj = CreateUIObject("Modal_LanLobby", canvasObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            LanLobbyModal lanModal = lanModalObj.AddComponent<LanLobbyModal>();
            lanModalObj.SetActive(false);
            so.FindProperty("lanLobbyModal").objectReferenceValue = lanModal;"""

text = text.replace('\r\n', '\n')
target1 = target1.replace('\r\n', '\n')
target2 = target2.replace('\r\n', '\n')
replacement1 = replacement1.replace('\r\n', '\n')
replacement2 = replacement2.replace('\r\n', '\n')

if target1 in text and target2 in text:
    text = text.replace(target1, replacement1).replace(target2, replacement2)
    with open('Assets/Scripts/Editor/MainMenuSetupTool.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("MainMenuSetupTool updated successfully.")
else:
    print("Targets not found in MainMenuSetupTool.")
