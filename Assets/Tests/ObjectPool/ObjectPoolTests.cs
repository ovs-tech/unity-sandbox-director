using NUnit.Framework;
using UnityEngine;
using Systems.ObjectPool;
using System.Collections.Generic;
using System.Linq;

namespace Tests.ObjectPool
{
    [TestFixture]
    public class ObjectPoolTests
    {
        private GameObject _testPrefab;
        private List<GameObject> _prefabsToCleanup;

        [SetUp]
        public void SetUp()
        {
            _prefabsToCleanup = new List<GameObject>();
            
            // Create a simple test prefab with PooledInstance component
            _testPrefab = new GameObject("TestPrefab");
            _testPrefab.AddComponent<PooledInstance>();
            _prefabsToCleanup.Add(_testPrefab);
            
            // Clear all namespaces before each test
            Systems.ObjectPool.ObjectPool.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up all namespaces
            Systems.ObjectPool.ObjectPool.ClearAll();
            
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
        public void Spawn_WithDefaultNamespace_CreatesInstance()
        {
            // Act
            GameObject instance = Systems.ObjectPool.ObjectPool.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);

            // Assert
            Assert.IsNotNull(instance, "Spawned instance should not be null");
            Assert.AreNotEqual(_testPrefab, instance, "Spawned instance should be a clone, not the prefab itself");
            Assert.IsTrue(instance.activeSelf, "Spawned instance should be active");
            
            // Verify PooledInstance is attached
            PooledInstance pooledInstance = instance.GetComponent<PooledInstance>();
            Assert.IsNotNull(pooledInstance, "PooledInstance component should be attached");
        }

        [Test]
        public void Spawn_WithCustomNamespace_CreatesInstanceInNamespace()
        {
            // Act
            GameObject instance = Systems.ObjectPool.ObjectPool.Namespace("custom").Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);

            // Assert
            Assert.IsNotNull(instance, "Spawned instance should not be null");
            Assert.IsTrue(instance.activeSelf, "Spawned instance should be active");
        }

        [Test]
        public void Release_ReturnsInstanceToPool()
        {
            // Arrange
            GameObject instance = Systems.ObjectPool.ObjectPool.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            
            // Act
            Systems.ObjectPool.ObjectPool.Release(instance);

            // Assert
            Assert.IsFalse(instance.activeSelf, "Released instance should be inactive");
        }

