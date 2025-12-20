using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.Core;
using MiniTimeline.UI.Commands;
using Core.Behaviors.Command;

namespace MiniTimeline.UI.MVVM.Timeline {
    /// <summary>
    /// Controller for Timeline Editor MVVM.
    /// Orchestrates View-ViewModel interactions, wires commands, and manages state.
    /// </summary>
    public class TimelineEditorController {
        readonly TimelineEditorView _view;
        readonly TimelineEditorModel _model;
        ViewModel _viewModel;

        TimelineEditorController(TimelineEditorView view, TimelineEditorModel model) {
            Debug.Assert(view != null, "View is null");
            Debug.Assert(model != null, "Model is null");
            _view = view;
            _model = model;

            _view.StartCoroutine(Initialize());
        }

        IEnumerator Initialize() {
            _viewModel = new ViewModel(_model);
            yield return _view.InitializeView(_viewModel);
            Bind(_viewModel);
        }

        public void Bind(ViewModel vm) {
            // Playback controls
            WireButton("playButton", vm.Play);
            WireButton("pauseButton", vm.Pause);
            WireButton("stopButton", vm.Stop);
            
            // Undo/Redo
            WireButton("undoButton", vm.Undo);
            WireButton("redoButton", vm.Redo);
            
            // Track/Project management
            WireButton("addTrackButton", vm.ShowAddTrackForm);
            WireButton("bindingManagerButton", vm.ShowBindingManager);
            WireButton("saveButton", vm.ShowSaveProjectForm);
            WireButton("loadButton", vm.ShowLoadProjectForm);

            // Sliders with value conversion
            var timeSlider = _view.GetSlider("timeSlider");
            if (timeSlider != null) {
                timeSlider.RegisterValueChangedCallback(evt => {
                    // Convert 0-1 normalized slider value to time
                    if (_model.Director != null) {
                        float time = evt.newValue * _model.Director.Length;
                        vm.SetTime(time);
                    }
                });
            }

            var zoomSlider = _view.GetSlider("zoomSlider");
            if (zoomSlider != null) {
                zoomSlider.value = _model.Zoom;
                zoomSlider.RegisterValueChangedCallback(evt => vm.SetZoom(evt.newValue));
            }

            // Update UI labels
            var statusLabel = _view.GetLabel("statusLabel");
            if (statusLabel != null) statusLabel.text = vm.StatusText.Value;

            var timeLabel = _view.GetLabel("timeLabel");
            if (timeLabel != null) {
                timeLabel.text = FormatTime(_model.Time);
                _model.OnCommandExecuted += () => {
                    timeLabel.text = FormatTime(_model.Time);
                };
            }

            var zoomLabel = _view.GetLabel("zoomLabel");
            if (zoomLabel != null) {
                zoomLabel.text = $"{_model.Zoom * 100:F0}%";
                _model.OnCommandExecuted += () => {
                    zoomLabel.text = $"{_model.Zoom * 100:F0}%";
                };
            }

            // Subscribe to model state changes
            _model.OnCommandExecuted += vm.RefreshUndoRedoButtons;
            _model.OnCommandStacksChanged += vm.RefreshUndoRedoButtons;
        }

        private void WireButton(string buttonName, Action callback) {
            var button = _view.GetButton(buttonName);
            if (button != null) button.clicked += callback;
        }

        private string FormatTime(float time) {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int frames = Mathf.FloorToInt((time % 1f) * 30f);
            return $"{minutes:00}:{seconds:00}:{frames:00}";
        }

        public class ViewModel {
            public readonly BindableProperty<bool> IsPlaying;
            public readonly BindableProperty<float> Time;
            public readonly BindableProperty<float> Zoom;
            public readonly BindableProperty<string> StatusText;
            public readonly BindableProperty<bool> CanUndo;
            public readonly BindableProperty<bool> CanRedo;

            readonly TimelineEditorModel _model;

            public ViewModel(TimelineEditorModel model) {
                _model = model;
                IsPlaying = BindableProperty<bool>.Bind(() => _model.IsPlaying);
                Time = BindableProperty<float>.Bind(() => _model.Time);
                Zoom = BindableProperty<float>.Bind(() => _model.Zoom);
                StatusText = BindableProperty<string>.Bind(() => _model.StatusText);
                CanUndo = BindableProperty<bool>.Bind(() => _model.CanUndo);
                CanRedo = BindableProperty<bool>.Bind(() => _model.CanRedo);
            }

            public void Play() => _model.Play();
            public void Pause() => _model.Pause();
            public void Stop() => _model.Stop();
            public void SetTime(float time) => _model.SetTime(time);
            public void SetZoom(float zoom) => _model.SetZoom(zoom);
            
            public void Undo() => _model.Undo();
            public void Redo() => _model.Redo();

            public void RefreshUndoRedoButtons() {
                // UI will update automatically via BindableProperty binding
            }

            // Track operations
            public void ShowAddTrackForm() {
                Debug.Log("Show add track form - TODO: Implement in controller");
                // TODO: Trigger form for creating new track
                // AddTrackCommand will be created and executed
            }

            public void ShowBindingManager() {
                Debug.Log("Show binding manager - TODO: Implement in controller");
                // TODO: Show binding manager UI
                // Uses director?.BindingContext to manage scene bindings
            }

            public void ShowSaveProjectForm() {
                Debug.Log("Show save project form - TODO: Implement in controller");
                // TODO: Show save project UI
                // Calls ProjectSerializer.SaveToFile(director.Project, director, filePath);
            }

            public void ShowLoadProjectForm() {
                Debug.Log("Show load project form - TODO: Implement in controller");
                // TODO: Show load project UI
                // Calls ProjectSerializer.LoadFromFile(filePath);
            }
        }

        public class Builder {
            TimelineEditorView _view;
            TimelineEditorModel _model;
            MiniTimelineDirector _director;

            public Builder(TimelineEditorView view) { _view = view; }
            public Builder WithModel(TimelineEditorModel model) { _model = model; return this; }
            public Builder WithDirector(MiniTimelineDirector director) { _director = director; return this; }
            
            public TimelineEditorController Build() {
                if (_model == null) _model = new TimelineEditorModel();
                if (_director != null) _model.Initialize(_director);
                return new TimelineEditorController(_view, _model);
            }
        }
    }
}