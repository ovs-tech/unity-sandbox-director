using System;
using System.Linq;
using Systems.CommandSystem;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.Tracks;
using UnityEngine;

namespace Systems.MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for adding a new track to the timeline.
    /// Uses Odin-serialized runtime track instances directly (no DTO conversion).
    /// </summary>
    public class AddTrackCommand : CommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly string trackType;
        private readonly string trackName;
        private readonly string bindKey;
        private readonly bool enabled;
        private IMiniTrack createdTrack;

        /// <summary>
        /// Create a new track add command with a track type string.
        /// </summary>
        /// <param name="timelineDirector">The timeline director instance</param>
        /// <param name="type">Track type name (e.g., MiniTimelineConstants.TRACK_ANIM)</param>
        /// <param name="name">Track display name</param>
        /// <param name="bindingKey">Binding key for scene object resolution</param>
        /// <param name="isEnabled">Whether the track is enabled</param>
        public AddTrackCommand(MiniTimelineDirector timelineDirector, string type, string name = "New Track", string bindingKey = "", bool isEnabled = true)
            : base($"Add {type} Track")
        {
            director = timelineDirector;
            trackType = type;
            trackName = name;
            bindKey = string.IsNullOrEmpty(bindingKey) ? "" : bindingKey;
            enabled = isEnabled;
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

                // Generate unique ID and BindKey for the new track
                string trackId = System.Guid.NewGuid().ToString();
                
                // Use reflection to set properties on the track instance
                var idProperty = createdTrack.GetType().GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var nameProperty = createdTrack.GetType().GetProperty("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var bindKeyProperty = createdTrack.GetType().GetProperty("BindKey", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var enabledProperty = createdTrack.GetType().GetProperty("Enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (idProperty?.CanWrite == true)
                {
                    idProperty.SetValue(createdTrack, trackId);
                }
                
                if (nameProperty?.CanWrite == true)
                {
                    nameProperty.SetValue(createdTrack, trackName);
                }
                
                if (bindKeyProperty?.CanWrite == true)
                {
                    // Use provided bindKey or fall back to trackId
                    string finalBindKey = !string.IsNullOrEmpty(bindKey) ? bindKey : trackId;
                    bindKeyProperty.SetValue(createdTrack, finalBindKey);
                }
                
                if (enabledProperty?.CanWrite == true)
                {
                    enabledProperty.SetValue(createdTrack, enabled);
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
