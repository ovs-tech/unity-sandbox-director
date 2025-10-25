# Drag and Drop Implementation Summary

## Status: ✅ Implementation Complete - Ready for Testing

## What Was Fixed

### 1. Coordinate System Conversion ✅
**Problem**: UI Toolkit panel coordinates have origin at **top-left**, but Unity screen coordinates have origin at **bottom-left**.

**Solution**: Added `PanelToScreenPosition()` method:
```csharp
private Vector2 PanelToScreenPosition(Vector2 panelPosition)
{
    float screenHeight = Screen.height;
    return new Vector2(panelPosition.x, screenHeight - panelPosition.y);
}
```

**File**: `Assets/Scripts/SceneSandbox/UI/ObjectPaletteUIToolkit.cs` (line ~586)

### 2. Comprehensive Debug Logging ✅
Added debug logs at every critical point:
- Drag start: logs object name and position
- Drag end: logs panel pos, screen pos, viewport validation
- Coordinate conversion: logs all transformations
- Placement: logs success or detailed errors

**Files Modified**:
- `ObjectPaletteUIToolkit.cs`: OnItemDragStarted, OnItemDragEnded, IsScreenPositionOverScene, ScreenToWorldPosition

### 3. Error Handling ✅
Added comprehensive try-catch blocks and null checks:
```csharp
try
{
    if (_sandboxBuilder != null)
    {
        _sandboxBuilder.PlaceObject(item.ObjectData.id, worldPosition);
        Debug.Log($"Successfully called PlaceObject");
    }
    else
    {
        Debug.LogError("SandboxBuilder is null, cannot place object!");
    }
}
catch (System.Exception ex)
{
    Debug.LogError($"Exception placing object: {ex.Message}\n{ex.StackTrace}");
}
```

### 4. Debug Utility Created ✅
Created `DragDropDebugger.cs` - attach to any GameObject to:
- See startup diagnostic in Console
- Press **F1** to show real-time diagnostic overlay
- Verify all references are assigned
- Test coordinate conversions in real-time
- Check raycast functionality

**File**: `Assets/Scripts/SceneSandbox/UI/DragDropDebugger.cs`

## How to Test

### Step 1: Verify Setup
1. Open your scene in Unity
2. Check **Console** for startup diagnostic:
   ```
   === DRAG & DROP STARTUP DIAGNOSTIC ===
   ✓ SceneSandboxBuilder found: ...
   ✓ SandboxBuilderUIToolkit found: ...
   ✓ Main Camera found: ...
   ```
3. If any item shows ✗, fix that first!

### Step 2: Test Drag and Drop
1. Enter Play Mode
2. Open Object Palette UI
3. Click and drag an item to the scene
4. Watch the **Console** for logs:
   ```
   Drag started for 'ObjectName' at (x, y)
   Drag ended at panel pos (x, y), screen pos (x, y)
   Is over scene? True
   Attempting to place object 'ObjectName' at (x, y, z)
   Successfully called PlaceObject
   ```

### Step 3: Use Debug Overlay (Optional)
1. Add `DragDropDebugger` component to any GameObject
2. Enter Play Mode
3. Press **F1** to show diagnostic overlay
4. Move mouse around to see coordinate conversions
5. Drag items and watch values update

## Expected Behavior

### ✅ Success Case
- Drag item from palette
- See blue preview follow mouse
- Drop on scene view
- Object appears at cursor position in 3D space
- Console shows all steps succeeded

### ❌ Failure Cases and Solutions

#### Case 1: "SandboxBuilder is null"
**Solution**: In Inspector, assign `SceneSandboxBuilder` to `SandboxBuilderUIToolkit` component.

#### Case 2: "Drag started" but no "Drag ended"
**Solution**: Make sure you release the mouse button over the Scene view.

#### Case 3: "Raycast missed"
**Solution**: Add a ground plane (plane GameObject at Y=0) with a collider.

#### Case 4: Object appears at wrong position
**Solution**: Already fixed with coordinate conversion. If still wrong, check camera position/rotation.

#### Case 5: No console output at all
**Solution**: Check that ObjectPalette is properly initialized. Run `AssignPaletteResources()`.

## Files Modified

