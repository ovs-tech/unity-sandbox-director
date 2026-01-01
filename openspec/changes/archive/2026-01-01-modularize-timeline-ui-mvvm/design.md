# Design: Timeline UI MVVM

## Architecture Overview
- **Model**: Data structures representing timeline state (tracks, clips, time, zoom). Serializable and source of truth.
- **ViewModel**: Presentation-friendly state and commands bound to the View; emits events and applies user actions to the Model via Controller.
- **View**: UXML-driven UI Toolkit hierarchy; exposes typed references to controls and subscribe points.
- **Controller**: Orchestrates View ↔ ViewModel interactions, listens for UI events, invokes ViewModel commands, and updates Models.

## Component Modules
- **Timeline Editor**
  - View: play/pause/stop buttons, time and zoom sliders, status text, container regions.
  - ViewModel: `isPlaying`, `time`, `zoom`, `statusText`; commands: `Play`, `Pause`, `Stop`, `Save`, `Load`, `AddTrack`, `OpenBindingManager`.
  - Controller: routes button clicks, slider changes, updates playhead and rulers.
- **Track**
  - View: header controls (enabled, mute, solo, menu), binding info, lanes, clips container.
  - ViewModel: `title`, `bindKey`, `type`, `enabled`, `muted`, `solo`; commands: `ToggleEnabled`, `Mute`, `Solo`, `OpenMenu`.
  - Controller: manages track lifecycle, state toggles, lane updates.
- **Clip**
  - View: header, status icons, preview area, markers, resize handles, selection border, context trigger.
  - ViewModel: `title`, `duration`, `details`, `locked`, `muted`, selection and resize interactions; commands: `ResizeLeft`, `ResizeRight`, `Select`, `OpenContext`.
  - Controller: applies edit operations and propagates changes to the track.
- **Ruler**
  - View: current time indicator, snap guide, playhead area, generated markers.
  - ViewModel: `currentTime`, marker density based on zoom; commands: `MovePlayhead`, `SnapTo`.
  - Controller: updates markers and playhead position on time/zoom changes.

## Integration Strategy
- New MVVM modules live under `Assets/Scripts/MiniTimeline/UI/MVVM/`.
- Follow Inventory MVVM conventions to ease adoption:
  - Controller file contains the controller class, an inner `Builder` class for composition, and (optionally) a simple `ViewModel` class structure when practical.
  - Separate `View` and `Model` classes in their own files with `InitializeView()` and domain methods respectively.
  - Use `Initialize()` and `Bind()` lifecycle methods on the controller to mirror `InventoryController`.
- A bootstrap (e.g., `MiniTimelineController`) delegates to Views (`InitializeView()`) to instantiate UXML, wires ViewModels via controller code and binding properties, and registers handlers (no separate loader/binder).
- Legacy classes remain untouched; consumers can opt into the MVVM flow where needed.

## Event & Data Flow
1. User interacts with View (button/slider/drag).
2. View raises events to Controller.
3. Controller invokes ViewModel commands.
4. ViewModel updates Model and notifies bindings.
5. Controller refreshes Views (e.g., playhead position, labels).

## Testing & Validation
- Unit tests for ViewModel commands and state transitions.
- Integration tests that exercise binding and event routing with mocked Views.
- Editor tests for UXML instantiation and control lookup stability.
- Parity tests against Inventory patterns: ensure `Builder.Build()` returns a bound controller and `Bind()` correctly wires `Model` → `ViewModel` → `View`.

## Extensibility
- Composition enables swapping bindings, DI, and specialized controllers.
- Future DLC can extend Models and ViewModels without changing existing Views.
