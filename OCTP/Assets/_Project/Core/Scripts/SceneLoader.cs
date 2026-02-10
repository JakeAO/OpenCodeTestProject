using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace OCTP.Core
{
    /// <summary>
    /// MVP skeleton implementation of ISceneLoader.
    /// Simulates zone loading with delays and state transitions.
    /// </summary>
    public class SceneLoader : MonoBehaviour, ISceneLoader
    {
        private IGameStateManager _gameStateManager;
        private string _currentZone;
        private bool _isLoading;
        
        public string CurrentZone => _currentZone;
        public bool IsLoading => _isLoading;
        
        public event Action<string> OnZoneLoadStarted;
        public event Action<string> OnZoneLoadCompleted;
        
        private void Awake()
        {
            _gameStateManager = ServiceLocator.Get<IGameStateManager>();
        }
        
        public async UniTask LoadZoneAsync(string zoneName, bool isExploration = true)
        {
            // 1. Check if already loading → return if true
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading. Cannot load {zoneName}");
                return;
            }
            
            // 2. Set IsLoading = true
            _isLoading = true;
            
            // 3. Fire OnZoneLoadStarted event
            OnZoneLoadStarted?.Invoke(zoneName);
            
            // 4. Set GameState to TransitionLoading
            _gameStateManager?.TrySetState(GameState.TransitionLoading);
            
            // 5. Simulate load delay (500ms via UniTask.Delay)
            await UniTask.Delay(500);
            
            // 6. Set CurrentZone to zoneName
            _currentZone = zoneName;
            
            // 7. Set GameState to Exploration or SafeZone (based on isExploration param)
            GameState targetState = isExploration ? GameState.Exploration : GameState.SafeZone;
            _gameStateManager?.TrySetState(targetState);
            
            // 8. Fire OnZoneLoadCompleted event
            OnZoneLoadCompleted?.Invoke(zoneName);
            
            // 9. Set IsLoading = false
            _isLoading = false;
        }
        
        public async UniTask UnloadCurrentZoneAsync()
        {
            // 1. Check if zone loaded → return if none
            if (string.IsNullOrEmpty(_currentZone))
            {
                Debug.LogWarning("[SceneLoader] No zone loaded to unload");
                return;
            }
            
            // 2. Set IsLoading = true
            _isLoading = true;
            
            // 3. Simulate unload delay (300ms)
            await UniTask.Delay(300);
            
            // 4. Clear CurrentZone
            _currentZone = null;
            
            // 5. Set IsLoading = false
            _isLoading = false;
        }
    }
}
