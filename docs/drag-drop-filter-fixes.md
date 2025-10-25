# Drag & Drop and Filter Fixes

## Issues Found

### Issue 1: Filter Not Updating ❌
**Problem**: When changing category, type, or search, filters showed:
```
[FILTER] RefreshPalette() called - Type: -1, Category: 'All', Search: ''
```
Even though UI controls were changed.

**Root Cause**: Filter UI elements (`type-filter`, `category-filter`, `search-field`) were not found when querying the UI hierarchy, so event callbacks were never registered.

**Fix**: Added debug logging to `QueryUIElements()` and `SetupUI()` to verify which elements are found and whether callbacks are registered.

---

### Issue 2: Drag Position is NaN ❌
**Problem**: Drag logs showed:
```
[DRAG] Panel position: (NaN, NaN)
[DRAG] Cannot convert position - root element or panel is null
```

**Root Cause**: The drag position passed from `ObjectPaletteItemUIToolkit` was using `evt.position` which is in **element-local coordinates**, not panel/screen coordinates. When the palette was integrated into another UIDocument, the local coordinates couldn't be properly converted because they were relative to the wrong coordinate space.

**Fix**: Changed `ObjectPaletteItemUIToolkit` to use `_rootElement.LocalToWorld(evt.localPosition)` to convert element-local coordinates to panel coordinates before passing them to the palette.

---

## Code Changes

### File: `ObjectPaletteItemUIToolkit.cs`

#### Change 1: OnPointerMove - Convert to Panel Coordinates
```csharp
// OLD (using evt.position - local coordinates)
if (!_isDragging && dragDistance > 5f)
{
    _isDragging = true;
    CreateDragPreview(evt.position);
    OnItemDragStarted?.Invoke(this, evt.position);  // ❌ Local coords
}

// NEW (convert to panel coordinates)
if (!_isDragging && dragDistance > 5f)
{
    _isDragging = true;
    
    // Convert local position to panel position
    Vector2 panelPosition = _rootElement.LocalToWorld(evt.localPosition);
    
    CreateDragPreview(evt.position);
    OnItemDragStarted?.Invoke(this, panelPosition);  // ✅ Panel coords
}
```

#### Change 2: OnPointerUp - Convert to Panel Coordinates
```csharp
// OLD
private void OnPointerUp(PointerUpEvent evt)
{
    if (_isDragging)
    {
        DestroyDragPreview();
        OnItemDragEnded?.Invoke(this, evt.position);  // ❌ Local coords
        evt.StopPropagation();
    }
    
    _isDragging = false;
    _rootElement.ReleasePointer(evt.pointerId);
}

// NEW
private void OnPointerUp(PointerUpEvent evt)
{
    if (_isDragging)
    {
        // Convert local position to panel position
        Vector2 panelPosition = _rootElement.LocalToWorld(evt.localPosition);
        
        DestroyDragPreview();
        OnItemDragEnded?.Invoke(this, panelPosition);  // ✅ Panel coords
        evt.StopPropagation();
    }
    
    _isDragging = false;
    _rootElement.ReleasePointer(evt.pointerId);
}
```

---

### File: `ObjectPaletteUIToolkit.cs`

#### Change 1: QueryUIElements - Added Debug Logging
```csharp
private void QueryUIElements()
{
    Debug.Log("[FILTER] QueryUIElements() - Searching for UI elements...");
    
    _paletteContainer = _rootElement.Q<VisualElement>("palette-container");
    Debug.Log($"[FILTER] palette-container: {(_paletteContainer != null ? "Found" : "NOT FOUND")}");
    
    _typeFilter = _rootElement.Q<DropdownField>("type-filter");
    Debug.Log($"[FILTER] type-filter: {(_typeFilter != null ? "Found" : "NOT FOUND")}");
    
    _categoryFilter = _rootElement.Q<DropdownField>("category-filter");
    Debug.Log($"[FILTER] category-filter: {(_categoryFilter != null ? "Found" : "NOT FOUND")}");
    
    _searchField = _rootElement.Q<TextField>("search-field");
    Debug.Log($"[FILTER] search-field: {(_searchField != null ? "Found" : "NOT FOUND")}");
    
    _refreshButton = _rootElement.Q<Button>("refresh-button");
    Debug.Log($"[FILTER] refresh-button: {(_refreshButton != null ? "Found" : "NOT FOUND")}");
    
    _itemsScrollView = _rootElement.Q<ScrollView>("items-scroll-view");
    Debug.Log($"[FILTER] items-scroll-view: {(_itemsScrollView != null ? "Found" : "NOT FOUND")}");
    
    _itemsContainer = _rootElement.Q<VisualElement>("items-container");
    Debug.Log($"[FILTER] items-container: {(_itemsContainer != null ? "Found" : "NOT FOUND")}");
}
```

