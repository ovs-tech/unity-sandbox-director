# Proposal: Refactor SceneSandboxBuilder - Extract Gizmo Rendering & Remove Redundant Methods

## Why
The SceneSandboxBuilder class (2770 lines) contains extensive gizmo rendering logic (~570 lines) that violates single responsibility principle. Additionally, it has redundant properties and methods that duplicate functionality already provided by extracted subsystems (PlacementSystem, SelectionManager, GridManager, etc.).

Extracting gizmo rendering into a dedicated component will:
- Reduce SceneSandboxBuilder complexity and improve maintainability
- Follow the established pattern of component extraction (PlacementSystem, SelectionManager, etc.)
- Make gizmo behavior testable and reusable
- Simplify the main controller class

Removing redundant wrapper methods/properties will:
- Eliminate duplication with subsystem APIs
- Reduce confusion about which API to use
- Improve code discoverability (use subsystem directly, not wrappers)

## What Changes

### 1. Extract Gizmo Rendering
Move all gizmo-related code from SceneSandboxBuilder to a new `SandboxGizmoRenderer` component:
- **Gizmo drawing methods** (~570 lines):
  - `OnDrawGizmos()`, `DrawSceneGizmos()`, `DrawObjectGizmos()`
  - `DrawSceneGrid()`, `DrawSceneBounds()`, `DrawStageArea()`, `DrawPlacementHeight()`
  - `DrawObjectAxes()`, `DrawManipulationHandles()`, `DrawDropIndicatorGizmos()`, `DrawPlacementPreviewGizmos()`
  - Helper: `CalculateObjectBounds()`
  
- **Gizmo configuration fields** (~14 fields):
  - `_enableGizmos`, `_enableSceneGizmos`
  - `_showBoundsGizmo`, `_showAxesGizmo`, `_showHandlesGizmo`, `_showSceneGrid`, `_showSceneBounds`, `_showStageAreaGizmo`, `_showPlacementHeightGizmo`
  - `_showGridSnapIndicator`, `_showSurfaceNormal`
  - `_gizmoBoundsColor`, `_gizmoAxisLength`, `_sceneBoundsColor`, `_placementHeightColor`, `_surfaceNormalLength`

- **Gizmo setter methods** (~8 methods):
  - `SetGizmoVisibility()`, `SetSceneGizmoVisibility()`, `SetSceneGridVisibility()`, `SetSceneBoundsVisibility()`
  - `SetStageAreaGizmoVisibility()`, `SetPlacementHeightGizmoVisibility()`, `SetSceneBoundsColor()`

### 2. Remove Redundant Properties
Remove read-only properties that simply forward to subsystem properties:
- `CurrentScene` → Use `_sceneSerializer.CurrentScene` directly
- `CurrentProject` → Use `_sceneSerializer.CurrentProject` directly  
- `IsInPreviewMode` → Use `_previewController.IsInPreviewMode` directly
- `CurrentPlacementState` → Use `_placementSystem.State` directly
- `IsPlacementActive` → Use `_placementSystem.IsActive` directly

### 3. Remove Redundant Wrapper Methods
Identify and remove public methods that just delegate to subsystems without adding logic:
- Grid-related wrappers (if GridManager already exposes these)
- Placement wrappers (if PlacementSystem already exposes these)
- Selection wrappers (if SelectionManager already exposes these)

## Out of Scope
- Changes to subsystem classes (PlacementSystem, SelectionManager, etc.)
- New gizmo rendering features
- UI or input handling changes
- Other architectural changes beyond gizmo extraction

## Technical Approach

### Phase 1: Create SandboxGizmoRenderer
1. Create new `SandboxGizmoRenderer.cs` in `Assets/Scripts/SceneSandbox/Core/`
2. Move all gizmo fields, methods, and drawing logic
3. Add dependencies injection for required data:
   - Reference to SceneSandboxBuilder for scene configuration
   - Reference to PlacementSystem for placement state
   - Reference to SelectionManager for selected objects
   - Reference to GridManager for grid settings
4. Preserve all existing gizmo behavior (zero functional changes)

### Phase 2: Update SceneSandboxBuilder
1. Add `[SerializeField] private SandboxGizmoRenderer _gizmoRenderer;` field
2. Initialize in `InitializeComponents()` (get or create pattern)
3. Remove all extracted gizmo code
4. Update any external references (if any)

### Phase 3: Remove Redundant APIs
1. Identify wrapper properties/methods via grep analysis
2. Search for usages across codebase
3. Update call sites to use subsystem APIs directly
4. Remove redundant wrappers from SceneSandboxBuilder

### Phase 4: Validation
1. Ensure all gizmos render correctly in editor and play mode
2. Verify no compilation errors
3. Test scene building, object placement, selection, transform modes
4. Verify all public API consumers still work

## Benefits
- **Maintainability**: SceneSandboxBuilder reduced from ~2770 to ~2200 lines
- **Testability**: Gizmo rendering can be tested independently
- **Reusability**: SandboxGizmoRenderer can be used in other scenes/editors
- **Clarity**: Main controller class focuses on coordination, not rendering
- **Consistency**: Follows established extraction pattern (PlacementSystem, SelectionManager, etc.)
- **API Clarity**: Removes confusion between wrapper methods and subsystem APIs

## Risks
- **Low**: Gizmo code is self-contained with clear boundaries
- **Medium**: Redundant API removal may require updating UI/external consumers
  - *Mitigation*: Thorough grep search for usages, careful incremental removal
- **Low**: Missing dependency injection could break gizmo rendering
  - *Mitigation*: Preserve existing data access patterns through references

## Success Criteria
- ✅ SandboxGizmoRenderer renders all gizmos identically to current behavior
- ✅ SceneSandboxBuilder reduced by ~570+ lines
- ✅ No compilation errors or runtime exceptions
- ✅ All existing sandbox features work unchanged
- ✅ Redundant wrapper APIs removed, call sites updated
- ✅ Code follows project conventions (naming, architecture, DI)

## Related
- Previous refactoring: `refactor-clean-scenesandboxbuilder` (archived)
- Follows Unity coding conventions from `openspec/project.md`
- Aligns with component extraction pattern established for PlacementSystem, SelectionManager
