using System.Collections.Generic;
using UnityEngine;

namespace Paint3D.Addons
{
    /// <summary>
    /// Physics collision painting: paints at contact points when this rigidbody/collider hits something
    /// (balls leaving marks, tires, impacts). Radius and opacity can scale with impact strength.
    /// </summary>
    [AddComponentMenu("Paint3D/Addons/Collision Painter")]
    public class CollisionPainter : MonoBehaviour
    {
        public enum PaintTarget
        {
            /// <summary>Paint the object that was hit.</summary>
            Other = 0,

            /// <summary>Paint this object (e.g. a car taking damage).</summary>
            Self = 1,

            /// <summary>Paint both.</summary>
            Both = 2
        }

        [Header("Brush")]
        [SerializeField] private PaintBrush _brush = new PaintBrush { Radius = 0.1f };
        [SerializeField] private MirrorSettings _mirror = new MirrorSettings();

        [Header("Behaviour")]
        [SerializeField] private PaintTarget _target = PaintTarget.Other;

        [Tooltip("Also paint while contacts persist (sliding / rolling trails).")]
        [SerializeField] private bool _paintOnStay;

        [Tooltip("Impulse below which contacts are ignored.")]
        [SerializeField, Min(0f)] private float _minImpulse = 0.1f;

        [Tooltip("Impulse that maps to full radius and opacity (0 = ignore impulse).")]
        [SerializeField, Min(0f)] private float _maxImpulse = 10f;

        [Tooltip("Radius used at the minimum impulse, as a fraction of the brush radius.")]
        [SerializeField, Range(0f, 1f)] private float _minRadiusFraction = 0.3f;

        [Tooltip("Minimum seconds between dabs for the same collider.")]
        [SerializeField, Min(0f)] private float _cooldown = 0.02f;

        private readonly List<ContactPoint> _contacts = new List<ContactPoint>();
        private readonly Dictionary<Collider, float> _lastPaintTime = new Dictionary<Collider, float>();

        public PaintBrush Brush => _brush;

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision, true);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (_paintOnStay)
            {
                HandleCollision(collision, false);
            }
        }

        private void OnDisable()
        {
            _lastPaintTime.Clear();
        }

        private void HandleCollision(Collision collision, bool isEnter)
        {
            float impulse = collision.impulse.magnitude;
            if (isEnter && impulse < _minImpulse)
            {
                return;
            }

            if (_lastPaintTime.TryGetValue(collision.collider, out float lastTime) && Time.time - lastTime < _cooldown)
            {
                return;
            }
            _lastPaintTime[collision.collider] = Time.time;

            float strength = _maxImpulse > 0f && isEnter ? Mathf.Clamp01(impulse / _maxImpulse) : 1f;
            ResolvedBrush brush = _brush.Resolve(1f, isEnter ? 0f : Time.fixedDeltaTime);
            brush = brush.WithRadius(brush.Radius * Mathf.Lerp(_minRadiusFraction, 1f, strength));
            brush = brush.WithOpacity(brush.Opacity * Mathf.Lerp(0.25f, 1f, strength));

            Paintable other = collision.collider.GetComponentInParent<Paintable>();
            Paintable self = GetComponentInParent<Paintable>();

            collision.GetContacts(_contacts);
            foreach (ContactPoint contact in _contacts)
            {
                // Contact normals point from the other collider toward this one.
                if ((_target == PaintTarget.Other || _target == PaintTarget.Both) && other != null)
                {
                    PaintEngine.Paint(other, new SpherePaintOperation(contact.point, -contact.normal, brush), _mirror);
                }
                if ((_target == PaintTarget.Self || _target == PaintTarget.Both) && self != null)
                {
                    PaintEngine.Paint(self, new SpherePaintOperation(contact.point, contact.normal, brush), _mirror);
                }
            }
        }
    }
}
