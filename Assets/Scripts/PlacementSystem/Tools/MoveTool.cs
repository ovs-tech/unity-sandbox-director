using UnityEngine;
using Systems.PlacementSystem.Tools.Commands;
using Systems.PlacementSystem.Core.Components;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool for moving the primary selection with pointer input.
    /// </summary>
    [CreateAssetMenu(menuName = "Placement System/Tools/Move Tool")]
    public class MoveTool : ScriptableObject, IPlacementTool
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
            {
                return;
            }

            Vector3 targetPosition = snapState.RequestPosition;
            Quaternion targetRotation = snapState.RequestRotation;

            if (snapState.HasSnap)
            {
                targetPosition = snapState.SnappedPosition;
                targetRotation = snapState.SnappedRotation;
            }
            else if (_context.PlacementStrategy != null)
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
        }

        /// <summary>
        /// Confirms the current move operation.
        /// </summary>
        public void ConfirmMove()
        {
            if (!_isMoveActive)
                return;

            GameObject movedObject = _movingObject;
            if (movedObject != null)
            {
                var toPos = movedObject.transform.position;
                var toRot = movedObject.transform.rotation;
                var cmd = new MoveObjectCommand(movedObject, _startPosition, _startRotation, toPos, toRot);
                _context.CommandExecutor.Execute(cmd, true, Systems.CommandSystem.CommandManager.DEFAULT_NAMESPACE);
                OnMoveConfirmed?.Invoke(movedObject);
            }

            _movingObject = null;
            _isMoveActive = false;
            UpdateMoveState();
            _context?.SelectionTool?.ClearSelection();
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

            var part = _movingObject.GetComponent<PlacementPart>();
            var socketType = part != null ? part.RequiredSocketType : null;
            float snapRange = part != null ? part.SnapRange : 0f;

            // Always set request with current mouse position - SnapTool handles sticky snap logic
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
