using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private float _dragThreshold = 10f;
        [SerializeField] private LayerMask _interactionLayers = -1;
        
        // Input Actions
        private InputAction _pointAction;
        private InputAction _clickAction;
        private InputAction _rightClickAction;
        private InputAction _dragAction;
        private InputAction _selectAction;
        private InputAction _deleteAction;
        
        // Touch-specific actions
        private InputAction _touchPressAction;
        private InputAction _touchPositionAction;
        
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
            if (_inputActions == null) return;
            
            // Get actions from the input asset
            _pointAction = _inputActions.FindAction("UI/Point");
            _clickAction = _inputActions.FindAction("UI/Click");
            _rightClickAction = _inputActions.FindAction("UI/RightClick");
            
            // Touch actions (if available)
            _touchPressAction = _inputActions.FindAction("UI/TouchPress");
            _touchPositionAction = _inputActions.FindAction("UI/TouchPosition");
            
            // Scene builder specific actions (we'll add these to the input actions)
            _selectAction = _inputActions.FindAction("SceneBuilder/Select");
            _deleteAction = _inputActions.FindAction("SceneBuilder/Delete");
            
            BindInputEvents();
        }
        
        private void BindInputEvents()
        {
            if (_pointAction != null)
            {
                _pointAction.performed += OnPointPerformed;
            }
            
            if (_clickAction != null)
            {
                _clickAction.started += OnClickStarted;
                _clickAction.performed += OnClickPerformed;
                _clickAction.canceled += OnClickCanceled;
            }
            
            if (_rightClickAction != null)
            {
                _rightClickAction.performed += OnRightClickPerformed;
            }
            
            if (_selectAction != null)
            {
                _selectAction.performed += OnSelectPerformed;
            }
            
            if (_deleteAction != null)
            {
                _deleteAction.performed += OnDeletePerformed;
            }
            
            // Touch input
            if (_touchPressAction != null && _touchPositionAction != null)
            {
                _touchPressAction.started += OnTouchStarted;
                _touchPressAction.performed += OnTouchPerformed;
                _touchPressAction.canceled += OnTouchCanceled;
            }
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
            
            if (_selectAction != null)
                _selectAction.performed -= OnSelectPerformed;
            
            if (_deleteAction != null)
                _deleteAction.performed -= OnDeletePerformed;
            
            if (_touchPressAction != null)
            {
                _touchPressAction.started -= OnTouchStarted;
                _touchPressAction.performed -= OnTouchPerformed;
                _touchPressAction.canceled -= OnTouchCanceled;
            }
        }
        
        private void OnPointPerformed(InputAction.CallbackContext context)
        {
            _currentPosition = context.ReadValue<Vector2>();
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
            
            if (hitObject != null)
            {
                _draggedObject = hitObject;
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
        
        private void OnSelectPerformed(InputAction.CallbackContext context)
        {
            var hitObject = GetObjectAtScreenPosition(_currentPosition);
            if (hitObject != null)
            {
                OnObjectSelect?.Invoke(hitObject);
            }
        }
        
        private void OnDeletePerformed(InputAction.CallbackContext context)
        {
            var hitObject = GetObjectAtScreenPosition(_currentPosition);
            if (hitObject != null)
            {
                OnObjectDelete?.Invoke(hitObject);
            }
        }
        
        // Touch input handlers
        private void OnTouchStarted(InputAction.CallbackContext context)
        {
            if (_touchPositionAction != null)
            {
                _currentPosition = _touchPositionAction.ReadValue<Vector2>();
                _dragStartPosition = _currentPosition;
                
                var hitObject = GetObjectAtScreenPosition(_currentPosition);
                if (hitObject != null)
                {
                    _draggedObject = hitObject;
                }
            }
        }
        
        private void OnTouchPerformed(InputAction.CallbackContext context)
        {
            if (_isDragging)
            {
                EndDrag();
            }
            else
            {
                OnClick?.Invoke(_currentPosition);
                
                var hitObject = GetObjectAtScreenPosition(_currentPosition);
                if (hitObject != null)
                {
                    OnObjectSelect?.Invoke(hitObject);
                }
            }
        }
        
        private void OnTouchCanceled(InputAction.CallbackContext context)
        {
            if (_isDragging)
            {
                EndDrag();
            }
        }
        
        private void Update()
        {
            CheckForDragStart();
        }
        
        private void CheckForDragStart()
        {
            if (!_isDragging && _draggedObject != null)
            {
                float dragDistance = Vector2.Distance(_dragStartPosition, _currentPosition);
                if (dragDistance > _dragThreshold)
                {
                    StartDrag();
                }
            }
        }
        
        private void StartDrag()
        {
            if (_draggedObject == null) return;
            
            _isDragging = true;
            OnDragStart?.Invoke(_draggedObject, _dragStartPosition);
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
                _hoveredObject = hitObject;
                OnObjectHover?.Invoke(_hoveredObject);
            }
        }
        
        private GameObject GetObjectAtScreenPosition(Vector2 screenPosition)
        {
            if (_camera == null) return null;
            
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _interactionLayers))
            {
                return hit.collider.gameObject;
            }
            
            return null;
        }
        
        public void Enable()
        {
            _inputActions?.Enable();
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
    }
}