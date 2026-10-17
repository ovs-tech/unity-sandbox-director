using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Paint3D.Addons.Tests
{
    public class AddonsTests
    {
        [Test]
        public void ColorCounting_CountsNearestColorAndSkipsUncovered()
        {
            var pixels = new[]
            {
                new Color32(255, 0, 0, 255),
                new Color32(250, 10, 5, 255),
                new Color32(0, 0, 255, 255),
                new Color32(0, 0, 255, 0) // outside UV islands
            };
            var palette = new List<Color> { Color.red, Color.blue };
            var ratios = new float[2];

            int covered = ColorCounting.Count(pixels, palette, ratios);

            Assert.AreEqual(3, covered);
            Assert.AreEqual(2f / 3f, ratios[0], 1e-5f);
            Assert.AreEqual(1f / 3f, ratios[1], 1e-5f);
        }

        [Test]
        public void ColorCounting_ToleranceRejectsFarColors()
        {
            var pixels = new[] { new Color32(255, 255, 255, 255), new Color32(255, 0, 0, 255) };
            var palette = new List<Color> { Color.red };
            var ratios = new float[1];

            ColorCounting.Count(pixels, palette, ratios, 0.2f);

            Assert.AreEqual(0.5f, ratios[0], 1e-5f);
        }

        [Test]
        public void DecalOperation_BoundsCoverRotatedBox()
        {
            var brush = new ResolvedBrush(Color.white, 0.1f, 0.5f, 1f, PaintBlendMode.Alpha);
            var operation = new DecalPaintOperation(Vector3.zero, Quaternion.Euler(0f, 45f, 0f), Vector3.one, null, brush);

            Bounds bounds = operation.WorldBounds;

            float halfDiagonal = Mathf.Sqrt(0.5f);
            Assert.AreEqual(halfDiagonal, bounds.extents.x, 1e-4f);
            Assert.AreEqual(0.5f, bounds.extents.y, 1e-4f);
        }

        [Test]
        public void DecalOperation_ReflectMirrorsVolume()
        {
            var brush = new ResolvedBrush(Color.white, 0.1f, 0.5f, 1f, PaintBlendMode.Alpha);
            var operation = new DecalPaintOperation(new Vector3(2f, 0f, 0f), Quaternion.identity, Vector3.one, null, brush);

            var reflected = (DecalPaintOperation)operation.Reflect(Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)));

            Assert.Less(Vector3.Distance(new Vector3(-2f, 0f, 0f), reflected.WorldBounds.center), 1e-4f);
        }
    }
}
