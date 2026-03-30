# Change: mini-timeline-persistence-integration

Change-id: mini-timeline-persistence-integration

Summary
- Integrate `Systems.Persistence` with `Systems.MiniTimeline` so MiniTimeline projects (timeline, tracks, clips, bindings, metadata) can be saved, loaded, and versioned using the existing persistence subsystem.
- Add non-breaking async save/load support (Option C: Task-based core APIs plus coroutine wrappers) to reduce main-thread stalls during project IO.

Goals
- Provide a minimal, backwards-compatible save/load format for MiniTimeline projects.
- Use `Systems.Persistence` APIs for storage, serialization, and versioning where possible.
- Add validation and simple migration/versioning metadata to guard compatibility.
- Provide async subsystem save/load APIs in `GamePersistenceManager` and data services.
- Provide coroutine-based async wrappers in `MiniTimelineDirector` while preserving existing sync APIs.

Scope
- ADDED: persistence support for project-level save/load for MiniTimeline editor and runtime use.
- ADDED: async load/save APIs for file-per-subsystem persistence paths.
- MODIFIED: MiniTimeline codepaths to call persistence APIs; documentation and tests.
- OUT OF SCOPE: large refactors of `Systems.Persistence` internals, addressing unrelated bugs in MiniTimeline playback.

Stakeholders
- Systems.MiniTimeline owners
- Systems.Persistence owners
- Editor tooling team (for autosave/manual save UI)

Risks & Mitigations
- Risk: mismatched serialization schemas across versions. Mitigation: include schema-version metadata and a simple migration path in design.md.
- Risk: large project size/asset references. Mitigation: persist lightweight references (GUIDs/addresses) and rely on existing asset-addressing in `Systems.Persistence`.
- Risk: Unity main-thread violations from async continuations. Mitigation: restrict Unity object mutation (`SetProject`, events, bindings) to main-thread coroutine completion path.
- Risk: API churn for existing callers. Mitigation: additive async methods only; existing sync methods remain supported.

Deliverables
- proposal.md (this file)
- tasks.md (ordered, verifiable work items)
- design.md (integration and data model)
- spec deltas in `specs/` for validation and review
- tests for async roundtrip and main-thread-safe load application
