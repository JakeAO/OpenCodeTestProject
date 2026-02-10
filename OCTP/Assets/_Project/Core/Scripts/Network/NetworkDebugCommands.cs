#if DEVELOPMENT_BUILD || UNITY_EDITOR
using IngameDebugConsole;
using UnityEngine;

namespace OCTP.Core
{
    /// <summary>
    /// Provides debug console commands for managing network environment configuration.
    /// Allows runtime switching between Local, Development, Staging, and Production environments.
    /// </summary>
    /// <remarks>
    /// All commands are only available in development builds and the Unity Editor.
    /// Environment changes require application restart to take effect.
    /// </remarks>
    public static class NetworkDebugCommands
    {
        private const string COLOR_GREEN = "<color=green>";
        private const string COLOR_YELLOW = "<color=yellow>";
        private const string COLOR_RED = "<color=red>";
        private const string COLOR_CYAN = "<color=cyan>";
        private const string COLOR_END = "</color>";
        
        /// <summary>
        /// Display current network environment and configuration.
        /// Shows server URL, port, SSL status, and available environments.
        /// </summary>
        [ConsoleMethod("network_status", "Display current network environment and configuration")]
        public static void ShowNetworkStatus()
        {
            if (!TryGetNetworkManager(out INetworkManager networkManager))
            {
                return;
            }
            
            Debug.Log("=== Network Configuration Status ===");
            Debug.Log($"{COLOR_CYAN}Current Environment:{COLOR_END} {COLOR_GREEN}{networkManager.CurrentEnvironment}{COLOR_END}");
            Debug.Log($"{COLOR_CYAN}Server URL:{COLOR_END} {networkManager.FullServerUrl}");
            Debug.Log($"{COLOR_CYAN}Port:{COLOR_END} {networkManager.ServerPort}");
            Debug.Log($"{COLOR_CYAN}SSL:{COLOR_END} {(networkManager.UseSSL ? $"{COLOR_GREEN}Enabled{COLOR_END}" : $"{COLOR_YELLOW}Disabled{COLOR_END}")}");
            Debug.Log($"{COLOR_CYAN}Has Override:{COLOR_END} {(networkManager.HasEnvironmentOverride ? $"{COLOR_YELLOW}Yes{COLOR_END}" : "No")}");
            Debug.Log($"\n{COLOR_CYAN}Available Environments:{COLOR_END}");
            Debug.Log("  - Local (local development server)");
            Debug.Log("  - Development (shared dev server)");
            Debug.Log("  - Staging (pre-production testing)");
            Debug.Log("  - Production (live server)");
        }
        
        /// <summary>
        /// Switch to a different network environment (requires restart).
        /// </summary>
        /// <param name="environmentName">Environment name: local, development, staging, or production</param>
        [ConsoleMethod("network_switch", "Switch to a different network environment (requires restart)")]
        public static void SwitchEnvironment(string environmentName)
        {
            if (!TryGetNetworkManager(out INetworkManager networkManager))
            {
                return;
            }
            
            // Parse environment name (case-insensitive)
            if (!TryParseEnvironment(environmentName, out NetworkEnvironment targetEnvironment))
            {
                Debug.LogError($"{COLOR_RED}Invalid environment name:{COLOR_END} '{environmentName}'");
                Debug.LogError($"Valid options: {COLOR_YELLOW}local, development, staging, production{COLOR_END}");
                return;
            }
            
            // Check if already on target environment with override
            if (networkManager.CurrentEnvironment == targetEnvironment && networkManager.HasEnvironmentOverride)
            {
                Debug.LogWarning($"{COLOR_YELLOW}Already using environment:{COLOR_END} {targetEnvironment}");
                return;
            }
            
            // Set the environment
            networkManager.SetEnvironment(targetEnvironment);
            
            Debug.Log($"{COLOR_GREEN}Environment switched to:{COLOR_END} {targetEnvironment}");
            Debug.Log($"{COLOR_YELLOW}⚠ Restart required to apply changes!{COLOR_END}");
            Debug.Log($"Call '{COLOR_CYAN}network_reload{COLOR_END}' to restart the application now.");
        }
        
