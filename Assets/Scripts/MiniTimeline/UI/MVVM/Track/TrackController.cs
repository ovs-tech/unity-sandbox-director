using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.Core;

namespace MiniTimeline.UI.MVVM.Track {
    /// <summary>
    /// Controller for Track MVVM.
    /// Manages track state, UI interaction, and clip management.
    /// </summary>
    public class TrackController {
        readonly TrackView _view;
        readonly TrackModel _model;

        TrackController(TrackView view, TrackModel model) {
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
            // State toggles
            var enabled = _view.GetToggle("trackEnabledToggle");
            var mute = _view.GetToggle("trackMuteToggle");
            var solo = _view.GetToggle("trackSoloToggle");
            
            // Display elements
            var title = _view.GetLabel("trackTitleLabel");
            var bindInfo = _view.GetLabel("trackBindInfoLabel");
            
            // Action buttons
            var addClipButton = _view.GetButton("addClipButton");
            var settingsButton = _view.GetButton("trackSettingsButton");
            var deleteButton = _view.GetButton("deleteTrackButton");

            // Wire state toggles
            if (enabled != null) {
                enabled.value = vm.Enabled.Value;
                enabled.RegisterValueChangedCallback(_ => vm.ToggleEnabled());
            }
            if (mute != null) {
                mute.value = vm.Muted.Value;
                mute.RegisterValueChangedCallback(_ => vm.Mute());
            }
            if (solo != null) {
                solo.value = vm.Solo.Value;
                solo.RegisterValueChangedCallback(_ => vm.ToggleSolo());
            }

            // Update display labels
            if (title != null) title.text = vm.Title.Value;
            if (bindInfo != null) bindInfo.text = $"Bind: {vm.BindKey.Value}";

            // Wire action buttons
            if (addClipButton != null) addClipButton.clicked += vm.ShowAddClipForm;
            if (settingsButton != null) settingsButton.clicked += vm.ShowSettings;
            if (deleteButton != null) deleteButton.clicked += vm.ShowDeleteConfirmation;

            // Subscribe to model changes
            _model.OnTitleChanged += () => {
                if (title != null) title.text = vm.Title.Value;
            };
            _model.OnStateChanged += () => {
                if (bindInfo != null) bindInfo.text = $"Bind: {vm.BindKey.Value}";
            };
            _model.OnClipsChanged += vm.RefreshClips;
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

            public Builder(TrackView view) { _view = view; }
            public Builder WithModel(TrackModel model) { _model = model; return this; }
            public Builder WithTrack(IMiniTrack track) { _track = track; return this; }
            
            public TrackController Build() {
                if (_model == null) _model = new TrackModel();
                if (_track != null) _model.Initialize(_track);
                return new TrackController(_view, _model);
            }
        }
    }
}