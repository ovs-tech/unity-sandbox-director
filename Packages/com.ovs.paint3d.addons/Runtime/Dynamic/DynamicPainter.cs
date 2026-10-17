using UnityEngine;

namespace Paint3D.Addons
{
    /// <summary>
    /// Dynamic painting: a moving object (brush tip, wheel, foot, sword, mop...) paints every paintable it passes
    /// through. Movement between frames is interpolated with the brush spacing so fast objects leave continuous trails,
    /// and both the painter and the targets may be animated (skinned targets are re-baked each frame).
    /// </summary>
    [AddComponentMenu("Paint3D/Addons/Dynamic Painter")]
    public class DynamicPainter : MonoBehaviour
    {
        public enum DirectionMode
        {
            /// <summary>Omnidirectional (paints all faces inside the sphere).</summary>
            None = 0,

            /// <summary>Paints surfaces facing against the movement direction.</summary>
            Movement = 1,

            /// <summary>Paints surfaces facing against this transform's forward.</summary>
            TransformForward = 2
        }

        [Header("Brush")]
        [SerializeField] private PaintBrush _brush = new PaintBrush { Radius = 0.1f, PaintThrough = true };
        [SerializeField] private MirrorSettings _mirror = new MirrorSettings();

        [Tooltip("Layers that can be painted.")]
        [SerializeField] private LayerMask _layers = ~0;

        [Header("Behaviour")]
        [Tooltip("Facing test direction used when the brush does not paint through.")]
        [SerializeField] private DirectionMode _direction = DirectionMode.Movement;

        [Tooltip("Only paint while moving faster than this (units / second). 0 paints while stationary too.")]
        [SerializeField, Min(0f)] private float _minSpeed = 0.01f;

        [Tooltip("Optional pressure (0..1) source, e.g. driven by gameplay. 1 = full strength.")]
        [SerializeField, Range(0f, 1f)] private float _pressure = 1f;

        private Vector3 _lastPosition;
        private bool _hasLastPosition;
        private float _distanceToNextDab;

        public PaintBrush Brush => _brush;
        public float Pressure { get => _pressure; set => _pressure = Mathf.Clamp01(value); }
        public bool Painting { get; set; } = true;

        private void OnEnable()
        {
            _hasLastPosition = false;
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            if (!Painting || Paintable.ActivePaintables.Count == 0)
            {
                _lastPosition = position;
                _hasLastPosition = true;
                return;
            }

            if (!_hasLastPosition)
            {
                _lastPosition = position;
                _hasLastPosition = true;
                _distanceToNextDab = 0f;
            }

            Vector3 delta = position - _lastPosition;
            float distance = delta.magnitude;
            float speed = Time.deltaTime > 0f ? distance / Time.deltaTime : 0f;
            if (speed < _minSpeed)
            {
                if (_minSpeed <= 0f || _brush.Modifiers.Gradual)
                {
                    Dab(position, delta, _brush.Resolve(_pressure, Time.deltaTime));
                }
                _lastPosition = position;
                return;
            }

            if (_brush.Modifiers.Gradual)
            {
                Dab(position, delta, _brush.Resolve(_pressure, Time.deltaTime));
                _lastPosition = position;
                return;
            }

            ResolvedBrush resolved = _brush.Resolve(_pressure);
            float spacing = _brush.GetSpacing(resolved.Radius);
            float travelled = _distanceToNextDab;
            while (travelled <= distance)
            {
                Vector3 point = _lastPosition + (delta * (travelled / Mathf.Max(distance, 1e-6f)));
                Dab(point, delta, _brush.Resolve(_pressure));
                travelled += spacing;
            }

            _distanceToNextDab = travelled - distance;
            _lastPosition = position;
        }

        private void Dab(Vector3 point, Vector3 movement, in ResolvedBrush brush)
        {
            Vector3 direction = _direction switch
            {
                DirectionMode.Movement => movement,
                DirectionMode.TransformForward => transform.forward,
                _ => Vector3.zero
            };
            PaintEngine.PaintAll(new SpherePaintOperation(point, direction, brush), _mirror, _layers);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(_brush.Color.r, _brush.Color.g, _brush.Color.b, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _brush.Radius);
        }
    }
}
