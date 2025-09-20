using System;
using System.Collections.Generic;
using UnityEngine;
using MiniTimeline.Core;
using MiniTimeline.Tracks;

namespace MiniTimeline.Serialization
{
    /// <summary>
    /// Factory for creating track instances from serialized data
    /// Handles the mapping between track type names and actual track classes
    /// </summary>
    public static class TrackFactory
    {
        private static readonly Dictionary<string, Func<TrackData, IMiniTrack>> trackCreators 
            = new Dictionary<string, Func<TrackData, IMiniTrack>>();
        
        static TrackFactory()
        {
            RegisterDefaultTrackTypes();
        }
        
        /// <summary>
        /// Register default track types
        /// </summary>
        private static void RegisterDefaultTrackTypes()
        {
            RegisterTrackType(MiniTimelineConstants.TRACK_ANIM, CreateAnimTrack);
            RegisterTrackType(MiniTimelineConstants.TRACK_MORPH, CreateMorphTrack);
            RegisterTrackType(MiniTimelineConstants.TRACK_MOVEMENT, CreateMovementTrack);
            RegisterTrackType(MiniTimelineConstants.TRACK_ANIMATOR, CreateAnimatorTrack);
            RegisterTrackType(MiniTimelineConstants.TRACK_EVENT, CreateEventTrack);
            // Add more track types as they are implemented
        }
        
        /// <summary>
        /// Register a track type creator
        /// </summary>
        /// <param name="typeName">Track type name</param>
        /// <param name="creator">Creator function</param>
        public static void RegisterTrackType(string typeName, Func<TrackData, IMiniTrack> creator)
        {
            trackCreators[typeName] = creator;
        }
        
        /// <summary>
        /// Create a track instance from track data
        /// </summary>
        /// <param name="data">Serialized track data</param>
        /// <returns>Track instance or null if type not supported</returns>
        public static IMiniTrack CreateTrack(TrackData data)
        {
            if (trackCreators.TryGetValue(data.type, out var creator))
            {
                return creator(data);
            }
            
            Debug.LogWarning($"[TrackFactory] Unsupported track type: {data.type}");
            return null;
        }
        
        /// <summary>
        /// Get all registered track type names
        /// </summary>
        /// <returns>Collection of track type names</returns>
        public static IEnumerable<string> GetRegisteredTrackTypes()
        {
            return trackCreators.Keys;
        }
        
        #region Track Creators
        
        private static IMiniTrack CreateAnimTrack(TrackData data)
        {
            var track = new AnimTrack
            {
                Id = data.id,
                BindKey = data.bindKey,
                Enabled = data.enabled
            };
            
            // Convert clip data to AnimClips
            var clips = new List<AnimClip>();
            foreach (var clipData in data.clips)
            {
                var clip = DeserializeAnimClip(clipData);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }
            
            track.SetClips(clips);
            return track;
        }
        
        private static IMiniTrack CreateMorphTrack(TrackData data)
        {
            var track = new MorphTrack
            {
                Id = data.id,
                BindKey = data.bindKey,
                Enabled = data.enabled
            };
            
            // Convert clip data to morph clips (mixed types)
            var clips = new List<IMorphClip>();
            foreach (var clipData in data.clips)
            {
                var clip = DeserializeMorphClip(clipData);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }
            
            track.SetClips(clips);
            return track;
        }
        
        private static IMiniTrack CreateMovementTrack(TrackData data)
        {
            var track = new MovementTrack
            {
                Id = data.id,
                BindKey = data.bindKey,
                Enabled = data.enabled
            };
            
            // Convert clip data to CameraClips
            var clips = new List<MovementClip>();
            foreach (var clipData in data.clips)
            {
                var clip = DeserializeCameraClip(clipData);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }
            
            track.SetClips(clips);
            return track;
        }
        
        private static IMiniTrack CreateEventTrack(TrackData data)
        {
            var track = new EventTrack
            {
                Id = data.id,
                BindKey = data.bindKey,
                Enabled = data.enabled
            };
            
            // Convert clip data to SignalClips
            var clips = new List<SignalClip>();
            foreach (var clipData in data.clips)
            {
                var clip = DeserializeSignalClip(clipData);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }
            
            track.SetClips(clips);
            return track;
        }
        
        private static IMiniTrack CreateAnimatorTrack(TrackData data)
        {
            var track = new AnimatorTrack
            {
                Id = data.id,
                BindKey = data.bindKey,
                Enabled = data.enabled
            };
            
            // Convert clip data to AnimatorClips
            var clips = new List<AnimatorClip>();
            foreach (var clipData in data.clips)
            {
                var clip = DeserializeAnimatorClip(clipData);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }
            
            track.SetClips(clips);
            return track;
        }
        
        #endregion
        
        #region Clip Deserializers
        
