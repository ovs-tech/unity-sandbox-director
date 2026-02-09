using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Tracks;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class SignalTrackTests
    {
        private SignalTrack track;
        private List<TimelineEvent> firedEvents;

        [SetUp]
        public void Setup()
        {
            track = new SignalTrack();
            firedEvents = new List<TimelineEvent>();
        }

        [Test]
        public void Test_AddSignal_AddsClip()
        {
            var clip = track.AddSignal(5f, "TestEvent", "Payload");

            Assert.AreEqual(1, GetClipCount(track));
            Assert.AreEqual(5f, clip.Start);
            Assert.AreEqual("TestEvent", clip.eventId);
            Assert.AreEqual("Payload", clip.payload);
        }

        [Test]
        public void Test_RegisterEventHandler_FiresEvent()
        {
            track.RegisterEventHandler("TestEvent", OnEvent);

            track.FireManualEvent("TestEvent", "Payload", 5f);

            Assert.AreEqual(1, firedEvents.Count);
            Assert.AreEqual("TestEvent", firedEvents[0].eventId);
            Assert.AreEqual("Payload", firedEvents[0].payload);
        }

        [Test]
        public void Test_ShouldTrigger_Logic()
        {
            var clip = new SignalClip { Start = 5f, edge = EventTriggerEdge.OnEnter };

            // Forward pass crossing 5
            Assert.IsTrue(clip.ShouldTrigger(4.9f, 5.0f, false));
            Assert.IsTrue(clip.ShouldTrigger(4.9f, 5.1f, false));

            // Backward pass (OnEnter only triggers forward by default?)
            // edge = OnEnter: crossedForward = prev < Start && curr >= Start
            Assert.IsFalse(clip.ShouldTrigger(5.1f, 4.9f, false));

            // Not crossing
            Assert.IsFalse(clip.ShouldTrigger(4.0f, 4.5f, false));
        }

        private void OnEvent(TimelineEvent evt)
        {
            firedEvents.Add(evt);
        }

        private int GetClipCount(SignalTrack track)
        {
            int count = 0;
            foreach (var clip in track.GetClips()) count++;
            return count;
        }
    }
}
