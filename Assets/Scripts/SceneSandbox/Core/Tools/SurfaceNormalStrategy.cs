using UnityEngine;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Surface Normal placement: Aligns the object's up vector to the surface normal.
    /// Useful for placing objects on walls, slopes, or spheres.
    /// </summary>
    public class SurfaceNormalStrategy : IPlacementStrategy
    {
        private float _maxDistance = 1000f;
        private LayerMask _placementLayers = -1;

        public SurfaceNormalStrategy(float maxDistance, LayerMask layers)
        {
            _maxDistance = maxDistance;
            _placementLayers = layers;
        }

        public bool GetPlacement(Ray ray, ToolContext context, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _placementLayers))
            {
                position = hit.point;
                // Align Up to Normal
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                return true;
            }

            return false;
        }
    }
}
