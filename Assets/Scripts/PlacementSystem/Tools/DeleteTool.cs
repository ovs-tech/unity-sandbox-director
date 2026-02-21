using UnityEngine;
using Systems.PlacementSystem.Tools.Commands;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool for deleting selected objects on delete input.
    /// </summary>
    public class DeleteTool : IPlacementTool
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

            if (!_context.InputProvider.IsDeleteActionTriggered())
                return;

            var selectedObjects = _context.SelectionState.SelectedObjects;
            for (int i = selectedObjects.Count - 1; i >= 0; i--)
            {
                var obj = selectedObjects[i];
                if (obj != null)
                {
                    var cmd = new DeleteObjectCommand(obj, _context.SnapManager);
                    Systems.CommandSystem.CommandManager.Instance.ExecuteCommand(cmd, false, Systems.CommandSystem.CommandManager.DEFAULT_NAMESPACE);
                }
            }

            _context.SelectionState.Clear();
            _context.PlacementVisualizer?.Cleanup();
        }

        public void Tick()
        {
        }

        public void HandleSelection(GameObject selected)
        {
        }
    }
}
