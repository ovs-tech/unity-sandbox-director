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
            RegisterTrackType(MiniTimelineConstants.TRACK_SIGNAL, CreateSignalTrack);
            RegisterTrackType(MiniTimelineConstants.TRACK_UMA_WARDROBE, CreateUmaWardrobeTrack);
            RegisterTrackType(MiniTimelineConstants.TRACK_UMA_EXPRESSION, CreateUmaExpressionTrack);
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
        
        /// <summary>
        /// Create a clip instance from form data for the specified track type
        /// </summary>
        /// <param name="trackType">The track type</param>
        /// <param name="formData">Form data containing clip properties</param>
        /// <returns>Clip instance or null if type not supported</returns>
        public static IMiniClip CreateClipFromFormData(string trackType, Dictionary<string, object> formData)
        {
            // Extract common fields
            string clipId = System.Guid.NewGuid().ToString();
            float startTime = formData.ContainsKey("start") ? Convert.ToSingle(formData["start"]) : 0f;
            float duration = formData.ContainsKey("duration") ? Convert.ToSingle(formData["duration"]) : 1f;
            
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_ANIM:
                    return CreateAnimClipFromForm(clipId, startTime, duration, formData);
                case MiniTimelineConstants.TRACK_MORPH:
                    return CreateMorphClipFromForm(clipId, startTime, duration, formData);
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    return CreateMovementClipFromForm(clipId, startTime, duration, formData);
                case MiniTimelineConstants.TRACK_ANIMATOR:
                    return CreateAnimatorClipFromForm(clipId, startTime, duration, formData);
                case MiniTimelineConstants.TRACK_SIGNAL:
                    return CreateSignalClipFromForm(clipId, startTime, duration, formData);
                case MiniTimelineConstants.TRACK_UMA_WARDROBE:
                    return CreateUmaWardrobeClipFromForm(clipId, startTime, duration, formData);
                case MiniTimelineConstants.TRACK_UMA_EXPRESSION:
                    return CreateUmaExpressionClipFromForm(clipId, startTime, duration, formData);
                default:
                    Debug.LogWarning($"[TrackFactory] Clip creation from form not implemented for track type: {trackType}");
                    return null;
            }
        }
        
        #region Track Creators
        
        private static IMiniTrack CreateUmaExpressionTrack(TrackData data)
        {
            var track = new UMAExpressionTrack
            {
                Id = data.id,
                BindKey = data.bindKey,
                Enabled = data.enabled
            };

            var clips = new List<UMAExpressionClip>();
            foreach (var clipData in data.clips)
            {
                var clip = DeserializeUmaExpressionClip(clipData);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            track.SetClips(clips);
            return track;
        }

        private static IMiniTrack CreateUmaWardrobeTrack(TrackData data)
        {
            var track = new UmaWardrobeTrack
            {
                Id = data.id,
                BindKey = data.bindKey,
                Enabled = data.enabled
            };

            var clips = new List<UmaWardrobeClip>();
            foreach (var clipData in data.clips)
            {
                var clip = DeserializeUmaWardrobeClip(clipData);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            track.SetClips(clips);
            return track;
        }

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
        
    private static IMiniTrack CreateSignalTrack(TrackData data)
        {
            var track = new SignalTrack
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
        
        #region Clip Form Creators
        
        private static AnimClip CreateAnimClipFromForm(string clipId, float startTime, float duration, Dictionary<string, object> formData)
        {
            var animClip = new AnimClip
            {
                Id = clipId,
                Start = startTime,
                Duration = duration
            };
            
            // Set properties from form data
            if (formData.ContainsKey("animationAsset"))
                animClip.animationAsset = formData["animationAsset"].ToString();
                
            if (formData.ContainsKey("speed") && float.TryParse(formData["speed"].ToString(), out float speed))
                animClip.speed = speed;
                
            if (formData.ContainsKey("fadeIn") && float.TryParse(formData["fadeIn"].ToString(), out float fadeIn))
                animClip.fadeIn = fadeIn;
                
            if (formData.ContainsKey("fadeOut") && float.TryParse(formData["fadeOut"].ToString(), out float fadeOut))
                animClip.fadeOut = fadeOut;
                
            if (formData.ContainsKey("weight") && float.TryParse(formData["weight"].ToString(), out float weight))
                animClip.weight = weight;
                
            if (formData.ContainsKey("layer") && int.TryParse(formData["layer"].ToString(), out int layer))
                animClip.layer = layer;
                
            if (formData.ContainsKey("clipOffset") && float.TryParse(formData["clipOffset"].ToString(), out float offset))
                animClip.clipOffset = offset;
                
            // Handle loop checkbox as wrapMode enum
            if (formData.ContainsKey("loop") && formData["loop"] is bool loop)
                animClip.wrapMode = loop ? AnimWrapMode.Loop : AnimWrapMode.Once;
            
            return animClip;
        }
        
        private static IMorphClip CreateMorphClipFromForm(string clipId, float startTime, float duration, Dictionary<string, object> formData)
        {
            // Create a basic MorphKeyClip
            var morphClip = new MorphKeyClip
            {
                Id = clipId,
                Start = startTime,
                Duration = duration,
                keys = new List<MorphKey>()
            };
            
            // Set properties from form data
            if (formData.ContainsKey("blendMode") && Enum.TryParse<MorphBlendMode>(formData["blendMode"].ToString(), out var blendMode))
                morphClip.blendMode = blendMode;
                
            if (formData.ContainsKey("priority") && int.TryParse(formData["priority"].ToString(), out int priority))
                morphClip.priority = priority;
                
            if (formData.ContainsKey("weight") && float.TryParse(formData["weight"].ToString(), out float weight))
                morphClip.weight = weight;
            
            if (formData.ContainsKey("blendShapeName") && formData.ContainsKey("targetWeight"))
            {
                string blendShapeName = formData["blendShapeName"].ToString();
                float targetWeight = Convert.ToSingle(formData["targetWeight"]);
                
                morphClip.keys.Add(new MorphKey
                {
                    id = blendShapeName,
                    startValue = 0f,
                    endValue = targetWeight,
                    curve = AnimationCurve.Linear(0f, 0f, 1f, 1f)
                });
            }
            
            return morphClip;
        }
        
        private static MovementClip CreateMovementClipFromForm(string clipId, float startTime, float duration, Dictionary<string, object> formData)
        {
            var movementClip = new MovementClip
            {
                Id = clipId,
                Start = startTime,
                Duration = duration
            };
            
            // Position data
            if (formData.ContainsKey("hasPosition") && formData["hasPosition"] is bool hasPos)
                movementClip.hasPosition = hasPos;
                
            if (formData.ContainsKey("startPosX") && formData.ContainsKey("startPosY") && formData.ContainsKey("startPosZ"))
            {
                float x = Convert.ToSingle(formData["startPosX"]);
                float y = Convert.ToSingle(formData["startPosY"]);
                float z = Convert.ToSingle(formData["startPosZ"]);
                movementClip.startPosition = new Vector3(x, y, z);
            }
            
            if (formData.ContainsKey("endPosX") && formData.ContainsKey("endPosY") && formData.ContainsKey("endPosZ"))
            {
                float x = Convert.ToSingle(formData["endPosX"]);
                float y = Convert.ToSingle(formData["endPosY"]);
                float z = Convert.ToSingle(formData["endPosZ"]);
                movementClip.endPosition = new Vector3(x, y, z);
            }
            
            // Rotation data
            if (formData.ContainsKey("hasRotation") && formData["hasRotation"] is bool hasRot)
                movementClip.hasRotation = hasRot;
                
            if (formData.ContainsKey("startRotX") && formData.ContainsKey("startRotY") && formData.ContainsKey("startRotZ"))
            {
                float x = Convert.ToSingle(formData["startRotX"]);
                float y = Convert.ToSingle(formData["startRotY"]);
                float z = Convert.ToSingle(formData["startRotZ"]);
                movementClip.startRotation = Quaternion.Euler(x, y, z);
            }
            
            if (formData.ContainsKey("endRotX") && formData.ContainsKey("endRotY") && formData.ContainsKey("endRotZ"))
            {
                float x = Convert.ToSingle(formData["endRotX"]);
                float y = Convert.ToSingle(formData["endRotY"]);
                float z = Convert.ToSingle(formData["endRotZ"]);
                movementClip.endRotation = Quaternion.Euler(x, y, z);
            }
            
            // Field of view data
            if (formData.ContainsKey("hasFieldOfView") && formData["hasFieldOfView"] is bool hasFOV)
                movementClip.hasFieldOfView = hasFOV;
                
            if (formData.ContainsKey("startFOV") && float.TryParse(formData["startFOV"].ToString(), out float startFOV))
                movementClip.startFieldOfView = startFOV;
                
            if (formData.ContainsKey("endFOV") && float.TryParse(formData["endFOV"].ToString(), out float endFOV))
                movementClip.endFieldOfView = endFOV;
            
            // Animation curve
            if (formData.ContainsKey("animationCurve") && Enum.TryParse<CameraAnimationCurve>(formData["animationCurve"].ToString(), out var animCurve))
                movementClip.animationCurve = animCurve;
            
            // Fade values
            if (formData.ContainsKey("fadeIn") && float.TryParse(formData["fadeIn"].ToString(), out float fadeIn))
                movementClip.fadeIn = fadeIn;
                
            if (formData.ContainsKey("fadeOut") && float.TryParse(formData["fadeOut"].ToString(), out float fadeOut))
                movementClip.fadeOut = fadeOut;
            
            return movementClip;
        }
        
        private static AnimatorClip CreateAnimatorClipFromForm(string clipId, float startTime, float duration, Dictionary<string, object> formData)
        {
            var animatorClip = new AnimatorClip
            {
                Id = clipId,
                Start = startTime,
                Duration = duration
            };
            
            // Fade values
            if (formData.ContainsKey("fadeIn") && float.TryParse(formData["fadeIn"].ToString(), out float fadeIn))
                animatorClip.fadeIn = fadeIn;
                
            if (formData.ContainsKey("fadeOut") && float.TryParse(formData["fadeOut"].ToString(), out float fadeOut))
                animatorClip.fadeOut = fadeOut;
            
            // Blend mode
            if (formData.ContainsKey("blendMode") && Enum.TryParse<AnimatorBlendMode>(formData["blendMode"].ToString(), out var blendMode))
                animatorClip.blendMode = blendMode;
            
            // Handle parameter keys from form data
            // This could be extended to handle more complex parameter key creation from form
            
            return animatorClip;
        }
        
        private static SignalClip CreateSignalClipFromForm(string clipId, float startTime, float duration, Dictionary<string, object> formData)
        {
            var signalClip = new SignalClip
            {
                Id = clipId,
                Start = startTime,
                Duration = 0f // Signals always have zero duration
            };
            
            if (formData.ContainsKey("eventId"))
                signalClip.eventId = formData["eventId"].ToString();
            
            if (formData.ContainsKey("payload"))
                signalClip.payload = formData["payload"].ToString();
            
            if (formData.ContainsKey("edge") && Enum.TryParse<EventTriggerEdge>(formData["edge"].ToString(), out var edge))
                signalClip.edge = edge;
            
            if (formData.ContainsKey("fireOnScrub") && formData["fireOnScrub"] is bool fireOnScrub)
                signalClip.fireOnScrub = fireOnScrub;
            
            if (formData.ContainsKey("color"))
            {
                string colorStr = formData["color"].ToString();
                if (ColorUtility.TryParseHtmlString(colorStr, out Color color))
                    signalClip.color = color;
            }
            
            return signalClip;
        }
        
        private static UmaWardrobeClip CreateUmaWardrobeClipFromForm(string clipId, float startTime, float duration, Dictionary<string, object> formData)
        {
            var umaClip = new UmaWardrobeClip
            {
                Id = clipId,
                Start = startTime,
                Duration = duration
            };

            if (formData.ContainsKey("wardrobeJson"))
                umaClip.wardrobeJson = formData["wardrobeJson"].ToString();

            return umaClip;
        }

        private static UMAExpressionClip CreateUmaExpressionClipFromForm(string clipId, float startTime, float duration, Dictionary<string, object> formData)
        {
            var expressionClip = new UMAExpressionClip
            {
                Id = clipId,
                Start = startTime,
                Duration = duration
            };

            if (formData.ContainsKey("expression"))
                expressionClip.expression = formData["expression"].ToString();

            return expressionClip;
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

        private static UMAExpressionClip DeserializeUmaExpressionClip(ClipData data)
        {
            try
            {
                var clip = new UMAExpressionClip
                {
                    Id = data.id,
                    Start = data.start,
                    Duration = data.duration
                };

                if (data.payload.TryGetValue("expression", out var expression))
                {
                    clip.expression = expression.ToString();
                }

                // Note: blendCurve is not serialized/deserialized for simplicity.
                // It will use the default value from the UMAExpressionClip class.

                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrackFactory] Error deserializing UMAExpressionClip: {e.Message}");
                return null;
            }
        }

        private static UmaWardrobeClip DeserializeUmaWardrobeClip(ClipData data)
        {
            try
            {
                var clip = new UmaWardrobeClip
                {
                    Id = data.id,
                    Start = data.start,
                    Duration = data.duration
                };

                if (data.payload.TryGetValue("wardrobeJson", out var json))
                {
                    clip.wardrobeJson = json.ToString();
                }

                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrackFactory] Error deserializing UmaWardrobeClip: {e.Message}");
                return null;
            }
        }
        
        #endregion
    }
}