# Network Manager System Specification

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.0
- **Last Updated**: 2026-02-09
- **Owner**: OCTP Team
- **Related Docs**: [architecture-overview, game-manager-spec, nakama-server-spec, plugins-packages]

## Overview

The Network Manager System provides environment-aware Nakama server connection management with support for local, development, staging, and production environments. It enables developers to switch between backend environments using compiler flags or runtime configuration, with a debug console interface for testing. The system ensures proper environment isolation and prevents accidental production data corruption during development.

## Goals

- **Multi-Environment Support**: Seamlessly connect to local, dev, staging, or production Nakama servers
- **Compiler Flag Integration**: Use conditional compilation for environment selection
- **Runtime Switching**: Allow environment changes via debug console without recompilation
- **Persistence**: Remember environment selection across app restarts
- **Debug Console Integration**: Provide in-game commands for network configuration
- **Build Safety**: Default to appropriate environments (local in editor, production in builds)
- **Security**: Prevent shipping development credentials in production builds

## Dependencies

- **Nakama Unity SDK**: Backend connection and authentication (https://github.com/heroiclabs/nakama-unity)
- **IngameDebugConsole**: Command interface for runtime switching (https://github.com/yasirkula/UnityIngameDebugConsole)
- **ServiceLocator**: Centralized service access pattern
- **Game Manager**: Initialization and lifecycle hooks

## Constraints

- **Environment Isolation**: Each environment must use separate server URLs and credentials
- **Restart Required**: Environment changes require app restart to reinitialize Nakama client
- **Credential Security**: Development/staging keys must be stripped from production builds
- **Persistence**: Environment selection persists in PlayerPrefs (only overrides compiler default)
- **Thread Safety**: Configuration access must be thread-safe for background network operations

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Game Systems                           │
│  (Analytics, RemoteConfig, Matchmaking, etc.)               │
└────────────────┬────────────────────────────────────────────┘
                 │ ServiceLocator.Get<INetworkManager>()
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                    NetworkManager                           │
│  • Environment Selection (Compiler + Runtime)               │
│  • Configuration Provider (URL, Port, Keys)                 │
│  • Persistence (PlayerPrefs)                                │
│  • Debug Commands (via IngameDebugConsole)                  │
└────────────────┬────────────────────────────────────────────┘
                 │ CurrentEnvironment, ServerUrl, HttpKey
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                    Nakama Client                            │
│  • Connection Management                                    │
│  • Authentication                                           │
│  • Session Management                                       │
└────────────────┬────────────────────────────────────────────┘
                 │ HTTP/WebSocket
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                 Nakama Backend Servers                      │
│  [Local:7350] [Dev:server] [Staging:server] [Prod:server]  │
└─────────────────────────────────────────────────────────────┘
```

## Environment Configuration

### NetworkEnvironment Enum

```csharp
/// <summary>
/// Available network environments for Nakama backend connection.
/// </summary>
public enum NetworkEnvironment
{
    /// <summary>Local development server (localhost:7350)</summary>
    Local = 0,
    
    /// <summary>Shared development server (for team testing)</summary>
    Development = 1,
    
    /// <summary>Staging/QA server (pre-production testing)</summary>
    Staging = 2,
    
    /// <summary>Production server (live users)</summary>
    Production = 3
}
```

### Environment Configuration Structure

```csharp
/// <summary>
/// Configuration for a single network environment.
/// </summary>
[System.Serializable]
public class EnvironmentConfig
{
    [Tooltip("Display name for this environment")]
    public string Name;
    
    [Tooltip("Nakama server URL (without protocol)")]
    public string ServerUrl;
    
    [Tooltip("Server port number")]
    public int Port = 7350;
    
    [Tooltip("Nakama HTTP key for authentication")]
    public string HttpKey = "defaultkey";
    
    [Tooltip("Use SSL/TLS for secure connection")]
    public bool UseSSL = false;
    
    [Tooltip("Optional: Custom server key for authentication")]
    public string ServerKey = "";
}
```

### Example Configurations

**Local Environment:**
```csharp
{
    Name = "Local Development",
    ServerUrl = "127.0.0.1",
    Port = 7350,
    HttpKey = "defaultkey",
    UseSSL = false,
    ServerKey = ""
}
```

**Development Environment:**
```csharp
{
    Name = "Development Server",
    ServerUrl = "dev.game-backend.example.com",
    Port = 443,
    HttpKey = "dev_http_key_abc123",
    UseSSL = true,
    ServerKey = "dev_server_key_xyz789"
}
```

**Staging Environment:**
```csharp
{
    Name = "Staging Server",
    ServerUrl = "staging.game-backend.example.com",
    Port = 443,
    HttpKey = "staging_http_key_def456",
    UseSSL = true,
    ServerKey = "staging_server_key_uvw456"
}
```

**Production Environment:**
```csharp
{
    Name = "Production Server",
    ServerUrl = "api.game-backend.example.com",
    Port = 443,
    HttpKey = "prod_http_key_ghi789",
    UseSSL = true,
    ServerKey = "prod_server_key_rst123"
}
```

## Compiler Flag Support

### Scripting Define Symbols

The Network Manager uses conditional compilation to select default environments:

- `NETWORK_LOCAL` - Default to Local environment
- `NETWORK_DEV` - Default to Development environment
- `NETWORK_STAGING` - Default to Staging environment
- `NETWORK_PRODUCTION` - Default to Production environment

### Default Behavior

**Unity Editor:**
- Default: `NETWORK_LOCAL` (unless overridden)
- Reason: Safe for local development, prevents accidental production writes

**Builds (All Platforms):**
- Default: `NETWORK_PRODUCTION` (unless overridden)
- Reason: Production builds should connect to production by default

### Implementation

```csharp
private NetworkEnvironment GetDefaultEnvironment()
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
        // Fallback: Local in editor, Production in builds
        #if UNITY_EDITOR
            return NetworkEnvironment.Local;
        #else
            return NetworkEnvironment.Production;
        #endif
    #endif
}
```

### Setting Compiler Flags

**Via Player Settings UI:**
1. Edit → Project Settings → Player
2. Other Settings → Scripting Define Symbols
3. Add: `NETWORK_LOCAL`, `NETWORK_DEV`, `NETWORK_STAGING`, or `NETWORK_PRODUCTION`

**Via Build Pipeline Script:**
```csharp
// Set staging flag for QA builds
BuildPlayerOptions buildOptions = new BuildPlayerOptions();
buildOptions.extraScriptingDefines = new[] { "NETWORK_STAGING" };
```

## Runtime Switching

### Persistent Environment Selection

The Network Manager stores environment overrides in PlayerPrefs:

```csharp
private const string PREF_KEY_ENVIRONMENT = "OCTP.Network.Environment";

/// <summary>
/// Set environment for next session (persists via PlayerPrefs).
/// Requires app restart to take effect.
/// </summary>
public void SetEnvironment(NetworkEnvironment environment)
{
    PlayerPrefs.SetInt(PREF_KEY_ENVIRONMENT, (int)environment);
    PlayerPrefs.Save();
    
    Debug.Log($"[NetworkManager] Environment set to {environment}. Restart required.");
    OnEnvironmentChanged?.Invoke(environment);
}

/// <summary>
/// Get environment with runtime override priority:
/// 1. PlayerPrefs override (if set)
/// 2. Compiler flag default
/// </summary>
public NetworkEnvironment GetCurrentEnvironment()
{
    if (PlayerPrefs.HasKey(PREF_KEY_ENVIRONMENT))
    {
        int envValue = PlayerPrefs.GetInt(PREF_KEY_ENVIRONMENT);
        return (NetworkEnvironment)envValue;
    }
    
    return GetDefaultEnvironment();
}
```

### Restart Requirement

Environment changes require an app restart because:
1. Nakama Client is initialized once at startup
2. Active connections/sessions must be closed gracefully
3. Prevents mid-session environment confusion

The `RequestReload()` method provides a safe reload mechanism:

```csharp
/// <summary>
/// Request app reload to apply new environment settings.
/// In editor: Reloads current scene.
/// In builds: Displays restart prompt to user.
/// </summary>
public void RequestReload()
{
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => 
        {
            UnityEditor.EditorApplication.isPlaying = true;
        });
    #else
        // Show in-game dialog: "Settings changed. Please restart the app."
        Debug.LogWarning("[NetworkManager] Environment changed. User restart required.");
        // Could integrate with UI system to show restart dialog
    #endif
}
```

### Clearing Override

```csharp
/// <summary>
/// Clear environment override, reverting to compiler flag default.
/// </summary>
public void ClearEnvironmentOverride()
{
    if (PlayerPrefs.HasKey(PREF_KEY_ENVIRONMENT))
    {
        PlayerPrefs.DeleteKey(PREF_KEY_ENVIRONMENT);
        PlayerPrefs.Save();
        Debug.Log("[NetworkManager] Environment override cleared. Restart to apply.");
    }
}
```

## INetworkManager Interface

```csharp
/// <summary>
/// Network configuration manager for multi-environment Nakama backend connections.
/// Provides environment switching via compiler flags and runtime configuration.
/// </summary>
public interface INetworkManager
{
    /// <summary>
    /// Current active network environment (may differ from default if overridden).
    /// </summary>
    NetworkEnvironment CurrentEnvironment { get; }
    
