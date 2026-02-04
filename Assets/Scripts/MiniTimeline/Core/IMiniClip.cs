namespace Systems.MiniTimeline.Core
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
}