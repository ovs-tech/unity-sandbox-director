using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// Immutable snapshot of a <see cref="PaintBrush"/> after modifiers (pressure, jitter, flow) were applied.
    /// This is what paint operations consume.
    /// </summary>
    public readonly struct ResolvedBrush
    {
        public readonly Color Color;
        public readonly float Radius;
        public readonly float Hardness;
        public readonly float Opacity;
        public readonly PaintBlendMode BlendMode;
        public readonly int Group;
        public readonly bool PaintThrough;
        public readonly float NormalThreshold;

        public ResolvedBrush(
            Color color,
            float radius,
            float hardness,
            float opacity,
            PaintBlendMode blendMode,
            int group = 0,
            bool paintThrough = false,
            float normalThreshold = 0f)
        {
            Color = color;
            Radius = Mathf.Max(radius, 1e-5f);
            Hardness = Mathf.Clamp01(hardness);
            Opacity = Mathf.Clamp01(opacity);
            BlendMode = blendMode;
            Group = group;
            PaintThrough = paintThrough;
            NormalThreshold = Mathf.Clamp(normalThreshold, -1f, 1f);
        }

        /// <summary>Value sent to shaders: a threshold below -1 disables the facing test.</summary>
        public float ShaderNormalThreshold => PaintThrough ? -2f : NormalThreshold;

        public ResolvedBrush WithRadius(float radius) =>
            new ResolvedBrush(Color, radius, Hardness, Opacity, BlendMode, Group, PaintThrough, NormalThreshold);

        public ResolvedBrush WithOpacity(float opacity) =>
            new ResolvedBrush(Color, Radius, Hardness, opacity, BlendMode, Group, PaintThrough, NormalThreshold);

        public ResolvedBrush WithColor(Color color) =>
            new ResolvedBrush(color, Radius, Hardness, Opacity, BlendMode, Group, PaintThrough, NormalThreshold);
    }
}
