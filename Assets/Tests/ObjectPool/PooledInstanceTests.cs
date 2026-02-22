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

        private class MockPool : IPooledInstancePool
        {
            public void Release(GameObject instance) { }
        }
    }
}
