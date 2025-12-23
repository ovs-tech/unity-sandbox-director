using System.Linq;
using Core.Behaviors.Command;
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
        private IMiniTrack createdTrack;

        public AddTrackCommand(MiniTimelineDirector timelineDirector, TrackData data)
            : base($"Add {data.type} Track")
        {
            director = timelineDirector;
            trackData = data;
        }

        protected override void ExecuteInternal()
        {
            if (director?.Project == null)
            {
                Debug.LogError("Cannot add track: No project loaded");
                return;
            }

            try
            {
                // Add track data to project
                director.Project.tracks.Add(trackData);
                
                // Force immediate track rebuild by calling BuildTracks via reflection
                ForceTrackRebuild();
                
                // Find the created track in the director's track list
                createdTrack = director.Tracks.FirstOrDefault(t => t.Id == trackData.id);
                
                if (createdTrack != null)
                {
                    // Successfully created and added the track (informational log removed)
                }
                else
                {
                    Debug.LogWarning($"Track was added to project but not found in runtime tracks. Type: {trackData.type}, ID: {trackData.id}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to add track: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Force track rebuild using reflection to call BuildTracks method
        /// </summary>
        private void ForceTrackRebuild()
        {
            try
            {
                var directorType = director.GetType();
                
                // Try to find BuildTracks method
                var buildTracksMethod = directorType.GetMethod("BuildTracks", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public);
                
                if (buildTracksMethod != null)
                {
                    buildTracksMethod.Invoke(director, null);
                    // Informational log removed for successful reflection call
                }
                else
                {
                    // Fallback: mark dirty and seek
                    director.MarkDirty();
                    if (Application.isPlaying)
                    {
                        director.Seek(director.Time);
                    }
                    // Informational fallback log removed
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to force track rebuild: {ex.Message}");
                // Fallback to original method
                director.MarkDirty();
                if (Application.isPlaying)
                {
                    director.Seek(director.Time);
                }
            }
        }

        protected override void UndoInternal()
        {
            if (director?.Project == null)
            {
                Debug.LogWarning("Cannot undo add track: No project loaded");
                return;
            }

            try
            {
                // Remove track data from project
                var trackToRemove = director.Project.tracks.FirstOrDefault(t => t.id == trackData.id);
                if (trackToRemove != null)
                {
                    director.Project.tracks.Remove(trackToRemove);
                    
                    // Force immediate track rebuild
                    ForceTrackRebuild();
                    
                    // Successfully removed the track (informational log removed)
                }
                else
                {
                    Debug.LogWarning($"Track with ID {trackData.id} not found for removal");
                }
                
                // Clean up the created track reference
                createdTrack = null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to undo add track: {ex.Message}");
            }
        }
    }

}