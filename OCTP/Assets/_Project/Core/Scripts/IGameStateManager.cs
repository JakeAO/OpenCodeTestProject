using System;

namespace OCTP.Core
{
    /// <summary>
    /// Game states for the finite state machine
    /// </summary>
    public enum GameState
    {
        // Main gameplay states
        MainMenu,
        Exploration,
        SafeZone,
        Dialog,
        Paused,
        GameOver,
        
        // Intermediate animation states
        DialogClosing,
        SafeZoneClosing,
        TransitionLoading
    }
    
    /// <summary>
    /// Interface for managing game states (Exploration, Combat, Paused, etc.)
    /// </summary>
    public interface IGameStateManager : IGameService
    {
        /// <summary>
        /// Gets the current game state
        /// </summary>
        GameState CurrentState { get; }
        
        /// <summary>
        /// Gets the previous game state
        /// </summary>
        GameState PreviousState { get; }
        
        /// <summary>
        /// Attempts to transition to a new state. Returns false if transition is invalid.
        /// </summary>
        bool TrySetState(GameState newState);
        
        /// <summary>
        /// Forces a state change without validation (admin override)
        /// </summary>
        void ForceSetState(GameState newState);
        
        /// <summary>
        /// Checks if transition to target state is valid without changing state
        /// </summary>
        bool CanTransitionTo(GameState targetState);
        
        /// <summary>
        /// Fired when state changes: (oldState, newState)
        /// </summary>
        event Action<GameState, GameState> OnStateChanged;
        
        /// <summary>
        /// Fired when entering a new state (after OnStateChanged)
        /// </summary>
        event Action<GameState> OnStateEntering;
        
        /// <summary>
        /// Fired when exiting a state (before OnStateChanged)
        /// </summary>
        event Action<GameState> OnStateExiting;
    }
}
