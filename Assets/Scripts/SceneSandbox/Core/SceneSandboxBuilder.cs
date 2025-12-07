using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using SceneSandbox.Data;
using SceneSandbox.Serialization;
using MiniTimeline.Core;

namespace SceneSandbox.Core
{
    // Custom UnityEvent classes for events with parameters
    [System.Serializable]
    public class SceneConfigurationEvent : UnityEvent<SceneConfiguration> { }

    [System.Serializable]
    public class GameObjectEvent : UnityEvent<GameObject> { }

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    [System.Serializable]
    public class SandboxModeEvent : UnityEvent<SandboxMode> { }
    /// <summary>
    /// Main controller for the Scene Sandbox Builder system
    /// Manages the placement, interaction, and organization of scene objects
    /// </summary>
    public class SceneSandboxBuilder : MonoBehaviour
    {
        // Phase 1: non-breaking extraction hooks
        [SerializeField] private CameraRaycaster _cameraRaycaster;
        [SerializeField] private SandboxInputManager _inputManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private PlacementSystem _placementSystem;
        [SerializeField] private SelectionManager _selectionManager;
        [SerializeField] private TransformController _transformController;
        [SerializeField] private SceneSerializer _sceneSerializer;
        [SerializeField] private PreviewController _previewController;


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
        [SerializeField] private InputActionReference _switchPlacementItemActionRef;
        [SerializeField] private InputActionReference _startPlacementActionRef;

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
        [SerializeField] private bool _showGridSnapIndicator = true;
        [SerializeField] private bool _showSurfaceNormal = true;
        [SerializeField] private float _surfaceNormalLength = 1f;

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
        [SerializeField] private float _rotationSnapDegrees = 15f; // Snap increment for rotation when grid snapping is enabled
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
        [Tooltip("Automatically load the first available project on start (Play mode only)")]
        [SerializeField] private bool _autoLoadFirstProject = false;

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

        // Transform control state
        private List<TransformableItem> _activeTransformItems = new List<TransformableItem>();

        // State
        private SceneConfiguration _currentScene;
        private SandboxProjectData _currentProject;
        private Dictionary<string, GameObject> _placedObjects;
        private GameObject _selectedObject;
        private List<GameObject> _previewObjects;

        // Drop Indicator State
        private GameObject _currentDropIndicator;

        // Placement & Transform State (migrated to PlacementSystem except transform mode/axis)
        private TransformModeType _currentTransformMode = TransformModeType.Position;
        private TransformAxis _currentTransformAxis = TransformAxis.All;
        private Quaternion _currentPlacementRotation = Quaternion.identity;
        private Vector3 _currentPlacementScale = Vector3.one;
        private Vector3 _lastValidSurfaceNormal = Vector3.up;

        [Header("Events")]
        [SerializeField] private SceneConfigurationEvent _onSceneLoaded = new SceneConfigurationEvent();
        [SerializeField] private SceneConfigurationEvent _onSceneSaved = new SceneConfigurationEvent();
        [SerializeField] private GameObjectEvent _onObjectPlaced = new GameObjectEvent();
        [SerializeField] private GameObjectEvent _onObjectRemoved = new GameObjectEvent();
        [SerializeField] private GameObjectEvent _onObjectSelected = new GameObjectEvent();
        [SerializeField] private UnityEvent _onSceneCleared = new UnityEvent();
        [SerializeField] private BoolEvent _onPreviewStateChanged = new BoolEvent();
        [SerializeField] private SandboxModeEvent _onModeChanged = new SandboxModeEvent();

        // Public accessors for backward compatibility
        public SceneConfigurationEvent OnSceneLoaded => _onSceneLoaded;
        public SceneConfigurationEvent OnSceneSaved => _onSceneSaved;
        public GameObjectEvent OnObjectPlaced => _onObjectPlaced;
        public GameObjectEvent OnObjectRemoved => _onObjectRemoved;
        public GameObjectEvent OnObjectSelected => _onObjectSelected;
        public UnityEvent OnSceneCleared => _onSceneCleared;
        public BoolEvent OnPreviewStateChanged => _onPreviewStateChanged;
        public SandboxModeEvent OnModeChanged => _onModeChanged;

        // Properties
        public SceneConfiguration CurrentScene => _sceneSerializer?.CurrentScene ?? _currentScene;
        public SandboxProjectData CurrentProject => _sceneSerializer?.CurrentProject ?? _currentProject;
        public SceneObjectLibrary ObjectLibrary => _objectLibrary;
        public bool IsInPreviewMode => _previewController?.IsInPreviewMode ?? (_previewObjects?.Count > 0);
        public GameObject SelectedObject => _selectedObject;
        public bool GizmosEnabled => _enableGizmos;
        public bool SceneGizmosEnabled => _enableSceneGizmos;
        public Vector3 SceneBounds => _sceneBounds;
        public Vector3 SceneBoundsOffset => _sceneBoundsOffset;
        public PivotPoint SceneBoundsPivot => _sceneBoundsPivot;
        public Vector3 GridOffset => _gridOffset;
        public PivotPoint GridPivotOffset => _gridPivotOffset;
        public bool DropIndicatorEnabled => _enableDropIndicator;
        public PlacementState CurrentPlacementState => _placementSystem?.State ?? PlacementState.Idle;
        public bool IsPlacementActive => _placementSystem?.IsActive ?? false;
        public TransformModeType CurrentTransformMode => _currentTransformMode;
        public TransformAxis CurrentTransformAxis => _currentTransformAxis;
        public SandboxMode CurrentMode => _currentMode;
        public bool IsInBuildMode => _currentMode == SandboxMode.Build;

