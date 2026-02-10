using System;

namespace OCTP.Core
{
    /// <summary>
    /// Interface for managing network environment configuration and Nakama server connections.
    /// Handles environment switching (Local, Development, Staging, Production) with runtime
    /// overrides and persistent settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The NetworkManager provides centralized access to environment-specific server configuration,
    /// including URLs, ports, authentication keys, and SSL settings. It supports runtime environment
    /// switching with persistence via PlayerPrefs, requiring application restart to apply changes.
    /// </para>
    /// 
    /// <para><strong>Usage Example:</strong></para>
    /// <code>
    /// // Get the service from ServiceLocator
    /// var networkManager = ServiceLocator.Get&lt;INetworkManager&gt;();
    /// 
    /// // Access current environment configuration
    /// string serverUrl = networkManager.FullServerUrl;
    /// string httpKey = networkManager.HttpKey;
    /// NetworkEnvironment env = networkManager.CurrentEnvironment;
    /// 
    /// // Listen for environment changes
    /// networkManager.OnEnvironmentChanged += (newEnv) => {
    ///     Debug.Log($"Environment changed to {newEnv}. Restart required.");
    /// };
    /// 
    /// // Switch environment (requires restart)
    /// networkManager.SetEnvironment(NetworkEnvironment.Staging);
    /// networkManager.RequestReload(); // Prompts user or restarts play mode
    /// 
    /// // Clear override to revert to compiler flag default
    /// if (networkManager.HasEnvironmentOverride) {
    ///     networkManager.ClearEnvironmentOverride();
    ///     networkManager.RequestReload();
    /// }
    /// </code>
    /// 
    /// <para><strong>Environment Selection Priority:</strong></para>
    /// <list type="number">
    ///   <item>Runtime override (if set via SetEnvironment)</item>
    ///   <item>Compiler flag (DEVELOPMENT, STAGING, PRODUCTION)</item>
    ///   <item>Platform default (Local for Editor, Production for builds)</item>
    /// </list>
    /// 
    /// <para><strong>Important Notes:</strong></para>
    /// <list type="bullet">
    ///   <item>Environment changes require application restart to take effect</item>
    ///   <item>Override settings persist across sessions via PlayerPrefs</item>
    ///   <item>Use RequestReload() to apply changes (stops/starts Editor, prompts in builds)</item>
    ///   <item>Configuration loaded from NetworkConfig ScriptableObject in Resources</item>
    /// </list>
    /// </remarks>
    public interface INetworkManager : IGameService
    {
        /// <summary>
        /// Gets the currently active network environment.
        /// </summary>
        /// <remarks>
        /// Returns the environment currently in use, which may differ from the default
        /// if a runtime override has been applied via SetEnvironment().
        /// </remarks>
        NetworkEnvironment CurrentEnvironment { get; }
        
        /// <summary>
        /// Gets the server URL for the current environment.
        /// </summary>
        /// <remarks>
        /// Returns the Nakama server URL without protocol (e.g., "localhost", "dev.example.com").
        /// Use FullServerUrl for the complete URL with protocol and port.
        /// </remarks>
        string ServerUrl { get; }
        
        /// <summary>
        /// Gets the server port for the current environment.
        /// </summary>
        /// <remarks>
        /// Typically 7350 for HTTP or 443 for HTTPS in production environments.
        /// </remarks>
        int ServerPort { get; }
        
        /// <summary>
        /// Gets the HTTP key for the current environment.
        /// </summary>
        /// <remarks>
        /// Used for Nakama HTTP authentication. This is the primary authentication
        /// key required for all API requests.
        /// </remarks>
        string HttpKey { get; }
        
        /// <summary>
        /// Gets whether SSL should be used for the current environment.
        /// </summary>
        /// <remarks>
        /// When true, connections use HTTPS/WSS. When false, uses HTTP/WS.
        /// Production environments should always use SSL.
        /// </remarks>
        bool UseSSL { get; }
        
        /// <summary>
        /// Gets the full server URL with protocol (http/https) and port.
        /// </summary>
        /// <remarks>
        /// Returns a complete URL ready for use in network requests, formatted as:
        /// "http://localhost:7350" or "https://api.example.com:443"
        /// </remarks>
        string FullServerUrl { get; }
        
        /// <summary>
        /// Gets whether an environment override is currently active.
        /// </summary>
        /// <remarks>
        /// Returns true if SetEnvironment() has been called and the override persists
        /// in PlayerPrefs. Returns false if using the default environment from compiler flags.
        /// </remarks>
        bool HasEnvironmentOverride { get; }
        
        /// <summary>
        /// Fired when the environment is changed (before restart).
        /// </summary>
        /// <remarks>
        /// Triggered immediately when SetEnvironment() is called, before the application
        /// restarts. Use this to save state or notify the user that a restart is required.
        /// The parameter provides the new environment that will be active after restart.
        /// </remarks>
        event Action<NetworkEnvironment> OnEnvironmentChanged;
        
        /// <summary>
        /// Sets the network environment. Requires app restart to take effect.
        /// </summary>
        /// <param name="environment">Target environment to switch to</param>
        /// <remarks>
        /// <para>
        /// Saves the environment selection to PlayerPrefs as a persistent override.
        /// The change will not take effect until the application is restarted.
        /// Call RequestReload() after this method to apply the changes immediately.
        /// </para>
        /// <para>
        /// Fires the OnEnvironmentChanged event with the new environment value.
        /// </para>
        /// </remarks>
        void SetEnvironment(NetworkEnvironment environment);
        
        /// <summary>
        /// Requests an application reload to apply environment changes.
        /// </summary>
        /// <remarks>
        /// <para><strong>Editor behavior:</strong> Stops and restarts play mode automatically.</para>
        /// <para><strong>Build behavior:</strong> Displays a dialog prompting the user to restart the application.</para>
        /// <para>
        /// Call this method after SetEnvironment() or ClearEnvironmentOverride() to apply
        /// the environment change. Without a reload, the new environment will only take
        /// effect on the next application launch.
        /// </para>
        /// </remarks>
        void RequestReload();
        
        /// <summary>
        /// Clears any runtime environment override (reverts to compiler flag default).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Removes the persistent environment override from PlayerPrefs, causing the
        /// application to revert to the default environment determined by compiler flags
        /// and platform settings on next restart.
        /// </para>
        /// <para>
        /// Call RequestReload() after this method to apply the change immediately.
        /// </para>
        /// </remarks>
        void ClearEnvironmentOverride();
    }
}
