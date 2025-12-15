# Scene Object Library UI Specification

**Capability ID:** `scene-object-library-ui`  
**Status:** Proposed  
**Version:** 1.0.0  
**Last Updated:** 2025-12-15

---

## ADDED Requirements

### REQ-1: Object Library Display

The system SHALL provide a UI for browsing scene objects from SceneObjectLibrary.

**Context:**
Users need an intuitive interface to view all available scene objects (actors, props, cameras, lights) that can be placed in the scene sandbox. The library may contain 10-200+ objects across multiple categories.

#### Scenario: Display all objects in grid layout

**Given** a SceneObjectLibrary with 50 objects  
**When** the user opens the library UI  
**Then** the UI displays all 50 objects in a grid layout  
**And** each object shows its icon, name, and category badge  
**And** the grid is scrollable if objects exceed viewport  

#### Scenario: Display empty library state

**Given** a SceneObjectLibrary with 0 objects  
**When** the user opens the library UI  
**Then** the UI displays "No objects available" message  
**And** provides instructions to add objects to the library  

#### Scenario: Handle missing object icons

**Given** an object without an assigned icon  
**When** the UI renders that object's card  
**Then** the card displays a default icon based on object type  
**And** the icon uses a type-specific color (Actor=blue, Prop=green, Camera=yellow, Light=white)  

---

### REQ-2: Type-Based Filtering

The system SHALL allow users to filter objects by type (Actor, Prop, Camera, Light).

**Context:**
Users working on specific tasks (e.g., placing props) need to quickly narrow down the object list to relevant types. Type filtering is the primary way to categorize objects.

#### Scenario: Filter to show only actors

**Given** a library with 20 actors, 15 props, 10 cameras, 5 lights  
**When** the user clicks the "Actor" filter button  
**Then** the grid displays only the 20 actor objects  
**And** the "Actor" button is visually highlighted as active  
**And** the filter updates in <100ms  

#### Scenario: Show all objects when "All" is selected

**Given** the user has filtered to "Props" (showing 15 objects)  
**When** the user clicks the "All" button  
**Then** the grid displays all 50 objects across all types  
**And** the "All" button is visually highlighted as active  

#### Scenario: Multiple type filters cannot be active simultaneously

**Given** the "Actor" filter is active  
**When** the user clicks the "Prop" filter button  
**Then** the "Actor" filter deactivates  
**And** only the "Prop" filter is active  
**And** the grid shows only props  

---

### REQ-3: Category-Based Filtering

The system SHALL allow users to filter objects by category.

**Context:**
Objects within a type may be further organized by categories (e.g., "Furniture", "Characters", "Weapons"). Categories are dynamically built from object metadata and may vary per library.

#### Scenario: Filter objects by selected category

**Given** a library with objects in categories "Furniture", "Vehicles", "Characters"  
**And** the category dropdown is set to "Furniture"  
**When** the user opens the dropdown and selects "Vehicles"  
**Then** the grid displays only objects with category "Vehicles"  
**And** the category dropdown shows "Vehicles" as selected  

#### Scenario: Category filter combines with type filter

**Given** the type filter is set to "Prop"  
**And** the category filter is set to "Furniture"  
**When** the grid updates  
**Then** the grid displays only objects that are Props AND in the Furniture category  
**And** objects that are Props but in other categories are hidden  

#### Scenario: Category dropdown populated from library

**Given** a library with objects having categories: "A", "B", "C", "Default"  
**When** the UI initializes  
**Then** the category dropdown contains options: "All", "Default", "A", "B", "C"  
**And** "Default" appears first after "All"  
**And** other categories are alphabetically sorted  

---

### REQ-4: Search Functionality

The system SHALL provide text search to filter objects by name.

**Context:**
Users may know the name of an object they want to place. Search enables fast access without scrolling or filtering through categories.

#### Scenario: Search filters objects by partial name match

**Given** a library with objects named "Hero", "Hero_Alt", "Villain", "Sidekick"  
**When** the user types "hero" in the search field  
**Then** the grid displays "Hero" and "Hero_Alt"  
**And** the search is case-insensitive  
**And** the grid updates after a 300ms debounce  

#### Scenario: Search combines with type and category filters

**Given** the type filter is "Actor" and category filter is "Characters"  
**And** the search field contains "hero"  
**When** the filters are applied  
**Then** the grid shows objects matching ALL criteria: type=Actor, category=Characters, name contains "hero"  

#### Scenario: Clear search restores filtered view

**Given** the search field contains "hero" (showing 2 objects)  
**When** the user clears the search field  
**Then** the grid returns to the previously active filters (type/category)  
**And** all matching objects for those filters are displayed  

#### Scenario: No search results displays empty state

