using System;
using UnityEngine;
using Sirenix.OdinSerializer;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks
{
    /// <summary>
    /// Event trigger edge type
    /// </summary>
    public enum EventTriggerEdge
    {
        OnEnter,    // Trigger when playhead enters the marker
        OnExit,     // Trigger when playhead exits the marker
        Both        // Trigger on both enter and exit
    }
    
    /// <summary>
    /// Event signal clip (marker with zero duration)
    /// Triggers events when playhead crosses specific time points
    /// </summary>
    [Serializable]
    public class SignalClip : Core.MiniClipBase
    {
        /// <summary>
        /// Event identifier
        /// </summary>
        public string eventId;
        
        /// <summary>
        /// Event payload data (JSON string, number, or simple string)
        /// </summary>
        [OdinSerialize]
        public string payload = "";
        
        /// <summary>
        /// When to trigger the event
        /// </summary>
        [OdinSerialize]
        public EventTriggerEdge edge = EventTriggerEdge.OnEnter;
        
        /// <summary>
        /// Whether to fire events during scrub operations
        /// </summary>
        [OdinSerialize]
        public bool fireOnScrub = false;
        
        /// <summary>
        /// Display color for editor
        /// </summary>
        [OdinSerialize]
        public Color color = Color.red;
        
        /// <summary>
        /// Constructor - signals have zero duration by default
        /// </summary>
        public SignalClip()
        {
            Duration = 0f;
        }
        
        /// <summary>
        /// Check if event should trigger based on playhead movement
        /// </summary>
        /// <param name="previousTime">Previous playhead time</param>
        /// <param name="currentTime">Current playhead time</param>
        /// <param name="scrub">Whether this is a scrub operation</param>
        /// <returns>True if event should fire</returns>
        public bool ShouldTrigger(float previousTime, float currentTime, bool scrub)
        {
            // Don't fire on scrub unless explicitly enabled
            if (scrub && !fireOnScrub) return false;
            
            // Check if playhead crossed the marker time
            bool crossedForward = previousTime < Start && currentTime >= Start;
            bool crossedBackward = previousTime >= Start && currentTime < Start;
            
            switch (edge)
            {
                case EventTriggerEdge.OnEnter:
                    return crossedForward;
                    
                case EventTriggerEdge.OnExit:
                    return crossedBackward;
                    
                case EventTriggerEdge.Both:
                    return crossedForward || crossedBackward;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Get trigger direction
        /// </summary>
        /// <param name="previousTime">Previous time</param>
        /// <param name="currentTime">Current time</param>
        /// <returns>1 for forward, -1 for backward, 0 for no movement</returns>
        public int GetTriggerDirection(float previousTime, float currentTime)
        {
            if (currentTime > previousTime) return 1;
            if (currentTime < previousTime) return -1;
            return 0;
        }
    }
    
    /// <summary>
    /// Event data passed to event handlers
    /// </summary>
    public struct TimelineEvent
    {
        /// <summary>
        /// Event identifier
        /// </summary>
        public string eventId;
        
        /// <summary>
        /// Event payload
        /// </summary>
        public string payload;
        
        /// <summary>
        /// Time when event was triggered
        /// </summary>
        public float time;
        
        /// <summary>
        /// Trigger direction (1 = forward, -1 = backward)
        /// </summary>
        public int direction;
        
        /// <summary>
        /// Whether this was triggered by scrub
        /// </summary>
        public bool isScrub;
        
        /// <summary>
        /// Source clip that triggered the event
        /// </summary>
        public SignalClip sourceClip;
    }
}