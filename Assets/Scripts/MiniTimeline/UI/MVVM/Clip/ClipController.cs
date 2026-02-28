using System;
using UnityEngine;
using UnityEngine.UIElements;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.UI.FormDefinitions;
using Systems.Core.UI.FormSubmit;
using System.Collections.Generic;

namespace Systems.MiniTimeline.UI.MVVM.Clip
{
    /// <summary>
    /// Controller for Clip MVVM.
    /// Manages clip state, drag/resize interactions, and selection.
    /// </summary>
    public class ClipController
    {
        readonly ClipView _view;
        readonly ClipModel _model;
        ViewModel _viewModel;

        ClipController(ClipView view, ClipModel model)
        {
            _view = view;
            _model = model;
            Initialize();
        }

        void Initialize()
        {
            _viewModel = new ViewModel(_model, _view);
            Bind(_viewModel);
        }

        public void Bind(ViewModel vm)
        {
            // Delegate all UI binding and event registration to the view
            _view.Bind(vm, _model);
        }

        /// <summary>
        /// Updates zoom level and pixels per second.
        /// </summary>
        public void UpdateZoom(float zoom, float pixelsPerSecond)
        {
            _model.SetZoom(zoom);
            _model.SetPixelsPerSecond(pixelsPerSecond);
        }

        public class ViewModel
        {
            public readonly BindableProperty<string> Title;
            public readonly BindableProperty<float> Duration;
            public readonly BindableProperty<string> DurationText;
            public readonly BindableProperty<float> StartTime;
            public readonly BindableProperty<bool> Locked;
            public readonly BindableProperty<bool> Muted;
            public readonly BindableProperty<bool> Selected;
            public readonly BindableProperty<float> PixelsPerSecond;

            readonly ClipModel _model;
            readonly ClipView _view;

            public ViewModel(ClipModel model, ClipView view)
            {
                _model = model;
                _view = view;
                Title = BindableProperty<string>.Bind(() => _model.Title);
                Duration = BindableProperty<float>.Bind(() => _model.Duration);
                DurationText = BindableProperty<string>.Bind(() => $"{_model.Duration:F2}s");
                StartTime = BindableProperty<float>.Bind(() => _model.StartTime);
                Locked = BindableProperty<bool>.Bind(() => _model.Locked);
                Muted = BindableProperty<bool>.Bind(() => _model.Muted);
                Selected = BindableProperty<bool>.Bind(() => _model.Selected);
                PixelsPerSecond = BindableProperty<float>.Bind(() => _model.PixelsPerSecond);
            }

            public void ShowClipSettingForm()
            {
                var actionFields = ClipContextMenuDefinitions.GetClipActionFields();
                FormSubmitPanelUIToolkit.Instance.Show(
                    ClipContextMenuDefinitions.GetClipMenuTitle(),
                    actionFields,
                    OnClipSettingsFormSubmitted,
                    OnClipSettingsFormCancelled,
                    _view.PanelRoot
                );
            }

            private void OnClipSettingsFormCancelled()
            {
                throw new NotImplementedException();
            }

            private void OnClipSettingsFormSubmitted(Dictionary<string, object> dictionary)
            {
                
            }
        }

        public class Builder
        {
            ClipView _view;
            ClipModel _model;
            IMiniClip _clip;
            IMiniTrack _parentTrack;
            MiniTimelineDirector _director;

            public Builder(ClipView view) { _view = view; }
            public Builder WithModel(ClipModel model) { _model = model; return this; }
            public Builder WithClip(IMiniClip clip) { _clip = clip; return this; }
            public Builder WithParentTrack(IMiniTrack track) { _parentTrack = track; return this; }
            public Builder WithDirector(MiniTimelineDirector director) { _director = director; return this; }

            public ClipController Build()
            {
                if (_model == null) _model = new ClipModel();
                if (_clip != null || _parentTrack != null || _director != null)
                {
                    _model.Initialize(_clip, _parentTrack, _director);
                }
                return new ClipController(_view, _model);
            }
        }
    }
}