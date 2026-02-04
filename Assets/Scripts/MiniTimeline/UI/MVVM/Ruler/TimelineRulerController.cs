using System;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.UI.MVVM.Ruler {
    /// <summary>
    /// Controller for Timeline Ruler MVVM.
    /// Manages ruler display, playhead positioning, markers, and time scrubbing.
    /// </summary>
    public class TimelineRulerController {
        readonly TimelineRulerView _view;
        readonly TimelineRulerModel _model;
        readonly ViewModel _vm;


        TimelineRulerController(TimelineRulerView view, TimelineRulerModel model) {
            _view = view;
            _model = model;
            _vm = new ViewModel(_model);
            _view.Bind(_model, _vm);
        }

        public class ViewModel {
            public readonly BindableProperty<float> CurrentTime;
            public readonly BindableProperty<float> Zoom;
            public readonly BindableProperty<int> MarkerCount;
            public readonly BindableProperty<float> Length;
            public readonly BindableProperty<float> PixelsPerSecond;

            readonly TimelineRulerModel _model;
            
            public ViewModel(TimelineRulerModel model) {
                _model = model;
                CurrentTime = BindableProperty<float>.Bind(() => _model.CurrentTime);
                Zoom = BindableProperty<float>.Bind(() => _model.Zoom);
                MarkerCount = BindableProperty<int>.Bind(() => _model.Markers.Count);
                Length = BindableProperty<float>.Bind(() => _model.Length);
                PixelsPerSecond = BindableProperty<float>.Bind(() => _model.PixelsPerSecond);
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
            MiniTimelineDirector _director;

            public Builder(TimelineRulerView view) { _view = view; }
            public Builder WithModel(TimelineRulerModel model) { _model = model; return this; }
            public Builder WithLength(float length) { _length = length; return this; }
            public Builder WithFrameRate(int frameRate) { _frameRate = frameRate; return this; }
            public Builder WithDirector(MiniTimelineDirector director) { _director = director; return this; }
            
            public TimelineRulerController Build() {
                if (_model == null) _model = new TimelineRulerModel();
                if (_director != null) {
                    _model.BindDirector(_director);
                    if (_length <= 0f) _length = _director.Length;
                    if (_frameRate <= 0) _frameRate = Mathf.RoundToInt(_director.Project?.frameRate ?? 30f);
                }
                _model.Initialize(_length, _frameRate);
                return new TimelineRulerController(_view, _model);
            }
        }
    }
}