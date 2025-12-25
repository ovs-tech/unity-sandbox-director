# Proposal: Add Scene Object Library UI

**Change ID:** `add-scene-object-library-ui`  
**Status:** Draft  
**Created:** 2025-12-15  
**Author:** GitHub Copilot

## Summary

Create a Unity UI Toolkit-based user interface for the SceneObjectLibrary using MVVM architecture. This UI will enable users to browse, filter, search, and select scene objects (actors, props, cameras, lights) for placement in the Scene Sandbox Builder.

## Why

The current SceneObjectLibrary is a ScriptableObject data structure that stores all available scene objects, but it lacks a user interface. Users need an intuitive, performant UI to browse, filter, and select objects for placement in the Scene Sandbox Builder. This UI will enable efficient object discovery and selection, improving the overall workflow for scene creation.

## What Changes

- Add Scene Object Library UI capability with MVVM components (ViewModel, View, Controller)
- Implement filtering by type (Actor, Prop, Camera, Light), category, and search
- Create UI Toolkit-based interface with UXML/USS styling
- Integrate object selection with SceneSandboxBuilder placement system

## Motivation

The current SceneObjectLibrary is a ScriptableObject data structure that stores all available scene objects, but it lacks a user interface. Users need an intuitive, performant UI to:
- Browse objects by type (Actor, Prop, Camera, Light)
- Filter objects by category
- Search objects by name
- View object details (icon, name, category)
- Select objects for placement in the scene

A UI Toolkit + MVVM implementation provides:
- Clean separation of UI logic from business logic
- Reactive data binding for automatic UI updates
- Performance benefits of UI Toolkit over IMGUI
- Consistency with existing systems (InventoryView pattern)
- Testability through isolated ViewModels

## Goals

1. **Create a browsable UI** for SceneObjectLibrary with filtering and search capabilities
2. **Implement MVVM pattern** following project conventions (ViewModel, View, Controller)
3. **Use UI Toolkit** with UXML/USS for layout and styling
4. **Enable object selection** that integrates with SceneSandboxBuilder placement system
5. **Support all object types** (Actor, Prop, Camera, Light) with category filtering
6. **Maintain 60 FPS performance** on mobile devices with large libraries (100+ objects)

## Non-Goals

- Object editing or manipulation within the library UI (view-only)
- Asset importing or library management features
- 3D preview rendering of objects
- Drag-and-drop from library to scene (phase 1 focuses on selection)
- Multi-select or batch operations

## Proposed Changes

### 1. Scene Object Library UI Capability (New)

**Components:**
- `SceneObjectLibraryViewModel` - Bindable properties for UI state
- `SceneObjectLibraryView` - UI Toolkit visual elements with data binding
- `SceneObjectLibraryController` - Mediator between Model and View
- `SceneObjectLibraryWindow.uxml` - UI layout definition
- `SceneObjectLibraryWindow.uss` - Styling

**Key Features:**
- Type filter tabs (All, Actor, Prop, Camera, Light)
- Category dropdown filter
- Search field with real-time filtering
- Grid view of object cards (icon, name, category badge)
- Selection highlighting
- Integration point with SceneSandboxBuilder

### 2. Integration with SceneSandboxBuilder

- Add event `OnObjectSelected(SceneObjectData obj)` to controller
- SceneSandboxBuilder subscribes to selection events
- Selected object triggers placement mode in builder

## Impact Assessment

### Architecture
- **New System:** Scene Object Library UI (MVVM components)
- **Modified:** SceneSandboxBuilder (adds library UI integration)
- **Dependencies:** UI Toolkit, existing MVVM infrastructure (BindableProperty)

### Performance
- **UI Updates:** Data binding eliminates manual refresh calls
- **Filtering:** In-memory filtering of library data (~100 objects = <1ms)
- **Memory:** ~50KB for UI elements (negligible)
- **Target:** 60 FPS maintained during filtering/search operations

