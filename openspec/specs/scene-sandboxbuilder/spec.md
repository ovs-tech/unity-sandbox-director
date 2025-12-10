# scene-sandboxbuilder Specification

## Purpose
TBD - created by archiving change refactor-gizmo-scenesandboxbuilder. Update Purpose after archive.
## Requirements
### Requirement: SandboxGizmoRenderer component handles all gizmo rendering
SandboxGizmoRenderer MUST be a dedicated component that encapsulates all gizmo rendering logic, supporting scene-level, object-level, and placement preview gizmos with configurable visibility.

#### Scenario: SandboxGizmoRenderer is created as separate component
- **GIVEN** the need to extract gizmo rendering from SceneSandboxBuilder
- **WHEN** SandboxGizmoRenderer is created in SceneSandbox.Core namespace
- **THEN** it inherits from MonoBehaviour
- **AND** it implements OnDrawGizmos() for Unity lifecycle integration
- **AND** it contains all gizmo drawing methods (DrawSceneGizmos, DrawObjectGizmos, etc.)
- **AND** it follows project naming conventions (_camelCase for fields, PascalCase for methods)

#### Scenario: SandboxGizmoRenderer initializes with dependencies
- **GIVEN** SandboxGizmoRenderer component added to GameObject
- **WHEN** Initialize() is called from SceneSandboxBuilder.InitializeComponents()
- **THEN** renderer receives references to SceneSandboxBuilder, PlacementSystem, SelectionManager, GridManager, and stageArea Transform
- **AND** all dependencies are validated (null checks)
- **AND** renderer is ready to draw gizmos based on subsystem state

#### Scenario: SandboxGizmoRenderer draws scene-level gizmos
- **GIVEN** SandboxGizmoRenderer with _enableSceneGizmos = true
- **WHEN** OnDrawGizmos() is called
- **THEN** scene grid is drawn if _showSceneGrid = true
- **AND** scene bounds are drawn if _showSceneBounds = true
- **AND** stage area is drawn if _showStageAreaGizmo = true
- **AND** placement height is drawn if _showPlacementHeightGizmo = true
- **AND** grid snapping state affects gizmo colors (green when active)

#### Scenario: SandboxGizmoRenderer draws object-level gizmos
- **GIVEN** SandboxGizmoRenderer with _enableGizmos = true
- **AND** SelectionManager has a selected object
- **WHEN** OnDrawGizmos() is called
- **THEN** object bounds are drawn if _showBoundsGizmo = true
- **AND** coordinate axes are drawn if _showAxesGizmo = true
- **AND** manipulation handles are drawn if _showHandlesGizmo = true
- **AND** object bounds are calculated from Renderer or Collider components

#### Scenario: SandboxGizmoRenderer draws placement preview gizmos
- **GIVEN** SandboxGizmoRenderer with PlacementSystem.IsActive = true
- **AND** PlacementSystem.CurrentObject is not null
- **WHEN** OnDrawGizmos() is called
- **THEN** placement validity indicator is drawn (green for valid, red for invalid)
- **AND** grid snap indicator is drawn if _showGridSnapIndicator = true
- **AND** surface normal indicator is drawn if _showSurfaceNormal = true
- **AND** drop indicator gizmo is drawn at placement position

#### Scenario: SandboxGizmoRenderer visibility can be toggled
- **GIVEN** SandboxGizmoRenderer with default visibility settings
- **WHEN** SetGizmoVisibility(false) is called
- **THEN** object-level gizmos are disabled (_enableGizmos = false)
- **AND** OnDrawGizmos() skips object gizmo rendering
- **AND** SetSceneGizmoVisibility(false) disables scene-level gizmos
- **AND** individual gizmo types can be toggled (grid, bounds, axes, handles, etc.)

### Requirement: SceneSandboxBuilder delegates gizmo rendering to SandboxGizmoRenderer
SceneSandboxBuilder MUST delegate all gizmo rendering to a dedicated SandboxGizmoRenderer component to reduce complexity and improve maintainability.

#### Scenario: Gizmo rendering is extracted to dedicated component
- **GIVEN** SceneSandboxBuilder with extracted subsystems (PlacementSystem, SelectionManager, etc.)
- **WHEN** gizmo rendering code is moved to SandboxGizmoRenderer
- **THEN** all gizmos render identically to previous behavior
- **AND** SceneSandboxBuilder complexity is reduced by ~570 lines
- **AND** gizmo behavior is isolated and testable

#### Scenario: Gizmo visibility can be controlled via builder API
- **GIVEN** a SceneSandboxBuilder instance with SandboxGizmoRenderer
- **WHEN** SetGizmoVisibility(bool) or SetSceneGizmoVisibility(bool) is called
- **THEN** the builder delegates to the gizmo renderer
- **AND** gizmo visibility changes accordingly
- **AND** public API contract is maintained for UI consumers

#### Scenario: Gizmo renderer accesses live data from subsystems
- **GIVEN** SandboxGizmoRenderer initialized with subsystem references
- **WHEN** OnDrawGizmos() is called by Unity
- **THEN** renderer queries PlacementSystem for placement state
- **AND** queries SelectionManager for selected objects
- **AND** queries GridManager for grid settings
- **AND** renders gizmos based on current state without caching

### Requirement: SceneSandboxBuilder provides minimal wrapper APIs
SceneSandboxBuilder MUST expose subsystems directly and remove redundant wrapper properties/methods that only forward to subsystems without adding value.

#### Scenario: Redundant wrapper properties are removed
- **GIVEN** wrapper properties like CurrentScene, IsPlacementActive that only forward to subsystems
- **WHEN** consumers need access to scene/placement state
- **THEN** they access subsystem properties directly (e.g., _sceneSerializer.CurrentScene)
- **AND** SceneSandboxBuilder does not duplicate subsystem APIs
- **AND** code clarity improves by removing indirection

#### Scenario: Critical APIs with validation logic are retained
- **GIVEN** wrapper methods that add validation, events, or coordination logic
- **WHEN** evaluating whether to remove wrapper
- **THEN** wrapper is retained if it provides value beyond simple delegation
- **AND** simple pass-through wrappers are removed
- **AND** API consumers are updated to use subsystems directly

