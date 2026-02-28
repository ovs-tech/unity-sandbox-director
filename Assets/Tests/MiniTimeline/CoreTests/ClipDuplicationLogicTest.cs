using NUnit.Framework;
using System.Collections.Generic;
using Systems.MiniTimeline.Core;
using UnityEngine;
using System.Linq;

namespace Systems.MiniTimeline.Core.Tests
{
    public class ClipDuplicationLogicTest
    {
        // Concrete implementations for testing
        [System.Serializable]
        public class TestClip : MiniClipBase
        {
            public float customValue;
        }

        [System.Serializable]
        public class TestTrack : MiniTrackBase<TestClip>
        {
            protected override void OnEvaluate(float time, bool scrub) { }
        }

        [Test]
        public void CloneClip_WithJsonUtility_PreservesData()
        {
            // Arrange
            var originalClip = new TestClip();
            originalClip.Id = "original";
            originalClip.Start = 1.0f;
            originalClip.Duration = 2.0f;
            originalClip.customValue = 42.0f;

            // Act
            string json = JsonUtility.ToJson(originalClip);
            TestClip clonedClip = (TestClip)JsonUtility.FromJson(json, typeof(TestClip));

            // Assert
            Assert.AreEqual(originalClip.Id, clonedClip.Id);
            Assert.AreEqual(originalClip.Start, clonedClip.Start);
            Assert.AreEqual(originalClip.Duration, clonedClip.Duration);
            Assert.AreEqual(originalClip.customValue, clonedClip.customValue);
            Assert.AreNotSame(originalClip, clonedClip);
        }

        [Test]
        public void DuplicateAndAddClip_AddsNewClipToTrack()
        {
            // Arrange
            var track = new TestTrack();
            track.Id = "track1";

            var originalClip = new TestClip();
            originalClip.Id = "clip1";
            originalClip.Start = 1.0f;
            originalClip.Duration = 2.0f;

            track.AddClip(originalClip);

            // Act - Simulate Duplication Logic
            string json = JsonUtility.ToJson(originalClip);
            TestClip newClip = (TestClip)JsonUtility.FromJson(json, typeof(TestClip));

            newClip.Id = $"{originalClip.Id}_Copy";
            newClip.Start = originalClip.End;

            track.AddClip(newClip);

            // Assert
            var clips = track.GetClips().ToList();
            Assert.AreEqual(2, clips.Count);

            var addedClip = clips[1] as TestClip;
            Assert.IsNotNull(addedClip);
            Assert.AreEqual("clip1_Copy", addedClip.Id);
            Assert.AreEqual(3.0f, addedClip.Start); // 1.0 + 2.0
            Assert.AreEqual(2.0f, addedClip.Duration);
        }
    }
}
