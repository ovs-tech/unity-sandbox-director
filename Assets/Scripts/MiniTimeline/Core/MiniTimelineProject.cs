using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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
            Debug.Log($"MiniTimelineProject.OnBeforeSerialize: tracks={(tracks == null ? 0 : tracks.Count)}");
            _serializedTracks = SerializationUtils.SerializeEnumerable(tracks);
            Debug.Log($"MiniTimelineProject.OnBeforeSerialize: serializedTracks={( _serializedTracks == null ? 0 : _serializedTracks.Count)}");
        }

        public void OnAfterDeserialize()
        {
            Debug.Log($"MiniTimelineProject.OnAfterDeserialize: _serializedTracks={( _serializedTracks == null ? 0 : _serializedTracks.Count)}");
            if (tracks == null) tracks = new List<IMiniTrack>();
            tracks.Clear();

            if (_serializedTracks == null) return;

            var objs = SerializationUtils.DeserializeToObjects(_serializedTracks, "MiniTimelineProject.OnAfterDeserialize");
            int added = 0;
            foreach (var o in objs)
            {
                if (o is IMiniTrack t)
                {
                    tracks.Add(t);
                    added++;
                }
                else
                {
                    Debug.LogWarning($"MiniTimelineProject.OnAfterDeserialize: deserialized object not IMiniTrack (actual={o?.GetType()})");
                }
            }

            Debug.Log($"MiniTimelineProject.OnAfterDeserialize: tracks_added={added}, total_tracks={tracks.Count}");
        }

        // Type resolution moved to TypeResolver helper.
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
            Debug.Log($"ProjectMetadata.OnBeforeSerialize: editorData={(editorData == null ? 0 : editorData.Count)}");
            _serializedEditorData = new SerializedMetadataMap
            {
                keys = new List<string>(),
                values = new List<string>(),
                valueTypes = new List<string>()
            };

            if (editorData == null) return;

            int serialized = 0;
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

                serialized++;
            }

            Debug.Log($"ProjectMetadata.OnBeforeSerialize: serializedEntries={serialized}");
        }

        public void OnAfterDeserialize()
        {
            Debug.Log($"ProjectMetadata.OnAfterDeserialize: serializedKeys={( _serializedEditorData == null || _serializedEditorData.keys == null ? 0 : _serializedEditorData.keys.Count)}");
            editorData = new Dictionary<string, object>();
            if (_serializedEditorData.keys == null) return;

            int restored = 0;
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
                        restored++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"ProjectMetadata.OnAfterDeserialize: failed to deserialize key={key}, type={typeName}, err={ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"ProjectMetadata.OnAfterDeserialize: type not found: {typeName}");
                }
            }

            Debug.Log($"ProjectMetadata.OnAfterDeserialize: restoredEntries={restored}");
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
        public const string TRACK_CINEMACHINE = "CinemachineTrack";
        public const string TRACK_TIME_SCALE = "TimeScaleTrack";
        public const string TRACK_SUB_TIMELINE = "SubTimelineTrack";

        // Asset reference prefixes
        public const string ASSET_ADDRESSABLE = "addr:";
        public const string ASSET_SCENE = "scene:";
        
        // Default frame rate
        public const float DEFAULT_FRAME_RATE = 30f;
        
        // Minimum clip duration
        public const float MIN_CLIP_DURATION = 0.01f;
    }
}