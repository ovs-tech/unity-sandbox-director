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
        GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null);

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
        public string namespaceName;
        public int poolCount;
        public int totalActiveInstances;
        public int totalInactiveInstances;
    }
}
