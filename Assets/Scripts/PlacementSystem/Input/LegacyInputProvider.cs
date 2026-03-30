using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Input
{
    /// <summary>
    /// Input provider implementation using Unity's Legacy Input Manager.
    /// Provides abstraction over Input.GetMouseButton, Input.GetKey, etc.
    /// </summary>
    public class LegacyInputProvider : BaseInputProvider
    {
        [Header("Input Configuration")]
        [SerializeField, Tooltip("Key code for place action")]
        private KeyCode _placeKey = KeyCode.Mouse0;

        [SerializeField, Tooltip("Key code for cancel action")]
        private KeyCode _cancelKey = KeyCode.Mouse1;

        [SerializeField, Tooltip("Key code for rotate action")]
        private KeyCode _rotateKey = KeyCode.R;

        [SerializeField, Tooltip("Key code for delete action")]
        private KeyCode _deleteKey = KeyCode.Delete;

        [SerializeField, Tooltip("Alternative cancel key")]
        private KeyCode _alternativeCancelKey = KeyCode.Escape;

        public override Vector2 GetPointerPosition()
        {
            return UnityEngine.Input.mousePosition;
        }

        public override bool IsPlaceActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(_placeKey);
        }

        public override bool IsCancelActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(_cancelKey) || 
                   UnityEngine.Input.GetKeyDown(_alternativeCancelKey);
        }

        public override bool IsRotateActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(_rotateKey);
        }

        public override bool IsDeleteActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(_deleteKey);
        }

    }
}
