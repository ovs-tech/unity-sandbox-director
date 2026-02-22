using UnityEngine;

namespace Systems.ObjectPool
{
    /// <summary>
    /// Interface for pools that can release GameObject instances
    /// </summary>
    public interface IPooledInstancePool
    {
        void Release(GameObject instance);
    }

    /// <summary>
    /// Component automatically attached to pooled GameObjects to track their origin pool
    /// Handles automatic return to pool when disabled or destroyed
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledInstance : MonoBehaviour
    {
        /// <summary>
        /// The pool this instance belongs to
        /// </summary>
        public IPooledInstancePool Pool { get; private set; }

        /// <summary>
        /// Assign this instance to a specific pool
        /// </summary>
        public void SetPool(IPooledInstancePool pool)
        {
            Pool = pool;
        }

        /// <summary>
        /// Release this instance back to its pool
        /// </summary>
        public void ReleaseToPool()
        {
            if (Pool != null)
            {
                Pool.Release(gameObject);
            }
            else
            {
                Debug.LogWarning($"[PooledInstance] Attempted to release {gameObject.name} but no pool is assigned.");
            }
        }
    }
}
