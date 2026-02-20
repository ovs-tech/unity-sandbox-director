using UnityEngine;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Interface for handling visual feedback during placement.
    /// Decouples visual representation from placement logic, allowing different visual styles.
    /// </summary>
    public interface IPlacementVisualizer
    {
        /// <summary>
        /// Initializes the visualizer with the ghost object.
        /// </summary>
        /// <param name="ghostObject">The preview object to visualize</param>
        void Initialize(GameObject ghostObject);

        /// <summary>
        /// Updates the visual state based on placement validity.
        /// </summary>
        /// <param name="isValid">Whether the current placement is valid</param>
        void UpdateVisual(bool isValid);

        /// <summary>
        /// Cleans up visualizer resources when placement ends.
        /// </summary>
        void Cleanup();
    }
}
