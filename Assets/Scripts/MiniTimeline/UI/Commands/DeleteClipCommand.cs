using System;
using Systems.CommandSystem;
using Systems.MiniTimeline.Core;
using UnityEngine;

namespace Systems.MiniTimeline.UI.Commands
{
    /// <summary>
    /// Command for deleting a clip from a track.
    /// Supports undo/redo operations for clip deletion.
    /// </summary>
    public class DeleteClipCommand : CommandBase
    {
        private readonly IMiniTrack track;
        private readonly IMiniClip clip;
        private readonly IMiniClip clipBackup;

        public DeleteClipCommand(IMiniTrack sourceTrack, IMiniClip clipToDelete)
            : base($"Delete {clipToDelete.Id}")
        {
            track = sourceTrack;
            clip = clipToDelete;
            // Backup the clip instance for undo
            clipBackup = clipToDelete;
        }

        protected override void ExecuteInternal()
        {
            if (track == null || clip == null)
            {
                Debug.LogError("DeleteClipCommand: Invalid track or clip");
                return;
            }

            try
            {
                bool success = track.RemoveClip(clip);
                if (success)
                {
                    Debug.Log($"Successfully deleted clip {clip.Id}");
                }
                else
                {
                    Debug.LogError($"Failed to delete clip {clip.Id}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove clip {clip.Id}: {ex.Message}");
            }
        }

        protected override void UndoInternal()
        {
            if (track == null || clipBackup == null)
            {
                Debug.LogError("DeleteClipCommand: Cannot restore clip - invalid track or backup");
                return;
            }

            try
            {
                track.AddClip(clipBackup);
                Debug.Log($"Restored clip {clipBackup.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to restore clip: {ex.Message}");
            }
        }
    }

}
