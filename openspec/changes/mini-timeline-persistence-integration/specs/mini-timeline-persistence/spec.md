## ADDED Requirements

Requirement: MiniTimeline projects MUST be saveable and loadable via `Systems.Persistence` APIs.

#### Scenario: Save project to disk
- Given a MiniTimeline project in the editor
- When the user triggers Save
- Then the project is serialized into the configured persistence format and written to storage with `schemaVersion` and `projectId` metadata

#### Scenario: Load project from disk
- Given a saved MiniTimeline project file written by the persistence subsystem
- When the user triggers Load for that file
- Then the MiniTimeline editor reconstructs the timeline with tracks, clips, and bindings matching the saved DTO

Requirement: Persisted projects MUST include `schemaVersion` to enable future migrations.

#### Scenario: Load older schema and migrate
- Given a project file with `schemaVersion` = N (older)
- When the user loads the file
- Then the loader applies a migration to transform DTOs to current schema and warns if automatic migration fails

Requirement: Asset references (actor, object, camera bindings) MUST be persisted as lightweight references (GUIDs or addressable keys), not heavy asset blobs.

#### Scenario: Resolve bindings after load
- Given a project with bindings saved as GUIDs/addresses
- When project is loaded
- Then the system resolves available assets; missing assets are reported to the user and marked unresolved in the editor

Requirement: Save/Load MUST support a roundtrip test: save → load → compare equals for canonical fields.

#### Scenario: Roundtrip equality
- Given a representative MiniTimeline project
- When saved and then loaded via persistence
- Then the resulting in-memory model equals the original for canonical fields (IDs, track structure, clip timings, metadata)

## MODIFIED Requirements

- MiniTimeline editor: Add calls to the persistence adapter APIs described in design.md for Save/Load events. The editor UI itself is out-of-scope for this spec but must call adapter methods.

## Notes
- Implementation must include unit tests for each Scenario above. Tests belong to implementation phase.
