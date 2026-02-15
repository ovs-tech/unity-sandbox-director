using System.Collections.Generic;
using UnityEngine;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Manages the active tool state.
    /// </summary>
    public class SandboxToolManager : MonoBehaviour
    {
        [SerializeField] private bool _debugLogs = false;

        private ISandboxTool _activeTool;
        private ToolContext _context;
        private Dictionary<string, ISandboxTool> _tools = new Dictionary<string, ISandboxTool>();

        public ISandboxTool ActiveTool => _activeTool;

        public void Initialize(ToolContext context)
        {
            _context = context;
        }

        public void RegisterTool(ISandboxTool tool)
        {
            if (tool == null) return;
            if (_tools.ContainsKey(tool.ToolName))
            {
                Debug.LogWarning($"[SandboxToolManager] Tool '{tool.ToolName}' already registered.");
                return;
            }
            _tools.Add(tool.ToolName, tool);
        }

        public void ActivateTool(string toolName)
        {
            if (_tools.TryGetValue(toolName, out var tool))
            {
                SetActiveTool(tool);
            }
            else
            {
                Debug.LogError($"[SandboxToolManager] Tool '{toolName}' not found.");
            }
        }

        public void SetActiveTool(ISandboxTool tool)
        {
            if (_activeTool == tool) return;

            if (_activeTool != null)
            {
                if (_debugLogs) Debug.Log($"[SandboxToolManager] Exiting tool: {_activeTool.ToolName}");
                _activeTool.OnExit();
            }

            _activeTool = tool;

            if (_activeTool != null)
            {
                if (_debugLogs) Debug.Log($"[SandboxToolManager] Entering tool: {_activeTool.ToolName}");
                _activeTool.OnEnter(_context);
            }
        }

        private void Update()
        {
            if (_activeTool != null)
            {
                _activeTool.OnUpdate();
            }
        }

        private void OnDrawGizmos()
        {
            if (_activeTool != null)
            {
                _activeTool.OnDrawGizmos();
            }
        }
    }
}
