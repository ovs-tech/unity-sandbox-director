using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class AnimTrackTests
    {
        private GameObject targetObject;
        private AnimTrack track;

        [SetUp]
        public void Setup()
        {
            targetObject = new GameObject("AnimTarget");
            targetObject.AddComponent<Animator>();
            track = new AnimTrack();
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null) Object.DestroyImmediate(targetObject);
        }

        private void PrepareTrack(AnimTrack track, GameObject target)
        {
            var baseType = typeof(MiniTrackBase<AnimClip>);

            var targetField = baseType.GetField("targetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField != null) targetField.SetValue(track, target);

            var boundField = baseType.GetField("isBound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (boundField != null) boundField.SetValue(track, true);

            track.Prepare();
        }

        [Test]
        public void Test_AddClip_AddsToClipsList()
        {
            var clip = new AnimClip { Id = "test_clip", Start = 0, Duration = 5 };
            track.AddClip(clip);

            Assert.AreEqual(1, track.GetAllClips().Count);
            Assert.AreEqual(clip, track.GetClip("test_clip"));
        }

        [Test]
        public void Test_HasActiveClipsAt()
        {
            var clip = new AnimClip { Id = "test_clip", Start = 0, Duration = 5 };
            track.AddClip(clip);

            Assert.IsTrue(track.HasActiveClipsAt(2.5f));
            Assert.IsFalse(track.HasActiveClipsAt(6f));
        }

        [Test]
        public void Test_GetLoadingProgress_NoAssets()
        {
            // Should be 100% if no assets
            Assert.AreEqual(1.0f, track.GetLoadingProgress());
        }
    }
}
