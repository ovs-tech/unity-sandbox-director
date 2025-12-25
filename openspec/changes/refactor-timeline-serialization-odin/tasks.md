# Tasks: Refactor Timeline Serialization with Odin Serializer

## Implementation Checklist

### Phase 1: Preparation & Setup
- [ ] **1.1** Review Odin Serializer documentation for Unity integration best practices
- [ ] **1.2** Create feature branch: `feature/odin-serialization-refactor`
- [ ] **1.3** Backup existing timeline projects to `ProjectBackups/` folder
- [ ] **1.4** Create unit test project structure for serialization tests
- [ ] **1.5** Document current project file format (schema) for reference

### Phase 2: Add Odin Attributes to Runtime Classes
- [ ] **2.1** Add `[Serializable]` attribute to all track classes:
  - [ ] `MiniTrackBase<TClip>`
  - [ ] `AnimTrack`
  - [ ] `AnimatorTrack`
  - [ ] `MorphTrack`
  - [ ] `MovementTrack`
  - [ ] `SignalTrack`
  - [ ] `UmaWardrobeTrack`
  - [ ] `UMAExpressionTrack`
- [ ] **2.2** Add `[OdinSerialize]` to serializable fields in `MiniTrackBase`:
  - [ ] `clips` list
  - [ ] Track properties (Id, BindKey, Enabled, EvaluateMode)
- [ ] **2.3** Add `[NonSerialized]` to transient fields in `MiniTrackBase`:
  - [ ] `targetObject`
  - [ ] `isBound`
  - [ ] `isPrepared`
  - [ ] `wasActive`
- [ ] **2.4** Add `[Serializable]` attribute to all clip classes:
  - [ ] `MiniClipBase`
  - [ ] `AnimClip`
  - [ ] `AnimatorClip`
  - [ ] `MorphClip`
  - [ ] `MovementClip`
  - [ ] `SignalClip`
  - [ ] `UmaWardrobeClip`
  - [ ] `UMAExpressionClip`
- [ ] **2.5** Add `[OdinSerialize]` to clip properties (convert from payload dictionary to typed fields where needed)
- [ ] **2.6** Verify compilation after adding attributes

### Phase 3: Update MiniTimelineProject
- [ ] **3.1** Change `MiniTimelineProject.tracks` from `List<TrackData>` to `List<IMiniTrack>`
- [ ] **3.2** Add `[OdinSerialize]` attribute to `tracks` field
- [ ] **3.3** Add `[Serializable]` to `MiniTimelineProject` class
- [ ] **3.4** Add `[Serializable]` to `ProjectMetadata` class
- [ ] **3.5** Update `MiniTimelineConstants` if needed (keep track type strings for backward compatibility)
- [ ] **3.6** Verify compilation

### Phase 4: Implement Odin-Based ProjectSerializer
- [ ] **4.1** Create `ProjectSerializerOdin.cs` class (keep old `ProjectSerializer` temporarily)
- [ ] **4.2** Implement `SaveWithOdin()` method using `SerializationUtility.SerializeValue()`:
  - [ ] Accept `MiniTimelineProject` parameter
  - [ ] Serialize to JSON format (`DataFormat.JSON`)
  - [ ] Handle exceptions gracefully
  - [ ] Return JSON string
- [ ] **4.3** Implement `LoadWithOdin()` method using `SerializationUtility.DeserializeValue()`:
  - [ ] Accept JSON string parameter
  - [ ] Deserialize to `MiniTimelineProject`
  - [ ] Handle exceptions gracefully
  - [ ] Return project instance
- [ ] **4.4** Implement `SaveToFileWithOdin()` wrapper:
  - [ ] Call `SaveWithOdin()`
  - [ ] Write to file path
  - [ ] Add error handling
- [ ] **4.5** Implement `LoadFromFileWithOdin()` wrapper:
  - [ ] Read from file path
  - [ ] Call `LoadWithOdin()`
  - [ ] Add error handling
- [ ] **4.6** Test basic serialization round-trip with simple project

### Phase 5: Unify Serializer and Remove Factory
- [ ] **5.1** Remove `TrackFactory` conversion methods and registration code
- [ ] **5.2** Ensure a single `ProjectSerializer` API is used for save/load
- [ ] **5.3** Replace all factory usages with direct runtime tracks in project
- [ ] **5.4** Verify no references to `TrackData`/`ClipData` remain in new code paths

### Phase 6: Update MiniTimelineDirector
-- [ ] **6.1** Update `LoadProject()` method:
  - [ ] Use unified `ProjectSerializer.Load()` with Odin
  - [ ] Remove `TrackFactory.CreateTrack()` calls
  - [ ] Directly assign `project.tracks` to `tracks` list
  - [ ] Ensure binding still works
-- [ ] **6.2** Update `SaveProject()` method:
  - [ ] Use unified `ProjectSerializer.Save()` with Odin
  - [ ] Remove `UpdateProjectFromRuntimeTracks()` call (tracks are already in project)
- [ ] **6.3** Update `CreateNewProject()` method:
  - [ ] Initialize `project.tracks` as `List<IMiniTrack>` instead of `List<TrackData>`
- [ ] **6.4** Test load/save cycle in Unity Editor

### Phase 7: Update Track Management Methods
- [ ] **7.1** Update `AddTrack()` method in `MiniTimelineDirector`:
  - [ ] Add runtime track instance directly to `project.tracks`
  - [ ] Remove any TrackData creation logic
