using System;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Base implementation of IMiniClip with common functionality
    /// </summary>
    [Serializable]
    public abstract class MiniClipBase : IMiniClip
    {
        public string Id { get; set; }
        public float Start { get; set; }
        public float Duration { get; set; }

        public virtual bool Contains(float time)
        {
            return time >= Start && time <= (Start + Duration);
        }

        public virtual float GetNormalizedTime(float time)
        {
            if (Duration <= 0f) return 0f;
            return UnityEngine.Mathf.Clamp01((time - Start) / Duration);
        }
    }
}