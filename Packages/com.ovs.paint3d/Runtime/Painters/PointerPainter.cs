using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// Live in-game painting with mouse, pen or touch. Strokes are interpolated with the brush spacing, recorded for
    /// undo / redo, mirrored, and can paint gradually while the pointer is held. Override <see cref="CreateOperation"/>
    /// to paint something other than a sphere (the addons package uses this for decals).
    /// </summary>
    [AddComponentMenu("Paint3D/Pointer Painter")]
    public class PointerPainter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("Camera used to cast pointer rays. Defaults to Camera.main.")]
        [SerializeField] private Camera _camera;

        [Header("Brush")]
        [SerializeField] private PaintBrush _brush = new PaintBrush();

        [Tooltip("Mirror every dab across the hit paintable's local axes (or a shared pivot).")]
        [SerializeField] private MirrorSettings _mirror = new MirrorSettings();

        [Header("Targeting")]
        [Tooltip("Layers that can be painted.")]
        [SerializeField] private LayerMask _layers = ~0;

        [Tooltip("Maximum ray distance.")]
        [SerializeField, Min(0.01f)] private float _maxDistance = 1000f;

        [Tooltip("Use physics colliders for ray hits (non-paintable colliders occlude painting).")]
        [SerializeField] private bool _usePhysics = true;

        [Tooltip("Paint every overlapping paintable, not only the one under the pointer.")]
        [SerializeField] private bool _paintAllOverlapping = true;

        [Header("Behaviour")]
        [Tooltip("Ignore strokes that start over uGUI elements.")]
        [SerializeField] private bool _ignoreUI = true;

        [Tooltip("Record strokes for undo / redo.")]
        [SerializeField] private bool _recordHistory = true;

        [Tooltip("Handle Ctrl/Cmd+Z and Ctrl/Cmd+Y (or Shift+Z).")]
        [SerializeField] private bool _keyboardShortcuts = true;

        #endregion

        private bool _stroking;
        private bool _blockedStroke;
        private bool _hasLastPoint;
        private Vector3 _lastPoint;
        private Vector3 _lastNormal;
        private float _distanceToNextDab;

        public PaintBrush Brush { get => _brush; set => _brush = value; }
        public MirrorSettings Mirror { get => _mirror; set => _mirror = value; }
        public Camera Camera { get => _camera; set => _camera = value; }
        public bool IsStroking => _stroking;

        protected virtual void Update()
        {
            if (_keyboardShortcuts && !_stroking)
            {
                if (PaintPointerInput.UndoPressed())
                {
                    PaintHistory.Undo();
                }
                else if (PaintPointerInput.RedoPressed())
                {
                    PaintHistory.Redo();
                }
            }

            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null || !PaintPointerInput.TryGetPointer(out PaintPointerInput.PointerState pointer) || !pointer.Pressed)
            {
                EndStroke();
                return;
            }

            if (!_stroking && !_blockedStroke && _ignoreUI && PaintPointerInput.IsPointerOverUI(pointer.PointerId))
            {
                _blockedStroke = true;
            }
            if (_blockedStroke)
            {
                return;
            }

            if (!_stroking)
            {
                BeginStroke();
            }

            Ray ray = cam.ScreenPointToRay(pointer.Position);
            if (!PaintRaycaster.Raycast(ray, out PaintHit hit, _maxDistance, _layers, _usePhysics))
            {
                // Lifting off the surface breaks interpolation but keeps the stroke (and its undo step) open.
                _hasLastPoint = false;
                return;
            }

            StrokeTo(hit, cam.transform.position, pointer.Pressure);
        }

        protected virtual void OnDisable()
        {
            EndStroke();
        }

        /// <summary>Creates the operation for one dab. Override to paint decals or custom shapes.</summary>
        protected virtual PaintOperation CreateOperation(Vector3 point, Vector3 normal, Vector3 viewDirection, in ResolvedBrush brush)
        {
            return new SpherePaintOperation(point, viewDirection, brush);
        }

        /// <summary>Distance between dabs; override for stamp-like painters.</summary>
        protected virtual float GetSpacing(in ResolvedBrush brush) => _brush.GetSpacing(brush.Radius);

        protected void BeginStroke()
        {
            _stroking = true;
            _hasLastPoint = false;
            if (_recordHistory)
            {
                PaintHistory.BeginStroke();
            }
        }

        protected void EndStroke()
        {
            _blockedStroke = false;
            if (!_stroking)
            {
                return;
            }

            _stroking = false;
            _hasLastPoint = false;
            if (_recordHistory)
            {
                PaintHistory.EndStroke();
            }
        }

        private void StrokeTo(in PaintHit hit, Vector3 eye, float pressure)
        {
            if (_brush.Modifiers.Gradual)
            {
                // Gradual painting accumulates every frame at the current point.
                Dab(hit.Paintable, hit.Point, hit.Normal, eye, _brush.Resolve(pressure, Time.deltaTime));
                _lastPoint = hit.Point;
                _lastNormal = hit.Normal;
                _hasLastPoint = true;
                return;
            }

            ResolvedBrush resolved = _brush.Resolve(pressure);
            float spacing = GetSpacing(resolved);

            if (!_hasLastPoint)
            {
                Dab(hit.Paintable, hit.Point, hit.Normal, eye, resolved);
                _lastPoint = hit.Point;
                _lastNormal = hit.Normal;
                _hasLastPoint = true;
                _distanceToNextDab = spacing;
                return;
            }

            Vector3 delta = hit.Point - _lastPoint;
            float distance = delta.magnitude;

            // Large jumps (e.g. moving across a gap to another object) restart the stroke path.
            if (distance > resolved.Radius * 16f)
            {
                _hasLastPoint = false;
                StrokeTo(hit, eye, pressure);
                return;
            }

            float travelled = _distanceToNextDab;
            while (travelled <= distance)
            {
                float t = travelled / Mathf.Max(distance, 1e-6f);
                Vector3 point = _lastPoint + (delta * t);
                Vector3 normal = Vector3.Slerp(_lastNormal, hit.Normal, t);
                Dab(hit.Paintable, point, normal, eye, _brush.Resolve(pressure));
                travelled += spacing;
            }

            _distanceToNextDab = travelled - distance;
            _lastPoint = hit.Point;
            _lastNormal = hit.Normal;
        }

        private void Dab(Paintable target, Vector3 point, Vector3 normal, Vector3 eye, in ResolvedBrush brush)
        {
            Vector3 viewDirection = (point - eye).normalized;
            PaintOperation operation = CreateOperation(point, normal, viewDirection, brush);
            if (operation == null)
            {
                return;
            }

            if (_paintAllOverlapping)
            {
                Transform mirrorSpace = _mirror != null && _mirror.Pivot != null ? _mirror.Pivot : target.MirrorSpace;
                PaintEngine.PaintAll(operation, _mirror, _layers, mirrorSpace);
            }
            else
            {
                PaintEngine.Paint(target, operation, _mirror);
            }
        }
    }
}
