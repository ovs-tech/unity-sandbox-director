using UnityEngine;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Defines how an object is placed in the scene (e.g. alignment, snapping, etc.)
    /// </summary>
    public interface IPlacementStrategy
    {
        /// <summary>
        /// Calculate the target position and rotation for the placement.
        /// </summary>
        /// <param name="ray">Ray from camera</param>
        /// <param name="context">Tool context with dependencies</param>
        /// <param name="position">Output position</param>
        /// <param name="rotation">Output rotation</param>
        /// <returns>True if a valid placement position was found</returns>
        bool GetPlacement(Ray ray, ToolContext context, out Vector3 position, out Quaternion rotation);
    }
}
