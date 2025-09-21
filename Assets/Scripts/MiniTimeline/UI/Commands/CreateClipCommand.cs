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
        private readonly ClipData clipData;
        private readonly TrackUI trackUI;
        private IMiniClip createdClip;

        public CreateClipCommand(IMiniTrack targetTrack, ClipData data, TrackUI trackInterface)
            : base($"Create Clip")
        {
            track = targetTrack;
            clipData = data;
            trackUI = trackInterface;
        }

        protected override void ExecuteInternal()
        {
            // TODO: Create clip through proper track API
            // createdClip = track.CreateClip(clipData);

            // Update UI
            // trackUI?.BuildClipUIs();

            Debug.Log($"Creating clip at {clipData.start} with duration {clipData.duration}");
        }

        protected override void UndoInternal()
        {
            if (createdClip != null)
            {
                // TODO: Remove clip through proper track API
                // track.RemoveClip(createdClip);

                // Update UI
                // trackUI?.BuildClipUIs();

                Debug.Log($"Removing clip {createdClip.Id}");
            }
        }
    }

}