using System;
using System.Collections;
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

        // Drag/resize state
        private bool _isDragging;
        private bool _isResizingLeft;
        private bool _isResizingRight;
        private float _dragStartX;
        private float _dragStartTime;

        ClipController(ClipView view, ClipModel model) {
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
            var title = _view.GetLabel("clipTitleLabel");
            var durationLabel = _view.GetLabel("clipDurationLabel");
            var lockedIcon = _view.GetLabel("clipLockedIcon");
            var mutedIcon = _view.GetLabel("clipMutedIcon");
            
            // Display elements
            if (title != null) title.text = vm.Title.Value;
            if (durationLabel != null) durationLabel.text = $"{vm.Duration.Value:F2}s";

            // Selection and interaction
            var clipElement = _view.GetElement("clipContainer");
            if (clipElement != null) {
                clipElement.RegisterCallback<MouseDownEvent>(OnMouseDown);
                clipElement.RegisterCallback<MouseUpEvent>(OnMouseUp);
                clipElement.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            }

            // Resize handles
            var resizeLeftHandle = _view.GetElement("resizeHandleLeft");
            if (resizeLeftHandle != null) {
                resizeLeftHandle.RegisterCallback<MouseDownEvent>(evt => {
                    _isResizingLeft = true;
                    _dragStartX = evt.mousePosition.x;
                    evt.StopPropagation();
                });
            }

            var resizeRightHandle = _view.GetElement("resizeHandleRight");
            if (resizeRightHandle != null) {
                resizeRightHandle.RegisterCallback<MouseDownEvent>(evt => {
                    _isResizingRight = true;
                    _dragStartX = evt.mousePosition.x;
                    evt.StopPropagation();
                });
            }

            // Update display on changes
            _model.OnPropertyChanged += () => {
                if (durationLabel != null) durationLabel.text = $"{vm.Duration.Value:F2}s";
                if (lockedIcon != null) lockedIcon.style.display = vm.Locked.Value ? DisplayStyle.Flex : DisplayStyle.None;
                if (mutedIcon != null) mutedIcon.style.display = vm.Muted.Value ? DisplayStyle.Flex : DisplayStyle.None;
            };

            _model.OnSelectionChanged += () => {
                if (clipElement != null) {
                    clipElement.EnableInClassList("clip-selected", vm.Selected.Value);
                }
            };
        }

        private void OnMouseDown(MouseDownEvent evt) {
            if (!_isResizingLeft && !_isResizingRight) {
                var vm = _view as dynamic;
                vm?.Select(!vm.Selected.Value);
                _isDragging = true;
                _dragStartX = evt.mousePosition.x;
                _model.OnDragStart();
                evt.StopPropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent evt) {
            _isDragging = false;
            _isResizingLeft = false;
            _isResizingRight = false;
            _model.OnDragEnd();
        }

        private void OnMouseMove(MouseMoveEvent evt) {
            // TODO: Implement drag/resize movement with snapping
            // Would use time conversion and snap-to-grid logic
        }

        public class ViewModel {
            public readonly BindableProperty<string> Title;
            public readonly BindableProperty<float> Duration;
            public readonly BindableProperty<float> StartTime;
            public readonly BindableProperty<bool> Locked;
            public readonly BindableProperty<bool> Muted;
            public readonly BindableProperty<bool> Selected;

            readonly ClipModel _model;
            
            public ViewModel(ClipModel model) {
                _model = model;
                Title = BindableProperty<string>.Bind(() => _model.Title);
                Duration = BindableProperty<float>.Bind(() => _model.Duration);
                StartTime = BindableProperty<float>.Bind(() => _model.StartTime);
                Locked = BindableProperty<bool>.Bind(() => _model.Locked);
                Muted = BindableProperty<bool>.Bind(() => _model.Muted);
                Selected = BindableProperty<bool>.Bind(() => _model.Selected);
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