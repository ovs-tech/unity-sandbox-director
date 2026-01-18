using UnityEngine;
using UnityEngine.UIElements;

namespace Systems {
    public class PanelResizeManipulator : PointerManipulator {
        bool _isResizing;
        Vector2 _startPointer;
        Vector2 _startSize;
        readonly float _minWidth;
        readonly float _minHeight;
        readonly float _maxWidth;
        readonly float _maxHeight;
        readonly VisualElement _resizeTarget;

        public PanelResizeManipulator(
            VisualElement resizeTarget = null,
            float minWidth = 100f,
            float minHeight = 60f,
            float maxWidth = float.PositiveInfinity,
            float maxHeight = float.PositiveInfinity
        ) {
            _resizeTarget = resizeTarget;
            _minWidth = Mathf.Max(0f, minWidth);
            _minHeight = Mathf.Max(0f, minHeight);
            _maxWidth = maxWidth <= 0f ? float.PositiveInfinity : maxWidth;
            _maxHeight = maxHeight <= 0f ? float.PositiveInfinity : maxHeight;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget() {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        protected override void UnregisterCallbacksFromTarget() {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        void OnPointerDown(PointerDownEvent evt) {
            var resizable = GetResizeTarget();
            if (resizable == null || !CanStartManipulation(evt) || _isResizing) return;

            _startPointer = evt.position;
            _startSize = ResolveCurrentSize(resizable);
            _isResizing = true;

            target.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt) {
            var resizable = GetResizeTarget();
            if (resizable == null || !_isResizing || !target.HasPointerCapture(evt.pointerId)) return;

            Vector2 delta = (Vector2)evt.position - _startPointer;
            float width = Mathf.Clamp(_startSize.x + delta.x, _minWidth, _maxWidth);
            float height = Mathf.Clamp(_startSize.y + delta.y, _minHeight, _maxHeight);

            resizable.style.width = width;
            resizable.style.height = height;
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt) {
            if (!CanStopManipulation(evt) || !_isResizing) return;

            _isResizing = false;
            target.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        VisualElement GetResizeTarget() => _resizeTarget ?? target;

        static Vector2 ResolveCurrentSize(VisualElement element) {
            float width = element.resolvedStyle.width;
            float height = element.resolvedStyle.height;

            if (float.IsNaN(width) || width <= 0f) width = element.layout.width;
            if (float.IsNaN(height) || height <= 0f) height = element.layout.height;

            return new Vector2(width, height);
        }
    }
}
