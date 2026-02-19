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
            SelectionState = new PlacementSelectionState();
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
    }
}
