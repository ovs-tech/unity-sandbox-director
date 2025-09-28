using System.Linq;
using MiniTimeline.Core;
using MiniTimeline.Serialization;
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
            if (director?.Project == null)
            {
                Debug.LogError("Cannot add track: No project loaded");
                return;
            }

            try
            {
                // Add track data to project
                director.Project.tracks.Add(trackData);
                
                // Create track instance using factory
                createdTrack = TrackFactory.CreateTrack(trackData);
                
                // Mark director as dirty (this typically triggers a rebuild)
                director.MarkDirty();
                
                // Trigger UI rebuild if available
                editorUI?.BuildTimelineUI();

                Debug.Log($"Successfully added {trackData.type} track with ID: {trackData.id}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to add track: {ex.Message}");
            }
        }

        protected override void UndoInternal()
        {
            if (director?.Project == null || createdTrack == null)
            {
                Debug.LogWarning("Cannot undo add track: Invalid state");
                return;
            }

            try
            {
                // Remove track data from project
                var trackToRemove = director.Project.tracks.FirstOrDefault(t => t.id == trackData.id);
                if (trackToRemove != null)
                {
                    director.Project.tracks.Remove(trackToRemove);
                    
                    // Mark director as dirty
                    director.MarkDirty();
                    
                    // Trigger UI rebuild
                    editorUI?.BuildTimelineUI();
                    
                    Debug.Log($"Successfully removed {trackData.type} track with ID: {trackData.id}");
                }
                else
                {
                    Debug.LogWarning($"Track with ID {trackData.id} not found for removal");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to undo add track: {ex.Message}");
            }
        }
    }

}