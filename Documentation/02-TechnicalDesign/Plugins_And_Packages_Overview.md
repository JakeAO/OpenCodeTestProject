# Plugins and Packages Overview

## Metadata
- **Type**: Technical Reference
- **Status**: Active
- **Version**: 1.2
- **Last Updated**: 2026-02-09
- **Owner**: OCTP Team
- **Related Docs**: Nakama_Server_Spec.md, Scene_Management_Spec.md, State_Management_Spec.md, Network_Manager_Spec.md

## Overview

This document catalogs all third-party plugins and Unity packages integrated into the OCTP project, their versions, purposes, and usage guidelines. All plugins are located in `Assets/Plugins/` and Unity packages are managed via Unity Package Manager.

## Purpose

- **Centralized Reference**: Single source of truth for all external dependencies
- **Version Tracking**: Track versions for compatibility and updates
- **Usage Guidelines**: Establish best practices for each plugin
- **Onboarding**: Help new developers understand the plugin ecosystem

---

## Third-Party Plugins (Assets/Plugins/)

### 1. Nakama Unity SDK

**Version**: 3.21.0  
**Location**: `Assets/Plugins/Nakama/`  
**Type**: Game Server Client SDK  
**License**: Apache 2.0  
**Documentation**: [Nakama Unity Client Guide](https://heroiclabs.com/docs/unity-client-guide)

**Purpose**:
- Connect Unity client to OCTP Nakama server (OCTP_Server/)
- Send analytics events via RPC calls
- Fetch remote configuration
- Manage experiment assignments

**Key Features**:
- User authentication (device, custom, email, social)
- Real-time sockets for multiplayer (future use)
- RPC system for custom server logic
- Storage and leaderboards (future use)

**Integration Points**:
- `AnalyticsManager`: Uses Nakama client for batch event submission
- `RemoteConfigManager`: Fetches config via FetchRemoteConfig RPC
- Assembly: `Nakama.asmdef` (referenced by OCTP.Services)

**Setup Requirements**:
- Server URL: `http://localhost:7350` (dev), production URL TBD
- HTTP Key: `defaulthttpkey` (dev), **must change in production**
- Client initialization in `GameManager.Awake()`

**Best Practices**:
- ✅ Always authenticate before RPC calls
- ✅ Use async/await pattern for all Nakama operations
- ✅ Cache session token, don't re-authenticate every RPC
- ✅ Handle network errors gracefully (retry logic + fallbacks)
- ❌ Don't call RPCs from Update() (performance impact)
- ❌ Don't hardcode HTTP keys (use ScriptableObject config)

**Common Pitfalls**:
- Forgetting to authenticate before RPC calls → 401 errors
- Not handling WebGL CORS issues (configure Nakama CORS settings)
- Missing JSON serialization for custom payloads

---

### 2. PrimeTween

**Version**: 1.3.7  
**Location**: `Assets/Plugins/PrimeTween/`  
**Type**: Animation Library  
**License**: Commercial (Asset Store)  
**Documentation**: [PrimeTween GitHub](https://github.com/KyryloKuzyk/PrimeTween)

**Purpose**:
- High-performance, zero-allocation animation system
- Tween UI elements, transforms, materials, values
- Animation sequences and callbacks

**Key Features**:
- Zero GC allocations (struct-based design)
- Faster than DOTween and LeanTween (benchmarked)
- Supports all Unity types (Vector3, Color, float, Quaternion, etc.)
- Inspector integration for visual animation editing
- Sequence builder with delays, callbacks, loops

**Integration Points**:
- UI animations (menu transitions, button feedback)
- Character movement interpolation
- Camera shake and effects
- Damage number pop-ups

**Usage Examples**:
```csharp
// Simple position tween (1 line, zero allocations)
Tween.Position(transform, endValue: new Vector3(5, 0, 0), duration: 1f);

// UI fade with callback
Tween.Alpha(canvasGroup, endValue: 0f, duration: 0.5f)
    .OnComplete(() => gameObject.SetActive(false));

// Sequence with delays
Tween.Sequence()
    .Append(Tween.Scale(logo, 1.2f, 0.3f))
    .Append(Tween.Rotation(logo, new Vector3(0, 0, 360), 0.5f))
    .Append(Tween.Scale(logo, 1f, 0.3f));

// Shake effect
Tween.ShakeLocalPosition(camera.transform, strength: 0.5f, duration: 0.3f);
```

**Best Practices**:
- ✅ Store Tween references to stop/control them: `Tween tween = Tween.Position(...);`
- ✅ Use `SetRemainingCycles(-1)` for infinite loops
- ✅ Use `useUnscaledTime: true` for UI animations (unaffected by Time.timeScale)
- ✅ Call `tween.Stop()` or `tween.Complete()` when destroying objects mid-animation
- ❌ Don't create new tweens every frame (check if tween is alive first)
- ❌ Don't tween properties that physics controls (use Rigidbody.MovePosition instead)

**Performance Notes**:
- All tweens are struct-based (no heap allocations)
- Tweens are stored in arrays with minimal overhead
- Can handle 1000+ active tweens at 60fps

---

### 3. TransitionsPlus

**Version**: 5.1.1  
**Location**: `Assets/Plugins/TransitionsPlus/`  
**Type**: Screen Transition Effects  
**License**: Commercial (Asset Store - Kronnect)  
**Documentation**: `Assets/Plugins/TransitionsPlus/Documentation/`

**Purpose**:
- Full-screen transition effects between scenes/cameras
- Visual polish for scene changes
- Multiple effect types (fade, wipe, dissolve, shapes, etc.)

**Key Features**:
- 15+ transition effects (Fade, Wipe, Shape, Spiral, Pixelate, Melt, Cube, etc.)
- Camera-to-camera or overlay modes
- Profile-based configurations (save/load transition settings)
- Code API + Inspector-driven workflow
- VR support

**Integration Points**:
- Scene transitions (MenuState → GameplayState)
- Zone transitions (safe zone → exploration zone)
- Death screen effects
- Victory/defeat screen transitions

**Usage Examples**:
```csharp
// Simple fade transition
TransitionAnimator.Start(TransitionProfile.Fade, duration: 1f);

// Code-controlled transition with specific effect
TransitionAnimator.Start(
    effect: TransitionProfile.Effect.Wipe,
    duration: 1.5f,
    onComplete: () => SceneManager.LoadScene("GameplayScene")
);

// Using a saved profile
TransitionAnimator.Start(myTransitionProfile);
```

**Best Practices**:
- ✅ Create profiles for common transitions (fade in/out, scene changes)
- ✅ Use `onComplete` callback for scene loading synchronization
- ✅ Test transitions with different screen resolutions/aspect ratios
- ✅ Set `useUnscaledTime = true` for pause-independent transitions
- ❌ Don't start multiple transitions simultaneously (wait for completion)
- ❌ Don't use heavy effects (Melt, Cube) every frame (performance cost)

**Performance Notes**:
- Full-screen shader effects can be expensive on mobile
- Use simpler effects (Fade, Wipe) for frequently triggered transitions
- Consider disabling post-processing during transitions for performance

---

### 4. IngameDebugConsole

**Version**: 1.8.4  
**Location**: `Assets/Plugins/IngameDebugConsole/`  
**Type**: Runtime Debug Console  
**License**: MIT  
**Documentation**: [GitHub](https://github.com/yasirkula/UnityIngameDebugConsole)

**Purpose**:
- View Unity console logs on device (mobile, standalone builds)
- Execute custom commands at runtime
- Debug production builds without development console

**Key Features**:
- Captures Debug.Log/LogWarning/LogError
- Filterable log levels
- Search and timestamp display
- Custom command registration
- Touch-friendly UI for mobile testing

**Integration Points**:
- Development builds only (exclude from production)
- Custom debug commands for testing (spawn enemies, give gold, etc.)
- QA testing on devices
- Remote debugging via touch interface

**Usage Examples**:
```csharp
// Place prefab in scene (Assets/Plugins/IngameDebugConsole/IngameDebugConsole.prefab)
// Logs automatically appear

// Register custom commands
[ConsoleMethod("give_gold", "Adds gold to player")]
public static void GiveGold(int amount)
{
    PlayerInventory.Gold += amount;
    Debug.Log($"Added {amount} gold");
}

[ConsoleMethod("teleport", "Teleport to zone")]
public static void Teleport(string zoneName)
{
    ZoneManager.LoadZone(zoneName);
}
```

**Best Practices**:
- ✅ Include in development builds only (use preprocessor directives)
- ✅ Register useful debug commands for QA testing
- ✅ Clear logs periodically to avoid memory issues (long sessions)
- ✅ Use `#if DEVELOPMENT_BUILD` to wrap debug-only code
- ❌ Don't ship in production builds (security/cheating risk)
- ❌ Don't log sensitive data (passwords, tokens)

**Setup**:
```csharp
#if DEVELOPMENT_BUILD || UNITY_EDITOR
using IngameDebugConsole;

public class DebugCommands
{
    [ConsoleMethod("analytics_status", "Show analytics queue status")]
    public static void ShowAnalyticsStatus()
    {
        var manager = ServiceLocator.Get<IAnalyticsManager>();
        Debug.Log($"Pending events: {manager.GetPendingEventCount()}");
    }
}
#endif
```

---

### 5. UniTask

**Version**: 2.5.10  
**Location**: `Assets/Plugins/UniTask/`  
**Type**: Async/Await Library  
**License**: MIT  
**Documentation**: [UniTask GitHub](https://github.com/Cysharp/UniTask)

**Purpose**:
- Efficient async/await integration for Unity
- Zero-allocation async operations
- Replaces standard Task with Unity-optimized UniTask
- Seamless integration with Unity's PlayerLoop

**Key Features**:
- **Zero Allocation**: Struct-based UniTask (no GC pressure)
- **PlayerLoop Integration**: Awaitable Unity operations (WaitForSeconds, WaitForEndOfFrame, etc.)
- **Cancellation**: Full CancellationToken support
- **LINQ-style Operators**: Async LINQ for sequences
- **AsyncReactiveProperty**: Reactive value changes
- **Tracker Window**: Debug active tasks in editor

**Integration Points**:
- Nakama RPC calls (replace Task with UniTask)
- Scene loading with Addressables
- Animation sequencing
- Network operations
- Resource loading

**Usage Examples**:
```csharp
using Cysharp.Threading.Tasks;

// Simple async method (zero allocation)
public async UniTask LoadSceneAsync(string sceneName)
{
    await Addressables.LoadSceneAsync(sceneName).ToUniTask();
}

// Unity-specific waits (no allocations)
async UniTask DelayedAction()
{
    await UniTask.Delay(1000); // Wait 1 second
    await UniTask.WaitForEndOfFrame(); // Wait for end of frame
    await UniTask.NextFrame(); // Wait next frame
}

// Timeout handling
async UniTask FetchWithTimeout()
{
    var cts = new CancellationTokenSource();
    cts.CancelAfterSlim(TimeSpan.FromSeconds(5)); // 5 second timeout
    
    try
    {
        var result = await FetchDataAsync(cts.Token);
        return result;
    }
    catch (OperationCanceledException)
    {
        Debug.LogWarning("Request timed out");
    }
}

// Parallel operations
async UniTask LoadMultipleAssets()
{
    var (sprite1, sprite2, audio) = await UniTask.WhenAll(
        LoadSpriteAsync("icon1"),
        LoadSpriteAsync("icon2"),
        LoadAudioAsync("bgm")
    );
}

// Replace Nakama Task with UniTask
async UniTask SendAnalyticsAsync()
{
    var session = await client.AuthenticateDeviceAsync(deviceId);
    var payload = JsonUtility.ToJson(analyticsData);
    var result = await client.RpcAsync(session, "AnalyticsCollectEvents", payload);
    return result;
}

// Forget pattern (fire-and-forget with error handling)
LoadDataAsync().Forget(ex => Debug.LogError($"Load failed: {ex}"));

// AsyncReactiveProperty (observable values)
var health = new AsyncReactiveProperty<int>(100);
health.Subscribe(value => Debug.Log($"Health: {value}"));
health.Value = 80; // Triggers subscription

// MonoBehaviour lifecycle integration
async UniTask Start()
{
    // Use this.GetCancellationTokenOnDestroy() for cleanup
    await LongRunningTask(this.GetCancellationTokenOnDestroy());
}
```

**UniTask vs Task Comparison**:
```csharp
// ❌ Standard Task (allocates on heap)
async Task<int> StandardAsync()
{
    await Task.Delay(1000);
    return 42;
}

// ✅ UniTask (zero allocation)
async UniTask<int> OptimizedAsync()
{
    await UniTask.Delay(1000);
    return 42;
}

// ❌ Unity coroutine (string allocations, less flexible)
IEnumerator CoroutineWait()
{
    yield return new WaitForSeconds(1f); // Allocates
}

// ✅ UniTask (no allocation, cancellable, composable)
async UniTask UniTaskWait()
{
    await UniTask.Delay(TimeSpan.FromSeconds(1));
}
```

**Best Practices**:
- ✅ Use UniTask instead of Task for Unity operations (zero GC)
- ✅ Always pass CancellationToken from `this.GetCancellationTokenOnDestroy()` for long operations
- ✅ Use `.Forget()` for fire-and-forget tasks with error logging
- ✅ Use `UniTask.WhenAll()` for parallel operations instead of manual Task.WhenAll
- ✅ Use `UniTask.Delay()` instead of `Task.Delay()` (no timer allocation)
- ✅ Use UniTask Tracker window (Window → UniTask Tracker) to debug leaked tasks
- ❌ Don't use `async void` - use `async UniTaskVoid` or `.Forget()` instead
- ❌ Don't forget cancellation tokens on long-running tasks (memory leaks)
- ❌ Don't mix Task and UniTask unnecessarily (can convert with `.AsTask()` / `.AsUniTask()`)

**Performance Notes**:
- **Zero allocation**: UniTask<T> is a struct, no heap allocations
- **Faster than Task**: 2-10x faster than standard Task for Unity operations
- **No thread overhead**: Runs on Unity's PlayerLoop, not thread pool
- **Coroutine replacement**: 50-100x faster than StartCoroutine for simple delays

**Nakama Integration Pattern**:
```csharp
// Convert Nakama Task<T> to UniTask<T>
public class NakamaService
{
    private IClient _client;
    
    public async UniTask<ISession> AuthenticateAsync(string deviceId)
    {
        // Convert Nakama's Task to UniTask
        return await _client.AuthenticateDeviceAsync(deviceId).AsUniTask();
    }
    
    public async UniTask<IApiRpc> CallRpcAsync(ISession session, string rpcId, string payload)
    {
        // Timeout + cancellation token
        var cts = new CancellationTokenSource();
        cts.CancelAfterSlim(TimeSpan.FromSeconds(10));
        
        try
        {
            return await _client.RpcAsync(session, rpcId, payload).AsUniTask(cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.LogError($"RPC {rpcId} timed out");
            throw;
        }
    }
}
```

**Common Patterns**:
```csharp
// Retry logic
async UniTask<T> RetryAsync<T>(Func<UniTask<T>> operation, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            if (i == maxRetries - 1) throw;
            await UniTask.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // Exponential backoff
        }
    }
    throw new InvalidOperationException("Should not reach here");
}

// Throttle/Debounce
async UniTask DebounceAsync(float delaySeconds, CancellationToken ct)
{
    await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);
}

// Loading screen with progress
async UniTask LoadWithProgressAsync(IProgress<float> progress)
{
    for (float i = 0; i < 1f; i += 0.1f)
    {
        await UniTask.Delay(100);
        progress.Report(i);
    }
}
```

**Debugging**:
- Open **Window → UniTask Tracker** to see all active tasks
- Shows task ID, status, creation time, stacktrace
- Helps identify leaked tasks (tasks not completed/cancelled)

**Migration from Task**:
```csharp
// Before (Task-based)
public async Task<string> FetchDataAsync()
{
    await Task.Delay(1000);
    return "data";
}

// After (UniTask-based)
public async UniTask<string> FetchDataAsync()
{
    await UniTask.Delay(1000);
    return "data";
}

// If you need Task for external APIs
public Task<string> FetchDataAsTask()
{
    return FetchDataAsync().AsTask();
}
```

---

## Core Systems

### NetworkManager

**Version**: 1.0  
**Location**: `Assets/_Project/Core/Scripts/Network/`  
**Type**: Core System (not a plugin)  
**Specification**: [Network_Manager_Spec.md](./Network_Manager_Spec.md)

**Purpose**:
- Environment-aware Nakama server connection management
- Support for Local, Development, Staging, and Production environments
- Runtime environment switching with persistent configuration
- Compiler flag integration for build-time environment selection

**Key Features**:
- **Multi-Environment Support**: Seamlessly switch between local dev server, shared dev, staging, and production
- **Compiler Flags**: Use `NETWORK_LOCAL`, `NETWORK_DEV`, `NETWORK_STAGING`, `NETWORK_PRODUCTION` for build-time configuration
- **Runtime Switching**: Change environments via debug console without rebuilding
- **Persistent Settings**: Environment overrides saved in PlayerPrefs across sessions
- **Debug Console Integration**: In-game commands for environment management (`network_status`, `network_switch`)
- **Automatic Defaults**: Local in Editor, Production in builds (unless overridden)

**Integration Points**:
- Service Locator: `ServiceLocator.Get<INetworkManager>()`
- Used by Nakama client initialization
- AnalyticsManager and RemoteConfigManager use NetworkManager for server URLs
- Debug console commands for QA testing

**Usage Example**:
```csharp
// Get network configuration
var networkManager = ServiceLocator.Get<INetworkManager>();
string serverUrl = networkManager.FullServerUrl;  // "http://localhost:7350" or "https://api.example.com:443"
string httpKey = networkManager.HttpKey;
NetworkEnvironment env = networkManager.CurrentEnvironment;

// Initialize Nakama client with NetworkManager config
var client = new Nakama.Client(
    scheme: networkManager.UseSSL ? "https" : "http",
    host: networkManager.ServerUrl,
    port: networkManager.ServerPort,
    serverKey: networkManager.HttpKey
);

// Listen for environment changes
networkManager.OnEnvironmentChanged += (newEnv) => {
    Debug.Log($"Environment changed to {newEnv}. Restart required.");
};

// Runtime environment switching (requires restart)
networkManager.SetEnvironment(NetworkEnvironment.Staging);
networkManager.RequestReload();

// Clear override to revert to compiler flag default
if (networkManager.HasEnvironmentOverride) {
    networkManager.ClearEnvironmentOverride();
    networkManager.RequestReload();
}
```

**Debug Console Commands**:
```bash
# Show current environment and configuration
network_status

# Switch to different environment (requires restart)
network_switch development
network_switch staging
network_switch production
network_switch local

# Reload application to apply changes
network_reload

# Clear environment override (revert to compiler flag)
network_clear_override
```

**Best Practices**:
- ✅ Use NetworkManager for all server URL/key access (single source of truth)
- ✅ Set compiler flags in Build Settings → Scripting Define Symbols
- ✅ Test environment switching before deploying to production
- ✅ Use debug console for QA testing on different environments
- ✅ Clear overrides before production builds to ensure defaults work
- ❌ Don't hardcode server URLs or keys in code
- ❌ Don't forget to restart after environment changes (config doesn't hot-reload)
- ❌ Don't ship development credentials in production builds

**Compiler Flag Setup**:
```
// Edit → Project Settings → Player → Scripting Define Symbols

// Development build with staging server
NETWORK_STAGING;DEVELOPMENT_BUILD

// Production build (no flag = auto-detects Production in builds)
(empty or NETWORK_PRODUCTION)

// Local development (default in editor)
NETWORK_LOCAL
```

**Files**:
- `NetworkManager.cs` (217 lines) - Main manager implementation
- `INetworkManager.cs` (177 lines) - Interface definition
- `NetworkConfig.cs` (103 lines) - ScriptableObject config
- `NetworkEndpointConfig.cs` (113 lines) - Endpoint configuration data
- `NetworkEnvironment.cs` (33 lines) - Environment enum
- `NetworkDebugCommands.cs` (202 lines) - Debug console commands
- **Total**: 845 lines of production code
- **Tests**: `NetworkManagerTests.cs` (580 lines, 28 test cases)

**Common Patterns**:
```csharp
// Pattern 1: Initialize Nakama with NetworkManager
public class NakamaService
{
    private IClient _client;
    
    public void Initialize()
    {
        var netManager = ServiceLocator.Get<INetworkManager>();
        _client = new Client(
            scheme: netManager.UseSSL ? "https" : "http",
            host: netManager.ServerUrl,
            port: netManager.ServerPort,
            serverKey: netManager.HttpKey
        );
    }
}

// Pattern 2: Environment-specific behavior
public class AnalyticsManager
{
    public void RecordEvent(string eventName)
    {
        var netManager = ServiceLocator.Get<INetworkManager>();
        
        // Disable analytics in local environment
        if (netManager.CurrentEnvironment == NetworkEnvironment.Local)
        {
            Debug.Log($"[Local] Skipping analytics: {eventName}");
            return;
        }
        
        // Send to server for other environments
        SendToServer(eventName);
    }
}

// Pattern 3: Environment change notification
public class GameManager : MonoBehaviour
{
    private void Start()
    {
        var netManager = ServiceLocator.Get<INetworkManager>();
        netManager.OnEnvironmentChanged += OnNetworkEnvironmentChanged;
    }
    
    private void OnNetworkEnvironmentChanged(NetworkEnvironment newEnv)
    {
        // Save game state before restart
        SaveGameState();
        Debug.Log($"Preparing to restart with {newEnv} environment");
    }
}
```

---

## Unity Packages (Package Manager)

### Core Packages

#### 1. Input System (v1.18.0)
**Package**: `com.unity.inputsystem`  
**Purpose**: Modern input handling for all platforms

**Usage in OCTP**:
- Player movement input (WASD, arrow keys, gamepad)
- UI navigation and selection
- Multi-platform input bindings

**Key Classes**:
- `InputAction` - Define input actions
- `InputActionAsset` - Store input bindings
- `PlayerInput` component - Automatic setup

**Best Practices**:
- Use Input Actions instead of `Input.GetKey()`
- Create separate Action Maps for Gameplay, UI, Menu states
- Enable Auto-Switch action maps based on state

---

#### 2. Addressables (v2.8.1)
**Package**: `com.unity.addressables`  
**Purpose**: Asynchronous asset loading and memory management

**Usage in OCTP**:
- Scene loading (background preload)
- Character sprite atlas loading
- Audio clip streaming
- Remote asset bundles (future DLC)

**Key Operations**:
```csharp
// Load asset asynchronously
var handle = Addressables.LoadAssetAsync<Sprite>("character_atlas");
await handle.Task;

// Load scene additively
await Addressables.LoadSceneAsync("SafeZone_Town", LoadSceneMode.Additive);

// Release when done
Addressables.Release(handle);
```

**Best Practices**:
- Always release handles when done (memory leaks)
- Use labels for batch loading ("ui_icons", "zone_1_assets")
- Preload critical assets during loading screens

---

#### 3. Localization (v1.5.9)
**Package**: `com.unity.localization`  
**Purpose**: Multi-language support with string tables and asset variants

**Usage in OCTP** (Future):
- UI text translation
- Character dialogue
- Tutorial text
- Language-specific audio

**Setup Requirements**:
- Create String Tables for each language
- Use `LocalizedString` in UI components
- Configure locale selector in settings menu

---

#### 4. URP (Universal Render Pipeline v17.3.0)
**Package**: `com.unity.render-pipelines.universal`  
**Purpose**: Modern rendering pipeline for 2D/3D graphics

**Usage in OCTP**:
- 2D Lights (combat effects, ambient lighting)
- Post-processing (bloom, color grading)
- Sprite shaders with custom effects
- Performance optimization for mobile

**Features Used**:
- 2D Renderer with lighting
- Post-processing stack
- Custom 2D sprite shaders

---

#### 5. 2D Animation (v13.0.4)
**Package**: `com.unity.2d.animation`  
**Purpose**: Sprite rigging and skeletal animation

**Usage in OCTP** (Future):
- Character idle/walk animations
- Snake body segment interpolation
- Boss enemy animations

---

#### 6. Timeline (v1.8.10)
**Package**: `com.unity.timeline`  
**Purpose**: Cutscene and cinematic sequencing

**Usage in OCTP** (Future):
- Boss introduction cutscenes
- Story events
- Tutorial sequences

---

## Plugin Integration Architecture

### Service Locator Integration

All plugin managers are registered with the service locator pattern:

```csharp
// In GameManager.Awake()
ServiceLocator.Register<INakamaClient>(nakamaClient);
ServiceLocator.Register<IAnalyticsManager>(analyticsManager);
ServiceLocator.Register<IRemoteConfigManager>(remoteConfigManager);

// Usage from any system
var analytics = ServiceLocator.Get<IAnalyticsManager>();
analytics.RecordEvent("level_start", new { level = 5 });
```

### Assembly Definition Strategy

```
OCTP.Plugins.Nakama.asmdef
  └─ References: Nakama.asmdef

OCTP.Services.asmdef
  └─ References: OCTP.Plugins.Nakama, PrimeTween

OCTP.Gameplay.asmdef
  └─ References: OCTP.Services, TransitionsPlus
```

**Benefits**:
- Faster compilation (only recompile changed assemblies)
- Clear dependency graph
- Easier to remove/replace plugins

---

## Version Update Strategy

### When to Update

✅ **Update When**:
- Security patches released
- Critical bug fixes
- New features needed for upcoming work
- Unity version upgrade requires it

❌ **Don't Update When**:
- Nearing release milestone (stability > new features)
- No specific need (if it works, don't break it)
- Breaking changes without migration plan

### Update Process

1. **Backup Project** (Git commit or branch)
2. **Read Changelog** - Check for breaking changes
3. **Update in Test Branch** - Never update directly in main
4. **Run All Tests** - Verify no regressions
5. **Manual Testing** - Test critical paths
6. **Update Documentation** - Record version and changes
7. **Merge to Main** - After verification

---

## Best Practices Summary

### General Plugin Usage

1. **Centralize Plugin Initialization**
   - Initialize in `GameManager.Awake()` or dedicated initializer
   - Register with Service Locator for global access

2. **Handle Plugin Errors Gracefully**
   - Wrap plugin calls in try-catch for non-critical features
   - Provide fallbacks when plugins fail (offline mode, default config)

3. **Document Plugin-Specific Patterns**
   - Create code snippets for common operations
   - Share knowledge via code reviews

4. **Monitor Plugin Performance**
   - Use Unity Profiler to track plugin overhead
   - Optimize hot paths (animation updates, network calls)

5. **Avoid Plugin Lock-In**
   - Use interface abstractions for critical systems
   - Make plugins swappable (e.g., IAnimationSystem interface)

### Development Workflow

- **Development Builds**: Include IngameDebugConsole
- **Production Builds**: Strip debug console, use secure Nakama keys
- **Testing**: Test plugin integrations on target platforms early

---

## Troubleshooting

### Common Issues

**Nakama Connection Fails**:
- Check server is running: `docker ps | grep nakama`
- Verify HTTP key matches client and server
- Check firewall/network settings
- Review Nakama server logs: `docker logs nakama`

**PrimeTween Not Animating**:
- Ensure object is active in hierarchy
- Check if another tween is already running on same property
- Verify duration > 0
- Check if Time.timeScale = 0 and not using unscaledTime

**TransitionsPlus Black Screen**:
- Verify cameras are properly configured
- Check if transition is stuck (call `TransitionAnimator.Stop()`)
- Ensure transition duration is reasonable (0.5-2 seconds)

**IngameDebugConsole Not Showing**:
- Check if prefab is in scene
- Verify Development Build flag is enabled
- Try toggling visibility (usually tilde key `~` or tap corner)

---

## Future Plugin Considerations

### Potential Additions

1. **DOTween Pro** (if PrimeTween insufficient)
   - More features but higher cost
   - Consider only if specific features needed

2. **Odin Inspector** (Editor enhancement)
   - Better inspector UI for designers
   - Evaluate cost vs productivity gain

3. **TextMesh Pro** (Already in Unity 2020+)
   - Already included, start using for UI text

4. **Cinemachine** (Camera management)
   - If complex camera behaviors needed
   - Evaluate for boss fights/cutscenes

---

## References

- **Nakama Docs**: https://heroiclabs.com/docs
- **PrimeTween GitHub**: https://github.com/KyryloKuzyk/PrimeTween
- **TransitionsPlus Support**: https://kronnect.com/support
- **IngameDebugConsole GitHub**: https://github.com/yasirkula/UnityIngameDebugConsole
- **UniTask GitHub**: https://github.com/Cysharp/UniTask
- **Unity Input System**: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- **Unity Addressables**: https://docs.unity3d.com/Packages/com.unity.addressables@latest

---

## Changelog

- **v1.2** (2026-02-09): Added Core Systems section with NetworkManager documentation (environment switching, compiler flags, debug console)
- **v1.1** (2026-02-09): Added UniTask v2.5.10 documentation with comprehensive usage patterns, Nakama integration, and migration guide
- **v1.0** (2026-02-09): Initial documentation of all plugins and packages
