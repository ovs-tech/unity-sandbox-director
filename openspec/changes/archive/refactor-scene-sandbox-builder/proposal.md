# Proposal: Refactor SceneSandboxBuilder for Maintainability and Performance

**Change ID:** `refactor-scene-sandbox-builder`  
**Type:** Refactoring  
**Status:** Completed  
**Author:** AI Assistant  
**Date:** 2025-11-30  
**Archived:** 2025-12-07

---

## Why

The `SceneSandboxBuilder` class has grown to 4,169 lines with 88 serialized fields, making it the single largest maintainability bottleneck in the codebase. This refactoring is essential for:

1. **Developer Productivity**: Current complexity makes bug fixes and feature additions risky and time-consuming. Breaking into focused components will accelerate development velocity.

2. **Performance**: The monolithic Update() loop processes all input and logic regardless of active mode. Modular architecture enables selective activation of only needed systems, reducing per-frame overhead.

3. **Testability**: Tight coupling prevents unit testing of individual features. Component isolation enables comprehensive test coverage, reducing regression risk.

4. **Team Scalability**: Multiple developers cannot work on SceneSandboxBuilder simultaneously without merge conflicts. Separate components allow parallel development.

5. **Technical Debt**: Deprecated code, TODOs, and legacy patterns have accumulated. This refactoring provides opportunity to clean up debt while improving structure.

6. **Future Features**: Planned features (command pattern for undo/redo, DLC system integration, custom placement validators) require cleaner architecture to implement safely.

Without this refactoring, continued feature development will compound complexity, increasing bug density and slowing team velocity. The investment in restructuring will pay dividends in reduced maintenance costs and faster feature delivery.

---

## Problem Statement

The `SceneSandboxBuilder` class has become a monolithic component with multiple responsibilities, making it difficult to maintain, test, and extend. Current issues include:

### Code Complexity
- **4,169 lines of code** in a single class file
- **88 serialized fields** exposed in Unity Inspector
- **Multiple responsibilities** violating Single Responsibility Principle:
  - Object placement and validation
  - Input handling (pointer, keyboard, scroll)
  - Transform mode management (Position/Rotation/Scale)
  - Scene serialization and project management
  - Selection and multi-object management
  - Preview and playback control
  - Drop indicator management
  - Material and ghost preview handling
  - Grid and bounds management
  - Camera and raycast operations

### Performance Concerns
- All input handling runs in `Update()` regardless of mode
- Excessive `Update()` polling even when not needed
- No clear separation between Build Mode and Play Mode systems
- Potential GC allocations from frequent list operations
- Raycast operations mixed with business logic

### Maintainability Issues
- Deprecated fields and methods still present (`_enableGhostPreview`, `SetTransformMode_Legacy`)
- TODOs scattered throughout (context menu system, timeline track setup)
- Unclear state management with multiple overlapping state variables
- Hard to test due to tight coupling with Unity components
- Difficult to extend with new features without modifying core class

### Technical Debt
- Legacy regions (`#region Public Interface (Legacy)`)
- Duplicated logic across placement and editing workflows
- Event system mixed with direct method calls
- No clear interface boundaries for subsystems

---

## Proposed Solution

Decompose `SceneSandboxBuilder` into focused, single-responsibility components following Unity's modular architecture patterns and the project's established conventions.

### High-Level Architecture

```
SceneSandboxBuilder (Orchestrator)
├── InputManager (Input handling)
├── PlacementSystem (Object placement & validation)
├── SelectionManager (Object selection & multi-select)
├── TransformController (Position/Rotation/Scale modes)
├── SceneSerializer (Save/Load operations)
├── PreviewController (Timeline preview management)
├── GridManager (Grid snapping & bounds)
└── CameraRaycaster (Raycast operations)
```

### Key Principles
1. **Single Responsibility**: Each component handles one concern
2. **Interface-Based Design**: Components communicate through well-defined interfaces
3. **Dependency Injection**: Use constructor or property injection for loose coupling
4. **Event-Driven Communication**: Prefer events over direct method calls
5. **Performance Optimization**: Move logic out of `Update()` to event-driven callbacks
6. **Testability**: Enable unit testing through interface mocking

---

## Impact Analysis

### Affected Systems
- **Scene Sandbox Builder** (Core refactoring)
- **UI Integration** (ObjectPalette, SandboxBuilderUI must adapt to new events)
- **Editor Tools** (SceneSandboxBuilderEditor may need property drawer updates)
- **Serialization** (Minor adjustments to project save/load flow)
- **Input System** (Centralized input handling)

