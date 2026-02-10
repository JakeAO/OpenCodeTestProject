using NUnit.Framework;
using OCTP.Core;
using UnityEngine;
using System;

namespace OCTP.Tests
{
    public class NetworkManagerTests
    {
        private GameObject _gameObject;
        private NetworkManager _networkManager;
        private NetworkConfig _mockConfig;

        private const string PREF_KEY_OVERRIDE = "OCTP_NetworkEnvironmentOverride";
        private const string PREF_KEY_OVERRIDE_ACTIVE = "OCTP_NetworkEnvironmentOverrideActive";

        [SetUp]
        public void SetUp()
        {
            // Clear PlayerPrefs
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // Create GameObject with NetworkManager
            _gameObject = new GameObject("NetworkManager");
            _networkManager = _gameObject.AddComponent<NetworkManager>();

            // Create mock NetworkConfig
            CreateMockNetworkConfig();
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            if (_gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_gameObject);
            }

            if (_mockConfig != null)
            {
                UnityEngine.Object.DestroyImmediate(_mockConfig);
            }
        }

        private void CreateMockNetworkConfig()
        {
            _mockConfig = ScriptableObject.CreateInstance<NetworkConfig>();
            
            // Use reflection to set private fields
            var localConfig = new NetworkEndpointConfig("localhost", 7350, "local_key", false);
            var devConfig = new NetworkEndpointConfig("dev.example.com", 7350, "dev_key", true);
            var stagingConfig = new NetworkEndpointConfig("staging.example.com", 443, "staging_key", true);
            var prodConfig = new NetworkEndpointConfig("prod.example.com", 443, "prod_key", true);

            var configType = typeof(NetworkConfig);
            configType.GetField("_localConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_mockConfig, localConfig);
            configType.GetField("_developmentConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_mockConfig, devConfig);
            configType.GetField("_stagingConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_mockConfig, stagingConfig);
            configType.GetField("_productionConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_mockConfig, prodConfig);
        }

        #region Compiler Flag Tests

        [Test]
        public void DefaultEnvironment_InEditor_IsLocal()
        {
            // The NetworkManager should default to Local in editor when no compiler flags are set
            #if UNITY_EDITOR && !NETWORK_PRODUCTION && !NETWORK_STAGING && !NETWORK_DEV && !NETWORK_LOCAL
            // Initialize without any overrides
            _networkManager.Initialize();
            
            // Assert
            Assert.AreEqual(NetworkEnvironment.Local, _networkManager.CurrentEnvironment);
            #endif
        }

        #endregion

        #region Environment Switching Tests

        [Test]
        public void SetEnvironment_SavesToPlayerPrefs()
        {
            // Arrange
            var targetEnvironment = NetworkEnvironment.Development;

            // Act
            _networkManager.SetEnvironment(targetEnvironment);

            // Assert
            Assert.AreEqual(1, PlayerPrefs.GetInt(PREF_KEY_OVERRIDE_ACTIVE));
            Assert.AreEqual((int)targetEnvironment, PlayerPrefs.GetInt(PREF_KEY_OVERRIDE));
        }

        [Test]
        public void SetEnvironment_FiresEvent()
        {
            // Arrange
            NetworkEnvironment capturedEnvironment = NetworkEnvironment.Local;
            bool eventFired = false;
            _networkManager.OnEnvironmentChanged += (env) =>
            {
                eventFired = true;
                capturedEnvironment = env;
            };
            var targetEnvironment = NetworkEnvironment.Staging;

            // Act
            _networkManager.SetEnvironment(targetEnvironment);

            // Assert
            Assert.IsTrue(eventFired, "OnEnvironmentChanged event should fire");
            Assert.AreEqual(targetEnvironment, capturedEnvironment);
        }

        [Test]
        public void SetEnvironment_AllEnvironments_SavesCorrectly()
        {
            // Test all environment values
            var environments = new[] 
            { 
                NetworkEnvironment.Local, 
                NetworkEnvironment.Development, 
                NetworkEnvironment.Staging, 
                NetworkEnvironment.Production 
            };

            foreach (var env in environments)
            {
                // Act
                _networkManager.SetEnvironment(env);

                // Assert
                Assert.AreEqual((int)env, PlayerPrefs.GetInt(PREF_KEY_OVERRIDE), $"Failed for {env}");
                Assert.AreEqual(1, PlayerPrefs.GetInt(PREF_KEY_OVERRIDE_ACTIVE), $"Failed to activate override for {env}");
                
                // Clear for next iteration
                PlayerPrefs.DeleteAll();
            }
        }

        [Test]
        public void SetEnvironment_SameEnvironmentWithOverride_LogsWarning()
        {
            // Arrange
            var environment = NetworkEnvironment.Production;
            _networkManager.SetEnvironment(environment);

            // Re-initialize to pick up the override
            _networkManager.Initialize();

            // Act - Set to same environment again
            _networkManager.SetEnvironment(environment);

            // Assert - Should complete without exception (warning logged internally)
            Assert.AreEqual(environment, (NetworkEnvironment)PlayerPrefs.GetInt(PREF_KEY_OVERRIDE));
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void GetConfig_ReturnsCorrectValues_ForAllEnvironments()
        {
            // Test Local
            var localConfig = _mockConfig.GetConfig(NetworkEnvironment.Local);
            Assert.AreEqual("localhost", localConfig.serverUrl);
            Assert.AreEqual(7350, localConfig.serverPort);
            Assert.AreEqual("local_key", localConfig.httpKey);
            Assert.IsFalse(localConfig.useSSL);

            // Test Development
            var devConfig = _mockConfig.GetConfig(NetworkEnvironment.Development);
            Assert.AreEqual("dev.example.com", devConfig.serverUrl);
            Assert.AreEqual(7350, devConfig.serverPort);
            Assert.AreEqual("dev_key", devConfig.httpKey);
            Assert.IsTrue(devConfig.useSSL);

            // Test Staging
            var stagingConfig = _mockConfig.GetConfig(NetworkEnvironment.Staging);
            Assert.AreEqual("staging.example.com", stagingConfig.serverUrl);
            Assert.AreEqual(443, stagingConfig.serverPort);
            Assert.AreEqual("staging_key", stagingConfig.httpKey);
            Assert.IsTrue(stagingConfig.useSSL);

            // Test Production
            var prodConfig = _mockConfig.GetConfig(NetworkEnvironment.Production);
            Assert.AreEqual("prod.example.com", prodConfig.serverUrl);
            Assert.AreEqual(443, prodConfig.serverPort);
            Assert.AreEqual("prod_key", prodConfig.httpKey);
            Assert.IsTrue(prodConfig.useSSL);
        }

        [Test]
        public void ServerUrl_ReturnsCorrectValue()
        {
            // Arrange - Use reflection to set config
            SetNetworkManagerConfig(_mockConfig);
            _networkManager.Initialize();

            // Assert - Should return local by default in editor
            Assert.IsNotNull(_networkManager.ServerUrl);
            Assert.IsNotEmpty(_networkManager.ServerUrl);
        }

        [Test]
        public void ServerPort_ReturnsCorrectValue()
        {
            // Arrange
            SetNetworkManagerConfig(_mockConfig);
            _networkManager.Initialize();

            // Assert
            Assert.Greater(_networkManager.ServerPort, 0);
            Assert.LessOrEqual(_networkManager.ServerPort, 65535);
        }

        [Test]
        public void HttpKey_ReturnsCorrectValue()
        {
            // Arrange
            SetNetworkManagerConfig(_mockConfig);
            _networkManager.Initialize();

            // Assert
            Assert.IsNotNull(_networkManager.HttpKey);
            Assert.IsNotEmpty(_networkManager.HttpKey);
        }

        [Test]
        public void UseSSL_ReturnsCorrectValue()
        {
            // Arrange
            SetNetworkManagerConfig(_mockConfig);
            _networkManager.Initialize();

            // Assert - Local environment should not use SSL
            Assert.IsFalse(_networkManager.UseSSL);
        }

        [Test]
        public void FullServerUrl_BuildsCorrectly_WithoutSSL()
        {
            // Arrange
            SetNetworkManagerConfig(_mockConfig);
            _networkManager.Initialize();

            // Act
            string fullUrl = _networkManager.FullServerUrl;

            // Assert
            Assert.IsTrue(fullUrl.StartsWith("http://"), "Should use http without SSL");
            Assert.IsTrue(fullUrl.Contains(_networkManager.ServerUrl), "Should contain server URL");
            Assert.IsTrue(fullUrl.Contains(_networkManager.ServerPort.ToString()), "Should contain port");
        }

        [Test]
        public void FullServerUrl_BuildsCorrectly_WithSSL()
        {
            // Arrange
            SetNetworkManagerConfig(_mockConfig);
            PlayerPrefs.SetInt(PREF_KEY_OVERRIDE, (int)NetworkEnvironment.Production);
            PlayerPrefs.SetInt(PREF_KEY_OVERRIDE_ACTIVE, 1);
            PlayerPrefs.Save();
            _networkManager.Initialize();

            // Act
            string fullUrl = _networkManager.FullServerUrl;

            // Assert
            Assert.IsTrue(fullUrl.StartsWith("https://"), "Should use https with SSL");
            Assert.IsTrue(fullUrl.Contains(_networkManager.ServerUrl), "Should contain server URL");
        }

        #endregion

        #region Override Tests

        [Test]
        public void PlayerPrefsOverride_TakesPriority_OverCompilerFlag()
        {
            // Arrange - Set override to Production
            PlayerPrefs.SetInt(PREF_KEY_OVERRIDE, (int)NetworkEnvironment.Production);
            PlayerPrefs.SetInt(PREF_KEY_OVERRIDE_ACTIVE, 1);
            PlayerPrefs.Save();
            
            SetNetworkManagerConfig(_mockConfig);

            // Act
            _networkManager.Initialize();

            // Assert
            Assert.AreEqual(NetworkEnvironment.Production, _networkManager.CurrentEnvironment);
            Assert.IsTrue(_networkManager.HasEnvironmentOverride);
        }

        [Test]
        public void HasEnvironmentOverride_ReturnsFalse_WhenNoOverride()
        {
            // Assert
            Assert.IsFalse(_networkManager.HasEnvironmentOverride);
        }

        [Test]
        public void HasEnvironmentOverride_ReturnsTrue_WhenOverrideSet()
        {
            // Act
            _networkManager.SetEnvironment(NetworkEnvironment.Development);

            // Assert
            Assert.IsTrue(_networkManager.HasEnvironmentOverride);
        }

        [Test]
        public void ClearEnvironmentOverride_RemovesPlayerPrefs()
        {
            // Arrange - Set an override first
            _networkManager.SetEnvironment(NetworkEnvironment.Production);
            Assert.IsTrue(_networkManager.HasEnvironmentOverride, "Setup: Override should be set");

            // Act
            _networkManager.ClearEnvironmentOverride();

            // Assert
            Assert.IsFalse(PlayerPrefs.HasKey(PREF_KEY_OVERRIDE), "Override key should be removed");
            Assert.IsFalse(PlayerPrefs.HasKey(PREF_KEY_OVERRIDE_ACTIVE), "Override active key should be removed");
            Assert.IsFalse(_networkManager.HasEnvironmentOverride, "HasEnvironmentOverride should return false");
        }

        [Test]
        public void ClearEnvironmentOverride_WhenNoOverride_LogsWarning()
        {
            // Arrange - Ensure no override exists
            Assert.IsFalse(_networkManager.HasEnvironmentOverride);

            // Act - Should not throw, just log warning
            _networkManager.ClearEnvironmentOverride();

            // Assert - No exception thrown
            Assert.IsFalse(_networkManager.HasEnvironmentOverride);
        }

        [Test]
        public void ClearEnvironmentOverride_RevertsToDefault()
        {
            // Arrange
            SetNetworkManagerConfig(_mockConfig);
            _networkManager.Initialize();
            var originalEnvironment = _networkManager.CurrentEnvironment;

            // Set override to different environment
            var overrideEnvironment = NetworkEnvironment.Production;
            if (originalEnvironment == NetworkEnvironment.Production)
            {
                overrideEnvironment = NetworkEnvironment.Development;
            }
            
            PlayerPrefs.SetInt(PREF_KEY_OVERRIDE, (int)overrideEnvironment);
            PlayerPrefs.SetInt(PREF_KEY_OVERRIDE_ACTIVE, 1);
            PlayerPrefs.Save();

            // Act
            _networkManager.ClearEnvironmentOverride();

            // Simulate restart by re-initializing
            _networkManager.Initialize();

            // Assert - Should revert to original default
            Assert.AreEqual(originalEnvironment, _networkManager.CurrentEnvironment);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void MissingNetworkConfig_DefaultsToLocal()
        {
            // Arrange - Don't set any config
            // Act
            _networkManager.Initialize();

            // Assert
            Assert.AreEqual(NetworkEnvironment.Local, _networkManager.CurrentEnvironment);
            Assert.AreEqual("localhost", _networkManager.ServerUrl);
            Assert.AreEqual(7350, _networkManager.ServerPort);
        }

        [Test]
        public void MissingNetworkConfig_StillAllowsEnvironmentChange()
        {
            // Arrange
            _networkManager.Initialize();

            // Act
            _networkManager.SetEnvironment(NetworkEnvironment.Production);

            // Assert - Should not throw, event should fire
            Assert.AreEqual(1, PlayerPrefs.GetInt(PREF_KEY_OVERRIDE_ACTIVE));
        }

        #endregion

        #region Persistence Tests

        [Test]
        public void EnvironmentPersists_AcrossRestarts()
        {
            // Arrange - Set environment
            SetNetworkManagerConfig(_mockConfig);
            _networkManager.SetEnvironment(NetworkEnvironment.Staging);
            
            // Destroy and recreate (simulating restart)
            UnityEngine.Object.DestroyImmediate(_gameObject);
            _gameObject = new GameObject("NetworkManager");
            _networkManager = _gameObject.AddComponent<NetworkManager>();
            SetNetworkManagerConfig(_mockConfig);

            // Act - Initialize (simulating startup after restart)
            _networkManager.Initialize();

            // Assert
            Assert.AreEqual(NetworkEnvironment.Staging, _networkManager.CurrentEnvironment);
            Assert.IsTrue(_networkManager.HasEnvironmentOverride);
        }

        [Test]
        public void OverrideFlag_PersistsCorrectly()
        {
            // Arrange
            _networkManager.SetEnvironment(NetworkEnvironment.Development);

            // Act - Simulate restart
            UnityEngine.Object.DestroyImmediate(_gameObject);
            _gameObject = new GameObject("NetworkManager");
            _networkManager = _gameObject.AddComponent<NetworkManager>();

            // Assert
            Assert.IsTrue(_networkManager.HasEnvironmentOverride);
            Assert.AreEqual(1, PlayerPrefs.GetInt(PREF_KEY_OVERRIDE_ACTIVE));
            Assert.AreEqual((int)NetworkEnvironment.Development, PlayerPrefs.GetInt(PREF_KEY_OVERRIDE));
        }

        [Test]
        public void ClearedOverride_DoesNotPersist()
        {
            // Arrange
            _networkManager.SetEnvironment(NetworkEnvironment.Production);
            _networkManager.ClearEnvironmentOverride();

            // Act - Simulate restart
            UnityEngine.Object.DestroyImmediate(_gameObject);
            _gameObject = new GameObject("NetworkManager");
            _networkManager = _gameObject.AddComponent<NetworkManager>();

            // Assert
            Assert.IsFalse(_networkManager.HasEnvironmentOverride);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnEnvironmentChanged_CanBeSubscribedMultipleTimes()
        {
            // Arrange
            int callCount = 0;
            _networkManager.OnEnvironmentChanged += (env) => callCount++;
            _networkManager.OnEnvironmentChanged += (env) => callCount++;

            // Act
            _networkManager.SetEnvironment(NetworkEnvironment.Development);

            // Assert
            Assert.AreEqual(2, callCount, "Both subscribers should be notified");
        }

        [Test]
        public void OnEnvironmentChanged_PassesCorrectEnvironment()
        {
            // Arrange
            NetworkEnvironment? capturedEnv = null;
            _networkManager.OnEnvironmentChanged += (env) => capturedEnv = env;

            // Act
            _networkManager.SetEnvironment(NetworkEnvironment.Staging);

            // Assert
            Assert.IsNotNull(capturedEnv);
            Assert.AreEqual(NetworkEnvironment.Staging, capturedEnv.Value);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CompleteWorkflow_SetClearReinitialize()
        {
            // Arrange
            SetNetworkManagerConfig(_mockConfig);
            _networkManager.Initialize();
            var originalEnv = _networkManager.CurrentEnvironment;

            // Act 1 - Set override
            _networkManager.SetEnvironment(NetworkEnvironment.Production);
            Assert.IsTrue(_networkManager.HasEnvironmentOverride);

            // Act 2 - Clear override
            _networkManager.ClearEnvironmentOverride();
            Assert.IsFalse(_networkManager.HasEnvironmentOverride);

            // Act 3 - Reinitialize
            _networkManager.Initialize();

            // Assert
            Assert.AreEqual(originalEnv, _networkManager.CurrentEnvironment);
        }

        [Test]
        public void MultipleEnvironmentSwitches_WorkCorrectly()
        {
            // Arrange
            var environments = new[] 
            { 
                NetworkEnvironment.Local,
                NetworkEnvironment.Development, 
                NetworkEnvironment.Staging,
                NetworkEnvironment.Production,
                NetworkEnvironment.Local
            };

            // Act & Assert
            foreach (var env in environments)
            {
                _networkManager.SetEnvironment(env);
                Assert.AreEqual((int)env, PlayerPrefs.GetInt(PREF_KEY_OVERRIDE));
                Assert.IsTrue(_networkManager.HasEnvironmentOverride);
            }
        }

        [Test]
        public void CurrentEnvironment_ReflectsInitializedState()
        {
            // Arrange
            SetNetworkManagerConfig(_mockConfig);

            // Act
            _networkManager.Initialize();

            // Assert
            Assert.AreNotEqual((NetworkEnvironment)(-1), _networkManager.CurrentEnvironment);
            Assert.IsTrue(Enum.IsDefined(typeof(NetworkEnvironment), _networkManager.CurrentEnvironment));
        }

        #endregion

        #region Helper Methods

        private void SetNetworkManagerConfig(NetworkConfig config)
        {
            var managerType = typeof(NetworkManager);
            var configField = managerType.GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(_networkManager, config);
        }

        #endregion
    }
}