        /// <summary>
        /// Set the current transform mode (Position/Rotation/Scale)
        /// </summary>
        public void SetTransformMode(TransformModeType mode)
        {
            _transformController.SetTransformMode(mode);
            // TransformController event will sync _currentTransformMode

            // Update active transform items with new axis
            UpdateActiveTransformItemsAxis();
        }

        /// <summary>
        /// Toggle the current transform axis (X, Y, Z, All)
        /// Only applicable for Rotation and Scale modes
        /// </summary>
        public void ToggleTransformAxis()
        {
            _transformController.ToggleTransformAxis();
            // TransformController event will sync _currentTransformAxis
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
            Debug.Log($"[SceneSandboxBuilder] Toggling mode from {_currentMode}");
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

            _currentMode = mode;

            if (mode == SandboxMode.Play)
            {
                // Entering Play Mode

                // Cancel any active placement
                if (_placementSystem.IsActive)
                {
                    _placementSystem.CancelPlacement();
                }

                // Exit all transform modes
                _selectionManager?.ExitAllTransformModes();

                // Disable build-related input actions
                DisableBuildInputActions();
            }
            else
            {
                // Entering Build Mode

                // Enable build-related input actions
                EnableBuildInputActions();
            }

            // Invoke mode changed event
            _onModeChanged?.Invoke(mode);
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
            // Auto-load first project if enabled
            if (_autoLoadFirstProject && Application.isPlaying)
            {
                TryAutoLoadFirstProject();
            }

            // Create new scene if no project was loaded
            if (_currentProject == null)
            {
                CreateNewScene();
            }
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
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

            // Ensure helper components exist with safe defaults
            // Input Manager (Phase 3.3 foundation)
            var existingInputManager = GetComponent<SandboxInputManager>();
            if (existingInputManager == null)
            {
                existingInputManager = gameObject.AddComponent<SandboxInputManager>();
            }
            _inputManager = existingInputManager;

            // Copy input action references BEFORE initializing (so they're available when EnableActions is called)
            _inputManager.SetInputActionReferences(
                _toggleModeActionRef,
                _exitAllModesActionRef,
                _moveHotkeyActionRef,
                _rotateHotkeyActionRef,
                _scaleHotkeyActionRef,
                _transformModeIncreaseActionRef,
                _transformModeDecreaseActionRef,
                _transformModeToggleAxisActionRef,
                _pointerPositionActionRef,
                _leftClickActionRef,
                _rightClickActionRef,
                _mouseScrollActionRef,
                _cancelPlacementActionRef,
                _switchPlacementItemActionRef,
                _startPlacementActionRef
            );

            // Initialize AFTER setting references
            _inputManager.Initialize(_enableHotkeys);

            if (_cameraRaycaster == null)
            {
                _cameraRaycaster = GetComponent<CameraRaycaster>();
                if (_cameraRaycaster == null)
                {
                    _cameraRaycaster = gameObject.AddComponent<CameraRaycaster>();
                }
            }

            if (_gridManager == null)
            {
                _gridManager = GetComponent<GridManager>();
                if (_gridManager == null)
                {
                    _gridManager = gameObject.AddComponent<GridManager>();
                }
            }

            if (_placementSystem == null)
            {
                _placementSystem = GetComponent<PlacementSystem>();
                if (_placementSystem == null)
                {
                    _placementSystem = gameObject.AddComponent<PlacementSystem>();
                }
                // Initialize with current builder dependencies
                _placementSystem.Initialize(
                    _objectLibrary,
                    _gridManager,
                    _cameraRaycaster,
                    _stageArea,
                    _snapToGrid,
                    _rotationSnapDegrees,
                    _placementLayers,
                    _maxRaycastDistance,
                    _selectionLayers,
                    _checkCollisions,
                    _collisionLayers,
                    _groundLayers,
                    _ignoreStaticObjects,
                    _requireSurfaceBelow
                );

                // Subscribe to placement events (non-breaking: delegate to existing methods)
                _placementSystem.OnPlacementStarted.RemoveAllListeners();
                _placementSystem.OnPlacementStarted.AddListener((string objectId, GameObject currentObject) =>
                {
                    if (_selectionManager != null && currentObject != null)
                    {
                        _selectionManager.SelectObject(currentObject);
                    }
                });

                _placementSystem.OnPlacementUpdated.RemoveAllListeners();
                _placementSystem.OnPlacementUpdated.AddListener((Vector3 worldPos) =>
                {
                    // Get current placement object from PlacementSystem
                    GameObject currentObject = _placementSystem?.CurrentObject;
                    if (currentObject == null)
                    {
                        return;
                    }

                    // Apply transform updates (moved from UpdateGhostPosition)
                    var transformableItem = currentObject.GetComponent<TransformableItem>();
                    SyncGridManagerSettings();

                    _transformController.SetTransformableItemPosition(worldPos, transformableItem);
                });

                _placementSystem.OnPlacementConfirmed.RemoveAllListeners();
                _placementSystem.OnPlacementConfirmed.AddListener((string objectDataId, GameObject obj) =>
                {
                    // Scene configuration and tracking remain in builder
                    var draggable = obj.GetComponent<TransformableItem>();
                    if (draggable == null)
                    {
                        draggable = obj.AddComponent<TransformableItem>();
                    }

                    draggable.enabled = true;

                    var placedObjectData = new Data.PlacedObjectData(draggable.ObjectDataId, obj.transform.position)
                    {
                        id = draggable.ObjectId,
                        rotation = obj.transform.eulerAngles,
                        scale = obj.transform.localScale,
                        customName = draggable?.name ?? obj.name
                    };

                    // Register with SceneSerializer (Phase 3.1: non-breaking)
                    if (_sceneSerializer != null)
                    {
                        _sceneSerializer.RegisterPlacedObject(placedObjectData);
                    }
                    else if (_currentScene != null)
                    {
                        _currentScene.CreateOrUpdatePlacedObject(placedObjectData);
                    }

                    _onObjectPlaced?.Invoke(obj);
                });

                _placementSystem.OnPlacementCancelled.RemoveAllListeners();
                _placementSystem.OnPlacementCancelled.AddListener((GameObject obj) =>
                {
                    if (obj != null)
                    {
                        var draggable = obj.GetComponent<TransformableItem>();
                        if (draggable != null)
                        {
                            string objectId = draggable.ObjectId;
                            // Unregister with SceneSerializer (Phase 3.1: non-breaking)
                            if (_sceneSerializer != null)
                            {
                                _sceneSerializer.UnregisterPlacedObject(objectId);
                            }
                            else if (_currentScene != null)
                            {
                                _currentScene.RemovePlacedObject(objectId);
                            }
                        }
                        Destroy(obj);
                    }
                });
            }

            // Phase 2.2: SelectionManager
            if (_selectionManager == null)
            {
                _selectionManager = GetComponent<SelectionManager>();
                if (_selectionManager == null)
                {
                    _selectionManager = gameObject.AddComponent<SelectionManager>();
                }
                // Initialize with current builder settings
                _selectionManager.Initialize(_cameraRaycaster, _selectionLayers, _autoEditOnSelect);

                // Subscribe to selection events (non-breaking: integrate with existing behavior)
                _selectionManager.OnObjectSelected.RemoveAllListeners();
                _selectionManager.OnObjectSelected.AddListener((GameObject obj) =>
                {
                    _placementSystem?.StartPlacement(obj);
                });

                _selectionManager.OnObjectDeselected.RemoveAllListeners();
                _selectionManager.OnObjectDeselected.AddListener((GameObject obj) =>
                {
                    _placementSystem?.CancelPlacement();
                });

                _selectionManager.OnSelectionChanged.RemoveAllListeners();
                _selectionManager.OnSelectionChanged.AddListener((List<GameObject> selectedObjects) =>
                {
                    // Future: handle multi-selection UI updates
                });
            }

            // Phase 2.3: TransformController
            if (_transformController == null)
            {
                _transformController = GetComponent<TransformController>();
                if (_transformController == null)
                {
                    _transformController = gameObject.AddComponent<TransformController>();
                }
                // Initialize with dependencies
                _transformController.Initialize(_selectionManager, _gridManager);

                // Subscribe to transform events (non-breaking: integrate with existing behavior)
                _transformController.OnTransformModeChanged.RemoveAllListeners();
                _transformController.OnTransformModeChanged.AddListener((TransformModeType mode) =>
                {
                    // Sync with builder's current mode tracking
                    _currentTransformMode = mode;
                });

                _transformController.OnTransformAxisChanged.RemoveAllListeners();
                _transformController.OnTransformAxisChanged.AddListener((TransformAxis axis) =>
                {
                    // Sync with builder's current axis tracking
                    _currentTransformAxis = axis;
                });

                _transformController.OnTransformChanged.RemoveAllListeners();
                _transformController.OnTransformChanged.AddListener((GameObject obj) =>
                {                    // Update scene configuration on transform changes
                    var draggable = obj.GetComponent<TransformableItem>();
                    if (draggable != null)
                    {
                        // Update via SceneSerializer (Phase 3.1: non-breaking)
                        if (_sceneSerializer != null)
                        {
                            _sceneSerializer.UpdateObjectInScene(
                                draggable.ObjectId,
                                obj.transform.position,
                                obj.transform.eulerAngles,
                                obj.transform.localScale
                            );
                        }
                        else if (_currentScene != null)
                        {
                            var placedData = _currentScene.GetPlacedObject(draggable.ObjectId);
                            if (placedData != null)
                            {
                                placedData.position = obj.transform.position;
                                placedData.rotation = obj.transform.eulerAngles;
                                placedData.scale = obj.transform.localScale;
                                _currentScene.CreateOrUpdatePlacedObject(placedData);
                            }
                        }
                    }
                });
            }

            // Phase 3.1: SceneSerializer
            if (_sceneSerializer == null)
            {
                _sceneSerializer = GetComponent<SceneSerializer>();
                if (_sceneSerializer == null)
                {
                    _sceneSerializer = gameObject.AddComponent<SceneSerializer>();
                }
                // Initialize with dependencies
                _sceneSerializer.Initialize(_placementSystem, _selectionManager, this);

                // Subscribe to serialization events (non-breaking: integrate with existing behavior)
                _sceneSerializer.OnSceneLoaded += (scene) =>
                {
                    // Sync with builder's scene reference
                    _currentScene = scene;
                    _currentSceneName = scene?.sceneName ?? "Untitled Scene";
                };

                _sceneSerializer.OnSceneSaved += (scene) =>
                {
                    // Future: Add UI feedback for save confirmation
                };

                _sceneSerializer.OnSceneCleared += () =>
                {
                    // Sync with builder's state
                    _onSceneCleared?.Invoke();
                };
            }

            // Phase 3.2: PreviewController
            if (_previewController == null)
            {
                _previewController = GetComponent<PreviewController>();
                if (_previewController == null)
                {
                    _previewController = gameObject.AddComponent<PreviewController>();
                }
                // Initialize with dependencies
                _previewController.Initialize(_timelineDirector, _placementSystem);

                // Subscribe to preview events (non-breaking: integrate with existing behavior)
                _previewController.OnPreviewStateChanged += (isPreview) =>
                {
                    // Sync with builder's state
                    _onPreviewStateChanged?.Invoke(isPreview);
                };
            }

            // Wire SandboxInputManager events to existing handlers (non-breaking)
            if (_inputManager != null)
            {
                _inputManager.OnToggleMode += () => { Debug.Log("[InputManager] ToggleMode"); ToggleMode(); };
                _inputManager.OnExitAllModes += () => _selectionManager?.ExitAllTransformModes();
                _inputManager.OnMoveHotkey += () => OnMoveHotkeyPerformed(default);
                _inputManager.OnRotateHotkey += () => OnRotateHotkeyPerformed(default);
                _inputManager.OnScaleHotkey += () => OnScaleHotkeyPerformed(default);
                _inputManager.OnTransformIncrease += () => OnTransformModeIncreasePerformed(default);
                _inputManager.OnTransformDecrease += () => OnTransformModeDecreasePerformed(default);
                _inputManager.OnToggleAxis += () => OnTransformModeToggleAxisPerformed(default);
                _inputManager.OnCancelPlacement += () => OnCancelPlacementPerformed(default);
                _inputManager.OnSwitchPlacementItemHotkey += () => OnSwitchPlacementItemHotkey();
                _inputManager.OnStartPlacementHotkey += () => OnStartPlacementHotkey();

                // Pointer events routed to existing pointer handlers
                _inputManager.OnPointerDown += (pos) =>
                {
                    if (_placementSystem.IsActive)

                        ConfirmPlacement();
                    else
                    {
                        TransformableItem hitItem = _placementSystem.RaycastForItem(pos);
                        if (hitItem != null)
                            _selectionManager.SelectItem(hitItem);
                        else
                            _selectionManager.ClearSelection();
                    }

                };
                _inputManager.OnPointerUp += (pos) =>
                {

                };
                _inputManager.OnPointerMoved += (pos) =>
                {
                    _placementSystem.UpdatePlacement(pos);
                };
                _inputManager.OnScroll += (delta) => { /* existing scroll handling occurs in Update; foundation only */ };
            }
        }