    /// <summary>
    /// Nakama server URL for current environment (without protocol).
    /// </summary>
    string ServerUrl { get; }
    
    /// <summary>
    /// Server port for current environment.
    /// </summary>
    int ServerPort { get; }
    
    /// <summary>
    /// HTTP key for Nakama authentication (current environment).
    /// </summary>
    string HttpKey { get; }
    
    /// <summary>
    /// Whether to use SSL/TLS for secure connection.
    /// </summary>
    bool UseSSL { get; }
    
    /// <summary>
    /// Optional server key for additional authentication.
    /// </summary>
    string ServerKey { get; }
    
    /// <summary>
    /// Fired when environment selection changes (before restart).
    /// </summary>
    event Action<NetworkEnvironment> OnEnvironmentChanged;
    
    /// <summary>
    /// Set environment for next session (persists to PlayerPrefs).
    /// Requires app restart to take effect.
    /// </summary>
    void SetEnvironment(NetworkEnvironment environment);
    
    /// <summary>
    /// Clear environment override, reverting to compiler flag default.
    /// Requires app restart to take effect.
    /// </summary>
    void ClearEnvironmentOverride();
    
    /// <summary>
    /// Request app reload to apply environment changes.
    /// Editor: Stops/starts play mode. Builds: Prompts user to restart.
    /// </summary>
    void RequestReload();
    