#### Change 2: SetupUI - Added Debug Logging
```csharp
private void SetupUI()
{
    Debug.Log("[FILTER] SetupUI() - Registering event callbacks...");
    
    // Setup type filter
    if (_typeFilter != null)
    {
        _typeFilter.RegisterValueChangedCallback(evt => OnTypeFilterChanged(evt.newValue));
        Debug.Log("[FILTER] ✅ Type filter callback registered");
    }
    else
    {
        Debug.LogWarning("[FILTER] ❌ Type filter not found in UI - cannot register callback!");
    }
    
    // ... similar for other filters
    
    Debug.Log("[FILTER] SetupUI() completed");
}
```

---

## Understanding UI Toolkit Coordinate Systems

### 1. Element Local Coordinates
- **Origin**: Top-left of the element itself
- **Usage**: `evt.position`, `evt.localPosition`
- **Problem**: Changes depending on where element is in hierarchy

### 2. Panel Coordinates
- **Origin**: Top-left of the UIDocument panel
- **Usage**: Result of `element.LocalToWorld(localPos)`
- **Conversion**: `_rootElement.LocalToWorld(evt.localPosition)`

### 3. Screen Coordinates
- **Origin**: Bottom-left of the screen (Unity standard)
- **Usage**: Unity camera and input systems
- **Conversion**: `new Vector2(panelPos.x, Screen.height - panelPos.y)`

### Coordinate Flow Diagram
```
Mouse Click/Drag
    ↓
Element Local Coords (evt.localPosition)
    ↓
LocalToWorld() → Panel Coords
    ↓
PanelToScreenPosition() → Screen Coords (Y-flipped)
    ↓
Camera.ScreenPointToRay() → World Ray
    ↓
Raycast → World Position
```

---

## Expected Console Output After Fix

### When UI Initializes:
```
[FILTER] QueryUIElements() - Searching for UI elements...
[FILTER] palette-container: Found
[FILTER] type-filter: Found (or NOT FOUND if missing)
[FILTER] category-filter: Found (or NOT FOUND if missing)
[FILTER] search-field: Found (or NOT FOUND if missing)
[FILTER] refresh-button: Found (or NOT FOUND if missing)
[FILTER] items-scroll-view: Found
[FILTER] items-container: Found

[FILTER] SetupUI() - Registering event callbacks...
[FILTER] ✅ Type filter callback registered (or ❌ if not found)
[FILTER] ✅ Category filter callback registered (or ❌ if not found)
[FILTER] ✅ Search field callback registered (or ❌ if not found)
[FILTER] ✅ Refresh button callback registered (or ❌ if not found)
[FILTER] SetupUI() completed
```

### When Dragging:
```
[DRAG] Drag started for 'Base Female' at panel position (523.45, 347.89)
[DRAG] ========== DRAG ENDED ==========
[DRAG] Object: 'Base Female' (ID: base_female)
[DRAG] Panel position: (645.12, 412.34)
[DRAG] Coordinate conversion: Panel(645.12, 412.34) -> Screen(645.12, 667.66) [Screen height: 1080]
[DRAG] Screen position (after conversion): (645.12, 667.66)
[DRAG] Using camera: Main Camera
[DRAG] Viewport position: (0.45, 0.62) (valid range: 0-1)
[DRAG] Is over scene viewport? True
[DRAG] SandboxBuilder found: SandboxSceneBuilder
[DRAG] Ray origin: (0, 5, -10), direction: (0.23, -0.45, 0.86)
[DRAG] ✅ Raycast hit ground at distance 11.2, world position: (2.3, 0, 4.1)
[DRAG] World position: (2.3, 0, 4.1)
[DRAG] Attempting to place object...
[DRAG] ✅ Successfully called PlaceObject
[DRAG] ========== END DRAG ==========
```

