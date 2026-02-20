using UnityEngine;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Base class for all ScriptableObject-based placement visualizers.
    /// Provides inspector typing for serialized visualizer assets.
    /// </summary>
    public abstract class BasePlacementVisualizer : ScriptableObject, IPlacementVisualizer
    {
        public abstract void Initialize(GameObject ghostObject);
        public abstract void UpdateVisual(bool isValid);
        public abstract void Cleanup();
    }
}
