using MiniTimeline.Core;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for changing timeline zoom level
    /// </summary>
    public class ZoomTimelineCommand : TimelineCommandBase
    {
        private readonly TimelineEditorUI editor;
        private readonly float oldZoom;
        private readonly float newZoom;

        public ZoomTimelineCommand(TimelineEditorUI timelineEditor, float oldZoomValue, float newZoomValue)
            : base($"Zoom to {newZoomValue:F1}x")
        {
            editor = timelineEditor;
            oldZoom = oldZoomValue;
            newZoom = newZoomValue;
        }

        protected override void ExecuteInternal()
        {
            editor?.SetZoom(newZoom);
        }

        protected override void UndoInternal()
        {
            editor?.SetZoom(oldZoom);
        }

        public override bool CanMergeWith(ITimelineCommand other)
        {
            // Zoom commands can be merged
            return other is ZoomTimelineCommand;
        }

        public override void MergeWith(ITimelineCommand other)
        {
            if (other is ZoomTimelineCommand zoomCommand)
            {
                // Keep our old zoom, update to the new command's new zoom
                // newZoom = zoomCommand.newZoom; // Would be done through proper API
            }
        }
    }

}