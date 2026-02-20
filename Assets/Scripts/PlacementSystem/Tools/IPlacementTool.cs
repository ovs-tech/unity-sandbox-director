using UnityEngine;

namespace Systems.PlacementSystem.Tools
{
    /// <summary>
    /// Defines the tool lifecycle and per-frame hooks for placement system tools.
    /// </summary>
    public interface IPlacementTool
    {
        void OnEnter(PlacementToolContext context);
        void OnExit();
        void HandleInput();
        void Tick();
        void HandleSelection(GameObject selected);
    }
}
