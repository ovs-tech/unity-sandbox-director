using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Behaviors.Command;
using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for updating track settings including bind key, enabled state, order, and binding context
    /// Supports undo/redo operations for track property changes
    /// </summary>
    public class UpdateTrackSettingsCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly Dictionary<string, object> oldSettings;
        private readonly Dictionary<string, object> newSettings;
        private readonly ITimelineEditorUI editorUI;
        private readonly MiniTimelineDirector director;

        public UpdateTrackSettingsCommand(IMiniTrack targetTrack, Dictionary<string, object> previousSettings, 
            Dictionary<string, object> updatedSettings, ITimelineEditorUI timelineEditor, MiniTimelineDirector timelineDirector)
            : base($"Update {TrackUIHelper.GetTrackDisplayName(targetTrack)} Settings")
        {
            track = targetTrack;
            oldSettings = new Dictionary<string, object>(previousSettings);
            newSettings = new Dictionary<string, object>(updatedSettings);
            editorUI = timelineEditor;
            director = timelineDirector;
        }

        protected override void ExecuteInternal()
        {
            ApplySettings(newSettings);
            UpdateUI();
        }

        protected override void UndoInternal()
        {
            ApplySettings(oldSettings);
            UpdateUI();
        }

        private void ApplySettings(Dictionary<string, object> settings)
        {
            try
            {
                foreach (var setting in settings)
                {
                    ApplySetting(setting.Key, setting.Value);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to apply track settings: {ex.Message}");
            }
        }

        private void ApplySetting(string settingName, object value)
        {
            switch (settingName)
            {
                case "bindKey":
                    SetTrackBindKey(value?.ToString() ?? "");
                    break;
                    
                case "enabled":
                    SetTrackEnabled(System.Convert.ToBoolean(value));
                    break;
                    
                case "trackOrder":
                    SetTrackOrder(System.Convert.ToInt32(value));
                    break;
                    
                default:
                    // Handle track-specific settings
                    SetTrackSpecificSetting(settingName, value);
                    break;
            }
        }

        private void SetTrackBindKey(string bindKey)
        {
            var trackType = track.GetType();
            var bindKeyProperty = trackType.GetProperty("BindKey");
            
            if (bindKeyProperty != null && bindKeyProperty.CanWrite)
            {
                bindKeyProperty.SetValue(track, bindKey);
                Debug.Log($"Updated track {track.Id} bind key to: {bindKey}");
            }
            else
            {
                // Try to access bind key field directly if property is not writable
                var bindKeyField = trackType.GetField("bindKey", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (bindKeyField != null)
                {
                    bindKeyField.SetValue(track, bindKey);
                    Debug.Log($"Updated track {track.Id} bind key (field) to: {bindKey}");
                }
                else
                {
                    Debug.LogWarning($"Cannot update bind key for track {track.Id}: no writable property or field found");
                }
            }
        }

        private void SetTrackEnabled(bool enabled)
        {
            var trackType = track.GetType();
            var enabledProperty = trackType.GetProperty("Enabled");
            
            if (enabledProperty != null && enabledProperty.CanWrite)
            {
                enabledProperty.SetValue(track, enabled);
                Debug.Log($"Updated track {track.Id} enabled state to: {enabled}");
            }
            else
            {
                // Try to access enabled field directly
                var enabledField = trackType.GetField("enabled", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (enabledField != null)
                {
                    enabledField.SetValue(track, enabled);
                    Debug.Log($"Updated track {track.Id} enabled state (field) to: {enabled}");
                }
                else
                {
                    Debug.LogWarning($"Cannot update enabled state for track {track.Id}: no writable property or field found");
                }
            }
        }

        private void SetTrackOrder(int order)
        {
            // Track order is typically managed at the project level
            // This would need to be implemented based on your project structure
            if (director?.Project != null)
            {
                var project = director.Project;
                var trackData = project.tracks?.FirstOrDefault(t => t.id == track.Id);
                if (trackData != null)
                {
                    trackData.order = order;
                    Debug.Log($"Updated track {track.Id} order to: {order}");
                }
            }
        }

        private void SetBindingContext(string bindingContext)
        {
            // Removed - binding context is now managed through the binding manager
            Debug.LogWarning("SetBindingContext called but binding context is now managed through binding manager");
        }

        private void SetTrackSpecificSetting(string settingName, object value)
        {
            // Handle track-specific settings using reflection
            var trackType = track.GetType();
            
            // Try property first
            var property = trackType.GetProperty(settingName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                try
                {
                    // Convert value to the correct type
                    var convertedValue = System.Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(track, convertedValue);
                    Debug.Log($"Updated track {track.Id} property {settingName} to: {value}");
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to set property {settingName}: {ex.Message}");
                }
            }
            
            // Try field if property didn't work
            var field = trackType.GetField(settingName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                try
                {
                    var convertedValue = System.Convert.ChangeType(value, field.FieldType);
                    field.SetValue(track, convertedValue);
                    Debug.Log($"Updated track {track.Id} field {settingName} to: {value}");
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to set field {settingName}: {ex.Message}");
                }
            }
            
            Debug.LogWarning($"Could not find property or field {settingName} on track {track.Id}");
        }

        private void UpdateUI()
        {
            // Rebuild the timeline UI to reflect changes
            editorUI?.BuildTimelineUI();
        }

        public override bool CanMergeWith(ITimelineCommand other)
        {
            // Track settings commands can be merged if they're for the same track
            if (other is UpdateTrackSettingsCommand settingsCommand)
            {
                return settingsCommand.track == track;
            }
            return false;
        }

        public override void MergeWith(ITimelineCommand other)
        {
            if (other is UpdateTrackSettingsCommand settingsCommand && settingsCommand.track == track)
            {
                // Update our new settings with the other command's settings
                // Keep the old settings from the first command for proper undo
                foreach (var kvp in settingsCommand.newSettings)
                {
                    newSettings[kvp.Key] = kvp.Value;
                }
                Debug.Log($"Merged track settings command for track {track.Id}");
            }
        }
    }
}