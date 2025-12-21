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
            Bind(vm);
            InitializeClipControllers();
        }

        public void Bind(ViewModel vm) {
            // State toggles
            var enabled = _view.GetToggle("track-enabled");
            var mute = _view.GetButton("track-mute");
            var solo = _view.GetButton("track-solo");
            
            // Display elements
            var title = _view.GetLabel("track-title");
            var bindInfo = _view.GetLabel("track-bind-key");
            
            // Action buttons
            var menuButton = _view.GetButton("track-menu-button");

            // Wire state toggles
            if (enabled != null) {
                enabled.value = vm.Enabled.Value;
                enabled.RegisterValueChangedCallback(_ => vm.ToggleEnabled());
            }
            if (mute != null) {
                mute.clicked += vm.Mute;
            }
            if (solo != null) {
                solo.clicked += vm.ToggleSolo;
            }

            // Update display labels
            if (title != null) title.text = vm.Title.Value;
            if (bindInfo != null) bindInfo.text = vm.BindKey.Value;

            // Wire action buttons
            if (menuButton != null) menuButton.clicked += vm.ShowSettings;

            // Subscribe to model changes
            _model.OnTitleChanged += () => {
                if (title != null) title.text = vm.Title.Value;
            };
            _model.OnStateChanged += () => {
                if (bindInfo != null) bindInfo.text = vm.BindKey.Value;
            };
            _model.OnClipsChanged += () => {
                vm.RefreshClips();
                SyncClipControllers();
            };
        }

        /// <summary>
        /// Initialize ClipControllers for all existing clips in the track.
        /// </summary>
        private void InitializeClipControllers() {
            if (_model.Track == null) return;

            // Get clips container from track view (place clips under first lane)
            var clipsContainer = _view.GetElement("lane-1");
            if (clipsContainer == null) {
                Debug.LogWarning("Cannot find 'lane-1' element in TrackView");
                return;
            }

            // Create controllers for all current clips
            foreach (var clip in _model.Clips) {
                CreateAndRegisterClipController(clip.Id, clip, clipsContainer);
            }

            Debug.Log($"Initialized {_clipControllers.Count} ClipControllers for track");
        }

        /// <summary>
        /// Syncs clip controllers based on the current clip list in the model.
        /// </summary>
        private void SyncClipControllers() {
            var clipsContainer = _view.GetElement("lane-1");
            if (clipsContainer == null) {
                Debug.LogWarning("Cannot find 'lane-1' element in TrackView");
                return;
            }

            // Get the set of clip IDs currently in the model
            var clipIdsInModel = new HashSet<string>();
            foreach (var clip in _model.Clips) {
                if (clip != null) {
                    clipIdsInModel.Add(clip.Id);

                    // Create controller if it doesn't exist
                    if (!_clipControllers.ContainsKey(clip.Id)) {
                        CreateAndRegisterClipController(clip.Id, clip, clipsContainer);
                    }
                }
            }

            // Remove controllers for clips no longer in the model
            var clipsToRemove = new List<string>();
            foreach (var clipId in _clipControllers.Keys) {
                if (!clipIdsInModel.Contains(clipId)) {
                    clipsToRemove.Add(clipId);
                }
            }

            foreach (var clipId in clipsToRemove) {
                UnregisterClipController(clipId);
            }
        }

        /// <summary>
        /// Creates and registers a ClipController for a given clip.
        /// </summary>
        private ClipController CreateAndRegisterClipController(string clipId, IMiniClip clip, VisualElement clipsContainer) {
            if (string.IsNullOrEmpty(clipId) || clip == null) {
                Debug.LogWarning("Cannot create ClipController: Invalid clipId or clip");
                return null;
            }

            if (_clipControllers.ContainsKey(clipId)) {
                Debug.LogWarning($"ClipController already exists for clip {clipId}");
                return _clipControllers[clipId];
            }

            try {
                // Create a VisualElement for this clip and add it to the container
                var clipElement = new VisualElement { name = $"clip_{clipId}" };
                clipsContainer.Add(clipElement);

                // Create ClipView with the VisualElement and optional UI assets
                var clipView = new ClipView(clipElement, _clipUxml, _clipUss);

                // Create ClipModel and initialize with clip
                var clipModel = new ClipModel();
                clipModel.Initialize(clip, _model.Track);

                // Create ClipController with builder pattern
                var clipController = new ClipController.Builder(clipView)
                    .WithModel(clipModel)
                    .WithClip(clip)
                    .Build();

                // Register the controller
                _clipControllers[clipId] = clipController;

                Debug.Log($"Successfully created and registered ClipController for clip: {clipId}");
                return clipController;
            } catch (Exception e) {
                Debug.LogError($"Failed to create ClipController for clip '{clipId}': {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Unregisters a ClipController for a given clip ID.
        /// </summary>
        private void UnregisterClipController(string clipId) {
            if (_clipControllers.ContainsKey(clipId)) {
                _clipControllers.Remove(clipId);
                Debug.Log($"Unregistered ClipController for clip: {clipId}");
            }
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

        public class ViewModel {
            public readonly BindableProperty<string> Title;
            public readonly BindableProperty<bool> Enabled;
            public readonly BindableProperty<bool> Muted;
            public readonly BindableProperty<bool> Solo;
            public readonly BindableProperty<string> BindKey;
            public readonly BindableProperty<int> ClipCount;

            readonly TrackModel _model;
            
            public ViewModel(TrackModel model) {
                _model = model;
                Title = BindableProperty<string>.Bind(() => _model.Title);
                Enabled = BindableProperty<bool>.Bind(() => _model.Enabled);
                Muted = BindableProperty<bool>.Bind(() => _model.Muted);
                Solo = BindableProperty<bool>.Bind(() => _model.Solo);
                BindKey = BindableProperty<string>.Bind(() => _model.BindKey);
                ClipCount = BindableProperty<int>.Bind(() => _model.Clips.Count);
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

            public Builder(TrackView view) { _view = view; }
            public Builder WithModel(TrackModel model) { _model = model; return this; }
            public Builder WithTrack(IMiniTrack track) { _track = track; return this; }
            public Builder WithClipUI(VisualTreeAsset uxml, StyleSheet uss) { _clipUxml = uxml; _clipUss = uss; return this; }
            
            public TrackController Build() {
                if (_model == null) _model = new TrackModel();
                if (_track != null) _model.Initialize(_track);
                return new TrackController(_view, _model, _clipUxml, _clipUss);
            }
        }
    }
}