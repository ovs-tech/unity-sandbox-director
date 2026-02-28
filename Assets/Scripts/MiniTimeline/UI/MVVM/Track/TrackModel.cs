using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Systems.CommandSystem;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.UI.Commands;

namespace Systems.MiniTimeline.UI.MVVM.Track
{
    public class TrackModel
    {
        string _title = "Track";
        bool _enabled = true;
        bool _muted;
        bool _solo;
        string _bindKey = string.Empty;
        string _type = "Generic";
        MiniTimelineDirector _director;
        float _zoom = 1f;
        float _pixelsPerSecond = 100f;

        // Reference to the actual track data
        private IMiniTrack _track;
        private List<IMiniClip> _clips = new List<IMiniClip>();

        // Events for state changes
        public event Action OnTitleChanged;
        public event Action OnStateChanged;
        public event Action OnClipsChanged;
        public event Action OnZoomChanged;

        public string Title => _title;
        public bool Enabled => _enabled;
        public bool Muted => _muted;
        public bool Solo => _solo;
        public string BindKey => _bindKey;
        public string Type => _type;
        public IMiniTrack Track => _track;
        public IReadOnlyList<IMiniClip> Clips => _clips.AsReadOnly();
        public float Zoom
        {
            get => _zoom;
            set
            {
                if (_zoom != value)
                {
                    _zoom = value;
                    OnZoomChanged?.Invoke();
                }
            }
        }
        public float TimelineWidth => _director != null ? _director.Length * _pixelsPerSecond : 1000f;
        public float PixelsPerSecond => _pixelsPerSecond;
        public MiniTimelineDirector Director => _director;

        public TrackModel()
        {
        }

        public void Initialize(IMiniTrack track, MiniTimelineDirector director)
        {
            _track = track;
            _director = director;
            if (track != null)
            {
                _title = !string.IsNullOrEmpty(track.Name) ? track.Name : track.Id;
                _enabled = track.Enabled;
                _bindKey = track.BindKey ?? string.Empty;
                _type = track.GetType().Name;
                RefreshClips();
            }
        }

        public void RefreshClips()
        {
            if (_track != null)
            {
                _clips.Clear();
                var clips = _track.GetClips();
                if (clips != null)
                {
                    _clips.AddRange(clips);
                }
                OnClipsChanged?.Invoke();
            }
        }

        public void SetEnabled(bool enabled)
        {
            bool newEnabled = enabled;
            ExecuteTrackSettingsCommand(new Dictionary<string, object> { { "enabled", newEnabled } });

            bool stateChanged = _enabled != newEnabled;
            _enabled = newEnabled;
            if (_track != null && CommandManager.Instance == null)
            {
                _track.Enabled = newEnabled;
            }

            if (stateChanged) OnStateChanged?.Invoke();
        }

        public void Mute()
        {
            _muted = !_muted;
            OnStateChanged?.Invoke();
        }

        public void ToggleSolo()
        {
            _solo = !_solo;
            OnStateChanged?.Invoke();
        }

        public void SetTitle(string title)
        {
            string newTitle = title ?? string.Empty;
            ExecuteTrackSettingsCommand(new Dictionary<string, object> { { "Name", newTitle } });

            if (_title != newTitle)
            {
                _title = newTitle;
                OnTitleChanged?.Invoke();
            }
        }

        public void SetBindKey(string key)
        {
            string newKey = key ?? string.Empty;
            ExecuteTrackSettingsCommand(new Dictionary<string, object> { { "bindKey", newKey } }, allowMerge: true);

            bool stateChanged = _bindKey != newKey;
            _bindKey = newKey;
            if (stateChanged) OnStateChanged?.Invoke();
        }

        public void SetType(string type)
        {
            _type = type;
        }

