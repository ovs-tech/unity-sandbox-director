using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;
using System.Reflection;
using System.Threading.Tasks;

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
            if (targetObject != null)
                Object.DestroyImmediate(targetObject);
        }

        private void SetTargetAndPrepare(AnimTrack track, GameObject target)
        {
            var baseType = typeof(MiniTrackBase<AnimClip>);

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

            // Invoke OnPrepare via reflection or just rely on public Prepare() if exposed?
            // MiniTrackBase usually exposes Prepare() which calls OnPrepare().
            // But MiniTrackBase might not have public Prepare(). Let's check.
            // Assuming Prepare is not public, we invoke OnPrepare via reflection.

            var onPrepareMethod = typeof(AnimTrack).GetMethod("OnPrepare", BindingFlags.Instance | BindingFlags.NonPublic);
            if (onPrepareMethod != null)
            {
                onPrepareMethod.Invoke(track, null);
            }
        }

        [Test]
        public void Test_AnimClip_GetAnimationTime()
        {
            var clip = new AnimClip
            {
                Start = 0f,
                Duration = 10f,
                speed = 1f,
                clipOffset = 0f
            };

            // At start
            Assert.AreEqual(0f, clip.GetAnimationTime(0f));

            // At 5s
            Assert.AreEqual(5f, clip.GetAnimationTime(5f));

            // With speed 2x
            clip.speed = 2f;
            Assert.AreEqual(10f, clip.GetAnimationTime(5f));

            // With offset
            clip.speed = 1f;
            clip.clipOffset = 2f;
            Assert.AreEqual(7f, clip.GetAnimationTime(5f));
        }

        [Test]
        public void Test_AnimClip_GetBlendWeight()
        {
            var clip = new AnimClip
            {
                Start = 0f,
                Duration = 10f,
                fadeIn = 2f,
                fadeOut = 2f,
                weight = 1f
            };

            // At start (0s) - fade in 0
            Assert.AreEqual(0f, clip.GetBlendWeight(0f), 0.001f);

            // At 1s - fade in 0.5
            Assert.AreEqual(0.5f, clip.GetBlendWeight(1f), 0.001f);

            // At 2s - full weight
            Assert.AreEqual(1f, clip.GetBlendWeight(2f), 0.001f);

            // At 5s - full weight
            Assert.AreEqual(1f, clip.GetBlendWeight(5f), 0.001f);

            // At 8s - start fade out (duration 10, fadeOut 2 -> starts at 8)
            Assert.AreEqual(1f, clip.GetBlendWeight(8f), 0.001f);

            // At 9s - fade out 0.5
            Assert.AreEqual(0.5f, clip.GetBlendWeight(9f), 0.001f);

            // At 10s - fade out 0
            Assert.AreEqual(0f, clip.GetBlendWeight(10f), 0.001f);
        }

        [Test]
        public void Test_AnimClip_ShouldPlay()
        {
            var clip = new AnimClip
            {
                Start = 5f,
                Duration = 5f
            };

            Assert.IsFalse(clip.ShouldPlay(4f));
            Assert.IsTrue(clip.ShouldPlay(5f));
            Assert.IsTrue(clip.ShouldPlay(7.5f));
            Assert.IsFalse(clip.ShouldPlay(10.1f));
        }

        [Test]
        public void Test_AnimTrack_AddAndRemoveClip()
        {
            SetTargetAndPrepare(track, targetObject);

            var clip = new AnimClip { Id = "clip1", Start = 0, Duration = 5 };
            track.AddClip(clip);

            Assert.AreEqual(1, track.GetAllClips().Count);
            Assert.AreEqual(clip, track.GetClip("clip1"));

            track.RemoveClip("clip1");
            Assert.AreEqual(0, track.GetAllClips().Count);
        }

        [Test]
        public void Test_AnimTrack_HasActiveClipsAt()
        {
            SetTargetAndPrepare(track, targetObject);

            var clip = new AnimClip { Id = "clip1", Start = 5, Duration = 5 };
            track.AddClip(clip);

            Assert.IsFalse(track.HasActiveClipsAt(4f));
            Assert.IsTrue(track.HasActiveClipsAt(7f));
            Assert.IsFalse(track.HasActiveClipsAt(11f));
        }
    }
}
