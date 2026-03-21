using System.Collections.Generic;
using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Sockets;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Shared, read-only context for placement tools.
    /// </summary>
    public class PlacementToolContext
    {
        public IInputProvider InputProvider { get; }
        public IPlacementStrategy PlacementStrategy { get; }
        public IPlacementValidator PlacementValidator { get; }
        public IPlacementVisualizer PlacementVisualizer { get; }
        public SnapManager SnapManager { get; }
        public Camera PlacementCamera { get; }
        public float MaxRaycastDistance { get; }
        public float RotationIncrementDegrees { get; }
        public float RotationSnapDegrees { get; }
        public bool AllowMultiSelection { get; }
        public bool RequireModifierForMultiSelect { get; }
        public bool MoveSelectedWithPointer { get; }
        public LayerMask PlacementSurface { get; }
        public LayerMask SelectionLayer { get; }
        public LayerMask SelectionMovementSurface { get; }
        public PlacementSelectionState SelectionState { get; }
        public SelectionTool SelectionTool { get; }
        public ToolStateRegistry ToolStates { get; }
        public ICommandExecutor CommandExecutor { get; }

        public PlacementToolContext(
            IInputProvider inputProvider,
            IPlacementStrategy placementStrategy,
            IPlacementValidator placementValidator,
            IPlacementVisualizer placementVisualizer,
            SnapManager snapManager,
            Camera placementCamera,
            float maxRaycastDistance,
            float rotationIncrementDegrees,
            float rotationSnapDegrees,
            bool allowMultiSelection)
            : this(
                inputProvider,
                placementStrategy,
                placementValidator,
                placementVisualizer,
                snapManager,
                placementCamera,
                maxRaycastDistance,
                rotationIncrementDegrees,
                rotationSnapDegrees,
                allowMultiSelection,
                requireModifierForMultiSelect: false,
                moveSelectedWithPointer: true,
                placementSurface: -1,
                selectionLayer: -1,
                selectionMovementSurface: -1,
                selectionState: null,
                selectionTool: null,
                toolStates: null,
                commandExecutor: null)
        {
        }

        public PlacementToolContext(
            IInputProvider inputProvider,
            IPlacementStrategy placementStrategy,
            IPlacementValidator placementValidator,
            IPlacementVisualizer placementVisualizer,
            SnapManager snapManager,
            Camera placementCamera,
            float maxRaycastDistance,
            float rotationIncrementDegrees,
            float rotationSnapDegrees,
            bool allowMultiSelection,
            bool requireModifierForMultiSelect,
            bool moveSelectedWithPointer,
            LayerMask placementSurface,
            LayerMask selectionLayer,
            LayerMask selectionMovementSurface,
            PlacementSelectionState selectionState,
            SelectionTool selectionTool,
            ToolStateRegistry toolStates,
            ICommandExecutor commandExecutor = null)
        {
            InputProvider = inputProvider;
            PlacementStrategy = placementStrategy;
            PlacementValidator = placementValidator;
            PlacementVisualizer = placementVisualizer;
            SnapManager = snapManager;
            PlacementCamera = placementCamera;
            MaxRaycastDistance = maxRaycastDistance;
            RotationIncrementDegrees = rotationIncrementDegrees;
            RotationSnapDegrees = rotationSnapDegrees;
            AllowMultiSelection = allowMultiSelection;
            RequireModifierForMultiSelect = requireModifierForMultiSelect;
            MoveSelectedWithPointer = moveSelectedWithPointer;
            PlacementSurface = placementSurface;
            SelectionLayer = selectionLayer;
            SelectionMovementSurface = selectionMovementSurface;
            SelectionState = selectionState ?? new PlacementSelectionState();
            SelectionTool = selectionTool;
            ToolStates = toolStates ?? new ToolStateRegistry();
            CommandExecutor = commandExecutor ?? new DefaultCommandExecutor();
        }
    }

    /// <summary>
    /// Shared selection container for placement tools.
    /// </summary>
    public class PlacementSelectionState
    {
        private readonly List<GameObject> _selectedObjects = new List<GameObject>();

        public IReadOnlyList<GameObject> SelectedObjects => _selectedObjects;
        public GameObject PrimarySelection { get; set; }
        public bool HasSelection => _selectedObjects.Count > 0;
        public bool Contains(GameObject obj) => obj != null && _selectedObjects.Contains(obj);

        public void Clear()
        {
            _selectedObjects.Clear();
            PrimarySelection = null;
        }

        public void SetSelection(GameObject primarySelection, IEnumerable<GameObject> selectedObjects)
        {
            _selectedObjects.Clear();

            if (selectedObjects != null)
            {
                _selectedObjects.AddRange(selectedObjects);
            }

            PrimarySelection = primarySelection;
        }

        public void AddSelection(GameObject obj, bool makePrimary)
        {
            if (obj == null)
                return;

            if (!_selectedObjects.Contains(obj))
                _selectedObjects.Add(obj);

            if (makePrimary)
                PrimarySelection = obj;
        }

        public void RemoveSelection(GameObject obj)
        {
            if (obj == null)
                return;

            _selectedObjects.Remove(obj);

            if (PrimarySelection == obj)
                PrimarySelection = _selectedObjects.Count > 0 ? _selectedObjects[0] : null;
        }
    }

    /// <summary>
    /// Shared registry for tool-specific state across the tool pipeline.
    /// </summary>
    public class ToolStateRegistry
    {
        private readonly Dictionary<System.Type, object> _states = new Dictionary<System.Type, object>();

        public T GetOrCreate<T>() where T : new()
        {
            var type = typeof(T);
            if (_states.TryGetValue(type, out var state))
                return (T)state;

            var newState = new T();
            _states[type] = newState;
            return newState;
        }

        public bool TryGet<T>(out T state) where T : class
        {
            if (_states.TryGetValue(typeof(T), out var value))
            {
                state = value as T;
                return state != null;
            }

            state = null;
            return false;
        }

        public void Clear()
        {
            _states.Clear();
        }
    }
}
