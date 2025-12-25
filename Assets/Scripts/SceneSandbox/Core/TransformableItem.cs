using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Systems.SceneSandbox.Core
{
    /// <summary>
    /// Grid information for snapping objects to a grid
    /// </summary>
    [System.Serializable]
    public struct GridInfo
    {
        public bool enableSnap;
        public float gridSize;
        public Vector3 gridOffset;

        public GridInfo(bool enableSnap, float gridSize, Vector3 gridOffset = default)
        {
            this.enableSnap = enableSnap;
            this.gridSize = gridSize;
            this.gridOffset = gridOffset;
        }
    }

    /// <summary>
    /// Simplified component for objects in the scene sandbox.
    /// Handles selection, visual feedback, and integration with SelectionManager.
    /// Drag & drop placement is handled by SceneSandboxBuilder's ghost preview system.
    /// </summary>
    public class TransformableItem : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Material _hoverMaterial;
        [SerializeField] private Material _selectedMaterial;

        [Header("Transform Control Settings")]
        [SerializeField] private bool _enableTransformControls = true;
        [SerializeField] private Vector3 _minScale = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 _maxScale = new Vector3(5f, 5f, 5f);
        
        [Header("Transform Step Settings")]
        [SerializeField] private float _positionStep = 0.1f; // Position units per step
        [SerializeField] private float _rotationStep = 5f; // Degrees per step
        [SerializeField] private float _scaleStep = 0.1f; // Scale units per step

        [Header("Gizmo Settings")]
        [SerializeField] private bool _showGizmo = true;
        [SerializeField] private float _gizmoScreenScale = 0.12f;
        [SerializeField] private LayerMask _gizmoLayer = 1 << 2; // Default to layer 2
        [SerializeField] private Color _gizmoXColor = new Color(1, 0.2f, 0.2f);
        [SerializeField] private Color _gizmoYColor = new Color(0.2f, 1, 0.2f);
        [SerializeField] private Color _gizmoZColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _gizmoHighlightColor = Color.yellow;
        
        [Header("Snap Grid Settings")]
        [SerializeField] private SnapMode _snapMode = SnapMode.Extend;
        [SerializeField] private bool _enableSnapGrid = false;
        [SerializeField] private float _snapGridSize = 0.5f;

        [Header("Placement Settings")]
        [SerializeField] private PivotPoint _pivotPoint = PivotPoint.Center; // Pivot point for placement
        [SerializeField] private Vector3 _placementOffset = Vector3.zero; // Offset applied during placement

        // Transform control state
        [SerializeField] private TransformModeType _currentTransformModeType = TransformModeType.None;
        [SerializeField] private TransformAxis _currentTransformAxis = TransformAxis.All;
        private Vector3 _originalScale;
        private Vector3 _originalRotation;
        private Vector3 _originalPosition;
        private bool _isInTransformModeType = false;

        // Gizmo state
        private Transform _gizmoRoot;
        private GizmoAxisHandle _xHandle, _yHandle, _zHandle;
        private bool _gizmoInitialized = false;

        [Header("Object Data")]
        [SerializeField] private string _objectId;
        [SerializeField] private string _objectDataId;

        // Components
        private Renderer[] _renderers;
        private Material[] _originalMaterials;
        private Collider _collider;

        // State
        private bool _isSelected;
        private Camera _camera;

        // Events
        public System.Action<TransformableItem, bool> OnSelectionChanged;
        public System.Action<TransformableItem, TransformModeType> OnTransformModeTypeChanged;

        [Header("Unity Events")]
        [SerializeField] private UnityEvent _onSelectItem = new UnityEvent();
        [SerializeField] private UnityEvent _onDeselectItem = new UnityEvent();

        // Public accessors for Unity Events
        public UnityEvent OnSelectItem => _onSelectItem;
        public UnityEvent OnDeselectItem => _onDeselectItem;


        // Properties
        public string ObjectId
        {
            get => _objectId;
            set => _objectId = value;
        }

        public string ObjectDataId
        {
            get => _objectDataId;
            set => _objectDataId = value;
        }

        public bool IsSelected => _isSelected;
        public TransformModeType CurrentTransformModeType => _currentTransformModeType;
        public TransformAxis CurrentTransformAxis => _currentTransformAxis;
        public bool IsInTransformModeType => _isInTransformModeType;
        public bool EnableTransformControls
        {
            get => _enableTransformControls;
            set => _enableTransformControls = value;
        }
        
        public SnapMode SnapMode
        {
            get => _snapMode;
            set => _snapMode = value;
        }
        
        public bool EnableSnapGrid
        {
            get => _enableSnapGrid;
            set => _enableSnapGrid = value;
        }
        
        public float SnapGridSize
        {
            get => _snapGridSize;
            set => _snapGridSize = value;
        }

        public PivotPoint PivotPoint
        {
            get => _pivotPoint;
            set => _pivotPoint = value;
        }

        public Vector3 PlacementOffset
        {
            get => _placementOffset;
            set => _placementOffset = value;
        }

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            _camera = Camera.main ?? FindFirstObjectByType<Camera>();

            if (string.IsNullOrEmpty(_objectId))
            {
                _objectId = System.Guid.NewGuid().ToString();
                Debug.LogWarning($"TransformableItem on '{gameObject.name}' had no ObjectId. Generated new ID: {_objectId}");
            }
            
            // Initialize gizmo if transform controls are enabled
            if (_enableTransformControls && _showGizmo)
            {
                InitializeGizmo();
            }
        }

        private void OnDisable()
        {
            // Clean up gizmo when disabled
            DestroyGizmo();
        }

        private void OnDestroy()
        {
            // Clean up gizmo
            DestroyGizmo();
        }

        private void Update()
        {
            UpdateGizmo();
        }

        private void InitializeComponents()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _collider = GetComponent<Collider>();

            // Store original materials
            if (_renderers != null && _renderers.Length > 0)
            {
                _originalMaterials = new Material[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _originalMaterials[i] = _renderers[i].material;
                }
            }

            // Ensure we have a collider for interaction
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }
        }

        #region Visual Feedback

        /// <summary>
        /// Set hover visual feedback
        /// </summary>
        public void SetHoverState(bool isHovered)
        {
            if (isHovered && _hoverMaterial != null)
            {
                SetMaterial(_hoverMaterial);
            }
            else
            {
                RestoreOriginalMaterial();
            }
        }

        /// <summary>
        /// Set selection visual feedback
        /// </summary>
        public void SetSelectedState(bool isSelected)
        {
            // Store the selection state
            _isSelected = isSelected;

            // Invoke Unity Events
            if (isSelected)
            {
                _onSelectItem?.Invoke();
            }
            else
            {
                _onDeselectItem?.Invoke();
            }

            // Don't override transform mode visuals
            if (_isInTransformModeType && _currentTransformModeType != TransformModeType.None)
            {
                UpdateTransformVisuals();
                return;
            }

            // Update visual material feedback for selection
            if (isSelected)
            {
                if (_selectedMaterial != null)
                {
                    SetMaterial(_selectedMaterial);
                }
                else if (_hoverMaterial != null)
                {
                    SetMaterial(_hoverMaterial);
                }
            }
            else
            {
                RestoreOriginalMaterial();
            }
        }

        private void SetMaterial(Material material)
        {
            if (_renderers == null) return;

            foreach (var renderer in _renderers)
            {
                renderer.material = material;
            }
        }

        private void RestoreOriginalMaterial()
        {
            if (_renderers == null || _originalMaterials == null) return;

            // If in transform mode, show transform mode material instead
            if (_isInTransformModeType && _currentTransformModeType != TransformModeType.None)
            {
                UpdateTransformVisuals();
                return;
            }

            // If the item is selected, show selection material instead of original
            if (_isSelected)
            {
                if (_selectedMaterial != null)
                {
                    SetMaterial(_selectedMaterial);
                }
                else if (_hoverMaterial != null)
                {
                    SetMaterial(_hoverMaterial);
                }
                return;
            }

            // Restore original materials
            for (int i = 0; i < _renderers.Length && i < _originalMaterials.Length; i++)
            {
                _renderers[i].material = _originalMaterials[i];
            }
        }

        /// <summary>
        /// Set the object data reference
        /// </summary>
        public void SetObjectData(string objectDataId, string objectId = null)
        {
            _objectDataId = objectDataId;

            if (!string.IsNullOrEmpty(objectId))
            {
                _objectId = objectId;
            }
        }

        #endregion

        #region Transform Control Methods

        /// <summary>
        /// Set the current transform mode
        /// </summary>
        public void SetTransformModeType(TransformModeType mode)
        {
            TransformModeType previousMode = _currentTransformModeType;
            _currentTransformModeType = mode;
            _isInTransformModeType = mode != TransformModeType.None;

            // Reset axis to All when changing to Position mode or None
            if (mode == TransformModeType.Position || mode == TransformModeType.None)
            {
                _currentTransformAxis = TransformAxis.All;
            }

            // Update visual feedback
            UpdateTransformVisuals();
            
            // Update gizmo visuals
            UpdateGizmoVisuals();

            // Store original values when entering transform mode
            if (mode != TransformModeType.None && previousMode == TransformModeType.None)
            {
                _originalPosition = transform.position;
                _originalScale = transform.localScale;
                _originalRotation = transform.eulerAngles;
            }

            // Restore original material when exiting transform mode
            if (mode == TransformModeType.None && previousMode != TransformModeType.None)
            {
                RestoreOriginalMaterial();
            }

            OnTransformModeTypeChanged?.Invoke(this, mode);
        }

        /// <summary>
        /// Set the current transform axis (X, Y, Z, All)
        /// Only applicable for Rotation and Scale modes
        /// </summary>
        public void SetTransformAxis(TransformAxis axis)
        {
            // Only allow axis change for Position, Rotation and Scale modes
            if (_currentTransformModeType == TransformModeType.None)
            {
                _currentTransformAxis = TransformAxis.All;
                return;
            }

            _currentTransformAxis = axis;
            
            // Update gizmo to highlight selected axis
            UpdateGizmoVisuals();
        }

        /// <summary>
        /// Update visual feedback based on current transform mode
        /// </summary>
        private void UpdateTransformVisuals()
        {
            switch (_currentTransformModeType)
            {
                case TransformModeType.None:
                    RestoreOriginalMaterial();
                    break;
                case TransformModeType.Position:
                case TransformModeType.Rotation:
                case TransformModeType.Scale:
                    // Use hover material or selected material for transform mode visual feedback
                    if (_selectedMaterial != null)
                        SetMaterial(_selectedMaterial);
                    else if (_hoverMaterial != null)
                        SetMaterial(_hoverMaterial);
                    break;
            }
        }

        /// <summary>
        /// Reset transform to original values
        /// </summary>
        public void ResetTransform()
        {
            if (_originalPosition != Vector3.zero)
                transform.position = _originalPosition;
            
            if (_originalScale != Vector3.zero)
                transform.localScale = _originalScale;
            
            if (_originalRotation != Vector3.zero)
                transform.eulerAngles = _originalRotation;
            
            // Reset transform for item
        }

        /// <summary>
        /// Convert PivotPoint enum to local Vector3 offset based on object bounds
        /// </summary>
        /// <param name="pivotPoint">Pivot point enum</param>
        /// <returns>Local space offset for the pivot point</returns>
        private Vector3 GetPivotPointOffset(PivotPoint pivotPoint)
        {
            // Calculate bounds in LOCAL SPACE to handle rotated objects correctly
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            // Get local bounds from renderers
            if (_renderers != null && _renderers.Length > 0)
            {
                foreach (var renderer in _renderers)
                {
                    if (renderer != null)
                    {
                        // Get mesh bounds in local space
                        if (renderer is MeshRenderer meshRenderer)
                        {
                            var meshFilter = renderer.GetComponent<MeshFilter>();
                            if (meshFilter != null && meshFilter.sharedMesh != null)
                            {
                                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                                
                                // Transform mesh bounds to this object's local space
                                Vector3 meshLocalCenter = transform.InverseTransformPoint(
                                    renderer.transform.TransformPoint(meshBounds.center)
                                );
                                Vector3 meshLocalSize = new Vector3(
                                    meshBounds.size.x * renderer.transform.lossyScale.x / transform.lossyScale.x,
                                    meshBounds.size.y * renderer.transform.lossyScale.y / transform.lossyScale.y,
                                    meshBounds.size.z * renderer.transform.lossyScale.z / transform.lossyScale.z
                                );
                                
                                Bounds transformedBounds = new Bounds(meshLocalCenter, meshLocalSize);
                                
                                if (!hasBounds)
                                {
                                    localBounds = transformedBounds;
                                    hasBounds = true;
                                }
                                else
                                {
                                    localBounds.Encapsulate(transformedBounds);
                                }
                            }
                        }
                        else if (renderer is SkinnedMeshRenderer skinnedRenderer)
                        {
                            // For skinned mesh, use local bounds
                            if (!hasBounds)
                            {
                                localBounds = skinnedRenderer.localBounds;
                                hasBounds = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(skinnedRenderer.localBounds);
                            }
                        }
                    }
                }
            }

            // If no bounds from renderers, try collider
            if (!hasBounds && _collider != null)
            {
                if (_collider is BoxCollider boxCollider)
                {
                    localBounds = new Bounds(boxCollider.center, boxCollider.size);
                    hasBounds = true;
                }
                else if (_collider is SphereCollider sphereCollider)
                {
                    float diameter = sphereCollider.radius * 2f;
                    localBounds = new Bounds(sphereCollider.center, new Vector3(diameter, diameter, diameter));
                    hasBounds = true;
                }
                else if (_collider is CapsuleCollider capsuleCollider)
                {
                    float diameter = capsuleCollider.radius * 2f;
                    localBounds = new Bounds(capsuleCollider.center, new Vector3(diameter, capsuleCollider.height, diameter));
                    hasBounds = true;
                }
            }

            // If still no bounds, return zero
            if (!hasBounds)
            {
                return Vector3.zero;
            }

            // Get local min, max, and center
            Vector3 localMin = localBounds.min;
            Vector3 localMax = localBounds.max;
            Vector3 localCenter = localBounds.center;

            // Calculate pivot offset based on enum
            switch (pivotPoint)
            {
                // Center
                case PivotPoint.Center:
                    return localCenter;

                // Face Centers
                case PivotPoint.FrontCenter:
                    return new Vector3(localCenter.x, localCenter.y, localMax.z);
                case PivotPoint.BackCenter:
                    return new Vector3(localCenter.x, localCenter.y, localMin.z);
                case PivotPoint.LeftCenter:
                    return new Vector3(localMin.x, localCenter.y, localCenter.z);
                case PivotPoint.RightCenter:
                    return new Vector3(localMax.x, localCenter.y, localCenter.z);
                case PivotPoint.TopCenter:
                    return new Vector3(localCenter.x, localMax.y, localCenter.z);
                case PivotPoint.BottomCenter:
                    return new Vector3(localCenter.x, localMin.y, localCenter.z);

                // Bottom Edge Centers
                case PivotPoint.BottomFrontEdge:
                    return new Vector3(localCenter.x, localMin.y, localMax.z);
                case PivotPoint.BottomBackEdge:
                    return new Vector3(localCenter.x, localMin.y, localMin.z);
                case PivotPoint.BottomLeftEdge:
                    return new Vector3(localMin.x, localMin.y, localCenter.z);
                case PivotPoint.BottomRightEdge:
                    return new Vector3(localMax.x, localMin.y, localCenter.z);

                // Top Edge Centers
                case PivotPoint.TopFrontEdge:
                    return new Vector3(localCenter.x, localMax.y, localMax.z);
                case PivotPoint.TopBackEdge:
                    return new Vector3(localCenter.x, localMax.y, localMin.z);
                case PivotPoint.TopLeftEdge:
                    return new Vector3(localMin.x, localMax.y, localCenter.z);
                case PivotPoint.TopRightEdge:
                    return new Vector3(localMax.x, localMax.y, localCenter.z);

                // Vertical Edge Centers
                case PivotPoint.FrontLeftEdge:
                    return new Vector3(localMin.x, localCenter.y, localMax.z);
                case PivotPoint.FrontRightEdge:
                    return new Vector3(localMax.x, localCenter.y, localMax.z);
                case PivotPoint.BackLeftEdge:
                    return new Vector3(localMin.x, localCenter.y, localMin.z);
                case PivotPoint.BackRightEdge:
                    return new Vector3(localMax.x, localCenter.y, localMin.z);

                // Bottom Corners
                case PivotPoint.BottomFrontLeft:
                    return new Vector3(localMin.x, localMin.y, localMax.z);
                case PivotPoint.BottomFrontRight:
                    return new Vector3(localMax.x, localMin.y, localMax.z);
                case PivotPoint.BottomBackLeft:
                    return localMin;
                case PivotPoint.BottomBackRight:
                    return new Vector3(localMax.x, localMin.y, localMin.z);

                // Top Corners
                case PivotPoint.TopFrontLeft:
                    return new Vector3(localMin.x, localMax.y, localMax.z);
                case PivotPoint.TopFrontRight:
                    return new Vector3(localMax.x, localMax.y, localMax.z);
                case PivotPoint.TopBackLeft:
                    return new Vector3(localMin.x, localMax.y, localMin.z);
                case PivotPoint.TopBackRight:
                    return localMax;

                default:
                    return localCenter;
            }
        }

        /// <summary>
        /// Apply snap grid to a position if snap grid is enabled
        /// </summary>
        private Vector3 ApplySnapGrid(Vector3 position)
        {
            if (!_enableSnapGrid || _snapGridSize <= 0)
                return position;

            return new Vector3(
                Mathf.Round(position.x / _snapGridSize) * _snapGridSize,
                Mathf.Round(position.y / _snapGridSize) * _snapGridSize,
                Mathf.Round(position.z / _snapGridSize) * _snapGridSize
            );
        }

        /// <summary>
        /// Get snapped position based on current snap mode and settings
        /// Respects SnapMode: Extend uses global + local, Self uses only local
        /// </summary>
        /// <param name="position">Position to snap</param>
        /// <param name="globalGridInfo">Optional global grid info from SceneSandboxBuilder</param>
        /// <returns>Snapped position</returns>
        private Vector3 GetSnappedPosition(Vector3 position, GridInfo? globalGridInfo = null)
        {
            switch (_snapMode)
            {
                case SnapMode.Self:
                    // Self mode: Only use item's own settings, ignore global
                    if (_enableSnapGrid && _snapGridSize > 0)
                    {
                        return ApplySnapGrid(position);
                    }
                    // If item snap is disabled in Self mode, no snapping at all
                    return position;

                case SnapMode.Extend:
                default:
                    // Extend mode: Use global grid if provided, otherwise use item's own
                    if (globalGridInfo.HasValue && globalGridInfo.Value.enableSnap && globalGridInfo.Value.gridSize > 0)
                    {
                        // Apply global grid snap
                        float gridSize = globalGridInfo.Value.gridSize;
                        Vector3 offset = globalGridInfo.Value.gridOffset;
                        
                        float snappedX = Mathf.Round((position.x - offset.x) / gridSize) * gridSize + offset.x;
                        float snappedZ = Mathf.Round((position.z - offset.z) / gridSize) * gridSize + offset.z;
                        return new Vector3(snappedX, position.y, snappedZ);
                    }
                    
                    // Fall back to item's own grid if no global grid or global is disabled
                    if (_enableSnapGrid && _snapGridSize > 0)
                    {
                        return ApplySnapGrid(position);
                    }
                    
                    return position;
            }
        }

        /// <summary>
        /// Get snapped position value without setting it
        /// Useful for retrieving snapped position before applying
        /// </summary>
        /// <param name="position">Position to snap</param>
        /// <param name="globalGridInfo">Optional global grid info from SceneSandboxBuilder</param>
        /// <returns>Snapped position</returns>
        public Vector3 GetSnappedPositionValue(Vector3 position, GridInfo? globalGridInfo = null)
        {
            return GetSnappedPosition(position, globalGridInfo);
        }

        /// <summary>
        /// Set position with optional global grid info and snap
        /// Convenience wrapper that calls SetPositionWithPivot with default settings
        /// </summary>
        /// <param name="position">Target position</param>
        /// <param name="globalGridInfo">Optional global grid info from SceneSandboxBuilder</param>
        public void SetPosition(Vector3 position, GridInfo? globalGridInfo = null)
        {
            SetPositionWithPivot(position, applyPivot: true, applyOffset: true, globalGridInfo: globalGridInfo);
        }

        /// <summary>
        /// Set position with pivot point and placement offset applied
        /// Useful for placing objects with a specific anchor point
        /// </summary>
        /// <param name="position">Target world position</param>
        /// <param name="applyPivot">Whether to apply pivot point offset</param>
        /// <param name="applyOffset">Whether to apply placement offset</param>
        /// <param name="globalGridInfo">Optional global grid info for snapping</param>
        public void SetPositionWithPivot(Vector3 position, bool applyPivot = true, bool applyOffset = true, GridInfo? globalGridInfo = null)
        {
            Vector3 finalPosition = position;
            
            // Apply placement offset
            if (applyOffset)
            {
                finalPosition += _placementOffset;
            }
            
            // Apply pivot point offset (relative to object's bounds)
            if (applyPivot)
            {
                // Calculate pivot offset in world space using enum-based pivot point
                Vector3 localPivot = GetPivotPointOffset(_pivotPoint);
                Vector3 pivotOffset = transform.TransformVector(localPivot);
                finalPosition -= pivotOffset;
            }
            
            // Apply snapping if grid info is provided
            if (globalGridInfo.HasValue)
            {
                finalPosition = GetSnappedPosition(finalPosition, globalGridInfo);
            }
            
            transform.position = finalPosition;
        }

        /// <summary>
        /// Toggle snap grid on/off
        /// </summary>
        public void ToggleSnapGrid()
        {
            _enableSnapGrid = !_enableSnapGrid;
        }

        /// <summary>
        /// Set snap grid settings
        /// </summary>
        public void SetSnapGridSettings(bool enabled, float gridSize, SnapMode snapMode = SnapMode.Extend)
        {
            _enableSnapGrid = enabled;
            _snapGridSize = Mathf.Max(0.01f, gridSize); // Ensure minimum grid size
            _snapMode = snapMode;
        }

        /// <summary>
        /// Toggle between Extend and Self snap modes
        /// </summary>
        public void ToggleSnapMode()
        {
            _snapMode = (_snapMode == SnapMode.Extend) ? SnapMode.Self : SnapMode.Extend;
        }

        /// <summary>
        /// Set snap mode explicitly
        /// </summary>
        public void SetSnapMode(SnapMode mode)
        {
            _snapMode = mode;
        }

        /// <summary>
        /// Set placement settings (pivot point and offset)
        /// </summary>
        /// <param name="pivotPoint">Pivot point enum for the object</param>
        /// <param name="placementOffset">Offset to apply during placement</param>
        public void SetPlacementSettings(PivotPoint pivotPoint, Vector3 placementOffset)
        {
            _pivotPoint = pivotPoint;
            _placementOffset = placementOffset;
        }

        /// <summary>
        /// Toggle transform control feature on/off
        /// </summary>
        public void ToggleTransformControls()
        {
            _enableTransformControls = !_enableTransformControls;
            
            if (!_enableTransformControls)
            {
                SetTransformModeType(TransformModeType.None);
                
                // Destroy gizmo
                DestroyGizmo();
            }
            else
            {
                // Initialize gizmo
                if (_showGizmo)
                {
                    InitializeGizmo();
                }
            }
            
            // Transform controls toggled for item
        }

        /// <summary>
        /// Increase transform value based on current mode (step-based adjustment)
        /// </summary>
        public void IncreaseTransformValue()
        {
            if (!_enableTransformControls || _currentTransformModeType == TransformModeType.None)
                return;

            switch (_currentTransformModeType)
            {
                case TransformModeType.Position:
                    // Move position based on selected axis
                    // Apply offset according to current transform axis
                    Vector3 positionDelta = Vector3.zero;
                    switch (_currentTransformAxis)
                    {
                        case TransformAxis.X:
                            positionDelta = Vector3.right * _positionStep;
                            break;
                        case TransformAxis.Y:
                            positionDelta = Vector3.up * _positionStep;
                            break;
                        case TransformAxis.Z:
                            positionDelta = Vector3.forward * _positionStep;
                            break;
                        case TransformAxis.All:
                            // For All, move in forward direction (Z) by default
                            positionDelta = Vector3.forward * _positionStep;
                            break;
                    }
                    transform.position += positionDelta;
                    break;

                case TransformModeType.Rotation:
                    // Rotate based on selected axis
                    Vector3 currentRotation = transform.eulerAngles;
                    switch (_currentTransformAxis)
                    {
                        case TransformAxis.X:
                            currentRotation.x += _rotationStep;
                            break;
                        case TransformAxis.Y:
                            currentRotation.y += _rotationStep;
                            break;
                        case TransformAxis.Z:
                            currentRotation.z += _rotationStep;
                            break;
                        case TransformAxis.All:
                            currentRotation.y += _rotationStep; // Default to Y for All
                            break;
                    }
                    transform.eulerAngles = currentRotation;
                    break;

                case TransformModeType.Scale:
                    // Scale based on selected axis
                    Vector3 newScale = transform.localScale;
                    switch (_currentTransformAxis)
                    {
                        case TransformAxis.X:
                            newScale.x += _scaleStep;
                            newScale.x = Mathf.Clamp(newScale.x, _minScale.x, _maxScale.x);
                            break;
                        case TransformAxis.Y:
                            newScale.y += _scaleStep;
                            newScale.y = Mathf.Clamp(newScale.y, _minScale.y, _maxScale.y);
                            break;
                        case TransformAxis.Z:
                            newScale.z += _scaleStep;
                            newScale.z = Mathf.Clamp(newScale.z, _minScale.z, _maxScale.z);
                            break;
                        case TransformAxis.All:
                            newScale += Vector3.one * _scaleStep;
                            newScale.x = Mathf.Clamp(newScale.x, _minScale.x, _maxScale.x);
                            newScale.y = Mathf.Clamp(newScale.y, _minScale.y, _maxScale.y);
                            newScale.z = Mathf.Clamp(newScale.z, _minScale.z, _maxScale.z);
                            break;
                    }
                    transform.localScale = newScale;
                    break;
            }
        }

        /// <summary>
        /// Decrease transform value based on current mode (step-based adjustment)
        /// </summary>
        public void DecreaseTransformValue()
        {
            if (!_enableTransformControls || _currentTransformModeType == TransformModeType.None)
                return;

            switch (_currentTransformModeType)
            {
                case TransformModeType.Position:
                    // Move position based on selected axis (negative)
                    // Apply offset according to current transform axis
                    Vector3 positionDelta = Vector3.zero;
                    switch (_currentTransformAxis)
                    {
                        case TransformAxis.X:
                            positionDelta = Vector3.left * _positionStep;
                            break;
                        case TransformAxis.Y:
                            positionDelta = Vector3.down * _positionStep;
                            break;
                        case TransformAxis.Z:
                            positionDelta = Vector3.back * _positionStep;
                            break;
                        case TransformAxis.All:
                            // For All, move in backward direction (Z) by default
                            positionDelta = Vector3.back * _positionStep;
                            break;
                    }
                    transform.position += positionDelta;
                    break;

                case TransformModeType.Rotation:
                    // Rotate based on selected axis (negative)
                    Vector3 currentRotation = transform.eulerAngles;
                    switch (_currentTransformAxis)
                    {
                        case TransformAxis.X:
                            currentRotation.x -= _rotationStep;
                            break;
                        case TransformAxis.Y:
                            currentRotation.y -= _rotationStep;
                            break;
                        case TransformAxis.Z:
                            currentRotation.z -= _rotationStep;
                            break;
                        case TransformAxis.All:
                            currentRotation.y -= _rotationStep; // Default to Y for All
                            break;
                    }
                    transform.eulerAngles = currentRotation;
                    break;

                case TransformModeType.Scale:
                    // Scale based on selected axis (decrease)
                    Vector3 newScale = transform.localScale;
                    switch (_currentTransformAxis)
                    {
                        case TransformAxis.X:
                            newScale.x -= _scaleStep;
                            newScale.x = Mathf.Clamp(newScale.x, _minScale.x, _maxScale.x);
                            break;
                        case TransformAxis.Y:
                            newScale.y -= _scaleStep;
                            newScale.y = Mathf.Clamp(newScale.y, _minScale.y, _maxScale.y);
                            break;
                        case TransformAxis.Z:
                            newScale.z -= _scaleStep;
                            newScale.z = Mathf.Clamp(newScale.z, _minScale.z, _maxScale.z);
                            break;
                        case TransformAxis.All:
                            newScale -= Vector3.one * _scaleStep;
                            newScale.x = Mathf.Clamp(newScale.x, _minScale.x, _maxScale.x);
                            newScale.y = Mathf.Clamp(newScale.y, _minScale.y, _maxScale.y);
                            newScale.z = Mathf.Clamp(newScale.z, _minScale.z, _maxScale.z);
                            break;
                    }
                    transform.localScale = newScale;
                    break;
            }
        }

        #endregion

        #region Gizmo Management

        /// <summary>
        /// Manually show/hide gizmo for testing
        /// </summary>
        public void ToggleGizmo(bool show)
        {
            if (!_enableTransformControls)
                return;

            if (show && !_gizmoInitialized)
            {
                InitializeGizmo();
            }

            SetGizmoVisible(show);
        }

        /// <summary>
        /// Initialize the visual gizmo for transform manipulation
        /// </summary>
        private void InitializeGizmo()
        {
            if (_gizmoInitialized || !_enableTransformControls || !_showGizmo)
            {
                return;
            }

            // Create gizmo root - DON'T parent it to the object to avoid scale/rotation dependency
            var gizmoObj = new GameObject($"{name}_Gizmo");
            _gizmoRoot = gizmoObj.transform;
            _gizmoRoot.gameObject.layer = LayerMaskToLayer(_gizmoLayer);
            
            // Don't parent to this transform - keep it independent in world space
            // This ensures gizmo scale/rotation is not affected by object's transform
            _gizmoRoot.position = transform.position;
            _gizmoRoot.rotation = Quaternion.identity;
            _gizmoRoot.localScale = Vector3.one;

            // Create axis handles
            _xHandle = CreateAxisHandle("X", Vector3.right, _gizmoXColor);
            _yHandle = CreateAxisHandle("Y", Vector3.up, _gizmoYColor);
            _zHandle = CreateAxisHandle("Z", Vector3.forward, _gizmoZColor);

            _gizmoInitialized = true;
            
            // Initially hide gizmo
            SetGizmoVisible(false);
        }

        /// <summary>
        /// Create a single axis handle (arrow, ring, box)
        /// </summary>
        private GizmoAxisHandle CreateAxisHandle(string axisName, Vector3 localAxis, Color color)
        {
            var handleObj = new GameObject($"{axisName}Handle");
            handleObj.layer = LayerMaskToLayer(_gizmoLayer);
            handleObj.transform.SetParent(_gizmoRoot, false);

            var handle = new GizmoAxisHandle
            {
                localAxis = localAxis,
                baseColor = color,
                highlightColor = _gizmoHighlightColor
            };

            int gizmoLayerIndex = LayerMaskToLayer(_gizmoLayer);

            // Build arrow for Move mode
            handle.BuildArrow(handleObj.transform, color, gizmoLayerIndex);
            
            // Build ring for Rotate mode
            handle.BuildRing(handleObj.transform, color, gizmoLayerIndex);
            
            // Build box for Scale mode
            handle.BuildBox(handleObj.transform, color, gizmoLayerIndex);

            return handle;
        }

        /// <summary>
        /// Update gizmo position, rotation, and scale each frame
        /// </summary>
        private void UpdateGizmo()
        {
            if (!_gizmoInitialized || !_showGizmo || _gizmoRoot == null)
                return;

            // Find camera if not cached
            if (_camera == null)
            {
                _camera = Camera.main ?? FindFirstObjectByType<Camera>();
                if (_camera == null)
                    return;
            }

            // Show gizmo when selected AND in transform mode
            bool shouldShow = _isSelected && _isInTransformModeType && _currentTransformModeType != TransformModeType.None;
            SetGizmoVisible(shouldShow);

            if (!shouldShow)
                return;

            // Keep gizmo at object position (independent of object's transform)
            _gizmoRoot.position = transform.position;
            
            // Gizmo rotation (always world space - not affected by object rotation)
            _gizmoRoot.rotation = Quaternion.identity;

            // Scale gizmo based on distance to camera (independent of object scale)
            float dist = Vector3.Distance(_camera.transform.position, transform.position);
            float gizmoWorldScale = Mathf.Max(0.0001f, dist * _gizmoScreenScale);
            _gizmoRoot.localScale = Vector3.one * gizmoWorldScale;

            // Update visibility of axis handles based on current mode
            UpdateGizmoVisuals();
        }

        /// <summary>
        /// Update which parts of the gizmo are visible based on current transform mode
        /// </summary>
        private void UpdateGizmoVisuals()
        {
            if (!_gizmoInitialized)
                return;

            _xHandle?.Show(_currentTransformModeType, _currentTransformAxis, _gizmoHighlightColor);
            _yHandle?.Show(_currentTransformModeType, _currentTransformAxis, _gizmoHighlightColor);
            _zHandle?.Show(_currentTransformModeType, _currentTransformAxis, _gizmoHighlightColor);
        }

        /// <summary>
        /// Show or hide the entire gizmo
        /// </summary>
        private void SetGizmoVisible(bool visible)
        {
            if (_gizmoRoot != null)
            {
                _gizmoRoot.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Destroy the gizmo
        /// </summary>
        private void DestroyGizmo()
        {
            if (_gizmoRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_gizmoRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(_gizmoRoot.gameObject);
                }
                _gizmoRoot = null;
            }

            _gizmoInitialized = false;
        }

        /// <summary>
        /// Convert LayerMask to layer index
        /// </summary>
        private int LayerMaskToLayer(LayerMask mask)
        {
            int m = mask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((m & (1 << i)) != 0)
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// Helper class to represent a single axis handle with arrow, ring, and box
        /// </summary>
        private class GizmoAxisHandle
        {
            public Vector3 localAxis;
            public Color baseColor;
            public Color highlightColor;

            // Visual components
            private MeshRenderer _arrowRenderer;
            private MeshRenderer _ringRenderer;
            private MeshRenderer _boxRenderer;
            
            private Material _arrowMaterial;
            private Material _ringMaterial;
            private Material _boxMaterial;

            private Collider _arrowCollider;
            private Collider _ringCollider;
            private Collider _boxCollider;

            /// <summary>
            /// Build arrow visual for Move mode
            /// </summary>
            public void BuildArrow(Transform parent, Color color, int layer)
            {
                // Create shaft
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.layer = layer;
                shaft.transform.SetParent(parent, false);
                shaft.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                shaft.transform.localPosition = localAxis * 0.6f;
                shaft.transform.localScale = new Vector3(0.04f, 0.6f, 0.04f);

                // Create tip
                var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tip.layer = layer;
                tip.transform.SetParent(parent, false);
                tip.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                tip.transform.localPosition = localAxis * 1.3f;
                tip.transform.localScale = new Vector3(0.10f, 0.2f, 0.10f);

                // Setup materials
                _arrowMaterial = new Material(Shader.Find("Unlit/Color"));
                _arrowMaterial.color = color;
                
                _arrowRenderer = shaft.GetComponent<MeshRenderer>();
                _arrowRenderer.material = _arrowMaterial;
                
                var tipRenderer = tip.GetComponent<MeshRenderer>();
                tipRenderer.material = _arrowMaterial;

                // Setup colliders
                _arrowCollider = tip.GetComponent<Collider>();
                shaft.GetComponent<Collider>().enabled = false;
            }

            /// <summary>
            /// Build ring visual for Rotate mode
            /// </summary>
            public void BuildRing(Transform parent, Color color, int layer)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.layer = layer;
                ring.transform.SetParent(parent, false);
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                ring.transform.localScale = new Vector3(1.2f, 0.01f, 1.2f);

                _ringMaterial = new Material(Shader.Find("Unlit/Color"));
                _ringMaterial.color = color;
                
                _ringRenderer = ring.GetComponent<MeshRenderer>();
                _ringRenderer.material = _ringMaterial;

                _ringCollider = ring.GetComponent<Collider>();
            }

            /// <summary>
            /// Build box visual for Scale mode
            /// </summary>
            public void BuildBox(Transform parent, Color color, int layer)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.layer = layer;
                box.transform.SetParent(parent, false);
                box.transform.localPosition = localAxis * 1.0f;
                box.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

                _boxMaterial = new Material(Shader.Find("Unlit/Color"));
                _boxMaterial.color = color;
                
                _boxRenderer = box.GetComponent<MeshRenderer>();
                _boxRenderer.material = _boxMaterial;

                _boxCollider = box.GetComponent<Collider>();
            }

            /// <summary>
            /// Show/hide components based on current transform mode and highlight selected axis
            /// </summary>
            public void Show(TransformModeType mode, TransformAxis selectedAxis, Color highlightColor)
            {
                bool showArrow = mode == TransformModeType.Position;
                bool showRing = mode == TransformModeType.Rotation;
                bool showBox = mode == TransformModeType.Scale;

                // Hide all when mode is None
                if (mode == TransformModeType.None)
                {
                    showArrow = showRing = showBox = false;
                }

                // Determine if this axis should be highlighted
                bool isHighlighted = IsAxisHighlighted(selectedAxis);

                // Update arrow visibility and color
                if (_arrowRenderer != null)
                {
                    _arrowRenderer.enabled = showArrow;
                    if (showArrow && _arrowMaterial != null)
                    {
                        _arrowMaterial.color = isHighlighted ? highlightColor : baseColor;
                    }
                }
                if (_arrowCollider != null)
                    _arrowCollider.enabled = showArrow;

                // Update ring visibility and color
                if (_ringRenderer != null)
                {
                    _ringRenderer.enabled = showRing;
                    if (showRing && _ringMaterial != null)
                    {
                        _ringMaterial.color = isHighlighted ? highlightColor : baseColor;
                    }
                }
                if (_ringCollider != null)
                    _ringCollider.enabled = showRing;

                // Update box visibility and color
                if (_boxRenderer != null)
                {
                    _boxRenderer.enabled = showBox;
                    if (showBox && _boxMaterial != null)
                    {
                        _boxMaterial.color = isHighlighted ? highlightColor : baseColor;
                    }
                }
                if (_boxCollider != null)
                    _boxCollider.enabled = showBox;
            }

            /// <summary>
            /// Determine if this axis should be highlighted based on selected axis
            /// </summary>
            private bool IsAxisHighlighted(TransformAxis selectedAxis)
            {
                // Always highlight when All is selected
                if (selectedAxis == TransformAxis.All)
                    return true;

                // Check if this handle's axis matches the selected axis
                if (selectedAxis == TransformAxis.X && Vector3.Dot(localAxis, Vector3.right) > 0.9f)
                    return true;
                if (selectedAxis == TransformAxis.Y && Vector3.Dot(localAxis, Vector3.up) > 0.9f)
                    return true;
                if (selectedAxis == TransformAxis.Z && Vector3.Dot(localAxis, Vector3.forward) > 0.9f)
                    return true;

                return false;
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw grid gizmo if snap grid is enabled
            if (_enableSnapGrid && _snapGridSize > 0)
            {
                DrawSnapGridGizmo();
            }
        }

        /// <summary>
        /// Draw snap grid visualization
        /// </summary>
        private void DrawSnapGridGizmo()
        {
            Vector3 position = transform.position;
            float gridSize = _snapGridSize;
            Color gridColor = _snapMode == SnapMode.Self ? Color.yellow : Color.cyan;
            gridColor.a = 0.3f;

            // Draw grid lines around the object (5x5 grid)
            int gridExtent = 5;
            Vector3 snappedCenter = new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                position.y,
                Mathf.Round(position.z / gridSize) * gridSize
            );

            Gizmos.color = gridColor;

            // Draw horizontal lines (X direction)
            for (int z = -gridExtent; z <= gridExtent; z++)
            {
                Vector3 start = snappedCenter + new Vector3(-gridExtent * gridSize, 0, z * gridSize);
                Vector3 end = snappedCenter + new Vector3(gridExtent * gridSize, 0, z * gridSize);
                Gizmos.DrawLine(start, end);
            }

            // Draw vertical lines (Z direction)
            for (int x = -gridExtent; x <= gridExtent; x++)
            {
                Vector3 start = snappedCenter + new Vector3(x * gridSize, 0, -gridExtent * gridSize);
                Vector3 end = snappedCenter + new Vector3(x * gridSize, 0, gridExtent * gridSize);
                Gizmos.DrawLine(start, end);
            }

            // Draw snap point indicator at current snapped position
            Gizmos.color = _snapMode == SnapMode.Self ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(snappedCenter, gridSize * 0.1f);

            // Draw label for snap mode
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                snappedCenter + Vector3.up * 0.5f,
                $"Snap: {_snapMode}\nGrid: {gridSize}m",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = gridColor * 2f },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                }
            );
#endif
        }

        #endregion
    }
}