---

## Troubleshooting

### If filters still show "NOT FOUND":

This means the UXML template doesn't have elements with the expected names. Two solutions:

#### Solution A: Fix UXML Template
Ensure your UXML has elements with these exact names:
```xml
<ui:DropdownField name="type-filter" label="Type" />
<ui:DropdownField name="category-filter" label="Category" />
<ui:TextField name="search-field" label="Search" />
<ui:Button name="refresh-button" text="Refresh" />
<ui:ScrollView name="items-scroll-view">
    <ui:VisualElement name="items-container" />
</ui:ScrollView>
```

#### Solution B: Use Code-Generated UI
If `_paletteTemplate` is null, the palette will create UI from code in `CreatePaletteFromCode()`. This guarantees all elements exist with correct names.

To force code generation:
1. Set `Palette Template` to `None` in Inspector
2. Palette will auto-generate UI with all required elements

---

### If drag still shows NaN:

Check these in order:

1. **Is LocalToWorld being called?**
   - Should see: `[DRAG] Panel position: (X, Y)` with real numbers
   - If still NaN, element might not be properly added to hierarchy

2. **Is element in visual tree?**
   ```csharp
   // In ObjectPaletteItemUIToolkit, add:
   Debug.Log($"Element in hierarchy: {_rootElement.panel != null}");
   ```

3. **Is palette properly integrated?**
   - Check `SandboxBuilderUIToolkit` attached palette to `palette-container`
   - Palette's UIDocument should be disabled after integration

---

## Testing Checklist

### ✅ Filters Working:
1. Open palette UI
2. Change Type dropdown → Should see `[FILTER] Type filter changed to: 'XXX'`
3. Change Category dropdown → Should see `[FILTER] Category filter changed to: 'XXX'`
4. Type in Search field → Should see `[FILTER] Search text changed to: 'XXX'`
5. Click Refresh → Should see `[FILTER] RefreshPalette() called`
6. Items should update to show filtered results

### ✅ Drag Working:
1. Click and drag a palette item
2. Should see `[DRAG] Drag started` with **real coordinates** (not NaN)
3. Release over scene
4. Should see `[DRAG] Panel position: (X, Y)` with **real numbers**
5. Should see `[DRAG] Screen position (after conversion): (X, Y)` with **real numbers**
6. Object should appear in scene at cursor position

---

## Technical Details

### LocalToWorld Method
```csharp
// Converts element-relative position to panel-relative position
Vector2 panelPos = _rootElement.LocalToWorld(evt.localPosition);

// Under the hood:
// 1. Gets element's position in panel
// 2. Adds local offset
// 3. Accounts for any parent transforms
// 4. Returns absolute position in panel coordinate space
```

### Why evt.position Failed
- `evt.position` is relative to the element that **received** the event
- When palette is nested in another UIDocument hierarchy, this creates coordinate space confusion
- `evt.localPosition` + `LocalToWorld()` always gives correct panel-space coordinates

### Why NaN Occurred
- `PanelToScreenPosition()` checks if `_rootElement.panel != null`
- If using `evt.position` from wrong coordinate space, the conversion fails
- Result: `new Vector2(NaN, NaN)`

---

## Summary

| Issue | Symptom | Cause | Fix |
|-------|---------|-------|-----|
| Filters not working | Type/Category/Search don't update, always show defaults | UI elements not found in hierarchy | Added debug logs to identify missing elements |
| Drag position NaN | `Panel position: (NaN, NaN)` | Using `evt.position` (wrong coordinate space) | Use `LocalToWorld(evt.localPosition)` for panel coords |
| Coordinate conversion fails | Screen position calculation fails | Trying to convert invalid coordinates | Fix input coordinates first (see above) |

**Status**: ✅ Both issues should now be fixed. Test in Unity and check console logs to verify elements are found and coordinates are valid.

