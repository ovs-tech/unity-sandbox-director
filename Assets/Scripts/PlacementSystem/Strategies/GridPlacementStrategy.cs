using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Strategies
{
    /// <summary>
    /// Grid-based placement strategy.
    /// Snaps objects to a regular 2D grid on the XZ plane.
    /// Useful for building games with tile-based layouts (like The Sims).
    /// </summary>
    [CreateAssetMenu(fileName = "GridPlacementStrategy", menuName = "Placement System/Strategies/Grid Strategy")]
    public class GridPlacementStrategy : BasePlacementStrategy
    {
        [Header("Grid Settings")]
        [SerializeField, Tooltip("Size of each grid cell")]
        private float _gridSize = 1f;

        [SerializeField, Tooltip("Grid origin offset")]
        private Vector3 _gridOrigin = Vector3.zero;

        [SerializeField, Tooltip("Rotation snapping (in degrees, 0 = no snap)")]
        private float _rotationSnap = 90f;

        [Header("Visualization")]
        [SerializeField, Tooltip("Show grid in scene view")]
        private bool _showGridGizmo = true;

        [SerializeField, Tooltip("Grid visualization size")]
        private int _gridVisualizationSize = 10;

        public override Vector3 CalculatePosition(Vector3 rawWorldPos, GameObject ghostObject)
        {
            // Convert to grid space
            Vector3 relativePos = rawWorldPos - _gridOrigin;

            // Snap to grid
            float snappedX = Mathf.Round(relativePos.x / _gridSize) * _gridSize;
            float snappedZ = Mathf.Round(relativePos.z / _gridSize) * _gridSize;

            // Keep Y position (height) from raycast
            return new Vector3(snappedX, rawWorldPos.y, snappedZ) + _gridOrigin;
        }

        public override Quaternion CalculateRotation(Quaternion currentRotation)
        {
            if (_rotationSnap <= 0f)
                return currentRotation;

            // Extract Y rotation
            Vector3 euler = currentRotation.eulerAngles;
            float snappedY = Mathf.Round(euler.y / _rotationSnap) * _rotationSnap;

            return Quaternion.Euler(0, snappedY, 0);
        }

        public override void OnDrawGizmos()
        {
            if (!_showGridGizmo)
                return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

            // Draw grid lines
            int halfSize = _gridVisualizationSize / 2;
            for (int x = -halfSize; x <= halfSize; x++)
            {
                Vector3 start = _gridOrigin + new Vector3(x * _gridSize, 0, -halfSize * _gridSize);
                Vector3 end = _gridOrigin + new Vector3(x * _gridSize, 0, halfSize * _gridSize);
                Gizmos.DrawLine(start, end);
            }

            for (int z = -halfSize; z <= halfSize; z++)
            {
                Vector3 start = _gridOrigin + new Vector3(-halfSize * _gridSize, 0, z * _gridSize);
                Vector3 end = _gridOrigin + new Vector3(halfSize * _gridSize, 0, z * _gridSize);
                Gizmos.DrawLine(start, end);
            }

            // Draw origin marker
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_gridOrigin, 0.1f);
        }
    }
}
