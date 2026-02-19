using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Selection;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Validation;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool for ghost placement workflow.
    /// Handles raycast-based placement, socket snapping, validation, and visual feedback.
    /// This is a plain C# class (not MonoBehaviour) that receives the ghost object from the caller.
    /// </summary>
    public class PlacementTool : IPlacementTool
    {
        private GameObject _ghostObject;
        private PlacementToolContext _context;
        private float _currentRotationAngle;
        private Socket _nearestSocket;
        private LayerMask _placementSurface;
        private Vector3 _lastValidPosition;
        private Quaternion _lastValidRotation;
        private GameObject _objectToPrefab;
        private bool _makeObjectsSelectable = true;

        /// <summary>
        /// Fired when placement is confirmed and object is instantiated.
        /// Passes the placed object as a parameter.
        /// Fired after: object instantiation, Selectable component addition (if enabled), socket occupation marking, and ghost cleanup.
        /// The passed object is fully initialized and ready for use.
        /// </summary>
        public System.Action<GameObject> OnPlacementConfirmed { get; set; }

        /// <summary>
        /// Fired when placement is cancelled.
        /// Carries no object since nothing was placed; only confirmation needs the reference.
        /// </summary>
        public System.Action OnPlacementCancelled { get; set; }

        public PlacementTool()
        {
            _currentRotationAngle = 0f;
            _placementSurface = -1;
        }

        public void OnEnter(PlacementToolContext context)
        {
            _context = context;
            _ghostObject = null;
            _currentRotationAngle = 0f;
            _nearestSocket = null;
            _lastValidPosition = Vector3.zero;
            _lastValidRotation = Quaternion.identity;
            _placementSurface = _context != null ? _context.PlacementSurface : -1;
        }

        /// <summary>
        /// Initializes placement with a ghost object and optional prefab for instantiation on confirm.
        /// Initializes the visualizer and disables physics on the ghost.
        /// </summary>
        public void SetupPlacement(GameObject ghostObject, GameObject objectToPrefab, bool makeSelectable = true)
        {
            _ghostObject = ghostObject;
            _objectToPrefab = objectToPrefab ?? ghostObject;
            _makeObjectsSelectable = makeSelectable;

            // Initialize visualizer on ghost
            if (_context?.PlacementVisualizer != null)
            {
                _context.PlacementVisualizer.Initialize(_ghostObject);
            }

            // Disable physics components on ghost
            DisablePhysicsOnGhost(_ghostObject);
        }

        public void OnExit()
        {
            if (_context?.PlacementVisualizer != null)
            {
                _context.PlacementVisualizer.Cleanup();
            }

            if (_context != null)
            {
                var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();
                snapState.ClearRequest();
                snapState.ClearSnap();
            }

            _context = null;
            _ghostObject = null;
            _nearestSocket = null;
        }

        public void HandleInput()
        {
            if (_context == null)
                return;

            var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();
            if (_context.InputProvider == null || _ghostObject == null)
            {
                snapState.ClearRequest();
                return;
            }

            // Handle confirm placement
            if (_context.InputProvider.IsPlaceActionTriggered())
            {
                ConfirmPlacement();
                return;
            }

            // Handle cancel placement
            if (_context.InputProvider.IsCancelActionTriggered())
            {
                CancelPlacement();
                return;
            }

            // Handle rotation
            if (_context.InputProvider.IsRotateActionTriggered())
            {
                _currentRotationAngle += _context.RotationIncrementDegrees;
                if (_currentRotationAngle >= 360f)
                    _currentRotationAngle -= 360f;
            }

            UpdateSnapRequest();
        }

        public void Tick()
        {
            if (_context?.InputProvider == null || _ghostObject == null)
                return;

            UpdateGhostPosition();
        }

        public void HandleSelection(GameObject selected)
        {
            // Not used in placement tool
        }

        /// <summary>
        /// Confirms the placement and instantiates the real object.
        /// Validates final placement, instantiates real object, marks socket as occupied,
        /// cleans up ghost, and fires OnPlacementConfirmed callback.
        /// </summary>
        public void ConfirmPlacement()
        {
            if (_ghostObject == null || _context == null)
                return;

            // Validate final placement
            bool isValid = _context.PlacementValidator.IsPlacementValid(
                _ghostObject.transform.position,
                _ghostObject.transform.rotation,
                _ghostObject
            );

            if (!isValid)
            {
                Debug.LogWarning("Cannot place object - validation failed");
                return;
            }

            // Instantiate real object at ghost position
            GameObject placedObject = Object.Instantiate(
                _objectToPrefab,
                _ghostObject.transform.position,
                _ghostObject.transform.rotation
            );
            placedObject.name = _objectToPrefab.name;

            // Add Selectable component if enabled
            if (_makeObjectsSelectable && placedObject.GetComponent<Selectable>() == null)
            {
                placedObject.AddComponent<Selectable>();
            }

            // Register any sockets on the placed object with SnapManager
            if (_context.SnapManager != null)
            {
                var sockets = placedObject.GetComponentsInChildren<Socket>();
                foreach (var socket in sockets)
                {
                    _context.SnapManager.RegisterSocket(socket);
                }
            }

            // Mark socket as occupied if we snapped to one
            if (_nearestSocket != null)
            {
                _nearestSocket.IsOccupied = true;
            }

            // Clean up ghost
            CleanupGhost();

            // Fire callback with placed object
            OnPlacementConfirmed?.Invoke(placedObject);
        }

        /// <summary>
        /// Cancels the placement and cleans up the ghost object.
        /// Fires OnPlacementCancelled callback.
        /// </summary>
        public void CancelPlacement()
        {
            if (_ghostObject == null)
                return;

            CleanupGhost();
            OnPlacementCancelled?.Invoke();
        }

        /// <summary>
        /// Cleans up the ghost object and visualizer.
        /// </summary>
        private void CleanupGhost()
        {
            if (_context?.PlacementVisualizer != null)
            {
                _context.PlacementVisualizer.Cleanup();
            }

            if (_ghostObject != null)
            {
                Object.Destroy(_ghostObject);
                _ghostObject = null;
            }

            _nearestSocket = null;
        }

        /// <summary>
        /// Updates the ghost object position and rotation based on raycast and strategy.
        /// </summary>
        private void UpdateGhostPosition()
        {
            if (_context?.PlacementCamera == null)
                return;

            var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();
            if (!snapState.HasRequest)
                return;

            Vector3 targetPosition = snapState.RequestPosition;
            Quaternion targetRotation = snapState.RequestRotation;

            _nearestSocket = null;
            if (snapState.HasSnap)
            {
                _nearestSocket = snapState.SnappedSocket;
                targetPosition = snapState.SnappedPosition;
                targetRotation = snapState.SnappedRotation;
            }
            else if (_context.PlacementStrategy != null)
            {
                targetPosition = _context.PlacementStrategy.CalculatePosition(targetPosition, _ghostObject);
                targetRotation = _context.PlacementStrategy.CalculateRotation(targetRotation);
            }

            _ghostObject.transform.position = targetPosition;
            _ghostObject.transform.rotation = targetRotation;

            bool isValid = _context.PlacementValidator.IsPlacementValid(
                targetPosition,
                targetRotation,
                _ghostObject
            );

            _context.PlacementVisualizer?.UpdateVisual(isValid);

            if (isValid)
            {
                _lastValidPosition = targetPosition;
                _lastValidRotation = targetRotation;
            }
        }

        private void UpdateSnapRequest()
        {
            if (_context == null)
                return;

            var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();

            if (_ghostObject == null || _context.PlacementCamera == null)
            {
                snapState.ClearRequest();
                return;
            }

            Vector2 pointerPos = _context.InputProvider.GetPointerPosition();
            Ray ray = _context.PlacementCamera.ScreenPointToRay(pointerPos);

            if (!Physics.Raycast(ray, out RaycastHit hit, _context.MaxRaycastDistance, _placementSurface))
            {
                snapState.ClearRequest();
                return;
            }

            Quaternion baseRotation = Quaternion.Euler(0, _currentRotationAngle, 0);
            var placeableObject = _ghostObject.GetComponent<PlaceableObject>();
            var socketType = placeableObject != null ? placeableObject.RequiredSocketType : null;
            float snapRange = placeableObject != null ? placeableObject.SnapRange : 0f;

            snapState.SetRequest(_ghostObject, hit.point, baseRotation, socketType, snapRange);
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
    }
}