### Core Implementation
1. **ObjectPaletteUIToolkit.cs**
   - Added: `PanelToScreenPosition()` method
   - Modified: `OnItemDragEnded()` - extensive logging and error handling
   - Modified: `OnItemDragStarted()` - added logging
   - Modified: `IsScreenPositionOverScene()` - added logging
   - Modified: `ScreenToWorldPosition()` - added logging

2. **ObjectPaletteItemUIToolkit.cs** (already correct from previous fixes)
   - Drag detection with 5px threshold
   - Event propagation stops
   - PickingMode.Ignore on preview elements

3. **SandboxBuilderUIToolkit.cs** (already correct)
   - Palette integration via coroutine
   - Resource assignment via reflection
   - Event subscriptions

### Documentation
4. **drag-drop-troubleshooting.md** (NEW)
   - Comprehensive troubleshooting guide
   - Step-by-step diagnosis
   - Common scenarios and fixes
   - Configuration checklists

### Debug Utilities
5. **DragDropDebugger.cs** (NEW)
   - Runtime diagnostic tool
   - F1 overlay with live info
   - Reference verification
   - Coordinate testing

## Configuration Checklist

Before testing, verify:

### In Scene Hierarchy
- [ ] **SceneSandboxBuilder** GameObject exists
- [ ] SceneSandboxBuilder component attached
- [ ] Camera tagged as "MainCamera" OR Camera.main exists
- [ ] Ground plane with collider (for raycast)

### In Inspector (SandboxBuilderUIToolkit)
- [ ] UI Document assigned
- [ ] Main UI Template assigned
- [ ] **Object Library** assigned ← CRITICAL
- [ ] Palette Template assigned (optional - will use Resources if null)
- [ ] Item Template assigned (optional - will use Resources if null)

### In Inspector (SceneSandboxBuilder)
- [ ] **Object Library** assigned ← CRITICAL
- [ ] Component is enabled

### In Project (Object Library Asset)
- [ ] Library asset exists (Create > SceneBuilder > Object Library)
- [ ] Objects added to library
- [ ] Each object has:
  - [ ] Valid ID
  - [ ] Display name
  - [ ] **Prefab reference** ← CRITICAL
  - [ ] Category

## Integration Points

### Event Flow
```
User clicks item
  ↓
ObjectPaletteItemUIToolkit.OnPointerDown
  ↓
User moves mouse > 5px
  ↓
ObjectPaletteItemUIToolkit.OnPointerMove
  ↓ (fires event)
ObjectPaletteUIToolkit.OnItemDragStarted
  ↓
[Preview follows mouse]
  ↓
User releases mouse
  ↓
ObjectPaletteItemUIToolkit.OnPointerUp
  ↓ (fires event)
ObjectPaletteUIToolkit.OnItemDragEnded
  ↓
PanelToScreenPosition(panel coords)
  ↓ returns screen coords
IsScreenPositionOverScene(screen coords)
  ↓ returns true if in scene view
ScreenToWorldPosition(screen coords)
  ↓ returns world Vector3
SceneSandboxBuilder.PlaceObject(id, position)
  ↓
Object instantiated in scene!
```

### Coordinate Transformation Pipeline
```
Mouse Click
  ↓
UI Toolkit Panel Space: (0,0) = top-left
  panelPosition.x, panelPosition.y
  ↓
PanelToScreenPosition()
  ↓
Unity Screen Space: (0,0) = bottom-left
  screenPosition.x, Screen.height - screenPosition.y
  ↓
Camera.ScreenToViewportPoint()
  ↓
Viewport Space: (0-1, 0-1)
  ↓
Camera.ScreenPointToRay()
  ↓
World Ray
  ↓
Physics.Raycast(ray)
  ↓
World Position: (x, y, z)
```

## Known Limitations

1. **Requires Ground Plane**: Must have collider for raycast to hit
2. **Single Camera**: Uses `Camera.main` only
3. **Y=0 Assumption**: Places objects at raycast hit point (typically ground)
4. **No Undo**: Object placement doesn't integrate with Unity Undo system yet
5. **No Collision Check**: Doesn't prevent overlapping objects

## Future Enhancements

- [ ] Grid snapping
- [ ] Rotation controls during drag
- [ ] Preview object in 3D (instead of 2D icon)
- [ ] Collision detection before placement
- [ ] Undo/Redo support
- [ ] Multi-object selection and drag
- [ ] Drag from scene to delete
- [ ] Custom placement rules per object type

