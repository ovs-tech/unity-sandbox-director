# timeline-editor Specification

## Purpose
TBD - created by archiving change modularize-timeline-ui-mvvm. Update Purpose after archive.
## Requirements
### Requirement: MVVM-driven Timeline Editor View
- The system SHALL instantiate the Timeline Editor UXML and bind to a separated ViewModel and Controller.
- The system SHALL expose commands for play, pause, stop, save, load, add track, and binding manager.

#### Scenario: Instantiate and Bind Timeline Editor
- Given the asset at `Assets/Scripts/MiniTimeline/UI/Styles/TimelineEditorUIToolkit.uxml`
- When `TimelineEditorView.InitializeView()` loads the UXML
- Then a `TimelineEditorView` is created and bound to `TimelineEditorViewModel` via Inventory-style property bindings and controller wiring
- And control events (play/pause/stop) invoke ViewModel commands
- And time and zoom sliders reflect `time` and `zoom` from ViewModel

#### Scenario: Status Text Localization
- Given the `status-text` element bound to a localization table
- When ViewModel updates `statusText`
- Then the View updates the label using the localized string

### Requirement: Playhead Coordination
- The system SHALL update the playhead position in response to time changes.

#### Scenario: Playhead Moves on Time Change
- Given `TimelineEditorViewModel.time` changes
- When Controller receives the change
- Then the `playhead` element position updates accordingly

