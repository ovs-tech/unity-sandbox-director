using UnityEngine;

namespace Systems.PlacementSystem.Sockets
{
    /// <summary>
    /// ScriptableObject tag for socket types.
    /// Using ScriptableObjects instead of enums allows for extensibility without recompilation.
    /// New socket types can be created in the editor without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "SocketType", menuName = "Placement System/Socket Type")]
    public class SocketType : ScriptableObject
    {
        [SerializeField, Tooltip("User-friendly name for this socket type")]
        private string _displayName;

        [SerializeField, Tooltip("Color for debugging visualization")]
        private Color _gizmoColor = Color.magenta;

        public string DisplayName => _displayName;
        public Color GizmoColor => _gizmoColor;

        private void OnValidate()
        {
            // Auto-set display name from asset name if empty
            if (string.IsNullOrEmpty(_displayName))
            {
                _displayName = name;
            }
        }
    }
}
