using UnityEngine;

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
        public bool IsInTransformModeType => _isInTransformModeType;
        public bool EnableTransformControls
        {
            get => _enableTransformControls;
            set => _enableTransformControls = value;
        }

        private void Awake()
        {
            Debug.Log($"[TransformableItem] Awake called for {gameObject.name}");
            
            InitializeComponents();
            
            // Check collider for raycast interaction
            if (_collider == null)
            {
                Debug.LogWarning($"[TransformableItem] No collider on {gameObject.name}!");
            }
            else
            {
                Debug.Log($"[TransformableItem] Collider found: {_collider.GetType().Name}, enabled: {_collider.enabled}");
            }
        }

        private void OnDestroy()
        {
            // Clean up gizmo
            DestroyGizmo();
            
            // Unregister from selection manager (handles both selection and transform control)
            if (TransformableSelectionManager.Instance != null)
            {
                TransformableSelectionManager.Instance.UnregisterDraggableItem(this);
            }
        }

        private void Update()
        {
            UpdateGizmo();
        }

        private void Start()
        {
            Debug.Log($"[TransformableItem] Start called for {gameObject.name}");
            
            _camera = Camera.main ?? FindFirstObjectByType<Camera>();

            if (_camera == null)
            {
                Debug.LogError($"[TransformableItem] No camera found for {gameObject.name}!");
            }
            else
            {
                Debug.Log($"[TransformableItem] Camera found: {_camera.name}");
            }

            if (string.IsNullOrEmpty(_objectId))
            {
                _objectId = System.Guid.NewGuid().ToString();
            }

            // Register with selection manager (handles both selection and transform control)
            Debug.Log($"[TransformableItem] Registering {gameObject.name} with SelectionManager");
            TransformableSelectionManager.Instance.RegisterDraggableItem(this);
            
            // Initialize gizmo if transform controls are enabled
            if (_enableTransformControls && _showGizmo)
            {
                InitializeGizmo();
            }
            
            Debug.Log($"[TransformableItem] Initialization complete for {gameObject.name}");
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
            
            Debug.Log($"Reset transform for item: {name}");
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
                
                // Unregister from transform control
                if (TransformableSelectionManager.Instance != null)
                {
                    TransformableSelectionManager.Instance.UnregisterItemFromTransformControl(this);
                }
            }
            else
            {
                // Initialize gizmo
                if (_showGizmo)
                {
                    InitializeGizmo();
                }
                
                // Register with transform control
                if (TransformableSelectionManager.Instance != null)
                {
                    TransformableSelectionManager.Instance.RegisterItemForTransformControl(this);
                }
            }
            
            Debug.Log($"Transform controls {(_enableTransformControls ? "enabled" : "disabled")} for item: {name}");
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
                    Debug.Log($"[TransformableItem] Position mode does not support increase/decrease - controlled by SceneSandboxBuilder");
                    break;

                case TransformModeType.Rotation:
                    // Rotate around Y axis by step
                    Vector3 currentRotation = transform.eulerAngles;
                    currentRotation.y += _rotationStep;
                    transform.eulerAngles = currentRotation;
                    Debug.Log($"[TransformableItem] Increased rotation by {_rotationStep}° - new rotation: {currentRotation.y}°");
                    break;

                case TransformModeType.Scale:
                    // Increase scale uniformly
                    Vector3 newScale = transform.localScale + Vector3.one * _scaleStep;
                    newScale.x = Mathf.Clamp(newScale.x, _minScale.x, _maxScale.x);
                    newScale.y = Mathf.Clamp(newScale.y, _minScale.y, _maxScale.y);
                    newScale.z = Mathf.Clamp(newScale.z, _minScale.z, _maxScale.z);
                    transform.localScale = newScale;
                    Debug.Log($"[TransformableItem] Increased scale by {_scaleStep} - new scale: {newScale}");
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
                    Debug.Log($"[TransformableItem] Position mode does not support increase/decrease - controlled by SceneSandboxBuilder");
                    break;

                case TransformModeType.Rotation:
                    // Rotate around Y axis by step (negative)
                    Vector3 currentRotation = transform.eulerAngles;
                    currentRotation.y -= _rotationStep;
                    transform.eulerAngles = currentRotation;
                    Debug.Log($"[TransformableItem] Decreased rotation by {_rotationStep}° - new rotation: {currentRotation.y}°");
                    break;

                case TransformModeType.Scale:
                    // Decrease scale uniformly
                    Vector3 newScale = transform.localScale - Vector3.one * _scaleStep;
                    newScale.x = Mathf.Clamp(newScale.x, _minScale.x, _maxScale.x);
                    newScale.y = Mathf.Clamp(newScale.y, _minScale.y, _maxScale.y);
                    newScale.z = Mathf.Clamp(newScale.z, _minScale.z, _maxScale.z);
                    transform.localScale = newScale;
                    Debug.Log($"[TransformableItem] Decreased scale by {_scaleStep} - new scale: {newScale}");
                    break;
            }
        }

        #endregion

        #region Gizmo Management

        /// <summary>
        /// Initialize the visual gizmo for transform manipulation
        /// </summary>
        private void InitializeGizmo()
        {
            if (_gizmoInitialized || !_enableTransformControls || !_showGizmo)
                return;

            // Create gizmo root
            var gizmoObj = new GameObject($"{name}_Gizmo");
            _gizmoRoot = gizmoObj.transform;
            _gizmoRoot.SetParent(transform, false);
            _gizmoRoot.localPosition = Vector3.zero;
            _gizmoRoot.localRotation = Quaternion.identity;

            // Create axis handles
            _xHandle = CreateAxisHandle("X", Vector3.right, _gizmoXColor);
            _yHandle = CreateAxisHandle("Y", Vector3.up, _gizmoYColor);
            _zHandle = CreateAxisHandle("Z", Vector3.forward, _gizmoZColor);

            _gizmoInitialized = true;
            
            // Initially hide gizmo
            SetGizmoVisible(false);
            
            Debug.Log($"[TransformableItem] Gizmo initialized for {name}");
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
            if (!_gizmoInitialized || !_showGizmo || _gizmoRoot == null || _camera == null)
                return;

            // Show/hide gizmo based on transform mode
            bool shouldShow = _isInTransformModeType && _currentTransformModeType != TransformModeType.None;
            SetGizmoVisible(shouldShow);

            if (!shouldShow)
                return;

            // Keep gizmo at object position
            _gizmoRoot.position = transform.position;
            
            // Gizmo rotation (always world space for now)
            _gizmoRoot.rotation = Quaternion.identity;

            // Scale gizmo based on distance to camera
            float dist = Vector3.Distance(_camera.transform.position, transform.position);
            _gizmoRoot.localScale = Vector3.one * Mathf.Max(0.0001f, dist * _gizmoScreenScale);

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