        public void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, 0.1f, 5f);
            OnZoomChanged?.Invoke();
        }

        public void SetPixelsPerSecond(float pixelsPerSecond)
        {
            _pixelsPerSecond = Mathf.Max(1f, pixelsPerSecond);
            OnZoomChanged?.Invoke();
        }

        // Clip management
        public void AddClip(IMiniClip clip)
        {
            if (clip == null || _clips.Contains(clip))
                return;

            if (_track == null || _director == null)
            {
                Debug.LogWarning("Cannot add clip: Track or Director is null");
                return;
            }

            // Create and execute command for undo/redo support
            var command = new AddClipCommand(_director, _track.Id, clip);
            var commandManager = CommandManager.Instance;
            
            if (commandManager != null)
            {
                commandManager.ExecuteCommand(command, false);
            }
            else
            {
                command.Execute();
            }

            // Update local state
            if (!_clips.Contains(clip))
            {
                _clips.Add(clip);
                OnClipsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Creates and adds a clip from form data dictionary
        /// </summary>
        public IMiniClip AddClip(Dictionary<string, object> formData)
        {
            if (formData == null || _track == null || _director == null)
            {
                Debug.LogError("Cannot create clip: Invalid parameters");
                return null;
            }

            try
            {
                // Extract clip parameters from form data
                string clipId = formData.ContainsKey("id") ? formData["id"].ToString() : Guid.NewGuid().ToString();
                float start = formData.ContainsKey("start") ? Convert.ToSingle(formData["start"]) : 0f;
                float duration = formData.ContainsKey("duration") ? Convert.ToSingle(formData["duration"]) : 1f;

                // Create clip instance based on track type
                IMiniClip newClip = CreateClipForTrackType(clipId, start, duration, formData);

                if (newClip != null)
                {
                    // Add clip using the command pattern
                    AddClip(newClip);
                }

                return newClip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create clip from form data: {ex.Message}");
                return null;
            }
        }

        private IMiniClip CreateClipForTrackType(string clipId, float start, float duration, Dictionary<string, object> formData)
        {
            if (string.IsNullOrEmpty(_type))
            {
                Debug.LogError("Cannot create clip: Track type is not set");
                return null;
            }

            try
            {
                IMiniClip newClip = null;

                // Create clip based on track type
                switch (_type)
                {
                    case "AnimatorTrack":
                        newClip = new MiniTimeline.Tracks.AnimatorClip
                        {
                            Id = clipId,
                            Start = start,
                            Duration = duration
                        };
                        break;

                    case "AnimTrack":
                        newClip = new MiniTimeline.Tracks.AnimClip
                        {
                            Id = clipId,
                            Start = start,
                            Duration = duration
                        };
                        break;

                    case "MovementTrack":
                        newClip = new MiniTimeline.Tracks.MovementClip
                        {
                            Id = clipId,
                            Start = start,
                            Duration = duration
                        };
                        break;

                    case "SignalTrack":
                        newClip = new MiniTimeline.Tracks.SignalClip
                        {
                            Id = clipId,
                            Start = start,
                            Duration = duration
                        };
                        break;

                    default:
                        Debug.LogWarning($"Clip creation not implemented for track type: {_type}");
                        return null;
                }

                if (newClip != null)
                {
                    Debug.Log($"Created {_type} clip '{clipId}' at {start}s for {duration}s");
                }

                return newClip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create clip for track type '{_type}': {ex.Message}");
                return null;
            }
        }

        public void RemoveClip(IMiniClip clip)
        {
            if (clip != null && _clips.Remove(clip))
            {
                OnClipsChanged?.Invoke();
            }
        }

        public bool ContainsClip(IMiniClip clip) => _clips.Contains(clip);

        /// <summary>
        /// Delete this track through the command manager for undo/redo support.
        /// </summary>
        public void DeleteTrack()
        {
            if (_director == null || _track == null)
            {
                Debug.LogError("Cannot delete track: Director or Track is null");
                return;
            }
            var cmd = new RemoveTrackCommand(_director, _track);
            var commandManager = CommandManager.Instance;
            if (commandManager != null)
            {
                commandManager.ExecuteCommand(cmd, false);
            }
            else
            {
                cmd.Execute();
            }
        }

        /// <summary>
        /// Apply one or more track setting changes via the CommandManager so they are undoable.
        /// </summary>
        public void ApplySettings(Dictionary<string, object> formData, bool allowMerge = true)
        {
            if (formData == null || formData.Count == 0) return;

            var newSettings = new Dictionary<string, object>();
            foreach (var kvp in formData)
            {
                switch (kvp.Key)
                {
                    case "trackName":
                        newSettings["Name"] = kvp.Value?.ToString() ?? string.Empty;
                        break;
                    default:
                        newSettings[kvp.Key] = kvp.Value;
                        break;
                }
            }

            ExecuteTrackSettingsCommand(newSettings, allowMerge);

            bool stateChanged = false;

            if (newSettings.TryGetValue("Name", out var nameValue))
            {
                string newTitle = nameValue?.ToString() ?? string.Empty;
                if (_title != newTitle)
                {
                    _title = newTitle;
                    OnTitleChanged?.Invoke();
                }
            }

            if (newSettings.TryGetValue("bindKey", out var bindKeyValue))
            {
                string newBindKey = bindKeyValue?.ToString() ?? string.Empty;
                if (_bindKey != newBindKey)
                {
                    _bindKey = newBindKey;
                    stateChanged = true;
                }
            }

            if (newSettings.TryGetValue("enabled", out var enabledValue))
            {
                bool newEnabled = Convert.ToBoolean(enabledValue);
                if (_enabled != newEnabled)
                {
                    _enabled = newEnabled;
                    stateChanged = true;
                }
            }

            if (stateChanged) OnStateChanged?.Invoke();
        }

        Dictionary<string, object> CaptureCurrentSettings(IEnumerable<string> settingKeys)
        {
            var settings = new Dictionary<string, object>();
            if (settingKeys == null) return settings;

            foreach (var key in settingKeys)
            {
                switch (key)
                {
                    case "enabled":
                        settings[key] = _track?.Enabled ?? _enabled;
                        break;
                    case "bindKey":
                        settings[key] = _track?.BindKey ?? _bindKey;
                        break;
                    case "Name":
                    case "trackName":
                        settings[key] = GetTrackMemberValue("Name", _title);
                        break;
                    default:
                        settings[key] = GetTrackMemberValue(key);
                        break;
                }
            }

            return settings;
        }

        object GetTrackMemberValue(string memberName, object fallback = null)
        {
            if (_track == null || string.IsNullOrEmpty(memberName)) return fallback;

            var trackType = _track.GetType();
            var property = trackType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanRead)
            {
                return property.GetValue(_track);
            }

            var field = trackType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(_track);
            }

            return fallback;
        }

        void ExecuteTrackSettingsCommand(Dictionary<string, object> newSettings, bool allowMerge = false)
        {
            if (newSettings == null || newSettings.Count == 0) return;

            if (_track == null)
            {
                Debug.LogWarning("Cannot apply track settings: Track is null");
                return;
            }

            var oldSettings = CaptureCurrentSettings(newSettings.Keys);
            var command = new UpdateTrackSettingsCommand(_track, oldSettings, newSettings, _director);

            var commandManager = CommandManager.Instance;
            if (commandManager != null)
            {
                commandManager.ExecuteCommand(command, allowMerge);
            }
            else
            {
                command.Execute();
            }
        }
    }
}
