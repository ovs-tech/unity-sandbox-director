using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// Spherical brush dab: every texel whose surface point lies inside the sphere receives paint,
    /// with a hardness based falloff and an optional facing test (disabled when painting through).
    /// </summary>
    public sealed class SpherePaintOperation : PaintOperation
    {
        public SpherePaintOperation(Vector3 position, Vector3 direction, in ResolvedBrush brush)
            : base(brush)
        {
            Position = position;
            Direction = direction;
        }

        /// <summary>Brush center in world space.</summary>
        public Vector3 Position { get; }

        /// <summary>Direction the brush moves toward the surface (e.g. a camera ray). Zero disables the facing test.</summary>
        public Vector3 Direction { get; }

        public override Shader Shader => PaintShaders.Find(PaintShaders.SPHERE);

        public override Bounds WorldBounds => new Bounds(Position, Vector3.one * (Brush.Radius * 2f));

        public override void SetProperties(MaterialPropertyBlock properties)
        {
            Vector3 dir = Direction.sqrMagnitude > 1e-8f ? Direction.normalized : Vector3.zero;
            properties.SetVector(PaintShaders.BrushPosition, Position);
            properties.SetVector(PaintShaders.BrushDirection, new Vector4(dir.x, dir.y, dir.z, dir == Vector3.zero ? 0f : 1f));
            properties.SetVector(PaintShaders.BrushParams, new Vector4(Brush.Radius, Brush.Hardness, Brush.Opacity, Brush.ShaderNormalThreshold));
            properties.SetColor(PaintShaders.BrushColor, Brush.Color);
        }

        public override PaintOperation Reflect(Matrix4x4 reflection) =>
            new SpherePaintOperation(reflection.MultiplyPoint3x4(Position), reflection.MultiplyVector(Direction), Brush);
    }
}
