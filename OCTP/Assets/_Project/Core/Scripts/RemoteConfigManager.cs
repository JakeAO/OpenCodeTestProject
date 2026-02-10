using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Nakama;

namespace OCTP.Core
{
    /// <summary>
    /// Remote configuration manager implementation.
    /// Loads configs from Nakama at startup, caches locally, provides fallback to defaults.
    /// </summary>
    public class RemoteConfigManager : MonoBehaviour, IRemoteConfigManager
    {
        public event Action OnConfigLoaded;
        
        private Dictionary<string, object> _configCache;
        private readonly object _cacheLock = new object();
        
        private string _experimentId;
        private string _cohort;
        private bool _isLoaded;
        
        public bool IsLoaded => _isLoaded;
        
        private void Awake()
        {
            _configCache = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Load remote config from Nakama.
        /// </summary>
        public async UniTask LoadFromNakamaAsync()
        {
            try
            {
                Debug.Log("[RemoteConfig] Loading from Nakama...");
                
                // Call Nakama RPC endpoint to fetch config
                var payload = await FetchConfigFromNakama();
                
                if (payload != null && payload.config != null)
                {
                    lock (_cacheLock)
                    {
                        _configCache = new Dictionary<string, object>(payload.config);
                        _experimentId = payload.experimentId;
                        _cohort = payload.cohort;
                    }
                    
                    _isLoaded = true;
                    OnConfigLoaded?.Invoke();
                    Debug.Log($"[RemoteConfig] Loaded {_configCache.Count} config keys, experiment: {_experimentId}, cohort: {_cohort}");
                }
                else
                {
                    _isLoaded = true;
                    Debug.LogWarning("[RemoteConfig] Nakama config fetch returned null, using defaults only");
                }
            }
            catch (Exception ex)
            {
                // Network error: continue with empty cache (will use fallbacks)
                _isLoaded = true;
                Debug.LogWarning($"[RemoteConfig] Failed to load from Nakama: {ex.Message}. Using defaults only.");
            }
        }
        
        public T Get<T>(string key, T defaultValue)
        {
            if (!_isLoaded)
            {
                Debug.LogWarning($"[RemoteConfig] Config not yet loaded, returning default for key '{key}'");
                return defaultValue;
            }
            
            lock (_cacheLock)
            {
                // Handle hierarchical keys (dot notation)
                if (TryGetNestedValue(key, out object value))
                {
                    return ConvertValue<T>(value, defaultValue);
                }
            }
            
            return defaultValue;
        }
        
        public bool HasKey(string key)
        {
            lock (_cacheLock)
            {
                // Check both direct and nested keys
                if (_configCache.ContainsKey(key))
                    return true;
                
                return TryGetNestedValue(key, out _);
            }
        }
        
        public int GetLoadedKeyCount()
        {
            lock (_cacheLock)
            {
                return _configCache.Count;
            }
        }
        
        public string GetExperimentId() => _experimentId;
        
        public string GetCohort() => _cohort;
        
        private bool TryGetNestedValue(string key, out object value)
        {
            value = null;
            
            // First try direct key
            if (_configCache.TryGetValue(key, out value))
                return true;
            
            // Try nested key with dot notation (e.g., "balance.player_health")
            if (!key.Contains("."))
                return false;
            
            var parts = key.Split('.');
            object current = _configCache;
            
            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue(part, out current))
                        continue;
                    return false;
                }
                return false;
            }
            
            value = current;
            return true;
        }
        
        private T ConvertValue<T>(object value, T defaultValue)
        {
            if (value == null)
                return defaultValue;
            
            // Handle type conversions
            if (value is T typedValue)
                return typedValue;
            
            var targetType = typeof(T);
            
            // Try Convert.ChangeType for numeric/primitive types
            try
            {
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                Debug.LogWarning($"[RemoteConfig] Failed to convert {value} ({value?.GetType()}) to {targetType}");
                return defaultValue;
            }
        }
        
        private async UniTask<RemoteConfigPayload> FetchConfigFromNakama()
        {
            try
            {
                var networkMgr = ServiceLocator.Get<INetworkManager>();
                
                var client = new Client(
                    networkMgr.UseSSL ? "https" : "http",
                    networkMgr.ServerUrl,
                    networkMgr.ServerPort,
                    networkMgr.HttpKey
                );
                
                var session = await client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier).AsUniTask();
                var response = await client.RpcAsync(session, "FetchRemoteConfig", "").AsUniTask();
                
                return JsonUtility.FromJson<RemoteConfigPayload>(response.Payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RemoteConfig] Failed to fetch from Nakama: {ex.GetType().Name} -> {ex.Message}");
                
                // Return null to trigger fallback to defaults
                return null;
            }
        }
    }
}
