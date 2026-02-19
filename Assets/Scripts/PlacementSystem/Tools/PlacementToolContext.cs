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
                selectionState: null)
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
            PlacementSelectionState selectionState)
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
}
