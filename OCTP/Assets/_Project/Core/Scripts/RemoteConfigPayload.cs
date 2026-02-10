using System;
using System.Collections.Generic;

namespace OCTP.Core
{
    /// <summary>
    /// Remote config payload from Nakama backend.
    /// </summary>
    [Serializable]
    public class RemoteConfigPayload
    {
        /// <summary>
        /// Experiment ID for this user/session.
        /// </summary>
        public string experimentId;
        
        /// <summary>
        /// Cohort/variant assignment for user.
        /// </summary>
        public string cohort;
        
        /// <summary>
        /// Config values (key-value pairs).
        /// </summary>
        public Dictionary<string, object> config;
        
        public RemoteConfigPayload()
        {
            config = new Dictionary<string, object>();
        }
    }
}
