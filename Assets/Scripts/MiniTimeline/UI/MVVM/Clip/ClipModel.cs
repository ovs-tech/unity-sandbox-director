using System;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.UI.MVVM.Clip {
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
                // _director.UpdateClip(_clip.Id, _parentTrack.Id, _clip.Start, _clip.Duration);
            }
            OnPropertyChanged?.Invoke();
        }

        public void SetStartTime(float time) {
            _startTime = Mathf.Max(0f, time);
            if (_clip != null && _clip is MiniClipBase clipBase) {
                clipBase.Start = _startTime;
                // _director.UpdateClip(_clip.Id, _parentTrack.Id, _clip.Start, _clip.Duration);
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
    }
}