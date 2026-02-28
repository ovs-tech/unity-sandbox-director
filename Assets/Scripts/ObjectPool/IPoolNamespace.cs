using UnityEngine;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Represents a namespace/category of object pools
    /// Allows grouping and managing related pools together
    /// </summary>
    public interface IPoolNamespace
    {
        /// <summary>
        /// Spawn an instance from this namespace
        /// </summary>
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>
        /// Release an instance back to its pool in this namespace
        /// </summary>
        void Release(GameObject instance);

        /// <summary>
        /// Clear all pools in this namespace
        /// </summary>
        void Clear();

        /// <summary>
        /// Get statistics for this namespace
        /// </summary>
        PoolNamespaceStats GetStats();
    }

    /// <summary>
    /// Statistics for a pool namespace
    /// </summary>
    public struct PoolNamespaceStats
    {
        /// <summary>Name of the namespace</summary>
        public string NamespaceName;
        
        /// <summary>Number of distinct prefab pools in this namespace</summary>
        public int PoolCount;
        
        /// <summary>Total number of active (spawned) instances across all pools</summary>
        public int TotalActiveInstances;
        
        /// <summary>Total number of inactive (pooled) instances across all pools</summary>
        public int TotalInactiveInstances;
    }
}
