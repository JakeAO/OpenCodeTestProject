# Testing Strategy

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.0
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [architecture-overview, all system specs]

## Overview

Comprehensive testing strategy covering unit tests, integration tests, performance tests, and gameplay tests to ensure code quality and prevent regressions.

## Test Framework

- **Unit Tests**: NUnit (already in Unity)
- **Integration Tests**: Unity Test Framework (UTF)
- **Performance Tests**: Unity Performance Testing Extension
- **Location**: OCTP/Assets/Tests/ (organized per system)

## Test Organization

```
OCTP/Assets/Tests/
├── Editor/
│   ├── Systems/
│   │   ├── StateManagementTests.cs
│   │   ├── SaveSystemTests.cs
│   │   ├── InputManagerTests.cs
│   │   ├── PartySystemTests.cs
│   │   └── CombatSystemTests.cs
│   ├── Data/
│   │   ├── CharacterStatsTests.cs
│   │   ├── EquipmentTests.cs
│   │   └── SerializationTests.cs
│   └── Integration/
│       ├── GameFlowTests.cs
│       ├── SaveLoadCycleTests.cs
│       └── SceneTransitionTests.cs
└── Gameplay/
    ├── Movement/
    │   ├── SnakeMovementTests.cs
    │   └── CollisionTests.cs
    └── Combat/
        ├── DamageCalculationTests.cs
        └── AbilityTests.cs
```

## Unit Tests by System

### State Management Tests

```csharp
[TestFixture]
public class StateManagementTests
{
    private GameStateManager _stateManager;
    
    [SetUp]
    public void Setup()
    {
        _stateManager = new GameObject().AddComponent<GameStateManager>();
    }
    
    [Test]
    public void TestValidTransition_ExplorationToSafeZone()
    {
        _stateManager.ForceSetState(GameState.Exploration);
        bool result = _stateManager.TrySetState(GameState.SafeZone);
        
        Assert.IsTrue(result);
        Assert.AreEqual(GameState.SafeZone, _stateManager.CurrentState);
    }
    
    [Test]
    public void TestBlockedTransition_SafeZoneToSafeZone()
    {
        _stateManager.ForceSetState(GameState.SafeZone);
        bool result = _stateManager.TrySetState(GameState.SafeZone);
        
        Assert.IsFalse(result);
        Assert.AreEqual(GameState.SafeZone, _stateManager.CurrentState);
    }
    
    [Test]
    public void TestStateChangeEvent_Emitted()
    {
        bool eventFired = false;
        _stateManager.OnStateChanged += (old, new) => eventFired = true;
        
        _stateManager.TrySetState(GameState.SafeZone);
        
        Assert.IsTrue(eventFired);
    }
}
```

### Save System Tests

```csharp
[TestFixture]
public class SaveSystemTests
{
    private SaveManager _saveManager;
    private string _testSavePath;
    
    [SetUp]
    public void Setup()
    {
        _saveManager = new GameObject().AddComponent<SaveManager>();
        _testSavePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "test_save.bin");
    }
    
    [TearDown]
    public void Cleanup()
    {
        if (System.IO.File.Exists(_testSavePath))
            System.IO.File.Delete(_testSavePath);
    }
    
    [Test]
    public void TestSaveFile_Created()
    {
        _saveManager.Save();
        Assert.IsTrue(System.IO.File.Exists(_testSavePath));
    }
    
    [Test]
    public void TestSaveLoad_RoundTrip()
    {
        _saveManager.Save();
        bool loaded = _saveManager.TryLoad(out var saveData);
        
        Assert.IsTrue(loaded);
        Assert.IsNotNull(saveData.Party);
    }
    
    [Test]
    public void TestChecksum_Validation()
    {
        _saveManager.Save();
        
        // Corrupt file
        var data = System.IO.File.ReadAllBytes(_testSavePath);
        data[100] ^= 0xFF;
        System.IO.File.WriteAllBytes(_testSavePath, data);
        
        bool loaded = _saveManager.TryLoad(out _);
        Assert.IsFalse(loaded, "Corrupted save should fail validation");
    }
}
```

### Party & Character Tests

```csharp
[TestFixture]
public class PartySystemTests
{
    private Party _party;
    
    [SetUp]
    public void Setup()
    {
        _party = new Party();
    }
    
    [Test]
    public void TestAddMember_Success()
    {
        var character = CreateTestCharacter("Hero", CharacterClass.Warrior);
        bool result = _party.TryAddMember(character);
        
        Assert.IsTrue(result);
        Assert.AreEqual(1, _party.Members.Count);
    }
    
    [Test]
    public void TestAddMember_ExceedsMax()
    {
        for (int i = 0; i < Party.MaxPartySize + 1; i++)
        {
            var character = CreateTestCharacter($"Hero{i}", CharacterClass.Warrior);
            _party.TryAddMember(character);
        }
        
        Assert.AreEqual(Party.MaxPartySize, _party.Members.Count);
    }
    
    [Test]
    public void TestCharacterStats_Calculated()
    {
        var character = CreateTestCharacter("Hero", CharacterClass.Warrior);
        character.BaseStats.Strength = 10;
        
        float damage = character.GetStat(StatType.Damage);
        Assert.Greater(damage, 0);
    }
    
    [Test]
    public void TestEquipment_Modifiers()
    {
        var character = CreateTestCharacter("Hero", CharacterClass.Warrior);
        var weapon = new Weapon 
        { 
            Damage = 10,
            Modifiers = new StatModifier { Strength = 5 }
        };
        
        character.Equipment.EquipWeapon(weapon);
        float newDamage = character.GetStat(StatType.Damage);
        
        Assert.Greater(newDamage, 0);
    }
    
    [Test]
    public void TestDownedState_ReducesStats()
    {
        var character = CreateTestCharacter("Hero", CharacterClass.Warrior);
        float originalDamage = character.GetStat(StatType.Damage);
        
        character.TakeDamage(character.MaxHP);
        float downedDamage = character.GetStat(StatType.Damage);
        
        Assert.Less(downedDamage, originalDamage);
    }
    
    private Character CreateTestCharacter(string name, CharacterClass @class)
    {
        return new Character 
        { 
            Name = name, 
            Class = @class,
            BaseStats = new CharacterStats()
        };
    }
}
```

