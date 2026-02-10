using System;
using System.Collections.Generic;

namespace OCTP.Core.Analytics
{
    /// <summary>
    /// Batch of events sent to Nakama backend.
    /// </summary>
    [Serializable]
    public class EventBatch
    {
        /// <summary>Batch ID (unique per flush)</summary>
        public string batchId;
        
        /// <summary>Timestamp when batch was created (Unix milliseconds)</summary>
        public long createdAtMs;
        
        /// <summary>Events in this batch</summary>
        public List<AnalyticsEvent> events;
        
        /// <summary>User ID who created this batch</summary>
        public string userId;
        
        /// <summary>Session ID when batch was created</summary>
        public string sessionId;
        
        public EventBatch()
        {
            batchId = Guid.NewGuid().ToString();
            createdAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            events = new List<AnalyticsEvent>();
        }
    }
}
