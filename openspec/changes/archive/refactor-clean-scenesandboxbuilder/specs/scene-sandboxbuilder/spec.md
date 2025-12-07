## MODIFIED Requirements

### Requirement: SceneSandboxBuilder class contains only necessary logic
The SceneSandboxBuilder class MUST NOT contain fallback logic, unused properties, or unused methods.

#### Scenario: After refactor, fallback logic and unused code are removed
- Given the refactored SceneSandboxBuilder
- When fallback logic, unused properties, and unused methods are identified
- Then they are removed without affecting required functionality

### Requirement: SceneSandboxBuilder remains functional
The SceneSandboxBuilder class MUST continue to provide all expected sandbox features after cleanup.

#### Scenario: After cleanup, sandbox features work as before
- Given the cleaned SceneSandboxBuilder
- When the sandbox is used in editor and play mode
- Then all expected features (placement, selection, transform, save/load) work as before