    /// <summary>
    /// Get default environment based on compiler flags and platform.
    /// </summary>
    NetworkEnvironment GetDefaultEnvironment();
    
    /// <summary>
    /// Check if environment is overridden via runtime setting.
    /// </summary>
    bool HasEnvironmentOverride();
}
```

## NetworkConfig ScriptableObject

```csharp
/// <summary>
/// ScriptableObject containing all environment configurations.
/// Asset location: Assets/Resources/Config/NetworkConfig.asset
/// </summary>
[CreateAssetMenu(fileName = "NetworkConfig", menuName = "OCTP/Network/Network Config")]
public class NetworkConfig : ScriptableObject
{
    [Header("Environment Configurations")]
    [Tooltip("Configuration for local development server")]
    public EnvironmentConfig LocalConfig;
    
    [Tooltip("Configuration for shared development server")]
    public EnvironmentConfig DevelopmentConfig;
    
    [Tooltip("Configuration for staging/QA server")]
    public EnvironmentConfig StagingConfig;
    
    [Tooltip("Configuration for production server")]
    public EnvironmentConfig ProductionConfig;
    
    /// <summary>
    /// Get configuration for specified environment.
    /// </summary>
    public EnvironmentConfig GetConfig(NetworkEnvironment environment)
    {
        switch (environment)
        {
            case NetworkEnvironment.Local:
                return LocalConfig;
            case NetworkEnvironment.Development:
                return DevelopmentConfig;
            case NetworkEnvironment.Staging:
                return StagingConfig;
            case NetworkEnvironment.Production:
                return ProductionConfig;
            default:
                Debug.LogError($"[NetworkConfig] Unknown environment: {environment}");
                return LocalConfig; // Safe fallback
        }
    }
    
    /// <summary>
    /// Validate all configurations (called in OnValidate).
    /// </summary>
    public void ValidateConfigs()
    {
        ValidateConfig(LocalConfig, "Local");
        ValidateConfig(DevelopmentConfig, "Development");
        ValidateConfig(StagingConfig, "Staging");
        ValidateConfig(ProductionConfig, "Production");
    }
    
