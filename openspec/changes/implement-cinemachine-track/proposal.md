# Proposal: Implement Cinemachine Track with Shot Presets

## Summary
Introduce a new `CinemachineTrack` and `CinemachineClip` to the MiniTimeline system. This track will allow users to control a Cinemachine Virtual Camera using high-level shot presets (e.g., High Angle, Low Angle, Eye Level) relative to multiple target subjects.

To support this, we will also implement a `MultiTargetMiniTrackBase` class, which extends the existing `MiniTrackBase` to support binding to multiple target objects (e.g., actors in a scene).

## Motivation
Currently, MiniTimeline lacks a dedicated track for procedural camera control using Cinemachine. Users need a simple way to sequence camera shots without manually animating transform positions for every shot. By using semantic shot descriptions ("Close-up", "Bird's Eye"), we can speed up the layout of cutscenes.

## Proposed Changes

### 1. Core: `MultiTargetMiniTrackBase`
- Create a new abstract base class `MultiTargetMiniTrackBase<TClip>` in `Assets/Scripts/MiniTimeline/Core` (or `Tracks` if `Core` is restricted, but `Core` is preferred for base classes).
- This class will manage a list of `BindKey`s and resolve them to a list of `GameObject` or `Transform` targets.

### 2. Tracks: `CinemachineTrack` & `CinemachineClip`
- **`CinemachineClip`**:
    - Contains a `ShotType` enum (EyeLevel, HighAngle, LowAngle, Overhead, WormEye, BirdEye).
    - Contains an index to select which target (from the track's list) to look at/frame.
    - Contains damping/blending settings.
- **`CinemachineTrack`**:
    - Binds to a `CinemachineVirtualCamera`.
    - Uses the `MultiTargetMiniTrackBase` to hold references to potential subjects (Actors).
    - In `OnEvaluate`, positions the Virtual Camera based on the active clip's `ShotType` relative to the selected target.

## Dependencies
- `com.unity.cinemachine` package must be installed (assumed available or will check).
