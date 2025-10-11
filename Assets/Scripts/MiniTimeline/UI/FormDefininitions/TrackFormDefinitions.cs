using System.Collections.Generic;
using MiniTimeline.Core;
using Core.UI.FormSubmit;

namespace MiniTimeline.UI.FormDefinitions
{
    /// <summary>
    /// Provides form field definitions for track settings and configuration
    /// Maps track types to their corresponding settings forms
    /// </summary>
    public static class TrackFormDefinitions
    {
        /// <summary>
        /// Get form field definitions for track settings based on track type
        /// </summary>
        public static List<FormFieldDefinition> GetTrackSettingsFields(string trackType)
        {
            var commonFields = GetCommonTrackFields();
            var specificFields = GetTrackSpecificFields(trackType);
            
            // Combine common and specific fields
            var allFields = new List<FormFieldDefinition>(commonFields);
            allFields.AddRange(specificFields);
            
            return allFields;
        }
        
        /// <summary>
        /// Get the display name for a track type
        /// </summary>
        public static string GetTrackTypeDisplayName(string trackType)
        {
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_ANIM:
                    return "Animation Track";
                case MiniTimelineConstants.TRACK_ANIMATOR:
                    return "Animator Track";
                case MiniTimelineConstants.TRACK_MORPH:
                    return "Morph Track";
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    return "Movement Track";
                case MiniTimelineConstants.TRACK_SIGNAL:
                    return "Signal Track";
                case MiniTimelineConstants.TRACK_UMA_WARDROBE:
                    return "UMA Wardrobe Track";
                default:
                    return "Generic Track";
            }
        }
        
        /// <summary>
        /// Get available track types for track creation
        /// </summary>
        public static List<string> GetAvailableTrackTypes()
        {
            return new List<string>
            {
                MiniTimelineConstants.TRACK_ANIM,
                MiniTimelineConstants.TRACK_ANIMATOR,
                MiniTimelineConstants.TRACK_MORPH,
                MiniTimelineConstants.TRACK_MOVEMENT,
                MiniTimelineConstants.TRACK_SIGNAL,
                MiniTimelineConstants.TRACK_UMA_WARDROBE,
                MiniTimelineConstants.TRACK_UMA_EXPRESSION
            };
        }
        
