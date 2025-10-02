using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using SceneSandbox.Core;

namespace SceneSandbox.Input
{
    /// <summary>
    /// Interface for input handlers in the scene sandbox builder
    /// </summary>
    public interface IInputHandler
    {
        void Initialize(InputActionAsset inputActions);
        void Enable();
        void Disable();
        void Cleanup();
    }

    /// <summary>
    /// Input handler for scene sandbox builder operations
    /// Handles both mouse and touch input for drag and drop functionality
    /// </summary>
    public class SandboxInputHandler : MonoBehaviour, IInputHandler
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private float _dragThreshold = 3f; // Reduced for better responsiveness
        [SerializeField] private LayerMask _interactionLayers = -1;
        
        // Input Actions
        private InputAction _pointAction;
        private InputAction _clickAction;
        private InputAction _rightClickAction;
        
        // Placement actions
        private InputAction _placeAnchorAction;
        private InputAction _moveAction;
        private InputAction _rotateAction;
        private InputAction _scaleAction;
        
        // Drag state
        private bool _isDragging;
        private Vector2 _dragStartPosition;
        private Vector2 _currentPosition;
        private GameObject _draggedObject;
        private GameObject _hoveredObject;
        
        // Events
        public System.Action<Vector2> OnPointMove;
        public System.Action<Vector2> OnClick;
        public System.Action<Vector2> OnRightClick;
        public System.Action<GameObject, Vector2> OnDragStart;
        public System.Action<GameObject, Vector2> OnDragMove;
        public System.Action<GameObject, Vector2> OnDragEnd;
        public System.Action<GameObject> OnObjectSelect;
        public System.Action<GameObject> OnObjectHover;
        public System.Action<GameObject> OnObjectDelete;
        
        // Placement events
        public System.Action<Vector2> OnPlaceAnchor;
        public System.Action<Vector2> OnMove;
        public System.Action<Vector2> OnRotate;
        public System.Action<Vector2> OnScale;
        
        private Camera _camera;
        
        private void Awake()
        {
            _camera = Camera.main ?? FindFirstObjectByType<Camera>();
        }
        
        public void Initialize(InputActionAsset inputActions)
        {
            _inputActions = inputActions;
            SetupInputActions();
        }
        
        private void SetupInputActions()
        {
            if (_inputActions == null) 
            {
                Debug.LogError("[SandboxInputHandler] Input actions asset is null!");
                return;
            }
            
            Debug.Log($"[SandboxInputHandler] Setting up input actions from asset: {_inputActions.name}");
            
            // Get actions from the input asset
            _pointAction = _inputActions.FindAction("UI/Point");
            _clickAction = _inputActions.FindAction("UI/Click");
            _rightClickAction = _inputActions.FindAction("UI/RightClick");
            
            Debug.Log($"[SandboxInputHandler] Found actions - Point: {_pointAction != null}, Click: {_clickAction != null}, RightClick: {_rightClickAction != null}");
            
            // Placement actions
            _placeAnchorAction = _inputActions.FindAction("Placement/PlaceAnchor");
            _moveAction = _inputActions.FindAction("Placement/Move");
            _rotateAction = _inputActions.FindAction("Placement/Rotate");
            _scaleAction = _inputActions.FindAction("Placement/Scale");
            
            BindInputEvents();
        }
        
        private void BindInputEvents()
        {
            Debug.Log("[SandboxInputHandler] Binding input events...");
            
            if (_pointAction != null)
            {
                _pointAction.performed += OnPointPerformed;
                Debug.Log("[SandboxInputHandler] Bound Point action");
            }
            else
            {
                Debug.LogWarning("[SandboxInputHandler] Point action not found!");
            }
            
            if (_clickAction != null)
            {
                _clickAction.started += OnClickStarted;
                _clickAction.performed += OnClickPerformed;
                _clickAction.canceled += OnClickCanceled;
                Debug.Log("[SandboxInputHandler] Bound Click action events");
            }
            else
            {
                Debug.LogWarning("[SandboxInputHandler] Click action not found!");
            }
            
            if (_rightClickAction != null)
            {
                _rightClickAction.performed += OnRightClickPerformed;
                Debug.Log("[SandboxInputHandler] Bound RightClick action");
            }
            else
            {
                Debug.LogWarning("[SandboxInputHandler] RightClick action not found!");
            }
            
            // Placement actions
            if (_placeAnchorAction != null)
            {
                _placeAnchorAction.performed += OnPlaceAnchorPerformed;
            }
            
            if (_moveAction != null)
            {
                _moveAction.performed += OnMovePerformed;
            }
            
            if (_rotateAction != null)
            {
                _rotateAction.performed += OnRotatePerformed;
            }
            
            if (_scaleAction != null)
            {
                _scaleAction.performed += OnScalePerformed;
            }
            
            Debug.Log("[SandboxInputHandler] Input event binding complete");
        }
        
