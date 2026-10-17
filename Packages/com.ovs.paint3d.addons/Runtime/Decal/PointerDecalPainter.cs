using UnityEngine;

namespace Paint3D.Addons
{
    public enum DecalRotationMode
    {
        /// <summary>Decal "up" follows the camera up vector.</summary>
        AlignToView = 0,

        /// <summary>Decal "up" follows the stroke direction.</summary>
        AlignToStroke = 1,

        /// <summary>Random roll per stamp.</summary>
        Random = 2
    }

    /// <summary>
    /// Live decal painting with mouse, pen or touch: every dab of the stroke is a projected texture
    /// (stickers, stamps, footprints, text...). Inherits mirroring, undo / redo and modifiers from <see cref="PointerPainter"/>.
    /// </summary>
    [AddComponentMenu("Paint3D/Addons/Pointer Decal Painter")]
    public class PointerDecalPainter : PointerPainter
    {
        [Header("Decal")]
        [Tooltip("Texture projected onto surfaces. Alpha masks the paint.")]
        [SerializeField] private Texture _decalTexture;

        [Tooltip("Width / height ratio of the decal (the brush radius sets the half height).")]
        [SerializeField, Min(0.01f)] private float _aspect = 1f;

        [Tooltip("Projection depth as a multiple of the brush radius.")]
        [SerializeField, Min(0.01f)] private float _depthScale = 1f;

        [Tooltip("Additional roll in degrees.")]
        [SerializeField, Range(-180f, 180f)] private float _angle;

        [SerializeField] private DecalRotationMode _rotationMode = DecalRotationMode.AlignToView;

        [Tooltip("Stamp a single decal per press instead of a continuous stroke.")]
        [SerializeField] private bool _stampOncePerPress = true;

        private Vector3 _previousPoint;
        private bool _hasPreviousPoint;
        private bool _stampedThisPress;

        public Texture DecalTexture { get => _decalTexture; set => _decalTexture = value; }

        protected override void Update()
        {
            if (!IsStroking)
            {
                _stampedThisPress = false;
                _hasPreviousPoint = false;
            }
            base.Update();
        }

        protected override float GetSpacing(in ResolvedBrush brush) =>
            _stampOncePerPress ? float.MaxValue : base.GetSpacing(brush);

        protected override PaintOperation CreateOperation(Vector3 point, Vector3 normal, Vector3 viewDirection, in ResolvedBrush brush)
        {
            if (_stampOncePerPress)
            {
                if (_stampedThisPress)
                {
                    return null;
                }
                _stampedThisPress = true;
            }

            Vector3 forward = -normal;
            Vector3 up;
            switch (_rotationMode)
            {
                case DecalRotationMode.AlignToStroke when _hasPreviousPoint && (point - _previousPoint).sqrMagnitude > 1e-8f:
                    up = point - _previousPoint;
                    break;
                case DecalRotationMode.Random:
                    up = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), forward) * Vector3.ProjectOnPlane(Vector3.up, forward);
                    break;
                default:
                    Camera cam = Camera != null ? Camera : Camera.main;
                    up = cam != null ? cam.transform.up : Vector3.up;
                    break;
            }

            if (Vector3.ProjectOnPlane(up, forward).sqrMagnitude < 1e-6f)
            {
                up = Vector3.ProjectOnPlane(Vector3.forward, forward);
            }

            _previousPoint = point;
            _hasPreviousPoint = true;

            Quaternion rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, 0f, _angle);
            float height = brush.Radius * 2f;
            var size = new Vector3(height * _aspect, height, brush.Radius * 2f * _depthScale);
            return new DecalPaintOperation(point, rotation, size, _decalTexture, brush);
        }
    }
}
