using UnityEngine;
using SceneSandbox.Data;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Handles all gizmo rendering for the Scene Sandbox Builder
    /// Extracts gizmo logic from SceneSandboxBuilder to improve maintainability and testability
    /// </summary>
    public class SandboxGizmoRenderer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private SceneSandboxBuilder _builder;
        [SerializeField] private PlacementSystem _placementSystem;
        [SerializeField] private SelectionManager _selectionManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private Transform _stageArea;

        [Header("Gizmo Settings")]
        [SerializeField] private bool _enableGizmos = true;
        [SerializeField] private bool _showBoundsGizmo = true;
        [SerializeField] private bool _showAxesGizmo = true;
        [SerializeField] private bool _showHandlesGizmo = true;
        [SerializeField] private Color _gizmoBoundsColor = Color.yellow;
        [SerializeField] private float _gizmoAxisLength = 1f;

        [Header("Scene Gizmo Settings")]
        [SerializeField] private bool _enableSceneGizmos = true;
        [SerializeField] private bool _showSceneGrid = true;
        [SerializeField] private bool _showSceneBounds = true;
        [SerializeField] private bool _showStageAreaGizmo = true;
        [SerializeField] private bool _showPlacementHeightGizmo = true;
        [SerializeField] private Color _sceneBoundsColor = Color.cyan;
        [SerializeField] private Color _placementHeightColor = Color.yellow;

        [Header("Placement Preview Settings")]
        [SerializeField] private bool _showGridSnapIndicator = true;
        [SerializeField] private bool _showSurfaceNormal = true;
        [SerializeField] private float _surfaceNormalLength = 1f;

        // Scene configuration references
        private Vector3 _sceneBounds;
        private Vector3 _sceneBoundsOffset;
        private PivotPoint _sceneBoundsPivot;
        private Vector3 _gridOffset;
        private PivotPoint _gridPivotOffset;
        private float _gridSize;
        private bool _snapToGrid;
        private bool _enableDropIndicator;
        private Color _validDropColor;
        private Color _invalidDropColor;
        private float _dropIndicatorSize;
        private LayerMask _placementLayers;
        private bool _enableCostSystem;
        private int _placementCost;

        // State
        private GameObject _selectedObject;
        private GameObject _currentDropIndicator;
        private Vector3 _lastValidSurfaceNormal = Vector3.up;

        /// <summary>
        /// Initialize the gizmo renderer with required dependencies
        /// </summary>
        public void Initialize(
            SceneSandboxBuilder builder,
            PlacementSystem placementSystem,
            SelectionManager selectionManager,
            GridManager gridManager,
            Transform stageArea)
        {
            _builder = builder;
            _placementSystem = placementSystem;
            _selectionManager = selectionManager;
            _gridManager = gridManager;
            _stageArea = stageArea;

            if (_builder == null)
            {
                Debug.LogError("[SandboxGizmoRenderer] Builder reference is null!");
            }
            if (_placementSystem == null)
            {
                Debug.LogError("[SandboxGizmoRenderer] PlacementSystem reference is null!");
            }
            if (_gridManager == null)
            {
                Debug.LogError("[SandboxGizmoRenderer] GridManager reference is null!");
            }
        }

        /// <summary>
        /// Update scene configuration from builder
        /// Call this when scene settings change
        /// </summary>
        public void UpdateSceneConfig(
            Vector3 sceneBounds,
            Vector3 sceneBoundsOffset,
            PivotPoint sceneBoundsPivot,
            Vector3 gridOffset,
            PivotPoint gridPivotOffset,
            float gridSize,
            bool snapToGrid,
            bool enableDropIndicator,
            Color validDropColor,
            Color invalidDropColor,
            float dropIndicatorSize,
            LayerMask placementLayers,
            bool enableCostSystem,
            int placementCost)
        {
            _sceneBounds = sceneBounds;
            _sceneBoundsOffset = sceneBoundsOffset;
            _sceneBoundsPivot = sceneBoundsPivot;
            _gridOffset = gridOffset;
            _gridPivotOffset = gridPivotOffset;
            _gridSize = gridSize;
            _snapToGrid = snapToGrid;
            _enableDropIndicator = enableDropIndicator;
            _validDropColor = validDropColor;
            _invalidDropColor = invalidDropColor;
            _dropIndicatorSize = dropIndicatorSize;
            _placementLayers = placementLayers;
            _enableCostSystem = enableCostSystem;
            _placementCost = placementCost;
        }

        /// <summary>
        /// Update selected object reference
        /// </summary>
        public void SetSelectedObject(GameObject selectedObject)
        {
            _selectedObject = selectedObject;
        }

        /// <summary>
        /// Update drop indicator reference
        /// </summary>
        public void SetDropIndicator(GameObject dropIndicator)
        {
            _currentDropIndicator = dropIndicator;
        }

        #region Visibility Control

        /// <summary>
        /// Set visibility of all object gizmos
        /// </summary>
        public void SetGizmoVisibility(bool visible)
        {
            _enableGizmos = visible;
        }

        /// <summary>
        /// Set visibility of all scene gizmos
        /// </summary>
        public void SetSceneGizmoVisibility(bool visible)
        {
            _enableSceneGizmos = visible;
        }

        /// <summary>
        /// Set visibility of scene grid gizmo
        /// </summary>
        public void SetSceneGridVisibility(bool visible)
        {
            _showSceneGrid = visible;
        }

        /// <summary>
        /// Set visibility of scene bounds gizmo
        /// </summary>
        public void SetSceneBoundsVisibility(bool visible)
        {
            _showSceneBounds = visible;
        }

        /// <summary>
        /// Set visibility of stage area gizmo
        /// </summary>
        public void SetStageAreaGizmoVisibility(bool visible)
        {
            _showStageAreaGizmo = visible;
        }

        /// <summary>
        /// Set visibility of placement height gizmo
        /// </summary>
        public void SetPlacementHeightGizmoVisibility(bool visible)
        {
            _showPlacementHeightGizmo = visible;
        }

        /// <summary>
        /// Set scene bounds color
        /// </summary>
        public void SetSceneBoundsColor(Color color)
        {
            _sceneBoundsColor = color;
        }

        #endregion

        #region Gizmo Drawing

        /// <summary>
        /// Draw gizmos for the scene and selected objects
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_enableGizmos && !_enableSceneGizmos) return;

            // Draw scene gizmos
            if (_enableSceneGizmos)
            {
                DrawSceneGizmos();
            }

            // Draw object gizmos
            if (_enableGizmos && _selectedObject != null)
            {
                DrawObjectGizmos(_selectedObject);
            }

            // Draw drop indicator gizmos when dropping
            // Note: Null check needed as OnDrawGizmos can be called before Awake
            if (_enableDropIndicator && _placementSystem != null && _placementSystem.IsActive)
            {
                DrawDropIndicatorGizmos();
            }

            // Draw placement preview gizmos
            // Note: Null check needed as OnDrawGizmos can be called before Awake
            if (_placementSystem != null && _placementSystem.IsActive && _placementSystem.CurrentObject != null)
            {
                DrawPlacementPreviewGizmos();
            }
        }

        /// <summary>
        /// Draw scene-level gizmos (grid, bounds, stage area)
        /// </summary>
        private void DrawSceneGizmos()
        {
            // Draw scene grid
            if (_showSceneGrid)
            {
                DrawSceneGrid();
            }

            // Draw scene bounds
            if (_showSceneBounds)
            {
                DrawSceneBounds();
            }

            // Draw stage area
            if (_showStageAreaGizmo && _stageArea != null)
            {
                DrawStageArea();
            }

            // Draw placement height level
            if (_showPlacementHeightGizmo)
            {
                DrawPlacementHeight();
            }
        }

        /// <summary>
        /// Draw gizmos for a selected object
        /// </summary>
        private void DrawObjectGizmos(GameObject obj)
        {
            if (obj == null) return;

            Bounds bounds = CalculateObjectBounds(obj);

            // Draw bounding box
            if (_showBoundsGizmo)
            {
                Gizmos.color = _gizmoBoundsColor;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            // Draw coordinate axes
            if (_showAxesGizmo)
            {
                DrawObjectAxes(obj.transform, bounds.center);
            }

            // Draw manipulation handles
            if (_showHandlesGizmo)
            {
                DrawManipulationHandles(bounds);
            }
        }

        /// <summary>
        /// Draw the scene grid (2D plane based on scene bounds)
        /// </summary>
        private void DrawSceneGrid()
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;

            // First, align grid with scene bounds (subtract scene bounds pivot)
            // Then, apply grid pivot offset (add grid offset)
            Vector3 sceneBoundsPivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            Vector3 gridPivotOffset = CalculatePivotOffset(_gridPivotOffset, _sceneBounds);

            Vector3 center = stageCenter + _sceneBoundsOffset + _gridOffset - sceneBoundsPivotOffset + gridPivotOffset;

            // Use scene bounds to determine grid extent (X, Z only)
            float gridExtentX = _sceneBounds.x / 2f;
            float gridExtentZ = _sceneBounds.z / 2f;

            int gridLinesX = Mathf.RoundToInt(_sceneBounds.x / _gridSize);
            int gridLinesZ = Mathf.RoundToInt(_sceneBounds.z / _gridSize);

            // Use different color based on snap mode
            Color gridLineColor = _snapToGrid ? new Color(0f, 1f, 0f, 0.5f) : Color.gray;
            Gizmos.color = gridLineColor;

            // Draw vertical lines (along Z-axis)
            for (int i = 0; i <= gridLinesX; i++)
            {
                float x = center.x - gridExtentX + (i * _gridSize);
                Vector3 start = new Vector3(x, center.y, center.z - gridExtentZ);
                Vector3 end = new Vector3(x, center.y, center.z + gridExtentZ);

                // Center line in different color
                if (Mathf.Approximately(x, center.x))
                {
                    Gizmos.color = _snapToGrid ? Color.green : Color.white;
                    Gizmos.DrawLine(start, end);
                    Gizmos.color = gridLineColor;
                }
                else
                {
                    Gizmos.DrawLine(start, end);
                }
            }

            // Draw horizontal lines (along X-axis)
            for (int i = 0; i <= gridLinesZ; i++)
            {
                float z = center.z - gridExtentZ + (i * _gridSize);
                Vector3 start = new Vector3(center.x - gridExtentX, center.y, z);
                Vector3 end = new Vector3(center.x + gridExtentX, center.y, z);

                // Center line in different color
                if (Mathf.Approximately(z, center.z))
                {
                    Gizmos.color = _snapToGrid ? Color.green : Color.white;
                    Gizmos.DrawLine(start, end);
                    Gizmos.color = gridLineColor;
                }
                else
                {
                    Gizmos.DrawLine(start, end);
                }
            }

            // Draw origin marker
            Gizmos.color = _snapToGrid ? Color.green : Color.white;
            Gizmos.DrawWireSphere(center, _snapToGrid ? _gridSize * 0.15f : 0.1f);

            // Draw grid info label when snap is enabled
#if UNITY_EDITOR
            if (_snapToGrid)
            {
                Vector3 labelPos = center + Vector3.up * 0.5f;
                UnityEditor.Handles.Label(
                    labelPos,
                    $"Snap Grid: {_gridSize}m\nOffset: {_gridOffset}\nPivot: {_gridPivotOffset}",
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.green },
                        fontSize = 10,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    }
                );
            }
