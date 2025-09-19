# Mini Timeline Demo - Updated Features

The MiniTimelineDemo has been enhanced to showcase the new CameraTrack functionality along with existing features.

## New Features Added

### CameraTrack Support
- **Camera Binding**: Automatically binds the main camera or a specified demo camera
- **Camera Controls**: Reset camera to original position with 'C' key or GUI button
- **Camera Animation**: Sample project now includes camera movement, rotation, and field of view changes

### Enhanced UI
- **Track Information**: Toggle to show active tracks and clip counts
- **Camera Reset**: Button and keyboard shortcut to restore original camera settings
- **Extended Timeline**: Sample project extended to 12 seconds to showcase camera animations

### Sample Project Enhancements
The sample project now includes:

1. **Animation Track** (0-3s): Character running animation
2. **Camera Track** (0-7s): 
   - Position movement (0-4s): Camera moves from (0,2,-5) to (5,3,-3)
   - Rotation change (2-5s): Camera rotates 15 degrees on X and Y axes
   - Field of view zoom (5-7s): Zooms from 60° to 30° FOV
3. **Morph Track** (1-3s): Smile expression blend
4. **Event Track** (2.5s): Timeline event trigger

## Setup Instructions

1. **Camera Setup**: 
   - Assign a Camera to the `demoCamera` field in the inspector, or
   - Leave empty to use Camera.main automatically

2. **Character Setup**: 
   - Assign a GameObject with Animator to `characterObject` field
   - Ensure character has addressable animations at "addr:Animations/Run"

3. **Controls**:
   - **Space**: Play/Pause
   - **S**: Stop
   - **R**: Restart
   - **C**: Reset camera to original position

## Camera Animation Examples

The demo showcases three types of camera animation:

### Position Animation (0-4 seconds)
```csharp
// Camera moves smoothly from start to end position
startPosition: (0, 2, -5)
endPosition: (5, 3, -3)
animationCurve: EaseInOut
```

### Rotation Animation (2-5 seconds)
```csharp
// Camera rotates to look at scene from different angle
startRotation: Quaternion.identity
endRotation: Quaternion.Euler(15, 15, 0)
animationCurve: Linear
```

### Field of View Animation (5-7 seconds)
```csharp
// Zoom effect by changing camera FOV
startFieldOfView: 60°
endFieldOfView: 30° (zoom in)
animationCurve: EaseInOut
```

## Debug Features

- **Track Information Panel**: Shows all loaded tracks with active/total clip counts
- **Real-time State**: Displays playback state, time, speed, and project info
- **Time Scrubber**: Interactive timeline scrubbing
- **Camera Reset**: Restore original camera transform and FOV

## File Operations

- **Save**: Saves current project to persistent data path
- **Load**: Loads project from persistent data path
- **Auto-load**: Automatically loads sample project on start (configurable)

The demo now provides a comprehensive showcase of the Mini Timeline system's capabilities, including the new camera animation features!