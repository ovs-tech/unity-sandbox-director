using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Implementation of a pool namespace using Unity's native ObjectPool
    /// Manages multiple object pools grouped under a single namespace
    /// </summary>
    public class PoolNamespace : IPoolNamespace, IPooledInstancePool
    {
        private readonly string _namespaceName;
        private readonly Dictionary<int, ObjectPool<GameObject>> _pools;
        private readonly Dictionary<int, GameObject> _prefabs;
        private readonly Dictionary<GameObject, int> _instanceToPrefabId;
        private GameObject _poolRoot;

        public PoolNamespace(string namespaceName)
        {
            _namespaceName = namespaceName;
            _pools = new Dictionary<int, ObjectPool<GameObject>>();
            _prefabs = new Dictionary<int, GameObject>();
            _instanceToPrefabId = new Dictionary<GameObject, int>();
        }

        /// <summary>
        /// Get or create the pool root GameObject for organizing inactive instances
        /// </summary>
        private GameObject GetPoolRoot()
        {
            if (_poolRoot == null)
            {
                _poolRoot = new GameObject($"[Pool: {_namespaceName}]");
                _poolRoot.SetActive(false); // Keep inactive to avoid OnEnable/Start calls
            }
            return _poolRoot;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolNamespace] Cannot spawn null prefab");
                throw new System.ArgumentNullException(nameof(prefab));
            }

            int prefabId = prefab.GetInstanceID();
            
            // Get or create pool for this prefab
            if (!_pools.TryGetValue(prefabId, out var pool))
            {
                pool = new ObjectPool<GameObject>(
                    createFunc: () => CreateInstance(prefab, prefabId),
                    actionOnGet: OnGetFromPool,
                    actionOnRelease: OnReleaseToPool,
                    actionOnDestroy: OnDestroyPoolObject,
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 10000
                );
                
                _pools[prefabId] = pool;
                _prefabs[prefabId] = prefab;
            }

            var instance = pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(parent, worldPositionStays: true);
            
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarning("[PoolNamespace] Cannot release null instance");
                return;
            }

            if (!_instanceToPrefabId.TryGetValue(instance, out int prefabId))
            {
                Debug.LogWarning($"[PoolNamespace] Instance {instance.name} is not tracked by this namespace");
                return;
            }

            if (_pools.TryGetValue(prefabId, out var pool))
            {
                pool.Release(instance);
            }
        }

        public void Clear()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            
            _pools.Clear();
            _prefabs.Clear();
            _instanceToPrefabId.Clear();

            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot);
                _poolRoot = null;
            }
        }

        public PoolNamespaceStats GetStats()
        {
            int totalActive = 0;
            int totalInactive = 0;

            foreach (var pool in _pools.Values)
            {
                totalActive += pool.CountActive;
                totalInactive += pool.CountInactive;
            }

            return new PoolNamespaceStats
            {
                NamespaceName = _namespaceName,
                PoolCount = _pools.Count,
                TotalActiveInstances = totalActive,
                TotalInactiveInstances = totalInactive
            };
        }

        /// <summary>
        /// Proxy method implementing IPooledInstancePool.Release for PooledInstance callbacks
        /// </summary>
        void IPooledInstancePool.Release(GameObject instance)
        {
            Release(instance);
        }

        // ============= Pool Lifecycle Callbacks =============

        private GameObject CreateInstance(GameObject prefab, int prefabId)
        {
            var instance = Object.Instantiate(prefab);
            
            // Add PooledInstance component
            var pooledInstance = instance.GetComponent<PooledInstance>();
            if (pooledInstance == null)
            {
                pooledInstance = instance.AddComponent<PooledInstance>();
            }
            pooledInstance.SetPool(this);
            
            // Track instance
            _instanceToPrefabId[instance] = prefabId;
            
            return instance;
        }

        private void OnGetFromPool(GameObject instance)
        {
            instance.SetActive(true);
        }

        private void OnReleaseToPool(GameObject instance)
        {
            instance.SetActive(false);
            instance.transform.SetParent(GetPoolRoot().transform, worldPositionStays: false);
        }

        private void OnDestroyPoolObject(GameObject instance)
        {
            if (_instanceToPrefabId.ContainsKey(instance))
            {
                _instanceToPrefabId.Remove(instance);
            }
            
            Object.Destroy(instance);
        }
    }
}