### Breaking Changes
- Internal architecture changes (public API maintained for backward compatibility)
- Event signatures may change (provide migration guide)
- Inspector layout will be reorganized (preserve existing configurations)

### Non-Breaking Changes
- Public methods preserved with same signatures
- Existing scenes and projects remain compatible
- Serialized data format unchanged

---

## Benefits

### Code Quality
- **Reduced complexity**: Each component < 500 lines
- **Better organization**: Clear separation of concerns
- **Easier testing**: Mock interfaces for unit tests
- **Improved readability**: Focused classes with clear purpose

### Performance
- **Reduced Update() overhead**: Event-driven input instead of polling
- **Better memory management**: Clearer object lifetime management
- **Optimized raycasts**: Cached results, conditional execution
- **Mode-specific systems**: Disable Build Mode systems in Play Mode

### Maintainability
- **Easier feature additions**: Add new components without modifying core
- **Clearer dependencies**: Explicit through constructor injection
- **Better debugging**: Isolated systems easier to trace
- **Reduced merge conflicts**: Changes scoped to specific files

### Extensibility
- **Plugin architecture**: New transform modes via interface implementation
- **Custom placement rules**: Implement IPlacementValidator
- **Alternative input handlers**: Swap InputManager implementations
- **DLC integration**: Hook into events for custom behaviors

---

## Success Criteria

1. ✅ `SceneSandboxBuilder.cs` reduced to < 800 lines (orchestrator only)
2. ✅ Each extracted component < 500 lines
3. ✅ All existing functionality preserved (backward compatible)
4. ✅ No performance regression (maintain 45-60 FPS target)
5. ✅ Serialized fields reduced to < 20 (move to component-specific configs)
6. ✅ Zero deprecated code remaining
7. ✅ All TODOs addressed or moved to issue tracker
8. ✅ Unit tests added for each component (min 80% coverage)
9. ✅ Inspector workflow preserved (no UX regression)
10. ✅ Existing scenes and projects load without errors

---

## Alternatives Considered

### Option 1: Partial Refactoring (Selected)
- Extract critical systems first (Input, Placement, Selection)
- Iterative approach, maintain backward compatibility
- Lower risk, easier to review and test
- **Chosen for pragmatic balance of improvement and safety**

### Option 2: Complete Rewrite
- Start from scratch with new architecture
- Higher risk of introducing bugs
- Longer development time
- Difficult to maintain backward compatibility
- **Rejected: Too risky for production codebase**

### Option 3: Minimal Cleanup
- Just remove deprecated code and reorganize regions
- Doesn't address core architectural issues
- No performance benefits
- Quick but provides minimal long-term value
- **Rejected: Doesn't solve underlying problems**

---

## Migration Strategy

### Phase 1: Non-Breaking Extraction (Week 1)
- Extract InputManager (input action handling)
- Extract CameraRaycaster (raycast operations)
- Extract GridManager (grid snapping logic)
- Keep existing public API intact

### Phase 2: System Decomposition (Week 2)
- Extract PlacementSystem (object placement workflow)
- Extract SelectionManager (selection and multi-select)
- Extract TransformController (transform modes)
- Wire up event communication

### Phase 3: Cleanup & Optimization (Week 3)
- Remove deprecated code
- Optimize Update() logic
- Add unit tests for each component
- Performance profiling and optimization

### Phase 4: Documentation & Polish (Week 4)
- Update XML documentation
- Create migration guide
- Update inspector tooltips
- Final testing and validation

---

## Dependencies

### Required
- None (internal refactoring)

### Recommended
- Unity Test Framework (for unit tests)
- Optional: Zenject/VContainer (for DI, but can use manual injection)

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking existing functionality | High | Comprehensive regression testing, maintain public API |
| Performance regression | Medium | Profile before/after, benchmark critical paths |
| Inspector workflow changes | Medium | Preserve SerializeField references via FormerlySerializedAs |
| Increased file count | Low | Clear naming and folder organization |
| Learning curve for team | Low | Documentation, code comments, pair programming |

---

## References

- [Unity SOLID Principles](https://unity.com/how-to/solid-principles-single-responsibility-principle)
- [Command Pattern in Unity](https://www.habrador.com/tutorials/programming-patterns/1-command-pattern/)
- Project Conventions: `openspec/project.md`
- Game Design Doc: `docs/gdd.md`

---

## Approval Requirements

- [ ] Technical Lead Review
- [ ] QA Testing Plan Approved
- [ ] Performance Benchmarks Defined
- [ ] Migration Guide Reviewed
