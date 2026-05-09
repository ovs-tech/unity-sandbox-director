using System.Collections.Generic;
using UnityEngine;

namespace Systems.PlacementSystem.Sockets
{
    /// <summary>
    /// Manages socket detection and snapping for the placement system.
    /// Uses spatial queries to find nearby sockets efficiently.
    /// </summary>
    public class SnapManager : MonoBehaviour
    {
        [Header("Socket Detection")]
        [SerializeField, Tooltip("Layer mask for socket detection")]
        private LayerMask _socketLayer = -1;

        [SerializeField, Tooltip("Tag to identify socket GameObjects (optional, leave empty to use layer only)")]
        private string _socketTag = "Socket";

        [SerializeField, Tooltip("Use tag-based detection in addition to layer mask")]
        private bool _useTagDetection = false;

        [Header("Performance")]
        [SerializeField, Tooltip("Maximum number of sockets to check per frame")]
        private int _maxSocketChecksPerFrame = 50;

        [SerializeField, Tooltip("Enable socket caching for better performance. Disable to always search all sockets dynamically.")]
        private bool _enableSocketCaching = true;

        // Cache all sockets in the scene for quick access
        private List<Socket> _registeredSockets = new List<Socket>();
        private bool _socketsCached = false;

        private void Start()
        {
            if (_enableSocketCaching)
            {
                RefreshSocketCache();
            }
        }

        /// <summary>
        /// Finds the nearest available socket of the specified type within range.
        /// Returns null if no suitable socket is found.
        /// </summary>
        public Socket FindNearestSocket(Vector3 position, SocketType requiredType, float maxDistance)
        {
            if (requiredType == null)
                return null;

            // Ensure socket cache is up to date
            if (_enableSocketCaching && !_socketsCached)
                RefreshSocketCache();

            Socket nearestSocket = null;
            float nearestDistance = maxDistance;

            // Use Physics.OverlapSphere for efficient spatial query
            Collider[] nearbyColliders = Physics.OverlapSphere(position, maxDistance, _socketLayer);

            if (nearbyColliders == null || nearbyColliders.Length == 0)
            {
                return null;
            }

            int checkedCount = 0;
            int socketsFound = 0;
            int socketsChecked = 0;

            foreach (var collider in nearbyColliders)
            {
                // Performance limit
                if (checkedCount >= _maxSocketChecksPerFrame)
                {
                    break;
                }

                // Optional tag check
                if (_useTagDetection && !collider.CompareTag(_socketTag))
                {
                    checkedCount++;
                    continue;
                }

                Socket socket = collider.GetComponent<Socket>();
                if (socket == null)
                {
                    socket = collider.GetComponentInParent<Socket>();
                }

                if (socket != null)
                {
                    socketsChecked++;
                    float distance = Vector3.Distance(position, socket.transform.position);
                    bool canAccept = socket.CanAccept(requiredType);

                    if (canAccept)
                    {
                        socketsFound++;
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestSocket = socket;
                        }
                    }
                }

                checkedCount++;
            }
            return nearestSocket;
        }

        /// <summary>
        /// Refreshes the internal cache of all sockets in the scene.
        /// Call this when sockets are added or removed dynamically.
        /// </summary>
        public void RefreshSocketCache()
        {
            _registeredSockets.Clear();

            // Find all Socket components in the scene
            _registeredSockets.AddRange(FindObjectsByType<Socket>());

            _socketsCached = true;
        }

        /// <summary>
        /// Manually registers a socket (useful for dynamically spawned sockets).
        /// Only works when socket caching is enabled.
        /// </summary>
        public void RegisterSocket(Socket socket)
        {
            if (!_enableSocketCaching)
                return;

            if (socket != null && !_registeredSockets.Contains(socket))
            {
                _registeredSockets.Add(socket);
            }
        }

        /// <summary>
        /// Manually unregisters a socket (useful when sockets are destroyed).
        /// Only works when socket caching is enabled.
        /// </summary>
        public void UnregisterSocket(Socket socket)
        {
            if (!_enableSocketCaching)
                return;

            _registeredSockets.Remove(socket);
        }

        /// <summary>
        /// Gets all sockets of a specific type (useful for debugging or UI).
        /// </summary>
        public List<Socket> GetSocketsOfType(SocketType type)
        {
            List<Socket> result = new List<Socket>();

            foreach (var socket in _registeredSockets)
            {
                if (socket != null && socket.SocketType == type)
                {
                    result.Add(socket);
                }
            }

            return result;
        }

        /// <summary>
        /// Clears all occupied sockets (useful for resetting the scene).
        /// </summary>
        public void ClearAllSockets()
        {
            foreach (var socket in _registeredSockets)
            {
                if (socket != null)
                {
                    socket.Clear();
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_socketsCached)
                return;

            // Draw connections between nearby sockets for debugging
            Gizmos.color = Color.gray;
            for (int i = 0; i < _registeredSockets.Count; i++)
            {
                if (_registeredSockets[i] == null)
                    continue;

                // Optional: visualize socket network
                // (Commented out to avoid clutter, enable if needed)
                /*
                for (int j = i + 1; j < _registeredSockets.Count; j++)
                {
                    if (_registeredSockets[j] == null)
                        continue;

                    float distance = Vector3.Distance(
                        _registeredSockets[i].transform.position,
                        _registeredSockets[j].transform.position
                    );

                    if (distance < 2f) // Only show very close sockets
                    {
                        Gizmos.DrawLine(
                            _registeredSockets[i].transform.position,
                            _registeredSockets[j].transform.position
                        );
                    }
                }
                */
            }
        }
    }
}
