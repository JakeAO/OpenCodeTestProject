# Architecture Overview

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.1
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [gdd-core, movement-system, combat-system, party-system, progression-system]

## Overview

OCTP uses a hybrid scene architecture with a persistent main scene containing core services and asynchronously loaded content/UI scenes for zones and menus. This design provides persistent party state, responsive scene transitions, and clear separation of concerns while maintaining 60 FPS performance targets.

## Goals

- **Persistent Party State**: Party composition and stats accessible across all scenes
- **Responsive Transitions**: Async loading for smooth zone transitions (<3s)
- **Clear Separation**: Core systems independent of content/UI scenes
- **Testable Architecture**: Dependency injection and service locator pattern for unit testing
- **Scalable Systems**: Modular assembly definitions for parallel development
- **Future-Proof**: Cloud save integration, mobile/console support planned

## Dependencies

- **Game Design Documents** - All GDD docs for context on mechanics
- **Unity 6000.3.7f1** - Target engine version
- **New Input System** - Already in project, wrapped by InputManager
- **URP 17.3.0** - Rendering pipeline constraints

## Constraints

- **Target FPS**: 60 FPS sustained with 10+ party members + 15+ enemies
- **Scene Load Time**: < 3 seconds per zone transition
- **Memory**: < 512MB (targeting future mobile support)
- **Draw Calls**: < 500 with URP (batch-friendly)
- **Single Save Slot**: One active playthrough per player
- **Cloud Sync**: Must not cause frame drops (async/background thread)

## Architecture Overview

### Scene Hierarchy

```
┌─────────────────────────────────────────────────────────────────┐
│ MAIN SCENE (Persistent) - Never unloaded                        │
│                                                                  │
│ ┌────────────────────────────────────────────────────────────┐  │
│ │ CORE MANAGERS (Singletons)                                │  │
│ ├────────────────────────────────────────────────────────────┤  │
│ │ • GameManager        - Lifecycle, initialization, service  │  │
│ │ • StateManager       - Game state machine, transitions      │  │
│ │ • SaveManager        - Local save/load, persists party      │  │
│ │ • InputManager       - New Input System wrapper, actions    │  │
│ │ • SceneLoader        - Async loading orchestrator           │  │
│ │ • PartyManager       - Active party, stats, equipment       │  │
│ │ • AudioManager       - Music, SFX pools (persistent)        │  │
│ │ • ConfigManager      - Design-time constants, balancing     │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│ ┌────────────────────────────────────────────────────────────┐  │
│ │ DATA CONTAINERS                                            │  │
│ ├────────────────────────────────────────────────────────────┤  │
│ │ • Party (members, inventory, equipment)                    │  │
│ │ • PartyEvents (member downed, level up, etc.)              │  │
│ │ • SaveData (serialized game state)                         │  │
│ │ • WorldState (defeated enemies, unlocks, etc.)             │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
    ↓ Async Load                        ↓ Async Load
┌─────────────────────────┐      ┌─────────────────────────┐
│ CONTENT SCENES          │      │ UI SCENES               │
│ (Loaded/Unloaded)       │      │ (Loaded/Unloaded)       │
├─────────────────────────┤      ├─────────────────────────┤
│ • Zone_SafeZone         │      │ • ExplorationHUD        │
│   ├─ Shop              │      │ • SafeZoneUI            │
│   ├─ Inn               │      │ • DialogUI              │
│   ├─ Blacksmith        │      │ • LoadingScreen         │
│   └─ Portals           │      │ • PauseMenu             │
│                         │      │                         │
│ • Zone_Grasslands      │      │                         │
│ • Zone_Forest          │      │                         │
│ • Zone_Mountain        │      │                         │
│ • Zone_Boss            │      │                         │
│                         │      │                         │
│ (Spawners, Enemies,    │      │ (Canvases, Buttons,    │
│  Obstacles, Interactables)    │  Text, Panels)         │
└─────────────────────────┘      └─────────────────────────┘
```

### System Layers

