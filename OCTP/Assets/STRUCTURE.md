# OCTP Assets Structure

This document provides a quick reference for the project folder structure.

## Top-Level Organization

### _Project/
Main game code organized by feature. Each feature has Scripts/, Prefabs/, and asset-specific subfolders.

**Features:**
- **Core/** - Shared utilities, managers, interfaces (referenced by all features)
- **Player/** - Player character and snake-like movement mechanics
- **Party/** - Party management, character roster, RPG elements
- **Enemies/** - Enemy AI, behaviors, and spawning
- **Combat/** - Combat system, damage calculation, effects
- **UI/** - In-game UI (HUD, menus, party management screens)
- **World/** - Level design, tiles, environment prefabs
- **Systems/** - Game systems (inventory, progression, save/load, etc.)

### Art/
Shared art assets used across multiple features.
- **Sprites/** - Sprite sheets and individual sprites
- **Materials/** - Shared materials
- **Shaders/** - Custom shaders
- **Animations/** - Shared animation clips

### Audio/
All audio files organized by type.
- **Music/** - Background music tracks
- **SFX/** - Sound effects
- **Mixers/** - Audio mixer assets

### Scenes/
All game scenes.
- **Main/** - Primary game scenes
- **Testing/** - Test and debug scenes
- **UI/** - UI-only scenes

### Settings/
ScriptableObject settings and configurations.
- **Input/** - Input System actions (InputSystem_Actions.inputactions)
- **Rendering/** - URP settings and render pipeline config
- **Audio/** - Audio mixer settings

### Tests/
Test assemblies separated by type.
- **EditMode/** - Editor tests (no play mode required)
- **PlayMode/** - Play mode tests (run in game environment)

### Editor/
Editor-only scripts and tools.
- **Scripts/** - Custom editor windows, inspectors, and utilities

### ThirdParty/
External assets and packages not created by the team.

### Plugins/
Third-party code plugins and native plugins.

### Resources/
Runtime-loadable assets via Resources.Load().

### Sandbox/
Experimental and prototyping area. Not for production code.

## Assembly Definitions

The project uses assembly definitions for faster compilation:

- **OCTP.Core** - Core utilities (no dependencies)
- **OCTP.Player** - Player scripts (depends on Core, Input System)
- **OCTP.Party** - Party management (depends on Core)
- **OCTP.Enemies** - Enemy AI (depends on Core)
- **OCTP.Combat** - Combat system (depends on Core)
- **OCTP.UI** - UI scripts (depends on Core)
- **OCTP.Editor** - Editor tools (editor-only, depends on Core)
- **OCTP.Tests.EditMode** - Edit mode tests
- **OCTP.Tests.PlayMode** - Play mode tests

## Naming Conventions

- **Folders**: PascalCase, no spaces
- **Scripts**: PascalCase matching class name
- **Prefabs**: PascalCase with descriptive names (e.g., `PlayerSnake`, `EnemyGoblin`)
- **Scenes**: PascalCase (e.g., `MainMenu`, `Level01`)
- **ScriptableObjects**: PascalCase with type suffix (e.g., `PlayerStats_SO`, `EnemyData_SO`)

## Adding New Features

1. Create feature folder in `_Project/` with standard subfolders
2. Add assembly definition in `Scripts/` subfolder
3. Reference `OCTP.Core` and other needed assemblies
4. Keep feature-specific assets within the feature folder
5. Use shared Art/Audio folders for cross-feature assets

## Notes

- Keep Sandbox/ for quick tests; move working code to proper features
- Use Resources/ sparingly (prefer AssetBundles or Addressables for runtime loading)
- ThirdParty/ assets should never be modified directly
- All assembly names use `OCTP.` prefix for consistency
