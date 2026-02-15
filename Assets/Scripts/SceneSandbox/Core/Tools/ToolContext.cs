using UnityEngine;
using Systems.SceneSandbox.Core;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Context passed to tools, containing dependencies and shared state.
    /// </summary>
    public class ToolContext
    {
        public SandboxInputManager InputManager { get; private set; }
        public PlacementSystem PlacementSystem { get; private set; }
        public SelectionManager SelectionManager { get; private set; }
        public TransformController TransformController { get; private set; }
        public GridManager GridManager { get; private set; }
        public CameraRaycaster CameraRaycaster { get; private set; }
        public Transform StageArea { get; private set; }

        // You might add a reference to the Builder itself if needed for high-level commands,
        // but prefer specific managers to keep coupling low.
        public SceneSandboxBuilder Builder { get; private set; }

        public ToolContext(
            SandboxInputManager input,
            PlacementSystem placement,
            SelectionManager selection,
            TransformController transformController,
            GridManager grid,
            CameraRaycaster raycaster,
            Transform stageArea,
            SceneSandboxBuilder builder)
        {
            InputManager = input;
            PlacementSystem = placement;
            SelectionManager = selection;
            TransformController = transformController;
            GridManager = grid;
            CameraRaycaster = raycaster;
            StageArea = stageArea;
            Builder = builder;
        }
    }
}
