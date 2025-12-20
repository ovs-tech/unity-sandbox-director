# MiniTimeline MVVM Usage Guide

## Overview

The Timeline MVVM system provides a clean, modular architecture for timeline UI components. It separates concerns across Models (data), ViewModels (presentation logic), Views (UI), and Controllers (orchestration).

## Key Components

### Models
- **TimelineEditorModel**: Stores editor state (time, zoom, playing).
- **TrackModel**: Stores track metadata (title, enabled, muted).
- **ClipModel**: Stores clip state (duration, locked, selected).
- **TimelineRulerModel**: Stores ruler state (current time, zoom).

All models are serializable and hold the single source of truth.

### Views
- **TimelineEditorView**: Loads `TimelineEditorUIToolkit.uxml` and exposes typed control getters (buttons, sliders, labels).
- **TrackView**: Loads `TrackUIToolkit.uxml` and exposes toggles and buttons.
- **ClipView**: Loads `ClipUIToolkit.uxml` and exposes clip controls.
- **TimelineRulerView**: Loads `TimelineRulerToolkit.uxml` and exposes ruler elements.

Views call `InitializeView(viewModel)` as a coroutine to set up UI Toolkit hierarchies.

### ViewModels
- Expose `BindableProperty<T>` instances for UI binding.
- Provide command methods (Play, Pause, Stop, Mute, etc.).
- Bind properties to Model values via `BindableProperty<T>.Bind(() => model.Property)`.

### Controllers
- Wire View events to ViewModel commands.
- Update UI in response to Model changes.
- Use Builder pattern for composable instantiation.

## Composition with Builder

```csharp
// Simple composition
var view = myGameObject.AddComponent<TimelineEditorView>();
var model = new TimelineEditorModel();
var controller = new TimelineEditorController.Builder(view)
    .WithModel(model)
    .Build();

// Fluent API mirrors InventoryController
var controller = new TimelineEditorController.Builder(editorView)
    .WithModel(myCustomModel)
    .Build();
```

## Integration with Existing UI

Do NOT modify `TimelineEditorUIToolkit` or other legacy classes. Instead:

1. Create new entry points (scenes, editor windows) that use MVVM controllers.
2. Opt-in by instantiating `MiniTimelineController` as a bootstrap.
3. Use factory methods to create Track, Clip, and Ruler instances as needed.

Example scene setup:
```csharp
public class TimelineEditorScene : MonoBehaviour {
    void Start() {
        var bootstrap = gameObject.AddComponent<MiniTimelineController>();
        // Bootstrap automatically initializes; no legacy code affected.
    }
}
```

## Event Flow

1. **User Interaction**: Click button, drag slider, etc.
2. **View Event**: View fires event to Controller.
3. **ViewModel Command**: Controller invokes ViewModel method.
4. **Model Update**: ViewModel updates Model state.
5. **UI Refresh**: Controller binds updated ViewModel properties to View controls.

Example:
```csharp
// User clicks Play button
view.OnPlayClicked += () => viewModel.Play();

// ViewModel updates Model
public void Play() => _model.Play();

// Model notifies via BindableProperty
public class ViewModel {
    public readonly BindableProperty<bool> IsPlaying = 
        BindableProperty<bool>.Bind(() => _model.IsPlaying);
}
```

## Testing

Run unit tests from `Assets/Tests/MiniTimelineMVVM/Editor/`:

- **ViewModelTests.cs**: Verifies ViewModel command execution and state transitions.
- **MVVMParityTests.cs**: Ensures Builder pattern matches Inventory conventions.
- **IntegrationTests.cs**: Tests View initialization, UXML instantiation, and controller binding.

Example test:
```csharp
[Test]
public void ViewModel_Play_SetsIsPlayingTrue() {
    var model = new TimelineEditorModel();
    var vm = new TimelineEditorController.ViewModel(model);
    vm.Play();
    Assert.IsTrue(vm.IsPlaying.Value);
}
```

## Localization

Views bind labels via `BindableProperty<T>`. To support localization:

1. Add a localization key field to Models.
2. Extend ViewModel to fetch translated text via `LocalizedString`.
3. Bind result to View labels via `SetBinding(...)` on UI Toolkit controls.

Example:
```csharp
var label = view.GetLabel("statusLabel");
if (label != null) {
    label.SetBinding(nameof(Label.text), new DataBinding {
        dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
        bindingMode = BindingMode.ToTarget
    });
}
```

## Extensibility

- **Custom Models**: Create new serializable Model classes for specialized data.
- **Custom ViewModels**: Add domain-specific commands and properties.
- **Custom Views**: Load alternative UXML layouts for different skins.
- **Custom Controllers**: Subclass or compose existing controllers for enhanced behavior.

All components follow the same pattern, enabling incremental extension without breaking changes.

