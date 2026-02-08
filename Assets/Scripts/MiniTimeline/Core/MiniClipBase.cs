using System;
using UnityEngine;

namespace Systems.MiniTimeline.Core
{
    /// <summary>
    /// Base implementation of IMiniClip with common functionality
    /// </summary>
    [Serializable]
    public abstract class MiniClipBase : IMiniClip
    {
        [SerializeField] private string id;
        [SerializeField] private float start;
        [SerializeField] private float duration;

        public string Id { get => id; set => id = value; }
        public float Start { get => start; set => start = value; }
        public float Duration { get => duration; set => duration = value; }
        public virtual float End => Start + Duration;

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