using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Input
{
    /// <summary>
    /// Input provider implementation using Unity's Legacy Input Manager.
    /// Provides abstraction over Input.GetMouseButton, Input.GetKey, etc.
    /// </summary>
    public class LegacyInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Input Configuration")]
        [SerializeField, Tooltip("Key code for place action")]
        private KeyCode _placeKey = KeyCode.Mouse0;

        [SerializeField, Tooltip("Key code for cancel action")]
        private KeyCode _cancelKey = KeyCode.Mouse1;

        [SerializeField, Tooltip("Key code for rotate action")]
        private KeyCode _rotateKey = KeyCode.R;

        [SerializeField, Tooltip("Alternative cancel key")]
        private KeyCode _alternativeCancelKey = KeyCode.Escape;

        public Vector2 GetPointerPosition()
        {
            return UnityEngine.Input.mousePosition;
        }

        public bool IsPlaceActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(_placeKey);
        }

        public bool IsCancelActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(_cancelKey) || 
                   UnityEngine.Input.GetKeyDown(_alternativeCancelKey);
        }

        public bool IsRotateActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(_rotateKey);
        }
    }
}
