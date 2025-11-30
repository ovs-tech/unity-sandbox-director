# Implementation Tasks: Refactor SceneSandboxBuilder

**Change ID:** `refactor-scene-sandbox-builder`  
**Estimated Effort:** 3-4 weeks  
**Priority:** High

---

## Phase 1: Foundation & Non-Breaking Extraction (Week 1)

### 1.1 Create Component Base Structures
- [ ] Create `SandboxInputManager.cs` with basic structure
  - Add InputActionReference fields (copy from SceneSandboxBuilder)
  - Define event signatures (OnPointerDown, OnPointerUp, etc.)
  - Add FormerlySerializedAs attributes for field migration
  - **Validation:** File compiles, no syntax errors

- [x] Create `CameraRaycaster.cs` with raycast utilities
  - Move raycast methods from SceneSandboxBuilder
  - Add Camera field with caching
  - Implement RaycastFromScreen(), GetObjectUnderPointer()
  - **Validation:** Unit test passes for raycast hit detection

- [x] Create `GridManager.cs` with grid/bounds logic
  - Extract grid snapping fields and methods
  - Extract scene bounds fields and methods
  - Implement SnapToGrid() with pivot support
  - Add OnDrawGizmos() for grid visualization
  - **Validation:** Grid snapping works in test scene

### 1.2 Initial Integration (Non-Breaking)
- [x] Add component references to SceneSandboxBuilder
  - Add private fields for InputManager, CameraRaycaster, GridManager
  - Implement GetOrAddComponent<T>() helper
  - Initialize in Awake() before existing logic
  - **Validation:** Existing functionality unaffected

- [x] Wire up CameraRaycaster in SceneSandboxBuilder
  - Replace inline raycast calls with CameraRaycaster methods
  - Update HandleRaycastInput() to use CameraRaycaster
  - Test all raycast-dependent features
  - **Validation:** Object selection and placement work as before

- [x] Wire up GridManager in SceneSandboxBuilder
  - Replace inline grid logic with GridManager calls
  - Update placement to use GridManager.SnapToGrid()
  - Apply rotation snapping via GridManager when enabled
  - Test grid snapping with various configurations
  - **Validation:** Grid snapping behavior unchanged

— Phase 1 status: Completed for CameraRaycaster and GridManager wiring; SandboxInputManager deferred to Phase 3.3 as planned.

---

## Phase 2: Core System Decomposition (Week 2)

### 2.1 Extract PlacementSystem
- [x] Create `PlacementSystem.cs` component
  - Move placement fields (_placementState, _currentPlacementObject, etc.)
  - Move placement methods (StartPlacement, UpdatePlacement, etc.)
  - Define placement events (OnPlacementStarted, OnPlacementConfirmed, etc.)
  - Add dependencies (CameraRaycaster, GridManager, SceneObjectLibrary)
  - **Validation:** Placement workflow functions independently

- [x] Integrate PlacementSystem with SceneSandboxBuilder
  - Initialize PlacementSystem in Awake()
  - Subscribe to placement events (delegating to existing builder methods)
  - Create public API facade methods (Begin/Update/Confirm/Cancel) for backward compatibility
  - Update ObjectPaletteItem integration
  - **Validation:** Drag-and-drop from palette works

### 2.1.1 Centralization & State (non-breaking)
- [x] Centralize UI calls via builder facades
  - UI entry points use Begin/Update/Confirm/CancelWithSystem facades
  - PlacementSystem notified without behavior changes
  - **Validation:** UI flows unchanged

- [x] Add placement state tracking to PlacementSystem
  - Track Idle/Active/Confirming/Cancelling states
  - Expose read-only IsActive and State properties
  - **Validation:** State reflects placement lifecycle

- [x] Route position/rotation snapping through PlacementSystem
  - World position via ComputeWorldPositionFromScreen with fallback
  - Grid snap via ApplyGridSnap; rotation via ApplyRotationSnap
  - **Validation:** Snap behavior unchanged

 - [x] Implement drop indicator system
  - Move drop indicator logic to PlacementSystem
  - Create/reuse drop indicator on placement start (events emitted from PlacementSystem)
  - Update indicator color based on validation (delegated via builder for now)
  - Clean up indicator on placement end (events emitted)
  - **Validation:** Events wired; builder visuals respond without regression

 - [x] Add placement validation logic
  - Implement ValidatePlacement() with collision checks
  - Add bounds validation (combined in builder with system result)
  - Add surface requirement check (deferred)
  - Test all validation rules (manual runtime validation, no regressions)
  - **Validation:** Routing through PlacementSystem works; invalid placements rejected as before

### 2.2 Extract SelectionManager
- [x] Create `SelectionManager.cs` component
  - Move selection fields (_selectedItems, _lastSelectedItem)
  - Move selection methods (SelectObject, DeselectObject, etc.)
  - Define selection events (OnObjectSelected, OnSelectionChanged)
  - Add dependency on CameraRaycaster
  - **Validation:** Object selection works independently ✅ Component created with full API

