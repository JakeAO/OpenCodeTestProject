using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;
using OCTP.Core.Analytics;
using Nakama;

namespace OCTP.Core
{
    /// <summary>
    /// Analytics manager implementation with Nakama integration.
    /// Records events to in-memory queue, flushes to disk periodically,
    /// and sends to Nakama backend asynchronously.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour, IAnalyticsManager
    {
        public event Action<int> OnEventBatchSent;
        public event Action<int> OnEventQueuePersisted;
        
        private Queue<AnalyticsEvent> _eventQueue;
        private readonly object _queueLock = new object();
        
        private string _sessionId;
        private string _playerId;
        private string _experimentId;
        private string _cohort;
        
        private const float FLUSH_INTERVAL = 30f;
        private const int BATCH_SIZE_LIMIT = 100;
        private const int MAX_QUEUE_SIZE_EVENTS = 1000;
        
        private string _persistedQueuePath;
        private bool _isShuttingDown = false;
        private bool _isInitialized = false;
        
        private void Awake()
        {
            _sessionId = Guid.NewGuid().ToString();
            _eventQueue = new Queue<AnalyticsEvent>();
            _persistedQueuePath = Path.Combine(Application.persistentDataPath, "analytics_queue.json");
            
            LoadPersistedQueue();
            _isInitialized = true;
            
            // Start background flush coroutine
            StartCoroutine(BackgroundFlushCoroutine());
        }
        
        private IEnumerator BackgroundFlushCoroutine()
        {
            while (!_isShuttingDown)
            {
                yield return new WaitForSeconds(FLUSH_INTERVAL);
                
                if (GetPendingEventCount() > 0)
                {
                    FlushAsync().Forget();
                }
            }
        }
        
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                PersistQueueToDisk();
            }
        }
        
        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
            PersistQueueToDisk();
        }
        
        public void RecordEvent(
            string eventName,
            object properties = null,
            string experimentId = null,
            string cohort = null,
            float sampleRate = 1.0f)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[Analytics] RecordEvent called before initialization");
                return;
            }
            
            // Validate event name
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[Analytics] Event name is null or empty");
                return;
            }
            
            // Sample rate check
            if (UnityEngine.Random.value > sampleRate)
                return;
            
            // Convert properties to Dictionary
            Dictionary<string, object> propsDict = null;
            if (properties != null)
            {
                propsDict = ConvertObjectToDict(properties);
            }
            
            var evt = new AnalyticsEvent(eventName)
            {
                sessionId = _sessionId,
                playerId = _playerId,
                experimentId = experimentId ?? _experimentId,
                cohort = cohort ?? _cohort,
                properties = propsDict ?? new Dictionary<string, object>()
            };
            
            lock (_queueLock)
            {
                // Check queue size limit
                if (_eventQueue.Count >= MAX_QUEUE_SIZE_EVENTS)
                {
                    Debug.LogWarning($"[Analytics] Queue full ({MAX_QUEUE_SIZE_EVENTS} events), dropping oldest event");
                    _eventQueue.Dequeue();
                }
                
                _eventQueue.Enqueue(evt);
            }
            
            // Auto-flush if batch size reached
            if (GetPendingEventCount() >= BATCH_SIZE_LIMIT)
            {
                FlushAsync().Forget();
            }
        }
        
        public async UniTask FlushAsync()
        {
            if (_isShuttingDown)
                return;
            
            List<AnalyticsEvent> eventsToSend = new List<AnalyticsEvent>();
            
            lock (_queueLock)
            {
                while (_eventQueue.Count > 0 && eventsToSend.Count < BATCH_SIZE_LIMIT)
                {
                    eventsToSend.Add(_eventQueue.Dequeue());
                }
            }
            
            if (eventsToSend.Count == 0)
                return;
            
            var batch = new EventBatch
            {
                events = eventsToSend,
                userId = _playerId,
                sessionId = _sessionId
            };
            
            try
            {
                await SendBatchToNakama(batch);
                OnEventBatchSent?.Invoke(eventsToSend.Count);
                
                #if DEBUG_ANALYTICS
                Debug.Log($"[Analytics] Sent {eventsToSend.Count} events to Nakama");
                #endif
            }
            catch (Exception ex)
            {
                // Network error: re-queue events for retry
                Debug.LogWarning($"[Analytics] Send failed: {ex.Message}. Events re-queued.");
                
                lock (_queueLock)
                {
                    // Re-enqueue events (push to front of queue)
                    var tempQueue = new Queue<AnalyticsEvent>(eventsToSend);
                    foreach (var evt in _eventQueue)
                    {
                        tempQueue.Enqueue(evt);
                    }
                    _eventQueue = tempQueue;
                }
            }
        }
        
        public int GetPendingEventCount()
        {
            lock (_queueLock)
            {
                return _eventQueue.Count;
            }
        }
        
        public void SetExperimentContext(string experimentId, string cohort)
        {
            _experimentId = experimentId;
            _cohort = cohort;
            
            #if DEBUG_ANALYTICS
            Debug.Log($"[Analytics] Experiment context set: {experimentId} / {cohort}");
            #endif
        }
        
        public void ClearExperimentContext()
        {
            _experimentId = null;
            _cohort = null;
            
            #if DEBUG_ANALYTICS
            Debug.Log("[Analytics] Experiment context cleared");
            #endif
        }
        
        private async UniTask SendBatchToNakama(EventBatch batch)
        {
            var networkMgr = ServiceLocator.Get<INetworkManager>();
            
            var client = new Client(
                networkMgr.UseSSL ? "https" : "http",
                networkMgr.ServerUrl,
                networkMgr.ServerPort,
                networkMgr.HttpKey
            );
            
            var session = await client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier).AsUniTask();
            string jsonPayload = JsonUtility.ToJson(batch);
            await client.RpcAsync(session, "AnalyticsCollectEvents", jsonPayload).AsUniTask();
            
            #if DEBUG_ANALYTICS
            Debug.Log($"[Analytics] Sent batch {batch.batchId} with {batch.events.Count} events to Nakama");
            #endif
        }
        
        private void PersistQueueToDisk()
        {
            if (!_isInitialized)
                return;
            
            lock (_queueLock)
            {
                if (_eventQueue.Count == 0)
                    return;
                
                try
                {
                    var persistedQueue = new PersistedEventQueue
                    {
                        events = new List<AnalyticsEvent>(_eventQueue)
                    };
                    
                    string json = JsonUtility.ToJson(persistedQueue);
                    File.WriteAllText(_persistedQueuePath, json);
                    
                    OnEventQueuePersisted?.Invoke(_eventQueue.Count);
                    Debug.Log($"[Analytics] Persisted {_eventQueue.Count} events to disk");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Analytics] Failed to persist queue: {ex}");
                }
            }
        }
        
        private void LoadPersistedQueue()
        {
            if (!File.Exists(_persistedQueuePath))
                return;
            
            try
            {
                string json = File.ReadAllText(_persistedQueuePath);
                var persistedQueue = JsonUtility.FromJson<PersistedEventQueue>(json);
                
                lock (_queueLock)
                {
                    foreach (var evt in persistedQueue.events)
                    {
                        _eventQueue.Enqueue(evt);
                    }
                }
                
                Debug.Log($"[Analytics] Loaded {persistedQueue.events.Count} events from disk");
                
                // Delete persisted file (will be recreated if needed)
                File.Delete(_persistedQueuePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Analytics] Failed to load persisted queue: {ex}");
            }
        }
        
        private Dictionary<string, object> ConvertObjectToDict(object obj)
        {
            if (obj is Dictionary<string, object> dict)
            {
                return dict;
            }
            
            var result = new Dictionary<string, object>();
            
            if (obj != null)
            {
                var type = obj.GetType();
                var properties = type.GetProperties();
                
                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(obj);
                        result[prop.Name] = value;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Analytics] Failed to read property {prop.Name}: {ex.Message}");
                    }
                }
            }
            
            return result;
        }
    }
}
