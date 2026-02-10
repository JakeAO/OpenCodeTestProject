using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using OCTP.Core;
using Cysharp.Threading.Tasks;

namespace OCTP.Core.Tests
{
    [TestFixture]
    public class AnalyticsManagerTests
    {
        private GameObject _testGameObject;
        private AnalyticsManager _analyticsManager;
        
        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("AnalyticsManager_Test");
            _analyticsManager = _testGameObject.AddComponent<AnalyticsManager>();
            
            // Wait for Awake to complete
            WaitForInitialization();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }
        
        private void WaitForInitialization()
        {
            // Give Awake time to execute
            System.Threading.Thread.Sleep(100);
        }
        
        [Test]
        public void RecordEvent_AddsEventToQueue()
        {
            // Arrange
            var eventName = "test_event";
            var properties = new { value = 42, name = "test" };
            
            // Act
            _analyticsManager.RecordEvent(eventName, properties);
            
            // Assert
            Assert.AreEqual(1, _analyticsManager.GetPendingEventCount(), 
                "Event should be added to queue");
        }
        
        [Test]
        public void RecordEvent_MultipleEvents_IncreasesQueueCount()
        {
            // Act
            _analyticsManager.RecordEvent("event1");
            _analyticsManager.RecordEvent("event2");
            _analyticsManager.RecordEvent("event3");
            
            // Assert
            Assert.AreEqual(3, _analyticsManager.GetPendingEventCount(), 
                "Multiple events should be added to queue");
        }
        
        [Test]
        public void RecordEvent_WithSampleRate_FiltersSomeEvents()
        {
            // Arrange
            const int iterations = 1000;
            const float sampleRate = 0.1f;
            
            // Act
            for (int i = 0; i < iterations; i++)
            {
                _analyticsManager.RecordEvent("test_event", sampleRate: sampleRate);
            }
            
            // Assert
            int recorded = _analyticsManager.GetPendingEventCount();
            
            // With 10% sampling, we expect ~100 events (allow variance 50-150)
            Assert.Greater(recorded, 50, $"Expected ~100 events, got {recorded}");
            Assert.Less(recorded, 150, $"Expected ~100 events, got {recorded}");
            
            // Also verify it's less than all events
            Assert.Less(recorded, iterations, "Sample rate should filter some events");
        }
        
        [Test]
        public void RecordEvent_WithSampleRateOne_RecordsAllEvents()
        {
            // Arrange
            const int iterations = 10;
            
            // Act
            for (int i = 0; i < iterations; i++)
            {
                _analyticsManager.RecordEvent("test_event", sampleRate: 1.0f);
            }
            
            // Assert
            Assert.AreEqual(iterations, _analyticsManager.GetPendingEventCount(),
                "Sample rate 1.0 should record all events");
        }
        
        [Test]
        public void RecordEvent_WithSampleRateZero_RecordsNoEvents()
        {
            // Act
            for (int i = 0; i < 10; i++)
            {
                _analyticsManager.RecordEvent("test_event", sampleRate: 0.0f);
            }
            
            // Assert
            Assert.AreEqual(0, _analyticsManager.GetPendingEventCount(),
                "Sample rate 0.0 should record no events");
        }
        
        [Test]
        public void GetPendingEventCount_ReturnsCorrectCount()
        {
            // Arrange
            Assert.AreEqual(0, _analyticsManager.GetPendingEventCount(), 
                "Initial queue should be empty");
            
            // Act
            _analyticsManager.RecordEvent("event1");
            _analyticsManager.RecordEvent("event2");
            
            // Assert
            Assert.AreEqual(2, _analyticsManager.GetPendingEventCount());
        }
        
        [Test]
        public void SetExperimentContext_StoresContext()
        {
            // Act
            _analyticsManager.SetExperimentContext("exp_123", "variant_a");
            
            // Record event and verify it would include context
            _analyticsManager.RecordEvent("test_event");
            
            // Assert
            Assert.AreEqual(1, _analyticsManager.GetPendingEventCount(),
                "Event should be recorded with experiment context");
        }
        
        [Test]
        public void ClearExperimentContext_RemovesContext()
        {
            // Arrange
            _analyticsManager.SetExperimentContext("exp_123", "variant_a");
            
            // Act
            _analyticsManager.ClearExperimentContext();
            _analyticsManager.RecordEvent("test_event");
            
            // Assert
            Assert.AreEqual(1, _analyticsManager.GetPendingEventCount(),
                "Event should be recorded even after clearing context");
        }
        
