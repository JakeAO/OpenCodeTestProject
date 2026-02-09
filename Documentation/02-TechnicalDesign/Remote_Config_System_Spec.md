# Remote Configuration System Specification

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.0
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [architecture-overview, game-manager-spec, analytics-system-spec]

## Overview

The Remote Configuration System provides runtime overrides of design constants without requiring game redeployment. Configs are loaded from Nakama backend at startup and cached locally. Systems can pull from remote config first, with fallback to design-time ScriptableObject defaults. Supports A/B testing via cohort-based config variants.

## Goals

- **Accessibility**: Any code can read config values via ServiceLocator
- **Type Safety**: Generic Get<T> prevents casting errors
- **Fallback Support**: Chain to ConfigManager/ScriptableObject defaults seamlessly
- **ABT Support**: Serve different configs per cohort/variant
- **Immutability**: Config values read-only after load (prevent accidental runtime changes)
- **Performance**: Synchronous read access (no async waits during gameplay)

## Dependencies

- **Nakama Backend**: Config storage and variant assignment (https://github.com/heroiclabs/nakama)
- **Architecture Overview**: Service locator pattern, integration points
- **Game Manager**: Initialization and lifecycle hooks
- **ConfigManager**: Fallback for missing remote config keys

## Constraints

- **Sync Frequency**: Load from Nakama only at startup (no periodic refresh in MVP)
- **Cache Size**: Assume hundreds of config keys (not thousands)
- **Load Time**: Config load must complete < 3 seconds (part of startup sequence)
- **Thread Safety**: Read operations thread-safe, writes only during initialization
- **Fallback Strategy**: Always have sensible defaults in ScriptableObjects

## Implementation

### Service Interface

```csharp
/// <summary>
/// Remote configuration service for runtime overrides of design constants.
/// Loaded from Nakama at startup, cached locally, with fallback to design-time defaults.
/// Supports A/B testing via cohort-based config variants.
/// </summary>
public interface IRemoteConfigManager
{
    /// <summary>
    /// Fired when remote config is loaded from Nakama.
    /// </summary>
    event Action<int> OnConfigLoaded; // int = config key count
    
    /// <summary>
    /// Fired when a config value changes (either from Nakama or local override).
    /// </summary>
    event Action<string, object, object> OnConfigChanged; // key, oldValue, newValue
    
    /// <summary>
    /// Get a typed config value by key with fallback default.
    /// Thread-safe. Returns value from cache, or default if key not found.
    /// </summary>
    T Get<T>(string key, T defaultValue = default);
    
    /// <summary>
    /// Get a config value with fallback provider function.
    /// Useful for chaining to ConfigManager or expensive computations.
    /// Thread-safe.
    /// </summary>
    T Get<T>(string key, System.Func<T> fallbackProvider);
    
    /// <summary>
    /// Check if config key exists in cache.
    /// Thread-safe.
    /// </summary>
    bool HasKey(string key);
    
    /// <summary>
    /// Get number of loaded config keys.
    /// Thread-safe.
    /// </summary>
    int GetLoadedKeyCount();
    
    /// <summary>
    /// Get user's assigned experiment ID (null if not in experiment).
    /// Thread-safe.
    /// </summary>
    string GetExperimentId();
    
    /// <summary>
    /// Get user's assigned cohort/variant (null if not in experiment).
    /// Thread-safe.
    /// </summary>
    string GetCohort();
    
    /// <summary>
    /// Load config from Nakama backend asynchronously.
    /// Must be called during startup before accessing config values.
    /// </summary>
    Task LoadFromNakamaAsync(string nakamaSession);
    
    /// <summary>
    /// Reload config from Nakama (advanced usage, not needed in MVP).
    /// </summary>
    Task ReloadAsync(string nakamaSession);
}
```

### Data Structures

```csharp
/// <summary>
/// Remote config payload from Nakama backend.
/// </summary>
[System.Serializable]
public class RemoteConfigPayload
{
    /// <summary>Config values (key-value pairs)</summary>
    public Dictionary<string, object> Config;
    
    /// <summary>Experiment ID for this user/session</summary>
    public string ExperimentId;
    
    /// <summary>Cohort/variant assignment for user</summary>
    public string Cohort;
    
    /// <summary>Timestamp when config was generated</summary>
    public long GeneratedAtMs;
    
    /// <summary>Version for future config migration</summary>
    public int Version = 1;
    
    public RemoteConfigPayload()
    {
        Config = new Dictionary<string, object>();
        GeneratedAtMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

/// <summary>
/// Config value wrapper with type-safe access and change tracking.
/// </summary>
public class ConfigValue<T>
{
    private T _value;
    private object _lock = new object();
    
    public T Value
    {
        get
        {
            lock (_lock)
            {
                return _value;
            }
        }
        private set
        {
            lock (_lock)
            {
                _value = value;
            }
        }
    }
    
    public ConfigValue(T initialValue = default)
    {
        Value = initialValue;
    }
    
    public void SetValue(T newValue)
    {
        Value = newValue;
    }
}
```

### Implementation Class

```csharp
/// <summary>
/// Remote configuration manager implementation.
/// Loads configs from Nakama at startup, caches locally, provides fallback to defaults.
/// </summary>
public class RemoteConfigManager : MonoBehaviour, IRemoteConfigManager
{
    public event Action<int> OnConfigLoaded;
    public event Action<string, object, object> OnConfigChanged;
    
    private Dictionary<string, object> _configCache;
    private System.Object _cacheLock = new object();
    
    private string _experimentId;
    private string _cohort;
    
    private bool _isLoaded = false;
    
    private void Awake()
    {
        _configCache = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Load remote config from Nakama.
    /// Call during GameManager.InitializeAllSystems() before other systems access config.
    /// </summary>
    public async Task LoadFromNakamaAsync(string nakamaSession)
    {
        try
        {
            UnityEngine.Debug.Log("[RemoteConfig] Loading from Nakama...");
            
            // Call Nakama RPC endpoint to fetch config
            var payload = await FetchConfigFromNakama(nakamaSession);
            
            if (payload != null)
            {
                lock (_cacheLock)
                {
                    _configCache = new Dictionary<string, object>(payload.Config);
                    _experimentId = payload.ExperimentId;
                    _cohort = payload.Cohort;
                }
                
                _isLoaded = true;
                OnConfigLoaded?.Invoke(_configCache.Count);
                UnityEngine.Debug.Log($"[RemoteConfig] Loaded {_configCache.Count} config keys, experiment: {_experimentId}, cohort: {_cohort}");
            }
            else
            {
                _isLoaded = true;
                UnityEngine.Debug.LogWarning("[RemoteConfig] Nakama config fetch returned null, using defaults only");
            }
        }
        catch (System.Exception ex)
        {
            // Network error: continue with empty cache (will use fallbacks)
            _isLoaded = true;
            UnityEngine.Debug.LogWarning($"[RemoteConfig] Failed to load from Nakama: {ex.Message}. Using defaults only.");
        }
    }
    
    public T Get<T>(string key, T defaultValue = default)
    {
        if (!_isLoaded)
        {
            UnityEngine.Debug.LogWarning($"[RemoteConfig] Config not yet loaded, returning default for key '{key}'");
            return defaultValue;
        }
        
        lock (_cacheLock)
        {
            if (_configCache.TryGetValue(key, out var value))
            {
                // Try to cast/convert value to requested type
                return ConvertValue<T>(value);
            }
        }
        
        return defaultValue;
    }
    
    public T Get<T>(string key, System.Func<T> fallbackProvider)
    {
        T value = Get<T>(key);
        
        // If key not found and default returned, try fallback provider
        if (value == null || (value is int i && i == 0) || (value is float f && f == 0f))
        {
            return fallbackProvider();
        }
        
        return value;
    }
    
    public bool HasKey(string key)
    {
        lock (_cacheLock)
        {
            return _configCache.ContainsKey(key);
        }
    }
    
    public int GetLoadedKeyCount()
    {
        lock (_cacheLock)
        {
            return _configCache.Count;
        }
    }
    
    public string GetExperimentId() => _experimentId;
    
    public string GetCohort() => _cohort;
    
    public async Task ReloadAsync(string nakamaSession)
    {
        await LoadFromNakamaAsync(nakamaSession);
    }
    
    private async Task<RemoteConfigPayload> FetchConfigFromNakama(string nakamaSession)
    {
        // Serialize request
        var requestPayload = new
        {
            sessionId = UnityEngine.Application.systemLanguage.ToString()
        };
        
        // Call Nakama RPC endpoint
        // Implementation depends on Nakama C# SDK setup
        // Pseudo-code:
        // var client = GetNakamaClient();
        // var response = await client.RpcAsync(session: nakamaSession, id: "FetchRemoteConfig", payload: JsonUtility.ToJson(requestPayload));
        // var payload = JsonUtility.FromJson<RemoteConfigPayload>(response.Payload);
        
        // For now, return null (will use fallbacks)
        return null;
    }
    
    private T ConvertValue<T>(object value)
    {
        // Handle type conversions
        if (value is T typedValue)
            return typedValue;
        
        var targetType = typeof(T);
        
        // Try Convert.ChangeType for numeric/primitive types
        try
        {
            return (T)System.Convert.ChangeType(value, targetType);
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"[RemoteConfig] Failed to convert {value} ({value?.GetType()}) to {targetType}");
            return default;
        }
    }
}
```

## Service Locator Integration

```csharp
// In GameManager.InitializeAllSystems()

// Create remote config manager
var remoteConfigManager = gameObject.AddComponent<RemoteConfigManager>();

// Register by interface (NOT concrete type)
ServiceLocator.Register<IRemoteConfigManager>(remoteConfigManager);

// Load config from Nakama early in startup
if (TryGetNakamaSession(out var nakamaSession))
{
    await remoteConfigManager.LoadFromNakamaAsync(nakamaSession);
}

// After config loaded, other systems can access it
// Retrieve config manager from anywhere
var config = ServiceLocator.Get<IRemoteConfigManager>();
```

## Config Hierarchy & Naming Conventions

Config keys use dot notation for hierarchy:

```
progression/
  xp_level_1          → progression.xp_level_1
  xp_level_2          → progression.xp_level_2
  stat_growth_strength → progression.stat_growth_strength
  
balance/
  enemy_damage        → balance.enemy_damage
  enemy_speed         → balance.enemy_speed
  player_dodge_base   → balance.player_dodge_base
  player_crit_chance  → balance.player_crit_chance
  
zones/
  grasslands_enemy_budget  → zones.grasslands_enemy_budget
  grasslands_enemy_types   → zones.grasslands_enemy_types
  forest_enemy_budget      → zones.forest_enemy_budget
  
features/
  new_combat          → features.new_combat (bool flag)
  new_save_system     → features.new_save_system (bool flag)
  new_ui              → features.new_ui (bool flag)
  
experiments/
  active_experiment   → experiments.active_experiment (experiment ID)
  user_cohort         → experiments.user_cohort (variant)
```

## Usage Examples

### Basic Config Access

```csharp
var config = ServiceLocator.Get<IRemoteConfigManager>();

// Get integer config with default
int maxPartySize = config.Get<int>("party.max_size", defaultValue: 10);

// Get float config
float enemyDamageMultiplier = config.Get<float>("balance.enemy_damage", defaultValue: 1.0f);

// Get bool config (feature flag)
bool enableNewCombat = config.Get<bool>("features.new_combat", defaultValue: false);

// Get string config
string activeZone = config.Get<string>("zones.current", defaultValue: "Grasslands");
```

### Fallback to ConfigManager

```csharp
// Chain to ConfigManager for expensive computations
var config = ServiceLocator.Get<IRemoteConfigManager>();
var configManager = ServiceLocator.Get<IConfigManager>();

int xpRequired = config.Get<int>("progression.xp_level_5",
    fallbackProvider: () => configManager.GetXPForLevel(5));
```

### A/B Testing Context

```csharp
var config = ServiceLocator.Get<IRemoteConfigManager>();

// Get experiment and cohort assignment
string experimentId = config.GetExperimentId();
string cohort = config.GetCohort();

if (experimentId == "balance_patch_2026")
{
    // Apply variant-specific config
    float damageMultiplier = config.Get<float>("balance.enemy_damage", 1.0f);
    // Variant A: 1.0x, Variant B: 0.8x
}
```

### Feature Flags

```csharp
var config = ServiceLocator.Get<IRemoteConfigManager>();

// Check if feature is enabled via remote config
if (config.Get<bool>("features.new_combat", defaultValue: false))
{
    // Use new combat system
    var combatSystem = gameObject.AddComponent<NewCombatSystem>();
}
else
{
    // Use legacy combat system
    var combatSystem = gameObject.AddComponent<LegacyCombatSystem>();
}
```

### Listening for Config Changes

```csharp
var config = ServiceLocator.Get<IRemoteConfigManager>();

// Subscribe to config changes
config.OnConfigChanged += (key, oldValue, newValue) =>
{
    if (key == "balance.enemy_damage")
    {
        Debug.Log($"Enemy damage changed: {oldValue} → {newValue}");
        // Update enemy spawner or difficulty scaling
    }
};
```

## Integration with ConfigManager

ConfigManager should be updated to use RemoteConfigManager as primary source:

```csharp
public class ConfigManager : MonoBehaviour, IConfigManager
{
    private IRemoteConfigManager _remoteConfig;
    
    private void Start()
    {
        _remoteConfig = ServiceLocator.Get<IRemoteConfigManager>();
    }
    
    public int GetXPForLevel(int level)
    {
        // Try remote config first
        string key = $"progression.xp_level_{level}";
        int xp = _remoteConfig.Get<int>(key);
        
        if (xp > 0)
            return xp;
        
        // Fallback to ScriptableObject default
        return GetXPForLevel_ScriptableObject(level);
    }
    
    public float GetStatGrowth(int level, StatType stat)
    {
        // Try remote config first
        string key = $"progression.stat_growth_{stat.ToString().ToLower()}_{level}";
        float growth = _remoteConfig.Get<float>(key);
        
        if (growth > 0)
            return growth;
        
        // Fallback to ScriptableObject default
        return GetStatGrowth_ScriptableObject(level, stat);
    }
}
```

## Nakama Backend Integration

Remote config is stored in Nakama and fetched via RPC endpoint.

**Nakama RPC Function**: `FetchRemoteConfig`

**Request**:
```json
{
    "sessionId": "en",
    "userId": "user_123"
}
```

**Response**:
```json
{
    "config": {
        "progression.xp_level_1": 100,
        "progression.xp_level_2": 200,
        "progression.xp_level_5": 1000,
        "balance.enemy_damage": 1.0,
        "balance.enemy_speed": 1.0,
        "balance.player_dodge_base": 0.1,
        "zones.grasslands_enemy_budget": 100,
        "zones.grasslands_enemy_types": ["Goblin", "Rat", "Spider"],
        "features.new_combat": false,
        "features.new_save_system": true,
        "party.max_size": 10
    },
    "experimentId": "balance_patch_2026",
    "cohort": "variant_b",
    "generatedAtMs": 1707432115932,
    "version": 1
}
```

Backend stores config variants per experiment:

```
experiments/
  balance_patch_2026/
    control/
      config.json        ← Variant A (1.0x enemy damage)
    treatment/
      config.json        ← Variant B (0.8x enemy damage)
```

## Performance Characteristics

| Operation | Latency | Thread Safety |
|-----------|---------|---------------|
| Get<T>() | < 0.1ms | Thread-safe |
| HasKey() | < 0.05ms | Thread-safe |
| GetLoadedKeyCount() | < 0.05ms | Thread-safe |
| GetExperimentId() | < 0.01ms | Thread-safe |
| LoadFromNakamaAsync() | 1-3 seconds | Blocking until complete |
| ReloadAsync() | 1-3 seconds | Blocking until complete |

## Testing Strategy

### Unit Tests

```csharp
[TestFixture]
public class RemoteConfigManagerTests
{
    private IRemoteConfigManager _config;
    
    [SetUp]
    public void Setup()
    {
        var go = new GameObject("RemoteConfigManager");
        var manager = go.AddComponent<RemoteConfigManager>();
        _config = manager;
    }
    
    [Test]
    public void Get_ReturnsDefaultWhenKeyNotFound()
    {
        int value = _config.Get<int>("nonexistent.key", defaultValue: 42);
        Assert.AreEqual(42, value);
    }
    
    [Test]
    public void Get_ReturnsValueWhenKeyExists()
    {
        // Manually populate cache
        // (requires exposing cache or using mock)
        // ...
        float value = _config.Get<float>("balance.enemy_damage", 1.0f);
        Assert.AreEqual(expectedValue, value);
    }
    
    [Test]
    public void HasKey_ReturnsFalseWhenNotLoaded()
    {
        Assert.IsFalse(_config.HasKey("any.key"));
    }
    
    [Test]
    public async Task LoadFromNakamaAsync_PopulatesCache()
    {
        // Mock Nakama session
        await _config.LoadFromNakamaAsync("test_session");
        
        // Verify config loaded
        Assert.Greater(_config.GetLoadedKeyCount(), 0);
    }
    
    [Test]
    public void ConvertValue_HandlesTypeConversions()
    {
        // Create manager and test internal conversion logic
        // ...
    }
}
```

### Integration Tests

```csharp
[TestFixture]
public class RemoteConfigIntegrationTests
{
    [Test]
    public async Task LoadFromNakama_LoadsExperimentContext()
    {
        var config = CreateRemoteConfigManager();
        await config.LoadFromNakamaAsync("test_session");
        
        // Verify experiment assignment
        Assert.IsNotNull(config.GetExperimentId());
        Assert.IsNotNull(config.GetCohort());
    }
    
    [Test]
    public void Get_WithFallbackProvider_UsesProviderWhenKeyMissing()
    {
        var config = CreateRemoteConfigManager();
        
        int value = config.Get<int>("missing.key", 
            fallbackProvider: () => 999);
        
        Assert.AreEqual(999, value);
    }
    
    [Test]
    public void Get_WithFallbackProvider_UsesRemoteValueWhenPresent()
    {
        var config = CreateRemoteConfigManager();
        // Populate cache with value
        config.SetValue("existing.key", 123);
        
        int value = config.Get<int>("existing.key",
            fallbackProvider: () => 999);
        
        Assert.AreEqual(123, value);
    }
}
```

## Config Management in Editor

For testing different configs locally:

1. **Create test config files** in `Assets/Resources/Configs/`:
   ```
   Assets/Resources/Configs/
   ├─ default_config.json
   ├─ variant_a_config.json
   └─ variant_b_config.json
   ```

2. **Load in Unity Editor**:
   ```csharp
   #if UNITY_EDITOR
   public static RemoteConfigPayload LoadConfigFromEditorAsset(string configName)
   {
       var asset = Resources.Load<TextAsset>($"Configs/{configName}");
       return JsonUtility.FromJson<RemoteConfigPayload>(asset.text);
   }
   #endif
   ```

## Monitoring & Debugging

Enable debug logging:

```csharp
// Check loaded config count
var config = ServiceLocator.Get<IRemoteConfigManager>();
Debug.Log($"Loaded config keys: {config.GetLoadedKeyCount()}");

// Check experiment assignment
Debug.Log($"Experiment: {config.GetExperimentId()}, Cohort: {config.GetCohort()}");

// Check specific config value
float damageMultiplier = config.Get<float>("balance.enemy_damage", 1.0f);
Debug.Log($"Enemy damage multiplier: {damageMultiplier}");
```

## Future Enhancements

1. **Periodic Refresh**: Optional config reload every 5+ minutes for live balance adjustments
2. **Config Sections**: Load configs on-demand by section (progression/, balance/, etc.)
3. **Change Notifications**: Reactive updates when config changes (for live balance tuning)
4. **Config Validation**: Schema validation for config values
5. **Config History**: Track config changes over time for rollback support
6. **A/B Test Analytics**: Auto-correlate config values with experiment outcomes

## Summary

The Remote Configuration System provides flexible, type-safe runtime overrides of design constants with automatic fallback to design-time defaults. Integration with Nakama enables A/B testing, live balance adjustments, and feature flag management without requiring game updates.

