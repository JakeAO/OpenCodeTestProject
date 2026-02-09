# Data Structures Specification

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.1
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [save-system-spec, party-character-system-spec]

## Overview

This document defines all serializable data structures used for save data, party state, and configuration. All classes are marked [System.Serializable] for binary serialization.

## SaveData Container

```csharp
[System.Serializable]
public class SaveData
{
    public int Version { get; set; } = 1;
    public long Checksum { get; set; }
    public System.DateTime SaveTime { get; set; }
    public float PlaytimeSeconds { get; set; }
    public uint SessionSeed { get; set; }  // Seed for reproducible runs (speedrunning, A/B testing)
    
    public PlayerProgress Progress { get; set; }
    public PartyData Party { get; set; }
    public WorldState WorldState { get; set; }
    public InventoryData Inventory { get; set; }
}
```

## PlayerProgress

```csharp
[System.Serializable]
public class PlayerProgress
{
    public int CurrentLevel { get; set; } = 1;
    public int TotalXP { get; set; } = 0;
    public string CurrentZone { get; set; } = "Zone_SafeZone";
    
    [System.Serializable]
    public struct Vector3Data
    {
        public float X, Y, Z;
        public static Vector3Data FromVector3(UnityEngine.Vector3 v) 
            => new() { X = v.x, Y = v.y, Z = v.z };
        public UnityEngine.Vector3 ToVector3() 
            => new(X, Y, Z);
    }
    
    public Vector3Data PlayerPosition { get; set; }
    public int GoldCollected { get; set; } = 0;
    
    public Dictionary<string, int> ZoneProgression { get; set; }
        = new();  // Zone name -> completion count
}
```

## PartyData

```csharp
[System.Serializable]
public class PartyData
{
    public List<CharacterData> ActiveMembers { get; set; } = new();
    public List<CharacterData> RecruitedNPCs { get; set; } = new();
    
    public int GetPartySize() => ActiveMembers.Count;
    public int GetTotalRecruits() => RecruitedNPCs.Count;
}
```

## CharacterData

```csharp
[System.Serializable]
public class CharacterData
{
    public string Name { get; set; }
    public int Class { get; set; }  // CharacterClass enum value
    public int Level { get; set; } = 1;
    public int CurrentXP { get; set; } = 0;
    public float CurrentHP { get; set; }
    public bool IsDowned { get; set; } = false;
    
    // Primary stats
    public int Strength { get; set; } = 5;
    public int Skill { get; set; } = 5;
    public int Toughness { get; set; } = 5;
    public int Speed { get; set; } = 5;
    public int Will { get; set; } = 5;
    public int Aura { get; set; } = 5;
    public int Luck { get; set; } = 5;
    
    // Equipment
    public WeaponData Equipment_Weapon { get; set; }
    public ArmorData Equipment_Armor { get; set; }
    public AccessoryData Equipment_Accessory { get; set; }
    
    // Abilities (slots 1-9)
    public List<AbilityData> Abilities { get; set; } = new();
    
    // Status effects
    public List<StatusEffectData> ActiveStatusEffects { get; set; } = new();
}
```

## EquipmentData

```csharp
[System.Serializable]
public class WeaponData
{
    public string ID { get; set; }
    public string Name { get; set; }
    public float BaseDamage { get; set; }
    public int StatBonus_Strength { get; set; }
    public int StatBonus_Skill { get; set; }
}

[System.Serializable]
public class ArmorData
{
    public string ID { get; set; }
    public string Name { get; set; }
    public float BaseDefense { get; set; }
    public int StatBonus_Toughness { get; set; }
    public int StatBonus_Aura { get; set; }
}

[System.Serializable]
public class AccessoryData
{
    public string ID { get; set; }
    public string Name { get; set; }
    public int StatBonus_Will { get; set; }
    public int StatBonus_Luck { get; set; }
}
```

## AbilityData

```csharp
[System.Serializable]
public class AbilityData
{
    public string ID { get; set; }
    public string Name { get; set; }
    public int Slot { get; set; }  // 1-9
    public int Level { get; set; } = 1;
    public float Cooldown { get; set; }
    public float RemainingCooldown { get; set; }
    public bool IsActive { get; set; }  // vs Passive
}
```

## StatusEffectData

```csharp
[System.Serializable]
public class StatusEffectData
{
    public string EffectID { get; set; }
    public string EffectName { get; set; }
    public float RemainingDuration { get; set; }
    public int Stacks { get; set; } = 1;
    
    public enum EffectType
    {
        Stun, Slow, Poison, Weakened, Protected, Cursed
    }
    public int EffectType { get; set; }
}
```

## InventoryData