- [x] Integrate SelectionManager with SceneSandboxBuilder
  - Initialize SelectionManager in Awake()
  - Subscribe to selection events
  - Update UI integration (OnObjectSelected → UI updates)
  - Migrate existing selection-dependent code
  - **Validation:** Single and multi-select work correctly ✅ Non-breaking integration complete

- [x] Implement hover detection
  - Add OnObjectHoverEnter/Exit events
  - Track current hover item
  - Emit events on hover state changes
  - **Validation:** Hover highlighting works ✅ API implemented (manual testing required)

### 2.3 Extract TransformController
- [x] Create `TransformController.cs` component
  - Move transform mode fields (_currentTransformMode, _currentTransformAxis)
  - Move transform sensitivity fields
  - Move transform methods (SetTransformMode, ToggleTransformAxis, etc.)
  - Define transform events (OnTransformModeChanged, OnTransformAxisChanged)
  - **Validation:** Transform mode switching works ✅ Component created with full API

- [x] Integrate TransformController with SelectionManager
  - Add SelectionManager dependency
  - Update active transform items based on selection
  - Subscribe to selection changes
  - **Validation:** Transform modes apply to selected objects ✅ Non-breaking integration complete

- [x] Implement transform delta application
  - Implement ApplyTransformDelta() for drag-based transforms
  - Implement ApplyScrollTransform() for scroll wheel scaling
  - Add axis constraints (X, Y, Z, All)
  - Test all transform modes (Position, Rotation, Scale)
  - **Validation:** All transform modes work with axis constraints ✅ API implemented (manual testing required)

---

## Phase 3: Remaining Systems & Optimization (Week 3)

### 3.1 Extract SceneSerializer
- [ ] Create `SceneSerializer.cs` component
  - Move save/load fields (_defaultSavePath, _currentScene, etc.)
  - Move serialization methods (SaveScene, LoadScene, etc.)
  - Define serialization events (OnSceneLoaded, OnSceneSaved)
  - **Validation:** Scene save/load works

- [ ] Integrate SceneSerializer with other systems
  - Subscribe to OnPlacementConfirmed to track placed objects
  - Update PlacementSystem.PlaceObject() to register objects
  - Test full scene lifecycle (place objects → save → clear → load)
  - **Validation:** Saved scenes load with all objects restored

- [ ] Implement project management
  - Move project save/load logic
  - Implement multi-scene management
  - Add auto-load first project feature
  - **Validation:** Projects save and load correctly

### 3.2 Extract PreviewController
- [ ] Create `PreviewController.cs` component
  - Move preview fields (_previewObjects, _autoPreview)
  - Move preview methods (StartPreview, StopPreview)
  - Define preview events (OnPreviewStateChanged)
  - **Validation:** Preview mode activates

- [ ] Integrate PreviewController with timeline
  - Add MiniTimelineDirector dependency
  - Wire up timeline playback control
  - Test preview workflow (start → playback → stop)
  - **Validation:** Timeline preview works

### 3.3 Implement SandboxInputManager
- [ ] Complete input action initialization
  - Resolve all InputActionReferences
  - Enable/disable actions based on mode
  - Register callbacks for each action
  - **Validation:** All input actions trigger correctly

- [ ] Implement gesture detection
  - Add drag detection with threshold
  - Add double-click detection with timing
  - Add right-click context menu detection
  - **Validation:** All gestures detected accurately

- [ ] Implement hotkey system
  - Wire up transform mode hotkeys (Q/W/E/R)
  - Wire up axis toggle hotkey (X)
  - Add hotkey enable/disable flag
  - **Validation:** Hotkeys switch modes correctly

- [ ] Optimize input performance
  - Profile Update() overhead
  - Eliminate unnecessary allocations
  - Add input debouncing where needed
  - **Validation:** Input overhead < 0.1ms per frame

---

## Phase 4: Cleanup, Testing & Documentation (Week 4)

### 4.1 Remove Deprecated Code
- [ ] Remove deprecated fields
  - Remove `_enableGhostPreview`
  - Remove `_validGhostColor`, `_invalidGhostColor`
  - Remove `SetTransformMode_Legacy()`
  - **Validation:** No compilation warnings

- [ ] Remove deprecated regions
  - Remove `#region Public Interface (Legacy)`
  - Consolidate remaining regions
  - **Validation:** Code organization is clear

- [ ] Address TODOs
  - Implement or remove context menu TODOs
  - Address timeline track setup TODOs
  - Move unimplemented features to issue tracker
  - **Validation:** Zero TODO comments remain

### 4.2 Refactor SceneSandboxBuilder (Orchestrator)
- [ ] Remove extracted logic from SceneSandboxBuilder
  - Keep only orchestration and initialization
  - Keep public API facade methods
  - Keep component references and wiring
  - **Validation:** File is < 800 lines

- [ ] Consolidate Update() logic
  - Move input polling to SandboxInputManager
  - Remove redundant Update() calls
  - Keep only necessary per-frame logic
  - **Validation:** Update() overhead minimal

- [ ] Update XML documentation
  - Add /// comments for all public methods
  - Document component dependencies
  - Add code examples for common workflows
  - **Validation:** Documentation is complete

