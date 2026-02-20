using UnityEngine;

namespace Systems.PlacementSystem.Sockets
{
    /// <summary>
    /// Represents a snap point for modular building.
    /// Sockets define exact positions and rotations where objects should connect.
    /// Example use cases: wall panels snapping to wall sockets, lights snapping to ceiling grid.
    /// </summary>
    [RequireComponent(typeof(Collider))]
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

        
        // Gizmo size will be calculated from the collider bounds at runtime

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

            // Choose color based on occupied state
            Color baseColor = _isOccupied ? Color.red : _socketType.GizmoColor;
            Gizmos.color = baseColor;

            // Try to find a collider on this object or its children
            Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();

            float gizmoSize = 0.2f;
            if (col != null)
            {
                // Visualize common collider shapes
                if (col is BoxCollider box)
                {
                    Vector3 worldCenter = transform.TransformPoint(box.center);
                    Vector3 worldSize = Vector3.Scale(box.size, transform.lossyScale);
                    Gizmos.DrawWireCube(worldCenter, worldSize);
                    // use half the max dimension as gizmo radius for axes
                    gizmoSize = Mathf.Max(worldSize.x, worldSize.y, worldSize.z) * 0.5f;
                }
                else if (col is SphereCollider sphere)
                {
                    Vector3 worldCenter = transform.TransformPoint(sphere.center);
                    float radius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                    Gizmos.DrawWireSphere(worldCenter, radius);
                    gizmoSize = radius;
                }
                else if (col is CapsuleCollider capsule)
                {
                    // approximate capsule with two spheres and a connecting box
                    Vector3 worldCenter = transform.TransformPoint(capsule.center);
                    int dir = capsule.direction; // 0=x,1=y,2=z
                    Vector3 axis = dir == 0 ? transform.right : (dir == 1 ? transform.up : transform.forward);
                    float radius = capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                    float height = Mathf.Max(0f, capsule.height * transform.lossyScale[dir] - radius * 2f);
                    Vector3 offset = axis.normalized * (height * 0.5f);
                    Gizmos.DrawWireSphere(worldCenter + offset, radius);
                    Gizmos.DrawWireSphere(worldCenter - offset, radius);
                    // draw connecting lines (approx)
                    Gizmos.DrawLine(worldCenter + offset + transform.right * radius, worldCenter - offset + transform.right * radius);
                    Gizmos.DrawLine(worldCenter + offset - transform.right * radius, worldCenter - offset - transform.right * radius);
                    gizmoSize = radius + (height * 0.5f);
                }
                else if (col is MeshCollider meshCol && meshCol.sharedMesh != null)
                {
                    var mesh = meshCol.sharedMesh;
                    Gizmos.DrawWireMesh(mesh, transform.position, transform.rotation, transform.lossyScale);
                    gizmoSize = mesh.bounds.extents.magnitude * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                }
                else
                {
                    // fallback: draw a small sphere
                    Gizmos.DrawSphere(transform.position, gizmoSize);
                }
            }
            else
            {
                // no collider - fallback visual
                Gizmos.DrawSphere(transform.position, gizmoSize);
            }

            // Draw orientation axes for reference
            Gizmos.color = Color.blue;
            Vector3 forward = transform.forward * (gizmoSize * 3f);
            Gizmos.DrawLine(transform.position, transform.position + forward);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.right * gizmoSize);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * gizmoSize);
        }

        private void OnDrawGizmosSelected()
        {
            if (_socketType == null)
                return;

            // Draw more detailed info when selected
            Gizmos.color = _socketType.GizmoColor;
            // Draw more detailed info when selected (label only)
            Gizmos.color = _socketType.GizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.2f * 2f); // Keeping the label

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"{_socketType.DisplayName}\n{(_isOccupied ? "Occupied" : "Available")}" 
            );
            #endif
        }

        private void OnValidate()
        {
            // Editor-time check: ensure a collider exists for physics detection
            Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"Socket '{name}' requires a Collider component for socket detection. Add a Collider to this GameObject or its children.", this);
            }
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
