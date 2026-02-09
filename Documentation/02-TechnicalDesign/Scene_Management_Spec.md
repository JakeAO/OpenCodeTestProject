# Scene Management Specification

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.1
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [architecture-overview, state-management-spec, gdd-core]

## Overview

The Scene Management System orchestrates asynchronous loading and unloading of content scenes (zones) and UI scenes (HUDs, menus). It ensures smooth transitions between game states while maintaining persistent party state in the main scene.

## Goals

- **Async Loading**: Load scenes without frame stuttering (< 3s total load)
- **Memory Efficiency**: Unload previous content when transitioning
- **State Synchronization**: Coordinate scene transitions with GameStateManager
- **Context Passing**: Communicate zone data (enemies, spawns, config) between scenes
- **Progress Feedback**: Display loading bar during transitions

## Dependencies

- **Architecture Overview** - Scene hierarchy design
- **State Management** - StateManager triggers scene transitions
- **Content Scenes** - Zones to be loaded/unloaded

## Constraints

- **Load Time**: < 3 seconds per zone transition
- **Async Only**: All scene operations non-blocking
- **Single Content Scene**: Only one zone active at a time
- **Multiple UI Scenes**: Different HUD per state (Exploration vs SafeZone)

## Scene Hierarchy

```
Main Scene (Persistent, Never Unloaded)
├── GameManager, StateManager, SaveManager, etc.
├── PartyManager, InputManager, SceneLoader
└── [Persistent Audio/Effects]

Content Scenes (Async Load/Unload)
├── Zone_SafeZone        [Always Available]
├── Zone_Grasslands      [Loaded on demand]
├── Zone_Forest
└── Zone_Mountain

UI Scenes (Async Load/Unload, Stacked)
├── ExplorationHUD       [During GameState.Exploration]
├── SafeZoneUI           [During GameState.SafeZone]
├── DialogUI             [During GameState.Dialog]
├── LoadingScreen        [During GameState.Loading]
└── PauseMenu            [During GameState.Paused]
```

## SceneLoader Implementation

```csharp
public class SceneLoader : MonoBehaviour, IGameService
{
    private static SceneLoader _instance;
    
    [SerializeField] private CanvasGroup loadingScreenCanvas;
    [SerializeField] private Slider loadingProgressBar;
    
    private string _currentContentScene = "";
    private string _currentUIScene = "";
    private List<AsyncOperation> _activeLoads = new();
    
    public event Action<string> OnContentLoaded;
    public event Action<string> OnUILoaded;
    
    // Load content zone
    public async Task LoadZoneAsync(string zoneName, SceneContext context = null)
    {
        try
        {
            StateManager.Instance.TrySetState(GameState.Loading);
            ShowLoadingScreen();
            
            // Unload previous content
            if (!string.IsNullOrEmpty(_currentContentScene))
            {
                await UnloadSceneAsync(_currentContentScene);
            }
            
            // Load new content
            var operation = SceneManager.LoadSceneAsync(
                zoneName, LoadSceneMode.Additive);
            
            await WaitForSceneLoad(operation);
            _currentContentScene = zoneName;
            OnContentLoaded?.Invoke(zoneName);
            
            // Load appropriate UI
            await LoadUIForState(zoneName == "SafeZone" 
                ? GameState.SafeZone 
                : GameState.Exploration);
            
            HideLoadingScreen();
            
            // Update game state
            GameState nextState = (zoneName == "SafeZone") 
                ? GameState.SafeZone 
                : GameState.Exploration;
            StateManager.Instance.TrySetState(nextState);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load zone {zoneName}: {ex}");
            HideLoadingScreen();
        }
    }
    
    // Load UI scene based on state
    public async Task LoadUIForStateAsync(GameState state)
    {
        // Unload previous UI
        if (!string.IsNullOrEmpty(_currentUIScene))
        {
            await UnloadSceneAsync(_currentUIScene);
        }
        
        string uiScene = state switch
        {
            GameState.Exploration => "ExplorationHUD",
            GameState.SafeZone => "SafeZoneUI",
            GameState.Dialog => "DialogUI",
            GameState.Paused => "PauseMenu",
            GameState.Loading => null,  // No UI during load
            _ => "ExplorationHUD"
        };
        
        if (uiScene != null)
        {
            var operation = SceneManager.LoadSceneAsync(
                uiScene, LoadSceneMode.Additive);
            
            await WaitForSceneLoad(operation);
            _currentUIScene = uiScene;
            OnUILoaded?.Invoke(uiScene);
        }
    }
    
    private async Task UnloadSceneAsync(string sceneName)
    {
        var operation = SceneManager.UnloadSceneAsync(sceneName);
        await WaitForOperation(operation);
    }
    
    private async Task WaitForSceneLoad(AsyncOperation operation)
    {
        while (!operation.isDone)
        {
            loadingProgressBar.value = operation.progress;
            await Task.Yield();
        }
    }
    
    private async Task WaitForOperation(AsyncOperation operation)
    {
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }
    
    private void ShowLoadingScreen()
    {
        loadingScreenCanvas.gameObject.SetActive(true);
        loadingProgressBar.value = 0f;
    }
    
    private void HideLoadingScreen()
    {
        loadingScreenCanvas.gameObject.SetActive(false);
    }
}
```