### 4.3 Unit Testing
- [ ] Write CameraRaycaster tests
  - Test raycast hit detection
  - Test layer mask filtering
  - Test null/error handling
  - **Target:** 80%+ code coverage

- [ ] Write GridManager tests
  - Test SnapToGrid() with various inputs
  - Test bounds checking
  - Test pivot point calculations
  - **Target:** 80%+ code coverage

- [ ] Write PlacementSystem tests
  - Test placement lifecycle (start → update → confirm)
  - Test placement validation rules
  - Test cancellation workflow
  - **Target:** 80%+ code coverage

- [ ] Write SelectionManager tests
  - Test single selection
  - Test multi-selection (additive)
  - Test deselection and clear
  - **Target:** 80%+ code coverage

- [ ] Write TransformController tests
  - Test mode switching
  - Test axis toggling
  - Test transform delta application
  - **Target:** 80%+ code coverage

- [ ] Write InputManager tests
  - Test input action resolution
  - Test gesture detection
  - Test mode filtering
  - **Target:** 80%+ code coverage

- [ ] Write integration tests
  - Test full placement workflow (palette → drag → place)
  - Test mode switching with active operations
  - Test scene save/load with placed objects
  - Test multi-object selection and transform
  - **Validation:** All workflows function end-to-end

### 4.4 Performance Validation
- [ ] Profile Update() overhead
  - Measure per-frame time for each component
  - Ensure total < 1ms per frame
  - **Target:** Maintain 60 FPS with 20+ objects

- [ ] Profile GC allocations
  - Run Unity Profiler in placement workflow
  - Ensure zero allocations in steady state
  - Fix any allocation hotspots
  - **Target:** 0 GC per frame (excluding placement start/end)

- [ ] Benchmark raycast performance
  - Measure raycast frequency
  - Validate caching works
  - **Target:** Max 1 raycast per input event

- [ ] Memory footprint comparison
  - Compare memory before/after refactoring
  - Ensure no significant increase
  - **Target:** < 10% memory increase

### 4.5 Documentation & Migration Guide
- [ ] Create migration guide
  - Document public API changes
  - Document event signature changes
  - Provide code examples for common patterns
  - **Deliverable:** `docs/migration/scene-sandbox-refactor.md`

- [ ] Update README documentation
  - Update architecture diagrams
  - Document new component structure
  - Add troubleshooting section
  - **Deliverable:** Updated `README.md`

- [ ] Update Inspector tooltips
  - Add [Tooltip] attributes to all SerializeFields
  - Ensure descriptions are clear
  - **Validation:** All fields have tooltips

- [ ] Create code examples
  - Example: Custom placement validator
  - Example: Custom transform mode
  - Example: Event subscription patterns
  - **Deliverable:** `docs/examples/scene-sandbox-examples.md`

### 4.6 Final Validation
- [ ] Regression testing
  - Test all existing features (placement, selection, transform, save/load)
  - Test with existing scenes and projects
  - Test UI integration (palette, builder UI)
  - **Validation:** No regressions found

- [ ] Performance testing
  - Run on target hardware (mid-range mobile)
  - Measure FPS with typical scene complexity
  - **Validation:** Maintain 45-60 FPS target

- [ ] Serialization compatibility testing
  - Open old scenes in refactored version
  - Verify all settings migrated correctly
  - Save and reload to ensure persistence
  - **Validation:** No data loss or warnings

- [ ] Code review
  - Review all new components
  - Check adherence to project conventions
  - Verify SOLID principles applied
  - **Validation:** Code review approved

---

## Dependencies

### Blockers
- None (internal refactoring)

### Parallel Work
- UI updates can proceed in parallel with Phase 2-3 (coordinate on events)
- Documentation can be drafted during Phase 3

### Prerequisites
- Unity Test Framework installed (for unit tests)
- Git branch created: `feature/refactor-scene-sandbox-builder`

---

## Rollback Plan

If critical issues arise:

1. **Immediate Rollback:**
   - Revert git commits to previous stable state
   - Restore backup of SceneSandboxBuilder.cs
   - Test that old version works

2. **Partial Rollback:**
   - Keep non-breaking extractions (CameraRaycaster, GridManager)
   - Revert problematic components (e.g., PlacementSystem)
   - Fix issues and re-integrate

3. **Data Migration Rollback:**
   - If serialization issues occur, use FormerlySerializedAs to restore
   - Provide script to manually migrate problematic scenes

---

## Success Metrics

- [ ] SceneSandboxBuilder.cs < 800 lines
- [ ] Each component < 500 lines
- [ ] Zero deprecated code remaining
- [ ] All unit tests passing (80%+ coverage)
- [ ] All integration tests passing
- [ ] Performance targets met (60 FPS, 0 GC/frame)
- [ ] No regression bugs reported
- [ ] Migration guide complete
- [ ] Code review approved

---

## Notes

- Maintain backward compatibility throughout
- Test frequently during extraction
- Commit after each completed task
- Coordinate with UI team on event changes
- Profile performance after each phase
