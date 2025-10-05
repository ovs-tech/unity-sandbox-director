using System;
using System.Collections.Generic;

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
        public int version = 1;
        
        /// <summary>
        /// Project name/title
        /// </summary>
        public string name = "Untitled";
        
        /// <summary>
        /// Total length of timeline in seconds
        /// </summary>
        public float length = 10f;
        
        /// <summary>
        /// Playback frame rate (for snapping)
        /// </summary>
        public float frameRate = 30f;
        
        /// <summary>
        /// All tracks in this project
        /// </summary>
        public List<TrackData> tracks = new List<TrackData>();
        
        /// <summary>
        /// Metadata for editor settings
        /// </summary>
        public ProjectMetadata metadata = new ProjectMetadata();
    }
    
    /// <summary>
    /// Serializable track data
    /// </summary>
    [Serializable]
    public class TrackData
    {
        /// <summary>
        /// Track unique ID
        /// </summary>
        public string id;
        
        /// <summary>
        /// Track type name (e.g., "AnimTrack", "MorphTrack")
        /// </summary>
        public string type;
        
        /// <summary>
        /// Binding key for target object
        /// </summary>
        public string bindKey;
        
        /// <summary>
        /// Whether track is enabled
        /// </summary>
        public bool enabled = true;
        
        /// <summary>
        /// Evaluation order
        /// </summary>
        public int order = 0;
        
        /// <summary>
        /// All clips in this track
        /// </summary>
        public List<ClipData> clips = new List<ClipData>();
        
        /// <summary>
        /// Track-specific properties
        /// </summary>
        public Dictionary<string, object> properties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Serializable clip data
    /// </summary>
    [Serializable]
    public class ClipData
    {
        /// <summary>
        /// Clip unique ID
        /// </summary>
        public string id;
        
        /// <summary>
        /// Start time in seconds
        /// </summary>
        public float start;
        
        /// <summary>
        /// Duration in seconds
        /// </summary>
        public float duration;
        
        /// <summary>
        /// Clip-specific payload data
        /// </summary>
        public Dictionary<string, object> payload = new Dictionary<string, object>();
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
        public float zoom = 1f;
        
        /// <summary>
        /// Timeline scroll position
        /// </summary>
        public float scrollPosition = 0f;
        
        /// <summary>
        /// Selected clips/tracks
        /// </summary>
        public List<string> selection = new List<string>();
        
        /// <summary>
        /// Custom editor properties
        /// </summary>
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