using Systems.CommandSystem;
using Systems.MiniTimeline.Core;
using UnityEngine;

namespace Systems.MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command to add a clip to a track with undo/redo support
    /// </summary>
    public class AddClipCommand : CommandBase
    {
        private readonly MiniTimelineDirector director;
        private readonly string trackId;
        private readonly IMiniClip clipInstance;

        public AddClipCommand(MiniTimelineDirector director, string trackId, IMiniClip clip)
            : base($"Add Clip")
        {
            this.director = director;
            this.trackId = trackId;
            clipInstance = clip;
        }

        protected override void ExecuteInternal()
        {
            // Add clip to track via director
            bool success = director.AddClip(clipInstance, trackId);
            
            if (!success)
            {
                Debug.LogError($"Failed to add clip '{clipInstance.Id}' to track '{trackId}'");
            }
        }

        protected override void UndoInternal()
        {
            if (clipInstance != null && director != null)
            {
                // Remove clip from track via director
                bool removed = director.RemoveClip(clipInstance.Id, trackId);

                if (!removed)
                {
                    Debug.LogWarning($"Failed to remove clip '{clipInstance.Id}' from track '{trackId}'");
                }
            }
        }
    }
}