        private void SyncGridManagerSettings()
        {
            if (_gridManager == null) return;
            _gridManager.Enabled = _snapToGrid;
            _gridManager.CellSize = _gridSize;
            _gridManager.Offset = _gridOffset;
        }

        private void InitializeState()
        {
            _placedObjects = new Dictionary<string, GameObject>();
            _previewObjects = new List<GameObject>();

            // Don't create drop indicator here - create it when first needed
            // This avoids potential issues with early initialization
        }

        #region New Placement System (Drag-Drop - Direct Object Manipulation)

        /// <summary>
        /// Start placement operation - creates real object and enters Active state
        /// </summary>
        /// <param name="objectDataId">ID of the object to place</param>
        /// <param name="screenPosition">Initial screen position for placement</param>
        public void StartPlacement(string objectDataId, Vector2 screenPosition)
        {
            if (_placementSystem == null)
            {
                Debug.LogWarning("[Placement] PlacementSystem missing; cannot start placement.");
                return;
            }

            // Cancel any existing placement
            if (_placementSystem.IsActive)
            {
                _placementSystem.CancelPlacement();
            }

            // Delegate creation to PlacementSystem; builder will handle scene config
            GameObject created = _placementSystem.StartPlacementAndCreate(objectDataId, screenPosition);
            if (created == null)
            {
                return;
            }

            // Track in scene configuration and local dictionary
            var draggable = created.GetComponent<TransformableItem>() ?? created.AddComponent<TransformableItem>();
            string placedObjectId = draggable.ObjectId;
            var objectData = _objectLibrary?.GetObjectById(objectDataId);
            var placedObjectData = new Data.PlacedObjectData(objectDataId, created.transform.position)
            {
                id = placedObjectId,
                rotation = created.transform.eulerAngles,
                scale = created.transform.localScale,
                customName = objectData?.displayName ?? created.name
            };

            if (_currentScene != null)
            {
                _currentScene.AddPlacedObject(placedObjectData);
            }
            if (!string.IsNullOrEmpty(placedObjectId))
            {
                _placedObjects[placedObjectId] = created;
            }

            // Temporarily disable transformables during placement
            foreach (var t in created.GetComponentsInChildren<TransformableItem>())
            {
                t.enabled = false;
            }
        }

