using UnityEngine;
#if PAINT3D_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if PAINT3D_UGUI
using UnityEngine.EventSystems;
#endif

namespace Paint3D
{
    /// <summary>
    /// Platform agnostic pointer input (mouse, pen with pressure, touch) for both the Input System and the legacy
    /// Input Manager, so painters run unchanged on desktop, mobile and WebGL.
    /// </summary>
    public static class PaintPointerInput
    {
        public struct PointerState
        {
            public Vector2 Position;
            public bool Pressed;
            public float Pressure;
            public int PointerId;
        }

        public static bool TryGetPointer(out PointerState state)
        {
            state = new PointerState { Pressure = 1f, PointerId = -1 };
#if PAINT3D_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                state.Position = touchscreen.primaryTouch.position.ReadValue();
                state.Pressed = true;
                float pressure = touchscreen.primaryTouch.pressure.ReadValue();
                state.Pressure = pressure > 0f ? pressure : 1f;
                state.PointerId = touchscreen.primaryTouch.touchId.ReadValue();
                return true;
            }

            Pen pen = Pen.current;
            if (pen != null && pen.tip.isPressed)
            {
                state.Position = pen.position.ReadValue();
                state.Pressed = true;
                state.Pressure = pen.pressure.ReadValue();
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                state.Position = mouse.position.ReadValue();
                state.Pressed = mouse.leftButton.isPressed;
                return true;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                state.Position = touch.position;
                state.Pressed = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
                state.Pressure = touch.pressure > 0f ? touch.pressure / Mathf.Max(touch.maximumPossiblePressure, 1e-3f) : 1f;
                state.PointerId = touch.fingerId;
                return true;
            }

            if (Input.mousePresent)
            {
                state.Position = Input.mousePosition;
                state.Pressed = Input.GetMouseButton(0);
                return true;
            }
            return false;
#else
            return false;
#endif
        }

        public static bool UndoPressed() => ShortcutPressed(false);

        public static bool RedoPressed() => ShortcutPressed(true);

        /// <summary>True when the pointer is over a uGUI element (so painting should be ignored).</summary>
        public static bool IsPointerOverUI(int pointerId = -1)
        {
#if PAINT3D_UGUI
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }
            return pointerId >= 0 ? eventSystem.IsPointerOverGameObject(pointerId) : eventSystem.IsPointerOverGameObject();
#else
            return false;
#endif
        }

        // Ctrl/Cmd + Z undoes; Ctrl/Cmd + Y or Ctrl/Cmd + Shift + Z redoes.
        private static bool ShortcutPressed(bool redo)
        {
#if PAINT3D_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            bool modifier = keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed;
            bool shift = keyboard.shiftKey.isPressed;
            if (!modifier)
            {
                return false;
            }
            return redo
                ? keyboard.yKey.wasPressedThisFrame || (shift && keyboard.zKey.wasPressedThisFrame)
                : !shift && keyboard.zKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            bool modifier = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!modifier)
            {
                return false;
            }
            return redo
                ? Input.GetKeyDown(KeyCode.Y) || (shift && Input.GetKeyDown(KeyCode.Z))
                : !shift && Input.GetKeyDown(KeyCode.Z);
#else
            return false;
#endif
        }
    }
}
