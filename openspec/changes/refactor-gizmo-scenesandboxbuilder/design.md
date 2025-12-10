# Design: Extract Gizmo Rendering from SceneSandboxBuilder

## Context
SceneSandboxBuilder has grown to 2770 lines and violates single responsibility principle by handling both scene orchestration AND gizmo rendering. Previous refactoring extracted PlacementSystem, SelectionManager, GridManager, etc., but gizmo code remained in the main controller. Additionally, wrapper properties/methods create API confusion.

### Stakeholders
- Developers maintaining SceneSandboxBuilder
- UI developers consuming builder APIs
- Future contributors adding new gizmo features

### Constraints
- Must preserve exact gizmo rendering behavior (zero visual changes)
- Cannot break existing public API consumers
- Must follow Unity component pattern (SerializeField, Initialize pattern)
- Must align with project conventions (naming, architecture)

## Goals / Non-Goals

### Goals
1. Extract all gizmo rendering logic into `SandboxGizmoRenderer` component
2. Reduce SceneSandboxBuilder complexity by ~570+ lines
3. Remove redundant wrapper properties/methods that duplicate subsystem APIs
4. Make gizmo rendering testable and reusable
5. Maintain backward compatibility where needed

### Non-Goals
- Changing gizmo visual appearance or behavior
- Adding new gizmo features
- Refactoring subsystem classes (PlacementSystem, etc.)
- UI redesign or input handling changes

## Decisions

### Decision 1: Component Extraction Pattern
**Choice**: Create `SandboxGizmoRenderer` as a separate MonoBehaviour component

**Rationale**:
- Follows established pattern (PlacementSystem, SelectionManager, GridManager)
- Enables Unity lifecycle hooks (OnDrawGizmos)
- Supports dependency injection via Initialize()
- Can be tested independently
- Can be reused in other scenes/editors

