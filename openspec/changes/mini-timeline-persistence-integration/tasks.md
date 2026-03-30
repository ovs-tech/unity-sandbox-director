Tasks for mini-timeline-persistence-integration

1. Create openspec scaffolding (proposal, tasks, design, specs). -- Validation: files present in `openspec/changes/mini-timeline-persistence-integration/`.
2. Define data model and versioning rules in `design.md`. -- Validation: design.md contains schema and migration notes.
3. Draft spec delta(s) for the capability `mini-timeline-persistence` with explicit scenarios. -- Validation: spec.md passes `openspec validate` (manual step).
4. Add async contracts in `IDataService`/`BaseDataService` and implement async IO in `FileDataService`. -- Validation: async file save/load roundtrip test passes.
5. Add async subsystem APIs in `GamePersistenceManager` (`SaveFileAsync`, `LoadFileAsync`). -- Validation: async subsystem roundtrip test passes.
6. Add async coroutine wrappers in `MiniTimelineDirector` (`SaveProjectAsync`, `LoadProjectAsync`) while preserving sync APIs. -- Validation: async director load/save tests pass and existing director tests remain green.
7. Add editor autosave/manual-save integration for async calls where appropriate and document fallback sync behavior. -- Validation: editor flow invokes async path in play/edit mode smoke checks.
8. Add migration/version compatibility tests for existing schema behavior with async load path. -- Validation: older schema test projects still load or fail with clear warnings.
9. Run `openspec validate mini-timeline-persistence-integration --strict` and resolve issues. -- Validation: strict validation returns OK.

Notes:
- Steps 4-8 are implementation-stage tasks and will be executed after this proposal is approved.
