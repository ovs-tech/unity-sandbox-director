using System;
using System.Linq;
using Core.Behaviors.Command;
using MiniTimeline.Core;
using MiniTimeline.Tracks;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for adding a new track to the timeline.
    /// Uses Odin-serialized runtime track instances directly (no DTO conversion).
    /// </summary>
    public class AddTrackCommand : TimelineCommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly string trackType;
        private IMiniTrack createdTrack;

        /// <summary>
        /// Create a new track add command with a track type string.
        /// </summary>
        /// <param name="timelineDirector">The timeline director instance</param>
        /// <param name="type">Track type name (e.g., MiniTimelineConstants.TRACK_ANIM)</param>
        public AddTrackCommand(MiniTimelineDirector timelineDirector, string type)
            : base($"Add {type} Track")
        {
            director = timelineDirector;
            trackType = type;
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
                // Create runtime track instance directly
                createdTrack = CreateRuntimeTrack(trackType);
                if (createdTrack == null)
                {
                    Debug.LogError($"Cannot create track of type {trackType}");
                    return;
                }

                // Add directly to director (which manages project.tracks and binding)
                director.AddTrack(createdTrack);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add track: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a runtime track instance from a type name.
        /// </summary>
        private IMiniTrack CreateRuntimeTrack(string type)
        {
            return type switch
            {
                MiniTimelineConstants.TRACK_ANIM => new AnimTrack(),
                MiniTimelineConstants.TRACK_MORPH => new MorphTrack(),
                MiniTimelineConstants.TRACK_MOVEMENT => new MovementTrack(),
                MiniTimelineConstants.TRACK_ANIMATOR => new AnimatorTrack(),
                MiniTimelineConstants.TRACK_SIGNAL => new SignalTrack(),
                MiniTimelineConstants.TRACK_UMA_WARDROBE => new UmaWardrobeTrack(),
                MiniTimelineConstants.TRACK_UMA_EXPRESSION => new UMAExpressionTrack(),
                _ => null
            };
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
                if (createdTrack != null)
                {
                    director.RemoveTrack(createdTrack.Id);
                }
                createdTrack = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to undo add track: {ex.Message}");
            }
        }
    }

}