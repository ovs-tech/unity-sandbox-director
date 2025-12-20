# MiniTimeline MVVM Architecture

This folder contains a modular MVVM (Model–ViewController–ViewModel–View) architecture for timeline UI components. It follows Inventory MVVM conventions and provides clean separation of concerns without modifying legacy UI classes.

## Directory Structure

- **Timeline/**: Timeline editor controller, view, and model (play/pause, time slider, zoom, status).
- **Track/**: Track component with enable/mute/solo toggles and binding info.
- **Clip/**: Clip component with title, duration, locked/muted state, and resize handles.
- **Ruler/**: Playhead ruler with time markers and snap guide.
- **MiniTimelineController.cs**: Bootstrap controller that composes all modules.

## Architecture

### Model
Serializable data structures holding state. Examples:
- `TimelineEditorModel`: `time`, `zoom`, `isPlaying`, `statusText`
- `TrackModel`: `title`, `enabled`, `muted`, `solo`, `bindKey`
- `ClipModel`: `title`, `duration`, `locked`, `muted`, `selected`

### View
Loads UXML via `InitializeView()` coroutine and exposes typed control getters. No event binding directly; delegates to Controller.

### ViewModel
Exposes `BindableProperty<T>` for UI binding and provides command methods (Play, Pause, etc.). Instances are created by Controllers.

### Controller
Wires View events to ViewModel commands, updates Models, and refreshes UI. Uses Builder pattern (with `Builder.Build()`) for composition.

## Usage

### Basic Bootstrap (in a scene)

```csharp
var bootstrap = gameObject.AddComponent<MiniTimelineController>();
// Initialization happens automatically on Start()
```

### Creating Track Instances

```csharp
var trackGO = new GameObject("Track");
var track = bootstrap.CreateTrack(trackGO);
```

### Creating Clip Instances

```csharp
var clipGO = new GameObject("Clip");
var clip = bootstrap.CreateClip(clipGO);
```

## Conventions

1. **File Layout**: `TimelineEditorController.cs` contains Controller, optional ViewModel, and Builder.
2. **View/Model**: Separate files (`TimelineEditorView.cs`, `TimelineEditorModel.cs`).
3. **Lifecycle**: `Initialize()` → `Bind()` → event wiring (mirroring `InventoryController`).
4. **Binding**: Views bind labels via `BindableProperty<T>.Bind(() => model.Property)`.
5. **No Separate Loader/Binder**: Views instantiate UXML directly; Controllers wire events.

## Extensibility

- Swap Models to change data source.
- Extend ViewModels with additional commands.
- Add Views for new control layouts without changing Controllers.

## Testing

- Unit tests for ViewModel commands and state transitions.
- Integration tests exercise binding and event routing.
- Parity tests ensure Builder flow matches Inventory patterns.

