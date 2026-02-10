using System;
using UnityEngine;

namespace OCTP.Core
{
    /// <summary>
    /// Central orchestrator for core game systems.
    /// Handles initialization, lifecycle management (Play/Pause/Resume/Quit),
    /// and service locator registration for all core managers.
    /// </summary>
    public class GameManager : MonoBehaviour, IGameService
    {
        private static GameManager _instance;

        /// <summary>
        /// Singleton instance of the GameManager.
        /// </summary>
        public static GameManager Instance => _instance;

        /// <summary>
        /// Event fired when the game starts.
        /// </summary>
        public event Action OnGameStarted;

        /// <summary>
        /// Event fired when the game is paused.
        /// </summary>
        public event Action OnGamePaused;

        /// <summary>
        /// Event fired when the game is resumed.
        /// </summary>
        public event Action OnGameResumed;

        /// <summary>
        /// Event fired when the game is quitting.
        /// </summary>
        public event Action OnGameQuitting;

        private bool _isInitialized = false;

        private void Awake()
        {
            // Singleton pattern implementation
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAllSystems();
        }

        private void InitializeAllSystems()
        {
            try
            {
                Debug.Log("=== OCTP GameManager Initialization ===");

                // 1. Register GameManager itself with ServiceLocator
                ServiceLocator.Register<IGameService>(this);

                // 2. Initialize NetworkManager FIRST (needed by Analytics/RemoteConfig)
                Debug.Log("[GameManager] Initializing NetworkManager...");
                var networkManager = gameObject.AddComponent<NetworkManager>();
                networkManager.Initialize();
                ServiceLocator.Register<INetworkManager>(networkManager);
                Debug.Log("[GameManager] NetworkManager initialized");

                // 3. Create and register other managers (order matters: some depend on NetworkManager)
                ServiceLocator.Register<IGameStateManager>(gameObject.AddComponent<GameStateManager>());
                ServiceLocator.Register<IAnalyticsManager>(gameObject.AddComponent<AnalyticsManager>());
                ServiceLocator.Register<IRemoteConfigManager>(gameObject.AddComponent<RemoteConfigManager>());
                ServiceLocator.Register<IInputManager>(gameObject.AddComponent<InputManager>());
                ServiceLocator.Register<ISceneLoader>(gameObject.AddComponent<SceneLoader>());
                ServiceLocator.Register<ISaveManager>(gameObject.AddComponent<SaveManager>());
                ServiceLocator.Register<IPartyManager>(gameObject.AddComponent<PartyManager>());

                _isInitialized = true;

                Debug.Log("GameManager initialization complete");

                // 4. Kick off remote config fetch (non-blocking)
                ServiceLocator.Get<IRemoteConfigManager>().LoadFromNakamaAsync();

                // 5. Fire OnGameStarted event
                OnGameStarted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameManager initialization failed: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Starts or resumes the game.
        /// </summary>
        public void Play()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("GameManager not initialized, cannot play");
                return;
            }

            Resume();
        }

        /// <summary>
        /// Pauses the game.
        /// </summary>
        public void Pause()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("GameManager not initialized, cannot pause");
                return;
            }

            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
            Debug.Log("Game paused");
        }

        /// <summary>
        /// Resumes the game from paused state.
        /// </summary>
        public void Resume()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("GameManager not initialized, cannot resume");
                return;
            }

            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
            Debug.Log("Game resumed");
        }

        /// <summary>
        /// Quits the game and performs cleanup.
        /// </summary>
        public void Quit()
        {
            Debug.Log("Quitting game");
            OnGameQuitting?.Invoke();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }

        private void OnApplicationQuit()
        {
            OnGameQuitting?.Invoke();
        }
    }
}
