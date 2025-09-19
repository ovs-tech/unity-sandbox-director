# Mini Timeline System for Unity

A lightweight, mobile-optimized timeline system for creating in-game cinematic sequences in Unity. Built for sandbox 3D games where players can create their own "movie scenes" using drag-and-drop animations, camera movements, morphs, audio, and more.

## Features

- **Track-based Timeline**: Support for Animation, Morph, Camera, Audio, and Event tracks
- **Mobile Optimized**: Designed for 45-60 FPS performance on mid-range Android devices
- **Playables Integration**: Uses Unity's Playables API for smooth animation blending
- **JSON Serialization**: Lightweight, versioned project format for easy save/load
- **Addressables Support**: Efficient asset loading and memory management
- **Extensible Architecture**: Easy to add new track types and clip behaviors
- **Real-time Scrubbing**: Smooth timeline scrubbing for precise editing
- **Event System**: Trigger custom events at specific timeline points

## Quick Start

### 1. Setup Demo Scene

In the Unity Editor:
1. Go to **Mini Timeline > Create Demo Scene**
2. Press Play to see the system in action
3. Use these controls:
   - **Space**: Play/Pause
   - **S**: Stop
   - **R**: Restart
   - Use the GUI controls for scrubbing and speed adjustment

### 2. Basic Usage

```csharp
using MiniTimeline.Core;
using MiniTimeline.Serialization;

// Create a timeline director
var director = gameObject.AddComponent<MiniTimelineDirector>();

// Setup binding context
director.BindingContext.Bind("character", characterGameObject);

// Load a project
var project = SampleProjectCreator.CreateSampleProject();
director.SetProject(project);

// Control playback
director.Play();
director.Pause();
director.Seek(2.5f); // Jump to 2.5 seconds
```

### 3. Creating Custom Projects

```csharp
using MiniTimeline.Core;
using MiniTimeline.Tracks;

// Create a new project
var project = new MiniTimelineProject
{
    name = "My Timeline",
    length = 10f,
    frameRate = 30f
};

// Add an animation track
var animTrack = new TrackData
{
    id = "character_anim",
    type = MiniTimelineConstants.TRACK_ANIM,
    bindKey = "character",
    enabled = true
};

// Add an animation clip
var animClip = new ClipData
{
    id = "walk_clip",
    start = 0f,
    duration = 3f,
    payload = new Dictionary<string, object>
    {
        { "animationAsset", "addr:Animations/Walk" },
        { "speed", 1f },
        { "fadeIn", 0.2f }
    }
};

animTrack.clips.Add(animClip);
project.tracks.Add(animTrack);

// Save project
ProjectSerializer.SaveToFile(project, "my_timeline.json");
```

## Track Types

### Animation Track
- Plays `AnimationClip` assets on `Animator` components
- Supports crossfading, speed control, and looping
- Uses Unity Playables for smooth blending

```csharp
// Animation clip payload
{
    "animationAsset": "addr:Animations/Walk",
    "speed": 1.0,
    "wrapMode": "Loop",
    "fadeIn": 0.2,
    "fadeOut": 0.2,
    "weight": 1.0
}
```

### Morph Track
- Controls blendshapes on `SkinnedMeshRenderer` components  
- Supports additive and override blend modes
- Key-based or curve-based animation

```csharp
// Morph key clip payload
{
    "blendMode": "Additive",
    "weight": 1.0,
    "keys": [
        {
            "id": "Smile",
            "startValue": 0,
            "endValue": 80,
            "curve": { /* AnimationCurve data */ }
        }
    ]
}
```

### Event Track
- Triggers events when playhead crosses markers
- Supports different trigger edges (enter/exit/both)
- Useful for gameplay events, UI changes, etc.

```csharp
// Signal clip payload
{
    "eventId": "StartDialogue",
    "payload": "Hello world!",
    "edge": "OnEnter",
    "fireOnScrub": false
}
```

## Performance Guidelines

- **Target**: 45-60 FPS with 6-8 active tracks and ~20 clips
- **Memory**: Uses object pooling and caching to minimize GC allocation
- **Assets**: Use Addressables for efficient asset loading
- **Evaluation**: Tracks are sorted by order and evaluated efficiently

## Architecture

### Core Components

- **MiniTimelineDirector**: Main controller for playback and track coordination
- **IMiniTrack**: Interface for all track types with Bind/Prepare/Evaluate lifecycle
- **IMiniClip**: Interface for all clip types with time-based operations
- **BindingContext**: Maps string keys to Unity objects for track binding
- **MiniPlayableGraph**: Wrapper for Unity Playables API

### Data Flow

1. **Project Loading**: JSON → TrackData → Track instances
2. **Binding**: Track keys resolved to Unity objects via BindingContext
3. **Preparation**: Tracks load assets and initialize internal state
4. **Evaluation**: Director calls Evaluate() on all tracks each frame
5. **Cleanup**: Tracks dispose resources when project closes

## Extending the System

### Adding New Track Types

1. Implement `IMiniTrack` interface:

```csharp
public class MyCustomTrack : MiniTrackBase<MyCustomClip>
{
    public override int Order => 50; // Evaluation order
    
    protected override void OnPrepare()
    {
        // Initialize track resources
    }
    
    protected override void OnEvaluate(float time, bool scrub)
    {
        // Update track state at given time
    }
    
    protected override void OnCleanup()
    {
        // Dispose track resources
    }
}
```

2. Register with TrackFactory:

```csharp
TrackFactory.RegisterTrackType("MyCustomTrack", (data) => {
    var track = new MyCustomTrack();
    // Configure track from data
    return track;
});
```

### Custom Clip Types

```csharp
[Serializable]
public class MyCustomClip : MiniClipBase
{
    public string customProperty;
    
    public void DoSomething(float time)
    {
        if (Contains(time))
        {
            float normalizedTime = GetNormalizedTime(time);
            // Custom logic here
        }
    }
}
```

## File Structure

```
Assets/Scripts/MiniTimeline/
├── Core/                    # Core interfaces and director
│   ├── IMiniClip.cs
│   ├── IMiniTrack.cs
│   ├── MiniTimelineDirector.cs
│   ├── MiniTimelineProject.cs
│   ├── BindingContext.cs
│   └── MiniPlayableGraph.cs
├── Tracks/                  # Track implementations
│   ├── AnimTrack.cs
│   ├── AnimClip.cs
│   ├── MorphTrack.cs
│   ├── MorphClips.cs
│   ├── EventTrack.cs
│   └── SignalClip.cs
├── Serialization/          # JSON save/load system
│   ├── ProjectSerializer.cs
│   └── TrackFactory.cs
├── Demo/                   # Demo and testing
│   └── MiniTimelineDemo.cs
└── Editor/                 # Editor utilities
    └── MiniTimelineEditorUtils.cs
```

## Testing

Run **Mini Timeline > Test Serialization** to verify JSON save/load functionality.

The demo scene includes:
- Animation track with walk cycle
- Morph track with smile animation  
- Event track with emotion trigger
- Real-time scrubbing and playback controls

## Requirements

- Unity 2021.3+ (uses Playables API)
- Universal Render Pipeline (URP) recommended
- Addressables package for asset loading

## License

This is sample code based on the provided specifications. Adapt and extend as needed for your project.