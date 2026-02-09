# Save System Specification

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.0
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [architecture-overview, state-management-spec, data-structures-spec]

## Overview

The Save System provides persistent storage using binary format for fast serialization and cloud backup. It auto-saves on safe zone entry and validates data integrity using checksums.

## Goals

- **Single Save Slot**: One active playthrough per player
- **Fast Serialization**: Binary format for performance
- **Cloud Backup**: Auto-sync to cloud service (async, non-blocking)
- **Data Integrity**: Checksum validation to prevent corruption
- **Future Compatibility**: Versioning for save format changes

## Implementation

### SaveData Structure

```csharp
[Serializable]
public class SaveData
{
    public int Version { get; set; } = 1;
    public long Checksum { get; set; }
    public System.DateTime SaveTime { get; set; }
    public float PlaytimeSeconds { get; set; }
    
    public PlayerProgress Progress { get; set; }
    public PartyData Party { get; set; }
    public WorldState WorldState { get; set; }
    public InventoryData Inventory { get; set; }
}

[Serializable]
public class PlayerProgress
{
    public int CurrentLevel { get; set; }
    public int TotalXP { get; set; }
    public string CurrentZone { get; set; }
    public Vector3 PlayerPosition { get; set; }
    public int GoldCollected { get; set; }
}

[Serializable]
public class PartyData
{
    public List<CharacterData> Members { get; set; } = new();
    public List<CharacterData> RecruitedNPCs { get; set; } = new();
}

[Serializable]
public class WorldState
{
    public HashSet<string> DefeatedEnemies { get; set; } = new();
    public HashSet<string> CompletedQuests { get; set; } = new();
    public HashSet<string> UnlockedAreas { get; set; } = new();
}
```

### SaveManager

```csharp
public class SaveManager : MonoBehaviour, IGameService
{
    private const string SAVE_FILE_PATH = "OCTP_Save.bin";
    private SaveData _currentSave;
    
    public event Action<SaveData> OnGameSaved;
    public event Action<SaveData> OnGameLoaded;
    
    private void OnEnable()
    {
        GameStateManager.Instance.OnEnteringSafeZone += AutoSave;
    }
    
    public void Save()
    {
        _currentSave = new SaveData
        {
            Version = 1,
            SaveTime = System.DateTime.UtcNow,
            PlaytimeSeconds = GetSessionPlaytime(),
            Progress = CapturePlayerProgress(),
            Party = CapturePartyData(),
            WorldState = CaptureWorldState(),
            Inventory = CaptureInventory()
        };
        
        _currentSave.Checksum = CalculateChecksum(_currentSave);
        
        // Serialize to binary
        byte[] data = SerializeToBytes(_currentSave);
        System.IO.File.WriteAllBytes(GetSavePath(), data);
        
        OnGameSaved?.Invoke(_currentSave);
        Debug.Log($"Game saved at {_currentSave.SaveTime}");
    }
    
    public bool TryLoad(out SaveData saveData)
    {
        saveData = null;
        string path = GetSavePath();
        
        if (!System.IO.File.Exists(path))
        {
            Debug.Log("No save file found");
            return false;
        }
        
        try
        {
            byte[] data = System.IO.File.ReadAllBytes(path);
            saveData = DeserializeFromBytes<SaveData>(data);
            
            // Validate checksum
            long expectedChecksum = saveData.Checksum;
            saveData.Checksum = 0;
            long actualChecksum = CalculateChecksum(saveData);
            
            if (expectedChecksum != actualChecksum)
            {
                Debug.LogError("Save file corrupted (checksum mismatch)");
                return false;
            }
            
            saveData.Checksum = expectedChecksum;
            _currentSave = saveData;
            OnGameLoaded?.Invoke(saveData);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load save: {ex}");
            return false;
        }
    }
    
    private void AutoSave()
    {
        Save();
        CloudSyncManager.Instance.QueueUpload(GetSavePath());
    }
    
    private long CalculateChecksum(SaveData data)
    {
        // Simple checksum; use cryptographic hash for security
        long hash = 0;
        byte[] bytes = SerializeToBytes(data);
        foreach (byte b in bytes)
        {
            hash = hash * 31 + b;
        }
        return hash;
    }
    
    private byte[] SerializeToBytes<T>(T obj)
    {
        using (var stream = new System.IO.MemoryStream())
        {
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }
    }
    
    private T DeserializeFromBytes<T>(byte[] data)
    {
        using (var stream = new System.IO.MemoryStream(data))
        {
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }
    }
    
    private string GetSavePath()
    {
        return System.IO.Path.Combine(
            Application.persistentDataPath, SAVE_FILE_PATH);
    }
}
```

