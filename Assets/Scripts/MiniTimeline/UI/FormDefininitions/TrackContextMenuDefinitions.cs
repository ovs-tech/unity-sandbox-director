using System.Collections.Generic;
using MiniTimeline.Core;
using Core.UI.FormSubmit.Fields;

namespace MiniTimeline.UI.FormDefinitions
{
    /// <summary>
    /// Provides form field definitions for track context menu actions
    /// Separate from TrackFormDefinitions which handles track settings
    /// This class defines the action buttons that appear in track context menus
    /// </summary>
    public static class TrackContextMenuDefinitions
    {
        /// <summary>
        /// Get context menu action fields for a track
        /// </summary>
        public static List<FormFieldDefinition> GetTrackActionFields()
        {
            var fields = new List<FormFieldDefinition>();

            // Add Clip action
            var addClipField = new FormFieldDefinition("addClip", "Add Clip", "button");
            addClipField.options = new Dictionary<string, object>
            {
                ["action"] = "addClip",
                ["closeForm"] = false // Keep form open for transition to clip form
            };
            fields.Add(addClipField);

            // Mute action
            var muteField = new FormFieldDefinition("mute", "Mute Track", "button");
            muteField.options = new Dictionary<string, object>
            {
                ["action"] = "mute"
            };
            fields.Add(muteField);

            // Solo action
            var soloField = new FormFieldDefinition("solo", "Solo Track", "button");
            soloField.options = new Dictionary<string, object>
            {
                ["action"] = "solo"
            };
            fields.Add(soloField);

            // Delete action
            var deleteField = new FormFieldDefinition("delete", "Delete Track", "button");
            deleteField.options = new Dictionary<string, object>
            {
                ["action"] = "delete"
            };
            fields.Add(deleteField);

            // Settings action
            var settingsField = new FormFieldDefinition("settings", "Track Settings", "button");
            settingsField.options = new Dictionary<string, object>
            {
                ["action"] = "settings",
                ["closeForm"] = false
            };
            fields.Add(settingsField);

            return fields;
        }

        /// <summary>
        /// Get display title for track context menu
        /// </summary>
        public static string GetTrackMenuTitle()
        {
            return "Track Actions";
        }
    }
}
