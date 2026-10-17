using UnityEngine;

namespace Paint3D.Addons
{
    /// <summary>
    /// Projects a texture onto surfaces inside an oriented box (the decal volume), along the box's local +Z.
    /// Texture alpha masks the paint; the brush color tints it; hardness controls the fade toward the box's depth ends.
    /// </summary>
    public sealed class DecalPaintOperation : PaintOperation
    {
        public const string SHADER = "Hidden/Paint3D/Decal";

        private static readonly int DecalMatrixId = Shader.PropertyToID("_DecalMatrix");
        private static readonly int DecalTextureId = Shader.PropertyToID("_DecalTex");

        /// <param name="decalToWorld">Maps the unit cube (-0.5..0.5) to the decal volume in world space.</param>
        public DecalPaintOperation(Matrix4x4 decalToWorld, Texture texture, in ResolvedBrush brush)
            : base(brush)
        {
            DecalToWorld = decalToWorld;
            Texture = texture;
        }

        public DecalPaintOperation(Vector3 position, Quaternion rotation, Vector3 size, Texture texture, in ResolvedBrush brush)
            : this(Matrix4x4.TRS(position, rotation, size), texture, brush)
        {
        }

        public Matrix4x4 DecalToWorld { get; }
        public Texture Texture { get; }

        /// <summary>Projection direction (decal local +Z in world space).</summary>
        public Vector3 Direction => DecalToWorld.MultiplyVector(Vector3.forward).normalized;

        public override Shader Shader => PaintShaders.Find(SHADER);

        public override Bounds WorldBounds
        {
            get
            {
                var bounds = new Bounds(DecalToWorld.MultiplyPoint3x4(Vector3.zero), Vector3.zero);
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, (i & 4) == 0 ? -0.5f : 0.5f);
                    bounds.Encapsulate(DecalToWorld.MultiplyPoint3x4(corner));
                }
                return bounds;
            }
        }

        public override void SetProperties(MaterialPropertyBlock properties)
        {
            Vector3 direction = Direction;
            properties.SetMatrix(DecalMatrixId, DecalToWorld.inverse);
            properties.SetTexture(DecalTextureId, Texture != null ? Texture : Texture2D.whiteTexture);
            properties.SetVector(PaintShaders.BrushDirection, new Vector4(direction.x, direction.y, direction.z, 1f));
            properties.SetVector(PaintShaders.BrushParams, new Vector4(0f, Brush.Hardness, Brush.Opacity, Brush.ShaderNormalThreshold));
            properties.SetColor(PaintShaders.BrushColor, Brush.Color);
        }

        public override PaintOperation Reflect(Matrix4x4 reflection) =>
            new DecalPaintOperation(reflection * DecalToWorld, Texture, Brush);
    }
}