        /// <summary>
        /// Reload the application to apply network changes.
        /// In Editor: Stops play mode. In builds: Reloads the first scene or quits.
        /// </summary>
        [ConsoleMethod("network_reload", "Reload the application to apply network changes")]
        public static void ReloadApplication()
        {
            if (!TryGetNetworkManager(out INetworkManager networkManager))
            {
                return;
            }
            
            Debug.Log($"{COLOR_YELLOW}Reloading application to apply network changes...{COLOR_END}");
            networkManager.RequestReload();
        }
        
        /// <summary>
        /// Clear environment override and revert to compiler flag default.
        /// Removes the persistent PlayerPrefs override, allowing the application
        /// to use the environment defined by compiler flags or platform defaults.
        /// </summary>
        [ConsoleMethod("network_clear_override", "Clear environment override and revert to compiler flag default")]
        public static void ClearOverride()
        {
            if (!TryGetNetworkManager(out INetworkManager networkManager))
            {
                return;
            }
            
            if (!networkManager.HasEnvironmentOverride)
            {
                Debug.LogWarning($"{COLOR_YELLOW}No environment override active.{COLOR_END}");
                Debug.Log("Currently using default environment from compiler flags.");
                return;
            }
            
            networkManager.ClearEnvironmentOverride();
            
            Debug.Log($"{COLOR_GREEN}Environment override cleared.{COLOR_END}");
            Debug.Log($"{COLOR_YELLOW}⚠ Restart required to revert to default environment!{COLOR_END}");
            Debug.Log($"Call '{COLOR_CYAN}network_reload{COLOR_END}' to restart the application now.");
        }
        
        /// <summary>
        /// Attempts to retrieve the NetworkManager from the ServiceLocator.
        /// Logs an error and returns false if the service is not registered.
        /// </summary>
        /// <param name="networkManager">The retrieved NetworkManager instance, or null if not found</param>
        /// <returns>True if the NetworkManager was successfully retrieved, false otherwise</returns>
        private static bool TryGetNetworkManager(out INetworkManager networkManager)
        {
            try
            {
                networkManager = ServiceLocator.Get<INetworkManager>();
                
                if (networkManager == null)
                {
                    Debug.LogError($"{COLOR_RED}NetworkManager not found in ServiceLocator.{COLOR_END}");
                    Debug.LogError("Ensure NetworkManager is registered during initialization.");
                    return false;
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{COLOR_RED}Error retrieving NetworkManager:{COLOR_END} {ex.Message}");
                networkManager = null;
                return false;
            }
        }
        
        /// <summary>
        /// Parses an environment name string into a NetworkEnvironment enum value.
        /// Case-insensitive parsing with support for common aliases.
        /// </summary>
        /// <param name="environmentName">The environment name to parse</param>
        /// <param name="environment">The parsed NetworkEnvironment value</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        private static bool TryParseEnvironment(string environmentName, out NetworkEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                environment = NetworkEnvironment.Local;
                return false;
            }
            
            // Normalize to lowercase for comparison
            string normalized = environmentName.Trim().ToLowerInvariant();
            
            switch (normalized)
            {
                case "local":
                case "localhost":
                    environment = NetworkEnvironment.Local;
                    return true;
                    
                case "dev":
                case "development":
                    environment = NetworkEnvironment.Development;
                    return true;
                    
                case "staging":
                case "stage":
                    environment = NetworkEnvironment.Staging;
                    return true;
                    
                case "prod":
                case "production":
                    environment = NetworkEnvironment.Production;
                    return true;
                    
                default:
                    environment = NetworkEnvironment.Local;
                    return false;
            }
        }
    }
}
#endif
