using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Systems.SceneSandbox.Core
{
    /// <summary>
    /// Centralizes input actions for the Scene Sandbox and exposes events for pointer and hotkey interactions.
    /// Minimal foundation per OpenSpec Phase 3.3 (non-breaking).
    /// </summary>
    public class SandboxInputManager : MonoBehaviour
    {
        [Header("Input Action References")]
        [FormerlySerializedAs("_toggleModeActionRef")]
        [SerializeField] private InputActionReference _toggleModeActionRef;

        [FormerlySerializedAs("_exitAllModesActionRef")]
        [SerializeField] private InputActionReference _exitAllModesActionRef;

        [FormerlySerializedAs("_moveHotkeyActionRef")]
        [SerializeField] private InputActionReference _moveHotkeyActionRef;

        [FormerlySerializedAs("_rotateHotkeyActionRef")]
        [SerializeField] private InputActionReference _rotateHotkeyActionRef;

        [FormerlySerializedAs("_scaleHotkeyActionRef")]
        [SerializeField] private InputActionReference _scaleHotkeyActionRef;

        [FormerlySerializedAs("_transformModeIncreaseActionRef")]
        [SerializeField] private InputActionReference _transformModeIncreaseActionRef;

        [FormerlySerializedAs("_transformModeDecreaseActionRef")]
        [SerializeField] private InputActionReference _transformModeDecreaseActionRef;

        [FormerlySerializedAs("_transformModeToggleAxisActionRef")]
        [SerializeField] private InputActionReference _transformModeToggleAxisActionRef;

        [FormerlySerializedAs("_pointerPositionActionRef")]
        [SerializeField] private InputActionReference _pointerPositionActionRef;

        [FormerlySerializedAs("_leftClickActionRef")]
        [SerializeField] private InputActionReference _leftClickActionRef;

        [FormerlySerializedAs("_rightClickActionRef")]
        [SerializeField] private InputActionReference _rightClickActionRef;

        [FormerlySerializedAs("_mouseScrollActionRef")]
        [SerializeField] private InputActionReference _mouseScrollActionRef;

        [FormerlySerializedAs("_cancelPlacementActionRef")]
        [SerializeField] private InputActionReference _cancelPlacementActionRef;

        [FormerlySerializedAs("_switchPlacementItemActionRef")]
        [SerializeField] private InputActionReference _switchPlacementItemActionRef;

        [FormerlySerializedAs("_startPlacementActionRef")]
        [SerializeField] private InputActionReference _startPlacementActionRef;

        [Header("Flags")]
        [SerializeField] private bool _enableHotkeys = true;
        [SerializeField] private bool _debugLogs = false;

        // Events (foundation signatures)
        public event Action<Vector2> OnPointerMoved;
        public event Action<Vector2> OnPointerDown;
        public event Action<Vector2> OnPointerUp;
        public event Action<Vector2, float> OnPointerDrag;
        public event Action<float> OnScroll;

        public event Action OnToggleMode;
        public event Action OnExitAllModes;
        public event Action OnMoveHotkey;
        public event Action OnRotateHotkey;
        public event Action OnScaleHotkey;
        public event Action OnTransformIncrease;
        public event Action OnTransformDecrease;
        public event Action OnToggleAxis;
        public event Action OnCancelPlacement;
        public event Action OnSwitchPlacementItemHotkey;
        public event Action OnStartPlacementHotkey;

        private bool _initialized;
        private bool _isPointerDown;
        private float _dragDistance;
        private Vector2 _cachedPointerPosition = Vector2.zero;

        public bool EnableHotkeys => _enableHotkeys;

        void Update()
        {
            if (_leftClickActionRef == null || !_leftClickActionRef.action.enabled) return;

            bool leftButtonDown = _leftClickActionRef.action.WasPressedThisFrame();
            bool leftButtonUp = _leftClickActionRef.action.WasReleasedThisFrame();
            bool leftButtonHeld = _leftClickActionRef.action.IsPressed();

            if (leftButtonDown)
            {
                if (_debugLogs) Debug.Log($"[SandboxInputManager] Pointer Down at {_cachedPointerPosition}");
                OnPointerDown?.Invoke(_cachedPointerPosition);
                _isPointerDown = true;
            }

            if (leftButtonHeld)
            {
                if (_isPointerDown) {
                    _dragDistance += Vector2.Distance(_cachedPointerPosition, GetPointer());
                    if (_debugLogs) Debug.Log($"[SandboxInputManager] Pointer Drag at {_cachedPointerPosition}, distance: {_dragDistance}");
                    OnPointerDrag?.Invoke(_cachedPointerPosition, _dragDistance);
                } else {
                    OnPointerDown?.Invoke(_cachedPointerPosition);
                }
            }

             if (leftButtonUp && _isPointerDown)
            {
                if (_debugLogs) Debug.Log($"[SandboxInputManager] Pointer Up at {_cachedPointerPosition}");
                OnPointerUp?.Invoke(_cachedPointerPosition);
                _isPointerDown = false;
            }
        }

        public void Initialize(bool enableHotkeys)
        {
            _enableHotkeys = enableHotkeys;
            _initialized = true;
            
            if (_debugLogs) Debug.Log($"[SandboxInputManager] Initialized with hotkeys: {enableHotkeys}");
            
            // Enable actions immediately after initialization only when playing.
            // In EditMode tests we should avoid enabling Input System actions to prevent runtime asserts.
            if (Application.isPlaying)
            {
                EnableActions();
            }
        }

        /// <summary>
        /// Set input action references from SceneSandboxBuilder (runtime initialization)
        /// </summary>
        public void SetInputActionReferences(
            InputActionReference toggleMode,
            InputActionReference exitAllModes,
            InputActionReference moveHotkey,
            InputActionReference rotateHotkey,
            InputActionReference scaleHotkey,
            InputActionReference transformIncrease,
            InputActionReference transformDecrease,
            InputActionReference transformToggleAxis,
            InputActionReference pointerPosition,
            InputActionReference leftClick,
            InputActionReference rightClick,
            InputActionReference mouseScroll,
            InputActionReference cancelPlacement,
            InputActionReference switchPlacementItem,
            InputActionReference startPlacement)
        {
            if (_debugLogs) Debug.Log("[SandboxInputManager] Setting input action references");
            
            _toggleModeActionRef = toggleMode;
            _exitAllModesActionRef = exitAllModes;
            _moveHotkeyActionRef = moveHotkey;
            _rotateHotkeyActionRef = rotateHotkey;
            _scaleHotkeyActionRef = scaleHotkey;
            _transformModeIncreaseActionRef = transformIncrease;
            _transformModeDecreaseActionRef = transformDecrease;
            _transformModeToggleAxisActionRef = transformToggleAxis;
            _pointerPositionActionRef = pointerPosition;
            _leftClickActionRef = leftClick;
            _rightClickActionRef = rightClick;
            _mouseScrollActionRef = mouseScroll;
            _cancelPlacementActionRef = cancelPlacement;
            _switchPlacementItemActionRef = switchPlacementItem;
            _startPlacementActionRef = startPlacement;
        }

        private void OnEnable()
        {
            if (!_initialized) return;
            EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        public void EnableActions()
        {
            if (_debugLogs) Debug.Log("[SandboxInputManager] Enabling actions");
            
            // Pointer + mouse
            _pointerPositionActionRef?.action.Enable();
            _leftClickActionRef?.action.Enable();
            _rightClickActionRef?.action.Enable();
            _mouseScrollActionRef?.action.Enable();

            // Mode + hotkeys
            _toggleModeActionRef?.action.Enable();
            _exitAllModesActionRef?.action.Enable();

            if (_enableHotkeys)
            {
                _moveHotkeyActionRef?.action.Enable();
                _rotateHotkeyActionRef?.action.Enable();
                _scaleHotkeyActionRef?.action.Enable();
                _transformModeIncreaseActionRef?.action.Enable();
                _transformModeDecreaseActionRef?.action.Enable();
                _transformModeToggleAxisActionRef?.action.Enable();
            }

            _cancelPlacementActionRef?.action.Enable();
            _switchPlacementItemActionRef?.action.Enable();
            _startPlacementActionRef?.action.Enable();

            RegisterCallbacks();
        }

        public void DisableActions()
        {
            if (_debugLogs) Debug.Log("[SandboxInputManager] Disabling actions");
            
            UnregisterCallbacks();

            _pointerPositionActionRef?.action.Disable();
            _leftClickActionRef?.action.Disable();
            _rightClickActionRef?.action.Disable();
            _mouseScrollActionRef?.action.Disable();

            _toggleModeActionRef?.action.Disable();
            _exitAllModesActionRef?.action.Disable();

            _moveHotkeyActionRef?.action.Disable();
            _rotateHotkeyActionRef?.action.Disable();
            _scaleHotkeyActionRef?.action.Disable();
            _transformModeIncreaseActionRef?.action.Disable();
            _transformModeDecreaseActionRef?.action.Disable();
            _transformModeToggleAxisActionRef?.action.Disable();

            _cancelPlacementActionRef?.action.Disable();
            _switchPlacementItemActionRef?.action.Disable();
            _startPlacementActionRef?.action.Disable();
        }

        private void RegisterCallbacks()
        {
            // Pointer position changes (performed acts like a stream for value actions)
            if (_pointerPositionActionRef != null)
            {
                _pointerPositionActionRef.action.performed += ctx => 
                {
                    _cachedPointerPosition = ctx.ReadValue<Vector2>();
                    if (_debugLogs) Debug.Log($"[SandboxInputManager] Pointer Moved to {_cachedPointerPosition}");
                    OnPointerMoved?.Invoke(_cachedPointerPosition);
                };
            }

            if (_leftClickActionRef != null)
            {
                // For button-type actions: started = press down, canceled = release
                // For press-release actions: performed may also fire on press
                _leftClickActionRef.action.started += ctx => 
                {
                    OnPointerDown?.Invoke(_cachedPointerPosition);
                };
                _leftClickActionRef.action.performed += ctx => 
                {
                    // Only invoke if started didn't fire (some action types)
                };
                _leftClickActionRef.action.canceled += ctx => 
                {
                    OnPointerUp?.Invoke(_cachedPointerPosition);
                };
            }

            if (_mouseScrollActionRef != null)
            {
                _mouseScrollActionRef.action.performed += ctx =>
                {
                    float scrollValue = ctx.ReadValue<Vector2>().y;
                    if (_debugLogs) Debug.Log($"[SandboxInputManager] Scroll: {scrollValue}");
                    OnScroll?.Invoke(scrollValue);
                };
            }

            if (_toggleModeActionRef != null)
            {
                _toggleModeActionRef.action.performed += ctx =>
                {
                    if (_debugLogs) Debug.Log("[SandboxInputManager] Toggle Mode triggered");
                    OnToggleMode?.Invoke();
                };
            }

            if (_exitAllModesActionRef != null)
            {
                _exitAllModesActionRef.action.performed += ctx =>
                {
                    if (_debugLogs) Debug.Log("[SandboxInputManager] Exit All Modes triggered");
                    OnExitAllModes?.Invoke();
                };
            }

            if (_enableHotkeys)
            {
                if (_moveHotkeyActionRef != null)
                    _moveHotkeyActionRef.action.performed += ctx =>
                    {
                        if (_debugLogs) Debug.Log("[SandboxInputManager] Move Hotkey triggered");
                        OnMoveHotkey?.Invoke();
                    };
                if (_rotateHotkeyActionRef != null)
                    _rotateHotkeyActionRef.action.performed += ctx =>
                    {
                        if (_debugLogs) Debug.Log("[SandboxInputManager] Rotate Hotkey triggered");
                        OnRotateHotkey?.Invoke();
                    };
                if (_scaleHotkeyActionRef != null)
                    _scaleHotkeyActionRef.action.performed += ctx =>
                    {
                        if (_debugLogs) Debug.Log("[SandboxInputManager] Scale Hotkey triggered");
                        OnScaleHotkey?.Invoke();
                    };
                if (_transformModeIncreaseActionRef != null)
                    _transformModeIncreaseActionRef.action.performed += ctx =>
                    {
                        if (_debugLogs) Debug.Log("[SandboxInputManager] Transform Increase triggered");
                        OnTransformIncrease?.Invoke();
                    };
                if (_transformModeDecreaseActionRef != null)
                    _transformModeDecreaseActionRef.action.performed += ctx =>
                    {
                        if (_debugLogs) Debug.Log("[SandboxInputManager] Transform Decrease triggered");
                        OnTransformDecrease?.Invoke();
                    };
                if (_transformModeToggleAxisActionRef != null)
                    _transformModeToggleAxisActionRef.action.performed += ctx =>
                    {
                        if (_debugLogs) Debug.Log("[SandboxInputManager] Toggle Axis triggered");
                        OnToggleAxis?.Invoke();
                    };
            }

            if (_cancelPlacementActionRef != null)
            {
                _cancelPlacementActionRef.action.performed += ctx =>
                {
                    if (_debugLogs) Debug.Log("[SandboxInputManager] Cancel Placement triggered");
                    OnCancelPlacement?.Invoke();
                };
            }

            if (_switchPlacementItemActionRef != null)
            {
                _switchPlacementItemActionRef.action.performed += ctx =>
                {
                    if (_debugLogs) Debug.Log("[SandboxInputManager] Switch Placement Item triggered");
                    OnSwitchPlacementItemHotkey?.Invoke();
                };
            }

            if (_startPlacementActionRef != null)
            {
                _startPlacementActionRef.action.performed += ctx =>
                {
                    if (_debugLogs) Debug.Log("[SandboxInputManager] Start Placement triggered");
                    OnStartPlacementHotkey?.Invoke();
                };
            }
        }

        private void UnregisterCallbacks()
        {
            // Unity Input System does not provide a direct unsubscribe API for lambda handlers.
            // To avoid duplicate registrations, disable actions before re-enabling and do not register multiple times.
        }

        private Vector2 GetPointer()
        {
            return _pointerPositionActionRef != null ? _pointerPositionActionRef.action.ReadValue<Vector2>() : Vector2.zero;
        }
    }
}
