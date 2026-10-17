using UnityEngine;

namespace Paint3D
{
    /// <summary>
    /// Render texture helpers that behave the same on every platform (no compute, no CopyTexture requirement).
    /// </summary>
    public static class PaintTextureUtility
    {
        public static RenderTexture Create(int width, int height, bool sRGB, string name, RenderTextureFormat format = RenderTextureFormat.ARGB32)
        {
            if (!SystemInfo.SupportsRenderTextureFormat(format))
            {
                format = RenderTextureFormat.ARGB32;
            }

            var descriptor = new RenderTextureDescriptor(width, height, format, 0)
            {
                sRGB = sRGB,
                useMipMap = false,
                autoGenerateMips = false,
                msaaSamples = 1
            };

            var texture = new RenderTexture(descriptor)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.Create();
            return texture;
        }

        public static RenderTexture CreateLike(RenderTexture source, string name)
        {
            var texture = new RenderTexture(source.descriptor)
            {
                name = name,
                filterMode = source.filterMode,
                wrapMode = source.wrapMode,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.Create();
            return texture;
        }

        /// <summary>Fills a render texture with a solid color, respecting sRGB conversions.</summary>
        public static void Fill(RenderTexture target, Color color)
        {
            var solid = new Texture2D(1, 1, TextureFormat.RGBA32, false, !target.sRGB)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            solid.SetPixel(0, 0, color);
            solid.Apply(false);
            Graphics.Blit(solid, target);
            Destroy(solid);
        }

        /// <summary>Synchronously reads a render texture back to a new CPU texture (caller owns the result).</summary>
        public static Texture2D ReadToTexture2D(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, !source.sRGB);
            texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
            texture.Apply(false);
            RenderTexture.active = previous;
            return texture;
        }

        public static void Destroy(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj is RenderTexture rt)
            {
                rt.Release();
            }

            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