**Given** a library with 50 objects  
**When** the user searches for "nonexistent"  
**Then** the grid is empty  
**And** the UI displays "No objects match your search"  
**And** the search field remains populated with "nonexistent"  

---

### REQ-5: Object Selection

The system SHALL allow users to select an object and trigger placement mode.

**Context:**
The primary purpose of the library UI is to select objects for placement in the scene. Selection should integrate seamlessly with the existing SceneSandboxBuilder placement system.

#### Scenario: Click object card to select

**Given** the library UI is open  
**When** the user clicks on an object card  
**Then** that object card is visually highlighted as selected  
**And** the `OnObjectSelected` event fires with the selected object data  
**And** the selection happens within 50ms of click  

#### Scenario: Only one object can be selected at a time

**Given** object "A" is currently selected  
**When** the user clicks object "B"  
**Then** object "A" is deselected (no highlight)  
**And** object "B" is selected (highlighted)  
**And** the `OnObjectSelected` event fires with object "B" data  

#### Scenario: Selection triggers placement mode in SceneSandboxBuilder

**Given** the SceneSandboxBuilder is integrated and listening for selections  
**When** the user selects an object from the library  
**Then** the SceneSandboxBuilder receives the object data via event  
**And** the SceneSandboxBuilder enters placement mode with the selected object  
**And** the user can now place the object in the scene  

---

### REQ-6: Performance Requirements

The system SHALL maintain 60 FPS during all UI operations on target hardware.

**Context:**
The library may contain 100+ objects. UI operations (filtering, search, scrolling) must be optimized to maintain smooth 60 FPS performance on mid-range mobile devices (Android/iOS).

#### Scenario: Filtering large library maintains 60 FPS

**Given** a library with 150 objects  
**When** the user changes the type filter  
**Then** the grid updates within 100ms  
**And** the frame rate does not drop below 60 FPS during update  
**And** no GC allocations occur during filter updates  

#### Scenario: Search with debouncing reduces update frequency

**Given** the user is typing in the search field  
**When** the user types 5 characters rapidly (within 1 second)  
**Then** the search filter is applied only once (300ms after last keystroke)  
**And** intermediate keystrokes do not trigger filtering  
**And** the frame rate remains at 60 FPS during typing  

#### Scenario: Object card pooling prevents GC pressure

**Given** the library has 100 objects  
**When** the user switches between type filters multiple times  
**Then** object cards are reused from a pool rather than recreated  
**And** no GC allocations occur after the initial pool creation  
**And** memory usage remains stable (no growth per filter change)  

---

### REQ-7: MVVM Architecture

The system SHALL implement MVVM pattern for separation of concerns and testability.

**Context:**
The project uses MVVM architecture (established in InventoryView) for UI components. This pattern enables unit testing of UI logic without Unity Editor and maintains consistency with the codebase.

#### Scenario: ViewModel is pure C# without Unity dependencies

**Given** the SceneObjectLibraryViewModel class  
**When** the class is inspected  
**Then** it contains no references to `UnityEngine` namespace  
**And** all properties are BindableProperty<T> or readonly values  
**And** the ViewModel can be instantiated and tested in a standard .NET test runner  

#### Scenario: View binds to ViewModel properties

**Given** a ViewModel with `FilteredObjects` property  
**When** the View initializes with the ViewModel  
**Then** the View's grid container is data-bound to `FilteredObjects`  
**And** changes to `FilteredObjects.Value` automatically update the grid  
**And** no manual refresh methods are called in the View  

#### Scenario: Controller mediates Model and View

**Given** a SceneObjectLibraryController instance  
**When** the Controller initializes  
**Then** it creates the Model (data source wrapper)  
**And** creates the ViewModel with data from Model  
**And** initializes the View with the ViewModel  
**And** subscribes to View events and Model change events  
**And** exposes `OnObjectSelected` event for external integration  

---

### REQ-8: UI Toolkit Implementation

The system SHALL use Unity UI Toolkit (UXML/USS) for all UI components.

**Context:**
The project standard is UI Toolkit for runtime UI. This requirement ensures consistency with other UI systems (InventoryView, TimelineEditorUIToolkit) and leverages UI Toolkit's performance benefits.

#### Scenario: Layout defined in UXML

**Given** the SceneObjectLibraryWindow.uxml file  
**When** the file is parsed  
**Then** it defines the complete UI structure (filters, search, grid)  
**And** no UI elements are created in C# code (except dynamic cards)  
**And** the UXML can be edited in UI Builder without code changes  

#### Scenario: Styling defined in USS

**Given** the SceneObjectLibraryWindow.uss file  
**When** applied to the View  
**Then** all visual styling (colors, fonts, spacing, states) is defined in USS  
**And** hover states, active states, and selected states use USS classes  
**And** no styling is hardcoded in C# (except dynamic data like icons)  

