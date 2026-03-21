using UnityEngine;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Base component type for all placement input providers.
    /// </summary>
    public abstract class BaseInputProvider : MonoBehaviour, IInputProvider
    {
        public abstract Vector2 GetPointerPosition();
        public abstract bool IsPlaceActionTriggered();
        public abstract bool IsCancelActionTriggered();
        public abstract bool IsRotateActionTriggered();
        public abstract bool IsDeleteActionTriggered();

        public virtual bool IsMultiSelectModifierHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftControl) ||
                   UnityEngine.Input.GetKey(KeyCode.RightControl) ||
                   UnityEngine.Input.GetKey(KeyCode.LeftCommand) ||
                   UnityEngine.Input.GetKey(KeyCode.RightCommand);
        }
    }
}
