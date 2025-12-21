using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.Core;
using MiniTimeline.UI.MVVM.Clip;

namespace MiniTimeline.UI.MVVM.Track {
    /// <summary>
    /// Controller for Track MVVM.
    /// Manages track state, UI interaction, and clip management.
    /// </summary>
    public class TrackController {
        readonly TrackView _view;
        readonly TrackModel _model;
        readonly Dictionary<string, ClipController> _clipControllers = new Dictionary<string, ClipController>();
        readonly VisualTreeAsset _clipUxml;
        readonly StyleSheet _clipUss;

        TrackController(TrackView view, TrackModel model, VisualTreeAsset clipUxml = null, StyleSheet clipUss = null) {
            _view = view;
            _model = model;
            _clipUxml = clipUxml;
            _clipUss = clipUss;
            Initialize();
        }

        void Initialize() {
            var vm = new ViewModel(_model);
            _view.Render(vm, _model);
            _view.InitializeClipControllers(_model, _clipControllers, _clipUxml, _clipUss);
            
            // Subscribe to model changes for clip sync
            _model.OnClipsChanged += () => {
                _view.SyncClipControllers(_model, _clipControllers, _clipUxml, _clipUss);
            };

            // Subscribe to zoom/width changes
            _model.OnZoomChanged += () => {
                _view.UpdateZoom(_model, _clipControllers);
            };
            _model.OnTimelineWidthChanged += () => {
                _view.UpdateZoom(_model, _clipControllers);
            };
        }

        /// <summary>
        /// Gets a ClipController by clip ID.
        /// </summary>
        public ClipController GetClipController(string clipId) {
            return _clipControllers.TryGetValue(clipId, out var controller) ? controller : null;
        }

        /// <summary>
        /// Gets all registered ClipControllers.
        /// </summary>
        public IReadOnlyDictionary<string, ClipController> GetAllClipControllers() {
            return _clipControllers;
        }

        /// <summary>
        /// Clears all registered ClipControllers.
        /// </summary>
        public void ClearClipControllers() {
            _clipControllers.Clear();
            Debug.Log("Cleared all ClipControllers");
        }

        /// <summary>
        /// Updates zoom level and timeline width.
        /// </summary>
        public void UpdateZoom(float zoom, float pixelsPerSecond) {
            _model.SetZoom(zoom);
            _model.SetPixelsPerSecond(pixelsPerSecond);

            // update clips zoom
            foreach (var clipController in _clipControllers.Values) {
                clipController.UpdateZoom(zoom, pixelsPerSecond);
            }
        }

        public class ViewModel {
            public readonly BindableProperty<string> Title;
            public readonly BindableProperty<bool> Enabled;
            public readonly BindableProperty<bool> Muted;
            public readonly BindableProperty<bool> Solo;
            public readonly BindableProperty<string> BindKey;
            public readonly BindableProperty<int> ClipCount;
            public readonly BindableProperty<float> TimelineWidth;
            public readonly BindableProperty<float> Zoom;

            readonly TrackModel _model;
            
            public ViewModel(TrackModel model) {
                _model = model;
                Title = BindableProperty<string>.Bind(() => _model.Title);
                Enabled = BindableProperty<bool>.Bind(() => _model.Enabled);
                Muted = BindableProperty<bool>.Bind(() => _model.Muted);
                Solo = BindableProperty<bool>.Bind(() => _model.Solo);
                BindKey = BindableProperty<string>.Bind(() => _model.BindKey);
                ClipCount = BindableProperty<int>.Bind(() => _model.Clips.Count);
                TimelineWidth = BindableProperty<float>.Bind(() => _model.TimelineWidth);
                Zoom = BindableProperty<float>.Bind(() => _model.Zoom);
            }

            public void ToggleEnabled() => _model.ToggleEnabled();
            public void Mute() => _model.Mute();
            public void ToggleSolo() => _model.ToggleSolo();
            
            public void RefreshClips() {
                // Refresh clip count from model - UI will update automatically via BindableProperty binding
            }
            
            public void ShowAddClipForm() {
                Debug.Log($"Show add clip form for track: {_model.Title}");
                // TODO: Show form to add new clip to this track
            }

            public void ShowSettings() {
                Debug.Log($"Show settings for track: {_model.Title}");
                // TODO: Show track settings form
            }

            public void ShowDeleteConfirmation() {
                Debug.Log($"Show delete confirmation for track: {_model.Title}");
                // TODO: Show delete confirmation dialog
            }
        }

        public class Builder {
            TrackView _view;
            TrackModel _model;
            IMiniTrack _track;
            VisualTreeAsset _clipUxml;
            StyleSheet _clipUss;
            MiniTimelineDirector _director;

            public Builder(TrackView view) { _view = view; }
            public Builder WithModel(TrackModel model) { _model = model; return this; }
            public Builder WithTrack(IMiniTrack track) { _track = track; return this; }
            public Builder WithClipUI(VisualTreeAsset uxml, StyleSheet uss) { _clipUxml = uxml; _clipUss = uss; return this; }
            public Builder WithDirector(MiniTimelineDirector director) { _director = director; return this; }
            
            public TrackController Build() {
                if (_model == null) _model = new TrackModel();
                if (_track != null) _model.Initialize(_track, _director);
                return new TrackController(_view, _model, _clipUxml, _clipUss);
            }
        }
    }
}