using UnityEngine;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Defines how an object is placed in the scene (e.g. alignment, snapping, etc.)
    /// </summary>
    public interface IPlacementStrategy
    {
        bool GetPlacement(Ray ray, ToolContext context, out Vector3 position, out Quaternion rotation);
    }
}
