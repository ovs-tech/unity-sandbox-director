using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class CinemachineTrackTests
    {
        private CinemachineTrack track;

        [SetUp]
        public void Setup()
        {
            track = new CinemachineTrack();
        }

        [Test]
        public void Test_CinemachineClip_Defaults()
        {
            var clip = new CinemachineClip();
            Assert.AreEqual(CinemachineShotType.EyeLevel, clip.shotType);
            Assert.AreEqual(5f, clip.distance);
            Assert.AreEqual(60f, clip.fieldOfView);
        }

        [Test]
        public void Test_CinemachineTrack_AddShotClip()
        {
            var clip = track.AddShotClip(0f, 5f, CinemachineShotType.CloseUp, 1, 3f);

            Assert.IsNotNull(clip);
            Assert.AreEqual(0f, clip.Start);
            Assert.AreEqual(5f, clip.Duration);
            Assert.AreEqual(CinemachineShotType.CloseUp, clip.shotType);
            Assert.AreEqual(1, clip.targetIndex);
            Assert.AreEqual(3f, clip.distance);

            // GetClips returns IEnumerable<IMiniClip>, convert to list to check
            var clips = track.GetClips().ToList();
            Assert.Contains(clip, clips);
        }
    }
}
