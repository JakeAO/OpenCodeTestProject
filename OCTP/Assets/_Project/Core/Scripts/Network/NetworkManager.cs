using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OCTP.Core
{
    /// <summary>
    /// Manages network environment configuration and provides centralized access to server settings.
    /// Handles environment switching (Local, Development, Staging, Production) with compiler flag detection,
    /// runtime overrides, and persistent settings via PlayerPrefs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The NetworkManager detects the active environment using a priority system:
    /// 1. PlayerPrefs override (set via SetEnvironment)
    /// 2. Compiler flags (NETWORK_LOCAL, NETWORK_DEV, NETWORK_STAGING, NETWORK_PRODUCTION)
    /// 3. Platform default (Local in Editor, Production in builds)
    /// </para>
    /// <para>
    /// Configuration is loaded from a NetworkConfig ScriptableObject in Resources.
    /// Environment changes require application restart to take effect.
    /// </para>
    /// </remarks>
    public class NetworkManager : MonoBehaviour, INetworkManager
    {
        private const string PREF_KEY_OVERRIDE = "OCTP_NetworkEnvironmentOverride";
        private const string PREF_KEY_OVERRIDE_ACTIVE = "OCTP_NetworkEnvironmentOverrideActive";
        private const string CONFIG_RESOURCE_PATH = "NetworkConfig";
        private const string LOG_PREFIX = "[NetworkManager]";
        
        private NetworkConfig _config;
        private NetworkEnvironment _currentEnvironment;
        private NetworkEndpointConfig _currentEndpointConfig;
        
        /// <inheritdoc/>
        public event Action<NetworkEnvironment> OnEnvironmentChanged;
        
        /// <inheritdoc/>
        public NetworkEnvironment CurrentEnvironment => _currentEnvironment;
        
        /// <inheritdoc/>
        public string ServerUrl => _currentEndpointConfig?.serverUrl ?? "localhost";
        
        /// <inheritdoc/>
        public int ServerPort => _currentEndpointConfig?.serverPort ?? 7350;
        
        /// <inheritdoc/>
        public string HttpKey => _currentEndpointConfig?.httpKey ?? "defaultkey";
        
        /// <inheritdoc/>
        public bool UseSSL => _currentEndpointConfig?.useSSL ?? false;
        
        /// <inheritdoc/>
        public string FullServerUrl
        {
            get
            {
                string protocol = UseSSL ? "https" : "http";
                return $"{protocol}://{ServerUrl}:{ServerPort}";
            }
        }
        
        /// <inheritdoc/>
        public bool HasEnvironmentOverride => PlayerPrefs.GetInt(PREF_KEY_OVERRIDE_ACTIVE, 0) == 1;
        
        private void Awake()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initializes the NetworkManager by loading configuration and determining the active environment.
        /// </summary>
        public void Initialize()
        {
            LoadConfiguration();
            DetermineEnvironment();
            LogEnvironmentSelection();
        }
        
        /// <summary>
        /// Loads the NetworkConfig from Resources.
        /// </summary>
        private void LoadConfiguration()
        {
            _config = Resources.Load<NetworkConfig>(CONFIG_RESOURCE_PATH);
            
            if (_config == null)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to load NetworkConfig from Resources/{CONFIG_RESOURCE_PATH}. Using default Local environment.");
                _currentEnvironment = NetworkEnvironment.Local;
                _currentEndpointConfig = new NetworkEndpointConfig();
            }
        }
        
        /// <summary>
        /// Determines the active environment based on priority: PlayerPrefs override > Compiler flag > Platform default.
        /// </summary>
        private void DetermineEnvironment()
        {
            if (_config == null)
            {
                _currentEnvironment = NetworkEnvironment.Local;
                _currentEndpointConfig = new NetworkEndpointConfig();
                return;
            }
            
            // Check for PlayerPrefs override first
            if (HasEnvironmentOverride)
            {
                int overrideValue = PlayerPrefs.GetInt(PREF_KEY_OVERRIDE, (int)NetworkEnvironment.Local);
                _currentEnvironment = (NetworkEnvironment)overrideValue;
            }
            else
            {
                // Use compiler flag or platform default
                _currentEnvironment = GetCompilerFlagEnvironment();
            }
            
            // Load the endpoint configuration for the determined environment
            _currentEndpointConfig = _config.GetConfig(_currentEnvironment);
        }
        
        /// <summary>
        /// Detects the environment based on compiler flags and platform defaults.
        /// </summary>
        /// <returns>The environment determined by compiler flags or platform default.</returns>
        private NetworkEnvironment GetCompilerFlagEnvironment()
        {
            #if NETWORK_PRODUCTION
                return NetworkEnvironment.Production;
            #elif NETWORK_STAGING
                return NetworkEnvironment.Staging;
            #elif NETWORK_DEV
                return NetworkEnvironment.Development;
            #elif NETWORK_LOCAL
                return NetworkEnvironment.Local;
            #else
                #if UNITY_EDITOR
                    return NetworkEnvironment.Local;
                #else
                    return NetworkEnvironment.Production;
                #endif
            #endif
        }
        
        /// <summary>
        /// Logs the selected environment and configuration details.
        /// </summary>
        private void LogEnvironmentSelection()
        {
            string overrideStatus = HasEnvironmentOverride ? " (Override Active)" : " (Compiler Flag/Default)";
            Debug.Log($"{LOG_PREFIX} Environment: {_currentEnvironment}{overrideStatus}");
            Debug.Log($"{LOG_PREFIX} Server: {FullServerUrl}");
            Debug.Log($"{LOG_PREFIX} SSL: {(UseSSL ? "Enabled" : "Disabled")}");
        }
        
        /// <inheritdoc/>
        public void SetEnvironment(NetworkEnvironment environment)
        {
            // Validate environment change
            if (environment == _currentEnvironment && HasEnvironmentOverride)
            {
                Debug.LogWarning($"{LOG_PREFIX} Already using environment: {environment}");
                return;
            }
            
            // Save override to PlayerPrefs
            PlayerPrefs.SetInt(PREF_KEY_OVERRIDE, (int)environment);
            PlayerPrefs.SetInt(PREF_KEY_OVERRIDE_ACTIVE, 1);
            PlayerPrefs.Save();
            
            Debug.Log($"{LOG_PREFIX} Environment set to {environment}. Restart required to apply changes.");
            
            // Fire event
            OnEnvironmentChanged?.Invoke(environment);
        }
        
        /// <inheritdoc/>
        public void ClearEnvironmentOverride()
        {
            if (!HasEnvironmentOverride)
            {
                Debug.LogWarning($"{LOG_PREFIX} No environment override to clear.");
                return;
            }
            
            PlayerPrefs.DeleteKey(PREF_KEY_OVERRIDE);
            PlayerPrefs.DeleteKey(PREF_KEY_OVERRIDE_ACTIVE);
            PlayerPrefs.Save();
            
            NetworkEnvironment defaultEnv = GetCompilerFlagEnvironment();
            Debug.Log($"{LOG_PREFIX} Environment override cleared. Will revert to {defaultEnv} on next restart.");
        }
        
        /// <inheritdoc/>
        public void RequestReload()
        {
            Debug.Log($"{LOG_PREFIX} Requesting application reload to apply environment changes...");
            
            #if UNITY_EDITOR
                // In editor, stop play mode
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                // In builds, try to reload the first scene or quit
                if (SceneManager.sceneCount > 0)
                {
                    SceneManager.LoadScene(0);
                }
                else
                {
                    Application.Quit();
                }
            #endif
        }
    }
}
