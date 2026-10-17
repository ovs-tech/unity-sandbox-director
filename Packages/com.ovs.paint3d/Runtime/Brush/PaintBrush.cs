using System;
using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// Serializable brush settings shared by every painter (pointer, editor, VR, collision, particles...).
    /// </summary>
    [Serializable]
    public class PaintBrush
    {
        [Header("Appearance")]
        [Tooltip("Paint color. Alpha is used as an additional opacity factor.")]
        [SerializeField] private Color _color = Color.red;

        [Tooltip("Brush radius in world units.")]
        [SerializeField, Min(0.0001f)] private float _radius = 0.05f;

        [Tooltip("0 = very soft falloff, 1 = hard edge.")]
        [SerializeField, Range(0f, 1f)] private float _hardness = 0.5f;

        [Tooltip("Strength of a single dab.")]
        [SerializeField, Range(0f, 1f)] private float _opacity = 1f;

        [Tooltip("How the brush is combined with the existing texture.")]
        [SerializeField] private PaintBlendMode _blendMode = PaintBlendMode.Alpha;

        [Header("Targeting")]
        [Tooltip("Only paintable textures with the same group are painted (e.g. 0 = albedo, 1 = emission, 2 = mask).")]
        [SerializeField] private int _group;

        [Tooltip("Paint every surface inside the brush volume, including back faces and surfaces behind the hit point.")]
        [SerializeField] private bool _paintThrough;

        [Tooltip("When not painting through: minimum dot(surfaceNormal, -brushDirection) for a texel to receive paint.")]
        [SerializeField, Range(-1f, 1f)] private float _normalThreshold;

        [Header("Modifiers")]
        [SerializeField] private BrushModifiers _modifiers = new BrushModifiers();

        public Color Color { get => _color; set => _color = value; }
        public float Radius { get => _radius; set => _radius = Mathf.Max(0.0001f, value); }
        public float Hardness { get => _hardness; set => _hardness = Mathf.Clamp01(value); }
        public float Opacity { get => _opacity; set => _opacity = Mathf.Clamp01(value); }
        public PaintBlendMode BlendMode { get => _blendMode; set => _blendMode = value; }
        public int Group { get => _group; set => _group = value; }
        public bool PaintThrough { get => _paintThrough; set => _paintThrough = value; }
        public float NormalThreshold { get => _normalThreshold; set => _normalThreshold = Mathf.Clamp(value, -1f, 1f); }
        public BrushModifiers Modifiers => _modifiers ??= new BrushModifiers();

        /// <summary>
        /// Applies pressure, jitter and gradual flow to produce the values used for one dab.
        /// </summary>
        /// <param name="pressure">Pen / touch / trigger pressure in 0..1 (use 1 when unknown).</param>
        /// <param name="deltaTime">Frame delta; only used when gradual painting is enabled.</param>
        /// <param name="random">Optional deterministic random source (defaults to UnityEngine.Random).</param>
        public ResolvedBrush Resolve(float pressure = 1f, float deltaTime = 0f, System.Random random = null)
        {
            var modifiers = Modifiers;
            float radius = _radius;
            float opacity = _opacity * _color.a;
            Color color = _color;

            float pressureFactor = modifiers.EvaluatePressure(pressure);
            if (modifiers.PressureAffectsRadius)
            {
                radius *= Mathf.Lerp(0.1f, 1f, pressureFactor);
            }
            if (modifiers.PressureAffectsOpacity)
            {
                opacity *= pressureFactor;
            }

            if (modifiers.RadiusJitter > 0f)
            {
                radius *= 1f + (NextSigned(random) * modifiers.RadiusJitter);
            }
            if (modifiers.OpacityJitter > 0f)
            {
                opacity *= 1f - (Next01(random) * modifiers.OpacityJitter);
            }
            if (modifiers.HueJitter > 0f)
            {
                Color.RGBToHSV(color, out float h, out float s, out float v);
                h = Mathf.Repeat(h + (NextSigned(random) * modifiers.HueJitter * 0.5f), 1f);
                Color jittered = Color.HSVToRGB(h, s, v);
                jittered.a = color.a;
                color = jittered;
            }

            if (modifiers.Gradual)
            {
                opacity *= Mathf.Clamp01(deltaTime * modifiers.FlowRate);
            }

            color.a = 1f;
            return new ResolvedBrush(color, radius, _hardness, opacity, _blendMode, _group, _paintThrough, _normalThreshold);
        }

        /// <summary>World distance between two dabs of a stroke for the given resolved radius.</summary>
        public float GetSpacing(float resolvedRadius) => Mathf.Max(resolvedRadius * Modifiers.Spacing, 1e-4f);

        public PaintBrush Clone()
        {
            var clone = (PaintBrush)MemberwiseClone();
            clone._modifiers = Modifiers.Clone();
            return clone;
        }

        private static float Next01(System.Random random) =>
            random != null ? (float)random.NextDouble() : UnityEngine.Random.value;

        private static float NextSigned(System.Random random) => (Next01(random) * 2f) - 1f;
    }
}
