using UnityEngine;

namespace Systems.PlacementSystem.Validation
{
    /// <summary>
    /// Rule that requires the object to be placed on a specific surface type.
    /// Uses a downward raycast to verify the surface layer.
    /// </summary>
    [CreateAssetMenu(fileName = "RequireSurfaceRule", menuName = "Placement System/Rules/Require Surface Rule")]
    public class RequireSurfaceRule : PlacementRule
    {
        [Header("Surface Requirements")]
        [SerializeField, Tooltip("Layer mask for valid placement surfaces (e.g., 'Stage Floor' layer)")]
        private LayerMask _requiredSurfaceLayer;

        [SerializeField, Tooltip("Maximum distance to raycast downward to find surface")]
        private float _raycastDistance = 2f;

        [SerializeField, Tooltip("Offset from object position to start raycast (useful for elevated objects)")]
        private float _raycastStartOffset = 0.5f;

        [SerializeField, Tooltip("If true, also checks that the surface is relatively flat")]
        private bool _requireFlatSurface = true;

        [SerializeField, Tooltip("Maximum angle (in degrees) from vertical for a 'flat' surface")]
        private float _maxSurfaceAngle = 30f;

        public override ValidationResult CheckRule(Vector3 position, Quaternion rotation, GameObject ghostObject)
        {
            // Start raycast slightly above the position
            Vector3 rayStart = position + Vector3.up * _raycastStartOffset;
            Vector3 rayDirection = Vector3.down;

            // Raycast to find surface
            if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, _raycastDistance, _requiredSurfaceLayer))
            {
                // Check if surface is flat enough (if required)
                if (_requireFlatSurface)
                {
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    if (angle > _maxSurfaceAngle)
                    {
                        return ValidationResult.Failure("Surface is too steep.");
                    }
                }

                return ValidationResult.Success;
            }

            // No valid surface found
            return ValidationResult.Failure("Valid placement surface not found.");
        }

        public override string GetDebugInfo()
        {
            return $"Surface check: Layer mask {_requiredSurfaceLayer.value}, max angle: {_maxSurfaceAngle}°";
        }
    }
}