        private static AnimClip DeserializeAnimClip(ClipData data)
        {
            try
            {
                var clip = new AnimClip
                {
                    Id = data.id,
                    Start = data.start,
                    Duration = data.duration
                };
                
                // Deserialize payload
                if (data.payload.TryGetValue("animationAsset", out var animAsset))
                    clip.animationAsset = animAsset.ToString();
                
                if (data.payload.TryGetValue("speed", out var speed))
                    clip.speed = Convert.ToSingle(speed);
                
                if (data.payload.TryGetValue("wrapMode", out var wrapMode))
                    Enum.TryParse<AnimWrapMode>(wrapMode.ToString(), out clip.wrapMode);
                
                if (data.payload.TryGetValue("layer", out var layer))
                    clip.layer = Convert.ToInt32(layer);
                
                if (data.payload.TryGetValue("fadeIn", out var fadeIn))
                    clip.fadeIn = Convert.ToSingle(fadeIn);
                
                if (data.payload.TryGetValue("fadeOut", out var fadeOut))
                    clip.fadeOut = Convert.ToSingle(fadeOut);
                
                if (data.payload.TryGetValue("weight", out var weight))
                    clip.weight = Convert.ToSingle(weight);
                
                if (data.payload.TryGetValue("clipOffset", out var offset))
                    clip.clipOffset = Convert.ToSingle(offset);
                
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrackFactory] Error deserializing AnimClip: {e.Message}");
                return null;
            }
        }
        
