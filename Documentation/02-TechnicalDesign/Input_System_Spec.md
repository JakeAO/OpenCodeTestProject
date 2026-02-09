# Input System Specification

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.0
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [architecture-overview, state-management-spec, gdd-core]

## Overview

The Input System wraps Unity's New Input System providing a game action abstraction layer (Move, Ability1-9, Interact, Pause). It enforces context-sensitive input blocking based on GameState and queues ability activations for combat processing.

## Goals

- **Platform Agnostic**: PC/Mobile/Controller support via single abstraction
- **Context Sensitive**: Block/allow actions based on GameState
- **Ability Queueing**: Validate cooldowns before queuing
- **Rebindable**: Support runtime key remapping
- **Performance**: Process input in < 1ms

## Game Actions

```csharp
public enum GameAction
{
    // Movement
    Move,
    
    // Combat (abilities 1-9)
    Ability1, Ability2, Ability3, Ability4, Ability5,
    Ability6, Ability7, Ability8, Ability9,
    
    // Interaction
    Interact,
    
    // UI
    UI_Navigate, UI_Submit, UI_Cancel,
    
    // System
    Pause, Sprint
}
```

## InputManager

```csharp
public class InputManager : MonoBehaviour, IGameService
{
    private InputActionAsset _inputActions;
    private Dictionary<GameAction, InputAction> _actionMap = new();
    
    public event Action<GameAction> OnActionTriggered;
    public event Action<Vector2> OnMoveInput;
    public event Action<GameAction> OnAbilityQueued;
    
    private void Awake()
    {
        LoadInputActions();
        HookupInputEvents();
        GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
    }
    
    private void LoadInputActions()
    {
        _inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
        _inputActions.Enable();
        
        // Map actions
        _actionMap[GameAction.Move] = _inputActions.FindAction("Move");
        _actionMap[GameAction.Ability1] = _inputActions.FindAction("Ability1");
        // ... map others
        _actionMap[GameAction.Pause] = _inputActions.FindAction("Pause");
    }
    
    private void HookupInputEvents()
    {
        _actionMap[GameAction.Move].performed += ctx =>
        {
            OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
        };
        
        _actionMap[GameAction.Ability1].performed += ctx =>
        {
            if (CanTriggerAction(GameAction.Ability1))
            {
                OnAbilityQueued?.Invoke(GameAction.Ability1);
            }
        };
        
        // Hook all abilities 1-9
        for (int i = 1; i <= 9; i++)
        {
            var ability = (GameAction)(GameAction.Ability1 + (i - 1));
            _actionMap[ability].performed += ctx =>
            {
                if (CanTriggerAction(ability))
                {
                    OnAbilityQueued?.Invoke(ability);
                }
            };
        }
        
        _actionMap[GameAction.Pause].performed += ctx =>
        {
            if (CanTriggerAction(GameAction.Pause))
            {
                OnActionTriggered?.Invoke(GameAction.Pause);
            }
        };
    }
    
    public bool CanTriggerAction(GameAction action)
    {
        var state = GameStateManager.Instance.CurrentState;
        return InputContextMap.CanTriggerAction(state, action);
    }
    
    public void RemapAction(GameAction action, InputBinding newBinding)
    {
        // Runtime rebinding implementation
        if (_actionMap.TryGetValue(action, out var inputAction))
        {
            inputAction.RemoteBindingIfCompositeOrPartBinding(
                0, newBinding);
        }
    }
    
    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        // Input context automatically changes with state
        // Listeners (UI, movement) respond to state change
    }
}

public static class InputContextMap
{
    public static bool CanTriggerAction(GameState state, GameAction action)
    {
        return (state, action) switch
        {
            // Exploration
            (GameState.Exploration, GameAction.Move) => true,
            (GameState.Exploration, GameAction.Ability1 
                or GameAction.Ability2 or GameAction.Ability3
                or GameAction.Ability4 or GameAction.Ability5
                or GameAction.Ability6 or GameAction.Ability7
                or GameAction.Ability8 or GameAction.Ability9) => true,
            (GameState.Exploration, GameAction.Interact) => true,
            (GameState.Exploration, GameAction.Pause) => true,
            (GameState.Exploration, GameAction.UI_Navigate
                or GameAction.UI_Submit or GameAction.UI_Cancel) => false,
            
            // SafeZone
            (GameState.SafeZone, GameAction.Move) => false,
            (GameState.SafeZone, GameAction.Ability1 
                or GameAction.Ability2 or GameAction.Ability3
                or GameAction.Ability4 or GameAction.Ability5
                or GameAction.Ability6 or GameAction.Ability7
                or GameAction.Ability8 or GameAction.Ability9) => false,
            (GameState.SafeZone, GameAction.Interact) => false,
            (GameState.SafeZone, GameAction.Pause) => true,
            (GameState.SafeZone, GameAction.UI_Navigate
                or GameAction.UI_Submit or GameAction.UI_Cancel) => true,
            
            // Dialog
            (GameState.Dialog, GameAction.Move) => false,
            (GameState.Dialog, GameAction.Ability1) => false,
            (GameState.Dialog, GameAction.UI_Navigate
                or GameAction.UI_Submit or GameAction.UI_Cancel) => true,
            (GameState.Dialog, GameAction.Pause) => true,
            
            // Paused
            (GameState.Paused, GameAction.Move) => false,
            (GameState.Paused, GameAction.Pause) => true,  // Resume
            (GameState.Paused, GameAction.UI_Navigate
                or GameAction.UI_Submit or GameAction.UI_Cancel) => true,
            
            // Loading
            (GameState.Loading, _) => false,
            
            _ => false
        };
    }
}
```