    private void ValidateConfig(EnvironmentConfig config, string envName)
    {
        if (string.IsNullOrEmpty(config.ServerUrl))
            Debug.LogWarning($"[NetworkConfig] {envName} ServerUrl is empty!");
            
        if (config.Port <= 0 || config.Port > 65535)
            Debug.LogWarning($"[NetworkConfig] {envName} Port is invalid: {config.Port}");
            
        if (string.IsNullOrEmpty(config.HttpKey))
            Debug.LogWarning($"[NetworkConfig] {envName} HttpKey is empty!");
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateConfigs();
    }
    #endif
}
```

## IngameDebugConsole Integration

The Network Manager registers console commands for runtime environment management:

### Available Commands

**1. `network_status`**
- **Description**: Display current network environment and configuration
- **Output**:
  ```
  [Network Status]
  Current Environment: Development
  Default Environment: Local (NETWORK_LOCAL)
  Override Active: Yes
  
  Server URL: dev.game-backend.example.com
  Port: 443
  SSL: Enabled
  HTTP Key: dev_http_***23 (masked)
  
  Available Environments:
  - Local (127.0.0.1:7350)
  - Development (dev.game-backend.example.com:443) [CURRENT]
  - Staging (staging.game-backend.example.com:443)
  - Production (api.game-backend.example.com:443)
  ```

**2. `network_switch <environment>`**
- **Description**: Switch to specified environment (requires restart)
- **Arguments**: `local`, `dev`, `staging`, `prod` (case-insensitive)
- **Example**: `network_switch staging`
- **Output**:
  ```
  [NetworkManager] Environment set to Staging.
  [NetworkManager] Restart required to apply changes.
  [NetworkManager] Use 'network_reload' to restart now.
  ```

**3. `network_reload`**
- **Description**: Restart app to apply environment changes
- **Behavior**:
  - Editor: Stops and restarts play mode
  - Builds: Displays restart prompt to user
- **Output**:
  ```
  [NetworkManager] Reloading to apply environment changes...
  ```

**4. `network_clear_override`**
- **Description**: Clear environment override, revert to compiler flag default
- **Output**:
  ```
  [NetworkManager] Environment override cleared.
  [NetworkManager] Will revert to Local (compiler default) on next restart.
  [NetworkManager] Use 'network_reload' to restart now.
  ```

### Command Registration

```csharp
#if INGAME_DEBUG_CONSOLE
using IngameDebugConsole;

public class NetworkManager : INetworkManager
{
    private void RegisterDebugConsoleCommands()
    {
        DebugLogConsole.AddCommand("network_status", 
            "Display current network environment and configuration", 
            ShowNetworkStatus);
            
        DebugLogConsole.AddCommand<string>("network_switch", 
            "Switch network environment (local/dev/staging/prod)", 
            SwitchEnvironmentCommand);
            
        DebugLogConsole.AddCommand("network_reload", 
            "Restart app to apply network environment changes", 
            RequestReload);
            
        DebugLogConsole.AddCommand("network_clear_override", 
            "Clear environment override and revert to default", 
            ClearEnvironmentOverrideCommand);
    }
    
    private void ShowNetworkStatus()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[Network Status]");
        sb.AppendLine($"Current Environment: {CurrentEnvironment}");
        sb.AppendLine($"Default Environment: {GetDefaultEnvironment()} " +
            $"(compiler: {GetCompilerFlagName()})");
        sb.AppendLine($"Override Active: {(HasEnvironmentOverride() ? "Yes" : "No")}");
        sb.AppendLine();
        sb.AppendLine($"Server URL: {ServerUrl}");
        sb.AppendLine($"Port: {ServerPort}");
        sb.AppendLine($"SSL: {(UseSSL ? "Enabled" : "Disabled")}");
        sb.AppendLine($"HTTP Key: {MaskKey(HttpKey)}");
        sb.AppendLine();
        sb.AppendLine("Available Environments:");
        
        foreach (NetworkEnvironment env in System.Enum.GetValues(typeof(NetworkEnvironment)))
        {
            EnvironmentConfig cfg = _config.GetConfig(env);
            string marker = (env == CurrentEnvironment) ? " [CURRENT]" : "";
            sb.AppendLine($"  - {env} ({cfg.ServerUrl}:{cfg.Port}){marker}");
        }
        
