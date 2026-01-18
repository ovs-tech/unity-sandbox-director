using UnityEngine;

namespace Systems.MiniTimeline.UI.Input
{
    /// <summary>
    /// Base interface for input interactions like tap, hold, drag, etc.
    /// </summary>
    public interface IInputInteraction
    {
        /// <summary>
        /// Called when the interaction starts (button press)
        /// </summary>
        /// <param name="screenPosition">Screen position where the interaction started</param>
        void OnInteractionStart(Vector2 screenPosition);

        /// <summary>
        /// Called continuously while the interaction is active
        /// </summary>
        /// <param name="screenPosition">Current screen position</param>
        /// <param name="deltaTime">Time since last update</param>
        void OnInteractionUpdate(Vector2 screenPosition, float deltaTime);

        /// <summary>
        /// Called when the interaction ends (button release)
        /// </summary>
        /// <param name="screenPosition">Screen position where the interaction ended</param>
        void OnInteractionEnd(Vector2 screenPosition);

        /// <summary>
        /// Called when the interaction is cancelled (e.g., by another interaction taking over)
        /// </summary>
        void OnInteractionCancel();

        /// <summary>
        /// Returns true if this interaction is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Returns the priority of this interaction (higher values take precedence)
        /// </summary>
        int Priority { get; }
    }
}