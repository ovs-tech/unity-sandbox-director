# Design Document: Scene Object Library UI

**Change ID:** `add-scene-object-library-ui`  
**System:** Scene Object Library UI with MVVM  
**Created:** 2025-12-15

## Overview

This document captures the architectural decisions and technical patterns for implementing a Unity UI Toolkit-based interface for SceneObjectLibrary using the MVVM pattern. The design enables efficient browsing, filtering, and selection of scene objects for placement in the Scene Sandbox Builder.

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                    SceneSandboxBuilder                      │
│                  (Integration Point)                         │
└──────────────────────┬──────────────────────────────────────┘
                       │ subscribes to
                       │ OnObjectSelected
┌──────────────────────▼──────────────────────────────────────┐
│          SceneObjectLibraryController                       │
│              (Mediator/Orchestrator)                         │
│  • Initializes Model, ViewModel, View                      │
│  • Handles business logic events                            │
│  • Exposes OnObjectSelected event                           │
└──────────┬────────────────────────────┬─────────────────────┘
           │                            │
           │ creates & updates          │ initializes with
           ▼                            ▼
┌──────────────────────┐    ┌──────────────────────────────┐
│  SceneObjectLibrary  │    │ SceneObjectLibraryViewModel │
│       Model          │    │   (Bindable State)          │
│                      │    │                             │
│  • Wraps SO data     │───▶│  • FilteredObjects          │
│  • Observable        │    │  • ActiveTypeFilter         │
│    collections       │    │  • ActiveCategoryFilter     │
│  • Change events     │    │  • SearchQuery              │
│                      │    │  • SelectedObject           │
│                      │    │  • AvailableCategories      │
└──────────────────────┘    └──────────────┬───────────────┘
                                           │
                                           │ data binding
                                           ▼
                            ┌──────────────────────────────┐
                            │ SceneObjectLibraryView      │
                            │    (UI Toolkit)             │
                            │                             │
                            │  • UXML structure           │
                            │  • USS styling              │
                            │  • Event handlers           │
                            │  • Visual element refs      │
                            └─────────────────────────────┘
```

## Design Decisions

### 1. MVVM Pattern Implementation

**Decision:** Follow the existing InventoryView MVVM pattern established in the project.

**Rationale:**
- Consistency with project architecture conventions
- Separation of concerns enables unit testing
- ViewModel can be tested without Unity Editor
- Data binding reduces manual UI refresh code
- Proven pattern in the codebase

**Implementation Details:**

```csharp
// ViewModel - Pure C#, no Unity dependencies
public class SceneObjectLibraryViewModel 
{
    public readonly BindableProperty<List<SceneObjectData>> FilteredObjects;
    public readonly BindableProperty<SceneObjectType?> ActiveTypeFilter;
    public readonly BindableProperty<string> ActiveCategoryFilter;
    public readonly BindableProperty<string> SearchQuery;
    public readonly BindableProperty<SceneObjectData> SelectedObject;
    public readonly BindableProperty<List<string>> AvailableCategories;
    
    // Filtering logic lives here
    private void UpdateFilteredObjects() { ... }
}

// View - Unity UI Toolkit components
public class SceneObjectLibraryView : MonoBehaviour 
{
    public IEnumerator InitializeView(SceneObjectLibraryViewModel viewModel)
    {
        // Setup data bindings
        objectGrid.dataSource = viewModel.FilteredObjects;
        // ... bind other properties
        yield return null;
    }
}

// Controller - Mediator
public class SceneObjectLibraryController 
{
    public event Action<SceneObjectData> OnObjectSelected;
    
