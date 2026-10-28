with open('Assets/Scripts/Core/GameManager.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target1 = """        public enum GameMode
        {
            VsCpu,
            LocalPvP
        }"""

replacement1 = """        public enum GameMode
        {
            VsCpu,
            LocalPvP,
            LanPvP
        }"""

target2 = """        [Header("CPU AI Difficulty")]
        [SerializeField] private CpuDifficulty currentCpuDifficulty = CpuDifficulty.Easy;"""

replacement2 = """        [Header("CPU AI Difficulty")]
        [SerializeField] private CpuDifficulty currentCpuDifficulty = CpuDifficulty.Easy;

        [Header("LAN Multiplayer Settings")]
        [SerializeField] private bool isLanHost = true;"""

target3 = """        public CpuDifficulty CurrentCpuDifficulty
        {
            get => currentCpuDifficulty;
            set => currentCpuDifficulty = value;
        }"""

replacement3 = """        public CpuDifficulty CurrentCpuDifficulty
        {
            get => currentCpuDifficulty;
            set => currentCpuDifficulty = value;
        }

        /// <summary>
        /// True if this machine acts as the authoritative LAN Host. False if Client.
        /// </summary>
        public bool IsLanHost
        {
            get => isLanHost;
            set => isLanHost = value;
        }"""

if target1 in text and target2 in text and target3 in text:
    text = text.replace(target1, replacement1).replace(target2, replacement2).replace(target3, replacement3)
    with open('Assets/Scripts/Core/GameManager.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("SUCCESS: GameManager.cs updated.")
else:
    print("Target not found. Checking CRLF vs LF...")
    text = text.replace('\r\n', '\n')
    target1 = target1.replace('\r\n', '\n')
    target2 = target2.replace('\r\n', '\n')
    target3 = target3.replace('\r\n', '\n')
    if target1 in text and target2 in text and target3 in text:
        text = text.replace(target1, replacement1).replace(target2, replacement2).replace(target3, replacement3)
        with open('Assets/Scripts/Core/GameManager.cs', 'w', encoding='utf-8') as f:
            f.write(text)
        print("SUCCESS: GameManager.cs updated after LF normalization.")
    else:
        print("ERROR: Target still not found.")
