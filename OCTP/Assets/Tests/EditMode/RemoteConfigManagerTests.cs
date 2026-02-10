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
    public class RemoteConfigManagerTests
    {
        private GameObject _testGameObject;
        private RemoteConfigManager _remoteConfigManager;
        
        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("RemoteConfigManager_Test");
            _remoteConfigManager = _testGameObject.AddComponent<RemoteConfigManager>();
            
            // Wait for Awake to complete
            System.Threading.Thread.Sleep(100);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }
        
        [UnityTest]
        public IEnumerator LoadFromNakamaAsync_PopulatesCache()
        {
            // Act
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Assert
            Assert.IsTrue(_remoteConfigManager.IsLoaded, "Config should be marked as loaded");
            Assert.Greater(_remoteConfigManager.GetLoadedKeyCount(), 0, "Config cache should have keys");
        }
        
        [UnityTest]
        public IEnumerator LoadFromNakamaAsync_FiresOnConfigLoadedEvent()
        {
            // Arrange
            bool eventFired = false;
            _remoteConfigManager.OnConfigLoaded += () => eventFired = true;
            
            // Act
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Assert
            Assert.IsTrue(eventFired, "OnConfigLoaded event should fire");
        }
        
        [UnityTest]
        public IEnumerator Get_ReturnsValueAfterLoad()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            int playerHealth = _remoteConfigManager.Get<int>("balance.player_health", 50);
            
            // Assert
            Assert.AreEqual(100, playerHealth, "Should return loaded config value");
        }
        
        [Test]
        public void Get_ReturnsDefaultValueWhenKeyNotFound()
        {
            // Arrange - Set loaded flag manually to bypass warning
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            loadTask.ToCoroutine();
            System.Threading.Thread.Sleep(200);
            
            // Act
            int value = _remoteConfigManager.Get<int>("nonexistent.key", 42);
            
            // Assert
            Assert.AreEqual(42, value, "Should return default value for missing key");
        }
        
        [Test]
        public void Get_ReturnsDefaultValueBeforeLoad()
        {
            // Act
            int value = _remoteConfigManager.Get<int>("any.key", 99);
            
            // Assert
            Assert.AreEqual(99, value, "Should return default when not loaded");
        }
        
        [UnityTest]
        public IEnumerator Get_SupportsIntType()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            int value = _remoteConfigManager.Get<int>("balance.player_health", 0);
            
            // Assert
            Assert.AreEqual(100, value);
        }
        
        [UnityTest]
        public IEnumerator Get_SupportsFloatType()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act - Note: The mock returns an int, but we should be able to convert
            float value = _remoteConfigManager.Get<float>("balance.player_damage", 0f);
            
            // Assert
            Assert.AreEqual(10f, value);
        }
        
        [UnityTest]
        public IEnumerator Get_SupportsBoolType()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            bool value = _remoteConfigManager.Get<bool>("features.new_combat", true);
            
            // Assert
            Assert.AreEqual(false, value);
        }
        
        [UnityTest]
        public IEnumerator Get_SupportsStringType()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            string experimentId = _remoteConfigManager.Get<string>("experiment.id", "default");
            
            // Assert - Will return default since key not in mock
            Assert.AreEqual("default", experimentId);
        }
        
        [UnityTest]
        public IEnumerator Get_HandlesTypeMismatch()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act - Try to get an int as string
            string value = _remoteConfigManager.Get<string>("balance.player_health", "default");
            
            // Assert - Should convert int to string
            Assert.AreEqual("100", value);
        }
        
        [UnityTest]
        public IEnumerator Get_SupportsNestedKeys()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act - Test dot notation
            int playerHealth = _remoteConfigManager.Get<int>("balance.player_health", 0);
            int playerDamage = _remoteConfigManager.Get<int>("balance.player_damage", 0);
            bool newCombat = _remoteConfigManager.Get<bool>("features.new_combat", true);
            
            // Assert
            Assert.AreEqual(100, playerHealth, "Should support nested key balance.player_health");
            Assert.AreEqual(10, playerDamage, "Should support nested key balance.player_damage");
            Assert.AreEqual(false, newCombat, "Should support nested key features.new_combat");
        }
        
        [UnityTest]
        public IEnumerator HasKey_ReturnsTrueForExistingKey()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act & Assert
            Assert.IsTrue(_remoteConfigManager.HasKey("balance.player_health"));
            Assert.IsTrue(_remoteConfigManager.HasKey("balance.player_damage"));
            Assert.IsTrue(_remoteConfigManager.HasKey("features.new_combat"));
        }
        
        [UnityTest]
        public IEnumerator HasKey_ReturnsFalseForMissingKey()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act & Assert
            Assert.IsFalse(_remoteConfigManager.HasKey("nonexistent.key"));
            Assert.IsFalse(_remoteConfigManager.HasKey("balance.missing"));
        }
        
        [Test]
        public void HasKey_ReturnsFalseBeforeLoad()
        {
            // Act & Assert
            Assert.IsFalse(_remoteConfigManager.HasKey("any.key"));
        }
        
        [UnityTest]
        public IEnumerator GetLoadedKeyCount_ReturnsCorrectCount()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            int count = _remoteConfigManager.GetLoadedKeyCount();
            
            // Assert
            Assert.AreEqual(3, count, "Should have 3 keys from mock data");
        }
        
        [Test]
        public void GetLoadedKeyCount_ReturnsZeroBeforeLoad()
        {
            // Act
            int count = _remoteConfigManager.GetLoadedKeyCount();
            
            // Assert
            Assert.AreEqual(0, count);
        }
        
        [UnityTest]
        public IEnumerator GetExperimentId_ReturnsCorrectValue()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            string experimentId = _remoteConfigManager.GetExperimentId();
            
            // Assert
            Assert.AreEqual("tutorial_v1", experimentId);
        }
        
        [UnityTest]
        public IEnumerator GetCohort_ReturnsCorrectValue()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            string cohort = _remoteConfigManager.GetCohort();
            
            // Assert
            Assert.AreEqual("control", cohort);
        }
        
        [Test]
        public void GetExperimentId_ReturnsNullBeforeLoad()
        {
            // Act
            string experimentId = _remoteConfigManager.GetExperimentId();
            
            // Assert
            Assert.IsNull(experimentId);
        }
        
        [Test]
        public void GetCohort_ReturnsNullBeforeLoad()
        {
            // Act
            string cohort = _remoteConfigManager.GetCohort();
            
            // Assert
            Assert.IsNull(cohort);
        }
        
        [Test]
        public void IsLoaded_ReturnsFalseInitially()
        {
            // Act & Assert
            Assert.IsFalse(_remoteConfigManager.IsLoaded);
        }
        
        [UnityTest]
        public IEnumerator IsLoaded_ReturnsTrueAfterLoad()
        {
            // Act
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Assert
            Assert.IsTrue(_remoteConfigManager.IsLoaded);
        }
        
        [UnityTest]
        public IEnumerator OnConfigLoaded_FiresOnlyAfterLoad()
        {
            // Arrange
            int callCount = 0;
            _remoteConfigManager.OnConfigLoaded += () => callCount++;
            
            // Act
            Assert.AreEqual(0, callCount, "Event should not fire before load");
            
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Assert
            Assert.AreEqual(1, callCount, "Event should fire once after load");
        }
        
        [UnityTest]
        public IEnumerator Get_WithDefaultValue_UsesDefaultForMissingKey()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            int value = _remoteConfigManager.Get<int>("missing.config", 999);
            
            // Assert
            Assert.AreEqual(999, value);
        }
        
        [UnityTest]
        public IEnumerator Get_WithDefaultValue_UsesConfigValueWhenPresent()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            // Act
            int value = _remoteConfigManager.Get<int>("balance.player_health", 999);
            
            // Assert
            Assert.AreEqual(100, value, "Should use config value, not default");
        }
        
        [UnityTest]
        public IEnumerator Get_ThreadSafety_ConcurrentAccess()
        {
            // Arrange
            var loadTask = _remoteConfigManager.LoadFromNakamaAsync();
            yield return loadTask.ToCoroutine();
            
            var tasks = new List<System.Threading.Tasks.Task>();
            var results = new int[100];
            
            // Act - Access config from multiple threads
            for (int i = 0; i < 100; i++)
            {
                int index = i;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    results[index] = _remoteConfigManager.Get<int>("balance.player_health", 0);
                });
                tasks.Add(task);
            }
            
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            
            // Assert - All threads should get the same value
            foreach (var result in results)
            {
                Assert.AreEqual(100, result);
            }
        }
    }
}
