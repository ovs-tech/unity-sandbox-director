# Spec: Cinemachine Track

## ADDED Requirements

### Requirement: Shot Type Control
The track SHALL position the bound Cinemachine Virtual Camera based on the active clip's shot type relative to the selected target.

#### Scenario: High Angle Shot
Given a CinemachineTrack bound to "VCam1"
And a clip with `ShotType` set to `HighAngle`
And `targetIndex` set to 0 (referencing "ActorA")
When `OnEvaluate` is called
Then "VCam1" transform should be positioned above and looking down at "ActorA"

#### Scenario: Eye Level Shot
Given a CinemachineTrack bound to "VCam1"
And a clip with `ShotType` set to `EyeLevel`
And `targetIndex` set to 0 (referencing "ActorA")
When `OnEvaluate` is called
Then "VCam1" transform should be at the same height level as "ActorA"'s eye/center

### Requirement: Multi-Target Selection
The track SHALL allow switching the camera focus between different targets defined in the multi-target list.

#### Scenario: Switching Targets
Given a CinemachineTrack with targets ["ActorA", "ActorB"]
And Clip1 references `targetIndex` 0 ("ActorA")
And Clip2 references `targetIndex` 1 ("ActorB")
When time transitions from Clip1 to Clip2
Then the camera should move to frame "ActorB" instead of "ActorA"
