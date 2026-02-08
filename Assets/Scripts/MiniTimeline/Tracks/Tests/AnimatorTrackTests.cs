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
    public class AnimatorTrackTests
    {
        private GameObject targetObject;
        private AnimatorTrack track;

        [SetUp]
        public void Setup()
        {
            targetObject = new GameObject("AnimatorTarget");
            targetObject.AddComponent<Animator>();
            track = new AnimatorTrack();
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null)
                Object.DestroyImmediate(targetObject);
        }

        private void SetTargetAndPrepare(AnimatorTrack track, GameObject target)
        {
            var baseType = typeof(MiniTrackBase<AnimatorClip>);

            var targetField = baseType.GetField("targetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField != null)
                targetField.SetValue(track, target);

            var boundField = baseType.GetField("isBound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (boundField != null)
                boundField.SetValue(track, true);

            var onPrepareMethod = typeof(AnimatorTrack).GetMethod("OnPrepare", BindingFlags.Instance | BindingFlags.NonPublic);
            if (onPrepareMethod != null)
            {
                onPrepareMethod.Invoke(track, null);
            }
        }

        [Test]
        public void Test_AnimatorClip_ParameterLogic()
        {
            var clip = new AnimatorClip
            {
                Start = 0f,
                Duration = 10f
            };

            // Add a float parameter key
            var floatKey = new AnimatorParameterKey
            {
                parameterName = "Speed",
                parameterType = AnimatorControllerParameterType.Float,
                startFloatValue = 0f,
                endFloatValue = 10f,
                curveType = AnimatorParameterCurve.Linear
            };
            clip.AddParameterKey(floatKey);

            // Check value at start
            Assert.AreEqual(0f, (float)clip.GetParameterValueAtTime("Speed", 0f), 0.001f);

            // Check value at mid
            Assert.AreEqual(5f, (float)clip.GetParameterValueAtTime("Speed", 5f), 0.001f);

            // Check value at end
            Assert.AreEqual(10f, (float)clip.GetParameterValueAtTime("Speed", 10f), 0.001f);
        }

        [Test]
        public void Test_AnimatorClip_RemoveParameterKey()
        {
            var clip = new AnimatorClip();
            var key = new AnimatorParameterKey { parameterName = "Test" };
            clip.AddParameterKey(key);

            Assert.AreEqual(1, clip.parameterKeys.Count);

            bool removed = clip.RemoveParameterKey("Test");
            Assert.IsTrue(removed);
            Assert.AreEqual(0, clip.parameterKeys.Count);
        }

        [Test]
        public void Test_AnimatorTrack_CreateClip()
        {
            var createdClip = track.CreateClip("testClip", 0f, 5f);

            Assert.IsNotNull(createdClip);
            Assert.AreEqual("testClip", createdClip.Id);
            Assert.AreEqual(0f, createdClip.Start);
            Assert.AreEqual(5f, createdClip.Duration);

            Assert.Contains(createdClip, track.GetAnimatorClips());
        }
    }
}
