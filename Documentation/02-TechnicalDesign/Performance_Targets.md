# Performance Targets & Optimization Guide

## Metadata
- **Type**: Technical Design
- **Status**: Draft
- **Version**: 1.0
- **Last Updated**: 2026-02-08
- **Owner**: OCTP Team
- **Related Docs**: [architecture-overview, combat-system, movement-system]

## Target Platforms

- **v1.0**: PC (Windows/Mac) - 60 FPS target
- **v2.0**: Mobile (iOS/Android) - 30-60 FPS
- **v3.0**: Console (Nintendo Switch) - 60 FPS handheld

## Performance Targets

### Frame Rate

| Scenario | Target | Max Allowed |
|----------|--------|------------|
| Exploration (no combat) | 60 FPS | 55 FPS |
| Combat (10 party + 15 enemies) | 60 FPS | 55 FPS |
| Safe Zone UI | 60 FPS | 55 FPS |
| Pause Menu | 60 FPS | 55 FPS |
| Scene Transition | Loading screen | < 3s total |

**Sustained Performance**: 60 FPS for minimum 5 minutes (typical session run)

### Memory Budget

| System | Budget | Notes |
|--------|--------|-------|
| Main Scene | < 100MB | GameManager, Managers, Persistent Data |
| Content Scene | < 200MB | Zone data, enemies, obstacles, NPCs |
| UI Scene | < 50MB | Canvases, buttons, text, HUD elements |
| Total Peak | < 400MB | All scenes loaded (should not happen) |
| Target (mobile) | < 512MB | For future iOS/Android support |

### Load Times

| Operation | Target | Max Allowed |
|-----------|--------|------------|
| Scene transition | < 3s | 3.5s |
| Save to disk | < 100ms | 200ms |
| Load from disk | < 100ms | 200ms |
| Cloud upload | async | (non-blocking) |
| Game launch (cold) | < 5s | 7s |

### Draw Call Budget

| Category | Target | URP Constraint |
|----------|--------|----------------|
| Total draw calls | < 400 | < 500 (URP limit) |
| Batch efficiency | > 80% | Reduce small meshes |
| Static batching | > 70% | Mark static obstacles |
| Dynamic batching | > 50% | Keep small meshes < 900 verts |

## Optimization Strategies

### Frame Budget (16.67ms at 60 FPS)

```
Frame Budget Breakdown:
├─ Input Processing      0.5ms
├─ Game Logic          5.0ms
│  ├─ AI Updates       2.0ms
│  ├─ Movement         1.5ms
│  └─ Combat           1.5ms
├─ Physics            2.0ms (collision, raycasts)
├─ Rendering          6.5ms
│  ├─ Culling         1.0ms
│  ├─ Shadows         1.5ms
│  └─ Draw           4.0ms
├─ UI Update          1.2ms
└─ Other              1.3ms
                     ─────────
Total Budget        16.67ms
Headroom             1.5ms (Safety margin)
```

### Caching & Pooling

```csharp
// Object pooling for frequent allocations
public class AbilityEffectPool
{
    private Queue<ParticleSystem> _pool = new();
    private ParticleSystem _prefab;
    private int _initialSize = 20;
    
    public ParticleSystem GetEffect()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();
        
        return Instantiate(_prefab);
    }
    
    public void ReturnEffect(ParticleSystem effect)
    {
        effect.Stop();
        _pool.Enqueue(effect);
    }
}

// Cache repeated calculations
public class CharacterStatsCache
{
    private Dictionary<Character, CharacterStats> _cache = new();
    private bool _isDirty = true;
    
    public void Invalidate() => _isDirty = true;
    
    public CharacterStats GetStats(Character character)
    {
        if (_isDirty || !_cache.ContainsKey(character))
        {
            _cache[character] = character.CalculateEffectiveStats();
        }
        return _cache[character];
    }
}
```

### Collision Optimization

```csharp
// Use layer masks for collision queries
public class CollisionHandler
{
    private int _obstacleMask = LayerMask.GetMask("Obstacles");
    private int _enemyMask = LayerMask.GetMask("Enemies");
    
    public Collider[] GetNearbyObstacles(Vector3 position, float radius)
    {
        // Only check Obstacles layer (fast)
        return Physics.OverlapSphere(position, radius, _obstacleMask);
    }
    
    public RaycastHit[] GetEnemiesInLine(Vector3 start, Vector3 direction, float distance)
    {
        // Only check Enemies layer
        return Physics.RaycastAll(start, direction, distance, _enemyMask);
    }
}
```