        private void UnbindInputEvents()
        {
            if (_pointAction != null)
                _pointAction.performed -= OnPointPerformed;
            
            if (_clickAction != null)
            {
                _clickAction.started -= OnClickStarted;
                _clickAction.performed -= OnClickPerformed;
                _clickAction.canceled -= OnClickCanceled;
            }
            
            if (_rightClickAction != null)
                _rightClickAction.performed -= OnRightClickPerformed;
            
            // Placement actions
            if (_placeAnchorAction != null)
                _placeAnchorAction.performed -= OnPlaceAnchorPerformed;
            
            if (_moveAction != null)
                _moveAction.performed -= OnMovePerformed;
            
            if (_rotateAction != null)
                _rotateAction.performed -= OnRotatePerformed;
            
            if (_scaleAction != null)
                _scaleAction.performed -= OnScalePerformed;
        }
        
        private void OnPointPerformed(InputAction.CallbackContext context)
        {
            Vector2 previousPosition = _currentPosition;
            _currentPosition = context.ReadValue<Vector2>();
            
            // Log movement for debugging when we have a dragged object
            if (_draggedObject != null)
            {
                float distance = Vector2.Distance(previousPosition, _currentPosition);
                if (distance > 1f)
                {
                    Debug.Log($"[SandboxInputHandler] OnPointPerformed - Movement: {distance:F2}px, DraggedObject: {_draggedObject.name}, IsDragging: {_isDragging}");
                }
            }
            
            OnPointMove?.Invoke(_currentPosition);
            
            UpdateHoveredObject();
            
            // Handle drag movement
            if (_isDragging && _draggedObject != null)
            {
                OnDragMove?.Invoke(_draggedObject, _currentPosition);
            }
        }
        
        private void OnClickStarted(InputAction.CallbackContext context)
        {
            _dragStartPosition = _currentPosition;
            var hitObject = GetObjectAtScreenPosition(_currentPosition);
            
            Debug.Log($"[SandboxInputHandler] OnClickStarted - Position: {_currentPosition}, Hit Object: {hitObject?.name}");
            
            if (hitObject != null)
            {
                var draggableItem = hitObject.GetComponent<DraggableItem>();
                if (draggableItem != null && draggableItem.CanDrag)
                {
                    _draggedObject = hitObject;
                    Debug.Log($"[SandboxInputHandler] Set dragged object to: {_draggedObject.name}");
                    
                    // Immediately trigger object selection to enable indicator
                    OnObjectSelect?.Invoke(_draggedObject);
                    
                    // Check for immediate drag start if the object is already moving significantly
                    StartCoroutine(CheckForImmediateDrag());
                }
                else
                {
                    Debug.Log($"[SandboxInputHandler] Hit object {hitObject.name} is not draggable or cannot be dragged");
                    _draggedObject = null;
                    
                    // Still trigger selection for non-draggable objects
                    OnObjectSelect?.Invoke(hitObject);
                }
            }
            else
            {
                Debug.Log($"[SandboxInputHandler] No object hit - clearing dragged object");
                _draggedObject = null;
            }
        }
        
        private void OnClickPerformed(InputAction.CallbackContext context)
        {
            if (_isDragging)
            {
                // End drag
                EndDrag();
            }
            else
            {
                // Regular click
                OnClick?.Invoke(_currentPosition);
                
                var hitObject = GetObjectAtScreenPosition(_currentPosition);
                if (hitObject != null)
                {
                    OnObjectSelect?.Invoke(hitObject);
                }
            }
        }
        
        private void OnClickCanceled(InputAction.CallbackContext context)
        {
            if (_isDragging)
            {
                EndDrag();
            }
        }
        
