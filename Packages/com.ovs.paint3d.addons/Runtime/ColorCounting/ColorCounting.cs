using System.Collections.Generic;
using UnityEngine;

namespace Paint3D.Addons
{
    /// <summary>Pure CPU color classification used by <see cref="PaintColorCounter"/> (kept separate so it is testable).</summary>
    public static class ColorCounting
    {
        /// <summary>
        /// Classifies each covered pixel (alpha >= 128, i.e. inside the UV mask) to the nearest palette color and writes
        /// the fraction of covered pixels per palette entry into <paramref name="ratios"/>. Pixels farther than
        /// <paramref name="tolerance"/> (RGB distance, 0..~1.73) from every palette color are counted as unmatched.
        /// </summary>
        /// <returns>Number of covered pixels.</returns>
        public static int Count(IReadOnlyList<Color32> pixels, IReadOnlyList<Color> palette, float[] ratios, float tolerance = 2f)
        {
            int paletteCount = palette.Count;
            for (int i = 0; i < ratios.Length; i++)
            {
                ratios[i] = 0f;
            }

            if (paletteCount == 0)
            {
                return 0;
            }

            var paletteBytes = new Vector3[paletteCount];
            for (int p = 0; p < paletteCount; p++)
            {
                Color32 c = palette[p];
                paletteBytes[p] = new Vector3(c.r, c.g, c.b);
            }

            float maxDistanceSq = tolerance * 255f * tolerance * 255f;
            int covered = 0;
            for (int i = 0; i < pixels.Count; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.a < 128)
                {
                    continue;
                }
                covered++;

                int best = -1;
                float bestDistance = maxDistanceSq;
                for (int p = 0; p < paletteCount; p++)
                {
                    float dr = pixel.r - paletteBytes[p].x;
                    float dg = pixel.g - paletteBytes[p].y;
                    float db = pixel.b - paletteBytes[p].z;
                    float distance = (dr * dr) + (dg * dg) + (db * db);
                    if (distance <= bestDistance)
                    {
                        bestDistance = distance;
                        best = p;
                    }
                }

                if (best >= 0 && best < ratios.Length)
                {
                    ratios[best] += 1f;
                }
            }

            if (covered > 0)
            {
                for (int i = 0; i < ratios.Length; i++)
                {
                    ratios[i] /= covered;
                }
            }
            return covered;
        }
    }
}