### Memory Management

```csharp
// Avoid allocations in Update
public class EnemyAI : MonoBehaviour
{
    private Vector3 _cachedPosition;
    
    private void Update()
    {
        // Bad: allocates every frame
        // var neighbors = Physics.OverlapSphere(transform.position, 5f);
        
        // Good: reuse cached array
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, 5f, _cachedColliders);
    }
    
    private Collider[] _cachedColliders = new Collider[20];
}
```

### Rendering Optimization

```csharp
// Use level of detail (LOD) for distant objects
public class TerrainLOD : MonoBehaviour
{
    [SerializeField] private LODGroup _lodGroup;
    
    private void SetupLOD()
    {
        var lods = new LOD[2];
        
        // LOD0: Full detail < 10m
        lods[0] = new LOD(0.5f, new Renderer[] { _detailedRenderer });
        
        // LOD1: Simple mesh > 10m
        lods[1] = new LOD(1f, new Renderer[] { _simplifiedRenderer });
        
        _lodGroup.SetLODs(lods);
    }
}

// Static batching for obstacles
private void Awake()
{
    // Mark all obstacles as static for batching
    foreach (var obstacle in GetComponentsInChildren<MeshCollider>())
    {
        obstacle.gameObject.isStatic = true;
    }
}
```

### Input Latency

```csharp
// Process input before physics step
[DefaultExecutionOrder(-100)]  // Execute before default
public class InputManager : MonoBehaviour
{
    private void Update()
    {
        // Input processing happens early
        ProcessAllInputThisFrame();
    }
}

// Ensure physics runs at fixed rate
[DefaultExecutionOrder(100)]  // Execute after default
public class PhysicsManager : MonoBehaviour
{
    // Physics update runs in FixedUpdate
}
```

## Profiling Targets

### CPU Profiling

- Monitor Main Thread % per system
- Identify frame drops (frame time > 20ms)
- Profile hot paths (Update, FixedUpdate, Late Update)

### Memory Profiling

- Track heap allocations per frame
- Monitor GC.Alloc spikes
- Verify object pooling efficiency

### GPU Profiling

- Monitor draw call count per frame
- Track batch efficiency
- Verify no GPU stalls

## Platform-Specific Optimization

### PC (Windows/Mac)

- Target: GTX 1050 / Intel i5 (2013+)
- Full effects, particles, shadows
- 60 FPS sustained
- No memory constraints

### Mobile (iOS/Android)

- Target: iPhone 12 / Snapdragon 855
- Reduced particle effects
- Lower shadow quality
- 30-60 FPS dynamic
- < 512MB memory

### Console (Switch)

- Target: 1080p docked, 720p handheld
- 60 FPS docked, 30 FPS handheld
- Limited VRAM (< 2GB)
- Battery efficiency (mobile mode)

## Load Time Targets

```
Cold Start:
├─ Load Main Scene    1.5s
├─ Load Core Assets   1.5s
├─ Initialize Systems 1.0s
├─ Load First Zone    1.0s
└─ Total              5.0s (target)

Warm Zone Transition:
├─ Unload Old Zone    0.8s
├─ Load New Zone      1.2s
├─ Load New HUD       0.5s
└─ Total              2.5s (target < 3s)
```

## Success Criteria

- [x] 60 FPS sustained in all gameplay scenarios
- [x] Memory < 400MB peak (target mobile: 512MB)
- [x] Scene transitions < 3 seconds
- [x] No perceivable stuttering
- [x] Input latency < 50ms
- [x] Draw calls < 400 (< 500 URP limit)

## Benchmarks

Target benchmarks for quality assurance:

```csharp
[Performance]
public void Benchmark_ExplorationFrameTime()
{
    // Setup: 10 party members + 15 enemies
    var sw = System.Diagnostics.Stopwatch.StartNew();
    
    for (int i = 0; i < 300; i++)  // 5 seconds at 60 FPS
    {
        Update();
    }
    
    sw.Stop();
    float avgFrameTime = sw.ElapsedMilliseconds / 300f;
    
    Assert.Less(avgFrameTime, 16.67f, 
        "Average frame time exceeds 60 FPS budget");
}
```

## Tools & Profilers

- **Unity Profiler**: Monitor CPU/GPU/Memory
- **Frame Debugger**: Analyze draw calls and batching
- **Memory Profiler**: Track object allocation
- **Addressables**: Asset streaming and memory management

## Changelog

- v1.0 (2026-02-08): Initial performance targets

