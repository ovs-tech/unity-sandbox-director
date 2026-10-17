using NUnit.Framework;
using UnityEngine;

namespace Paint3D.Tests
{
    public class PaintBrushTests
    {
        [Test]
        public void Resolve_WithoutModifiers_ReturnsBrushValues()
        {
            var brush = new PaintBrush { Color = Color.green, Radius = 0.2f, Hardness = 0.3f, Opacity = 0.8f };
            brush.Modifiers.PressureAffectsOpacity = false;

            ResolvedBrush resolved = brush.Resolve(0.5f);

            Assert.AreEqual(0.2f, resolved.Radius, 1e-5f);
            Assert.AreEqual(0.3f, resolved.Hardness, 1e-5f);
            Assert.AreEqual(0.8f, resolved.Opacity, 1e-5f);
            Assert.AreEqual(Color.green, resolved.Color);
        }

        [Test]
        public void Resolve_PressureScalesOpacityAndRadius()
        {
            var brush = new PaintBrush { Radius = 1f, Opacity = 1f };
            brush.Modifiers.PressureAffectsOpacity = true;
            brush.Modifiers.PressureAffectsRadius = true;

            ResolvedBrush resolved = brush.Resolve(0.5f);

            Assert.AreEqual(0.5f, resolved.Opacity, 1e-4f);
            Assert.AreEqual(Mathf.Lerp(0.1f, 1f, 0.5f), resolved.Radius, 1e-4f);
        }

        [Test]
        public void Resolve_ColorAlphaActsAsOpacity()
        {
            var brush = new PaintBrush { Color = new Color(1f, 0f, 0f, 0.5f), Opacity = 1f };
            brush.Modifiers.PressureAffectsOpacity = false;

            ResolvedBrush resolved = brush.Resolve();

            Assert.AreEqual(0.5f, resolved.Opacity, 1e-5f);
            Assert.AreEqual(1f, resolved.Color.a, 1e-5f);
        }

        [Test]
        public void Resolve_GradualScalesOpacityByDeltaTime()
        {
            var brush = new PaintBrush { Opacity = 1f };
            brush.Modifiers.PressureAffectsOpacity = false;
            brush.Modifiers.Gradual = true;
            brush.Modifiers.FlowRate = 2f;

            Assert.AreEqual(0.2f, brush.Resolve(1f, 0.1f).Opacity, 1e-5f);
            Assert.AreEqual(1f, brush.Resolve(1f, 1f).Opacity, 1e-5f);
        }

        [Test]
        public void Resolve_JitterStaysWithinRange()
        {
            var brush = new PaintBrush { Radius = 1f, Opacity = 1f };
            brush.Modifiers.PressureAffectsOpacity = false;
            brush.Modifiers.RadiusJitter = 0.5f;
            brush.Modifiers.OpacityJitter = 0.5f;
            var random = new System.Random(42);

            for (int i = 0; i < 100; i++)
            {
                ResolvedBrush resolved = brush.Resolve(1f, 0f, random);
                Assert.That(resolved.Radius, Is.InRange(0.5f, 1.5f));
                Assert.That(resolved.Opacity, Is.InRange(0.5f, 1f));
            }
        }

        [Test]
        public void PaintThrough_DisablesFacingThreshold()
        {
            var brush = new PaintBrush { PaintThrough = true, NormalThreshold = 0.5f };
            Assert.Less(brush.Resolve().ShaderNormalThreshold, -1f);

            brush.PaintThrough = false;
            Assert.AreEqual(0.5f, brush.Resolve().ShaderNormalThreshold, 1e-5f);
        }

        [Test]
        public void GetSpacing_IsFractionOfRadius()
        {
            var brush = new PaintBrush();
            brush.Modifiers.Spacing = 0.5f;
            Assert.AreEqual(0.1f, brush.GetSpacing(0.2f), 1e-5f);
        }

        [Test]
        public void Clone_IsDeep()
        {
            var brush = new PaintBrush();
            PaintBrush clone = brush.Clone();
            clone.Modifiers.Spacing = 2f;
            Assert.AreNotEqual(brush.Modifiers.Spacing, clone.Modifiers.Spacing);
        }
    }
}
