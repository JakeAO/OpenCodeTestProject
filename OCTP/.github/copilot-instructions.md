# Copilot Instructions - OCTP Unity Project

## Project Overview

Unity 2D game project using Universal Render Pipeline (URP), Unity 6000.3.7f1. 2D project with Input System, animation, sprite tools, and test framework.

## Build & Test

### Unity Editor Operations
- **Build**: File > Build Settings or Ctrl/Cmd+Shift+B
- **Tests**: Window > General > Test Runner (PlayMode and EditMode tests)
- **Build targets**: Set via File > Build Settings

### Command Line (CI/CD)
```bash
# Build (requires custom build script in Assets/Editor)
/Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode \
  -projectPath /Users/jaoxscp/Documents/OpenCodeTestProject/OCTP \
  -executeMethod BuildScript.Build

# Run tests
/Applications/Unity/Unity.app/Contents/MacOS/Unity -runTests -testPlatform PlayMode \
  -projectPath /Users/jaoxscp/Documents/OpenCodeTestProject/OCTP
```

**Note**: No custom build scripts currently exist - builds/tests must be run from Unity Editor.

## Architecture

### Game Type
Top-down real-time game with Snake-like movement controls and RPG party-building elements.

### Assets Folder Structure
Project uses **hybrid organization** (feature-based with asset-type subfolders):

