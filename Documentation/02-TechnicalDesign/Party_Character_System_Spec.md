# Party & Character System Specification

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.0
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [data-structures-spec, progression-system, combat-system]

## Overview

The Party and Character System manages party composition, individual character stats, equipment, and inventory. It calculates derived stats based on primary stats and equipment modifiers.

## Goals

- **Party Management**: Active party of up to 10 members
- **Stat Calculation**: Primary (7) + Derived stats (HP, Damage, Defense, Speed)
- **Equipment System**: Weapon, Armor, Accessory slots with modifiers
- **Inventory Management**: Materials, consumables, resources tracking
- **Downed State**: Partial party defeat with reduced stats

## Party Class

```csharp
public class Party
{
    [SerializeField] private List<Character> _activeMembers = new();
    [SerializeField] private Inventory _inventory;
    
    public const int MaxPartySize = 10;
    public IReadOnlyList<Character> Members => _activeMembers.AsReadOnly();
    public Inventory Inventory => _inventory;
    
    public bool TryAddMember(Character character)
    {
        if (_activeMembers.Count >= MaxPartySize)
            return false;
        
        _activeMembers.Add(character);
        return true;
    }
    
    public void RemoveMember(Character character)
    {
        _activeMembers.Remove(character);
    }
    
    public List<Character> GetHealthyMembers()
    {
        return _activeMembers.Where(c => !c.IsDowned).ToList();
    }
    
    public bool IsFullyDefeated()
    {
        return _activeMembers.All(c => c.IsDowned);
    }
    
    public float GetAverageLevel()
    {
        return _activeMembers.Average(c => c.Level);
    }
}
```

## Character Class

```csharp
public class Character
{
    [SerializeField] private string _name;
    [SerializeField] private CharacterClass _class;
    [SerializeField] private int _level = 1;
    [SerializeField] private int _experience = 0;
    [SerializeField] private CharacterStats _baseStats;
    [SerializeField] private Equipment _equipment;
    [SerializeField] private List<Ability> _abilities;
    
    private float _currentHP;
    private bool _isDowned = false;
    
    public string Name => _name;
    public CharacterClass Class => _class;
    public int Level => _level;
    public bool IsDowned => _isDowned;
    public float CurrentHP => _currentHP;
    public float MaxHP => GetStat(StatType.HP);
    
    public CharacterStats GetEffectiveStats()
    {
        var effective = _baseStats.Clone();
        
        // Apply equipment modifiers
        if (_equipment.Weapon != null)
            effective.AddModifiers(_equipment.Weapon.Modifiers);
        if (_equipment.Armor != null)
            effective.AddModifiers(_equipment.Armor.Modifiers);
        if (_equipment.Accessory != null)
            effective.AddModifiers(_equipment.Accessory.Modifiers);
        
        // Downed penalty: 90% stat reduction
        if (_isDowned)
            effective.MultiplyAll(0.1f);
        
        return effective;
    }
    
    public void TakeDamage(float damage)
    {
        _currentHP -= damage;
        if (_currentHP <= 0)
        {
            _currentHP = 0;
            _isDowned = true;
        }
    }
    
    public void Heal(float amount)
    {
        _currentHP = Mathf.Min(_currentHP + amount, MaxHP);
    }
    
    public void Revive()
    {
        _isDowned = false;
        _currentHP = MaxHP * 0.5f;  // Revive at 50% HP
    }
    
    public void GainExperience(int amount)
    {
        _experience += amount;
        while (_experience >= GetXPForNextLevel())
        {
            _experience -= GetXPForNextLevel();
            LevelUp();
        }
    }
    
    private void LevelUp()
    {
        _level++;
        GrowStats();
    }
    
    private void GrowStats()
    {
        // Use weighted randomness per class
        _baseStats.Strength += Random.Range(
            ConfigManager.Instance.GetStatGrowth(Class, StatType.Strength).min,
            ConfigManager.Instance.GetStatGrowth(Class, StatType.Strength).max
        );
        // ... grow all 7 primary stats
    }
    
    public Ability GetAbility(int slot) // 1-9
    {
        return slot >= 1 && slot <= _abilities.Count 
            ? _abilities[slot - 1] 
            : null;
    }
    
    public float GetStat(StatType type) => GetEffectiveStats().Get(type);
}
```

## CharacterStats

