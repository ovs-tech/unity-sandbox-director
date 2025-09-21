using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{

    /// <summary>
    /// Command for deleting a clip from a track
    /// </summary>
    public class DeleteClipCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly IMiniClip clip;
        private readonly ClipData clipBackup;
        private readonly TrackUI trackUI;

        public DeleteClipCommand(IMiniTrack sourceTrack, IMiniClip clipToDelete, TrackUI trackInterface)
            : base($"Delete {clipToDelete.Id}")
        {
            track = sourceTrack;
            clip = clipToDelete;
            trackUI = trackInterface;

            // TODO: Backup clip data for undo
            clipBackup = new ClipData
            {
                id = clip.Id,
                start = clip.Start,
                duration = clip.Duration,
                // payload = clip.GetPayload() // Need proper API
            };
        }

        protected override void ExecuteInternal()
        {
            // TODO: Remove clip through proper track API
            // track.RemoveClip(clip);

            // Update UI
            // trackUI?.BuildClipUIs();

            Debug.Log($"Deleting clip {clip.Id}");
        }

        protected override void UndoInternal()
        {
            // TODO: Recreate clip through proper track API
            // var restoredClip = track.CreateClip(clipBackup);

            // Update UI
            // trackUI?.BuildClipUIs();

            Debug.Log($"Restoring clip {clip.Id}");
        }
    }

}