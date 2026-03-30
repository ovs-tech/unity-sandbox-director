using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Core;
using Systems.Persistence;

#if !UNITY_INCLUDE_TESTS
namespace UnityEngine.TestTools
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class UnityTestAttribute : System.Attribute { }
}
#endif

namespace MiniTimeline.Core.Tests
{
    public class MiniTimelineDirectorTests
    {
        private GameObject _gameObject;
        private MiniTimelineDirector _director;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("MiniTimelineDirectorTest");
            _director = _gameObject.AddComponent<MiniTimelineDirector>();
            _gameObject.AddComponent<BindableObjectManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }

            // Ensure singleton-style persistence objects created during a test
            // do not leak into subsequent tests.
            var managers = Object.FindObjectsByType<GamePersistenceManager>(FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                if (manager != null)
                {
                    Object.DestroyImmediate(manager.gameObject);
                }
            }
        }

        [Test]
        public void CreateNewProject_CreatesProjectWithDefaults()
        {
            // Act
            bool result = _director.CreateNewProject("TestProject", 20f, 60f);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(_director.Project);
            Assert.AreEqual("TestProject", _director.Project.name);
            Assert.AreEqual(20f, _director.Project.length);
            Assert.AreEqual(60f, _director.Project.frameRate);
            Assert.AreEqual(20f, _director.Length);
        }

        [Test]
        public void AddTrack_AddsTrackToProject()
        {
            // Arrange
            _director.CreateNewProject("TestProject");
            var track = new MockTrack();

            // Act
            bool result = _director.AddTrack(track);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _director.Tracks.Count);
            Assert.AreEqual(track, _director.GetTrack(track.Id));
        }

        [Test]
        public void RemoveTrack_RemovesTrackFromProject()
        {
            // Arrange
            _director.CreateNewProject("TestProject");
            var track = new MockTrack();
            _director.AddTrack(track);

            // Act
            bool result = _director.RemoveTrack(track.Id);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _director.Tracks.Count);
            Assert.IsNull(_director.GetTrack(track.Id));
        }

        [Test]
        public void AddClip_AddsClipToTrack()
        {
            // Arrange
            _director.CreateNewProject("TestProject");
            var track = new MockTrack();
            _director.AddTrack(track);
            var clip = new MockClip(0f, 5f);

            // Act
            bool result = _director.AddClip(clip, track.Id);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, track.GetClips().Count());
            Assert.AreEqual(clip, _director.GetClip(clip.Id, track.Id));
        }

        [Test]
        public void RemoveClip_RemovesClipFromTrack()
        {
            // Arrange
            _director.CreateNewProject("TestProject");
            var track = new MockTrack();
            _director.AddTrack(track);
            var clip = new MockClip(0f, 5f);
            _director.AddClip(clip, track.Id);

            // Act
            bool result = _director.RemoveClip(clip.Id, track.Id);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, track.GetClips().Count());
        }

        [Test]
        public void UpdateClip_UpdatesClipProperties()
        {
            // Arrange
            _director.CreateNewProject("TestProject");
            var track = new MockTrack();
            _director.AddTrack(track);
            var clip = new MockClip(0f, 5f);
            _director.AddClip(clip, track.Id);

            // Act
            bool result = _director.UpdateClip(clip.Id, track.Id, 2f, 8f);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2f, clip.Start);
            Assert.AreEqual(8f, clip.Duration);
        }

        [Test]
        public void Play_StartsPlayback()
        {
            // Arrange
            _director.CreateNewProject("TestProject");

            // Act
            _director.Play();

            // Assert
            Assert.AreEqual(PlaybackState.Playing, _director.State);
            Assert.IsTrue(_director.IsPlaying);
        }

        [Test]
        public void Pause_PausesPlayback()
        {
            // Arrange
            _director.CreateNewProject("TestProject");
            _director.Play();

            // Act
            _director.Pause();

            // Assert
            Assert.AreEqual(PlaybackState.Paused, _director.State);
            Assert.IsFalse(_director.IsPlaying);
        }

        [Test]
        public void Stop_StopsPlaybackAndResetsTime()
        {
            // Arrange
            _director.CreateNewProject("TestProject");
            _director.Play();
            _director.Seek(5f);

            // Act
            _director.Stop();

            // Assert
            Assert.AreEqual(PlaybackState.Stopped, _director.State);
            Assert.AreEqual(0f, _director.Time);
        }

        [Test]
        public void Update_AdvancesTime_WhenPlaying()
        {
            // Arrange
            _director.CreateNewProject("TestProject");
            _director.Play();
            float initialTime = _director.Time;

            // Simulate one frame by advancing time and invoking the private Evaluate method
            _director.Seek(initialTime + 0.02f);
            var method = typeof(MiniTimelineDirector).GetMethod("Evaluate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_director, new object[] { false });

            // Assert
            Assert.Greater(_director.Time, initialTime);
        }

        [Test]
        public void TimeToFrame_ConvertsCorrectly()
        {
            // Arrange
            _director.CreateNewProject("TestProject", 10f, 30f); // 30 FPS

            // Act
            int frame = _director.TimeToFrame(1.5f);

            // Assert
            Assert.AreEqual(45, frame);
        }

        [Test]
        public void FrameToTime_ConvertsCorrectly()
        {
            // Arrange
            _director.CreateNewProject("TestProject", 10f, 30f); // 30 FPS

            // Act
            float time = _director.FrameToTime(45);

            // Assert
            Assert.AreEqual(1.5f, time);
        }

        [Test]
        public void GetProjectsFolder_DoesNotAutoCreatePersistenceManager_InEditMode()
        {
            // Ensure no manager exists at the start of this assertion.
            var existing = Object.FindObjectsByType<GamePersistenceManager>(FindObjectsSortMode.None);
            foreach (var manager in existing)
            {
                if (manager != null)
                {
                    Object.DestroyImmediate(manager.gameObject);
                }
            }

            string projectsFolder = _director.GetProjectsFolder();
            Assert.IsTrue(string.IsNullOrEmpty(projectsFolder));

            var managersAfter = Object.FindObjectsByType<GamePersistenceManager>(FindObjectsSortMode.None);
            Assert.AreEqual(0, managersAfter.Length,
                "GetProjectsFolder should not auto-create GamePersistenceManager during edit-time calls.");

            LogAssert.NoUnexpectedReceived();
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
