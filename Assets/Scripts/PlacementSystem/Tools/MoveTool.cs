using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Validation;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool for moving the primary selection with pointer input.
    /// </summary>
    public class MoveTool : IPlacementTool
    {
        private PlacementToolContext _context;

        public void OnEnter(PlacementToolContext context)
        {
            _context = context;
        }

        public void OnExit()
        {
            _context = null;
        }

        public void HandleInput()
        {
        }

        public void Tick()
        {
            if (_context?.InputProvider == null || _context.PlacementCamera == null)
                return;

            if (!_context.MoveSelectedWithPointer)
                return;

            GameObject selected = _context.SelectionState.PrimarySelection;
            if (selected == null)
                return;

            Vector2 pointerPosition = _context.InputProvider.GetPointerPosition();
            Ray ray = _context.PlacementCamera.ScreenPointToRay(pointerPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, _context.MaxRaycastDistance, _context.SelectionMovementSurface))
                return;

            Vector3 targetPosition = hit.point;
            Quaternion targetRotation = selected.transform.rotation;

            if (_context.SnapManager != null)
            {
                var placeable = selected.GetComponent<PlaceableObject>();
                if (placeable != null && placeable.RequiredSocketType != null)
                {
                    Socket nearest = _context.SnapManager.FindNearestSocket(
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

            if (_context.PlacementStrategy != null)
            {
                targetPosition = _context.PlacementStrategy.CalculatePosition(targetPosition, selected);
                targetRotation = _context.PlacementStrategy.CalculateRotation(targetRotation);
            }

            selected.transform.position = targetPosition;
            selected.transform.rotation = targetRotation;
            _context.PlacementVisualizer?.UpdateVisual(true);
        }

        public void HandleSelection(GameObject selected)
        {
        }
    }
}
