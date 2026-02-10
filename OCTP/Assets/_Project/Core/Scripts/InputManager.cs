using System;
using UnityEngine;

namespace OCTP.Core
{
    /// <summary>
    /// Manages input contexts and validates actions based on game state.
    /// MVP skeleton implementation - does not handle actual input processing.
    /// </summary>
    public class InputManager : MonoBehaviour, IInputManager
    {
        private IGameStateManager _stateManager;
        private InputContext _currentContext = InputContext.None;
        
        public InputContext CurrentContext => _currentContext;
        
        public event Action<InputContext> OnContextChanged;
        
        public void Initialize()
        {
            _stateManager = ServiceLocator.Get<IGameStateManager>();
            if (_stateManager == null)
            {
                Debug.LogError("[InputManager] GameStateManager not found in ServiceLocator");
                return;
            }
            
            _stateManager.OnStateChanged += OnGameStateChanged;
            
            // Set initial context based on current state
            _currentContext = MapStateToContext(_stateManager.CurrentState);
        }
        
        public bool CanTriggerAction(GameAction action)
        {
            return IsActionAllowedInContext(_currentContext, action);
        }
        
        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            InputContext newContext = MapStateToContext(newState);
            
            if (newContext != _currentContext)
            {
                InputContext oldContext = _currentContext;
                _currentContext = newContext;
                OnContextChanged?.Invoke(newContext);
                
                Debug.Log($"[InputManager] Context changed: {oldContext} → {newContext}");
            }
        }
        
        private InputContext MapStateToContext(GameState state)
        {
            return state switch
            {
                GameState.Exploration => InputContext.Gameplay,
                GameState.SafeZone => InputContext.Gameplay,
                GameState.Dialog => InputContext.Dialog,
                GameState.DialogClosing => InputContext.Dialog,
                GameState.Paused => InputContext.Paused,
                GameState.MainMenu => InputContext.Menu,
                GameState.GameOver => InputContext.Menu,
                GameState.TransitionLoading => InputContext.None,
                GameState.SafeZoneClosing => InputContext.Gameplay,
                _ => InputContext.None
            };
        }
        
        private bool IsActionAllowedInContext(InputContext context, GameAction action)
        {
            return (context, action) switch
            {
                // Gameplay context
                (InputContext.Gameplay, GameAction.Move) => true,
                (InputContext.Gameplay, GameAction.Attack) => true,
                (InputContext.Gameplay, GameAction.UseAbility) => true,
                (InputContext.Gameplay, GameAction.OpenMenu) => true,
                (InputContext.Gameplay, GameAction.Pause) => true,
                (InputContext.Gameplay, GameAction.Interact) => true,
                
                // UI context
                (InputContext.UI, GameAction.Confirm) => true,
                (InputContext.UI, GameAction.Cancel) => true,
                (InputContext.UI, GameAction.Back) => true,
                
                // Menu context
                (InputContext.Menu, GameAction.Confirm) => true,
                (InputContext.Menu, GameAction.Cancel) => true,
                
                // Dialog context
                (InputContext.Dialog, GameAction.Confirm) => true,
                (InputContext.Dialog, GameAction.Cancel) => true,
                (InputContext.Dialog, GameAction.Back) => true,
                
                // Paused context
                (InputContext.Paused, GameAction.Pause) => true,
                
                // None context - no actions allowed
                (InputContext.None, _) => false,
                
                // All other combinations are not allowed
                _ => false
            };
        }
        
        private void OnDestroy()
        {
            if (_stateManager != null)
            {
                _stateManager.OnStateChanged -= OnGameStateChanged;
            }
        }
    }
}
