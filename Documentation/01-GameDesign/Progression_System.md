# Progression & Difficulty System

## Metadata
- **Type**: Game Design
- **Status**: Draft
- **Version**: 1.1
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [gdd-core, party-system, combat-system]

## Overview

Progression in OCTP occurs across multiple timescales: **within a run** (leveling and recruitment), **across runs** (meta-progression unlocks), and **difficulty scaling** (enemy budget system). Each session should be 15-30 minutes of exploration between safe zones.

## Goals

- Make character progression feel meaningful without overwhelming
- Scale difficulty appropriately as player grows
- Reward skilled play and smart positioning
- Enable multiple viable party compositions

## Dependencies

- **Core GDD** - Game structure and session/zone concepts
- **Party System** - Character leveling and recruitment
- **Combat System** - Enemy creation and scaling

## Character Progression (Within Run)

### Experience & Leveling

**Leveling Trigger**:
- Party gains XP from defeated enemies
- Entire party levels **together** when entering a safe zone
- Happens after collecting loot from enemies

**XP Distribution**:
- Defeated enemies drop XP orbs
- Total XP collected divided equally among all permanent party members
- NPCs from rescue missions do NOT share in XP (remain weak unless leveled separately)

**Leveling Curve**:
```
Level 1: 0 XP
Level 2: 100 XP
Level 3: 210 XP (110 additional)
Level 4: 330 XP (120 additional)
Level 5: 460 XP (130 additional)
...
Level 20: 19,900 XP
```

**Formula**: `NextLevelXP = CurrentLevelXP + (100 + 10 * CurrentLevel)`

**Effective Maximum**: Levels continue infinitely, but cost becomes prohibitive (level 50+ takes hours).

### Stat Growth Per Level

When party levels up in a safe zone, each character's primary stats increase based on **weighted randomness**:

```csharp
// Pseudocode for stat gain
public void LevelUp(Character character)
{
    // Class determines stat weights
    // Warrior: Strength 40%, Toughness 30%, Speed 20%, other 10%
    // Ranger: Skill 40%, Speed 30%, Strength 20%, other 10%
    // Healer: Will 40%, Aura 30%, Toughness 20%, other 10%
    
    int totalPointsToDistribute = 20; // Points per level
    for (int i = 0; i < totalPointsToDistribute; i++)
    {
        PrimaryStat stat = PickStatByWeight(character.Class.StatWeights);
        character.IncreaseStat(stat, Random.Range(1, 3)); // Gain 1-2 per point
    }
}
```

**Variance**: Same character leveling twice will have slightly different stats (one might be stronger, one tankier).

### Recruitment & Party Composition

See [Party_System.md](Party_System.md) for recruitment mechanics.

**Party Power Calculation**:
- Party strength is sum of all member stats
- Longer snakes are stronger but harder to control
- Synergy between abilities matters more than pure stats

## Difficulty Scaling

### Enemy Budget System

Dynamic enemy generation ensures appropriate challenge based on:

```
Enemy Zone Budget = BaseBudget
                  * ZoneDifficultyMultiplier
                  * PlayerLevelMultiplier
                  * AreaEnemyCountMultiplier
```

**Example**:
```
Base Budget: 1000 XP worth of enemies
Mountain Lower (difficulty 1.0x): 1000 XP budget
Mountain Peaks (difficulty 2.5x): 2500 XP budget
Player Level 5 vs Level 15: Budget scales up
```

### Enemy Snake Generation

Each enemy snake in a zone is generated using a portion of the zone's budget:

```csharp
public EnemySnake GenerateEnemySnake(float budgetAllocation)
{
    var snake = new EnemySnake();
    
    // Constraints for this area
    MinSnakeLength = 2;      // At least 2 enemies
    MaxSnakeLength = 8;      // At most 8 enemies
    MinPerEnemyBudget = 100; // Minimum power per enemy
    MaxPerEnemyBudget = 500; // Maximum power per enemy
    
    while (TotalBudgetSpent < budgetAllocation && 
           snake.Length < MaxSnakeLength)
    {
        float enemyBudget = Random.Range(MinPerEnemyBudget, MaxPerEnemyBudget);
        EnemyCharacter enemy = CreateEnemyWithinBudget(enemyBudget);
        snake.AddMember(enemy);
        TotalBudgetSpent += enemyBudget;
    }
    
    return snake;
}
```

### Flexible Guard Rails

**Minimum Constraints**:
- At least 2 enemies per snake (prevent trivial encounters)
- Minimum enemy level relative to player level

**Maximum Constraints**:
- No snake longer than 8 members (prevent overwhelming)
- No single enemy vastly overpowered compared to player max level
- Difficulty doesn't spike unexpectedly between adjacent areas

### Difficulty Progression by Zone

**Example Zone Progression**:
```
Town (Safe Zone) - No enemies
  ↓
Grasslands (Easy) - Budget 500 XP, difficulty 0.8x, 1-3 enemies per snake
  ↓
Forest (Normal) - Budget 1000 XP, difficulty 1.0x, 2-4 enemies per snake
  ↓
Mountain Lower (Hard) - Budget 1500 XP, difficulty 1.5x, 2-5 enemies per snake
  ↓
Mountain Peaks (Very Hard) - Budget 2500 XP, difficulty 2.5x, 3-6 enemies per snake, Boss encounter
```

**Player Level Scaling**:
- Level 1-5: Grasslands appropriate
- Level 5-10: Forest recommended, Mountains possible
- Level 10+: Mountains accessible, unlocks deeper content

## Session Structure

### Single Session (15-30 minutes)

```
1. Start in Safe Zone
2. Exit to Exploration Area
3. Auto-advance through 3-5 encounters
4. Collect loot (gold, XP, items, healing orbs)
5. Return to Safe Zone
6. Level up party
7. Manage party composition / equipment
8. Decide: Go deeper or return to town?
```

