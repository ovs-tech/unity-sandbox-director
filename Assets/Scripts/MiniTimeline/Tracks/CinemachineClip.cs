using System;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Presets for common camera shots relative to a target.
    /// </summary>
    public enum CinemachineShotType
    {
        EyeLevel,       // At target eye/center height
        HighAngle,      // Looking down from above
        LowAngle,       // Looking up from below
        Overhead,       // Directly above looking down
        BirdEye,        // High altitude looking down
        WormEye,        // Very low looking up
        SideView,       // Profiles
        CloseUp,        // Very close to target

        // Additional common shot types
        DutchAngle,     // Tilted/oblique
        OverTheShoulder,// Over the shoulder (OTS)
        POV,            // Point-of-view (first person)

        // Common shot sizes
        ExtremeLongShot,
        LongShot,
        MediumShot,
        ExtremeCloseUp
    }

    /// <summary>
    /// Clip for controlling Cinemachine Virtual Camera properties and shot presets.
    /// </summary>
    [Serializable]
    public class CinemachineClip : MiniClipBase
    {
        [Header("Shot Settings")]
        public CinemachineShotType shotType = CinemachineShotType.EyeLevel;
        public int targetIndex = 0;
        
        [Header("Framing")]
        public float distance = 5f;
        public Vector3 offset = Vector3.zero;
        public float yaw = 0f;      // Rotation around Y
        public float pitch = 0f;    // Vertical angle adjustment
        public float roll = 0f;     // Roll/tilt (for Dutch angle)
        
        [Header("Lens")]
        public float fieldOfView = 60f;
        public bool overrideFOV = false;

        [Header("Transitions")]
        public CameraAnimationCurve animationCurve = CameraAnimationCurve.EaseInOut;
        public float smoothTime = 0.5f;

        public float GetLocalTime(float globalTime)
        {
            return globalTime - Start;
        }
    }
}
