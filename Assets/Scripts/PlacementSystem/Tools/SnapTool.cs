using UnityEngine;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Tool that resolves snap requests using the shared snap manager.
    /// </summary>
    [CreateAssetMenu(menuName = "Placement System/Tools/Snap Tool")]
    public class SnapTool : ScriptableObject, IPlacementTool
    {
        [SerializeField, Tooltip("Optional cap for snap distance to keep movement stable. Set <= 0 to use per-part SnapRange only.")]
        private float _maxStableSnapDistance = 0f;

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

            float effectiveSnapRange = snapState.SnapRange;
            if (_maxStableSnapDistance > 0f)
            {
                effectiveSnapRange = Mathf.Min(effectiveSnapRange, _maxStableSnapDistance);
            }

            if (effectiveSnapRange <= 0f)
            {
                snapState.ClearSnap();
                return;
            }

            // Sticky snap logic: if already snapped, only break if request moved beyond snap range
            if (snapState.HasSnap)
            {
                float distanceFromSnappedSocket = Vector3.Distance(snapState.RequestPosition, snapState.SnappedPosition);

                if (distanceFromSnappedSocket <= effectiveSnapRange)
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
                effectiveSnapRange
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
