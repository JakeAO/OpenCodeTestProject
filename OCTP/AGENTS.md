# AGENTS.md - Unity Project

This is a Unity 2D-URP project (Universal Render Pipeline). Use this guide when working in this codebase.

## Project Structure

- **Assets/**: Contains all game assets (scripts, scenes, prefabs, etc.)
- **ProjectSettings/**: Unity project configuration
- **Library/**: Cached Unity data (do not edit directly)
- **Logs/**: Unity editor logs
- **Packages/**: Unity Package Manager dependencies
- **OCTP.slnx**: Visual Studio solution file (dotnet.defaultSolution in .vscode/settings.json)

## Build/Test Commands

Unity projects do not use traditional build commands. Build/test operations are done through:

### Unity Editor
- Open Unity Editor and use the build pipeline
- Use Unity Test Runner for tests (Window > General > Test Runner)
- Build settings: File > Build Settings

### Command Line (if CI/CD configured)
```bash
# Build project (if build script exists)
/Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath /Users/jaoxscp/Documents/OpenCodeTestProject/OCTP -executeMethod BuildScript.Build

# Run tests (if test script exists)
/Applications/Unity/Unity.app/Contents/MacOS/Unity -runTests -testPlatform PlayMode -projectPath /Users/jaoxscp/Documents/OpenCodeTestProject/OCTP
```

Note: This project currently has no custom build scripts. Tests must be run within Unity Editor.

## Code Style Guidelines

### General
- **Indentation**: 2 spaces (see .editorconfig in Unity package cache)
- **End of line**: LF
- **Language**: C# (.NET compatible with Unity)

### Naming Conventions
- **Classes/Structs**: PascalCase (e.g., `PlayerController`)
- **Methods**: PascalCase (e.g., `Start()`, `Update()`)
- **Private fields**: camelCase with `_` prefix (e.g., `_health`)
- **Public fields**: PascalCase (e.g., `Speed`)
- **Properties**: PascalCase (e.g., `Health { get; set; }`)
- **Constants**: ALL_CAPS (e.g., `MAX_SPEED`)
- **Enums**: PascalCase for name, PascalCase for values

### Imports
- Group Unity imports first, then .NET imports, then custom
- Keep imports organized and remove unused ones

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Custom namespace imports
```

### Unity-Specific Patterns
- **MonoBehaviour**: Inherit from MonoBehaviour for scripts attached to GameObjects
- **Lifecycle methods**: Use `Awake()`, `Start()`, `Update()`, `FixedUpdate()`, etc.
- **SerializeField**: Use `[SerializeField]` for private fields editable in Inspector
- **RequireComponent**: Use `[RequireComponent(typeof(Rigidbody))]` for dependencies

### Error Handling
- Use `Debug.Log()`, `Debug.LogWarning()`, `Debug.LogError()` for debugging
- Validate public fields in `OnValidate()` or `Awake()`
- Handle null checks for component dependencies

### Formatting
- Keep lines under 120 characters
- One class per file (unless small nested classes)
- Use regions for large classes if needed
- Follow Unity C# Style Guide conventions

### Best Practices
- Avoid `Find()` and `FindObjectOfType()` in Update() - cache references in Awake/Start
- Use `transform` instead of `gameObject.transform` for performance
- Prefer `GetComponent<T>()` over `GetComponent(typeof(T))`
- Use `ScriptableObject` for data containers shared across scenes

## File Exclusions

Per .vscode/settings.json, excluded from editor:
- Build/, build/ folders
- Library/, Logs/, obj/, temp/ folders
- *.asset, *.meta, *.prefab, *.unity files (treated as YAML)
- Generated Unity files (.csproj, .sln when nested)

## Version Control

- DO NOT commit: Library/, Logs/, obj/, Temp/, UserSettings/
- DO commit: Assets/, ProjectSettings/, Packages/manifest.json
- Use .gitignore patterns appropriate for Unity projects
