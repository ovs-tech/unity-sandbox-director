using MiniTimeline.Core;
using UnityEngine;
using System;
using System.Collections.Generic;
using Core.Behaviors.Command;

namespace MiniTimeline.UI.Commands
{

    /// <summary>
    /// Command for deleting a clip from a track
    /// </summary>
    public class DeleteClipCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly IMiniClip clip;
        private readonly ClipData clipBackup;
        private readonly TrackUI trackUI;

        public DeleteClipCommand(IMiniTrack sourceTrack, IMiniClip clipToDelete, TrackUI trackInterface)
            : base($"Delete {clipToDelete.Id}")
        {
            track = sourceTrack;
            clip = clipToDelete;
            trackUI = trackInterface;

            // Backup clip data for undo
            clipBackup = new ClipData
            {
                id = clip.Id,
                start = clip.Start,
                duration = clip.Duration,
                payload = new Dictionary<string, object>()
                // TODO: Need to properly serialize the clip's payload data
                // This would require access to each clip type's specific data
            };
        }

        protected override void ExecuteInternal()
        {
            bool success = false;
            
            try
            {
                // Try to call RemoveClip using reflection
                var removeMethod = track.GetType().GetMethod("RemoveClip", new Type[] { typeof(string) });
                if (removeMethod != null)
                {
                    var result = removeMethod.Invoke(track, new object[] { clip.Id });
                    success = (bool)result;
                }
                else
                {
                    Debug.LogWarning($"Track type {track.GetType().Name} doesn't have RemoveClip method");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove clip {clip.Id}: {ex.Message}");
            }

            if (success)
            {
                // Update UI - rebuild the entire track UI to ensure consistency
                trackUI?.RebuildClipUIs();
                Debug.Log($"Successfully deleted clip {clip.Id}");
            }
            else
            {
                Debug.LogError($"Failed to delete clip {clip.Id}");
            }
        }

        protected override void UndoInternal()
        {
            // TODO: Recreate clip through proper track API
            // This is more complex because we need to create the appropriate clip type
            // and restore all its properties from the backup data
            
            Debug.LogWarning($"Undo delete clip not fully implemented yet for {clip.Id}");
            
            // For now, just refresh the UI in case anything changed
            trackUI?.RefreshUI();
        }
    }

}