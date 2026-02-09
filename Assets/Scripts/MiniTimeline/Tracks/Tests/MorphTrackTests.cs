using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class MorphTrackTests
    {
        private GameObject targetObject;
        private MorphTrack track;
        private SkinnedMeshRenderer renderer;
        private Mesh mesh;

        [SetUp]
        public void Setup()
        {
            targetObject = new GameObject("MorphTarget");
            renderer = targetObject.AddComponent<SkinnedMeshRenderer>();

            // Create a mesh with blendshapes
            mesh = new Mesh();
            mesh.name = "MorphMesh";

            // Minimal setup for mesh
            Vector3[] vertices = new Vector3[] { Vector3.zero, Vector3.up, Vector3.right };
            mesh.vertices = vertices;

            // Add a blendshape
            Vector3[] deltaVertices = new Vector3[] { Vector3.up, Vector3.zero, Vector3.zero };
            mesh.AddBlendShapeFrame("Smile", 100f, deltaVertices, null, null);

            renderer.sharedMesh = mesh;

            track = new MorphTrack();
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null) Object.DestroyImmediate(targetObject);
            if (mesh != null) Object.DestroyImmediate(mesh);
        }

        private void PrepareTrack(MorphTrack track, GameObject target)
        {
            var baseType = typeof(MiniTrackBase<IMorphClip>);

            var targetField = baseType.GetField("targetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField != null) targetField.SetValue(track, target);

            var boundField = baseType.GetField("isBound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (boundField != null) boundField.SetValue(track, true);

            // Access internal preparation
            var method = typeof(MorphTrack).GetMethod("OnPrepare", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null) method.Invoke(track, null);

            // Set prepared flag
            var prepField = baseType.GetField("isPrepared", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (prepField != null) prepField.SetValue(track, true);
        }

        [Test]
        public void Test_BlendShape_Initialization()
        {
            PrepareTrack(track, targetObject);

            var shapes = track.GetAvailableBlendshapes();
            Assert.Contains("Smile", (System.Collections.ICollection)shapes);
        }

        [Test]
        public void Test_AddKeyClip()
        {
            PrepareTrack(track, targetObject);

            var keys = new System.Collections.Generic.List<MorphKey>();
            keys.Add(new MorphKey { id = "Smile", startValue = 0, endValue = 100 });

            var clip = track.AddKeyClip(0, 10, keys);

            Assert.AreEqual(1, track.GetClipsCount()); // Helper needed? No, GetClips() returns IEnumerable

            int count = 0;
            foreach(var c in track.GetClips()) count++;
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Test_Evaluate_UpdatesRenderer()
        {
            PrepareTrack(track, targetObject);

            var keys = new System.Collections.Generic.List<MorphKey>();
            keys.Add(new MorphKey { id = "Smile", startValue = 0, endValue = 100 });

            track.AddKeyClip(0, 10, keys);

            // Evaluate at 5s (midpoint) -> should be 50
            track.Evaluate(5f, false);

            float weight = renderer.GetBlendShapeWeight(0);
            Assert.AreEqual(50f, weight, 0.1f);
        }
    }
}
