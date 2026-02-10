using System;
using Cysharp.Threading.Tasks;

namespace OCTP.Core
{
    /// <summary>
    /// Interface for scene loading and management.
    /// </summary>
    public interface ISceneLoader : IGameService
    {
        /// <summary>
        /// Loads a zone asynchronously.
        /// </summary>
        /// <param name="zoneName">Name of the zone to load</param>
        /// <param name="isExploration">Whether this is an exploration zone (true) or safe zone (false)</param>
        UniTask LoadZoneAsync(string zoneName, bool isExploration = true);
        
        /// <summary>
        /// Unloads the currently loaded zone.
        /// </summary>
        UniTask UnloadCurrentZoneAsync();
        
        /// <summary>
        /// Gets the name of the currently loaded zone.
        /// </summary>
        string CurrentZone { get; }
        
        /// <summary>
        /// Gets whether a zone is currently being loaded or unloaded.
        /// </summary>
        bool IsLoading { get; }
        
        /// <summary>
        /// Fired when zone loading starts.
        /// </summary>
        event Action<string> OnZoneLoadStarted;
        
        /// <summary>
        /// Fired when zone loading completes.
        /// </summary>
        event Action<string> OnZoneLoadCompleted;
    }
}
