using NUnit.Framework;
using UnityEngine;
using Systems.ObjectPool;
using System.Collections.Generic;

namespace Tests.ObjectPool
{
    [TestFixture]
    public class PoolNamespaceTests
    {
        private PoolNamespace _poolNamespace;
        private GameObject _testPrefab;
        private List<GameObject> _prefabsToCleanup;

        [SetUp]
        public void SetUp()
        {
            _poolNamespace = new PoolNamespace("TestNamespace");
            _prefabsToCleanup = new List<GameObject>();
            
            // Create a simple test prefab
            _testPrefab = new GameObject("TestPrefab");
            _prefabsToCleanup.Add(_testPrefab);
        }

        [TearDown]
        public void TearDown()
        {
            _poolNamespace?.Clear();
            
            // Clean up all prefabs created during test
            foreach (var prefab in _prefabsToCleanup)
            {
                if (prefab != null)
                {
                    Object.DestroyImmediate(prefab);
                }
            }
            _prefabsToCleanup.Clear();
        }

        [Test]
        public void Spawn_CreatesNewInstance()
        {
            // Act
            GameObject instance = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);

            // Assert
            Assert.IsNotNull(instance, "Spawned instance should not be null");
            Assert.AreNotEqual(_testPrefab, instance, "Spawned instance should be a clone, not the prefab itself");
            Assert.IsTrue(instance.activeInHierarchy, "Spawned instance should be active");
        }

        [Test]
        public void Spawn_AttachesPooledInstanceComponent()
        {
            // Act
            GameObject instance = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);

            // Assert
            PooledInstance pooledInstance = instance.GetComponent<PooledInstance>();
            Assert.IsNotNull(pooledInstance, "Spawned instance should have PooledInstance component");
            Assert.IsNotNull(pooledInstance.Pool, "PooledInstance should have pool reference assigned");
        }

        [Test]
        public void Spawn_SetsPositionAndRotation()
        {
            // Arrange
            Vector3 expectedPosition = new Vector3(10f, 20f, 30f);
            Quaternion expectedRotation = Quaternion.Euler(45f, 90f, 135f);

            // Act
            GameObject instance = _poolNamespace.Spawn(_testPrefab, expectedPosition, expectedRotation, null);

            // Assert
            Assert.AreEqual(expectedPosition, instance.transform.position, "Instance should have correct position");
            Assert.AreEqual(expectedRotation, instance.transform.rotation, "Instance should have correct rotation");
        }

        [Test]
        public void Spawn_SetsParent()
        {
            // Arrange
            GameObject parentObject = new GameObject("Parent");
            _prefabsToCleanup.Add(parentObject);
            Transform parent = parentObject.transform;

            // Act
            GameObject instance = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, parent);

            // Assert
            Assert.AreEqual(parent, instance.transform.parent, "Instance should have correct parent");
        }

        [Test]
        public void Release_DeactivatesInstance()
        {
            // Arrange
            GameObject instance = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            Assert.IsTrue(instance.activeInHierarchy, "Instance should be active after spawn");

            // Act
            _poolNamespace.Release(instance);

            // Assert
            Assert.IsFalse(instance.activeInHierarchy, "Instance should be inactive after release");
        }

        [Test]
        public void Spawn_ReusesPreviouslyReleasedInstance()
        {
            // Arrange
            GameObject firstInstance = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            int firstInstanceId = firstInstance.GetInstanceID();
            
            _poolNamespace.Release(firstInstance);

            // Act
            GameObject secondInstance = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            int secondInstanceId = secondInstance.GetInstanceID();

            // Assert
            Assert.AreEqual(firstInstanceId, secondInstanceId, "Pool should reuse the same instance");
            Assert.IsTrue(secondInstance.activeInHierarchy, "Reused instance should be active");
        }

        [Test]
        public void Clear_DestroysAllInstances()
        {
            // Arrange
            GameObject instance1 = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            GameObject instance2 = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            _poolNamespace.Release(instance1);

            // Act
            _poolNamespace.Clear();

            // Assert
            // After clear, instances should be destroyed (this is Unity-specific behavior)
            // We'll verify by checking stats instead
            PoolNamespaceStats stats = _poolNamespace.GetStats();
            Assert.AreEqual(0, stats.PoolCount, "PoolCount should be 0 after clear");
            Assert.AreEqual(0, stats.TotalActiveInstances, "TotalActiveInstances should be 0 after clear");
            Assert.AreEqual(0, stats.TotalInactiveInstances, "TotalInactiveInstances should be 0 after clear");
            
            // Verify instances are actually destroyed
            Assert.IsTrue(instance1 == null, "Instance1 should be destroyed after Clear");
            Assert.IsTrue(instance2 == null, "Instance2 should be destroyed after Clear");
        }

        [Test]
        public void GetStats_ReturnsCorrectCounts()
        {
            // Arrange
            GameObject instance1 = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            GameObject instance2 = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            _poolNamespace.Release(instance1);

            // Act
            PoolNamespaceStats stats = _poolNamespace.GetStats();

            // Assert
            Assert.AreEqual("TestNamespace", stats.NamespaceName, "NamespaceName should match");
            Assert.AreEqual(1, stats.PoolCount, "Should have 1 pool for the test prefab");
            Assert.AreEqual(1, stats.TotalActiveInstances, "Should have 1 active instance");
            Assert.AreEqual(1, stats.TotalInactiveInstances, "Should have 1 inactive instance in pool");
        }

        [Test]
        public void GetStats_WithMultiplePrefabs_ReturnsCorrectCounts()
        {
            // Arrange
            GameObject prefab2 = new GameObject("TestPrefab2");
            _prefabsToCleanup.Add(prefab2);
            
            GameObject instance1 = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            GameObject instance2 = _poolNamespace.Spawn(prefab2, Vector3.zero, Quaternion.identity, null);
            GameObject instance3 = _poolNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            
            _poolNamespace.Release(instance1);

            // Act
            PoolNamespaceStats stats = _poolNamespace.GetStats();

            // Assert
            Assert.AreEqual(2, stats.PoolCount, "Should have 2 pools for 2 different prefabs");
            Assert.AreEqual(2, stats.TotalActiveInstances, "Should have 2 active instances");
            Assert.AreEqual(1, stats.TotalInactiveInstances, "Should have 1 inactive instance");
        }

        [Test]
        public void Release_WithNullInstance_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _poolNamespace.Release(null), 
                "Releasing null should not throw");
        }

        [Test]
        public void Release_WithUnknownInstance_HandlesGracefully()
        {
            // Arrange
            GameObject unknownInstance = new GameObject("Unknown");
            _prefabsToCleanup.Add(unknownInstance);
            
            // Act & Assert
            Assert.DoesNotThrow(() => _poolNamespace.Release(unknownInstance), 
                "Releasing unknown instance should not throw");
        }

        [Test]
        public void Spawn_WithNullPrefab_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                _poolNamespace.Spawn(null, Vector3.zero, Quaternion.identity, null),
                "Spawning null prefab should throw ArgumentNullException");
        }
    }
}