        Debug.Log(sb.ToString());
    }
    
    private void SwitchEnvironmentCommand(string envName)
    {
        NetworkEnvironment targetEnv;
        
        switch (envName.ToLower())
        {
            case "local":
            case "loc":
                targetEnv = NetworkEnvironment.Local;
                break;
            case "dev":
            case "development":
                targetEnv = NetworkEnvironment.Development;
                break;
            case "staging":
            case "stage":
                targetEnv = NetworkEnvironment.Staging;
                break;
            case "prod":
            case "production":
                targetEnv = NetworkEnvironment.Production;
                break;
            default:
                Debug.LogError($"[NetworkManager] Unknown environment: {envName}. " +
                    "Use: local, dev, staging, or prod");
                return;
        }
        
        SetEnvironment(targetEnv);
        Debug.Log("[NetworkManager] Restart required to apply changes.");
        Debug.Log("[NetworkManager] Use 'network_reload' to restart now.");
    }
    
    private void ClearEnvironmentOverrideCommand()
    {
        ClearEnvironmentOverride();
        Debug.Log($"[NetworkManager] Will revert to {GetDefaultEnvironment()} " +
            "(compiler default) on next restart.");
        Debug.Log("[NetworkManager] Use 'network_reload' to restart now.");
    }
    
    private string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 6)
            return "***";
        
        return key.Substring(0, key.Length - 2) + "**" + 
            key.Substring(key.Length - 2);
    }
    
    private string GetCompilerFlagName()
    {
        #if NETWORK_PRODUCTION
            return "NETWORK_PRODUCTION";
        #elif NETWORK_STAGING
            return "NETWORK_STAGING";
        #elif NETWORK_DEV
            return "NETWORK_DEV";
        #elif NETWORK_LOCAL
            return "NETWORK_LOCAL";
        #else
            return "None (auto-select)";
        #endif
    }
}
#endif
```

## Implementation Details

### Initialization Order

1. **ServiceLocator Registration** (GameManager.Awake)
   ```csharp
   ServiceLocator.Register<INetworkManager>(new NetworkManager());
   ```

2. **Load NetworkConfig ScriptableObject**
   ```csharp
   NetworkConfig config = Resources.Load<NetworkConfig>("Config/NetworkConfig");
   ```

3. **Determine Active Environment**
   - Check PlayerPrefs for override (`OCTP.Network.Environment`)
   - If no override, use compiler flag default
   - Apply configuration for selected environment

4. **Initialize Nakama Client** (GameManager.Start)
   ```csharp
   INetworkManager netManager = ServiceLocator.Get<INetworkManager>();
   
   var client = new Nakama.Client(
       scheme: netManager.UseSSL ? "https" : "http",
       host: netManager.ServerUrl,
       port: netManager.ServerPort,
       serverKey: netManager.HttpKey
   );
   ```

5. **Register Debug Console Commands**
   ```csharp
   #if INGAME_DEBUG_CONSOLE
       networkManager.RegisterDebugCommands();
   #endif
   ```

### PlayerPrefs Key

The Network Manager uses a single PlayerPrefs key:

```csharp
private const string PREF_KEY_ENVIRONMENT = "OCTP.Network.Environment";
```

**Value Format**: Integer enum value (0=Local, 1=Development, 2=Staging, 3=Production)

### Reload Mechanism

The reload mechanism differs by platform:

**Unity Editor:**
```csharp
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
    await System.Threading.Tasks.Task.Delay(100);
    UnityEditor.EditorApplication.isPlaying = true;
#endif
```

**Builds:**
```csharp
// Option 1: Quit and let OS/user restart
Application.Quit();

// Option 2: Show UI dialog prompting manual restart
UIManager.ShowDialog(
    title: "Restart Required",
    message: "Network settings changed. Please restart the app to apply.",
    confirmText: "Quit"
).OnConfirm(() => Application.Quit());
```

### Thread Safety

Configuration properties are read-only after initialization:

```csharp
private NetworkEnvironment _currentEnvironment;
private EnvironmentConfig _activeConfig;
private readonly object _lock = new object();

public NetworkEnvironment CurrentEnvironment 
{ 
    get 
    { 
        lock (_lock) 
        { 
            return _currentEnvironment; 
        } 
    } 
}

public string ServerUrl => _activeConfig.ServerUrl; // Immutable after init
```

## Security Considerations

### 1. Credential Isolation

**Problem**: Development/staging credentials in production builds expose backend to attacks.

**Solution**: Use compiler flag stripping in build pipeline

```csharp
// In NetworkConfig.GetConfig()
public EnvironmentConfig GetConfig(NetworkEnvironment environment)
{
    #if !UNITY_EDITOR && !NETWORK_DEV && !NETWORK_STAGING
        // In production builds WITHOUT dev/staging flags, 
        // block access to non-production environments
        if (environment != NetworkEnvironment.Production)
        {
            Debug.LogError($"[NetworkConfig] Access to {environment} blocked in production build. " +
                "Using Production config.");
            return ProductionConfig;
        }
    #endif
    
    switch (environment)
    {
        // ... rest of implementation
    }
}
```

### 2. Key Masking in Logs

**Problem**: HTTP keys logged to console can leak credentials.

**Solution**: Mask keys in debug output

```csharp
private string MaskKey(string key)
{
    if (string.IsNullOrEmpty(key) || key.Length < 6)
        return "***";
    
    // Show first N-2 chars and last 2 chars
    return key.Substring(0, key.Length - 4) + "***" + 
        key.Substring(key.Length - 2);
}