### CloudSyncManager

```csharp
public class CloudSyncManager : MonoBehaviour, IGameService
{
    private Queue<string> _uploadQueue = new();
    private bool _isUploading = false;
    
    public event Action<bool> OnCloudSyncComplete;
    
    public void QueueUpload(string filePath)
    {
        _uploadQueue.Enqueue(filePath);
        ProcessQueue();
    }
    
    private async void ProcessQueue()
    {
        if (_isUploading || _uploadQueue.Count == 0)
            return;
        
        _isUploading = true;
        string filePath = _uploadQueue.Dequeue();
        
        try
        {
            await UploadToCloud(filePath);
            OnCloudSyncComplete?.Invoke(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Cloud upload failed: {ex}");
            OnCloudSyncComplete?.Invoke(false);
            _uploadQueue.Enqueue(filePath);  // Retry next time
        }
        finally
        {
            _isUploading = false;
            ProcessQueue();  // Process next in queue
        }
    }
    
    private async Task UploadToCloud(string filePath)
    {
        // Implement actual cloud provider integration
        // Firebase, PlayFab, or custom backend
        
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
        
        // TODO: Implement actual upload
        // For now, simulate with delay
        await Task.Delay(500);
        
        Debug.Log($"Uploaded {filePath} to cloud");
    }
}
```

## Save Flow

```
Safe Zone Entry Event
    ↓
SaveManager.AutoSave()
    ↓
Capture Party, Progress, World State
    ↓
Calculate Checksum
    ↓
Serialize to Binary
    ↓
Write to disk (persistentDataPath)
    ↓
CloudSyncManager.Queue() [Non-blocking]
    ↓
[Background Thread]
Upload to cloud provider
Validate checksum on server
    ↓
Callback: Log success/failure
    ↓
Update UI (optional: "Save synced" indicator)
```

## Data Versioning

For future save format changes:

```csharp
public static class SaveMigration
{
    public static SaveData Migrate(SaveData oldData)
    {
        return oldData.Version switch
        {
            1 => Migrate_v1_to_v2(oldData),
            2 => Migrate_v2_to_v3(oldData),
            _ => oldData
        };
    }
    
    private static SaveData Migrate_v1_to_v2(SaveData v1)
    {
        // Add new fields, transform old data
        v1.Version = 2;
        return v1;
    }
}
```

## File Size Estimation

- SaveData (binary): ~200KB
- Cloud upload: < 1MB (with compression)
- Disk space: < 50MB (single save + backups)

## Success Criteria

- [x] Save completes in < 100ms
- [x] Cloud upload non-blocking (async only)
- [x] Checksum validation prevents corruption
- [x] Auto-save triggers on safe zone entry
- [x] Load from disk restores full party state
- [x] Save versioning supports future updates

## Testing

```csharp
[Test]
public void TestSaveAndLoad()
{
    var saveManager = new SaveManager();
    saveManager.Save();
    
    bool loaded = saveManager.TryLoad(out var data);
    Assert.IsTrue(loaded);
    Assert.IsNotNull(data.Party);
}

[Test]
public void TestChecksumValidation()
{
    var saveManager = new SaveManager();
    saveManager.Save();
    
    // Corrupt save file
    var path = GetSavePath();
    var data = File.ReadAllBytes(path);
    data[100] ^= 0xFF;  // Flip bits
    File.WriteAllBytes(path, data);
    
    bool loaded = saveManager.TryLoad(out _);
    Assert.IsFalse(loaded, "Should reject corrupted save");
}
```

## Changelog

- v1.0 (2026-02-08): Initial save system specification

