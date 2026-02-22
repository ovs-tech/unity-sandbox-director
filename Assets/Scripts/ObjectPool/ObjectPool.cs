using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Static facade for object pooling with namespace support
    /// Provides zero-allocation, easy-to-use pooling for GameObjects
    /// </summary>
    /// <remarks>This class is NOT thread-safe. All methods must be called from the main thread.</remarks>
    public static class ObjectPool
    {
        public const string DEFAULT_NAMESPACE = "default";
        private static readonly Dictionary<string, PoolNamespace> _namespaces = new();

        /// <summary>
        /// Spawn an instance from the default namespace
        /// Automatically detects and uses PooledInstance component
        /// </summary>
        /// <param name="prefab">The prefab to spawn from</param>
        /// <param name="position">World position for the spawned instance</param>
        /// <param name="rotation">World rotation for the spawned instance</param>
        /// <param name="parent">Optional parent transform for the spawned instance</param>
        /// <returns>The spawned GameObject instance</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if prefab is null</exception>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            IPoolNamespace defaultNamespace = Namespace(DEFAULT_NAMESPACE);
            return defaultNamespace.Spawn(prefab, position, rotation, parent);
        }

        /// <summary>
        /// Release an instance back to its pool
        /// Automatically detects the correct pool via PooledInstance component
        /// </summary>
        /// <param name="instance">The instance to release</param>
        public static void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            PooledInstance pooledInstance = instance.GetComponent<PooledInstance>();
            if (pooledInstance != null && pooledInstance.Pool != null)
            {
                pooledInstance.Pool.Release(instance);
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Instance {instance.name} has no valid PooledInstance component or pool assigned.");
            }
        }

        /// <summary>
        /// Get or create a named namespace
        /// Multiple calls with the same name return the same namespace instance
        /// </summary>
        /// <param name="namespaceName">The name of the namespace</param>
        /// <returns>The requested namespace</returns>
        public static IPoolNamespace Namespace(string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName))
                throw new System.ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));
            
            if (!_namespaces.TryGetValue(namespaceName, out var poolNamespace))
            {
                poolNamespace = new PoolNamespace(namespaceName);
                _namespaces[namespaceName] = poolNamespace;
            }

            return poolNamespace;
        }

        /// <summary>
        /// Clear all instances in a specific namespace
        /// </summary>
        /// <param name="namespaceName">The name of the namespace to clear</param>
        public static void ClearNamespace(string namespaceName)
        {
            if (_namespaces.TryGetValue(namespaceName, out var poolNamespace))
            {
                poolNamespace.Clear();
                _namespaces.Remove(namespaceName);
            }
        }

        /// <summary>
        /// Clear all instances across all namespaces
        /// </summary>
        public static void ClearAll()
        {
            foreach (var poolNamespace in _namespaces.Values)
            {
                poolNamespace.Clear();
            }

            _namespaces.Clear();
        }

        /// <summary>
        /// Get statistics for all namespaces
        /// </summary>
        /// <returns>Array of PoolNamespaceStats for all namespaces</returns>
        public static PoolNamespaceStats[] GetAllNamespaceStats()
        {
            var statsList = new List<PoolNamespaceStats>();

            foreach (var poolNamespace in _namespaces.Values)
            {
                statsList.Add(poolNamespace.GetStats());
            }

            return statsList.ToArray();
        }

        /// <summary>
        /// Get the names of all namespaces (editor debugging only)
        /// </summary>
        /// <returns>Array of namespace names</returns>
        public static string[] GetNamespaceNames()
        {
            return _namespaces.Keys.ToArray();
        }
    }
}