### Input Manager Tests

```csharp
[TestFixture]
public class InputManagerTests
{
    [Test]
    public void TestInputContext_ExplorationAllowsAbilities()
    {
        bool allowed = InputContextMap.CanTriggerAction(
            GameState.Exploration, GameAction.Ability1);
        Assert.IsTrue(allowed);
    }
    
    [Test]
    public void TestInputContext_SafeZoneBlocksAbilities()
    {
        bool allowed = InputContextMap.CanTriggerAction(
            GameState.SafeZone, GameAction.Ability1);
        Assert.IsFalse(allowed);
    }
    
    [Test]
    public void TestInputContext_LoadingBlocksAll()
    {
        bool allowed = InputContextMap.CanTriggerAction(
            GameState.Loading, GameAction.Move);
        Assert.IsFalse(allowed);
    }
}
```

## Integration Tests

### Game Flow Tests

```csharp
[TestFixture]
public class GameFlowTests
{
    [UnityTest]
    public IEnumerator TestNewGame_Initialization()
    {
        GameManager.Instance.NewGame();
        yield return null;
        
        var party = ServiceLocator.Get<PartyManager>().GetParty();
        Assert.Greater(party.Members.Count, 0);
        Assert.AreEqual(GameState.Exploration, 
            ServiceLocator.Get<GameStateManager>().CurrentState);
    }
    
    [UnityTest]
    public IEnumerator TestExplorationToSafeZone_Transition()
    {
        GameStateManager.Instance.TrySetState(GameState.Exploration);
        yield return null;
        
        SceneLoader sceneLoader = ServiceLocator.Get<SceneLoader>();
        var task = sceneLoader.LoadZoneAsync("Zone_SafeZone");
        
        yield return new WaitUntil(() => task.IsCompleted);
        
        Assert.AreEqual(GameState.SafeZone, 
            GameStateManager.Instance.CurrentState);
    }
    
    [UnityTest]
    public IEnumerator TestPauseResume()
    {
        GameManager.Instance.PauseGame();
        yield return null;
        
        Assert.AreEqual(GameState.Paused,
            GameStateManager.Instance.CurrentState);
        Assert.AreEqual(0f, Time.timeScale);
        
        GameManager.Instance.ResumeGame();
        yield return null;
        
        Assert.AreEqual(1f, Time.timeScale);
    }
}
```

### Save/Load Tests

```csharp
[TestFixture]
public class SaveLoadCycleTests
{
    [UnityTest]
    public IEnumerator TestSaveLoad_Persistence()
    {
        // Setup: Create party and modify state
        var partyManager = ServiceLocator.Get<PartyManager>();
        var party = partyManager.GetParty();
        var character = party.Members[0];
        int originalLevel = character.Level;
        
        character.GainExperience(1000);
        yield return null;
        
        int newLevel = character.Level;
        Assert.Greater(newLevel, originalLevel);
        
        // Save
        SaveManager.Instance.Save();
        yield return new WaitForSeconds(0.2f);
        
        // Modify further
        character.GainExperience(5000);
        yield return null;
        
        // Load - should revert to saved state
        SaveManager.Instance.TryLoad(out var saveData);
        partyManager.LoadPartyFromSave(saveData);
        yield return null;
        
        Assert.AreEqual(newLevel, 
            partyManager.GetParty().Members[0].Level);
    }
}
```

## Performance Tests

```csharp
[Performance]
public class PerformanceTests
{
    [Performance]
    public void Benchmark_StateTransition()
    {
        var stateManager = new GameObject()
            .AddComponent<GameStateManager>();
        
        Measure.Frames()
            .Warmup(10)
            .Run(() =>
            {
                stateManager.TrySetState(GameState.SafeZone);
                stateManager.TrySetState(GameState.Exploration);
            });
    }
    
    [Performance]
    public void Benchmark_CharacterStatsCalculation()
    {
        var character = CreateTestCharacter();
        
        Measure.Frames()
            .Warmup(10)
            .Run(() =>
            {
                var stats = character.GetEffectiveStats();
            });
    }
    
    [Performance]
    public void Benchmark_InputProcessing()
    {
        var inputManager = new GameObject()
            .AddComponent<InputManager>();
        
        Measure.Frames()
            .Warmup(10)
            .Run(() =>
            {
                inputManager.ProcessInput();
            });
    }
}
```

## Test Coverage Goals

| System | Target Coverage |
|--------|-----------------|
| State Management | 100% |
| Save System | 90% |
| Party/Character | 85% |
| Input Manager | 90% |
| Combat System | 80% |
| Movement System | 75% |

## Continuous Integration

Run on every commit:
- All unit tests
- Static code analysis (linting)
- Memory leak detection

Run nightly:
- Full integration tests
- Performance benchmarks
- Platform-specific tests (PC, Mobile, Console)

## Success Criteria

- [x] > 80% code coverage for core systems
- [x] All tests pass before release
- [x] No memory leaks detected
- [x] Performance benchmarks meet targets
- [x] Zero critical bugs at release

## Changelog

- v1.0 (2026-02-08): Initial testing strategy

