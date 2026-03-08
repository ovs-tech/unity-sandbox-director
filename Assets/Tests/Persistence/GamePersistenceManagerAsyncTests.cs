using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Systems.Persistence;
using Systems.Persistence.Core;
using Systems.Persistence.Services;
using UnityEngine;

namespace Systems.Persistence.Tests
{
    public class GamePersistenceManagerAsyncTests
    {
        [Serializable]
        private class FakePayload
        {
            public int value;
        }

        private class FakeSubsystem : ISubsystemPersistence
        {
            public string Namespace => "minitimeline";
            public string PersistentName => "AsyncProject";
            public PersistenceTarget Target => PersistenceTarget.External;
            public object Data { get; set; } = new FakePayload { value = 7 };
            public object GetSaveData() => Data;
            public void LoadData(object data) => Data = data;
            public Type DataType => typeof(FakePayload);
        }

        [Test]
        public async Task SaveFileAsync_ThenLoadFileAsync_RoundTripsSubsystemData()
        {
            var managerGo = new GameObject("GamePersistenceManagerAsyncTests");
            var manager = managerGo.AddComponent<GamePersistenceManager>();
            var service = ScriptableObject.CreateInstance<FileDataService>();
            service.Setup();

            var saveName = "AsyncSave_GamePersistenceManagerAsyncTests";
            var subsystem = new FakeSubsystem();

            var dataServiceField = typeof(GamePersistenceManager).GetField("dataService", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(dataServiceField, "Expected private dataService field not found");
            dataServiceField.SetValue(manager, service);

            try
            {
                await manager.SaveFileAsync(subsystem, saveName, subsystem.PersistentName, true, CancellationToken.None);
                var loaded = await manager.LoadFileAsync<FakePayload>(subsystem, saveName, subsystem.PersistentName, CancellationToken.None);

                Assert.IsNotNull(loaded);
                Assert.AreEqual(7, loaded.value);
            }
            finally
            {
                service.Delete(saveName);
                UnityEngine.Object.DestroyImmediate(service);
                UnityEngine.Object.DestroyImmediate(managerGo);
            }
        }
    }
}
