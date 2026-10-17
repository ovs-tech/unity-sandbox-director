using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Paint3D.Tests
{
    public class MirrorAndRaycastTests
    {
        [Test]
        public void AppendReflections_SingleAxis_ProducesOneReflection()
        {
            var results = new List<Matrix4x4>();
            MirrorSettings.AppendReflections(Matrix4x4.identity, MirrorAxes.X, results);

            Assert.AreEqual(1, results.Count);
            Vector3 mirrored = results[0].MultiplyPoint3x4(new Vector3(1f, 2f, 3f));
            Assert.AreEqual(new Vector3(-1f, 2f, 3f), mirrored);
        }

        [Test]
        public void AppendReflections_TwoAxes_ProducesThreeReflections()
        {
            var results = new List<Matrix4x4>();
            MirrorSettings.AppendReflections(Matrix4x4.identity, MirrorAxes.X | MirrorAxes.Z, results);
            Assert.AreEqual(3, results.Count);
        }

        [Test]
        public void AppendReflections_AllAxes_ProducesSevenReflections()
        {
            var results = new List<Matrix4x4>();
            MirrorSettings.AppendReflections(Matrix4x4.identity, MirrorAxes.X | MirrorAxes.Y | MirrorAxes.Z, results);
            Assert.AreEqual(7, results.Count);
        }

        [Test]
        public void AppendReflections_UsesLocalSpace()
        {
            var results = new List<Matrix4x4>();
            Matrix4x4 space = Matrix4x4.TRS(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            MirrorSettings.AppendReflections(space, MirrorAxes.X, results);

            // Local +X of a transform rotated 90° around Y points along world -Z.
            Vector3 world = space.MultiplyPoint3x4(new Vector3(1f, 0f, 0f));
            Vector3 mirrored = results[0].MultiplyPoint3x4(world);
            Vector3 expected = space.MultiplyPoint3x4(new Vector3(-1f, 0f, 0f));
            Assert.Less(Vector3.Distance(expected, mirrored), 1e-4f);
        }

        [Test]
        public void SphereOperation_Reflect_MirrorsPositionAndDirection()
        {
            var brush = new ResolvedBrush(Color.white, 0.1f, 0.5f, 1f, PaintBlendMode.Alpha);
            var operation = new SpherePaintOperation(new Vector3(1f, 0f, 0f), Vector3.left, brush);

            var reflected = (SpherePaintOperation)operation.Reflect(Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)));

            Assert.AreEqual(new Vector3(-1f, 0f, 0f), reflected.Position);
            Assert.AreEqual(Vector3.right, reflected.Direction);
            Assert.AreEqual(brush.Radius, reflected.Brush.Radius);
        }

        [Test]
        public void IntersectTriangle_HitsAndMisses()
        {
            Vector3 a = new Vector3(-1f, -1f, 0f);
            Vector3 b = new Vector3(1f, -1f, 0f);
            Vector3 c = new Vector3(0f, 1f, 0f);

            Assert.IsTrue(PaintRaycaster.IntersectTriangle(new Vector3(0f, 0f, -5f), Vector3.forward, a, b, c, out float t));
            Assert.AreEqual(5f, t, 1e-4f);
            Assert.IsFalse(PaintRaycaster.IntersectTriangle(new Vector3(3f, 0f, -5f), Vector3.forward, a, b, c, out _));
            Assert.IsFalse(PaintRaycaster.IntersectTriangle(new Vector3(0f, 0f, 5f), Vector3.forward, a, b, c, out _));
        }

        [Test]
        public void SanitizeKey_ReplacesPathCharacters()
        {
            string key = PaintStorage.SanitizeKey("Scene/Root/Child:1");
            Assert.IsFalse(key.Contains("/"));
            Assert.IsFalse(key.Contains(":"));
            Assert.AreEqual("unnamed", PaintStorage.SanitizeKey(null));
        }
    }
}
