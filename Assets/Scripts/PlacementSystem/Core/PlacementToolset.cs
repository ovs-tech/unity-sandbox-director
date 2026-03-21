using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.PlacementSystem.Tools;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// ScriptableObject channel for tool activation requests.
    /// Usage: toolset.SetActiveTool("selection").
    /// </summary>
    [CreateAssetMenu(menuName = "Placement System/Toolset")]
    public class PlacementToolset : ScriptableObject
    {
        [Serializable]
        public class ToolEntry
        {
            public PlacementController.PlacementToolType ToolType;
            [Tooltip("ScriptableObject implementing IPlacementTool")]
            public ScriptableObject ToolAsset;
            [Tooltip("Tools that must run before this one")]
            public List<PlacementController.PlacementToolType> Dependencies = new List<PlacementController.PlacementToolType>();

            public IPlacementTool AsPlacementTool => ToolAsset as IPlacementTool;
        }

        [SerializeField, Tooltip("Registered tools and dependency graph")]
        private List<ToolEntry> _tools = new List<ToolEntry>();

        public event Action<PlacementController.PlacementToolType> OnSetActiveToolRequested;

        public IReadOnlyList<ToolEntry> Tools => _tools;

        public void SetActiveTool(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                Debug.LogWarning("PlacementToolset: tool name is null or empty.");
                return;
            }

            if (!Enum.TryParse(toolName.Trim(), true, out PlacementController.PlacementToolType parsedTool))
            {
                Debug.LogWarning($"PlacementToolset: unknown tool '{toolName}'.");
                return;
            }

            SetActiveTool(parsedTool);
        }

        public void SetActiveTool(PlacementController.PlacementToolType toolType)
        {
            OnSetActiveToolRequested?.Invoke(toolType);
        }

        public bool TryGetTool(PlacementController.PlacementToolType toolType, out IPlacementTool tool)
        {
            tool = null;
            var byType = BuildMap();
            if (!byType.TryGetValue(toolType, out var entry))
                return false;

            tool = entry.AsPlacementTool;
            if (tool == null)
            {
                Debug.LogWarning($"PlacementToolset: entry '{toolType}' is missing a valid IPlacementTool asset.");
                return false;
            }

            return true;
        }

        public bool TryBuildPipeline(PlacementController.PlacementToolType activeTool, out PlacementController.PlacementToolType[] pipeline)
        {
            pipeline = Array.Empty<PlacementController.PlacementToolType>();

            var byType = BuildMap();
            if (!byType.ContainsKey(activeTool))
                return false;

            var ordered = new List<PlacementController.PlacementToolType>();
            var visited = new HashSet<PlacementController.PlacementToolType>();
            var recursion = new HashSet<PlacementController.PlacementToolType>();

            if (!ResolveDependencies(activeTool, byType, visited, recursion, ordered))
                return false;

            pipeline = ordered.ToArray();
            return pipeline.Length > 0;
        }

        private Dictionary<PlacementController.PlacementToolType, ToolEntry> BuildMap()
        {
            var map = new Dictionary<PlacementController.PlacementToolType, ToolEntry>();
            for (int i = 0; i < _tools.Count; i++)
            {
                var entry = _tools[i];
                if (entry == null)
                    continue;

                map[entry.ToolType] = entry;
            }

            return map;
        }

        private bool ResolveDependencies(
            PlacementController.PlacementToolType tool,
            Dictionary<PlacementController.PlacementToolType, ToolEntry> byType,
            HashSet<PlacementController.PlacementToolType> visited,
            HashSet<PlacementController.PlacementToolType> recursion,
            List<PlacementController.PlacementToolType> ordered)
        {
            if (visited.Contains(tool))
                return true;

            if (!byType.TryGetValue(tool, out var entry))
            {
                Debug.LogWarning($"PlacementToolset: missing tool entry for dependency '{tool}'.");
                return false;
            }

            if (!recursion.Add(tool))
            {
                Debug.LogWarning($"PlacementToolset: cyclic dependency detected at '{tool}'.");
                return false;
            }

            for (int i = 0; i < entry.Dependencies.Count; i++)
            {
                if (!ResolveDependencies(entry.Dependencies[i], byType, visited, recursion, ordered))
                    return false;
            }

            recursion.Remove(tool);
            visited.Add(tool);
            ordered.Add(tool);
            return true;
        }
    }
}