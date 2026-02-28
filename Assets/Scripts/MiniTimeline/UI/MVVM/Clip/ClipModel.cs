using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.CommandSystem;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.UI.Commands;

namespace Systems.MiniTimeline.UI.MVVM.Clip {
    public class ClipModel {
        string _title = "Clip";
        float _duration = 1f;
        float _startTime = 0f;
        bool _locked;
        bool _muted;
        bool _selected;
        float _zoom = 1f;
        float _pixelsPerSecond = 100f;

        // Reference to the actual clip data
        private IMiniClip _clip;
        private IMiniTrack _parentTrack;
        private MiniTimelineDirector _director;

        // Events
        public event Action OnPropertyChanged;
        public event Action<bool> OnSelectionChanged;
        public event Action OnPositionChanged;
        public event Action OnZoomChanged;
        public event Action OnPixelsPerSecondChanged;

        public string Title => _title;
        public float Duration => _duration;
        public float StartTime => _startTime;
        public bool Locked => _locked;
        public bool Muted => _muted;
        public bool Selected => _selected;
        public IMiniClip Clip => _clip;
        public IMiniTrack ParentTrack => _parentTrack;
        public float Zoom => _zoom;
        public float PixelsPerSecond => _pixelsPerSecond;

        public ClipModel() {
        }

        public void Initialize(IMiniClip clip, IMiniTrack parentTrack, MiniTimelineDirector director = null) {
            _clip = clip;
            _parentTrack = parentTrack;
            _director = director;
            if (clip != null) {
                _title = clip.Id;
                _duration = clip.Duration;
                _startTime = clip.Start;
            }
        }

        public void SetTitle(string title) { 
            _title = title;
            OnPropertyChanged?.Invoke();
        }

        public void SetDuration(float value) { 
            _duration = Mathf.Max(0f, value);
            if (_clip != null && _clip is MiniClipBase clipBase) {
                clipBase.Duration = _duration;
            }
            OnPropertyChanged?.Invoke();
        }

        public void SetStartTime(float time) {
            _startTime = Mathf.Max(0f, time);
            if (_clip != null && _clip is MiniClipBase clipBase) {
                clipBase.Start = _startTime;
            }
            OnPositionChanged?.Invoke();
        }

        public void ToggleLocked() { 
            _locked = !_locked;
            OnPropertyChanged?.Invoke();
        }

        public void ToggleMuted() { 
            _muted = !_muted;
            OnPropertyChanged?.Invoke();
        }

        public void Select(bool selected) { 
            _selected = selected;
            OnSelectionChanged?.Invoke(selected);
        }

        // Drag/Resize operations
        public void ResizeLeft(float delta) { 
            float newStart = Mathf.Max(0f, _startTime + delta);
            float newDuration = _duration - delta;
            if (newDuration > 0.1f) {
                _startTime = newStart;
                _duration = newDuration;
                SetStartTime(_startTime);
                SetDuration(_duration);
            }
        }

        public void ResizeRight(float delta) { 
            float newDuration = Mathf.Max(0.1f, _duration + delta);
            _duration = newDuration;
            SetDuration(_duration);
        }

        public void Drag(float delta) {
            float newStart = Mathf.Max(0f, _startTime + delta);
            _startTime = newStart;
            SetStartTime(_startTime);
        }

        public void SetZoom(float zoom) {
            _zoom = zoom;
            OnZoomChanged?.Invoke();
        }

        public void SetPixelsPerSecond(float pixelsPerSecond) {
            _pixelsPerSecond = pixelsPerSecond;
            OnPixelsPerSecondChanged?.Invoke();
        }

        /// <summary>
        /// Commit a move (drag) action through the command manager for undo/redo support.
        /// </summary>
        public void CommitMove(float originalStart, float newStart, bool allowMerge = false) {
            if (_clip == null) return;

            float clampedNewStart = Mathf.Max(0f, newStart);
            if (Mathf.Approximately(originalStart, clampedNewStart)) return;

            SetStartTime(clampedNewStart);
            var command = new MoveClipCommand(_clip, originalStart, clampedNewStart);
            ExecuteCommand(command, allowMerge);
        }

        /// <summary>
        /// Commit a resize action through the command manager for undo/redo support.
        /// </summary>
        public void CommitResize(float originalStart, float originalDuration, float newStart, float newDuration, bool allowMerge = false) {
            if (_clip == null) return;

            float clampedStart = Mathf.Max(0f, newStart);
            float clampedDuration = Mathf.Max(0.1f, newDuration);

            if (Mathf.Approximately(originalStart, clampedStart) && Mathf.Approximately(originalDuration, clampedDuration)) {
                SetStartTime(clampedStart);
                SetDuration(clampedDuration);
                return;
            }

            SetStartTime(clampedStart);
            SetDuration(clampedDuration);
            var command = new ResizeClipCommand(_clip, originalStart, originalDuration, clampedStart, clampedDuration);
            ExecuteCommand(command, allowMerge);
        }

        /// <summary>
        /// Apply arbitrary clip property changes via EditClipCommand for undo/redo.
        /// Keys should match clip property/field names (e.g., name, start, duration).
        /// </summary>
        public void ApplyEdit(Dictionary<string, object> newValues) {
            if (_clip == null || newValues == null || newValues.Count == 0) return;

            var command = new EditClipCommand(_clip, newValues);
            ExecuteCommand(command);

            if (newValues.TryGetValue("name", out var nameVal)) {
                var newName = nameVal?.ToString() ?? string.Empty;
                if (_title != newName) {
                    _title = newName;
                    OnPropertyChanged?.Invoke();
                }
            }

            if (newValues.TryGetValue("start", out var startVal) && float.TryParse(startVal.ToString(), out var newStart)) {
                SetStartTime(newStart);
            }

            if (newValues.TryGetValue("duration", out var durVal) && float.TryParse(durVal.ToString(), out var newDur)) {
                SetDuration(newDur);
            }
        }

        void ExecuteCommand(ICommand command, bool allowMerge = false) {
            if (command == null) return;

            var commandManager = CommandManager.Instance;
            if (commandManager != null) {
                commandManager.ExecuteCommand(command, allowMerge);
            } else {
                command.Execute();
            }
        }
    }
}
