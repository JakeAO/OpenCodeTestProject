# Glossary

## Metadata
- **Type**: Reference
- **Status**: Draft
- **Version**: 1.0
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [gdd-core]

## Overview

Definitions of terms used throughout OCTP documentation to ensure consistency and clarity.

## Game Terms

### A

**Active Party**
The characters currently in the snake formation, maximum of 10 members.

**Assembly Definition (asmdef)**
Unity configuration file that creates a separate code assembly for faster compilation.

### C

**Chain**
The snake-like formation of party members following the head character.

**Coiling**
Movement pattern where the snake curves back on itself, creating a spiral formation.

**Cooldown**
Time period after using an ability before it can be used again.

### D

**Dash**
Quick burst of movement with increased speed and invulnerability, 2-second cooldown.

**Downed**
Party member state at 0 HP, removed from formation but can be revived after combat.

### F

**Follower**
Party member that trails behind the head in the snake chain.

**Formation**
The shape and positioning of the party chain at any given moment.

### H

**Head**
The first party member in the snake chain, directly controlled by the player.

### M

**MVP (Minimum Viable Product)**
Core features required for initial release: movement, party, combat, 3 classes.

### P

**Party Member**
A recruited character in the active party formation.

**Position Trail**
Queue of historical positions used by followers to create smooth chain movement.

### R

**Reserve Party**
(Future feature) Characters recruited but not currently in active formation.

**Run**
Single playthrough from start to game over or victory.

### S

**Self-Collision**
When the snake head tries to move through its own party members.

**Snake Formation**
The visual appearance of party members following each other in a line.

**Synergy**
When two or more abilities or characters work together for enhanced effect.

### T

**Tail**
The last party member in the snake chain (slots 8-10).

**Trail Queue**
Data structure storing recent positions for follower movement calculation.

### W

**Whip Turn**
Sharp directional change causing the tail of the snake to swing rapidly.

## Technical Terms

### A

**Assembly**
Group of C# scripts compiled together, defined by .asmdef files.

**AutoReferenced**
Assembly definition setting that makes the assembly available to other assemblies automatically.

### C

**Component**
Unity MonoBehaviour attached to a GameObject.

**Constraint**
Limitation or boundary condition in design or implementation.

### E

**Event Bus**
Central system for publishing and subscribing to game events.

**EventBus.Publish<T>**
Method to send an event to all subscribers.

**EventBus.Subscribe<T>**
Method to register a listener for an event type.

### F

**FixedUpdate**
Unity method called at fixed time intervals, used for physics calculations.

**Follower Spacing**
Distance maintained between party members in the chain (0.8 units).

### G

**GameObject**
Basic Unity entity that exists in a scene.

**Gizmos**
Debug visualization drawn in Unity's Scene view.

### I

**IGameEvent**
Interface marking a type as usable with the EventBus.

**Input System**
Unity's new input handling package (com.unity.inputsystem).

### L

**Lerp (Linear Interpolation)**
Smoothing function: `Lerp(a, b, t) = a + (b - a) * t`.

### M

**MANIFEST.json**
Structured index file listing all documentation for AI agent discovery.

**MonoBehaviour**
Base class for Unity scripts attached to GameObjects.

### O

**Object Pool**
Reusable collection of objects to avoid allocation/destruction costs.

### R

**Rigidbody2D**
Unity physics component for 2D objects.

### S

**ScriptableObject**
Unity asset type for data storage, doesn't require scene presence.

**ServiceLocator**
Pattern for accessing global services without direct references.

**Singleton**
Design pattern ensuring only one instance of a class exists.

**Success Criteria**
Measurable conditions that indicate a feature is complete and working.

### T

**TBD (To Be Determined)**
Placeholder indicating design decision not yet made.

**Trail Index**
Position in the trail queue used to calculate follower target position.

### U

**Update**
Unity method called every frame for game logic.

**URP (Universal Render Pipeline)**
Unity's modern rendering pipeline optimized for performance.

### V

**Vector2**
2D coordinate (x, y) in Unity.

## Status Definitions

**Draft**
Initial version, subject to change.

**In Review**
Being reviewed by team/stakeholders.

**Approved**
Design finalized, ready for implementation.

**Implemented**
Code exists matching the design.

**Deprecated**
No longer valid, kept for reference.

## Priority Levels

**Critical**
Must have for MVP, blocks other work.

**High**
Important for core gameplay.

**Medium**
Enhances experience.

**Low**
Nice to have, polish, future consideration.

## Abbreviations

- **AI**: Artificial Intelligence
- **DPS**: Damage Per Second
- **FPS**: Frames Per Second
- **GDD**: Game Design Document
- **HP**: Health Points
- **HUD**: Heads-Up Display
- **MVP**: Minimum Viable Product
- **NPC**: Non-Player Character
- **RPG**: Role-Playing Game
- **TDD**: Technical Design Document
- **UI**: User Interface
- **URP**: Universal Render Pipeline
- **UX**: User Experience
- **XP**: Experience Points

## Changelog

- **v1.0** (2026-02-08): Initial glossary

---

*Add new terms as they're introduced in documentation.*
