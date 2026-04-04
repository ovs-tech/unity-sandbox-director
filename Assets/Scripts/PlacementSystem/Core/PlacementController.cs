using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Tools;
using Systems.PlacementSystem.Persistence;
using Systems.Persistence;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Main orchestrator for the placement system.
    /// Coordinates input, strategy, validation, visualization, snapping, and tool lifecycle.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        public enum PlacementToolType
        {
            Placement,
            Selection,
            Snap,
            Move,
            Rotate,
            Delete
        }

        // Backward-compatible aliases for older tests and callers.
        public static class ToolIds
        {
            public const PlacementToolType Placement = PlacementToolType.Placement;
            public const PlacementToolType Selection = PlacementToolType.Selection;
            public const PlacementToolType Snap = PlacementToolType.Snap;
            public const PlacementToolType Move = PlacementToolType.Move;
            public const PlacementToolType Rotate = PlacementToolType.Rotate;
            public const PlacementToolType Delete = PlacementToolType.Delete;
        }

        [Header("Events")]
        public UnityEngine.Events.UnityEvent<GameObject> OnPlacementStartedEvent;
        public UnityEngine.Events.UnityEvent<GameObject> OnPlacementSuccessEvent;
        public UnityEngine.Events.UnityEvent<string> OnPlacementFailedEvent;
        public UnityEngine.Events.UnityEvent OnPlacementCancelledEvent;
        public UnityEngine.Events.UnityEvent<PlacementToolType> OnToolChangedEvent;

        [Header("Dependencies")]
        [SerializeField, Tooltip("The camera used for raycasting")]
        private Camera _placementCamera;

        [SerializeField, Tooltip("Layer mask for placement surfaces")]
        private LayerMask _placementSurface = -1;

        [SerializeField, Tooltip("The prefab to place")]
        private GameObject _objectToPlace;

        [Header("System Components")]
        [SerializeField, Tooltip("Reference to the input provider component")]
        private BaseInputProvider _inputProviderComponent;

        [SerializeField, Tooltip("Reference to the placement strategy asset")]
        private BasePlacementStrategy _placementStrategy;

        [SerializeField, Tooltip("Reference to the placement visualizer asset")]
        private BasePlacementVisualizer _placementVisualizer;

        [SerializeField, Tooltip("Optional ScriptableObject validator asset")]
        private BasePlacementValidator _placementValidatorAsset;

        [SerializeField, Tooltip("When false, default validation fails if PlacementPart is missing")]
        private bool _allowPlacementWithoutPart = true;

        [SerializeField, Tooltip("Reference to the snap manager (optional)")]
        private SnapManager _snapManager;

        [SerializeField, Tooltip("PlacementToolset asset — defines tools, dependencies, and handles SetActiveTool() calls")]
        private PlacementToolset _toolset;

        [Header("Placement Settings")]
        [SerializeField, Tooltip("Maximum raycast distance")]
        private float _maxRaycastDistance = 100f;

        [SerializeField, Tooltip("Rotation snap increment for tools")]
        private float _rotationSnapDegrees = 90f;

        [SerializeField, Tooltip("Rotation increment per rotation input")]
        private float _rotationIncrement = 90f;

        [SerializeField, Tooltip("Automatically add Selectable component to placed objects")]
        private bool _makeObjectsSelectable = true;

        [Header("Selection Settings")]
        [SerializeField, Tooltip("Layer mask for selectable objects")]
        private LayerMask _selectionLayer = -1;

        [SerializeField, Tooltip("Layer mask for selection movement surfaces")]
        private LayerMask _selectionMovementSurface = -1;

        [SerializeField, Tooltip("Allow multi-selection of objects")]
        private bool _allowMultiSelection = false;

        [SerializeField, Tooltip("Require Ctrl/Cmd for multi-selection")]
        private bool _requireModifierForMultiSelect = true;

        [SerializeField, Tooltip("Move selected object to pointer each frame")]
        private bool _moveSelectedWithPointer = true;

        private IInputProvider _inputProvider;
        private IPlacementValidator _placementValidator;

        [SerializeField, Tooltip("Optional ToolManager component. If missing, one is added at runtime.")]
        private ToolManager _toolManager;

        private PlacementToolContext _toolContext;
        private PlacementSelectionState _selectionState;

        public ToolManager ToolManager => _toolManager;

        private GameObject _currentGhost;
        private bool _isPlacementActive;

        // Tracked placed objects for persistence
        private readonly Dictionary<string, GameObject> _placedObjects = new Dictionary<string, GameObject>();

        private void Awake()
        {
            _inputProvider = _inputProviderComponent;
            _placementValidator = ResolvePlacementValidator();

            if (_toolset != null)
                _toolset.OnSetActiveToolRequested += HandleToolsetActiveToolRequested;

            if (_inputProvider == null)
            {
                // Missing input provider; caller should assign a valid implementation
            }
            if (_placementStrategy == null)
            {
                // Missing placement strategy; caller should assign a valid implementation
            }
            if (_placementVisualizer == null)
            {
                // Missing placement visualizer; caller should assign a valid implementation
            }

            if (_placementCamera == null)
                _placementCamera = Camera.main;

            InitializeTooling();

            // Register a persistence adapter if SaveLoadSystem is available
            try
            {
                if (GamePersistenceManager.HasInstance)
                {
                    var sls = GamePersistenceManager.Instance;
                    var adapter = new PlacementPersistenceAdapter(this, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    sls.RegisterSubsystem(adapter);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PlacementController: unable to register persistence adapter: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (_toolset != null)
                _toolset.OnSetActiveToolRequested -= HandleToolsetActiveToolRequested;
        }

        private void Update()
        {
            if (_toolManager == null)
                return;

            _toolManager.HandleInput();
            _toolManager.Tick();
        }

        public void InitializeForTesting()
        {
            _inputProvider = _inputProviderComponent;
            _placementValidator = ResolvePlacementValidator();
            InitializeTooling();
        }

        private void InitializeTooling()
        {
            _selectionState = new PlacementSelectionState();

            if (_toolManager == null)
                _toolManager = GetComponent<ToolManager>();

            if (_toolManager == null)
                _toolManager = gameObject.AddComponent<ToolManager>();

            if (!RegisterToolsFromToolset())
            {
                Debug.LogError("PlacementController: no tools registered from PlacementToolset. Configure Toolset entries before runtime.");
            }

            var placementTool = _toolManager.GetTool(PlacementToolType.Placement) as PlacementTool;
            if (placementTool != null)
            {
                placementTool.OnPlacementConfirmed = HandlePlacementConfirmed;
                placementTool.OnPlacementCancelled = HandlePlacementCancelled;
                placementTool.OnPlacementFailed = HandlePlacementFailed;
            }

            RebuildToolContext();
            SetActiveTool(PlacementToolType.Selection);
        }

        private bool RegisterToolsFromToolset()
        {
            if (_toolManager != null && _toolManager.RegisterToolsFromToolset())
                return true;

            if (_toolset == null || _toolset.Tools == null || _toolset.Tools.Count == 0)
                return false;

            bool anyRegistered = false;
            for (int i = 0; i < _toolset.Tools.Count; i++)
            {
                var entry = _toolset.Tools[i];
                if (entry == null)
                    continue;

                if (_toolset.TryGetTool(entry.ToolType, out var tool))
                {
                    _toolManager.RegisterTool(entry.ToolType, tool);
                    anyRegistered = true;
                }
            }

            return anyRegistered;
        }

        /// <summary>
        /// Starts the placement process with a ghost object.
        /// </summary>
        public void StartPlacement()
        {
            if (_objectToPlace == null || _currentGhost != null)
                return;

            SetActiveTool(PlacementToolType.Placement);

            _currentGhost = Instantiate(_objectToPlace);
            _currentGhost.name = $"{_objectToPlace.name}_Ghost";

            var placementTool = ToolManager.GetTool(PlacementToolType.Placement) as PlacementTool;
            placementTool?.SetupPlacement(_currentGhost, _objectToPlace, _makeObjectsSelectable);
            _isPlacementActive = true;
            OnPlacementStartedEvent?.Invoke(_currentGhost);
        }

        /// <summary>
        /// Stops the placement process and cleans up.
        /// </summary>
        public void CancelPlacement()
        {
            if (_currentGhost == null)
                return;

            var placementTool = ToolManager.GetTool(PlacementToolType.Placement) as PlacementTool;
            placementTool?.CancelPlacement();
            _currentGhost = null;
            _isPlacementActive = false;
        }

        /// <summary>
        /// Confirms placement and instantiates the real object.
        /// </summary>
        public void ConfirmPlacement()
        {
            var placementTool = ToolManager.GetTool(PlacementToolType.Placement) as PlacementTool;
            placementTool?.ConfirmPlacement();
        }

        private void HandlePlacementConfirmed(GameObject placedObject)
        {
            _currentGhost = null;
            _isPlacementActive = false;
            // Register the placed object for persistence and notify listeners
            RegisterPlacedObject(placedObject);
            OnPlacementSuccessEvent?.Invoke(placedObject);

            StartPlacement();
        }

        private void HandlePlacementCancelled()
        {
            _currentGhost = null;
            _isPlacementActive = false;
            OnPlacementCancelledEvent?.Invoke();
        }

        private void HandlePlacementFailed(string reason)
        {
            OnPlacementFailedEvent?.Invoke(reason);
        }

        public void SetActiveTool(PlacementToolType toolType)
        {
            if (_toolManager != null && _toolManager.TryBuildPipeline(toolType, out var pipeline) && pipeline.Length > 0)
            {
                SetToolStack(pipeline);
                OnToolChangedEvent?.Invoke(toolType);
                return;
            }

            Debug.LogWarning($"PlacementController: unable to resolve pipeline for '{toolType}'. Check PlacementToolset tool dependencies.");
        }

        public void SetActiveTool(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                Debug.LogWarning("PlacementController: tool name is null or empty.");
                return;
            }

            if (!Enum.TryParse(toolName, true, out PlacementToolType parsedTool))
            {
                Debug.LogWarning($"PlacementController: unknown tool '{toolName}'.");
                return;
            }

            SetActiveTool(parsedTool);
        }

        private void HandleToolsetActiveToolRequested(PlacementToolType toolType)
        {
            SetActiveTool(toolType);
        }

        public void SetToolStack(params PlacementToolType[] toolTypes)
        {
            _toolManager?.SetPipeline(toolTypes);
        }

        private void RebuildToolContext()
        {
            _toolContext = new PlacementToolContext(
                _inputProvider,
                _placementStrategy,
                _placementValidator,
                _placementVisualizer,
                _snapManager,
                _placementCamera,
                _maxRaycastDistance,
                _rotationIncrement,
                _rotationSnapDegrees,
                _allowMultiSelection,
                _requireModifierForMultiSelect,
                _moveSelectedWithPointer,
                _placementSurface,
                _selectionLayer,
                _selectionMovementSurface,
                _selectionState,
                _toolManager.GetTool(PlacementToolType.Selection) as SelectionTool,
                _toolManager.ToolStates
            );

            _toolManager.InitializeContext(_toolContext);
        }

        private IPlacementValidator ResolvePlacementValidator()
        {
            if (_placementValidatorAsset != null)
                return _placementValidatorAsset;

            Debug.LogWarning("PlacementController: no validator asset assigned. Falling back to DefaultPlacementValidator.");

            return new Validation.DefaultPlacementValidator(_allowPlacementWithoutPart, searchInChildren: true);
        }

        /// <summary>
        /// Changes the placement strategy at runtime.
        /// </summary>
        public void SetPlacementStrategy(BasePlacementStrategy newStrategy)
        {
            _placementStrategy = newStrategy;
            RebuildToolContext();
        }

        public void SetPlacementValidator(IPlacementValidator validator)
        {
            _placementValidator = validator ?? new Validation.DefaultPlacementValidator(_allowPlacementWithoutPart, searchInChildren: true);
            RebuildToolContext();
        }

        /// <summary>
        /// Exposes the input provider for modules like SelectionManager.
        /// </summary>
        public IInputProvider InputProviderComponent => _inputProvider;

        /// <summary>
        /// Exposes the placement camera for modules like SelectionManager.
        /// </summary>
        public Camera PlacementCamera => _placementCamera;

        /// <summary>
        /// Handles selection click input routed from external systems.
        /// </summary>
        public void HandleSelectionClick(GameObject selected)
        {
            var selectionTool = _toolManager.GetTool(PlacementToolType.Selection) as SelectionTool;
            if (selectionTool == null)
                return;

            selectionTool.OnEnter(_toolContext);
            selectionTool.HandleSelection(selected);
        }

        /// <summary>
        /// Clears selection state.
        /// </summary>
        public void DeselectAll()
        {
            var selectionTool = _toolManager.GetTool(PlacementToolType.Selection) as SelectionTool;
            if (selectionTool == null)
                return;

            selectionTool.OnEnter(_toolContext);
            selectionTool.HandleSelection(null);
        }

        /// <summary>
        /// Sets the object to be placed.
        /// </summary>
        public void SetObjectToPlace(GameObject prefab)
        {
            _objectToPlace = prefab;
        }

        private void OnDrawGizmos()
        {
            if (_isPlacementActive && _currentGhost != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_currentGhost.transform.position, 0.3f);
            }

            _placementStrategy?.OnDrawGizmos();
        }

        // -----------------------------------------------------------------
        // Persistence / placed object helpers
        // -----------------------------------------------------------------

        public IEnumerable<GameObject> GetPlacedObjects()
        {
            return new List<GameObject>(_placedObjects.Values);
        }

        public void RegisterPlacedObject(GameObject instance, string id = null, string prefabName = null)
        {
            if (instance == null) return;

            var info = instance.GetComponent<PlacedObjectInfo>();
            if (info == null) info = instance.AddComponent<PlacedObjectInfo>();

            if (!string.IsNullOrEmpty(id)) info.Id = id;
            if (!string.IsNullOrEmpty(prefabName)) info.PrefabName = prefabName;
            if (string.IsNullOrEmpty(info.Id)) info.Id = Guid.NewGuid().ToString();

            _placedObjects[info.Id] = instance;
        }

        public GameObject PlaceObjectFromRecord(PlacedObjectRecord record)
        {
            if (record == null) return null;

            GameObject prefab = null;
            if (!string.IsNullOrEmpty(record.prefabName))
            {
                // Try Resources lookup first
                prefab = Resources.Load<GameObject>(record.prefabName);
            }

            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, record.position, Quaternion.Euler(record.rotation));
                instance.name = string.IsNullOrEmpty(record.customName) ? prefab.name : record.customName;
            }
            else
            {
                instance = new GameObject(string.IsNullOrEmpty(record.customName) ? (record.prefabName ?? "PlacedObject") : record.customName);
                instance.transform.position = record.position;
                instance.transform.eulerAngles = record.rotation;
                instance.transform.localScale = record.scale;
            }

            instance.transform.position = record.position;
            instance.transform.eulerAngles = record.rotation;
            instance.transform.localScale = record.scale;

            RegisterPlacedObject(instance, record.id, record.prefabName);
            return instance;
        }

        public void ClearPlacedObjects(bool destroyGameObjects = true)
        {
            foreach (var kv in new List<GameObject>(_placedObjects.Values))
            {
                if (kv == null) continue;
                if (destroyGameObjects)
                {
                    if (Application.isPlaying) Destroy(kv);
                    else DestroyImmediate(kv);
                }
            }
            _placedObjects.Clear();
        }
    }
}
