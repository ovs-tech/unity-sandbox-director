Design notes: Systems.Persistence → Systems.MiniTimeline integration

1) Integration overview
- Provide a small adapter in MiniTimeline (editor/runtime) that maps MiniTimeline's in-memory project model to a persistence DTO. Use `Systems.Persistence` for serialization/storage and retrieval.
- Add Option C async path:
  - Task-based async in persistence core (`IDataService`, `BaseDataService`, `FileDataService`, `GamePersistenceManager`)
  - Coroutine wrappers in `MiniTimelineDirector` for Unity-friendly use

2) Data model (minimal)
- Project container:
  - `projectId` (GUID)
  - `schemaVersion` (int)
  - `name` (string)
  - `createdAt` (ISO8601)
  - `modifiedAt` (ISO8601)
  - `tracks` (array of track DTOs)
  - `metadata` (map; arbitrary key/value for editor flags)

- Track DTO:
  - `id` (GUID)
  - `type` (enum: Actor, Camera, Generic)
  - `clips` (array of clip DTOs)
  - `bindings` (lightweight references e.g., GUID or address)

- Clip DTO:
  - `id` (GUID)
  - `start` (float)
  - `duration` (float)
  - `properties` (map)

3) Serialization and addresses
- Use `Systems.Persistence` to serialize DTOs to JSON or binary per existing configs. Persist asset references as GUIDs or addressable keys rather than embedding full assets.
- Async persistence path performs serialization and file IO off the main thread where safe.

4) Versioning & migration
- Embed `schemaVersion` at the project root.
- Implement simple migration functions keyed by version that transform older DTOs to current schema.

5) Editor integration points
- Expose `SaveProject(projectId, path, options)` and `LoadProject(path)` adapter methods in MiniTimeline.
- Hook existing editor Save/Load buttons and autosave to these adapters.
- Add `SaveProjectAsync` and `LoadProjectAsync` coroutine wrappers in `MiniTimelineDirector`.
- Keep existing synchronous `SaveProject` and `LoadProject` APIs for compatibility.

6) Async API details
- `GamePersistenceManager` adds:
  - `Task SaveFileAsync(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, bool overwrite = true, CancellationToken token = default)`
  - `Task<T> LoadFileAsync<T>(ISubsystemPersistence subsystem, string saveName = null, string fileName = null, CancellationToken token = default)`
- `IDataService` and `BaseDataService` add namespace/file-level async methods for save/load.
- `FileDataService` uses async file APIs (`ReadAllTextAsync`, `WriteAllTextAsync`) plus background serialization/deserialization.

7) Threading rules
- Unity object access must remain on main thread.
- Background async operations may perform only pure serialization and file IO.
- `MiniTimelineDirector.SetProject()` must run only from coroutine completion on main thread.

8) Testing & validation
- Unit tests: roundtrip serialization (in-memory → persist → load → compare) with representative projects.
- Migration tests: load vN project and validate fields map to vN+1.
- Async tests: async data service roundtrip, async manager subsystem roundtrip, async director load wrapper success/failure.

9) Trade-offs
- Keep DTO minimal to reduce coupling. Avoid serializing engine-heavy runtime state; persist only the authoritative timeline model.
- Option C increases API surface slightly but minimizes adoption risk and keeps Unity call sites ergonomic.
