using UnityEngine;

namespace Systems.PlacementSystem.Validation
{
    /// <summary>
    /// Abstract base class for ScriptableObject-based placement rules.
    /// Follows Open-Closed Principle: new rules can be added without modifying existing code.
    /// Rules are data-driven and can be mixed and matched per object.
    /// </summary>
    public abstract class PlacementRule : ScriptableObject
    {
        [SerializeField, Tooltip("User-friendly description of what this rule checks")]
        private string _description;

        /// <summary>
        /// Checks if this placement rule is satisfied.
        /// </summary>
        /// <param name="position">World position to validate</param>
        /// <param name="rotation">Rotation to validate</param>
        /// <param name="ghostObject">The ghost object being placed (contains colliders for checking)</param>
        /// <returns>True if the rule passes, false otherwise</returns>
        public abstract ValidationResult CheckRule(Vector3 position, Quaternion rotation, GameObject ghostObject);

        /// <summary>
        /// Optional: Gets debug information about why the rule failed.
        /// </summary>
        public virtual string GetDebugInfo()
        {
            return _description;
        }
    }
}
