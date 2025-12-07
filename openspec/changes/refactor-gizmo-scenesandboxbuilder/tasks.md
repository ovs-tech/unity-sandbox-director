# Tasks: Refactor SceneSandboxBuilder - Extract Gizmo Rendering & Remove Redundant Methods

## Phase 1: Analysis & Planning
- [ ] 1.1 Grep search for all gizmo-related methods and fields in SceneSandboxBuilder
- [ ] 1.2 Grep search for redundant wrapper properties (forward to subsystems)
- [ ] 1.3 Grep search for redundant wrapper methods (delegate to subsystems)
- [ ] 1.4 Document all public API usage sites for wrappers to be removed
- [ ] 1.5 Identify gizmo dependencies (what data from builder/subsystems is needed)

## Phase 2: Create SandboxGizmoRenderer
- [ ] 2.1 Create `Assets/Scripts/SceneSandbox/Core/SandboxGizmoRenderer.cs`
- [ ] 2.2 Add class header, namespace, and basic structure
- [ ] 2.3 Add SerializeField dependencies (SceneSandboxBuilder, PlacementSystem, SelectionManager, GridManager, Transform stageArea)
- [ ] 2.4 Move all gizmo configuration fields from SceneSandboxBuilder
- [ ] 2.5 Create `Initialize()` method for dependency injection
- [ ] 2.6 Move `OnDrawGizmos()` and all helper methods (DrawSceneGizmos, DrawObjectGizmos, etc.)
- [ ] 2.7 Move `CalculateObjectBounds()` helper method
- [ ] 2.8 Add public visibility setters (SetGizmoVisibility, SetSceneGizmoVisibility, etc.)
- [ ] 2.9 Update all internal references to use injected dependencies
- [ ] 2.10 Add XML documentation comments for all public methods

## Phase 3: Integrate SandboxGizmoRenderer into SceneSandboxBuilder
- [ ] 3.1 Add `[SerializeField] private SandboxGizmoRenderer _gizmoRenderer;` field
- [ ] 3.2 Update `InitializeComponents()` to get or create SandboxGizmoRenderer
- [ ] 3.3 Call `_gizmoRenderer.Initialize()` with required dependencies
- [ ] 3.4 Remove all gizmo drawing methods from SceneSandboxBuilder
- [ ] 3.5 Remove gizmo configuration fields from SceneSandboxBuilder
- [ ] 3.6 Update gizmo setter methods to delegate to `_gizmoRenderer`
- [ ] 3.7 Remove `_lastValidSurfaceNormal` field (move to gizmo renderer if needed)

## Phase 4: Remove Redundant Wrapper Properties
- [ ] 4.1 Remove `CurrentScene` property (use `_sceneSerializer.CurrentScene`)
- [ ] 4.2 Remove `CurrentProject` property (use `_sceneSerializer.CurrentProject`)
- [ ] 4.3 Remove `IsInPreviewMode` property (use `_previewController.IsInPreviewMode`)
- [ ] 4.4 Remove `CurrentPlacementState` property (use `_placementSystem.State`)
- [ ] 4.5 Remove `IsPlacementActive` property (use `_placementSystem.IsActive`)
- [ ] 4.6 Update all usages in SceneSandboxBuilder to access subsystems directly
- [ ] 4.7 Search and update external usages (UI, other scripts)

## Phase 5: Identify & Remove Redundant Wrapper Methods
- [ ] 5.1 Grep for methods that only delegate to PlacementSystem (e.g., UpdatePlacement)
- [ ] 5.2 Grep for methods that only delegate to SelectionManager (e.g., SelectObject overloads)
- [ ] 5.3 Grep for methods that only delegate to GridManager (e.g., GetSnappedPosition)
- [ ] 5.4 For each wrapper method: search for all usages
- [ ] 5.5 Update call sites to use subsystem APIs directly
- [ ] 5.6 Remove wrapper methods from SceneSandboxBuilder
- [ ] 5.7 Update any UI scripts or external consumers

## Phase 6: Validation & Testing
- [ ] 6.1 Compile project, fix any errors
- [ ] 6.2 Test in editor: verify all gizmos render correctly (grid, bounds, stage area, placement height)
- [ ] 6.3 Test object gizmos: verify selected object shows bounds, axes, handles
- [ ] 6.4 Test placement gizmos: verify drop indicator and preview gizmos work
- [ ] 6.5 Test gizmo visibility toggles (all SetGizmoVisibility methods)
- [ ] 6.6 Test scene building: place objects, select, transform, save, load
- [ ] 6.7 Test mode switching (Build/Play mode)
- [ ] 6.8 Test all subsystem APIs still work when called directly
- [ ] 6.9 Run any existing unit/integration tests

## Phase 7: Documentation & Cleanup
- [ ] 7.1 Update XML comments in SceneSandboxBuilder for changed APIs
- [ ] 7.2 Add XML comments to SandboxGizmoRenderer
- [ ] 7.3 Verify code follows project conventions (naming, formatting)
- [ ] 7.4 Update any relevant documentation files
- [ ] 7.5 Final line count verification (~570 lines removed)

## Dependencies
- Phase 2 depends on Phase 1 (need to know what to extract)
- Phase 3 depends on Phase 2 (SandboxGizmoRenderer must exist)
- Phase 4-5 can be done in parallel after Phase 3
- Phase 6 depends on all previous phases
- Phase 7 depends on Phase 6

## Validation Checklist
- [ ] No compilation errors
- [ ] No runtime exceptions in editor or play mode
- [ ] All gizmos render identically to before refactoring
- [ ] Scene building features work (place, select, transform, save, load)
- [ ] Mode switching works (Build/Play)
- [ ] Gizmo visibility toggles work
- [ ] Public API consumers (UI scripts, etc.) still work
- [ ] Code follows project conventions
- [ ] Line count reduced by ~570+ lines
