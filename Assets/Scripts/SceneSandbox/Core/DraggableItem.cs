using UnityEngine;
using UnityEngine.EventSystems;
using SceneSandbox.Input;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Component for objects that can be dragged in the scene sandbox
    /// Supports both mouse and touch input
    /// </summary>
    public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
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

        [Header("Object Data")]
        [SerializeField] private string _objectId;
        [SerializeField] private string _objectDataId;

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

        // Events
        public System.Action<DraggableItem, Vector3> OnDragStarted;
        public System.Action<DraggableItem, Vector3> OnDragMoved;
        public System.Action<DraggableItem, Vector3> OnDragEnded;
        public System.Action<DraggableItem> OnItemClicked;
        public System.Action<DraggableItem> OnItemSelected;

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

        private void Awake()
        {
            InitializeComponents();
            
            // Debug EventSystem setup
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                
            }
            else
            {
                
            }
        }

        private void Start()
        {
            _camera = Camera.main ?? FindFirstObjectByType<Camera>();

            // Ensure camera has PhysicsRaycaster for EventSystem 3D interaction
            if (_camera != null && _camera.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
            {
                
                _camera.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            }

            if (string.IsNullOrEmpty(_objectId))
            {
                _objectId = System.Guid.NewGuid().ToString();
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

            
            StartDrag(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_canDrag || !_isDragging) return;

            ContinueDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_canDrag || !_isDragging) return;

            EndDrag(eventData.position);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            
            OnItemClicked?.Invoke(this);

            if (eventData.clickCount == 2)
            {
                
                OnItemSelected?.Invoke(this);
            }
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

        /// <summary>
        /// Debug method to test if EventSystem can detect this object
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void TestEventSystemDetection()
        {
            if (_camera == null)
            {
                
                return;
            }

            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                
                return;
            }

            var physicsRaycaster = _camera.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            if (physicsRaycaster == null)
            {
                
                return;
            }

            
        }
    }
}