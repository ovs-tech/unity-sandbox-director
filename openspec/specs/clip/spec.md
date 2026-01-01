# clip Specification

## Purpose
TBD - created by archiving change modularize-timeline-ui-mvvm. Update Purpose after archive.
## Requirements
### Requirement: Clip Header and Status
- The system SHALL bind clip title and status icons (lock, mute) via ViewModel.

#### Scenario: Update Clip Title
- Given `ClipViewModel.title="Intro"`
- When bound
- Then `clip-title` displays `Intro`

### Requirement: Resize Handles
- The system SHALL support resizing via left/right handles and update duration.

#### Scenario: Resize Right Handle
- Given a clip of duration 2.5s
- When user drags `clip-resize-right`
- Then `ClipViewModel.duration` increases
- And `clip-duration` label updates

### Requirement: Selection and Context
- The system SHALL reflect selection state and open context on trigger.

#### Scenario: Select Clip
- Given a non-selected clip
- When user clicks within `clip-root`
- Then `ClipViewModel.selected=true`
- And `clip-selection-border` becomes visible

