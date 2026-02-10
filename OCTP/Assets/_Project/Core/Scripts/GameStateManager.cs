using System;
using UnityEngine;

namespace OCTP.Core
{
    /// <summary>
    /// Manages game states with a finite state machine
    /// </summary>
    public class GameStateManager : MonoBehaviour, IGameStateManager
    {
        private GameState _currentState = GameState.MainMenu;
        private GameState _previousState = GameState.MainMenu;
        
        public GameState CurrentState => _currentState;
        public GameState PreviousState => _previousState;
        
        public event Action<GameState, GameState> OnStateChanged;
        public event Action<GameState> OnStateEntering;
        public event Action<GameState> OnStateExiting;
        
        public bool CanTransitionTo(GameState targetState)
        {
            return IsValidTransition(_currentState, targetState);
        }
        
        public bool TrySetState(GameState newState)
        {
            if (!IsValidTransition(_currentState, newState))
            {
                Debug.LogWarning($"[GameStateManager] Invalid transition: {_currentState} → {newState}");
                return false;
            }
            
            SetStateInternal(newState);
            return true;
        }
        
        public void ForceSetState(GameState newState)
        {
            SetStateInternal(newState);
        }
        
        private void SetStateInternal(GameState newState)
        {
            if (_currentState == newState)
                return;
            
            GameState oldState = _currentState;
            
            // Exit current state
            OnStateExiting?.Invoke(oldState);
            
            // Update state
            _previousState = _currentState;
            _currentState = newState;
            
            // Notify listeners of change
            OnStateChanged?.Invoke(oldState, newState);
            
            // Enter new state
            OnStateEntering?.Invoke(newState);
        }
        
        private bool IsValidTransition(GameState from, GameState to)
        {
            // Same state transition is always invalid
            if (from == to)
                return false;
            
            switch (from)
            {
                case GameState.MainMenu:
                    return to == GameState.Exploration || to == GameState.GameOver;
                    
                case GameState.Exploration:
                    return to == GameState.SafeZone 
                        || to == GameState.Dialog 
                        || to == GameState.Paused 
                        || to == GameState.TransitionLoading 
                        || to == GameState.GameOver;
                    
                case GameState.SafeZone:
                    return to == GameState.Exploration 
                        || to == GameState.Dialog 
                        || to == GameState.Paused 
                        || to == GameState.SafeZoneClosing
                        || to == GameState.GameOver;
                    
                case GameState.Dialog:
                    return to == GameState.DialogClosing;
                    
                case GameState.DialogClosing:
                    return to == GameState.Exploration || to == GameState.SafeZone;
                    
                case GameState.Paused:
                    // Can return to previous state (Exploration or SafeZone)
                    return to == GameState.Exploration || to == GameState.SafeZone;
                    
                case GameState.SafeZoneClosing:
                    return to == GameState.Exploration;
                    
                case GameState.TransitionLoading:
                    return to == GameState.Exploration || to == GameState.SafeZone;
                    
                case GameState.GameOver:
                    return to == GameState.MainMenu;
                    
                default:
                    return false;
            }
        }
    }
}
