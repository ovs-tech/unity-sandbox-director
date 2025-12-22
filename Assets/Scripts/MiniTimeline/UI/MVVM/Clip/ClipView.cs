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
            if (clipElement != null) {
                clipElement.RegisterCallback<PointerDownEvent>(evt => {
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
            if (resizeLeftHandle != null) {
                resizeLeftHandle.RegisterCallback<PointerDownEvent>(evt => {
                    IsResizingLeft = true;
                    DragStartX = evt.position.x;
                    evt.StopPropagation();
                });
            }

            var resizeRightHandle = GetElement("clip-resize-right");
            if (resizeRightHandle != null) {
                resizeRightHandle.RegisterCallback<PointerDownEvent>(evt => {
                    IsResizingRight = true;
                    DragStartX = evt.position.x;
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
                model.Select(!model.Selected);
                IsDragging = true;
                DragStartX = evt.position.x;
                model.OnDragStart();
                
                // Get element for capture (ClipUIToolkit uses clipElement which is GetElement("clip-root"))
                var clipElement = GetElement("clip-root");
                if (clipElement != null) {
                    clipElement.CapturePointer(evt.pointerId);
                }

                InitializeDrag(evt.localPosition, model.StartTime, model.Duration);
            }
        }

        private void OnPointerUp(PointerUpEvent evt, ClipModel model) {
            var clipElement = GetElement("clip-root");
            if (clipElement != null && clipElement.HasPointerCapture(evt.pointerId)) {
                clipElement.ReleasePointer(evt.pointerId);
            }

            if (IsDragging) {
                EndDrag();
                model.OnDragEnd();
                // Apply the new time to the model
                model.SetStartTime(_currentDragNewStartTime);
            }
            
            IsDragging = false;
            IsResizingLeft = false;
            IsResizingRight = false;
        }

        private void OnPointerMove(PointerMoveEvent evt, ClipModel model) {
            // IsResizingLeft = IsResizingLeft; // No-op
            // IsResizingRight = IsResizingRight; // No-op
            
            if (IsDragging) {
                // Logic ported from ClipUIToolkit.HandleDrag
                // 1. Get container local position
                // Note: ClipUIToolkit uses clipElement.parent.WorldToLocal(mousePosition).
                // Here Root is the clip wrapper. If Root.parent is the track container, we use that.

                if (Root == null || Root.parent == null) return;

                // evt.position is in panel coordinates (screen/window space equivalent in UIElements)
                // We need to convert it to the coordinate system of the parent of the clip.
                Vector2 localPos = Root.parent.WorldToLocal(evt.position);

                // 2. Calculate new position
                float targetX = localPos.x - _clipDragOffset;

                // 3. Convert to time
                // ClipUIToolkit uses editorUI.PositionToTimePublic(targetX)
                // We use _vm.PixelsPerSecond.Value to calculate time.
                // Time = Position / PixelsPerSecond
                float newStartTime = targetX / _vm.PixelsPerSecond.Value;

                // 4. Snap and clamp (simplified here, assume snapping is handled elsewhere or later)
                newStartTime = Mathf.Max(0f, newStartTime);

                // 5. Update visual
                UpdateDragPosition(newStartTime, model.Duration);
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
        }

        /// <summary>
        /// Update clip visual timing during drag operation.
        /// </summary>
        public void UpdateDragPosition(float newStartTime, float duration)
        {
            _currentDragNewStartTime = newStartTime;
            UpdateClipVisualTiming(newStartTime, duration);
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

            // Use TimeToPosition logic implicitly (newStart * PixelsPerSecond)
            // Ideally we could expose TimeToPosition function from VM, but calculation is simple.
            float startPos = newStart * _vm.PixelsPerSecond.Value;
            float width = visibleDuration * _vm.PixelsPerSecond.Value;

            Root.style.left = startPos;
            Root.style.width = width;
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