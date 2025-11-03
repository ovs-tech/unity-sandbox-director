using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SceneSandbox.Core
{
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
        [SerializeField] private float _rotationSensitivity = 2f;
        [SerializeField] private float _scaleSensitivity = 0.01f;
        [SerializeField] private Vector3 _minScale = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 _maxScale = new Vector3(5f, 5f, 5f);
        
        [Header("Transform Step Settings")]
        [SerializeField] private float _positionStep = 0.1f; // Step for position adjustment (not used in placement mode)
        [SerializeField] private float _rotationStep = 5f; // Degrees per step
        [SerializeField] private float _scaleStep = 0.1f; // Scale units per step

        [Header("Gizmo Settings")]
        [SerializeField] private bool _showGizmo = true;
        [SerializeField] private float _gizmoScreenScale = 0.12f;
        [SerializeField] private LayerMask _gizmoLayer = 1 << 2; // Default to layer 2
        [SerializeField] private Color _gizmoXColor = new Color(1, 0.2f, 0.2f);
        [SerializeField] private Color _gizmoYColor = new Color(0.2f, 1, 0.2f);
        [SerializeField] private Color _gizmoZColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _gizmoHighlight = Color.yellow;

        // Transform control state
        [SerializeField] private TransformModeType _currentTransformModeType = TransformModeType.None;
        [SerializeField] private TransformAxis _currentTransformAxis = TransformAxis.All;
        private Vector3 _originalScale;
        private Vector3 _originalRotation;
        private Vector3 _originalPosition;
        private Vector2 _lastInputPosition;
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
        public System.Action<TransformableItem> OnItemClicked;
        public System.Action<TransformableItem> OnItemSelected;
        public System.Action<TransformableItem, bool> OnSelectionChanged;
        public System.Action<TransformableItem, TransformModeType> OnTransformModeTypeChanged;


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

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Reinitialize gizmo in editor when values change
            if (_enableTransformControls && _showGizmo)
            {
                DestroyGizmo();
                if (Application.isPlaying)
                {
                    InitializeGizmo();
                }
            }
            else
            {
                DestroyGizmo();
            }
        }
#endif

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

            Debug.Log($"[TransformableItem] Transform mode changed to: {mode}, isInMode: {_isInTransformModeType}, isSelected: {_isSelected}");

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
            // Only allow axis change for Rotation and Scale modes
            if (_currentTransformModeType != TransformModeType.Rotation && _currentTransformModeType != TransformModeType.Scale)
            {
                _currentTransformAxis = TransformAxis.All;
                return;
            }

            _currentTransformAxis = axis;
            
            // Update gizmo to highlight selected axis
            UpdateGizmoVisuals();
            
            Debug.Log($"[TransformableItem] Axis set to: {axis}");
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
                    // Position is controlled by SceneSandboxBuilder during placement
                    // This method is not used for position mode
                    // Position mode does not support increase/decrease - controlled by SceneSandboxBuilder
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
                    // Position is controlled by SceneSandboxBuilder during placement
                    // This method is not used for position mode
                    // Position mode does not support increase/decrease - controlled by SceneSandboxBuilder
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

        #region Editor Gizmos (Edit Mode)

