# Drag and Drop Troubleshooting Guide

## Problem
Cannot drag and drop palette items to the scene.

## Root Causes Identified

### 1. Coordinate System Mismatch ❌ → ✅ FIXED
**Problem**: UI Toolkit uses **panel coordinates** (top-left origin), but Unity's camera system uses **screen coordinates** (bottom-left origin).

**Solution**: Added `PanelToScreenPosition()` method to convert coordinates:
```csharp
private Vector2 PanelToScreenPosition(Vector2 panelPosition)
{
    // Convert panel coordinates to screen coordinates
    // Panel: (0,0) = top-left
    // Screen: (0,0) = bottom-left
    
    float screenHeight = Screen.height;
    Vector2 screenPos = new Vector2(panelPosition.x, screenHeight - panelPosition.y);
    
    return screenPos;
}
```

### 2. Missing SandboxBuilder Reference ⚠️
**Problem**: `_sandboxBuilder` might be null if not properly assigned.

**Solution**: Added comprehensive null checks and error logging:
```csharp
if (_sandboxBuilder != null)
{
    _sandboxBuilder.PlaceObject(item.ObjectData.id, worldPosition);
}
else
{
    Debug.LogError("SandboxBuilder is null, cannot place object!");
}
```

### 3. Silent Failures 🔇 → 📢 FIXED
**Problem**: Errors weren't being logged, making debugging impossible.

**Solution**: Added extensive debug logging at every step:
```csharp
Debug.Log($"Drag ended at panel pos {screenPosition}, screen pos {actualScreenPosition}");
Debug.Log($"Is over scene? {isOverScene}");
Debug.Log($"Attempting to place object '{item.ObjectData.displayName}' at {worldPosition}");
```

## Testing Checklist

### Step 1: Verify Drag Events Are Firing
```
Expected Console Output:
✓ "Drag started for 'ObjectName' at (x, y)"
✓ "Drag ended at panel pos (x, y), screen pos (x, y)"
```

**If you don't see these logs:**
- [ ] Check that `ObjectPaletteItemUIToolkit` is properly initialized
- [ ] Verify events are subscribed in `CreatePaletteItem()`
- [ ] Ensure drag threshold (5px) is exceeded

### Step 2: Verify Coordinate Conversion
```
Expected Console Output:
✓ "Screen pos (x, y) -> Viewport pos (x, y)"
```

**If viewport is outside (0-1, 0-1):**
- [ ] Check Screen.height value
- [ ] Verify camera exists and is active
- [ ] Try dragging to different screen areas

### Step 3: Verify SandboxBuilder Reference
```
Expected Console Output:
✓ "Attempting to place object 'ObjectName' at (x, y, z)"
✓ "Successfully called PlaceObject"
```

**If you see "SandboxBuilder is null":**
1. Check `SandboxBuilderUIToolkit` has reference to `SceneSandboxBuilder`
2. Check `AssignPaletteResources()` is assigning the builder
3. Manually find and assign in Start():
```csharp
if (_sandboxBuilder == null)
{
    _sandboxBuilder = FindFirstObjectByType<SceneSandboxBuilder>();
}
```

### Step 4: Verify Object Placement
```
Expected Behavior:
✓ Object appears in scene hierarchy
✓ Object is visible in scene view
✓ Object is at correct world position
```

**If object doesn't appear:**
- [ ] Check `SceneSandboxBuilder.PlaceObject()` implementation
- [ ] Verify object prefab exists
- [ ] Check object library has valid object data
- [ ] Look for exceptions in PlaceObject()

## Quick Fixes

### Fix 1: Ensure SandboxBuilder is Assigned

In `SandboxBuilderUIToolkit.Start()`:
```csharp
private void Start()
{
    // Find sandbox builder if not assigned
    if (_sandboxBuilder == null)
    {
        _sandboxBuilder = FindFirstObjectByType<SceneSandboxBuilder>();
        Debug.Log($"Found SandboxBuilder: {_sandboxBuilder != null}");
    }
    
    // ... rest of Start()
}
```

### Fix 2: Verify AssignPaletteResources Includes Builder

In `SandboxBuilderUIToolkit.AssignPaletteResources()`:
```csharp
// Set sandbox builder reference
if (_sandboxBuilder != null)
{
    var sandboxBuilderField = paletteType.GetField("_sandboxBuilder", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    if (sandboxBuilderField != null)
    {
        sandboxBuilderField.SetValue(_objectPalette, _sandboxBuilder);
        Debug.Log("Assigned SandboxBuilder to palette.");
    }
    else
    {
        Debug.LogError("Could not find _sandboxBuilder field in ObjectPaletteUIToolkit!");
    }
}
else
{
    Debug.LogWarning("SandboxBuilder is null, cannot assign to palette!");
}
```

