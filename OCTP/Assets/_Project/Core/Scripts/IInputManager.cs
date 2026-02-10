using System;

namespace OCTP.Core
{
    /// <summary>
    /// Game actions that can be triggered
    /// </summary>
    public enum GameAction
    {
        Move,
        Attack,
        UseAbility,
        OpenMenu,
        Interact,
        Pause,
        Cancel,
        Confirm,
        Back
    }
    
    /// <summary>
    /// Input contexts based on game state
    /// </summary>
    public enum InputContext
    {
        None,
        Gameplay,
        UI,
        Menu,
        Dialog,
        Paused
    }
    
    /// <summary>
    /// Interface for input handling and management.
    /// </summary>
    public interface IInputManager : IGameService
    {
        /// <summary>
        /// Gets the current input context
        /// </summary>
        InputContext CurrentContext { get; }
        
        /// <summary>
        /// Checks if a game action can be triggered in the current context
        /// </summary>
        bool CanTriggerAction(GameAction action);
        
        /// <summary>
        /// Fired when the input context changes
        /// </summary>
        event Action<InputContext> OnContextChanged;
    }
}
