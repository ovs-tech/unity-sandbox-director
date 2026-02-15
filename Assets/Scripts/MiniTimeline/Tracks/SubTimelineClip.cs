using System;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Clip for playing a sub-timeline project
    /// </summary>
    [Serializable]
    public class SubTimelineClip : MiniClipBase
    {
        [Header("Project")]
        [Tooltip("Path to the MiniTimeline project JSON file (relative to Projects folder)")]
        public string projectPath;

        [Header("Playback")]
        [Tooltip("Playback speed multiplier")]
        public float speed = 1.0f;

        public override string ToString()
        {
            return $"SubTimeline: {projectPath} ({Start:F2}s - {End:F2}s)";
        }
    }
}
