using System.Linq;
using Core.Behaviors.Command;
using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for removing a track from the timeline
    /// Supports undo/redo operations for track removal
    /// </summary>
    public class RemoveTrackCommand : TimelineCommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly IMiniTrack track;
        private readonly TrackData trackBackup;
        private readonly TimelineEditorUI editorUI;
        private readonly int trackIndex;

        public RemoveTrackCommand(MiniTimelineDirector timelineDirector, IMiniTrack trackToRemove, TimelineEditorUI editor)
            : base($"Remove {GetTrackDisplayName(trackToRemove)}")
        {
            director = timelineDirector;
            track = trackToRemove;
            editorUI = editor;

            // Backup track data and find index
            if (director?.Project?.tracks != null)
            {
                trackIndex = director.Project.tracks.FindIndex(t => t.id == track.Id);
                if (trackIndex >= 0)
                {
                    // Create a deep copy of the track data for restoration
                    var originalTrackData = director.Project.tracks[trackIndex];
                    trackBackup = new TrackData
                    {
                        id = originalTrackData.id,
                        type = originalTrackData.type,
                        bindKey = originalTrackData.bindKey,
                        enabled = originalTrackData.enabled,
                        order = originalTrackData.order,
                        clips = originalTrackData.clips?.ToList() ?? new System.Collections.Generic.List<ClipData>()
                    };
                }
                else
                {
                    Debug.LogError($"RemoveTrackCommand: Could not find track {track.Id} in project tracks");
                }
            }
        }

        protected override void ExecuteInternal()
        {
            if (director?.Project?.tracks == null || trackIndex < 0)
            {
                Debug.LogError("RemoveTrackCommand: Cannot remove track - invalid director or track index");
                return;
            }

            try
            {
                // Remove track data from project
                director.Project.tracks.RemoveAt(trackIndex);
                
                // Remove track from director's runtime tracks
                var runtimeTracks = director.Tracks?.ToList();
                if (runtimeTracks != null)
                {
                    var trackToRemove = runtimeTracks.FirstOrDefault(t => t.Id == track.Id);
                    if (trackToRemove != null)
                    {
                        // Try to remove from director using reflection or proper API
                        RemoveTrackFromDirector(trackToRemove);
                    }
                }

                // Mark project as dirty if method exists
                MarkProjectDirty();

                // Rebuild UI to reflect changes
                editorUI?.BuildTimelineUI();

                Debug.Log($"Removed track {track.Id} from timeline");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to remove track: {ex.Message}");
            }
        }

        protected override void UndoInternal()
        {
            if (director?.Project?.tracks == null || trackBackup == null)
            {
                Debug.LogError("RemoveTrackCommand: Cannot restore track - invalid director or backup data");
                return;
            }

            try
            {
                // Restore track data to project
                if (trackIndex >= 0 && trackIndex <= director.Project.tracks.Count)
                {
                    director.Project.tracks.Insert(trackIndex, trackBackup);
                }
                else
                {
                    // Add at end if original index is invalid
                    director.Project.tracks.Add(trackBackup);
                }

                // Mark project as dirty if method exists
                MarkProjectDirty();

                // Rebuild UI to reflect changes
                editorUI?.BuildTimelineUI();

                Debug.Log($"Restored track {track.Id} to timeline");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to restore track: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove track from director's runtime tracks using reflection
        /// </summary>
        private void RemoveTrackFromDirector(IMiniTrack trackToRemove)
        {
            try
            {
                var directorType = director.GetType();
                
                // Try to find a RemoveTrack method
                var removeMethod = directorType.GetMethod("RemoveTrack");
                if (removeMethod != null)
                {
                    removeMethod.Invoke(director, new object[] { trackToRemove });
                    return;
                }

                // Try to access tracks field/property directly
                var tracksProperty = directorType.GetProperty("Tracks");
                var tracksField = directorType.GetField("tracks", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                if (tracksProperty != null && tracksProperty.CanWrite)
                {
                    // If tracks is a writable property, we need to rebuild the track list
                    Debug.LogWarning("RemoveTrackCommand: Cannot directly modify tracks property - UI rebuild will handle this");
                }
                else if (tracksField != null)
                {
                    var tracksList = tracksField.GetValue(director);
                    if (tracksList != null)
                    {
                        var removeFromListMethod = tracksList.GetType().GetMethod("Remove");
                        if (removeFromListMethod != null)
                        {
                            removeFromListMethod.Invoke(tracksList, new object[] { trackToRemove });
                            Debug.Log("Removed track from director's tracks list using reflection");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not remove track from director runtime: {ex.Message}");
                // This is not critical - the UI rebuild will handle creating the correct tracks
            }
        }

        /// <summary>
        /// Mark project as dirty using reflection if method exists
        /// </summary>
        private void MarkProjectDirty()
        {
            try
            {
                var directorType = director.GetType();
                var markDirtyMethod = directorType.GetMethod("MarkDirty") ?? directorType.GetMethod("MarkProjectDirty");
                
                if (markDirtyMethod != null)
                {
                    markDirtyMethod.Invoke(director, null);
                }
                else
                {
                    // Try on project directly
                    var projectType = director.Project.GetType();
                    var projectMarkDirty = projectType.GetMethod("MarkDirty");
                    if (projectMarkDirty != null)
                    {
                        projectMarkDirty.Invoke(director.Project, null);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not mark project as dirty: {ex.Message}");
            }
        }

        /// <summary>
        /// Get display name for track type
        /// </summary>
        private static string GetTrackDisplayName(IMiniTrack track)
        {
            if (track == null) return "Unknown Track";
            return TrackUIHelper.GetTrackDisplayName(track);
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