        private static IMorphClip DeserializeMorphClip(ClipData data)
        {
            try
            {
                // Determine clip type from payload
                bool isKeyClip = data.payload.ContainsKey("keys");
                bool isCurveClip = data.payload.ContainsKey("channels");
                
                if (isKeyClip)
                {
                    return DeserializeMorphKeyClip(data);
                }
                else if (isCurveClip)
                {
                    return DeserializeMorphCurveClip(data);
                }
                else
                {
                    Debug.LogWarning($"[TrackFactory] Unknown morph clip type for clip {data.id}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrackFactory] Error deserializing MorphClip: {e.Message}");
                return null;
            }
        }
        
        private static MorphKeyClip DeserializeMorphKeyClip(ClipData data)
        {
            var clip = new MorphKeyClip
            {
                Id = data.id,
                Start = data.start,
                Duration = data.duration
            };
            
            // Deserialize basic properties
            if (data.payload.TryGetValue("blendMode", out var blendMode))
                Enum.TryParse<MorphBlendMode>(blendMode.ToString(), out clip.blendMode);
            
            if (data.payload.TryGetValue("priority", out var priority))
                clip.priority = Convert.ToInt32(priority);
            
            if (data.payload.TryGetValue("weight", out var weight))
                clip.weight = Convert.ToSingle(weight);
            
            // Deserialize keys
            if (data.payload.TryGetValue("keys", out var keysObj) && keysObj is List<object> keysList)
            {
                clip.keys = new List<MorphKey>();
                foreach (var keyObj in keysList)
                {
                    if (keyObj is Dictionary<string, object> keyDict)
                    {
                        var key = DeserializeMorphKey(keyDict);
                        clip.keys.Add(key);
                    }
                }
            }
            
            return clip;
        }
        
        private static MorphCurveClip DeserializeMorphCurveClip(ClipData data)
        {
            var clip = new MorphCurveClip
            {
                Id = data.id,
                Start = data.start,
                Duration = data.duration
            };
            
            // Deserialize basic properties
            if (data.payload.TryGetValue("blendMode", out var blendMode))
                Enum.TryParse<MorphBlendMode>(blendMode.ToString(), out clip.blendMode);
            
            if (data.payload.TryGetValue("priority", out var priority))
                clip.priority = Convert.ToInt32(priority);
            
            if (data.payload.TryGetValue("weight", out var weight))
                clip.weight = Convert.ToSingle(weight);
            
            // Deserialize channels
            if (data.payload.TryGetValue("channels", out var channelsObj) && channelsObj is List<object> channelsList)
            {
                clip.channels = new List<MorphCurveClip.CurveChannel>();
                foreach (var channelObj in channelsList)
                {
                    if (channelObj is Dictionary<string, object> channelDict)
                    {
                        var channel = DeserializeCurveChannel(channelDict);
                        clip.channels.Add(channel);
                    }
                }
            }
            
            return clip;
        }
        
        private static SignalClip DeserializeSignalClip(ClipData data)
        {
            var clip = new SignalClip
            {
                Id = data.id,
                Start = data.start,
                Duration = 0f // Signals always have zero duration
            };
            
            if (data.payload.TryGetValue("eventId", out var eventId))
                clip.eventId = eventId.ToString();
            
            if (data.payload.TryGetValue("payload", out var payload))
                clip.payload = payload.ToString();
            
            if (data.payload.TryGetValue("edge", out var edge))
                Enum.TryParse<EventTriggerEdge>(edge.ToString(), out clip.edge);
            
            if (data.payload.TryGetValue("fireOnScrub", out var fireOnScrub))
                clip.fireOnScrub = Convert.ToBoolean(fireOnScrub);
            
            if (data.payload.TryGetValue("color", out var colorObj))
            {
                if (ColorUtility.TryParseHtmlString(colorObj.ToString(), out Color color))
                    clip.color = color;
            }
            
            return clip;
        }
        
        private static MorphKey DeserializeMorphKey(Dictionary<string, object> keyDict)
        {
            var key = new MorphKey();
            
            if (keyDict.TryGetValue("id", out var id))
                key.id = id.ToString();
            
            if (keyDict.TryGetValue("startValue", out var startValue))
                key.startValue = Convert.ToSingle(startValue);
            
            if (keyDict.TryGetValue("endValue", out var endValue))
                key.endValue = Convert.ToSingle(endValue);
            
            if (keyDict.TryGetValue("curve", out var curveObj))
                key.curve = DeserializeAnimationCurve(curveObj);
            
            return key;
        }
        
        private static MorphCurveClip.CurveChannel DeserializeCurveChannel(Dictionary<string, object> channelDict)
        {
            var channel = new MorphCurveClip.CurveChannel();
            
            if (channelDict.TryGetValue("id", out var id))
                channel.id = id.ToString();
            
            if (channelDict.TryGetValue("multiplier", out var multiplier))
                channel.multiplier = Convert.ToSingle(multiplier);
            
            if (channelDict.TryGetValue("curve", out var curveObj))
                channel.curve = DeserializeAnimationCurve(curveObj);
            
            return channel;
        }
        
        private static AnimationCurve DeserializeAnimationCurve(object curveObj)
        {
            // Simple curve deserialization - in practice you might want a more robust system
            if (curveObj is Dictionary<string, object> curveDict)
            {
                var curve = new AnimationCurve();
                
                if (curveDict.TryGetValue("keys", out var keysObj) && keysObj is List<object> keysList)
                {
                    foreach (var keyObj in keysList)
                    {
                        if (keyObj is Dictionary<string, object> keyDict)
                        {
                            float time = keyDict.TryGetValue("time", out var t) ? Convert.ToSingle(t) : 0f;
                            float value = keyDict.TryGetValue("value", out var v) ? Convert.ToSingle(v) : 0f;
                            float inTangent = keyDict.TryGetValue("inTangent", out var inT) ? Convert.ToSingle(inT) : 0f;
                            float outTangent = keyDict.TryGetValue("outTangent", out var outT) ? Convert.ToSingle(outT) : 0f;
                            
                            var keyframe = new Keyframe(time, value, inTangent, outTangent);
                            curve.AddKey(keyframe);
                        }
                    }
                }
                
                return curve;
            }
            
            // Fallback - linear curve
            return AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        
        private static MovementClip DeserializeCameraClip(ClipData data)
        {
            try
            {
                var clip = new MovementClip
                {
                    Id = data.id,
                    Start = data.start,
                    Duration = data.duration
                };
                
                // Deserialize position data
                if (data.payload.TryGetValue("hasPosition", out var hasPos))
                    clip.hasPosition = Convert.ToBoolean(hasPos);
                
                if (data.payload.TryGetValue("startPosition", out var startPos))
                    clip.startPosition = DeserializeVector3(startPos);
                
                if (data.payload.TryGetValue("endPosition", out var endPos))
                    clip.endPosition = DeserializeVector3(endPos);
                
                // Deserialize rotation data
                if (data.payload.TryGetValue("hasRotation", out var hasRot))
                    clip.hasRotation = Convert.ToBoolean(hasRot);
                
                if (data.payload.TryGetValue("startRotation", out var startRot))
                    clip.startRotation = DeserializeQuaternion(startRot);
                
                if (data.payload.TryGetValue("endRotation", out var endRot))
                    clip.endRotation = DeserializeQuaternion(endRot);
                
                // Deserialize field of view data
                if (data.payload.TryGetValue("hasFieldOfView", out var hasFOV))
                    clip.hasFieldOfView = Convert.ToBoolean(hasFOV);
                
                if (data.payload.TryGetValue("startFieldOfView", out var startFOV))
                    clip.startFieldOfView = Convert.ToSingle(startFOV);
                
                if (data.payload.TryGetValue("endFieldOfView", out var endFOV))
                    clip.endFieldOfView = Convert.ToSingle(endFOV);
                
                // Deserialize animation curve
                if (data.payload.TryGetValue("animationCurve", out var animCurve))
                    Enum.TryParse<CameraAnimationCurve>(animCurve.ToString(), out clip.animationCurve);
                
                // Deserialize fade values
                if (data.payload.TryGetValue("fadeIn", out var fadeIn))
                    clip.fadeIn = Convert.ToSingle(fadeIn);
                
                if (data.payload.TryGetValue("fadeOut", out var fadeOut))
                    clip.fadeOut = Convert.ToSingle(fadeOut);
                
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrackFactory] Error deserializing CameraClip: {e.Message}");
                return null;
            }
        }
        
        private static AnimatorClip DeserializeAnimatorClip(ClipData data)
        {
            try
            {
                var clip = new AnimatorClip
                {
                    Id = data.id,
                    Start = data.start,
                    Duration = data.duration
                };
                
                // Deserialize fade values
                if (data.payload.TryGetValue("fadeIn", out var fadeIn))
                    clip.fadeIn = Convert.ToSingle(fadeIn);
                
                if (data.payload.TryGetValue("fadeOut", out var fadeOut))
                    clip.fadeOut = Convert.ToSingle(fadeOut);
                
                // Deserialize blend mode
                if (data.payload.TryGetValue("blendMode", out var blendMode))
                    Enum.TryParse<AnimatorBlendMode>(blendMode.ToString(), out clip.blendMode);
                
                // Deserialize parameter keys
                if (data.payload.TryGetValue("parameterKeys", out var paramKeysData))
                {
                    DeserializeParameterKeys(clip, paramKeysData);
                }
                
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrackFactory] Error deserializing AnimatorClip: {e.Message}");
                return null;
            }
        }
        