```
┌──────────────────────────────────────────────────────┐
│ PRESENTATION LAYER                                   │
│ ├─ UI Controllers (HUD, Menus, Dialogs)             │
│ ├─ Scene UI (overlays per content scene)            │
│ └─ Input Visualization (button prompts, crosshairs)│
├──────────────────────────────────────────────────────┤
│ GAME LOGIC LAYER                                     │
│ ├─ Combat System (damage, targeting, cooldowns)     │
│ ├─ Movement System (pathfinding, collision)         │
│ ├─ Party System (characters, stats, equipment)      │
│ ├─ Progression System (leveling, unlocks)           │
│ └─ Enemy AI (behavior, pathfinding, abilities)      │
├──────────────────────────────────────────────────────┤
│ CORE SYSTEMS LAYER                                   │
│ ├─ State Management (game state machine)            │
│ ├─ Scene Management (async loading)                 │
│ ├─ Input Management (action mapping, blocking)      │
│ ├─ Save/Load System (persistence, cloud sync)       │
│ ├─ Configuration (design-time constants)            │
│ └─ Events & Messaging (inter-system communication)  │
├──────────────────────────────────────────────────────┤
│ ENGINE LAYER                                         │
│ ├─ Physics (collision detection, raycasts)          │
│ ├─ Rendering (URP, particle effects, animations)    │
│ ├─ Audio (AudioSource, AudioMixer, spatial audio)   │
│ └─ Input System (InputManager, InputDevice)         │
└──────────────────────────────────────────────────────┘
```

## Core Systems

### 1. State Management System

Tracks game state (Exploration, SafeZone, Dialog, Paused, Loading, GameOver) and manages valid transitions.

**Key Classes:**
- `GameState` enum: Exploration, SafeZone, Dialog, Paused, Loading, Transiting, GameOver
- `GameStateManager` singleton: Manages state transitions, emits events
- `StateChangeEvent`: Observable for systems to react to state changes
- `GameStateMachine`: Validates transitions, prevents invalid state changes

**Interaction:**
- InputManager listens to StateManager for input context changes
- SceneLoader listens to StateManager for scene transition triggers
- UI systems respond to state changes (hide exploration HUD in safe zone, etc.)

### 2. Scene Management System

Orchestrates async loading/unloading of content and UI scenes.

**Key Classes:**
- `SceneLoader` singleton: Handles LoadScene/UnloadScene operations
- `SceneContext`: Carries data between scenes (zone config, spawn points, etc.)
- `LoadingScreen`: UI feedback during transitions
- `AsyncSceneHandle`: Tracks load progress, completion callbacks

**Lifecycle:**
1. Unload previous content scene (async)
2. Unload previous UI scene (async)
3. Load new content scene (async)
4. Load new UI scene (async)
5. Initialize scene (setup references, spawn players, etc.)

### 3. Save System

Persists game state to local storage with cloud backup.

**Key Classes:**
- `SaveData`: Binary-serialized container (player progress, party, world state)
- `SaveManager`: Local save/load operations, manages autosave
- `CloudSyncManager`: Queues and uploads to cloud in background thread
- `SaveValidator`: Checksum validation for corruption detection

**Save Flow:**
```
Safe Zone Entry
   ↓
SaveManager.Save() → Binary file on disk
   ↓
CloudSyncManager.Queue() → Add to upload queue (async)
   ↓
[Background Network Thread]
Upload to cloud + Checksum validation
   ↓
Callback: Update local timestamp on success
```

### 4. Game Manager

Core orchestrator managing initialization, lifecycle, and service coordination.

**Key Classes:**
- `GameManager` singleton: Bootstrap, service locator, lifecycle hooks
- `ServiceLocator`: Static access to core managers (GameStateManager, SaveManager, etc.)
- `IGameService` interface: Contract for all core services

**Responsibilities:**
- Initialization sequence (load config, instantiate managers, subscribe to events)
- Play/Pause/Resume/Quit lifecycle
- Cross-system communication (mediator pattern)

### 5. Input Manager

Unified input abstraction for PC/Mobile/Controller support.

**Key Classes:**
- `InputManager` singleton: Wraps New Input System, emits game actions
- `IInputManager` interface: Contract for input service (register this, not InputManager)
- `GameAction` enum: Move, Ability1-9, Interact, Pause, UISubmit, UICancel, etc.
- `InputContext` enum: Exploration, SafeZone, Dialog, Paused, Loading
- `ActionBinding`: Maps hardware input to game action with modifiers

**Service Locator Registration:**
```csharp
// Register interface, not concrete type
ServiceLocator.Register<IInputManager>(inputManager);
// Retrieve by interface
var input = ServiceLocator.Get<IInputManager>();
```

**Context-Sensitive Blocking:**
```csharp
// Exploration context: All actions enabled
inputManager.CanTriggerAction(GameAction.Ability1) // true
inputManager.CanTriggerAction(GameAction.UISubmit) // false (not in safe zone)

// SafeZone context: UI actions only
inputManager.CanTriggerAction(GameAction.UISubmit) // true
inputManager.CanTriggerAction(GameAction.Ability1) // false (in safe zone)

// Loading context: All actions blocked
inputManager.CanTriggerAction(GameAction.Any) // false
```