// Usage
Debug.Log($"Connected with key: {MaskKey(HttpKey)}");
```

### 3. ScriptableObject Protection

**Problem**: NetworkConfig.asset committed to version control exposes production keys.

**Solution Options**:

**A. Git-ignored secrets file:**
```
# .gitignore
Assets/Resources/Config/NetworkConfig_Production.asset
Assets/Resources/Config/NetworkConfig_Production.asset.meta
```

**B. Build-time injection:**
```csharp
// In build pipeline script
public static void InjectProductionConfig()
{
    NetworkConfig config = Resources.Load<NetworkConfig>("Config/NetworkConfig");
    
    // Load from secure environment variable or key vault
    config.ProductionConfig.HttpKey = System.Environment.GetEnvironmentVariable("NAKAMA_PROD_KEY");
    config.ProductionConfig.ServerKey = System.Environment.GetEnvironmentVariable("NAKAMA_PROD_SERVER_KEY");
    
    UnityEditor.EditorUtility.SetDirty(config);
    UnityEditor.AssetDatabase.SaveAssets();
}
```

**C. Encrypted ScriptableObject (Advanced):**
```csharp
[System.Serializable]
public class EncryptedEnvironmentConfig
{
    [SerializeField] private byte[] _encryptedData;
    
    public EnvironmentConfig Decrypt(string password)
    {
        // AES decrypt _encryptedData using password from secure source
    }
}
```

### 4. Runtime Override Restrictions

**Problem**: Users could modify PlayerPrefs to access dev/staging servers.

**Solution**: Validate environment availability in builds

```csharp
public void SetEnvironment(NetworkEnvironment environment)
{
    #if !UNITY_EDITOR && !NETWORK_DEV && !NETWORK_STAGING
        // Block non-production environments in production builds
        if (environment != NetworkEnvironment.Production)
        {
            Debug.LogWarning($"[NetworkManager] Environment {environment} not available in this build.");
            return;
        }
    #endif
    
    PlayerPrefs.SetInt(PREF_KEY_ENVIRONMENT, (int)environment);
    PlayerPrefs.Save();
}
```

## Testing Strategy

### Unit Tests

**Test Compiler Flag Selection:**
```csharp
[Test]
public void CompilerFlag_Local_SetsLocalEnvironment()
{
    // Requires NETWORK_LOCAL flag in test assembly
    NetworkManager manager = new NetworkManager();
    Assert.AreEqual(NetworkEnvironment.Local, manager.GetDefaultEnvironment());
}

[Test]
public void CompilerFlag_Production_SetsProductionEnvironment()
{
    // Requires NETWORK_PRODUCTION flag in test assembly
    NetworkManager manager = new NetworkManager();
    Assert.AreEqual(NetworkEnvironment.Production, manager.GetDefaultEnvironment());
}
```

**Test Runtime Override Persistence:**
```csharp
[Test]
public void RuntimeOverride_Persists_AcrossRestarts()
{
    NetworkManager manager = new NetworkManager();
    manager.SetEnvironment(NetworkEnvironment.Staging);
    
    // Simulate restart
    manager = new NetworkManager();
    Assert.AreEqual(NetworkEnvironment.Staging, manager.CurrentEnvironment);
}

[Test]
public void ClearOverride_RestoresDefault()
{
    NetworkManager manager = new NetworkManager();
    manager.SetEnvironment(NetworkEnvironment.Development);
    manager.ClearEnvironmentOverride();
    
    // Simulate restart
    manager = new NetworkManager();
    Assert.AreEqual(manager.GetDefaultEnvironment(), manager.CurrentEnvironment);
}
```

**Test Configuration Loading:**
```csharp
[Test]
public void NetworkConfig_AllEnvironments_LoadSuccessfully()
{
    NetworkConfig config = Resources.Load<NetworkConfig>("Config/NetworkConfig");
    Assert.IsNotNull(config);
    
    Assert.IsNotNull(config.GetConfig(NetworkEnvironment.Local));
    Assert.IsNotNull(config.GetConfig(NetworkEnvironment.Development));
    Assert.IsNotNull(config.GetConfig(NetworkEnvironment.Staging));
    Assert.IsNotNull(config.GetConfig(NetworkEnvironment.Production));
}

