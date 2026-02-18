using UnityEngine;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Interface for validating placement locations.
    /// Separates validation logic from placement logic, following Single Responsibility Principle.
    /// </summary>
    public interface IPlacementValidator
    {
        /// <summary>
        /// Validates if an object can be placed at the specified position and rotation.
        /// </summary>
        /// <param name="position">The world position to validate</param>
        /// <param name="rotation">The rotation to validate</param>
        /// <param name="ghostObject">The ghost object being validated</param>
        /// <returns>True if placement is valid, false otherwise</returns>
        bool IsPlacementValid(Vector3 position, Quaternion rotation, GameObject ghostObject);
    }
}