#endif
        }

        /// <summary>
        /// Draw scene boundaries
        /// </summary>
        private void DrawSceneBounds()
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;
            Vector3 pivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            // We SUBTRACT the pivot offset to position the bounds relative to the pivot point
            Vector3 center = stageCenter + _sceneBoundsOffset - pivotOffset;
            Vector3 size = _sceneBounds; // Use configurable scene bounds

            Gizmos.color = _sceneBoundsColor;
            Gizmos.DrawWireCube(center, size);

            // Draw corner markers
            float markerSize = 0.2f;
            Vector3[] corners = {
                center + new Vector3(-size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, -size.y/2, size.z/2),
                center + new Vector3(size.x/2, -size.y/2, size.z/2)
            };

            foreach (var corner in corners)
            {
                Gizmos.DrawWireCube(corner, Vector3.one * markerSize);
            }
        }

        /// <summary>
        /// Draw stage area gizmo
        /// </summary>
        private void DrawStageArea()
        {
            if (_stageArea == null) return;

            Vector3 stagePos = _stageArea.position;
            Vector3 stageScale = _stageArea.localScale;

            // Draw stage area bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(stagePos, stageScale);

            // Draw floor
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 floorPos = new Vector3(stagePos.x, stagePos.y - stageScale.y / 2f, stagePos.z);
            Vector3 floorSize = new Vector3(stageScale.x, 0.01f, stageScale.z);
            Gizmos.DrawCube(floorPos, floorSize);
        }

        /// <summary>
        /// Draw default placement height level
        /// </summary>
        private void DrawPlacementHeight()
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;

            // First, align with scene bounds (subtract scene bounds pivot)
            // Then, apply grid pivot offset (add grid offset)
            Vector3 sceneBoundsPivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            Vector3 gridPivotOffset = CalculatePivotOffset(_gridPivotOffset, _sceneBounds);

            Vector3 center = stageCenter + _sceneBoundsOffset + _gridOffset - sceneBoundsPivotOffset + gridPivotOffset;

            // Get placement height from builder
            float defaultPlacementHeight = 0f;
            if (_builder != null)
            {
                // Access via reflection or add public property
                defaultPlacementHeight = 0f; // Will be set from builder config
            }

            // Calculate placement height relative to grid
            float placementHeight = center.y + defaultPlacementHeight;

            // Use scene bounds to determine placement height grid extent
            float gridExtentX = _sceneBounds.x / 2f;
            float gridExtentZ = _sceneBounds.z / 2f;

            int gridLinesX = Mathf.Max(2, Mathf.RoundToInt(_sceneBounds.x / _gridSize));
            int gridLinesZ = Mathf.Max(2, Mathf.RoundToInt(_sceneBounds.z / _gridSize));

            // Draw placement height plane
            Gizmos.color = _placementHeightColor;

            // Draw horizontal grid lines at placement height
            for (int i = 0; i <= gridLinesX; i++)
            {
                float x = center.x - gridExtentX + (i * _gridSize);
                Vector3 start = new Vector3(x, placementHeight, center.z - gridExtentZ);
                Vector3 end = new Vector3(x, placementHeight, center.z + gridExtentZ);
                Gizmos.DrawLine(start, end);
            }

            for (int i = 0; i <= gridLinesZ; i++)
            {
                float z = center.z - gridExtentZ + (i * _gridSize);
                Vector3 start = new Vector3(center.x - gridExtentX, placementHeight, z);
                Vector3 end = new Vector3(center.x + gridExtentX, placementHeight, z);
                Gizmos.DrawLine(start, end);
            }

            // Draw placement height indicator at center
            Vector3 placementPos = new Vector3(center.x, placementHeight, center.z);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(placementPos, 0.2f);

            // Draw vertical line from ground to placement height
            Vector3 groundPos = new Vector3(center.x, center.y, center.z);
            Gizmos.color = _placementHeightColor;
            Gizmos.DrawLine(groundPos, placementPos);

            // Draw height text indicator (visual marker)
            Vector3[] heightMarkers = {
                placementPos + Vector3.right * 2f,
                placementPos + Vector3.left * 2f,
                placementPos + Vector3.forward * 2f,
                placementPos + Vector3.back * 2f
            };

            foreach (var marker in heightMarkers)
            {
                Gizmos.DrawWireCube(marker, Vector3.one * 0.1f);
            }
        }

        /// <summary>
        /// Draw coordinate axes for an object
        /// </summary>
        private void DrawObjectAxes(Transform objTransform, Vector3 center)
        {
            // X axis (Red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, center + objTransform.right * _gizmoAxisLength);

            // Y axis (Green)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + objTransform.up * _gizmoAxisLength);

            // Z axis (Blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(center, center + objTransform.forward * _gizmoAxisLength);
        }

        /// <summary>
        /// Draw manipulation handles
        /// </summary>
        private void DrawManipulationHandles(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            float handleSize = 0.1f;

            Gizmos.color = Color.white;

            // Corner handles
            Vector3[] corners = {
                center + new Vector3(-size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, -size.y/2, size.z/2),
                center + new Vector3(size.x/2, -size.y/2, size.z/2),
                center + new Vector3(-size.x/2, size.y/2, -size.z/2),
                center + new Vector3(size.x/2, size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, size.y/2, size.z/2),
                center + new Vector3(size.x/2, size.y/2, size.z/2)
            };

            foreach (var corner in corners)
            {
                Gizmos.DrawWireCube(corner, Vector3.one * handleSize);
            }

            // Face center handles
            Gizmos.color = _gizmoBoundsColor;
            Vector3[] faces = {
                center + new Vector3(-size.x/2, 0, 0), // Left
                center + new Vector3(size.x/2, 0, 0),  // Right
                center + new Vector3(0, -size.y/2, 0), // Bottom
                center + new Vector3(0, size.y/2, 0),  // Top
                center + new Vector3(0, 0, -size.z/2), // Back
                center + new Vector3(0, 0, size.z/2)   // Front
            };

            foreach (var face in faces)
            {
                Gizmos.DrawWireCube(face, Vector3.one * handleSize * 0.7f);
            }
        }

        /// <summary>
        /// Calculate bounds for an object
        /// </summary>
        private Bounds CalculateObjectBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();

            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return bounds;
            }
            else if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
                return bounds;
            }
            else
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }
        }

        /// <summary>
        /// Draw drop indicator gizmos during drag operations
        /// </summary>
        private void DrawDropIndicatorGizmos()
        {
            if (!_placementSystem.IsActive) return;

            Vector3 position = _currentDropIndicator?.transform.position ?? Vector3.zero;
            bool isValidPosition = IsPositionInSceneBounds(position);

            // Draw drop zone circle
            Gizmos.color = isValidPosition ? _validDropColor : _invalidDropColor;

            // Draw a circle on the ground
            float radius = _dropIndicatorSize * 0.5f;
            int segments = 32;
            float angleStep = 360f / segments;

            Vector3 prevPoint = position + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }

            // Draw vertical indicator line
            Gizmos.DrawLine(position, position + Vector3.up * 2f);

            // Draw grid snap indicator if snapping is enabled
            if (_snapToGrid)
            {
                // Create temporary TransformableItem for snap logic
                GameObject tempObject = new GameObject("TempSnapHelper");
                var tempDraggable = tempObject.AddComponent<TransformableItem>();
                GridInfo gridInfo = new GridInfo(_snapToGrid, _gridSize, _gridOffset);
                Vector3 snappedPos = tempDraggable.GetSnappedPositionValue(position, gridInfo);
                DestroyImmediate(tempObject);

                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(snappedPos + Vector3.up * 0.05f, Vector3.one * _gridSize * 0.1f);
            }
        }

        /// <summary>
        /// Draw placement preview gizmos (ghost object indicators)
        /// </summary>
        private void DrawPlacementPreviewGizmos()
        {
            GameObject currentPlacementObject = _placementSystem.CurrentObject;
            if (currentPlacementObject == null) return;
            Vector3 position = currentPlacementObject.transform.position;
            Bounds bounds = GetObjectBounds(currentPlacementObject);

            // Draw validity indicator with color
            bool isValid = _placementSystem.IsValidPlacementCurrentPosition;
            Gizmos.color = isValid ? Color.green : Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Draw grid snap indicator if enabled
            if (_showGridSnapIndicator && _snapToGrid)
            {
                // Get TransformableItem if available for snap logic
                var transformableItem = currentPlacementObject.GetComponent<TransformableItem>();
                GridInfo gridInfo = new GridInfo(_snapToGrid, _gridSize, _gridOffset);
                Vector3 snappedPos = transformableItem != null
                    ? transformableItem.GetSnappedPositionValue(position, gridInfo)
                    : position;

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(snappedPos + Vector3.up * 0.05f, Vector3.one * _gridSize * 0.15f);

                // Draw line from ghost to snap point
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawLine(position, snappedPos);
            }

            // Draw surface normal indicator if enabled
            if (_showSurfaceNormal)
            {
                // Raycast down to find surface
                if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 10f, _placementLayers))
                {
                    _lastValidSurfaceNormal = hit.normal;

                    // Draw normal vector
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(hit.point, hit.point + hit.normal * _surfaceNormalLength);

                    // Draw surface point
                    Gizmos.DrawWireSphere(hit.point, 0.1f);
                }
                else if (_lastValidSurfaceNormal != Vector3.zero)
                {
                    // Draw last known normal if no surface hit
                    Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
                    Gizmos.DrawLine(position, position + _lastValidSurfaceNormal * _surfaceNormalLength);
                }
            }

            // Draw cost indicator (if enabled)
            if (_enableCostSystem)
            {
                Gizmos.color = isValid ? Color.green : Color.red;
                Vector3 labelPos = bounds.center + Vector3.up * (bounds.extents.y + 0.5f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(labelPos, $"Cost: {_placementCost}");
#endif
            }

            // Draw validation feedback
            if (!isValid)
            {
                // Draw X indicator for invalid placement
                Gizmos.color = Color.red;
                Vector3 center = bounds.center;
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f;

                Gizmos.DrawLine(center + new Vector3(-size, size, 0), center + new Vector3(size, -size, 0));
                Gizmos.DrawLine(center + new Vector3(-size, -size, 0), center + new Vector3(size, size, 0));
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculate pivot offset based on pivot point
        /// </summary>
        private Vector3 CalculatePivotOffset(PivotPoint pivot, Vector3 size)
        {
            float halfX = size.x / 2f;
            float halfY = size.y / 2f;
            float halfZ = size.z / 2f;

            switch (pivot)
            {
                case PivotPoint.Center:
                    return Vector3.zero;
                case PivotPoint.FrontCenter:
                    return new Vector3(0, 0, -halfZ);
                case PivotPoint.BackCenter:
                    return new Vector3(0, 0, halfZ);
                case PivotPoint.LeftCenter:
                    return new Vector3(-halfX, 0, 0);
                case PivotPoint.RightCenter:
                    return new Vector3(halfX, 0, 0);
                case PivotPoint.TopCenter:
                    return new Vector3(0, halfY, 0);
                case PivotPoint.BottomCenter:
                    return new Vector3(0, -halfY, 0);
                case PivotPoint.BottomFrontEdge:
                    return new Vector3(0, -halfY, -halfZ);
                case PivotPoint.BottomBackEdge:
                    return new Vector3(0, -halfY, halfZ);
                case PivotPoint.BottomLeftEdge:
                    return new Vector3(-halfX, -halfY, 0);
                case PivotPoint.BottomRightEdge:
                    return new Vector3(halfX, -halfY, 0);
                case PivotPoint.TopFrontEdge:
                    return new Vector3(0, halfY, -halfZ);
                case PivotPoint.TopBackEdge:
                    return new Vector3(0, halfY, halfZ);
                case PivotPoint.TopLeftEdge:
                    return new Vector3(-halfX, halfY, 0);
                case PivotPoint.TopRightEdge:
                    return new Vector3(halfX, halfY, 0);
                case PivotPoint.FrontLeftEdge:
                    return new Vector3(-halfX, 0, -halfZ);
                case PivotPoint.FrontRightEdge:
                    return new Vector3(halfX, 0, -halfZ);
                case PivotPoint.BackLeftEdge:
                    return new Vector3(-halfX, 0, halfZ);
                case PivotPoint.BackRightEdge:
                    return new Vector3(halfX, 0, halfZ);
                case PivotPoint.BottomFrontLeft:
                    return new Vector3(-halfX, -halfY, -halfZ);
                case PivotPoint.BottomFrontRight:
                    return new Vector3(halfX, -halfY, -halfZ);
                case PivotPoint.BottomBackLeft:
                    return new Vector3(-halfX, -halfY, halfZ);
                case PivotPoint.BottomBackRight:
                    return new Vector3(halfX, -halfY, halfZ);
                case PivotPoint.TopFrontLeft:
                    return new Vector3(-halfX, halfY, -halfZ);
                case PivotPoint.TopFrontRight:
                    return new Vector3(halfX, halfY, -halfZ);
                case PivotPoint.TopBackLeft:
                    return new Vector3(-halfX, halfY, halfZ);
                case PivotPoint.TopBackRight:
                    return new Vector3(halfX, halfY, halfZ);
                default:
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// Check if a position is within the scene bounds
        /// </summary>
        private bool IsPositionInSceneBounds(Vector3 position)
        {
            Vector3 stageCenter = _stageArea != null ? _stageArea.position : transform.position;
            Vector3 pivotOffset = CalculatePivotOffset(_sceneBoundsPivot, _sceneBounds);
            Vector3 boundsCenter = stageCenter + _sceneBoundsOffset - pivotOffset;
            Vector3 localPos = position - boundsCenter;

            bool withinX = Mathf.Abs(localPos.x) <= _sceneBounds.x / 2f;
            bool withinY = Mathf.Abs(localPos.y) <= _sceneBounds.y / 2f;
            bool withinZ = Mathf.Abs(localPos.z) <= _sceneBounds.z / 2f;

            return withinX && withinY && withinZ;
        }

        /// <summary>
        /// Get object bounds (helper for placement preview)
        /// </summary>
        private Bounds GetObjectBounds(GameObject obj)
        {
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            var renderers = obj.GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        #endregion
    }
}
