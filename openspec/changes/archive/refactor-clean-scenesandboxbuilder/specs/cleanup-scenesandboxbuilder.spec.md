## MODIFIED Requirements

### Requirement: SceneSandboxBuilder class contains only necessary logic
#### Scenario: After refactor, fallback logic and unused code are removed
- Given the refactored SceneSandboxBuilder
- When fallback logic, unused properties, and unused methods are identified
- Then they are removed without affecting required functionality

### Requirement: SceneSandboxBuilder remains functional
#### Scenario: After cleanup, sandbox features work as before
- Given the cleaned SceneSandboxBuilder
- When the sandbox is used in editor and play mode
- Then all expected features (placement, selection, transform, save/load) work as before