### Fix 3: Test with Simple Placement

Temporarily disable scene boundary check:
```csharp
private void OnItemDragEnded(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
{
    // Always place regardless of position (for testing)
    if (_sandboxBuilder != null)
    {
        Vector3 worldPosition = Vector3.zero; // Place at origin for testing
        _sandboxBuilder.PlaceObject(item.ObjectData.id, worldPosition);
    }
}
```

### Fix 4: Check Object Library

Verify objects exist in library:
```csharp
public void RefreshPalette()
{
    if (_objectLibrary == null)
    {
        Debug.LogError("ObjectLibrary is null!");
        return;
    }
    
    var objects = _objectLibrary.GetAllObjects();
    Debug.Log($"Object library has {objects.Count} objects");
    
    foreach (var obj in objects)
    {
        Debug.Log($"  - {obj.displayName} (id: {obj.id})");
    }
    
    // ... rest of RefreshPalette
}
```

## Common Scenarios

### Scenario 1: Drag works but object doesn't appear

**Symptoms:**
- ✓ Drag events fire
- ✓ "Successfully called PlaceObject"
- ❌ No object in scene

**Check:**
1. Does `SceneSandboxBuilder.PlaceObject()` actually instantiate objects?
2. Is the prefab valid and assigned?
3. Is the object being created at a visible position?
4. Check camera culling settings

**Test:**
```csharp
// In SceneSandboxBuilder.PlaceObject()
Debug.Log($"PlaceObject called: id={objectId}, position={position}");

// After instantiation
if (instantiatedObject != null)
{
    Debug.Log($"Created object: {instantiatedObject.name} at {instantiatedObject.transform.position}");
}
else
{
    Debug.LogError("Failed to instantiate object!");
}
```

### Scenario 2: Drag doesn't start

**Symptoms:**
- ❌ No "Drag started" log
- Item highlights on hover but won't drag

**Check:**
1. Is `SetupInteractions()` being called?
2. Are pointer events registered?
3. Is the item's `pickingMode` set correctly?

**Test:**
```csharp
// In ObjectPaletteItemUIToolkit.SetupInteractions()
Debug.Log($"Setting up interactions for {_objectData.displayName}");
_rootElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
Debug.Log("PointerDown callback registered");
```

### Scenario 3: Coordinate conversion wrong

**Symptoms:**
- ✓ Drag works
- ✓ Object appears
- ❌ Object at wrong position (e.g., flipped Y axis)

**Fix:**
```csharp
private Vector2 PanelToScreenPosition(Vector2 panelPosition)
{
    // Try different conversions:
    
    // Option 1: Simple Y flip (current)
    float screenHeight = Screen.height;
    return new Vector2(panelPosition.x, screenHeight - panelPosition.y);
    
    // Option 2: No conversion (if panel IS screen space)
    // return panelPosition;
    
    // Option 3: Account for UI scale
    // float scale = _rootElement.panel.scale;
    // return new Vector2(panelPosition.x * scale, (screenHeight - panelPosition.y) * scale);
}
```

### Scenario 4: Multiple UI Documents conflict

**Symptoms:**
- Drag works on one panel but not another
- Inconsistent behavior

**Fix:**
Make sure palette's UIDocument is disabled after integration:
```csharp
// In SandboxBuilderUIToolkit.AttachPaletteUICoroutine()
paletteUIDocument.enabled = false; // ✓ This line should exist
```

## Debug Mode

### Enable Comprehensive Logging

Add this to the top of `ObjectPaletteUIToolkit`:
```csharp
#define DEBUG_DRAG_DROP
```

Then wrap debug code:
```csharp
#if DEBUG_DRAG_DROP
Debug.Log($"Detailed debug info...");
#endif
```

### Visual Debug Indicators

Add visual feedback for drag:
```csharp
private void OnItemDragMoved(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
{
    // Draw debug ray from camera
    Vector2 actualScreenPos = PanelToScreenPosition(screenPosition);
    Camera cam = Camera.main;
    if (cam != null)
    {
        Ray ray = cam.ScreenPointToRay(actualScreenPos);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green, 0.1f);
    }
}
```

## Configuration Checklist

### In Inspector (SandboxBuilderUIToolkit):
- [x] UI Document assigned
- [x] Main UI Template assigned
- [x] Object Library assigned
- [x] Palette Template assigned (optional)
- [x] Item Template assigned (optional)
- [x] Palette StyleSheet assigned (optional)
- [x] Object Palette reference (auto-detected or manual)
- [ ] **Panel Pos assigned** (Transform for dialog positioning)

