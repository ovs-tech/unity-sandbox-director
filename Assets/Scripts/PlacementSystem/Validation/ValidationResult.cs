using UnityEngine;

namespace Systems.PlacementSystem.Validation
{
    public struct ValidationResult {
        public bool IsValid;
        public string ErrorMessage;

        public static ValidationResult Success => new ValidationResult { IsValid = true, ErrorMessage = string.Empty };
        public static ValidationResult Failure(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}
