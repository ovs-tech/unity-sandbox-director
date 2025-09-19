using System;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Base interface for all timeline clips
    /// Represents a time segment with duration and payload data
    /// </summary>
    public interface IMiniClip
    {
        /// <summary>
        /// Unique identifier for this clip
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Start time in seconds
        /// </summary>
        float Start { get; }
        
        /// <summary>
        /// Duration in seconds (0 for markers/events)
        /// </summary>
        float Duration { get; }
        
        /// <summary>
        /// End time (Start + Duration)
        /// </summary>
        float End => Start + Duration;
        
        /// <summary>
        /// Check if the given time falls within this clip
        /// </summary>
        /// <param name="time">Time to check</param>
        /// <returns>True if time is within [Start, End]</returns>
        bool Contains(float time);
        
        /// <summary>
        /// Get normalized time (0-1) within this clip
        /// </summary>
        /// <param name="time">Global timeline time</param>
        /// <returns>Local normalized time (0-1)</returns>
        float GetNormalizedTime(float time);
    }
    
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