using System.Collections.Generic;
using UnityEngine;

namespace Paint3D.Addons
{
    /// <summary>
    /// Paints where particles collide (paint guns, blood, rain, mud...). Enables collision messages on the particle
    /// system's collision module automatically. Requires the collision module to be set to World.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("Paint3D/Addons/Particle Collision Painter")]
    public class ParticleCollisionPainter : MonoBehaviour
    {
        [Header("Brush")]
        [SerializeField] private PaintBrush _brush = new PaintBrush { Radius = 0.05f, Hardness = 0.2f };
        [SerializeField] private MirrorSettings _mirror = new MirrorSettings();

        [Tooltip("Optional decal stamped instead of a sphere (leave empty for spheres).")]
        [SerializeField] private Texture _decalTexture;

        [Header("Behaviour")]
        [Tooltip("Scale the brush radius by the particle's speed (0 = disabled). Radius *= 1 + speed * scale.")]
        [SerializeField, Min(0f)] private float _speedToRadius;

        [Tooltip("Use the particle color as the paint color.")]
        [SerializeField] private bool _useParticleColor;

        [Tooltip("Also paint other paintables overlapping the impact (not only the collided object).")]
        [SerializeField] private bool _paintOverlapping;

        [Tooltip("Hard cap of dabs per frame to protect performance.")]
        [SerializeField, Min(1)] private int _maxDabsPerFrame = 64;

        private ParticleSystem _particleSystem;
        private readonly List<ParticleCollisionEvent> _events = new List<ParticleCollisionEvent>();
        private ParticleSystem.Particle[] _particles;
        private int _frame = -1;
        private int _dabsThisFrame;

        public PaintBrush Brush => _brush;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            ParticleSystem.CollisionModule collision = _particleSystem.collision;
            collision.enabled = true;
            collision.sendCollisionMessages = true;
        }

        private void OnParticleCollision(GameObject other)
        {
            if (_frame != Time.frameCount)
            {
                _frame = Time.frameCount;
                _dabsThisFrame = 0;
            }

            Paintable target = other.GetComponentInParent<Paintable>();
            if (target == null && !_paintOverlapping)
            {
                return;
            }

            int count = _particleSystem.GetCollisionEvents(other, _events);
            for (int i = 0; i < count && _dabsThisFrame < _maxDabsPerFrame; i++)
            {
                ParticleCollisionEvent collisionEvent = _events[i];
                ResolvedBrush brush = _brush.Resolve();
                Vector3 velocity = collisionEvent.velocity;

                if (_speedToRadius > 0f)
                {
                    brush = brush.WithRadius(brush.Radius * (1f + (velocity.magnitude * _speedToRadius)));
                }
                if (_useParticleColor)
                {
                    brush = brush.WithColor(GetNearestParticleColor(collisionEvent.intersection));
                }

                Vector3 direction = velocity.sqrMagnitude > 1e-6f ? velocity : -collisionEvent.normal;
                PaintOperation operation = CreateOperation(collisionEvent.intersection, collisionEvent.normal, direction, brush);

                if (_paintOverlapping || target == null)
                {
                    PaintEngine.PaintAll(operation, _mirror);
                }
                else
                {
                    PaintEngine.Paint(target, operation, _mirror);
                }
                _dabsThisFrame++;
            }
        }

        private PaintOperation CreateOperation(Vector3 point, Vector3 normal, Vector3 direction, in ResolvedBrush brush)
        {
            if (_decalTexture == null)
            {
                return new SpherePaintOperation(point, direction, brush);
            }

            Vector3 forward = -normal;
            Vector3 up = Vector3.ProjectOnPlane(Random.onUnitSphere, forward);
            if (up.sqrMagnitude < 1e-6f)
            {
                up = Vector3.ProjectOnPlane(Vector3.up, forward);
            }
            float size = brush.Radius * 2f;
            return new DecalPaintOperation(point, Quaternion.LookRotation(forward, up), new Vector3(size, size, size), _decalTexture, brush);
        }

        private Color GetNearestParticleColor(Vector3 point)
        {
            int max = _particleSystem.main.maxParticles;
            if (_particles == null || _particles.Length < max)
            {
                _particles = new ParticleSystem.Particle[max];
            }

            int alive = _particleSystem.GetParticles(_particles);
            bool world = _particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.World;
            Color best = _brush.Color;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < alive; i++)
            {
                Vector3 position = world ? _particles[i].position : transform.TransformPoint(_particles[i].position);
                float distance = (position - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = _particles[i].GetCurrentColor(_particleSystem);
                }
            }
            best.a = 1f;
            return best;
        }
    }
}
