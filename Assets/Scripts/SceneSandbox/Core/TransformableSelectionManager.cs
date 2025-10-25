using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Manages selection state for draggable items in the scene sandbox
    /// Handles raycast-based input detection and interaction with TransformableItems
    /// Ensures only one item is selected at a time and provides selection events
    /// </summary>
    public class TransformableSelectionManager : MonoBehaviour
    {
        private static TransformableSelectionManager _instance;
        public static TransformableSelectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TransformableSelectionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SelectionManager");
                        _instance = go.AddComponent<TransformableSelectionManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Selection Settings")]
        [SerializeField] private bool _allowMultipleSelection = false;

        [Header("Transform Control Settings")]
        [SerializeField] private bool _allowMultipleTransformModes = false;
        [SerializeField] private bool _enableHotkeys = true;

        [Header("Input Action References")]
        [SerializeField] private InputActionReference _exitAllModesActionRef;
        [SerializeField] private InputActionReference _moveHotkeyActionRef;
        [SerializeField] private InputActionReference _rotateHotkeyActionRef;
        [SerializeField] private InputActionReference _scaleHotkeyActionRef;
        
        [Header("Pointer Input Actions")]
        [SerializeField] private InputActionReference _pointerPositionActionRef;
        [SerializeField] private InputActionReference _leftClickActionRef;
        [SerializeField] private InputActionReference _rightClickActionRef;

        [Header("Raycast Settings")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private LayerMask _raycastLayers = -1;
        [SerializeField] private float _maxRaycastDistance = 1000f;
        [SerializeField] private float _dragThreshold = 5f; // Pixels before considering it a drag
        [SerializeField] private float _doubleClickTime = 0.3f; // Time window for double click

        // Current selection
        [SerializeField]private readonly List<TransformableItem> _selectedItems = new List<TransformableItem>();
        [SerializeField] private TransformableItem _lastSelectedItem;

        // Transform control state
        private List<TransformableItem> _activeTransformItems = new List<TransformableItem>();
        [SerializeField] private TransformableItem _currentActiveTransformItem;

        // Input Actions
        private InputAction _exitAllModesAction;
        private InputAction _moveHotkeyAction;
        private InputAction _rotateHotkeyAction;
        private InputAction _scaleHotkeyAction;
        
        // Pointer Input Actions
        private InputAction _pointerPositionAction;
        private InputAction _leftClickAction;
        private InputAction _rightClickAction;

        // Input state
        private bool _isPointerDown = false;
        private bool _isDragging = false;
        private Vector2 _pointerDownPosition;
        private TransformableItem _currentHoverItem;
        private TransformableItem _currentDragItem;
        private float _lastClickTime = 0f;
        private int _clickCount = 0;

        // Events
        public System.Action<TransformableItem> OnItemSelected;
        public System.Action<TransformableItem> OnItemDeselected;
        public System.Action<List<TransformableItem>> OnSelectionChanged;
        public System.Action<TransformableItem, TransformMode> OnGlobalTransformModeChanged;

        // Properties
        public TransformableItem SelectedItem => _selectedItems.Count > 0 ? _selectedItems[0] : null;
        public List<TransformableItem> SelectedItems => new List<TransformableItem>(_selectedItems);
        public bool HasSelection => _selectedItems.Count > 0;
        public int SelectionCount => _selectedItems.Count;

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
                return;
            }

            // Initialize camera
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            }
        }

        private void Start()
        {
            // Find all existing draggable items and register for their events
            RegisterExistingDraggableItems();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void Update()
        {
            HandleRaycastInput();
        }

        #region Raycast Input Handling

        /// <summary>
        /// Handle raycast-based input detection
        /// </summary>
        private void HandleRaycastInput()
        {
            if (_mainCamera == null) return;

            // Get input position from InputAction
            Vector2 inputPosition = GetInputPosition();
            
            // Perform raycast to detect items
            TransformableItem hitItem = RaycastForItem(inputPosition);

            // Handle hover state
            HandleHoverState(hitItem);

            // Check if left click action exists and is enabled
            if (_leftClickAction == null) return;

            // Get click state from InputAction
            bool leftButtonDown = _leftClickAction.WasPressedThisFrame();
            bool leftButtonUp = _leftClickAction.WasReleasedThisFrame();
            bool leftButtonHeld = _leftClickAction.IsPressed();
            
            // Right click
            bool rightButtonDown = _rightClickAction != null && _rightClickAction.WasPressedThisFrame();

            // Pointer down
            if (leftButtonDown)
            {
                HandlePointerDown(inputPosition, hitItem);
            }

            // Pointer drag
            if (leftButtonHeld && _isPointerDown)
            {
                HandlePointerDrag(inputPosition);
            }

            // Pointer up
            if (leftButtonUp && _isPointerDown)
            {
                HandlePointerUp(inputPosition, hitItem);
            }

            // Right click
            if (rightButtonDown && hitItem != null)
            {
                hitItem.OnRaycastClick(inputPosition, 1, true);
            }
        }

        /// <summary>
        /// Get current input position from InputAction
        /// </summary>
        private Vector2 GetInputPosition()
        {
            if (_pointerPositionAction != null && _pointerPositionAction.enabled)
            {
                return _pointerPositionAction.ReadValue<Vector2>();
            }
            
            // Fallback to mouse position if action not configured
            return Mouse.current?.position.ReadValue() ?? Vector2.zero;
        }

        /// <summary>
        /// Perform raycast to detect TransformableItem
        /// </summary>
        private TransformableItem RaycastForItem(Vector2 screenPosition)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, _raycastLayers))
            {
                // Try to get TransformableItem from hit object or its parents
                return hit.collider.GetComponentInParent<TransformableItem>();
            }

            return null;
        }

        /// <summary>
        /// Handle hover state changes
        /// </summary>
        private void HandleHoverState(TransformableItem hitItem)
        {
            if (_currentHoverItem != hitItem)
            {
                // Clear previous hover
                if (_currentHoverItem != null)
                {
                    _currentHoverItem.SetHoverState(false);
                }

                // Set new hover
                _currentHoverItem = hitItem;
                if (_currentHoverItem != null)
                {
                    _currentHoverItem.SetHoverState(true);
                }
            }
        }

        /// <summary>
        /// Handle pointer down event
        /// </summary>
        private void HandlePointerDown(Vector2 inputPosition, TransformableItem hitItem)
        {
            _isPointerDown = true;
            _pointerDownPosition = inputPosition;
            _currentDragItem = hitItem;

            if (hitItem != null)
            {
                // Notify item of pointer down (for long press detection)
                hitItem.OnRaycastPointerDown(inputPosition, true);
            }
        }

        /// <summary>
        /// Handle pointer drag event
        /// </summary>
        private void HandlePointerDrag(Vector2 inputPosition)
        {
            float dragDistance = Vector2.Distance(_pointerDownPosition, inputPosition);

            if (!_isDragging && dragDistance > _dragThreshold)
            {
                // Start dragging
                _isDragging = true;

                if (_currentDragItem != null)
                {
                    Debug.Log($"[SelectionManager] Starting drag on {_currentDragItem.name}");
                    _currentDragItem.OnRaycastDragStart(_pointerDownPosition);
                }
            }

            if (_isDragging && _currentDragItem != null)
            {
                _currentDragItem.OnRaycastDrag(inputPosition);
            }
        }

        /// <summary>
        /// Handle pointer up event
        /// </summary>
        private void HandlePointerUp(Vector2 inputPosition, TransformableItem hitItem)
        {
            if (_currentDragItem != null)
            {
                _currentDragItem.OnRaycastPointerUp(inputPosition);
            }

            if (_isDragging)
            {
                // End drag
                if (_currentDragItem != null)
                {
                    Debug.Log($"[SelectionManager] Ending drag on {_currentDragItem.name}");
                    _currentDragItem.OnRaycastDragEnd(inputPosition);
                }
                _isDragging = false;
            }
            else
            {
                // Handle click (no drag occurred)
                if (hitItem != null && hitItem == _currentDragItem)
                {
                    HandleClick(inputPosition, hitItem);
                }
                else if (hitItem == null)
                {
                    // Clicked on empty space - deselect all
                    ClearSelection();
                }
            }

            _isPointerDown = false;
            _currentDragItem = null;
        }

        /// <summary>
        /// Handle click with double-click detection
        /// </summary>
        private void HandleClick(Vector2 inputPosition, TransformableItem hitItem)
        {
            float timeSinceLastClick = Time.time - _lastClickTime;

            if (timeSinceLastClick <= _doubleClickTime)
            {
                _clickCount++;
            }
            else
            {
                _clickCount = 1;
            }

            _lastClickTime = Time.time;

            Debug.Log($"[SelectionManager] Click on {hitItem.name}, count={_clickCount}");
            hitItem.OnRaycastClick(inputPosition, _clickCount, false);
        }

        #endregion

        #region Transform Control Management

        /// <summary>
        /// Register item for transform control
        /// </summary>
        public void RegisterItemForTransformControl(TransformableItem item)
        {
            if (item != null)
            {
                item.OnTransformModeChanged += OnItemTransformModeChanged;
            }
        }

        /// <summary>
        /// Unregister item from transform control
        /// </summary>
        public void UnregisterItemFromTransformControl(TransformableItem item)
        {
            if (item != null)
            {
                item.OnTransformModeChanged -= OnItemTransformModeChanged;
                _activeTransformItems.Remove(item);

                if (_currentActiveTransformItem == item)
                {
                    _currentActiveTransformItem = null;
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
                if (_currentActiveTransformItem == item)
                {
                    _currentActiveTransformItem = null;
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
                _currentActiveTransformItem = item;
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
            _currentActiveTransformItem = null;
        }

        /// <summary>
        /// Set transform mode for all selected items
        /// </summary>
        public void SetModeForSelectedItems(TransformMode mode)
        {
            foreach (var item in _selectedItems)
            {
                if (item != null && item.EnableTransformControls)
                {
                    item.SetTransformMode(mode);
                }
            }
        }

        /// <summary>
        /// Get the currently active transform item
        /// </summary>
        public TransformableItem GetCurrentActiveTransformItem()
        {
            return _currentActiveTransformItem;
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
            if (!allow && _activeTransformItems.Count > 1 && _currentActiveTransformItem != null)
            {
                ExitOtherTransformModes(_currentActiveTransformItem);
            }
        }

        /// <summary>
        /// Get statistics about current transform state
        /// </summary>
        public string GetTransformStateInfo()
        {
            return $"Active Transform Items: {_activeTransformItems.Count}, " +
                   $"Current Active: {(_currentActiveTransformItem ? _currentActiveTransformItem.name : "None")}, " +
                   $"Multiple Modes: {_allowMultipleTransformModes}";
        }

        #endregion

        #region Input Actions Setup

        /// <summary>
        /// Initialize input actions and bind events
        /// </summary>
        private void InitializeInputActions()
        {
            // Subscribe to transform mode hotkey callbacks
            if (_exitAllModesActionRef != null)
            {
                _exitAllModesActionRef.action.performed += OnExitAllModesPerformed;
            }

            if (_enableHotkeys)
            {
                if (_moveHotkeyActionRef != null)
                {
                    _moveHotkeyActionRef.action.performed += OnMoveHotkeyPerformed;
                }

                if (_rotateHotkeyActionRef != null)
                {
                    _rotateHotkeyActionRef.action.performed += OnRotateHotkeyPerformed;
                }

                if (_scaleHotkeyActionRef != null)
                {
                    _scaleHotkeyActionRef.action.performed += OnScaleHotkeyPerformed;
                }
            }
            
            // Initialize pointer input actions
            if (_pointerPositionActionRef != null)
            {
                _pointerPositionAction = _pointerPositionActionRef.action;
            }
            
            if (_leftClickActionRef != null)
            {
                _leftClickAction = _leftClickActionRef.action;
            }
            
            if (_rightClickActionRef != null)
            {
                _rightClickAction = _rightClickActionRef.action;
            }
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
            
            // Enable pointer input actions
            _pointerPositionAction?.Enable();
            _leftClickAction?.Enable();
            _rightClickAction?.Enable();
        }

        private void DisableInputActions()
        {
            _exitAllModesActionRef?.action?.Disable();
            _moveHotkeyActionRef?.action?.Disable();
            _rotateHotkeyActionRef?.action?.Disable();
            _scaleHotkeyActionRef?.action?.Disable();
            
            // Disable pointer input actions
            _pointerPositionAction?.Disable();
            _leftClickAction?.Disable();
            _rightClickAction?.Disable();
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

        /// <summary>
        /// Register for events on all existing draggable items in the scene
        /// </summary>
        private void RegisterExistingDraggableItems()
        {
            var existingItems = FindObjectsByType<TransformableItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var item in existingItems)
            {
                RegisterDraggableItem(item);
            }
        }

        /// <summary>
        /// Register a draggable item for selection management
        /// </summary>
        /// <param name="item">The draggable item to register</param>
        public void RegisterDraggableItem(TransformableItem item)
        {
            if (item == null) return;

            // Subscribe to selection events
            item.OnSelectionChanged -= OnDraggableItemSelectionChanged;
            item.OnSelectionChanged += OnDraggableItemSelectionChanged;

            // Register for transform control if enabled
            if (item.EnableTransformControls)
            {
                RegisterItemForTransformControl(item);
            }
        }

        /// <summary>
        /// Unregister a draggable item from selection management
        /// </summary>
        /// <param name="item">The draggable item to unregister</param>
        public void UnregisterDraggableItem(TransformableItem item)
        {
            if (item == null) return;

            // Unsubscribe from events
            item.OnSelectionChanged -= OnDraggableItemSelectionChanged;

            // Unregister from transform control
            UnregisterItemFromTransformControl(item);

            // Remove from selection if it was selected
            if (_selectedItems.Contains(item))
            {
                _selectedItems.Remove(item);
                OnItemDeselected?.Invoke(item);
                OnSelectionChanged?.Invoke(SelectedItems);
            }
        }

        /// <summary>
        /// Handle selection state change from a draggable item
        /// </summary>
        /// <param name="item">The item whose selection changed</param>
        /// <param name="isSelected">Whether the item is now selected</param>
        private void OnDraggableItemSelectionChanged(TransformableItem item, bool isSelected)
        {
            if (isSelected)
            {
                SelectItem(item);
            }
            else
            {
                DeselectItem(item);
            }
        }

        /// <summary>
        /// Select an item
        /// </summary>
        /// <param name="item">The item to select</param>
        public void SelectItem(TransformableItem item)
        {
            Debug.Log($"[SelectionManager] SelectItem called for {(item != null ? item.name : "null")}");
            
            if (item == null || _selectedItems.Contains(item))
            {
                Debug.Log($"[SelectionManager] SelectItem aborted - item is null or already selected");
                return;
            }

            // If multiple selection is not allowed, deselect all other items
            if (!_allowMultipleSelection && _selectedItems.Count > 0)
            {
                Debug.Log($"[SelectionManager] Clearing previous selection");
                ClearSelection();
            }

            _selectedItems.Add(item);
            _lastSelectedItem = item;

            // Ensure the item's visual state is updated
            Debug.Log($"[SelectionManager] Setting selected state for {item.name}");
            item.SetSelectedState(true);

            OnItemSelected?.Invoke(item);
            OnSelectionChanged?.Invoke(SelectedItems);

            Debug.Log($"[SelectionManager] Successfully selected item: {item.name}");
        }

        /// <summary>
        /// Deselect an item
        /// </summary>
        /// <param name="item">The item to deselect</param>
        public void DeselectItem(TransformableItem item)
        {
            if (item == null || !_selectedItems.Contains(item)) return;

            _selectedItems.Remove(item);

            // Ensure the item's visual state is updated
            item.SetSelectedState(false);

            OnItemDeselected?.Invoke(item);
            OnSelectionChanged?.Invoke(SelectedItems);

            if (_lastSelectedItem == item)
            {
                _lastSelectedItem = _selectedItems.Count > 0 ? _selectedItems[_selectedItems.Count - 1] : null;
            }

            Debug.Log($"Deselected item: {item.name}");
        }

        /// <summary>
        /// Clear all selection
        /// </summary>
        public void ClearSelection()
        {
            var itemsToDeselect = new List<TransformableItem>(_selectedItems);
            
            foreach (var item in itemsToDeselect)
            {
                if (item != null)
                {
                    item.SetSelectedState(false);
                }
            }

            _selectedItems.Clear();
            _lastSelectedItem = null;

            foreach (var item in itemsToDeselect)
            {
                if (item != null)
                {
                    OnItemDeselected?.Invoke(item);
                }
            }

            OnSelectionChanged?.Invoke(SelectedItems);

            Debug.Log("Cleared all selection");
        }

        /// <summary>
        /// Toggle selection state of an item
        /// </summary>
        /// <param name="item">The item to toggle</param>
        public void ToggleSelection(TransformableItem item)
        {
            if (item == null) return;

            if (_selectedItems.Contains(item))
            {
                DeselectItem(item);
            }
            else
            {
                SelectItem(item);
            }
        }

        /// <summary>
        /// Check if an item is selected
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if the item is selected</returns>
        public bool IsSelected(TransformableItem item)
        {
            return item != null && _selectedItems.Contains(item);
        }

        /// <summary>
        /// Delete all selected items
        /// </summary>
        public void DeleteSelectedItems()
        {
            if (_selectedItems.Count == 0) return;

            var itemsToDelete = new List<TransformableItem>(_selectedItems);
            ClearSelection();

            foreach (var item in itemsToDelete)
            {
                if (item != null && item.gameObject != null)
                {
                    Debug.Log($"Deleting selected item: {item.name}");
                    
                    if (Application.isPlaying)
                    {
                        Destroy(item.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(item.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Called when a scene is unloaded - clean up all scene-specific data
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"TransformableSelectionManager: Cleaning up for unloaded scene '{scene.name}'");
            ClearSelection();
            
            // Clear transform control state
            _activeTransformItems.Clear();
            _currentActiveTransformItem = null;
            ExitAllTransformModes();
        }

        /// <summary>
        /// Called when a scene is loaded - register existing draggable items
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"TransformableSelectionManager: Registering items in loaded scene '{scene.name}'");
            // Re-register items in the new scene
            RegisterExistingDraggableItems();
        }

        private void OnDestroy()
        {
            // Unsubscribe from scene events
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Unbind input action events
            if (_exitAllModesActionRef != null && _exitAllModesActionRef.action != null)
            {
                _exitAllModesActionRef.action.performed -= OnExitAllModesPerformed;
            }
            if (_moveHotkeyActionRef != null && _moveHotkeyActionRef.action != null)
            {
                _moveHotkeyActionRef.action.performed -= OnMoveHotkeyPerformed;
            }
            if (_rotateHotkeyActionRef != null && _rotateHotkeyActionRef.action != null)
            {
                _rotateHotkeyActionRef.action.performed -= OnRotateHotkeyPerformed;
            }
            if (_scaleHotkeyActionRef != null && _scaleHotkeyActionRef.action != null)
            {
                _scaleHotkeyActionRef.action.performed -= OnScaleHotkeyPerformed;
            }
            
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            // Show selection info in the corner for debugging
            GUI.Box(new Rect(10, 10, 200, 60), "");
            GUI.Label(new Rect(15, 15, 190, 20), $"Selected Items: {_selectedItems.Count}");
            
            if (_lastSelectedItem != null)
            {
                GUI.Label(new Rect(15, 35, 190, 20), $"Last: {_lastSelectedItem.name}");
            }
        }
        #endif
    }
}