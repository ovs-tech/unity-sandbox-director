using System.Collections.Generic;
using UnityEngine;
using MiniTimeline.Core;
using MiniTimeline.Tracks;

namespace MiniTimeline.UI
{
    /// <summary>
    /// Provides form field definitions for creating different types of clips
    /// Maps track types to their corresponding clip creation forms
    /// </summary>
    public static class ClipFormDefinitions
    {
        /// <summary>
        /// Get form field definitions for creating a clip of the specified track type
        /// </summary>
        public static List<FormFieldDefinition> GetFieldsForTrackType(string trackType)
        {
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_ANIM:
                    return GetAnimationClipFields();
                
                case MiniTimelineConstants.TRACK_ANIMATOR:
                    return GetAnimatorClipFields();
                
                case MiniTimelineConstants.TRACK_MORPH:
                    return GetMorphClipFields();
                
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    return GetMovementClipFields();
                
                case MiniTimelineConstants.TRACK_SIGNAL:
                    return GetSignalClipFields();
                
                default:
                    return GetGenericClipFields();
            }
        }
        
        /// <summary>
        /// Get the display name for a track type
        /// </summary>
        public static string GetTrackTypeDisplayName(string trackType)
        {
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_ANIM:
                    return "Animation Clip";
                case MiniTimelineConstants.TRACK_ANIMATOR:
                    return "Animator Clip";
                case MiniTimelineConstants.TRACK_MORPH:
                    return "Morph Clip";
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    return "Movement Clip";
                case MiniTimelineConstants.TRACK_SIGNAL:
                    return "Signal Clip";
                default:
                    return "Generic Clip";
            }
        }
        
        #region Animation Clip Fields
        
        private static List<FormFieldDefinition> GetAnimationClipFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition("name", "Clip Name", "text", "New Animation Clip")
                {
                    required = true,
                    placeholder = "Enter clip name",
                    tooltip = "Display name for this animation clip"
                },
                
                new FormFieldDefinition("start", "Start Time", "number", 0f)
                {
                    required = true,
                    placeholder = "0.0",
                    tooltip = "When the clip starts on the timeline (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 300f }
                    }
                },
                
                new FormFieldDefinition("duration", "Duration", "number", 1f)
                {
                    required = true,
                    placeholder = "1.0",
                    tooltip = "How long the clip lasts (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0.1f },
                        { "max", 60f }
                    }
                },
                
                new FormFieldDefinition("animationAsset", "Animation Asset", "text", "")
                {
                    required = true,
                    placeholder = "addr:Animations/WalkCycle",
                    tooltip = "Addressable reference to the animation asset"
                },
                
                new FormFieldDefinition("speed", "Speed", "slider", 1f)
                {
                    tooltip = "Playback speed multiplier",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0.1f },
                        { "max", 3f }
                    }
                },
                
                new FormFieldDefinition("fadeIn", "Fade In", "number", 0.2f)
                {
                    tooltip = "Fade in duration (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 5f }
                    }
                },
                
                new FormFieldDefinition("fadeOut", "Fade Out", "number", 0.2f)
                {
                    tooltip = "Fade out duration (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 5f }
                    }
                },
                
                new FormFieldDefinition("loop", "Loop", "toggle", true)
                {
                    tooltip = "Whether the animation should loop during the clip duration"
                }
            };
        }
        
        #endregion
        
        #region Animator Clip Fields
        
        private static List<FormFieldDefinition> GetAnimatorClipFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition("name", "Clip Name", "text", "New Animator Clip")
                {
                    required = true,
                    placeholder = "Enter clip name",
                    tooltip = "Display name for this animator clip"
                },
                
                new FormFieldDefinition("start", "Start Time", "number", 0f)
                {
                    required = true,
                    placeholder = "0.0",
                    tooltip = "When the clip starts on the timeline (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 300f }
                    }
                },
                
                new FormFieldDefinition("duration", "Duration", "number", 1f)
                {
                    required = true,
                    placeholder = "1.0",
                    tooltip = "How long the clip lasts (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0.1f },
                        { "max", 60f }
                    }
                },
                
                new FormFieldDefinition("stateName", "State Name", "text", "")
                {
                    required = true,
                    placeholder = "Walk",
                    tooltip = "Name of the animator state to trigger"
                },
                
                new FormFieldDefinition("layerIndex", "Layer Index", "number", 0)
                {
                    tooltip = "Animator layer index",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0 },
                        { "max", 10 }
                    }
                },
                
                new FormFieldDefinition("normalizedTime", "Normalized Time", "slider", 0f)
                {
                    tooltip = "Starting normalized time for the animation state",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 1f }
                    }
                },
                
                new FormFieldDefinition("fixedDuration", "Fixed Duration", "toggle", false)
                {
                    tooltip = "Whether to use a fixed duration or let the animation play naturally"
                }
            };
        }
        
        #endregion
        
        #region Morph Clip Fields
        
        private static List<FormFieldDefinition> GetMorphClipFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition("name", "Clip Name", "text", "New Morph Clip")
                {
                    required = true,
                    placeholder = "Enter clip name",
                    tooltip = "Display name for this morph clip"
                },
                
                new FormFieldDefinition("start", "Start Time", "number", 0f)
                {
                    required = true,
                    placeholder = "0.0",
                    tooltip = "When the clip starts on the timeline (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 300f }
                    }
                },
                
                new FormFieldDefinition("duration", "Duration", "number", 2f)
                {
                    required = true,
                    placeholder = "2.0",
                    tooltip = "How long the morph transition lasts (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0.1f },
                        { "max", 30f }
                    }
                },
                
                new FormFieldDefinition("blendShapeName", "Blend Shape", "text", "")
                {
                    required = true,
                    placeholder = "Smile",
                    tooltip = "Name of the blend shape to animate"
                },
                
                new FormFieldDefinition("targetWeight", "Target Weight", "slider", 100f)
                {
                    tooltip = "Target weight for the blend shape (0-100)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 100f }
                    }
                },
                
                new FormFieldDefinition("easing", "Easing", "select", "Linear")
                {
                    tooltip = "Easing curve for the morph transition",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Linear", "EaseIn", "EaseOut", "EaseInOut", "Smooth" } }
                    }
                },
                
                new FormFieldDefinition("holdAtEnd", "Hold at End", "toggle", true)
                {
                    tooltip = "Whether to maintain the morph state after the clip ends"
                }
            };
        }
        
        #endregion
        
        #region Movement Clip Fields
        
        private static List<FormFieldDefinition> GetMovementClipFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition("name", "Clip Name", "text", "New Movement Clip")
                {
                    required = true,
                    placeholder = "Enter clip name",
                    tooltip = "Display name for this movement clip"
                },
                
                new FormFieldDefinition("start", "Start Time", "number", 0f)
                {
                    required = true,
                    placeholder = "0.0",
                    tooltip = "When the clip starts on the timeline (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 300f }
                    }
                },
                
                new FormFieldDefinition("duration", "Duration", "number", 3f)
                {
                    required = true,
                    placeholder = "3.0",
                    tooltip = "How long the movement takes (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0.1f },
                        { "max", 60f }
                    }
                },
                
                new FormFieldDefinition("movementType", "Movement Type", "select", "Linear")
                {
                    required = true,
                    tooltip = "Type of movement to perform",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Linear", "Curved", "Spline", "Circular" } }
                    }
                },
                
                new FormFieldDefinition("targetPosition", "Target Position", "text", "0,0,0")
                {
                    placeholder = "x,y,z",
                    tooltip = "Target position in world space (comma-separated)"
                },
                
                new FormFieldDefinition("targetRotation", "Target Rotation", "text", "0,0,0")
                {
                    placeholder = "x,y,z",
                    tooltip = "Target rotation in euler angles (comma-separated)"
                },
                
                new FormFieldDefinition("easing", "Easing", "select", "EaseInOut")
                {
                    tooltip = "Easing curve for the movement",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Linear", "EaseIn", "EaseOut", "EaseInOut", "Bounce", "Elastic" } }
                    }
                },
                
                new FormFieldDefinition("relative", "Relative Movement", "toggle", false)
                {
                    tooltip = "Whether the movement is relative to the current position"
                }
            };
        }
        
        #endregion
        
        #region Signal Clip Fields
        
        private static List<FormFieldDefinition> GetSignalClipFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition("name", "Clip Name", "text", "New Signal")
                {
                    required = true,
                    placeholder = "Enter signal name",
                    tooltip = "Display name for this signal"
                },
                
                new FormFieldDefinition("start", "Start Time", "number", 0f)
                {
                    required = true,
                    placeholder = "0.0",
                    tooltip = "When the signal fires on the timeline (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 300f }
                    }
                },
                
                new FormFieldDefinition("signalType", "Signal Type", "select", "Event")
                {
                    required = true,
                    tooltip = "Type of signal to send",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Event", "Trigger", "Boolean", "Float", "String" } }
                    }
                },
                
                new FormFieldDefinition("signalValue", "Signal Value", "text", "")
                {
                    placeholder = "Signal parameter value",
                    tooltip = "Value to send with the signal (if applicable)"
                },
                
                new FormFieldDefinition("targetObject", "Target Object", "text", "")
                {
                    placeholder = "Binding key for target object",
                    tooltip = "Which bound object should receive this signal"
                },
                
                new FormFieldDefinition("methodName", "Method Name", "text", "")
                {
                    placeholder = "OnSignalReceived",
                    tooltip = "Method to call on the target object"
                },
                
                new FormFieldDefinition("description", "Description", "textarea", "")
                {
                    placeholder = "Optional description of what this signal does",
                    tooltip = "Documentation for this signal"
                }
            };
        }
        
        #endregion
        
        #region Generic Clip Fields
        
        private static List<FormFieldDefinition> GetGenericClipFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition("name", "Clip Name", "text", "New Clip")
                {
                    required = true,
                    placeholder = "Enter clip name",
                    tooltip = "Display name for this clip"
                },
                
                new FormFieldDefinition("start", "Start Time", "number", 0f)
                {
                    required = true,
                    placeholder = "0.0",
                    tooltip = "When the clip starts on the timeline (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0f },
                        { "max", 300f }
                    }
                },
                
                new FormFieldDefinition("duration", "Duration", "number", 1f)
                {
                    required = true,
                    placeholder = "1.0",
                    tooltip = "How long the clip lasts (in seconds)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0.1f },
                        { "max", 60f }
                    }
                },
                
                new FormFieldDefinition("description", "Description", "textarea", "")
                {
                    placeholder = "Optional description of this clip",
                    tooltip = "Documentation for this clip"
                }
            };
        }
        
        #endregion
        
        /// <summary>
        /// Convert form data to clip payload for the specified track type
        /// </summary>
        public static Dictionary<string, object> ConvertFormDataToPayload(Dictionary<string, object> formData, string trackType)
        {
            var payload = new Dictionary<string, object>();
            
            // Common fields that don't go in payload
            var excludeFromPayload = new HashSet<string> { "name", "start", "duration" };
            
            foreach (var kvp in formData)
            {
                if (!excludeFromPayload.Contains(kvp.Key))
                {
                    payload[kvp.Key] = kvp.Value;
                }
            }
            
            // Track-specific payload processing
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    ProcessMovementPayload(payload);
                    break;
                
                case MiniTimelineConstants.TRACK_MORPH:
                    ProcessMorphPayload(payload);
                    break;
            }
            
            return payload;
        }
        
        private static void ProcessMovementPayload(Dictionary<string, object> payload)
        {
            // Convert position/rotation strings to Vector3
            if (payload.ContainsKey("targetPosition") && payload["targetPosition"] is string posStr)
            {
                if (TryParseVector3(posStr, out Vector3 position))
                {
                    payload["targetPosition"] = position;
                }
            }
            
            if (payload.ContainsKey("targetRotation") && payload["targetRotation"] is string rotStr)
            {
                if (TryParseVector3(rotStr, out Vector3 rotation))
                {
                    payload["targetRotation"] = rotation;
                }
            }
        }
        
        private static void ProcessMorphPayload(Dictionary<string, object> payload)
        {
            // Convert target weight to proper range (0-1 instead of 0-100)
            if (payload.ContainsKey("targetWeight") && payload["targetWeight"] is float weight)
            {
                payload["targetWeight"] = weight / 100f;
            }
        }
        
        private static bool TryParseVector3(string str, out Vector3 result)
        {
            result = Vector3.zero;
            
            if (string.IsNullOrEmpty(str))
                return false;
            
            var parts = str.Split(',');
            if (parts.Length != 3)
                return false;
            
            if (float.TryParse(parts[0].Trim(), out float x) &&
                float.TryParse(parts[1].Trim(), out float y) &&
                float.TryParse(parts[2].Trim(), out float z))
            {
                result = new Vector3(x, y, z);
                return true;
            }
            
            return false;
        }
    }
}