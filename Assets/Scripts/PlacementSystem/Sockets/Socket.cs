using UnityEngine;

namespace Systems.PlacementSystem.Sockets
{
    /// <summary>
    /// Represents a snap point for modular building.
    /// Sockets define exact positions and rotations where objects should connect.
    /// Example use cases: wall panels snapping to wall sockets, lights snapping to ceiling grid.
    /// </summary>
    public class Socket : MonoBehaviour
    {
        [Header("Socket Configuration")]
        [SerializeField, Tooltip("The type of socket (what can connect here)")]
        private SocketType _socketType;

        [SerializeField, Tooltip("Is this socket currently occupied?")]
        private bool _isOccupied = false;

        [Header("Debug Visualization")]
        [SerializeField, Tooltip("Show socket gizmo in scene view")]
        private bool _showGizmo = true;

        [SerializeField, Tooltip("Size of the gizmo sphere")]
        private float _gizmoSize = 0.2f;

        /// <summary>
        /// Gets the socket type.
        /// </summary>
        public SocketType SocketType => _socketType;

        /// <summary>
        /// Gets or sets whether this socket is occupied.
        /// </summary>
        public bool IsOccupied
        {
            get => _isOccupied;
            set => _isOccupied = value;
        }

        /// <summary>
        /// Checks if this socket can accept objects of the given type.
        /// </summary>
        public bool CanAccept(SocketType requiredType)
        {
            if (_isOccupied)
                return false;

            if (requiredType == null || _socketType == null)
                return false;

            return _socketType == requiredType;
        }

        private void OnDrawGizmos()
        {
            if (!_showGizmo || _socketType == null)
                return;

            // Draw socket position
            Gizmos.color = _isOccupied ? Color.red : _socketType.GizmoColor;
            Gizmos.DrawSphere(transform.position, _gizmoSize);

            // Draw forward direction arrow
            Gizmos.color = Color.blue;
            Vector3 forward = transform.forward * (_gizmoSize * 3f);
            Gizmos.DrawLine(transform.position, transform.position + forward);

            // Draw coordinate axes for orientation
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.right * _gizmoSize);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * _gizmoSize);
        }

        private void OnDrawGizmosSelected()
        {
            if (_socketType == null)
                return;

            // Draw more detailed info when selected
            Gizmos.color = _socketType.GizmoColor;
            Gizmos.DrawWireSphere(transform.position, _gizmoSize * 2f);

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"{_socketType.DisplayName}\n{(_isOccupied ? "Occupied" : "Available")}"
            );
            #endif
        }

        /// <summary>
        /// Clears the occupied status (useful when an object is removed).
        /// </summary>
        public void Clear()
        {
            _isOccupied = false;
        }
    }
}
