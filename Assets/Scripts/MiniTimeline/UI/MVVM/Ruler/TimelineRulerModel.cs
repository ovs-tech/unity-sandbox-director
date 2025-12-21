using System;
using System.Collections.Generic;
using UnityEngine;
using MiniTimeline.Core;

namespace MiniTimeline.UI.MVVM.Ruler {
    [Serializable]
    public class TimelineRulerModel {
        [SerializeField] float _currentTime;
        [SerializeField] float _zoom = 1f;
        [SerializeField] float _length = 10f;
        [SerializeField] int _frameRate = 30;
        [SerializeField] float _pixelsPerSecond = 100f;

        private List<TimelineMarker> _markers = new List<TimelineMarker>();
        private MiniTimelineDirector _director;

        // Events
        public event Action OnTimeChanged;
        public event Action OnZoomChanged;
        public event Action OnMarkersChanged;

        public float CurrentTime => _currentTime;
        public float Zoom => _zoom;
        public float Length => _length;
        public int FrameRate => _frameRate;
        public float PixelsPerSecond => _pixelsPerSecond * _zoom;
        public IReadOnlyList<TimelineMarker> Markers => _markers.AsReadOnly();

        public void SetTime(float t) { 
            _currentTime = Mathf.Max(0f, t);
            OnTimeChanged?.Invoke();
        }

        public void SetZoom(float z) { 
            _zoom = Mathf.Clamp(z, 0.1f, 5f);
            GenerateMarkers();
            OnZoomChanged?.Invoke();
        }

        public void Initialize(float length, int frameRate) {
            _length = length;
            _frameRate = frameRate;
            GenerateMarkers();
        }

        public void GenerateMarkers() {
            _markers.Clear();
            
            if (_length <= 0) return;

            float markerInterval = CalculateMarkerInterval();
            float pixelsPerSecond = PixelsPerSecond;

            // Generate major and minor markers
            for (float time = 0f; time <= _length; time += markerInterval * 0.1f) {
                bool isMajor = Mathf.Approximately(time % markerInterval, 0f);
                float position = time * pixelsPerSecond;
                
                _markers.Add(new TimelineMarker {
                    time = time,
                    position = position,
                    label = FormatTimeLabel(time),
                    isMajor = isMajor,
                    height = isMajor ? 20f : 10f
                });
            }

            OnMarkersChanged?.Invoke();
        }

        private float CalculateMarkerInterval() {
            // Dynamically calculate marker interval based on zoom level
            // At zoom 1, markers every second; scales with zoom
            float baseInterval = 1f; // 1 second
            float scaledInterval = baseInterval / _zoom;
            
            // Clamp to reasonable ranges
            if (scaledInterval < 0.1f) scaledInterval = 0.1f;
            if (scaledInterval > 10f) scaledInterval = 10f;
            
            return scaledInterval;
        }

        private string FormatTimeLabel(float time) {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int frames = Mathf.FloorToInt((time % 1f) * _frameRate);
            return $"{minutes:00}:{seconds:00}:{frames:00}";
        }

        public float SnapTime(float time, float snapThreshold = 0.05f) {
            // Snap to nearest frame
            if (_frameRate > 0) {
                float frameTime = 1f / _frameRate;
                float snappedTime = Mathf.Round(time / frameTime) * frameTime;
                
                if (Mathf.Abs(snappedTime - time) < snapThreshold) {
                    return snappedTime;
                }
            }
            return time;
        }

        public float PositionToTime(float position) {
            return position / PixelsPerSecond;
        }

        public float TimeToPosition(float time) {
            return time * PixelsPerSecond;
        }

        // Director binding
        public void BindDirector(MiniTimelineDirector director) {
            _director = director;
            if (_director == null) return;

            _director.OnTimeChanged += OnDirectorTimeChanged;
            _director.OnProjectLoaded += OnDirectorProjectLoaded;
            _director.OnProjectClosed += OnDirectorProjectClosed;

            // Initialize from current director state
            _length = _director.Length;
            _frameRate = Mathf.RoundToInt(_director.Project?.frameRate ?? _frameRate);
            GenerateMarkers();
        }

        public void UnbindDirector() {
            if (_director == null) return;
            _director.OnTimeChanged -= OnDirectorTimeChanged;
            _director.OnProjectLoaded -= OnDirectorProjectLoaded;
            _director.OnProjectClosed -= OnDirectorProjectClosed;
            _director = null;
        }

        private void OnDirectorTimeChanged(float time) {
            SetTime(time);
        }

        private void OnDirectorProjectLoaded() {
            if (_director == null) return;
            _length = _director.Length;
            _frameRate = Mathf.RoundToInt(_director.Project?.frameRate ?? _frameRate);
            GenerateMarkers();
        }

        private void OnDirectorProjectClosed() {
            _length = 0f;
            _markers.Clear();
            OnMarkersChanged?.Invoke();
        }
    }

    [Serializable]
    public class TimelineMarker {
        public float time;
        public float position;
        public string label;
        public bool isMajor;
        public float height;
    }
}