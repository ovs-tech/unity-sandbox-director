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
    public class SignalTrackTests
    {
        private SignalTrack track;
        private GameObject directorObject;
        private MiniTimelineDirector director;

        [SetUp]
        public void Setup()
        {
            track = new SignalTrack();
            directorObject = new GameObject("Director");
            director = directorObject.AddComponent<MiniTimelineDirector>();
        }

        [TearDown]
        public void Teardown()
        {
            if (directorObject != null)
                Object.DestroyImmediate(directorObject);
        }

        [Test]
        public void Test_SignalClip_ShouldTrigger()
        {
            var clip = new SignalClip { Start = 5f, edge = EventTriggerEdge.OnEnter };

            // Forward crossing: 4 -> 6
            Assert.IsTrue(clip.ShouldTrigger(4f, 6f, false));

            // Backward crossing: 6 -> 4 (Should be false for OnEnter)
            Assert.IsFalse(clip.ShouldTrigger(6f, 4f, false));

            // No crossing: 2 -> 4
            Assert.IsFalse(clip.ShouldTrigger(2f, 4f, false));

            clip.edge = EventTriggerEdge.OnExit;
            // Forward crossing: 4 -> 6 (False for OnExit)
            Assert.IsFalse(clip.ShouldTrigger(4f, 6f, false));
            // Backward crossing: 6 -> 4 (True for OnExit)
            Assert.IsTrue(clip.ShouldTrigger(6f, 4f, false));

            clip.edge = EventTriggerEdge.Both;
            Assert.IsTrue(clip.ShouldTrigger(4f, 6f, false));
            Assert.IsTrue(clip.ShouldTrigger(6f, 4f, false));
        }

        [Test]
        public void Test_SignalTrack_AddSignal()
        {
            var signal = track.AddSignal(5f, "TestEvent", "Payload");

            Assert.IsNotNull(signal);
            Assert.AreEqual(5f, signal.Start);
            Assert.AreEqual("TestEvent", signal.eventId);
            Assert.AreEqual("Payload", signal.payload);

            var clips = track.GetClips().ToList();
            Assert.Contains(signal, clips);
        }

        [Test]
        public void Test_SignalTrack_GetSignalsByEventId()
        {
            track.AddSignal(1f, "EventA");
            track.AddSignal(2f, "EventB");
            track.AddSignal(3f, "EventA");

            var eventASignals = track.GetSignalsByEventId("EventA").ToList();
            Assert.AreEqual(2, eventASignals.Count);
            Assert.AreEqual(1f, eventASignals[0].Start);
            Assert.AreEqual(3f, eventASignals[1].Start);
        }

        [Test]
        public void Test_SignalTrack_FireManualEvent()
        {
            bool eventFired = false;
            string receivedEventId = "";

            track.RegisterEventHandler("TestEvent", (evt) =>
            {
                eventFired = true;
                receivedEventId = evt.eventId;
            });

            track.FireManualEvent("TestEvent", "Payload", 10f);

            Assert.IsTrue(eventFired);
            Assert.AreEqual("TestEvent", receivedEventId);
        }
    }
}
