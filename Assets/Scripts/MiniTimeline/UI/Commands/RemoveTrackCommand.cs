using System;
using Core.Behaviors.Command;
using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for removing a track from the timeline.
    /// Supports undo/redo operations for track removal.
    /// </summary>
    public class RemoveTrackCommand : TimelineCommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly IMiniTrack track;
        private readonly IMiniTrack trackBackup;
        private readonly int trackIndex;

        public RemoveTrackCommand(MiniTimelineDirector timelineDirector, IMiniTrack trackToRemove)
            : base($"Remove {trackToRemove.Id}")
        {
            director = timelineDirector;
            track = trackToRemove;

            // Backup track reference and find index for undo
            if (director?.Project?.tracks != null)
            {
                trackIndex = director.Project.tracks.FindIndex(t => t.Id == track.Id);
                if (trackIndex >= 0)
                {
                    trackBackup = director.Project.tracks[trackIndex];
                }
                else
                {
                    Debug.LogError($"RemoveTrackCommand: Could not find track {track.Id} in project tracks");
                }
            }
        }

        protected override void ExecuteInternal()
        {
            if (director == null || track == null)
            {
                Debug.LogError("RemoveTrackCommand: Invalid director or track");
                return;
            }

            try
            {
                director.RemoveTrack(track.Id);
                Debug.Log($"Removed track {track.Id} from timeline");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove track: {ex.Message}");
            }
        }

        protected override void UndoInternal()
        {
            if (director?.Project == null || trackBackup == null)
            {
                Debug.LogError("RemoveTrackCommand: Cannot restore track - invalid director or backup data");
                return;
            }

            try
            {
                // Restore track to project at original index
                if (trackIndex >= 0 && trackIndex <= director.Project.tracks.Count)
                {
                    director.Project.tracks.Insert(trackIndex, trackBackup);
                }
                else
                {
                    director.Project.tracks.Add(trackBackup);
                }

                // Re-add track to director's runtime list
                director.AddTrack(trackBackup);
                
                Debug.Log($"Restored track {track.Id} to timeline");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to restore track: {ex.Message}");
            }
        }

        public override bool CanMergeWith(ITimelineCommand other)
        {
            // Track removal commands should not be merged
            return false;
        }

        public override void MergeWith(ITimelineCommand other)
        {
            // Track removal commands should not be merged
        }
    }

}
