using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// A single GPU paint dab. Subclasses supply the shader and its parameters; the <see cref="PaintEngine"/>
    /// handles rendering into every matching <see cref="PaintableTexture"/>, mirroring, history and seam fixing.
    /// Addons extend painting by deriving from this class (see the decal operation in the addons package).
    /// </summary>
    public abstract class PaintOperation
    {
        protected PaintOperation(in ResolvedBrush brush)
        {
            Brush = brush;
        }

        /// <summary>Brush values (color, opacity, blend mode, group...) used by this dab.</summary>
        public ResolvedBrush Brush { get; protected set; }

        /// <summary>
        /// Shader rendering the dab in texture space. It must use <c>Paint3DVert</c> from Paint3DCommon.cginc
        /// and expose the <c>_SrcBlend/_DstBlend/_SrcBlendA/_DstBlendA/_BlendOp</c> blend properties.
        /// </summary>
        public abstract Shader Shader { get; }

        /// <summary>World space volume affected by the dab; used to cull paintables.</summary>
        public abstract Bounds WorldBounds { get; }

        /// <summary>Writes operation specific shader parameters.</summary>
        public abstract void SetProperties(MaterialPropertyBlock properties);

        /// <summary>Returns a copy of this operation transformed by a mirror (reflection) matrix.</summary>
        public abstract PaintOperation Reflect(Matrix4x4 reflection);
    }
}
