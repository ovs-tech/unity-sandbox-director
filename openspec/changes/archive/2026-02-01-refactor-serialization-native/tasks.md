# Tasks: Native JSON Serialization

- [x] Define `SerializedWrapper` and other helper types in `Scripts/MiniTimeline/Serialization/SerializationTypes.cs` <!-- id: 0 -->
- [x] Refactor `MiniTimelineProject` to implement `ISerializationCallbackReceiver` and replace Odin attributes <!-- id: 1 -->
- [x] Refactor `ProjectMetadata` to handle dictionary serialization <!-- id: 2 -->
- [x] Refactor `MiniTrackBase` to use backing fields and implement `ISerializationCallbackReceiver` for clips <!-- id: 3 -->
- [x] Refactor `MiniClipBase` to use backing fields and remove Odin attributes <!-- id: 4 -->
- [x] Update `ProjectSerializer` to use `JsonUtility` <!-- id: 5 -->
- [x] Verify `MorphTrack` serialization (nested complex types) <!-- id: 6 -->
- [x] Verify `SignalTrack` serialization (custom fields) <!-- id: 7 -->
- [x] Create a test/validation script to confirm save/load loop integrity <!-- id: 8 -->
