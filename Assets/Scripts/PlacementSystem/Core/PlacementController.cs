using UnityEngine;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Tools;

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
            Move,
            Rotate,
            Delete
        }

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

        [SerializeField, Tooltip("Reference to the placement strategy component")]
        private MonoBehaviour _placementStrategyComponent;

        [SerializeField, Tooltip("Reference to the placement validator component")]
        private MonoBehaviour _placementValidatorComponent;

        [SerializeField, Tooltip("Reference to the placement visualizer component")]
        private MonoBehaviour _placementVisualizerComponent;

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
        private IPlacementStrategy _placementStrategy;
        private IPlacementValidator _placementValidator;
        private IPlacementVisualizer _placementVisualizer;

        private PlacementToolContext _toolContext;
        private PlacementSelectionState _selectionState;

        private PlacementTool _placementTool;
        private SelectionTool _selectionTool;
        private MoveTool _moveTool;
        private RotateTool _rotateTool;
        private DeleteTool _deleteTool;
        private IPlacementTool _activeTool;

        private GameObject _currentGhost;
        private bool _isPlacementActive;

        private void Awake()
        {
            _inputProvider = _inputProviderComponent as IInputProvider;
            _placementStrategy = _placementStrategyComponent as IPlacementStrategy;
            _placementValidator = _placementValidatorComponent as IPlacementValidator;
            _placementVisualizer = _placementVisualizerComponent as IPlacementVisualizer;

            if (_inputProvider == null)
                Debug.LogError("PlacementController: Input provider must implement IInputProvider");
            if (_placementStrategy == null)
                Debug.LogError("PlacementController: Placement strategy must implement IPlacementStrategy");
            if (_placementValidator == null)
                Debug.LogError("PlacementController: Placement validator must implement IPlacementValidator");
            if (_placementVisualizer == null)
                Debug.LogError("PlacementController: Placement visualizer must implement IPlacementVisualizer");

            if (_placementCamera == null)
                _placementCamera = Camera.main;

            _selectionState = new PlacementSelectionState();
            RebuildToolContext();

            _placementTool = new PlacementTool();
            _selectionTool = new SelectionTool();
            _moveTool = new MoveTool();
            _rotateTool = new RotateTool();
            _deleteTool = new DeleteTool();

            _placementTool.OnPlacementConfirmed = HandlePlacementConfirmed;
            _placementTool.OnPlacementCancelled = HandlePlacementCancelled;

            SetActiveTool(PlacementToolType.Selection);
        }

        private void Update()
        {
            if (_activeTool == null)
                return;

            _activeTool.HandleInput();
            _activeTool.Tick();
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

            _placementTool.SetupPlacement(_currentGhost, _objectToPlace, _makeObjectsSelectable);
            _isPlacementActive = true;
        }

        /// <summary>
        /// Stops the placement process and cleans up.
        /// </summary>
        public void CancelPlacement()
        {
            if (_currentGhost == null)
                return;

            _placementTool.CancelPlacement();
            _currentGhost = null;
            _isPlacementActive = false;
        }

        /// <summary>
        /// Confirms placement and instantiates the real object.
        /// </summary>
        public void ConfirmPlacement()
        {
            _placementTool.ConfirmPlacement();
        }

        private void HandlePlacementConfirmed(GameObject placedObject)
        {
            _currentGhost = null;
            _isPlacementActive = false;

            StartPlacement();
        }

        private void HandlePlacementCancelled()
        {
            _currentGhost = null;
            _isPlacementActive = false;
        }

        public void SetActiveTool(PlacementToolType toolType)
        {
            IPlacementTool nextTool = GetTool(toolType);
            if (nextTool == _activeTool)
                return;

            _activeTool?.OnExit();
            _activeTool = nextTool;
            _activeTool?.OnEnter(_toolContext);
        }

        private IPlacementTool GetTool(PlacementToolType toolType)
        {
            switch (toolType)
            {
                case PlacementToolType.Placement:
                    return _placementTool;
                case PlacementToolType.Selection:
                    return _selectionTool;
                case PlacementToolType.Move:
                    return _moveTool;
                case PlacementToolType.Rotate:
                    return _rotateTool;
                case PlacementToolType.Delete:
                    return _deleteTool;
                default:
                    return _selectionTool;
            }
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
                _selectionState
            );
        }

        /// <summary>
        /// Changes the placement strategy at runtime.
        /// </summary>
        public void SetPlacementStrategy(IPlacementStrategy newStrategy)
        {
            _placementStrategy = newStrategy;
            RebuildToolContext();
            _activeTool?.OnEnter(_toolContext);
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
            if (_selectionTool == null)
                return;

            _selectionTool.OnEnter(_toolContext);
            _selectionTool.HandleSelection(selected);
        }

        /// <summary>
        /// Clears selection state.
        /// </summary>
        public void DeselectAll()
        {
            if (_selectionTool == null)
                return;

            _selectionTool.OnEnter(_toolContext);
            _selectionTool.HandleSelection(null);
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
        }
    }
}
