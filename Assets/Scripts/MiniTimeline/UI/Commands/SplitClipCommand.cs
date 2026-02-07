using Core.Behaviors.Command;
using Systems.MiniTimeline.Core;
using UnityEngine;

namespace Systems.MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for splitting a clip at a specific time.
    /// Shortens the original clip and creates a new clip for the remaining duration.
    /// </summary>
    public class SplitClipCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly IMiniClip originalClip;
        private readonly float splitTime;

        private float oldDuration;
        private IMiniClip newClip;

        public SplitClipCommand(IMiniTrack track, IMiniClip clip, float splitTime)
            : base($"Split {clip?.Id}")
        {
            this.track = track;
            this.originalClip = clip;
            this.splitTime = splitTime;
        }

        protected override void ExecuteInternal()
        {
            if (track == null || originalClip == null)
            {
                Debug.LogError("SplitClipCommand: Invalid track or clip");
                return;
            }

            oldDuration = originalClip.Duration;

            // Adjust original clip
            float firstDuration = splitTime - originalClip.Start;

            // Validate split time
            if (firstDuration <= 0.001f || firstDuration >= oldDuration - 0.001f)
            {
                Debug.LogWarning("SplitClipCommand: Split time is too close to clip edges");
                return;
            }

            // Use reflection or interface property setter if available (MiniClipBase has setters)
            if (originalClip is MiniClipBase baseClip)
            {
                // Clone original clip first to keep properties
                string json = JsonUtility.ToJson(baseClip);
                var type = originalClip.GetType();
                newClip = (IMiniClip)JsonUtility.FromJson(json, type);

                // Update original clip duration
                baseClip.Duration = firstDuration;

                // Update new clip properties
                if (newClip is MiniClipBase newBaseClip)
                {
                    newBaseClip.Id = originalClip.Id + "_split";
                    newBaseClip.Start = splitTime;
                    newBaseClip.Duration = oldDuration - firstDuration;

                    // Handle offset for clips that support it (e.g. AnimClip)
                    HandleClipOffset(originalClip, newBaseClip, splitTime);
                }

                track.AddClip(newClip);
                Debug.Log($"Split clip '{originalClip.Id}' at {splitTime}. Original duration: {firstDuration}, New clip duration: {newClip.Duration}");
            }
            else
            {
                 Debug.LogError($"SplitClipCommand: Clip type {originalClip.GetType().Name} is not derived from MiniClipBase");
            }
        }

        private void HandleClipOffset(IMiniClip original, IMiniClip newClip, float splitTime)
        {
            // Check for clipOffset field using reflection to support any clip type with this property
            var type = original.GetType();
            var offsetField = type.GetField("clipOffset");
            var speedField = type.GetField("speed");

            if (offsetField != null && offsetField.FieldType == typeof(float))
            {
                float oldOffset = (float)offsetField.GetValue(original);
                float speed = 1.0f;
                if (speedField != null && speedField.FieldType == typeof(float))
                {
                    speed = (float)speedField.GetValue(original);
                }

                float timePlayed = (splitTime - original.Start) * speed;
                float newOffset = oldOffset + timePlayed;

                offsetField.SetValue(newClip, newOffset);
                Debug.Log($"SplitClipCommand: Adjusted clipOffset for {newClip.Id} from {oldOffset} to {newOffset}");
            }
        }

        protected override void UndoInternal()
        {
            if (track != null)
            {
                if (newClip != null)
                {
                    track.RemoveClip(newClip);
                }

                if (originalClip is MiniClipBase baseClip)
                {
                    baseClip.Duration = oldDuration;
                }

                Debug.Log($"Undoing split for clip '{originalClip.Id}'");
            }
        }
    }
}
