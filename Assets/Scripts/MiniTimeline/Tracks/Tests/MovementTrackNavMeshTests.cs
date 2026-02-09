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
        private GameObject navMeshPlane;
        private NavMeshDataInstance navMeshDataInstance;
        private MovementTrack track;

        [SetUp]
        public void Setup()
        {
            targetObject = new GameObject("MovementTarget");
            targetObject.AddComponent<NavMeshAgent>();
            track = new MovementTrack();

            // Create a simple flat plane and build a NavMesh at runtime so NavMeshAgent can be tested
            navMeshPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            navMeshPlane.name = "NavMeshPlane";
            navMeshPlane.transform.position = Vector3.zero;
            // Make the plane larger so NavMesh covers clip end positions (e.g., x=10)
            navMeshPlane.transform.localScale = new Vector3(4f, 1f, 4f);

            var mesh = navMeshPlane.GetComponent<MeshFilter>().sharedMesh;
            var sources = new List<NavMeshBuildSource>();
            var src = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = mesh,
                transform = navMeshPlane.transform.localToWorldMatrix,
                area = 0
            };
            sources.Add(src);

            var bounds = new Bounds(navMeshPlane.transform.position, new Vector3(50, 50, 50));
            var buildSettings = NavMesh.GetSettingsByIndex(0);
            var builtData = NavMeshBuilder.BuildNavMeshData(buildSettings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (builtData != null)
            {
                navMeshDataInstance = NavMesh.AddNavMeshData(builtData);
                // Ensure the test agent GameObject is placed on the NavMesh so the NavMeshAgent is valid when enabled
                var agent = targetObject.GetComponent<NavMeshAgent>();
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetObject.transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    targetObject.transform.position = hit.position;
                    // If agent exists and is enabled it may need warping; ensure it's positioned correctly
                    if (agent != null && agent.isActiveAndEnabled)
                    {
                        agent.Warp(hit.position);
                    }
                }
            }
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null)
                Object.DestroyImmediate(targetObject);
            if (navMeshDataInstance.valid)
                NavMesh.RemoveNavMeshData(navMeshDataInstance);
            if (navMeshPlane != null)
                Object.DestroyImmediate(navMeshPlane);
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
            // Allow small positional tolerance due to NavMesh sampling height differences
            Assert.LessOrEqual(Vector3.Distance(agent.destination, new Vector3(10, 0, 0)), 0.1f, "NavMeshAgent destination should be set to clip end position (within tolerance)");
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
