using NUnit.Framework;
using UnityEngine;

namespace OCTP.Core.Tests
{
    /// <summary>
    /// Tests for the GameManager initialization, lifecycle, and service registration.
    /// </summary>
    [TestFixture]
    public class GameManagerTests
    {
        private GameObject gameManagerObject;
        private GameManager gameManager;

        [SetUp]
        public void Setup()
        {
            // Clear ServiceLocator before each test
            ServiceLocator.Clear();

            // Create a new GameObject with GameManager
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }

            ServiceLocator.Clear();
        }

        [Test]
        public void TestSingletonPattern()
        {
            // Verify singleton is set correctly
            Assert.IsNotNull(GameManager.Instance, "GameManager Instance should not be null after creation");
            Assert.AreEqual(gameManager, GameManager.Instance, "GameManager Instance should match the created instance");
        }

        [Test]
        public void TestSingletonPreventsDuplicates()
        {
            // Create a second GameManager
            var secondGameManagerObject = new GameObject("GameManager2");
            var secondGameManager = secondGameManagerObject.AddComponent<GameManager>();

            // The second instance should be destroyed
            Assert.IsNull(secondGameManager.gameObject.GetComponent<GameManager>(), 
                "Second GameManager should be destroyed");
            
            // Original instance should still be the singleton
            Assert.AreEqual(gameManager, GameManager.Instance, 
                "Original GameManager should still be the singleton instance");

            Object.DestroyImmediate(secondGameManagerObject);
        }

        [Test]
        public void TestServiceRegistration()
        {
            // Verify all managers are registered
            Assert.IsTrue(ServiceLocator.Has<IGameStateManager>(), "IGameStateManager should be registered");
            Assert.IsTrue(ServiceLocator.Has<IAnalyticsManager>(), "IAnalyticsManager should be registered");
            Assert.IsTrue(ServiceLocator.Has<IRemoteConfigManager>(), "IRemoteConfigManager should be registered");
            Assert.IsTrue(ServiceLocator.Has<IInputManager>(), "IInputManager should be registered");
            Assert.IsTrue(ServiceLocator.Has<ISceneLoader>(), "ISceneLoader should be registered");
            Assert.IsTrue(ServiceLocator.Has<ISaveManager>(), "ISaveManager should be registered");
            Assert.IsTrue(ServiceLocator.Has<IPartyManager>(), "IPartyManager should be registered");
        }

        [Test]
        public void TestCanRetrieveRegisteredServices()
        {
            // Verify we can retrieve all registered services
            Assert.IsNotNull(ServiceLocator.Get<IGameStateManager>(), "Should retrieve IGameStateManager");
            Assert.IsNotNull(ServiceLocator.Get<IAnalyticsManager>(), "Should retrieve IAnalyticsManager");
            Assert.IsNotNull(ServiceLocator.Get<IRemoteConfigManager>(), "Should retrieve IRemoteConfigManager");
            Assert.IsNotNull(ServiceLocator.Get<IInputManager>(), "Should retrieve IInputManager");
            Assert.IsNotNull(ServiceLocator.Get<ISceneLoader>(), "Should retrieve ISceneLoader");
            Assert.IsNotNull(ServiceLocator.Get<ISaveManager>(), "Should retrieve ISaveManager");
            Assert.IsNotNull(ServiceLocator.Get<IPartyManager>(), "Should retrieve IPartyManager");
        }

        [Test]
        public void TestPlayMethod()
        {
            // Ensure time scale is normal after Play()
            gameManager.Play();
            Assert.AreEqual(1f, Time.timeScale, "Time scale should be 1 after Play()");
        }

        [Test]
        public void TestPauseMethod()
        {
            bool pauseEventFired = false;
            gameManager.OnGamePaused += () => pauseEventFired = true;

            gameManager.Pause();

            Assert.AreEqual(0f, Time.timeScale, "Time scale should be 0 when game is paused");
            Assert.IsTrue(pauseEventFired, "OnGamePaused event should be fired");
        }

        [Test]
        public void TestResumeMethod()
        {
            bool resumeEventFired = false;
            gameManager.OnGameResumed += () => resumeEventFired = true;

            // First pause, then resume
            gameManager.Pause();
            gameManager.Resume();

            Assert.AreEqual(1f, Time.timeScale, "Time scale should be 1 when game is resumed");
            Assert.IsTrue(resumeEventFired, "OnGameResumed event should be fired");
        }

        [Test]
        public void TestOnGameStartedEvent()
        {
            // Need to test with a fresh GameManager since Awake already fired
            ServiceLocator.Clear();
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }

            bool startEventFired = false;
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
            gameManager.OnGameStarted += () => startEventFired = true;

            // Trigger Awake by accessing Instance (already happened in AddComponent)
            // The event should have fired during initialization
            Assert.IsTrue(startEventFired, "OnGameStarted event should be fired during initialization");
        }

        [Test]
        public void TestPauseResumeSequence()
        {
            int pauseCount = 0;
            int resumeCount = 0;

            gameManager.OnGamePaused += () => pauseCount++;
            gameManager.OnGameResumed += () => resumeCount++;

            // Pause and resume multiple times
            gameManager.Pause();
            gameManager.Resume();
            gameManager.Pause();
            gameManager.Resume();

            Assert.AreEqual(2, pauseCount, "OnGamePaused should fire twice");
            Assert.AreEqual(2, resumeCount, "OnGameResumed should fire twice");
            Assert.AreEqual(1f, Time.timeScale, "Time scale should be 1 after final resume");
        }

        [Test]
        public void TestQuitEvent()
        {
            bool quitEventFired = false;
            gameManager.OnGameQuitting += () => quitEventFired = true;

            // Note: We can't actually test Application.Quit() in edit mode tests,
            // but we can test that the event fires
            gameManager.Quit();

            Assert.IsTrue(quitEventFired, "OnGameQuitting event should be fired");
        }
    }
}
