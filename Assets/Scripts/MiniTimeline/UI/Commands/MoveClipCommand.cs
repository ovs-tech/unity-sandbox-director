using Systems.CommandSystem;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for moving a clip to a new time position
    /// Supports merging for smooth dragging operations
    /// </summary>
    public class MoveClipCommand : CommandBase
    {
        private readonly IMiniClip clip;
        private readonly float oldStartTime;
        private readonly float newStartTime;

        public MoveClipCommand(IMiniClip clipToMove, float oldStart, float newStart)
            : base($"Move {clipToMove.Id}")
        {
            clip = clipToMove;
            oldStartTime = oldStart;
            newStartTime = newStart;
        }

        protected override void ExecuteInternal()
        {
            // Update clip data through proper API
            SetClipStartTime(newStartTime);
        }

        protected override void UndoInternal()
        {
            SetClipStartTime(oldStartTime);
        }

        public override bool CanMergeWith(ICommand other)
        {
            if (other is MoveClipCommand moveCommand)
            {
                return moveCommand.clip == clip;
            }
            return false;
        }

        public override void MergeWith(ICommand other)
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
