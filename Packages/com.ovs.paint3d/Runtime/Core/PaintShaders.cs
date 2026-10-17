using System.Collections.Generic;
using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// Shader names and property ids shared by the paint engine and paint operations.
    /// Paint3D shaders live in a Resources folder so they are always included in player builds.
    /// </summary>
    public static class PaintShaders
    {
        public const string SPHERE = "Hidden/Paint3D/Sphere";
        public const string DILATE = "Hidden/Paint3D/Dilate";
        public const string UV_MASK = "Hidden/Paint3D/UVMask";

        // Engine level (set per paintable texture).
        public static readonly int Matrix = Shader.PropertyToID("_Paint3DMatrix");
        public static readonly int NormalMatrix = Shader.PropertyToID("_Paint3DNormalMatrix");
        public static readonly int UVChannel = Shader.PropertyToID("_Paint3DUVChannel");
        public static readonly int Mode = Shader.PropertyToID("_Paint3DMode");
        public static readonly int Original = Shader.PropertyToID("_Paint3DOriginal");
        public static readonly int Mask = Shader.PropertyToID("_Paint3DMask");

        // Blend state (material level, cannot be set through property blocks).
        public static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        public static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        public static readonly int SrcBlendAlpha = Shader.PropertyToID("_SrcBlendA");
        public static readonly int DstBlendAlpha = Shader.PropertyToID("_DstBlendA");
        public static readonly int BlendOp = Shader.PropertyToID("_BlendOp");

        // Brush (set per operation).
        public static readonly int BrushPosition = Shader.PropertyToID("_BrushPosition");
        public static readonly int BrushDirection = Shader.PropertyToID("_BrushDirection");
        public static readonly int BrushParams = Shader.PropertyToID("_BrushParams");
        public static readonly int BrushColor = Shader.PropertyToID("_BrushColor");

        private static readonly Dictionary<string, Shader> _cache = new Dictionary<string, Shader>();

        /// <summary>Finds and caches a shader, logging an error once if it is missing.</summary>
        public static Shader Find(string shaderName)
        {
            if (_cache.TryGetValue(shaderName, out var shader) && shader != null)
            {
                return shader;
            }

            shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[Paint3D] Shader '{shaderName}' not found. Make sure the Paint3D package Resources folder is intact.");
            }
            _cache[shaderName] = shader;
            return shader;
        }
    }
}
