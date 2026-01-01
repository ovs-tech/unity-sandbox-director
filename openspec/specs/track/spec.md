# track Specification

## Purpose
TBD - created by archiving change modularize-timeline-ui-mvvm. Update Purpose after archive.
## Requirements
### Requirement: Track Header Controls
- The system SHALL bind enabled, mute, solo toggles/buttons to ViewModel.

#### Scenario: Toggle Enabled
- Given a track with `enabled=true`
- When the user toggles the `track-enabled` control
- Then `TrackViewModel.enabled` updates
- And the View reflects the new state

### Requirement: Track Binding Info
- The system SHALL display binding key and type via ViewModel.

#### Scenario: Show Binding Key and Type
- Given `bindKey="actor01"` and `type="anim"`
- When the Track View binds
- Then `track-bind-key` shows `actor01`
- And `track-type` shows `anim`

### Requirement: Lanes and Clips Container
- The system SHALL manage lanes and clip placement via Controller.

#### Scenario: Add Clip to Lane
- Given a track with lanes
- When Controller adds a clip to lane 2
- Then the clip element appears under `lane-2`

