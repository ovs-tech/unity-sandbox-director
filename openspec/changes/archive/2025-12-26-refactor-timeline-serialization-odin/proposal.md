# Proposal: Refactor Timeline Serialization with Odin Serializer

## Summary
Eliminate the intermediate `TrackData` and `ClipData` serialization layer by using Odin Serializer to directly serialize track and clip runtime classes (`IMiniTrack`, `IMiniClip` implementations).

## Why
The current MiniTimeline architecture maintains dual representations of timeline data:
1. **Runtime classes**: `IMiniTrack` and `IMiniClip` implementations (e.g., `AnimTrack`, `AnimClip`, `MorphTrack`, etc.)
2. **Serialization DTOs**: `TrackData` and `ClipData` as intermediate JSON-friendly data structures

This creates several issues:
- **Code duplication**: `TrackFactory` contains ~1200 lines of conversion logic between runtime objects and DTOs
- **Maintenance burden**: Every new track/clip property requires updates in 3 places: runtime class, DTO, and conversion methods
- **Error-prone**: Manual serialization/deserialization increases risk of data loss or inconsistency
- **Performance overhead**: Double conversion (runtime → DTO → JSON and reverse) on every save/load
- **Limited serialization**: Current approach can't handle complex types (nested objects, references, polymorphism)

With Odin Serializer already installed in the project, we can leverage its powerful serialization capabilities to:
- Directly serialize runtime track/clip instances
- Support polymorphic serialization (base interface → concrete types)
- Handle complex Unity types (Vector3, Quaternion, Color, etc.)
- Reduce codebase by ~30% in serialization layer
- Simplify future extensions (new track types only require class definition)

## What
Refactor the serialization architecture to:

1. **Add Odin serialization attributes** to track and clip classes
2. **Replace JSON serialization** with Odin's binary or JSON format
3. **Remove intermediate layer**: Delete `TrackData`, `ClipData`, and `TrackFactory` conversion methods
4. **Update `MiniTimelineProject`** to store runtime track instances directly
5. **Modify `ProjectSerializer`** to use Odin serialization APIs
6. **Unify serialization API** by merging `TrackFactory` responsibilities into `ProjectSerializer` and dropping legacy format support

### Key Changes:
- `MiniTimelineProject.tracks`: Change from `List<TrackData>` to `List<IMiniTrack>` with Odin serialization
- Track/Clip classes: Add `[OdinSerialize]` attributes to all serializable fields
- Remove `TrackFactory` conversion layer entirely
- Unify responsibilities in `ProjectSerializer` as the single entrypoint for save/load
- Update `ProjectSerializer` to use `SerializationUtility` from Odin

### Out of Scope:
- Changing the file format (can still use .json extension with Odin's JSON format)
- Modifying track/clip behavior or evaluation logic
- UI changes (editor remains unchanged)
- Addressable asset loading patterns

## How
See `design.md` for architectural details and `tasks.md` for implementation steps.

## Impact

### Benefits:
- **Reduced complexity**: Remove ~1200 lines of conversion code
- **Faster development**: New track types require only class definition, no DTO/factory updates
- **Better maintainability**: Single source of truth for track/clip data
- **Enhanced capabilities**: Support for complex nested data structures
- **Performance**: Single-pass serialization/deserialization

### Risks:
- **Breaking change**: Existing JSON project files will not load; manual recreation is required
- **Learning curve**: Team needs familiarity with Odin serialization attributes
- **Binary format**: If using binary, projects become non-human-readable (mitigated by keeping JSON format option)
- **Dependency**: Tighter coupling to Odin Serializer library

### Migration Path:
No legacy support. Projects saved in the previous JSON DTO format will not be auto-migrated. Editors must open, re-create tracks/clips using runtime classes, and save using the new Odin-based serializer.

## Timeline Estimate
- Design & setup: 0.5 days
- Implementation: 2-3 days
- Testing & migration: 1 day
- **Total**: 3.5-4.5 days

## Dependencies
- Odin Serializer package (already installed)
- No other changes required

## Success Criteria
- [ ] Projects save and load with Odin serialization without data loss (requires Unity testing)
- [x] TrackFactory fully removed; unified `ProjectSerializer` API in place
- [x] All track types (Anim, Morph, Movement, Signal, etc.) serialize correctly (code complete, requires Unity testing)
- [x] Unit tests pass for serialization round-trip (N/A - unit tests not required per project decision)
- [ ] Performance is equal or better than current implementation (requires Unity testing)
- [x] Documentation updated with new serialization approach