    IEnumerator Initialize()
    {
        yield return view.InitializeView(new SceneObjectLibraryViewModel(model));
        view.OnCardClicked += HandleCardClicked;
        // ... wire up other events
    }
}
```

**Trade-offs:**
- ✅ **Pro:** Testable, maintainable, consistent with project
- ✅ **Pro:** Clear separation of concerns
- ❌ **Con:** More boilerplate than direct UI manipulation
- ❌ **Con:** Learning curve for developers unfamiliar with MVVM

### 2. UI Toolkit vs IMGUI/Canvas

**Decision:** Use Unity UI Toolkit with UXML/USS for all UI components.

**Rationale:**
- Project standard for UI (see InventoryView, TimelineEditorUIToolkit)
- Better performance than IMGUI or Canvas on mobile
- Declarative layout (UXML) separates structure from logic
- CSS-like styling (USS) enables theme consistency
- Supports data binding for reactive updates

**Implementation Details:**
- **UXML:** Layout structure for window, cards, filters
- **USS:** Visual styling, responsive grid, states (hover, selected)
- **C# View:** References visual elements, handles interactions

**Trade-offs:**
- ✅ **Pro:** High performance, modern API, better styling
- ✅ **Pro:** Runtime + Editor compatible
- ❌ **Con:** Less mature than Canvas, fewer learning resources
- ❌ **Con:** Requires understanding of UXML/USS

### 3. Filtering Architecture

**Decision:** All filtering logic resides in the ViewModel.

**Filtering Strategy:**
```csharp
public class SceneObjectLibraryViewModel 
{
    private List<SceneObjectData> ApplyFilters(List<SceneObjectData> allObjects)
    {
        var filtered = allObjects;
        
        // Type filter
        if (ActiveTypeFilter.Value != null)
            filtered = filtered.Where(o => o.objectType == ActiveTypeFilter.Value).ToList();
        
        // Category filter
        if (!string.IsNullOrEmpty(ActiveCategoryFilter.Value))
            filtered = filtered.Where(o => o.category == ActiveCategoryFilter.Value).ToList();
        
        // Search filter (case-insensitive, partial match)
        if (!string.IsNullOrEmpty(SearchQuery.Value))
            filtered = filtered.Where(o => 
                o.displayName.Contains(SearchQuery.Value, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        
        return filtered;
    }
}
```

**Rationale:**
- Pure C# filtering enables unit testing without Unity
- Centralized logic prevents duplication
- Easy to extend with additional filter criteria
- Performance: O(n) filtering is acceptable for <500 objects

**Trade-offs:**
- ✅ **Pro:** Testable, maintainable, single source of truth
- ✅ **Pro:** Can optimize (caching, indexing) in one place
- ❌ **Con:** Re-filters entire list on each change (acceptable for small datasets)

**Optimization Strategy (if needed for 500+ objects):**
- Implement incremental filtering
- Add filter result caching
- Use ObservableCollection for delta updates

### 4. Search Debouncing

**Decision:** Debounce search input by 300ms to reduce filtering frequency.

**Implementation:**
```csharp
private Coroutine _searchDebounceCoroutine;

void OnSearchFieldChanged(ChangeEvent<string> evt)
{
    if (_searchDebounceCoroutine != null)
        StopCoroutine(_searchDebounceCoroutine);
    
    _searchDebounceCoroutine = StartCoroutine(DebounceSearch(evt.newValue));
}

IEnumerator DebounceSearch(string query)
{
    yield return new WaitForSeconds(0.3f);
    viewModel.SearchQuery.Value = query;
}
```

**Rationale:**
- Reduces filtering calls from ~10/sec (typing) to ~3/sec
- Improves performance with large libraries
- Better UX (no flickering during fast typing)

**Trade-offs:**
- ✅ **Pro:** Significant performance improvement
- ✅ **Pro:** Smoother UI experience
- ❌ **Con:** 300ms delay before results appear

### 5. Object Card Rendering

**Decision:** Use UXML template instantiation with object pooling for 100+ objects.

**Card Structure (UXML):**
```xml
<ui:VisualElement name="ObjectCard" class="object-card">
    <ui:VisualElement name="CardIcon" class="card-icon" />
    <ui:Label name="CardName" class="card-name" />
    <ui:Label name="CardCategory" class="card-category-badge" />
</ui:VisualElement>
```

**Pooling Strategy:**
```csharp
private Queue<VisualElement> _cardPool = new Queue<VisualElement>();

VisualElement GetOrCreateCard()
{
    if (_cardPool.Count > 0)
        return _cardPool.Dequeue();
    
    return _cardTemplate.CloneTree();
}

void ReturnCard(VisualElement card)
{
    card.RemoveFromHierarchy();
    _cardPool.Enqueue(card);
}
```

**Rationale:**
- Pooling reduces GC allocations (important for mobile)
- Template reuse maintains consistency
- Lazy instantiation only creates visible cards (if virtual scrolling added)

**Trade-offs:**
- ✅ **Pro:** Zero GC allocation after initial pool creation
- ✅ **Pro:** Consistent appearance across all cards
- ❌ **Con:** Additional complexity vs. direct instantiation

**Alternative Considered:** Virtual scrolling (only render visible cards)
- Deferred to Phase 2 if 100+ objects cause performance issues
- Would reduce memory and rendering cost for large libraries

### 6. Icon Fallback System

**Decision:** Use type-based default icons with distinct colors when object icons are missing.

**Fallback Mapping:**
```csharp
private static readonly Dictionary<SceneObjectType, (Sprite Icon, Color Color)> _defaultIcons = new()
{
    { SceneObjectType.Actor, (ActorIcon, Color.blue) },
    { SceneObjectType.Prop, (PropIcon, Color.green) },
    { SceneObjectType.Camera, (CameraIcon, Color.yellow) },
    { SceneObjectType.Light, (LightIcon, Color.white) }
};

Sprite GetIcon(SceneObjectData obj)
{
    return obj.icon != null ? obj.icon : _defaultIcons[obj.objectType].Icon;
}
```

**Rationale:**
- Prevents blank cards when icons are missing
- Type-coded colors aid quick visual scanning
- Graceful degradation

**Trade-offs:**
- ✅ **Pro:** Robust handling of missing data
- ✅ **Pro:** Improves visual hierarchy
- ❌ **Con:** Requires creating/assigning default icon assets

### 7. Integration with SceneSandboxBuilder

**Decision:** Use event-based integration with minimal coupling.

**Integration Pattern:**
```csharp
// In SceneSandboxBuilder
private SceneObjectLibraryController _libraryController;

void Start()
{
    _libraryController = GetComponent<SceneObjectLibraryController>();
    _libraryController.OnObjectSelected += HandleLibraryObjectSelected;
}

void HandleLibraryObjectSelected(SceneObjectData obj)
{
    // Convert to placement format
    StartObjectPlacement(obj.prefab, obj.id);
}
```

**Rationale:**
- Loose coupling through events
- SceneSandboxBuilder remains independent
- Library can be used standalone or integrated
- Easy to mock for testing

**Trade-offs:**
- ✅ **Pro:** Loose coupling, testable, flexible
- ✅ **Pro:** Preserves existing SceneSandboxBuilder API
- ❌ **Con:** Event-based flow harder to trace than direct calls

## Data Flow

### Selection Flow
```
User clicks card
    ↓
View.OnCardClicked fires
    ↓
Controller.HandleCardClicked(SceneObjectData)
    ↓
ViewModel.SelectedObject.Value = obj (updates binding)
    ↓
Controller.OnObjectSelected fires
    ↓
SceneSandboxBuilder.HandleLibraryObjectSelected
    ↓
Placement mode starts
```

### Filtering Flow
```
User changes type filter
    ↓
View.OnTypeFilterChanged fires
    ↓
ViewModel.ActiveTypeFilter.Value = newType
    ↓
ViewModel.UpdateFilteredObjects() executes
    ↓
ViewModel.FilteredObjects.Value = filtered list
    ↓
UI automatically updates (data binding)
    ↓
Cards re-render in grid
```

## Performance Considerations

### Bottlenecks & Mitigations

| Bottleneck | Impact | Mitigation |
|------------|--------|------------|
| Filtering large lists (500+ objects) | Medium | Debounced search, cached filters, indexed lookups |
| Card instantiation (100+ cards) | High | Object pooling, virtual scrolling |
| GC allocations from List creation | Low | Use `List.Clear()` + reuse, avoid LINQ where possible |
| Icon loading (texture uploads) | Low | Lazy loading, icon atlasing |

### Performance Targets
- **Initial Load:** <100ms to display library
- **Filter Update:** <16ms (60 FPS)
- **Search (debounced):** <50ms after debounce
- **Memory:** <5MB for UI + 100 objects

### Profiling Strategy
1. Unity Profiler: Frame time during filtering
2. Memory Profiler: Detect leaks from card instantiation
3. Deep Profile: Identify LINQ allocations
4. Mobile device testing: Validate 60 FPS on target hardware

## Error Handling

### Graceful Degradation

1. **Missing SceneObjectLibrary:** Show message "No library assigned"
2. **Empty library:** Show "No objects available. Add objects to library."
3. **No search results:** Show "No objects match your search"
4. **Missing icons:** Use type-based default icons
5. **Null references:** Log error, disable affected UI component

### Validation
```csharp
void Initialize()
{
    if (sceneObjectLibrary == null)
    {
        Debug.LogError("SceneObjectLibrary not assigned!");
        ShowErrorMessage("Library missing. Please assign in Inspector.");
        return;
    }
    
    if (sceneObjectLibrary.GetAllObjects().Count == 0)
    {
        Debug.LogWarning("SceneObjectLibrary is empty.");
        ShowEmptyState();
        return;
    }
    
    // Normal initialization
}
```

## Testing Strategy

### Unit Tests (ViewModel)
```csharp
[Test]
public void FilterByType_ReturnsOnlyActors()
{
    var viewModel = new SceneObjectLibraryViewModel(mockModel);
    viewModel.ActiveTypeFilter.Value = SceneObjectType.Actor;
    
    Assert.IsTrue(viewModel.FilteredObjects.Value.All(o => o.objectType == SceneObjectType.Actor));
}

[Test]
public void SearchFilter_CaseInsensitive_PartialMatch()
{
    var viewModel = new SceneObjectLibraryViewModel(mockModel);
    viewModel.SearchQuery.Value = "hero";
    
    Assert.IsTrue(viewModel.FilteredObjects.Value.All(o => 
        o.displayName.Contains("hero", StringComparison.OrdinalIgnoreCase)));
}

[Test]
public void CombinedFilters_TypeAndCategory_ReturnsIntersection()
{
    var viewModel = new SceneObjectLibraryViewModel(mockModel);
    viewModel.ActiveTypeFilter.Value = SceneObjectType.Prop;
    viewModel.ActiveCategoryFilter.Value = "Furniture";
    
    var filtered = viewModel.FilteredObjects.Value;
    Assert.IsTrue(filtered.All(o => 
        o.objectType == SceneObjectType.Prop && o.category == "Furniture"));
}
```

### Integration Tests
- ViewModel → View data binding updates UI
- Controller initialization flow succeeds
- Selection event propagates to SceneSandboxBuilder

### Manual Tests
- Test with 10, 50, 100, 200 objects
- Profile FPS during filtering operations
- Test on mid-range Android device
- Verify all filter combinations work correctly

## Future Extensions

### Phase 2 Enhancements (Post-MVP)
1. **Virtual Scrolling:** Only render visible cards (for 500+ objects)
2. **Drag-and-Drop:** Drag objects directly from library to scene
3. **Multi-Select:** Select multiple objects for batch placement
4. **Custom Sorting:** Sort by name, type, recently used
5. **Favorites System:** Pin frequently used objects to top
6. **Grid/List Toggle:** Switch between grid and list view
7. **Object Preview:** 3D preview on hover/selection
8. **Categories Editor:** Add/remove categories directly in UI

### Extensibility Points
- **IFilterStrategy:** Plugin custom filters
- **ICardRenderer:** Custom card layouts per object type
- **IObjectSource:** Support multiple library sources

## Dependencies

### Internal
- `BindableProperty<T>` (from Inventory system)
- `SceneObjectLibrary` (existing ScriptableObject)
- `SceneObjectData` (existing data structure)
- `SceneSandboxBuilder` (integration point)

### External (Unity)
- UI Toolkit (Unity 6000.2.6f2 built-in)
- Input System (for toggle hotkey)
- Coroutines (for debouncing)

## Open Technical Questions

1. **Should we implement virtual scrolling in Phase 1?**
   - **Recommendation:** No. Implement only if testing reveals FPS <60 with 100+ objects
   - **Reasoning:** Adds complexity, may not be needed

2. **Where should default icons live?**
   - **Option A:** Resources folder (always loaded)
   - **Option B:** Addressables (lazy loaded)
   - **Recommendation:** Resources for simplicity (4 icons = ~100KB)

3. **Should category dropdown be searchable?**
   - **Recommendation:** No for Phase 1. Add if >20 categories
   - **Reasoning:** Typical libraries have <10 categories

## Sign-off

- [ ] Architecture reviewed
- [ ] Performance targets agreed
- [ ] Testing strategy approved
- [ ] Integration approach validated
- [ ] Ready for implementation

---

**Next Steps:** Proceed with Phase 1 implementation following `tasks.md`.
