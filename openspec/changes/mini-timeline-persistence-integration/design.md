Design notes: Systems.Persistence → Systems.MiniTimeline integration

1) Integration overview
- Provide a small adapter in MiniTimeline (editor/runtime) that maps MiniTimeline's in-memory project model to a persistence DTO. Use `Systems.Persistence` for serialization/storage and retrieval.

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

4) Versioning & migration
- Embed `schemaVersion` at the project root.
- Implement simple migration functions keyed by version that transform older DTOs to current schema.

5) Editor integration points
- Expose `SaveProject(projectId, path, options)` and `LoadProject(path)` adapter methods in MiniTimeline.
- Hook existing editor Save/Load buttons and autosave to these adapters.

6) Testing & validation
- Unit tests: roundtrip serialization (in-memory → persist → load → compare) with representative projects.
- Migration tests: load vN project and validate fields map to vN+1.

7) Trade-offs
- Keep DTO minimal to reduce coupling. Avoid serializing engine-heavy runtime state; persist only the authoritative timeline model.
