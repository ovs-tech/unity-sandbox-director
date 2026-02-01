# Tasks: Implement Cinemachine Track

1.  **Create MultiTargetMiniTrackBase**
    - [x] Create `Assets/Scripts/MiniTimeline/Core/MultiTargetMiniTrackBase.cs`.
    - [x] Implement `targetBindKeys` list and resolution logic in `OnPrepare`.

2.  **Create Cinemachine Definitions**
    - [x] Create `Assets/Scripts/MiniTimeline/Tracks/CinemachineClip.cs`.
    - [x] Define `CinemachineShotType` enum.
    - [x] Define properties (`shotType`, `targetIndex`, `distance`, `yaw`, `pitch`).

3.  **Create CinemachineTrack**
    - [x] Create `Assets/Scripts/MiniTimeline/Tracks/CinemachineTrack.cs`.
    - [x] Implement `OnEvaluate` logic to calculate camera position based on Shot Type and Target.
    - [x] Apply transform updates to the bound Virtual Camera.

4.  **Assembly Definition Update (If needed)**
    - [x] Check if `MiniTimeline` asmdef references `Cinemachine`. If not, add it. (No asmdef found, assuming default assembly).
