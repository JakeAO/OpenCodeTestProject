using UnityEngine;

namespace OCTP.Core
{
    /// <summary>
    /// ScriptableObject containing network configuration for all environments.
    /// Provides environment-specific connection settings for the Nakama backend server.
    /// Asset location: Assets/_Project/Core/Resources/NetworkConfig.asset
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "OCTP/Network/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("Environment Configurations")]
        [Tooltip("Configuration for local development server (localhost:7350)")]
        [SerializeField] private NetworkEndpointConfig _localConfig;
        
        [Tooltip("Configuration for shared development server")]
        [SerializeField] private NetworkEndpointConfig _developmentConfig;
        
        [Tooltip("Configuration for staging/pre-production server")]
        [SerializeField] private NetworkEndpointConfig _stagingConfig;
        
        [Tooltip("Configuration for production server")]
        [SerializeField] private NetworkEndpointConfig _productionConfig;
        
        /// <summary>
        /// Gets the configuration for the local development environment.
        /// </summary>
        public NetworkEndpointConfig LocalConfig => _localConfig;
        
        /// <summary>
        /// Gets the configuration for the development environment.
        /// </summary>
        public NetworkEndpointConfig DevelopmentConfig => _developmentConfig;
        
        /// <summary>
        /// Gets the configuration for the staging environment.
        /// </summary>
        public NetworkEndpointConfig StagingConfig => _stagingConfig;
        
        /// <summary>
        /// Gets the configuration for the production environment.
        /// </summary>
        public NetworkEndpointConfig ProductionConfig => _productionConfig;
        
        /// <summary>
        /// Gets the appropriate configuration based on the specified environment.
        /// </summary>
        /// <param name="environment">The target network environment.</param>
        /// <returns>The configuration for the specified environment, or local config as fallback.</returns>
        public NetworkEndpointConfig GetConfig(NetworkEnvironment environment)
        {
            switch (environment)
            {
                case NetworkEnvironment.Local:
                    return _localConfig;
                    
                case NetworkEnvironment.Development:
                    return _developmentConfig;
                    
                case NetworkEnvironment.Staging:
                    return _stagingConfig;
                    
                case NetworkEnvironment.Production:
                    return _productionConfig;
                    
                default:
                    Debug.LogError($"[NetworkConfig] Unknown environment: {environment}. Using Local as fallback.");
                    return _localConfig;
            }
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Validates all configurations when values change in the Unity Editor.
        /// Called automatically by Unity when Inspector values are modified.
        /// </summary>
        private void OnValidate()
        {
            ValidateConfig(_localConfig, "Local");
            ValidateConfig(_developmentConfig, "Development");
            ValidateConfig(_stagingConfig, "Staging");
            ValidateConfig(_productionConfig, "Production");
        }
        
        /// <summary>
        /// Validates a single configuration and logs warnings for issues.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="envName">The environment name for logging purposes.</param>
        private void ValidateConfig(NetworkEndpointConfig config, string envName)
        {
            if (config == null)
            {
                Debug.LogWarning($"[NetworkConfig] {envName} configuration is null!");
                return;
            }
            
            config.Validate();
        }
#endif
    }
}
