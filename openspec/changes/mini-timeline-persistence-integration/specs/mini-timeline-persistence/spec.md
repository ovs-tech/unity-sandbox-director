## ADDED Requirements

### Requirement: MiniTimeline Persistence Integration
MiniTimeline projects MUST be saveable and loadable via `Systems.Persistence` APIs.

#### Scenario: Save project to disk
- **GIVEN** a MiniTimeline project in the editor
- **WHEN** the user triggers Save
- **THEN** the project is serialized into the configured persistence format and written to storage with `schemaVersion` and `projectId` metadata

#### Scenario: Load project from disk
- **GIVEN** a saved MiniTimeline project file written by the persistence subsystem
- **WHEN** the user triggers Load for that file
- **THEN** the MiniTimeline editor reconstructs the timeline with tracks, clips, and bindings matching the saved DTO

### Requirement: Schema Versioning Metadata
Persisted projects MUST include `schemaVersion` to enable future migrations.

#### Scenario: Load older schema and migrate
- **GIVEN** a project file with `schemaVersion` = N (older)
- **WHEN** the user loads the file
- **THEN** the loader applies a migration to transform DTOs to current schema and warns if automatic migration fails

### Requirement: Lightweight Binding References
Asset references (actor, object, camera bindings) MUST be persisted as lightweight references (GUIDs or addressable keys), not heavy asset blobs.

#### Scenario: Resolve bindings after load
- **GIVEN** a project with bindings saved as GUIDs/addresses
- **WHEN** project is loaded
- **THEN** the system resolves available assets; missing assets are reported to the user and marked unresolved in the editor

### Requirement: Persistence Roundtrip Fidelity
Save/Load MUST support a roundtrip test: save -> load -> compare equals for canonical fields.

#### Scenario: Roundtrip equality
- **GIVEN** a representative MiniTimeline project
- **WHEN** saved and then loaded via persistence
- **THEN** the resulting in-memory model equals the original for canonical fields (IDs, track structure, clip timings, metadata)

### Requirement: Async Persistence APIs
The persistence layer SHALL provide asynchronous save/load APIs for subsystem files without removing existing synchronous APIs.

#### Scenario: Async APIs are additive
- **GIVEN** existing synchronous persistence calls in runtime/editor code
- **WHEN** async persistence support is introduced
- **THEN** existing synchronous APIs remain available and behavior-compatible
- **AND** new async APIs are exposed for subsystem save/load operations

#### Scenario: Async save from MiniTimeline
- **GIVEN** an open MiniTimeline project
- **WHEN** `SaveProjectAsync` is triggered
- **THEN** save IO runs asynchronously through `GamePersistenceManager` async APIs
- **AND** completion is reported via coroutine callback on the main thread

#### Scenario: Async load from MiniTimeline
- **GIVEN** a persisted MiniTimeline project
- **WHEN** `LoadProjectAsync` is triggered
- **THEN** load IO and deserialization run asynchronously
- **AND** `SetProject` executes on the main thread after successful completion

### Requirement: Main-Thread Safety for Async Load Application
Unity object mutation during async load SHALL occur only on the Unity main thread.

#### Scenario: Apply loaded project on main thread
- **GIVEN** an async load operation has completed with project data
- **WHEN** the director applies the loaded data
- **THEN** track binding, project events, and `SetProject` execution occur on the main thread
- **AND** no Unity API calls are performed from background threads

## MODIFIED Requirements

### Requirement: Editor Persistence Integration
MiniTimeline editor save/load entry points SHALL call persistence adapter APIs described in design.md.

#### Scenario: Editor save path uses adapter
- **GIVEN** the editor Save action is invoked
- **WHEN** the project is persisted
- **THEN** the save operation routes through persistence adapter APIs (sync or async)

#### Scenario: Editor load path uses adapter
- **GIVEN** the editor Load action is invoked
- **WHEN** a project is loaded
- **THEN** the load operation routes through persistence adapter APIs (sync or async)

## Notes
- Implementation must include unit tests for each Scenario above. Tests belong to implementation phase.
