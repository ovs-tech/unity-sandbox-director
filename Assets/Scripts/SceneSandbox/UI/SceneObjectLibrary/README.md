# Scene Object Library UI

## Overview

The Scene Object Library UI provides a user-friendly interface for browsing, filtering, and selecting scene objects (actors, props, cameras, lights) from the SceneObjectLibrary for placement in the Scene Sandbox Builder.

## Features

- **Type Filtering**: Filter objects by type (All, Actor, Prop, Camera, Light)
- **Category Filtering**: Filter objects by category with dynamic category list
- **Search Functionality**: Real-time search with 300ms debouncing for performance
- **Object Cards**: Visual representation of objects with icons, names, and categories
- **Empty State Handling**: User-friendly messages when no objects match filters
- **Icon Fallbacks**: Type-specific color fallbacks for objects without assigned icons
- **Event-Based Integration**: Seamless integration with SceneSandboxBuilder

## Architecture

### MVVM Pattern

The library UI follows the MVVM (Model-View-ViewModel) architectural pattern:

```
┌─────────────────────────────────────────────┐
│          SceneObjectLibrary                 │
│         (ScriptableObject Data)              │
└────────────────────┬────────────────────────┘
                     │
                     │ wraps
                     ▼
┌─────────────────────────────────────────────┐
│       SceneObjectLibraryModel                │
│   (Data Access & Validation Layer)           │
└────────────────────┬────────────────────────┘
                     │
                     │ creates
                     ▼
┌─────────────────────────────────────────────┐
│      SceneObjectLibraryViewModel             │
│   (Filtering Logic & State Management)       │
└────────────────────┬────────────────────────┘
                     │
                     │ updates
                     ▼
┌─────────────────────────────────────────────┐
│      SceneObjectLibraryView                  │
│   (UI Rendering & User Interaction)          │
└────────────────────┬────────────────────────┘
                     │
                     │ fires events
                     ▼
┌─────────────────────────────────────────────┐
│    SceneObjectLibraryController              │
│     (Mediator & Event Coordinator)           │
└────────────────────┬────────────────────────┘
                     │
                     │ integrates
                     ▼
┌─────────────────────────────────────────────┐
│       SceneSandboxBuilder                    │
│    (Object Placement & Scene Management)     │
└─────────────────────────────────────────────┘
```

### Components

#### SceneObjectLibraryViewModel
- **Purpose**: Pure C# filtering logic and bindable state
- **No Unity Dependencies**: Can be unit tested without Editor
- **Bindable Properties**:
  - `FilteredObjects`: List of objects matching current filters
  - `ActiveTypeFilter`: Currently active type filter (null = "All")
  - `ActiveCategoryFilter`: Currently active category filter (empty = "All")
  - `SearchQuery`: Current search query string
  - `SelectedObject`: Currently selected object
  - `AvailableCategories`: List of categories in library

#### SceneObjectLibraryModel
- **Purpose**: Data access wrapper around SceneObjectLibrary
- **Responsibilities**:
  - Validate library exists and has objects
  - Provide access to library data
  - Handle null/empty cases gracefully

#### SceneObjectLibraryView
- **Purpose**: UI rendering and user interaction
- **Features**:
  - Programmatically creates UI elements
  - Renders object cards in responsive grid layout
  - Handles user interactions (clicks, input changes)
  - Emits events for controller to handle
  - Debounced search input (300ms)

#### SceneObjectLibraryController
- **Purpose**: Mediates between Model, ViewModel, and View
- **Responsibilities**:
  - Initializes all components asynchronously
  - Wires up event handlers
  - Exposes `OnObjectSelected` event for integration
  - Manages component lifecycle

## Usage

### Setup in Scene

1. Create a GameObject with a UIDocument component
2. Assign a Canvas/Panel to the UIDocument
3. Add `SceneObjectLibraryController` component to the GameObject
4. Assign your SceneObjectLibrary to the controller's `_objectLibrary` field
5. Assign the UIDocument to the controller's `_uiDocument` field

### Integrating with SceneSandboxBuilder

The controller automatically integrates with SceneSandboxBuilder if assigned:

```csharp
// In SceneSandboxBuilder inspector:
// 1. Create a GameObject with SceneObjectLibraryController
// 2. Assign it to the SceneSandboxBuilder's "_libraryController" field
// 3. On Start(), the integration is automatically set up
```

When an object is selected in the library UI:
1. Library fires `OnObjectSelected` event
2. SceneSandboxBuilder receives the event
3. Placement mode starts with the selected object
4. User can place the object in the scene

### Programmatic Usage