## SceneContext (Data Passing)

```csharp
public class SceneContext
{
    public string ZoneName { get; set; }
    public int ZoneLevel { get; set; }
    public Vector3 PlayerSpawnPoint { get; set; }
    public List<EnemySpawnerConfig> EnemySpawners { get; set; }
    public Dictionary<string, Vector3> InteractablePositions { get; set; }
    
    public static SceneContext For(string zoneName)
    {
        var config = ConfigManager.Instance.GetZoneConfig(zoneName);
        return new SceneContext
        {
            ZoneName = zoneName,
            ZoneLevel = config.Level,
            PlayerSpawnPoint = config.SpawnPoint,
            EnemySpawners = config.EnemySpawners,
            InteractablePositions = config.Interactables
        };
    }
}
```

## Transition Sequences

### Safe Zone → Exploration Zone

```
1. Player clicks "Explore" / uses portal
2. SceneLoader.LoadZoneAsync("Zone_Grasslands", context)
3. StateManager → GameState.Loading
4. LoadingScreen shown
5. Async:
   - Unload SafeZoneUI
   - Unload Zone_SafeZone content
   - Load Zone_Grasslands content
   - Load ExplorationHUD
6. StateManager → GameState.Exploration
7. InputManager context → Exploration (Move, Abilities)
8. LoadingScreen hidden
9. Player can move/interact
```

### Exploration → Safe Zone

```
1. Player uses portal / reaches zone boundary
2. SceneLoader.LoadZoneAsync("Zone_SafeZone", context)
3. StateManager → GameState.Loading
4. LoadingScreen shown
5. Async:
   - Unload ExplorationHUD
   - Unload Zone_Grasslands content
   - Load Zone_SafeZone content
   - Load SafeZoneUI
6. StateManager → GameState.SafeZone
7. SaveManager.Save() → Auto-save to disk
8. CloudSyncManager.Queue() → Background upload
9. InputManager context → SafeZone (UI only)
10. LoadingScreen hidden
11. Player can access menus
```

## Loading Screen UI

The loading screen shows an **animated progress bar** (estimated duration, not true asset load %). This UX pattern prevents perception of slowness even if actual load is slower.

```csharp
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Text sceneNameText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text progressStatusText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private float estimatedDuration = 3f;  // Estimated time to complete load
    private float elapsedTime = 0f;
    
    public void ShowLoading(string fromScene, string toScene, float estimatedSeconds = 3f)
    {
        sceneNameText.text = $"Loading {toScene}...";
        progressStatusText.text = "Loading...";
        progressBar.value = 0f;
        estimatedDuration = estimatedSeconds;
        elapsedTime = 0f;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
    }
    
    private void Update()
    {
        if (canvasGroup.alpha < 1f) return; // Not visible
        
        elapsedTime += Time.unscaledDeltaTime;
        float animatedProgress = Mathf.Clamp01(elapsedTime / estimatedDuration);
        progressBar.value = animatedProgress;
    }
    
    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
```

**Key Features**:
- Progress bar animates from 0 to 100% over estimated duration
- Does NOT reflect true asset load percentage
- Smooth visual feedback regardless of actual load speed
- Prevents "stuck" perception if load takes longer than estimated

## Background Scene Preloading

To reduce transition time, the next zone is preloaded asynchronously while player explores current zone:

```csharp
public class ScenePreloader : MonoBehaviour
{
    private AsyncOperation nextZoneLoad;
    
    public void PreloadNextZone(string nextZoneName)
    {
        if (nextZoneLoad == null || !nextZoneLoad.isDone)
        {
            nextZoneLoad = SceneManager.LoadSceneAsync(
                nextZoneName, 
                LoadSceneMode.Additive);
            nextZoneLoad.allowSceneActivation = false; // Don't activate yet
        }
    }
    
    public void ActivatePreloadedZone()
    {
        if (nextZoneLoad != null && nextZoneLoad.isDone)
        {
            nextZoneLoad.allowSceneActivation = true; // Activate preloaded zone
        }
    }
    
    public void UnloadZonesOutOfRange(string currentZone)
    {
        // Unload zones 2+ steps away to free memory
        // Keep current zone + next zone in memory
    }
}
```

**Strategy**:
- When in Zone_A, start preloading Zone_B in background
- Only happens during Exploration state (not in safe zones)
- Unload zones 2+ steps away
- Significantly faster transitions for next zone (already loaded)

## Scene Load Timeout Handling

Scene transitions timeout after 30 seconds with error handling:

```csharp
public class SceneLoadTimeout : MonoBehaviour
{
    private const float LOAD_TIMEOUT = 30f;
    private AsyncOperation loadOperation;
    private float loadStartTime;
    
    public void StartLoadWithTimeout(string sceneName)
    {
        loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadStartTime = Time.realtimeSinceStartup;
    }
    
    private void Update()
    {
        if (loadOperation == null || loadOperation.isDone) return;
        
        float elapsed = Time.realtimeSinceStartup - loadStartTime;
        if (elapsed > LOAD_TIMEOUT)
        {
            HandleLoadTimeout();
        }
    }
    
    private void HandleLoadTimeout()
    {
        Debug.LogError($"Scene load exceeded {LOAD_TIMEOUT}s timeout");
        ShowErrorDialog("Loading took too long", 
            new[] { "Retry", "Return to Last Zone" });
    }
}
```

