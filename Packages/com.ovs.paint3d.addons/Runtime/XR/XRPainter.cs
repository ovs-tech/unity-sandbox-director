using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Paint3D.Addons
{
    /// <summary>
    /// VR painting with any XR controller (OpenXR, Oculus, etc.) through the built-in UnityEngine.XR input API,
    /// so no extra package is required. Paint by touching surfaces with the brush tip (contact mode) or by pointing
    /// at them (ray mode). Trigger = paint with pressure, primary button = undo, secondary button = redo.
    /// </summary>
    [AddComponentMenu("Paint3D/Addons/XR Painter")]
    public class XRPainter : MonoBehaviour
    {
        public enum PaintMode
        {
            /// <summary>Paints surfaces within the brush radius of the tip.</summary>
            Contact = 0,

            /// <summary>Paints where the tip's forward ray hits a paintable.</summary>
            Ray = 1
        }

        [Header("Controller")]
        [Tooltip("Hand whose controller drives painting.")]
        [SerializeField] private XRNode _hand = XRNode.RightHand;

        [Tooltip("Brush tip transform (usually a child of the tracked controller). Defaults to this transform.")]
        [SerializeField] private Transform _tip;

        [Tooltip("Trigger value above which painting starts.")]
        [SerializeField, Range(0.01f, 1f)] private float _triggerThreshold = 0.1f;

        [Header("Brush")]
        [SerializeField] private PaintBrush _brush = new PaintBrush { Radius = 0.02f };
        [SerializeField] private MirrorSettings _mirror = new MirrorSettings();

        [Tooltip("Layers that can be painted.")]
        [SerializeField] private LayerMask _layers = ~0;

        [Header("Mode")]
        [SerializeField] private PaintMode _mode = PaintMode.Contact;

        [Tooltip("Maximum ray length in ray mode.")]
        [SerializeField, Min(0.01f)] private float _rayLength = 5f;

        [Header("Feedback")]
        [Tooltip("Haptic amplitude while painting (0 = off).")]
        [SerializeField, Range(0f, 1f)] private float _hapticAmplitude = 0.1f;

        [Tooltip("Record strokes for undo / redo with the controller buttons.")]
        [SerializeField] private bool _recordHistory = true;

        private InputDevice _device;
        private readonly List<InputDevice> _devices = new List<InputDevice>();
        private bool _stroking;
        private bool _previousPrimary;
        private bool _previousSecondary;
        private bool _hasLastPoint;
        private Vector3 _lastPoint;
        private float _distanceToNextDab;

        public PaintBrush Brush => _brush;
        public PaintMode Mode { get => _mode; set => _mode = value; }
        public Transform Tip => _tip != null ? _tip : transform;

        /// <summary>True when the tip touches (contact) or points at (ray) a paintable this frame.</summary>
        public bool HasTarget { get; private set; }

        /// <summary>Current target point (valid when <see cref="HasTarget"/>).</summary>
        public Vector3 TargetPoint { get; private set; }

        private void OnDisable()
        {
            EndStroke();
        }

        private void Update()
        {
            if (!_device.isValid)
            {
                InputDevices.GetDevicesAtXRNode(_hand, _devices);
                _device = _devices.Count > 0 ? _devices[0] : default;
                if (!_device.isValid)
                {
                    EndStroke();
                    return;
                }
            }

            HandleHistoryButtons();

            _device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
            bool pressed = trigger >= _triggerThreshold;

            Vector3 point;
            Vector3 direction;
            HasTarget = FindTarget(out point, out direction);
            TargetPoint = point;

            if (!pressed)
            {
                EndStroke();
                return;
            }

            if (!_stroking)
            {
                BeginStroke();
            }

            if (!HasTarget)
            {
                _hasLastPoint = false;
                return;
            }

            float pressure = Mathf.InverseLerp(_triggerThreshold, 1f, trigger);
            StrokeTo(point, direction, pressure);

            if (_hapticAmplitude > 0f)
            {
                _device.SendHapticImpulse(0u, _hapticAmplitude * Mathf.Max(pressure, 0.25f), Time.deltaTime * 2f);
            }
        }

        private bool FindTarget(out Vector3 point, out Vector3 direction)
        {
            Transform tip = Tip;
            point = tip.position;
            direction = tip.forward;

            if (_mode == PaintMode.Ray)
            {
                if (PaintRaycaster.Raycast(new Ray(tip.position, tip.forward), out PaintHit hit, _rayLength, _layers))
                {
                    point = hit.Point;
                    return true;
                }
                return false;
            }

            // Contact: is the tip sphere close to any paintable's bounds?
            var tipBounds = new Bounds(point, Vector3.one * (_brush.Radius * 2f));
            IReadOnlyList<Paintable> paintables = Paintable.ActivePaintables;
            for (int i = 0; i < paintables.Count; i++)
            {
                Paintable paintable = paintables[i];
                if (paintable != null && (_layers & (1 << paintable.gameObject.layer)) != 0 && paintable.WorldBounds.Intersects(tipBounds))
                {
                    return true;
                }
            }
            return false;
        }

        private void StrokeTo(Vector3 point, Vector3 direction, float pressure)
        {
            if (_brush.Modifiers.Gradual || !_hasLastPoint)
            {
                ResolvedBrush first = _brush.Resolve(pressure, Time.deltaTime);
                Dab(point, direction, first);
                _lastPoint = point;
                _hasLastPoint = true;
                _distanceToNextDab = _brush.GetSpacing(first.Radius);
                return;
            }

            Vector3 delta = point - _lastPoint;
            float distance = delta.magnitude;
            float spacing = _brush.GetSpacing(_brush.Resolve(pressure).Radius);
            float travelled = _distanceToNextDab;
            while (travelled <= distance)
            {
                Dab(_lastPoint + (delta * (travelled / Mathf.Max(distance, 1e-6f))), direction, _brush.Resolve(pressure));
                travelled += spacing;
            }

            _distanceToNextDab = travelled - distance;
            _lastPoint = point;
        }

        private void Dab(Vector3 point, Vector3 direction, in ResolvedBrush brush)
        {
            Vector3 facing = _mode == PaintMode.Ray ? direction : Vector3.zero;
            PaintEngine.PaintAll(new SpherePaintOperation(point, facing, brush), _mirror, _layers);
        }

        private void HandleHistoryButtons()
        {
            _device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary);
            _device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary);

            if (_recordHistory && !_stroking)
            {
                if (primary && !_previousPrimary)
                {
                    PaintHistory.Undo();
                }
                else if (secondary && !_previousSecondary)
                {
                    PaintHistory.Redo();
                }
            }

            _previousPrimary = primary;
            _previousSecondary = secondary;
        }

        private void BeginStroke()
        {
            _stroking = true;
            _hasLastPoint = false;
            if (_recordHistory)
            {
                PaintHistory.BeginStroke();
            }
        }

        private void EndStroke()
        {
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

        private void OnDrawGizmosSelected()
        {
            Transform tip = Tip;
            Gizmos.color = new Color(_brush.Color.r, _brush.Color.g, _brush.Color.b, 0.8f);
            Gizmos.DrawWireSphere(tip.position, _brush.Radius);
            if (_mode == PaintMode.Ray)
            {
                Gizmos.DrawRay(tip.position, tip.forward * _rayLength);
            }
        }
    }
}