### Multi-Zone Progression

Zones are connected via safe zones:

```
Town (Starting Safe Zone)
  ↓
Grasslands (Easy, no boss)
  ↓
Forest Camp (Safe Zone)
  ↓
Forest (Normal, with mini-boss)
  ↓
Forest Deep (Hard)
  ↓
Mountain Base Camp (Safe Zone)
  ↓
Mountain Lower (Hard)
  ↓
Mountain Peaks (Very Hard, Final Boss)
```

**Player Decision**:
- Stay in early zones for easier loot/leveling
- Push into harder zones for better rewards but risk
- Balance is key: too greedy and you wipe, too cautious and you don't progress

## Progression Milestones

### Character Progression
- **Level 5**: Basic competency, most abilities unlocked
- **Level 10**: Mid-game power, noticeable stat differences
- **Level 20**: Powerful enough to attempt harder zones
- **Level 50+**: Endgame territory, can tackle any content

### Equipment Progression
- Early runs: Basic equipment found in Grasslands
- Mid-game: Forged/crafted equipment in Forest areas
- Late-game: Rare drops from bosses and deep encounters
- Endgame: Legendary equipment from specific challenges

### Party Progression
- Early: Small snake (2-4 members), basic synergies
- Mid: Medium snake (4-7 members), clear roles (tank, DPS, healer)
- Late: Large snake (7-10 members), complex synergies and playstyle

## Difficulty Perception vs Actual

**Easy** (Grasslands):
- Enemies: 500 XP budget, 1-3 members
- Player: Level 1-5
- Outcome: Player should win 95%+ of encounters

**Normal** (Forest):
- Enemies: 1000 XP budget, 2-4 members
- Player: Level 5-10
- Outcome: Challenging but winnable (80% success rate)

**Hard** (Mountain Lower):
- Enemies: 1500 XP budget, 2-5 members
- Player: Level 10+
- Outcome: Tough; mistakes are punished (60% success rate)

**Very Hard** (Mountain Peaks):
- Enemies: 2500 XP budget, 3-6 members
- Player: Level 15+
- Outcome: Boss-level challenge; requires good strategy (40% success rate)

## Success Criteria

- [ ] Player feels character power growing meaningfully
- [ ] Difficulty scales smoothly (no sudden spikes)
- [ ] All party compositions viable at appropriate levels
- [ ] Skilled players can attempt harder zones earlier than level-gated
- [ ] Sessions complete in 15-30 minutes target time
- [ ] No single stat is overpowered (Strength vs Skill balanced)
- [ ] Equipment meaningfully improves characters (not negligible)

## Run Seeding for Reproducibility

OCTP uses **seed-based deterministic difficulty** to enable reproducible runs:

```csharp
public class GameSession
{
    public uint SessionSeed { get; set; }  // e.g., Unix timestamp at start
    
    public GameSession()
    {
        SessionSeed = (uint)System.DateTime.Now.Ticks;
        Random.InitState((int)SessionSeed);
    }
}
```

**Benefits**:
- **Speedrunning**: Players can share seeds for verified runs
- **A/B Testing**: Same seed = same difficulty curve for analytics
- **Debugging**: Reproduce exact encounter sequences
- **Balance Testing**: Compare player performance on identical seeds

**Usage**:
1. Session starts with unique seed (stored in save data)
2. All RNG uses this seed (enemy spawning, stats, loot)
3. Same seed always generates same difficulty/encounters
4. Player can see/share seed in UI for verification

## Progressive Tutorial System

Rather than a dedicated tutorial state, OCTP uses **progressive hints** in early zones:

```csharp
public class TutorialHintSystem : MonoBehaviour
{
    public class TutorialHint
    {
        public string HintId;              // e.g., "movement_basics", "combat_first_enemy"
        public string HintText;
        public Trigger TriggerCondition;  // When to show
        public bool CanDismiss;           // Player can hide this hint
    }
    
    private List<TutorialHint> hints = new()
    {
        new() { HintId = "movement_forward", 
                HintText = "Your party auto-advances. Steer with arrow keys.",
                TriggerCondition = FirstMovement },
        new() { HintId = "combat_enemy_detected",
                HintText = "Enemy snake detected! Press 1-9 to activate abilities.",
                TriggerCondition = FirstEnemyEncounter },
        new() { HintId = "combat_positioning",
                HintText = "Position your party for optimal attacks. Distance affects damage.",
                TriggerCondition = SecondEnemyEncounter },
        new() { HintId = "safe_zone_rest",
                HintText = "Safe zones let you heal, recruit, and manage party.",
                TriggerCondition = FirstSafeZoneEntry },
        new() { HintId = "leveling",
                HintText = "Party levels up together when returning to safe zones.",
                TriggerCondition = FirstLevelUp }
    };
}
```

**Features**:
- Hints trigger on first encounter with mechanic (not forced tutorials)
- Each hint can be dismissed/hidden
- No interruption to gameplay
- Familiar players can ignore hints and play naturally

## Open Questions

- **Exact XP Amounts**: Needs playtesting to fine-tune curve
- **Boss Scaling**: Do zone bosses use same budget system or custom scaling?
- **Prestige/New Game+**: After beating final boss, what's meta-progression?
- **Difficulty Settings**: Should difficulty be player-selectable?

## Changelog

- **v1.1** (2026-02-09): Added seeding for reproducible runs (speedrunning, A/B testing), progressive tutorial system (no dedicated tutorial state)
- **v1.0** (2026-02-08): Initial progression and difficulty system design

---

*Balance is critical; these systems require extensive playtesting to tune.*