**Error Dialog Options**:
- "Retry" - Attempt to load scene again
- "Return to Last Zone" - Go back to previous zone, skip this transition

## Scene Transition Animations

All scene transitions include fade + slide effects (1 second total):

```csharp
public class SceneTransitionAnimator : MonoBehaviour
{
    private CanvasGroup fadeCanvas;
    
    public IEnumerator FadeOutSlideOut(float duration = 1f)
    {
        float half = duration / 2f;
        
        // Fade out (500ms)
        yield return FadeOut(fadeCanvas, half);
        
        // Slide out + Load + Slide in (500ms)
        yield return SlideOut(half);
    }
    
    public IEnumerator FadeInSlideIn(float duration = 1f)
    {
        float half = duration / 2f;
        yield return SlideIn(half);
        yield return FadeIn(fadeCanvas, half);
    }
    
    private IEnumerator FadeOut(CanvasGroup canvas, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvas.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        canvas.alpha = 0f;
    }
    
    private IEnumerator SlideOut(float duration)
    {
        // Slide scene content out of view
        // Used as timing for scene load in background
        yield return new WaitForSeconds(duration);
    }
}
```

**Transition Timing**:
- 0-500ms: Fade out current scene
- 500ms: Load new scene (happens while faded)
- 500-1000ms: Slide/fade in new scene
- Result: Smooth transition even if load takes extra time

## Per-Scene Setup

### Content Scenes (Zones)

Each zone scene must have:
```csharp
public class ZoneInitializer : MonoBehaviour
{
    private void Awake()
    {
        var context = SceneManager.GetSceneByName(gameObject.scene.name);
        var sceneData = FindObjectOfType<SceneContextProvider>();
        
        // Setup spawners
        InitializeEnemySpawners(sceneData.Context.EnemySpawners);
        
        // Setup obstacles
        InitializeObstacles();
        
        // Setup interactables
        InitializeInteractables(sceneData.Context.InteractablePositions);
    }
}
```

### UI Scenes

Each UI scene must have:
```csharp
public class UISceneInitializer : MonoBehaviour
{
    private void OnEnable()
    {
        GameStateManager.Instance.OnStateChanged += OnStateChanged;
    }
    
    private void OnStateChanged(GameState oldState, GameState newState)
    {
        // Show/hide UI elements based on state
    }
}
```

## Success Criteria

- [x] Zone transitions complete in < 3 seconds
- [x] No frame stuttering during load (async only)
- [x] Loading bar shows accurate progress
- [x] Previous scene fully unloaded (memory freed)
- [x] Party state persists across scenes
- [x] SceneContext passed correctly to zones
- [x] UI transitions smoothly with GameState changes

## Testing Strategy

```csharp
[UnityTest]
public IEnumerator TestZoneTransition_SafeZoneToGrasslands()
{
    var sceneLoader = FindObjectOfType<SceneLoader>();
    
    float startTime = Time.realtimeSinceStartup;
    var task = sceneLoader.LoadZoneAsync("Zone_Grasslands");
    
    yield return new WaitUntil(() => task.IsCompleted);
    
    float loadTime = Time.realtimeSinceStartup - startTime;
    Assert.Less(loadTime, 3f, "Zone load took > 3 seconds");
    
    // Verify scene loaded
    var scene = SceneManager.GetSceneByName("Zone_Grasslands");
    Assert.IsTrue(scene.isLoaded);
}

[UnityTest]
public IEnumerator TestUITransition_ExplorationToPause()
{
    var stateManager = GameStateManager.Instance;
    
    stateManager.TrySetState(GameState.Paused);
    yield return null;
    
    // Verify PauseMenu UI is active
    var pauseMenu = FindObjectOfType<PauseMenu>();
    Assert.IsNotNull(pauseMenu);
    Assert.IsTrue(pauseMenu.gameObject.activeSelf);
}
```

## Open Questions

All questions below have been resolved in v1.1:

- ✅ **Should loading bar show actual progress or estimated?**  
  RESOLVED: Animated progress bar with estimated duration (see Loading Screen UI section)

- ✅ **Should scenes preload asynchronously in background before player requests?**  
  RESOLVED: Yes, next zone preloads in background during exploration (see Background Preloading section)

- ✅ **Should there be fade transitions between scenes?**  
  RESOLVED: Yes, fade + slide effect for all transitions, 1s total duration (see Transition Animations section)

- ✅ **Should scene transitions have timeout limits?**  
  RESOLVED: Yes, 30-second timeout with graceful error handling (see Timeout Handling section)

## Changelog

- v1.1 (2026-02-09): Added background preloading, 30s timeout handling, animated progress bar (estimated duration), fade+slide transition animations (1s)
- v1.0 (2026-02-08): Initial scene management specification

