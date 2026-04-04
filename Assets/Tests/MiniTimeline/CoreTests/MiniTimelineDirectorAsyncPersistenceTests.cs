using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Systems.MiniTimeline.Core;
using Systems.Persistence;
using Systems.Persistence.Core;
using Systems.Persistence.Services;
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

            var persistenceGo = new GameObject("GamePersistenceManagerTest");
            var persistenceManager = persistenceGo.AddComponent<GamePersistenceManager>();
            var gameData = ScriptableObject.CreateInstance<GameData>();
            var dataService = ScriptableObject.CreateInstance<FileDataService>();
            dataService.Setup();

            persistenceManager.gameData = gameData;
            SetPrivateField(persistenceManager, "dataService", dataService);

            director.CreateNewProject("AsyncProject", 8f, 30f);
            Assert.IsTrue(director.SaveProject("AsyncProject"));

            bool? result = null;
            yield return director.LoadProjectAsync("AsyncProject", success => result = success);

            Assert.AreEqual(true, result);
            Assert.IsNotNull(director.Project);
            Assert.AreEqual("AsyncProject", director.Project.name);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(persistenceGo);
            Object.DestroyImmediate(gameData);
            Object.DestroyImmediate(dataService);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = instance.GetType().GetField(fieldName, flags);
            Assert.IsNotNull(field, $"Expected field '{fieldName}' on type '{instance.GetType().Name}'");
            field.SetValue(instance, value);
        }
    }
}
