using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Systems.Persistence.Services;
using UnityEngine;

namespace Systems.Persistence.Tests
{
    public class FileDataServiceAsyncTests
    {
        [Serializable]
        private class Payload
        {
            public int value;
        }

        [Test]
        public async Task SaveAsync_ThenLoadAsync_RoundTripsPayload()
        {
            var service = ScriptableObject.CreateInstance<FileDataService>();
            service.Setup();

            var saveName = "AsyncSave_FileDataServiceAsyncTests";
            var ns = "tests";
            var fileName = "payload";

            try
            {
                var expected = new Payload { value = 42 };

                await service.SaveAsync(expected, saveName, ns, fileName, true, CancellationToken.None);
                var actual = await service.LoadAsync<Payload>(saveName, ns, fileName, CancellationToken.None);

                Assert.IsNotNull(actual);
                Assert.AreEqual(42, actual.value);
            }
            finally
            {
                service.Delete(saveName);
                UnityEngine.Object.DestroyImmediate(service);
            }
        }
    }
}
