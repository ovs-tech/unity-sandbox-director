using System.Collections.Generic;
using UnityEngine;
using Systems.PlacementSystem.Tools;

namespace Systems.PlacementSystem.Core
{
    /// <summary>
    /// Manages the pipeline of active placement tools.
    /// Replaces the hardcoded tool instantiation in PlacementController.
    /// </summary>
    public class ToolManager
    {
        private readonly Dictionary<PlacementController.PlacementToolType, IPlacementTool> _toolRegistry 
            = new Dictionary<PlacementController.PlacementToolType, IPlacementTool>();
            
        private readonly List<IPlacementTool> _activeTools = new List<IPlacementTool>();
        private PlacementToolContext _context;

        public ToolStateRegistry ToolStates { get; } = new ToolStateRegistry();

        public void InitializeContext(PlacementToolContext context)
        {
            _context = context;
            RefreshActiveToolContexts();
        }

        public void RegisterTool(PlacementController.PlacementToolType type, IPlacementTool tool)
        {
            _toolRegistry[type] = tool;
        }

        public bool HasTool(PlacementController.PlacementToolType type)
        {
            return _toolRegistry.ContainsKey(type);
        }

        public IPlacementTool GetTool(PlacementController.PlacementToolType type)
        {
            return _toolRegistry.TryGetValue(type, out var tool) ? tool : null;
        }

        public void SetPipeline(params PlacementController.PlacementToolType[] toolTypes)
        {
            if (toolTypes == null || toolTypes.Length == 0)
                return;

            var nextTools = new List<IPlacementTool>(toolTypes.Length);
            var seen = new HashSet<IPlacementTool>();

            for (int i = 0; i < toolTypes.Length; i++)
            {
                var tool = GetTool(toolTypes[i]);
                if (tool != null && seen.Add(tool))
                    nextTools.Add(tool);
            }

            ApplyToolStack(nextTools);
        }

        private void ApplyToolStack(IReadOnlyList<IPlacementTool> nextTools)
        {
            var nextSet = new HashSet<IPlacementTool>(nextTools);
            for (int i = 0; i < _activeTools.Count; i++)
            {
                var tool = _activeTools[i];
                if (!nextSet.Contains(tool))
                    tool.OnExit();
            }

            var previousSet = new HashSet<IPlacementTool>(_activeTools);
            _activeTools.Clear();
            _activeTools.AddRange(nextTools);

            if (_context != null)
            {
                for (int i = 0; i < _activeTools.Count; i++)
                {
                    var tool = _activeTools[i];
                    if (!previousSet.Contains(tool))
                        tool.OnEnter(_context);
                }
            }
        }

        private void RefreshActiveToolContexts()
        {
            if (_context == null) return;
            
            for (int i = 0; i < _activeTools.Count; i++)
            {
                _activeTools[i].OnEnter(_context);
            }
        }

        public void HandleInput()
        {
            if (_activeTools.Count == 0) return;
            
            for (int i = 0; i < _activeTools.Count; i++)
            {
                _activeTools[i].HandleInput();
            }
        }

        public void Tick()
        {
            if (_activeTools.Count == 0) return;
            
            for (int i = 0; i < _activeTools.Count; i++)
            {
                _activeTools[i].Tick();
            }
        }
        
        public void Clear()
        {
            for (int i = 0; i < _activeTools.Count; i++)
            {
                _activeTools[i].OnExit();
            }
            _activeTools.Clear();
            ToolStates.Clear();
        }
    }
}
