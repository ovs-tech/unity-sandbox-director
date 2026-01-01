# Change: Modularize Timeline UI with MVVM (Controller–ViewModel–View–Model)

## Summary
Introduce a separated MVVM architecture for timeline-related UI defined in the following UXML assets:
- Assets/Scripts/MiniTimeline/UI/Styles/TimelineEditorUIToolkit.uxml
- Assets/Scripts/MiniTimeline/UI/Styles/TrackUIToolkit.uxml
- Assets/Scripts/MiniTimeline/UI/Styles/ClipUIToolkit.uxml
- Assets/Scripts/MiniTimeline/UI/Styles/TimelineRulerToolkit.uxml

Without touching existing classes like `TimelineEditorUIToolkit`, we will add new controller, viewmodel, view, and model abstractions to load UXML, bind data, and handle interactions.

## Goals
- Establish clear separation of concerns: Controller, ViewModel, View, Model.
- Keep changes minimally invasive: do not modify legacy classes; integrate via new entry points.
- Maintain current visual structure defined by UXML.
- Enable testable, data-driven UI with localized tooltips and labels.

## Non-Goals
- Re-skinning or redesigning UXML layouts.
- Changing existing asset GUIDs or localization tables.
- Implementing gameplay logic beyond UI orchestration.

## Constraints & Guardrails
- Do not touch existing `TimelineEditorUIToolkit` class or other legacy UI scripts.
- Favor straightforward minimal implementations first; extend only when required.
- Do not introduce new Core interfaces; follow Inventory’s concrete class pattern (controllers, views, models, optional colocated viewmodels).
- Preserve Input System usage and localization bindings seen in the UXML.

## Approach
- Create a new `MVVM` folder under `Assets/Scripts/MiniTimeline/UI` for the runtime/editor components.
- Align structure and conventions with the existing Inventory MVVM:
  - Separate files for `Controller`, `View`, and `Model` (e.g., `TimelineEditorController.cs`, `TimelineEditorView.cs`, `TimelineEditorModel.cs`).
  - Provide a `Builder` class under the controller file to compose dependencies (`With...` methods) and `Build()` returning a bound controller, mirroring `InventoryController.Builder`.
  - Use `Initialize()` and `Bind()` patterns to set up views and models, consistent with `InventoryController.Initialize` and `Bind`.
- Views load UXML themselves via `InitializeView()`; binding relies on `ViewModel` `BindingProperty` and controller wiring (no separate loader/binder).
- Implement component-specific modules:
  - Timeline Editor: play/pause/stop, time slider, zoom, status bar.
  - Track: header controls, lanes, enable/mute/solo, binding info.
  - Clip: header, markers, resize handles, selection state.
  - Ruler: time markers, playhead interactions, snapping guide.
- Provide a bootstrap controller (e.g., `MiniTimelineController`) to assemble views and viewmodels without touching old classes.

## Risks
- Event duplication if legacy classes are still active; mitigate by opting into new controllers only in new entry points.
- Binding complexity across nested views; mitigate by clear View ownership of UXML and typed ViewModels; avoid extra helper abstraction.

## Open Questions
- Should the MVVM live in Editor-only code, or support runtime playback? (Default: Editor, extensible to runtime.)
- Preferred dependency injection: Zenject or custom lightweight? (Default: custom lightweight DI to minimize dependencies.)

## References
- See `openspec/AGENTS.md` for conventions.
- UXML references listed in Summary.
