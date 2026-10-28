using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysticCombat.Network;
using MysticCombat.Core;

namespace MysticCombat.UI
{
    /// <summary>
    /// Arcade LAN Duel Lobby Modal.
    /// Handles Host creation, LAN IP discovery, Client connection, and auto-transition to Character Select.
    /// Supports both Inspector-wired UI and full procedural generation fallback.
    /// </summary>
    public class LanLobbyModal : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI Root")]
        [SerializeField] private GameObject modalRoot;

        [Header("Status & Info")]
        [SerializeField] private TextMeshProUGUI headerTitleText;
        [SerializeField] private TextMeshProUGUI localIpText;
        [SerializeField] private TextMeshProUGUI statusPromptText;

        [Header("Host Section")]
        [SerializeField] private Button hostButton;
        [SerializeField] private TextMeshProUGUI hostButtonText;

        [Header("Client Section")]
        [SerializeField] private TMP_InputField ipInputField;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI joinButtonText;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        [Header("References")]
        [SerializeField] private CharacterSelectController characterSelectController;
        [SerializeField] private GameObject characterSelectRoot;
        [SerializeField] private MainMenuController mainMenuController;

        #endregion

        #region State

        private bool isWaitingForOpponent = false;
        private string targetIp = "127.0.0.1";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (modalRoot == null)
            {
                modalRoot = gameObject;
            }

