#if UNITY_EDITOR
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniTimeline.UI.MVVM.Clip {
    public class ClipView {
        public VisualElement Root { get; private set; }

        private const float MinClipDuration = 0.001f;
        private const float MinClipWidthPixels = 10f;

        // Drag state
        private float _dragStartTime;
        private float _dragStartDuration;
        private float _currentDragNewStartTime;

        // Resize state
        private float _resizeDragStartTime;
        private float _resizeDragStartDuration;
        private float _resizeStartMouseX;
        private float _currentResizeNewStart;
        private float _currentResizeNewDuration;

        // UI state
        public bool IsDragging { get; set; }
        public bool IsResizingLeft { get; set; }
        public bool IsResizingRight { get; set; }
        public float DragStartX { get; set; }

        // Cached elements
        private VisualElement _clipElement;
        private VisualElement _resizeLeftHandle;
        private VisualElement _resizeRightHandle;

        public ClipView(VisualElement root, VisualTreeAsset uxml = null, StyleSheet uss = null) {
            Root = root;
            Initialize(uxml, uss);
        }

        private void Initialize(VisualTreeAsset uxml, StyleSheet uss)
        {
            if (Root == null) return;
            if (uss != null) Root.styleSheets.Add(uss);
            if (uxml != null) Root.Add(uxml.Instantiate());
        }

        public Button GetButton(string name) => Root?.Q<Button>(name);
        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);

        /// <summary>
        /// Convert time in seconds to pixel position using PixelsPerSecond.
        /// </summary>
        private float TimeToPosition(float timeInSeconds)
        {
            float pps = _viewModel?.PixelsPerSecond?.Value ?? 1f;
            return timeInSeconds * pps;
        }

        /// <summary>
        /// Convert pixel position to time in seconds using PixelsPerSecond.
        /// </summary>
        private float PositionToTime(float pixelPosition)
        {
            float pps = _viewModel?.PixelsPerSecond?.Value ?? 1f;
            return pixelPosition / pps;
        }

        /// <summary>
        /// Clamp duration to minimum allowed value.
        /// </summary>
        private float ClampMinDuration(float duration) => Mathf.Max(duration, MinClipDuration);

        /// <summary>
        /// Clamp start time to non-negative value.
        /// </summary>
        private float ClampStartTime(float startTime) => Mathf.Max(startTime, 0f);

        ClipController.ViewModel _viewModel;
        ClipModel _model;

        /// <summary>
        /// Binds the clip view to the view model and sets up all UI element interactions.
        /// </summary>
        public void Bind(ClipController.ViewModel viewModel, ClipModel model) {
            _viewModel = viewModel;
            _model = model;
            var title = GetLabel("clip-title");
            var durationLabel = GetLabel("clip-duration");
            var lockedIcon = GetElement("clip-lock-icon");
            var mutedIcon = GetElement("clip-mute-icon");
            
            // Display elements
            if (title != null) {
                title.SetBinding(nameof(Label.text), new DataBinding
                {
                    dataSource = viewModel.Title,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
                    bindingMode = BindingMode.ToTarget
                });
            }
            if (durationLabel != null) {
                durationLabel.text = $"{viewModel.Duration.Value:F2}s";
                durationLabel.SetBinding(nameof(Label.text), new DataBinding
                {
                    dataSource = viewModel.DurationText,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<float>.Value)),
                    bindingMode = BindingMode.ToTarget
                });
            }

            // Selection and interaction
            var clipElement = GetElement("clip-root");
            _clipElement = clipElement;
            
            if (_clipElement != null) {
                _clipElement.SetBinding(nameof(Label.tooltip), new DataBinding
                {
                    dataSource = viewModel.Title,
                    dataSourcePath = new PropertyPath(nameof(BindableProperty<string>.Value)),
                    bindingMode = BindingMode.ToTarget
                });
                _clipElement.pickingMode = PickingMode.Position;
                _clipElement.RegisterCallback<PointerDownEvent>(evt => {
                    OnPointerDown(evt, model);
                    _clipElement.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                });
                _clipElement.RegisterCallback<PointerMoveEvent>(evt => {
                    OnPointerMove(evt, model);
                });
                _clipElement.RegisterCallback<PointerUpEvent>(evt => {
                    OnPointerUp(evt, model);
                    _clipElement.ReleasePointer(evt.pointerId);
                });
            }

            // Resize handles - MUST be registered BEFORE clip-root handlers
            var resizeLeftHandle = GetElement("clip-resize-left");
            if (resizeLeftHandle != null) {
                _resizeLeftHandle = resizeLeftHandle;
                _resizeLeftHandle.pickingMode = PickingMode.Position;
                resizeLeftHandle.RegisterCallback<PointerDownEvent>(evt => {
                    IsResizingLeft = true;
                    DragStartX = evt.position.x;
                    InitializeResize(viewModel.StartTime.Value, viewModel.Duration.Value, evt.position.x);
                    _resizeLeftHandle.CapturePointer(evt.pointerId);
                    _resizeLeftHandle.AddToClassList("selected");
                    evt.StopPropagation();
                });

                resizeLeftHandle.RegisterCallback<PointerMoveEvent>(evt => {
                    if (!IsResizingLeft) return;
                    float timeDelta = PositionToTime(evt.position.x - _resizeStartMouseX);
                    UpdateResizeLeft(_resizeDragStartTime + timeDelta, _resizeDragStartDuration - timeDelta);
                    evt.StopPropagation();
                });

                resizeLeftHandle.RegisterCallback<PointerUpEvent>(evt => {
                    if (IsResizingLeft)
                    {
                        IsResizingLeft = false;
                        model.ResizeLeft(_currentResizeNewStart - _resizeDragStartTime);
                        EndResize();
                    }
                    _resizeLeftHandle.ReleasePointer(evt.pointerId);
                    _resizeLeftHandle.RemoveFromClassList("selected");
                    evt.StopPropagation();
                });
            }

            var resizeRightHandle = GetElement("clip-resize-right");
            if (resizeRightHandle != null) {
                _resizeRightHandle = resizeRightHandle;
                _resizeRightHandle.pickingMode = PickingMode.Position;
                resizeRightHandle.RegisterCallback<PointerDownEvent>(evt => {
                    IsResizingRight = true;
                    DragStartX = evt.position.x;
                    InitializeResize(viewModel.StartTime.Value, viewModel.Duration.Value, evt.position.x);
                    _resizeRightHandle.CapturePointer(evt.pointerId);
                    _resizeRightHandle.AddToClassList("selected");
                    evt.StopPropagation();
                });

                resizeRightHandle.RegisterCallback<PointerMoveEvent>(evt => {
                    if (!IsResizingRight) return;
                    float timeDelta = PositionToTime(evt.position.x - _resizeStartMouseX);
                    UpdateResizeRight(_resizeDragStartTime, _resizeDragStartDuration + timeDelta);
                    evt.StopPropagation();
                });

                resizeRightHandle.RegisterCallback<PointerUpEvent>(evt => {
                    if (IsResizingRight)
                    {
                        IsResizingRight = false;
                        model.ResizeRight(_currentResizeNewDuration - _resizeDragStartDuration);
                        EndResize();
                    }
                    _resizeRightHandle.ReleasePointer(evt.pointerId);
                    _resizeRightHandle.RemoveFromClassList("selected");
                    evt.StopPropagation();
                });
            }

            // Update display on changes
            model.OnPropertyChanged += () => {
                if (durationLabel != null) durationLabel.text = $"{viewModel.Duration.Value:F2}s";
                if (lockedIcon != null) lockedIcon.style.display = viewModel.Locked.Value ? DisplayStyle.Flex : DisplayStyle.None;
                if (mutedIcon != null) mutedIcon.style.display = viewModel.Muted.Value ? DisplayStyle.Flex : DisplayStyle.None;
            };

            model.OnSelectionChanged += (selected) => {
                if (clipElement != null) {
                    clipElement.EnableInClassList("selected", selected);
                }
            };
            model.OnZoomChanged += () => {
                UpdateClipVisualTiming(viewModel.StartTime.Value, viewModel.Duration.Value);
            };
        }

        private void OnPointerDown(PointerDownEvent evt, ClipModel model)
        {
            if (IsResizingLeft || IsResizingRight) return;

            model.Select(!model.Selected);
            IsDragging = true;
            DragStartX = evt.position.x;
            InitializeDrag(model.StartTime, model.Duration);
            
            // Capture pointer to ensure we receive move/up events even if pointer leaves element
            if (_clipElement != null) {
                _clipElement.CapturePointer(evt.pointerId);
            }
        }

        private void OnPointerUp(PointerUpEvent evt, ClipModel model)
        {
            if (IsDragging) {
                model.CommitMove(_dragStartTime, _currentDragNewStartTime);
                EndDrag();
                
                // Release pointer capture
                if (_clipElement != null) {
                    _clipElement.ReleasePointer(evt.pointerId);
                }
            }
            if (IsResizingLeft || IsResizingRight) {
                model.CommitResize(_resizeDragStartTime, _resizeDragStartDuration, _currentResizeNewStart, _currentResizeNewDuration);
                EndResize();
            }
            
            model.Select(false);
            IsDragging = false;
            IsResizingLeft = false;
            IsResizingRight = false;
        }
        
        private void OnPointerMove(PointerMoveEvent evt, ClipModel model)
        {
            if (IsDragging)
            {
                float timeDelta = PositionToTime(evt.position.x - DragStartX);
                UpdateDragPosition(_dragStartTime + timeDelta, model.Duration);
            }

            if (IsResizingLeft)
            {
                float timeDelta = PositionToTime(evt.position.x - _resizeStartMouseX);
                UpdateResizeLeft(_resizeDragStartTime + timeDelta, _resizeDragStartDuration - timeDelta);
            }
            else if (IsResizingRight)
            {
                float timeDelta = PositionToTime(evt.position.x - _resizeStartMouseX);
                UpdateResizeRight(_resizeDragStartTime, _resizeDragStartDuration + timeDelta);
            }
        }

        /// <summary>
        /// Initialize drag operation with start time and duration.
        /// </summary>
        private void InitializeDrag(float clipStartTime, float clipDuration)
        {
            _dragStartTime = clipStartTime;
            _dragStartDuration = clipDuration;
            _currentDragNewStartTime = clipStartTime;
            
            Root?.AddToClassList("dragging");
            if (Root != null) Root.style.opacity = 0.8f;
        }

        /// <summary>
        /// Update clip visual timing during drag operation.
        /// </summary>
        private void UpdateDragPosition(float newStartTime, float duration)
        {
            _currentDragNewStartTime = ClampStartTime(newStartTime);
            _dragStartDuration = ClampMinDuration(duration);
            UpdateClipVisualTiming(_currentDragNewStartTime, _dragStartDuration);
        }

        /// <summary>
        /// End drag operation and restore visual state.
        /// </summary>
        private void EndDrag()
        {
            Root?.RemoveFromClassList("dragging");
            if (Root != null) Root.style.opacity = 1f;
        }

        /// <summary>
        /// Initialize resize operation with start time, duration, and mouse position.
        /// </summary>
        private void InitializeResize(float startTime, float duration, float mouseX)
        {
            _resizeDragStartTime = startTime;
            _resizeDragStartDuration = duration;
            _resizeStartMouseX = mouseX;
            _currentResizeNewStart = startTime;
            _currentResizeNewDuration = duration;
            Root?.AddToClassList("resizing");
        }

        /// <summary>
        /// Update clip visual during left handle resize (changes start time).
        /// </summary>
        private void UpdateResizeLeft(float newStart, float newDuration)
        {
            _currentResizeNewStart = ClampStartTime(newStart);
            _currentResizeNewDuration = ClampMinDuration(newDuration);
            UpdateClipVisualTiming(_currentResizeNewStart, _currentResizeNewDuration);
        }

        /// <summary>
        /// Update clip visual during right handle resize (changes duration).
        /// </summary>
        private void UpdateResizeRight(float startTime, float newDuration)
        {
            _currentResizeNewStart = ClampStartTime(startTime);
            _currentResizeNewDuration = ClampMinDuration(newDuration);
            UpdateClipVisualTiming(startTime, _currentResizeNewDuration);
        }

        /// <summary>
        /// End resize operation and restore visual state.
        /// </summary>
        private void EndResize()
        {
            Root?.RemoveFromClassList("resizing");
        }

        /// <summary>
        /// Update clip visual timing (position and size).
        /// This only updates the UI visual representation, NOT the model state.
        /// </summary>
        public void UpdateClipVisualTiming(float newStart, float newDuration)
        {
            if (Root == null) return;

            // Only update visual representation, NOT the model
            Root.style.left = TimeToPosition(newStart);
            Root.style.width = TimeToPosition(newDuration);
        }
    }
}
#endif