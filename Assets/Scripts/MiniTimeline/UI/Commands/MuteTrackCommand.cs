using Systems.CommandSystem;
using Systems.MiniTimeline.Core;
using UnityEngine;

namespace Systems.MiniTimeline.UI.Commands
{


    /// <summary>
    /// Command for muting/unmuting a track
    /// Supports undo/redo operations for track enabled state
    /// </summary>
    public class MuteTrackCommand : CommandBase
    {
        private readonly IMiniTrack track;
        private readonly bool oldEnabledState;
        private readonly bool newEnabledState;

        public MuteTrackCommand(IMiniTrack trackToMute, bool oldEnabled, bool newEnabled)
            : base(newEnabled ? $"Unmute {trackToMute.Id}" : $"Mute {trackToMute.Id}")
        {
            track = trackToMute;
            oldEnabledState = oldEnabled;
            newEnabledState = newEnabled;
        }

        protected override void ExecuteInternal()
        {
            SetTrackEnabled(newEnabledState);
        }

        protected override void UndoInternal()
        {
            SetTrackEnabled(oldEnabledState);
        }

        public override bool CanMergeWith(ICommand other)
        {
            // Mute commands can be merged if they're for the same track
            if (other is MuteTrackCommand muteCommand)
            {
                return muteCommand.track == track;
            }
            return false;
        }

        public override void MergeWith(ICommand other)
        {
            if (other is MuteTrackCommand muteCommand && muteCommand.track == track)
            {
                // Update our new state to the other command's state
                // The old state stays the same (original before the sequence of mute operations)
                // This would be done through reflection or proper API
                Debug.Log($"Merging mute command for track {track.Id}: old={oldEnabledState}, new={muteCommand.newEnabledState}");
            }
        }

        private void SetTrackEnabled(bool enabled)
        {
            Debug.Log($"SetTrackEnabled - Setting track {track.Id} enabled state to {enabled}");

            // Try to cast to a mutable track interface or use reflection to set the enabled state
            // Check if track has a settable Enabled property
            var trackType = track.GetType();
            var enabledProperty = trackType.GetProperty("Enabled");

            if (enabledProperty != null && enabledProperty.CanWrite)
            {
                Debug.Log($"SetTrackEnabled - Using property setter for track {track.Id}");
                enabledProperty.SetValue(track, enabled);
            }
            else
            {
                // Try to access enabled field directly
                var enabledField = trackType.GetField("enabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (enabledField != null)
                {
                    Debug.Log($"SetTrackEnabled - Using field setter for track {track.Id}");
                    enabledField.SetValue(track, enabled);
                }
                else
                {
                    Debug.LogError($"SetTrackEnabled - Cannot find way to set enabled state for track {track.Id} of type {trackType}");
                }
            }
        }
    }

}
