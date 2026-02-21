using UnityEngine;
using Systems.PlacementSystem.Tools.Commands;
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

            var fromRot = selected.transform.rotation;
            var toRot = fromRot * Quaternion.Euler(0, _context.RotationIncrementDegrees, 0);
            var cmd = new RotateObjectCommand(selected, fromRot, toRot);
            Systems.CommandSystem.CommandManager.Instance.ExecuteCommand(cmd, true, Systems.CommandSystem.CommandManager.DEFAULT_NAMESPACE);
        }

        public void Tick()
        {
        }

        public void HandleSelection(GameObject selected)
        {
        }
    }
}
