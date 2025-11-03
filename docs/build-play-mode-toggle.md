# Build/Play Mode Toggle Feature

## Overview
Added a sandbox mode toggle system that allows switching between **Build Mode** (full editing capabilities) and **Play Mode** (performance-optimized read-only preview). This improves performance during testing and previewing by disabling unnecessary input processing.

## Implementation Summary

### 1. New Enum: `SandboxMode`
**File:** `Assets/Scripts/SceneSandbox/Core/SandboxMode.cs`

```csharp
public enum SandboxMode
{
    Build,  // Build mode - can place, select, and edit objects
    Play    // Play mode - read-only preview, optimized performance
}
```

### 2. Core Changes

#### SceneSandboxBuilder.cs

**New Components:**
- `_toggleModeActionRef` - Input action reference for mode toggle
- `_toggleModeAction` - Runtime input action instance
- `_currentMode` - Current sandbox mode state (defaults to Build)

**New Properties:**
- `CurrentMode` - Get current sandbox mode
- `IsInBuildMode` - Quick check if in Build mode

**New Events:**
- `OnModeChanged` - Invoked when mode changes (passes new SandboxMode)

**New Methods:**
- `ToggleMode()` - Toggle between Build and Play modes
- `SetMode(SandboxMode mode)` - Set specific mode with proper state management
- `EnableBuildInputActions()` - Enable all build-related input actions
- `DisableBuildInputActions()` - Disable all build-related input actions

**Updated Methods:**
- `Update()` - Early return if in Play Mode (performance optimization)
- `InitializeInputActions()` - Initialize mode toggle action (always active)
- `EnableInputActions()` - Only enable build actions if in Build Mode
- `DisableInputActions()` - Properly handle mode toggle action

#### SceneSandboxBuilderEditor.cs

**New SerializedProperty:**
- `_toggleModeActionRef` - For Inspector field

**New Visual Indicators:**
- Mode status display in header (BUILD MODE in green / PLAY MODE in cyan)
- Help box showing "Play Mode: Input actions disabled, read-only preview"

**Updated UI:**
- Added "Mode Control" section in Input Settings
- Tooltip: "Toggle between Build Mode (editing enabled) and Play Mode (read-only, performance optimized)"

## Mode Behavior

### Build Mode (Default)
- **Input Actions:** All enabled
- **Functionality:** Full editing capabilities
  - Place objects
  - Select objects
  - Move/Rotate/Scale objects
  - Delete objects
  - Transform mode hotkeys
  - Mouse/pointer input
  - Scroll input
- **Performance:** Normal (all Update() logic runs)
- **Visual:** Green "BUILD MODE" label in Inspector

### Play Mode
- **Input Actions:** All build actions disabled
- **Functionality:** Read-only preview
  - Cannot place objects
  - Cannot select objects
  - Cannot edit objects
  - Input processing skipped
  - Raycast input handling skipped
- **Performance:** Optimized (Update() exits early)
- **Visual:** Cyan "PLAY MODE" label in Inspector
- **Auto-Actions on Enter:**
  - Cancels any active placement
  - Exits all transform modes
  - Disables all build input actions

### Mode Toggle Action
- **Always Active:** The toggle mode input action remains enabled in both modes
- **Purpose:** Allows switching back to Build mode from Play mode

## Usage Instructions

### Setup (Unity Inspector)

1. Select the `SceneSandboxBuilder` GameObject
2. In the Inspector, find **Mode Control** section (under Input Action References)
3. Assign an Input Action to **"Toggle Build/Play Mode"**
4. Recommended key binding: **P** (for Play) or **Tab**

### Runtime Usage

#### Toggle Between Modes:
1. Press the assigned toggle key (e.g., P)
2. Mode switches: Build ↔ Play
3. Console logs: `[MODE] Sandbox mode changed: Build → Play`
4. Inspector updates to show current mode

#### Workflow Examples:

**Testing Performance:**
```
1. Build your scene in Build Mode
2. Press P to enter Play Mode
3. Test scene without input overhead
4. Press P to return to Build Mode for edits
```

**Preview Scene:**
```
1. Arrange objects in Build Mode
2. Enter Play Mode to preview without accidentally moving objects
3. Return to Build Mode to continue editing
```

## Performance Benefits

### Update() Method Optimization
```csharp
private void Update()
{
    // Skip all build-related input handling in Play Mode
    if (_currentMode == SandboxMode.Play)
    {
        return;  // Early exit = zero overhead
    }

    // Build mode logic...
    HandleRaycastInput();
    // ... pointer tracking, scroll, placement updates ...
}
```

**Performance Impact:**
- **Build Mode:** Full functionality, normal performance
- **Play Mode:** Zero input processing overhead
  - No raycast checks
  - No pointer tracking
  - No scroll handling
  - No placement updates
  - Input actions disabled at system level

### Input Action Management

**Build Actions (Disabled in Play Mode):**
- Exit all modes
- Move/Rotate/Scale hotkeys
- Transform increase/decrease
- Toggle axis
- Pointer position
- Left/Right click
- Mouse scroll
- Cancel placement

**Always Active Actions:**
- Toggle mode (so you can return to Build mode)

## Code Architecture

### State Management
```csharp
// Mode state
private SandboxMode _currentMode = SandboxMode.Build;

// Public properties
public SandboxMode CurrentMode => _currentMode;
public bool IsInBuildMode => _currentMode == SandboxMode.Build;

// Event notification
public System.Action<SandboxMode> OnModeChanged;
```