        [Test]
        public void Release_WithNullInstance_HandlesGracefully()
        {
            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => Systems.ObjectPool.ObjectPool.Release(null));
        }

        [Test]
        public void Release_CallsCorrectPoolNamespace()
        {
            // Arrange
            IPoolNamespace customNamespace = Systems.ObjectPool.ObjectPool.Namespace("custom");
            GameObject instance = customNamespace.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            var stats = customNamespace.GetStats();
            Assert.AreEqual(1, stats.TotalActiveInstances, "Should have 1 active instance after spawn");

            // Act
            Systems.ObjectPool.ObjectPool.Release(instance);

            // Assert
            stats = customNamespace.GetStats();
            Assert.AreEqual(0, stats.TotalActiveInstances, "Should have 0 active instances after release");
            Assert.AreEqual(1, stats.TotalInactiveInstances, "Should have 1 inactive instance after release");
        }

        [Test]
        public void Namespace_ReturnsNamedNamespace()
        {
            // Act
            IPoolNamespace namespace1 = Systems.ObjectPool.ObjectPool.Namespace("test_namespace");

            // Assert
            Assert.IsNotNull(namespace1, "Namespace should not be null");
        }

        [Test]
        public void Namespace_ReturnsSameInstanceForSameName()
        {
            // Act
            IPoolNamespace namespace1 = Systems.ObjectPool.ObjectPool.Namespace("test_namespace");
            IPoolNamespace namespace2 = Systems.ObjectPool.ObjectPool.Namespace("test_namespace");

            // Assert
            Assert.AreSame(namespace1, namespace2, "Same namespace name should return same instance");
        }

        [Test]
        public void Namespace_ReturnsDifferentInstancesForDifferentNames()
        {
            // Act
            IPoolNamespace namespace1 = Systems.ObjectPool.ObjectPool.Namespace("namespace1");
            IPoolNamespace namespace2 = Systems.ObjectPool.ObjectPool.Namespace("namespace2");

            // Assert
            Assert.AreNotSame(namespace1, namespace2, "Different namespace names should return different instances");
        }

        [Test]
        public void ClearNamespace_ClearsSpecificNamespace()
        {
            // Arrange
            IPoolNamespace namespace1 = Systems.ObjectPool.ObjectPool.Namespace("namespace1");
            IPoolNamespace namespace2 = Systems.ObjectPool.ObjectPool.Namespace("namespace2");
            
            GameObject instance1 = namespace1.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            GameObject instance2 = namespace2.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            
            var stats1 = namespace1.GetStats();
            var stats2 = namespace2.GetStats();
            Assert.AreEqual(1, stats1.TotalActiveInstances);
            Assert.AreEqual(1, stats2.TotalActiveInstances);

            // Act
            Systems.ObjectPool.ObjectPool.ClearNamespace("namespace1");

            // Assert
            stats1 = namespace1.GetStats();
            stats2 = namespace2.GetStats();
            Assert.AreEqual(0, stats1.PoolCount, "namespace1 should be cleared");
            Assert.AreEqual(1, stats2.TotalActiveInstances, "namespace2 should still have active instances");
        }

        [Test]
        public void ClearAll_ClearsAllNamespaces()
        {
            // Arrange
            IPoolNamespace namespace1 = Systems.ObjectPool.ObjectPool.Namespace("namespace1");
            IPoolNamespace namespace2 = Systems.ObjectPool.ObjectPool.Namespace("namespace2");
            IPoolNamespace namespace3 = Systems.ObjectPool.ObjectPool.Namespace("namespace3");
            
            GameObject instance1 = namespace1.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            GameObject instance2 = namespace2.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            GameObject instance3 = namespace3.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);

            // Act
            Systems.ObjectPool.ObjectPool.ClearAll();

            // Assert
            // After ClearAll, stats should show 0 pools/instances
            var stats1 = namespace1.GetStats();
            var stats2 = namespace2.GetStats();
            var stats3 = namespace3.GetStats();
            
            Assert.AreEqual(0, stats1.PoolCount, "namespace1 should be cleared");
            Assert.AreEqual(0, stats2.PoolCount, "namespace2 should be cleared");
            Assert.AreEqual(0, stats3.PoolCount, "namespace3 should be cleared");
        }

        [Test]
        public void GetAllNamespaceStats_ReturnsAllNamespaces()
        {
            // Arrange
            IPoolNamespace namespace1 = Systems.ObjectPool.ObjectPool.Namespace("namespace1");
            IPoolNamespace namespace2 = Systems.ObjectPool.ObjectPool.Namespace("namespace2");
            
            GameObject instance1 = namespace1.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            GameObject instance2 = namespace2.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);

            // Act
            PoolNamespaceStats[] allStats = Systems.ObjectPool.ObjectPool.GetAllNamespaceStats();

            // Assert
            Assert.IsNotNull(allStats, "Stats array should not be null");
            Assert.NotZero(allStats.Length, "Stats array should contain at least one namespace");
            
            // Should contain namespace1 and namespace2
            Assert.IsTrue(allStats.Any(s => s.NamespaceName == "namespace1"), "Should contain namespace1");
            Assert.IsTrue(allStats.Any(s => s.NamespaceName == "namespace2"), "Should contain namespace2");
            
            var stats1 = allStats.First(s => s.NamespaceName == "namespace1");
            var stats2 = allStats.First(s => s.NamespaceName == "namespace2");
            
            Assert.AreEqual(1, stats1.TotalActiveInstances);
            Assert.AreEqual(1, stats2.TotalActiveInstances);
        }

        [Test]
        public void GetAllNamespaceStats_IncludesDefaultNamespace()
        {
            // Arrange
            GameObject instance = Systems.ObjectPool.ObjectPool.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);

            // Act
            PoolNamespaceStats[] allStats = Systems.ObjectPool.ObjectPool.GetAllNamespaceStats();

            // Assert
            Assert.IsTrue(allStats.Any(s => s.NamespaceName == "default"), "Should contain default namespace");
        }

        [Test]
        public void Spawn_MultipleInstances_ReusesPooledObjects()
        {
            // Arrange: Spawn an instance and release it
            GameObject instance1 = Systems.ObjectPool.ObjectPool.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            Systems.ObjectPool.ObjectPool.Release(instance1);

            // Act: Spawn another instance
            GameObject instance2 = Systems.ObjectPool.ObjectPool.Spawn(_testPrefab, Vector3.one, Quaternion.identity, null);

            // Assert: Should reuse the pooled instance
            Assert.AreSame(instance1, instance2, "Should reuse pooled instance");
            Assert.IsTrue(instance2.activeSelf, "Reused instance should be active");
            Assert.AreEqual(Vector3.one, instance2.transform.position, "Position should be updated");
        }

        [Test]
        public void Spawn_WithPositionAndRotation_SetsTransform()
        {
            // Arrange
            Vector3 testPosition = new Vector3(5f, 10f, 15f);
            Quaternion testRotation = Quaternion.Euler(45f, 90f, 135f);

            // Act
            GameObject instance = Systems.ObjectPool.ObjectPool.Spawn(_testPrefab, testPosition, testRotation, null);

            // Assert
            Assert.AreEqual(testPosition, instance.transform.position);
            Assert.AreEqual(testRotation, instance.transform.rotation);
        }

        [Test]
        public void Spawn_WithParent_SetsParentTransform()
        {
            // Arrange
            GameObject parentObject = new GameObject("Parent");
            _prefabsToCleanup.Add(parentObject);
            Transform parent = parentObject.transform;

            // Act
            GameObject instance = Systems.ObjectPool.ObjectPool.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, parent);

            // Assert
            Assert.AreEqual(parent, instance.transform.parent, "Instance should be parented to the specified transform");
        }

        [Test]
        public void Release_WithPooledInstance_ProxiesCorrectly()
        {
            // Arrange
            GameObject instance = Systems.ObjectPool.ObjectPool.Spawn(_testPrefab, Vector3.zero, Quaternion.identity, null);
            PooledInstance pooledComponent = instance.GetComponent<PooledInstance>();
            Assert.IsNotNull(pooledComponent, "PooledInstance should exist");

            // Act
            pooledComponent.ReleaseToPool();

            // Assert
            Assert.IsFalse(instance.activeSelf, "Instance should be inactive after ReleaseToPool");
        }
    }
}
