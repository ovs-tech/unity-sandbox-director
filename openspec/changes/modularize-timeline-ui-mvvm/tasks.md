# Tasks: Modularize Timeline UI with MVVM

1. Create folder `Assets/Scripts/MiniTimeline/UI/MVVM/` and subfolders `Timeline/`, `Track/`, `Clip/`, `Ruler/` (no `Core/` folder).
2. Use View classes to load UXML directly via `InitializeView()` (no separate loader), mirroring Inventory.
3. Handle binding via `ViewModel` properties (`BindingProperty`) and controller event wiring (no separate binder), mirroring Inventory.
4. Mirror Inventory MVVM conventions for file/class layout:
	- `TimelineEditorController.cs` containing `TimelineEditorController`, inner `Builder`, and optional `TimelineEditorViewModel`.
	- `TimelineEditorView.cs` with `InitializeView()` and typed control references.
	- `TimelineEditorModel.cs` with serializable state and `Bind()` style events.
5. Scaffold Timeline Editor View, ViewModel, Controller classes; load from `TimelineEditorUIToolkit.uxml` and bind controls using `Initialize()` and `Bind()` lifecycle like `InventoryController`.
6. Scaffold Track View, ViewModel, Controller classes; load from `TrackUIToolkit.uxml` and bind header, lanes, clips.
7. Scaffold Clip View, ViewModel, Controller classes; load from `ClipUIToolkit.uxml` and bind markers, resize handles.
8. Scaffold Ruler View, ViewModel, Controller classes; load from `TimelineRulerToolkit.uxml` and generate markers.
9. Add a bootstrap `MiniTimelineController` that composes editor-level UI without touching `TimelineEditorUIToolkit`.
10. Implement localization hookup for tooltips/labels to mirror existing bindings.
11. Add unit tests for ViewModels (commands/state) and controller event wiring.
12. Add parity tests against Inventory: builder flow, bind lifecycle, and event propagation.
13. Add editor integration tests: instantiate UXMLs, verify binding and interactions.
14. Document usage with a short README and code comments in MVVM modules.
15. Run `openspec validate modularize-timeline-ui-mvvm --strict` and fix any issues.
