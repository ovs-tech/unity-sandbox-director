using UnityEngine;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool that resolves snap requests using the shared snap manager.
    /// </summary>
    public class SnapTool : IPlacementTool
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
            if (_context == null)
                return;

            var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();
            if (!snapState.HasRequest || _context.SnapManager == null)
            {
                snapState.ClearSnap();
                return;
            }

            if (snapState.RequiredSocketType == null)
            {
                Debug.Log("[SnapTool] No socket type required, clearing snap");
                snapState.ClearSnap();
                return;
            }

            var socket = _context.SnapManager.FindNearestSocket(
                snapState.RequestPosition,
                snapState.RequiredSocketType,
                snapState.SnapRange
            );

            if (socket != null)
            {
                Debug.Log($"[SnapTool] Found snap socket: {socket.name} at {socket.transform.position}, distance: {Vector3.Distance(snapState.RequestPosition, socket.transform.position):F2}");
                snapState.SetSnap(socket);
            }
            else
            {
                Debug.Log($"[SnapTool] No socket found within range {snapState.SnapRange} for type {snapState.RequiredSocketType.name}");
                snapState.ClearSnap();
            }
        }

        public void HandleSelection(GameObject selected)
        {
        }
    }
}
