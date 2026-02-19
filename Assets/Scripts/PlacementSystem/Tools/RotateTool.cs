using UnityEngine;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool for rotating the primary selection by rotation increment.
    /// </summary>
    public class RotateTool : IPlacementTool
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
            if (_context?.InputProvider == null)
                return;

            if (!_context.InputProvider.IsRotateActionTriggered())
                return;

            GameObject selected = _context.SelectionState.PrimarySelection;
            if (selected == null)
                return;

            selected.transform.Rotate(Vector3.up, _context.RotationIncrementDegrees, Space.World);
        }

        public void Tick()
        {
        }

        public void HandleSelection(GameObject selected)
        {
        }
    }
}
