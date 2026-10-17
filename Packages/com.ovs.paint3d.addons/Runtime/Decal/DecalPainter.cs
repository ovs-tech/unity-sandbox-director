using UnityEngine;

namespace Paint3D.Addons
{
    /// <summary>
    /// Stamps a decal using this transform as the projection box (scale = <see cref="Size"/>, projecting along +Z).
    /// Call <see cref="PaintNow"/> from scripts/events, or enable continuous painting.
    /// </summary>
    [AddComponentMenu("Paint3D/Addons/Decal Painter")]
    public class DecalPainter : MonoBehaviour
    {
        [Header("Decal")]
        [Tooltip("Texture projected onto surfaces. Alpha masks the paint.")]
        [SerializeField] private Texture _texture;

        [Tooltip("Projection box size (width, height, depth) in world units.")]
        [SerializeField] private Vector3 _size = new Vector3(0.25f, 0.25f, 0.25f);

        [Tooltip("Tint, opacity, blend mode, group and facing settings. Radius is ignored.")]
        [SerializeField] private PaintBrush _brush = new PaintBrush { Color = Color.white, Hardness = 0.8f };

        [SerializeField] private MirrorSettings _mirror = new MirrorSettings();

        [Tooltip("Layers that can be painted.")]
        [SerializeField] private LayerMask _layers = ~0;

        [Header("Triggering")]
        [Tooltip("Stamp once when the component starts.")]
        [SerializeField] private bool _paintOnStart;

        [Tooltip("Stamp every frame (combine with gradual brush modifiers for spray-like decals).")]
        [SerializeField] private bool _continuous;

        [Tooltip("Record each PaintNow() call as an undo step.")]
        [SerializeField] private bool _recordHistory;

        public Texture Texture { get => _texture; set => _texture = value; }
        public Vector3 Size { get => _size; set => _size = value; }
        public PaintBrush Brush => _brush;

        private void Start()
        {
            if (_paintOnStart)
            {
                PaintNow();
            }
        }

        private void Update()
        {
            if (_continuous)
            {
                Paint(_brush.Resolve(1f, Time.deltaTime));
            }
        }

        /// <summary>Stamps the decal once on every overlapping paintable.</summary>
        public int PaintNow()
        {
            if (_recordHistory)
            {
                PaintHistory.BeginStroke();
            }

            int painted = Paint(_brush.Resolve());

            if (_recordHistory)
            {
                PaintHistory.EndStroke();
            }
            return painted;
        }

        private int Paint(in ResolvedBrush brush)
        {
            var operation = new DecalPaintOperation(transform.position, transform.rotation, _size, _texture, brush);
            return PaintEngine.PaintAll(operation, _mirror, _layers);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, _size);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.5f);
        }
    }
}
