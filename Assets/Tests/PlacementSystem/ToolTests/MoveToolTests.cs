using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Tools;

namespace Systems.PlacementSystem.Tests
{
    public class MoveToolTests
    {
        private GameObject _testObject;
        private Camera _camera;

        #pragma warning disable 0649
        private class MockInput : IInputProvider
        {
            private readonly bool _placeTriggered;
            private readonly bool _cancelTriggered;
            private readonly bool _rotateTriggered;
            private readonly Vector2 _pointerPos;

            public MockInput(bool placeTriggered = false, bool cancelTriggered = false, bool rotateTriggered = false, Vector2 pointerPos = default)
            {
                _placeTriggered = placeTriggered;
                _cancelTriggered = cancelTriggered;
                _rotateTriggered = rotateTriggered;
                _pointerPos = pointerPos == default ? Vector2.zero : pointerPos;
            }

            public bool IsPlaceActionTriggered() => _placeTriggered;
            public bool IsCancelActionTriggered() => _cancelTriggered;
            public bool IsRotateActionTriggered() => _rotateTriggered;
            public bool IsDeleteActionTriggered() => false;
            public Vector2 GetPointerPosition() => _pointerPos;
            public bool IsMultiSelectModifierHeld() => false;
        }
        #pragma warning restore 0649

        [SetUp]
        public void SetUp()
        {
            _testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _testObject.transform.position = new Vector3(5, 0, 5);
            _testObject.transform.rotation = Quaternion.Euler(0, 45, 0);

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
        public void StartMove_SavesOriginalPositionAndActivates()
        {
            var tool = new MoveTool();
            var states = new ToolStateRegistry();
            var context = new PlacementToolContext(
                new MockInput(), null, null, null, null, _camera, 100, 90, 90, false, false, true, -1, -1, -1, new PlacementSelectionState(), null, states
            );

            tool.OnEnter(context);
            tool.StartMove(_testObject);

            Assert.IsTrue(states.GetOrCreate<MoveToolState>().IsMoveActive);
        }

        [Test]
        public void HandleInput_TriggersStartMoveFromSelection()
        {
            var tool = new MoveTool();
            var states = new ToolStateRegistry();
            var context = new PlacementToolContext(
                new MockInput(), null, null, null, null, _camera, 100, 90, 90, false, false, true, -1, -1, -1, new PlacementSelectionState(), null, states
            );

            tool.OnEnter(context);

            var selState = states.GetOrCreate<SelectionToolState>();
            selState.RecordSelectionInput(_testObject, true);

            tool.HandleInput();

            Assert.IsTrue(states.GetOrCreate<MoveToolState>().IsMoveActive);
        }

        [Test]
        public void CancelMove_RestoresOriginalPosition()
        {
            var tool = new MoveTool();
            var context = new PlacementToolContext(
                new MockInput(), null, null, null, null, _camera, 100, 90, 90, false, false, true, -1, -1, -1, new PlacementSelectionState(), null, new ToolStateRegistry()
            );

            bool cancelled = false;
            tool.OnMoveCancelled = () => cancelled = true;

            tool.OnEnter(context);
            tool.StartMove(_testObject);

            // Change position to simulate moving
            _testObject.transform.position = new Vector3(10, 0, 10);

            tool.CancelMove();

            Assert.IsTrue(cancelled);
            Assert.AreEqual(new Vector3(5, 0, 5), _testObject.transform.position);
            Assert.AreEqual(Quaternion.Euler(0, 45, 0).eulerAngles, _testObject.transform.rotation.eulerAngles);
        }

        [Test]
        public void ConfirmMove_CompletesMoveAndKeepsNewPosition()
        {
            var tool = new MoveTool();
            var context = new PlacementToolContext(
                new MockInput(), null, null, null, null, _camera, 100, 90, 90, false, false, true, -1, -1, -1, new PlacementSelectionState(), null, new ToolStateRegistry()
            );

            bool confirmed = false;
            tool.OnMoveConfirmed = (go) => confirmed = true;

            tool.OnEnter(context);
            tool.StartMove(_testObject);

            // Change position to simulate moving
            _testObject.transform.position = new Vector3(10, 0, 10);

            tool.ConfirmMove();

            Assert.IsTrue(confirmed);
            Assert.AreEqual(new Vector3(10, 0, 10), _testObject.transform.position); // Remains at new position
        }
    }
}