### 6. Party Manager

Manages active party members, stats, equipment, and inventory.

**Key Classes:**
- `PartyManager` singleton: Active party management
- `IPartyManager` interface: Contract for party service (register this, not PartyManager)
- `Party`: Collection of active characters
- `Character`: Individual character with stats, abilities, equipment
- `CharacterStats`: Stat calculation (primary + derived stats)
- `Equipment`: Weapon, Armor, Accessory slots
- `Inventory`: Materials, consumables, resources

**Service Locator Registration:**
```csharp
// Register interface, not concrete type
ServiceLocator.Register<IPartyManager>(partyManager);
// Retrieve by interface
var party = ServiceLocator.Get<IPartyManager>();
```

**Data Persistence:**
Party state lives in main scene. SaveManager periodically serializes Party to SaveData.

### 7. Configuration System

Centralized design-time constants managed via ScriptableObjects.

**Key Classes:**
- `ConfigManager` singleton: Configuration data access
- `IConfigManager` interface: Contract for config service (register this, not ConfigManager)
- Design-time ScriptableObject assets (BaseXPTable, EnemyBudgetConfig, etc.)

**Service Locator Registration:**
```csharp
// Register interface, not concrete type
ServiceLocator.Register<IConfigManager>(configManager);
// Retrieve by interface
var config = ServiceLocator.Get<IConfigManager>();
ConfigManager.Instance.GetXPForLevel(characterLevel)
```

**ScriptableObject Assets:**
- `BaseXPTable.asset`: Leveling curve (XP per level)
- `EnemyBudgetConfig.asset`: Difficulty scaling parameters
- `AbilityRegistry.asset`: All ability definitions
- `ZoneConfigs.asset`: Zone-specific settings (enemies, obstacles, loot)
- `CharacterConfigs.asset`: Base stats per class, stat growth curves
- `BalanceConfig.asset`: Combat modifiers, status effect durations, etc.

### 8. Configuration System

Centralized design-time constants managed via ScriptableObjects.

**ScriptableObject Assets:**
- `BaseXPTable.asset`: Leveling curve (XP per level)
- `EnemyBudgetConfig.asset`: Difficulty scaling parameters
- `AbilityRegistry.asset`: All ability definitions
- `ZoneConfigs.asset`: Zone-specific settings (enemies, obstacles, loot)
- `CharacterConfigs.asset`: Base stats per class, stat growth curves
- `BalanceConfig.asset`: Combat modifiers, status effect durations, etc.

## Service Interfaces (For Service Locator)

All core services must implement their respective interfaces. The service locator ONLY registers and retrieves interfaces, never concrete types.

```csharp
// State Management Service
public interface IGameStateManager
{
    GameState CurrentState { get; }
    GameState PreviousState { get; }
    event Action<GameState, GameState> OnStateChanged;
    event Action OnEnteringExploration;
    event Action OnLeavingExploration;
    event Action OnEnteringSafeZone;
    event Action OnLeavingSafeZone;
    event Action OnEnteringDialog;
    event Action OnLeavingDialog;
    event Action OnPaused;
    event Action OnResumed;
    event Action OnLoadingStarted;
    event Action OnLoadingComplete;
    event Action OnGameOver;
    
    bool CanTransitionTo(GameState newState);
    bool TrySetState(GameState newState);
    void ForceSetState(GameState newState);
}

// Scene Management Service
public interface ISceneLoader
{
    event Action<string> OnContentLoaded;
    event Action<string> OnUILoaded;
    
    Task LoadZoneAsync(string zoneName, SceneContext context = null);
    Task LoadUIForStateAsync(GameState state);
}

// Save System Service
public interface ISaveManager
{
    event Action<SaveData> OnGameSaved;
    event Action<SaveData> OnGameLoaded;
    
    void Save();
    bool TryLoad(out SaveData saveData);
}

// Cloud Sync Service
public interface ICloudSyncManager
{
    event Action<bool> OnCloudSyncComplete;
    
    void QueueUpload(string filePath);
}

// Input System Service
public interface IInputManager
{
    event Action<GameAction> OnActionTriggered;
    event Action<Vector2> OnMoveInput;
    event Action<GameAction> OnAbilityQueued;
    
    bool CanTriggerAction(GameAction action);
    void RemapAction(GameAction action, InputBinding newBinding);
}

// Party Management Service
public interface IPartyManager
{
    Party GetParty();
    void InitializeNewParty();
    void LoadPartyFromSave(SaveData saveData);
}

// Configuration Service
public interface IConfigManager
{
    int GetXPForLevel(int level);
    (int min, int max) GetStatGrowth(int classID, StatType stat);
    Zone GetZoneConfig(string zoneID);
    Ability GetAbility(string abilityID);
}
```

