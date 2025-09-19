# CameraTrack Usage Examples

The CameraTrack allows you to animate camera position, rotation, and field of view over time.

## Basic Usage

```csharp
// Get the camera track from timeline
var cameraTrack = timeline.GetTrack<CameraTrack>("main_camera");

// Add a position movement clip
var posClip = cameraTrack.AddPositionClip(
    start: 0f, 
    duration: 3f,
    startPos: new Vector3(0, 2, -5),
    endPos: new Vector3(10, 2, -5)
);

// Add a rotation clip
var rotClip = cameraTrack.AddRotationClip(
    start: 1f,
    duration: 2f, 
    startRot: Quaternion.Euler(0, 0, 0),
    endRot: Quaternion.Euler(0, 45, 0)
);

// Add a field of view change
var fovClip = cameraTrack.AddFieldOfViewClip(
    start: 2f,
    duration: 1f,
    startFOV: 60f,
    endFOV: 30f  // Zoom in
);
```

## Animation Curves

You can control the animation curve for smooth transitions:

```csharp
var clip = cameraTrack.AddPositionClip(0f, 2f, startPos, endPos);
clip.animationCurve = CameraAnimationCurve.EaseInOut; // Smooth start and end
```

Available curves:
- `Linear` - Constant speed
- `EaseIn` - Slow start, fast end  
- `EaseOut` - Fast start, slow end
- `EaseInOut` - Smooth start and end
- `Custom` - Use custom AnimationCurve

## Combined Clips

You can create a single clip that animates multiple properties:

```csharp
var clip = new CameraClip
{
    Id = Guid.NewGuid().ToString(),
    Start = 0f,
    Duration = 5f,
    
    // Position
    hasPosition = true,
    startPosition = new Vector3(0, 2, -5),
    endPosition = new Vector3(10, 5, -2),
    
    // Rotation  
    hasRotation = true,
    startRotation = Quaternion.identity,
    endRotation = Quaternion.Euler(15, 45, 0),
    
    // Field of view
    hasFieldOfView = true,
    startFieldOfView = 60f,
    endFieldOfView = 45f,
    
    // Animation
    animationCurve = CameraAnimationCurve.EaseInOut,
    
    // Blending
    fadeIn = 0.5f,
    fadeOut = 0.5f
};

cameraTrack.clips.Add(clip);
```

## Serialization

CameraTrack clips are automatically serialized to JSON with the following format:

```json
{
  "id": "camera_clip_1",
  "start": 0.0,
  "duration": 3.0,
  "payload": {
    "hasPosition": true,
    "startPosition": "0,2,-5",
    "endPosition": "10,2,-5",
    "hasRotation": false,
    "hasFieldOfView": false,
    "animationCurve": "Linear",
    "fadeIn": 0.0,
    "fadeOut": 0.0
  }
}
```

## Track Binding

Bind the camera track to a Camera component:

```csharp
// In timeline setup
var timeline = GetComponent<MiniTimelineDirector>();
timeline.SetBinding("main_camera", mainCamera);

// Or bind to GameObject with Camera component
timeline.SetBinding("main_camera", cameraGameObject);
```

The track will automatically find the Camera component and control its transform and properties.