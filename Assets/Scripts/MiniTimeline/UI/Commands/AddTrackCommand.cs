using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{

    /// <summary>
    /// Command for adding a new track to the timeline
    /// </summary>
    public class AddTrackCommand : TimelineCommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly TrackData trackData;
        private readonly TimelineEditorUI editorUI;
        private IMiniTrack createdTrack;

        public AddTrackCommand(MiniTimelineDirector timelineDirector, TrackData data, TimelineEditorUI editor)
            : base($"Add {data.type} Track")
        {
            director = timelineDirector;
            trackData = data;
            editorUI = editor;
        }

        protected override void ExecuteInternal()
        {
            // TODO: Add track through proper project API
            // director.Project.tracks.Add(trackData);
            // director.MarkDirty();

            // Rebuild UI
            // editorUI?.BuildTimelineUI();

            Debug.Log($"Adding {trackData.type} track");
        }

        protected override void UndoInternal()
        {
            if (createdTrack != null)
            {
                // TODO: Remove track through proper project API
                // director.Project.tracks.Remove(trackData);
                // director.MarkDirty();

                // Rebuild UI
                // editorUI?.BuildTimelineUI();

                Debug.Log($"Removing {trackData.type} track");
            }
        }
    }

}