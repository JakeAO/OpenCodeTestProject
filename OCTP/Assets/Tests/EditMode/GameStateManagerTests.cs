using NUnit.Framework;
using OCTP.Core;
using UnityEngine;

namespace OCTP.Tests.EditMode
{
    [TestFixture]
    public class GameStateManagerTests
    {
        private GameStateManager _manager;
        
        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("TestGameStateManager");
            _manager = go.AddComponent<GameStateManager>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_manager != null)
            {
                Object.DestroyImmediate(_manager.gameObject);
            }
        }
        
        #region Initial State Tests
        
        [Test]
        public void InitialState_ShouldBeMainMenu()
        {
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
            Assert.AreEqual(GameState.MainMenu, _manager.PreviousState);
        }
        
        #endregion
        
        #region Valid Transition Tests - MainMenu
        
        [Test]
        public void ValidTransition_MainMenuToExploration()
        {
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
            Assert.AreEqual(GameState.MainMenu, _manager.PreviousState);
        }
        
        [Test]
        public void ValidTransition_MainMenuToGameOver()
        {
            bool result = _manager.TrySetState(GameState.GameOver);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.GameOver, _manager.CurrentState);
        }
        
        #endregion
        
        #region Valid Transition Tests - Exploration
        
        [Test]
        public void ValidTransition_ExplorationToSafeZone()
        {
            _manager.ForceSetState(GameState.Exploration);
            bool result = _manager.TrySetState(GameState.SafeZone);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.SafeZone, _manager.CurrentState);
            Assert.AreEqual(GameState.Exploration, _manager.PreviousState);
        }
        
        [Test]
        public void ValidTransition_ExplorationToDialog()
        {
            _manager.ForceSetState(GameState.Exploration);
            bool result = _manager.TrySetState(GameState.Dialog);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Dialog, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_ExplorationToPaused()
        {
            _manager.ForceSetState(GameState.Exploration);
            bool result = _manager.TrySetState(GameState.Paused);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Paused, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_ExplorationToTransitionLoading()
        {
            _manager.ForceSetState(GameState.Exploration);
            bool result = _manager.TrySetState(GameState.TransitionLoading);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.TransitionLoading, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_ExplorationToGameOver()
        {
            _manager.ForceSetState(GameState.Exploration);
            bool result = _manager.TrySetState(GameState.GameOver);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.GameOver, _manager.CurrentState);
        }
        
        #endregion
        
        #region Valid Transition Tests - SafeZone
        
        [Test]
        public void ValidTransition_SafeZoneToExploration()
        {
            _manager.ForceSetState(GameState.SafeZone);
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_SafeZoneToDialog()
        {
            _manager.ForceSetState(GameState.SafeZone);
            bool result = _manager.TrySetState(GameState.Dialog);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Dialog, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_SafeZoneToPaused()
        {
            _manager.ForceSetState(GameState.SafeZone);
            bool result = _manager.TrySetState(GameState.Paused);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Paused, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_SafeZoneToSafeZoneClosing()
        {
            _manager.ForceSetState(GameState.SafeZone);
            bool result = _manager.TrySetState(GameState.SafeZoneClosing);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.SafeZoneClosing, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_SafeZoneToGameOver()
        {
            _manager.ForceSetState(GameState.SafeZone);
            bool result = _manager.TrySetState(GameState.GameOver);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.GameOver, _manager.CurrentState);
        }
        
        #endregion
        
        #region Valid Transition Tests - Dialog & DialogClosing
        
        [Test]
        public void ValidTransition_DialogToDialogClosing()
        {
            _manager.ForceSetState(GameState.Dialog);
            bool result = _manager.TrySetState(GameState.DialogClosing);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.DialogClosing, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_DialogClosingToExploration()
        {
            _manager.ForceSetState(GameState.DialogClosing);
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_DialogClosingToSafeZone()
        {
            _manager.ForceSetState(GameState.DialogClosing);
            bool result = _manager.TrySetState(GameState.SafeZone);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.SafeZone, _manager.CurrentState);
        }
        
        #endregion
        
        #region Valid Transition Tests - Paused
        
        [Test]
        public void ValidTransition_PausedToExploration()
        {
            _manager.ForceSetState(GameState.Paused);
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_PausedToSafeZone()
        {
            _manager.ForceSetState(GameState.Paused);
            bool result = _manager.TrySetState(GameState.SafeZone);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.SafeZone, _manager.CurrentState);
        }
        
        #endregion
        
        #region Valid Transition Tests - SafeZoneClosing
        
        [Test]
        public void ValidTransition_SafeZoneClosingToExploration()
        {
            _manager.ForceSetState(GameState.SafeZoneClosing);
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        #endregion
        
        #region Valid Transition Tests - TransitionLoading
        
        [Test]
        public void ValidTransition_TransitionLoadingToExploration()
        {
            _manager.ForceSetState(GameState.TransitionLoading);
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        [Test]
        public void ValidTransition_TransitionLoadingToSafeZone()
        {
            _manager.ForceSetState(GameState.TransitionLoading);
            bool result = _manager.TrySetState(GameState.SafeZone);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.SafeZone, _manager.CurrentState);
        }
        
        #endregion
        
        #region Valid Transition Tests - GameOver
        
        [Test]
        public void ValidTransition_GameOverToMainMenu()
        {
            _manager.ForceSetState(GameState.GameOver);
            bool result = _manager.TrySetState(GameState.MainMenu);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
        }
        
        #endregion
        
        #region Invalid Transition Tests
        
        [Test]
        public void InvalidTransition_SameState_MainMenu()
        {
            bool result = _manager.TrySetState(GameState.MainMenu);
            Assert.IsFalse(result);
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
        }
        
        [Test]
        public void InvalidTransition_SameState_Exploration()
        {
            _manager.ForceSetState(GameState.Exploration);
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void InvalidTransition_MainMenuToSafeZone()
        {
            bool result = _manager.TrySetState(GameState.SafeZone);
            Assert.IsFalse(result);
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
        }
        
        [Test]
        public void InvalidTransition_MainMenuToDialog()
        {
            bool result = _manager.TrySetState(GameState.Dialog);
            Assert.IsFalse(result);
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
        }
        
        [Test]
        public void InvalidTransition_MainMenuToPaused()
        {
            bool result = _manager.TrySetState(GameState.Paused);
            Assert.IsFalse(result);
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
        }
        
        [Test]
        public void InvalidTransition_ExplorationToDialogClosing()
        {
            _manager.ForceSetState(GameState.Exploration);
            bool result = _manager.TrySetState(GameState.DialogClosing);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void InvalidTransition_SafeZoneToTransitionLoading()
        {
            _manager.ForceSetState(GameState.SafeZone);
            bool result = _manager.TrySetState(GameState.TransitionLoading);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void InvalidTransition_DialogToExploration()
        {
            _manager.ForceSetState(GameState.Dialog);
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void InvalidTransition_DialogToSafeZone()
        {
            _manager.ForceSetState(GameState.Dialog);
            bool result = _manager.TrySetState(GameState.SafeZone);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void InvalidTransition_PausedToPaused()
        {
            _manager.ForceSetState(GameState.Paused);
            bool result = _manager.TrySetState(GameState.Paused);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void InvalidTransition_PausedToDialog()
        {
            _manager.ForceSetState(GameState.Paused);
            bool result = _manager.TrySetState(GameState.Dialog);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void InvalidTransition_GameOverToExploration()
        {
            _manager.ForceSetState(GameState.GameOver);
            bool result = _manager.TrySetState(GameState.Exploration);
            Assert.IsFalse(result);
        }
        
        #endregion
        
        #region ForceSetState Tests
        
        [Test]
        public void ForceSetState_BypassesValidation()
        {
            // MainMenu -> Dialog is invalid normally
            bool canTransition = _manager.CanTransitionTo(GameState.Dialog);
            Assert.IsFalse(canTransition);
            
            // But ForceSetState should work
            _manager.ForceSetState(GameState.Dialog);
            Assert.AreEqual(GameState.Dialog, _manager.CurrentState);
        }
        
        [Test]
        public void ForceSetState_UpdatesPreviousState()
        {
            _manager.ForceSetState(GameState.Exploration);
            _manager.ForceSetState(GameState.GameOver);
            Assert.AreEqual(GameState.GameOver, _manager.CurrentState);
            Assert.AreEqual(GameState.Exploration, _manager.PreviousState);
        }
        
        [Test]
        public void ForceSetState_SameState_DoesNothing()
        {
            _manager.ForceSetState(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
            Assert.AreEqual(GameState.MainMenu, _manager.PreviousState);
        }
        
        #endregion
        
        #region CanTransitionTo Tests
        
        [Test]
        public void CanTransitionTo_ValidTransition_ReturnsTrue()
        {
            _manager.ForceSetState(GameState.Exploration);
            bool canTransition = _manager.CanTransitionTo(GameState.SafeZone);
            Assert.IsTrue(canTransition);
        }
        
        [Test]
        public void CanTransitionTo_InvalidTransition_ReturnsFalse()
        {
            _manager.ForceSetState(GameState.Dialog);
            bool canTransition = _manager.CanTransitionTo(GameState.SafeZone);
            Assert.IsFalse(canTransition);
        }
        
        [Test]
        public void CanTransitionTo_DoesNotChangeState()
        {
            _manager.ForceSetState(GameState.Exploration);
            _manager.CanTransitionTo(GameState.SafeZone);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        #endregion
        
        #region Event Tests
        
        [Test]
        public void OnStateChanged_FiresOnValidTransition()
        {
            _manager.ForceSetState(GameState.Exploration);
            
            bool eventFired = false;
            GameState oldState = GameState.MainMenu;
            GameState newState = GameState.MainMenu;
            
            _manager.OnStateChanged += (old, newS) =>
            {
                eventFired = true;
                oldState = old;
                newState = newS;
            };
            
            _manager.TrySetState(GameState.SafeZone);
            
            Assert.IsTrue(eventFired);
            Assert.AreEqual(GameState.Exploration, oldState);
            Assert.AreEqual(GameState.SafeZone, newState);
        }
        
        [Test]
        public void OnStateChanged_DoesNotFireOnInvalidTransition()
        {
            _manager.ForceSetState(GameState.Dialog);
            
            bool eventFired = false;
            _manager.OnStateChanged += (old, newS) => eventFired = true;
            
            _manager.TrySetState(GameState.Exploration);
            
            Assert.IsFalse(eventFired);
        }
        
        [Test]
        public void OnStateEntering_FiresOnValidTransition()
        {
            _manager.ForceSetState(GameState.Exploration);
            
            bool eventFired = false;
            GameState enteredState = GameState.MainMenu;
            
            _manager.OnStateEntering += (state) =>
            {
                eventFired = true;
                enteredState = state;
            };
            
            _manager.TrySetState(GameState.SafeZone);
            
            Assert.IsTrue(eventFired);
            Assert.AreEqual(GameState.SafeZone, enteredState);
        }
        
        [Test]
        public void OnStateExiting_FiresOnValidTransition()
        {
            _manager.ForceSetState(GameState.Exploration);
            
            bool eventFired = false;
            GameState exitedState = GameState.MainMenu;
            
            _manager.OnStateExiting += (state) =>
            {
                eventFired = true;
                exitedState = state;
            };
            
            _manager.TrySetState(GameState.SafeZone);
            
            Assert.IsTrue(eventFired);
            Assert.AreEqual(GameState.Exploration, exitedState);
        }
        
        [Test]
        public void Events_FireInCorrectOrder()
        {
            _manager.ForceSetState(GameState.Exploration);
            
            var eventOrder = new System.Collections.Generic.List<string>();
            
            _manager.OnStateExiting += (state) => eventOrder.Add("Exiting");
            _manager.OnStateChanged += (old, newS) => eventOrder.Add("Changed");
            _manager.OnStateEntering += (state) => eventOrder.Add("Entering");
            
            _manager.TrySetState(GameState.SafeZone);
            
            Assert.AreEqual(3, eventOrder.Count);
            Assert.AreEqual("Exiting", eventOrder[0]);
            Assert.AreEqual("Changed", eventOrder[1]);
            Assert.AreEqual("Entering", eventOrder[2]);
        }
        
        [Test]
        public void OnStateChanged_FiresWithForceSetState()
        {
            bool eventFired = false;
            _manager.OnStateChanged += (old, newS) => eventFired = true;
            
            _manager.ForceSetState(GameState.Dialog);
            
            Assert.IsTrue(eventFired);
        }
        
        #endregion
        
        #region Previous State Tracking Tests
        
        [Test]
        public void PreviousState_TracksCorrectly()
        {
            _manager.ForceSetState(GameState.Exploration);
            Assert.AreEqual(GameState.MainMenu, _manager.PreviousState);
            
            _manager.TrySetState(GameState.SafeZone);
            Assert.AreEqual(GameState.Exploration, _manager.PreviousState);
            
            _manager.TrySetState(GameState.Dialog);
            Assert.AreEqual(GameState.SafeZone, _manager.PreviousState);
        }
        
        #endregion
        
        #region Intermediate State Tests
        
        [Test]
        public void IntermediateState_DialogClosingFlow()
        {
            // Start in Exploration, open Dialog
            _manager.ForceSetState(GameState.Exploration);
            _manager.TrySetState(GameState.Dialog);
            
            // Close dialog (goes to DialogClosing)
            bool result = _manager.TrySetState(GameState.DialogClosing);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.DialogClosing, _manager.CurrentState);
            
            // Return to Exploration
            result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        [Test]
        public void IntermediateState_SafeZoneClosingFlow()
        {
            _manager.ForceSetState(GameState.SafeZone);
            
            // Start closing animation
            bool result = _manager.TrySetState(GameState.SafeZoneClosing);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.SafeZoneClosing, _manager.CurrentState);
            
            // Complete transition to Exploration
            result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        [Test]
        public void IntermediateState_TransitionLoadingFlow()
        {
            _manager.ForceSetState(GameState.Exploration);
            
            // Start loading
            bool result = _manager.TrySetState(GameState.TransitionLoading);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.TransitionLoading, _manager.CurrentState);
            
            // Load complete -> SafeZone
            result = _manager.TrySetState(GameState.SafeZone);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.SafeZone, _manager.CurrentState);
        }
        
        #endregion
        
        #region Pause/Unpause Behavior Tests
        
        [Test]
        public void PauseUnpause_FromExploration()
        {
            _manager.ForceSetState(GameState.Exploration);
            
            // Pause
            bool result = _manager.TrySetState(GameState.Paused);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Paused, _manager.CurrentState);
            Assert.AreEqual(GameState.Exploration, _manager.PreviousState);
            
            // Unpause back to Exploration
            result = _manager.TrySetState(GameState.Exploration);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        [Test]
        public void PauseUnpause_FromSafeZone()
        {
            _manager.ForceSetState(GameState.SafeZone);
            
            // Pause
            bool result = _manager.TrySetState(GameState.Paused);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.Paused, _manager.CurrentState);
            Assert.AreEqual(GameState.SafeZone, _manager.PreviousState);
            
            // Unpause back to SafeZone
            result = _manager.TrySetState(GameState.SafeZone);
            Assert.IsTrue(result);
            Assert.AreEqual(GameState.SafeZone, _manager.CurrentState);
        }
        
        [Test]
        public void Pause_WhileAlreadyPaused_Fails()
        {
            _manager.ForceSetState(GameState.Paused);
            
            bool result = _manager.TrySetState(GameState.Paused);
            Assert.IsFalse(result);
            Assert.AreEqual(GameState.Paused, _manager.CurrentState);
        }
        
        #endregion
        
        #region Complex Flow Tests
        
        [Test]
        public void ComplexFlow_ExplorationToSafeZoneToDialogAndBack()
        {
            // Start in Exploration
            _manager.ForceSetState(GameState.Exploration);
            
            // Enter SafeZone
            _manager.TrySetState(GameState.SafeZone);
            Assert.AreEqual(GameState.SafeZone, _manager.CurrentState);
            
            // Open Dialog
            _manager.TrySetState(GameState.Dialog);
            Assert.AreEqual(GameState.Dialog, _manager.CurrentState);
            
            // Close Dialog
            _manager.TrySetState(GameState.DialogClosing);
            Assert.AreEqual(GameState.DialogClosing, _manager.CurrentState);
            
            // Return to SafeZone
            _manager.TrySetState(GameState.SafeZone);
            Assert.AreEqual(GameState.SafeZone, _manager.CurrentState);
        }
        
        [Test]
        public void ComplexFlow_GameOverToRestart()
        {
            _manager.ForceSetState(GameState.Exploration);
            
            // Party wipe
            _manager.TrySetState(GameState.GameOver);
            Assert.AreEqual(GameState.GameOver, _manager.CurrentState);
            
            // Restart to main menu
            _manager.TrySetState(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _manager.CurrentState);
            
            // Start new game
            _manager.TrySetState(GameState.Exploration);
            Assert.AreEqual(GameState.Exploration, _manager.CurrentState);
        }
        
        #endregion
    }
}
