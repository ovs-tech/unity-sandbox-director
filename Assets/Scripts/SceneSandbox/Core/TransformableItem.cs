using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SceneSandbox.Input;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Transform control modes for enhanced manipulation
    /// </summary>
    public enum TransformMode
    {
        None,
        Move,
        Rotate,
        Scale
    }

    /// <summary>
    /// Component for objects that can be dragged in the scene sandbox
    /// Uses Physics Raycast for selection and interaction instead of UI Event System
    /// Supports both mouse and touch input, selection, form-based options, and transform controls
    /// </summary>
    public class TransformableItem : MonoBehaviour
    {
        [Header("Drag Settings")]
        [SerializeField] private bool _canDrag = true;
        [SerializeField] private bool _snapToGrid = false;
        [SerializeField] private float _gridSize = 1f;
        [SerializeField] private LayerMask _dropLayers = -1;

        [Header("Visual Feedback")]
        [SerializeField] private Material _dragMaterial;
        [SerializeField] private Material _hoverMaterial;
        [SerializeField] private float _dragHeight = 0.5f;
        [SerializeField] private bool _showDragPreview = true;

        [Header("Transform Control Settings")]
        [SerializeField] private bool _enableTransformControls = true;
        [SerializeField] private Material _moveMaterial;
        [SerializeField] private Material _rotateMaterial;
        [SerializeField] private Material _scaleMaterial;
        [SerializeField] private float _rotationSensitivity = 2f;
        [SerializeField] private float _scaleSensitivity = 0.01f;
        [SerializeField] private Vector3 _minScale = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 _maxScale = new Vector3(5f, 5f, 5f);

        [Header("Gizmo Settings")]
        [SerializeField] private bool _showGizmo = true;
        [SerializeField] private float _gizmoScreenScale = 0.12f;
        [SerializeField] private LayerMask _gizmoLayer = 1 << 2; // Default to layer 2
        [SerializeField] private Color _gizmoXColor = new Color(1, 0.2f, 0.2f);
        [SerializeField] private Color _gizmoYColor = new Color(0.2f, 1, 0.2f);
        [SerializeField] private Color _gizmoZColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _gizmoHighlight = Color.yellow;

        // Transform control state
        [SerializeField] private TransformMode _currentTransformMode = TransformMode.None;
        private Vector3 _originalScale;
        private Vector3 _originalRotation;
        private Vector2 _lastInputPosition;
        private bool _isInTransformMode = false;

        // Gizmo state
        private Transform _gizmoRoot;
        private GizmoAxisHandle _xHandle, _yHandle, _zHandle;
        private bool _gizmoInitialized = false;

        [Header("Object Data")]
        [SerializeField] private string _objectId;
        [SerializeField] private string _objectDataId;

        [Header("Long Press Settings")]
        [SerializeField] private float _longPressDuration = 0.8f; // Time to trigger long press in seconds

        // Components
        private Renderer[] _renderers;
        private Material[] _originalMaterials;
        private Collider _collider;
        private Rigidbody _rigidbody;

        // Drag state
        private bool _isDragging;
        private bool _dragInitiated; // Track if OnBeginDrag was called but not actually dragging yet
        private bool _isSelected;
        private Vector3 _originalPosition;
        private Vector3 _dragOffset;
        private Camera _camera;
        private Plane _dragPlane;

        // Long press detection
        private bool _isLongPressing = false;
        private bool _longPressTriggered = false;
        private Coroutine _longPressCoroutine = null;
        private Vector2 _longPressStartPosition;
        private float _longPressMoveThreshold = 20f; // Pixels

        // Events
        public System.Action<TransformableItem, Vector3> OnDragStarted;
        public System.Action<TransformableItem, Vector3> OnDragMoved;
        public System.Action<TransformableItem, Vector3> OnDragEnded;
        public System.Action<TransformableItem> OnItemClicked;
        public System.Action<TransformableItem> OnItemSelected;
        public System.Action<TransformableItem, bool> OnSelectionChanged;
        public System.Action<TransformableItem, Vector2> OnItemLongPressed;

        public System.Action<TransformableItem, TransformMode> OnTransformModeChanged;

        // Properties
        public bool CanDrag
        {
            get => _canDrag;
            set => _canDrag = value;
        }

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

        public bool IsDragging => _isDragging;
        public bool IsSelected => _isSelected;
        public bool IsLongPressing => _isLongPressing;
        public TransformMode CurrentTransformMode => _currentTransformMode;
        public bool IsInTransformMode => _isInTransformMode;
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
            // Clean up long press coroutine
            StopLongPressDetection();
            
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
            _rigidbody = GetComponent<Rigidbody>();

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

        #region Raycast Interaction Methods

        /// <summary>
        /// Handle click from raycast (replaces OnPointerClick)
        /// </summary>
        public void OnRaycastClick(Vector2 screenPosition, int clickCount = 1, bool isRightClick = false)
        {
            Debug.Log($"[TransformableItem] OnRaycastClick called for {name}");
            Debug.Log($"[TransformableItem] _longPressTriggered: {_longPressTriggered}, _isDragging: {_isDragging}");
            
            // Handle right click for options form
            if (isRightClick)
            {
                ShowOptionsForm(screenPosition, false);
                return;
            }
            
            // Don't handle click if long press was triggered or if we're dragging
            if (!_longPressTriggered && !_isDragging)
            {
                Debug.Log($"[TransformableItem] Processing click and selecting {name}");
                OnItemClicked?.Invoke(this);

                // Handle selection on single click using selection manager
                TransformableSelectionManager.Instance.SelectItem(this);

                if (clickCount >= 2)
                {
                    Debug.Log($"[TransformableItem] Double-click detected for {name}");
                    OnItemSelected?.Invoke(this);
                }
            }
            else
            {
                Debug.Log($"[TransformableItem] Click ignored due to longPress={_longPressTriggered} or drag={_isDragging}");
            }

            // Reset long press triggered flag
            _longPressTriggered = false;
        }

        /// <summary>
        /// Handle drag start from raycast (replaces OnBeginDrag)
        /// </summary>
        public void OnRaycastDragStart(Vector2 screenPosition)
        {
            Debug.Log($"[TransformableItem] OnRaycastDragStart called for {name}, _canDrag={_canDrag}");
            
            if (!_canDrag) 
            {
                Debug.Log($"[TransformableItem] OnRaycastDragStart ignored - _canDrag=false for {name}");
                return;
            }

            // Check if we should handle transform mode instead of regular drag
            if (_isInTransformMode && _currentTransformMode != TransformMode.None)
            {
                Debug.Log($"[TransformableItem] Starting transform control for {name}");
                StartTransformControl(screenPosition);
                return;
            }
            
            // Mark drag as initiated but don't set _isDragging yet
            // This allows OnRaycastClick to still work if there's no actual movement
            Debug.Log($"[TransformableItem] Marking drag as initiated for {name}");
            _dragInitiated = true;
            _originalPosition = transform.position;
            
            // Calculate drag offset for later use
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            _dragPlane = new Plane(Vector3.up, transform.position);
            
            if (_dragPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPosition = ray.GetPoint(distance);
                _dragOffset = transform.position - worldPosition;
            }
        }

        /// <summary>
        /// Handle drag continue from raycast (replaces OnDrag)
        /// </summary>
        public void OnRaycastDrag(Vector2 screenPosition)
        {
            if (!_canDrag) return;

            // Handle transform mode
            if (_isInTransformMode && _currentTransformMode != TransformMode.None)
            {
                ContinueTransformControl(screenPosition);
                return;
            }

            // Start actual dragging on first OnRaycastDrag call after OnRaycastDragStart
            if (_dragInitiated && !_isDragging)
            {
                Debug.Log($"[TransformableItem] First OnRaycastDrag - starting actual drag for {name}");
                _isDragging = true;
                _dragInitiated = false;
                
                // Visual feedback
                SetDragMaterial();
                
                // Disable physics during drag
                if (_rigidbody != null)
                {
                    _rigidbody.isKinematic = true;
                }
                
                OnDragStarted?.Invoke(this, _originalPosition);
            }

            // Handle regular drag
            if (_isDragging)
            {
                ContinueDrag(screenPosition);
            }
        }

        /// <summary>
        /// Handle drag end from raycast (replaces OnEndDrag)
        /// </summary>
        public void OnRaycastDragEnd(Vector2 screenPosition)
        {
            if (!_canDrag) return;

            // Handle transform mode
            if (_isInTransformMode && _currentTransformMode != TransformMode.None)
            {
                EndTransformControl(screenPosition);
                return;
            }

            // Reset drag initiated flag
            _dragInitiated = false;

            // Handle regular drag
            if (_isDragging)
            {
                EndDrag(screenPosition);
            }
        }

        /// <summary>
        /// Handle pointer down from raycast (for long press detection)
        /// </summary>
        public void OnRaycastPointerDown(Vector2 screenPosition, bool isLeftButton = true)
        {
            if (isLeftButton)
            {
                StartLongPressDetection(screenPosition);
            }
        }

        /// <summary>
        /// Handle pointer up from raycast (for long press detection)
        /// </summary>
        public void OnRaycastPointerUp(Vector2 screenPosition)
        {
            StopLongPressDetection();
            
            if (_longPressTriggered)
            {
                // Reset long press triggered flag for next interaction
                _longPressTriggered = false;
            }
        }

        #endregion

        /// <summary>
        /// Start dragging the item programmatically
        /// </summary>
        public void StartDrag(Vector2 screenPosition)
        {
            Debug.Log($"[TransformableItem] StartDrag called for {name}, _canDrag={_canDrag}, _isDragging={_isDragging}");
            
            if (!_canDrag || _isDragging) 
            {
                Debug.Log($"[TransformableItem] StartDrag aborted for {name}");
                return;
            }

            Debug.Log($"[TransformableItem] Setting _isDragging=true for {name}");

            _isDragging = true;
            _dragInitiated = false; // Clear initiated flag since we're now dragging
            _originalPosition = transform.position;

            // Calculate drag offset
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            _dragPlane = new Plane(Vector3.up, transform.position);

            if (_dragPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPosition = ray.GetPoint(distance);
                _dragOffset = transform.position - worldPosition;
            }

            // Visual feedback
            SetDragMaterial();

            // Disable physics during drag
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }

            OnDragStarted?.Invoke(this, _originalPosition);

            // Also notify SandboxInputHandler if it exists and isn't already aware
            var inputHandler = FindFirstObjectByType<SandboxInputHandler>();
            if (inputHandler != null)
            {
                
                inputHandler.ManualStartDrag(gameObject, screenPosition);
            }
        }

        /// <summary>
        /// Continue dragging the item
        /// </summary>
        public void ContinueDrag(Vector2 screenPosition)
        {
            if (!_isDragging || _camera == null) return;

            Ray ray = _camera.ScreenPointToRay(screenPosition);

            if (_dragPlane.Raycast(ray, out float distance))
            {
                Vector3 targetPosition = ray.GetPoint(distance) + _dragOffset;

                // Apply drag height offset
                targetPosition.y += _dragHeight;

                // Snap to grid if enabled
                if (_snapToGrid)
                {
                    targetPosition = SnapToGrid(targetPosition);
                }

                transform.position = targetPosition;
                OnDragMoved?.Invoke(this, targetPosition);
            }
        }

        /// <summary>
        /// End dragging the item
        /// </summary>
        public void EndDrag(Vector2 screenPosition)
        {
            Debug.Log($"[TransformableItem] EndDrag called for {name}, _isDragging={_isDragging}");
            
            if (!_isDragging) return;

            Vector3 finalPosition = transform.position;

            // Check for valid drop position
            if (IsValidDropPosition(finalPosition))
            {
                // Snap to surface
                Vector3 snappedPosition = GetSurfacePosition(finalPosition);
                transform.position = snappedPosition;
                finalPosition = snappedPosition;
            }
            else
            {
                // Return to original position if invalid drop
                transform.position = _originalPosition;
                finalPosition = _originalPosition;
            }

            Debug.Log($"[TransformableItem] Setting _isDragging=false for {name}");
            _isDragging = false;

            // Restore visual feedback
            RestoreOriginalMaterial();

            // Re-enable physics
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
            }

            OnDragEnded?.Invoke(this, finalPosition);
        }

        /// <summary>
        /// Set hover visual feedback
        /// </summary>
        public void SetHoverState(bool isHovered)
        {
            if (_isDragging) return;

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
            

            // Store the selection state regardless of current drag state
            _isSelected = isSelected;
            
            // Don't change visual state if currently dragging - drag material takes priority
            if (_isDragging) return;

            // Don't override transform mode visuals
            if (_isInTransformMode && _currentTransformMode != TransformMode.None)
            {
                UpdateTransformVisuals();
                return;
            }

            // Update visual material feedback for selection
            if (isSelected && _hoverMaterial != null)
            {
                SetMaterial(_hoverMaterial);
            }
            else if (!isSelected)
            {
                RestoreOriginalMaterial();
            }
        }

        private void SetDragMaterial()
        {
            if (_dragMaterial != null)
            {
                SetMaterial(_dragMaterial);
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
            if (_isInTransformMode && _currentTransformMode != TransformMode.None)
            {
                UpdateTransformVisuals();
                return;
            }

            // If the item is selected, show selection material instead of original
            if (_isSelected && _hoverMaterial != null)
            {
                SetMaterial(_hoverMaterial);
            }
            else
            {
                // Restore original materials
                for (int i = 0; i < _renderers.Length && i < _originalMaterials.Length; i++)
                {
                    _renderers[i].material = _originalMaterials[i];
                }
            }
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            float snappedX = Mathf.Round(position.x / _gridSize) * _gridSize;
            float snappedZ = Mathf.Round(position.z / _gridSize) * _gridSize;
            return new Vector3(snappedX, position.y, snappedZ);
        }

        private bool IsValidDropPosition(Vector3 position)
        {
            // Check if the position is within valid drop zones
            // For now, just check if it's not too far from the original position
            float maxDistance = 50f; // Configurable max drag distance
            return Vector3.Distance(_originalPosition, position) <= maxDistance;
        }

        private Vector3 GetSurfacePosition(Vector3 position)
        {
            // Cast down to find the surface to place the object on
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, _dropLayers))
            {
                return hit.point;
            }

            // If no surface found, just remove the drag height offset
            return new Vector3(position.x, position.y - _dragHeight, position.z);
        }

        /// <summary>
        /// Cancel current drag operation
        /// </summary>
        public void CancelDrag()
        {
            if (!_isDragging) return;

            transform.position = _originalPosition;
            _isDragging = false;

            RestoreOriginalMaterial();

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
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

        #region Long Press Detection

        /// <summary>
        /// Start long press detection
        /// </summary>
        private void StartLongPressDetection(Vector2 screenPosition)
        {
            if (_longPressCoroutine != null)
                StopCoroutine(_longPressCoroutine);

            _isLongPressing = true;
            _longPressTriggered = false;
            _longPressStartPosition = screenPosition;
            _longPressCoroutine = StartCoroutine(LongPressCoroutine(screenPosition));
        }

        /// <summary>
        /// Stop long press detection
        /// </summary>
        private void StopLongPressDetection()
        {
            if (_longPressCoroutine != null)
            {
                StopCoroutine(_longPressCoroutine);
                _longPressCoroutine = null;
            }
            _isLongPressing = false;
        }

        /// <summary>
        /// Check if current pointer position is still within long press threshold
        /// </summary>
        private bool IsWithinLongPressThreshold(Vector2 currentScreenPosition)
        {
            return Vector2.Distance(_longPressStartPosition, currentScreenPosition) <= _longPressMoveThreshold;
        }

        /// <summary>
        /// Coroutine that handles long press timing
        /// </summary>
        private IEnumerator LongPressCoroutine(Vector2 screenPosition)
        {
            float elapsedTime = 0f;

            while (elapsedTime < _longPressDuration)
            {
                yield return null;
                elapsedTime += Time.unscaledDeltaTime;

                // Check if we moved too far
                if (!_isLongPressing)
                {
                    yield break;
                }
            }

            // Long press triggered
            if (_isLongPressing && !_longPressTriggered)
            {
                HandleLongPress(screenPosition);
            }
        }

        /// <summary>
        /// Handle long press event
        /// </summary>
        private void HandleLongPress(Vector2 screenPosition)
        {
            _longPressTriggered = true;
            OnItemLongPressed?.Invoke(this, screenPosition);

            // Show options form on long press
            ShowOptionsForm(screenPosition, true);
        }

        #endregion

        #region Options Form

        /// <summary>
        /// Show options form for this draggable item using FormSubmitPanel
        /// </summary>
        public void ShowOptionsForm(Vector2 screenPosition, bool fromLongPress = false)
        {
            var fieldDefinitions = new List<FormFieldDefinition>();

            // Create form fields for the draggable item options
            
            // Selection actions
            if (!_isSelected)
            {
                fieldDefinitions.Add(new FormFieldDefinition("select", "Select Item", "button", "Select")
                {
                    options = new Dictionary<string, object> { ["action"] = "select" }
                });
            }
            else
            {
                fieldDefinitions.Add(new FormFieldDefinition("deselect", "Deselect Item", "button", "Deselect")
                {
                    options = new Dictionary<string, object> { ["action"] = "deselect" }
                });
            }

            // Edit actions
            fieldDefinitions.Add(new FormFieldDefinition("copy", "Copy Item", "button", "Copy")
            {
                options = new Dictionary<string, object> { ["action"] = "copy" }
            });

            fieldDefinitions.Add(new FormFieldDefinition("delete", "Delete Item", "button", "Delete")
            {
                options = new Dictionary<string, object> { ["action"] = "delete" }
            });

            // Position actions
            fieldDefinitions.Add(new FormFieldDefinition("resetPosition", "Reset Position", "button", "Reset Position")
            {
                options = new Dictionary<string, object> { ["action"] = "resetPosition" }
            });

            fieldDefinitions.Add(new FormFieldDefinition("resetTransform", "Reset Transform", "button", "Reset All")
            {
                options = new Dictionary<string, object> { ["action"] = "resetTransform" }
            });

            // Transform control actions (only if enabled)
            if (_enableTransformControls)
            {
                // Current mode display
                string currentModeText = _currentTransformMode == TransformMode.None ? "None" : _currentTransformMode.ToString();
                fieldDefinitions.Add(new FormFieldDefinition("currentMode", $"Current Mode: {currentModeText}", "info"));

                if (_currentTransformMode == TransformMode.None)
                {
                    fieldDefinitions.Add(new FormFieldDefinition("enableMove", "Enable Move Mode", "button", "Move")
                    {
                        options = new Dictionary<string, object> { ["action"] = "enableMove" }
                    });
                    fieldDefinitions.Add(new FormFieldDefinition("enableRotate", "Enable Rotate Mode", "button", "Rotate")
                    {
                        options = new Dictionary<string, object> { ["action"] = "enableRotate" }
                    });
                    fieldDefinitions.Add(new FormFieldDefinition("enableScale", "Enable Scale Mode", "button", "Scale")
                    {
                        options = new Dictionary<string, object> { ["action"] = "enableScale" }
                    });
                }
                else
                {
                    fieldDefinitions.Add(new FormFieldDefinition("disableTransform", "Exit Transform Mode", "button", "Exit")
                    {
                        options = new Dictionary<string, object> { ["action"] = "disableTransform" }
                    });
                }
            }

            // Lock/Unlock toggle
            string lockText = _canDrag ? "Lock Item" : "Unlock Item";
            fieldDefinitions.Add(new FormFieldDefinition("toggleLock", lockText, "button", lockText)
            {
                options = new Dictionary<string, object> { ["action"] = "toggleLock" }
            });

            // Transform controls toggle
            string transformText = _enableTransformControls ? "Disable Transform Controls" : "Enable Transform Controls";
            fieldDefinitions.Add(new FormFieldDefinition("toggleTransformControls", transformText, "button", transformText)
            {
                options = new Dictionary<string, object> { ["action"] = "toggleTransformControls" }
            });

            #if UNITY_EDITOR
            fieldDefinitions.Add(new FormFieldDefinition("debugInfo", "Debug Info", "button", "Show Debug")
            {
                options = new Dictionary<string, object> { ["action"] = "debugInfo" }
            });
            #endif

            // Show the form
            string formTitle = $"Options: {name}";
            FormSubmitPanel.Instance.Show(formTitle, fieldDefinitions, HandleOptionsFormSubmit, null);
        }

        /// <summary>
        /// Handle form submission from the options form
        /// </summary>
        private void HandleOptionsFormSubmit(Dictionary<string, object> formData)
        {
            if (formData.ContainsKey("action"))
            {
                string action = formData["action"].ToString();
                
                switch (action)
                {
                    case "select":
                        SelectItem();
                        break;
                    case "deselect":
                        DeselectItem();
                        break;
                    case "copy":
                        CopyItem();
                        break;
                    case "delete":
                        DeleteItem();
                        break;
                    case "resetPosition":
                        ResetPosition();
                        break;
                    case "resetTransform":
                        ResetTransform();
                        break;
                    case "toggleLock":
                        ToggleLock();
                        break;
                    case "enableMove":
                        SetTransformMode(TransformMode.Move);
                        break;
                    case "enableRotate":
                        SetTransformMode(TransformMode.Rotate);
                        break;
                    case "enableScale":
                        SetTransformMode(TransformMode.Scale);
                        break;
                    case "disableTransform":
                        SetTransformMode(TransformMode.None);
                        break;
                    case "toggleTransformControls":
                        ToggleTransformControls();
                        break;
                    case "debugInfo":
                        #if UNITY_EDITOR
                        LogDebugInfo();
                        #endif
                        break;
                    default:
                        Debug.LogWarning($"Unknown action: {action}");
                        break;
                }
            }
        }

        #endregion

        #region Item Actions

        private void SelectItem()
        {
            // Use selection manager to handle selection
            // This will trigger the OnSelectionChanged event and update visual state
            TransformableSelectionManager.Instance.SelectItem(this);
        }

        private void DeselectItem()
        {
            // Use selection manager to handle deselection
            // This will trigger the OnSelectionChanged event and update visual state
            TransformableSelectionManager.Instance.DeselectItem(this);
        }

        private void CopyItem()
        {
            Debug.Log($"Copying item: {name}");
            // In a full implementation, this would copy the item to a clipboard or duplicate it
        }

        private void DeleteItem()
        {
            Debug.Log($"Deleting item: {name}");
            
            // Notify about deletion
            OnSelectionChanged?.Invoke(this, false);
            
            // Destroy the game object
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void ResetPosition()
        {
            if (_originalPosition != Vector3.zero)
            {
                transform.position = _originalPosition;
                Debug.Log($"Reset position for item: {name}");
            }
        }

        private void ToggleLock()
        {
            _canDrag = !_canDrag;
            string state = _canDrag ? "unlocked" : "locked";
            Debug.Log($"Item {name} is now {state}");
        }

        #if UNITY_EDITOR
        private void LogDebugInfo()
        {
            Debug.Log($"DraggableItem Debug Info for {name}:\n" +
                     $"  ObjectId: {_objectId}\n" +
                     $"  ObjectDataId: {_objectDataId}\n" +
                     $"  CanDrag: {_canDrag}\n" +
                     $"  IsDragging: {_isDragging}\n" +
                     $"  IsSelected: {_isSelected}\n" +
                     $"  Position: {transform.position}");
        }
        #endif

        #endregion

        #region Transform Control Methods

        /// <summary>
        /// Set the current transform mode
        /// </summary>
        public void SetTransformMode(TransformMode mode)
        {
            TransformMode previousMode = _currentTransformMode;
            _currentTransformMode = mode;
            _isInTransformMode = mode != TransformMode.None;

            // Update visual feedback
            UpdateTransformVisuals();
            
            // Update gizmo visuals
            UpdateGizmoVisuals();

            // Store original values when entering transform mode
            if (mode != TransformMode.None && previousMode == TransformMode.None)
            {
                _originalPosition = transform.position;
                _originalScale = transform.localScale;
                _originalRotation = transform.eulerAngles;
            }

            // Restore original material when exiting transform mode
            if (mode == TransformMode.None && previousMode != TransformMode.None)
            {
                RestoreOriginalMaterial();
            }

            OnTransformModeChanged?.Invoke(this, mode);
            Debug.Log($"Transform mode changed to: {mode}");
        }

        /// <summary>
        /// Start transform control operation
        /// </summary>
        private void StartTransformControl(Vector2 screenPosition)
        {
            if (_currentTransformMode == TransformMode.None) return;

            _lastInputPosition = screenPosition;
            
            // Store starting values
            switch (_currentTransformMode)
            {
                case TransformMode.Move:
                    _originalPosition = transform.position;
                    break;
                case TransformMode.Rotate:
                    _originalRotation = transform.eulerAngles;
                    break;
                case TransformMode.Scale:
                    _originalScale = transform.localScale;
                    break;
            }

            Debug.Log($"Started {_currentTransformMode} control");
        }

        /// <summary>
        /// Continue transform control operation
        /// </summary>
        private void ContinueTransformControl(Vector2 screenPosition)
        {
            if (_currentTransformMode == TransformMode.None) return;

            Vector2 deltaInput = screenPosition - _lastInputPosition;

            switch (_currentTransformMode)
            {
                case TransformMode.Move:
                    HandleMoveControl(deltaInput);
                    break;
                case TransformMode.Rotate:
                    HandleRotateControl(deltaInput);
                    break;
                case TransformMode.Scale:
                    HandleScaleControl(deltaInput);
                    break;
            }

            _lastInputPosition = screenPosition;
        }

        /// <summary>
        /// End transform control operation
        /// </summary>
        private void EndTransformControl(Vector2 screenPosition)
        {
            if (_currentTransformMode == TransformMode.None) return;

            Debug.Log($"Ended {_currentTransformMode} control");
            
            // Optional: Auto-exit transform mode after operation
            // SetTransformMode(TransformMode.None);
        }

        /// <summary>
        /// Handle move control input
        /// </summary>
        private void HandleMoveControl(Vector2 deltaInput)
        {
            if (_camera == null) return;

            // Convert screen delta to world movement
            float movementScale = 0.01f; // Adjust sensitivity
            Vector3 cameraRight = _camera.transform.right;
            Vector3 cameraUp = Vector3.up; // Keep movement on horizontal plane
            
            Vector3 movement = (cameraRight * deltaInput.x + cameraUp * deltaInput.y) * movementScale;
            transform.position += movement;
        }

        /// <summary>
        /// Handle rotation control input
        /// </summary>
        private void HandleRotateControl(Vector2 deltaInput)
        {
            // Convert screen delta to rotation
            float rotationX = -deltaInput.y * _rotationSensitivity; // Pitch (around X axis)
            float rotationY = deltaInput.x * _rotationSensitivity;  // Yaw (around Y axis)

            // Apply rotation relative to current rotation
            Vector3 currentRotation = transform.eulerAngles;
            currentRotation.x += rotationX;
            currentRotation.y += rotationY;

            transform.eulerAngles = currentRotation;
        }

        /// <summary>
        /// Handle scale control input
        /// </summary>
        private void HandleScaleControl(Vector2 deltaInput)
        {
            // Use Y delta for uniform scaling, or both X and Y for non-uniform
            float scaleChange = deltaInput.y * _scaleSensitivity;
            
            Vector3 newScale = transform.localScale + Vector3.one * scaleChange;
            
            // Clamp to min/max values
            newScale.x = Mathf.Clamp(newScale.x, _minScale.x, _maxScale.x);
            newScale.y = Mathf.Clamp(newScale.y, _minScale.y, _maxScale.y);
            newScale.z = Mathf.Clamp(newScale.z, _minScale.z, _maxScale.z);
            
            transform.localScale = newScale;
        }

        /// <summary>
        /// Update visual feedback based on current transform mode
        /// </summary>
        private void UpdateTransformVisuals()
        {
            if (_isDragging) return; // Don't override drag material

            switch (_currentTransformMode)
            {
                case TransformMode.None:
                    RestoreOriginalMaterial();
                    break;
                case TransformMode.Move:
                    if (_moveMaterial != null)
                        SetMaterial(_moveMaterial);
                    else if (_dragMaterial != null)
                        SetMaterial(_dragMaterial);
                    break;
                case TransformMode.Rotate:
                    if (_rotateMaterial != null)
                        SetMaterial(_rotateMaterial);
                    else if (_hoverMaterial != null)
                        SetMaterial(_hoverMaterial);
                    break;
                case TransformMode.Scale:
                    if (_scaleMaterial != null)
                        SetMaterial(_scaleMaterial);
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
                SetTransformMode(TransformMode.None);
                
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
            bool shouldShow = _isInTransformMode && _currentTransformMode != TransformMode.None;
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

            _xHandle?.Show(_currentTransformMode);
            _yHandle?.Show(_currentTransformMode);
            _zHandle?.Show(_currentTransformMode);
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
            public void Show(TransformMode mode)
            {
                bool showArrow = mode == TransformMode.Move;
                bool showRing = mode == TransformMode.Rotate;
                bool showBox = mode == TransformMode.Scale;

                // Hide all when mode is None
                if (mode == TransformMode.None)
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