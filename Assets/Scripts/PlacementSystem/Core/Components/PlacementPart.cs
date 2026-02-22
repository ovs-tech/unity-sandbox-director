using System.Collections.Generic;
using UnityEngine;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Validation;

namespace Systems.PlacementSystem.Core.Components
{
    /// <summary>
    /// Component attached to prefabs that holds their placement validation rules.
    /// Rules are ScriptableObjects that can be configured per-object in the editor.
    /// This makes validation data-driven and easily customizable without code changes.
    /// </summary>
    public class PlacementPart : MonoBehaviour
    {
        [Header("Part Visuals")]
        [SerializeField, Tooltip("The main visual model of the part")]
        private GameObject _model;

        [SerializeField, Tooltip("The preview/ghost object used during placement")]
        private GameObject _preview;

        public GameObject Model => _model;
        public GameObject Preview => _preview;

        [Header("Validation Rules")]
        [SerializeField, Tooltip("List of rules that must ALL pass for placement to be valid")]
        private List<PlacementRule> _placementRules = new List<PlacementRule>();

        [Header("Socket Snapping (Optional)")]
        [SerializeField, Tooltip("If set, this object will try to snap to sockets of this type")]
        private SocketType _requiredSocketType;

        [SerializeField, Tooltip("Maximum distance to search for sockets")]
        private float _snapRange = 2f;

        /// <summary>
        /// Gets the required socket type for this object (null if no socket required).
        /// </summary>
        public SocketType RequiredSocketType => _requiredSocketType;

        /// <summary>
        /// Gets the snap range for socket detection.
        /// </summary>
        public float SnapRange => _snapRange;

        /// <summary>
        /// Validates placement by checking all rules.
        /// </summary>
        /// <param name="position">Position to validate</param>
        /// <param name="rotation">Rotation to validate</param>
        /// <param name="ghostObject">The ghost object (usually 'this.gameObject' or its parent)</param>
        /// <returns>True if all rules pass</returns>
        public ValidationResult ValidatePlacement(Vector3 position, Quaternion rotation, GameObject ghostObject)
        {
            // If no rules, allow placement by default
            if (_placementRules == null || _placementRules.Count == 0)
                return ValidationResult.Success;

            // All rules must pass
            foreach (var rule in _placementRules)
            {
                if (rule == null)
                {
                    continue;
                }

                var result = rule.CheckRule(position, rotation, ghostObject);
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// Adds a rule at runtime (useful for dynamic rule configuration).
        /// </summary>
        public void AddRule(PlacementRule rule)
        {
            if (rule != null && !_placementRules.Contains(rule))
            {
                _placementRules.Add(rule);
            }
        }

        /// <summary>
        /// Removes a rule at runtime.
        /// </summary>
        public void RemoveRule(PlacementRule rule)
        {
            _placementRules.Remove(rule);
        }

        /// <summary>
        /// Gets all current rules (read-only).
        /// </summary>
        public IReadOnlyList<PlacementRule> GetRules() => _placementRules.AsReadOnly();
    }
}
