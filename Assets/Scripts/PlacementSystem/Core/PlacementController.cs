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
        private MonoBehaviour _inputProviderComponent;

        [SerializeField, Tooltip("Reference to the placement strategy asset")]
        private BasePlacementStrategy _placementStrategy;

        [SerializeField, Tooltip("Reference to the placement visualizer asset")]
        private BasePlacementVisualizer _placementVisualizer;

        [SerializeField, Tooltip("Reference to the snap manager (optional)")]
        private SnapManager _snapManager;

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

        private PlacementToolContext _toolContext;
        private PlacementSelectionState _selectionState;

        public ToolManager ToolManager { get; private set; }

        private GameObject _currentGhost;
        private bool _isPlacementActive;

        // Tracked placed objects for persistence
        private readonly Dictionary<string, GameObject> _placedObjects = new Dictionary<string, GameObject>();

        private void Awake()
        {
            _inputProvider = _inputProviderComponent as IInputProvider;
            var placementValidator = new Validation.DefaultPlacementValidator();

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

            _selectionState = new PlacementSelectionState();
            ToolManager = new ToolManager();

            var placementTool = new PlacementTool();
            var selectionTool = new SelectionTool();
            var snapTool = new SnapTool();
            var moveTool = new MoveTool();
            var rotateTool = new RotateTool();
            var deleteTool = new DeleteTool();

            ToolManager.RegisterTool(PlacementToolType.Placement, placementTool);
            ToolManager.RegisterTool(PlacementToolType.Selection, selectionTool);
            ToolManager.RegisterTool(PlacementToolType.Snap, snapTool);
            ToolManager.RegisterTool(PlacementToolType.Move, moveTool);
            ToolManager.RegisterTool(PlacementToolType.Rotate, rotateTool);
            ToolManager.RegisterTool(PlacementToolType.Delete, deleteTool);

            RebuildToolContext();

            placementTool.OnPlacementConfirmed = HandlePlacementConfirmed;
            placementTool.OnPlacementCancelled = HandlePlacementCancelled;
            placementTool.OnPlacementFailed = HandlePlacementFailed;

            SetActiveTool(PlacementToolType.Selection);

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

        private void Update()
        {
            ToolManager.HandleInput();
            ToolManager.Tick();
        }

        public void InitializeForTesting()
        {
            _selectionState = new PlacementSelectionState();
            ToolManager = new ToolManager();

            var placementTool = new PlacementTool();
            var selectionTool = new SelectionTool();
            var snapTool = new SnapTool();
            var moveTool = new MoveTool();
            var rotateTool = new RotateTool();
            var deleteTool = new DeleteTool();

            ToolManager.RegisterTool(PlacementToolType.Placement, placementTool);
            ToolManager.RegisterTool(PlacementToolType.Selection, selectionTool);
            ToolManager.RegisterTool(PlacementToolType.Snap, snapTool);
            ToolManager.RegisterTool(PlacementToolType.Move, moveTool);
            ToolManager.RegisterTool(PlacementToolType.Rotate, rotateTool);
            ToolManager.RegisterTool(PlacementToolType.Delete, deleteTool);

            placementTool.OnPlacementConfirmed = HandlePlacementConfirmed;
            placementTool.OnPlacementCancelled = HandlePlacementCancelled;
            placementTool.OnPlacementFailed = HandlePlacementFailed;

            SetActiveTool(PlacementToolType.Selection);
            
            RebuildToolContext();
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
            switch (toolType)
            {
                case PlacementToolType.Placement:
                    SetToolStack(PlacementToolType.Snap, PlacementToolType.Placement);
                    break;
                case PlacementToolType.Selection:
                    SetToolStack(PlacementToolType.Selection);
                    break;
                case PlacementToolType.Snap:
                    SetToolStack(PlacementToolType.Snap);
                    break;
                case PlacementToolType.Move:
                    SetToolStack(PlacementToolType.Selection, PlacementToolType.Snap, PlacementToolType.Move);
                    break;
                case PlacementToolType.Rotate:
                    SetToolStack(PlacementToolType.Selection, PlacementToolType.Rotate);
                    break;
                case PlacementToolType.Delete:
                    SetToolStack(PlacementToolType.Selection, PlacementToolType.Delete);
                    break;
                default:
                    SetToolStack(PlacementToolType.Selection);
                    break;
            }
            OnToolChangedEvent?.Invoke(toolType);
        }

        public void SetToolStack(params PlacementToolType[] toolTypes)
        {
            ToolManager.SetPipeline(toolTypes);
        }

        private void RebuildToolContext()
        {
            _toolContext = new PlacementToolContext(
                _inputProvider,
                _placementStrategy,
                new Validation.DefaultPlacementValidator(),
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
                ToolManager.GetTool(PlacementToolType.Selection) as SelectionTool,
                ToolManager.ToolStates
            );

            ToolManager.InitializeContext(_toolContext);
        }

        /// <summary>
        /// Changes the placement strategy at runtime.
        /// </summary>
        public void SetPlacementStrategy(BasePlacementStrategy newStrategy)
        {
            _placementStrategy = newStrategy;
            RebuildToolContext();
        }

        /// <summary>
        /// Exposes the input provider component for modules like SelectionManager.
        /// </summary>
        public MonoBehaviour InputProviderComponent => _inputProviderComponent;

        /// <summary>
        /// Exposes the placement camera for modules like SelectionManager.
        /// </summary>
        public Camera PlacementCamera => _placementCamera;

        /// <summary>
        /// Handles selection click input routed from external systems.
        /// </summary>
        public void HandleSelectionClick(GameObject selected)
        {
            var selectionTool = ToolManager.GetTool(PlacementToolType.Selection) as SelectionTool;
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
            var selectionTool = ToolManager.GetTool(PlacementToolType.Selection) as SelectionTool;
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
