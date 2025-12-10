# Proposal: Refactor SceneSandboxBuilder - Remove Fallbacks & Unused Logic

## Motivation
Recent refactoring of the SceneSandboxBuilder class has left fallback logic, unused properties, and legacy code paths that are no longer required. Cleaning these up will reduce complexity, improve maintainability, and ensure the class only contains necessary logic.

## Scope
- Only the SceneSandboxBuilder class in Assets/Scripts/SceneSandbox/Core/SceneSandboxBuilder.cs
- Remove fallback logic, unused properties, and unused methods
- No functional changes; sandbox features must remain unaffected

## Out of Scope
- Changes to other classes or systems
- New features or architectural changes

## Risks
- Minor: accidental removal of still-used logic. Mitigated by validation and testing.

## Related
- Follows OpenSpec conventions and project coding standards
