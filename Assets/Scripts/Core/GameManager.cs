using UnityEngine;
using UnityEngine.SceneManagement;

namespace MysticCombat.Core
{
    /// <summary>
    /// Singleton game manager that persists across scenes.
    /// Manages high-level game state and selected characters.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        private static GameManager instance;

        /// <summary>
        /// Global access to the GameManager singleton.
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find an existing one in the scene
                    instance = FindAnyObjectByType<GameManager>();

                    if (instance == null)
                    {
                        // Create one if none exists
                        GameObject go = new GameObject("GameManager");
                        instance = go.AddComponent<GameManager>();
                    }
                }
                return instance;
            }
        }

        #endregion

        #region Game State Enum

        /// <summary>
        /// High-level game states.
        /// </summary>
        public enum GameState
        {
            MainMenu,
            CharacterSelect,
            InMatch,
            Results
        }

        #endregion

        #region Fields

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        [Header("Character Selections")]
        [SerializeField] private string p1SelectedCharacterId;
        [SerializeField] private string p2SelectedCharacterId;

        #endregion

        #region Properties

        /// <summary>
        /// The current high-level game state.
        /// </summary>
        public GameState CurrentState => currentState;

        /// <summary>
        /// The character ID selected by Player 1.
        /// </summary>
        public string P1SelectedCharacterId
        {
            get => p1SelectedCharacterId;
            set => p1SelectedCharacterId = value;
        }

        /// <summary>
        /// The character ID selected by Player 2.
        /// </summary>
        public string P2SelectedCharacterId
        {
            get => p2SelectedCharacterId;
            set => p2SelectedCharacterId = value;
        }

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            // Enforce singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[GameManager] Initialized.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Transitions to a new game state.
        /// </summary>
        /// <param name="newState">The target game state.</param>
        public void SetState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"[GameManager] State changed to: {newState}");
        }

        /// <summary>
        /// Loads a scene by name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        public void LoadScene(string sceneName)
        {
            Debug.Log($"[GameManager] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Loads a scene asynchronously by name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <returns>The AsyncOperation for tracking progress.</returns>
        public AsyncOperation LoadSceneAsync(string sceneName)
        {
            Debug.Log($"[GameManager] Loading scene async: {sceneName}");
            return SceneManager.LoadSceneAsync(sceneName);
        }

        #endregion
    }
}
