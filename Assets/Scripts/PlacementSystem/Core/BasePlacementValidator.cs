using UnityEngine;
using Systems.PlacementSystem.Validation;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Base ScriptableObject type for placement validators assignable from PlacementController.
    /// </summary>
    public abstract class BasePlacementValidator : ScriptableObject, IPlacementValidator
    {
        public abstract ValidationResult IsPlacementValid(Vector3 position, Quaternion rotation, GameObject ghostObject);
    }
}
