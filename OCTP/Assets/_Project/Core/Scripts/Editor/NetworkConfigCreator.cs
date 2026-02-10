using UnityEditor;
using UnityEngine;

namespace OCTP.Core.Editor
{
    /// <summary>
    /// Editor utility to create the default NetworkConfig asset.
    /// </summary>
    public static class NetworkConfigCreator
    {
        private const string ASSET_PATH = "Assets/_Project/Core/Resources/NetworkConfig.asset";
        
        [MenuItem("OCTP/Network/Create Default NetworkConfig")]
        public static void CreateDefaultNetworkConfig()
        {
            // Check if asset already exists
            NetworkConfig existingConfig = AssetDatabase.LoadAssetAtPath<NetworkConfig>(ASSET_PATH);
            if (existingConfig != null)
            {
                Debug.LogWarning($"[NetworkConfigCreator] NetworkConfig already exists at {ASSET_PATH}");
                Selection.activeObject = existingConfig;
                EditorGUIUtility.PingObject(existingConfig);
                return;
            }
            
            // Create new NetworkConfig instance
            NetworkConfig config = ScriptableObject.CreateInstance<NetworkConfig>();
            
            // Configure Local environment
            var localConfig = new NetworkEndpointConfig(
                url: "localhost",
                port: 7350,
                key: "defaulthttpkey",
                ssl: false
            );
            SetPrivateField(config, "_localConfig", localConfig);
            
            // Configure Development environment
            var devConfig = new NetworkEndpointConfig(
                url: "dev-nakama.yourdomain.com",
                port: 443,
                key: "dev_key_here",
                ssl: true
            );
            SetPrivateField(config, "_developmentConfig", devConfig);
            
            // Configure Staging environment
            var stagingConfig = new NetworkEndpointConfig(
                url: "staging-nakama.yourdomain.com",
                port: 443,
                key: "staging_key_here",
                ssl: true
            );
            SetPrivateField(config, "_stagingConfig", stagingConfig);
            
            // Configure Production environment
            var productionConfig = new NetworkEndpointConfig(
                url: "nakama.yourdomain.com",
                port: 443,
                key: "prod_key_here",
                ssl: true
            );
            SetPrivateField(config, "_productionConfig", productionConfig);
            
            // Ensure Resources directory exists
            string directory = System.IO.Path.GetDirectoryName(ASSET_PATH);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
            
            // Create and save the asset
            AssetDatabase.CreateAsset(config, ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the newly created asset
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            Debug.Log($"[NetworkConfigCreator] Created default NetworkConfig at {ASSET_PATH}");
        }
        
        /// <summary>
        /// Helper method to set private serialized fields using reflection.
        /// </summary>
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogError($"[NetworkConfigCreator] Could not find field: {fieldName}");
            }
        }
    }
}
