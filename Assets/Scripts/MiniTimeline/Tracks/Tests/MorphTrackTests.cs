using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class MorphTrackTests
    {
        private MorphTrack track;

        [SetUp]
        public void Setup()
        {
            track = new MorphTrack();
        }

        [Test]
        public void Test_MorphKeyClip_GetMorphValue()
        {
            var clip = new MorphKeyClip
            {
                Start = 0f,
                Duration = 10f,
                weight = 1f
            };

            var key = new MorphKey
            {
                id = "Smile",
                startValue = 0f,
                endValue = 100f
            };
            clip.keys.Add(key);

            // At 0s
            Assert.AreEqual(0f, clip.GetMorphValue("Smile", 0f).Value, 0.001f);

            // At 5s
            Assert.AreEqual(50f, clip.GetMorphValue("Smile", 5f).Value, 0.001f);

            // At 10s
            Assert.AreEqual(100f, clip.GetMorphValue("Smile", 10f).Value, 0.001f);
        }

        [Test]
        public void Test_MorphCurveClip_GetMorphValue()
        {
            var clip = new MorphCurveClip
            {
                Start = 0f,
                Duration = 10f,
                weight = 1f
            };

            var channel = new MorphCurveClip.CurveChannel
            {
                id = "Blink",
                multiplier = 100f,
                curve = AnimationCurve.Linear(0f, 0f, 10f, 1f) // 0 to 1 over 10s
            };
            clip.channels.Add(channel);

            // At 0s
            Assert.AreEqual(0f, clip.GetMorphValue("Blink", 0f).Value, 0.001f);

            // At 5s -> curve(5) = 0.5 -> 0.5 * 100 = 50
            Assert.AreEqual(50f, clip.GetMorphValue("Blink", 5f).Value, 0.001f);
        }

        [Test]
        public void Test_MorphTrack_AddClips()
        {
            var keyClip = track.AddKeyClip(0f, 5f, null);
            Assert.IsNotNull(keyClip);

            var curveClip = track.AddCurveClip(5f, 5f, null);
            Assert.IsNotNull(curveClip);

            var clips = new List<IMiniClip>(track.GetClips());
            Assert.AreEqual(2, clips.Count);
            Assert.Contains(keyClip, clips);
            Assert.Contains(curveClip, clips);
        }
    }
}