```csharp
// Get the view model (for testing or advanced usage)
var viewModel = libraryController.GetViewModel();

// Subscribe to selection events
libraryController.OnObjectSelected += obj => 
{
    Debug.Log($"Selected: {obj.displayName}");
};

// Change filters programmatically
viewModel.ActiveTypeFilter.Value = SceneObjectType.Actor;
viewModel.SearchQuery.Value = "Hero";
```

## Filtering Logic

### Type Filter
- **All** (null): Shows all objects regardless of type
- **Actor**: Shows only Actor objects
- **Prop**: Shows only Prop objects
- **Camera**: Shows only Camera objects
- **Light**: Shows only Light objects

### Category Filter
- **All** (empty): Shows objects from all categories
- **[Category Name]**: Shows only objects in that category
- Categories are extracted from objects and sorted alphabetically

### Search Filter
- **Case-insensitive** partial matching
- **Debounced** by 300ms for performance
- Filters by `displayName` field

### Combined Filters
All filters work together with AND logic:
```
Filtered = All Objects
  AND (Type Filter OR Type == null)
  AND (Category Filter OR Category == "")
  AND (Name contains SearchQuery OR SearchQuery == "")
```

## Performance Considerations

### Optimization Strategies

1. **Search Debouncing**: 300ms delay prevents excessive filtering
2. **List Filtering**: O(n) complexity acceptable for <500 objects
3. **Card Pooling**: Ready for implementation if >500 objects
4. **Virtual Scrolling**: Deferred to Phase 2 if needed

### Performance Targets
- **Initial Load**: <100ms
- **Filter Update**: <16ms (60 FPS)
- **Search Response**: <50ms after debounce
- **Memory**: <5MB for UI + 100 objects

## Icon System

### Icon Priority
1. Object's assigned icon (if available)
2. Type-specific fallback color:
   - **Actor**: Blue
   - **Prop**: Green
   - **Camera**: Yellow
   - **Light**: White

### Implementing Custom Icons

Add icons to SceneObjectData objects:

```csharp
var objectData = new SceneObjectData 
{
    id = "hero1",
    displayName = "Hero",
    objectType = SceneObjectType.Actor,
    icon = Resources.Load<Sprite>("Icons/actor_hero") // Add sprite
};
```

## Testing

### Unit Tests

Run ViewModel tests to validate filtering logic:

```
Assets/Scripts/SceneSandbox/UI/SceneObjectLibrary/Tests/SceneObjectLibraryViewModelTests.cs
```

Tests cover:
- Single filter type operations
- Combined filter operations
- Search functionality
- Category extraction and sorting
- Empty state handling

### Manual Testing

1. **Basic Functionality**
   - Create scene with SceneObjectLibraryController
   - Verify all objects display in grid
   - Test type filtering buttons
   - Test category dropdown
   - Test search input

2. **Integration Testing**
   - Assign library to SceneSandboxBuilder
   - Select an object in library
   - Verify placement mode starts
   - Verify selected object data is passed

3. **Performance Testing**
   - Test with 10, 50, 100, 200 objects
   - Monitor frame rate during filtering
   - Monitor memory usage
   - Test on target mobile device

## Future Enhancements

### Phase 2 Planned Features
- **Virtual Scrolling**: Optimize for 500+ objects
- **Drag-and-Drop**: Drag objects directly to scene
- **Multi-Select**: Select multiple objects
- **Favorites**: Pin frequently used objects
- **Grid/List Toggle**: Switch between view modes
- **3D Preview**: Preview objects on hover
- **Custom Sorting**: Sort by name, type, recently used

### Extension Points
- `IFilterStrategy`: Plugin custom filters
- `ICardRenderer`: Custom card layouts per type
- `IObjectSource`: Support multiple library sources

## Troubleshooting

### Objects Not Displaying
- Verify SceneObjectLibrary has objects
- Check that object `id` and `displayName` are set
- Verify object `objectType` is valid

### UI Not Showing
- Verify UIDocument is assigned to controller
- Check that UIDocument has a Canvas/Panel
- Verify controller `OnEnable()` is called

### Selection Not Working
- Verify SceneObjectLibraryController is assigned in SceneSandboxBuilder
- Check console for integration errors
- Verify SceneObjectLibrary has a valid SceneObjectLibrary reference

### Performance Issues
- Check object count (consider virtual scrolling if >500)
- Profile search debounce time
- Monitor GC allocations during filtering

## Architecture Documentation

See also:
- [Design Document](../design.md) - Detailed architecture decisions
- [Proposal](../proposal.md) - Feature requirements and goals
- [Specification](specs/scene-object-library-ui/spec.md) - Detailed requirements

## Credits

Implemented as part of the Ero Director (Mobile & AR Sandbox Edition) project.

**Pattern**: MVVM (Model-View-ViewModel)  
**Framework**: Unity 6000.2.6f2  
**UI System**: Unity UI Toolkit  
**Status**: MVP Complete - Ready for testing and optimization