## Architecture Notes

### Why UI Toolkit Instead of UGUI?
- Modern Unity UI system (2021+)
- Better performance for complex UIs
- Easier to style with USS (like CSS)
- Better scaling and layout
- More maintainable for large projects

### Why Reflection for Resource Assignment?
- Keeps `ObjectPaletteUIToolkit` decoupled from `SandboxBuilderUIToolkit`
- Allows palette to be used standalone
- Resources can be assigned either via:
  1. Reflection from SandboxBuilder (centralized)
  2. Direct assignment in Inspector (manual)
  3. Resources.Load fallback (automatic)

### Why Custom Drag Instead of Built-in?
- UI Toolkit's built-in drag-and-drop is designed for UI-to-UI
- We need UI-to-World (2D to 3D) which requires custom handling
- Custom implementation gives full control over:
  - Drag threshold
  - Preview appearance
  - Coordinate conversion
  - Placement logic

## Testing Scenarios

### Scenario 1: Basic Placement
1. Drag "Chair" to center of screen
2. Should appear at camera forward position on ground

### Scenario 2: Edge Cases
1. Drag item off screen → Should not place
2. Drag item over UI panel → Should not place (blocked by isOverScene check)
3. Drag item over void (no ground) → Should not place (raycast miss)

### Scenario 3: Multiple Objects
1. Drag 5 different objects
2. All should appear at different positions
3. No objects should overlap (future: collision check)

### Scenario 4: Fast Dragging
1. Click and immediately release → Should not drag (< 5px)
2. Click, move 10px, release fast → Should drag and place

## Debugging Tools Summary

### Built-in (Console Logs)
- Always enabled
- Shows detailed trace of every operation
- Filter by "ObjectPaletteUIToolkit" or "DragDrop"

### DragDropDebugger (F1 Overlay)
- Optional component
- Real-time status display
- No need to check console repeatedly
- Shows live mouse/viewport coordinates

### Visual Debugging
- Blue preview follows mouse during drag
- Can add `Debug.DrawRay()` in ScreenToWorldPosition to see raycast

## Support

If drag-and-drop still doesn't work:

1. **Check troubleshooting doc**: `docs/drag-drop-troubleshooting.md`
2. **Run diagnostic**: Add `DragDropDebugger`, press F1
3. **Check console**: Look for error messages
4. **Verify setup**: Use configuration checklist above
5. **Test minimal scene**: See troubleshooting doc "Minimal Setup"

## Success Criteria

✅ All of these should be true:
- [x] Code compiles without errors
- [ ] Startup diagnostic shows all ✓ (to be tested)
- [ ] Drag preview appears and follows mouse (to be tested)
- [ ] Console shows complete event flow (to be tested)
- [ ] Object appears in scene at cursor position (to be tested)
- [ ] Multiple objects can be placed (to be tested)
- [ ] Objects respect scene boundaries (to be tested)

## Commit Message Suggestion

```
feat: Implement drag-and-drop from Object Palette to Scene

- Added coordinate conversion (panel → screen → world)
- Fixed Y-axis inversion between UI Toolkit and Unity screen space
- Added comprehensive debug logging throughout drag pipeline
- Created DragDropDebugger utility for runtime diagnostics
- Added extensive error handling and null checks
- Documented troubleshooting steps and common issues

Files modified:
- ObjectPaletteUIToolkit.cs: Added PanelToScreenPosition() and debug logs
- Created DragDropDebugger.cs: Runtime diagnostic tool
- Created drag-drop-troubleshooting.md: Comprehensive guide

Closes: #[issue number if applicable]
```

## Next Steps

1. **Test in Unity Editor** - Enter play mode and try dragging items
2. **Check Console Output** - Verify all debug logs appear as expected
3. **Report Results** - If it works: ✅ If not: Share console output for further diagnosis
4. **Cleanup** - Once working, can optionally remove debug logs (or keep behind #if DEBUG)
5. **Polish** - Add grid snapping, rotation controls, etc.

---

**Implementation Date**: [Today's Date]
**Unity Version**: [Your Unity Version]
**Status**: Ready for Testing 🚀
