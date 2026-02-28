using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Strategies
{
    /// <summary>
    /// Free placement strategy - no snapping or constraints.
    /// Places objects exactly where the raycast hits.
    /// </summary>
    [CreateAssetMenu(fileName = "FreePlacementStrategy", menuName = "Placement System/Strategies/Free Strategy")]
    public class FreePositionStrategy : BasePlacementStrategy
    {
        [Header("Free Placement Settings")]
        [SerializeField, Tooltip("Offset from surface (useful to prevent z-fighting)")]
        private float _surfaceOffset = 0.05f;

        [SerializeField, Tooltip("Align to surface normal")]
        private bool _alignToSurface = false;

        public override Vector3 CalculatePosition(Vector3 rawWorldPos, GameObject ghostObject)
        {
            // Apply surface offset in the up direction
            return rawWorldPos + Vector3.up * _surfaceOffset;
        }

        public override Quaternion CalculateRotation(Quaternion currentRotation)
        {
            // Return rotation as-is (controlled by user input)
            return currentRotation;
        }

        public override void OnDrawGizmos()
        {
            // No visualization needed for free placement
        }
    }
}