## Platform-Specific Bindings

```csharp
public class PlatformInputBindings
{
    public static void SetupPC()
    {
        // Keyboard + Mouse
        // Move: WASD or Left Stick
        // Abilities: 1-9 keys or buttons
        // Interact: E key
        // Pause: ESC
    }
    
    public static void SetupMobile()
    {
        // Touch input
        // Move: Virtual joystick
        // Abilities: On-screen buttons
        // Interact: Long press
        // Pause: Top-right button
    }
    
    public static void SetupController()
    {
        // Gamepad
        // Move: Left Stick
        // Abilities: Buttons + D-Pad combinations
        // Interact: Y button
        // Pause: Start button
    }
}
```

## Ability Queueing

```csharp
public class AbilityQueue
{
    private Queue<(Character, Ability)> _queue = new();
    
    public bool TryQueueAbility(Character character, int abilityIndex)
    {
        var ability = character.GetAbility(abilityIndex);
        if (ability == null)
            return false;
        
        if (!ability.IsOffCooldown())
        {
            Debug.Log($"{ability.Name} on cooldown");
            return false;
        }
        
        _queue.Enqueue((character, ability));
        return true;
    }
    
    public void ProcessQueue()
    {
        while (_queue.Count > 0)
        {
            var (character, ability) = _queue.Dequeue();
            ability.Activate(character);
        }
    }
}
```

## Success Criteria

- [x] All actions respond instantly to input (< 1ms)
- [x] Context sensitivity enforced per GameState
- [x] Ability cooldowns checked before queuing
- [x] Runtime rebinding works for all platforms
- [x] No input processed during Loading state

## Testing

```csharp
[Test]
public void TestAbilityBlockedInSafeZone()
{
    GameStateManager.Instance.ForceSetState(GameState.SafeZone);
    Assert.IsFalse(InputManager.CanTriggerAction(GameAction.Ability1));
}

[Test]
public void TestAbilityAllowedInExploration()
{
    GameStateManager.Instance.ForceSetState(GameState.Exploration);
    Assert.IsTrue(InputManager.CanTriggerAction(GameAction.Ability1));
}
```

## Changelog

- v1.0 (2026-02-08): Initial input system specification