            EnsureUIBuilt();
            WireListeners();
        }

        private void OnEnable()
        {
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnConnectionStateChanged += HandleConnectionStateChanged;
            }
            RefreshLocalIP();
            SetStatus("CHOOSE HOST OR JOIN TO COMMENCE LAN COMBAT");
        }

        private void OnDisable()
        {
            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.OnConnectionStateChanged -= HandleConnectionStateChanged;
            }
        }

        #endregion

        #region Public Methods

        public void Open()
        {
            gameObject.SetActive(true);
            if (modalRoot != null) modalRoot.SetActive(true);
            RefreshLocalIP();
            SetStatus("SELECT HOST OR JOIN TO COMMENCE LAN COMBAT");
        }

        public void Close()
        {
            if (isWaitingForOpponent && LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.Disconnect();
                isWaitingForOpponent = false;
            }

            if (modalRoot != null) modalRoot.SetActive(false);
            gameObject.SetActive(false);

            if (mainMenuController != null)
            {
                mainMenuController.ReturnToMainMenuFromCharSelect();
            }
        }

        #endregion

        #region Host & Join Handlers

        public void OnHostClicked()
        {
            SetStatus("<color=#00e676>STARTING HOST SERVER ON PORT 7777...</color>");

            if (LanNetworkManager.Instance == null)
            {
                SetStatus("<color=#ff4444>ERROR: LanNetworkManager NOT FOUND!</color>");
                return;
            }

            bool success = LanNetworkManager.Instance.StartHost(7777);
            if (success)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetGameMode(GameManager.GameMode.LanPvP);
                    GameManager.Instance.IsLanHost = true;
                }

                isWaitingForOpponent = true;
                string ip = LanNetworkManager.Instance.GetLocalIPv4Address();
                SetStatus($"<color=#ffd700>WAITING FOR CHALLENGER TO JOIN...</color>\n<color=#00f5ff>TELL OPPONENT TO ENTER IP: {ip}</color>");
            }
            else
            {
                SetStatus("<color=#ff4444>FAILED TO BIND PORT 7777. CHECK FIREWALL.</color>");
            }
        }

        public void OnJoinClicked()
        {
            string ip = ipInputField != null ? ipInputField.text.Trim() : targetIp;
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

            SetStatus($"<color=#00f5ff>CONNECTING TO HOST AT {ip}:7777...</color>");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameMode(GameManager.GameMode.LanPvP);
                GameManager.Instance.IsLanHost = false;
            }

            if (LanNetworkManager.Instance != null)
            {
                LanNetworkManager.Instance.ConnectToHost(ip, 7777);
            }
        }

        private void HandleConnectionStateChanged(bool connected)
        {
            if (connected)
            {
                SetStatus("<color=#00e676>⚔ CHALLENGER CONNECTED! LAUNCHING CHARACTER SELECT... ⚔</color>");
                isWaitingForOpponent = false;
                Invoke(nameof(TransitionToCharacterSelect), 1.0f);
            }
            else
            {
                SetStatus("<color=#ff4444>DISCONNECTED OR CONNECTION ATTEMPT FAILED.</color>");
                isWaitingForOpponent = false;
            }
        }

        private void TransitionToCharacterSelect()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            gameObject.SetActive(false);

            if (characterSelectController != null && characterSelectRoot != null)
            {
                characterSelectRoot.SetActive(true);
                characterSelectController.Open(GameManager.GameMode.LanPvP);
            }
        }

        #endregion

        #region Helpers & UI Construction

        private void RefreshLocalIP()
        {
            string ip = LanNetworkManager.Instance != null ? LanNetworkManager.Instance.GetLocalIPv4Address() : "127.0.0.1";
            if (localIpText != null)
            {
                localIpText.text = $"YOUR LOCAL LAN IP: <color=#00f5ff>{ip}</color>  |  PORT: <color=#ffd700>7777</color>";
            }
        }

        private void SetStatus(string message)
        {
            if (statusPromptText != null)
            {
                statusPromptText.text = message;
            }
        }

        private void WireListeners()
        {
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
            if (backButton != null) backButton.onClick.AddListener(Close);
        }

        private void EnsureUIBuilt()
        {
            // If already wired via inspector, do nothing
            if (hostButton != null && joinButton != null) return;

            // Otherwise, construct procedural fallback UI within this GameObject
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.94f); // Rich dark arcade navy

            // Find font
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans");
            if (font == null) font = TMP_Settings.defaultFontAsset;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(transform, false);
            headerTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitleText.text = "⚡ LAN MULTIPLAYER COMBAT ⚡";
            headerTitleText.fontSize = 38;
            headerTitleText.alignment = TextAlignmentOptions.Center;
            headerTitleText.font = font;
            headerTitleText.color = new Color(1f, 0.85f, 0.1f, 1f);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 240f);
            titleRect.sizeDelta = new Vector2(800f, 60f);

            // Local IP Display
            GameObject ipObj = new GameObject("LocalIp");
            ipObj.transform.SetParent(transform, false);
            localIpText = ipObj.AddComponent<TextMeshProUGUI>();
            localIpText.fontSize = 20;
            localIpText.alignment = TextAlignmentOptions.Center;
            localIpText.font = font;
            localIpText.color = Color.white;
            RectTransform ipRect = ipObj.GetComponent<RectTransform>();
            ipRect.anchoredPosition = new Vector2(0f, 180f);
            ipRect.sizeDelta = new Vector2(800f, 40f);

            // Status Text
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(transform, false);
            statusPromptText = statusObj.AddComponent<TextMeshProUGUI>();
            statusPromptText.fontSize = 22;
            statusPromptText.alignment = TextAlignmentOptions.Center;
            statusPromptText.font = font;
            statusPromptText.color = new Color(0.6f, 0.85f, 1f, 1f);
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(0f, 120f);
            statusRect.sizeDelta = new Vector2(900f, 70f);

            // Host Button
            hostButton = CreateButton("Btn_Host", new Vector2(0f, 30f), new Vector2(480f, 60f), "⚔  HOST MATCH (WAIT FOR OPPONENT)", font);

            // Join IP Field & Button
            GameObject inputObj = new GameObject("Input_IP");
            inputObj.transform.SetParent(transform, false);
            Image inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.12f, 0.18f, 0.28f, 1f);
            RectTransform inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.anchoredPosition = new Vector2(-110f, -50f);
            inputRect.sizeDelta = new Vector2(240f, 55f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            TextMeshProUGUI inputTmp = textObj.AddComponent<TextMeshProUGUI>();
            inputTmp.fontSize = 22;
            inputTmp.alignment = TextAlignmentOptions.Center;
            inputTmp.font = font;
            inputTmp.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            ipInputField = inputObj.AddComponent<TMP_InputField>();
            ipInputField.textComponent = inputTmp;
            ipInputField.text = "127.0.0.1";

            joinButton = CreateButton("Btn_Join", new Vector2(150f, -50f), new Vector2(220f, 55f), "CONNECT & JOIN", font);

            // Back Button
            backButton = CreateButton("Btn_Back", new Vector2(0f, -140f), new Vector2(300f, 50f), "◄ BACK TO MAIN MENU", font);

            // Auto-link references if available in scene
            if (characterSelectController == null) characterSelectController = FindAnyObjectByType<CharacterSelectController>(FindObjectsInactive.Include);
            if (characterSelectRoot == null && characterSelectController != null) characterSelectRoot = characterSelectController.gameObject;
            if (mainMenuController == null) mainMenuController = FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
        }

        private Button CreateButton(string name, Vector2 pos, Vector2 size, string text, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(transform, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.22f, 0.35f, 1f);

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.95f, 1f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            Button btn = btnObj.AddComponent<Button>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = lblObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform lblRt = lblObj.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.sizeDelta = Vector2.zero;

            return btn;
        }

        #endregion
    }
}
