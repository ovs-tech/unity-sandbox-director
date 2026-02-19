using UnityEngine;

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
                    // Unregister any sockets before destroying
                    if (_context.SnapManager != null)
                    {
                        var sockets = obj.GetComponentsInChildren<Sockets.Socket>();
                        foreach (var socket in sockets)
                        {
                            _context.SnapManager.UnregisterSocket(socket);
                        }
                    }
                    
                    Object.Destroy(obj);
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
