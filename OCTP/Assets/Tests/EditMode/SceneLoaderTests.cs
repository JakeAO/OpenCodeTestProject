using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using OCTP.Core;
using Cysharp.Threading.Tasks;

namespace OCTP.Core.Tests
{
    [TestFixture]
    public class SceneLoaderTests
    {
        private GameObject _gameManagerObject;
        private GameObject _sceneLoaderObject;
        private GameStateManager _gameStateManager;
        private SceneLoader _sceneLoader;
        
        [SetUp]
        public void Setup()
        {
            // Create ServiceLocator
            GameObject serviceLocatorObject = new GameObject("ServiceLocator_Test");
            
            // Create GameStateManager
            _gameManagerObject = new GameObject("GameStateManager_Test");
            _gameStateManager = _gameManagerObject.AddComponent<GameStateManager>();
            
            // Register GameStateManager with ServiceLocator
            ServiceLocator.Register<IGameStateManager>(_gameStateManager);
            
            // Create SceneLoader
            _sceneLoaderObject = new GameObject("SceneLoader_Test");
            _sceneLoader = _sceneLoaderObject.AddComponent<SceneLoader>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_sceneLoaderObject != null)
                Object.DestroyImmediate(_sceneLoaderObject);
                
            if (_gameManagerObject != null)
                Object.DestroyImmediate(_gameManagerObject);
                
            // Clean up ServiceLocator
            ServiceLocator.Clear();
        }
        
        [UnityTest]
        public IEnumerator LoadZoneAsync_SetsStateToTransitionLoading()
        {
            // Arrange
            _gameStateManager.ForceSetState(GameState.SafeZone);
            
            // Act
            var loadTask = _sceneLoader.LoadZoneAsync("TestZone", true);
            
            // Wait a frame for the async operation to start
            yield return null;
            
            // Assert - Should be in TransitionLoading state
            Assert.AreEqual(GameState.TransitionLoading, _gameStateManager.CurrentState, 
                "GameState should be TransitionLoading during load");
            
            // Wait for load to complete
            yield return loadTask.ToCoroutine();
        }
        
        [UnityTest]
        public IEnumerator LoadZoneAsync_TransitionsToExplorationWhenIsExplorationTrue()
        {
            // Arrange
            _gameStateManager.ForceSetState(GameState.SafeZone);
            
            // Act
            var loadTask = _sceneLoader.LoadZoneAsync("ExplorationZone", true);
            yield return loadTask.ToCoroutine();
            
            // Assert
            Assert.AreEqual(GameState.Exploration, _gameStateManager.CurrentState, 
                "GameState should be Exploration after loading exploration zone");
        }
        
        [UnityTest]
        public IEnumerator LoadZoneAsync_TransitionsToSafeZoneWhenIsExplorationFalse()
        {
            // Arrange
            _gameStateManager.ForceSetState(GameState.Exploration);
            
            // Act
            var loadTask = _sceneLoader.LoadZoneAsync("SafeZone", false);
            yield return loadTask.ToCoroutine();
            
            // Assert
            Assert.AreEqual(GameState.SafeZone, _gameStateManager.CurrentState, 
                "GameState should be SafeZone after loading safe zone");
        }
        
        [UnityTest]
        public IEnumerator LoadZoneAsync_UpdatesCurrentZone()
        {
            // Arrange
            string zoneName = "TestZone";
            
            // Act
            var loadTask = _sceneLoader.LoadZoneAsync(zoneName, true);
            yield return loadTask.ToCoroutine();
            
            // Assert
            Assert.AreEqual(zoneName, _sceneLoader.CurrentZone, 
                "CurrentZone should be updated to loaded zone name");
        }
        
        [UnityTest]
        public IEnumerator LoadZoneAsync_SetsIsLoadingDuringLoad()
        {
            // Arrange
            Assert.IsFalse(_sceneLoader.IsLoading, "IsLoading should be false initially");
            
            // Act
            var loadTask = _sceneLoader.LoadZoneAsync("TestZone", true);
            
            // Wait a frame for async to start
            yield return null;
            
            // Assert - Should be loading
            Assert.IsTrue(_sceneLoader.IsLoading, "IsLoading should be true during load");
            
            // Wait for completion
            yield return loadTask.ToCoroutine();
            
            // Assert - Should not be loading anymore
            Assert.IsFalse(_sceneLoader.IsLoading, "IsLoading should be false after load completes");
        }
        
        [UnityTest]
        public IEnumerator LoadZoneAsync_FiresEventsInCorrectOrder()
        {
            // Arrange
            List<string> eventOrder = new List<string>();
            string zoneName = "TestZone";
            
            _sceneLoader.OnZoneLoadStarted += (zone) => eventOrder.Add($"Started:{zone}");
            _sceneLoader.OnZoneLoadCompleted += (zone) => eventOrder.Add($"Completed:{zone}");
            
            // Act
            var loadTask = _sceneLoader.LoadZoneAsync(zoneName, true);
            yield return loadTask.ToCoroutine();
            
            // Assert
            Assert.AreEqual(2, eventOrder.Count, "Should have fired 2 events");
            Assert.AreEqual($"Started:{zoneName}", eventOrder[0], "OnZoneLoadStarted should fire first");
            Assert.AreEqual($"Completed:{zoneName}", eventOrder[1], "OnZoneLoadCompleted should fire second");
        }
        
        [UnityTest]
        public IEnumerator LoadZoneAsync_CannotLoadWhileAlreadyLoading()
        {
            // Arrange
            var firstLoadTask = _sceneLoader.LoadZoneAsync("Zone1", true);
            
            // Wait a frame to ensure first load has started
            yield return null;
            Assert.IsTrue(_sceneLoader.IsLoading, "First load should be in progress");
            
            // Act - Try to load another zone while first is loading
            var secondLoadTask = _sceneLoader.LoadZoneAsync("Zone2", true);
            yield return secondLoadTask.ToCoroutine();
            
            // Wait for first load to complete
            yield return firstLoadTask.ToCoroutine();
            
            // Assert - Second load should have been rejected, first zone should be loaded
            Assert.AreEqual("Zone1", _sceneLoader.CurrentZone, 
                "CurrentZone should be the first zone, not the second");
        }
        
        [UnityTest]
        public IEnumerator UnloadCurrentZoneAsync_ClearsZone()
        {
            // Arrange - Load a zone first
            var loadTask = _sceneLoader.LoadZoneAsync("TestZone", true);
            yield return loadTask.ToCoroutine();
            
            Assert.AreEqual("TestZone", _sceneLoader.CurrentZone, "Zone should be loaded");
            
            // Act - Unload
            var unloadTask = _sceneLoader.UnloadCurrentZoneAsync();
            yield return unloadTask.ToCoroutine();
            
            // Assert
            Assert.IsNull(_sceneLoader.CurrentZone, "CurrentZone should be null after unload");
        }
        
        [UnityTest]
        public IEnumerator UnloadCurrentZoneAsync_HandlesNoZoneLoaded()
        {
            // Arrange
            Assert.IsNull(_sceneLoader.CurrentZone, "No zone should be loaded initially");
            
            // Act - Try to unload when nothing is loaded
            var unloadTask = _sceneLoader.UnloadCurrentZoneAsync();
            yield return unloadTask.ToCoroutine();
            
            // Assert - Should complete without error
            Assert.IsNull(_sceneLoader.CurrentZone, "CurrentZone should still be null");
        }
    }
}