### Registering Services by Interface

```csharp
// In GameManager.InitializeAllSystems()
var stateManager = gameObject.AddComponent<GameStateManager>();
ServiceLocator.Register<IGameStateManager>(stateManager);  // ✅ Register interface

var saveManager = gameObject.AddComponent<SaveManager>();
ServiceLocator.Register<ISaveManager>(saveManager);  // ✅ Register interface

// NEVER do this:
// ServiceLocator.Register<GameStateManager>(stateManager);  // ❌ Wrong!
```

### Accessing Services by Interface

```csharp
// In any system that needs a service
var stateManager = ServiceLocator.Get<IGameStateManager>();  // ✅ Get interface
stateManager.TrySetState(GameState.SafeZone);

var saveManager = ServiceLocator.Get<ISaveManager>();  // ✅ Get interface
saveManager.Save();

// NEVER do this:
// var stateManager = ServiceLocator.Get<GameStateManager>();  // ❌ Wrong!
```

### Why This Pattern?

**Testability**: Easy to mock interfaces in unit tests
```csharp
[Test]
public void TestSaveOnZoneEntry()
{
    var mockSaveManager = new MockSaveManager();
    ServiceLocator.Register<ISaveManager>(mockSaveManager);
    
    var stateManager = ServiceLocator.Get<IGameStateManager>();
    stateManager.TrySetState(GameState.SafeZone);
    
    Assert.IsTrue(mockSaveManager.SaveWasCalled);
}
```

**Loose Coupling**: Implementation can change without affecting dependent code
```csharp
// Can swap implementations at runtime
public class SqlSaveManager : ISaveManager { }
public class CloudSaveManager : ISaveManager { }

var saveManager = configUseCloud 
    ? new CloudSaveManager() 
    : new SqlSaveManager();
ServiceLocator.Register<ISaveManager>(saveManager);
```

**Dependency Injection Ready**: All dependencies go through interface contracts
```csharp
public class CombatSystem
{
    private ISaveManager _saveManager;
    private IGameStateManager _stateManager;
    
    public CombatSystem()
    {
        // Dependencies injected through interface, not concrete type
        _saveManager = ServiceLocator.Get<ISaveManager>();
        _stateManager = ServiceLocator.Get<IGameStateManager>();
    }
}
```


```
OCTP.Shared
├─ Enums: GameState, GameAction, CharacterClass, StatusEffect, etc.
├─ Data Contracts: SaveData, CharacterData, EquipmentData, etc.
├─ Events: GameStateChangeEvent, CharacterDownedEvent, etc.
└─ Interfaces: IGameService, ISavable, IAbility, etc.

OCTP.Core
├─ GameManager.cs
├─ StateManager.cs
├─ SaveManager.cs / CloudSyncManager.cs
├─ InputManager.cs
├─ SceneLoader.cs
├─ ConfigManager.cs
└─ References: [OCTP.Shared]

OCTP.Game
├─ Combat System
├─ Movement System
├─ Enemy AI
├─ Progression System
└─ References: [OCTP.Core, OCTP.Shared]

OCTP.Party
├─ Character.cs
├─ CharacterStats.cs
├─ Equipment.cs
├─ Inventory.cs
└─ References: [OCTP.Core, OCTP.Shared]

OCTP.Combat
├─ CombatSystem.cs
├─ AbilitySystem.cs
├─ DamageCalculator.cs
├─ TargetingSystem.cs
└─ References: [OCTP.Party, OCTP.Game, OCTP.Core, OCTP.Shared]

OCTP.Movement
├─ SnakeMovement.cs
├─ PathFollower.cs
├─ ObstacleAvoidance.cs
├─ CollisionHandler.cs
└─ References: [OCTP.Game, OCTP.Core, OCTP.Shared]

OCTP.UI
├─ ExplorationHUD.cs
├─ SafeZoneUI.cs
├─ DialogUI.cs
├─ MenuController.cs
└─ References: [OCTP.Game, OCTP.Party, OCTP.Core, OCTP.Shared]

OCTP.Editor
├─ Tools, Editors, Helpers
└─ References: [All]

OCTP.Tests
├─ Unit Tests
├─ Integration Tests
└─ References: [All]
```

## Communication Patterns

### Event-Driven (One-Way Broadcast)

Used for state changes and notifications:

