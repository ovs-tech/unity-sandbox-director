using UnityEngine;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool that resolves snap requests using the shared snap manager.
    /// </summary>
    [CreateAssetMenu(menuName = "Placement System/Tools/Snap Tool")]
    public class SnapTool : ScriptableObject, IPlacementTool
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
                return;
            }

            var snapState = _context.ToolStates.GetOrCreate<SnapToolState>();
            if (!snapState.HasRequest || _context.SnapManager == null)
            {
                snapState.ClearSnap();
                return;
            }

            if (snapState.RequiredSocketType == null)
            {
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
                    return;
                }
                else
                {
                    // Moved beyond range, break the snap and search for new socket
                    snapState.ClearSnap();
                }
            }

            var socket = _context.SnapManager.FindNearestSocket(
                snapState.RequestPosition,
                snapState.RequiredSocketType,
                snapState.SnapRange
            );

            if (socket != null)
            {
                snapState.SetSnap(socket);
            }
            else
            {
                snapState.ClearSnap();
            }
        }

        public void HandleSelection(GameObject selected)
        {
        }
    }
}