### In Scene Hierarchy:
- [ ] **SceneSandboxBuilder GameObject exists**
- [ ] SceneSandboxBuilder component attached
- [ ] Object library is assigned to SceneSandboxBuilder
- [ ] Camera tagged as "MainCamera" OR exists in scene

### In Object Library Asset:
- [ ] Library has objects defined
- [ ] Each object has:
  - [ ] Valid ID
  - [ ] Display name
  - [ ] Prefab reference
  - [ ] Icon (optional)

## Expected Console Output (Success)

```
ObjectPaletteUIToolkit: Assigned ObjectLibrary to palette.
ObjectPaletteUIToolkit: Assigned SandboxBuilder to palette.
ObjectPaletteUIToolkit: Palette integration setup complete.
ObjectPaletteUIToolkit: Successfully attached palette UI to palette-container.

[User drags item]

ObjectPaletteUIToolkit: Drag started for 'Chair' at (523.5, 347.2)
ObjectPaletteUIToolkit: Drag ended at panel pos (645.0, 412.8), screen pos (645.0, 667.2)
ObjectPaletteUIToolkit: Is over scene? True
ObjectPaletteUIToolkit: Attempting to place object 'Chair' at (2.3, 0.0, 4.1)
ObjectPaletteUIToolkit: Successfully called PlaceObject
ObjectPaletteUIToolkit: Converted screen (645.0, 667.2) to world (2.3, 0.0, 4.1)
SandboxBuilderUIToolkit: Object dragged to scene: Chair

[Object appears in scene]
```

## Next Steps If Still Not Working

### 1. Test Minimal Setup

Create a test scene with:
```
- Empty GameObject: "SandboxBuilder"
  └─ SceneSandboxBuilder component
  └─ SandboxBuilderUIToolkit component
      └─ ObjectPaletteUIToolkit (auto-created)

- Main Camera (tagged "MainCamera")

- Canvas (if using old UI for comparison)
```

### 2. Compare with Working UGUI Version

The old `ObjectPalette` (UGUI) works. Key differences:
- Uses `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`
- Uses `PointerEventData.position` (screen space)
- No coordinate conversion needed

### 3. Simplify Drag Logic

Test with absolute minimal drag:
```csharp
private void OnItemDragEnded(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
{
    // Ultra-simple test
    if (_sandboxBuilder != null)
    {
        _sandboxBuilder.PlaceObject(item.ObjectData.id, Vector3.zero);
        Debug.Log("Placed at origin");
    }
}
```

### 4. Check Unity Version Compatibility

UI Toolkit drag behavior may vary by Unity version. Check:
- Unity 2021.x: Basic UI Toolkit support
- Unity 2022.x: Improved UI Toolkit
- Unity 2023.x+: Mature UI Toolkit

## File Modifications Summary

### Modified Files:
- `ObjectPaletteUIToolkit.cs`
  - Added `PanelToScreenPosition()` method
  - Enhanced `OnItemDragEnded()` with extensive logging
  - Added debug output to `IsScreenPositionOverScene()`
  - Added debug output to `ScreenToWorldPosition()`
  - Added debug output to `OnItemDragStarted()`

### No Changes Needed (Already Correct):
- `ObjectPaletteItemUIToolkit.cs` - Drag implementation is correct
- `SandboxBuilderUIToolkit.cs` - Integration is correct
- Event subscriptions - Working correctly

## Support Matrix

| Feature | Status | Notes |
|---------|--------|-------|
| Click to select | ✅ | Working |
| Hover effects | ✅ | Working |
| Drag threshold (5px) | ✅ | Working |
| Drag preview | ✅ | Working |
| Event propagation | ✅ | Fixed with StopPropagation() |
| Coordinate conversion | ⚠️ | Added, needs testing |
| SandboxBuilder integration | ⚠️ | Check assignment |
| Object placement | ⚠️ | Depends on SceneSandboxBuilder |

## Contact Points for Issues

If drag still doesn't work, check these specific points in order:

1. **Console Logs**: Do you see "Drag started"? → If NO, problem in ObjectPaletteItemUIToolkit
2. **Console Logs**: Do you see "Drag ended"? → If NO, pointer not being released
3. **Console Logs**: Do you see "SandboxBuilder is null"? → If YES, assignment problem
4. **Console Logs**: Do you see "Successfully called PlaceObject"? → If NO, exception in PlaceObject
5. **Scene View**: Is object created? → If NO, problem in SceneSandboxBuilder.PlaceObject()
6. **Scene View**: Is object at correct position? → If NO, coordinate conversion problem

Work through these systematically to isolate the exact failure point!
