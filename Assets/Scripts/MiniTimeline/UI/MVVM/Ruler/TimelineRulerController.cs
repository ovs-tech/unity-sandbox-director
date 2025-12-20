using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniTimeline.UI.MVVM.Ruler {
    /// <summary>
    /// Controller for Timeline Ruler MVVM.
    /// Manages ruler display, playhead positioning, markers, and time scrubbing.
    /// </summary>
    public class TimelineRulerController {
        readonly TimelineRulerView _view;
        readonly TimelineRulerModel _model;

        private bool _isPlayheadDragging;
        private VisualElement _markersContainer;
        private VisualElement _playheadElement;

        TimelineRulerController(TimelineRulerView view, TimelineRulerModel model) {
            _view = view;
            _model = model;
            _view.StartCoroutine(Initialize());
        }

        IEnumerator Initialize() {
            var vm = new ViewModel(_model);
            yield return _view.InitializeView(vm);
            Bind(vm);
        }

        public void Bind(ViewModel vm) {
            // Get ruler elements
            _markersContainer = _view.GetElement("markersContainer");
            _playheadElement = _view.GetElement("playhead");

            // Setup ruler interaction for scrubbing
            var rulerElement = _view.GetElement("rulerArea");
            if (rulerElement != null) {
                rulerElement.RegisterCallback<MouseDownEvent>(OnRulerMouseDown);
                rulerElement.RegisterCallback<MouseMoveEvent>(OnRulerMouseMove);
                rulerElement.RegisterCallback<MouseUpEvent>(OnRulerMouseUp);
            }

            // Setup playhead element
            if (_playheadElement != null) {
                _playheadElement.RegisterCallback<MouseDownEvent>(OnPlayheadMouseDown);
            }

            // Generate and display markers
            GenerateMarkerDisplay();

            // Update playhead position on time change
            _model.OnTimeChanged += UpdatePlayheadPosition;
            _model.OnMarkersChanged += GenerateMarkerDisplay;
            _model.OnZoomChanged += GenerateMarkerDisplay;

            UpdatePlayheadPosition();
        }

        private void GenerateMarkerDisplay() {
            if (_markersContainer == null) return;

            _markersContainer.Clear();

            foreach (var marker in _model.Markers) {
                var markerElement = new VisualElement();
                markerElement.name = "timeline-marker";
                markerElement.style.position = Position.Absolute;
                markerElement.style.left = marker.position;
                markerElement.style.height = marker.height;
                markerElement.style.width = 1;
                markerElement.AddToClassList(marker.isMajor ? "marker-major" : "marker-minor");

                // Add marker label for major markers
                if (marker.isMajor) {
                    var label = new Label(marker.label);
                    label.style.fontSize = 10;
                    label.style.position = Position.Absolute;
                    label.style.left = marker.position + 5;
                    markerElement.Add(label);
                }

                _markersContainer.Add(markerElement);
            }
        }

        private void UpdatePlayheadPosition() {
            if (_playheadElement == null) return;

            float position = _model.TimeToPosition(_model.CurrentTime);
            _playheadElement.style.left = position;
        }

        private void OnRulerMouseDown(MouseDownEvent evt) {
            float position = evt.localMousePosition.x;
            float time = _model.PositionToTime(position);
            time = _model.SnapTime(time);
            
            _isPlayheadDragging = true;
            _view.StartCoroutine(DragPlayhead(time));
            evt.StopPropagation();
        }

        private void OnPlayheadMouseDown(MouseDownEvent evt) {
            _isPlayheadDragging = true;
            evt.StopPropagation();
        }

        private void OnRulerMouseMove(MouseMoveEvent evt) {
            if (_isPlayheadDragging) {
                float position = evt.localMousePosition.x;
                float time = _model.PositionToTime(position);
                time = _model.SnapTime(time);
                _model.SetTime(time);
            }
        }

        private void OnRulerMouseUp(MouseUpEvent evt) {
            _isPlayheadDragging = false;
        }

        private IEnumerator DragPlayhead(float targetTime) {
            _model.SetTime(targetTime);
            yield return null;
        }

        public class ViewModel {
            public readonly BindableProperty<float> CurrentTime;
            public readonly BindableProperty<float> Zoom;
            public readonly BindableProperty<int> MarkerCount;

            readonly TimelineRulerModel _model;
            
            public ViewModel(TimelineRulerModel model) {
                _model = model;
                CurrentTime = BindableProperty<float>.Bind(() => _model.CurrentTime);
                Zoom = BindableProperty<float>.Bind(() => _model.Zoom);
                MarkerCount = BindableProperty<int>.Bind(() => _model.Markers.Count);
            }

            public void MovePlayhead(float time) => _model.SetTime(time);
            public void SetZoom(float zoom) => _model.SetZoom(zoom);
            public float SnapTime(float time) => _model.SnapTime(time);
        }

        public class Builder {
            TimelineRulerView _view;
            TimelineRulerModel _model;
            float _length;
            int _frameRate = 30;

            public Builder(TimelineRulerView view) { _view = view; }
            public Builder WithModel(TimelineRulerModel model) { _model = model; return this; }
            public Builder WithLength(float length) { _length = length; return this; }
            public Builder WithFrameRate(int frameRate) { _frameRate = frameRate; return this; }
            
            public TimelineRulerController Build() {
                if (_model == null) _model = new TimelineRulerModel();
                _model.Initialize(_length, _frameRate);
                return new TimelineRulerController(_view, _model);
            }
        }
    }
}