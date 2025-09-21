using MiniTimeline.Core;
using UnityEngine;

namespace MiniTimeline.UI.Commands
{


    /// <summary>
    /// Command for muting/unmuting a track
    /// Supports undo/redo operations for track enabled state
    /// </summary>
    public class MuteTrackCommand : TimelineCommandBase
    {
        private readonly IMiniTrack track;
        private readonly bool oldEnabledState;
        private readonly bool newEnabledState;
        private readonly TrackUI trackUI;

        public MuteTrackCommand(IMiniTrack trackToMute, bool oldEnabled, bool newEnabled, TrackUI trackInterface)
            : base(newEnabled ? $"Unmute {GetTrackDisplayName(trackToMute)}" : $"Mute {GetTrackDisplayName(trackToMute)}")
        {
            track = trackToMute;
            oldEnabledState = oldEnabled;
            newEnabledState = newEnabled;
            trackUI = trackInterface;
        }

        protected override void ExecuteInternal()
        {
            SetTrackEnabled(newEnabledState);
            UpdateTrackUI();
        }

        protected override void UndoInternal()
        {
            SetTrackEnabled(oldEnabledState);
            UpdateTrackUI();
        }

        public override bool CanMergeWith(ITimelineCommand other)
        {
            // Mute commands can be merged if they're for the same track
            if (other is MuteTrackCommand muteCommand)
            {
                return muteCommand.track == track;
            }
            return false;
        }

        public override void MergeWith(ITimelineCommand other)
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

        private void UpdateTrackUI()
        {
            if (trackUI != null)
            {
                // Update the track header to reflect the new mute state
                trackUI.RefreshUI();
            }
        }

        private static string GetTrackDisplayName(IMiniTrack track)
        {
            if (track == null) return "Unknown Track";

            string trackType = track.GetType().Name;
            string friendlyName = trackType switch
            {
                "AnimTrack" => "Animation",
                "MorphTrack" => "Morph",
                "ExpressionTrack" => "Expression",
                "CameraTrack" => "Camera",
                "PoseIKTrack" => "IK Pose",
                "AudioTrack" => "Audio",
                "FxLightTrack" => "FX Light",
                "EventTrack" => "Events",
                _ => trackType.Replace("Track", "")
            };

            return $"{friendlyName} Track";
        }
    }

}