### Mode Transition Flow
```
User Presses Toggle Key
    ↓
OnToggleModePerformed() callback
    ↓
ToggleMode()
    ↓
SetMode(newMode)
    ↓
[If entering Play Mode]
- Cancel active placement
- Exit transform modes
- Disable build input actions
- Invoke OnModeChanged event
    ↓
[If entering Build Mode]
- Enable build input actions
- Invoke OnModeChanged event
```

## Integration with Existing Systems

### Compatible with:
- ✅ Transform modes (Position/Rotation/Scale)
- ✅ Axis toggle system
- ✅ Placement system
- ✅ Selection system
- ✅ Project/Scene management
- ✅ Timeline preview
- ✅ Gizmo rendering (still visible in Play Mode)

### Event Integration:
```csharp
// Subscribe to mode changes
sceneSandboxBuilder.OnModeChanged += (mode) =>
{
    if (mode == SandboxMode.Play)
    {
        // Custom Play Mode logic
        Debug.Log("Entered Play Mode");
    }
    else
    {
        // Custom Build Mode logic
        Debug.Log("Entered Build Mode");
    }
};
```

## Console Output Examples

### Entering Play Mode:
```
[MODE] Sandbox mode changed: Build → Play
[MODE] Entering Play Mode - Disabling build input actions
```

### Entering Build Mode:
```
[MODE] Sandbox mode changed: Play → Build
[MODE] Entering Build Mode - Enabling build input actions
```

## Visual Feedback

### Inspector Display:

**Build Mode:**
```
┌─────────────────────────────────┐
│ Current Mode: [BUILD MODE]      │  ← Green background
└─────────────────────────────────┘
```

**Play Mode:**
```
┌─────────────────────────────────┐
│ Current Mode: [PLAY MODE]       │  ← Cyan background
├─────────────────────────────────┤
│ ℹ Play Mode: Input actions      │
│   disabled, read-only preview   │
└─────────────────────────────────┘
```

## Future Enhancements

### Potential Improvements:

1. **On-Screen Mode Indicator:**
   - Display mode in game view (UI overlay)
   - Show toggle key hint

2. **Play Mode Features:**
   - Camera fly-through navigation
   - Basic object inspection (read-only)
   - Performance metrics display

3. **Automatic Mode Switching:**
   - Auto-enter Play Mode when timeline plays
   - Auto-return to Build Mode when timeline stops

4. **Mode-Specific Settings:**
   - Different camera settings per mode
   - Mode-specific layer visibility
   - Custom gizmo visibility per mode

5. **Play Mode Input Actions:**
   - Add optional camera controls for Play Mode
   - Add screenshot/recording controls
   - Add measurement/annotation tools

6. **Performance Profiling:**
   - Track performance difference between modes
   - Display frame time savings in Play Mode

7. **Mode Presets:**
   - Save/load different mode configurations
   - Quick mode templates (VR preview, Mobile preview, etc.)

## Testing Checklist

- [ ] Assign Toggle Mode input action in Inspector
- [ ] Verify mode indicator shows in Inspector header
- [ ] Toggle to Play Mode - verify green → cyan indicator
- [ ] Verify console log shows mode change
- [ ] Try placing object in Play Mode - should be disabled
- [ ] Try selecting object in Play Mode - should be disabled
- [ ] Verify Update() early return (use profiler)
- [ ] Toggle back to Build Mode - verify cyan → green
- [ ] Verify all build actions work again
- [ ] Test mode toggle during active placement - should cancel
- [ ] Test mode toggle during transform mode - should exit
- [ ] Verify input actions properly disabled/enabled
- [ ] Check performance improvement in Play Mode (profiler)
- [ ] Test OnModeChanged event fires correctly
- [ ] Verify mode state persists correctly

## Performance Measurements

### Expected Improvements in Play Mode:
- **Update() overhead:** ~90% reduction (single early return check)
- **Input processing:** 100% reduction (all actions disabled)
- **Raycast overhead:** 100% reduction (HandleRaycastInput skipped)
- **Pointer tracking:** 100% reduction (skipped)

### Measurement Example:
```csharp
// Use Unity Profiler to measure:
// Build Mode: SceneSandboxBuilder.Update() = X ms
// Play Mode: SceneSandboxBuilder.Update() = ~0.01 ms (just the mode check)
```

## Related Files

### New Files:
- `Assets/Scripts/SceneSandbox/Core/SandboxMode.cs`

### Modified Files:
- `Assets/Scripts/SceneSandbox/Core/SceneSandboxBuilder.cs`
- `Assets/Scripts/SceneSandbox/Editor/SceneSandboxBuilderEditor.cs`

### Related Systems:
- Input Action system
- Transform control system
- Placement system
- Selection system
- Timeline integration

## API Reference

### Public Methods:
```csharp
// Toggle between modes
public void ToggleMode()

// Set specific mode
public void SetMode(SandboxMode mode)
```

### Public Properties:
```csharp
// Get current mode
public SandboxMode CurrentMode { get; }

// Quick check for Build mode
public bool IsInBuildMode { get; }
```

### Events:
```csharp
// Invoked when mode changes
public System.Action<SandboxMode> OnModeChanged;
```

### Usage Example:
```csharp
var builder = GetComponent<SceneSandboxBuilder>();

// Subscribe to mode changes
builder.OnModeChanged += (mode) =>
{
    Debug.Log($"Mode changed to: {mode}");
};

// Toggle mode
builder.ToggleMode();

// Set specific mode
builder.SetMode(SandboxMode.Play);

// Check current mode
if (builder.IsInBuildMode)
{
    // Build mode logic
}
```
