using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;
using System.Reflection;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class MovementTrackNavMeshTests
    {
        private GameObject targetObject;
        private MovementTrack track;

        [SetUp]
        public void Setup()
        {
            targetObject = new GameObject("MovementTarget");
            targetObject.AddComponent<NavMeshAgent>();
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
        public void Test_NavMeshMode_EnablesAgent()
        {
            SetTargetAndPrepare(track, targetObject);
            var agent = targetObject.GetComponent<NavMeshAgent>();
            agent.enabled = false;

            // Create NavMesh clip
            var clip = track.AddPositionClip(0, 10, Vector3.zero, new Vector3(10, 0, 0));
            clip.mode = MovementMode.NavMesh;

            // Evaluate
            track.Evaluate(5f, false);

            Assert.IsTrue(agent.enabled, "NavMeshAgent should be enabled in NavMesh mode");
            Assert.AreEqual(new Vector3(10, 0, 0), agent.destination, "NavMeshAgent destination should be set to clip end position");
        }

        [Test]
        public void Test_NavMeshMode_SpeedCalculation_Fallback()
        {
            SetTargetAndPrepare(track, targetObject);
            var agent = targetObject.GetComponent<NavMeshAgent>();

            // Create NavMesh clip: 10 units in 5 seconds
            var startPos = Vector3.zero;
            var endPos = new Vector3(10, 0, 0);
            var duration = 5f;
            var clip = track.AddPositionClip(0, duration, startPos, endPos);
            clip.mode = MovementMode.NavMesh;

            // Evaluate
            track.Evaluate(1f, false);

            // Without baked NavMesh, fallback distance is straight line = 10 units.
            // Expected Speed = 10 / 5 = 2.
            float expectedSpeed = 2f;

            Assert.AreEqual(expectedSpeed, agent.speed, 0.01f, "NavMeshAgent speed should be calculated based on distance and duration");
        }

        [Test]
        public void Test_SwitchToDirect_DisablesAgent()
        {
            SetTargetAndPrepare(track, targetObject);
            var agent = targetObject.GetComponent<NavMeshAgent>();

            // Clip 1: NavMesh (0-5s)
            var clip1 = track.AddPositionClip(0, 5, Vector3.zero, new Vector3(10, 0, 0));
            clip1.mode = MovementMode.NavMesh;

            // Clip 2: Direct (5-10s)
            var clip2 = track.AddPositionClip(5, 5, new Vector3(10, 0, 0), new Vector3(20, 0, 0));
            clip2.mode = MovementMode.Direct;

            // Evaluate NavMesh clip
            track.Evaluate(2.5f, false);
            Assert.IsTrue(agent.enabled, "Agent should be enabled during NavMesh clip");

            // Evaluate Direct clip
            track.Evaluate(7.5f, false);
            Assert.IsFalse(agent.enabled, "Agent should be disabled during Direct clip");
        }
    }
}