        private static void DeserializeParameterKeys(AnimatorClip clip, object paramKeysData)
        {
            // For now, we'll implement a simple parameter key deserialization
            // In a real implementation, this would parse a more complex structure
            // Example format: "paramName:type:startValue:endValue"
            
            string paramKeysStr = paramKeysData.ToString();
            if (string.IsNullOrEmpty(paramKeysStr)) return;
            
            string[] keyEntries = paramKeysStr.Split(';');
            foreach (string keyEntry in keyEntries)
            {
                if (string.IsNullOrEmpty(keyEntry)) continue;
                
                string[] parts = keyEntry.Split(':');
                if (parts.Length >= 4)
                {
                    string paramName = parts[0];
                    if (Enum.TryParse<AnimatorControllerParameterType>(parts[1], out var paramType))
                    {
                        var paramKey = new AnimatorParameterKey
                        {
                            parameterName = paramName,
                            parameterType = paramType
                        };
                        
                        // Set values based on parameter type
                        switch (paramType)
                        {
                            case AnimatorControllerParameterType.Float:
                                if (float.TryParse(parts[2], out float startFloat) &&
                                    float.TryParse(parts[3], out float endFloat))
                                {
                                    paramKey.startFloatValue = startFloat;
                                    paramKey.endFloatValue = endFloat;
                                }
                                break;
                                
                            case AnimatorControllerParameterType.Int:
                                if (int.TryParse(parts[2], out int startInt) &&
                                    int.TryParse(parts[3], out int endInt))
                                {
                                    paramKey.startIntValue = startInt;
                                    paramKey.endIntValue = endInt;
                                }
                                break;
                                
                            case AnimatorControllerParameterType.Bool:
                                if (bool.TryParse(parts[2], out bool startBool) &&
                                    bool.TryParse(parts[3], out bool endBool))
                                {
                                    paramKey.startBoolValue = startBool;
                                    paramKey.endBoolValue = endBool;
                                }
                                break;
                                
                            case AnimatorControllerParameterType.Trigger:
                                if (bool.TryParse(parts[2], out bool triggerValue))
                                {
                                    paramKey.triggerValue = triggerValue;
                                }
                                break;
                        }
                        
                        // Set curve type if provided
                        if (parts.Length > 4 && 
                            Enum.TryParse<AnimatorParameterCurve>(parts[4], out var curveType))
                        {
                            paramKey.curveType = curveType;
                        }
                        
                        clip.AddParameterKey(paramKey);
                    }
                }
            }
        }
        
        private static Vector3 DeserializeVector3(object value)
        {
            // Simple Vector3 deserialization - assumes format "x,y,z"
            string str = value.ToString();
            string[] parts = str.Split(',');
            if (parts.Length == 3)
            {
                if (float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z))
                {
                    return new Vector3(x, y, z);
                }
            }
            return Vector3.zero;
        }
        
        private static Quaternion DeserializeQuaternion(object value)
        {
            // Simple Quaternion deserialization - assumes format "x,y,z,w"
            string str = value.ToString();
            string[] parts = str.Split(',');
            if (parts.Length == 4)
            {
                if (float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z) &&
                    float.TryParse(parts[3], out float w))
                {
                    return new Quaternion(x, y, z, w);
                }
            }
            return Quaternion.identity;
        }
        
        #endregion
    }
}