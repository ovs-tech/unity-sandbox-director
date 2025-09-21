using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for removing a track from the timeline
    /// </summary>
    public class RemoveTrackCommand : TimelineCommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly IMiniTrack track;
        private readonly TrackData trackBackup;
        private readonly TimelineEditorUI editorUI;
        private readonly int trackIndex;

        public RemoveTrackCommand(MiniTimelineDirector timelineDirector, IMiniTrack trackToRemove, TimelineEditorUI editor)
            : base($"Remove Track")
        {
            director = timelineDirector;
            track = trackToRemove;
            editorUI = editor;

            // TODO: Backup track data and find index
            // trackIndex = director.Project.tracks.FindIndex(t => t.id == track.Id);
            // trackBackup = director.Project.tracks[trackIndex];
        }

        protected override void ExecuteInternal()
        {
            // TODO: Remove track through proper project API
            // director.Project.tracks.RemoveAt(trackIndex);
            // director.MarkDirty();

            // Rebuild UI
            // editorUI?.BuildTimelineUI();

            Debug.Log($"Removing track {track.Id}");
        }

        protected override void UndoInternal()
        {
            // TODO: Restore track through proper project API
            // director.Project.tracks.Insert(trackIndex, trackBackup);
            // director.MarkDirty();

            // Rebuild UI
            // editorUI?.BuildTimelineUI();

            Debug.Log($"Restoring track {track.Id}");
        }
    }

}