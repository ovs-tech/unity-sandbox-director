using UnityEngine;

namespace SceneSandbox.Core
{
    /// Minimal, non-breaking grid manager.
    /// Computes snapped positions and rotations based on grid size and mode.
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField] private Vector3 _offset = Vector3.zero;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public float CellSize
        {
            get => _cellSize;
            set => _cellSize = Mathf.Max(0.0001f, value);
        }

        public Vector3 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        public Vector3 GetSnappedPosition(Vector3 worldPosition)
        {
            if (!_enabled) return worldPosition;

            var p = worldPosition - _offset;
            p.x = Mathf.Round(p.x / _cellSize) * _cellSize;
            p.y = Mathf.Round(p.y / _cellSize) * _cellSize;
            p.z = Mathf.Round(p.z / _cellSize) * _cellSize;
            return p + _offset;
        }

        public Quaternion GetSnappedRotation(Quaternion rotation, float angleIncrementDegrees = 15f)
        {
            if (!_enabled) return rotation;
            var e = rotation.eulerAngles;
            e.x = Mathf.Round(e.x / angleIncrementDegrees) * angleIncrementDegrees;
            e.y = Mathf.Round(e.y / angleIncrementDegrees) * angleIncrementDegrees;
            e.z = Mathf.Round(e.z / angleIncrementDegrees) * angleIncrementDegrees;
            return Quaternion.Euler(e);
        }
    }
}
