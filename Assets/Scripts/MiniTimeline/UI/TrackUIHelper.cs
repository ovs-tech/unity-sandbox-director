using MiniTimeline.Core;
using MiniTimeline.Tracks;
using UnityEngine;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Helper utility class for Track UI operations
    /// Provides common functionality used across track-related UI components
    /// </summary>
    public static class TrackUIHelper
    {
        /// <summary>
        /// Get user-friendly display name for a track instance
        /// </summary>
        /// <param name="track">The track instance</param>
        /// <returns>Friendly display name for the track</returns>
        public static string GetTrackDisplayName(IMiniTrack track)
        {
            if (track == null) return "Unknown Track";

            string trackType = track.GetType().Name;
            string friendlyName = trackType switch
            {
                "AnimTrack" => "Animation",
                "AnimatorTrack" => "Animator",
                "MorphTrack" => "Morph",
                "MovementTrack" => "Movement",
                "ExpressionTrack" => "Expression",
                "CameraTrack" => "Camera",
                "PoseIKTrack" => "IK Pose",
                "AudioTrack" => "Audio",
                "FxLightTrack" => "FX Light",
                "SignalTrack" => "Events",
                "UmaWardrobeTrack" => "UMA Wardrobe",
                _ => trackType.Replace("Track", "")
            };

            return $"{friendlyName} Track";
        }
        
        /// <summary>
        /// Get user-friendly display name for a track type string
        /// </summary>
        /// <param name="trackType">The track type string (e.g., MiniTimelineConstants.TRACK_ANIM)</param>
        /// <returns>Friendly display name for the track type</returns>
        public static string GetTrackDisplayName(string trackType)
        {
            if (string.IsNullOrEmpty(trackType)) return "Unknown Track";
            
            return trackType switch
            {
                MiniTimelineConstants.TRACK_ANIM => "Animation",
                MiniTimelineConstants.TRACK_ANIMATOR => "Animator",
                MiniTimelineConstants.TRACK_MORPH => "Morph",
                MiniTimelineConstants.TRACK_MOVEMENT => "Movement",
                MiniTimelineConstants.TRACK_SIGNAL => "Signal",
                MiniTimelineConstants.TRACK_AUDIO => "Audio",
                MiniTimelineConstants.TRACK_EXPRESSION => "Expression", 
                MiniTimelineConstants.TRACK_IK => "IK Pose",
                MiniTimelineConstants.TRACK_LIGHT_FX => "FX Light",
                MiniTimelineConstants.TRACK_UMA_WARDROBE => "UMA Wardrobe",
                _ => trackType.Replace("Track", "")
            };
        }
        
        /// <summary>
        /// Get friendly track name without the "Track" suffix
        /// </summary>
        /// <param name="track">The track instance</param>
        /// <returns>Friendly name without "Track" suffix</returns>
        public static string GetFriendlyTrackName(IMiniTrack track)
        {
            if (track == null) return "Unknown";

            string trackType = track.GetType().Name;
            return GetFriendlyTrackName(trackType);
        }
        
        /// <summary>
        /// Get friendly track name without the "Track" suffix from type name
        /// </summary>
        /// <param name="trackTypeName">The track type name (e.g., "AnimTrack")</param>
        /// <returns>Friendly name without "Track" suffix</returns>
        public static string GetFriendlyTrackName(string trackTypeName)
        {
            if (string.IsNullOrEmpty(trackTypeName)) return "Unknown";
            
            return trackTypeName switch
            {
                "AnimTrack" => "Animation",
                "AnimatorTrack" => "Animator",
                "MorphTrack" => "Morph",
                "MovementTrack" => "Movement",
                "ExpressionTrack" => "Expression",
                "CameraTrack" => "Camera",
                "PoseIKTrack" => "IK Pose",
                "AudioTrack" => "Audio",
                "FxLightTrack" => "FX Light",
                "SignalTrack" => "Events",
                "UmaWardrobeTrack" => "UMA Wardrobe",
                _ => trackTypeName.Replace("Track", "")
            };
        }
        
        /// <summary>
        /// Get track type string from track instance for use with constants
        /// </summary>
        /// <param name="track">The track instance</param>
        /// <returns>Track type string matching MiniTimelineConstants</returns>
        public static string GetTrackTypeString(IMiniTrack track)
        {
            if (track == null) return "";
            
            return track switch
            {
                AnimTrack _ => MiniTimelineConstants.TRACK_ANIM,
                AnimatorTrack _ => MiniTimelineConstants.TRACK_ANIMATOR,
                MorphTrack _ => MiniTimelineConstants.TRACK_MORPH,
                MovementTrack _ => MiniTimelineConstants.TRACK_MOVEMENT,
                SignalTrack _ => MiniTimelineConstants.TRACK_SIGNAL,
                UMAExpressionTrack _ => MiniTimelineConstants.TRACK_EXPRESSION,
                UmaWardrobeTrack _ => MiniTimelineConstants.TRACK_UMA_WARDROBE,
                _ => "generic"
            };
        }
        
        /// <summary>
        /// Get track type string from class name
        /// </summary>
        private static string GetTrackTypeFromClassName(string className)
        {
            return className switch
            {
                "AnimTrack" => MiniTimelineConstants.TRACK_ANIM,
                "AnimatorTrack" => MiniTimelineConstants.TRACK_ANIMATOR,
                "MorphTrack" => MiniTimelineConstants.TRACK_MORPH,
                "MovementTrack" => MiniTimelineConstants.TRACK_MOVEMENT,
                "SignalTrack" => MiniTimelineConstants.TRACK_SIGNAL,
                "AudioTrack" => MiniTimelineConstants.TRACK_AUDIO,
                "ExpressionTrack" => MiniTimelineConstants.TRACK_EXPRESSION,
                "CameraTrack" => "camera", // Use generic string if constant not available
                "PoseIKTrack" => MiniTimelineConstants.TRACK_IK,
                "FxLightTrack" => MiniTimelineConstants.TRACK_LIGHT_FX,
                "UmaWardrobeTrack" => MiniTimelineConstants.TRACK_UMA_WARDROBE,
                _ => "generic"
            };
        }
        
        /// <summary>
        /// Get appropriate icon or emoji for track type
        /// </summary>
        /// <param name="track">The track instance</param>
        /// <returns>Icon string for the track type</returns>
        public static string GetTrackIcon(IMiniTrack track)
        {
            if (track == null) return "❓";
            
            string trackType = track.GetType().Name;
            return trackType switch
            {
                "AnimTrack" => "🎭",
                "AnimatorTrack" => "🤖",
                "MorphTrack" => "🔄",
                "MovementTrack" => "🏃",
                "SignalTrack" => "📡",
                "AudioTrack" => "🔊",
                "CameraTrack" => "📷",
                "ExpressionTrack" => "😊",
                "PoseIKTrack" => "🦾",
                "FxLightTrack" => "💡",
                "UmaWardrobeTrack" => "�",
                _ => "📝"
            };
        }
        
        /// <summary>
        /// Get appropriate color for track type
        /// </summary>
        /// <param name="track">The track instance</param>
        /// <returns>Color for the track type</returns>
        public static Color GetTrackColor(IMiniTrack track)
        {
            if (track == null) return Color.gray;
            
            string trackType = track.GetType().Name;
            return trackType switch
            {
                "AnimTrack" => new Color(0.3f, 0.5f, 0.7f, 0.8f),     // Blue
                "AnimatorTrack" => new Color(0.7f, 0.3f, 0.5f, 0.8f), // Red
                "MorphTrack" => new Color(0.5f, 0.7f, 0.3f, 0.8f),    // Green
                "MovementTrack" => new Color(0.7f, 0.7f, 0.3f, 0.8f), // Yellow
                "SignalTrack" => new Color(0.5f, 0.3f, 0.7f, 0.8f),   // Purple
                "AudioTrack" => new Color(0.7f, 0.5f, 0.3f, 0.8f),    // Orange
                "ExpressionTrack" => new Color(0.8f, 0.4f, 0.8f, 0.8f), // Pink
                "CameraTrack" => new Color(0.4f, 0.8f, 0.8f, 0.8f),   // Cyan
                "PoseIKTrack" => new Color(0.8f, 0.8f, 0.4f, 0.8f),   // Light Yellow
                "FxLightTrack" => new Color(0.9f, 0.9f, 0.1f, 0.8f),  // Bright Yellow
                "UmaWardrobeTrack" => new Color(0.8f, 0.5f, 0.9f, 0.8f), // Light Purple
                _ => Color.gray
            };
        }
        
        /// <summary>
        /// Determine if a track type supports specific features
        /// </summary>
        /// <param name="track">The track instance</param>
        /// <param name="feature">Feature to check (e.g., "fade", "loop", "easing")</param>
        /// <returns>True if the track supports the feature</returns>
        public static bool SupportsFeature(IMiniTrack track, string feature)
        {
            if (track == null) return false;
            
            string trackType = track.GetType().Name;
            
            return feature.ToLower() switch
            {
                "fade" => trackType == "AnimTrack" || trackType == "AudioTrack",
                "loop" => trackType == "AnimTrack" || trackType == "AudioTrack",
                "easing" => trackType == "MovementTrack" || trackType == "MorphTrack",
                "speed" => trackType == "AnimTrack" || trackType == "AnimatorTrack",
                "weight" => trackType == "MorphTrack",
                _ => false
            };
        }
    }
}