[Test]
public void NetworkConfig_Validation_DetectsInvalidPort()
{
    NetworkConfig config = ScriptableObject.CreateInstance<NetworkConfig>();
    config.LocalConfig.Port = -1;
    
    // Should log warning
    config.ValidateConfigs();
}
```

### Integration Tests

**Test Nakama Client Initialization:**
```csharp
[UnityTest]
public IEnumerator NakamaClient_InitializesWithLocalConfig()
{
    INetworkManager networkManager = ServiceLocator.Get<INetworkManager>();
    networkManager.SetEnvironment(NetworkEnvironment.Local);
    
    var client = new Nakama.Client(
        scheme: networkManager.UseSSL ? "https" : "http",
        host: networkManager.ServerUrl,
        port: networkManager.ServerPort,
        serverKey: networkManager.HttpKey
    );
    
    Assert.IsNotNull(client);
    Assert.AreEqual("127.0.0.1", networkManager.ServerUrl);
    Assert.AreEqual(7350, networkManager.ServerPort);
    
    yield return null;
}
```

**Test Environment Switching:**
```csharp
[UnityTest]
public IEnumerator EnvironmentSwitch_FiresEvent()
{
    NetworkManager manager = new NetworkManager();
    bool eventFired = false;
    NetworkEnvironment receivedEnv = NetworkEnvironment.Local;
    
    manager.OnEnvironmentChanged += (env) =>
    {
        eventFired = true;
        receivedEnv = env;
    };
    
    manager.SetEnvironment(NetworkEnvironment.Staging);
    
    Assert.IsTrue(eventFired);
    Assert.AreEqual(NetworkEnvironment.Staging, receivedEnv);
    
    yield return null;
}
```

### Manual Testing Checklist

**Editor Testing:**
- [ ] Set `NETWORK_LOCAL` flag, verify local server connection
- [ ] Set `NETWORK_DEV` flag, verify dev server connection
- [ ] Use `network_status` command, verify output shows correct environment
- [ ] Use `network_switch staging`, verify environment changes after reload
- [ ] Use `network_clear_override`, verify reverts to compiler default

**Build Testing:**
- [ ] Create build without flags (should default to Production)
- [ ] Create build with `NETWORK_STAGING` flag, verify staging connection
- [ ] Verify production build blocks access to dev/staging (if security enabled)
- [ ] Test PlayerPrefs persistence across app restarts
- [ ] Test `network_reload` command behavior in builds

**Security Testing:**
- [ ] Verify production keys not exposed in version control
- [ ] Verify HTTP keys masked in console output
- [ ] Verify non-production environments blocked in production builds
- [ ] Test tampering with PlayerPrefs (if validation enabled)

## Usage Examples

### Example 1: Initialize Nakama Client

```csharp
using UnityEngine;
using Nakama;

public class NakamaService : MonoBehaviour
{
    private IClient _client;
    private ISession _session;
    
    private void Awake()
    {
        INetworkManager networkManager = ServiceLocator.Get<INetworkManager>();
        
        // Create Nakama client with current environment config
        _client = new Client(
            scheme: networkManager.UseSSL ? "https" : "http",
            host: networkManager.ServerUrl,
            port: networkManager.ServerPort,
            serverKey: networkManager.HttpKey
        );
        
        Debug.Log($"[NakamaService] Initialized client for {networkManager.CurrentEnvironment} environment");
    }
    
    private async void Start()
    {
        try
        {
            // Authenticate with device ID
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            _session = await _client.AuthenticateDeviceAsync(deviceId);
            
            Debug.Log($"[NakamaService] Authenticated: {_session.UserId}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NakamaService] Authentication failed: {ex.Message}");
        }
    }
}
```

### Example 2: Environment-Specific Features

```csharp
public class AnalyticsManager : MonoBehaviour
{
    private void Start()
    {
        INetworkManager networkManager = ServiceLocator.Get<INetworkManager>();
        
        // Enable verbose logging in non-production environments
        if (networkManager.CurrentEnvironment != NetworkEnvironment.Production)
        {
            Debug.Log("[AnalyticsManager] Verbose logging enabled (non-production)");
            SetVerboseLogging(true);
        }
        
        // Reduce batch size in local environment for faster testing
        if (networkManager.CurrentEnvironment == NetworkEnvironment.Local)
        {
            SetEventBatchSize(10); // Send after 10 events instead of 100
        }
    }
}
```

### Example 3: Debug Console Usage

```csharp
// In-game console session:

