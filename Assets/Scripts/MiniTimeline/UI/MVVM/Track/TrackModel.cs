using System;
using System.Collections.Generic;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.UI.MVVM.Track {
    [Serializable]
    public class TrackModel {
        [SerializeField] string _title = "Track";
        [SerializeField] bool _enabled = true;
        [SerializeField] bool _muted;
        [SerializeField] bool _solo;
        [SerializeField] string _bindKey = string.Empty;
        [SerializeField] string _type = "Generic";
        [SerializeField] MiniTimelineDirector _director;
        [SerializeField] float _zoom = 1f;
        [SerializeField] float _pixelsPerSecond = 100f;

        // Reference to the actual track data
        private IMiniTrack _track;
        private List<IMiniClip> _clips = new List<IMiniClip>();

        // Events for state changes
        public event Action OnTitleChanged;
        public event Action OnStateChanged;
        public event Action OnClipsChanged;
        public event Action OnZoomChanged;
        public event Action OnTimelineWidthChanged;
        public event Action OnPixelsPerSecondChanged;

        public string Title => _title;
        public bool Enabled => _enabled;
        public bool Muted => _muted;
        public bool Solo => _solo;
        public string BindKey => _bindKey;
        public string Type => _type;
        public IMiniTrack Track => _track;
        public IReadOnlyList<IMiniClip> Clips => _clips.AsReadOnly();
        public float Zoom => _zoom;
        public float TimelineWidth => _director != null ? _director.Length * _pixelsPerSecond : 1000f;
        public float PixelsPerSecond => _pixelsPerSecond;

        public void Initialize(IMiniTrack track, MiniTimelineDirector director) {
            _track = track;
            _director = director;
            if (track != null) {
                _title = track.Id;
                _enabled = track.Enabled;
                _bindKey = track.BindKey ?? string.Empty;
                _type = track.GetType().Name;
                RefreshClips();
            }
        }

        public void RefreshClips() {
            if (_track != null) {
                _clips.Clear();
                var clips = _track.GetClips();
                if (clips != null) {
                    _clips.AddRange(clips);
                }
                OnClipsChanged?.Invoke();
            }
        }

        public void ToggleEnabled() { 
            _enabled = !_enabled;
            if (_track != null) _track.Enabled = _enabled;
            OnStateChanged?.Invoke();
        }

        public void Mute() { 
            _muted = !_muted;
            OnStateChanged?.Invoke();
        }

        public void ToggleSolo() { 
            _solo = !_solo;
            OnStateChanged?.Invoke();
        }

        public void SetTitle(string title) { 
            _title = title;
            OnTitleChanged?.Invoke();
        }

        public void SetBindKey(string key) { 
            _bindKey = key;
            OnStateChanged?.Invoke();
        }

        public void SetType(string type) { 
            _type = type;
        }

        public void SetZoom(float zoom) {
            _zoom = Mathf.Clamp(zoom, 0.1f, 5f);
            OnZoomChanged?.Invoke();
        }

        public void SetPixelsPerSecond(float pixelsPerSecond) {
            _pixelsPerSecond = Mathf.Max(1f, pixelsPerSecond);
            OnPixelsPerSecondChanged?.Invoke();
        }

        // Clip management
        public void AddClip(IMiniClip clip) {
            if (clip != null && !_clips.Contains(clip)) {
                _clips.Add(clip);
                OnClipsChanged?.Invoke();
            }
        }

        public void RemoveClip(IMiniClip clip) {
            if (clip != null && _clips.Remove(clip)) {
                OnClipsChanged?.Invoke();
            }
        }

        public bool ContainsClip(IMiniClip clip) => _clips.Contains(clip);
    }
}