using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Tools;

namespace Systems.PlacementSystem.Tests
{
    public class RotateDeleteToolTests
    {
        private GameObject _testObject;
        private Camera _camera;

        private class MockInput : IInputProvider
        {
            public bool RotateTriggered;
            public bool DeleteTriggered;
            
            // Delete action maps to cancel/delete depending on implementation, 
            // but DeleteTool usually checks IsCancelActionTriggered or similar if mapped.
            public bool IsPlaceActionTriggered() => false;
            public bool IsCancelActionTriggered() => false;
            public bool IsRotateActionTriggered() => RotateTriggered;
            public bool IsDeleteActionTriggered() => DeleteTriggered;
            public Vector2 GetPointerPosition() => Vector2.zero;
        }

        [SetUp]
        public void SetUp()
        {
            _testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _testObject.transform.position = Vector3.zero;
            _testObject.transform.rotation = Quaternion.identity;
            
            var camObj = new GameObject("TestCam");
            camObj.transform.position = new Vector3(0, 10, 0);
            camObj.transform.rotation = Quaternion.Euler(90, 0, 0); // look down
            _camera = camObj.AddComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null) Object.DestroyImmediate(_testObject);
            if (_camera != null) Object.DestroyImmediate(_camera.gameObject);
        }

        [Test]
        public void RotateTool_HandleInput_RotatesPrimarySelection()
        {
            var tool = new RotateTool();
            var input = new MockInput { RotateTriggered = true };
            var selectionState = new PlacementSelectionState();
            selectionState.AddSelection(_testObject, true);

            var context = new PlacementToolContext(
                input, null, null, null, null, _camera, 100, 45, 90, false, false, false, -1, -1, -1, selectionState, null, new ToolStateRegistry()
            );

            tool.OnEnter(context);
            tool.HandleInput();

            // Check if rotation increased by increment
            Assert.AreEqual(45f, _testObject.transform.rotation.eulerAngles.y, 1f);
        }

        [Test]
        public void DeleteTool_HandleInput_DeletesPrimarySelection()
        {
            var tool = new DeleteTool();
            var input = new MockInput { DeleteTriggered = true }; // Simulate delete action
            var selectionState = new PlacementSelectionState();
            selectionState.AddSelection(_testObject, true);

            var context = new PlacementToolContext(
                input, null, null, null, null, _camera, 100, 45, 90, false, false, false, -1, -1, -1, selectionState, null, new ToolStateRegistry()
            );

            tool.OnEnter(context);
            tool.HandleInput();

            // Yield or wait for destruction frame is needed in play mode, but in edit mode Destroy is delayed.
            // But we can check if it cleared selection at least.
            Assert.IsFalse(selectionState.HasSelection);
        }
    }
}
