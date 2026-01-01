# Design: Refactor Timeline Serialization with Odin Serializer

## Architecture Overview

### Current Architecture
```
MiniTimelineProject (JSON serializable)
├── List<TrackData>               // Intermediate DTO
│   ├── List<ClipData>            // Intermediate DTO
│   └── Dictionary<string, object> // Generic properties
│
TrackFactory (Conversion Layer)   // ~1200 lines
├── CreateTrack(TrackData)         // DTO → Runtime
├── ConvertRuntimeTrackToData()    // Runtime → DTO
├── ExtractClipPayload()           // Complex type → Dictionary
└── ParseClipData()                // Dictionary → Complex type
│
Runtime Layer
├── IMiniTrack implementations     // AnimTrack, MorphTrack, etc.
└── IMiniClip implementations      // AnimClip, MorphClip, etc.
```

### Target Architecture
```
MiniTimelineProject (Odin serializable)
├── List<IMiniTrack>               // Direct runtime instances
│   └── List<IMiniClip>            // Direct runtime instances
│
ProjectSerializer (Thin wrapper)   // ~200 lines
├── SaveWithOdin()
└── LoadWithOdin()
│
Runtime Layer (with Odin attributes)
├── IMiniTrack implementations     // [OdinSerialize] attributes
└── IMiniClip implementations      // [OdinSerialize] attributes
```

## Key Design Decisions

### 1. Serialization Format: JSON vs Binary

**Decision**: Use Odin's JSON format (DataFormat.JSON)

**Rationale**:
- Human-readable for debugging and version control
- Text diff-friendly for collaboration
- Easier migration from current JSON format
- Only ~10-15% larger than binary
- Performance difference negligible for timeline data size

**Alternative considered**: Binary format for smaller file size, but rejected due to:
- Loss of human readability
- Harder debugging
- Team preference for text-based formats

### 2. Interface Serialization

**Challenge**: C# interfaces (`IMiniTrack`, `IMiniClip`) aren't directly serializable

**Solution**: Use Odin's polymorphic serialization with concrete type hints

```csharp
[Serializable]
public class MiniTimelineProject
{
    [OdinSerialize] // Odin handles polymorphic list
    public List<IMiniTrack> tracks = new List<IMiniTrack>();
}

// Concrete implementations
[Serializable]
public class AnimTrack : MiniTrackBase<AnimClip>
{
    [OdinSerialize]
    private List<AnimClip> clips; // Base class field
    
    // Odin serializes all fields including private
}
```

Odin automatically stores type information and restores correct concrete types.

### 3. Field Serialization Strategy

**Decision**: Use `[OdinSerialize]` on private fields, avoid Unity's `[SerializeField]`

**Rationale**:
- Unity serialization doesn't support interfaces/polymorphism
- Odin can serialize private fields, properties, and auto-properties
- Better encapsulation (keep fields private)
- No need to expose implementation details publicly

```csharp
public class AnimTrack : MiniTrackBase<AnimClip>
{
    [OdinSerialize] private Animator _animator;        // Private field
    [OdinSerialize] private PlayableGraph _playableGraph; // Complex Unity type
    [OdinSerialize] private List<AnimClip> clips;      // Inherited field
}
```

### 4. Unified Serializer (No Legacy Support)

**Decision**: Consolidate responsibilities into a single `ProjectSerializer` that handles save/load with Odin, and eliminate `TrackFactory` and legacy loaders entirely.

**Implementation**:
```csharp
public static class ProjectSerializer
{
    public static string Save(MiniTimelineProject project, bool pretty = true)
    {
        return SerializationUtility.SerializeValue(project, DataFormat.JSON);
    }

    public static MiniTimelineProject Load(string json)
    {
        return SerializationUtility.DeserializeValue<MiniTimelineProject>(json, DataFormat.JSON);
    }
}
```

No legacy format detection or migration is provided.

### 5. MiniTimelineDirector Integration

**Current**: Director loads project, then converts TrackData → runtime tracks
**Target**: Director loads project with runtime tracks already instantiated

**Changes needed**:
```csharp
public class MiniTimelineDirector : MonoBehaviour
{
    public void LoadProject(string projectName)
    {
        var json = File.ReadAllText(GetProjectPath(projectName));
        project = ProjectSerializer.Load(json); // Returns with runtime tracks
        
        // Tracks are already runtime instances, just bind them
        tracks.Clear();
        tracks.AddRange(project.tracks);
        
        // Bind all tracks
        foreach (var track in tracks)
        {
            track.Bind(bindableObjectManager);
        }
    }
}
```

No more `TrackFactory.CreateTrack()` calls needed!

### 5. Handling Non-Serializable Fields

Some runtime fields shouldn't be serialized (Unity objects, temporary state):

