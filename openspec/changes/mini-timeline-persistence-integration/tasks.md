Tasks for mini-timeline-persistence-integration

1. Create openspec scaffolding (proposal, tasks, design, specs). -- Validation: files present in `openspec/changes/mini-timeline-persistence-integration/`.
2. Define data model and versioning rules in `design.md`. -- Validation: design.md contains schema and migration notes.
3. Draft spec delta(s) for the capability `mini-timeline-persistence` with explicit scenarios. -- Validation: spec.md passes `openspec validate` (manual step).
4. Implement adapter layer in `Systems.MiniTimeline` to call `Systems.Persistence` (implementation stage, post-approval). -- Validation: unit tests for save/load roundtrip.
5. Add editor autosave/manual-save UI hooks and documentation. -- Validation: UI calls persistence API; small integration test.
6. Add migration tests and version compatibility checks. -- Validation: tests for older schema loads.
7. Run `openspec validate <id> --strict` and resolve issues. -- Validation: validate returns OK.

Notes:
- Steps 4-6 are implementation-stage tasks and will be executed after this proposal is approved.
