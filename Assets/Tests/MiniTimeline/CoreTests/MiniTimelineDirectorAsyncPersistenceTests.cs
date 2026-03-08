using System.Collections;
using NUnit.Framework;
using Systems.MiniTimeline.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniTimeline.Core.Tests
{
    public class MiniTimelineDirectorAsyncPersistenceTests
    {
        [UnityTest]
        public IEnumerator LoadProjectAsync_InvokesCompletionAndSetsProject()
        {
            var go = new GameObject("MiniTimelineAsyncTest");
            var director = go.AddComponent<MiniTimelineDirector>();
            go.AddComponent<BindableObjectManager>();

            director.CreateNewProject("AsyncProject", 8f, 30f);
            Assert.IsTrue(director.SaveProject("AsyncProject"));

            bool? result = null;
            yield return director.LoadProjectAsync("AsyncProject", success => result = success);

            Assert.AreEqual(true, result);
            Assert.IsNotNull(director.Project);
            Assert.AreEqual("AsyncProject", director.Project.name);

            Object.DestroyImmediate(go);
        }
    }
}
