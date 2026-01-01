using System;
using System.Collections.Generic;
using Sirenix.OdinSerializer;

namespace MiniTimeline.Core
{
    /// <summary>
    /// Serializable project data for Mini Timeline
    /// Contains all track and clip data in JSON-friendly format
    /// </summary>
    [Serializable]
    public class MiniTimelineProject
    {
        /// <summary>
        /// Project format version for compatibility
        /// </summary>
        [OdinSerialize]
        public int version = 1;
        
        /// <summary>
        /// Project name/title
        /// </summary>
        [OdinSerialize]
        public string name = "Untitled";

        /// <summary>
        /// Total length of timeline in seconds
        /// </summary>
        [OdinSerialize]
        public float length = 10f;

        /// <summary>
        /// Playback frame rate (for snapping)
        /// </summary>
        [OdinSerialize]
        public float frameRate = 30f;

        /// <summary>
        /// All runtime tracks in this project (polymorphic)
        /// </summary>
        [OdinSerialize]
        public List<IMiniTrack> tracks = new List<IMiniTrack>();

        /// <summary>
        /// Metadata for editor settings
        /// </summary>
        [OdinSerialize]
        public ProjectMetadata metadata = new ProjectMetadata();
    }
    
    /// <summary>
    /// Project metadata for editor settings
    /// </summary>
    [Serializable]
    public class ProjectMetadata
    {
        /// <summary>
        /// Timeline zoom level
        /// </summary>
        [OdinSerialize]
        public float zoom = 1f;
        
        /// <summary>
        /// Timeline scroll position
        /// </summary>
        [OdinSerialize]
        public float scrollPosition = 0f;
        
        /// <summary>
        /// Selected clips/tracks
        /// </summary>
        [OdinSerialize]
        public List<string> selection = new List<string>();
        
        /// <summary>
        /// Custom editor properties
        /// </summary>
        [OdinSerialize]
        public Dictionary<string, object> editorData = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Common enums and constants
    /// </summary>
    public static class MiniTimelineConstants
    {
        // Track type names
        public const string TRACK_ANIM = "AnimTrack";
        public const string TRACK_ANIMATOR = "AnimatorTrack";
        public const string TRACK_MORPH = "MorphTrack";
        public const string TRACK_EXPRESSION = "ExpressionTrack";
        public const string TRACK_MOVEMENT = "MovementTrack";
        public const string TRACK_IK = "PoseIKTrack";
        public const string TRACK_AUDIO = "AudioTrack";
        public const string TRACK_LIGHT_FX = "FxLightTrack";
        public const string TRACK_SIGNAL = "SignalTrack";
        public const string TRACK_UMA_WARDROBE = "UmaWardrobeTrack";
        public const string TRACK_UMA_EXPRESSION = "UMAExpressionTrack";

        // Asset reference prefixes
        public const string ASSET_ADDRESSABLE = "addr:";
        public const string ASSET_SCENE = "scene:";
        
        // Default frame rate
        public const float DEFAULT_FRAME_RATE = 30f;
        
        // Minimum clip duration
        public const float MIN_CLIP_DURATION = 0.01f;
    }
}