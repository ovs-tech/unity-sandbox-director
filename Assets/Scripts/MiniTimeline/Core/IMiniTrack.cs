using System.Collections.Generic;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Base interface for all timeline tracks
    /// A track contains clips and can evaluate its state at any given time
    /// </summary>
    public interface IMiniTrack
    {
        /// <summary>
        /// Unique identifier for this track
        /// </summary>
        string Id { get; set; }
        
        /// <summary>
        /// Display name for this track
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// Binding key to resolve target object from BindingContext
        /// </summary>
        string BindKey { get; set; }
        
        /// <summary>
        /// Whether this track is currently enabled
        /// </summary>
        bool Enabled { get; set; }
        
        /// <summary>
        /// Evaluation order (lower values evaluated first)
        /// </summary>
        int Order { get; set; }
        
        /// <summary>
        /// Whether this track is bound to its target object
        /// </summary>
        bool IsBound { get; }
        
        /// <summary>
        /// Whether this track is ready to evaluate (enabled and prepared)
        /// </summary>
        bool IsReady { get; }
        
        /// <summary>
        /// Evaluation mode determining when callbacks are triggered
        /// </summary>
        EvaluateMode EvaluateMode { get; set; }
        
        /// <summary>
        /// Bind this track to its target object via BindingContext
        /// </summary>
        /// <param name="context">Binding context to resolve objects</param>
        void Bind(BindableObjectManager context);
        
        /// <summary>
        /// Prepare internal resources (called after binding)
        /// </summary>
        void Prepare();
        
        /// <summary>
        /// Evaluate track state at given time
        /// </summary>
        /// <param name="time">Timeline time in seconds</param>
        /// <param name="scrub">True if this is a scrub operation (seek)</param>
        void Evaluate(float time, bool scrub);
        
        /// <summary>
        /// Get all clips in this track
        /// </summary>
        /// <returns>Collection of clips</returns>
        IEnumerable<IMiniClip> GetClips();
        
        /// <summary>
        /// Add a new clip to this track
        /// </summary>
        /// <param name="clip">The clip to add</param>
        void AddClip(IMiniClip clip);
        
        /// <summary>
        /// Remove a clip from this track
        /// </summary>
        /// <param name="clip">The clip to remove</param>
        /// <returns>True if clip was removed, false if not found</returns>
        bool RemoveClip(IMiniClip clip);
        
        /// <summary>
        /// Called when project is closed to cleanup resources
        /// </summary>
        void OnProjectClosed();
    }
}