with open('Assets/Scripts/Core/PrototypeBootstrap.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target1 = """using MysticCombat.AI;"""

replacement1 = """using MysticCombat.AI;
using MysticCombat.Network;"""

target2 = """            // Input Provider Assignment (Decoupled: Human Keyboard vs Tactical CPU HFSM)
            bool isCpu = (playerIndex == PlayerInputHandler.PlayerIndex.P2) &&
                         (GameManager.Instance == null || GameManager.Instance.CurrentGameMode == GameManager.GameMode.VsCpu);

            if (isCpu)
            {
                PlayerInputHandler existingInput = fighter.GetComponent<PlayerInputHandler>();
                if (existingInput != null) Destroy(existingInput);

                CpuDifficulty diff = (GameManager.Instance != null) ? GameManager.Instance.CurrentCpuDifficulty : CpuDifficulty.Easy;
                CpuFighterBrain brain = characterId == "goku"
                    ? (CpuFighterBrain)fighter.AddComponent<GokuBrain>()
                    : (CpuFighterBrain)fighter.AddComponent<SaitamaBrain>();

                brain.Configure(diff);
                fc.SetInputHandler(brain);
                Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} from {prefabName}.prefab as CPU ({diff})!");
            }
            else
            {
                PlayerInputHandler inputHandler = fighter.GetComponent<PlayerInputHandler>();
                if (inputHandler == null) inputHandler = fighter.AddComponent<PlayerInputHandler>();
                inputHandler.playerIndex = playerIndex;
                fc.SetInputHandler(inputHandler);
                Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} from {prefabName}.prefab for Player {playerIndex}!");
            }"""

replacement2 = """            // Input Provider Assignment (Decoupled: Human Keyboard vs Tactical CPU HFSM vs LAN Network)
            bool isLan = (GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameManager.GameMode.LanPvP);
            bool isHost = (GameManager.Instance == null || GameManager.Instance.IsLanHost);

            if (isLan)
            {
                if (playerIndex == PlayerInputHandler.PlayerIndex.P1)
                {
                    if (isHost)
                    {
                        PlayerInputHandler inputHandler = fighter.GetComponent<PlayerInputHandler>();
                        if (inputHandler == null) inputHandler = fighter.AddComponent<PlayerInputHandler>();
                        inputHandler.playerIndex = PlayerInputHandler.PlayerIndex.P1;
                        fc.SetInputHandler(inputHandler);
                        Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} as Host Local Player 1!");
                    }
                    else
                    {
                        PlayerInputHandler existing = fighter.GetComponent<PlayerInputHandler>();
                        if (existing != null) Destroy(existing);
                        Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} as Client Remote Mirror (Host P1)!");
                    }
                }
                else // P2
                {
                    if (isHost)
                    {
                        PlayerInputHandler existing = fighter.GetComponent<PlayerInputHandler>();
                        if (existing != null) Destroy(existing);

                        NetworkFighterInput netInput = fighter.GetComponent<NetworkFighterInput>();
                        if (netInput == null) netInput = fighter.AddComponent<NetworkFighterInput>();
                        fc.SetInputHandler(netInput);
                        Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} as Host Remote Player 2 (NetworkFighterInput)!");
                    }
                    else
                    {
                        PlayerInputHandler inputHandler = fighter.GetComponent<PlayerInputHandler>();
                        if (inputHandler == null) inputHandler = fighter.AddComponent<PlayerInputHandler>();
                        inputHandler.playerIndex = PlayerInputHandler.PlayerIndex.P1; // Solo ergonomic keyboard controls
                        fc.SetInputHandler(inputHandler);
                        Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} as Client Local Player 2!");
                    }
                }
            }
            else
            {
                bool isCpu = (playerIndex == PlayerInputHandler.PlayerIndex.P2) &&
                             (GameManager.Instance == null || GameManager.Instance.CurrentGameMode == GameManager.GameMode.VsCpu);

                if (isCpu)
                {
                    PlayerInputHandler existingInput = fighter.GetComponent<PlayerInputHandler>();
                    if (existingInput != null) Destroy(existingInput);

                    CpuDifficulty diff = (GameManager.Instance != null) ? GameManager.Instance.CurrentCpuDifficulty : CpuDifficulty.Easy;
                    CpuFighterBrain brain = characterId == "goku"
                        ? (CpuFighterBrain)fighter.AddComponent<GokuBrain>()
                        : (CpuFighterBrain)fighter.AddComponent<SaitamaBrain>();

                    brain.Configure(diff);
                    fc.SetInputHandler(brain);
                    Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} from {prefabName}.prefab as CPU ({diff})!");
                }
                else
                {
                    PlayerInputHandler inputHandler = fighter.GetComponent<PlayerInputHandler>();
                    if (inputHandler == null) inputHandler = fighter.AddComponent<PlayerInputHandler>();
                    inputHandler.playerIndex = playerIndex;
                    fc.SetInputHandler(inputHandler);
                    Debug.Log($"<color=#00e676>[PrototypeBootstrap]</color> Deployed {fighterName} from {prefabName}.prefab for Player {playerIndex}!");
                }
            }"""

target3 = """            // Wire HUDController if present
            HUDController hud = FindAnyObjectByType<HUDController>();
            if (hud != null)
            {
                hud.SetFighterReferences(
                    fighter1.GetComponent<HealthSystem>(),
                    fighter2.GetComponent<HealthSystem>(),
                    fighter1.GetComponent<EnergySystem>(),
                    fighter2.GetComponent<EnergySystem>()
                );
            }"""

replacement3 = """            // Wire HUDController if present
            HUDController hud = FindAnyObjectByType<HUDController>();
            if (hud != null)
            {
                hud.SetFighterReferences(
                    fighter1.GetComponent<HealthSystem>(),
                    fighter2.GetComponent<HealthSystem>(),
                    fighter1.GetComponent<EnergySystem>(),
                    fighter2.GetComponent<EnergySystem>()
                );
            }

            // Wire LAN NetworkMatchSynchronizer if in LAN mode
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameManager.GameMode.LanPvP)
            {
                GameObject syncObj = new GameObject("NetworkMatchSynchronizer");
                NetworkMatchSynchronizer synchronizer = syncObj.AddComponent<NetworkMatchSynchronizer>();
                synchronizer.Initialize(fc1, fc2, matchManager, GameManager.Instance.IsLanHost);
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
    with open('Assets/Scripts/Core/PrototypeBootstrap.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("PrototypeBootstrap updated successfully.")
else:
    print("Targets not found in PrototypeBootstrap.")
