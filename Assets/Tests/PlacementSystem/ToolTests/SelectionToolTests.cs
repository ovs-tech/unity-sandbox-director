using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Tools;
using Systems.PlacementSystem.Selection;

namespace Systems.PlacementSystem.Tests
{
    public class SelectionToolTests
    {
        private Camera _camera;
        private GameObject _selectableObject;

        #pragma warning disable 0649
        private class MockInput : IInputProvider
        {
            private readonly bool _selectTriggered;
            private readonly Vector2 _pointerPos;
            private readonly bool _isModifierPressed;

            public MockInput(bool selectTriggered = false, Vector2 pointerPos = default, bool isModifierPressed = false)
            {
                _selectTriggered = selectTriggered;
                _pointerPos = pointerPos == default ? Vector2.zero : pointerPos;
                _isModifierPressed = isModifierPressed;
            }

            public bool IsPlaceActionTriggered() => _selectTriggered; // Mapping Selection to PlaceAction usually or separate action
            public bool IsCancelActionTriggered() => false;
            public bool IsRotateActionTriggered() => false;
            public bool IsDeleteActionTriggered() => false;
            public Vector2 GetPointerPosition() => _pointerPos;
        }
        #pragma warning restore 0649

        [SetUp]
        public void SetUp()
        {
            var camObj = new GameObject("TestCam");
            camObj.transform.position = new Vector3(0, 10, 0);
            camObj.transform.rotation = Quaternion.Euler(90, 0, 0); // look down
            _camera = camObj.AddComponent<Camera>();

            _selectableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _selectableObject.transform.position = Vector3.zero;
            _selectableObject.AddComponent<Selectable>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_camera != null) Object.DestroyImmediate(_camera.gameObject);
            if (_selectableObject != null) Object.DestroyImmediate(_selectableObject);
        }

        [Test]
        public void HandleSelection_ValidSelectable_SelectsObject()
        {
            var tool = new SelectionTool();
            var state = new PlacementSelectionState();
            var context = new PlacementToolContext(
                new MockInput(), null, null, null, null, _camera, 100, 90, 90, false, false, false, -1, Physics.DefaultRaycastLayers, -1, state, tool, new ToolStateRegistry()
            );

            tool.OnEnter(context);
            // Simulate click
            tool.HandleSelection(_selectableObject);

            Assert.IsTrue(state.HasSelection);
            Assert.AreEqual(_selectableObject, state.PrimarySelection);
        }

        [Test]
        public void HandleSelection_NullObject_ClearsSelection()
        {
            var tool = new SelectionTool();
            var state = new PlacementSelectionState();
            var context = new PlacementToolContext(
                new MockInput(), null, null, null, null, _camera, 100, 90, 90, false, false, false, -1, Physics.DefaultRaycastLayers, -1, state, tool, new ToolStateRegistry()
            );

            tool.OnEnter(context);
            tool.HandleSelection(_selectableObject);
            Assert.IsTrue(state.HasSelection);

            tool.HandleSelection(null);
            Assert.IsFalse(state.HasSelection);
        }
    }
}