        /// <summary>
        /// Confirm placement - finalize the object that's already been created
        /// </summary>
        /// <returns>The placed GameObject, or null if placement failed</returns>
        public GameObject ConfirmPlacement()
        {
            // Delegate confirmation to PlacementSystem (emits events and resets its state)
            _placementSystem?.ConfirmPlacement();
            return _placementSystem?.CurrentObject;
        }

        /// <summary>
        /// Cancel the current placement operation
        /// </summary>
        public void CancelPlacement()
        {
            _placementSystem?.CancelPlacement();
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
        #region Public Interface

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
                _onObjectRemoved?.Invoke(obj);

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
        public void SelectObject(GameObject obj, bool? overrideAutoEdit = null)
        {
            bool autoEdit = overrideAutoEdit ?? _autoEditOnSelect;
            _selectionManager.SelectObject(obj, additive: false, overrideAutoEdit: autoEdit);
        }

        /// <summary>
        /// Clear all objects from the scene
        /// </summary>
        public void ClearScene()
        {
            // Delegate to SceneSerializer (Phase 3.1: non-breaking)
            _sceneSerializer.ClearScene();
        }

        /// <summary>
        /// Create a new empty scene
        /// </summary>
        public void CreateNewScene(string sceneName = null)
        {
            ClearScene();

            _currentScene = new SceneConfiguration(sceneName ?? "New Scene");
            _currentSceneName = _currentScene.sceneName;

            _onSceneLoaded?.Invoke(_currentScene);
        }

        /// <summary>
        /// Save the current scene configuration
        /// </summary>
        public bool SaveScene(string filePath = null)
        {
            return _sceneSerializer.SaveScene(filePath);
        }

        /// <summary>
        /// Load a scene configuration from file
        /// </summary>
        public bool LoadScene(string filePath)
        {
            return _sceneSerializer.LoadScene(filePath);
        }

        #endregion

        #region Project Management (New Serialization System)

        /// <summary>
        /// Create a new sandbox project
        /// </summary>
        public void CreateNewProject(string projectName = null)
        {
            string name = projectName ?? "New Sandbox Project";
            _sceneSerializer.CreateNewProject(name);
        }

        /// <summary>
        /// Save the current project to JSON file
        /// </summary>
        public bool SaveProject(string filePath = null)
        {
            return _sceneSerializer.SaveProject(filePath);
        }

        /// <summary>
        /// Load a project from JSON file
        /// </summary>
        public bool LoadProject(string filePath)
        {
            return _sceneSerializer.LoadProject(filePath);
        }

        /// <summary>
        /// Delete a project file from disk
        /// </summary>
        public bool DeleteProject(string filePath)
        {
            return _sceneSerializer.DeleteProject(filePath);
        }

        /// <summary>
        /// Get metadata for all projects in the default directory
        /// </summary>
        public List<SandboxProjectMetadata> GetAvailableProjects()
        {
            return _sceneSerializer.GetAvailableProjects();
        }

        /// <summary>
        /// Attempt to automatically load the first available project
        /// </summary>
        private void TryAutoLoadFirstProject()
        {
            var availableProjects = GetAvailableProjects();

            if (availableProjects != null && availableProjects.Count > 0)
            {
                var firstProject = availableProjects[0];
                Debug.Log($"[SceneSandboxBuilder] Auto-loading first project: {firstProject.projectName}");

                if (LoadProject(firstProject.filePath))
                {
                    Debug.Log($"[SceneSandboxBuilder] Successfully auto-loaded project: {firstProject.projectName}");
                }
                else
                {
                    Debug.LogWarning($"[SceneSandboxBuilder] Failed to auto-load project: {firstProject.projectName}");
                }
            }
            else
            {
                Debug.Log("[SceneSandboxBuilder] No projects available to auto-load");
            }
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
                duplicate.placedObjects.Add(objCopy);
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
        public List<SceneMetadata> GetAllSceneMetadata()
        {
            var metadataList = new List<SceneMetadata>();

            if (_currentProject == null)
                return metadataList;

            string activeSceneId = _currentProject.activeSceneId;

            foreach (var scene in _currentProject.scenes)
            {
                bool isActive = scene.sceneId == activeSceneId;
                metadataList.Add(new SceneMetadata(scene, isActive));
            }

            return metadataList;
        }

        #endregion

        /// <summary>
        /// Start preview mode using the timeline director
        /// </summary>
        public void StartPreview()
        {
            // Delegate to PreviewController (Phase 3.2: non-breaking)
            if (_previewController != null)
            {
                _previewController.StartPreview();
            }
            else
            {
                // Fallback to legacy behavior
                if (_timelineDirector == null || IsInPreviewMode) return;

                StopPreview(); // Ensure clean state

                // Create preview objects (snapshots of current scene)
                CreatePreviewObjects();

                // Set up timeline director with current scene
                SetupTimelineForPreview();

                // Start playback
                _timelineDirector.Play();

                _onPreviewStateChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// Stop preview mode and return to editing
        /// </summary>
        public void StopPreview()
        {
            // Delegate to PreviewController (Phase 3.2: non-breaking)
            if (_previewController != null)
            {
                _previewController.StopPreview();
            }
            else
            {
                // Fallback to legacy behavior
                if (_timelineDirector != null)
                {
                    _timelineDirector.Stop();
                }

                // Clean up preview objects
                CleanupPreviewObjects();

                _onPreviewStateChanged?.Invoke(false);
            }
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
            return _currentDropIndicator?.transform.position ?? Vector3.zero;
        }

        #region Input Management

        /// <summary>
        /// Initialize input actions and bind events
        /// NOTE: Input handling now delegated to SandboxInputManager (Phase 3.3)
        /// This method is kept for backward compatibility but actions are no longer subscribed here
        /// </summary>
        private void InitializeInputActions()
        {
            // DEPRECATED: Input actions are now managed by SandboxInputManager
            // Keeping this method for compatibility but no longer subscribing to actions
            // The SandboxInputManager handles all input and routes events to existing handlers

            // Store action references for legacy code that might check them
            if (_toggleModeActionRef != null)
            {
                _toggleModeAction = _toggleModeActionRef.action;
            }

            if (_transformModeIncreaseActionRef != null)
            {
                _transformModeIncreaseAction = _transformModeIncreaseActionRef.action;
            }

            if (_transformModeDecreaseActionRef != null)
            {
                _transformModeDecreaseAction = _transformModeDecreaseActionRef.action;
            }

            if (_transformModeToggleAxisActionRef != null)
            {
                _transformModeToggleAxisAction = _transformModeToggleAxisActionRef.action;
            }

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

            if (_cancelPlacementActionRef != null)
            {
                _cancelPlacementAction = _cancelPlacementActionRef.action;
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

            if (_inputManager.EnableHotkeys)
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
            _selectionManager?.ExitAllTransformModes();
        }

        private void OnMoveHotkeyPerformed(InputAction.CallbackContext context)
        {
            _transformController.SetTransformMode(TransformModeType.Position);
        }

        private void OnRotateHotkeyPerformed(InputAction.CallbackContext context)
        {
            _transformController.SetTransformMode(TransformModeType.Rotation);
        }

        private void OnScaleHotkeyPerformed(InputAction.CallbackContext context)
        {
            _transformController.SetTransformMode(TransformModeType.Scale);
        }

        private void OnTransformModeIncreasePerformed(InputAction.CallbackContext context)
        {
            _transformController?.IncreaseTransformValue();
        }

        private void OnTransformModeDecreasePerformed(InputAction.CallbackContext context)
        {
            _transformController?.DecreaseTransformValue();
        }

        private void OnTransformModeToggleAxisPerformed(InputAction.CallbackContext context)
        {
            _transformController.ToggleTransformAxis();
        }

        private void OnCancelPlacementPerformed(InputAction.CallbackContext context)
        {
            if (_placementSystem.IsActive)
            {
                _placementSystem?.CancelPlacement();
            }
        }

        private void OnSwitchPlacementItemHotkey()
        {
            if (_placementSystem != null)
            {
                string nextObjectId = _placementSystem.SwitchNextPlacement();
                if (!string.IsNullOrEmpty(nextObjectId))
                {
                    Debug.Log($"[SceneSandboxBuilder] Switched to next placement item: {nextObjectId}");
                }
            }
        }

        private void OnStartPlacementHotkey()
        {
            if (_placementSystem == null)
            {
                return;
            }

            if (_pointerPositionActionRef == null)
            {
                return;
            }

            Vector2 screenPosition = _pointerPositionActionRef.action.ReadValue<Vector2>();


            _placementSystem.StartPlacement(screenPosition);
        }

        #endregion

        #region Selection Management

        /// <summary>
        /// Select an item
        /// </summary>
        private void SelectItem(TransformableItem item, bool overrideAutoEdit = false)
        {
            _selectionManager.SelectItem(item, overrideAutoEdit);

        }

        /// <summary>
        /// Deselect an item
        /// </summary>
        private void DeselectItem(TransformableItem item)
        {
            _selectionManager.DeselectItem(item);
        }

        /// <summary>
        /// Clear all selection
        /// </summary>
        private void ClearSelection()
        {
            _selectionManager.ClearSelection();
        }

        #endregion

        #region Transform Control Management


        #endregion

        #region Helper Methods

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
            _placementSystem.ClearPlacedObjects();
            _selectionManager.ClearSelection();

            // Now set the new scene (after clearing runtime state)
            _currentScene = sceneConfig;
            _currentSceneName = sceneConfig.sceneName;



            // Recreate all placed objects
            foreach (var placedObjectData in sceneConfig.placedObjects)
            {
                _placementSystem.PlaceObject(placedObjectData);
            }

            _onSceneLoaded?.Invoke(_currentScene);
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

        /// <summary>
        /// Get the current global grid info for passing to TransformableItem
        /// </summary>
        private GridInfo GetGlobalGridInfo()
        {
            if (_gridManager != null)
            {
                _gridManager.Enabled = _snapToGrid;
                _gridManager.CellSize = _gridSize;
                _gridManager.Offset = _gridOffset;
            }
            SyncGridManagerSettings();
            return new GridInfo(_snapToGrid, _gridSize, _gridOffset);
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
            if (_cameraRaycaster != null)
            {
                var origMask = _cameraRaycaster.RaycastMask;
                var origMax = _cameraRaycaster.MaxDistance;
                _cameraRaycaster.RaycastMask = _placementLayers;
                _cameraRaycaster.MaxDistance = 20f;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitCamDown, _cameraRaycaster.MaxDistance, _cameraRaycaster.RaycastMask))
                {
                    float finalY = Mathf.Max(hitCamDown.point.y, minimumHeight);
                    Vector3 resultCam = new Vector3(position.x, finalY, position.z);
                    _cameraRaycaster.RaycastMask = origMask;
                    _cameraRaycaster.MaxDistance = origMax;
                    return resultCam;
                }
                _cameraRaycaster.RaycastMask = origMask;
                _cameraRaycaster.MaxDistance = origMax;
            }
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
            if (_cameraRaycaster != null)
            {
                var origMask = _cameraRaycaster.RaycastMask;
                var origMax = _cameraRaycaster.MaxDistance;
                _cameraRaycaster.RaycastMask = ~0;
                _cameraRaycaster.MaxDistance = Mathf.Infinity;
                if (_cameraRaycaster.TryRaycast(screenPosition, out RaycastHit hitCam))
                {
                    return hitCam.collider.gameObject;
                }
                _cameraRaycaster.RaycastMask = origMask;
                _cameraRaycaster.MaxDistance = origMax;
            }
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
            if (_cameraRaycaster != null)
            {
                var origMask = _cameraRaycaster.RaycastMask;
                var origMax = _cameraRaycaster.MaxDistance;
                _cameraRaycaster.RaycastMask = _placementLayers;
                _cameraRaycaster.MaxDistance = Mathf.Infinity;
                if (_cameraRaycaster.TryRaycast(screenPosition, out RaycastHit hitCam))
                {
                    float finalY = Mathf.Max(hitCam.point.y, minimumHeight);
                    worldPos = new Vector3(hitCam.point.x, finalY, hitCam.point.z);
                }
                else
                {
                    // fall through to physics fallback
                }
                _cameraRaycaster.RaycastMask = origMask;
                _cameraRaycaster.MaxDistance = origMax;
            }
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
            // Create temporary TransformableItem for snap logic
            GameObject tempObject = new GameObject("TempSnapHelper");
            var tempDraggable = tempObject.AddComponent<TransformableItem>();
            GridInfo gridInfo = GetGlobalGridInfo();
            Vector3 finalPosition = tempDraggable.GetSnappedPositionValue(position, gridInfo);
            Destroy(tempObject);

            // Note: GetWorldPositionFromScreen already handles surface detection, 
            // so we don't need to call GetSurfacePosition again here

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

            // Draw drop indicator gizmos when dropping
            if (_enableDropIndicator && _placementSystem?.IsActive == true)
            {
                DrawDropIndicatorGizmos();
            }

            // Draw placement preview gizmos
            if (IsPlacementActive && _placementSystem?.CurrentObject != null)
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

            // Create a set of occupied grid cells
            HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
            if (_placedObjects != null && _placedObjects.Count > 0)
            {
                foreach (var kvp in _placedObjects)
                {
                    if (kvp.Value != null)
                    {
                        Vector3 objPos = kvp.Value.transform.position;
                        // Calculate grid cell coordinates
                        int cellX = Mathf.RoundToInt((objPos.x - (center.x - gridExtentX)) / _gridSize);
                        int cellZ = Mathf.RoundToInt((objPos.z - (center.z - gridExtentZ)) / _gridSize);
                        occupiedCells.Add(new Vector2Int(cellX, cellZ));
                    }
                }
            }

            // Draw occupied cells with wire cubes (more visible)
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // Orange color for occupied cells
            foreach (var cell in occupiedCells)
            {
                float cellCenterX = center.x - gridExtentX + (cell.x * _gridSize) + (_gridSize * 0.5f);
                float cellCenterZ = center.z - gridExtentZ + (cell.y * _gridSize) + (_gridSize * 0.5f);
                Vector3 cellCenter = new Vector3(cellCenterX, center.y + 0.05f, cellCenterZ);
                Vector3 cellSize = new Vector3(_gridSize * 0.95f, 0.1f, _gridSize * 0.95f);

                // Draw thicker wire cube
                Gizmos.DrawWireCube(cellCenter, cellSize);

                // Draw cross pattern inside cell
                float halfSize = _gridSize * 0.4f;
                Gizmos.DrawLine(
                    new Vector3(cellCenterX - halfSize, center.y, cellCenterZ - halfSize),
                    new Vector3(cellCenterX + halfSize, center.y, cellCenterZ + halfSize)
                );
                Gizmos.DrawLine(
                    new Vector3(cellCenterX - halfSize, center.y, cellCenterZ + halfSize),
                    new Vector3(cellCenterX + halfSize, center.y, cellCenterZ - halfSize)
                );
            }

            // Use different color based on snap mode
            Color gridLineColor = _snapToGrid ? new Color(0f, 1f, 0f, 0.5f) : Color.gray;
            Gizmos.color = gridLineColor;

            // Draw vertical lines (along Z-axis)
            for (int i = 0; i <= gridLinesX; i++)
            {
                float x = center.x - gridExtentX + (i * _gridSize);
                Vector3 start = new Vector3(x, center.y, center.z - gridExtentZ);
                Vector3 end = new Vector3(x, center.y, center.z + gridExtentZ);

                // Center line in different color
                if (Mathf.Approximately(x, center.x))
                {
                    Gizmos.color = _snapToGrid ? Color.green : Color.white;
                    Gizmos.DrawLine(start, end);
                    Gizmos.color = gridLineColor;
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
                    Gizmos.color = _snapToGrid ? Color.green : Color.white;
                    Gizmos.DrawLine(start, end);
                    Gizmos.color = gridLineColor;
                }
                else
                {
                    Gizmos.DrawLine(start, end);
                }
            }

            // Draw origin marker
            Gizmos.color = _snapToGrid ? Color.green : Color.white;
            Gizmos.DrawWireSphere(center, _snapToGrid ? _gridSize * 0.15f : 0.1f);

            // Draw grid info label when snap is enabled
#if UNITY_EDITOR
            if (_snapToGrid)
            {
                Vector3 labelPos = center + Vector3.up * 0.5f;
                UnityEditor.Handles.Label(
                    labelPos,
                    $"Snap Grid: {_gridSize}m\nOffset: {_gridOffset}\nPivot: {_gridPivotOffset}",
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.green },
                        fontSize = 10,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    }
                );
            }
#endif
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
            if (_placementSystem?.IsActive != true) return;

            Vector3 position = _currentDropIndicator?.transform.position ?? Vector3.zero;
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
                // Create temporary TransformableItem for snap logic
                GameObject tempObject = new GameObject("TempSnapHelper");
                var tempDraggable = tempObject.AddComponent<TransformableItem>();
                GridInfo gridInfo = GetGlobalGridInfo();
                Vector3 snappedPos = tempDraggable.GetSnappedPositionValue(position, gridInfo);
                DestroyImmediate(tempObject);

                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(snappedPos + Vector3.up * 0.05f, Vector3.one * _gridSize * 0.1f);
            }
        }

