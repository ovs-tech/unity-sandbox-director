using UnityEngine;
using Systems.SceneSandbox.Core;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Default tool for selecting and transforming objects.
    /// </summary>
    public class SelectionTool : ISandboxTool
    {
        public string ToolName => "Selection";

        private ToolContext _context;
        private Vector2 _lastPointerPos;
        private bool _isDragging;

        public void OnEnter(ToolContext context)
        {
            _context = context;

            // Subscribe to inputs
            _context.InputManager.OnPointerDown += OnPointerDown;
            _context.InputManager.OnPointerDrag += OnPointerDrag;
            _context.InputManager.OnPointerUp += OnPointerUp;
            _context.InputManager.OnPointerMoved += OnPointerMove;

            // Hotkeys
            _context.InputManager.OnMoveHotkey += OnMoveHotkey;
            _context.InputManager.OnRotateHotkey += OnRotateHotkey;
            _context.InputManager.OnScaleHotkey += OnScaleHotkey;
            _context.InputManager.OnExitAllModes += OnExitAllModes;
        }

        public void OnExit()
        {
            if (_context != null)
            {
                // Unsubscribe from inputs
                _context.InputManager.OnPointerDown -= OnPointerDown;
                _context.InputManager.OnPointerDrag -= OnPointerDrag;
                _context.InputManager.OnPointerUp -= OnPointerUp;
                _context.InputManager.OnPointerMoved -= OnPointerMove;

                // Hotkeys
                _context.InputManager.OnMoveHotkey -= OnMoveHotkey;
                _context.InputManager.OnRotateHotkey -= OnRotateHotkey;
                _context.InputManager.OnScaleHotkey -= OnScaleHotkey;
                _context.InputManager.OnExitAllModes -= OnExitAllModes;

                // Ensure transform modes are exited when leaving the tool
                _context.SelectionManager.ExitAllTransformModes();
            }
        }

        public void OnUpdate()
        {
            // Hover logic handled via OnPointerMove event
        }

        public void OnDrawGizmos() { }

        private void OnPointerMove(Vector2 screenPos)
        {
            // Update hover
            _context.SelectionManager.UpdateHoverDetection(screenPos);
            _lastPointerPos = screenPos;
        }

        private void OnPointerDown(Vector2 screenPos)
        {
            _lastPointerPos = screenPos;
            _isDragging = false;

            // Raycast for selection
            TransformableItem item = _context.SelectionManager.RaycastForItem(screenPos);

            // Modifiers (shift/ctrl) would go here. Assuming single select for now.
            if (item != null)
            {
                _context.SelectionManager.SelectItem(item, false, true);
            }
            else
            {
                 // If clicking empty space, clear selection
                 _context.SelectionManager.ClearSelection();
            }
        }

        private void OnPointerDrag(Vector2 screenPos, float distance)
        {
            _isDragging = true;

            // Calculate delta
            Vector2 delta = screenPos - _lastPointerPos;

            // Apply transform if active
            if (_context.TransformController.CurrentMode != TransformModeType.None)
            {
                _context.TransformController.ApplyTransformDelta(delta);
            }

            _lastPointerPos = screenPos;
        }

        private void OnPointerUp(Vector2 screenPos)
        {
            _isDragging = false;
        }

        private void OnMoveHotkey()
        {
            _context.TransformController.SetTransformMode(TransformModeType.Position);
        }

        private void OnRotateHotkey()
        {
            _context.TransformController.SetTransformMode(TransformModeType.Rotation);
        }

        private void OnScaleHotkey()
        {
            _context.TransformController.SetTransformMode(TransformModeType.Scale);
        }

        private void OnExitAllModes()
        {
            _context.SelectionManager.ExitAllTransformModes();
        }
    }
}
