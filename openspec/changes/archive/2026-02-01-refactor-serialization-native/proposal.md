# Proposal: Native JSON Serialization

## Summary
Replace the dependency on `OdinSerializer` in `ProjectSerializer` with a native Unity `JsonUtility` implementation using `ISerializationCallbackReceiver` to handle polymorphism for Tracks and Clips. This fulfills the requirement to "self serialize" and removes external dependencies for core data persistence.

## Problem
Currently, `ProjectSerializer` relies on `Sirenix.OdinSerializer.SerializationUtility` to handle the polymorphic nature of `IMiniTrack` and `IMiniClip` lists. The user has requested to remove this dependency and implement a self-contained serialization logic.

## Solution
We will refactor `MiniTimelineProject`, `MiniTrackBase`, and `MiniClipBase` to implement `ISerializationCallbackReceiver`.
- **Polymorphism:** Lists of interfaces (`List<IMiniTrack>`, `List<IMiniClip>`) will be serialized into lists of `SerializedWrapper` structs (containing Type and JSON Data) during `OnBeforeSerialize`.
- **Properties:** Auto-properties in base classes will be converted to `[SerializeField]` backing fields to ensure `JsonUtility` captures them.
- **ProjectSerializer:** Will be simplified to wrap `JsonUtility.ToJson` and `JsonUtility.FromJson`.

## Design Details
### Data Structures
- `SerializedWrapper`: A struct holding `string type` and `string data`.
- `MiniTimelineProject`:
  - `List<IMiniTrack> tracks` (Runtime)
  - `List<SerializedWrapper> serializedTracks` (Serialized)
- `MiniTrackBase<TClip>`:
  - `List<TClip> clips` (Runtime)
  - `List<SerializedWrapper> serializedClips` (Serialized)

### Dictionary Support
`ProjectMetadata` contains a `Dictionary<string, object>`. This will be serialized by converting to parallel lists of keys and JSON-serialized values strings.

## Risks
- **Data Migration:** Existing save files using Odin's binary or JSON format will typically be incompatible. Since this is a dev tool/sandbox, we assume breaking changes are acceptable unless migration is requested (none requested).
- **Complex Types:** `JsonUtility` is shallow. Complex nested types in specific tracks must be `[Serializable]`. Most current tracks use simple fields or Unity types (`Vector3`, `AnimationCurve`) which are supported.