        /// <summary>
        /// Get form fields for creating a new track
        /// </summary>
        public static List<FormFieldDefinition> GetCreateTrackFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "trackType",
                    type = "selectbox",
                    label = "Track Type",
                    required = true,
                    options = new Dictionary<string, object>
                    {
                        { "items", GetAvailableTrackTypes() }
                    },
                    defaultValue = MiniTimelineConstants.TRACK_ANIM,
                    tooltip = "Select the type of track to create"
                },
                
                new FormFieldDefinition
                {
                    name = "trackName",
                    type = "text",
                    label = "Track Name",
                    required = true,
                    placeholder = "Enter track name...",
                    defaultValue = "New Track",
                    tooltip = "Display name for this track"
                },
                
                new FormFieldDefinition
                {
                    name = "bindKey",
                    type = "selectbox",
                    label = "Bind Key",
                    required = false,
                    tooltip = "Select a binding key to associate this track with scene objects",
                    options = new Dictionary<string, object>
                    {
                        { "items", GetAvailableBindingKeys() },
                        { "allowCustom", true },
                        { "placeholder", "Select or enter binding key..." }
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "enabled",
                    type = "toggle",
                    label = "Enabled",
                    required = false,
                    defaultValue = true,
                    tooltip = "Whether this track should be enabled by default"
                }
            };
        }
        
        #region Common Track Fields
        
        /// <summary>
        /// Get common settings fields that apply to all track types
        /// </summary>
        private static List<FormFieldDefinition> GetCommonTrackFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "trackName",
                    type = "text",
                    label = "Track Name",
                    required = true,
                    placeholder = "Enter track name...",
                    tooltip = "Display name for this track"
                },
                
                new FormFieldDefinition
                {
                    name = "bindKey",
                    type = "selectbox",
                    label = "Bind Key",
                    required = false,
                    tooltip = "Select a binding key to associate this track with scene objects",
                    options = new Dictionary<string, object>
                    {
                        { "allowCustom", true },
                        { "placeholder", "Select or enter binding key..." }
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "enabled",
                    type = "toggle",
                    label = "Enabled",
                    required = false,
                    defaultValue = true,
                    tooltip = "Enable or disable this track"
                },
                
                new FormFieldDefinition
                {
                    name = "trackOrder",
                    type = "number",
                    label = "Track Order",
                    required = false,
                    defaultValue = 0,
                    tooltip = "Display order of this track (lower numbers appear first)",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0 },
                        { "max", 100 }
                    }
                }
            };
        }
        
        #endregion
        
        #region Track-Specific Fields
        
        /// <summary>
        /// Get track-specific settings fields based on track type
        /// </summary>
        private static List<FormFieldDefinition> GetTrackSpecificFields(string trackType)
        {
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_ANIM:
                    return GetAnimTrackFields();
                case MiniTimelineConstants.TRACK_ANIMATOR:
                    return GetAnimatorTrackFields();
                case MiniTimelineConstants.TRACK_MORPH:
                    return GetMorphTrackFields();
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    return GetMovementTrackFields();
                case MiniTimelineConstants.TRACK_SIGNAL:
                    return GetSignalTrackFields();
                case MiniTimelineConstants.TRACK_UMA_WARDROBE:
                    return GetUmaWardrobeTrackFields();
                default:
                    return new List<FormFieldDefinition>();
            }
        }
        
        /// <summary>
        /// Animation Track specific settings
        /// </summary>
        private static List<FormFieldDefinition> GetAnimTrackFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "animationLayer",
                    type = "number",
                    label = "Animation Layer",
                    required = false,
                    defaultValue = 0,
                    tooltip = "Animation layer for this track",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0 },
                        { "max", 10 }
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "blendMode",
                    type = "selectbox",
                    label = "Blend Mode",
                    required = false,
                    defaultValue = "Override",
                    tooltip = "How this track blends with other animation tracks",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Override", "Additive", "Multiply" }}
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "defaultSpeed",
                    type = "slider",
                    label = "Default Speed",
                    required = false,
                    defaultValue = 1.0f,
                    tooltip = "Default playback speed for animations on this track",
                    options = new Dictionary<string, object>
                    {
                        { "minValue", 0.1f },
                        { "maxValue", 3.0f }
                    }
                }
            };
        }
        
        /// <summary>
        /// Animator Track specific settings
        /// </summary>
        private static List<FormFieldDefinition> GetAnimatorTrackFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "layerIndex",
                    type = "number",
                    label = "Animator Layer Index",
                    required = false,
                    defaultValue = 0,
                    tooltip = "Animator layer index for this track",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0 },
                        { "max", 10 }
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "applyRootMotion",
                    type = "toggle",
                    label = "Apply Root Motion",
                    required = false,
                    defaultValue = false,
                    tooltip = "Whether to apply root motion from animator states"
                },
                
                new FormFieldDefinition
                {
                    name = "updateMode",
                    type = "selectbox",
                    label = "Update Mode",
                    required = false,
                    defaultValue = "Normal",
                    tooltip = "Animator update mode for this track",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Normal", "AnimatePhysics", "UnscaledTime" }}
                    }
                }
            };
        }
        
        /// <summary>
        /// Morph Track specific settings
        /// </summary>
        private static List<FormFieldDefinition> GetMorphTrackFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "morphWeight",
                    type = "slider",
                    label = "Default Morph Weight",
                    required = false,
                    defaultValue = 1.0f,
                    tooltip = "Default weight for morph targets",
                    options = new Dictionary<string, object>
                    {
                        { "minValue", 0.0f },
                        { "maxValue", 1.0f }
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "blendShapeRenderer",
                    type = "text",
                    label = "Blend Shape Renderer",
                    required = false,
                    placeholder = "SkinnedMeshRenderer path",
                    tooltip = "Path to the SkinnedMeshRenderer component for blend shapes"
                },
                
                new FormFieldDefinition
                {
                    name = "restoreOnDisable",
                    type = "toggle",
                    label = "Restore on Disable",
                    required = false,
                    defaultValue = true,
                    tooltip = "Whether to restore original blend shape values when track is disabled"
                }
            };
        }
        
        /// <summary>
        /// Movement Track specific settings
        /// </summary>
        private static List<FormFieldDefinition> GetMovementTrackFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "coordinateSpace",
                    type = "selectbox",
                    label = "Coordinate Space",
                    required = false,
                    defaultValue = "World",
                    tooltip = "Coordinate space for movement calculations",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "World", "Local", "Parent" }}
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "defaultEasing",
                    type = "selectbox",
                    label = "Default Easing",
                    required = false,
                    defaultValue = "EaseInOut",
                    tooltip = "Default easing function for movement clips",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Linear", "EaseIn", "EaseOut", "EaseInOut", "Bounce", "Elastic" }}
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "snapToGround",
                    type = "toggle",
                    label = "Snap to Ground",
                    required = false,
                    defaultValue = false,
                    tooltip = "Whether movement should automatically snap to ground level"
                }
            };
        }
        
        /// <summary>
        /// Signal Track specific settings
        /// </summary>
        private static List<FormFieldDefinition> GetSignalTrackFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "signalPriority",
                    type = "number",
                    label = "Signal Priority",
                    required = false,
                    defaultValue = 0,
                    tooltip = "Priority level for signal processing",
                    options = new Dictionary<string, object>
                    {
                        { "min", -10 },
                        { "max", 10 }
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "defaultSignalType",
                    type = "selectbox",
                    label = "Default Signal Type",
                    required = false,
                    defaultValue = "Event",
                    tooltip = "Default type for new signals on this track",
                    options = new Dictionary<string, object>
                    {
                        { "items", new List<string> { "Event", "Trigger", "Boolean", "Float", "String" }}
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "debugOutput",
                    type = "toggle",
                    label = "Debug Output",
                    required = false,
                    defaultValue = false,
                    tooltip = "Whether to log signal events for debugging"
                }
            };
        }
        
        /// <summary>
        /// UMA Wardrobe Track specific settings
        /// </summary>
        private static List<FormFieldDefinition> GetUmaWardrobeTrackFields()
        {
            return new List<FormFieldDefinition>
            {
                new FormFieldDefinition
                {
                    name = "autoRebuild",
                    type = "toggle",
                    label = "Auto Rebuild",
                    required = false,
                    defaultValue = true,
                    tooltip = "Whether to automatically rebuild UMA avatar when wardrobe changes"
                },
                
                new FormFieldDefinition
                {
                    name = "transitionDuration",
                    type = "number",
                    label = "Transition Duration",
                    required = false,
                    defaultValue = 0.5f,
                    tooltip = "Default transition duration for wardrobe changes",
                    options = new Dictionary<string, object>
                    {
                        { "min", 0.0f },
                        { "max", 5.0f }
                    }
                },
                
                new FormFieldDefinition
                {
                    name = "preserveColors",
                    type = "toggle",
                    label = "Preserve Colors",
                    required = false,
                    defaultValue = false,
                    tooltip = "Whether to preserve existing colors when changing wardrobe items"
                }
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get available binding keys from the current timeline director's binding context
        /// </summary>
        public static List<string> GetAvailableBindingKeys()
        {
            var bindingKeys = new List<string>();
            
            // Try to get binding context from the current timeline director
            var director = UnityEngine.Object.FindFirstObjectByType<MiniTimelineDirector>();
            if (director?.BindingContext != null)
            {
                bindingKeys.AddRange(director.BindingContext.GetKeys());
            }
            
            // Also check for BindableObjects in the scene to suggest their type names
            var bindableObjects = UnityEngine.Object.FindObjectsByType<BindableObject>(UnityEngine.FindObjectsSortMode.None);
            foreach (var bindableObj in bindableObjects)
            {
                string typeName = bindableObj.TypeName;
                if (!string.IsNullOrEmpty(typeName) && !bindingKeys.Contains(typeName))
                {
                    bindingKeys.Add(typeName);
                }
                
                // Also include tags as additional options
                foreach (var tag in bindableObj.Tags)
                {
                    if (!string.IsNullOrEmpty(tag) && !bindingKeys.Contains(tag))
                    {
                        bindingKeys.Add(tag);
                    }
                }
            }
            
            // Sort alphabetically for better UX
            bindingKeys.Sort();
            
            return bindingKeys;
        }
        
        /// <summary>
        /// Get track type options for dropdown selection
        /// </summary>
        private static List<TrackTypeOption> GetTrackTypeOptions()
        {
            return new List<TrackTypeOption>
            {
                new TrackTypeOption { value = MiniTimelineConstants.TRACK_ANIM, label = "Animation Track", description = "Plays animation clips" },
                new TrackTypeOption { value = MiniTimelineConstants.TRACK_ANIMATOR, label = "Animator Track", description = "Controls Animator states" },
                new TrackTypeOption { value = MiniTimelineConstants.TRACK_MORPH, label = "Morph Track", description = "Animates blend shapes and morphs" },
                new TrackTypeOption { value = MiniTimelineConstants.TRACK_MOVEMENT, label = "Movement Track", description = "Handles object movement and positioning" },
                new TrackTypeOption { value = MiniTimelineConstants.TRACK_SIGNAL, label = "Signal Track", description = "Sends events and signals" },
                new TrackTypeOption { value = MiniTimelineConstants.TRACK_UMA_WARDROBE, label = "UMA Wardrobe Track", description = "Manages UMA wardrobe changes" }
            };
        }
        
        /// <summary>
        /// Validate track settings form data
        /// </summary>
        public static bool ValidateTrackSettings(Dictionary<string, object> formData, string trackType, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Validate required fields
            if (!formData.ContainsKey("trackName") || string.IsNullOrEmpty(formData["trackName"]?.ToString()))
            {
                errorMessage = "Track name is required";
                return false;
            }
            
            // Track-specific validation
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_ANIM:
                    return ValidateAnimTrackSettings(formData, out errorMessage);
                case MiniTimelineConstants.TRACK_ANIMATOR:
                    return ValidateAnimatorTrackSettings(formData, out errorMessage);
                case MiniTimelineConstants.TRACK_MORPH:
                    return ValidateMorphTrackSettings(formData, out errorMessage);
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    return ValidateMovementTrackSettings(formData, out errorMessage);
                case MiniTimelineConstants.TRACK_SIGNAL:
                    return ValidateSignalTrackSettings(formData, out errorMessage);
                case MiniTimelineConstants.TRACK_UMA_WARDROBE:
                    return ValidateUmaWardrobeTrackSettings(formData, out errorMessage);
            }
            
            return true;
        }
        
        /// <summary>
        /// Convert form data to track settings dictionary
        /// </summary>
        public static Dictionary<string, object> ConvertFormDataToTrackSettings(Dictionary<string, object> formData, string trackType)
        {
            var settings = new Dictionary<string, object>();
            
            // Copy all form data to settings
            foreach (var kvp in formData)
            {
                settings[kvp.Key] = kvp.Value;
            }
            
            // Track-specific processing
            switch (trackType)
            {
                case MiniTimelineConstants.TRACK_MOVEMENT:
                    ProcessMovementTrackSettings(settings);
                    break;
                case MiniTimelineConstants.TRACK_MORPH:
                    ProcessMorphTrackSettings(settings);
                    break;
            }
            
            return settings;
        }
        
        #endregion
        
        #region Validation Methods
        
        private static bool ValidateAnimTrackSettings(Dictionary<string, object> formData, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (formData.ContainsKey("animationLayer") && formData["animationLayer"] is int layer)
            {
                if (layer < 0 || layer > 10)
                {
                    errorMessage = "Animation layer must be between 0 and 10";
                    return false;
                }
            }
            
            return true;
        }
        
        private static bool ValidateAnimatorTrackSettings(Dictionary<string, object> formData, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (formData.ContainsKey("layerIndex") && formData["layerIndex"] is int layer)
            {
                if (layer < 0 || layer > 10)
                {
                    errorMessage = "Animator layer index must be between 0 and 10";
                    return false;
                }
            }
            
            return true;
        }
        
        private static bool ValidateMorphTrackSettings(Dictionary<string, object> formData, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (formData.ContainsKey("morphWeight") && formData["morphWeight"] is float weight)
            {
                if (weight < 0f || weight > 1f)
                {
                    errorMessage = "Morph weight must be between 0 and 1";
                    return false;
                }
            }
            
            return true;
        }
        
        private static bool ValidateMovementTrackSettings(Dictionary<string, object> formData, out string errorMessage)
        {
            errorMessage = string.Empty;
            // Movement track validation logic here
            return true;
        }
        
        private static bool ValidateSignalTrackSettings(Dictionary<string, object> formData, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (formData.ContainsKey("signalPriority") && formData["signalPriority"] is int priority)
            {
                if (priority < -10 || priority > 10)
                {
                    errorMessage = "Signal priority must be between -10 and 10";
                    return false;
                }
            }
            
            return true;
        }
        
        private static bool ValidateUmaWardrobeTrackSettings(Dictionary<string, object> formData, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (formData.ContainsKey("transitionDuration") && formData["transitionDuration"] is float duration)
            {
                if (duration < 0f || duration > 5f)
                {
                    errorMessage = "Transition duration must be between 0 and 5 seconds";
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion
        
        #region Processing Methods
        
        private static void ProcessMovementTrackSettings(Dictionary<string, object> settings)
        {
            // Any movement track specific processing
        }
        
        private static void ProcessMorphTrackSettings(Dictionary<string, object> settings)
        {
            // Any morph track specific processing
        }
        
        #endregion
    }
    
    /// <summary>
    /// Helper class for track type options in dropdowns
    /// </summary>
    public class TrackTypeOption
    {
        public string value;
        public string label;
        public string description;
        
        public override string ToString() => label;
    }
}