using UnityEngine;
using Systems.SceneSandbox.Core;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Standard placement: Raycast to placement layers or ground plane, with grid snapping.
    /// </summary>
    public class DefaultPlacementStrategy : IPlacementStrategy
    {
        private float _maxDistance = 1000f;
        private LayerMask _placementLayers = -1; // Default to all
        private float _gridSize = 1f;
        private bool _snapToGrid = true;

        public DefaultPlacementStrategy(float maxDistance, LayerMask layers, bool snapToGrid, float gridSize)
        {
            _maxDistance = maxDistance;
            _placementLayers = layers;
            _snapToGrid = snapToGrid;
            _gridSize = gridSize;
        }

        public bool GetPlacement(Ray ray, ToolContext context, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            // 1. Raycast
            if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _placementLayers))
            {
                position = hit.point;
            }
            else
            {
                // Fallback to ground plane at y=0 if no hit
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    position = ray.GetPoint(enter);
                }
                else
                {
                    position = ray.GetPoint(10f);
                }
            }

            // 2. Snap
            if (_snapToGrid && context.GridManager != null)
            {
                position = context.GridManager.GetSnappedPosition(position);
            }

            return true;
        }
    }
}
