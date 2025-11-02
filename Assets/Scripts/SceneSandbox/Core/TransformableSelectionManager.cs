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
        [SerializeField] private bool _allowMultipleTransformModeTypes = false;
        [SerializeField] private bool _enableHotkeys = true;

        [Header("Input Action References")]
        [SerializeField] private InputActionReference _exitAllModesActionRef;
        [SerializeField] private InputActionReference _moveHotkeyActionRef;
        [SerializeField] private InputActionReference _rotateHotkeyActionRef;
        [SerializeField] private InputActionReference _scaleHotkeyActionRef;
        [SerializeField] private InputActionReference _transformModeIncreaseActionRef;
        [SerializeField] private InputActionReference _transformModeDecreaseActionRef;
        
        [Header("Pointer Input Actions")]
        [SerializeField] private InputActionReference _pointerPositionActionRef;
        [SerializeField] private InputActionReference _leftClickActionRef;
        [SerializeField] private InputActionReference _rightClickActionRef;
        [SerializeField] private InputActionReference _mouseScrollActionRef;
        
        [Header("Placement Input Actions")]
        [SerializeField] private InputActionReference _cancelPlacementActionRef;

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
        private InputAction _transformModeIncreaseAction;
        private InputAction _transformModeDecreaseAction;
        
        // Pointer Input Actions
        private InputAction _pointerPositionAction;
        private InputAction _leftClickAction;
        private InputAction _rightClickAction;
        private InputAction _mouseScrollAction;
        
        // Placement Input Actions
        private InputAction _cancelPlacementAction;

        // Input state
        private bool _isPointerDown = false;
        private bool _isDragging = false;
        private Vector2 _pointerDownPosition;
        private Vector2 _lastPointerPosition;
        private TransformableItem _currentHoverItem;
        private TransformableItem _currentDragItem;
        private float _lastClickTime = 0f;
        private int _clickCount = 0;

        // Events
        public System.Action<TransformableItem> OnItemSelected;
        public System.Action<TransformableItem> OnItemDeselected;
        public System.Action<List<TransformableItem>> OnSelectionChanged;
        public System.Action<TransformableItem, TransformModeType> OnGlobalTransformModeTypeChanged;

        // Input/Placement Events (for decoupled communication)
        public System.Action<Vector2> OnEmptySpaceClicked; // Click on empty space (can trigger placement confirm)
        public System.Action<Vector2> OnPointerMoved; // Pointer moved (can update placement ghost)
        public System.Action<Vector2> OnPointerDragged; // Pointer dragged (delta for rotation/scale) - DEPRECATED for rotation/scale
        public System.Action<float> OnMouseScrolled; // Mouse scroll wheel (for scale) - DEPRECATED
        public System.Action<TransformModeType> OnTransformModeTypeChangeRequested; // Mode change requested from hotkeys
        public System.Action OnCancelRequested; // Cancel action triggered (ESC, right-click, etc.)
        public System.Action OnTransformModeIncrease; // Increase transform value (rotation/scale step)
        public System.Action OnTransformModeDecrease; // Decrease transform value (rotation/scale step)

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
                // Don't use DontDestroyOnLoad for editor workflow - let it be scene-specific
                // Only persist in builds if needed
                #if !UNITY_EDITOR
                DontDestroyOnLoad(gameObject);
                #endif
                
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
            
            // Get current input position
            Vector2 inputPosition = GetInputPosition();
            
            // Emit pointer moved event for systems that need continuous position updates
            OnPointerMoved?.Invoke(inputPosition);
            
            // Track pointer delta for drag operations
            Vector2 pointerDelta = inputPosition - _lastPointerPosition;
            if (pointerDelta.magnitude > 0.1f && _isDragging)
            {
                OnPointerDragged?.Invoke(pointerDelta);
            }
            _lastPointerPosition = inputPosition;
            
            // Handle scroll wheel input
            if (_mouseScrollAction != null && _mouseScrollAction.enabled)
            {
                float scrollDelta = _mouseScrollAction.ReadValue<Vector2>().y;
                if (Mathf.Abs(scrollDelta) > 0.01f)
                {
                    OnMouseScrolled?.Invoke(scrollDelta);
                }
            }
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
            if (_leftClickAction == null)
            {
                return;
            }

            // Get click state from InputAction
            bool leftButtonDown = _leftClickAction.WasPressedThisFrame();
            bool leftButtonUp = _leftClickAction.WasReleasedThisFrame();
            bool leftButtonHeld = _leftClickAction.IsPressed();
            
            // Right click
            bool rightButtonDown = _rightClickAction != null && _rightClickAction.WasPressedThisFrame();

            // Normal selection handling
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

            // Right click handling removed - no more options form
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
                TransformableItem item = hit.collider.GetComponentInParent<TransformableItem>();
                return item;
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

            // No more long press or drag start handling - TransformableItem simplified
        }

        /// <summary>
        /// Handle pointer drag event - simplified, no object dragging
        /// </summary>
        private void HandlePointerDrag(Vector2 inputPosition)
        {
            float dragDistance = Vector2.Distance(_pointerDownPosition, inputPosition);

            if (!_isDragging && dragDistance > _dragThreshold)
            {
                _isDragging = true;
                // Drag functionality moved to SceneSandboxBuilder's ghost system
            }

            // Invoke pointer dragged event for transform controls
            if (_isDragging)
            {
                Vector2 delta = inputPosition - _pointerDownPosition;
                OnPointerDragged?.Invoke(delta);
            }
        }

        /// <summary>
        /// Handle pointer up event
        /// </summary>
        private void HandlePointerUp(Vector2 inputPosition, TransformableItem hitItem)
        {
            if (!_isDragging)
            {
                // Handle click (no drag occurred)
                if (hitItem != null && hitItem == _currentDragItem)
                {
                    HandleClick(inputPosition, hitItem);
                }
                else if (hitItem == null)
                {
                    // Clicked on empty space - emit event and deselect all
                    OnEmptySpaceClicked?.Invoke(inputPosition);
                    ClearSelection();
                }
            }

            _isPointerDown = false;
            _isDragging = false;
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
            
            // Single click - select the item
            if (_clickCount == 1)
            {
                SelectItem(hitItem);
            }
            
            // Double click - could trigger additional action (e.g., focus/edit mode)
            if (_clickCount >= 2)
            {
                // Could add special double-click behavior here
            }
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
                item.OnTransformModeTypeChanged += OnItemTransformModeTypeChanged;
            }
        }

        /// <summary>
        /// Unregister item from transform control
        /// </summary>
        public void UnregisterItemFromTransformControl(TransformableItem item)
        {
            if (item != null)
            {
                item.OnTransformModeTypeChanged -= OnItemTransformModeTypeChanged;
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
        private void OnItemTransformModeTypeChanged(TransformableItem item, TransformModeType mode)
        {
            if (mode == TransformModeType.None)
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
                if (!_allowMultipleTransformModeTypes)
                {
                    // Exit all other items from transform mode
                    ExitOtherTransformModeTypes(item);
                }

                if (!_activeTransformItems.Contains(item))
                {
                    _activeTransformItems.Add(item);
                }
                _currentActiveTransformItem = item;
            }

            OnGlobalTransformModeTypeChanged?.Invoke(item, mode);
        }

        /// <summary>
        /// Exit transform mode for all items except the specified one
        /// </summary>
        private void ExitOtherTransformModeTypes(TransformableItem exceptItem)
        {
            var itemsToExit = new List<TransformableItem>(_activeTransformItems);
            foreach (var item in itemsToExit)
            {
                if (item != exceptItem && item != null)
                {
                    item.SetTransformModeType(TransformModeType.None);
                }
            }
        }

        /// <summary>
        /// Exit all transform modes
        /// </summary>
        public void ExitAllTransformModeTypes()
        {
            var itemsToExit = new List<TransformableItem>(_activeTransformItems);
            foreach (var item in itemsToExit)
            {
                if (item != null)
                {
                    item.SetTransformModeType(TransformModeType.None);
                }
            }
            _activeTransformItems.Clear();
            _currentActiveTransformItem = null;
        }

        /// <summary>
        /// Set transform mode for all selected items
        /// </summary>
        public void SetModeForSelectedItems(TransformModeType mode)
        {
            foreach (var item in _selectedItems)
            {
                if (item != null && item.EnableTransformControls)
                {
                    item.SetTransformModeType(mode);
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
        public void SetAllowMultipleTransformModeTypes(bool allow)
        {
            _allowMultipleTransformModeTypes = allow;

            // If disabling multiple modes and we have multiple active, keep only the current one
            if (!allow && _activeTransformItems.Count > 1 && _currentActiveTransformItem != null)
            {
                ExitOtherTransformModeTypes(_currentActiveTransformItem);
            }
        }

        /// <summary>
        /// Get statistics about current transform state
        /// </summary>
        public string GetTransformStateInfo()
        {
            return $"Active Transform Items: {_activeTransformItems.Count}, " +
                   $"Current Active: {(_currentActiveTransformItem ? _currentActiveTransformItem.name : "None")}, " +
                   $"Multiple Modes: {_allowMultipleTransformModeTypes}";
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
                
                if (_transformModeIncreaseActionRef != null)
                {
                    _transformModeIncreaseAction = _transformModeIncreaseActionRef.action;
                    _transformModeIncreaseAction.performed += OnTransformModeIncreasePerformed;
                }
                
                if (_transformModeDecreaseActionRef != null)
                {
                    _transformModeDecreaseAction = _transformModeDecreaseActionRef.action;
                    _transformModeDecreaseAction.performed += OnTransformModeDecreasePerformed;
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
            
            if (_mouseScrollActionRef != null)
            {
                _mouseScrollAction = _mouseScrollActionRef.action;
            }

            // Initialize placement input actions
            if (_cancelPlacementActionRef != null)
            {
                _cancelPlacementAction = _cancelPlacementActionRef.action;
                _cancelPlacementAction.performed += OnCancelPlacementPerformed;
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
                _transformModeIncreaseAction?.Enable();
                _transformModeDecreaseAction?.Enable();
            }
            
            // Enable pointer input actions
            _pointerPositionAction?.Enable();
            _leftClickAction?.Enable();
            _rightClickAction?.Enable();
            _mouseScrollAction?.Enable();

            // Enable placement input actions
            _cancelPlacementAction?.Enable();
        }

        private void DisableInputActions()
        {
            _exitAllModesActionRef?.action?.Disable();
            _moveHotkeyActionRef?.action?.Disable();
            _rotateHotkeyActionRef?.action?.Disable();
            _scaleHotkeyActionRef?.action?.Disable();
            _transformModeIncreaseAction?.Disable();
            _transformModeDecreaseAction?.Disable();
            
            // Disable pointer input actions
            _pointerPositionAction?.Disable();
            _leftClickAction?.Disable();
            _rightClickAction?.Disable();
            _mouseScrollAction?.Disable();

            // Disable placement input actions
            _cancelPlacementAction?.Disable();
        }

        #endregion

        #region Input Action Callbacks

        private void OnExitAllModesPerformed(InputAction.CallbackContext context)
        {
            ExitAllTransformModeTypes();
        }

        private void OnMoveHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetModeForSelectedItems(TransformModeType.Position);
                OnTransformModeTypeChangeRequested?.Invoke(TransformModeType.Position);
            }
        }

        private void OnRotateHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetModeForSelectedItems(TransformModeType.Rotation);
                OnTransformModeTypeChangeRequested?.Invoke(TransformModeType.Rotation);
            }
        }

        private void OnScaleHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetModeForSelectedItems(TransformModeType.Scale);
                OnTransformModeTypeChangeRequested?.Invoke(TransformModeType.Scale);
            }
        }

        private void OnTransformModeIncreasePerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                OnTransformModeIncrease?.Invoke();
                
                // Also directly call increase on selected item if available
                if (_currentActiveTransformItem != null)
                {
                    _currentActiveTransformItem.IncreaseTransformValue();
                }
            }
        }

        private void OnTransformModeDecreasePerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                OnTransformModeDecrease?.Invoke();
                
                // Also directly call decrease on selected item if available
                if (_currentActiveTransformItem != null)
                {
                    _currentActiveTransformItem.DecreaseTransformValue();
                }
            }
        }

        private void OnCancelPlacementPerformed(InputAction.CallbackContext context)
        {
            OnCancelRequested?.Invoke();
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
            
            if (item == null || _selectedItems.Contains(item))
            {
                return;
            }

            // If multiple selection is not allowed, deselect all other items
            if (!_allowMultipleSelection && _selectedItems.Count > 0)
            {
                ClearSelection();
            }

            _selectedItems.Add(item);
            _lastSelectedItem = item;

            // Ensure the item's visual state is updated
            item.SetSelectedState(true);

            OnItemSelected?.Invoke(item);
            OnSelectionChanged?.Invoke(SelectedItems);
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
            // Clear selection state
            ClearSelection();
            
            // Clear transform control state
            _activeTransformItems.Clear();
            _currentActiveTransformItem = null;
            ExitAllTransformModeTypes();
            
            // Clear input state
            _isPointerDown = false;
            _isDragging = false;
            _currentHoverItem = null;
            _currentDragItem = null;
            
            // In editor mode, if this was the last scene, clear the instance
            #if UNITY_EDITOR
            if (SceneManager.sceneCount == 0 || !Application.isPlaying)
            {
                if (_instance == this)
                {
                    _instance = null;
                }
            }
            #endif
        }

        /// <summary>
        /// Called when a scene is loaded - register existing draggable items
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Re-register items in the new scene
            RegisterExistingDraggableItems();
        }

        private void OnDestroy()
        {
            // Clean up selection state
            ClearSelection();
            ExitAllTransformModeTypes();
            
            // Unsubscribe from scene events
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Disable input actions before unbinding
            DisableInputActions();
            
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
            if (_cancelPlacementAction != null)
            {
                _cancelPlacementAction.performed -= OnCancelPlacementPerformed;
            }
            
            // Clear instance reference
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}