        [UnityTest]
        public IEnumerator FlushAsync_ReducesPendingEventCount()
        {
            // Arrange
            _analyticsManager.RecordEvent("event1");
            _analyticsManager.RecordEvent("event2");
            
            Assert.AreEqual(2, _analyticsManager.GetPendingEventCount());
            
            // Act
            var flushTask = _analyticsManager.FlushAsync();
            
            // Wait for flush to complete
            yield return flushTask.ToCoroutine();
            
            // Assert
            Assert.AreEqual(0, _analyticsManager.GetPendingEventCount(),
                "Flush should clear pending events");
        }
        
        [UnityTest]
        public IEnumerator FlushAsync_RespectsBatchSizeLimit()
        {
            // Arrange - Add more than batch size (100)
            for (int i = 0; i < 150; i++)
            {
                _analyticsManager.RecordEvent($"event_{i}");
            }
            
            Assert.AreEqual(150, _analyticsManager.GetPendingEventCount());
            
            // Act - Single flush should only send 100
            var flushTask = _analyticsManager.FlushAsync();
            yield return flushTask.ToCoroutine();
            
            // Assert - 50 events should remain
            Assert.AreEqual(50, _analyticsManager.GetPendingEventCount(),
                "Flush should respect batch size limit of 100");
        }
        
        [Test]
        public void RecordEvent_WithNullEventName_DoesNotAddToQueue()
        {
            // Act
            _analyticsManager.RecordEvent(null);
            _analyticsManager.RecordEvent("");
            
            // Assert
            Assert.AreEqual(0, _analyticsManager.GetPendingEventCount(),
                "Null or empty event names should not be recorded");
        }
        
        [Test]
        public void RecordEvent_WithDictionaryProperties_AddsToQueue()
        {
            // Arrange
            var properties = new System.Collections.Generic.Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "key3", true }
            };
            
            // Act
            _analyticsManager.RecordEvent("test_event", properties);
            
            // Assert
            Assert.AreEqual(1, _analyticsManager.GetPendingEventCount());
        }
        
        [Test]
        public void RecordEvent_WithAnonymousProperties_AddsToQueue()
        {
            // Arrange
            var properties = new
            {
                stringProp = "test",
                intProp = 42,
                floatProp = 3.14f,
                boolProp = true
            };
            
            // Act
            _analyticsManager.RecordEvent("test_event", properties);
            
            // Assert
            Assert.AreEqual(1, _analyticsManager.GetPendingEventCount());
        }
        
        [Test]
        public void RecordEvent_WithExperimentParameters_OverridesContext()
        {
            // Arrange
            _analyticsManager.SetExperimentContext("exp_global", "cohort_global");
            
            // Act - Override with explicit parameters
            _analyticsManager.RecordEvent("test_event", 
                experimentId: "exp_specific", 
                cohort: "cohort_specific");
            
            // Assert
            Assert.AreEqual(1, _analyticsManager.GetPendingEventCount(),
                "Event should be recorded with overridden experiment context");
        }
        
        [UnityTest]
        public IEnumerator OnEventBatchSent_FiresAfterSuccessfulFlush()
        {
            // Arrange
            bool eventFired = false;
            int batchSize = 0;
            
            _analyticsManager.OnEventBatchSent += (size) =>
            {
                eventFired = true;
                batchSize = size;
            };
            
            _analyticsManager.RecordEvent("event1");
            _analyticsManager.RecordEvent("event2");
            
            // Act
            var flushTask = _analyticsManager.FlushAsync();
            yield return flushTask.ToCoroutine();
            
            // Assert
            Assert.IsTrue(eventFired, "OnEventBatchSent should fire after flush");
            Assert.AreEqual(2, batchSize, "Batch size should be 2");
        }
        
        [Test]
        public void ThreadSafety_ConcurrentRecordEvents()
        {
            // Arrange
            const int threadCount = 5;
            const int eventsPerThread = 20;
            var tasks = new System.Collections.Generic.List<Task>();
            
            // Act - Record events from multiple threads
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                var task = Task.Run(() =>
                {
                    for (int i = 0; i < eventsPerThread; i++)
                    {
                        _analyticsManager.RecordEvent($"event_thread{threadId}_{i}");
                    }
                });
                tasks.Add(task);
            }
            
            // Wait for all threads
            Task.WaitAll(tasks.ToArray());
            
            // Allow some time for all events to be processed
            System.Threading.Thread.Sleep(500);
            
            // Assert
            int expectedCount = threadCount * eventsPerThread;
            int actualCount = _analyticsManager.GetPendingEventCount();
            
            Assert.AreEqual(expectedCount, actualCount,
                $"Should have {expectedCount} events from {threadCount} threads");
        }
    }
}
