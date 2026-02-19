using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Selection;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool for selection input and shared selection state management.
    /// </summary>
    public class SelectionTool : IPlacementTool
    {
        private PlacementToolContext _context;

        public void OnEnter(PlacementToolContext context)
        {
            SetContext(context);
        }

        public void OnExit()
        {
            _context = null;
        }

        public void SetContext(PlacementToolContext context)
        {
            _context = context;
        }

        public void HandleInput()
        {
            if (_context == null)
                return;

            var selectionState = _context.ToolStates.GetOrCreate<SelectionToolState>();
            selectionState.Clear();

            if (_context.InputProvider == null || _context.PlacementCamera == null)
                return;

            _context.ToolStates.TryGet(out MoveToolState moveState);
            if (moveState != null && moveState.IsMoveActive)
                return;

            if (!_context.InputProvider.IsPlaceActionTriggered())
                return;

            GameObject selected = TrySelectObject();
            bool wasPrimarySelection = _context.SelectionState.PrimarySelection == selected;
            selectionState.RecordSelectionInput(selected, wasPrimarySelection);

            if (moveState != null && moveState.IsMoveToolEnabled && wasPrimarySelection && !IsMultiSelectModifierHeld())
                return;

            HandleSelection(selected);
        }

        public void Tick()
        {
        }

        public void HandleSelection(GameObject selected)
        {
            if (_context == null)
                return;

            if (selected == null)
            {
                ClearSelection();
                return;
            }

            var selectable = selected.GetComponent<Selectable>();
            if (selectable != null && !selectable.IsSelectable)
                return;

            bool multiSelect = _context.AllowMultiSelection &&
                (!_context.RequireModifierForMultiSelect || IsMultiSelectModifierHeld());

            if (multiSelect)
                ToggleSelection(selected);
            else
                SelectObject(selected);
        }

        public GameObject TrySelectObject()
        {
            if (_context?.PlacementCamera == null)
                return null;

            Vector2 pointerPosition = _context.InputProvider.GetPointerPosition();
            Ray ray = _context.PlacementCamera.ScreenPointToRay(pointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _context.MaxRaycastDistance, _context.SelectionLayer))
            {
                var selectable = hit.collider.GetComponentInParent<Selectable>();
                return selectable != null ? selectable.gameObject : null;
            }

            return null;
        }

        public void SelectObject(GameObject obj)
        {
            if (obj == null || _context.SelectionState.Contains(obj))
                return;

            _context.SelectionState.AddSelection(obj, makePrimary: true);
            obj.GetComponent<Selectable>()?.NotifySelected();

            _context.PlacementVisualizer?.Cleanup();
            _context.PlacementVisualizer?.Initialize(obj);
            _context.PlacementVisualizer?.UpdateVisual(true);
        }

        private void DeselectObject(GameObject obj)
        {
            if (obj == null || !_context.SelectionState.Contains(obj))
                return;

            _context.SelectionState.RemoveSelection(obj);
            obj.GetComponent<Selectable>()?.NotifyDeselected();

            if (_context.SelectionState.PrimarySelection != null)
            {
                _context.PlacementVisualizer?.Cleanup();
                _context.PlacementVisualizer?.Initialize(_context.SelectionState.PrimarySelection);
                _context.PlacementVisualizer?.UpdateVisual(true);
            }
            else
            {
                _context.PlacementVisualizer?.Cleanup();
            }
        }

        public void ClearSelection()
        {
            var selectedObjects = _context.SelectionState.SelectedObjects;
            for (int i = selectedObjects.Count - 1; i >= 0; i--)
            {
                var obj = selectedObjects[i];
                if (obj != null)
                    obj.GetComponent<Selectable>()?.NotifyDeselected();
            }

            _context.SelectionState.Clear();
            _context.PlacementVisualizer?.Cleanup();
        }

        private void ToggleSelection(GameObject obj)
        {
            if (_context.SelectionState.Contains(obj))
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
    }
}
