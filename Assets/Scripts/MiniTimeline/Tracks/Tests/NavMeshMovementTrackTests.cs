using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class NavMeshMovementTrackTests
    {
        private GameObject targetObject;
        private NavMeshMovementTrack track;
        private NavMeshAgent agent;

        [SetUp]
        public void Setup()
        {
            targetObject = new GameObject("NavMeshTarget");
            agent = targetObject.AddComponent<NavMeshAgent>();
            track = new NavMeshMovementTrack();
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null) Object.DestroyImmediate(targetObject);
        }

        private void PrepareTrack(NavMeshMovementTrack track, GameObject target)
        {
            var baseType = typeof(MiniTrackBase<NavMeshMovementClip>);
            var multiTargetType = typeof(MultiTargetMiniTrackBase<NavMeshMovementClip>);

            // Set targetObject (just to be safe, though NavMeshMovementTrack uses targets list)
            var targetField = baseType.GetField("targetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField != null) targetField.SetValue(track, target);

            var boundField = baseType.GetField("isBound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (boundField != null) boundField.SetValue(track, true);

            // Set targets list
            var targetsField = multiTargetType.GetField("targets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetsField != null)
            {
                var list = new List<Transform>();
                list.Add(target.transform); // targetIndex 0
                targetsField.SetValue(track, list);
            }

            track.Prepare();
        }

        [Test]
        public void Test_AddMovementClip_AddsClip()
        {
            PrepareTrack(track, targetObject);

            var clip = track.AddMovementClip(0, 5, 0);

            Assert.AreEqual(1, GetClipCount(track));
            Assert.AreEqual(0, clip.Start);
            Assert.AreEqual(5, clip.Duration);
        }

        [Test]
        public void Test_Evaluate_SetsAgentDestination()
        {
            PrepareTrack(track, targetObject);

            var clip = track.AddMovementClip(0, 10, 0);
            clip.hasPosition = true;
            clip.startPosition = Vector3.zero;
            clip.endPosition = new Vector3(10, 0, 0);
            clip.hasSpeed = true;
            clip.startSpeed = 5f;

            // Evaluate at 5s (midpoint)
            // Normalized time 0.5
            // Position should be (5,0,0) (Linear default)

            // In scrubbing mode (true), it Warps agent.
            track.Evaluate(5f, true);

            // Agent warp moves transform
            Vector3 expectedPos = new Vector3(5, 0, 0);
            // NavMeshAgent.Warp might not work perfectly without NavMesh, but it usually sets transform position.
            // Let's verify transform position.

            Assert.AreEqual(expectedPos.x, targetObject.transform.position.x, 0.1f);
            Assert.AreEqual(expectedPos.y, targetObject.transform.position.y, 0.1f);
            Assert.AreEqual(expectedPos.z, targetObject.transform.position.z, 0.1f);
        }

        private int GetClipCount(NavMeshMovementTrack track)
        {
            int count = 0;
            foreach (var clip in track.GetClips()) count++;
            return count;
        }
    }
}
