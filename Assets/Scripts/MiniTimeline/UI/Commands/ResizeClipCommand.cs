using Core.Behaviors.Command;
using MiniTimeline.Core;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for resizing a clip (changing duration and/or start time)
    /// </summary>
    public class ResizeClipCommand : TimelineCommandBase
    {
        private readonly IMiniClip clip;
        private readonly float oldStartTime;
        private readonly float oldDuration;
        private readonly float newStartTime;
        private readonly float newDuration;
        private readonly TrackUI trackUI;

        public ResizeClipCommand(IMiniClip clipToResize, float oldStart, float oldDur,
                               float newStart, float newDur, TrackUI track)
            : base($"Resize {clipToResize.Id}")
        {
            clip = clipToResize;
            oldStartTime = oldStart;
            oldDuration = oldDur;
            newStartTime = newStart;
            newDuration = newDur;
            trackUI = track;
        }

        protected override void ExecuteInternal()
        {
            SetClipTiming(newStartTime, newDuration);

            // Update UI
            var clipUI = trackUI?.GetClipUI(clip.Id);
            clipUI?.UpdateLayout();
        }

        protected override void UndoInternal()
        {
            SetClipTiming(oldStartTime, oldDuration);

            // Update UI
            var clipUI = trackUI?.GetClipUI(clip.Id);
            clipUI?.UpdateLayout();
        }

        public override bool CanMergeWith(ITimelineCommand other)
        {
            if (other is ResizeClipCommand resizeCommand)
            {
                return resizeCommand.clip == clip;
            }
            return false;
        }

        public override void MergeWith(ITimelineCommand other)
        {
            if (other is ResizeClipCommand resizeCommand && resizeCommand.clip == clip)
            {
                // Update our new timing to the other command's timing
                // The old timing stays the same (original before resize)
                // This would be done through proper API
            }
        }

        private void SetClipTiming(float start, float duration)
        {

            // Use reflection to set the start time and duration properties
            var clipType = clip.GetType();
            
            // Set Start property
            var startProperty = clipType.GetProperty("Start");
            if (startProperty != null && startProperty.CanWrite)
            {
                startProperty.SetValue(clip, start);
            }
            else
            {
                // Try to access start field directly (could be "start" or "startTime")
                var startField = clipType.GetField("start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) ??
                                clipType.GetField("startTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                
                if (startField != null)
                {
                    startField.SetValue(clip, start);
                }
                else
                {
                }
            }
            
            // Set Duration property
            var durationProperty = clipType.GetProperty("Duration");
            if (durationProperty != null && durationProperty.CanWrite)
            {
                durationProperty.SetValue(clip, duration);
            }
            else
            {
                // Try to access duration field directly
                var durationField = clipType.GetField("duration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                
                if (durationField != null)
                {
                    durationField.SetValue(clip, duration);
                }
                else
                {
                }
            }
            
        }
    }

}