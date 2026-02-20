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
            {
                Debug.Log("[SnapTool.Tick] Context is null");
                return;
            }

            var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();
            if (!snapState.HasRequest || _context.SnapManager == null)
            {
                if (!snapState.HasRequest)
                    Debug.Log("[SnapTool.Tick] No snap request");
                if (_context.SnapManager == null)
                    Debug.Log("[SnapTool.Tick] SnapManager is null");
                    
                snapState.ClearSnap();
                return;
            }

            if (snapState.RequiredSocketType == null)
            {
                Debug.Log("[SnapTool] No socket type required, clearing snap");
                snapState.ClearSnap();
                return;
            }

            // Sticky snap logic: if already snapped, only break if request moved beyond snap range
            if (snapState.HasSnap)
            {
                float distanceFromSnappedSocket = Vector3.Distance(snapState.RequestPosition, snapState.SnappedPosition);
                
                if (distanceFromSnappedSocket <= snapState.SnapRange)
                {
                    // Still within range, maintain the snap
                    Debug.Log($"[SnapTool] Maintaining snap to {snapState.SnappedSocket.name}, distance: {distanceFromSnappedSocket:F2} <= {snapState.SnapRange}");
                    return;
                }
                else
                {
                    // Moved beyond range, break the snap and search for new socket
                    Debug.Log($"[SnapTool] Breaking snap - distance {distanceFromSnappedSocket:F2} > {snapState.SnapRange}");
                    snapState.ClearSnap();
                }
            }

            // Search for nearest socket at request position
            Debug.Log($"[SnapTool.Tick] Calling FindNearestSocket - Position: {snapState.RequestPosition}, Type: {snapState.RequiredSocketType.name}, Range: {snapState.SnapRange}");

            var socket = _context.SnapManager.FindNearestSocket(
                snapState.RequestPosition,
                snapState.RequiredSocketType,
                snapState.SnapRange
            );

            if (socket != null)
            {
                Debug.Log($"[SnapTool] Found snap socket: {socket.name} at {socket.transform.position}, distance: {Vector3.Distance(snapState.RequestPosition, socket.transform.position):F2}");
                snapState.SetSnap(socket);
                Debug.Log($"[SnapTool] After SetSnap - HasSnap: {snapState.HasSnap}, SnappedPosition: {snapState.SnappedPosition}");
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
