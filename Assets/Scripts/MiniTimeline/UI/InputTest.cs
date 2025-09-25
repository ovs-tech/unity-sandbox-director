using UnityEngine;
using UnityEngine.InputSystem;

class InputTest : MonoBehaviour
{
    public InputActionAsset inputActions;

    private InputAction _point;
    private InputAction _click;
    private InputAction _scroll;
    private InputAction _submit;
    private InputAction _cancel;

    // Debug UI variables
    private string _lastPoint = "None";
    private string _lastClick = "None";
    private string _lastScroll = "None";
    private string _lastSubmit = "None";
    private string _lastCancel = "None";
    private float _lastActionTime = 0f;
    
    // Click state tracking for float value detection
    private bool wasClickPressed = false;

    void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogWarning("InputTest: No InputActionAsset assigned.");
            return;
        }

        _point = inputActions.FindAction("UI/Point");
        _click = inputActions.FindAction("UI/Click");
        _scroll = inputActions.FindAction("UI/ScrollWheel");
        _submit = inputActions.FindAction("UI/Submit");
        _cancel = inputActions.FindAction("UI/Cancel");

        if (_point != null) _point.Enable();
        if (_click != null) _click.Enable();
        if (_scroll != null) _scroll.Enable();
        if (_submit != null) _submit.Enable();
        if (_cancel != null) _cancel.Enable();

        if (_point != null) _point.performed += OnPoint;
        if (_click != null) _click.performed += OnClick;
        if (_scroll != null) _scroll.performed += OnScroll;
        if (_submit != null) _submit.performed += OnSubmit;
        if (_cancel != null) _cancel.performed += OnCancel;
    }

    void OnDisable()
    {
        if (_point != null) _point.Disable();
        if (_click != null) _click.Disable();
        if (_scroll != null) _scroll.Disable();
        if (_submit != null) _submit.Disable();
        if (_cancel != null) _cancel.Disable();

        if (_point != null) _point.performed -= OnPoint;
        if (_click != null) _click.performed -= OnClick;
        if (_scroll != null) _scroll.performed -= OnScroll;
        if (_submit != null) _submit.performed -= OnSubmit;
        if (_cancel != null) _cancel.performed -= OnCancel;
    }

    private void OnPoint(InputAction.CallbackContext context)
    {
        Vector2 position = context.ReadValue<Vector2>();
        _lastPoint = $"Point at {position:F1}";
        _lastActionTime = Time.time;
        Debug.Log(_lastPoint);
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        // Read button value (float)
        float clickValue = context.ReadValue<float>();
        bool isCurrentlyPressed = clickValue > 0.5f;
        
        // Detect press (transition from not pressed to pressed)
        if (isCurrentlyPressed && !wasClickPressed)
        {
            Vector2 position = _point != null ? _point.ReadValue<Vector2>() : Vector2.zero;
            _lastClick = $"Click PRESS at {position:F1}";
            _lastActionTime = Time.time;
            Debug.Log(_lastClick);
            wasClickPressed = true;
        }
        // Detect release (transition from pressed to not pressed)
        else if (!isCurrentlyPressed && wasClickPressed)
        {
            Vector2 position = _point != null ? _point.ReadValue<Vector2>() : Vector2.zero;
            _lastClick = $"Click RELEASE at {position:F1}";
            _lastActionTime = Time.time;
            Debug.Log(_lastClick);
            wasClickPressed = false;
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        Vector2 scrollDelta = context.ReadValue<Vector2>();
        _lastScroll = $"Scroll {scrollDelta:F2}";
        _lastActionTime = Time.time;
        Debug.Log(_lastScroll);
    }
    
    private void OnSubmit(InputAction.CallbackContext context)
    {
        _lastSubmit = "Submit pressed";
        _lastActionTime = Time.time;
        Debug.Log("Submit");
    }
    
    private void OnCancel(InputAction.CallbackContext context)
    {
        _lastCancel = "Cancel pressed";
        _lastActionTime = Time.time;
        Debug.Log("Cancel");
    }

    void OnGUI()
    {
        // Set up GUI style
        GUI.skin.box.fontSize = 14;
        GUI.skin.box.normal.textColor = Color.white;
        
        // Create a semi-transparent background
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(10, 10, 300, 180), "");
        GUI.color = Color.white;

        // Display input information
        GUI.Label(new Rect(20, 20, 280, 25), "INPUT DEBUG INFO");
        GUI.Label(new Rect(20, 45, 280, 20), $"Point: {_lastPoint}");
        GUI.Label(new Rect(20, 65, 280, 20), $"Click: {_lastClick}");
        GUI.Label(new Rect(20, 85, 280, 20), $"Scroll: {_lastScroll}");
        GUI.Label(new Rect(20, 105, 280, 20), $"Submit: {_lastSubmit}");
        GUI.Label(new Rect(20, 125, 280, 20), $"Cancel: {_lastCancel}");
        
        // Show time since last action
        float timeSinceAction = Time.time - _lastActionTime;
        GUI.Label(new Rect(20, 150, 280, 20), $"Last Action: {timeSinceAction:F1}s ago");
        
        // Show input actions status
        string statusText = inputActions != null ? "Input Actions: Active" : "Input Actions: NULL";
        GUI.Label(new Rect(20, 170, 280, 20), statusText);
    }
}