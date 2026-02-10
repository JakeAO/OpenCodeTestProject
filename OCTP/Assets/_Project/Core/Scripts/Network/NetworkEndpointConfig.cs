using UnityEngine;

namespace OCTP.Core
{
    /// <summary>
    /// Configuration data for a network endpoint.
    /// Contains all necessary connection parameters for a Nakama server environment.
    /// </summary>
    [System.Serializable]
    public class NetworkEndpointConfig
    {
        /// <summary>
        /// The server URL or IP address (without protocol).
        /// Example: "localhost", "dev-nakama.yourdomain.com"
        /// </summary>
        [Tooltip("Server URL or IP address (without http:// or https://)")]
        public string serverUrl;
        
        /// <summary>
        /// The port number for the server connection.
        /// Default: 7350 (Nakama default), HTTPS typically uses 443.
        /// </summary>
        [Tooltip("Server port number (7350 for local, 443 for HTTPS)")]
        public int serverPort;
        
        /// <summary>
        /// The HTTP key used for Nakama authentication.
        /// This key is configured in the Nakama server settings.
        /// </summary>
        [Tooltip("HTTP key for Nakama authentication")]
        public string httpKey;
        
        /// <summary>
        /// Whether to use SSL/TLS for secure connections.
        /// Should be true for all non-local environments in production.
        /// </summary>
        [Tooltip("Enable SSL/TLS for secure connection (HTTPS)")]
        public bool useSSL;
        
        /// <summary>
        /// Creates a new NetworkEndpointConfig with default values.
        /// </summary>
        /// <param name="url">Server URL (default: "localhost")</param>
        /// <param name="port">Server port (default: 7350)</param>
        /// <param name="key">HTTP authentication key (default: "defaulthttpkey")</param>
        /// <param name="ssl">Use SSL/TLS (default: false)</param>
        public NetworkEndpointConfig(
            string url = "localhost", 
            int port = 7350, 
            string key = "defaulthttpkey", 
            bool ssl = false)
        {
            serverUrl = url;
            serverPort = port;
            httpKey = key;
            useSSL = ssl;
        }
        
        /// <summary>
        /// Validates the configuration and logs warnings for invalid values.
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise.</returns>
        public bool Validate()
        {
            bool isValid = true;
            
            if (string.IsNullOrEmpty(serverUrl))
            {
                Debug.LogWarning("[NetworkEndpointConfig] Server URL is empty!");
                isValid = false;
            }
            
            if (serverPort <= 0 || serverPort > 65535)
            {
                Debug.LogWarning($"[NetworkEndpointConfig] Invalid port: {serverPort}. Must be between 1 and 65535.");
                isValid = false;
            }
            
            if (string.IsNullOrEmpty(httpKey))
            {
                Debug.LogWarning("[NetworkEndpointConfig] HTTP key is empty!");
                isValid = false;
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Returns a string representation of this configuration for debugging.
        /// The HTTP key is partially masked for security.
        /// </summary>
        /// <returns>A formatted string with connection details.</returns>
        public override string ToString()
        {
            string protocol = useSSL ? "https" : "http";
            string maskedKey = MaskKey(httpKey);
            return $"{protocol}://{serverUrl}:{serverPort} [Key: {maskedKey}]";
        }
        
        /// <summary>
        /// Masks sensitive key information for safe logging.
        /// Shows only first few and last few characters.
        /// </summary>
        private string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 6)
                return "***";
            
            int visibleChars = Mathf.Min(4, key.Length - 2);
            return key.Substring(0, visibleChars) + "***" + key.Substring(key.Length - 2);
        }
    }
}
