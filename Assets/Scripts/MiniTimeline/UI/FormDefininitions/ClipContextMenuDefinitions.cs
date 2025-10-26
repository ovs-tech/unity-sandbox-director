using System.Collections.Generic;
using MiniTimeline.Core;
using Core.UI.FormSubmit.Fields;

namespace MiniTimeline.UI.FormDefinitions
{
    /// <summary>
    /// Provides form field definitions for clip context menu actions
    /// Separate from ClipFormDefinitions which handles clip creation
    /// This class defines the action buttons that appear in clip context menus
    /// </summary>
    public static class ClipContextMenuDefinitions
    {
        /// <summary>
        /// Get context menu action fields for a clip
        /// </summary>
        public static List<FormFieldDefinition> GetClipActionFields()
        {
            var fields = new List<FormFieldDefinition>();

            // Cut action
            var cutField = new FormFieldDefinition("cut", "Cut Clip", "button");
            cutField.options = new Dictionary<string, object>
            {
                ["action"] = "cut"
            };
            fields.Add(cutField);

            // Copy action
            var copyField = new FormFieldDefinition("copy", "Copy Clip", "button");
            copyField.options = new Dictionary<string, object>
            {
                ["action"] = "copy"
            };
            fields.Add(copyField);

            // Delete action
            var deleteField = new FormFieldDefinition("delete", "Delete Clip", "button");
            deleteField.options = new Dictionary<string, object>
            {
                ["action"] = "delete"
            };
            fields.Add(deleteField);

            // Duplicate action
            var duplicateField = new FormFieldDefinition("duplicate", "Duplicate Clip", "button");
            duplicateField.options = new Dictionary<string, object>
            {
                ["action"] = "duplicate"
            };
            fields.Add(duplicateField);

            // Split action
            var splitField = new FormFieldDefinition("split", "Split at Playhead", "button");
            splitField.options = new Dictionary<string, object>
            {
                ["action"] = "split"
            };
            fields.Add(splitField);

            // Properties action
            var propertiesField = new FormFieldDefinition("properties", "Properties", "button");
            propertiesField.options = new Dictionary<string, object>
            {
                ["action"] = "properties",
                ["closeForm"] = false // Keep form open for transition to properties form
            };
            fields.Add(propertiesField);

            return fields;
        }

        /// <summary>
        /// Get display title for clip context menu
        /// </summary>
        public static string GetClipMenuTitle()
        {
            return "Clip Actions";
        }
    }
}