```csharp
[System.Serializable]
public class InventoryData
{
    public Dictionary<string, int> Materials { get; set; } = new();
    public List<ConsumableItemData> Consumables { get; set; } = new();
    public int GoldOnHand { get; set; } = 0;
    
    public int GetMaterialCount(string materialID)
        => Materials.TryGetValue(materialID, out int count) ? count : 0;
}

[System.Serializable]
public class ConsumableItemData
{
    public string ID { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public string Effect { get; set; }  // "heal_100", "revive", etc.
}
```

## WorldState

```csharp
[System.Serializable]
public class WorldState
{
    public List<string> DefeatedEnemies { get; set; } = new();
    public List<string> CompletedQuests { get; set; } = new();
    public List<string> UnlockedAreas { get; set; } = new();
    public Dictionary<string, int> BossDefeats { get; set; } = new();
    
    public bool HasDefeatedEnemy(string enemyID) 
        => DefeatedEnemies.Contains(enemyID);
    
    public bool HasCompletedQuest(string questID) 
        => CompletedQuests.Contains(questID);
    
    public int GetBossDefeatCount(string bossID)
        => BossDefeats.TryGetValue(bossID, out int count) ? count : 0;
}
```

## Configuration ScriptableObjects

```csharp
[System.Serializable]
public class XPTableEntry
{
    public int Level { get; set; }
    public int RequiredXP { get; set; }
}

public class BaseXPTableConfig : ScriptableObject
{
    [SerializeField] private List<XPTableEntry> _xpTable = new();
    
    public int GetXPForLevel(int level)
    {
        if (level < 1 || level > _xpTable.Count)
            return int.MaxValue;
        return _xpTable[level - 1].RequiredXP;
    }
}
```

```csharp
[System.Serializable]
public class StatGrowthRange
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public class CharacterClassConfig : ScriptableObject
{
    [System.Serializable]
    public class ClassStats
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        
        public StatGrowthRange Strength_Growth { get; set; }
        public StatGrowthRange Skill_Growth { get; set; }
        public StatGrowthRange Toughness_Growth { get; set; }
        public StatGrowthRange Speed_Growth { get; set; }
        public StatGrowthRange Will_Growth { get; set; }
        public StatGrowthRange Aura_Growth { get; set; }
        public StatGrowthRange Luck_Growth { get; set; }
    }
    
    [SerializeField] private List<ClassStats> _classData = new();
    
    public StatGrowthRange GetStatGrowth(int classID, StatType stat)
    {
        var classStats = _classData.Find(cs => cs.ClassID == classID);
        if (classStats == null)
            throw new System.ArgumentException($"Class {classID} not found");
        
        return stat switch
        {
            StatType.Strength => classStats.Strength_Growth,
            StatType.Skill => classStats.Skill_Growth,
            // ... etc
            _ => throw new System.ArgumentException()
        };
    }
}
```

```csharp
[System.Serializable]
public class ZoneConfig : ScriptableObject
{
    public string ZoneID { get; set; }
    public string ZoneName { get; set; }
    public int RecommendedLevel { get; set; }
    
    [System.Serializable]
    public class Vector3Serialized
    {
        public float X, Y, Z;
        public UnityEngine.Vector3 ToVector3() => new(X, Y, Z);
    }
    
    public Vector3Serialized PlayerSpawnPoint { get; set; }
    
    [System.Serializable]
    public class EnemySpawner
    {
        public string EnemyID { get; set; }
        public int Count { get; set; }
        public Vector3Serialized SpawnLocation { get; set; }
    }
    
    [SerializeField] private List<EnemySpawner> _enemySpawners = new();
    
    public List<EnemySpawner> GetEnemySpawners() => _enemySpawners;
}
```

## Serialization Helpers

```csharp
public static class SerializationHelper
{
    public static byte[] SerializeToBytes<T>(T obj)
    {
        using (var stream = new System.IO.MemoryStream())
        {
            var formatter = new System.Runtime.Serialization
                .Formatters.Binary.BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }
    }
    
    public static T DeserializeFromBytes<T>(byte[] data)
    {
        using (var stream = new System.IO.MemoryStream(data))
        {
            var formatter = new System.Runtime.Serialization
                .Formatters.Binary.BinaryFormatter();
            return (T)formatter.Deserialize(stream);
        }
    }
}
```

## Size Estimates

| Data Type | Size |
|-----------|------|
| SaveData (full) | ~200KB |
| CharacterData | ~2-5KB |
| Party (10 members) | ~30KB |
| World State (100+ quests) | ~20KB |
| Inventory (materials) | ~10KB |

## Success Criteria

- [x] All data structures serializable to binary
- [x] Checksum validation prevents corruption
- [x] Version field supports future migrations
- [x] Vector3/DateTime properly serialized
- [x] ScriptableObjects load correctly
- [x] No circular references

## Changelog

- v1.1 (2026-02-09): Added SessionSeed to SaveData for reproducible runs
- v1.0 (2026-02-08): Initial data structures specification