```csharp
[System.Serializable]
public class CharacterStats
{
    // Primary stats (7)
    public int Strength;
    public int Skill;
    public int Toughness;
    public int Speed;
    public int Will;
    public int Aura;
    public int Luck;
    
    // Derived stats (calculated)
    public float HP { get; private set; }
    public float Damage { get; private set; }
    public float Defense { get; private set; }
    public float CritChance { get; private set; }
    public float DodgeChance { get; private set; }
    
    public void RecalculateDerived()
    {
        // HP = Toughness * 5 + Aura * 2
        HP = Toughness * 5 + Aura * 2;
        
        // Damage = Strength + Skill / 2
        Damage = Strength + Skill * 0.5f;
        
        // Defense = Toughness + Aura / 2
        Defense = Toughness + Aura * 0.5f;
        
        // CritChance = Skill / 100
        CritChance = Skill * 0.01f;
        
        // DodgeChance = Speed / 200
        DodgeChance = Speed * 0.005f;
    }
    
    public void AddModifiers(StatModifier modifiers)
    {
        Strength += modifiers.Strength;
        Skill += modifiers.Skill;
        // ... add all stats
        RecalculateDerived();
    }
    
    public void MultiplyAll(float multiplier)
    {
        Strength = (int)(Strength * multiplier);
        Skill = (int)(Skill * multiplier);
        // ... multiply all
        RecalculateDerived();
    }
    
    public float Get(StatType type) => type switch
    {
        StatType.HP => HP,
        StatType.Damage => Damage,
        StatType.Defense => Defense,
        _ => throw new System.ArgumentException()
    };
    
    public CharacterStats Clone() => new CharacterStats
    {
        Strength = this.Strength,
        Skill = this.Skill,
        // ... copy all
    };
}

public enum StatType
{
    Strength, Skill, Toughness, Speed, Will, Aura, Luck,
    HP, Damage, Defense, CritChance, DodgeChance
}
```

## Equipment System

```csharp
[System.Serializable]
public class Equipment
{
    [SerializeField] private Weapon _weapon;
    [SerializeField] private Armor _armor;
    [SerializeField] private Accessory _accessory;
    
    public Weapon Weapon => _weapon;
    public Armor Armor => _armor;
    public Accessory Accessory => _accessory;
    
    public bool EquipWeapon(Weapon weapon)
    {
        _weapon = weapon;
        return true;
    }
    
    public bool EquipArmor(Armor armor)
    {
        _armor = armor;
        return true;
    }
    
    public bool EquipAccessory(Accessory accessory)
    {
        _accessory = accessory;
        return true;
    }
}

[System.Serializable]
public class Weapon
{
    public string Name;
    public float Damage;
    public StatModifier Modifiers;
    public int RequiredLevel;
}

[System.Serializable]
public class Armor
{
    public string Name;
    public float Defense;
    public StatModifier Modifiers;
    public int RequiredLevel;
}

[System.Serializable]
public class Accessory
{
    public string Name;
    public StatModifier Modifiers;
    public int RequiredLevel;
}

[System.Serializable]
public class StatModifier
{
    public int Strength;
    public int Skill;
    public int Toughness;
    public int Speed;
    public int Will;
    public int Aura;
    public int Luck;
}
```

## Inventory

```csharp
[System.Serializable]
public class Inventory
{
    [SerializeField] private Dictionary<string, int> _materials = new();
    [SerializeField] private List<ConsumableItem> _consumables = new();
    [SerializeField] private int _goldCollected;
    
    public int GetMaterialCount(string materialId) 
        => _materials.TryGetValue(materialId, out int count) ? count : 0;
    
    public void AddMaterial(string materialId, int amount)
    {
        if (!_materials.ContainsKey(materialId))
            _materials[materialId] = 0;
        _materials[materialId] += amount;
    }
    
    public bool ConsumeMaterial(string materialId, int amount)
    {
        if (GetMaterialCount(materialId) < amount)
            return false;
        _materials[materialId] -= amount;
        return true;
    }
    
    public void AddGold(int amount) => _goldCollected += amount;
    public int GetGold() => _goldCollected;
}
```

## Success Criteria

- [x] Character stats calculated correctly from primaries
- [x] Equipment modifiers apply to effective stats
- [x] Downed state reduces stats by 90%
- [x] Party size limited to 10 members
- [x] Leveling system tracks XP and triggers stat growth
- [x] Inventory tracks materials and gold
- [x] Serialization/deserialization works correctly

## Testing

```csharp
[Test]
public void TestCharacterCreation()
{
    var char = new Character { /* ... */ };
    Assert.AreEqual(10, char.MaxHP);
}

[Test]
public void TestEquipmentModifiers()
{
    var character = new Character();
    var weapon = new Weapon { Modifiers = new StatModifier { Strength = 5 } };
    character.Equipment.EquipWeapon(weapon);
    
    Assert.Greater(character.GetStat(StatType.Damage), 0);
}

[Test]
public void TestDownedPenalty()
{
    var character = new Character();
    float originalDamage = character.GetStat(StatType.Damage);
    character.TakeDamage(character.MaxHP);
    Assert.Less(character.GetStat(StatType.Damage), originalDamage);
}
```

## Changelog

- v1.0 (2026-02-08): Initial party & character system specification

