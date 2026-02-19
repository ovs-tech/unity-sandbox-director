using UnityEngine;
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
        }

        public void OnExit()
        {
            _context = null;
            _ghostObject = null;
            _nearestSocket = null;
        }

        /// <summary>
        /// Sets the ghost object for this placement session.
        /// </summary>
        public void SetGhostObject(GameObject ghostObject)
        {
            _ghostObject = ghostObject;
        }

        public void HandleInput()
        {
            if (_context?.InputProvider == null || _ghostObject == null)
                return;

            if (_context.InputProvider.IsRotateActionTriggered())
            {
                _currentRotationAngle += _context.RotationIncrementDegrees;
                if (_currentRotationAngle >= 360f)
                    _currentRotationAngle -= 360f;
            }
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
        /// Updates the ghost object position and rotation based on raycast and strategy.
        /// </summary>
        private void UpdateGhostPosition()
        {
            if (_context?.PlacementCamera == null)
                return;

            Vector2 pointerPos = _context.InputProvider.GetPointerPosition();
            Ray ray = _context.PlacementCamera.ScreenPointToRay(pointerPos);

            if (Physics.Raycast(ray, out RaycastHit hit, _context.MaxRaycastDistance, _placementSurface))
            {
                Vector3 targetPosition = hit.point;
                Quaternion targetRotation = Quaternion.Euler(0, _currentRotationAngle, 0);

                _nearestSocket = null;
                if (_context.SnapManager != null)
                {
                    var placeableObject = _ghostObject.GetComponent<PlaceableObject>();
                    if (placeableObject != null && placeableObject.RequiredSocketType != null)
                    {
                        _nearestSocket = _context.SnapManager.FindNearestSocket(
                            hit.point,
                            placeableObject.RequiredSocketType,
                            placeableObject.SnapRange
                        );
                    }
                }

                if (_nearestSocket != null)
                {
                    targetPosition = _nearestSocket.transform.position;
                    targetRotation = _nearestSocket.transform.rotation;
                }
                else if (_context.PlacementStrategy != null)
                {
                    targetPosition = _context.PlacementStrategy.CalculatePosition(hit.point, _ghostObject);
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
        }
    }
}
