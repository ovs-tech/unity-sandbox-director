using System;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.Core;

namespace MiniTimeline.UI.MVVM.Clip {
    /// <summary>
    /// Controller for Clip MVVM.
    /// Manages clip state, drag/resize interactions, and selection.
    /// </summary>
    public class ClipController {
        readonly ClipView _view;
        readonly ClipModel _model;
        ViewModel _viewModel;

        ClipController(ClipView view, ClipModel model) {
            _view = view;
            _model = model;
            Initialize();
        }

        void Initialize() {
            _viewModel = new ViewModel(_model);
            Bind(_viewModel);
        }

        public void Bind(ViewModel vm) {
            // Delegate all UI binding and event registration to the view
            _view.Bind(vm, _model);
        }

        public bool IsDragging => _view.IsDragging;
        public bool IsResizingLeft => _view.IsResizingLeft;
        public bool IsResizingRight => _view.IsResizingRight;

        /// <summary>
        /// Updates zoom level and pixels per second.
        /// </summary>
        public void UpdateZoom(float zoom, float pixelsPerSecond) {
            _model.SetZoom(zoom);
            _model.SetPixelsPerSecond(pixelsPerSecond);
        }

        public class ViewModel {
            public readonly BindableProperty<string> Title;
            public readonly BindableProperty<float> Duration;
            public readonly BindableProperty<float> StartTime;
            public readonly BindableProperty<bool> Locked;
            public readonly BindableProperty<bool> Muted;
            public readonly BindableProperty<bool> Selected;
            public readonly BindableProperty<float> PixelsPerSecond;

            readonly ClipModel _model;
            
            public ViewModel(ClipModel model) {
                _model = model;
                Title = BindableProperty<string>.Bind(() => _model.Title);
                Duration = BindableProperty<float>.Bind(() => _model.Duration);
                StartTime = BindableProperty<float>.Bind(() => _model.StartTime);
                Locked = BindableProperty<bool>.Bind(() => _model.Locked);
                Muted = BindableProperty<bool>.Bind(() => _model.Muted);
                Selected = BindableProperty<bool>.Bind(() => _model.Selected);
                PixelsPerSecond = BindableProperty<float>.Bind(() => _model.PixelsPerSecond);
            }

            public void ResizeLeft(float delta) => _model.ResizeLeft(delta);
            public void ResizeRight(float delta) => _model.ResizeRight(delta);
            public void Select(bool selected) => _model.Select(selected);
            public void Drag(float delta) => _model.Drag(delta);

            public void ShowContextMenu() {
                Debug.Log($"Show context menu for clip: {_model.Title}");
                // TODO: Show context menu with actions
            }

            public void ShowProperties() {
                Debug.Log($"Show properties for clip: {_model.Title}");
                // TODO: Show clip property editor
            }
        }

        public class Builder {
            ClipView _view;
            ClipModel _model;
            IMiniClip _clip;
            IMiniTrack _parentTrack;

            public Builder(ClipView view) { _view = view; }
            public Builder WithModel(ClipModel model) { _model = model; return this; }
            public Builder WithClip(IMiniClip clip) { _clip = clip; return this; }
            public Builder WithParentTrack(IMiniTrack track) { _parentTrack = track; return this; }
            
            public ClipController Build() {
                if (_model == null) _model = new ClipModel();
                if (_clip != null) _model.Initialize(_clip, _parentTrack);
                return new ClipController(_view, _model);
            }
        }
    }
}