- [ ] **7.2** Update `RemoveTrack()` method:
  - [ ] Remove from `project.tracks` by track instance
  - [ ] Update track lookup dictionary
- [ ] **7.3** Update `GetTrack()` methods to work with runtime instances
- [ ] **7.4** Verify track add/remove/update operations work correctly

### Phase 8: Update UI Commands (if necessary)
- [ ] **8.1** Review `AddTrackCommand.cs`:
  - [ ] Ensure it creates runtime track instances
  - [ ] Remove any TrackData references
- [ ] **8.2** Review `RemoveTrackCommand.cs`:
  - [ ] Ensure it removes runtime instances
- [ ] **8.3** Review `UpdateTrackSettingsCommand.cs`:
  - [ ] Remove `trackData` lookup logic
  - [ ] Update track properties directly
- [ ] **8.4** Review clip commands (`CreateClipCommand`, `DeleteClipCommand`, etc.):
  - [ ] Ensure they work with runtime clip instances
  - [ ] Remove any ClipData references
- [ ] **8.5** Test all commands in editor UI

### Phase 9: Testing & Validation
- [ ] **9.1** Create unit tests for Odin serialization:
  - [ ] Test `AnimTrack` serialization round-trip
  - [ ] Test `MorphTrack` serialization round-trip
  - [ ] Test `MovementTrack` serialization round-trip
  - [ ] Test all other track types
  - [ ] Test nested clip serialization
  - [ ] Test polymorphic deserialization (interface → concrete type)
- [ ] **9.2** Test legacy project migration:
  - [ ] Load each existing test project
  - [ ] Verify track count matches
  - [ ] Verify clip count and properties match
  - [ ] Verify playback behavior is identical
- [ ] **9.3** Test edge cases:
  - [ ] Empty project (no tracks)
  - [ ] Project with single track
  - [ ] Project with 50+ tracks
  - [ ] Tracks with no clips
  - [ ] Tracks with 100+ clips
  - [ ] Missing bind keys
  - [ ] Null or invalid data
- [ ] **9.4** Performance testing:
  - [ ] Benchmark load time for medium project (10 tracks, 50 clips)
  - [ ] Benchmark save time
  - [ ] Compare against legacy performance
  - [ ] Verify memory usage is acceptable
- [ ] **9.5** Integration testing:
  - [ ] Load project in editor UI
  - [ ] Play timeline and verify playback
  - [ ] Add/remove tracks via UI
  - [ ] Create/delete/move clips via UI
  - [ ] Save and reload, verify persistence
  - [ ] Test undo/redo operations

### Phase 10: Cleanup
- [ ] **10.1** Remove old `ProjectSerializer` legacy methods
- [ ] **10.2** Remove `TrackData` and `ClipData` classes
- [ ] **10.3** Delete `TrackFactory` file entirely
- [ ] **10.4** Update inline comments to reference new architecture
- [ ] **10.5** Update docs to state no legacy project support

### Phase 11: Documentation
- [ ] **11.1** Update `Assets/Scripts/MiniTimeline/README.md`:
  - [ ] Document Odin serialization approach
  - [ ] Explain how to add new track types
  - [ ] Note that `TrackData` is deprecated
- [ ] **11.2** Update XML documentation comments in:
  - [ ] `MiniTimelineProject.cs`
  - [ ] `ProjectSerializerOdin.cs`
  - [ ] Track/Clip base classes
- [ ] **11.3** Create `docs/serialization-architecture.md` design doc
- [ ] **11.4** Update GDD or technical specs if needed

### Phase 12: Final Cleanup
- [ ] **12.1** Final code review and cleanup

## Validation Criteria

Each phase should meet these criteria before proceeding:

### Code Quality:
- No compilation errors
- No new warnings introduced
- All existing tests pass
- Code follows project conventions

### Functionality:
- Existing features work identically
- No data loss during save/load
- Performance is equal or better

### Testing:
- Unit tests written for new code
- Integration tests pass
- Manual testing successful

## Dependencies Between Tasks

- **Phase 2** must complete before **Phase 3** (attributes needed before project changes)
- **Phase 3** must complete before **Phase 4** (project structure needed for serializer)
- **Phase 4** must complete before **Phase 5** (Odin serializer needed before migration)
- **Phase 5** must complete before **Phase 6** (migration needed before director update)
- **Phase 6-8** can be done in parallel after Phase 5
- **Phase 9** requires Phases 6-8 complete
- **Phase 10-11** can overlap with Phase 9
- **Phase 12** only after extended testing period

## Rollback Points

Safe rollback points if issues are discovered:
1. **After Phase 2**: Can revert attributes without impact
2. **After Phase 5**: Backward compatibility ensures old format still works
3. **After Phase 9**: New system fully tested, can still fall back to legacy
4. **After Phase 12**: Full commitment, no easy rollback (ensure confidence before this point)

## Estimated Timeline

- **Phases 1-2**: 0.5 days (setup + attributes)
- **Phases 3-5**: 1.5 days (core serialization)
- **Phases 6-8**: 1 day (integration)
- **Phase 9**: 1 day (testing)
- **Phases 10-11**: 0.5 days (documentation)
- **Phase 12**: 0.5 days (final cleanup, after stabilization period)
- **Total**: 5 days implementation + 2-4 weeks stabilization
