using UnityEngine;
using Systems.PlacementSystem.Core;

#if ENABLE_INPUT_SYSTEM && false
using UnityEngine.InputSystem;
#endif

namespace Systems.PlacementSystem.Input
{
    /// <summary>
    /// Input provider implementation using Unity's New Input System.
    /// Demonstrates how the same interface can support different input backends.
    /// Note: Requires the Input System package to be installed.
    /// </summary>
    public class NewInputSystemProvider : BaseInputProvider
    {
#if ENABLE_INPUT_SYSTEM && false
        [Header("Input Action References")]
        [SerializeField, Tooltip("Input action for pointer position")]
        private InputActionReference _pointerPositionAction;

        [SerializeField, Tooltip("Input action for place action")]
        private InputActionReference _placeAction;

        [SerializeField, Tooltip("Input action for cancel action")]
        private InputActionReference _cancelAction;

        [SerializeField, Tooltip("Input action for rotate action")]
        private InputActionReference _rotateAction;

        [SerializeField, Tooltip("Input action for delete action")]
        private InputActionReference _deleteAction;

        private void OnEnable()
        {
            // Enable all input actions
            _pointerPositionAction?.action.Enable();
            _placeAction?.action.Enable();
            _cancelAction?.action.Enable();
            _rotateAction?.action.Enable();
            _deleteAction?.action.Enable();
        }

        private void OnDisable()
        {
            // Disable all input actions
            _pointerPositionAction?.action.Disable();
            _placeAction?.action.Disable();
            _cancelAction?.action.Disable();
            _rotateAction?.action.Disable();
            _deleteAction?.action.Disable();
        }

        public override Vector2 GetPointerPosition()
        {
            if (_pointerPositionAction != null && _pointerPositionAction.action.enabled)
            {
                return _pointerPositionAction.action.ReadValue<Vector2>();
            }

            // Fallback to classic Input mouse position (avoids referencing InputSystem.Mouse when unavailable)
            return (Vector2)UnityEngine.Input.mousePosition;
        }

        public override bool IsPlaceActionTriggered()
        {
            return _placeAction != null && _placeAction.action.WasPressedThisFrame();
        }

        public override bool IsCancelActionTriggered()
        {
            return _cancelAction != null && _cancelAction.action.WasPressedThisFrame();
        }

        public override bool IsRotateActionTriggered()
        {
            return _rotateAction != null && _rotateAction.action.WasPressedThisFrame();
        }

        public override bool IsDeleteActionTriggered()
        {
            return _deleteAction != null && _deleteAction.action.WasPressedThisFrame();
        }

        public override bool IsMultiSelectModifierHeld()
        {
            return Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed ||
                                                Keyboard.current.rightCtrlKey.isPressed ||
                                                Keyboard.current.leftCommandKey.isPressed ||
                                                Keyboard.current.rightCommandKey.isPressed);
        }
#else
        // Fallback implementation if Input System package is not installed
        public override Vector2 GetPointerPosition()
        {
            return UnityEngine.Input.mousePosition;
        }

        public override bool IsPlaceActionTriggered()
        {
            return UnityEngine.Input.GetMouseButtonDown(0);
        }

        public override bool IsCancelActionTriggered()
        {
            return UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        }

        public override bool IsRotateActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.R);
        }

        public override bool IsDeleteActionTriggered()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Delete);
        }

        public override bool IsMultiSelectModifierHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftControl) ||
                   UnityEngine.Input.GetKey(KeyCode.RightControl) ||
                   UnityEngine.Input.GetKey(KeyCode.LeftCommand) ||
                   UnityEngine.Input.GetKey(KeyCode.RightCommand);
        }
#endif
    }
}
