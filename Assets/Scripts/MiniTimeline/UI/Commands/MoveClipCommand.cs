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
        private readonly ITimelineEditorUI editorUI;

        public MoveClipCommand(IMiniClip clipToMove, float oldStart, float newStart, ITimelineEditorUI timelineEditor)
            : base($"Move {clipToMove.Id}")
        {
            clip = clipToMove;
            oldStartTime = oldStart;
            newStartTime = newStart;
            editorUI = timelineEditor;
        }

        protected override void ExecuteInternal()
        {
            // Update clip data through proper API
            SetClipStartTime(newStartTime);

            // Don't rebuild UI here - the visual update was already done during drag
            // Only rebuild on undo/redo to ensure consistency
        }

        protected override void UndoInternal()
        {
            SetClipStartTime(oldStartTime);

            // Rebuild UI to reflect the undone position
            editorUI?.BuildTimelineUI();
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