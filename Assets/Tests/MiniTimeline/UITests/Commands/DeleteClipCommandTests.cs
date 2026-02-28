using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.UI.Commands;

namespace Systems.MiniTimeline.UI.Commands.Tests
{
    public class DeleteClipCommandTests
    {
        [Test]
        public void Execute_DeletesClipFromTrack()
        {
            // Arrange
            var track = new MockTrack();
            var clip = new MockClip(0f, 5f);
            track.AddClip(clip);

            var command = new DeleteClipCommand(track, clip);

            // Act
            command.Execute();

            // Assert
            Assert.AreEqual(0, track.Clips.Count);
            Assert.IsFalse(track.Clips.Contains(clip));
        }

        [Test]
        public void Undo_RestoresClipToTrack()
        {
            // Arrange
            var track = new MockTrack();
            var clip = new MockClip(0f, 5f);
            track.AddClip(clip);

            var command = new DeleteClipCommand(track, clip);
            command.Execute(); // Delete it first

            // Act
            command.Undo();

            // Assert
            Assert.AreEqual(1, track.Clips.Count);
            Assert.IsTrue(track.Clips.Contains(clip));
            Assert.AreEqual(clip.Id, track.Clips[0].Id);
        }

        // Mocks
        private class MockTrack : IMiniTrack
        {
            public string Id { get; set; } = System.Guid.NewGuid().ToString();
            public string Name { get; set; } = "MockTrack";
            public string BindKey { get; set; } = "MockKey";
            public bool Enabled { get; set; } = true;
            public int Order { get; set; } = 0;
            public bool IsBound { get; private set; }
            public bool IsReady { get; private set; }
            public EvaluateMode EvaluateMode { get; set; }

            public List<IMiniClip> Clips { get; } = new List<IMiniClip>();

            public void Bind(BindableObjectManager context)
            {
                IsBound = true;
            }

            public void Prepare()
            {
                IsReady = true;
            }

            public void Evaluate(float time, bool scrub)
            {
            }

            public IEnumerable<IMiniClip> GetClips()
            {
                return Clips;
            }

            public void AddClip(IMiniClip clip)
            {
                Clips.Add(clip);
            }

            public bool RemoveClip(IMiniClip clip)
            {
                return Clips.Remove(clip);
            }

            public void OnProjectClosed()
            {
                IsBound = false;
                IsReady = false;
            }
        }

        private class MockClip : MiniClipBase
        {
            public MockClip(float start, float duration)
            {
                Start = start;
                Duration = duration;
                Id = System.Guid.NewGuid().ToString();
            }
        }
    }
}
