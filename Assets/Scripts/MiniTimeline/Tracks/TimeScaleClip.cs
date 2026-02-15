using System;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Clip for controlling Time.timeScale
    /// </summary>
    [Serializable]
    public class TimeScaleClip : MiniClipBase
    {
        [Header("Time Settings")]
        [Tooltip("The time scale to apply (1.0 = normal, 0.5 = half speed, etc)")]
        [Range(0.01f, 10f)]
        public float timeScale = 1.0f;

        [Header("Blending")]
        [Tooltip("Modulation curve (multiplied by timeScale). Default is constant 1.0.")]
        public AnimationCurve blendCurve = AnimationCurve.Constant(0, 1, 1);

        public override string ToString()
        {
            return $"TimeScale: {timeScale}x ({Start:F2}s - {End:F2}s)";
        }
    }
}