**Solution**: Use `[NonSerialized]` or `[OdinSerialize] = false`

```csharp
public class AnimTrack : MiniTrackBase<AnimClip>
{
    // Serialized
    [OdinSerialize] private List<AnimClip> clips;
    
    // Not serialized (runtime only)
    [NonSerialized] private Animator _animator;          // Resolved at bind time
    [NonSerialized] private PlayableGraph _playableGraph; // Created at prepare time
    [NonSerialized] private bool _isGraphInitialized;     // Runtime state
}
```

**Guidelines**:
- Serialize: Data that defines the timeline (clips, times, settings)
- Don't serialize: Unity object references (resolved via BindKey), transient state, caches

### 6. Clip Payload Data

**Current**: Clips store type-specific data in `Dictionary<string, object>`
**Target**: Clips use strongly-typed fields

**Before**:
```csharp
public class ClipData
{
    public Dictionary<string, object> payload; // Generic
}

// Usage in factory
payload["animationAsset"] = clip.animationAsset;
payload["speed"] = clip.speed;
```

**After**:
```csharp
[Serializable]
public class AnimClip : MiniClipBase
{
    [OdinSerialize] public string animationAsset;
    [OdinSerialize] public float speed = 1f;
    [OdinSerialize] public AnimationWrapMode wrapMode = AnimationWrapMode.Loop;
    // ... all fields directly serializable
}
```

Benefits: Type safety, IntelliSense, no casting, no runtime errors

## Migration Path

### Phase 1: Add Odin Serialization
- Add `[Serializable]` and `[OdinSerialize]` attributes to track/clip classes
- Keep existing `TrackData` and `TrackFactory` (not used but present)
- Test Odin serialization in parallel

### Phase 2: Update ProjectSerializer (Unified)
- Implement `LoadWithOdin()` and `SaveWithOdin()`
- Add legacy format detection
- Update `MiniTimelineDirector.LoadProject()` to use new loader
- Ensure all existing projects still load via legacy path

### Phase 3: Remove Legacy Code (Immediate Cleanup)
- Delete `TrackData` and `ClipData` classes
- Delete `TrackFactory` conversion methods
- Remove legacy loader after migration period
- Update documentation

## Error Handling

### Serialization Errors
```csharp
try
{
    var json = SerializationUtility.SerializeValue(project, DataFormat.JSON);
}
catch (Exception e)
{
    Debug.LogError($"Failed to serialize project: {e.Message}");
    // Fallback: try legacy serialization
}
```

### Deserialization Errors
```csharp
try
{
    project = SerializationUtility.DeserializeValue<MiniTimelineProject>(json, DataFormat.JSON);
}
catch (Exception e)
{
    Debug.LogError($"Failed to deserialize project: {e.Message}");
    // Try legacy format
    project = LoadLegacyJson(json);
}
```

## Performance Considerations

### Current Performance:
- Load: ~50-100ms for medium project (10 tracks, 50 clips)
  - 20ms JSON parse
  - 30-80ms TrackFactory conversions

### Expected Performance:
- Load: ~30-50ms for same project
  - 30-50ms Odin deserialization (includes object construction)
  - 0ms conversion (eliminated)

### Optimization Opportunities:
- Odin's binary format: 2-3x faster (if readability not required)
- Lazy deserialization: Defer track binding until needed
- Pooling: Reuse track/clip instances (future enhancement)

## Testing Strategy

### Unit Tests:
1. **Serialization round-trip**: Save → Load → Compare
2. **All track types**: Ensure each track type serializes correctly
3. **Polymorphism**: Verify interface types restore to correct concrete types
4. **Legacy compatibility**: Load old projects, verify data integrity
5. **Edge cases**: Empty projects, null fields, large projects

### Integration Tests:
1. Load existing project in editor, verify playback
2. Create new project, save, load, verify identical
3. Modify project, save, reload, verify changes persist
4. Test with all track types populated

### Performance Tests:
1. Benchmark load/save times vs. current implementation
2. Measure memory usage during serialization
3. Test with large projects (50+ tracks, 500+ clips)

## Rollback Plan

If issues arise after deployment:
1. **Immediate**: Revert to legacy serialization code (keep in codebase initially)
2. **Data recovery**: All projects have legacy format backup (pre-conversion)
3. **Gradual rollout**: Enable Odin serialization via feature flag initially

## Future Enhancements

Once Odin serialization is stable:
1. **Binary format option**: For production builds (smaller, faster)
2. **Streaming**: Load large projects progressively
3. **Compression**: Reduce file size further
4. **Versioning**: Easier schema evolution with Odin's version tolerance
5. **Cloud sync**: Serialize to cloud storage with minimal overhead
