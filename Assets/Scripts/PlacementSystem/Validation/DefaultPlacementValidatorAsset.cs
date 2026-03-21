using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Validation
{
    /// <summary>
    /// ScriptableObject-backed validator that delegates to DefaultPlacementValidator.
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultPlacementValidator", menuName = "Placement System/Validation/Default Validator")]
    public class DefaultPlacementValidatorAsset : BasePlacementValidator
    {
        [SerializeField, Tooltip("When false, placement fails if a PlacementPart is not found on the ghost or its children.")]
        private bool _allowPlacementWithoutPart = true;

        [SerializeField, Tooltip("When true, searches child hierarchy for PlacementPart if not found on the root object.")]
        private bool _searchInChildren = true;

        public override ValidationResult IsPlacementValid(Vector3 position, Quaternion rotation, GameObject ghostObject)
        {
            var validator = new DefaultPlacementValidator(_allowPlacementWithoutPart, _searchInChildren);
            return validator.IsPlacementValid(position, rotation, ghostObject);
        }
    }
}
