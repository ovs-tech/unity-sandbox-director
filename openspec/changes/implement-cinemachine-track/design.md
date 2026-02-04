# Design: Cinemachine Track

## Architecture

### MultiTargetMiniTrackBase
```csharp
public abstract class MultiTargetMiniTrackBase<T> : MiniTrackBase<T> where T : IMiniClip
{
    [SerializeField]
    protected List<string> targetBindKeys = new List<string>();
    
    protected List<Transform> targets = new List<Transform>();

    protected override void OnPrepare()
    {
        base.OnPrepare();
        targets.Clear();
        // Resolve each key in targetBindKeys using BindableObjectManager
        // ...
    }
}
```

### CinemachineClip
```csharp
public enum CinemachineShotType
{
    EyeLevel,
    HighAngle,
    LowAngle,
    Overhead,    // Top-down direct
    WormEye,     // Very low, looking up
    BirdEye,     // High, looking down (similar to overhead but maybe angled)
    SideShot,
    FrontShot
}

public class CinemachineClip : MiniClipBase
{
    public CinemachineShotType shotType = CinemachineShotType.EyeLevel;
    public int targetIndex = 0; // Index in the track's target list
    public float distance = 5f; // Distance from target
    public Vector3 offset = Vector3.zero; // Local offset
    
    // Blending/Damping params if needed
}
```

### CinemachineTrack
- **Target Object**: The `CinemachineVirtualCamera` component.
- **Functionality**:
    - During `OnEvaluate`:
        - Identify the active `CinemachineClip`.
        - Identify the subject `Transform` using `clip.targetIndex`.
        - Calculate the desired camera position and rotation.
            - *EyeLevel*: Same height as target, distance away (forward/back?).
            - *HighAngle*: Higher Y, looking down.
            - *LowAngle*: Lower Y, looking up.
            - *Overhead*: Directly above (+Y), looking down (+90 pitch).
        - Apply Position/Rotation to the VCam's `transform` (or manipulate VCam properties if utilizing specific body/aim components, but direct transform manipulation of a simple VCam is often easiest for "hard" cuts or simple moves. If we want Cinemachine's procedural powers, we might adjust `OrbitalTransposer` values).
    - **Approach**: For "simple setting", we might just move the VCam transform (overriding its body/aim) OR we assume the VCam has a specific Body/Aim component (like `Transposer` + `Composer`) and we modify the offsets.
    - **Decision**: To support "Shot Types" effectively without assuming a complex VCam setup, we will calculate the world position/rotation and apply it to the VCam's Transform. This acts like a "Hard" override, which is what `MovementTrack` does. If the user wants Cinemachine's soft damping, they can use the VCam's internal settings, but `MiniTimeline` usually drives things explicitly. However, if we move the transform every frame, we override physics.
    - *Alternative*: If the VCam uses `OrbitalTransposer`, we can set `m_FollowOffset`.
    - *Simpler Start*: Treat the VCam like a physical camera object and move it. This is consistent with `MovementTrack` but specific to camera logic (calculating pos relative to target).

## Constraints
- Assumes `Cinemachine` is present.
- `MultiTargetMiniTrackBase` needs access to `BindableObjectManager` to resolve keys.
