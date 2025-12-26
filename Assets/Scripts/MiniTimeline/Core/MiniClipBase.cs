using System;
using Sirenix.OdinSerializer;
using UnityEngine;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Base implementation of IMiniClip with common functionality
    /// </summary>
    [Serializable]
    public abstract class MiniClipBase : IMiniClip
    {
        [OdinSerialize] public string Id { get; set; }
        [OdinSerialize] public float Start { get; set; }
        [OdinSerialize] public float Duration { get; set; }

        public virtual bool Contains(float time)
        {
            return time >= Start && time <= (Start + Duration);
        }

        public virtual float GetNormalizedTime(float time)
        {
            if (Duration <= 0f) return 0f;
            return Mathf.Clamp01((time - Start) / Duration);
        }
    }
}