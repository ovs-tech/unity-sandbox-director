using Core.Behaviors.Command;
using MiniTimeline.Core;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for changing timeline zoom level
    /// Note: This command no longer directly modifies UI. UI should observe zoom state from the timeline data.
    /// </summary>
    public class ZoomTimelineCommand : TimelineCommandBase
    {
        private readonly float oldZoom;
        private readonly float newZoom;

        public ZoomTimelineCommand(float oldZoomValue, float newZoomValue)
            : base($"Zoom to {newZoomValue:F1}x")
        {
            oldZoom = oldZoomValue;
            newZoom = newZoomValue;
        }

        protected override void ExecuteInternal()
        {
            // Zoom state should be managed by timeline model
            // UI will observe and update accordingly
        }

        protected override void UndoInternal()
        {
            // Zoom state should be managed by timeline model
            // UI will observe and update accordingly
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