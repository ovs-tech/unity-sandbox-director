using UnityEngine;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Strategy pattern interface for different placement behaviors (free, grid, hex, surface, etc.).
    /// Allows runtime switching of placement modes without changing the PlacementController.
    /// </summary>
    public interface IPlacementStrategy
    {
        /// <summary>
        /// Calculates the final placement position based on the raw world position from raycast.
        /// </summary>
        /// <param name="rawWorldPos">The unprocessed world position from raycast</param>
        /// <param name="ghostObject">The ghost preview object being placed</param>
        /// <returns>The processed position (e.g., snapped to grid)</returns>
        Vector3 CalculatePosition(Vector3 rawWorldPos, GameObject ghostObject);

        /// <summary>
        /// Calculates the rotation for the object being placed.
        /// </summary>
        /// <param name="currentRotation">The current rotation of the ghost object</param>
        /// <returns>The calculated rotation (can be modified by user input or surface normal)</returns>
        Quaternion CalculateRotation(Quaternion currentRotation);
    }
}
