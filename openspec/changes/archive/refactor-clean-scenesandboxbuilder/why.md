# Why: Refactor SceneSandboxBuilder Cleanup

Recent refactoring has left fallback logic, unused properties, and legacy code paths in SceneSandboxBuilder. Removing these will:
- Reduce code complexity
- Improve maintainability
- Ensure only necessary logic remains
- Prevent confusion for future contributors

This change is non-breaking and will not affect sandbox features.