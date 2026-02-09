using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;

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
            // Add controller if possible, but testing parameters might fail if controller doesn't have them.
            // However, SetFloat/GetFloat work even without controller if in specific mode?
            // Actually, Animator.SetFloat throws warning if parameter doesn't exist.
            // But checking values via GetFloat might return default.

            // To properly test, we need an AnimatorController.
            // Since we can't easily create one in code without UnityEditor.Animations (which is Editor only),
            // we will rely on mocking or just testing track logic (clip data).

            track = new AnimatorTrack();
        }

        [TearDown]
        public void Teardown()
        {
            if (targetObject != null) Object.DestroyImmediate(targetObject);
        }

        private void PrepareTrack(AnimatorTrack track, GameObject target)
        {
            var baseType = typeof(MiniTrackBase<AnimatorClip>);

            var targetField = baseType.GetField("targetObject", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (targetField != null) targetField.SetValue(track, target);

            var boundField = baseType.GetField("isBound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (boundField != null) boundField.SetValue(track, true);

            track.Prepare();
        }

        [Test]
        public void Test_CreateClip()
        {
            var clip = track.CreateClip("clip1", 0, 5);
            Assert.AreEqual("clip1", clip.Id);
            Assert.AreEqual(0, clip.Start);
            Assert.AreEqual(5, clip.Duration);
            Assert.AreEqual(1, track.GetAnimatorClips().Count);
        }

        [Test]
        public void Test_ClipParameterValues()
        {
            var clip = new AnimatorClip { Start = 0, Duration = 5 };
            var key = new AnimatorParameterKey
            {
                parameterName = "Speed",
                parameterType = AnimatorControllerParameterType.Float,
                startFloatValue = 0f,
                endFloatValue = 10f,
                curveType = AnimatorParameterCurve.Linear
            };
            clip.AddParameterKey(key);

            // At time 0 (normalized 0) -> 0
            Assert.AreEqual(0f, (float)clip.GetParameterValueAtTime("Speed", 0f), 0.01f);

            // At time 2.5 (normalized 0.5) -> 5
            Assert.AreEqual(5f, (float)clip.GetParameterValueAtTime("Speed", 2.5f), 0.01f);

            // At time 5 (normalized 1) -> 10
            Assert.AreEqual(10f, (float)clip.GetParameterValueAtTime("Speed", 5f), 0.01f);
        }
    }
}
