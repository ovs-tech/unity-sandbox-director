using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiniTimeline.Core;
using MiniTimeline.Serialization;
using MiniTimeline.UI.Commands;
using Core.Behaviors.Command;

namespace MiniTimeline.UI.MVVM.Timeline {
    /// <summary>
    /// Model for Timeline Editor holding state, command manager, and director reference.
    /// Manages playback, zoom, time, selection, and undo/redo functionality.
    /// </summary>
    [Serializable]
    public class TimelineEditorModel {
        [SerializeField] float _time;
        [SerializeField] float _zoom = 1f;
        [SerializeField] bool _isPlaying;
        [SerializeField] string _statusText = "Ready";

        // Director and Command Manager references (non-serialized)
        private MiniTimelineDirector _director;
        private TimelineCommandManager _commandManager;

        // Zoom constraints
        [SerializeField] float _minZoom = 0.1f;
        [SerializeField] float _maxZoom = 5f;
        [SerializeField] float _pixelsPerSecond = 100f;
        [SerializeField] bool _enableFrameSnap = true;

        // Selection state
        private readonly List<IMiniClip> _selectedClips = new List<IMiniClip>();
        
        // Events for state changes
        public event Action OnCommandExecuted;
        public event Action OnUndoPerformed;
        public event Action OnRedoPerformed;
        public event Action OnCommandStacksChanged;

        public float Time => _time;
        public float Zoom => _zoom;
        public bool IsPlaying => _isPlaying;
        public string StatusText => _statusText;
        
        public MiniTimelineDirector Director => _director;
        public TimelineCommandManager CommandManager => _commandManager;
        public IReadOnlyList<IMiniClip> SelectedClips => _selectedClips.AsReadOnly();
        public float MinZoom => _minZoom;
        public float MaxZoom => _maxZoom;
        public float PixelsPerSecond => _pixelsPerSecond * _zoom;
        public bool EnableFrameSnap => _enableFrameSnap;

        public TimelineEditorModel() {
            _commandManager = new TimelineCommandManager();
            SubscribeToCommandManager();
        }

        public void Initialize(MiniTimelineDirector director) {
            _director = director;
        }

        // Command Manager event subscriptions
        private void SubscribeToCommandManager() {
            if (_commandManager == null) return;
            _commandManager.OnCommandExecuted += () => OnCommandExecuted?.Invoke();
            _commandManager.OnUndoPerformed += () => OnUndoPerformed?.Invoke();
            _commandManager.OnRedoPerformed += () => OnRedoPerformed?.Invoke();
            _commandManager.OnStacksChanged += () => OnCommandStacksChanged?.Invoke();
        }

        // Playback control
        public void SetTime(float time) { 
            _time = Mathf.Max(0f, time);
            if (_director != null) _director.Seek(_time);
        }
        
        public void SetZoom(float zoom) { 
            _zoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
        }
        
        public void Play() { 
            _isPlaying = true; 
            _statusText = "Playing";
            _director?.Play();
        }
        
        public void Pause() { 
            _isPlaying = false; 
            _statusText = "Paused";
            _director?.Pause();
        }
        
        public void Stop() { 
            _isPlaying = false; 
            _statusText = "Stopped"; 
            _time = 0f;
            _director?.Stop();
        }

        // Command execution interface
        public void ExecuteCommand(ITimelineCommand command, bool allowMerge = false) {
            _commandManager?.ExecuteCommand(command, allowMerge);
        }

        public void Undo() => _commandManager?.Undo();
        public void Redo() => _commandManager?.Redo();
        public void ClearCommandHistory() => _commandManager?.Clear();

        public bool CanUndo => _commandManager?.CanUndo ?? false;
        public bool CanRedo => _commandManager?.CanRedo ?? false;

        // Clip selection management
        public void SelectClip(IMiniClip clip) {
            if (!_selectedClips.Contains(clip)) {
                _selectedClips.Clear(); // Single selection for now
                _selectedClips.Add(clip);
            }
        }

        public void ClearSelection() => _selectedClips.Clear();

        public bool IsClipSelected(IMiniClip clip) => _selectedClips.Contains(clip);

        // Time/Position conversion utilities
        public float PositionToTime(float xPosition) => xPosition / PixelsPerSecond;
        public float TimeToPosition(float time) => time * PixelsPerSecond;
        
        public float SnapTime(float time) {
            if (!_enableFrameSnap || _director?.Project == null) return time;
            float frameRate = _director.Project.frameRate;
            return Mathf.Round(time * frameRate) / frameRate;
        }

        // Cleanup
        public void Dispose() {
            if (_commandManager != null) {
                _commandManager.OnCommandExecuted -= () => OnCommandExecuted?.Invoke();
                _commandManager.OnUndoPerformed -= () => OnUndoPerformed?.Invoke();
                _commandManager.OnRedoPerformed -= () => OnRedoPerformed?.Invoke();
                _commandManager.OnStacksChanged -= () => OnCommandStacksChanged?.Invoke();
            }
        }

        // Track management commands
        public void AddTrack(string trackType, string trackName, string bindKey = "", bool enabled = true) {
            if (_director == null || _director.Project == null) {
                Debug.LogError("Cannot add track: No director or project");
                return;
            }

            string trackId = Guid.NewGuid().ToString();
            int trackOrder = _director.Project.tracks.Count > 0 
                ? _director.Project.tracks.Max(t => t.order) + 1 
                : 0;

            var trackData = new TrackData {
                id = trackId,
                type = trackType,
                bindKey = bindKey,
                enabled = enabled,
                order = trackOrder,
                clips = new List<ClipData>()
            };

            var addTrackCommand = new AddTrackCommand(_director, trackData, null);
            ExecuteCommand(addTrackCommand);
        }

        public void RemoveTrack(IMiniTrack track) {
            if (_director == null || track == null) {
                Debug.LogError("Cannot remove track: Director or track is null");
                return;
            }

            // var removeCommand = new RemoveTrackCommand(_director, track, null);
            // ExecuteCommand(removeCommand);
            Debug.Log($"Remove track: {track.Id} - TODO: Implement RemoveTrackCommand");
        }

        public void AddClipToTrack(IMiniTrack track, IMiniClip clip) {
            if (track == null) {
                Debug.LogError("Cannot add clip: Track is null");
                return;
            }

            track.AddClip(clip);
            OnCommandExecuted?.Invoke();
        }

        public void RemoveClipFromTrack(IMiniTrack track, IMiniClip clip) {
            if (track == null || clip == null) {
                Debug.LogError("Cannot remove clip: Track or clip is null");
                return;
            }

            track.RemoveClip(clip);
            OnCommandExecuted?.Invoke();
        }
    }
}