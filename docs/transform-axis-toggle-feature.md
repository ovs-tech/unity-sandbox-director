# Transform Axis Toggle Feature

## Overview
Added a new input action system that allows users to toggle which axis (X, Y, Z, or All) to modify when in Rotation or Scale transform modes.

## Implementation Summary

### 1. New Enum: `TransformAxis`
**File:** `Assets/Scripts/SceneSandbox/Core/TransformAxis.cs`

```csharp
public enum TransformAxis
{
    All,    // Modify all axes (default)
    X,      // Modify X axis only
    Y,      // Modify Y axis only
    Z       // Modify Z axis only
}
```

### 2. Updated Files

#### SceneSandboxBuilder.cs
- **New Input Action Reference:** `_transformModeToggleAxisActionRef`
- **New State Variable:** `_currentTransformAxis` (defaults to `TransformAxis.All`)
- **New Method:** `ToggleTransformAxis()` - Cycles through: All → X → Y → Z → All
- **New Method:** `UpdateActiveTransformItemsAxis()` - Updates all active transform items with current axis
- **Updated Method:** `SetTransformMode()` - Resets axis to All when entering Position or None mode
- **New Callback:** `OnTransformModeToggleAxisPerformed()` - Handles input action callback

**Key Behavior:**
- Axis toggle only works in Rotation and Scale modes
- Automatically resets to "All" when switching to Position mode or exiting transform mode
- Updates all selected/active transform items when axis changes

#### TransformableItem.cs
- **New State Variable:** `_currentTransformAxis` (serialized, defaults to `TransformAxis.All`)
- **New Property:** `CurrentTransformAxis` (read-only)
- **New Method:** `SetTransformAxis(TransformAxis axis)` - Sets the current transform axis
- **Updated Method:** `SetTransformModeType()` - Resets axis to All when entering Position/None mode
- **Updated Method:** `IncreaseTransformValue()` - Now respects axis selection for rotation and scale
- **Updated Method:** `DecreaseTransformValue()` - Now respects axis selection for rotation and scale

**Key Behavior:**
- Rotation mode with specific axis: Rotates only around that axis
- Rotation mode with All: Rotates around Y axis (default behavior)
- Scale mode with specific axis: Scales only that dimension
- Scale mode with All: Scales uniformly on all axes

#### SceneSandboxBuilderEditor.cs
- **New SerializedProperty:** `_transformModeToggleAxisActionRef`
- **Updated:** Property caching in `CacheSerializedProperties()`
- **Updated:** Input settings UI to display new "Toggle Axis" field with tooltip

## Usage Instructions

### Setup (Unity Inspector)
1. Select the `SceneSandboxBuilder` GameObject
2. In the Inspector, find **Input Action References** section
3. Assign an Input Action to the new **"Toggle Axis"** field
4. The tooltip explains: "Toggle between X, Y, Z, and All axes for Rotation/Scale modes"

### Recommended Input Binding
- Suggested key: **Tab** or **X** key
- Should be easily accessible during transform operations

### Runtime Usage
1. **Enter Rotation or Scale Mode:**
   - Press the rotation hotkey (default: R) or scale hotkey (default: S)
   
2. **Toggle Axis:**
   - Press the toggle axis key (e.g., Tab)
   - Cycles through: All → X → Y → Z → All
   - Console log shows current axis: `[AXIS] Transform axis changed to: X`

3. **Modify Transform:**
   - Use Increase/Decrease transform keys (default: +/-)
   - Transform only applies to the selected axis

4. **Visual Feedback:**
   - Console logs indicate axis changes
   - Future enhancement: Gizmo highlighting for selected axis

### Example Workflows

#### Rotate Only on Y Axis:
1. Press R (enter rotation mode)
2. Press Tab until axis is Y
3. Press +/- to rotate around Y axis only

#### Scale Only Width (X):
1. Press S (enter scale mode)
2. Press Tab until axis is X
3. Press +/- to scale X dimension only

#### Uniform Scale:
1. Press S (enter scale mode)
2. Press Tab until axis is All (or leave as default)
3. Press +/- to scale uniformly

## Technical Notes

### Axis Reset Behavior
- **Automatic reset to All when:**
  - Entering Position mode
  - Entering None mode (exit all modes)
  - Changing transform mode type

### Rotation Axis Mapping
- **X axis:** Roll (rotation around forward/back)
- **Y axis:** Yaw (rotation around up/down) - Default for "All"
- **Z axis:** Pitch (rotation around left/right)

### Scale Axis Mapping
- **X axis:** Width
- **Y axis:** Height
- **Z axis:** Depth
- **All:** Uniform scaling on all dimensions with clamping

## Future Enhancements

### Potential Improvements:
1. **Gizmo Visual Feedback:**
   - Highlight the selected axis in the gizmo (change color)
   - Show axis label near gizmo

2. **UI Indicator:**
   - Display current axis in on-screen UI
   - Show keyboard hint for toggle key

3. **Advanced Rotation:**
   - When "All" is selected, allow free rotation on all axes simultaneously
   - Add quaternion-based smooth rotation

4. **Multi-Axis Selection:**
   - Allow selecting multiple axes (e.g., X+Y, X+Z, Y+Z)
   - Cycle pattern: All → X → Y → Z → XY → XZ → YZ → All

5. **Per-Mode Axis Memory:**
   - Remember last used axis for rotation mode separately from scale mode
   - Restore axis when returning to that mode

## Code Architecture

### Command Pattern Ready
The implementation follows the project's Command Pattern architecture:
- All methods are self-contained and reversible
- State changes are clearly defined
- Easy to extend with undo/redo functionality

### DLC/Module Extension
The axis system can be extended through:
- Custom axis configurations (e.g., diagonal axes)
- Mode-specific axis behaviors
- Input remapping through DLC packages

## Testing Checklist

- [ ] Assign Toggle Axis input action in Inspector
- [ ] Enter Rotation mode and toggle through all axes
- [ ] Verify rotation only affects selected axis
- [ ] Enter Scale mode and toggle through all axes
- [ ] Verify scaling only affects selected axis
- [ ] Verify axis resets to All when changing to Position mode
- [ ] Test with increase/decrease transform inputs
- [ ] Verify console logs show axis changes
- [ ] Test with multiple selected objects
- [ ] Verify axis synchronization across all selected items

## Related Files
- `Assets/Scripts/SceneSandbox/Core/TransformAxis.cs` (new)
- `Assets/Scripts/SceneSandbox/Core/TransformModeType.cs` (reference)
- `Assets/Scripts/SceneSandbox/Core/SceneSandboxBuilder.cs` (updated)
- `Assets/Scripts/SceneSandbox/Core/TransformableItem.cs` (updated)
- `Assets/Scripts/SceneSandbox/Editor/SceneSandboxBuilderEditor.cs` (updated)
