using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.Strategies
{
    /// <summary>
    /// Hexagonal grid placement strategy.
    /// Snaps objects to a hex grid pattern, useful for strategy games or specific building layouts.
    /// </summary>
    public class HexPlacementStrategy : MonoBehaviour, IPlacementStrategy
    {
        [Header("Hex Grid Settings")]
        [SerializeField, Tooltip("Size of each hexagon (distance from center to corner)")]
        private float _hexSize = 1f;

        [SerializeField, Tooltip("Grid origin offset")]
        private Vector3 _gridOrigin = Vector3.zero;

        [SerializeField, Tooltip("Hex orientation (flat-top vs pointy-top)")]
        private HexOrientation _orientation = HexOrientation.FlatTop;

        [Header("Visualization")]
        [SerializeField, Tooltip("Show hex grid in scene view")]
        private bool _showGridGizmo = true;

        [SerializeField, Tooltip("Grid visualization radius (in hex cells)")]
        private int _gridVisualizationRadius = 5;

        public enum HexOrientation
        {
            FlatTop,
            PointyTop
        }

        public Vector3 CalculatePosition(Vector3 rawWorldPos, GameObject ghostObject)
        {
            Vector3 relativePos = rawWorldPos - _gridOrigin;

            // Convert to hex coordinates
            Vector2Int hexCoord = WorldToHex(new Vector2(relativePos.x, relativePos.z));

            // Convert back to world position (snapped)
            Vector2 snappedPos = HexToWorld(hexCoord);

            return new Vector3(snappedPos.x, rawWorldPos.y, snappedPos.y) + _gridOrigin;
        }

        public Quaternion CalculateRotation(Quaternion currentRotation)
        {
            // Snap to 60-degree increments for hex grids
            Vector3 euler = currentRotation.eulerAngles;
            float snappedY = Mathf.Round(euler.y / 60f) * 60f;
            return Quaternion.Euler(0, snappedY, 0);
        }

        private Vector2Int WorldToHex(Vector2 worldPos)
        {
            float q, r;

            if (_orientation == HexOrientation.FlatTop)
            {
                q = (worldPos.x * Mathf.Sqrt(3f) / 3f - worldPos.y / 3f) / _hexSize;
                r = (worldPos.y * 2f / 3f) / _hexSize;
            }
            else // PointyTop
            {
                q = (worldPos.x * 2f / 3f) / _hexSize;
                r = (-worldPos.x / 3f + Mathf.Sqrt(3f) / 3f * worldPos.y) / _hexSize;
            }

            return HexRound(q, r);
        }

        private Vector2 HexToWorld(Vector2Int hexCoord)
        {
            float x, z;

            if (_orientation == HexOrientation.FlatTop)
            {
                x = _hexSize * (Mathf.Sqrt(3f) * hexCoord.x + Mathf.Sqrt(3f) / 2f * hexCoord.y);
                z = _hexSize * (3f / 2f * hexCoord.y);
            }
            else // PointyTop
            {
                x = _hexSize * (3f / 2f * hexCoord.x);
                z = _hexSize * (Mathf.Sqrt(3f) / 2f * hexCoord.x + Mathf.Sqrt(3f) * hexCoord.y);
            }

            return new Vector2(x, z);
        }

        private Vector2Int HexRound(float q, float r)
        {
            float s = -q - r;

            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);

            float q_diff = Mathf.Abs(rq - q);
            float r_diff = Mathf.Abs(rr - r);
            float s_diff = Mathf.Abs(rs - s);

            if (q_diff > r_diff && q_diff > s_diff)
            {
                rq = -rr - rs;
            }
            else if (r_diff > s_diff)
            {
                rr = -rq - rs;
            }

            return new Vector2Int(rq, rr);
        }

        private void OnDrawGizmos()
        {
            if (!_showGridGizmo)
                return;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);

            // Draw hex grid
            for (int q = -_gridVisualizationRadius; q <= _gridVisualizationRadius; q++)
            {
                int r1 = Mathf.Max(-_gridVisualizationRadius, -q - _gridVisualizationRadius);
                int r2 = Mathf.Min(_gridVisualizationRadius, -q + _gridVisualizationRadius);

                for (int r = r1; r <= r2; r++)
                {
                    DrawHexagon(new Vector2Int(q, r));
                }
            }

            // Draw origin marker
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_gridOrigin, 0.1f);
        }

        private void DrawHexagon(Vector2Int hexCoord)
        {
            Vector2 center = HexToWorld(hexCoord);
            Vector3 worldCenter = new Vector3(center.x, 0, center.y) + _gridOrigin;

            // Draw hex corners
            for (int i = 0; i < 6; i++)
            {
                float angle1 = 60f * i * Mathf.Deg2Rad;
                float angle2 = 60f * (i + 1) * Mathf.Deg2Rad;

                if (_orientation == HexOrientation.PointyTop)
                {
                    angle1 += 30f * Mathf.Deg2Rad;
                    angle2 += 30f * Mathf.Deg2Rad;
                }

                Vector3 corner1 = worldCenter + new Vector3(
                    _hexSize * Mathf.Cos(angle1),
                    0,
                    _hexSize * Mathf.Sin(angle1)
                );

                Vector3 corner2 = worldCenter + new Vector3(
                    _hexSize * Mathf.Cos(angle2),
                    0,
                    _hexSize * Mathf.Sin(angle2)
                );

                Gizmos.DrawLine(corner1, corner2);
            }
        }
    }
}
