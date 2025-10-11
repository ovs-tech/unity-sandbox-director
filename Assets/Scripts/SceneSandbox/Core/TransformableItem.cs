using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SceneSandbox.Input;
using Core.UI.FormSubmit;

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
    /// Supports both mouse and touch input, selection, form-based options, and transform controls
    /// </summary>
    public class TransformableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
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

        // Transform control state
        [SerializeField] private TransformMode _currentTransformMode = TransformMode.None;
        private Vector3 _originalScale;
        private Vector3 _originalRotation;
        private Vector2 _lastInputPosition;
        private bool _isInTransformMode = false;

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
            InitializeComponents();
            
            // Debug EventSystem setup
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                
            }
            else
            {
                
            }
        }

        private void OnDestroy()
        {
            // Clean up long press coroutine
            StopLongPressDetection();
            
            // Unregister from selection manager
            if (TransformableSelectionManager.Instance != null)
            {
                TransformableSelectionManager.Instance.UnregisterDraggableItem(this);
            }

            // Unregister from transform control manager
            if (TransformableControlManager.Instance != null)
            {
                TransformableControlManager.Instance.UnregisterItem(this);
            }
        }

        private void Start()
        {
            _camera = Camera.main ?? FindFirstObjectByType<Camera>();

            // Ensure camera has PhysicsRaycaster for EventSystem 3D interaction
            if (_camera != null && _camera.GetComponent<PhysicsRaycaster>() == null)
            {
                
                _camera.gameObject.AddComponent<PhysicsRaycaster>();
            }

            if (string.IsNullOrEmpty(_objectId))
            {
                _objectId = System.Guid.NewGuid().ToString();
            }

            // Register with selection manager
            TransformableSelectionManager.Instance.RegisterDraggableItem(this);

            // Register with transform control manager
            if (_enableTransformControls)
            {
                TransformableControlManager.Instance.RegisterItem(this);
            }
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            
            
            if (!_canDrag) 
            {
                
                return;
            }

            // Check if we should handle transform mode instead of regular drag
            if (_isInTransformMode && _currentTransformMode != TransformMode.None)
            {
                StartTransformControl(eventData.position);
                return;
            }
            
            StartDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_canDrag) return;

            // Handle transform mode
            if (_isInTransformMode && _currentTransformMode != TransformMode.None)
            {
                ContinueTransformControl(eventData.position);
                return;
            }

            // Handle regular drag
            if (_isDragging)
            {
                ContinueDrag(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_canDrag) return;

            // Handle transform mode
            if (_isInTransformMode && _currentTransformMode != TransformMode.None)
            {
                EndTransformControl(eventData.position);
                return;
            }

            // Handle regular drag
            if (_isDragging)
            {
                EndDrag(eventData.position);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                StartLongPressDetection(eventData.position);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopLongPressDetection();
            
            if (_longPressTriggered)
            {
                // Reset long press triggered flag for next interaction
                _longPressTriggered = false;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            
            
            // Don't handle click if long press was triggered or if we're dragging
            if (!_longPressTriggered && !_isDragging)
            {
                OnItemClicked?.Invoke(this);

                // Handle selection on single click using selection manager
                TransformableSelectionManager.Instance.SelectItem(this);

                if (eventData.clickCount == 2)
                {
                    
                    OnItemSelected?.Invoke(this);
                }
            }
            
            // Handle right click for options form
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ShowOptionsForm(eventData.position, false);
            }

            // Reset long press triggered flag
            _longPressTriggered = false;
        }

        /// <summary>
        /// Start dragging the item programmatically
        /// </summary>
        public void StartDrag(Vector2 screenPosition)
        {
            
            
            if (!_canDrag || _isDragging) 
            {
                
                return;
            }

            

            _isDragging = true;
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
                // Unregister from transform control manager
                if (TransformableControlManager.Instance != null)
                {
                    TransformableControlManager.Instance.UnregisterItem(this);
                }
            }
            else
            {
                // Register with transform control manager
                if (TransformableControlManager.Instance != null)
                {
                    TransformableControlManager.Instance.RegisterItem(this);
                }
            }
            
            Debug.Log($"Transform controls {(_enableTransformControls ? "enabled" : "disabled")} for item: {name}");
        }

        #endregion
    }
}