using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;
using System.Reflection;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class MovementTrackTests
    {
        private GameObject targetObject;
        private MovementTrack track;

        [SetUp]
        public void Setup()
        {
            targetObject = new GameObject("MovementTarget");
            track = new MovementTrack();
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null)
                Object.DestroyImmediate(targetObject);
        }

        private void SetTargetAndPrepare(MovementTrack track, GameObject target)
        {
            var baseType = typeof(MiniTrackBase<MovementClip>);

            // Set targetObject
            var targetField = baseType.GetField("targetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField != null)
                targetField.SetValue(track, target);
            else
                Debug.LogError("Could not find targetObject field");

            // Set isBound
            var boundField = baseType.GetField("isBound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (boundField != null)
                boundField.SetValue(track, true);
            else
                Debug.LogError("Could not find isBound field");

            track.Prepare();
        }

        [Test]
        public void Test_MultiClipBlending_Position_EqualWeights()
        {
            SetTargetAndPrepare(track, targetObject);

            // Clip A: 0 to 10s. Pos (0,0,0) to (10,0,0).
            var clipA = track.AddPositionClip(0, 10, Vector3.zero, new Vector3(10, 0, 0));

            // Clip B: 5 to 15s. Pos (0,10,0) to (0,20,0).
            var clipB = track.AddPositionClip(5, 10, new Vector3(0, 10, 0), new Vector3(0, 20, 0));

            // At time 7.5:
            // Clip A (t=7.5, norm=0.75): Pos = (7.5, 0, 0)
            // Clip B (t=7.5, local=2.5, norm=0.25): Pos = Lerp((0,10,0), (0,20,0), 0.25) = (0, 12.5, 0)

            // Expected Blend (Equal Weights): (3.75, 6.25, 0)

            track.Evaluate(7.5f, false);

            Vector3 expected = new Vector3(3.75f, 6.25f, 0f);

            // Use a tolerance because of floating point
            Assert.That(targetObject.transform.position.x, Is.EqualTo(expected.x).Within(0.01f));
            Assert.That(targetObject.transform.position.y, Is.EqualTo(expected.y).Within(0.01f));
            Assert.That(targetObject.transform.position.z, Is.EqualTo(expected.z).Within(0.01f));
        }

        [Test]
        public void Test_MultiClipBlending_Position_Weighted()
        {
            SetTargetAndPrepare(track, targetObject);

            // Clip A: 0 to 10s. Pos (0,0,0) to (10,0,0). No fade.
            var clipA = track.AddPositionClip(0, 10, Vector3.zero, new Vector3(10, 0, 0));

            // Clip B: 5 to 15s. FadeIn=5s.
            var clipB = track.AddPositionClip(5, 10, new Vector3(0, 10, 0), new Vector3(0, 20, 0));
            clipB.fadeIn = 5f;

            // At time 7.5:
            // Clip A weight = 1.0. Pos = (7.5, 0, 0).
            // Clip B weight (local=2.5, fadeIn=5) = 2.5/5 = 0.5. Pos = (0, 12.5, 0).

            // Total weight = 1.5.
            // Result = (PosA * 1 + PosB * 0.5) / 1.5
            // Result = ((7.5, 0, 0) + (0, 6.25, 0)) / 1.5
            // Result = (7.5, 6.25, 0) / 1.5 = (5, 4.1666, 0)

            track.Evaluate(7.5f, false);

            Vector3 expected = new Vector3(5f, 4.166666f, 0f);

            Assert.That(targetObject.transform.position.x, Is.EqualTo(expected.x).Within(0.01f));
            Assert.That(targetObject.transform.position.y, Is.EqualTo(expected.y).Within(0.01f));
            Assert.That(targetObject.transform.position.z, Is.EqualTo(expected.z).Within(0.01f));
        }
    }
}
