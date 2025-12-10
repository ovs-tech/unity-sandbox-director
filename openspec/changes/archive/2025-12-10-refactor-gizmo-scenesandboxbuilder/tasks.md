# Tasks: Refactor SceneSandboxBuilder - Extract Gizmo Rendering & Remove Redundant Methods

## Phase 1: Analysis & Planning
- [x] 1.1 Grep search for all gizmo-related methods and fields in SceneSandboxBuilder
- [x] 1.2 Grep search for redundant wrapper properties (forward to subsystems)
- [x] 1.3 Grep search for redundant wrapper methods (delegate to subsystems)
- [x] 1.4 Document all public API usage sites for wrappers to be removed
- [x] 1.5 Identify gizmo dependencies (what data from builder/subsystems is needed)

## Phase 2: Create SandboxGizmoRenderer
- [x] 2.1 Create `Assets/Scripts/SceneSandbox/Core/SandboxGizmoRenderer.cs`
- [x] 2.2 Add class header, namespace, and basic structure
- [x] 2.3 Add SerializeField dependencies (SceneSandboxBuilder, PlacementSystem, SelectionManager, GridManager, Transform stageArea)
- [x] 2.4 Move all gizmo configuration fields from SceneSandboxBuilder
- [x] 2.5 Create `Initialize()` method for dependency injection
- [x] 2.6 Move `OnDrawGizmos()` and all helper methods (DrawSceneGizmos, DrawObjectGizmos, etc.)
- [x] 2.7 Move `CalculateObjectBounds()` helper method
- [x] 2.8 Add public visibility setters (SetGizmoVisibility, SetSceneGizmoVisibility, etc.)
- [x] 2.9 Update all internal references to use injected dependencies
- [x] 2.10 Add XML documentation comments for all public methods

## Phase 3: Integrate SandboxGizmoRenderer into SceneSandboxBuilder
- [x] 3.1 Add `[SerializeField] private SandboxGizmoRenderer _gizmoRenderer;` field
- [x] 3.2 Update `InitializeComponents()` to get or create SandboxGizmoRenderer
- [x] 3.3 Call `_gizmoRenderer.Initialize()` with required dependencies
- [x] 3.4 Remove all gizmo drawing methods from SceneSandboxBuilder
- [x] 3.5 Remove gizmo configuration fields from SceneSandboxBuilder
- [x] 3.6 Update gizmo setter methods to delegate to `_gizmoRenderer`
- [x] 3.7 Remove `_lastValidSurfaceNormal` field (move to gizmo renderer if needed)

## Phase 4: Remove Redundant Wrapper Properties
- [x] 4.1 Remove `CurrentScene` property (use `_sceneSerializer.CurrentScene`)
- [x] 4.2 Remove `CurrentProject` property (use `_sceneSerializer.CurrentProject`)
- [x] 4.3 Remove `IsInPreviewMode` property (use `_previewController.IsInPreviewMode`)
- [x] 4.4 Remove `CurrentPlacementState` property (use `_placementSystem.State`)
- [x] 4.5 Remove `IsPlacementActive` property (use `_placementSystem.IsActive`)
- [x] 4.6 Update all usages in SceneSandboxBuilder to access subsystems directly
- [x] 4.7 Search and update external usages (UI, other scripts)

## Phase 5: Identify & Remove Redundant Wrapper Methods
- [x] 5.1 Grep for methods that only delegate to PlacementSystem (e.g., UpdatePlacement)
- [x] 5.2 Grep for methods that only delegate to SelectionManager (e.g., SelectObject overloads)
- [x] 5.3 Grep for methods that only delegate to GridManager (e.g., GetSnappedPosition)
- [x] 5.4 For each wrapper method: search for all usages
- [x] 5.5 Update call sites to use subsystem APIs directly
- [x] 5.6 Remove wrapper methods from SceneSandboxBuilder
- [x] 5.7 Update any UI scripts or external consumers

## Phase 6: Validation & Testing
- [x] 6.1 Compile project, fix any errors
- [x] 6.2 Test in editor: verify all gizmos render correctly (grid, bounds, stage area, placement height)
- [x] 6.3 Test object gizmos: verify selected object shows bounds, axes, handles
- [x] 6.4 Test placement gizmos: verify drop indicator and preview gizmos work
- [x] 6.5 Test gizmo visibility toggles (all SetGizmoVisibility methods)
- [x] 6.6 Test scene building: place objects, select, transform, save, load
- [x] 6.7 Test mode switching (Build/Play mode)
- [x] 6.8 Test all subsystem APIs still work when called directly
- [x] 6.9 Run any existing unit/integration tests

## Phase 7: Documentation & Cleanup
- [x] 7.1 Update XML comments in SceneSandboxBuilder for changed APIs
- [x] 7.2 Add XML comments to SandboxGizmoRenderer
- [x] 7.3 Verify code follows project conventions (naming, formatting)
- [x] 7.4 Update any relevant documentation files
- [x] 7.5 Final line count verification (~570 lines removed)

## Dependencies
- Phase 2 depends on Phase 1 (need to know what to extract)
- Phase 3 depends on Phase 2 (SandboxGizmoRenderer must exist)
- Phase 4-5 can be done in parallel after Phase 3
- Phase 6 depends on all previous phases
- Phase 7 depends on Phase 6

## Validation Checklist
- [x] No compilation errors
- [x] No runtime exceptions in editor or play mode
- [x] All gizmos render identically to before refactoring
- [x] Scene building features work (place, select, transform, save, load)
- [x] Mode switching works (Build/Play)
- [x] Gizmo visibility toggles work
- [x] Public API consumers (UI scripts, etc.) still work
- [x] Code follows project conventions
- [x] Line count reduced by ~557 lines (SceneSandboxBuilder: 2213 lines, SandboxGizmoRenderer: 847 lines)
