using System;
using UnityEngine;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Clip data for playing audio on the timeline
    /// </summary>
    [Serializable]
    public class AudioClipData : MiniClipBase
    {
        [Header("Audio Asset")]
        [Tooltip("Path to the audio asset (supports Addressables with 'addr:' prefix) or Resource path")]
        public string audioAsset;

        [Header("Playback Settings")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;

        public bool loop = false;

        [Header("Blending")]
        [Range(0f, 5f)]
        public float fadeIn = 0f;

        [Range(0f, 5f)]
        public float fadeOut = 0f;

        public override string ToString()
        {
            return $"Audio: {audioAsset} ({Start:F2}s - {End:F2}s)";
        }
    }
}
