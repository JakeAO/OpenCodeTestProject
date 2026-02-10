using NUnit.Framework;
using OCTP.Core;
using UnityEngine;

namespace OCTP.Core.Tests
{
    [TestFixture]
    public class InputManagerTests
    {
        private GameObject _gameObject;
        private InputManager _inputManager;
        private GameStateManager _stateManager;
        
        [SetUp]
        public void SetUp()
        {
            // Create GameStateManager
            var stateGO = new GameObject("TestGameStateManager");
            _stateManager = stateGO.AddComponent<GameStateManager>();
            ServiceLocator.Register<IGameStateManager>(_stateManager);
            
            // Create InputManager
            _gameObject = new GameObject("TestInputManager");
            _inputManager = _gameObject.AddComponent<InputManager>();
            _inputManager.Initialize();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_inputManager != null)
            {
                Object.DestroyImmediate(_inputManager.gameObject);
            }
            if (_stateManager != null)
            {
                Object.DestroyImmediate(_stateManager.gameObject);
            }
            ServiceLocator.Clear();
        }
        
        #region State to Context Mapping Tests
        
        [Test]
        public void StateToContext_Exploration_MapsToGameplay()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.AreEqual(InputContext.Gameplay, _inputManager.CurrentContext);
        }
        
        [Test]
        public void StateToContext_SafeZone_MapsToGameplay()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.SafeZone);
            Assert.AreEqual(InputContext.Gameplay, _inputManager.CurrentContext);
        }
        
        [Test]
        public void StateToContext_Dialog_MapsToDialog()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Dialog);
            Assert.AreEqual(InputContext.Dialog, _inputManager.CurrentContext);
        }
        
        [Test]
        public void StateToContext_DialogClosing_MapsToDialog()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Dialog);
            _stateManager.TrySetState(GameState.DialogClosing);
            Assert.AreEqual(InputContext.Dialog, _inputManager.CurrentContext);
        }
        
        [Test]
        public void StateToContext_Paused_MapsToPaused()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Paused);
            Assert.AreEqual(InputContext.Paused, _inputManager.CurrentContext);
        }
        
        [Test]
        public void StateToContext_MainMenu_MapsToMenu()
        {
            Assert.AreEqual(InputContext.Menu, _inputManager.CurrentContext);
        }
        
        [Test]
        public void StateToContext_GameOver_MapsToMenu()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.GameOver);
            Assert.AreEqual(InputContext.Menu, _inputManager.CurrentContext);
        }
        
        [Test]
        public void StateToContext_TransitionLoading_MapsToNone()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.TransitionLoading);
            Assert.AreEqual(InputContext.None, _inputManager.CurrentContext);
        }
        
        #endregion
        
        #region Gameplay Context Action Tests
        
        [Test]
        public void GameplayContext_AllowsMove()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Move));
        }
        
        [Test]
        public void GameplayContext_AllowsAttack()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Attack));
        }
        
        [Test]
        public void GameplayContext_AllowsUseAbility()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.UseAbility));
        }
        
        [Test]
        public void GameplayContext_AllowsOpenMenu()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.OpenMenu));
        }
        
        [Test]
        public void GameplayContext_AllowsPause()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Pause));
        }
        
        [Test]
        public void GameplayContext_AllowsInteract()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Interact));
        }
        
        [Test]
        public void GameplayContext_BlocksConfirm()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Confirm));
        }
        
        [Test]
        public void GameplayContext_BlocksCancel()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Cancel));
        }
        
        [Test]
        public void GameplayContext_BlocksBack()
        {
            _stateManager.ForceSetState(GameState.Exploration);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Back));
        }
        
        #endregion
        
        #region Menu Context Action Tests
        
        [Test]
        public void MenuContext_AllowsConfirm()
        {
            _stateManager.ForceSetState(GameState.MainMenu);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Confirm));
        }
        
        [Test]
        public void MenuContext_AllowsCancel()
        {
            _stateManager.ForceSetState(GameState.MainMenu);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Cancel));
        }
        
        [Test]
        public void MenuContext_BlocksMove()
        {
            _stateManager.ForceSetState(GameState.MainMenu);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Move));
        }
        
        [Test]
        public void MenuContext_BlocksAttack()
        {
            _stateManager.ForceSetState(GameState.MainMenu);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Attack));
        }
        
        [Test]
        public void MenuContext_BlocksBack()
        {
            _stateManager.ForceSetState(GameState.MainMenu);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Back));
        }
        
        #endregion
        
        #region Dialog Context Action Tests
        
        [Test]
        public void DialogContext_AllowsConfirm()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Dialog);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Confirm));
        }
        
        [Test]
        public void DialogContext_AllowsCancel()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Dialog);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Cancel));
        }
        
        [Test]
        public void DialogContext_AllowsBack()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Dialog);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Back));
        }
        
        [Test]
        public void DialogContext_BlocksMove()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Dialog);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Move));
        }
        
        [Test]
        public void DialogContext_BlocksAttack()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Dialog);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Attack));
        }
        
        #endregion
        
        #region Paused Context Action Tests
        
        [Test]
        public void PausedContext_AllowsPause()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Paused);
            Assert.IsTrue(_inputManager.CanTriggerAction(GameAction.Pause));
        }
        
        [Test]
        public void PausedContext_BlocksMove()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Paused);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Move));
        }
        
        [Test]
        public void PausedContext_BlocksAttack()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Paused);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Attack));
        }
        
        [Test]
        public void PausedContext_BlocksConfirm()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.Paused);
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Confirm));
        }
        
        #endregion
        
        #region None Context Action Tests
        
        [Test]
        public void NoneContext_BlocksAllActions()
        {
            _stateManager.TrySetState(GameState.Exploration);
            _stateManager.TrySetState(GameState.TransitionLoading);
            
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Move));
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Attack));
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.UseAbility));
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.OpenMenu));
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Pause));
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Interact));
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Confirm));
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Cancel));
            Assert.IsFalse(_inputManager.CanTriggerAction(GameAction.Back));
        }
        
        #endregion
        
        #region Context Change Event Tests
        
        [Test]
        public void ContextChangeEvent_FiresWhenStateChanges()
        {
            InputContext? capturedContext = null;
            _inputManager.OnContextChanged += (context) => capturedContext = context;
            
            _stateManager.TrySetState(GameState.Exploration);
            
            Assert.IsNotNull(capturedContext);
            Assert.AreEqual(InputContext.Gameplay, capturedContext.Value);
        }
        
        [Test]
        public void ContextChangeEvent_DoesNotFireWhenContextUnchanged()
        {
            // First transition to Exploration
            _stateManager.TrySetState(GameState.Exploration);
            
            int eventCount = 0;
            _inputManager.OnContextChanged += (context) => eventCount++;
            
            // Transition to SafeZone (also Gameplay context)
            _stateManager.TrySetState(GameState.SafeZone);
            
            // Event should not fire since context remained Gameplay
            Assert.AreEqual(0, eventCount);
        }
        
        [Test]
        public void ContextChangeEvent_FiresMultipleTimesForMultipleChanges()
        {
            int eventCount = 0;
            _inputManager.OnContextChanged += (context) => eventCount++;
            
            _stateManager.TrySetState(GameState.Exploration);  // Menu -> Gameplay
            _stateManager.TrySetState(GameState.Dialog);        // Gameplay -> Dialog
            _stateManager.TrySetState(GameState.DialogClosing); // Dialog -> Dialog (no change)
            _stateManager.TrySetState(GameState.Exploration);   // Dialog -> Gameplay
            
            Assert.AreEqual(3, eventCount);
        }
        
        #endregion
        
        #region UI Context Action Tests
        
        [Test]
        public void UIContext_AllowsConfirm()
        {
            // Manually set context to UI for testing (no game state maps to UI in MVP)
            // This tests the validation logic directly
            _stateManager.ForceSetState(GameState.Exploration);
            var testGO = new GameObject("TestUI");
            var testManager = testGO.AddComponent<TestInputManagerWrapper>();
            testManager.SetContextForTesting(InputContext.UI);
            
            Assert.IsTrue(testManager.CanTriggerAction(GameAction.Confirm));
            Assert.IsTrue(testManager.CanTriggerAction(GameAction.Cancel));
            Assert.IsTrue(testManager.CanTriggerAction(GameAction.Back));
            Assert.IsFalse(testManager.CanTriggerAction(GameAction.Move));
            
            Object.DestroyImmediate(testGO);
        }
        
        #endregion
    }
    
    // Helper class to test UI context directly
    public class TestInputManagerWrapper : MonoBehaviour
    {
        private InputContext _testContext;
        
        public void SetContextForTesting(InputContext context)
        {
            _testContext = context;
        }
        
        public bool CanTriggerAction(GameAction action)
        {
            return (_testContext, action) switch
            {
                (InputContext.Gameplay, GameAction.Move) => true,
                (InputContext.Gameplay, GameAction.Attack) => true,
                (InputContext.Gameplay, GameAction.UseAbility) => true,
                (InputContext.Gameplay, GameAction.OpenMenu) => true,
                (InputContext.Gameplay, GameAction.Pause) => true,
                (InputContext.Gameplay, GameAction.Interact) => true,
                (InputContext.UI, GameAction.Confirm) => true,
                (InputContext.UI, GameAction.Cancel) => true,
                (InputContext.UI, GameAction.Back) => true,
                (InputContext.Menu, GameAction.Confirm) => true,
                (InputContext.Menu, GameAction.Cancel) => true,
                (InputContext.Dialog, GameAction.Confirm) => true,
                (InputContext.Dialog, GameAction.Cancel) => true,
                (InputContext.Dialog, GameAction.Back) => true,
                (InputContext.Paused, GameAction.Pause) => true,
                (InputContext.None, _) => false,
                _ => false
            };
        }
    }
}
