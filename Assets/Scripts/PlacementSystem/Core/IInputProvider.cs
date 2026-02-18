using UnityEngine;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Abstracts input handling to allow switching between Legacy Input Manager and New Input System.
    /// Implements dependency inversion: high-level placement logic doesn't depend on specific input implementations.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Gets the current pointer position in screen space.
        /// </summary>
        Vector2 GetPointerPosition();

        /// <summary>
        /// Checks if the place action was triggered this frame (e.g., left mouse button down).
        /// </summary>
        bool IsPlaceActionTriggered();

        /// <summary>
        /// Checks if the cancel action was triggered this frame (e.g., right mouse button or Escape).
        /// </summary>
        bool IsCancelActionTriggered();

        /// <summary>
        /// Checks if the rotate action was triggered this frame (e.g., R key).
        /// </summary>
        bool IsRotateActionTriggered();
    }
}
