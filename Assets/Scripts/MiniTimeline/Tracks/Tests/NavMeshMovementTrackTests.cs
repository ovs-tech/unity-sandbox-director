using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;
using System.Linq;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class NavMeshMovementTrackTests
    {
        private NavMeshMovementTrack track;

        [SetUp]
        public void Setup()
        {
            track = new NavMeshMovementTrack();
        }

        [Test]
        public void Test_NavMeshMovementClip_GetPosition()
        {
            var clip = new NavMeshMovementClip
            {
                Start = 0f,
                Duration = 10f,
                hasPosition = true,
                startPosition = Vector3.zero,
                endPosition = new Vector3(10f, 0f, 0f),
                animationCurve = CameraAnimationCurve.Linear
            };

            // At 0s
            Assert.AreEqual(Vector3.zero, clip.GetPositionAtTime(0f));

            // At 5s
            Assert.AreEqual(new Vector3(5f, 0f, 0f), clip.GetPositionAtTime(5f));

            // At 10s
            Assert.AreEqual(new Vector3(10f, 0f, 0f), clip.GetPositionAtTime(10f));
        }

        [Test]
        public void Test_NavMeshMovementTrack_AddClip()
        {
            var clip = track.AddMovementClip(0f, 5f, 0);

            Assert.IsNotNull(clip);
            Assert.AreEqual(0f, clip.Start);
            Assert.AreEqual(5f, clip.Duration);
            Assert.AreEqual(0, clip.targetIndex);

            var clips = track.GetClips().ToList();
            Assert.Contains(clip, clips);
        }
    }
}
