using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.MiniTimeline.Serialization;

namespace Systems.MiniTimeline.Core
{
    /// <summary>
    /// Serializable project data for Mini Timeline
    /// Contains all track and clip data in JSON-friendly format
    /// </summary>
    [Serializable]
    public class MiniTimelineProject : ISerializationCallbackReceiver
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
        /// All runtime tracks in this project (polymorphic)
        /// </summary>
        [NonSerialized]
        public List<IMiniTrack> tracks = new List<IMiniTrack>();

        [SerializeField]
        private List<SerializedWrapper> _serializedTracks = new List<SerializedWrapper>();

        /// <summary>
        /// Metadata for editor settings
        /// </summary>
        public ProjectMetadata metadata = new ProjectMetadata();

        public void OnBeforeSerialize()
        {
            _serializedTracks.Clear();
            if (tracks == null) return;

            foreach (var track in tracks)
            {
                if (track == null) continue;
                _serializedTracks.Add(new SerializedWrapper
                {
                    type = track.GetType().AssemblyQualifiedName,
                    data = JsonUtility.ToJson(track)
                });
            }
        }

        public void OnAfterDeserialize()
        {
            if (tracks == null) tracks = new List<IMiniTrack>();
            tracks.Clear();
            
            if (_serializedTracks == null) return;

            foreach (var wrapped in _serializedTracks)
            {
                Type type = Type.GetType(wrapped.type);
                if (type != null)
                {
                    IMiniTrack track = (IMiniTrack)JsonUtility.FromJson(wrapped.data, type);
                    tracks.Add(track);
                }
            }
        }
    }
    
    /// <summary>
    /// Project metadata for editor settings
    /// </summary>
    [Serializable]
    public class ProjectMetadata : ISerializationCallbackReceiver
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
        [NonSerialized]
        public Dictionary<string, object> editorData = new Dictionary<string, object>();

        [SerializeField]
        private SerializedMetadataMap _serializedEditorData;

        public void OnBeforeSerialize()
        {
            _serializedEditorData = new SerializedMetadataMap
            {
                keys = new List<string>(),
                values = new List<string>(),
                valueTypes = new List<string>()
            };

            if (editorData == null) return;

            foreach (var kvp in editorData)
            {
                if (kvp.Value == null) continue;

                _serializedEditorData.keys.Add(kvp.Key);
                Type type = kvp.Value.GetType();
                _serializedEditorData.valueTypes.Add(type.AssemblyQualifiedName);

                if (type.IsPrimitive || type == typeof(string))
                {
                    _serializedEditorData.values.Add(kvp.Value.ToString());
                }
                else
                {
                    _serializedEditorData.values.Add(JsonUtility.ToJson(kvp.Value));
                }
            }
        }

        public void OnAfterDeserialize()
        {
            editorData = new Dictionary<string, object>();
            if (_serializedEditorData.keys == null) return;

            for (int i = 0; i < _serializedEditorData.keys.Count; i++)
            {
                string key = _serializedEditorData.keys[i];
                string valStr = _serializedEditorData.values[i];
                string typeName = _serializedEditorData.valueTypes[i];

                Type type = Type.GetType(typeName);
                if (type != null)
                {
                    try
                    {
                        if (type.IsPrimitive || type == typeof(string))
                        {
                            editorData[key] = Convert.ChangeType(valStr, type);
                        }
                        else
                        {
                            editorData[key] = JsonUtility.FromJson(valStr, type);
                        }
                    }
                    catch (Exception)
                    {
                        // Handle potential deserialization errors gracefully
                    }
                }
            }
        }
    }

    /// <summary>
    /// Helper wrapper for serializing polymorphic track instances
    /// </summary>
    [Serializable]
    public class SerializedWrapper
    {
        public string type;
        public string data;
    }

    /// <summary>
    /// Helper container for serializing editor metadata maps
    /// </summary>
    [Serializable]
    public class SerializedMetadataMap
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();
        public List<string> valueTypes = new List<string>();
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