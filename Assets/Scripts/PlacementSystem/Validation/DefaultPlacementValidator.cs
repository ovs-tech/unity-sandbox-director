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
        public ValidationResult IsPlacementValid(Vector3 position, Quaternion rotation, GameObject ghostObject)
        {
            if (ghostObject == null)
                return ValidationResult.Failure("Ghost object is null.");

            // Get the Part component from the ghost
            var part = ghostObject.GetComponent<PlacementPart>();

            if (part == null)
            {
                // If no Part component, assume placement is valid
                // (object has no specific validation requirements)
                return ValidationResult.Success;
            }

            // Validate using the object's rules
            return part.ValidatePlacement(position, rotation, ghostObject);
        }
    }
}