#if UNITY_EDITOR
        /// <summary>
        /// Draw gizmos when object is selected in editor (Edit Mode)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Debug.Log($"[TransformableItem] OnDrawGizmosSelected called - isPlaying:{Application.isPlaying}, enableControls:{_enableTransformControls}, showGizmo:{_showGizmo}");
            
            // Only draw in edit mode when transform controls are enabled
            if (Application.isPlaying || !_enableTransformControls || !_showGizmo)
            {
                Debug.Log($"[TransformableItem] Gizmo draw skipped");
                return;
            }

            Debug.Log($"[TransformableItem] Drawing gizmo in Edit Mode, mode: {_currentTransformModeType}");
            DrawEditorGizmo();
        }

        /// <summary>
        /// Draw the gizmo in Edit Mode using Unity Gizmos API
        /// </summary>
        private void DrawEditorGizmo()
        {
            Vector3 position = transform.position;
            float gizmoSize = HandleUtility.GetHandleSize(position) * _gizmoScreenScale;

            Debug.Log($"[TransformableItem] DrawEditorGizmo - pos:{position}, size:{gizmoSize}, mode:{_currentTransformModeType}");

            // Draw based on current transform mode (can be controlled via Inspector)
            switch (_currentTransformModeType)
            {
                case TransformModeType.Position:
                    DrawPositionGizmo(position, gizmoSize);
                    break;
                case TransformModeType.Rotation:
                    DrawRotationGizmo(position, gizmoSize);
                    break;
                case TransformModeType.Scale:
                    DrawScaleGizmo(position, gizmoSize);
                    break;
                default:
                    // Default: show position gizmo when selected
                    DrawPositionGizmo(position, gizmoSize);
                    break;
            }
        }

        /// <summary>
        /// Draw position mode arrows
        /// </summary>
        private void DrawPositionGizmo(Vector3 position, float size)
        {
            // X Axis (Red)
            Gizmos.color = _gizmoXColor;
            DrawArrow(position, Vector3.right, size);

            // Y Axis (Green)
            Gizmos.color = _gizmoYColor;
            DrawArrow(position, Vector3.up, size);

            // Z Axis (Blue)
            Gizmos.color = _gizmoZColor;
            DrawArrow(position, Vector3.forward, size);
        }

        /// <summary>
        /// Draw rotation mode rings
        /// </summary>
        private void DrawRotationGizmo(Vector3 position, float size)
        {
            // X Axis Ring (Red)
            Gizmos.color = _gizmoXColor;
            DrawCircle(position, Vector3.right, size * 1.2f);

            // Y Axis Ring (Green)
            Gizmos.color = _gizmoYColor;
            DrawCircle(position, Vector3.up, size * 1.2f);

            // Z Axis Ring (Blue)
            Gizmos.color = _gizmoZColor;
            DrawCircle(position, Vector3.forward, size * 1.2f);
        }

        /// <summary>
        /// Draw scale mode boxes
        /// </summary>
        private void DrawScaleGizmo(Vector3 position, float size)
        {
            float boxSize = size * 0.15f;

            // X Axis (Red)
            Gizmos.color = _gizmoXColor;
            Gizmos.DrawCube(position + Vector3.right * size, Vector3.one * boxSize);
            Gizmos.DrawLine(position, position + Vector3.right * size);

            // Y Axis (Green)
            Gizmos.color = _gizmoYColor;
            Gizmos.DrawCube(position + Vector3.up * size, Vector3.one * boxSize);
            Gizmos.DrawLine(position, position + Vector3.up * size);

            // Z Axis (Blue)
            Gizmos.color = _gizmoZColor;
            Gizmos.DrawCube(position + Vector3.forward * size, Vector3.one * boxSize);
            Gizmos.DrawLine(position, position + Vector3.forward * size);
        }

        /// <summary>
        /// Helper to draw an arrow
        /// </summary>
        private void DrawArrow(Vector3 from, Vector3 direction, float size)
        {
            Vector3 to = from + direction * size;
            
            // Draw shaft
            Gizmos.DrawLine(from, to);
            
            // Draw arrow head
            Vector3 right = Vector3.Cross(direction, Vector3.up);
            if (right.magnitude < 0.1f) right = Vector3.Cross(direction, Vector3.forward);
            right = right.normalized;
            
            Vector3 arrowTip = to;
            Vector3 arrowBase = to - direction * size * 0.2f;
            
            // Draw cone as arrow head
            Vector3 up = Vector3.Cross(direction, right).normalized;
            int segments = 8;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)i / segments * Mathf.PI * 2;
                float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
                
                Vector3 offset1 = (right * Mathf.Cos(angle1) + up * Mathf.Sin(angle1)) * size * 0.1f;
                Vector3 offset2 = (right * Mathf.Cos(angle2) + up * Mathf.Sin(angle2)) * size * 0.1f;
                
                Gizmos.DrawLine(arrowBase + offset1, arrowTip);
                Gizmos.DrawLine(arrowBase + offset1, arrowBase + offset2);
            }
        }

        /// <summary>
        /// Helper to draw a circle/ring
        /// </summary>
        private void DrawCircle(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 right = Vector3.Cross(normal, Vector3.up);
            if (right.magnitude < 0.1f) right = Vector3.Cross(normal, Vector3.forward);
            right = right.normalized;
            
            Vector3 up = Vector3.Cross(normal, right).normalized;
            
            int segments = 32;
            Vector3 prevPoint = center + right * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2;
                Vector3 point = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
#endif

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
            Debug.Log($"[TransformableItem] Gizmo toggled: {show}");
        }

        /// <summary>
        /// Initialize the visual gizmo for transform manipulation
        /// </summary>
        private void InitializeGizmo()
        {
            if (_gizmoInitialized || !_enableTransformControls || !_showGizmo)
            {
                Debug.Log($"[TransformableItem] Gizmo init skipped - initialized:{_gizmoInitialized}, controls:{_enableTransformControls}, show:{_showGizmo}");
                return;
            }

            Debug.Log($"[TransformableItem] Initializing gizmo for {name}");

            // Create gizmo root - DON'T parent it to the object to avoid scale/rotation dependency
            var gizmoObj = new GameObject($"{name}_Gizmo");
            _gizmoRoot = gizmoObj.transform;
            
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
            
            Debug.Log($"[TransformableItem] Gizmo initialized successfully for {name}");
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
                baseColor = color
            };

            // Build arrow for Move mode
            handle.BuildArrow(handleObj.transform, color);
            
            // Build ring for Rotate mode
            handle.BuildRing(handleObj.transform, color);
            
            // Build box for Scale mode
            handle.BuildBox(handleObj.transform, color);

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

            _xHandle?.Show(_currentTransformModeType);
            _yHandle?.Show(_currentTransformModeType);
            _zHandle?.Show(_currentTransformModeType);
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

            // Visual components
            private MeshRenderer _arrowRenderer;
            private MeshRenderer _ringRenderer;
            private MeshRenderer _boxRenderer;

            private Collider _arrowCollider;
            private Collider _ringCollider;
            private Collider _boxCollider;

            /// <summary>
            /// Build arrow visual for Move mode
            /// </summary>
            public void BuildArrow(Transform parent, Color color)
            {
                // Create shaft
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.transform.SetParent(parent, false);
                shaft.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                shaft.transform.localPosition = localAxis * 0.6f;
                shaft.transform.localScale = new Vector3(0.04f, 0.6f, 0.04f);

                // Create tip
                var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tip.transform.SetParent(parent, false);
                tip.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                tip.transform.localPosition = localAxis * 1.3f;
                tip.transform.localScale = new Vector3(0.10f, 0.2f, 0.10f);

                // Setup materials
                var mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = color;
                
                _arrowRenderer = shaft.GetComponent<MeshRenderer>();
                _arrowRenderer.material = mat;
                
                var tipRenderer = tip.GetComponent<MeshRenderer>();
                tipRenderer.material = mat;

                // Setup colliders
                _arrowCollider = tip.GetComponent<Collider>();
                shaft.GetComponent<Collider>().enabled = false;
            }

            /// <summary>
            /// Build ring visual for Rotate mode
            /// </summary>
            public void BuildRing(Transform parent, Color color)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.transform.SetParent(parent, false);
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localAxis);
                ring.transform.localScale = new Vector3(1.2f, 0.01f, 1.2f);

                var mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = color;
                
                _ringRenderer = ring.GetComponent<MeshRenderer>();
                _ringRenderer.material = mat;

                _ringCollider = ring.GetComponent<Collider>();
            }

            /// <summary>
            /// Build box visual for Scale mode
            /// </summary>
            public void BuildBox(Transform parent, Color color)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(parent, false);
                box.transform.localPosition = localAxis * 1.0f;
                box.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

                var mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = color;
                
                _boxRenderer = box.GetComponent<MeshRenderer>();
                _boxRenderer.material = mat;

                _boxCollider = box.GetComponent<Collider>();
            }

            /// <summary>
            /// Show/hide components based on current transform mode
            /// </summary>
            public void Show(TransformModeType mode)
            {
                bool showArrow = mode == TransformModeType.Position;
                bool showRing = mode == TransformModeType.Rotation;
                bool showBox = mode == TransformModeType.Scale;

                // Hide all when mode is None
                if (mode == TransformModeType.None)
                {
                    showArrow = showRing = showBox = false;
                }

                // Update arrow visibility
                if (_arrowRenderer != null)
                    _arrowRenderer.enabled = showArrow;
                if (_arrowCollider != null)
                    _arrowCollider.enabled = showArrow;

                // Update ring visibility
                if (_ringRenderer != null)
                    _ringRenderer.enabled = showRing;
                if (_ringCollider != null)
                    _ringCollider.enabled = showRing;

                // Update box visibility
                if (_boxRenderer != null)
                    _boxRenderer.enabled = showBox;
                if (_boxCollider != null)
                    _boxCollider.enabled = showBox;
            }
        }

        #endregion
    }
}