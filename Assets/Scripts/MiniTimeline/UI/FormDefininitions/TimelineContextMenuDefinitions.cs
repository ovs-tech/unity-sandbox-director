using System.Collections.Generic;
using Systems.MiniTimeline.Core;
using Core.UI.FormSubmit.Fields;

namespace Systems.MiniTimeline.UI.FormDefinitions
{
    /// <summary>
    /// Provides form field definitions for timeline context menu actions
    /// This class defines the action buttons that appear in timeline context menus
    /// </summary>
    public static class TimelineContextMenuDefinitions
    {
        /// <summary>
        /// Get context menu action fields for timeline
        /// </summary>
        public static List<FormFieldDefinition> GetTimelineActionFields()
        {
            var fields = new List<FormFieldDefinition>();

            // Add Track action
            var addTrackField = new FormFieldDefinition("addTrack", "Add Track", "button");
            addTrackField.options = new Dictionary<string, object>
            {
                ["action"] = "addTrack"
            };
            fields.Add(addTrackField);

            // Paste action
            var pasteField = new FormFieldDefinition("paste", "Paste", "button");
            pasteField.options = new Dictionary<string, object>
            {
                ["action"] = "paste"
            };
            fields.Add(pasteField);

            // Add Marker action
            var addMarkerField = new FormFieldDefinition("addMarker", "Add Marker", "button");
            addMarkerField.options = new Dictionary<string, object>
            {
                ["action"] = "addMarker"
            };
            fields.Add(addMarkerField);

            // Zoom to Fit action
            var zoomFitField = new FormFieldDefinition("zoomFit", "Zoom to Fit", "button");
            zoomFitField.options = new Dictionary<string, object>
            {
                ["action"] = "zoomFit"
            };
            fields.Add(zoomFitField);

            // Reset Zoom action
            var resetZoomField = new FormFieldDefinition("resetZoom", "Reset Zoom", "button");
            resetZoomField.options = new Dictionary<string, object>
            {
                ["action"] = "resetZoom"
            };
            fields.Add(resetZoomField);

            return fields;
        }

        /// <summary>
        /// Get display title for timeline context menu
        /// </summary>
        public static string GetTimelineMenuTitle()
        {
            return "Timeline Actions";
        }
    }
}