### User Experience
- **Improved:** Intuitive object browsing vs. manual asset selection
- **Workflow:** Select from library → place in scene (streamlined)
- **Learning Curve:** Familiar card-based UI pattern

### Testing
- **Unit Tests:** ViewModel filtering logic, search algorithms
- **Integration Tests:** Controller ↔ View data binding
- **Manual Tests:** Performance with large libraries, mobile device testing

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Performance degradation with 500+ objects | High | Implement object pooling for grid items, virtual scrolling |
| UI Toolkit version compatibility | Medium | Use stable APIs only, document version requirements |
| Integration breaks existing placement | High | Preserve existing placement API, add integration as optional |
| MVVM pattern inconsistency | Low | Follow InventoryView reference implementation |

## Alternatives Considered

### 1. IMGUI-based Editor Window
**Pros:** Simple to implement, Unity Editor native  
**Cons:** Not suitable for runtime UI, poor performance, doesn't follow project MVVM pattern  
**Decision:** Rejected - doesn't meet runtime and mobile requirements

### 2. Direct UI Toolkit without MVVM
**Pros:** Faster initial implementation  
**Cons:** Tight coupling, harder to test, inconsistent with project architecture  
**Decision:** Rejected - violates project conventions

### 3. Prefab-based UI with Canvas
**Pros:** Familiar to Unity developers  
**Cons:** Lower performance, harder styling, more memory overhead  
**Decision:** Rejected - UI Toolkit is the project standard

## Dependencies

- **Unity UI Toolkit** (Unity 6000.2.6f2 built-in)
- **Existing MVVM Infrastructure:** `BindableProperty<T>` from Inventory system
- **SceneObjectLibrary:** Data source (existing)
- **SceneSandboxBuilder:** Integration point (existing)

## Success Criteria

1. ✅ UI displays all objects from SceneObjectLibrary
2. ✅ Type filtering (All/Actor/Prop/Camera/Light) works correctly
3. ✅ Category filtering updates grid in real-time
4. ✅ Search filters objects by name (case-insensitive, partial match)
5. ✅ Object selection triggers placement mode in SceneSandboxBuilder
6. ✅ Performance: 60 FPS with 100+ objects during filtering
7. ✅ UI follows project styling conventions
8. ✅ MVVM pattern correctly implemented (testable ViewModel)

## Timeline Estimate

- **Design & Architecture:** 0.5 hours (this proposal)
- **Implementation:** 3-4 hours
  - ViewModel & Controller: 1 hour
  - UXML/USS Layout: 1 hour
  - View binding: 1 hour
  - Integration: 0.5 hour
  - Testing & polish: 0.5 hour
- **Total:** ~4 hours

## Open Questions

1. **Should the library UI be a persistent panel or a modal window?**
   - Persistent panel allows quick access but takes screen space
   - Modal keeps screen clear but requires open/close actions
   - *Recommendation:* Toggleable panel (collapsed/expanded state)

2. **What should be the default object selection behavior?**
   - Single-click selects for placement immediately
   - Single-click selects, separate button confirms placement
   - *Recommendation:* Single-click selects and enters placement mode (fewer clicks)

3. **Should search be debounced or real-time?**
   - Real-time: Immediate feedback but more frequent updates
   - Debounced (300ms): Reduces updates but slight delay
   - *Recommendation:* Debounced to optimize performance

4. **Icon fallback strategy for objects without icons?**
   - Use object type icons (generic actor/prop/camera/light)
   - Use first letter of object name
   - *Recommendation:* Type-based default icons with distinct colors

## Approval Checklist

- [ ] Architecture reviewed and approved
- [ ] Open questions resolved
- [ ] Success criteria agreed upon
- [ ] Timeline estimate accepted
- [ ] Ready for implementation (move to Stage 2)

---

**Next Steps:** Upon approval, proceed to implementation following `tasks.md`.
