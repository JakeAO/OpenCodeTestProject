using System;
using Cysharp.Threading.Tasks;

namespace OCTP.Core
{
    /// <summary>
    /// Interface for analytics and telemetry tracking with A/B testing support.
    /// </summary>
    public interface IAnalyticsManager : IGameService
    {
        /// <summary>
        /// Fired when a batch of events is successfully sent to Nakama.
        /// </summary>
        event Action<int> OnEventBatchSent;
        
        /// <summary>
        /// Fired when event queue is persisted to disk (e.g., on app pause).
        /// </summary>
        event Action<int> OnEventQueuePersisted;
        
        /// <summary>
        /// Record an arbitrary event with optional ABT context.
        /// Thread-safe. Event is added to queue and flushed on time/count trigger.
        /// </summary>
        void RecordEvent(
            string eventName, 
            object properties = null,
            string experimentId = null,
            string cohort = null,
            float sampleRate = 1.0f);
        
        /// <summary>
        /// Manually flush pending events to Nakama (async, non-blocking).
        /// </summary>
        UniTask FlushAsync();
        
        /// <summary>
        /// Get current pending event count in queue.
        /// </summary>
        int GetPendingEventCount();
        
        /// <summary>
        /// Set experiment context for subsequent events.
        /// All future events will include this experimentId and cohort.
        /// </summary>
        void SetExperimentContext(string experimentId, string cohort);
        
        /// <summary>
        /// Clear experiment context.
        /// </summary>
        void ClearExperimentContext();
    }
}
