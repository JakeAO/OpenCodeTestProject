using System;
using Cysharp.Threading.Tasks;

namespace OCTP.Core
{
    /// <summary>
    /// Interface for remote configuration management.
    /// </summary>
    public interface IRemoteConfigManager : IGameService
    {
        /// <summary>
        /// Load config from Nakama backend asynchronously.
        /// </summary>
        UniTask LoadFromNakamaAsync();
        
        /// <summary>
        /// Get a typed config value by key with fallback default.
        /// </summary>
        T Get<T>(string key, T defaultValue);
        
        /// <summary>
        /// Check if config key exists in cache.
        /// </summary>
        bool HasKey(string key);
        
        /// <summary>
        /// Get number of loaded config keys.
        /// </summary>
        int GetLoadedKeyCount();
        
        /// <summary>
        /// Get user's assigned experiment ID.
        /// </summary>
        string GetExperimentId();
        
        /// <summary>
        /// Get user's assigned cohort/variant.
        /// </summary>
        string GetCohort();
        
        /// <summary>
        /// Returns true if config has been loaded.
        /// </summary>
        bool IsLoaded { get; }
        
        /// <summary>
        /// Fired when remote config is loaded from Nakama.
        /// </summary>
        event Action OnConfigLoaded;
    }
}
