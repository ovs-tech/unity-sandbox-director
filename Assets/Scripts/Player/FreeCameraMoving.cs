using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraMoving : MonoBehaviour
{
    [Header("Camera Mode")]
    [SerializeField] private CameraMode cameraMode = CameraMode.FreeMove;
    
    [Header("Free Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
#pragma warning disable CS0414
    [SerializeField] private float moveSpeedIncrement = 2.5f;
#pragma warning restore CS0414
    [SerializeField] private float turboMultiplier = 3f;
    [SerializeField] private float lookSpeedMouse = 4f;
    [SerializeField] private float mouseSensitivityMultiplier = 0.01f;
    [SerializeField] private bool invertVerticalLook = false;
    [SerializeField] private bool invertHorizontalLook = false;
    
    [Header("Orbit Camera Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float orbitDistance = 10f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 50f;
    
    [Header("Movement Settings")]
    [SerializeField] private float orbitSpeed = 2f;
    [SerializeField] private float panSpeed = 2f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float smoothTime = 0.1f;
    
    [Header("Angle Constraints")]
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    
    public enum CameraMode
    {
        None,
        FreeMove,
        Orbit
    }
    
    // Input Actions
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction orbitAction;
    private InputAction panAction;
    private InputAction zoomAction;
    private InputAction resetCameraAction;
    private InputAction sprintAction;
    private InputAction verticalMoveAction;
    private InputAction switchModeAction;
    
    // Free movement variables
    private bool isSprinting = false;
    private bool isFreeLooking = false;
    
    // Camera control variables (for orbit mode)
    private Vector3 targetPosition;
    private float horizontalAngle = 0f;
    private float verticalAngle = 0f;
    private float currentDistance;
    
    // Initial values for reset
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialTargetPosition;
    private float initialDistance;
    private float initialHorizontalAngle;
    private float initialVerticalAngle;
    
    // Smooth movement variables
    private Vector3 velocityPosition;
    private float velocityDistance;
    private float velocityHorizontal;
    private float velocityVertical;
    
    // Mouse/input state
    private bool isOrbiting = false;
    private bool isPanning = false;
    
    private void Awake()
    {
        // Initialize input actions
        if (inputActions == null)
        {
            return;
        }
        
        // Find the camera actions
        moveAction = inputActions.FindAction("Camera/Move");
        lookAction = inputActions.FindAction("Camera/Look");
        sprintAction = inputActions.FindAction("Camera/Sprint");
        orbitAction = inputActions.FindAction("Camera/Orbit");
        panAction = inputActions.FindAction("Camera/Pan");
        zoomAction = inputActions.FindAction("Camera/Zoom");
        resetCameraAction = inputActions.FindAction("Camera/ResetCamera");
        switchModeAction = inputActions.FindAction("Camera/SwitchMode");
        
        // Create vertical movement action if it doesn't exist
        var actionMap = inputActions.FindActionMap("Player");
            if (actionMap != null)
            {
                verticalMoveAction = actionMap.FindAction("VerticalMove");
                if (verticalMoveAction == null)
                {
                    // If VerticalMove doesn't exist, we'll handle it differently
                }
            }
        
        if (moveAction == null || lookAction == null)
        {
            return;
        }
    }
    
    private void Start()
    {
        // Store initial values for reset functionality
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        if (cameraMode == CameraMode.Orbit)
        {
            // Initialize camera position and target for orbit mode
            if (target == null)
            {
                // Create a default target at the origin if none is assigned
                GameObject targetGO = new GameObject("Camera Target");
                target = targetGO.transform;
                target.position = Vector3.zero;
            }
            
            targetPosition = target.position;
            currentDistance = orbitDistance;
            
            // Store initial orbit values
            initialTargetPosition = targetPosition;
            initialDistance = currentDistance;
            initialHorizontalAngle = horizontalAngle;
            initialVerticalAngle = verticalAngle;
            
            // Set initial camera position
            UpdateCameraPosition();
        }
    }
    
    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
            
            // Subscribe to common input events
            if (lookAction != null)
            {
                lookAction.started += OnLookStarted;
                lookAction.canceled += OnLookCanceled;
            }
            
            if (sprintAction != null)
            {
                sprintAction.started += OnSprintStarted;
                sprintAction.canceled += OnSprintCanceled;
            }
            
            if (resetCameraAction != null)
            {
                resetCameraAction.performed += OnResetCamera;
            }
            
            if (switchModeAction != null)
            {
                switchModeAction.performed += OnSwitchMode;
            }
            
            // Subscribe to orbit-specific input events
            if (orbitAction != null)
            {
                orbitAction.started += OnOrbitStarted;
                orbitAction.canceled += OnOrbitCanceled;
            }
            
            if (panAction != null)
            {
                panAction.started += OnPanStarted;
                panAction.canceled += OnPanCanceled;
            }
            
            if (zoomAction != null)
            {
                zoomAction.performed += OnZoom;
            }
        }
    }
    
    private void OnDisable()
    {
        if (inputActions != null)
        {
            // Unsubscribe from input events
            if (lookAction != null)
            {
                lookAction.started -= OnLookStarted;
                lookAction.canceled -= OnLookCanceled;
            }
            
            if (sprintAction != null)
            {
                sprintAction.started -= OnSprintStarted;
                sprintAction.canceled -= OnSprintCanceled;
            }
            
            if (resetCameraAction != null)
            {
                resetCameraAction.performed -= OnResetCamera;
            }
            
            if (switchModeAction != null)
            {
                switchModeAction.performed -= OnSwitchMode;
            }
            
            if (orbitAction != null)
            {
                orbitAction.started -= OnOrbitStarted;
                orbitAction.canceled -= OnOrbitCanceled;
            }
            
            if (panAction != null)
            {
                panAction.started -= OnPanStarted;
                panAction.canceled -= OnPanCanceled;
            }
            
            if (zoomAction != null)
            {
                zoomAction.performed -= OnZoom;
            }
            
            inputActions.Disable();
        }
    }
    
    private void Update()
    {
        if (cameraMode == CameraMode.FreeMove)
        {
            HandleFreeMoveInput();
        }
        else if (cameraMode == CameraMode.Orbit)
        {
            HandleOrbitInput();
            UpdateCameraPosition();
        }
        // None mode: Do nothing, camera is completely disabled
    }
    
    private void HandleFreeMoveInput()
    {
        // Handle look input (mouse delta)
        if (isFreeLooking && lookAction != null)
        {
            Vector2 lookDelta = lookAction.ReadValue<Vector2>();
            
            // Apply invert settings
            float rotationX = lookDelta.x * lookSpeedMouse * mouseSensitivityMultiplier;
            float rotationY = lookDelta.y * lookSpeedMouse * mouseSensitivityMultiplier;
            
            if (invertHorizontalLook)
                rotationX = -rotationX;
            
            if (invertVerticalLook)
                rotationY = -rotationY;
            
            // Apply rotation
            float currentRotationX = transform.localEulerAngles.x;
            float newRotationY = transform.localEulerAngles.y + rotationX;
            
            // Handle vertical rotation clamping (similar to Unity's FreeCamera)
            float newRotationX = (currentRotationX - rotationY);
            if (currentRotationX <= 90.0f && newRotationX >= 0.0f)
                newRotationX = Mathf.Clamp(newRotationX, 0.0f, 90.0f);
            if (currentRotationX >= 270.0f)
                newRotationX = Mathf.Clamp(newRotationX, 270.0f, 360.0f);
            
            transform.localRotation = Quaternion.Euler(newRotationX, newRotationY, transform.localEulerAngles.z);
        }
        
        // Handle movement input
        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            float verticalInput = 0f;
            
            // Handle vertical movement (Q/E keys or Page Up/Down)
            if (verticalMoveAction != null)
            {
                Vector2 verticalVector = verticalMoveAction.ReadValue<Vector2>();
                verticalInput = verticalVector.y;
            }
            else
            {
                // Fallback: use keyboard input directly
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.qKey.isPressed) verticalInput = -1f;
                    if (Keyboard.current.eKey.isPressed) verticalInput = 1f;
                    if (Keyboard.current.pageUpKey.isPressed) verticalInput = 1f;
                    if (Keyboard.current.pageDownKey.isPressed) verticalInput = -1f;
                }
            }
            
            // Calculate movement
            float currentMoveSpeed = moveSpeed;
            if (isSprinting)
                currentMoveSpeed *= turboMultiplier;
            
            float deltaSpeed = Time.deltaTime * currentMoveSpeed;
            
            // Apply movement
            Vector3 movement = transform.forward * (deltaSpeed * moveInput.y) +
                              transform.right * (deltaSpeed * moveInput.x) +
                              Vector3.up * (deltaSpeed * verticalInput);
            
            transform.position += movement;
        }
    }
    
    private void HandleOrbitInput()
    {
        // Handle orbit input
        if (isOrbiting && orbitAction != null)
        {
            Vector2 orbitInput = orbitAction.ReadValue<Vector2>();
            
            horizontalAngle += orbitInput.x * orbitSpeed;
            verticalAngle -= orbitInput.y * orbitSpeed;
            
            // Clamp vertical angle
            verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
        }
        
        // Handle pan input
        if (isPanning && panAction != null)
        {
            Vector2 panInput = panAction.ReadValue<Vector2>();
            
            // Calculate pan movement in world space
            Vector3 right = transform.right;
            Vector3 up = Vector3.up;
            
            Vector3 panMovement = (-right * panInput.x + up * panInput.y) * panSpeed * Time.deltaTime;
            targetPosition += panMovement;
        }
    }
    
    private void UpdateCameraPosition()
    {
        // Smooth the angles and distance
        float targetHorizontalAngle = horizontalAngle;
        float targetVerticalAngle = verticalAngle;
        float targetDistance = currentDistance;
        
        horizontalAngle = Mathf.SmoothDampAngle(horizontalAngle, targetHorizontalAngle, ref velocityHorizontal, smoothTime);
        verticalAngle = Mathf.SmoothDampAngle(verticalAngle, targetVerticalAngle, ref velocityVertical, smoothTime);
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref velocityDistance, smoothTime);
        
        // Calculate camera position based on angles and distance
        Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        Vector3 offset = rotation * Vector3.back * currentDistance;
        
        Vector3 desiredPosition = targetPosition + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocityPosition, smoothTime);
        
        // Look at target
        transform.LookAt(targetPosition);
    }
    
    #region Input Event Handlers
    
    private void OnLookStarted(InputAction.CallbackContext context)
    {
        if (cameraMode == CameraMode.FreeMove)
        {
            isFreeLooking = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        if (cameraMode == CameraMode.FreeMove)
        {
            isFreeLooking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        isSprinting = true;
    }
    
    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }
    
    private void OnOrbitStarted(InputAction.CallbackContext context)
    {
        if (cameraMode == CameraMode.Orbit)
        {
            isOrbiting = true;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    private void OnOrbitCanceled(InputAction.CallbackContext context)
    {
        if (cameraMode == CameraMode.Orbit)
        {
            isOrbiting = false;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    private void OnPanStarted(InputAction.CallbackContext context)
    {
        if (cameraMode == CameraMode.Orbit)
        {
            isPanning = true;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    private void OnPanCanceled(InputAction.CallbackContext context)
    {
        if (cameraMode == CameraMode.Orbit)
        {
            isPanning = false;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    private void OnZoom(InputAction.CallbackContext context)
    {
        if (cameraMode == CameraMode.Orbit)
        {
            float zoomInput = context.ReadValue<float>();
            
            // Adjust zoom distance
            currentDistance -= zoomInput * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }
    
    private void OnResetCamera(InputAction.CallbackContext context)
    {
        if (cameraMode == CameraMode.FreeMove)
        {
            // Reset free camera to initial position and rotation
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
        else if (cameraMode == CameraMode.Orbit)
        {
            // Reset orbit camera to initial position and settings
            targetPosition = initialTargetPosition;
            currentDistance = initialDistance;
            horizontalAngle = initialHorizontalAngle;
            verticalAngle = initialVerticalAngle;
        }
        // None mode: Reset is not available
    }
    
    private void OnSwitchMode(InputAction.CallbackContext context)
    {
        // Cycle between camera modes: None -> FreeMove -> Orbit -> None
        switch (cameraMode)
        {
            case CameraMode.None:
                SetCameraMode(CameraMode.FreeMove);
                break;
            case CameraMode.FreeMove:
                // Only switch to orbit mode if we have a target
                if (target != null)
                {
                    SetCameraMode(CameraMode.Orbit);
                }
                else
                {
                    SetCameraMode(CameraMode.None);
                }
                break;
            case CameraMode.Orbit:
                SetCameraMode(CameraMode.None);
                break;
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Switch between camera modes
    /// </summary>
    /// <param name="mode">The camera mode to switch to</param>
    public void SetCameraMode(CameraMode mode)
    {
        cameraMode = mode;
        
        // Reset input states when switching modes
        isOrbiting = false;
        isPanning = false;
        isFreeLooking = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        
    }
    
    /// <summary>
    /// Set a new target for the camera to orbit around (Orbit mode only)
    /// </summary>
    /// <param name="newTarget">The new target transform</param>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null && cameraMode == CameraMode.Orbit)
        {
            target = newTarget;
            targetPosition = target.position;
        }
    }
    
    /// <summary>
    /// Set the camera distance from target (Orbit mode only)
    /// </summary>
    /// <param name="distance">The desired distance</param>
    public void SetDistance(float distance)
    {
        if (cameraMode == CameraMode.Orbit)
        {
            currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }
    
    /// <summary>
    /// Focus the camera on a specific position (Orbit mode only)
    /// </summary>
    /// <param name="focusPosition">Position to focus on</param>
    public void FocusOn(Vector3 focusPosition)
    {
        if (cameraMode == CameraMode.Orbit)
        {
            targetPosition = focusPosition;
        }
    }
    
    /// <summary>
    /// Set camera angles directly (Orbit mode only)
    /// </summary>
    /// <param name="horizontal">Horizontal angle in degrees</param>
    /// <param name="vertical">Vertical angle in degrees</param>
    public void SetAngles(float horizontal, float vertical)
    {
        if (cameraMode == CameraMode.Orbit)
        {
            horizontalAngle = horizontal;
            verticalAngle = Mathf.Clamp(vertical, minVerticalAngle, maxVerticalAngle);
        }
    }
    
    /// <summary>
    /// Set move speed for free movement mode
    /// </summary>
    /// <param name="speed">New movement speed</param>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
    }
    
    /// <summary>
    /// Get current camera mode
    /// </summary>
    /// <returns>Current camera mode</returns>
    public CameraMode GetCameraMode()
    {
        return cameraMode;
    }
    
    /// <summary>
    /// Set vertical look inversion for free movement mode
    /// </summary>
    /// <param name="invert">True to invert vertical look</param>
    public void SetInvertVerticalLook(bool invert)
    {
        invertVerticalLook = invert;
    }
    
    /// <summary>
    /// Set horizontal look inversion for free movement mode
    /// </summary>
    /// <param name="invert">True to invert horizontal look</param>
    public void SetInvertHorizontalLook(bool invert)
    {
        invertHorizontalLook = invert;
    }
    
    /// <summary>
    /// Get vertical look inversion setting
    /// </summary>
    /// <returns>True if vertical look is inverted</returns>
    public bool GetInvertVerticalLook()
    {
        return invertVerticalLook;
    }
    
    /// <summary>
    /// Get horizontal look inversion setting
    /// </summary>
    /// <returns>True if horizontal look is inverted</returns>
    public bool GetInvertHorizontalLook()
    {
        return invertHorizontalLook;
    }
    
    #endregion
    
    #region Debug and Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (cameraMode == CameraMode.Orbit && target != null)
        {
            // Draw target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
            
            // Draw camera orbit range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition, minDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPosition, maxDistance);
            
            // Draw line from camera to target
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        else if (cameraMode == CameraMode.FreeMove)
        {
            // Draw camera direction
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);
            
            // Draw initial position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(initialPosition, Vector3.one * 0.5f);
        }
    }
    
    #endregion
}