> network_status
[Network Status]
Current Environment: Local
Default Environment: Local (NETWORK_LOCAL)
Override Active: No

Server URL: 127.0.0.1
Port: 7350
SSL: Disabled
HTTP Key: default***ey

Available Environments:
  - Local (127.0.0.1:7350) [CURRENT]
  - Development (dev.game-backend.example.com:443)
  - Staging (staging.game-backend.example.com:443)
  - Production (api.game-backend.example.com:443)

> network_switch dev
[NetworkManager] Environment set to Development.
[NetworkManager] Restart required to apply changes.
[NetworkManager] Use 'network_reload' to restart now.

> network_reload
[NetworkManager] Reloading to apply environment changes...
[Play mode stopped and restarted]

> network_status
[Network Status]
Current Environment: Development
Default Environment: Local (NETWORK_LOCAL)
Override Active: Yes

Server URL: dev.game-backend.example.com
Port: 443
SSL: Enabled
HTTP Key: dev_http_***23
```

### Example 4: Conditional Compilation for Features

```csharp
public class DebugTools : MonoBehaviour
{
    private void Awake()
    {
        #if NETWORK_PRODUCTION
            // Disable debug tools in production
            gameObject.SetActive(false);
        #else
            // Enable debug tools in all other environments
            gameObject.SetActive(true);
        #endif
    }
}
```

### Example 5: Build Pipeline Integration

```csharp
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildPipeline
{
    [MenuItem("Build/Build Staging")]
    public static void BuildStaging()
    {
        // Set staging flag
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.extraScriptingDefines = new[] { "NETWORK_STAGING" };
        options.target = BuildTarget.StandaloneWindows64;
        options.locationPathName = "Builds/Staging/Game.exe";
        
        BuildReport report = BuildPipeline.BuildPlayer(options);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Staging build completed: {report.summary.totalSize} bytes");
        }
    }
    
    [MenuItem("Build/Build Production")]
    public static void BuildProduction()
    {
        // Set production flag and inject secure credentials
        InjectProductionCredentials();
        
        BuildPlayerOptions options = new BuildPlayerOptions();
        options.extraScriptingDefines = new[] { "NETWORK_PRODUCTION" };
        options.target = BuildTarget.StandaloneWindows64;
        options.locationPathName = "Builds/Production/Game.exe";
        
        BuildReport report = BuildPipeline.BuildPlayer(options);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Production build completed: {report.summary.totalSize} bytes");
        }
    }
    
    private static void InjectProductionCredentials()
    {
        NetworkConfig config = Resources.Load<NetworkConfig>("Config/NetworkConfig");
        
        // Load from secure environment variables
        string prodKey = System.Environment.GetEnvironmentVariable("NAKAMA_PROD_KEY");
        string serverKey = System.Environment.GetEnvironmentVariable("NAKAMA_PROD_SERVER_KEY");
        
        if (!string.IsNullOrEmpty(prodKey))
        {
            config.ProductionConfig.HttpKey = prodKey;
        }
        
        if (!string.IsNullOrEmpty(serverKey))
        {
            config.ProductionConfig.ServerKey = serverKey;
        }
        
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        
        Debug.Log("[BuildPipeline] Production credentials injected");
    }
}
```

## References

### External Documentation
- [Nakama Unity SDK Documentation](https://heroiclabs.com/docs/nakama/client-libraries/unity/)
- [Nakama Client Configuration](https://heroiclabs.com/docs/nakama/client-libraries/unity/#client-configuration)
- [Unity Scripting Define Symbols](https://docs.unity3d.com/Manual/CustomScriptingSymbols.html)
- [IngameDebugConsole GitHub](https://github.com/yasirkula/UnityIngameDebugConsole)

### Related Specifications
- [Nakama Server Specification](Nakama_Server_Spec.md) - Backend setup and deployment
- [Analytics System Specification](Analytics_System_Spec.md) - Event tracking via Nakama
- [Remote Config System Specification](Remote_Config_System_Spec.md) - Config loading via Nakama
- [Architecture Overview](Architecture_Overview.md) - Service locator pattern
- [Plugins and Packages Overview](Plugins_And_Packages_Overview.md) - Nakama SDK integration

### Internal Links
- Issue: Network environment switching implementation
- PR: Multi-environment Nakama configuration
- Docs: Deployment guide for staging and production environments
