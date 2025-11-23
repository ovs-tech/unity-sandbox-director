using System;
using System.Collections.Generic;
using Core.Behaviors.Command;
using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for editing clip properties (name, start, duration, and clip-specific properties)
    /// Supports undo/redo functionality for all property changes
    /// </summary>
    public class EditClipCommand : TimelineCommandBase
    {
        private readonly IMiniClip clip;
        private readonly ITimelineEditorUI editorUI;
        
        // Store original and new values for all properties
        private readonly Dictionary<string, object> originalValues;
        private readonly Dictionary<string, object> newValues;

        public EditClipCommand(IMiniClip clipToEdit, Dictionary<string, object> newFormData, ITimelineEditorUI timelineEditor)
            : base($"Edit {clipToEdit.Id}")
        {
            clip = clipToEdit;
            editorUI = timelineEditor;
            newValues = new Dictionary<string, object>(newFormData);
            originalValues = new Dictionary<string, object>();
            
            // Capture original values
            CaptureOriginalValues();
        }

        protected override void ExecuteInternal()
        {
            ApplyValues(newValues);
            UpdateUI();
        }

        protected override void UndoInternal()
        {
            ApplyValues(originalValues);
            UpdateUI();
        }

        public override bool CanMergeWith(ITimelineCommand other)
        {
            // Don't merge property edit commands - each should be a discrete action
            return false;
        }

        public override void MergeWith(ITimelineCommand other)
        {
            // Not implemented since CanMergeWith returns false
        }

        /// <summary>
        /// Capture the original values of all properties that will be changed
        /// </summary>
        private void CaptureOriginalValues()
        {
            var clipBase = clip as MiniClipBase;
            if (clipBase == null)
            {
                Debug.LogError($"Cannot edit clip: clip {clip.GetType().Name} is not derived from MiniClipBase");
                return;
            }
            
            // Capture basic properties
            originalValues["name"] = clipBase.Id;
            originalValues["start"] = clipBase.Start;
            originalValues["duration"] = clipBase.Duration;
            
            // Capture clip-specific properties that exist in the new values
            CaptureClipSpecificProperties();
        }
        
        /// <summary>
        /// Capture clip-specific properties that will be changed
        /// </summary>
        private void CaptureClipSpecificProperties()
        {
            var clipType = clip.GetType();
            foreach (var kvp in newValues)
            {
                // Skip basic properties and trackType
                if (kvp.Key == "name" || kvp.Key == "start" || kvp.Key == "duration" || kvp.Key == "trackType")
                    continue;
                
                try
                {
                    object value = GetPropertyOrFieldValue(clipType, kvp.Key);
                    if (value != null)
                    {
                        originalValues[kvp.Key] = value;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not capture original value for property '{kvp.Key}': {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Get the value of a property or field using reflection
        /// </summary>
        private object GetPropertyOrFieldValue(Type clipType, string memberName)
        {
            var property = clipType.GetProperty(memberName);
            if (property != null && property.CanRead)
            {
                return property.GetValue(clip);
            }
            
            var field = clipType.GetField(memberName);
            if (field != null)
            {
                return field.GetValue(clip);
            }
            
            return null;
        }

        /// <summary>
        /// Apply a set of values to the clip
        /// </summary>
        private void ApplyValues(Dictionary<string, object> values)
        {
            var clipBase = clip as MiniClipBase;
            if (clipBase == null)
            {
                Debug.LogError($"Cannot edit clip: clip {clip.GetType().Name} is not derived from MiniClipBase");
                return;
            }
            
            // Apply basic properties
            if (values.ContainsKey("name"))
            {
                clipBase.Id = values["name"].ToString();
            }
            
            if (values.ContainsKey("start") && float.TryParse(values["start"].ToString(), out float newStart))
            {
                clipBase.Start = newStart;
            }
            
            if (values.ContainsKey("duration") && float.TryParse(values["duration"].ToString(), out float newDuration))
            {
                clipBase.Duration = Mathf.Max(0.1f, newDuration); // Ensure minimum duration
            }
            
            // Apply clip-specific properties using reflection
            ApplyClipSpecificProperties(values);
        }

        /// <summary>
        /// Apply clip-specific properties using reflection
        /// </summary>
        private void ApplyClipSpecificProperties(Dictionary<string, object> values)
        {
            var clipType = clip.GetType();
            
            foreach (var kvp in values)
            {
                // Skip basic properties already handled
                if (kvp.Key == "trackType" || kvp.Key == "name" || kvp.Key == "start" || kvp.Key == "duration")
                    continue;
                
                try
                {
                    var property = clipType.GetProperty(kvp.Key);
                    var field = clipType.GetField(kvp.Key);
                    
                    if (property != null && property.CanWrite)
                    {
                        // Convert value to appropriate type
                        var convertedValue = ConvertValueToPropertyType(kvp.Value, property.PropertyType);
                        property.SetValue(clip, convertedValue);
                    }
                    else if (field != null)
                    {
                        // Convert value to appropriate type
                        var convertedValue = ConvertValueToPropertyType(kvp.Value, field.FieldType);
                        field.SetValue(clip, convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not set property '{kvp.Key}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Convert form value to the target property type
        /// </summary>
        private object ConvertValueToPropertyType(object value, Type targetType)
        {
            if (value == null) return null;
            
            // If already correct type, return as-is
            if (targetType.IsAssignableFrom(value.GetType()))
                return value;
            
            // Handle common type conversions
            if (targetType == typeof(string))
                return value.ToString();
            
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            
            // For other types, try direct conversion
            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Update UI after property changes
        /// </summary>
        private void UpdateUI()
        {
            // Rebuild the timeline UI to reflect changes
            // This ensures all clips and tracks are properly updated
            editorUI?.BuildTimelineUI();
        }
    }
}