```csharp
// StateManager emits when state changes
public event Action<GameState, GameState> OnStateChanged;

// Combat system listens for state changes
StateManager.OnStateChanged += (oldState, newState) => {
    if (newState == GameState.Paused) {
        combatSystem.PauseAllCombat();
    }
};
```

### Service Locator (Direct Access)

Used for core service access:

```csharp
// Any system can access core services
var saveManager = ServiceLocator.Get<ISaveManager>();
var partyManager = ServiceLocator.Get<IPartyManager>();
```

### Command Pattern (Queued Actions)

Used for input handling:

```csharp
// InputManager queues ability activation
InputManager.OnAbility1Pressed += () => {
    var command = new ActivateAbilityCommand(character, ability);
    commandQueue.Enqueue(command);
};
```

## Data Flow

### During Exploration

```
Player Input
    ↓
InputManager (context-sensitive)
    ↓
Command Dispatch (if valid for state)
    ↓
Movement/Combat Systems
    ↓
Update Party State
    ↓
Save to SaveData (periodic or on milestone)
```

### Safe Zone Entry

```
Player Enters SafeZone
    ↓
StateManager.SetState(SafeZone)
    ↓
SceneLoader Async Load SafeZoneUI + SafeZone Content
    ↓
InputManager.SetContext(SafeZone) → UI actions only
    ↓
SaveManager.Save() → Binary to disk (immediate)
    ↓
CloudSyncManager.Queue() → Upload in background
```

### Scene Transition

```
Player Exits SafeZone
    ↓
StateManager.SetState(Loading)
    ↓
InputManager.BlockAllInput()
    ↓
LoadingScreen Shows Progress
    ↓
SceneLoader Async:
  1. Unload SafeZoneUI
  2. Unload SafeZone Content
  3. Load Zone Content
  4. Load ExplorationHUD
    ↓
StateManager.SetState(Exploration)
    ↓
InputManager.SetContext(Exploration) → Movement + Abilities
```

## Initialization Sequence

```
1. Unity Awake() → GameManager.Initialize()
   ↓
2. Create Singletons in Order:
   - ServiceLocator
   - StateManager
   - SaveManager + CloudSyncManager
   - InputManager
   - SceneLoader
   - PartyManager
   - ConfigManager
   - AudioManager
   ↓
3. Load Persistent Managers:
   - Hook up event listeners
   - Subscribe to state changes
   ↓
4. Load MainScene Setup:
   - Instantiate persistent objects (PartyVisuals, etc.)
   ↓
5. Load SaveData or Start New Game:
   - If continue: Load party from last save
   - If new game: Initialize fresh party
   ↓
6. Set Initial State → Exploration (or MainMenu if first launch)
   ↓
7. Load First Content/UI Scenes Async
```

## Success Criteria

- [x] Party state persists across scene transitions
- [x] Scene load time < 3 seconds per transition
- [x] 60 FPS maintained with 10+ party + 15+ enemies
- [x] Save file < 5MB (even with multiple playthroughs planned)
- [x] Cloud sync < 100ms latency (non-blocking)
- [x] All systems testable with dependency injection
- [x] Clear separation of concerns (no circular dependencies)
- [x] State machine prevents invalid transitions

## Testing Strategy

### Unit Tests

- **StateManager**: Validate state transitions, blocked transitions
- **InputManager**: Verify context-sensitive blocking
- **SaveManager**: Save/load round-trip, checksum validation
- **ConfigManager**: Load ScriptableObjects, verify values

### Integration Tests

- **Scene Transitions**: Load/unload cycle, timing
- **Save + Load**: Full save/load/resume cycle
- **Party Updates**: Stat calculations, equipment changes persist

### Performance Tests

- **Frame Rate**: 60 FPS sustained under load
- **Memory**: Party + 10 enemies < 200MB
- **Load Times**: Scene transitions < 3 seconds

## Open Questions

- Which cloud provider for cloud saves? ✅ RESOLVED - Using Nakama backend
- Should difficulty be seed-based for reproducible runs? ✅ RESOLVED - Yes, seed-based for speedrunning/A/B testing
- Should architecture support co-op multiplayer (v2)? ✅ RESOLVED - MVP is single-player only; v2 can add co-op
- Analytics integration for balance telemetry? ✅ RESOLVED - Analytics system designed and implemented

## Scope Confirmation

**MVP Scope**: Single-player roguelike with network features (analytics, remote config) only.
**Co-op Design**: v2 feature; architecture is flexible enough to add local/online co-op later without major refactoring.

## Changelog

- v1.1 (2026-02-09): Confirmed single-player scope for MVP, noted co-op architecture flexibility for v2
- v1.0 (2026-02-08): Initial architecture overview