- **_Project/** - Main game features (Player, Party, Enemies, Combat, UI, World, Systems, Core)
  - Each feature has Scripts/, Prefabs/, and asset-specific subfolders
  - **Core/** contains shared utilities referenced by all features
- **Art/** - Shared sprites, materials, shaders, animations
- **Audio/** - Music, SFX, and audio mixers
- **Scenes/** - Game scenes (Main/, Testing/, UI/)
- **Settings/** - Input actions, URP config, audio settings
- **Editor/** - Editor-only scripts and tools
- **Tests/** - EditMode/ and PlayMode/ test assemblies
- **ThirdParty/** - External assets (never modify directly)
- **Plugins/** - Third-party code plugins
- **Sandbox/** - Prototyping area (not for production)

See `Assets/STRUCTURE.md` for detailed documentation.

### Assembly Definitions
Project uses 9 assembly definitions for faster compilation:
- **OCTP.Core** - Shared utilities (no dependencies)
- **OCTP.Player, OCTP.Party, OCTP.Enemies, OCTP.Combat, OCTP.UI** - Feature assemblies (depend on Core)
- **OCTP.Editor** - Editor tools (editor-only)
- **OCTP.Tests.EditMode, OCTP.Tests.PlayMode** - Test assemblies

Changes to one feature don't recompile unrelated features.

### Key Directories
- **ProjectSettings/**: Unity configuration (version-controlled)
- **Packages/**: Unity Package Manager dependencies (see manifest.json)
- **Library/**: Cached build data (gitignored, never edit)
- **UserSettings/**: User-specific preferences (gitignored)

### Core Unity Packages
- **2D Tools**: Animation, Sprite Shape, Tilemap, Aseprite importer
- **Input System**: New Input System (1.18.0) - use this, not legacy Input Manager
- **URP**: Universal Render Pipeline (17.3.0) for 2D rendering
- **Addressables**: Asynchronous asset loading (2.8.1)
- **Localization**: Multi-language support (1.5.9)
- **Test Framework**: Unity Test Framework for play/edit mode tests

### Third-Party Plugins
All plugins are in `Assets/Plugins/` - see `Documentation/02-TechnicalDesign/Plugins_And_Packages_Overview.md` for full details.

#### Nakama Unity SDK (v3.21.0)
Game server client for analytics, remote config, and A/B testing.
```csharp
// Already initialized in GameManager
var analytics = ServiceLocator.Get<IAnalyticsManager>();
analytics.RecordEvent("level_complete", new { level = 5 });

var config = ServiceLocator.Get<IRemoteConfigManager>();
await config.FetchConfig();
```
**Key Points**:
- Server URL: `http://localhost:7350` (dev), change for production
- Always authenticate before RPC calls
- Use async/await pattern
- Don't call RPCs in Update() (performance)

#### PrimeTween (v1.3.7)
High-performance, zero-allocation animation library.
```csharp
// Simple tween (one line, zero GC)
Tween.Position(transform, endValue: Vector3.right * 5, duration: 1f);

// UI fade with callback
Tween.Alpha(canvasGroup, 0f, 0.5f)
    .OnComplete(() => gameObject.SetActive(false));

// Sequence
Tween.Sequence()
    .Append(Tween.Scale(logo, 1.2f, 0.3f))
    .Append(Tween.Rotation(logo, new Vector3(0, 0, 360), 0.5f));
```
**Key Points**:
- Zero GC allocations (use for performance-critical animations)
- Store Tween reference to control/stop: `Tween tween = Tween.Position(...);`
- Use `useUnscaledTime: true` for UI (unaffected by pause)
- Don't tween physics-controlled properties

#### TransitionsPlus (v5.1.1)
Full-screen transition effects for scene changes.
```csharp
// Simple fade transition
TransitionAnimator.Start(TransitionProfile.Fade, duration: 1f);

// With callback
TransitionAnimator.Start(
    effect: TransitionProfile.Effect.Wipe,
    duration: 1.5f,
    onComplete: () => SceneManager.LoadScene("GameplayScene")
);
```
**Key Points**:
- Use for scene transitions, death screens, zone changes
- Don't start multiple transitions simultaneously
- Simpler effects (Fade, Wipe) better for mobile performance

#### IngameDebugConsole (v1.8.4)
Runtime debug console for device testing.
```csharp
#if DEVELOPMENT_BUILD || UNITY_EDITOR
using IngameDebugConsole;

[ConsoleMethod("give_gold", "Adds gold to player")]
public static void GiveGold(int amount)
{
    PlayerInventory.Gold += amount;
}
#endif
```
**Key Points**:
- Include in development builds only
- Register useful debug commands for QA
- Don't ship in production (security risk)

#### UniTask (v2.5.10)
Efficient async/await library for Unity (zero allocation).
```csharp
using Cysharp.Threading.Tasks;

// Replace Task with UniTask (zero GC)
public async UniTask<string> LoadDataAsync()
{
    await UniTask.Delay(1000); // No allocation
    return "data";
}

// Unity-specific awaits
await UniTask.WaitForEndOfFrame();
await UniTask.NextFrame();

// Convert Nakama Task to UniTask
var result = await client.RpcAsync(session, rpcId, payload).AsUniTask();

// Fire-and-forget with error handling
LoadSceneAsync().Forget(ex => Debug.LogError(ex));
```
**Key Points**:
- Use instead of Task for Unity operations (zero GC, 2-10x faster)
- Always pass CancellationToken: `this.GetCancellationTokenOnDestroy()`
- Use `.Forget()` for fire-and-forget tasks
- Convert Nakama/external Task with `.AsUniTask()`
- Use UniTask Tracker window to debug leaked tasks

### Solution File
Use `OCTP.slnx` (configured in `.vscode/settings.json` as dotnet.defaultSolution).

## Code Conventions

### Naming
- **Classes/Structs**: `PascalCase`
- **Methods**: `PascalCase` (e.g., `Start()`, `Update()`)
- **Private fields**: `_camelCase` with underscore prefix
- **Public fields/Properties**: `PascalCase`
- **Constants**: `ALL_CAPS`
- **Folders**: `PascalCase`, no spaces
- **Prefabs**: Descriptive PascalCase (e.g., `PlayerSnake`, `EnemyGoblin`)
- **Scenes**: PascalCase (e.g., `MainMenu`, `Level01`)
- **Assemblies**: `OCTP.FeatureName` format

### Formatting
- **Indentation**: 2 spaces (per .editorconfig)
- **Line length**: Keep under 120 characters
- **End of line**: LF

### Imports
Group Unity imports first, then .NET, then custom namespaces:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

// Custom namespaces
```

### Unity Patterns

#### MonoBehaviour Lifecycle
```csharp
// Use [SerializeField] for Inspector-editable private fields
[SerializeField] private float _speed = 5f;

private Rigidbody2D _rb;

void Awake()
{
  // Initialize and cache component references here
  _rb = GetComponent<Rigidbody2D>();
}

void Start()
{
  // Initialize after all Awake() calls complete
}

void Update()
{
  // Frame-based logic (input, camera, etc.)
}

void FixedUpdate()
{
  // Physics-based logic (use with Rigidbody2D)
}
```

#### Component References
- **Cache in Awake/Start**: Never use `Find()` or `FindObjectOfType()` in `Update()`
- **Use `transform`**: Not `gameObject.transform` (performance)
- **GetComponent<T>()**: Prefer generic over `GetComponent(typeof(T))`
- **RequireComponent**: Use `[RequireComponent(typeof(Rigidbody2D))]` for dependencies

#### Input System
Use new Input System (1.18.0), not legacy `Input.GetKey()`:
```csharp
using UnityEngine.InputSystem;

// Reference Settings/Input/InputSystem_Actions.inputactions
```

#### Debugging
```csharp
Debug.Log("Info message");
Debug.LogWarning("Warning message");
Debug.LogError("Error message");
```

#### Null Checks
Validate references in `OnValidate()` or `Awake()`:
```csharp
void OnValidate()
{
  if (_target == null)
    Debug.LogWarning("Target not assigned!", this);
}
```

### ScriptableObjects
Use for data containers shared across scenes (game settings, enemy stats, etc.):
```csharp
[CreateAssetMenu(fileName = "NewData", menuName = "Game/Data")]
public class GameData : ScriptableObject { }
```

## File Handling

### Project Organization
- **New features**: Create in `_Project/FeatureName/` with Scripts/, Prefabs/, etc. subfolders
- **Shared assets**: Use Art/, Audio/ for cross-feature resources
- **Prototyping**: Use Sandbox/ for experiments; move to proper features when ready
- **Tests**: Place in Tests/EditMode/ or Tests/PlayMode/
- **Editor tools**: Place in Editor/Scripts/

### Version Control (Git)
- **Commit**: Assets/, ProjectSettings/, Packages/manifest.json
- **Ignore**: Library/, Logs/, Temp/, UserSettings/, obj/, .vs/
- **Unity files**: .asset, .prefab, .unity, .meta files are YAML (version controlled)

### Assembly Definitions
- Each feature's Scripts/ folder contains a .asmdef file
- Reference OCTP.Core from other assemblies for shared utilities
- Add explicit assembly references via Inspector for inter-feature dependencies

### VS Code Exclusions
The following are hidden in VS Code (see .vscode/settings.json) but still exist:
- ProjectSettings/, UserSettings/
- .meta, .asset, .prefab, .unity files
- Binary assets (images, audio, 3D models)
- Build/, Library/, Logs/, obj/, temp/

## Common Pitfalls

### Performance
- ❌ `Find()` in Update() - cache references instead
- ❌ `gameObject.transform` - use `transform` directly
- ❌ String operations in Update() - cache or avoid
- ✅ Object pooling for frequently instantiated objects

### 2D Physics
- Use `Rigidbody2D` and `Collider2D`, not 3D equivalents
- Apply forces in `FixedUpdate()`, not `Update()`
- Set appropriate collision layers in ProjectSettings

### Input System
- Use new Input System (not `Input.GetKey()`)
- Input actions located in `Settings/Input/InputSystem_Actions.inputactions`

### Serialization
- Unity can't serialize dictionaries directly - use arrays of key-value pairs
- Private fields need `[SerializeField]` to appear in Inspector
- Public fields are auto-serialized (avoid if not needed in Inspector)

## Testing

### Test Runner
- **Window > General > Test Runner**
- **PlayMode**: Tests that run in actual game environment
- **EditMode**: Tests that run in editor without game running

### Writing Tests
Place tests in appropriate folder:
- **EditMode tests**: `Tests/EditMode/` - run without play mode
- **PlayMode tests**: `Tests/PlayMode/` - run in game environment

```csharp
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace OCTP.Tests
{
  public class GameTests
  {
    [Test]
    public void EditModeTest() { }

    [UnityTest]
    public IEnumerator PlayModeTest()
    {
      // Use yield return null; to wait for frames
      yield return null;
    }
  }
}
```
