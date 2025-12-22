#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniTimeline.UI.MVVM.Clip {
    public class ClipView {
        public VisualElement Root { get; private set; }

        // Drag/resize state exposed for controller
        public bool IsDragging { get; set; }
        public bool IsResizingLeft { get; set; }
        public bool IsResizingRight { get; set; }
        public float DragStartX { get; set; }

        // Drag state
        private Vector2 _dragStartPosition;
        private float _dragStartTime;
        private float _dragStartDuration;
        private float _clipDragOffset;
        private float _currentDragNewStartTime;

        // Resize state
        private float _resizeDragStartTime;
        private float _resizeDragStartDuration;
        private float _resizeStartMouseX;
        private float _currentResizeNewStart;
        private float _currentResizeNewDuration;
        private float _minClipWidth = 10f;

        // Cached elements
        private VisualElement _clipElement;
        private VisualElement _resizeLeftHandle;
        private VisualElement _resizeRightHandle;

        public ClipView(VisualElement root, VisualTreeAsset uxml = null, StyleSheet uss = null) {
            Root = root;
            Initialize(uxml, uss);
        }

        void Initialize(VisualTreeAsset uxml, StyleSheet uss) {
            if (Root == null) return;
            
            // Add stylesheet if provided
            if (uss != null) {
                Root.styleSheets.Add(uss);
            }
            
            // Instantiate and add UXML if provided
            if (uxml != null) {
                var tree = uxml.Instantiate();
                Root.Add(tree);
            }
        }

        public Button GetButton(string name) => Root?.Q<Button>(name);
        public Label GetLabel(string name) => Root?.Q<Label>(name);
        public VisualElement GetElement(string name) => Root?.Q<VisualElement>(name);

        ClipController.ViewModel _vm;

        /// <summary>
        /// Binds the clip view to the view model and sets up all UI element interactions.
        /// </summary>
        public void Bind(ClipController.ViewModel vm, ClipModel model) {
            _vm = vm;
            var title = GetLabel("clip-title");
            var durationLabel = GetLabel("clip-duration");
            var lockedIcon = GetElement("clip-lock-icon");
            var mutedIcon = GetElement("clip-mute-icon");
            
            // Display elements
            if (title != null) title.text = vm.Title.Value;
            if (durationLabel != null) durationLabel.text = $"{vm.Duration.Value:F2}s";

            // Selection and interaction
            var clipElement = GetElement("clip-root");
            _clipElement = clipElement;
            if (clipElement != null) {
                clipElement.RegisterCallback<PointerDownEvent>(evt => {
                    var targetVE = evt.target as VisualElement;
                    bool onLeft = _resizeLeftHandle != null && (_resizeLeftHandle == targetVE || _resizeLeftHandle.Contains(targetVE));
                    bool onRight = _resizeRightHandle != null && (_resizeRightHandle == targetVE || _resizeRightHandle.Contains(targetVE));
                    Debug.Log($"[ClipView] ClipRoot PointerDown target={(targetVE==null?"<null>":targetVE.name)} onLeft={onLeft} onRight={onRight}");
                    if (onLeft || onRight) {
                        // Let handle-specific callbacks process; do not start drag or stop propagation
                        return;
                    }
                    OnPointerDown(evt, model);
                    evt.StopPropagation();
                });
                clipElement.RegisterCallback<PointerUpEvent>(evt => {
                    OnPointerUp(evt, model);
                });
                clipElement.RegisterCallback<PointerMoveEvent>(evt => {
                    OnPointerMove(evt, model);
                });
            }

            // Resize handles
            var resizeLeftHandle = GetElement("clip-resize-left");
            Debug.Log($"[ClipView] Bind: resizeLeftHandle {(resizeLeftHandle == null ? "NOT FOUND" : "FOUND")}");
            if (resizeLeftHandle != null) {
                _resizeLeftHandle = resizeLeftHandle;
                _resizeLeftHandle.pickingMode = PickingMode.Position;
                resizeLeftHandle.RegisterCallback<PointerDownEvent>(evt => {
                    IsResizingLeft = true;
                    DragStartX = evt.position.x;
                    Debug.Log($"[ClipView] ResizeLeft PointerDown posX={evt.position.x} localPos={evt.localPosition} selected={vm.Selected.Value}");
                    InitializeResize(vm.StartTime.Value, vm.Duration.Value, evt.position.x);
                    _resizeLeftHandle.CapturePointer(evt.pointerId);
                    Debug.Log($"[ClipView] ResizeLeft CapturePointer id={evt.pointerId}");
                    evt.StopPropagation();
                });

                resizeLeftHandle.RegisterCallback<PointerMoveEvent>(evt => {
                    if (!IsResizingLeft) return;
                    float pps = _vm?.PixelsPerSecond?.Value ?? 1f;
                    float pixelDelta = evt.position.x - _resizeStartMouseX;
                    float timeDelta = pixelDelta / pps;
                    float newStart = _resizeDragStartTime + timeDelta;
                    float newDuration = _resizeDragStartDuration - timeDelta;
                    if (newDuration < 0.001f) newDuration = 0.001f;
                    UpdateResizeLeft(newStart, newDuration);
                    Debug.Log($"[ClipView] ResizeLeft Move pixelDelta={pixelDelta:F2} timeDelta={timeDelta:F3}s -> start={newStart:F3}s duration={newDuration:F3}s");
                    evt.StopPropagation();
                });

                resizeLeftHandle.RegisterCallback<PointerUpEvent>(evt => {
                    if (IsResizingLeft) {
                        IsResizingLeft = false;
                        // Commit model change for left resize using total delta
                        float delta = _currentResizeNewStart - _resizeDragStartTime;
                        Debug.Log($"[ClipView] ResizeLeft Commit delta={delta:F3}s");
                        model.ResizeLeft(delta);
                        EndResize();
                        Debug.Log($"[ClipView] ResizeLeft PointerUp end");
                    }
                    _resizeLeftHandle.ReleasePointer(evt.pointerId);
                    evt.StopPropagation();
                });
            }

            var resizeRightHandle = GetElement("clip-resize-right");
            Debug.Log($"[ClipView] Bind: resizeRightHandle {(resizeRightHandle == null ? "NOT FOUND" : "FOUND")}");
            if (resizeRightHandle != null) {
                _resizeRightHandle = resizeRightHandle;
                _resizeRightHandle.pickingMode = PickingMode.Position;
                resizeRightHandle.RegisterCallback<PointerDownEvent>(evt => {
                    IsResizingRight = true;
                    DragStartX = evt.position.x;
                    Debug.Log($"[ClipView] ResizeRight PointerDown posX={evt.position.x} localPos={evt.localPosition} selected={vm.Selected.Value}");
                    InitializeResize(vm.StartTime.Value, vm.Duration.Value, evt.position.x);
                    _resizeRightHandle.CapturePointer(evt.pointerId);
                    Debug.Log($"[ClipView] ResizeRight CapturePointer id={evt.pointerId}");
                    evt.StopPropagation();
                });

                resizeRightHandle.RegisterCallback<PointerMoveEvent>(evt => {
                    if (!IsResizingRight) return;
                    float pps = _vm?.PixelsPerSecond?.Value ?? 1f;
                    float pixelDelta = evt.position.x - _resizeStartMouseX;
                    float timeDelta = pixelDelta / pps;
                    float newStart = _resizeDragStartTime;
                    float newDuration = _resizeDragStartDuration + timeDelta;
                    if (newDuration < 0.001f) newDuration = 0.001f;
                    UpdateResizeRight(newStart, newDuration);
                    Debug.Log($"[ClipView] ResizeRight Move pixelDelta={pixelDelta:F2} timeDelta={timeDelta:F3}s -> start={newStart:F3}s duration={newDuration:F3}s");
                    evt.StopPropagation();
                });

                resizeRightHandle.RegisterCallback<PointerUpEvent>(evt => {
                    if (IsResizingRight) {
                        IsResizingRight = false;
                        // Commit model change for right resize using total delta
                        float delta = _currentResizeNewDuration - _resizeDragStartDuration;
                        Debug.Log($"[ClipView] ResizeRight Commit delta={delta:F3}s");
                        model.ResizeRight(delta);
                        EndResize();
                        Debug.Log($"[ClipView] ResizeRight PointerUp end");
                    }
                    _resizeRightHandle.ReleasePointer(evt.pointerId);
                    evt.StopPropagation();
                });
            }

            UpdateClipVisualStart(vm.StartTime.Value, vm.Duration.Value);

            // Update display on changes
            model.OnPropertyChanged += () => {
                if (durationLabel != null) durationLabel.text = $"{vm.Duration.Value:F2}s";
                if (lockedIcon != null) lockedIcon.style.display = vm.Locked.Value ? DisplayStyle.Flex : DisplayStyle.None;
                if (mutedIcon != null) mutedIcon.style.display = vm.Muted.Value ? DisplayStyle.Flex : DisplayStyle.None;
            };

            model.OnSelectionChanged += () => {
                if (clipElement != null) {
                    clipElement.EnableInClassList("selected", vm.Selected.Value);
                }
            };
            model.OnZoomChanged += () => {
                UpdateClipVisualTiming(vm.StartTime.Value, vm.Duration.Value);
            };
        }

        private void OnPointerDown(PointerDownEvent evt, ClipModel model) {
            if (!IsResizingLeft && !IsResizingRight) {
                // Select on drag start; do not toggle to avoid unintended deselect
                if (!model.Selected) model.Select(true);
                IsDragging = true;
                DragStartX = evt.position.x;
                Debug.Log($"[ClipView] DragStart posX={evt.position.x}, localPos={evt.localPosition}, startTime={model.StartTime}, duration={model.Duration}, pps={_vm?.PixelsPerSecond?.Value}");
                
                InitializeDrag(evt.localPosition, model.StartTime, model.Duration);
            }
        }

        private void OnPointerUp(PointerUpEvent evt, ClipModel model) {
            var wasDragging = IsDragging;
            IsDragging = false;
            IsResizingLeft = false;
            IsResizingRight = false;
            
            if (wasDragging) {
                EndDrag();
            }
            
            // End resize visual state
            EndResize();
            Debug.Log($"[ClipView] DragEnd newStart={GetDragNewStartTime()} origStart={GetDragStartTime()} duration={GetDragStartDuration()} pps={_vm?.PixelsPerSecond?.Value}");
        }

        private void OnPointerMove(PointerMoveEvent evt, ClipModel model) {
            IsResizingLeft = IsResizingLeft;
            IsResizingRight = IsResizingRight;
            
            if (IsDragging) {
                // Convert pixel delta to time delta using PixelsPerSecond
                float pixelDelta = evt.position.x - DragStartX;
                float timeDelta = pixelDelta / (_vm?.PixelsPerSecond?.Value ?? 1f);
                float newStartTime = GetDragStartTime() + timeDelta;
                UpdateDragPosition(newStartTime, model.Duration);
                Debug.Log($"[ClipView] DragMove screenX={evt.position.x:F2} startScreenX={DragStartX:F2} pixelDelta={pixelDelta:F2} timeDelta={timeDelta:F3}s newStart={newStartTime:F3}s duration={model.Duration:F3}s pps={_vm?.PixelsPerSecond?.Value}");
            }

            // Handle resizing left/right
            if (IsResizingLeft || IsResizingRight) {
                float pps = _vm?.PixelsPerSecond?.Value ?? 1f;
                float pixelDelta = evt.position.x - _resizeStartMouseX;
                float timeDelta = pixelDelta / pps;

                if (IsResizingLeft) {
                    float newStart = _resizeDragStartTime + timeDelta;
                    float newDuration = _resizeDragStartDuration - timeDelta;
                    if (newDuration < 0.001f) newDuration = 0.001f;
                    UpdateResizeLeft(newStart, newDuration);
                    Debug.Log($"[ClipView] ResizeLeft pixelDelta={pixelDelta:F2} timeDelta={timeDelta:F3}s -> start={newStart:F3}s duration={newDuration:F3}s");
                }

                if (IsResizingRight) {
                    float newStart = _resizeDragStartTime;
                    float newDuration = _resizeDragStartDuration + timeDelta;
                    if (newDuration < 0.001f) newDuration = 0.001f;
                    UpdateResizeRight(newStart, newDuration);
                    Debug.Log($"[ClipView] ResizeRight pixelDelta={pixelDelta:F2} timeDelta={timeDelta:F3}s -> start={newStart:F3}s duration={newDuration:F3}s");
                }
            }
        }

        /// <summary>
        /// Initialize drag operation with start time and position information.
        /// </summary>
        public void InitializeDrag(Vector2 localStartPos, float clipStartTime, float clipDuration)
        {
            _dragStartPosition = localStartPos;
            _dragStartTime = clipStartTime;
            _dragStartDuration = clipDuration;
            _currentDragNewStartTime = clipStartTime;
            _clipDragOffset = localStartPos.x;
            
            // Visual feedback
            Root?.AddToClassList("dragging");
            if (Root != null)
            {
                Root.style.opacity = 0.8f;
            }
            Debug.Log($"[ClipView] InitializeDrag localStartPos={localStartPos} startTime={clipStartTime} duration={clipDuration} pps={_vm?.PixelsPerSecond?.Value}");
        }

        /// <summary>
        /// Update clip visual timing during drag operation.
        /// </summary>
        public void UpdateDragPosition(float newStartTime, float duration)
        {
            _currentDragNewStartTime = newStartTime;
            UpdateClipVisualTiming(newStartTime, duration);
            Debug.Log($"[ClipView] UpdateDragPosition newStart={newStartTime:F3}s duration={duration:F3}s");
        }

        /// <summary>
        /// End drag operation and restore visual state.
        /// </summary>
        public void EndDrag()
        {
            Root?.RemoveFromClassList("dragging");
            if (Root != null)
            {
                Root.style.opacity = 1f;
            }
            Debug.Log("[ClipView] EndDrag visual reset");
        }

        /// <summary>
        /// Get the stored new start time from the last drag operation.
        /// </summary>
        public float GetDragNewStartTime() => _currentDragNewStartTime;

        /// <summary>
        /// Get the original start time before dragging.
        /// </summary>
        public float GetDragStartTime() => _dragStartTime;

        /// <summary>
        /// Get the original duration before dragging.
        /// </summary>
        public float GetDragStartDuration() => _dragStartDuration;

        /// <summary>
        /// Initialize resize operation with handle, start time, and duration information.
        /// </summary>
        public void InitializeResize(float startTime, float duration, float mouseX)
        {
            _resizeDragStartTime = startTime;
            _resizeDragStartDuration = duration;
            _resizeStartMouseX = mouseX;
            _currentResizeNewStart = startTime;
            _currentResizeNewDuration = duration;
            
            // Visual feedback
            Root?.AddToClassList("resizing");
            Debug.Log($"[ClipView] InitializeResize startTime={startTime:F3}s duration={duration:F3}s mouseX={mouseX:F2}");
        }

        /// <summary>
        /// Update clip visual during left handle resize (changes start time).
        /// </summary>
        public void UpdateResizeLeft(float newStart, float newDuration)
        {
            _currentResizeNewStart = newStart;
            _currentResizeNewDuration = newDuration;
            UpdateClipVisualTiming(newStart, newDuration);
        }

        /// <summary>
        /// Update clip visual during right handle resize (changes duration).
        /// </summary>
        public void UpdateResizeRight(float startTime, float newDuration)
        {
            _currentResizeNewStart = startTime;
            _currentResizeNewDuration = newDuration;
            UpdateClipVisualTiming(startTime, newDuration);
        }

        /// <summary>
        /// End resize operation and restore visual state.
        /// </summary>
        public void EndResize()
        {
            Root?.RemoveFromClassList("resizing");
            Debug.Log("[ClipView] EndResize visual reset");
        }

        /// <summary>
        /// Get the stored resize values from the last resize operation.
        /// </summary>
        public (float newStart, float newDuration) GetResizeValues()
        {
            return (_currentResizeNewStart, _currentResizeNewDuration);
        }

        /// <summary>
        /// Get the original resize values before resizing started.
        /// </summary>
        public (float startTime, float duration) GetResizeOriginalValues()
        {
            return (_resizeDragStartTime, _resizeDragStartDuration);
        }

        /// <summary>
        /// Update clip visual timing (position and size).
        /// </summary>
        public void UpdateClipVisualTiming(float newStart, float newDuration)
        {
            if (Root == null) return;

            // Ensure minimum width for usability
            float visibleDuration = newDuration;
            
            // For marker clips, use a fixed small width
            if (newDuration <= 0.001f)
            {
                visibleDuration = 20f;
            }

            // Use pixels-per-second to compute position and width in pixels
            float startPos = newStart * _vm.PixelsPerSecond.Value;
            float width = visibleDuration * _vm.PixelsPerSecond.Value;

            // Enforce a minimum pixel width for usability
            if (width < _minClipWidth)
            {
                width = _minClipWidth;
            }

            Root.style.left = startPos;
            Root.style.width = width;
            Debug.Log($"[ClipView] UpdateClipVisualTiming start={newStart:F3}s duration={newDuration:F3}s visibleDuration={visibleDuration:F3}s pps={_vm?.PixelsPerSecond?.Value} -> left={startPos:F2}px width={width:F2}px");
        }

        /// <summary>
        /// Update clip visual only the left edge (start time) - used during left handle resize.
        /// </summary>
        public void UpdateClipVisualStart(float newStart, float duration)
        {
            UpdateClipVisualTiming(newStart, duration);
        }

        /// <summary>
        /// Update clip visual only the right edge (duration) - used during right handle resize.
        /// </summary>
        public void UpdateClipVisualDuration(float startTime, float newDuration)
        {
            UpdateClipVisualTiming(startTime, newDuration);
        }
    }
}
#endif