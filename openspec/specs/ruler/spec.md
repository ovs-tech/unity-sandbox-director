# ruler Specification

## Purpose
TBD - created by archiving change modularize-timeline-ui-mvvm. Update Purpose after archive.
## Requirements
### Requirement: Ruler Marker Generation
- The system SHALL generate major/minor markers based on zoom and time range.

#### Scenario: Generate Markers on Zoom Change
- Given `TimelineEditorViewModel.zoom` changes
- When Controller receives the change
- Then `RulerView` regenerates markers with appropriate density

### Requirement: Playhead Interaction Area
- The system SHALL update playhead position and snap guide on user interaction.

#### Scenario: Move Playhead with Snap
- Given snap mode enabled
- When user drags within `ruler-playhead-area`
- Then playhead moves to nearest snap point
- And `ruler-snap-guide` becomes visible during drag

