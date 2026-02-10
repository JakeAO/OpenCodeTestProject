namespace OCTP.Core
{
    /// <summary>
    /// Defines the available network environments for connecting to the Nakama backend server.
    /// Used to switch between local development, team development, staging, and production servers.
    /// </summary>
    public enum NetworkEnvironment
    {
        /// <summary>
        /// Local development server running on localhost:7350.
        /// Used for individual developer testing without network latency.
        /// </summary>
        Local = 0,
        
        /// <summary>
        /// Shared development server for team collaboration and testing.
        /// Typically hosted on a cloud server accessible to the development team.
        /// </summary>
        Development = 1,
        
        /// <summary>
        /// Staging environment that mirrors production configuration.
        /// Used for pre-release testing and validation before deploying to production.
        /// </summary>
        Staging = 2,
        
        /// <summary>
        /// Production server for live gameplay.
        /// This environment serves real players and should have the highest security and stability.
        /// </summary>
        Production = 3
    }
}