**Alternatives considered**:
- Static utility class: Rejected (no Unity lifecycle, harder to test)
- Nested class: Rejected (doesn't solve complexity, no component benefits)

### Decision 2: Dependency Injection Strategy
**Choice**: Use explicit `Initialize()` method with required dependencies

**Pattern**:
```csharp
public void Initialize(
    SceneSandboxBuilder builder,
    PlacementSystem placementSystem,
    SelectionManager selectionManager,
    GridManager gridManager,
    Transform stageArea
)
```

**Rationale**:
- Makes dependencies explicit and testable
- Follows existing pattern in PlacementSystem, SelectionManager
- Avoids GetComponent() calls at runtime
- Supports SerializeField fallback for editor workflow

**Alternatives considered**:
- Constructor injection: Rejected (Unity doesn't support parameterized constructors for MonoBehaviours)
- Property injection: Rejected (less explicit, harder to validate)

### Decision 3: Gizmo Data Access
**Choice**: Pass references to subsystems, access their properties/state directly

**Rationale**:
- Gizmo renderer needs live data (selected object, placement state, grid settings)
- Avoids duplicating state in renderer
- Keeps renderer lightweight (no caching, no sync issues)
- Matches Unity immediate-mode gizmo pattern

**Alternatives considered**:
- Data structures/DTOs: Rejected (over-engineering, sync complexity)
- Events for state updates: Rejected (gizmo rendering is pull-based, not push)

### Decision 4: Redundant Wrapper API Removal
**Choice**: Remove wrapper properties/methods that just forward to subsystems

**Examples to remove**:
- `CurrentScene` → use `_sceneSerializer.CurrentScene`
- `IsPlacementActive` → use `_placementSystem.IsActive`
- Selection wrappers → use `_selectionManager` directly

**Rationale**:
- Eliminates confusion (which API to use?)
- Reduces maintenance burden
- Exposes subsystem capabilities directly
- Encourages proper dependency usage

**Migration strategy**:
1. Grep search for all usages
2. Update call sites incrementally
3. Remove wrappers only after all usages updated
4. Keep critical wrappers if they add validation logic

**Alternatives considered**:
- Keep wrappers for backward compatibility: Rejected (perpetuates technical debt)
- Mark as Obsolete first: Considered but unnecessary (internal project, no external consumers)

### Decision 5: Gizmo Visibility Control
**Choice**: Keep public setter methods in SceneSandboxBuilder, delegate to renderer

**Pattern**:
```csharp
public void SetGizmoVisibility(bool visible)
{
    _gizmoRenderer.SetGizmoVisibility(visible);
}
```

**Rationale**:
- Maintains public API contract for UI consumers
- SceneSandboxBuilder remains the main entry point
- Encapsulates renderer implementation detail
- Allows future extension (events, validation, etc.)

**Alternatives considered**:
- Expose renderer publicly: Rejected (leaky abstraction)
- Remove setters entirely: Rejected (breaks UI, no clear benefit)

## Implementation Plan

### Phase 1: Create SandboxGizmoRenderer (Non-Breaking)
1. Create new component with all gizmo fields/methods
2. Implement Initialize() for dependency injection
3. Copy all gizmo drawing logic (OnDrawGizmos, helpers)
4. Test in isolation (add to test scene)

### Phase 2: Integrate into SceneSandboxBuilder
1. Add `_gizmoRenderer` field
2. Initialize in `InitializeComponents()`
3. Remove extracted code from builder
4. Update setter methods to delegate
5. Compile and fix any errors

### Phase 3: Remove Redundant Wrappers
1. Grep for wrapper properties/methods
2. Find all usages
3. Update call sites to use subsystems
4. Remove wrappers incrementally
5. Test after each removal

### Phase 4: Validation
1. Visual testing: all gizmos render correctly
2. Functional testing: scene building workflow
3. API testing: UI consumers still work
4. Performance check: no regression

## Risks / Trade-offs

### Risk: Missing Dependencies in Gizmo Renderer
**Impact**: Gizmos won't render or will throw null reference exceptions

**Mitigation**:
- Validate all dependencies in Initialize()
- Add null checks in gizmo drawing methods
- Preserve existing defensive checks from OnDrawGizmos
- Test thoroughly in editor and play mode

### Risk: Breaking External API Consumers
**Impact**: UI scripts or external code that calls wrapper methods will break

**Mitigation**:
- Grep search for all usages before removal
- Update call sites systematically
- Test all UI flows after changes
- Keep setters that are actively used

### Trade-off: Slight Increase in Component Count
**Cost**: One additional component on SceneSandboxBuilder GameObject

**Benefit**: ~570 lines reduced, clearer separation of concerns, better testability

**Decision**: Accept trade-off (standard Unity pattern, minimal overhead)

### Trade-off: Indirect Data Access in Gizmo Renderer
**Cost**: Renderer accesses data through subsystem references (not direct fields)

**Benefit**: No state duplication, no synchronization issues, single source of truth

**Decision**: Accept trade-off (aligns with immediate-mode gizmo pattern)

## Migration Plan

### Step 1: Create & Test SandboxGizmoRenderer
- Create component in same namespace
- Copy all gizmo code exactly (no changes yet)
- Add to test scene, verify rendering

### Step 2: Integrate into SceneSandboxBuilder
- Add field, initialize, delegate setters
- Remove extracted code
- Compile, fix errors
- Test in editor

### Step 3: Remove Redundant Wrappers (Incremental)
For each wrapper:
1. Grep search for usages
2. Update call sites (commit)
3. Remove wrapper (commit)
4. Test (verify no breaks)

### Step 4: Final Validation
- Full smoke test: place, select, transform, save, load
- Gizmo visibility toggles
- Mode switching (Build/Play)
- UI workflows

### Rollback Plan
If critical issues found:
1. Revert commits incrementally
2. Fix issues in isolation
3. Re-apply changes with fixes

## Open Questions

### Q1: Should SandboxGizmoRenderer be optional?
**Answer**: No, it's core functionality. Always initialize with builder.

### Q2: Should we extract gizmo settings to ScriptableObject?
**Answer**: Out of scope for this refactoring. Could be future enhancement.

### Q3: How to handle gizmo state (e.g., _lastValidSurfaceNormal)?
**Answer**: Move transient state to renderer. It's gizmo-specific, not builder state.

### Q4: Should we create interfaces for testability?
**Answer**: Not immediately needed. Concrete Unity components work well. Add interfaces if testing friction arises.

## Success Metrics

### Quantitative
- ✅ SceneSandboxBuilder reduced from 2770 to ~2200 lines (~570 lines removed)
- ✅ Zero compilation errors
- ✅ Zero runtime exceptions in testing

### Qualitative
- ✅ Gizmos render identically to before refactoring
- ✅ Code follows project conventions
- ✅ Clear separation of concerns (orchestration vs rendering)
- ✅ Easier to understand and modify gizmo behavior
- ✅ API clarity improved (no confusion between wrappers and subsystems)