        /// <summary>
        /// Draw placement preview gizmos (ghost object indicators)
        /// </summary>
        private void DrawPlacementPreviewGizmos()
        {
            GameObject currentPlacementObject = _placementSystem?.CurrentObject;
            if (currentPlacementObject == null) return;
            Vector3 position = currentPlacementObject.transform.position;
            Bounds bounds = GetObjectBounds(currentPlacementObject);

            // Draw validity indicator with color
            bool isValid = _placementSystem?.IsValidPlacementCurrentPosition ?? true;
            Gizmos.color = isValid ? Color.green : Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Draw grid snap indicator if enabled
            if (_showGridSnapIndicator && _snapToGrid)
            {
                // Get TransformableItem if available for snap logic
                var transformableItem = currentPlacementObject.GetComponent<TransformableItem>();
                GridInfo gridInfo = GetGlobalGridInfo();
                Vector3 snappedPos = transformableItem != null
                    ? transformableItem.GetSnappedPositionValue(position, gridInfo)
                    : position;

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
                Gizmos.color = isValid ? Color.green : Color.red;
                Vector3 labelPos = bounds.center + Vector3.up * (bounds.extents.y + 0.5f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(labelPos, $"Cost: {_placementCost}");
#endif
            }

            // Draw validation feedback
            if (!isValid)
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

            // Cleanup placement object via PlacementSystem
            GameObject currentPlacementObject = _placementSystem?.CurrentObject;
            if (currentPlacementObject != null)
            {
                DestroyImmediate(currentPlacementObject);
            }
        }
    }
}