        private void OnRightClickPerformed(InputAction.CallbackContext context)
        {
            OnRightClick?.Invoke(_currentPosition);
        }
        
        // Placement action handlers
        private void OnPlaceAnchorPerformed(InputAction.CallbackContext context)
        {
            OnPlaceAnchor?.Invoke(_currentPosition);
        }
        
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            OnMove?.Invoke(_currentPosition);
        }
        
        private void OnRotatePerformed(InputAction.CallbackContext context)
        {
            OnRotate?.Invoke(_currentPosition);
        }
        
        private void OnScalePerformed(InputAction.CallbackContext context)
        {
            OnScale?.Invoke(_currentPosition);
        }
        
        private void Update()
        {
            CheckForDragStart();
            
            // Also check for immediate drag start on objects that support EventSystem dragging
            CheckForEventSystemDragStart();
        }
        
        /// <summary>
        /// Coroutine to check for immediate drag when touch/click starts moving quickly
        /// </summary>
        private System.Collections.IEnumerator CheckForImmediateDrag()
        {
            if (_draggedObject == null) yield break;
            
            float checkDuration = 0.1f; // Check for 100ms
            float elapsed = 0f;
            Vector2 startPos = _currentPosition;
            
            while (elapsed < checkDuration && _draggedObject != null && !_isDragging)
            {
                elapsed += Time.unscaledDeltaTime;
                
                float distance = Vector2.Distance(startPos, _currentPosition);
                if (distance > _dragThreshold)
                {
                    Debug.Log($"[SandboxInputHandler] Immediate drag detected! Distance: {distance:F2}");
                    StartDrag();
                    yield break;
                }
                
                yield return null;
            }
        }
        
        private void CheckForDragStart()
        {
            if (!_isDragging && _draggedObject != null)
            {
                float dragDistance = Vector2.Distance(_dragStartPosition, _currentPosition);
                
                // More frequent logging for debugging
                if (dragDistance > 0.5f)
                {
                    Debug.Log($"[SandboxInputHandler] CheckForDragStart - Distance: {dragDistance:F2}, Threshold: {_dragThreshold}, Object: {_draggedObject.name}");
                }
                
                if (dragDistance > _dragThreshold)
                {
                    Debug.Log($"[SandboxInputHandler] Drag threshold exceeded, starting drag");
                    StartDrag();
                }
            }
        }
        
        /// <summary>
        /// Alternative drag detection method that checks for EventSystem-based drags
        /// This works alongside the DraggableItem EventSystem implementation
        /// </summary>
        private void CheckForEventSystemDragStart()
        {
            // If we have a dragged object and it has a DraggableItem component that's already dragging,
            // we should start our indicator system too
            if (!_isDragging && _draggedObject != null)
            {
                var draggableItem = _draggedObject.GetComponent<DraggableItem>();
                if (draggableItem != null && draggableItem.IsDragging)
                {
                    Debug.Log($"[SandboxInputHandler] Detected EventSystem drag on {_draggedObject.name}, starting indicator system");
                    _isDragging = true;
                    OnDragStart?.Invoke(_draggedObject, _currentPosition);
                }
            }
        }
        
        private void StartDrag()
        {
            if (_draggedObject == null) return;
            
            Debug.Log($"[SandboxInputHandler] StartDrag called for object: {_draggedObject.name}");
            
            // Ensure the object has a DraggableItem component and can be dragged
            var draggableItem = _draggedObject.GetComponent<DraggableItem>();
            if (draggableItem == null || !draggableItem.CanDrag)
            {
                Debug.LogWarning($"[SandboxInputHandler] Cannot start drag - object {_draggedObject.name} is not draggable");
                _draggedObject = null;
                return;
            }
            
            _isDragging = true;
            OnDragStart?.Invoke(_draggedObject, _dragStartPosition);
            
            Debug.Log($"[SandboxInputHandler] OnDragStart event invoked with object: {_draggedObject.name} at position: {_dragStartPosition}");
        }
        
        private void EndDrag()
        {
            if (!_isDragging || _draggedObject == null) return;
            
            OnDragEnd?.Invoke(_draggedObject, _currentPosition);
            _isDragging = false;
            _draggedObject = null;
        }
        
        private void UpdateHoveredObject()
        {
            var hitObject = GetObjectAtScreenPosition(_currentPosition);
            
            if (hitObject != _hoveredObject)
            {
                // Log hover changes for debugging
                if (hitObject != null)
                {
                    var draggableItem = hitObject.GetComponent<DraggableItem>();
                    Debug.Log($"[SandboxInputHandler] Hovering over: {hitObject.name}, Draggable: {draggableItem != null && draggableItem.CanDrag}");
                }
                else if (_hoveredObject != null)
                {
                    Debug.Log($"[SandboxInputHandler] No longer hovering over: {_hoveredObject.name}");
                }
                
                _hoveredObject = hitObject;
                OnObjectHover?.Invoke(_hoveredObject);
            }
        }
        
        private GameObject GetObjectAtScreenPosition(Vector2 screenPosition)
        {
            if (_camera == null) 
            {
                return null;
            }
            
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _interactionLayers))
            {
                
                // Check if the hit object has a DraggableItem component
                var draggableItem = hit.collider.GetComponent<DraggableItem>();
                if (draggableItem != null)
                {
                    Debug.Log($"[SandboxInputHandler] Hit object has DraggableItem component, can drag: {draggableItem.CanDrag}");
                }
                else
                {
                    Debug.Log($"[SandboxInputHandler] Hit object does not have DraggableItem component");
                }
                
                return hit.collider.gameObject;
            }
            
            Debug.Log($"[SandboxInputHandler] Raycast missed - no objects found with layer mask: {_interactionLayers}");
            return null;
        }
        
        public void Enable()
        {
            if (_inputActions != null)
            {
                _inputActions.Enable();
                Debug.Log($"[SandboxInputHandler] Input actions enabled: {_inputActions.name}");
                
                // Debug enabled actions
                foreach (var map in _inputActions.actionMaps)
                {
                    Debug.Log($"[SandboxInputHandler] Action map '{map.name}' enabled: {map.enabled}");
                }
            }
            else
            {
                Debug.LogError("[SandboxInputHandler] Cannot enable - input actions are null!");
            }
        }
        
        public void Disable()
        {
            _inputActions?.Disable();
        }
        
        public void Cleanup()
        {
            UnbindInputEvents();
            Disable();
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
        
        /// <summary>
        /// Public method to manually trigger drag start for testing/debugging
        /// </summary>
        public void ManualStartDrag(GameObject obj, Vector2 screenPosition)
        {
            if (obj == null) return;
            
            Debug.Log($"[SandboxInputHandler] Manual drag start triggered for {obj.name}");
            _draggedObject = obj;
            _dragStartPosition = screenPosition;
            _currentPosition = screenPosition;
            _isDragging = true;
            OnDragStart?.Invoke(_draggedObject, _dragStartPosition);
        }
        
        /// <summary>
        /// Public method to manually trigger object selection
        /// </summary>
        public void ManualSelectObject(GameObject obj)
        {
            if (obj == null) return;
            
            Debug.Log($"[SandboxInputHandler] Manual object selection triggered for {obj.name}");
            OnObjectSelect?.Invoke(obj);
        }
        
        /// <summary>
        /// Debug method to test drag detection at current mouse position
        /// </summary>
        public void TestDragDetectionAtCurrentPosition()
        {
            Debug.Log($"[SandboxInputHandler] Testing drag detection at current position: {_currentPosition}");
            
            var hitObject = GetObjectAtScreenPosition(_currentPosition);
            if (hitObject != null)
            {
                Debug.Log($"[SandboxInputHandler] Found object at current position: {hitObject.name}");
                
                var draggableItem = hitObject.GetComponent<DraggableItem>();
                if (draggableItem != null)
                {
                    Debug.Log($"[SandboxInputHandler] Object has DraggableItem - CanDrag: {draggableItem.CanDrag}, IsDragging: {draggableItem.IsDragging}");
                    
                    // Trigger selection
                    OnObjectSelect?.Invoke(hitObject);
                }
                else
                {
                    Debug.Log($"[SandboxInputHandler] Object does not have DraggableItem component");
                }
            }
            else
            {
                Debug.Log($"[SandboxInputHandler] No object found at current position");
            }
        }
    }
}