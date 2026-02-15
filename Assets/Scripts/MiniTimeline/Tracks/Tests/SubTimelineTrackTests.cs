using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Core;
using Systems.MiniTimeline.Tracks;

namespace Systems.MiniTimeline.Tracks.Tests
{
    public class SubTimelineTrackTests
    {
        private GameObject trackGO;
        private SubTimelineTrack track;

        [SetUp]
        public void Setup()
        {
            trackGO = new GameObject("SubTimelineTrackTest");
            track = new SubTimelineTrack();
            track.Id = "SubTimelineTrack";
            track.BindKey = "";

            // Mock target object for parenting
            track.Bind(null); // Unbound is fine for SubTimeline

            // We need a dummy project to load.
            // Create a dummy project file?
            // TestRunner environment might not allow creating files easily or polluting disk.
            // We can mock LoadProject?
            // SubTimelineTrack uses `new MiniTimelineDirector()` and calls `LoadProject`.
            // We can't mock MiniTimelineDirector easily as it's a MonoBehaviour.
            // However, we can create a file if allowed.
            // Application.persistentDataPath works.

            string path = System.IO.Path.Combine(Application.persistentDataPath, "TimelineProjects");
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

            var dummyProject = new MiniTimelineProject { name = "TestProject", length = 5f };
            string filePath = System.IO.Path.Combine(path, "TestProject.json");
            System.IO.File.WriteAllText(filePath, JsonUtility.ToJson(dummyProject));
        }

        [TearDown]
        public void Teardown()
        {
            track.OnProjectClosed();
            if (trackGO != null) GameObject.DestroyImmediate(trackGO);

            // Cleanup project file
            string path = System.IO.Path.Combine(Application.persistentDataPath, "TimelineProjects", "TestProject.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        [Test]
        public void SubTimelineTrack_Prepare_LoadsProject()
        {
            var clip = new SubTimelineClip
            {
                Id = "SubClip",
                Start = 0f,
                Duration = 2f,
                projectPath = "TestProject",
                speed = 1f
            };

            track.AddClip(clip);
            track.Prepare();

            // Verify director created
            // We can check transform children of trackGO?
            // SubTimelineTrack creates "SubTimelines_Id" GameObject.
            // But track.targetObject is null (we passed null to Bind).
            // So it creates at root?

            // trackGO is where?
            // We can find by name?
            var container = GameObject.Find($"SubTimelines_{track.Id}");
            Assert.IsNotNull(container);

            var directorGO = container.transform.Find($"Director_{clip.Id}");
            Assert.IsNotNull(directorGO);

            var director = directorGO.GetComponent<MiniTimelineDirector>();
            Assert.IsNotNull(director);
            Assert.AreEqual("TestProject", director.Project.name);
        }

        [Test]
        public void SubTimelineTrack_Evaluate_AdvancesTime()
        {
            var clip = new SubTimelineClip
            {
                Id = "SubClip",
                Start = 0f,
                Duration = 2f,
                projectPath = "TestProject",
                speed = 1f
            };

            track.AddClip(clip);
            track.Prepare();

            // Evaluate at 1s
            track.Evaluate(1f, false);

            // Check director time
            var container = GameObject.Find($"SubTimelines_{track.Id}");
            var director = container.transform.Find($"Director_{clip.Id}").GetComponent<MiniTimelineDirector>();

            // Clip starts at 0. Time 1. Local time 1. Speed 1. SubTime 1.
            Assert.AreEqual(1f, director.Time, 0.01f);

            // Check if active
            Assert.IsTrue(director.gameObject.activeSelf);
        }

        [Test]
        public void SubTimelineTrack_Exit_StopsDirector()
        {
            var clip = new SubTimelineClip
            {
                Id = "SubClip",
                Start = 0f,
                Duration = 2f,
                projectPath = "TestProject",
                speed = 1f
            };

            track.AddClip(clip);
            track.Prepare();

            track.Evaluate(1f, false);
            var container = GameObject.Find($"SubTimelines_{track.Id}");
            var director = container.transform.Find($"Director_{clip.Id}").GetComponent<MiniTimelineDirector>();
            Assert.IsTrue(director.gameObject.activeSelf);

            // Evaluate past end (3f)
            track.Evaluate(3f, false);

            // Should be stopped/disabled
            // SubTimelineTrack logic: if clip not active -> StopDirectorCleanly -> SetActive(false)
            Assert.IsFalse(director.gameObject.activeSelf);
            Assert.AreEqual(0f, director.Time, 0.01f); // Stopped resets to 0
        }
    }
}
