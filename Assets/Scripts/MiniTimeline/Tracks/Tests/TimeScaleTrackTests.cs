using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.Tracks;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class TimeScaleTrackTests
    {
        private TimeScaleTrack track;
        private float originalTimeScale;

        [SetUp]
        public void Setup()
        {
            originalTimeScale = Time.timeScale;

            track = new TimeScaleTrack();
            track.Id = "TimeScaleTrack";
            track.BindKey = ""; // No binding needed

            track.Prepare();
        }

        [TearDown]
        public void Teardown()
        {
            Time.timeScale = originalTimeScale;
        }

        [Test]
        public void TimeScaleTrack_Evaluate_ModifiesTimeScale()
        {
            var clip = new TimeScaleClip
            {
                Id = "TestClip",
                Start = 0f,
                Duration = 2f,
                timeScale = 0.5f,
                blendCurve = AnimationCurve.Constant(0, 1, 1) // Constant
            };

            track.AddClip(clip);

            // Should modify time scale
            track.Evaluate(1f, false);
            Assert.AreEqual(0.5f, Time.timeScale, 0.001f);
        }

        [Test]
        public void TimeScaleTrack_Exit_RestoresTimeScale()
        {
            var clip = new TimeScaleClip
            {
                Id = "TestClip",
                Start = 0f,
                Duration = 2f,
                timeScale = 0.5f
            };

            track.AddClip(clip);

            // Set scale
            track.Evaluate(1f, false);
            Assert.AreEqual(0.5f, Time.timeScale, 0.001f);

            // Exit active range (e.g. at 3f)
            // MiniTrackBase logic: IsActiveAtTime(3) -> false.
            // But we need to call Evaluate(3).
            // Evaluate handles active check internally.

            // If we manually call Evaluate, it checks IsActiveAtTime.
            // If inactive, it triggers OnExit.

            track.Evaluate(3f, false);

            // Should restore original time scale (which was 1.0f at start)
            Assert.AreEqual(originalTimeScale, Time.timeScale, 0.001f);
        }

        [Test]
        public void TimeScaleTrack_Curve_ModulatesTimeScale()
        {
            var clip = new TimeScaleClip
            {
                Id = "TestClip",
                Start = 0f,
                Duration = 2f,
                timeScale = 2f,
                blendCurve = AnimationCurve.Linear(0, 0, 1, 1) // Linear 0 to 1
            };

            track.AddClip(clip);

            // At 50% (1s), curve value is 0.5. Scale = 2 * 0.5 = 1.0.
            track.Evaluate(1f, false);
            Assert.AreEqual(1.0f, Time.timeScale, 0.001f);

            // At 25% (0.5s), curve value is 0.25. Scale = 2 * 0.25 = 0.5.
            track.Evaluate(0.5f, false);
            Assert.AreEqual(0.5f, Time.timeScale, 0.001f);
        }
    }
}
