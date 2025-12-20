using System;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.UI.MVVM.Clip {
    [Serializable]
    public class ClipModel {
        [SerializeField] string _title = "Clip";
        [SerializeField] float _duration = 1f;
        [SerializeField] float _startTime = 0f;
        [SerializeField] bool _locked;
        [SerializeField] bool _muted;
        [SerializeField] bool _selected;

        // Reference to the actual clip data
        private IMiniClip _clip;
        private IMiniTrack _parentTrack;

        // Events
        public event Action OnPropertyChanged;
        public event Action OnSelectionChanged;
        public event Action OnPositionChanged;

        public string Title => _title;
        public float Duration => _duration;
        public float StartTime => _startTime;
        public bool Locked => _locked;
        public bool Muted => _muted;
        public bool Selected => _selected;
        public IMiniClip Clip => _clip;
        public IMiniTrack ParentTrack => _parentTrack;

        public void Initialize(IMiniClip clip, IMiniTrack parentTrack) {
            _clip = clip;
            _parentTrack = parentTrack;
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
            OnSelectionChanged?.Invoke();
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

        // Interaction feedback
        public void OnDragStart() {
            // Called when user starts dragging clip
        }

        public void OnDragEnd() {
            // Called when user finishes dragging clip
        }

        public void OnContextMenu() {
            // Called when user right-clicks clip
        }
    }
}