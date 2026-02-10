using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCTP.Core.Analytics
{
    /// <summary>
    /// Single analytics event with timestamp, properties, and A/B test context.
    /// </summary>
    [Serializable]
    public class AnalyticsEvent
    {
        /// <summary>Event name (e.g., "enemy_defeated", "ability_used")</summary>
        public string eventName;
        
        /// <summary>Arbitrary event properties (key-value pairs)</summary>
        public Dictionary<string, object> properties;
        
        /// <summary>Experiment ID (null if not in experiment)</summary>
        public string experimentId;
        
        /// <summary>Variant/cohort assignment (null if not in experiment)</summary>
        public string cohort;
        
        /// <summary>Client timestamp (Unix milliseconds since epoch)</summary>
        public long clientTimestamp;
        
        /// <summary>Session ID (persists for duration of app session)</summary>
        public string sessionId;
        
        /// <summary>Platform identifier (Unity_Windows, Unity_iOS, etc.)</summary>
        public string platform;
        
        /// <summary>Game version (from Application.version)</summary>
        public string gameVersion;
        
        /// <summary>Player ID (from Nakama auth, null if not authenticated)</summary>
        public string playerId;
        
        public AnalyticsEvent() 
        {
            properties = new Dictionary<string, object>();
        }
        
        public AnalyticsEvent(string eventName)
        {
            this.eventName = eventName;
            properties = new Dictionary<string, object>();
            clientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            platform = GetPlatformString();
            gameVersion = Application.version;
        }
        
        private string GetPlatformString()
        {
            #if UNITY_EDITOR
            return "Unity_Editor";
            #elif UNITY_STANDALONE_WIN
            return "Unity_Windows";
            #elif UNITY_STANDALONE_OSX
            return "Unity_macOS";
            #elif UNITY_STANDALONE_LINUX
            return "Unity_Linux";
            #elif UNITY_IOS
            return "Unity_iOS";
            #elif UNITY_ANDROID
            return "Unity_Android";
            #elif UNITY_WEBGL
            return "Unity_WebGL";
            #else
            return "Unknown";
            #endif
        }
    }
}
