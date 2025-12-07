# Design: SceneSandboxBuilder Cleanup

## Architectural Reasoning
This change is a straightforward code cleanup. No new patterns or systems are introduced. The only architectural consideration is to ensure that all fallback logic and unused code paths are truly obsolete and not required for backward compatibility or edge-case handling.

## Trade-offs
- Simplicity and maintainability are prioritized over legacy support for fallback logic.
- All removals will be validated by tests and manual review to ensure no required functionality is lost.

## Dependencies
- None outside SceneSandboxBuilder.

## Validation
- Build and run the project after cleanup.
- Run all relevant tests and verify sandbox features in editor and play mode.
