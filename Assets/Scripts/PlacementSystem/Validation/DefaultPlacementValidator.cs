using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Core.Components;

namespace Systems.PlacementSystem.Validation
{
    /// <summary>
    /// Concrete implementation of IPlacementValidator that uses Part rules.
    /// This bridges the interface-based architecture with the ScriptableObject rule system.
    /// It is a pure C# class that can be instantiated by the pipeline.
    /// </summary>
    public class DefaultPlacementValidator : IPlacementValidator
    {
        private readonly bool _allowPlacementWithoutPart;
        private readonly bool _searchInChildren;

        public DefaultPlacementValidator(bool allowPlacementWithoutPart = true, bool searchInChildren = true)
        {
            _allowPlacementWithoutPart = allowPlacementWithoutPart;
            _searchInChildren = searchInChildren;
        }

        public ValidationResult IsPlacementValid(Vector3 position, Quaternion rotation, GameObject ghostObject)
        {
            if (ghostObject == null)
                return ValidationResult.Failure("Ghost object is null.");

            // Get the Part component from the ghost
            var part = _searchInChildren
                ? ghostObject.GetComponentInChildren<PlacementPart>(true)
                : ghostObject.GetComponent<PlacementPart>();

            if (part == null)
            {
                return _allowPlacementWithoutPart
                    ? ValidationResult.Success
                    : ValidationResult.Failure("PlacementPart not found on ghost object.");
            }

            // Validate using the object's rules
            return part.ValidatePlacement(position, rotation, ghostObject);
        }
    }
}
