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
            public string ToolType;
            [Tooltip("ScriptableObject implementing IPlacementTool")]
            public ScriptableObject ToolAsset;
            [Tooltip("Tools that must run before this one")]
            public List<string> Dependencies = new List<string>();

            public IPlacementTool AsPlacementTool => ToolAsset as IPlacementTool;
        }

        [SerializeField, Tooltip("Registered tools and dependency graph")]
        private List<ToolEntry> _tools = new List<ToolEntry>();

        public event Action<string> OnSetActiveToolRequested;

        public IReadOnlyList<ToolEntry> Tools => _tools;

        public void SetActiveTool(string toolName)
        {
            var normalizedToolName = NormalizeToolName(toolName);
            if (string.IsNullOrEmpty(normalizedToolName))
            {
                Debug.LogWarning("PlacementToolset: tool name is null or empty.");
                return;
            }

            OnSetActiveToolRequested?.Invoke(normalizedToolName);
        }

        public bool TryGetTool(string toolType, out IPlacementTool tool)
        {
            tool = null;
            var normalizedToolType = NormalizeToolName(toolType);
            if (string.IsNullOrEmpty(normalizedToolType))
                return false;

            var byType = BuildMap();
            if (!byType.TryGetValue(normalizedToolType, out var entry))
                return false;

            tool = entry.AsPlacementTool;
            if (tool == null)
            {
                Debug.LogWarning($"PlacementToolset: entry '{normalizedToolType}' is missing a valid IPlacementTool asset.");
                return false;
            }

            return true;
        }

        public bool TryBuildPipeline(string activeTool, out string[] pipeline)
        {
            pipeline = Array.Empty<string>();

            var normalizedActiveTool = NormalizeToolName(activeTool);
            if (string.IsNullOrEmpty(normalizedActiveTool))
                return false;

            var byType = BuildMap();
            if (!byType.ContainsKey(normalizedActiveTool))
                return false;

            var ordered = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var recursion = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!ResolveDependencies(normalizedActiveTool, byType, visited, recursion, ordered))
                return false;

            pipeline = ordered.ToArray();
            return pipeline.Length > 0;
        }

        private Dictionary<string, ToolEntry> BuildMap()
        {
            var map = new Dictionary<string, ToolEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _tools.Count; i++)
            {
                var entry = _tools[i];
                if (entry == null)
                    continue;

                var normalizedToolType = NormalizeToolName(entry.ToolType);
                if (string.IsNullOrEmpty(normalizedToolType))
                    continue;

                map[normalizedToolType] = entry;
            }

            return map;
        }

        private bool ResolveDependencies(
            string tool,
            Dictionary<string, ToolEntry> byType,
            HashSet<string> visited,
            HashSet<string> recursion,
            List<string> ordered)
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
                var dependency = NormalizeToolName(entry.Dependencies[i]);
                if (string.IsNullOrEmpty(dependency))
                    continue;

                if (!ResolveDependencies(dependency, byType, visited, recursion, ordered))
                    return false;
            }

            recursion.Remove(tool);
            visited.Add(tool);
            ordered.Add(tool);
            return true;
        }

        private static string NormalizeToolName(string toolName)
        {
            return string.IsNullOrWhiteSpace(toolName) ? null : toolName.Trim();
        }
    }
}