using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Tools;
using System.Collections.Generic;

namespace Systems.PlacementSystem.Tests
{
    public class ToolManagerTests
    {
        private class MockTool : IPlacementTool
        {
            public bool HandleInputCalled { get; private set; }
            public bool TickCalled { get; private set; }
            public bool OnEnterCalled { get; private set; }
            public bool OnExitCalled { get; private set; }
            public GameObject LastSelected { get; private set; }

            public void HandleInput() => HandleInputCalled = true;
            public void Tick() => TickCalled = true;
            public void OnEnter(PlacementToolContext context) => OnEnterCalled = true;
            public void OnExit() => OnExitCalled = true;
            public void HandleSelection(GameObject selected) => LastSelected = selected;
        }

        [Test]
        public void ToolManager_RegisterTool_AddsToolToRegistry()
        {
            var manager = new ToolManager();
            var mockTool = new MockTool();
            
            manager.RegisterTool(PlacementController.PlacementToolType.Placement, mockTool);
            
            Assert.IsTrue(manager.HasTool(PlacementController.PlacementToolType.Placement));
        }

        [Test]
        public void ToolManager_SetPipeline_ExecutesOnlyActiveTools()
        {
            var manager = new ToolManager();
            var tool1 = new MockTool();
            var tool2 = new MockTool();
            
            manager.RegisterTool(PlacementController.PlacementToolType.Selection, tool1);
            manager.RegisterTool(PlacementController.PlacementToolType.Placement, tool2);
            
            var context = new PlacementToolContext(null, null, null, null, null, null, 10, 90, 90, false);
            manager.InitializeContext(context);
            
            manager.SetPipeline(PlacementController.PlacementToolType.Selection);
            
            manager.HandleInput();
            manager.Tick();
            
            Assert.IsTrue(tool1.HandleInputCalled);
            Assert.IsTrue(tool1.TickCalled);
            Assert.IsTrue(tool1.OnEnterCalled);
            
            Assert.IsFalse(tool2.HandleInputCalled);
            Assert.IsFalse(tool2.TickCalled);
            Assert.IsFalse(tool2.OnEnterCalled);
        }
    }
}
