using UnityEngine;
using Systems.PlacementSystem.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Systems.PlacementSystem.Input
{
    /// <summary>
    /// Input provider implementation using Unity's New Input System.
    /// Demonstrates how the same interface can support different input backends.
    /// Note: Requires the Input System package to be installed.
    /// </summary>
    public class NewInputSystemProvider : MonoBehaviour, IInputProvider
    {
#if ENABLE_INPUT_SYSTEM
        [Header("Input Action References")]
        [SerializeField, Tooltip("Input action for pointer position")]
        private InputActionReference _pointerPositionAction;

        [SerializeField, Tooltip("Input action for place action")]
        private InputActionReference _placeAction;

        [SerializeField, Tooltip("Input action for cancel action")]
        private InputActionReference _cancelAction;

        [SerializeField, Tooltip("Input action for rotate action")]
        private InputActionReference _rotateAction;

        private void OnEnable()
        {
            // Enable all input actions
            _pointerPositionAction?.action.Enable();
            _placeAction?.action.Enable();
            _cancelAction?.action.Enable();
            _rotateAction?.action.Enable();
        }

        private void OnDisable()
        {
            // Disable all input actions
            _pointerPositionAction?.action.Disable();
            _placeAction?.action.Disable();
            _cancelAction?.action.Disable();
            _rotateAction?.action.Disable();
        }

        public Vector2 GetPointerPosition()
        {
            if (_pointerPositionAction != null && _pointerPositionAction.action.enabled)
            {
                return _pointerPositionAction.action.ReadValue<Vector2>();
            }

            // Fallback to mouse position
            return Mouse.current?.position.ReadValue() ?? Vector2.zero;
        }

        public bool IsPlaceActionTriggered()
        {
            return _placeAction != null && _placeAction.action.WasPressedThisFrame();
        }

        public bool IsCancelActionTriggered()
        {
            return _cancelAction != null && _cancelAction.action.WasPressedThisFrame();
        }

        public bool IsRotateActionTriggered()
        {
            return _rotateAction != null && _rotateAction.action.WasPressedThisFrame();
        }
#else
        // Fallback implementation if Input System package is not installed
        public Vector2 GetPointerPosition()
        {
            Debug.LogWarning("New Input System not installed. Using legacy input fallback.");
            return UnityEngine.Input.mousePosition;
        }

        public bool IsPlaceActionTriggered()
        {
            return UnityEngine.Input.GetMouseButtonDown(0);
        }

        public bool IsCancelActionTriggered()
        {
            return UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        }

        public bool IsRotateActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.R);
        }
#endif
    }
}
