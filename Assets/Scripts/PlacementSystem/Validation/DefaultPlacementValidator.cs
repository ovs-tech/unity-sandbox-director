using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Validation
{
    /// <summary>
    /// Concrete implementation of IPlacementValidator that uses PlaceableObject rules.
    /// This bridges the interface-based architecture with the ScriptableObject rule system.
    /// It is a pure C# class that can be instantiated by the pipeline.
    /// </summary>
    public class DefaultPlacementValidator : IPlacementValidator
    {
        public bool IsPlacementValid(Vector3 position, Quaternion rotation, GameObject ghostObject)
        {
            if (ghostObject == null)
                return false;

            // Get the PlaceableObject component from the ghost
            var placeableObject = ghostObject.GetComponent<PlaceableObject>();

            if (placeableObject == null)
            {
                // If no PlaceableObject component, assume placement is valid
                // (object has no specific validation requirements)
                return true;
            }

            // Validate using the object's rules
            return placeableObject.ValidatePlacement(position, rotation, ghostObject);
        }
    }
}
