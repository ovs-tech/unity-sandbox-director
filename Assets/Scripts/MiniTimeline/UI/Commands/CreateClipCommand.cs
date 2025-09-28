using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for creating a new clip on a track
    /// </summary>
    public class CreateClipCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly IMiniClip clipInstance;
        private readonly TrackUI trackUI;

        public CreateClipCommand(IMiniTrack targetTrack, IMiniClip clip, TrackUI trackInterface)
            : base($"Create Clip")
        {
            track = targetTrack;
            clipInstance = clip;
            trackUI = trackInterface;
        }

        protected override void ExecuteInternal()
        {
            // Add clip to track
            track.AddClip(clipInstance);

            // Update UI to show the new clip
            trackUI?.RebuildClipUIs();

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
                    // Update UI
                    trackUI?.RebuildClipUIs();
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