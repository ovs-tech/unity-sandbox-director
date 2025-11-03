using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SceneSandbox.Data;
using SceneSandbox.Serialization;
using MiniTimeline.Core;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Main controller for the Scene Sandbox Builder system
    /// Manages the placement, interaction, and organization of scene objects
    /// </summary>
    public class SceneSandboxBuilder : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SceneObjectLibrary _objectLibrary;
        [SerializeField] private Transform _sceneRoot;
        [SerializeField] private Transform _stageArea;

        [Header("Input Action References")]
        [SerializeField] private InputActionReference _toggleModeActionRef;
        [SerializeField] private InputActionReference _exitAllModesActionRef;
        [SerializeField] private InputActionReference _moveHotkeyActionRef;
        [SerializeField] private InputActionReference _rotateHotkeyActionRef;
        [SerializeField] private InputActionReference _scaleHotkeyActionRef;
        [SerializeField] private InputActionReference _transformModeIncreaseActionRef;
        [SerializeField] private InputActionReference _transformModeDecreaseActionRef;
        [SerializeField] private InputActionReference _transformModeToggleAxisActionRef;
        
        [Header("Pointer Input Actions")]
        [SerializeField] private InputActionReference _pointerPositionActionRef;
        [SerializeField] private InputActionReference _leftClickActionRef;
        [SerializeField] private InputActionReference _rightClickActionRef;
        [SerializeField] private InputActionReference _mouseScrollActionRef;
        
        [Header("Placement Input Actions")]
        [SerializeField] private InputActionReference _cancelPlacementActionRef;

        [Header("Raycast & Input Settings")]
        [SerializeField] private float _maxRaycastDistance = 1000f;
        [SerializeField] private float _dragThreshold = 5f;
        [SerializeField] private float _doubleClickTime = 0.3f;
        [SerializeField] private bool _enableHotkeys = true;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask _placementLayers = -1;
        [SerializeField] private LayerMask _selectionLayers = -1;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private float _gridSize = 1f;
        [SerializeField] private Vector3 _gridOffset = Vector3.zero; // Grid offset from stage area (only X, Z used for 2D plane)
        [SerializeField] private PivotPoint _gridPivotOffset = PivotPoint.Center; // Grid pivot offset relative to scene bounds pivot
        [SerializeField] private float _defaultPlacementHeight = 0f;
        [SerializeField] private float _minimumPlacementHeight = 0f; // Minimum Y position for placement
        [SerializeField] private bool _useRaycastForPlacement = true;
        [SerializeField] private bool _useConsistentHeight = false; // Force consistent Y height for indicator
        [SerializeField] private Vector3 _sceneBounds = new Vector3(20f, 10f, 20f);
        [SerializeField] private Vector3 _sceneBoundsOffset = Vector3.zero; // Offset from stage area center
        [SerializeField] private PivotPoint _sceneBoundsPivot = PivotPoint.Center; // Scene bounds pivot point
        [SerializeField] private bool _enableDropIndicator = true;
        [SerializeField] private GameObject _dropIndicatorPrefab;
        [SerializeField] private Color _validDropColor = Color.green;
        [SerializeField] private Color _invalidDropColor = Color.red;
        [SerializeField] private float _dropIndicatorSize = 1f;

        [Header("Placement Preview Settings")]
        [SerializeField] private bool _enableGhostPreview = true; // DEPRECATED - kept for compatibility
        [SerializeField] private Color _validGhostColor = new Color(0f, 1f, 0f, 0.5f); // DEPRECATED
        [SerializeField] private Color _invalidGhostColor = new Color(1f, 0f, 0f, 0.5f); // DEPRECATED
        [SerializeField] private bool _showGridSnapIndicator = true;
        [SerializeField] private bool _showSurfaceNormal = true;
        [SerializeField] private float _surfaceNormalLength = 1f;
        
        [Header("Transform Mode Colors")]
        [SerializeField] private Color _positionModeColor = new Color(0f, 1f, 1f, 0.5f); // Cyan
        [SerializeField] private Color _rotationModeColor = new Color(1f, 1f, 0f, 0.5f); // Yellow
        [SerializeField] private Color _scaleModeColor = new Color(1f, 0f, 1f, 0.5f); // Magenta
        
        [Header("Selection & Edit Settings")]
        [SerializeField] private bool _autoEditOnSelect = true; // Auto start edit mode when object selected

        [Header("Placement Validation")]
        [SerializeField] private bool _checkCollisions = true;
        [SerializeField] private LayerMask _collisionLayers = -1;
        [SerializeField] private LayerMask _groundLayers = 0; // Layers to ignore in collision check (ground, floor, etc.)
        [SerializeField] private bool _ignoreStaticObjects = true; // Ignore static objects in collision check
        [SerializeField] private bool _requireSurfaceBelow = false;
        [SerializeField] private bool _enableCostSystem = false;
        [SerializeField] private int _placementCost = 10; // Placeholder for future resource system

        [Header("Transform Input Settings")]
        [SerializeField] private float _rotationSensitivity = 1.0f; // Degrees per pixel
        [SerializeField] private float _scaleSensitivity = 0.01f; // Scale units per pixel
        [SerializeField] private float _scrollScaleSensitivity = 0.1f; // Scale units per scroll notch
        [SerializeField] private Vector3 _minScale = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 _maxScale = new Vector3(10f, 10f, 10f);

        [Header("Preview Settings")]
        [SerializeField] private MiniTimelineDirector _timelineDirector;
        [SerializeField] private bool _autoPreview = false;
        [SerializeField] private float _previewDuration = 10f;

        [Header("Gizmo Settings")]
        [SerializeField] private bool _enableGizmos = true;
        [SerializeField] private bool _showBoundsGizmo = true;
        [SerializeField] private bool _showAxesGizmo = true;
        [SerializeField] private bool _showHandlesGizmo = true;
        [SerializeField] private Color _gizmoBoundsColor = Color.yellow;
        [SerializeField] private float _gizmoAxisLength = 1f;

        [Header("Scene Gizmo Settings")]
        [SerializeField] private bool _enableSceneGizmos = true;
        [SerializeField] private bool _showSceneGrid = true;
        [SerializeField] private bool _showSceneBounds = true;
        [SerializeField] private bool _showStageAreaGizmo = true;
        [SerializeField] private bool _showPlacementHeightGizmo = true;
        [SerializeField] private Color _sceneBoundsColor = Color.cyan;
        [SerializeField] private Color _placementHeightColor = Color.yellow;

        [Header("Save/Load")]
        [Tooltip("Scene save path (Read-Only). Automatically set to: persistentDataPath/SceneSandboxBuilder/SavedScenes")]
        [SerializeField] private string _defaultSavePath = ""; // Force initialized to Application.persistentDataPath/SceneSandboxBuilder/SavedScenes
        [Tooltip("Name for the current scene configuration")]
        [SerializeField] private string _currentSceneName = "Untitled Scene";
        [Tooltip("Project save path (Read-Only). Automatically set to: persistentDataPath/SceneSandboxBuilder/SavedProjects")]
        [SerializeField] private string _defaultProjectSavePath = ""; // Force initialized to Application.persistentDataPath/SceneSandboxBuilder/SavedProjects

        // Components
        private Camera _sceneCamera;

        // Input Actions
        private InputAction _toggleModeAction;
        private InputAction _exitAllModesAction;
        private InputAction _moveHotkeyAction;
        private InputAction _rotateHotkeyAction;
        private InputAction _scaleHotkeyAction;
        private InputAction _transformModeIncreaseAction;
        private InputAction _transformModeDecreaseAction;
        private InputAction _transformModeToggleAxisAction;
        private InputAction _pointerPositionAction;
        private InputAction _leftClickAction;
        private InputAction _rightClickAction;
        private InputAction _mouseScrollAction;
        private InputAction _cancelPlacementAction;

        // Mode state
        private SandboxMode _currentMode = SandboxMode.Build;

        // Input state
        private bool _isPointerDown = false;
        private bool _isDragging = false;
        private Vector2 _pointerDownPosition;
        private Vector2 _lastPointerPosition;
        private TransformableItem _currentHoverItem;
        private TransformableItem _currentDragItem;
        private float _lastClickTime = 0f;
        private int _clickCount = 0;

        // Selection state
        private readonly List<TransformableItem> _selectedItems = new List<TransformableItem>();
        private TransformableItem _lastSelectedItem;

        // Transform control state
        private List<TransformableItem> _activeTransformItems = new List<TransformableItem>();
        private TransformableItem _currentActiveTransformItem;

        // State
        private SceneConfiguration _currentScene;
        private SandboxProjectData _currentProject;
        private Dictionary<string, GameObject> _placedObjects;
        private GameObject _selectedObject;
        private List<GameObject> _previewObjects;

        // Drop Indicator State
        private GameObject _currentDropIndicator;
        private bool _isDraggingObject = false;
        private Vector3 _dragPreviewPosition;

        // Placement & Transform State
        private PlacementState _placementState = PlacementState.Idle;
        private TransformModeType _currentTransformMode = TransformModeType.Position;
        private TransformAxis _currentTransformAxis = TransformAxis.All;
        private GameObject _currentPlacementObject; // The actual object being placed (no ghost)
        private string _currentPlacementObjectId;
        private Vector3 _currentPlacementPosition;
        private Quaternion _currentPlacementRotation = Quaternion.identity;
        private Vector3 _currentPlacementScale = Vector3.one;
        private bool _isCurrentPlacementValid;
        private List<Material> _originalMaterials = new List<Material>();
        private List<Material> _ghostMaterials = new List<Material>();
        private Vector3 _lastValidSurfaceNormal = Vector3.up;
        
        // Selection Edit State (for direct editing of existing objects)
        private GameObject _selectedObjectForEdit; // Original object being edited
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;
        private Dictionary<Renderer, Material[]> _originalObjectMaterials; // Store original materials to restore

        // Events
        public System.Action<SceneConfiguration> OnSceneLoaded;
        public System.Action<SceneConfiguration> OnSceneSaved;
        public System.Action<GameObject> OnObjectPlaced;
        public System.Action<GameObject> OnObjectRemoved;
        public System.Action<GameObject> OnObjectSelected;
        public System.Action OnSceneCleared;
        public System.Action<bool> OnPreviewStateChanged;
        public System.Action<SandboxMode> OnModeChanged;

        // Properties
        public SceneConfiguration CurrentScene => _currentScene;
        public SandboxProjectData CurrentProject => _currentProject;
        public SceneObjectLibrary ObjectLibrary => _objectLibrary;
        public bool IsInPreviewMode => _previewObjects?.Count > 0;
        public GameObject SelectedObject => _selectedObject;
        public bool GizmosEnabled => _enableGizmos;
        public bool SceneGizmosEnabled => _enableSceneGizmos;
        public Vector3 SceneBounds => _sceneBounds;
        public Vector3 SceneBoundsOffset => _sceneBoundsOffset;
        public PivotPoint SceneBoundsPivot => _sceneBoundsPivot;
        public Vector3 GridOffset => _gridOffset;
        public PivotPoint GridPivotOffset => _gridPivotOffset;
        public bool DropIndicatorEnabled => _enableDropIndicator;
        public PlacementState CurrentPlacementState => _placementState;
        public bool IsPlacementActive => _placementState == PlacementState.Active;
        public TransformModeType CurrentTransformMode => _currentTransformMode;
        public TransformAxis CurrentTransformAxis => _currentTransformAxis;
        public SandboxMode CurrentMode => _currentMode;
        public bool IsInBuildMode => _currentMode == SandboxMode.Build;

        /// <summary>
        /// Set the current transform mode (Position/Rotation/Scale)
        /// </summary>
        public void SetTransformMode(TransformModeType mode)
        {
            _currentTransformMode = mode;
            
            // Reset axis to All when changing mode or entering Position mode
            if (mode == TransformModeType.Position || mode == TransformModeType.None)
            {
                _currentTransformAxis = TransformAxis.All;
            }
            
            // Update active transform items with new axis
            UpdateActiveTransformItemsAxis();
            
            // Update visual feedback if in placement mode
            if (IsPlacementActive && _currentPlacementObject != null)
            {
                UpdateGhostTransformMode();
            }
        }

        /// <summary>
        /// Toggle the current transform axis (X, Y, Z, All)
        /// Only applicable for Rotation and Scale modes
        /// </summary>
        public void ToggleTransformAxis()
        {
            // Only allow axis toggle for Rotation and Scale modes
            if (_currentTransformMode != TransformModeType.Rotation && _currentTransformMode != TransformModeType.Scale)
            {
                Debug.Log("[AXIS] Axis toggle only available in Rotation or Scale mode");
                return;
            }

            // Cycle through axes: All -> X -> Y -> Z -> All
            _currentTransformAxis = _currentTransformAxis switch
            {
                TransformAxis.All => TransformAxis.X,
                TransformAxis.X => TransformAxis.Y,
                TransformAxis.Y => TransformAxis.Z,
                TransformAxis.Z => TransformAxis.All,
                _ => TransformAxis.All
            };

            Debug.Log($"[AXIS] Transform axis changed to: {_currentTransformAxis}");
            
            // Update active transform items with new axis
            UpdateActiveTransformItemsAxis();
        }

        /// <summary>
        /// Update all active transform items with current axis selection
        /// </summary>
        private void UpdateActiveTransformItemsAxis()
        {
            foreach (var item in _activeTransformItems)
            {
                if (item != null)
                {
                    item.SetTransformAxis(_currentTransformAxis);
                }
            }
        }

        /// <summary>
        /// Toggle between Build Mode and Play Mode
        /// Build Mode: All input actions enabled, can place/select/edit objects
        /// Play Mode: Input actions disabled for performance, read-only preview
        /// </summary>
        public void ToggleMode()
        {
            if (_currentMode == SandboxMode.Build)
            {
                SetMode(SandboxMode.Play);
            }
            else
            {
                SetMode(SandboxMode.Build);
            }
        }

        /// <summary>
        /// Set the sandbox mode (Build or Play)
        /// </summary>
        public void SetMode(SandboxMode mode)
        {
            if (_currentMode == mode)
                return;

            SandboxMode previousMode = _currentMode;
            _currentMode = mode;

            Debug.Log($"[MODE] Sandbox mode changed: {previousMode} → {mode}");

            if (mode == SandboxMode.Play)
            {
                // Entering Play Mode
                Debug.Log("[MODE] Entering Play Mode - Disabling build input actions");
                
                // Cancel any active placement
                if (IsPlacementActive)
                {
                    CancelPlacement();
                }
                
                // Exit all transform modes
                ExitAllTransformModes();
                
                // Disable build-related input actions
                DisableBuildInputActions();
            }
            else
            {
                // Entering Build Mode
                Debug.Log("[MODE] Entering Build Mode - Enabling build input actions");
                
                // Enable build-related input actions
                EnableBuildInputActions();
            }

            // Invoke mode changed event
            OnModeChanged?.Invoke(mode);
        }

        /// <summary>
        /// Set the current transform mode (Position/Rotation/Scale)
        /// </summary>
        public void SetTransformMode_Legacy(TransformModeType mode)
        {
            _currentTransformMode = mode;
            
            // Update visual feedback if in placement mode
            if (IsPlacementActive && _currentPlacementObject != null)
            {
                UpdateGhostTransformMode();
            }
        }

        /// <summary>
        /// Set the object library for this sandbox builder
        /// </summary>
        public void SetObjectLibrary(SceneObjectLibrary library)
        {
            _objectLibrary = library;
        }

        /// <summary>
        /// Initialize default save paths using Application.persistentDataPath for multi-platform support
        /// Force root folder to always be persistentDataPath for consistency
        /// </summary>
        private void InitializeDefaultPaths()
        {
            // FORCE root to be Application.persistentDataPath for cross-platform compatibility
            // This ensures data is saved to a consistent, writable location on all platforms
            string rootPath = Application.persistentDataPath;
            
            // Always reset paths to use persistentDataPath root
            _defaultSavePath = System.IO.Path.Combine(rootPath, "SceneSandboxBuilder", "SavedScenes");
            _defaultProjectSavePath = System.IO.Path.Combine(rootPath, "SceneSandboxBuilder", "SavedProjects");

            // Ensure directories exist
            try
            {
                if (!System.IO.Directory.Exists(_defaultSavePath))
                {
                    System.IO.Directory.CreateDirectory(_defaultSavePath);
                }

                if (!System.IO.Directory.Exists(_defaultProjectSavePath))
                {
                    System.IO.Directory.CreateDirectory(_defaultProjectSavePath);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneSandboxBuilder] Failed to create default directories: {ex.Message}");
            }
        }

        private void Awake()
        {
            // Initialize default paths for multi-platform support
            InitializeDefaultPaths();

            InitializeComponents();
            InitializeState();
            InitializeInputActions();
        }

        private void Start()
        {
            CreateNewScene();
            RegisterExistingTransformableItems();
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
            // Skip all build-related input handling in Play Mode for performance
            if (_currentMode == SandboxMode.Play)
            {
                return;
            }

            HandleRaycastInput();
            
            // Get current input position
            Vector2 inputPosition = GetInputPosition();
            
            // Update pointer tracking
            Vector2 pointerDelta = inputPosition - _lastPointerPosition;
            _lastPointerPosition = inputPosition;
            
            // Handle scroll wheel input
            if (_mouseScrollAction != null && _mouseScrollAction.enabled)
            {
                float scrollDelta = _mouseScrollAction.ReadValue<Vector2>().y;
                if (Mathf.Abs(scrollDelta) > 0.01f)
                {
                    // Handle scroll for placement if needed
                }
            }
            
            // Continuously update placement position if in placement mode
            if (IsPlacementActive)
            {
                UpdatePlacement(inputPosition);
            }
        }

        private void InitializeComponents()
        {
            // Get or create scene camera
            _sceneCamera = Camera.main ?? FindFirstObjectByType<Camera>();

            // Create scene root if not assigned
            if (_sceneRoot == null)
            {
                _sceneRoot = new GameObject("Scene Root").transform;
            }

            // Create stage area if not assigned
            if (_stageArea == null)
            {
                var stageGO = new GameObject("Stage Area");
                stageGO.transform.parent = _sceneRoot;
                _stageArea = stageGO.transform;

                // Add a drop zone to the stage area
                var dropZone = stageGO.AddComponent<DropZone>();
                dropZone.ZoneSize = new Vector3(20f, 5f, 20f);
                dropZone.AcceptAllTypes = true;
            }
        }

        private void InitializeState()
        {
            _placedObjects = new Dictionary<string, GameObject>();
            _previewObjects = new List<GameObject>();
            _placementState = PlacementState.Idle;

            // Don't create drop indicator here - create it when first needed
            // This avoids potential issues with early initialization
        }

        #region Public Interface

        /// <summary>
        /// Place an object from the library at the specified position
        /// </summary>
        public GameObject PlaceObject(string objectDataId, Vector3 position, bool autoSelect = true)
        {
            
            if (_objectLibrary == null)
            {
                Debug.LogError("[PLACE] ❌ ObjectLibrary is NULL!");
                return null;
            }

            var objectData = _objectLibrary.GetObjectById(objectDataId);
            
            if (objectData == null)
            {
                Debug.LogError($"[PLACE] ❌ Object data not found for ID: {objectDataId}");
                return null;
            }
            
            if (objectData.prefab == null)
            {
                Debug.LogError($"[PLACE] ❌ Prefab is NULL for object: {objectData.displayName}");
                return null;
            }

            // Instantiate the object
            GameObject newObject = Instantiate(objectData.prefab, _stageArea);
            newObject.name = objectData.displayName;

            // Apply placement settings
            Vector3 finalPosition = _snapToGrid ? SnapToGrid(position) : position;
            finalPosition = GetSurfacePosition(finalPosition);
            newObject.transform.position = finalPosition;

            // Apply scale - preserve prefab scale if defaultScale is zero
            Vector3 targetScale = objectData.defaultScale;
            if (targetScale == Vector3.zero)
            {
                targetScale = objectData.prefab.transform.localScale;
            }
            newObject.transform.localScale = targetScale;

            // Add draggable component if not present
            var draggable = newObject.GetComponent<TransformableItem>();
            if (draggable == null)
            {
                draggable = newObject.AddComponent<TransformableItem>();
            }

            // Set up draggable item
            string placedObjectId = System.Guid.NewGuid().ToString();
            draggable.SetObjectData(objectDataId, placedObjectId);

            // Bind draggable events
            BindDraggableEvents(draggable);

            // Add to scene configuration
            var placedObjectData = new Data.PlacedObjectData(objectDataId, finalPosition)
            {
                id = placedObjectId,
                rotation = newObject.transform.eulerAngles,
                scale = newObject.transform.localScale,
                customName = objectData.displayName
            };

            if (_currentScene != null)
            {
                _currentScene.AddPlacedObject(placedObjectData);
            }
            
            _placedObjects[placedObjectId] = newObject;

            // Select the object if requested
            if (autoSelect)
            {
                SelectObject(newObject);
            }

            OnObjectPlaced?.Invoke(newObject);

            return newObject;
        }

        #endregion

        #region New Placement System (Drag-Drop - Direct Object Manipulation)

        /// <summary>
        /// Start placement operation - creates real object and enters Active state
        /// </summary>
        /// <param name="objectDataId">ID of the object to place</param>
        /// <param name="screenPosition">Initial screen position for placement</param>
        public void StartPlacement(string objectDataId, Vector2 screenPosition)
        {
            if (_objectLibrary == null)
            {
                return;
            }

            var objectData = _objectLibrary.GetObjectById(objectDataId);
            if (objectData == null || objectData.prefab == null)
            {
                Debug.LogWarning($"[Placement] Cannot start placement: Invalid object data for ID {objectDataId}");
                return;
            }

            // Cancel any existing placement
            if (_placementState == PlacementState.Active)
            {
                CancelPlacement();
            }

            _currentPlacementObjectId = objectDataId;
            _placementState = PlacementState.Active;

            // Convert screen position to world position
            _currentPlacementPosition = GetWorldPositionFromScreen(screenPosition);

            // Create real object directly (no ghost)
            CreateRealObjectForPlacement(objectData);
        }

        /// <summary>
        /// Update placement position during drag
        /// </summary>
        /// <param name="screenPosition">Current screen position</param>
        public void UpdatePlacement(Vector2 screenPosition)
        {
            if (_placementState != PlacementState.Active || _currentPlacementObject == null)
            {
                return;
            }

            _currentPlacementPosition = GetWorldPositionFromScreen(screenPosition);
            UpdateGhostPosition();
        }

        /// <summary>
        /// Confirm placement - finalize the object that's already been created
        /// </summary>
        /// <returns>The placed GameObject, or null if placement failed</returns>
        public GameObject ConfirmPlacement()
        {
            if (_placementState != PlacementState.Active || _currentPlacementObject == null)
            {
                return null;
            }

            if (!_isCurrentPlacementValid)
            {
                CancelPlacement();
                return null;
            }

            _placementState = PlacementState.Confirming;

            // The object is already placed and moved, just finalize it
            GameObject placedObject = _currentPlacementObject;
            
            // Enable any components that were disabled during placement
            foreach (var transformable in placedObject.GetComponentsInChildren<TransformableItem>())
            {
                transformable.enabled = true;
            }

            // Reset state
            _currentPlacementObjectId = null;
            _currentPlacementObject = null; // Don't destroy, just clear reference
            _placementState = PlacementState.Idle;

            // Select the placed object
            if (placedObject != null)
            {
                SelectObject(placedObject);
            }

            return placedObject;
        }

        /// <summary>
        /// Cancel the current placement operation
        /// </summary>
        public void CancelPlacement()
        {
            if (_placementState != PlacementState.Active)
                return;

            _placementState = PlacementState.Cancelling;

            // Destroy the real object if placement is cancelled
            if (_currentPlacementObject != null)
            {
                // Remove from scene configuration and placed objects dictionary
                var draggable = _currentPlacementObject.GetComponent<TransformableItem>();
                if (draggable != null)
                {
                    string objectId = draggable.ObjectId;
                    if (_currentScene != null)
                    {
                        _currentScene.RemovePlacedObject(objectId);
                    }
                    if (_placedObjects != null && _placedObjects.ContainsKey(objectId))
                    {
                        _placedObjects.Remove(objectId);
                    }
                }
                
                Destroy(_currentPlacementObject);
                _currentPlacementObject = null;
            }

            _currentPlacementObjectId = null;
            _currentPlacementPosition = Vector3.zero;
            _isCurrentPlacementValid = false;
            _placementState = PlacementState.Idle;

            Debug.Log("[Placement] ❌ Placement cancelled");
        }

        /// <summary>
        /// Create a real object for placement (no ghost, no material changes)
        /// </summary>
        private void CreateRealObjectForPlacement(SceneObjectData objectData)
        {
            if (objectData == null || objectData.prefab == null)
                return;

            // Create the actual object using PlaceObject
            Vector3 position = _currentPlacementPosition;
            if (_snapToGrid)
            {
                position = SnapToGrid(position);
            }
            position = GetSurfacePosition(position);

            // Use PlaceObject to create the object properly
            _currentPlacementObject = PlaceObject(_currentPlacementObjectId, position, autoSelect: false);
            
            if (_currentPlacementObject == null)
            {
                Debug.LogError("[Placement] Failed to create object for placement");
                return;
            }

            // Temporarily disable TransformableItem to prevent interaction during placement
            foreach (var transformable in _currentPlacementObject.GetComponentsInChildren<TransformableItem>())
            {
                transformable.enabled = false;
            }

            // Initial position
            _currentPlacementObject.transform.position = position;
            UpdateGhostValidation();
        }

        /// <summary>
        /// Update placement object color based on validity and transform mode (DEPRECATED - no longer used)
        /// </summary>
        private void UpdateGhostColor()
        {
            // No longer used - we don't change materials anymore
            // Kept for compatibility but does nothing
        }

        /// <summary>
        /// Update placement object position and rotation
        /// </summary>
        private void UpdateGhostPosition()
        {
            if (_currentPlacementObject == null)
                return;

            // Apply transform based on current mode
            switch (_currentTransformMode)
            {
                case TransformModeType.Position:
                    Vector3 targetPosition = _currentPlacementPosition;
                    if (_snapToGrid)
                    {
                        targetPosition = SnapToGrid(targetPosition);
                    }
                    _currentPlacementObject.transform.position = targetPosition;
                    break;

                case TransformModeType.Rotation:
                    _currentPlacementObject.transform.rotation = _currentPlacementRotation;
                    break;

                case TransformModeType.Scale:
                    _currentPlacementObject.transform.localScale = _currentPlacementScale;
                    break;
            }

            // Validate placement
            UpdateGhostValidation();
        }

        /// <summary>
        /// Update placement object visual feedback based on transform mode
        /// </summary>
        private void UpdateGhostTransformMode()
        {
            if (_currentPlacementObject == null)
                return;

            // Update transform only (no color changes)
            UpdateGhostPosition();
        }

        /// <summary>
        /// Validate the current placement position
        /// </summary>
        private void UpdateGhostValidation()
        {
            bool wasValid = _isCurrentPlacementValid;
            _isCurrentPlacementValid = ValidatePlacementPosition(_currentPlacementPosition);

            // Update visual feedback if validity changed
            if (wasValid != _isCurrentPlacementValid)
            {
                UpdateGhostColor();
            }
        }

        /// <summary>
        /// Check if a placement position is valid
        /// </summary>
        private bool ValidatePlacementPosition(Vector3 position)
        {
            // Check scene bounds
            if (!IsPositionInSceneBounds(position))
            {
                return false;
            }

            // Check collisions if enabled
            if (_checkCollisions && _currentPlacementObject != null)
            {
                Bounds ghostBounds = GetObjectBounds(_currentPlacementObject);
                
                // Use OverlapBox to get all colliders, then filter out ghost object's colliders
                Collider[] overlappingColliders = Physics.OverlapBox(
                    ghostBounds.center, 
                    ghostBounds.extents, 
                    _currentPlacementObject.transform.rotation, 
                    _collisionLayers,
                    QueryTriggerInteraction.Ignore
                );
                
                // Check if any overlapping collider belongs to a different object (not the ghost)
                foreach (var collider in overlappingColliders)
                {
                    // Skip if this collider belongs to the ghost object
                    if (collider.transform.IsChildOf(_currentPlacementObject.transform) || collider.gameObject == _currentPlacementObject)
                    {
                        continue;
                    }
                    
                    // Skip ground/floor objects (check if layer is in ground layers mask)
                    int objLayer = collider.gameObject.layer;
                    if ((_groundLayers.value & (1 << objLayer)) != 0)
                    {
                        continue;
                    }
                    
                    // Skip static ground/floor objects if enabled
                    if (_ignoreStaticObjects && collider.gameObject.isStatic)
                    {
                        continue;
                    }
                    
                    // Found a collision with a movable/dynamic object
                    return false;
                }
            }

            // Check if surface below is required
            if (_requireSurfaceBelow)
            {
                if (!Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, 0.2f, _placementLayers))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get the bounds of a game object including all renderers
        /// </summary>
        private Bounds GetObjectBounds(GameObject obj)
        {
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            var renderers = obj.GetComponentsInChildren<Renderer>();
            
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        /// <summary>
        /// Destroy the placement object and cleanup materials
        /// </summary>
        private void DestroyGhostObject()
        {
            if (_currentPlacementObject != null)
            {
                Destroy(_currentPlacementObject);
                _currentPlacementObject = null;
            }

            // Clear material lists (no materials to cleanup since we don't modify them)
            _ghostMaterials.Clear();
            _originalMaterials.Clear();
        }

        /// <summary>
        /// Set layer for a GameObject and all its children recursively
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            
            obj.layer = layer;
            
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        #endregion

        #region Public Interface (Legacy)

        /// <summary>
        /// Remove an object from the scene
        /// </summary>
        public bool RemoveObject(GameObject obj)
        {
            if (obj == null) return false;

            var draggable = obj.GetComponent<TransformableItem>();
            if (draggable == null) return false;

            string objectId = draggable.ObjectId;

            // Remove from scene configuration
            if (_currentScene != null && _currentScene.RemovePlacedObject(objectId))
            {
                if (_placedObjects != null)
                {
                    _placedObjects.Remove(objectId);
                }

                if (_selectedObject == obj)
                {
                    SelectObject(null);
                }

                Destroy(obj);
                OnObjectRemoved?.Invoke(obj);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove an object by its ID
        /// </summary>
        public bool RemoveObject(string objectId)
        {
            if (_placedObjects.TryGetValue(objectId, out GameObject obj))
            {
                return RemoveObject(obj);
            }
            return false;
        }

        /// <summary>
        /// Select an object in the scene
        /// </summary>
        public void SelectObject(GameObject obj)
        {
            if (obj != null)
            {
                var item = obj.GetComponent<TransformableItem>();
                if (item != null)
                {
                    SelectItem(item);
                }
            }
            else
            {
                // Deselect all
                ClearSelection();
            }
        }

        /// <summary>
        /// Start editing an existing object with direct manipulation
        /// Similar to PlaceObject, but modifies an existing object instead of creating new one
        /// </summary>
        public void BeginObjectEdit(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            // Cancel any existing placement/edit
            if (IsPlacementActive)
            {
                CancelPlacement();
            }

            _selectedObjectForEdit = targetObject;
            _originalPosition = targetObject.transform.position;
            _originalRotation = targetObject.transform.rotation;
            _originalScale = targetObject.transform.localScale;

            // No need to store or change materials - we'll edit the object directly
            _originalObjectMaterials = null;

            // Use the existing object as the placement object (no ghost created)
            _currentPlacementObject = targetObject;

            // Set initial transform state
            _currentPlacementPosition = _originalPosition;
            _currentPlacementRotation = _originalRotation;
            _currentPlacementScale = _originalScale;

            _placementState = PlacementState.Active;
            
            // Initial validation
            UpdateGhostValidation();
        }

        /// <summary>
        /// Confirm the edit - object is already transformed, just finalize
        /// </summary>
        public GameObject ConfirmObjectEdit()
        {
            if (_selectedObjectForEdit == null || _currentPlacementObject == null)
            {
                return null;
            }

            if (!_isCurrentPlacementValid)
            {
                CancelObjectEdit();
                return null;
            }

            _placementState = PlacementState.Confirming;

            // Object is already transformed, just update scene configuration
            UpdatePlacedObjectTransform(_selectedObjectForEdit);

            // Cleanup
            _currentPlacementObject = null;
            GameObject editedObject = _selectedObjectForEdit;
            _selectedObjectForEdit = null;
            _originalObjectMaterials = null;
            
            _placementState = PlacementState.Idle;

            Debug.Log($"[Edit] ✅ Confirmed edit for {editedObject.name}");
            return editedObject;
        }

        /// <summary>
        /// Cancel the edit - restore original transforms
        /// </summary>
        public void CancelObjectEdit()
        {
            if (_selectedObjectForEdit == null)
                return;

            _placementState = PlacementState.Cancelling;

            // Restore original transforms
            _selectedObjectForEdit.transform.position = _originalPosition;
            _selectedObjectForEdit.transform.rotation = _originalRotation;
            _selectedObjectForEdit.transform.localScale = _originalScale;

            // Cleanup
            _currentPlacementObject = null;
            _selectedObjectForEdit = null;
            _originalObjectMaterials = null;
            _placementState = PlacementState.Idle;

            Debug.Log("[Edit] ❌ Edit cancelled");
        }

        /// <summary>
        /// Update a placed object's transform in the scene configuration
        /// </summary>
        private void UpdatePlacedObjectTransform(GameObject obj)
        {
            if (obj == null || _currentScene == null)
                return;

            var draggable = obj.GetComponent<TransformableItem>();
            if (draggable == null)
                return;

            string objectId = draggable.ObjectId;
            var placedObjectData = _currentScene.placedObjects.Find(p => p.id == objectId);
            
            if (placedObjectData != null)
            {
                placedObjectData.position = obj.transform.position;
                placedObjectData.rotation = obj.transform.eulerAngles;
                placedObjectData.scale = obj.transform.localScale;
                _currentScene.lastModified = System.DateTime.Now;
            }
        }

        /// <summary>
        /// Clear all objects from the scene
        /// </summary>
        public void ClearScene()
        {
            // Remove all placed objects
            if (_placedObjects != null)
            {
                var objectsToRemove = new List<GameObject>(_placedObjects.Values);
                foreach (var obj in objectsToRemove)
                {
                    if (obj != null)
                    {
                        RemoveObject(obj);
                    }
                }

                _placedObjects.Clear();
            }

            // Clear scene configuration if it exists
            if (_currentScene != null)
            {
                _currentScene.ClearPlacedObjects();
            }

            SelectObject(null);
            OnSceneCleared?.Invoke();
        }

        /// <summary>
        /// Create a new empty scene
        /// </summary>
        public void CreateNewScene(string sceneName = null)
        {
            ClearScene();

            _currentScene = new SceneConfiguration(sceneName ?? "New Scene");
            _currentSceneName = _currentScene.sceneName;

            OnSceneLoaded?.Invoke(_currentScene);
        }

        /// <summary>
        /// Save the current scene configuration
        /// </summary>
        public bool SaveScene(string filePath = null)
        {
            if (_currentScene == null) return false;

            try
            {
                // Update scene configuration with current object states
                UpdateSceneConfiguration();

                string savePath = filePath ?? GetDefaultSavePath();
                
                // Ensure directory exists
                if (!EnsureDirectoryExists(savePath))
                {
                    return false;
                }

                string json = JsonUtility.ToJson(_currentScene, true);
                System.IO.File.WriteAllText(savePath, json);

                OnSceneSaved?.Invoke(_currentScene);

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save project: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a scene configuration from file
        /// </summary>
        public bool LoadScene(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return false;
                }

                string json = System.IO.File.ReadAllText(filePath);
                var sceneConfig = JsonUtility.FromJson<SceneConfiguration>(json);

                if (sceneConfig != null)
                {
                    LoadSceneConfiguration(sceneConfig);
                    return true;
                }
            }
            catch (System.Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// Begin placement operation from UI - starts placement with real object
        /// </summary>
        /// <param name="objectDataId">ID of the object to place</param>
        /// <param name="screenPosition">Screen position to start placement</param>
        public void BeginPlacement(string objectDataId, Vector2 screenPosition)
        {
            StartPlacement(objectDataId, screenPosition);
            
            // Also show drop indicator for additional visual feedback (optional)
            if (_enableDropIndicator)
            {
                Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);
                _isDraggingObject = true;
                _dragPreviewPosition = worldPosition;
                ShowDropIndicator(worldPosition);
            }
        }

        /// <summary>
        /// Begin placement operation - starts placement with real object at screen center
        /// </summary>
        /// <param name="objectDataId">ID of the object to place</param>
        public void BeginPlacement(string objectDataId)
        {
            // Use screen center as default position
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            
            BeginPlacement(objectDataId, screenCenter);
        }

        /// <summary>
        /// Update placement position from UI - updates object position
        /// </summary>
        public void UpdatePlacementUI(Vector2 screenPosition)
        {
            // Update placement ghost
            UpdatePlacement(screenPosition);

            // Also update drop indicator if enabled
            if (_isDraggingObject && _enableDropIndicator)
            {
                Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);
                _dragPreviewPosition = worldPosition;
                UpdateDropIndicator(worldPosition);
            }
        }

        /// <summary>
        /// Complete placement from UI - confirms placement and creates the object
        /// </summary>
        public GameObject CompletePlacement(string objectDataId, Vector2 screenPosition)
        {
            // Hide drop indicator
            if (_enableDropIndicator)
            {
                HideDropIndicator();
                _isDraggingObject = false;
            }

            // Confirm placement with the new system
            GameObject result = ConfirmPlacement();

            return result;
        }

        /// <summary>
        /// Cancel placement operation from UI
        /// </summary>
        public void CancelPlacementUI()
        {
            // Cancel the placement
            CancelPlacement();

            // Hide drop indicator
            if (_enableDropIndicator)
            {
                HideDropIndicator();
                _isDraggingObject = false;
            }
        }

        #endregion

        #region Project Management (New Serialization System)

        /// <summary>
        /// Create a new sandbox project
        /// </summary>
        public void CreateNewProject(string projectName = null)
        {
            ClearScene();

            string name = projectName ?? "New Sandbox Project";
            _currentProject = new SandboxProjectData(name);

            // Set default configuration from current settings
            UpdateProjectSettingsFromBuilder();

            // Get the default scene created by the constructor
            _currentScene = _currentProject.GetActiveScene();
            _currentSceneName = _currentScene.sceneName;

            OnSceneLoaded?.Invoke(_currentScene);
        }

        /// <summary>
        /// Save the current project to JSON file
        /// </summary>
        public bool SaveProject(string filePath = null)
        {
            if (_currentProject == null)
            {
                CreateDefaultProject();
            }

            try
            {
                // Update current scene configuration with current object states before saving
                if (_currentScene != null)
                {
                    UpdateSceneConfiguration();
                    
                    // Update scene in project's scene list
                    var sceneInProject = _currentProject.GetScene(_currentScene.sceneId);
                    if (sceneInProject != null)
                    {
                        // Update the scene in the list
                        int index = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
                        if (index >= 0)
                        {
                            _currentProject.scenes[index] = _currentScene;
                        }
                    }
                }

                // Update project settings from current builder state
                UpdateProjectSettingsFromBuilder();

                string savePath = filePath ?? GetDefaultProjectSavePath();
                
                // Ensure directory exists
                if (!EnsureDirectoryExists(savePath))
                {
                    return false;
                }

                bool success = SandboxProjectSerializer.SaveToFile(_currentProject, this, savePath);
                
                if (success)
                {
                    OnSceneSaved?.Invoke(_currentScene);
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save project: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a project from JSON file
        /// </summary>
        public bool LoadProject(string filePath)
        {
            try
            {
                var project = SandboxProjectSerializer.LoadFromFile(filePath);
                if (project != null)
                {
                    LoadProjectData(project);
                    return true;
                }
            }
            catch (System.Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// Delete a project file from disk
        /// </summary>
        public bool DeleteProject(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    
                    // If the deleted project is currently loaded, clear the current project
                    if (_currentProject != null && filePath.Contains(_currentProject.projectName))
                    {
                        _currentProject = null;
                        CreateNewScene();
                    }
                    
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get metadata for all projects in the default directory
        /// </summary>
        public List<SandboxProjectMetadata> GetAvailableProjects()
        {
            return SandboxProjectSerializer.GetProjectsInDirectory(_defaultProjectSavePath, "*.sbproj");
        }

        /// <summary>
        /// Get project information for display in UI
        /// </summary>
        public SandboxProjectMetadata GetCurrentProjectInfo()
        {
            if (_currentProject == null)
                return null;

            // Update project from runtime state before getting info
            SandboxProjectSerializer.UpdateProjectFromRuntimeState(_currentProject, this);

            return new SandboxProjectMetadata
            {
                projectName = _currentProject.projectName,
                filePath = GetDefaultProjectSavePath(),
                created = _currentProject.created,
                lastModified = _currentProject.lastModified,
                description = _currentProject.description,
                objectCount = _placedObjects?.Count ?? 0,
                hasTimelineIntegration = _currentProject.hasTimelineIntegration,
                sceneBounds = _currentProject.settings.sceneBounds
            };
        }

        /// <summary>
        /// Update project settings from current builder configuration
        /// </summary>
        private void UpdateProjectSettingsFromBuilder()
        {
            if (_currentProject?.settings == null)
                return;

            var settings = _currentProject.settings;
            
            // Update placement settings
            settings.snapToGrid = _snapToGrid;
            settings.gridSize = _gridSize;
            settings.defaultPlacementHeight = _defaultPlacementHeight;
            settings.minimumPlacementHeight = _minimumPlacementHeight;
            settings.useRaycastForPlacement = _useRaycastForPlacement;
            settings.useConsistentHeight = _useConsistentHeight;
            settings.placementLayers = _placementLayers;

            // Update grid settings
            settings.gridOffset = _gridOffset;
            settings.gridPivotOffset = _gridPivotOffset;

            // Update visual settings
            settings.sceneBounds = _sceneBounds;
            settings.sceneBoundsOffset = _sceneBoundsOffset;
            settings.sceneBoundsPivot = _sceneBoundsPivot;
            settings.sceneBoundsColor = _sceneBoundsColor;
            settings.enableGizmos = _enableGizmos;
            settings.showBoundsGizmo = _showBoundsGizmo;
            settings.showAxesGizmo = _showAxesGizmo;
            settings.showHandlesGizmo = _showHandlesGizmo;
            settings.gizmoBoundsColor = _gizmoBoundsColor;
            settings.gizmoAxisLength = _gizmoAxisLength;
            settings.enableSceneGizmos = _enableSceneGizmos;
            settings.showSceneGrid = _showSceneGrid;
            settings.showSceneBounds = _showSceneBounds;
            settings.showStageAreaGizmo = _showStageAreaGizmo;
            settings.showPlacementHeightGizmo = _showPlacementHeightGizmo;
            settings.placementHeightColor = _placementHeightColor;

            // Update drop indicator settings
            settings.enableDropIndicator = _enableDropIndicator;
            settings.validDropColor = _validDropColor;
            settings.invalidDropColor = _invalidDropColor;
            settings.dropIndicatorSize = _dropIndicatorSize;

            // Update preview settings
            settings.autoPreview = _autoPreview;
            settings.previewDuration = _previewDuration;

            _currentProject.lastModified = System.DateTime.Now;
        }

        /// <summary>
        /// Apply project settings to builder configuration
        /// </summary>
        private void ApplyProjectSettingsToBuilder()
        {
            if (_currentProject?.settings == null)
                return;

            var settings = _currentProject.settings;

            // Apply placement settings
            _snapToGrid = settings.snapToGrid;
            _gridSize = settings.gridSize;
            _defaultPlacementHeight = settings.defaultPlacementHeight;
            _minimumPlacementHeight = settings.minimumPlacementHeight;
            _useRaycastForPlacement = settings.useRaycastForPlacement;
            _useConsistentHeight = settings.useConsistentHeight;
            _placementLayers = settings.placementLayers;

            // Apply grid settings
            _gridOffset = settings.gridOffset;
            _gridPivotOffset = settings.gridPivotOffset;

            // Apply visual settings
            _sceneBounds = settings.sceneBounds;
            _sceneBoundsOffset = settings.sceneBoundsOffset;
            _sceneBoundsPivot = settings.sceneBoundsPivot;
            _sceneBoundsColor = settings.sceneBoundsColor;
            _enableGizmos = settings.enableGizmos;
            _showBoundsGizmo = settings.showBoundsGizmo;
            _showAxesGizmo = settings.showAxesGizmo;
            _showHandlesGizmo = settings.showHandlesGizmo;
            _gizmoBoundsColor = settings.gizmoBoundsColor;
            _gizmoAxisLength = settings.gizmoAxisLength;
            _enableSceneGizmos = settings.enableSceneGizmos;
            _showSceneGrid = settings.showSceneGrid;
            _showSceneBounds = settings.showSceneBounds;
            _showStageAreaGizmo = settings.showStageAreaGizmo;
            _showPlacementHeightGizmo = settings.showPlacementHeightGizmo;
            _placementHeightColor = settings.placementHeightColor;

            // Apply drop indicator settings
            _enableDropIndicator = settings.enableDropIndicator;
            _validDropColor = settings.validDropColor;
            _invalidDropColor = settings.invalidDropColor;
            _dropIndicatorSize = settings.dropIndicatorSize;

            // Apply preview settings
            _autoPreview = settings.autoPreview;
            _previewDuration = settings.previewDuration;
        }

        /// <summary>
        /// Load project data and apply to current builder state
        /// </summary>
        private void LoadProjectData(SandboxProjectData project)
        {
            // Clear current state
            ClearScene();

            // Set project
            _currentProject = project;

            // Apply project settings to builder
            ApplyProjectSettingsToBuilder();

            // Load active scene or first scene
            var activeScene = project.GetActiveScene();
            if (activeScene != null)
            {
                LoadSceneConfiguration(activeScene);
            }
            else if (project.scenes.Count > 0)
            {
                LoadSceneConfiguration(project.scenes[0]);
                project.activeSceneId = project.scenes[0].sceneId;
            }
            else
            {
                // Create default scene if none exists
                _currentScene = new SceneConfiguration($"{project.projectName} Scene");
                project.scenes.Add(_currentScene);
                project.activeSceneId = _currentScene.sceneId;
            }

            _currentSceneName = _currentScene.sceneName;
        }

        /// <summary>
        /// Create a default project from current state
        /// </summary>
        private void CreateDefaultProject()
        {
            if (_currentProject == null)
            {
                _currentProject = new SandboxProjectData(_currentSceneName ?? "Untitled Project");
            }

            if (_currentScene == null)
            {
                _currentScene = new SceneConfiguration(_currentSceneName ?? "New Scene");
            }

            // Ensure current scene is in project
            if (!_currentProject.scenes.Contains(_currentScene))
            {
                _currentProject.scenes.Add(_currentScene);
            }
            
            _currentProject.activeSceneId = _currentScene.sceneId;
            UpdateProjectSettingsFromBuilder();
        }

        /// <summary>
        /// Get default save path for projects
        /// </summary>
        private string GetDefaultProjectSavePath()
        {
            string projectName = _currentProject?.projectName ?? _currentSceneName ?? "Untitled";
            string safeName = SanitizeFileName(projectName);
            return System.IO.Path.Combine(_defaultProjectSavePath, $"{safeName}.sbproj");
        }

        #endregion

        #region Multi-Scene Management

        /// <summary>
        /// Create a new scene in the current project
        /// </summary>
        public SceneConfiguration CreateNewSceneInProject(string sceneName = null)
        {
            if (_currentProject == null)
            {
                CreateDefaultProject();
            }

            // Save current scene before switching and update it in the project's scene list
            if (_currentScene != null)
            {
                UpdateSceneConfiguration();
                
                // Update the scene in the project's scenes list
                int currentSceneIndex = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
                if (currentSceneIndex >= 0)
                {
                    _currentProject.scenes[currentSceneIndex] = _currentScene;
                }
            }

            string name = sceneName ?? $"Scene {_currentProject.scenes.Count + 1}";
            var newScene = _currentProject.AddScene(name);
            
            // Switch to new scene (this will clear and load the empty new scene)
            SwitchToScene(newScene.sceneId);
            
            return newScene;
        }

        /// <summary>
        /// Switch to a different scene in the project
        /// </summary>
        public bool SwitchToScene(string sceneId)
        {
            if (_currentProject == null)
            {
                return false;
            }

            var targetScene = _currentProject.GetScene(sceneId);
            if (targetScene == null)
            {
                Debug.LogError($"Scene not found: {sceneId}");
                return false;
            }

            // Save current scene state and update it in the project's scene list
            if (_currentScene != null)
            {
                UpdateSceneConfiguration();
                
                // Update the scene in the project's scenes list
                int currentSceneIndex = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
                if (currentSceneIndex >= 0)
                {
                    _currentProject.scenes[currentSceneIndex] = _currentScene;
                }
            }

            // Load target scene (LoadSceneConfiguration handles clearing internally)
            LoadSceneConfiguration(targetScene);
            _currentProject.SetActiveScene(sceneId);
            
            return true;
        }

        /// <summary>
        /// Delete a scene from the project
        /// </summary>
        public bool DeleteSceneFromProject(string sceneId)
        {
            if (_currentProject == null)
            {
                Debug.LogError("No project loaded");
                return false;
            }

            if (_currentProject.scenes.Count <= 1)
            {
                Debug.LogError("Cannot delete the last scene in project");
                return false;
            }

            // If deleting current scene, switch to another first
            if (_currentScene != null && _currentScene.sceneId == sceneId)
            {
                var otherScene = _currentProject.scenes.Find(s => s.sceneId != sceneId);
                if (otherScene != null)
                {
                    SwitchToScene(otherScene.sceneId);
                }
            }

            bool removed = _currentProject.RemoveScene(sceneId);
            
            return removed;
        }

        /// <summary>
        /// Get all scenes in the current project
        /// </summary>
        public List<SceneConfiguration> GetAllScenesInProject()
        {
            return _currentProject?.scenes ?? new List<SceneConfiguration>();
        }

        /// <summary>
        /// Duplicate the current scene
        /// </summary>
        public SceneConfiguration DuplicateCurrentScene()
        {
            if (_currentScene == null || _currentProject == null)
            {
                Debug.LogError("No scene or project loaded");
                return null;
            }

            // Save current state and update it in the project's scene list
            UpdateSceneConfiguration();
            
            // Update the scene in the project's scenes list
            int currentSceneIndex = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
            if (currentSceneIndex >= 0)
            {
                _currentProject.scenes[currentSceneIndex] = _currentScene;
            }

            // Create duplicate
            var duplicate = new SceneConfiguration($"{_currentScene.sceneName} (Copy)");
            duplicate.description = _currentScene.description;
            duplicate.defaultCameraPosition = _currentScene.defaultCameraPosition;
            duplicate.defaultCameraRotation = _currentScene.defaultCameraRotation;
            duplicate.environmentColor = _currentScene.environmentColor;
            duplicate.environmentLighting = _currentScene.environmentLighting;

            // Copy all placed objects
            foreach (var obj in _currentScene.placedObjects)
            {
                var objCopy = new Data.PlacedObjectData(obj.objectDataId, obj.position)
                {
                    id = System.Guid.NewGuid().ToString(), // New ID for copy
                    rotation = obj.rotation,
                    scale = obj.scale,
                    customName = obj.customName,
                    properties = new Dictionary<string, object>(obj.properties)
                };
                duplicate.AddPlacedObject(objCopy);
            }

            _currentProject.scenes.Add(duplicate);
            
            return duplicate;
        }

        /// <summary>
        /// Rename the current scene
        /// </summary>
        public void RenameCurrentScene(string newName)
        {
            if (_currentScene != null)
            {
                _currentScene.sceneName = newName;
                _currentSceneName = newName;
                _currentScene.lastModified = System.DateTime.Now;
            }
        }

        /// <summary>
        /// Get scene count in current project
        /// </summary>
        public int GetSceneCount()
        {
            return _currentProject?.scenes.Count ?? 0;
        }

        /// <summary>
        /// Get metadata for all scenes in project (for UI)
        /// </summary>
        public System.Collections.Generic.List<Data.SceneMetadata> GetAllSceneMetadata()
        {
            var metadataList = new System.Collections.Generic.List<Data.SceneMetadata>();
            
            if (_currentProject == null)
                return metadataList;

            string activeSceneId = _currentProject.activeSceneId;
            
            foreach (var scene in _currentProject.scenes)
            {
                bool isActive = scene.sceneId == activeSceneId;
                metadataList.Add(new Data.SceneMetadata(scene, isActive));
            }
            
            return metadataList;
        }

        #endregion

        /// <summary>
        /// Place an object using the new factory system
        /// </summary>
        public GameObject PlaceObjectWithFactory(string objectDataId, Vector3 position, bool autoSelect = true)
        {
            if (_objectLibrary == null) return null;

            var objectData = _objectLibrary.GetObjectById(objectDataId);
            if (objectData == null || objectData.prefab == null) return null;

            // Create placed object data using existing API
            string placedObjectId = System.Guid.NewGuid().ToString();
            var placedObjectData = new Data.PlacedObjectData(objectDataId, position)
            {
                id = placedObjectId,
                rotation = objectData.prefab.transform.eulerAngles,
                scale = objectData.defaultScale != Vector3.zero ? objectData.defaultScale : objectData.prefab.transform.localScale,
                customName = objectData.displayName
            };

            // Apply positioning
            placedObjectData.position = _snapToGrid ? SnapToGrid(position) : position;
            placedObjectData.position = GetSurfacePosition(placedObjectData.position);

            // Store factory-related properties in the existing properties dictionary
            placedObjectData.properties["objectType"] = SandboxObjectFactory.DetermineObjectType(objectData.prefab);
            placedObjectData.properties["libraryName"] = objectData.displayName;
            placedObjectData.properties["isActive"] = true;

            // Create new factory data for runtime object creation
            var factoryData = new Serialization.PlacedObjectData
            {
                instanceId = placedObjectId,
                objectType = placedObjectData.properties["objectType"].ToString(),
                position = placedObjectData.position,
                rotation = Quaternion.Euler(placedObjectData.rotation),
                scale = placedObjectData.scale,
                customName = placedObjectData.customName,
                isActive = true,
                customProperties = new Dictionary<string, object>(placedObjectData.properties)
            };

            // Create runtime object using factory
            GameObject newObject = SandboxObjectFactory.CreateRuntimeObjectFromData(factoryData, _stageArea);
            
            if (newObject == null)
            {
                return null;
            }

            // Add draggable component for interaction
            var draggable = newObject.GetComponent<TransformableItem>();
            if (draggable == null)
            {
                draggable = newObject.AddComponent<TransformableItem>();
            }

            draggable.SetObjectData(objectDataId, placedObjectId);
            BindDraggableEvents(draggable);

            // Add to scene and project
            if (_currentScene != null)
            {
                _currentScene.AddPlacedObject(placedObjectData);
            }
            
            _placedObjects[placedObjectId] = newObject;

            // Ensure project exists
            if (_currentProject == null)
            {
                CreateDefaultProject();
            }

            // Select if requested
            if (autoSelect)
            {
                SelectObject(newObject);
            }

            OnObjectPlaced?.Invoke(newObject);

            return newObject;
        }

        /// <summary>
        /// Start preview mode using the timeline director
        /// </summary>
        public void StartPreview()
        {
            if (_timelineDirector == null || IsInPreviewMode) return;

            StopPreview(); // Ensure clean state

            // Create preview objects (snapshots of current scene)
            CreatePreviewObjects();

            // Set up timeline director with current scene
            SetupTimelineForPreview();

            // Start playback
            _timelineDirector.Play();

            OnPreviewStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Stop preview mode and return to editing
        /// </summary>
        public void StopPreview()
        {
            if (_timelineDirector != null)
            {
                _timelineDirector.Stop();
            }

            // Clean up preview objects
            CleanupPreviewObjects();

            OnPreviewStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Toggle gizmo visibility for all objects
        /// </summary>
        public void SetGizmoVisibility(bool visible)
        {
            _enableGizmos = visible;
        }

        /// <summary>
        /// Toggle scene gizmo visibility
        /// </summary>
        public void SetSceneGizmoVisibility(bool visible)
        {
            _enableSceneGizmos = visible;
        }

        /// <summary>
        /// Toggle specific scene gizmo features
        /// </summary>
        public void SetSceneGridVisibility(bool visible)
        {
            _showSceneGrid = visible;
        }

        public void SetSceneBoundsVisibility(bool visible)
        {
            _showSceneBounds = visible;
        }

        public void SetStageAreaGizmoVisibility(bool visible)
        {
            _showStageAreaGizmo = visible;
        }

        /// <summary>
        /// Toggle placement height gizmo visibility
        /// </summary>
        public void SetPlacementHeightGizmoVisibility(bool visible)
        {
            _showPlacementHeightGizmo = visible;
        }

        /// <summary>
        /// Set the default placement height
        /// </summary>
        public void SetDefaultPlacementHeight(float height)
        {
            _defaultPlacementHeight = height;
        }

        /// <summary>
        /// Toggle raycast-based placement vs fixed height placement
        /// </summary>
        public void SetUseRaycastForPlacement(bool useRaycast)
        {
            _useRaycastForPlacement = useRaycast;
        }

        /// <summary>
        /// Set the scene bounds for visualization and validation
        /// </summary>
        public void SetSceneBounds(Vector3 bounds)
        {
            _sceneBounds = bounds;
        }

        /// <summary>
        /// Set the scene bounds offset from stage area center
        /// </summary>
        public void SetSceneBoundsOffset(Vector3 offset)
        {
            _sceneBoundsOffset = offset;
        }

        /// <summary>
        /// Set the scene bounds color
        /// </summary>
        public void SetSceneBoundsColor(Color color)
        {
            _sceneBoundsColor = color;
        }

        /// <summary>
        /// Toggle drop indicator system
        /// </summary>
        public void SetDropIndicatorEnabled(bool enabled)
        {
            _enableDropIndicator = enabled;
            if (!enabled)
            {
                HideDropIndicator();
            }
        }

        /// <summary>
        /// Set drop indicator colors
        /// </summary>
        public void SetDropIndicatorColors(Color validColor, Color invalidColor)
        {
            _validDropColor = validColor;
            _invalidDropColor = invalidColor;
        }

        /// <summary>
        /// Check if a position is valid for object placement
        /// </summary>
        public bool IsValidPlacementPosition(Vector3 position)
        {
            return IsPositionInSceneBounds(position);
        }

        /// <summary>
        /// Get the current drop indicator position during drag operations
        /// </summary>
        public Vector3 GetDropIndicatorPosition()
        {
            return _dragPreviewPosition;
        }

        #region Selection Manager Event Handlers

        private void HandleItemSelected(TransformableItem item)
        {
            if (item != null)
            {
                _selectedObject = item.gameObject;
                OnObjectSelected?.Invoke(_selectedObject);
                
                // Automatically start edit mode with ghost preview if enabled
                if (_autoEditOnSelect)
                {
                    BeginObjectEdit(item.gameObject);
                }
            }
        }

        private void HandleItemDeselected(TransformableItem item)
        {
            if (item != null && _selectedObject == item.gameObject)
            {
                _selectedObject = null;
                OnObjectSelected?.Invoke(null);
                
                // Cancel edit mode if active
                if (_autoEditOnSelect && IsPlacementActive && _selectedObjectForEdit != null)
                {
                    CancelObjectEdit();
                }
            }
        }

        #endregion

        #region Input Management

        /// <summary>
        /// Initialize input actions and bind events
        /// </summary>
        private void InitializeInputActions()
        {
            // Mode toggle action - always active
            if (_toggleModeActionRef != null)
            {
                _toggleModeAction = _toggleModeActionRef.action;
                _toggleModeAction.performed += OnToggleModePerformed;
            }

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
                
                if (_transformModeToggleAxisActionRef != null)
                {
                    _transformModeToggleAxisAction = _transformModeToggleAxisActionRef.action;
                    _transformModeToggleAxisAction.performed += OnTransformModeToggleAxisPerformed;
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
            // Mode toggle is always enabled
            _toggleModeAction?.Enable();

            // Only enable build actions if in Build Mode
            if (_currentMode == SandboxMode.Build)
            {
                EnableBuildInputActions();
            }
        }

        private void DisableInputActions()
        {
            // Mode toggle is always enabled (even when component disabled)
            _toggleModeAction?.Disable();
            
            // Disable all build actions
            DisableBuildInputActions();
        }

        /// <summary>
        /// Enable all build-related input actions (used in Build Mode)
        /// </summary>
        private void EnableBuildInputActions()
        {
            _exitAllModesActionRef?.action?.Enable();

            if (_enableHotkeys)
            {
                _moveHotkeyActionRef?.action?.Enable();
                _rotateHotkeyActionRef?.action?.Enable();
                _scaleHotkeyActionRef?.action?.Enable();
                _transformModeIncreaseAction?.Enable();
                _transformModeDecreaseAction?.Enable();
                _transformModeToggleAxisAction?.Enable();
            }
            
            _pointerPositionAction?.Enable();
            _leftClickAction?.Enable();
            _rightClickAction?.Enable();
            _mouseScrollAction?.Enable();
            _cancelPlacementAction?.Enable();
        }

        /// <summary>
        /// Disable all build-related input actions (used in Play Mode)
        /// </summary>
        private void DisableBuildInputActions()
        {
            _exitAllModesActionRef?.action?.Disable();
            _moveHotkeyActionRef?.action?.Disable();
            _rotateHotkeyActionRef?.action?.Disable();
            _scaleHotkeyActionRef?.action?.Disable();
            _transformModeIncreaseAction?.Disable();
            _transformModeDecreaseAction?.Disable();
            _transformModeToggleAxisAction?.Disable();
            
            _pointerPositionAction?.Disable();
            _leftClickAction?.Disable();
            _rightClickAction?.Disable();
            _mouseScrollAction?.Disable();
            _cancelPlacementAction?.Disable();
        }

        #endregion

        #region Input Action Callbacks

        private void OnToggleModePerformed(InputAction.CallbackContext context)
        {
            ToggleMode();
        }

        private void OnExitAllModesPerformed(InputAction.CallbackContext context)
        {
            ExitAllTransformModes();
        }

        private void OnMoveHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetTransformMode(TransformModeType.Position);
                
                // Apply to selected item if any
                if (_lastSelectedItem != null && _lastSelectedItem.EnableTransformControls)
                {
                    _lastSelectedItem.SetTransformModeType(TransformModeType.Position);
                }
            }
        }

        private void OnRotateHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetTransformMode(TransformModeType.Rotation);
                
                // Apply to selected item if any
                if (_lastSelectedItem != null && _lastSelectedItem.EnableTransformControls)
                {
                    _lastSelectedItem.SetTransformModeType(TransformModeType.Rotation);
                }
            }
        }

        private void OnScaleHotkeyPerformed(InputAction.CallbackContext context)
        {
            if (_enableHotkeys)
            {
                SetTransformMode(TransformModeType.Scale);
                
                // Apply to selected item if any
                if (_lastSelectedItem != null && _lastSelectedItem.EnableTransformControls)
                {
                    _lastSelectedItem.SetTransformModeType(TransformModeType.Scale);
                }
            }
        }

        private void OnTransformModeIncreasePerformed(InputAction.CallbackContext context)
        {
            if (!_enableHotkeys) return;
            
            // Priority 1: Placement object (if in placement mode)
            if (_currentPlacementObject != null)
            {
                var transformableItem = _currentPlacementObject.GetComponent<TransformableItem>();
                transformableItem?.IncreaseTransformValue();
            }
            // Priority 2: Selected item
            else if (_lastSelectedItem != null)
            {
                _lastSelectedItem.IncreaseTransformValue();
            }
            // Priority 3: Active transform item (fallback)
            else if (_currentActiveTransformItem != null)
            {
                _currentActiveTransformItem.IncreaseTransformValue();
            }
        }

        private void OnTransformModeDecreasePerformed(InputAction.CallbackContext context)
        {
            if (!_enableHotkeys) return;
            
            // Priority 1: Placement object (if in placement mode)
            if (_currentPlacementObject != null)
            {
                var transformableItem = _currentPlacementObject.GetComponent<TransformableItem>();
                transformableItem?.DecreaseTransformValue();
            }
            // Priority 2: Selected item
            else if (_lastSelectedItem != null)
            {
                _lastSelectedItem.DecreaseTransformValue();
            }
            // Priority 3: Active transform item (fallback)
            else if (_currentActiveTransformItem != null)
            {
                _currentActiveTransformItem.DecreaseTransformValue();
            }
        }

        private void OnTransformModeToggleAxisPerformed(InputAction.CallbackContext context)
        {
            if (!_enableHotkeys) return;
            
            // Toggle the axis
            ToggleTransformAxis();
            
            Debug.Log($"[INPUT] Toggle axis performed - Current axis: {_currentTransformAxis}");
        }

        private void OnCancelPlacementPerformed(InputAction.CallbackContext context)
        {
            if (IsPlacementActive)
            {
                if (_selectedObjectForEdit != null)
                {
                    CancelObjectEdit();
                }
                else
                {
                    CancelPlacement();
                }
            }
        }

        #endregion

        #region Raycast Input Handling

        /// <summary>
        /// Handle raycast-based input detection
        /// </summary>
        private void HandleRaycastInput()
        {
            if (_sceneCamera == null) return;

            Vector2 inputPosition = GetInputPosition();
            TransformableItem hitItem = RaycastForItem(inputPosition);

            HandleHoverState(hitItem);

            if (_leftClickAction == null) return;

            bool leftButtonDown = _leftClickAction.WasPressedThisFrame();
            bool leftButtonUp = _leftClickAction.WasReleasedThisFrame();
            bool leftButtonHeld = _leftClickAction.IsPressed();

            if (leftButtonDown)
            {
                HandlePointerDown(inputPosition, hitItem);
            }

            if (leftButtonHeld && _isPointerDown)
            {
                HandlePointerDrag(inputPosition);
            }

            if (leftButtonUp && _isPointerDown)
            {
                HandlePointerUp(inputPosition, hitItem);
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
            
            return Mouse.current?.position.ReadValue() ?? Vector2.zero;
        }

        /// <summary>
        /// Perform raycast to detect TransformableItem
        /// </summary>
        private TransformableItem RaycastForItem(Vector2 screenPosition)
        {
            Ray ray = _sceneCamera.ScreenPointToRay(screenPosition);

            // Use selection layers when not in placement mode, otherwise use placement layers
            LayerMask maskToUse = IsPlacementActive ? _placementLayers : _selectionLayers;

            if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, maskToUse))
            {
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
                if (_currentHoverItem != null)
                {
                    _currentHoverItem.SetHoverState(false);
                }

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
        }

        /// <summary>
        /// Handle pointer drag event
        /// </summary>
        private void HandlePointerDrag(Vector2 inputPosition)
        {
            float dragDistance = Vector2.Distance(_pointerDownPosition, inputPosition);

            if (!_isDragging && dragDistance > _dragThreshold)
            {
                _isDragging = true;
            }
        }

        /// <summary>
        /// Handle pointer up event
        /// </summary>
        private void HandlePointerUp(Vector2 inputPosition, TransformableItem hitItem)
        {
            if (!_isDragging)
            {
                // Click detected (no drag)
                
                // If in placement mode, clicking anywhere confirms placement
                if (IsPlacementActive)
                {
                    if (_selectedObjectForEdit != null)
                    {
                        ConfirmObjectEdit();
                    }
                    else
                    {
                        ConfirmPlacement();
                    }
                }
                // Normal selection mode
                else if (hitItem != null)
                {
                    // Clicked on an object - select it
                    HandleClick(inputPosition, hitItem);
                }
                else
                {
                    // Clicked on empty space - clear selection
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
            
            if (_clickCount == 1)
            {
                SelectItem(hitItem);
            }
        }

        #endregion

        #region Selection Management

        /// <summary>
        /// Register for events on all existing transformable items in the scene
        /// </summary>
        private void RegisterExistingTransformableItems()
        {
            var existingItems = FindObjectsByType<TransformableItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var item in existingItems)
            {
                RegisterTransformableItem(item);
            }
        }

        /// <summary>
        /// Register a transformable item for selection management
        /// </summary>
        private void RegisterTransformableItem(TransformableItem item)
        {
            if (item == null) return;

            item.OnSelectionChanged -= OnItemSelectionChanged;
            item.OnSelectionChanged += OnItemSelectionChanged;

            if (item.EnableTransformControls)
            {
                RegisterItemForTransformControl(item);
            }
        }

        /// <summary>
        /// Unregister a transformable item from selection management
        /// </summary>
        private void UnregisterTransformableItem(TransformableItem item)
        {
            if (item == null) return;

            item.OnSelectionChanged -= OnItemSelectionChanged;
            UnregisterItemFromTransformControl(item);

            if (_selectedItems.Contains(item))
            {
                _selectedItems.Remove(item);
            }
        }

        /// <summary>
        /// Handle selection state change from a transformable item
        /// </summary>
        private void OnItemSelectionChanged(TransformableItem item, bool isSelected)
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
        private void SelectItem(TransformableItem item)
        {
            if (item == null || _selectedItems.Contains(item))
                return;

            // Single selection only - clear others
            ClearSelection();

            _selectedItems.Add(item);
            _lastSelectedItem = item;
            item.SetSelectedState(true);
            
            _selectedObject = item.gameObject;
            OnObjectSelected?.Invoke(_selectedObject);
            
            // Enable transform controls for the item
            if (item.EnableTransformControls)
            {
                RegisterItemForTransformControl(item);
                
                // Set default transform mode to Position
                item.SetTransformModeType(TransformModeType.Position);
            }
            
            // Automatically start edit mode if enabled
            if (_autoEditOnSelect)
            {
                BeginObjectEdit(item.gameObject);
            }
        }

        /// <summary>
        /// Deselect an item
        /// </summary>
        private void DeselectItem(TransformableItem item)
        {
            if (item == null || !_selectedItems.Contains(item)) return;

            _selectedItems.Remove(item);
            item.SetSelectedState(false);
            
            // Exit transform mode when deselecting
            if (item.EnableTransformControls)
            {
                item.SetTransformModeType(TransformModeType.None);
                UnregisterItemFromTransformControl(item);
            }

            if (_lastSelectedItem == item)
            {
                _lastSelectedItem = null;
            }
            
            if (_selectedObject == item.gameObject)
            {
                _selectedObject = null;
                OnObjectSelected?.Invoke(null);
                
                if (_autoEditOnSelect && IsPlacementActive && _selectedObjectForEdit != null)
                {
                    CancelObjectEdit();
                }
            }
        }

        /// <summary>
        /// Clear all selection
        /// </summary>
        private void ClearSelection()
        {
            var itemsToDeselect = new List<TransformableItem>(_selectedItems);
            
            foreach (var item in itemsToDeselect)
            {
                if (item != null)
                {
                    item.SetSelectedState(false);
                    
                    // Exit transform mode when clearing selection
                    if (item.EnableTransformControls)
                    {
                        item.SetTransformModeType(TransformModeType.None);
                        UnregisterItemFromTransformControl(item);
                    }
                }
            }

            _selectedItems.Clear();
            _lastSelectedItem = null;
            _selectedObject = null;
        }

        #endregion

        #region Transform Control Management

        /// <summary>
        /// Register item for transform control
        /// </summary>
        private void RegisterItemForTransformControl(TransformableItem item)
        {
            if (item != null)
            {
                item.OnTransformModeTypeChanged += OnItemTransformModeTypeChanged;
            }
        }

        /// <summary>
        /// Unregister item from transform control
        /// </summary>
        private void UnregisterItemFromTransformControl(TransformableItem item)
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
                _activeTransformItems.Remove(item);
                if (_currentActiveTransformItem == item)
                {
                    _currentActiveTransformItem = null;
                }
            }
            else
            {
                ExitOtherTransformModes(item);

                if (!_activeTransformItems.Contains(item))
                {
                    _activeTransformItems.Add(item);
                }
                _currentActiveTransformItem = item;
            }
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
                    item.SetTransformModeType(TransformModeType.None);
                }
            }
        }

        /// <summary>
        /// Exit all transform modes
        /// </summary>
        private void ExitAllTransformModes()
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

        #endregion

        #region Helper Methods

        private void BindDraggableEvents(TransformableItem draggable)
        {
            // Register the item for selection management
            RegisterTransformableItem(draggable);
        }

        /// <summary>
        /// Calculate offset based on pivot point and size
        /// </summary>
        private Vector3 CalculatePivotOffset(PivotPoint pivot, Vector3 size)
        {
            float halfX = size.x / 2f;
            float halfY = size.y / 2f;
            float halfZ = size.z / 2f;
            
            switch (pivot)
            {
                // Center
                case PivotPoint.Center:
                    return Vector3.zero;
                
                // Face Centers (6)
                case PivotPoint.FrontCenter:
                    return new Vector3(0, 0, -halfZ);
                
                case PivotPoint.BackCenter:
                    return new Vector3(0, 0, halfZ);
                
                case PivotPoint.LeftCenter:
                    return new Vector3(-halfX, 0, 0);
                
                case PivotPoint.RightCenter:
                    return new Vector3(halfX, 0, 0);
                
                case PivotPoint.TopCenter:
                    return new Vector3(0, halfY, 0);
                
                case PivotPoint.BottomCenter:
                    return new Vector3(0, -halfY, 0);
                
                // Bottom Edge Centers (4)
                case PivotPoint.BottomFrontEdge:
                    return new Vector3(0, -halfY, -halfZ);
                
                case PivotPoint.BottomBackEdge:
                    return new Vector3(0, -halfY, halfZ);
                
                case PivotPoint.BottomLeftEdge:
                    return new Vector3(-halfX, -halfY, 0);
                
                case PivotPoint.BottomRightEdge:
                    return new Vector3(halfX, -halfY, 0);
                
                // Top Edge Centers (4)
                case PivotPoint.TopFrontEdge:
                    return new Vector3(0, halfY, -halfZ);
                
                case PivotPoint.TopBackEdge:
                    return new Vector3(0, halfY, halfZ);
                
                case PivotPoint.TopLeftEdge:
                    return new Vector3(-halfX, halfY, 0);
                
                case PivotPoint.TopRightEdge:
                    return new Vector3(halfX, halfY, 0);
                
                // Vertical Edge Centers (4)
                case PivotPoint.FrontLeftEdge:
                    return new Vector3(-halfX, 0, -halfZ);
                
                case PivotPoint.FrontRightEdge:
                    return new Vector3(halfX, 0, -halfZ);
                
                case PivotPoint.BackLeftEdge:
                    return new Vector3(-halfX, 0, halfZ);
                
                case PivotPoint.BackRightEdge:
                    return new Vector3(halfX, 0, halfZ);
                
                // Bottom Corners (4)
                case PivotPoint.BottomFrontLeft:
                    return new Vector3(-halfX, -halfY, -halfZ);
                
                case PivotPoint.BottomFrontRight:
                    return new Vector3(halfX, -halfY, -halfZ);
                
                case PivotPoint.BottomBackLeft:
                    return new Vector3(-halfX, -halfY, halfZ);
                
                case PivotPoint.BottomBackRight:
                    return new Vector3(halfX, -halfY, halfZ);
                
                // Top Corners (4)
                case PivotPoint.TopFrontLeft:
                    return new Vector3(-halfX, halfY, -halfZ);
                
                case PivotPoint.TopFrontRight:
                    return new Vector3(halfX, halfY, -halfZ);
                
                case PivotPoint.TopBackLeft:
                    return new Vector3(-halfX, halfY, halfZ);
                
                case PivotPoint.TopBackRight:
                    return new Vector3(halfX, halfY, halfZ);
                
                default:
                    return Vector3.zero;
            }
        }

        private void UpdateObjectInScene(TransformableItem draggable)
        {
            if (_currentScene == null) return;

            var placedObjectData = _currentScene.GetPlacedObject(draggable.ObjectId);
            if (placedObjectData != null)
            {
                placedObjectData.position = draggable.transform.position;
                placedObjectData.rotation = draggable.transform.eulerAngles;
                placedObjectData.scale = draggable.transform.localScale;
                _currentScene.lastModified = System.DateTime.Now;
            }
        }

        private void UpdateSceneConfiguration()
        {
            foreach (var kvp in _placedObjects)
            {
                var obj = kvp.Value;
                var draggable = obj.GetComponent<TransformableItem>();
                if (draggable != null)
                {
                    UpdateObjectInScene(draggable);
                }
            }
        }

        private void LoadSceneConfiguration(SceneConfiguration sceneConfig)
        {
            // Clear current scene GameObjects and runtime state first
            // Important: Clear before setting _currentScene to avoid clearing the new scene's data
            if (_placedObjects != null)
            {
                var objectsToRemove = new List<GameObject>(_placedObjects.Values);
                foreach (var obj in objectsToRemove)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                _placedObjects.Clear();
            }
            
            SelectObject(null);

            // Now set the new scene (after clearing runtime state)
            _currentScene = sceneConfig;
            _currentSceneName = sceneConfig.sceneName;

            // Recreate all placed objects
            foreach (var placedObjectData in sceneConfig.placedObjects)
            {
                var objectData = _objectLibrary.GetObjectById(placedObjectData.objectDataId);
                if (objectData?.prefab != null)
                {
                    GameObject obj = Instantiate(objectData.prefab, _stageArea);
                    obj.transform.position = placedObjectData.position;
                    obj.transform.eulerAngles = placedObjectData.rotation;
                    obj.transform.localScale = placedObjectData.scale;
                    obj.name = placedObjectData.customName ?? objectData.displayName;

                    var draggable = obj.GetComponent<TransformableItem>();
                    if (draggable == null)
                    {
                        draggable = obj.AddComponent<TransformableItem>();
                    }

                    draggable.SetObjectData(placedObjectData.objectDataId, placedObjectData.id);
                    BindDraggableEvents(draggable);

                    _placedObjects[placedObjectData.id] = obj;
                }
            }

            OnSceneLoaded?.Invoke(_currentScene);
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            if (!_snapToGrid) return position;

            float snappedX = Mathf.Round(position.x / _gridSize) * _gridSize;
            float snappedZ = Mathf.Round(position.z / _gridSize) * _gridSize;
            return new Vector3(snappedX, position.y, snappedZ);
        }

        /// <summary>
        /// Calculate the grid's Y position based on scene bounds and pivot offsets
        /// </summary>
        private float GetGridYPosition()
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;
            Vector3 sceneBoundsPivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            Vector3 gridPivotOffset = CalculatePivotOffset(_gridPivotOffset, _sceneBounds);
            
            // Calculate grid Y: stageY + sceneBoundsOffsetY + gridOffsetY - sceneBoundsPivotY + gridPivotY
            float gridY = stageCenter.y + _sceneBoundsOffset.y + _gridOffset.y - sceneBoundsPivotOffset.y + gridPivotOffset.y;
            return gridY;
        }

        private Vector3 GetSurfacePosition(Vector3 position)
        {
            // Calculate grid Y position based on scene bounds and pivots
            float gridY = GetGridYPosition();
            float defaultHeight = gridY + _defaultPlacementHeight; // Default height is relative to grid
            float minimumHeight = gridY + _minimumPlacementHeight; // Minimum height is relative to grid
            
            // If raycast placement is disabled, always use default placement height
            if (!_useRaycastForPlacement)
            {
                Vector3 result = new Vector3(position.x, defaultHeight, position.z);
                return result;
            }

            // Try to find surface through raycast
            Vector3 rayStart = position + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, _placementLayers))
            {
                // Found surface, use the hit point's Y position but respect minimum height
                float finalY = Mathf.Max(hit.point.y, minimumHeight);
                Vector3 result = new Vector3(position.x, finalY, position.z);
                return result;
            }

            // No surface found, use default placement height (respecting minimum)
            float finalDefaultHeight = Mathf.Max(defaultHeight, minimumHeight);
            Vector3 defaultResult = new Vector3(position.x, finalDefaultHeight, position.z);
            return defaultResult;
        }

        private GameObject GetObjectAtScreenPosition(Vector2 screenPosition)
        {
            if (_sceneCamera == null) return null;

            Ray ray = _sceneCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.collider.gameObject;
            }

            return null;
        }

        /// <summary>
        /// Convert screen position to world position for drop indicator
        /// </summary>
        private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
        {
            if (_sceneCamera == null)
            {
                return Vector3.zero;
            }

            Ray ray = _sceneCamera.ScreenPointToRay(screenPosition);
            Vector3 worldPos;

            // Calculate grid Y position and heights relative to grid
            float gridY = GetGridYPosition();
            float defaultHeight = gridY + _defaultPlacementHeight;
            float minimumHeight = gridY + _minimumPlacementHeight;

            // If raycast placement is disabled or consistent height is enabled, always project to default height
            if (!_useRaycastForPlacement || _useConsistentHeight)
            {
                Vector3 rayDirection = ray.direction.normalized;
                if (Mathf.Abs(rayDirection.y) < 0.001f)
                {
                    return new Vector3(0, defaultHeight, 0);
                }

                float t = (defaultHeight - ray.origin.y) / rayDirection.y;
                worldPos = ray.origin + rayDirection * t;
                return worldPos;
            }

            // Try to hit the ground or existing objects
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _placementLayers))
            {
                float finalY = Mathf.Max(hit.point.y, minimumHeight);
                worldPos = new Vector3(hit.point.x, finalY, hit.point.z);
            }
            else
            {
                Vector3 rayDirection = ray.direction.normalized;
                if (Mathf.Abs(rayDirection.y) < 0.001f)
                {
                    return new Vector3(0, defaultHeight, 0);
                }
                float t = (defaultHeight - ray.origin.y) / rayDirection.y;
                Vector3 projectedPos = ray.origin + rayDirection * t;
                float finalY = Mathf.Max(projectedPos.y, minimumHeight);
                worldPos = new Vector3(projectedPos.x, finalY, projectedPos.z);
            }

            return worldPos;
        }

        /// <summary>
        /// Check if a position is within the scene bounds
        /// </summary>
        private bool IsPositionInSceneBounds(Vector3 position)
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;
            Vector3 pivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            // We SUBTRACT the pivot offset to position the bounds relative to the pivot point
            Vector3 boundsCenter = stageCenter + _sceneBoundsOffset - pivotOffset;
            Vector3 localPos = position - boundsCenter;

            bool withinX = Mathf.Abs(localPos.x) <= _sceneBounds.x / 2f;
            bool withinY = Mathf.Abs(localPos.y) <= _sceneBounds.y / 2f;
            bool withinZ = Mathf.Abs(localPos.z) <= _sceneBounds.z / 2f;
            
            bool result = withinX && withinY && withinZ;
                   
            return result;
        }

        /// <summary>
        /// Show drop indicator at specified position
        /// </summary>
        private void ShowDropIndicator(Vector3 position)
        {
            if (!_enableDropIndicator)
            {
                return;
            }

            // Create drop indicator if it doesn't exist
            if (_currentDropIndicator == null)
            {
                CreateDropIndicator();
            }

            if (_currentDropIndicator != null)
            {
                _currentDropIndicator.SetActive(true);

                UpdateDropIndicator(position);

            }
        }

        /// <summary>
        /// Update drop indicator position and appearance
        /// </summary>
        private void UpdateDropIndicator(Vector3 position)
        {
            if (!_enableDropIndicator)
            {
                return;
            }

            // Create drop indicator if it doesn't exist
            if (_currentDropIndicator == null)
            {
                CreateDropIndicator();

                if (_currentDropIndicator != null)
                {
                    _currentDropIndicator.SetActive(true);
                }
                else
                {
                    return;
                }
            }

            // Ensure indicator is active
            if (!_currentDropIndicator.activeInHierarchy)
            {
                _currentDropIndicator.SetActive(true);
            }

            // Snap to grid if enabled
            Vector3 finalPosition = _snapToGrid ? SnapToGrid(position) : position;
            // Note: GetWorldPositionFromScreen already handles surface detection, 
            // so we don't need to call GetSurfacePosition again here


            _dragPreviewPosition = finalPosition;
            _currentDropIndicator.transform.position = finalPosition;

            // Update color based on validity
            bool isValidPosition = IsPositionInSceneBounds(finalPosition);
            Color indicatorColor = isValidPosition ? _validDropColor : _invalidDropColor;


            // Apply color to the indicator
            var renderer = _currentDropIndicator.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = indicatorColor;
            }
        }

        /// <summary>
        /// Hide drop indicator
        /// </summary>
        private void HideDropIndicator()
        {
            if (_currentDropIndicator != null)
            {
                _currentDropIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Create drop indicator GameObject
        /// </summary>
        private void CreateDropIndicator()
        {
            if (_dropIndicatorPrefab != null)
            {
                try
                {
                    _currentDropIndicator = Instantiate(_dropIndicatorPrefab);
                    _currentDropIndicator.name = "Drop Indicator (Prefab)";
                }
                catch (System.Exception)
                {
                    _currentDropIndicator = null;
                }
            }

            // If prefab failed or wasn't assigned, create default
            if (_currentDropIndicator == null)
            {
                try
                {
                    // Create default drop indicator
                    _currentDropIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    _currentDropIndicator.name = "Drop Indicator (Default)";

                    // Remove collider to prevent interference
                    var collider = _currentDropIndicator.GetComponent<Collider>();
                    if (collider != null)
                    {
                        DestroyImmediate(collider);
                    }

                    // Scale to indicator size
                    _currentDropIndicator.transform.localScale = new Vector3(_dropIndicatorSize, 0.1f, _dropIndicatorSize);

                    // Create a more visible material
                    var renderer = _currentDropIndicator.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material indicatorMaterial = new Material(Shader.Find("Standard"));
                        indicatorMaterial.color = _validDropColor;
                        indicatorMaterial.SetFloat("_Metallic", 0f);
                        indicatorMaterial.SetFloat("_Glossiness", 0.5f);
                        renderer.material = indicatorMaterial;
                    }
                }
                catch (System.Exception)
                {
                    return;
                }
            }

            if (_currentDropIndicator != null)
            {
                _currentDropIndicator.SetActive(false);
            }
        }

        private string GetDefaultSavePath()
        {
            string safeName = SanitizeFileName(_currentSceneName ?? "Untitled");
            return System.IO.Path.Combine(_defaultSavePath, $"{safeName}.json");
        }

        /// <summary>
        /// Sanitize filename to remove invalid characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Untitled";
            
            // Remove invalid filename characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string sanitized = string.Join("_", fileName.Split(invalidChars, System.StringSplitOptions.RemoveEmptyEntries));
            
            // Trim and ensure not empty
            sanitized = sanitized.Trim();
            return string.IsNullOrEmpty(sanitized) ? "Untitled" : sanitized;
        }

        /// <summary>
        /// Ensure directory exists before saving
        /// </summary>
        private bool EnsureDirectoryExists(string filePath)
        {
            try
            {
                string directory = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create directory: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Draw gizmos for the scene and selected objects
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_enableGizmos && !_enableSceneGizmos) return;

            // Draw scene gizmos
            if (_enableSceneGizmos)
            {
                DrawSceneGizmos();
            }

            // Draw object gizmos
            if (_enableGizmos && _selectedObject != null)
            {
                DrawObjectGizmos(_selectedObject);
            }

            // Draw drop indicator gizmos when dragging
            if (_enableDropIndicator && _isDraggingObject)
            {
                DrawDropIndicatorGizmos();
            }

            // Draw placement preview gizmos
            if (_placementState == PlacementState.Active && _currentPlacementObject != null)
            {
                DrawPlacementPreviewGizmos();
            }
        }

        /// <summary>
        /// Draw scene-level gizmos (grid, bounds, stage area)
        /// </summary>
        private void DrawSceneGizmos()
        {
            // Draw scene grid
            if (_showSceneGrid)
            {
                DrawSceneGrid();
            }

            // Draw scene bounds
            if (_showSceneBounds)
            {
                DrawSceneBounds();
            }

            // Draw stage area
            if (_showStageAreaGizmo && _stageArea != null)
            {
                DrawStageArea();
            }

            // Draw placement height level
            if (_showPlacementHeightGizmo)
            {
                DrawPlacementHeight();
            }
        }

        /// <summary>
        /// Draw gizmos for a selected object
        /// </summary>
        private void DrawObjectGizmos(GameObject obj)
        {
            if (obj == null) return;

            Bounds bounds = CalculateObjectBounds(obj);

            // Draw bounding box
            if (_showBoundsGizmo)
            {
                Gizmos.color = _gizmoBoundsColor;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            // Draw coordinate axes
            if (_showAxesGizmo)
            {
                DrawObjectAxes(obj.transform, bounds.center);
            }

            // Draw manipulation handles
            if (_showHandlesGizmo)
            {
                DrawManipulationHandles(bounds);
            }
        }

        /// <summary>
        /// Draw the scene grid (2D plane based on scene bounds)
        /// </summary>
        private void DrawSceneGrid()
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;
            
            // First, align grid with scene bounds (subtract scene bounds pivot)
            // Then, apply grid pivot offset (add grid offset)
            Vector3 sceneBoundsPivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            Vector3 gridPivotOffset = CalculatePivotOffset(_gridPivotOffset, _sceneBounds);
            
            Vector3 center = stageCenter + _sceneBoundsOffset + _gridOffset - sceneBoundsPivotOffset + gridPivotOffset;
            
            // Use scene bounds to determine grid extent (X, Z only)
            float gridExtentX = _sceneBounds.x / 2f;
            float gridExtentZ = _sceneBounds.z / 2f;
            
            int gridLinesX = Mathf.RoundToInt(_sceneBounds.x / _gridSize);
            int gridLinesZ = Mathf.RoundToInt(_sceneBounds.z / _gridSize);

            Gizmos.color = Color.gray;

            // Draw vertical lines (along Z-axis)
            for (int i = 0; i <= gridLinesX; i++)
            {
                float x = center.x - gridExtentX + (i * _gridSize);
                Vector3 start = new Vector3(x, center.y, center.z - gridExtentZ);
                Vector3 end = new Vector3(x, center.y, center.z + gridExtentZ);

                // Center line in different color
                if (Mathf.Approximately(x, center.x))
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(start, end);
                    Gizmos.color = Color.gray;
                }
                else
                {
                    Gizmos.DrawLine(start, end);
                }
            }

            // Draw horizontal lines (along X-axis)
            for (int i = 0; i <= gridLinesZ; i++)
            {
                float z = center.z - gridExtentZ + (i * _gridSize);
                Vector3 start = new Vector3(center.x - gridExtentX, center.y, z);
                Vector3 end = new Vector3(center.x + gridExtentX, center.y, z);

                // Center line in different color
                if (Mathf.Approximately(z, center.z))
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(start, end);
                    Gizmos.color = Color.gray;
                }
                else
                {
                    Gizmos.DrawLine(start, end);
                }
            }

            // Draw origin marker
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(center, 0.1f);
        }

        /// <summary>
        /// Draw scene boundaries
        /// </summary>
        private void DrawSceneBounds()
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;
            Vector3 pivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            // We SUBTRACT the pivot offset to position the bounds relative to the pivot point
            Vector3 center = stageCenter + _sceneBoundsOffset - pivotOffset;
            Vector3 size = _sceneBounds; // Use configurable scene bounds

            Gizmos.color = _sceneBoundsColor;
            Gizmos.DrawWireCube(center, size);

            // Draw corner markers
            float markerSize = 0.2f;
            Vector3[] corners = {
                center + new Vector3(-size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, -size.y/2, size.z/2),
                center + new Vector3(size.x/2, -size.y/2, size.z/2)
            };

            foreach (var corner in corners)
            {
                Gizmos.DrawWireCube(corner, Vector3.one * markerSize);
            }
        }

        /// <summary>
        /// Draw stage area gizmo
        /// </summary>
        private void DrawStageArea()
        {
            if (_stageArea == null) return;

            Vector3 stagePos = _stageArea.position;
            Vector3 stageScale = _stageArea.localScale;

            // Draw stage area bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(stagePos, stageScale);

            // Draw floor
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 floorPos = new Vector3(stagePos.x, stagePos.y - stageScale.y / 2f, stagePos.z);
            Vector3 floorSize = new Vector3(stageScale.x, 0.01f, stageScale.z);
            Gizmos.DrawCube(floorPos, floorSize);
        }

        /// <summary>
        /// Draw default placement height level
        /// </summary>
        private void DrawPlacementHeight()
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;
            
            // First, align with scene bounds (subtract scene bounds pivot)
            // Then, apply grid pivot offset (add grid offset)
            Vector3 sceneBoundsPivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            Vector3 gridPivotOffset = CalculatePivotOffset(_gridPivotOffset, _sceneBounds);
            
            Vector3 center = stageCenter + _sceneBoundsOffset + _gridOffset - sceneBoundsPivotOffset + gridPivotOffset;
            
            // Calculate placement height relative to grid
            float placementHeight = center.y + _defaultPlacementHeight;
            
            // Use scene bounds to determine placement height grid extent
            float gridExtentX = _sceneBounds.x / 2f;
            float gridExtentZ = _sceneBounds.z / 2f;
            
            int gridLinesX = Mathf.Max(2, Mathf.RoundToInt(_sceneBounds.x / _gridSize));
            int gridLinesZ = Mathf.Max(2, Mathf.RoundToInt(_sceneBounds.z / _gridSize));

            // Draw placement height plane
            Gizmos.color = _placementHeightColor;

            // Draw horizontal grid lines at placement height
            for (int i = 0; i <= gridLinesX; i++)
            {
                float x = center.x - gridExtentX + (i * _gridSize);
                Vector3 start = new Vector3(x, placementHeight, center.z - gridExtentZ);
                Vector3 end = new Vector3(x, placementHeight, center.z + gridExtentZ);
                Gizmos.DrawLine(start, end);
            }

            for (int i = 0; i <= gridLinesZ; i++)
            {
                float z = center.z - gridExtentZ + (i * _gridSize);
                Vector3 start = new Vector3(center.x - gridExtentX, placementHeight, z);
                Vector3 end = new Vector3(center.x + gridExtentX, placementHeight, z);
                Gizmos.DrawLine(start, end);
            }

            // Draw placement height indicator at center
            Vector3 placementPos = new Vector3(center.x, placementHeight, center.z);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(placementPos, 0.2f);

            // Draw vertical line from ground to placement height
            Vector3 groundPos = new Vector3(center.x, center.y, center.z);
            Gizmos.color = _placementHeightColor;
            Gizmos.DrawLine(groundPos, placementPos);

            // Draw height text indicator (visual marker)
            Vector3[] heightMarkers = {
                placementPos + Vector3.right * 2f,
                placementPos + Vector3.left * 2f,
                placementPos + Vector3.forward * 2f,
                placementPos + Vector3.back * 2f
            };

            foreach (var marker in heightMarkers)
            {
                Gizmos.DrawWireCube(marker, Vector3.one * 0.1f);
            }
        }

        /// <summary>
        /// Draw coordinate axes for an object
        /// </summary>
        private void DrawObjectAxes(Transform objTransform, Vector3 center)
        {
            // X axis (Red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, center + objTransform.right * _gizmoAxisLength);

            // Y axis (Green)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + objTransform.up * _gizmoAxisLength);

            // Z axis (Blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(center, center + objTransform.forward * _gizmoAxisLength);
        }

        /// <summary>
        /// Draw manipulation handles
        /// </summary>
        private void DrawManipulationHandles(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            float handleSize = 0.1f;

            Gizmos.color = Color.white;

            // Corner handles
            Vector3[] corners = {
                center + new Vector3(-size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, -size.y/2, size.z/2),
                center + new Vector3(size.x/2, -size.y/2, size.z/2),
                center + new Vector3(-size.x/2, size.y/2, -size.z/2),
                center + new Vector3(size.x/2, size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, size.y/2, size.z/2),
                center + new Vector3(size.x/2, size.y/2, size.z/2)
            };

            foreach (var corner in corners)
            {
                Gizmos.DrawWireCube(corner, Vector3.one * handleSize);
            }

            // Face center handles
            Gizmos.color = _gizmoBoundsColor;
            Vector3[] faces = {
                center + new Vector3(-size.x/2, 0, 0), // Left
                center + new Vector3(size.x/2, 0, 0),  // Right
                center + new Vector3(0, -size.y/2, 0), // Bottom
                center + new Vector3(0, size.y/2, 0),  // Top
                center + new Vector3(0, 0, -size.z/2), // Back
                center + new Vector3(0, 0, size.z/2)   // Front
            };

            foreach (var face in faces)
            {
                Gizmos.DrawWireCube(face, Vector3.one * handleSize * 0.7f);
            }
        }

        /// <summary>
        /// Calculate bounds for an object
        /// </summary>
        private Bounds CalculateObjectBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();

            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return bounds;
            }
            else if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
                return bounds;
            }
            else
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }
        }

        /// <summary>
        /// Draw drop indicator gizmos during drag operations
        /// </summary>
        private void DrawDropIndicatorGizmos()
        {
            if (!_isDraggingObject) return;

            Vector3 position = _dragPreviewPosition;
            bool isValidPosition = IsPositionInSceneBounds(position);

            // Draw drop zone circle
            Gizmos.color = isValidPosition ? _validDropColor : _invalidDropColor;

            // Draw a circle on the ground
            float radius = _dropIndicatorSize * 0.5f;
            int segments = 32;
            float angleStep = 360f / segments;

            Vector3 prevPoint = position + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }

            // Draw vertical indicator line
            Gizmos.DrawLine(position, position + Vector3.up * 2f);

            // Draw grid snap indicator if snapping is enabled
            if (_snapToGrid)
            {
                Vector3 snappedPos = SnapToGrid(position);
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(snappedPos + Vector3.up * 0.05f, Vector3.one * _gridSize * 0.1f);
            }
        }

        /// <summary>
        /// Draw placement preview gizmos (ghost object indicators)
        /// </summary>
        private void DrawPlacementPreviewGizmos()
        {
            if (_currentPlacementObject == null) return;

            Vector3 position = _currentPlacementObject.transform.position;
            Bounds bounds = GetObjectBounds(_currentPlacementObject);

            // Draw validity indicator with color
            Gizmos.color = _isCurrentPlacementValid ? _validGhostColor : _invalidGhostColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Draw grid snap indicator if enabled
            if (_showGridSnapIndicator && _snapToGrid)
            {
                Vector3 snappedPos = SnapToGrid(position);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(snappedPos + Vector3.up * 0.05f, Vector3.one * _gridSize * 0.15f);

                // Draw line from ghost to snap point
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawLine(position, snappedPos);
            }

            // Draw surface normal indicator if enabled
            if (_showSurfaceNormal)
            {
                // Raycast down to find surface
                if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 10f, _placementLayers))
                {
                    _lastValidSurfaceNormal = hit.normal;
                    
                    // Draw normal vector
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(hit.point, hit.point + hit.normal * _surfaceNormalLength);
                    
                    // Draw surface point
                    Gizmos.DrawWireSphere(hit.point, 0.1f);
                }
                else if (_lastValidSurfaceNormal != Vector3.zero)
                {
                    // Draw last known normal if no surface hit
                    Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
                    Gizmos.DrawLine(position, position + _lastValidSurfaceNormal * _surfaceNormalLength);
                }
            }

            // Draw cost indicator (if enabled)
            if (_enableCostSystem)
            {
                Gizmos.color = _isCurrentPlacementValid ? Color.green : Color.red;
                Vector3 labelPos = bounds.center + Vector3.up * (bounds.extents.y + 0.5f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(labelPos, $"Cost: {_placementCost}");
                #endif
            }

            // Draw validation feedback
            if (!_isCurrentPlacementValid)
            {
                // Draw X indicator for invalid placement
                Gizmos.color = Color.red;
                Vector3 center = bounds.center;
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f;
                
                Gizmos.DrawLine(center + new Vector3(-size, size, 0), center + new Vector3(size, -size, 0));
                Gizmos.DrawLine(center + new Vector3(-size, -size, 0), center + new Vector3(size, size, 0));
            }
        }

        private void ShowContextMenu(GameObject obj, Vector2 screenPosition)
        {
            // TODO: Implement context menu system

        }

        private void ShowGeneralContextMenu(Vector2 screenPosition)
        {
            // TODO: Implement general context menu

        }

        private void CreatePreviewObjects()
        {
            _previewObjects.Clear();

            foreach (var kvp in _placedObjects)
            {
                var original = kvp.Value;
                var preview = Instantiate(original);

                // Disable draggable component during preview
                var draggable = preview.GetComponent<TransformableItem>();
                if (draggable != null)
                {
                    draggable.enabled = false;
                }

                _previewObjects.Add(preview);
            }
        }

        private void CleanupPreviewObjects()
        {
            foreach (var previewObj in _previewObjects)
            {
                if (previewObj != null)
                {
                    Destroy(previewObj);
                }
            }
            _previewObjects.Clear();
        }

        private void SetupTimelineForPreview()
        {
            if (_timelineDirector?.Project == null) return;

            // TODO: Set up timeline tracks for preview objects
            // This would integrate with the existing MiniTimeline system
        }

        #endregion

        private void OnDestroy()
        {
            // Disable and cleanup input actions
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

            StopPreview();

            // Cleanup drop indicator
            if (_currentDropIndicator != null)
            {
                DestroyImmediate(_currentDropIndicator);
            }

            // Cleanup ghost object
            if (_currentPlacementObject != null)
            {
                DestroyImmediate(_currentPlacementObject);
            }
        }
    }
}
