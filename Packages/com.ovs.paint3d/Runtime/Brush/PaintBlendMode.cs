namespace Paint3D
{
    /// <summary>
    /// How a brush dab is combined with the texels already in the paint texture.
    /// </summary>
    public enum PaintBlendMode
    {
        /// <summary>Standard alpha blending of the brush color over the texture.</summary>
        Alpha = 0,

        /// <summary>Adds the brush color to the texture.</summary>
        Additive = 1,

        /// <summary>Subtracts the brush color from the texture.</summary>
        Subtract = 2,

        /// <summary>Multiplies the texture by the brush color.</summary>
        Multiply = 3,

        /// <summary>Restores the original (unpainted) texture under the brush.</summary>
        Erase = 4
    }
}
