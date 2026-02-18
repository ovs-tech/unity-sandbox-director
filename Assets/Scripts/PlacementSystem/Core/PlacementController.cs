using UnityEngine;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Selection;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Main orchestrator for the placement system.
    /// Coordinates input, strategy, validation, visualization, and socket snapping.
    /// Follows the Dependency Injection pattern - all dependencies are injected via inspector or constructor.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
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

        [SerializeField, Tooltip("Reference to the selection manager (optional)")]
        private SelectionManager _selectionManager;

        [Header("Placement Settings")]
        [SerializeField, Tooltip("Maximum raycast distance")]
        private float _maxRaycastDistance = 100f;

        [SerializeField, Tooltip("Current rotation angle (in degrees, increments of 90)")]
        private float _currentRotationAngle = 0f;

        [SerializeField, Tooltip("Rotation increment per rotation input")]
        private float _rotationIncrement = 90f;

        [SerializeField, Tooltip("Automatically add Selectable component to placed objects")]
        private bool _makeObjectsSelectable = true;

        [Header("Selection Settings")]
        [SerializeField, Tooltip("Layer mask for selection movement surfaces")]
        private LayerMask _selectionMovementSurface = -1;

        [SerializeField, Tooltip("Allow multi-selection of objects")]
        private bool _allowMultiSelection = false;

        [SerializeField, Tooltip("Require Ctrl/Cmd for multi-selection")]
        private bool _requireModifierForMultiSelect = true;

        [SerializeField, Tooltip("Move selected object to pointer each frame")]
        private bool _moveSelectedWithPointer = true;

        // Dependency interfaces resolved from components
        private IInputProvider _inputProvider;
        private IPlacementStrategy _placementStrategy;
        private IPlacementValidator _placementValidator;
        private IPlacementVisualizer _placementVisualizer;

        // Runtime state
        private GameObject _ghostObject;
        private bool _isPlacementActive;
        private Vector3 _lastValidPosition;
        private Quaternion _lastValidRotation;
        private Socket _nearestSocket;
        [SerializeField, Tooltip("Currently selected object (for selection manager)")]
        private GameObject _selectedObject;
        private readonly System.Collections.Generic.List<GameObject> _selectedObjects = new System.Collections.Generic.List<GameObject>();

        private void Awake()
        {
            // Resolve interface dependencies from serialized MonoBehaviour components
            // This allows flexible assignment in the inspector while maintaining loose coupling
            _inputProvider = _inputProviderComponent as IInputProvider;
            _placementStrategy = _placementStrategyComponent as IPlacementStrategy;
            _placementValidator = _placementValidatorComponent as IPlacementValidator;
            _placementVisualizer = _placementVisualizerComponent as IPlacementVisualizer;

            // Validate dependencies
            if (_inputProvider == null)
                Debug.LogError("PlacementController: Input provider must implement IInputProvider");
            if (_placementStrategy == null)
                Debug.LogError("PlacementController: Placement strategy must implement IPlacementStrategy");
            if (_placementValidator == null)
                Debug.LogError("PlacementController: Placement validator must implement IPlacementValidator");
            if (_placementVisualizer == null)
                Debug.LogError("PlacementController: Placement visualizer must implement IPlacementVisualizer");

            if (_selectionManager != null)
                _selectionManager.SetPlacementController(this);

            if (_placementCamera == null)
                _placementCamera = Camera.main;
        }

        /// <summary>
        /// Starts the placement process with a ghost object.
        /// </summary>
        public void StartPlacement()
        {
            if (_isPlacementActive || _objectToPlace == null)
                return;

            // Create ghost preview
            _ghostObject = Instantiate(_objectToPlace);
            _ghostObject.name = $"{_objectToPlace.name}_Ghost";

            // Disable physics and collisions on ghost
            DisablePhysicsOnGhost(_ghostObject);

            // Initialize visualizer
            _placementVisualizer?.Initialize(_ghostObject);

            _isPlacementActive = true;
            _currentRotationAngle = 0f;
        }

        /// <summary>
        /// Stops the placement process and cleans up.
        /// </summary>
        public void CancelPlacement()
        {
            if (!_isPlacementActive)
                return;

            _placementVisualizer?.Cleanup();

            if (_ghostObject != null)
                Destroy(_ghostObject);

            _isPlacementActive = false;
            _nearestSocket = null;
        }

        /// <summary>
        /// Confirms placement and instantiates the real object.
        /// </summary>
        public void ConfirmPlacement()
        {
            if (!_isPlacementActive || _ghostObject == null)
                return;

            // Validate final placement
            bool isValid = _placementValidator.IsPlacementValid(
                _ghostObject.transform.position,
                _ghostObject.transform.rotation,
                _ghostObject
            );

            if (!isValid)
            {
                Debug.LogWarning("Cannot place object - validation failed");
                return;
            }

            // Instantiate real object
            GameObject placedObject = Instantiate(
                _objectToPlace,
                _ghostObject.transform.position,
                _ghostObject.transform.rotation
            );
            placedObject.name = _objectToPlace.name;

            // Make object selectable if enabled
            if (_makeObjectsSelectable)
            {
                if (placedObject.GetComponent<Selectable>() == null)
                {
                    placedObject.AddComponent<Selectable>();
                }
            }

            // Mark socket as occupied if we snapped to one
            if (_nearestSocket != null)
            {
                _nearestSocket.IsOccupied = true;
            }

            // Clean up and prepare for next placement
            _placementVisualizer?.Cleanup();
            Destroy(_ghostObject);

            // Automatically start a new placement for quick successive placements
            StartPlacement();
        }

        private void Update()
        {
            if (!_isPlacementActive || _ghostObject == null)
                return;

            if (_moveSelectedWithPointer && _selectedObject != null)
            {
                UpdateSelectedObjectTransform();
            }

            // Handle rotation input
            if (_inputProvider.IsRotateActionTriggered())
            {
                _currentRotationAngle += _rotationIncrement;
                if (_currentRotationAngle >= 360f)
                    _currentRotationAngle -= 360f;
            }

            // Handle cancel input
            if (_inputProvider.IsCancelActionTriggered())
            {
                CancelPlacement();
                return;
            }

            // Handle place input
            if (_inputProvider.IsPlaceActionTriggered())
            {
                ConfirmPlacement();
                return;
            }

            // Update ghost position and rotation
            UpdateGhostPosition();
        }

        /// <summary>
        /// Handles selection click from SelectionManager.
        /// </summary>
        public void HandleSelectionClick(GameObject selected)
        {
            if (selected == null)
            {
                DeselectAll();
                return;
            }

            var selectable = selected.GetComponent<Selectable>();
            if (selectable != null && !selectable.IsSelectable)
                return;

            bool multiSelect = _allowMultiSelection &&
                (!_requireModifierForMultiSelect || IsMultiSelectModifierHeld());

            if (multiSelect)
                ToggleSelection(selected);
            else
                SelectObject(selected);
        }

        private void UpdateSelectedObjectTransform()
        {
            if (_inputProvider == null || _selectedObject == null)
                return;

            Vector2 pointerPosition = _inputProvider.GetPointerPosition();
            Ray ray = _placementCamera.ScreenPointToRay(pointerPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, _selectionMovementSurface))
                return;

            Vector3 targetPosition = hit.point;
            Quaternion targetRotation = _selectedObject.transform.rotation;

            if (_snapManager != null)
            {
                var placeable = _selectedObject.GetComponent<Validation.PlaceableObject>();
                if (placeable != null && placeable.RequiredSocketType != null)
                {
                    var nearest = _snapManager.FindNearestSocket(
                        hit.point,
                        placeable.RequiredSocketType,
                        placeable.SnapRange
                    );

                    if (nearest != null)
                    {
                        targetPosition = nearest.transform.position;
                        targetRotation = nearest.transform.rotation;
                    }
                }
            }

            if (_placementStrategy != null)
            {
                targetPosition = _placementStrategy.CalculatePosition(targetPosition, _selectedObject);
                targetRotation = _placementStrategy.CalculateRotation(targetRotation);
            }

            _selectedObject.transform.position = targetPosition;
            _selectedObject.transform.rotation = targetRotation;

            _placementVisualizer?.UpdateVisual(true);
        }

        /// <summary>
        /// Updates the ghost object position and rotation based on input and strategy.
        /// </summary>
        private void UpdateGhostPosition()
        {
            // Raycast from pointer to find placement surface
            Vector2 pointerPos = _inputProvider.GetPointerPosition();
            Ray ray = _placementCamera.ScreenPointToRay(pointerPos);

            if (Physics.Raycast(ray, out RaycastHit hit, _maxRaycastDistance, _placementSurface))
            {
                Vector3 targetPosition = hit.point;
                Quaternion targetRotation = Quaternion.Euler(0, _currentRotationAngle, 0);

                // Check for socket snapping first (overrides strategy if available)
                _nearestSocket = null;
                if (_snapManager != null)
                {
                    var placeableObject = _ghostObject.GetComponent<Validation.PlaceableObject>();
                    if (placeableObject != null && placeableObject.RequiredSocketType != null)
                    {
                        _nearestSocket = _snapManager.FindNearestSocket(
                            hit.point,
                            placeableObject.RequiredSocketType,
                            placeableObject.SnapRange
                        );
                    }
                }

                // If socket found, snap to it; otherwise use strategy
                if (_nearestSocket != null)
                {
                    targetPosition = _nearestSocket.transform.position;
                    targetRotation = _nearestSocket.transform.rotation;
                }
                else
                {
                    // Apply placement strategy
                    targetPosition = _placementStrategy.CalculatePosition(hit.point, _ghostObject);
                    targetRotation = _placementStrategy.CalculateRotation(targetRotation);
                }

                // Update ghost transform
                _ghostObject.transform.position = targetPosition;
                _ghostObject.transform.rotation = targetRotation;

                // Validate placement
                bool isValid = _placementValidator.IsPlacementValid(
                    targetPosition,
                    targetRotation,
                    _ghostObject
                );

                // Update visual feedback
                _placementVisualizer?.UpdateVisual(isValid);

                if (isValid)
                {
                    _lastValidPosition = targetPosition;
                    _lastValidRotation = targetRotation;
                }
            }
        }

        /// <summary>
        /// Disables physics components on the ghost object to prevent interference.
        /// </summary>
        private void DisablePhysicsOnGhost(GameObject ghost)
        {
            // Disable all rigidbodies
            var rigidbodies = ghost.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
            }

            // Disable all colliders (make them triggers so they can still be used for validation)
            var colliders = ghost.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.isTrigger = true;
            }
        }

        /// <summary>
        /// Sets the object to be placed.
        /// </summary>
        public void SetObjectToPlace(GameObject prefab)
        {
            _objectToPlace = prefab;
        }

        /// <summary>
        /// Changes the placement strategy at runtime.
        /// Demonstrates Open-Closed Principle: new strategies can be added without modifying this controller.
        /// </summary>
        public void SetPlacementStrategy(IPlacementStrategy newStrategy)
        {
            _placementStrategy = newStrategy;
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
        /// Selects an object (single selection by default).
        /// </summary>
        public void SelectObject(GameObject obj)
        {
            if (obj == null)
                return;

            if (!_allowMultiSelection)
                DeselectAll();

            if (!_selectedObjects.Contains(obj))
                _selectedObjects.Add(obj);

            _selectedObject = obj;
            obj.GetComponent<Selectable>()?.NotifySelected();

            _placementVisualizer?.Cleanup();
            _placementVisualizer?.Initialize(obj);
            _placementVisualizer?.UpdateVisual(true);
        }

        /// <summary>
        /// Deselects a specific object.
        /// </summary>
        public void DeselectObject(GameObject obj)
        {
            if (obj == null || !_selectedObjects.Contains(obj))
                return;

            _selectedObjects.Remove(obj);
            obj.GetComponent<Selectable>()?.NotifyDeselected();

            if (_selectedObject == obj)
            {
                _selectedObject = _selectedObjects.Count > 0 ? _selectedObjects[0] : null;
                if (_selectedObject != null)
                {
                    _placementVisualizer?.Cleanup();
                    _placementVisualizer?.Initialize(_selectedObject);
                    _placementVisualizer?.UpdateVisual(true);
                }
                else
                {
                    _placementVisualizer?.Cleanup();
                }
            }
        }

        /// <summary>
        /// Deselects all currently selected objects.
        /// </summary>
        public void DeselectAll()
        {
            foreach (var obj in _selectedObjects)
            {
                if (obj != null)
                    obj.GetComponent<Selectable>()?.NotifyDeselected();
            }

            _selectedObjects.Clear();
            _selectedObject = null;
            _placementVisualizer?.Cleanup();
        }

        /// <summary>
        /// Toggles selection state of an object.
        /// </summary>
        public void ToggleSelection(GameObject obj)
        {
            if (obj == null)
                return;

            if (_selectedObjects.Contains(obj))
                DeselectObject(obj);
            else
                SelectObject(obj);
        }

        private bool IsMultiSelectModifierHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftControl) ||
                   UnityEngine.Input.GetKey(KeyCode.RightControl) ||
                   UnityEngine.Input.GetKey(KeyCode.LeftCommand) ||
                   UnityEngine.Input.GetKey(KeyCode.RightCommand);
        }

        private void OnDrawGizmos()
        {
            if (_isPlacementActive && _ghostObject != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_ghostObject.transform.position, 0.3f);

                if (_nearestSocket != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(_ghostObject.transform.position, _nearestSocket.transform.position);
                }
            }
        }
    }
}
