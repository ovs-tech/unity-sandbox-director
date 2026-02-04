# Spec: MultiTargetMiniTrackBase

## ADDED Requirements

### Requirement: Support Multiple Target Bindings
The track SHALL be able to bind to multiple target objects via a list of bind keys.

#### Scenario: Binding multiple actors
Given a track that inherits from `MultiTargetMiniTrackBase`
And the track has `targetBindKeys` set to ["ActorA", "ActorB"]
When `OnPrepare` is called
Then `targets` list should contain the Transform of "ActorA" and "ActorB"