#### Scenario: Object cards use UXML template

**Given** an ObjectCard.uxml template  
**When** the View needs to display an object  
**Then** the template is instantiated via `CloneTree()`  
**And** the card's data fields (icon, name, category) are populated from SceneObjectData  
**And** all cards share consistent styling from the template  

---

### REQ-9: Integration with SceneSandboxBuilder

The system SHALL integrate with SceneSandboxBuilder for object placement.

**Context:**
The library UI is not standalone—it must work with the existing SceneSandboxBuilder to enable object placement. Integration should be loose-coupled through events.

#### Scenario: Library controller exposes selection event

**Given** a SceneObjectLibraryController instance  
**When** the Controller is instantiated  
**Then** it exposes a public `OnObjectSelected` event of type `Action<SceneObjectData>`  
**And** external systems can subscribe to this event  

#### Scenario: SceneSandboxBuilder subscribes to selection events

**Given** a SceneSandboxBuilder with an assigned SceneObjectLibraryController  
**When** SceneSandboxBuilder initializes  
**Then** it subscribes to `libraryController.OnObjectSelected`  
**And** when an event fires, SceneSandboxBuilder receives the selected object data  

#### Scenario: Selection triggers placement without breaking existing flow

**Given** SceneSandboxBuilder is in Build Mode  
**And** a user selects an object from the library  
**When** the selection event fires  
**Then** SceneSandboxBuilder starts placement mode with the selected object's prefab  
**And** existing placement functionality (manual prefab assignment) continues to work  
**And** the library integration is optional (SceneSandboxBuilder works without it)  

---

### REQ-10: UI Panel Toggle

The system SHALL provide a way to show/hide the library UI panel.

**Context:**
The library UI may occupy significant screen space. Users should be able to toggle it on/off as needed, especially on mobile devices with limited screen real estate.

#### Scenario: Panel can be toggled via input

**Given** the library UI panel is visible  
**When** the user presses the library toggle key (e.g., "L")  
**Then** the panel hides from view  
**And** pressing the key again shows the panel  

#### Scenario: Panel state persists during session

**Given** the user has hidden the library panel  
**When** the user switches scenes or enters Play Mode  
**Then** the panel remains hidden  
**And** the last panel state is preserved  

#### Scenario: Panel can be toggled via UI button

**Given** a toggle button in the UI  
**When** the user clicks the button  
**Then** the panel visibility toggles (shown ↔ hidden)  
**And** the button icon updates to reflect current state  

---

## Performance Benchmarks

| Metric | Target | Measurement Condition |
|--------|--------|-----------------------|
| Initial Load Time | <100ms | Display 100 objects from library |
| Filter Update Time | <100ms | Switch between type filters (100 objects) |
| Search Update Time | <50ms | After 300ms debounce (100 objects) |
| Frame Rate (Filtering) | 60 FPS | Sustained during filter changes |
| Frame Rate (Scrolling) | 60 FPS | Smooth scrolling through 100+ objects |
| Memory Overhead | <5MB | UI + 100 object references |
| GC Allocations (per frame) | 0 bytes | After initial pool creation |

---

## Acceptance Criteria Summary

- [ ] All 10 requirements implemented and passing tests
- [ ] MVVM pattern correctly implemented (ViewModel testable without Unity)
- [ ] UI Toolkit (UXML/USS) used for all UI components
- [ ] Type filtering (All, Actor, Prop, Camera, Light) functional
- [ ] Category filtering combines with type filtering
- [ ] Search filters by name (case-insensitive, debounced)
- [ ] Object selection triggers `OnObjectSelected` event
- [ ] Integration with SceneSandboxBuilder enables placement
- [ ] Performance targets met (60 FPS, <100ms filter updates)
- [ ] UI panel can be toggled on/off
- [ ] Empty states handled gracefully
- [ ] Missing icon fallback system works

---

## Dependencies

### Internal
- `SceneObjectLibrary` (Data source)
- `SceneObjectData` (Object metadata)
- `SceneObjectType` (Enum for type filtering)
- `BindableProperty<T>` (MVVM data binding)
- `SceneSandboxBuilder` (Integration point)

### External
- Unity UI Toolkit (6000.2.6f2+)
- Unity Input System (for toggle hotkey)

---

## Related Specifications

- **Inventory System:** Reference implementation for MVVM + UI Toolkit pattern
- **Scene Sandbox Builder:** Integration target for object placement
- **Timeline Editor UI:** Similar UI Toolkit usage patterns

---

**Status:** Ready for implementation upon approval of change proposal `add-scene-object-library-ui`.
