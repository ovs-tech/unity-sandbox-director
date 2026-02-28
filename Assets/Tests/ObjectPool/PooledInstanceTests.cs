using NUnit.Framework;
using UnityEngine;
using Systems.ObjectPool;

namespace Tests.ObjectPool
{
    public class PooledInstanceTests
    {
        private GameObject testObject;

        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("PooledTest");
        }

        [TearDown]
        public void Teardown()
        {
            if (testObject != null)
            {
                GameObject.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void PooledInstance_CanBeAttachedToGameObject()
        {
            var pooled = testObject.AddComponent<PooledInstance>();
            Assert.IsNotNull(pooled);
        }

        [Test]
        public void PooledInstance_StoresPoolReference()
        {
            var pooled = testObject.AddComponent<PooledInstance>();
            var mockPool = new MockPool();
            
            pooled.SetPool(mockPool);
            
            Assert.AreEqual(mockPool, pooled.Pool);
        }

        [Test]
        public void ReleaseToPool_WithValidPool_CallsPoolRelease()
        {
            var pooled = testObject.AddComponent<PooledInstance>();
            var mockPool = new MockPool();
            pooled.SetPool(mockPool);
            
            pooled.ReleaseToPool();
            
            Assert.IsTrue(mockPool.WasCalled);
            Assert.AreEqual(testObject, mockPool.ReleasedObject);
        }

        [Test]
        public void ReleaseToPool_WithNullPool_LogsWarning()
        {
            var pooled = testObject.AddComponent<PooledInstance>();
            
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*no pool is assigned.*"));
            pooled.ReleaseToPool();
        }

        private class MockPool : IPooledInstancePool
        {
            public bool WasCalled { get; private set; }
            public GameObject ReleasedObject { get; private set; }
            
            public void Release(GameObject instance)
            {
                WasCalled = true;
                ReleasedObject = instance;
            }
        }
    }
}
