using UnityEngine;
using System.Collections.Generic;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Interface for all sandbox tools.
    /// Tools handle input and update logic for specific modes (e.g. Selection, Placement).
    /// </summary>
    public interface ISandboxTool
    {
        /// <summary>
        /// Name of the tool for identification/UI
        /// </summary>
        string ToolName { get; }

        /// <summary>
        /// Called when the tool becomes active
        /// </summary>
        void OnEnter(ToolContext context);

        /// <summary>
        /// Called when the tool becomes inactive
        /// </summary>
        void OnExit();

        /// <summary>
        /// Called every frame while active
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// Optional: Draw gizmos for debugging or visualization
        /// </summary>
        void OnDrawGizmos();
    }
}
