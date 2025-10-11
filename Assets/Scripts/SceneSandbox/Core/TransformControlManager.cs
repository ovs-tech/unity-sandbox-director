using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Manager for coordinating transform controls across multiple DraggableItems
    /// Ensures only one item is in transform mode at a time and provides global control
    /// </summary>
    public class TransformableControlManager : MonoBehaviour
    {
        [Header("Global Settings")]
        [SerializeField] private bool _allowMultipleTransformModes = false;

        [Header("Input Action References")]
        [SerializeField] private InputActionReference _exitAllModesActionRef;
        [SerializeField] private InputActionReference _moveHotkeyActionRef;
        [SerializeField] private InputActionReference _rotateHotkeyActionRef;
        [SerializeField] private InputActionReference _scaleHotkeyActionRef;

        [Header("Hotkey Settings")]
        [SerializeField] private bool _enableHotkeys = true;

        // Singleton pattern
        private static TransformableControlManager _instance;
        public static TransformableControlManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TransformableControlManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TransformControlManager");
                        _instance = go.AddComponent<TransformableControlManager>();
                    }
                }
                return _instance;
            }
        }

        // Active items in transform modes
        private List<TransformableItem> _activeTransformItems = new List<TransformableItem>();
        [SerializeField] private TransformableItem _currentActiveItem;

        // Input Actions
        private InputAction _exitAllModesAction;
        private InputAction _moveHotkeyAction;
        private InputAction _rotateHotkeyAction;
        private InputAction _scaleHotkeyAction;

        // Events
        public System.Action<TransformableItem, TransformMode> OnGlobalTransformModeChanged;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                // Subscribe to scene change events for cleanup
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                SceneManager.sceneLoaded += OnSceneLoaded;

                InitializeInputActions();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Input actions handle the exit functionality through callbacks
            // No need for polling input here anymore
        }

        /// <summary>
        /// Register a draggable item with the manager
        /// </summary>
        public void RegisterItem(TransformableItem item)
        {
            if (item != null)
            {
                item.OnTransformModeChanged += OnItemTransformModeChanged;
            }
        }

        /// <summary>
        /// Unregister a draggable item from the manager
        /// </summary>
        public void UnregisterItem(TransformableItem item)
        {
            if (item != null)
            {
                item.OnTransformModeChanged -= OnItemTransformModeChanged;
                _activeTransformItems.Remove(item);

                if (_currentActiveItem == item)
                {
                    _currentActiveItem = null;
                }
            }
        }

        /// <summary>
        /// Handle transform mode changes from items
        /// </summary>
        private void OnItemTransformModeChanged(TransformableItem item, TransformMode mode)
        {
            if (mode == TransformMode.None)
            {
                // Item exited transform mode
                _activeTransformItems.Remove(item);
                if (_currentActiveItem == item)
                {
                    _currentActiveItem = null;
                }
            }
            else
            {
                // Item entered transform mode
                if (!_allowMultipleTransformModes)
                {
                    // Exit all other items from transform mode
                    ExitOtherTransformModes(item);
                }

                if (!_activeTransformItems.Contains(item))
                {
                    _activeTransformItems.Add(item);
                }
                _currentActiveItem = item;
            }

            OnGlobalTransformModeChanged?.Invoke(item, mode);
        }

        /// <summary>
        /// Exit transform mode for all items except the specified one
        /// </summary>
        private void ExitOtherTransformModes(TransformableItem exceptItem)
        {
            var itemsToExit = new List<TransformableItem>(_activeTransformItems);
            foreach (var item in itemsToExit)
            {
                if (item != exceptItem && item != null)
                {
                    item.SetTransformMode(TransformMode.None);
                }
            }
        }

        /// <summary>
        /// Exit all transform modes
        /// </summary>
        public void ExitAllTransformModes()
        {
            var itemsToExit = new List<TransformableItem>(_activeTransformItems);
            foreach (var item in itemsToExit)
            {
                if (item != null)
                {
                    item.SetTransformMode(TransformMode.None);
                }
            }
            _activeTransformItems.Clear();
            _currentActiveItem = null;
        }

        /// <summary>
        /// Set transform mode for all selected items
        /// </summary>
        public void SetModeForSelectedItems(TransformMode mode)
        {
            var selectedItems = TransformableSelectionManager.Instance?.SelectedItems;
            if (selectedItems != null)
            {
                foreach (var item in selectedItems)
                {
                    if (item.EnableTransformControls)
                    {
                        item.SetTransformMode(mode);
                    }
                }
            }
        }

        /// <summary>
        /// Get the currently active transform item
        /// </summary>
        public TransformableItem GetCurrentActiveItem()
        {
            return _currentActiveItem;
        }

        /// <summary>
        /// Get all items currently in transform mode
        /// </summary>
        public List<TransformableItem> GetActiveTransformItems()
        {
            return new List<TransformableItem>(_activeTransformItems);
        }

        /// <summary>
        /// Check if any items are currently in transform mode
        /// </summary>
        public bool HasActiveTransformItems()
        {
            return _activeTransformItems.Count > 0;
        }

        /// <summary>
        /// Toggle multiple transform modes support
        /// </summary>
        public void SetAllowMultipleTransformModes(bool allow)
        {
            _allowMultipleTransformModes = allow;

            // If disabling multiple modes and we have multiple active, keep only the current one
            if (!allow && _activeTransformItems.Count > 1 && _currentActiveItem != null)
            {
                ExitOtherTransformModes(_currentActiveItem);
            }
        }

        /// <summary>
        /// Get statistics about current transform state
        /// </summary>
        public string GetTransformStateInfo()
        {
            return $"Active Transform Items: {_activeTransformItems.Count}, " +
                   $"Current Active: {(_currentActiveItem ? _currentActiveItem.name : "None")}, " +
                   $"Multiple Modes: {_allowMultipleTransformModes}";
        }

        #region Input Actions Setup

        /// <summary>
        /// Initialize input actions and bind events
        /// </summary>
        private void InitializeInputActions()
        {
            // Get actions directly from InputActionReference
            _exitAllModesAction = _exitAllModesActionRef?.action;
            _moveHotkeyAction = _moveHotkeyActionRef?.action;
            _rotateHotkeyAction = _rotateHotkeyActionRef?.action;
            _scaleHotkeyAction = _scaleHotkeyActionRef?.action;

            // Bind events
            if (_exitAllModesAction != null)
            {
                _exitAllModesAction.performed += OnExitAllModesPerformed;
            }

            if (_enableHotkeys)
            {
                // Note: These actions are for direct hotkey access if desired in the future
                // For now, transform mode switching happens through the action menu
                if (_moveHotkeyAction != null)
                {
                    _moveHotkeyAction.performed += OnMoveHotkeyPerformed;
                }
                if (_rotateHotkeyAction != null)
                {
                    _rotateHotkeyAction.performed += OnRotateHotkeyPerformed;
                }
                if (_scaleHotkeyAction != null)
                {
                    _scaleHotkeyAction.performed += OnScaleHotkeyPerformed;
                }
            }
        }

        /// <summary>
        /// Enable input actions
        /// </summary>
        private void OnEnable()
        {
            EnableInputActions();
        }

        /// <summary>
        /// Disable input actions
        /// </summary>
        private void OnDisable()
        {
            DisableInputActions();
        }

        private void EnableInputActions()
        {
            _exitAllModesActionRef?.action?.Enable();

            if (_enableHotkeys)
            {
                _moveHotkeyActionRef?.action?.Enable();
                _rotateHotkeyActionRef?.action?.Enable();
                _scaleHotkeyActionRef?.action?.Enable();
            }
        }

        private void DisableInputActions()
        {
            _exitAllModesActionRef?.action?.Disable();
            _moveHotkeyActionRef?.action?.Disable();
            _rotateHotkeyActionRef?.action?.Disable();
            _scaleHotkeyActionRef?.action?.Disable();
        }

        #endregion

        #region Input Action Callbacks

        private void OnExitAllModesPerformed(InputAction.CallbackContext context)
        {
            ExitAllTransformModes();
        }

        private void OnMoveHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetModeForSelectedItems(TransformMode.Move);
            }
        }

        private void OnRotateHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetModeForSelectedItems(TransformMode.Rotate);
            }
        }

        private void OnScaleHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetModeForSelectedItems(TransformMode.Scale);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Called when a scene is unloaded - clean up all scene-specific data
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"TransformControlManager: Cleaning up for unloaded scene '{scene.name}'");
            
            // Clear all registered items
            _activeTransformItems.Clear();
            _currentActiveItem = null;
            
            // Exit all transform modes to clean up any active states
            ExitAllTransformModes();
        }

        /// <summary>
        /// Called when a scene is loaded - register existing draggable items
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"TransformControlManager: Setting up for loaded scene '{scene.name}'");
            // Items will register themselves when they awake, so no need to actively search
        }

        private void OnDestroy()
        {
            // Unsubscribe from scene events
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Unbind events to prevent memory leaks
            if (_exitAllModesAction != null)
            {
                _exitAllModesAction.performed -= OnExitAllModesPerformed;
            }
            if (_moveHotkeyAction != null)
            {
                _moveHotkeyAction.performed -= OnMoveHotkeyPerformed;
            }
            if (_rotateHotkeyAction != null)
            {
                _rotateHotkeyAction.performed -= OnRotateHotkeyPerformed;
            }
            if (_scaleHotkeyAction != null)
            {
                _scaleHotkeyAction.performed -= OnScaleHotkeyPerformed;
            }
        }

        #endregion
    }
}