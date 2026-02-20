using UnityEngine;

namespace Systems.PlacementSystem.Validation
{
    /// <summary>
    /// Rule that checks for clearance using Physics.OverlapBox.
    /// Ensures no other colliders are blocking the placement location.
    /// </summary>
    [CreateAssetMenu(fileName = "ClearanceRule", menuName = "Placement System/Rules/Clearance Rule")]
    public class ClearanceRule : PlacementRule
    {
        [Header("Clearance Settings")]
        [SerializeField, Tooltip("The size of the box to check for clearance")]
        private Vector3 _checkBoxSize = new Vector3(1f, 2f, 1f);

        [SerializeField, Tooltip("Layer mask to check against (e.g., other placed objects)")]
        private LayerMask _obstacleLayer = -1;

        [SerializeField, Tooltip("Offset from the object's position for the clearance check")]
        private Vector3 _checkOffset = Vector3.zero;

        [SerializeField, Tooltip("Maximum number of overlapping colliders allowed (0 = none)")]
        private int _maxAllowedOverlaps = 0;

        public override bool CheckRule(Vector3 position, Quaternion rotation, GameObject ghostObject)
        {
            // Calculate check position with offset
            Vector3 checkPosition = position + rotation * _checkOffset;

            // Perform overlap check
            Collider[] overlaps = Physics.OverlapBox(
                checkPosition,
                _checkBoxSize / 2f,
                rotation,
                _obstacleLayer,
                QueryTriggerInteraction.Ignore // Ignore trigger colliders
            );

            // Filter out colliders that belong to the ghost object itself
            int validOverlaps = 0;
            foreach (var overlap in overlaps)
            {
                if (!overlap.transform.IsChildOf(ghostObject.transform) && 
                    overlap.gameObject != ghostObject)
                {
                    validOverlaps++;
                }
            }

            return validOverlaps <= _maxAllowedOverlaps;
        }

        public override string GetDebugInfo()
        {
            return $"Clearance check: Box size {_checkBoxSize}, max overlaps: {_maxAllowedOverlaps}";
        }

        // Draw debug visualization in scene view
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, _checkBoxSize);
        }
    }
}
