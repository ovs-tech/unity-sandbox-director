using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Validation;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool for moving the primary selection with pointer input.
    /// </summary>
    public class MoveTool : IPlacementTool
    {
        private PlacementToolContext _context;
        private GameObject _movingObject;
        private bool _isMoveActive;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        /// <summary>
        /// Fired when a move is confirmed.
        /// </summary>
        public System.Action<GameObject> OnMoveConfirmed { get; set; }

        /// <summary>
        /// Fired when a move is cancelled.
        /// </summary>
        public System.Action OnMoveCancelled { get; set; }

        public void OnEnter(PlacementToolContext context)
        {
            _context = context;
            _movingObject = null;
            _isMoveActive = false;
            _startPosition = Vector3.zero;
            _startRotation = Quaternion.identity;
            SetMoveToolEnabled(true);
            UpdateMoveState();
        }

        public void OnExit()
        {
            if (_isMoveActive)
            {
                CancelMove();
            }

            if (_context != null)
            {
                var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();
                snapState.ClearRequest();
                snapState.ClearSnap();
            }

            SetMoveToolEnabled(false);
            UpdateMoveState();
            _context = null;
            _movingObject = null;
            _isMoveActive = false;
        }

        public void HandleInput()
        {
            if (_context?.InputProvider == null)
                return;

            UpdateMoveState();

            if (!_isMoveActive)
            {
                _context.ToolStates.GetOrCreate<SnapToolState>().ClearRequest();
                var selectionInput = _context.ToolStates.GetOrCreate<SelectionToolState>();
                if (selectionInput.WasSelectionInput && selectionInput.ClickedObject != null &&
                    selectionInput.WasPrimarySelection)
                {
                    StartMove(selectionInput.ClickedObject);
                }
                return;
            }

            UpdateSnapRequest();

            if (_context.InputProvider.IsPlaceActionTriggered())
            {
                ConfirmMove();
                return;
            }

            if (_context.InputProvider.IsCancelActionTriggered())
            {
                CancelMove();
                return;
            }
        }

        public void Tick()
        {
            if (_context?.InputProvider == null || _context.PlacementCamera == null)
                return;

            if (!_context.MoveSelectedWithPointer)
                return;

            if (!_isMoveActive)
                return;

            GameObject selected = _movingObject;
            if (selected == null)
                return;

            var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();
            if (!snapState.HasRequest)
                return;

            Vector3 targetPosition = snapState.RequestPosition;
            Quaternion targetRotation = snapState.RequestRotation;

            if (snapState.HasSnap)
            {
                Debug.Log($"[MoveTool] Using snap position: {snapState.SnappedPosition}, socket: {snapState.SnappedSocket.name}");
                targetPosition = snapState.SnappedPosition;
                targetRotation = snapState.SnappedRotation;
            }
            else if (_context.PlacementStrategy != null)
            {
                Debug.Log($"[MoveTool] Using placement strategy, no snap");
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

        /// <summary>
        /// Starts a move operation for the selected object.
        /// </summary>
        public void StartMove(GameObject selected)
        {
            if (_context == null || !_context.MoveSelectedWithPointer)
                return;

            if (selected == null)
                return;

            _movingObject = selected;
            _startPosition = selected.transform.position;
            _startRotation = selected.transform.rotation;
            _isMoveActive = true;
            UpdateMoveState();
            Debug.Log($"Started moving object: {_movingObject.name}");
        }

        /// <summary>
        /// Confirms the current move operation.
        /// </summary>
        public void ConfirmMove()
        {
            if (!_isMoveActive)
                return;

            GameObject movedObject = _movingObject;
            _movingObject = null;
            _isMoveActive = false;
            UpdateMoveState();

            Debug.Log($"Confirmed move for object: {movedObject.name}");
            _context?.SelectionTool?.ClearSelection();
            OnMoveConfirmed?.Invoke(movedObject);
        }

        /// <summary>
        /// Cancels the current move operation and restores the start transform.
        /// </summary>
        public void CancelMove()
        {
            if (!_isMoveActive)
                return;

            if (_movingObject != null)
            {
                _movingObject.transform.position = _startPosition;
                _movingObject.transform.rotation = _startRotation;
            }

            Debug.Log($"Cancelled move for object: {_movingObject.name}");

            _movingObject = null;
            _isMoveActive = false;
            UpdateMoveState();

            _context?.SelectionTool?.ClearSelection();
            OnMoveCancelled?.Invoke();
        }

        private void UpdateMoveState()
        {
            if (_context == null)
                return;

            _context.ToolStates.GetOrCreate<MoveToolState>().IsMoveActive = _isMoveActive;
        }

        private void UpdateSnapRequest()
        {
            if (_context == null || _context.PlacementCamera == null)
                return;

            var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();

            if (!_isMoveActive || _movingObject == null)
            {
                snapState.ClearRequest();
                return;
            }

            Vector2 pointerPosition = _context.InputProvider.GetPointerPosition();
            Ray ray = _context.PlacementCamera.ScreenPointToRay(pointerPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, _context.MaxRaycastDistance, _context.SelectionMovementSurface))
            {
                snapState.ClearRequest();
                return;
            }

            var placeable = _movingObject.GetComponent<PlaceableObject>();
            var socketType = placeable != null ? placeable.RequiredSocketType : null;
            float snapRange = placeable != null ? placeable.SnapRange : 0f;

            Debug.Log($"[MoveTool] Snap request - Object: {_movingObject.name}, SocketType: {socketType?.name ?? "null"}, Range: {snapRange}, Position: {hit.point}");
            snapState.SetRequest(_movingObject, hit.point, _movingObject.transform.rotation, socketType, snapRange);
        }

        private void SetMoveToolEnabled(bool isEnabled)
        {
            if (_context == null)
                return;

            _context.ToolStates.GetOrCreate<MoveToolState>().IsMoveToolEnabled = isEnabled;
        }
    }
}
