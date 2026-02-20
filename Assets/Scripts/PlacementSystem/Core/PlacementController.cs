using System.Collections.Generic;
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
            Snap,
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
        private SnapTool _snapTool;
        private MoveTool _moveTool;
        private RotateTool _rotateTool;
        private DeleteTool _deleteTool;
        private readonly List<IPlacementTool> _activeTools = new List<IPlacementTool>();
        private ToolStateRegistry _toolStates;

        private GameObject _currentGhost;
        private bool _isPlacementActive;

        private void Awake()
        {
            _inputProvider = _inputProviderComponent as IInputProvider;
            _placementStrategy = _placementStrategyComponent as IPlacementStrategy;
            _placementValidator = _placementValidatorComponent as IPlacementValidator;
            _placementVisualizer = _placementVisualizerComponent as IPlacementVisualizer;

            if (_inputProvider == null)
            {
                // Missing input provider; caller should assign a valid implementation
            }
            if (_placementStrategy == null)
            {
                // Missing placement strategy; caller should assign a valid implementation
            }
            if (_placementValidator == null)
            {
                // Missing placement validator; caller should assign a valid implementation
            }
            if (_placementVisualizer == null)
            {
                // Missing placement visualizer; caller should assign a valid implementation
            }

            if (_placementCamera == null)
                _placementCamera = Camera.main;

            _selectionState = new PlacementSelectionState();
            _toolStates = new ToolStateRegistry();
            _placementTool = new PlacementTool();
            _selectionTool = new SelectionTool();
            _snapTool = new SnapTool();
            _moveTool = new MoveTool();
            _rotateTool = new RotateTool();
            _deleteTool = new DeleteTool();

            RebuildToolContext();

            _placementTool.OnPlacementConfirmed = HandlePlacementConfirmed;
            _placementTool.OnPlacementCancelled = HandlePlacementCancelled;

            SetActiveTool(PlacementToolType.Selection);
        }

        private void Update()
        {
            if (_activeTools.Count == 0)
                return;

            for (int i = 0; i < _activeTools.Count; i++)
            {
                _activeTools[i].HandleInput();
            }

            for (int i = 0; i < _activeTools.Count; i++)
            {
                _activeTools[i].Tick();
            }
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
        }

        /// <summary>
        /// Sets the tool pipeline order for the current update loop.
        /// </summary>
        public void SetToolStack(params PlacementToolType[] toolTypes)
        {
            if (toolTypes == null || toolTypes.Length == 0)
                return;

            var nextTools = new List<IPlacementTool>(toolTypes.Length);
            var seen = new HashSet<IPlacementTool>();

            for (int i = 0; i < toolTypes.Length; i++)
            {
                var tool = GetTool(toolTypes[i]);
                if (tool != null && seen.Add(tool))
                    nextTools.Add(tool);
            }

            ApplyToolStack(nextTools);
        }

        private IPlacementTool GetTool(PlacementToolType toolType)
        {
            switch (toolType)
            {
                case PlacementToolType.Placement:
                    return _placementTool;
                case PlacementToolType.Selection:
                    return _selectionTool;
                case PlacementToolType.Snap:
                    return _snapTool;
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
                _selectionState,
                _selectionTool,
                _toolStates
            );

            _selectionTool?.SetContext(_toolContext);
        }

        /// <summary>
        /// Changes the placement strategy at runtime.
        /// </summary>
        public void SetPlacementStrategy(IPlacementStrategy newStrategy)
        {
            _placementStrategy = newStrategy;
            RebuildToolContext();
            RefreshActiveToolContexts();
        }

        private void ApplyToolStack(IReadOnlyList<IPlacementTool> nextTools)
        {
            var nextSet = new HashSet<IPlacementTool>(nextTools);
            for (int i = 0; i < _activeTools.Count; i++)
            {
                var tool = _activeTools[i];
                if (!nextSet.Contains(tool))
                    tool.OnExit();
            }

            var previousSet = new HashSet<IPlacementTool>(_activeTools);
            _activeTools.Clear();
            _activeTools.AddRange(nextTools);

            for (int i = 0; i < _activeTools.Count; i++)
            {
                var tool = _activeTools[i];
                if (!previousSet.Contains(tool))
                    tool.OnEnter(_toolContext);
            }
        }

        private void RefreshActiveToolContexts()
        {
            for (int i = 0; i < _activeTools.Count; i++)
            {
                _activeTools[i].OnEnter(_toolContext);
            }
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
