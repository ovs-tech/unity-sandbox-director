using UnityEngine;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Base class for all ScriptableObject-based placement strategies.
    /// Provides inspector typing for serialized strategy assets.
    /// </summary>
    public abstract class BasePlacementStrategy : ScriptableObject, IPlacementStrategy
    {
        public abstract Vector3 CalculatePosition(Vector3 rawWorldPos, GameObject ghostObject);
        public abstract Quaternion CalculateRotation(Quaternion currentRotation);
        public abstract void OnDrawGizmos();
    }
}
