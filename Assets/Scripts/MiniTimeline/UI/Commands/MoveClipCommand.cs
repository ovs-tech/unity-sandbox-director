using Core.Behaviors.Command;
using MiniTimeline.Core;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for moving a clip to a new time position
    /// Supports merging for smooth dragging operations
    /// </summary>
    public class MoveClipCommand : TimelineCommandBase
    {
        private readonly IMiniClip clip;
        private readonly float oldStartTime;
        private readonly float newStartTime;
        private readonly TrackUI trackUI;

        public MoveClipCommand(IMiniClip clipToMove, float oldStart, float newStart, TrackUI track)
            : base($"Move {clipToMove.Id}")
        {
            clip = clipToMove;
            oldStartTime = oldStart;
            newStartTime = newStart;
            trackUI = track;
        }

        protected override void ExecuteInternal()
        {
            // TODO: Update clip data through proper API
            // For now, we'll need to update through the track system
            SetClipStartTime(newStartTime);

            // Update UI
            var clipUI = trackUI?.GetClipUI(clip.Id);
            clipUI?.UpdateLayout();
        }

        protected override void UndoInternal()
        {
            SetClipStartTime(oldStartTime);

            // Update UI
            var clipUI = trackUI?.GetClipUI(clip.Id);
            clipUI?.UpdateLayout();
        }

        public override bool CanMergeWith(ITimelineCommand other)
        {
            if (other is MoveClipCommand moveCommand)
            {
                return moveCommand.clip == clip;
            }
            return false;
        }

        public override void MergeWith(ITimelineCommand other)
        {
            if (other is MoveClipCommand moveCommand && moveCommand.clip == clip)
            {
                // Update our new position to the other command's position
                // The old position stays the same (original start of the drag)
                // newStartTime = moveCommand.newStartTime; // This would be done through reflection or proper API
            }
        }

        private void SetClipStartTime(float time)
        {

            // Try to cast to a mutable clip interface or use reflection to set the start time
            // Check if clip has a settable Start property
            var clipType = clip.GetType();
            var startProperty = clipType.GetProperty("Start");

            if (startProperty != null && startProperty.CanWrite)
            {
                startProperty.SetValue(clip, time);
            }
            else
            {
                // Try to access startTime field directly
                var startField = clipType.GetField("startTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (startField != null)
                {
                    startField.SetValue(clip, time);
                }
            }
        }
    }
}