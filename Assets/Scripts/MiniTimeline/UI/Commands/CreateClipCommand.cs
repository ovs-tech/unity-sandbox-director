using Core.Behaviors.Command;
using Systems.MiniTimeline.Core;
using UnityEngine;

namespace Systems.MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for creating a new clip on a track
    /// </summary>
    public class CreateClipCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly IMiniClip clipInstance;

        public CreateClipCommand(IMiniTrack targetTrack, IMiniClip clip)
            : base($"Create Clip")
        {
            track = targetTrack;
            clipInstance = clip;
        }

        protected override void ExecuteInternal()
        {
            // Add clip to track
            track.AddClip(clipInstance);

            Debug.Log($"Created clip '{clipInstance.Id}' at {clipInstance.Start} with duration {clipInstance.Duration}");
        }

        protected override void UndoInternal()
        {
            if (clipInstance != null)
            {
                // Remove clip from track
                bool removed = track.RemoveClip(clipInstance);

                if (removed)
                {
                    Debug.Log($"Removed clip '{clipInstance.Id}'");
                }
                else
                {
                    Debug.LogWarning($"Failed to remove clip '{clipInstance.Id}' - not found in track");
                }
            }
        }
    }
}