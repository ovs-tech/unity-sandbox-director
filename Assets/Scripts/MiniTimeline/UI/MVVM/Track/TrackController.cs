using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using MiniTimeline.Core;
using MiniTimeline.UI.MVVM.Clip;
using MiniTimeline.UI.FormDefinitions;
using MiniTimeline.UI.Commands;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;
using Core.Behaviors.Command;

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
            var vm = new ViewModel(_model, _view);
            
            // Provide a UIElements parent container for forms
            vm.FormHost = _view?.Root;
            
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
            readonly TrackView _view;
            
            /// <summary>
            /// Visual element to host form dialogs
            /// </summary>
            public VisualElement FormHost { get; set; }
            
            public ViewModel(TrackModel model, TrackView view) {
                _model = model;
                _view = view;
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
                if (_model?.Track == null) {
                    Debug.LogError("Cannot show settings: Track is null");
                    return;
                }

                // Show track context actions (only), using context menu definitions
                var actionFields = TrackContextMenuDefinitions.GetTrackActionFields();
                FormSubmitPanelUIToolkit.Instance.Show(
                    TrackContextMenuDefinitions.GetTrackMenuTitle(),
                    actionFields,
                    OnTrackActionsFormSubmitted,
                    OnTrackSettingsFormCancelled,
                    _view.PanelRoot
                );

                Debug.Log($"Show track actions for track: {_model.Title}");
            }

            /// <summary>
            /// Handle track actions form submission (placeholder wiring).
            /// </summary>
            private void OnTrackActionsFormSubmitted(Dictionary<string, object> formData) {
                Debug.Log($"Track actions submitted for '{_model?.Title}': fields={formData?.Count ?? 0}");

                if (formData == null || formData.Count == 0) return;

                string action = null;
                if (formData.TryGetValue("action", out var actionObj) && actionObj != null) {
                    action = actionObj.ToString();
                } else {
                    // Fallback: infer from known field keys
                    if (formData.ContainsKey("addClip")) action = "addClip";
                    else if (formData.ContainsKey("mute")) action = "mute";
                    else if (formData.ContainsKey("solo")) action = "solo";
                    else if (formData.ContainsKey("delete")) action = "delete";
                    else if (formData.ContainsKey("settings")) action = "settings";
                }

                switch (action) {
                    case "mute":
                        _model.Mute();
                        FormSubmitPanelUIToolkit.Instance.CloseForm();
                        break;
                    case "solo":
                        _model.ToggleSolo();
                        FormSubmitPanelUIToolkit.Instance.CloseForm();
                        break;
                    case "delete":
                        _model?.DeleteTrack();
                        FormSubmitPanelUIToolkit.Instance.CloseForm();
                        break;
                    case "addClip":
                        ShowAddClipForm();
                        break;
                    case "settings":
                        ShowTrackSettingsForm();
                        break;
                    default:
                        Debug.LogWarning($"Unknown track action: '{action}'");
                        break;
                }
            }

            private void ShowTrackSettingsForm() {
                if (_model?.Track == null) {
                    Debug.LogError("Cannot show settings: Track is null");
                    return;
                }

                string trackType = _model.Type;
                var fieldDefinitions = TrackFormDefinitions.GetTrackSettingsFields(trackType);
                SetDefaultValuesForTrackSettings(fieldDefinitions);

                FormSubmitPanelUIToolkit.Instance.Show(
                    $"Track Settings - {TrackFormDefinitions.GetTrackTypeDisplayName(trackType)}",
                    fieldDefinitions,
                    OnTrackSettingsFormSubmitted,
                    OnTrackSettingsFormCancelled,
                    _view.PanelRoot
                );
            }
            
            /// <summary>
            /// Set default values for track settings form
            /// </summary>
            private void SetDefaultValuesForTrackSettings(List<FormFieldDefinition> fieldDefinitions) {
                foreach (var field in fieldDefinitions) {
                    switch (field.name) {
                        case "trackName":
                            field.defaultValue = _model.Title;
                            break;
                        case "bindKey":
                            field.defaultValue = _model.BindKey ?? "";
                            break;
                        case "enabled":
                            field.defaultValue = _model.Enabled;
                            break;
                    }
                }
            }
            
            /// <summary>
            /// Handle track settings form submission
            /// </summary>
            private void OnTrackSettingsFormSubmitted(Dictionary<string, object> formData) {
                try {
                    if (_model == null || _model.Track == null) {
                        Debug.LogError("Cannot update track: Track is null");
                        return;
                    }

                    _model.ApplySettings(formData);

                    Debug.Log($"Track settings updated: {_model.Title}");
                } catch (Exception ex) {
                    Debug.LogError($"Failed to update track settings: {ex.Message}");
                }
            }
            
            /// <summary>
            /// Update a track property using reflection (for read-only interface properties)
            /// </summary>
            private void UpdateTrackProperty(string propertyName, object value) {
                if (_model?.Track == null) return;

                var trackType = _model.Track.GetType();
                var property = trackType.GetProperty(propertyName);
                
                if (property != null && property.CanWrite) {
                    property.SetValue(_model.Track, value);
                    Debug.Log($"Updated track property {propertyName} to {value}");
                } else {
                    Debug.LogWarning($"Property {propertyName} is read-only or not found on {trackType.Name}");
                }
            }
            
            /// <summary>
            /// Handle track settings form cancellation
            /// </summary>
            private void OnTrackSettingsFormCancelled() {
                Debug.Log("Track settings cancelled");
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