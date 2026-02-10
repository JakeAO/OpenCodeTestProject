using System;
using System.Collections.Generic;

namespace OCTP.Core.Analytics
{
    /// <summary>
    /// Persisted event queue (saved to disk if network unavailable).
    /// </summary>
    [Serializable]
    public class PersistedEventQueue
    {
        /// <summary>Version for future migrations</summary>
        public int version = 1;
        
        /// <summary>When queue was last persisted (Unix milliseconds)</summary>
        public long lastPersistedMs;
        
        /// <summary>Events waiting to be sent</summary>
        public List<AnalyticsEvent> events;
        
        public PersistedEventQueue()
        {
            events = new List<AnalyticsEvent>();
            lastPersistedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
