using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Tools;
using Systems.PlacementSystem.Validation;

namespace Systems.PlacementSystem.Tests
{
    public class PlacementToolTests
    {
        private GameObject _ghost;
        private GameObject _prefab;
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
        }
        #pragma warning restore 0649

        private class MockStrategy : BasePlacementStrategy
        {
            public Vector3 ReturnPos;
            public Quaternion ReturnRot;

            public override Vector3 CalculatePosition(Vector3 rawWorldPos, GameObject ghostObject) => ReturnPos;
            public override Quaternion CalculateRotation(Quaternion currentRotation) => ReturnRot;

            public override void OnDrawGizmos()
            {
            }
        }

        private class MockValidator : IPlacementValidator
        {
            public bool ReturnValid = true;
            public bool IsPlacementValid(Vector3 position, Quaternion rotation, GameObject ghostObject) => ReturnValid;

            ValidationResult IPlacementValidator.IsPlacementValid(Vector3 position, Quaternion rotation, GameObject ghostObject)
            {
                return ReturnValid ? ValidationResult.Success : ValidationResult.Failure("Invalid placement");
            }
        }

        [SetUp]
        public void SetUp()
        {
            _ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghost.name = "Ghost";
            _prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _prefab.name = "Prefab";

            var camObj = new GameObject("TestCam");
            camObj.transform.position = new Vector3(0, 10, 0);
            camObj.transform.rotation = Quaternion.Euler(90, 0, 0); // look down
            _camera = camObj.AddComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_ghost != null) Object.DestroyImmediate(_ghost);
            if (_prefab != null) Object.DestroyImmediate(_prefab);
            if (_camera != null) Object.DestroyImmediate(_camera.gameObject);
        }

        [Test]
        public void SetupPlacement_DisablesPhysicsAndInitializesVisualizer()
        {
            var ghostRb = _ghost.AddComponent<Rigidbody>();
            var ghostCol = _ghost.GetComponent<Collider>();
            ghostRb.isKinematic = false;
            ghostCol.isTrigger = false;

            var tool = new PlacementTool();
            tool.SetupPlacement(_ghost, _prefab);

            Assert.IsTrue(ghostRb.isKinematic);
            Assert.IsTrue(ghostCol.isTrigger);
        }

        [Test]
        public void HandleInput_PlaceAction_CallsConfirmPlacement()
        {
            var tool = new PlacementTool();
            var input = new MockInput(placeTriggered: true);
            var context = new PlacementToolContext(
                input, null, new MockValidator(), null, null, _camera, 100, 90, 90, false, false, false, -1, -1, -1, new PlacementSelectionState(), null, new ToolStateRegistry()
            );

            bool confirmed = false;
            tool.OnPlacementConfirmed = (go) => { confirmed = true; Object.DestroyImmediate(go); };

            tool.OnEnter(context);
            tool.SetupPlacement(_ghost, _prefab);
            tool.HandleInput();

            Assert.IsTrue(confirmed);
            Assert.IsTrue(_ghost == null || _ghost.Equals(null)); // Ghost should be destroyed
        }

        [Test]
        public void HandleInput_CancelAction_CallsCancelPlacement()
        {
            var tool = new PlacementTool();
            var input = new MockInput(cancelTriggered: true);
            var context = new PlacementToolContext(
                input, null, new MockValidator(), null, null, _camera, 100, 90, 90, false, false, false, -1, -1, -1, new PlacementSelectionState(), null, new ToolStateRegistry()
            );

            bool cancelled = false;
            tool.OnPlacementCancelled = () => cancelled = true;

            tool.OnEnter(context);
            tool.SetupPlacement(_ghost, _prefab);
            tool.HandleInput();

            Assert.IsTrue(cancelled);
            Assert.IsTrue(_ghost == null || _ghost.Equals(null)); // Ghost should be destroyed
        }

        [Test]
        public void ConfirmPlacement_WhenInvalid_DoesNotPlace()
        {
            var tool = new PlacementTool();
            var validator = new MockValidator { ReturnValid = false };
            var context = new PlacementToolContext(
                new MockInput(), null, validator, null, null, _camera, 100, 90, 90, false, false, false, -1, -1, -1, new PlacementSelectionState(), null, new ToolStateRegistry()
            );

            bool confirmed = false;
            tool.OnPlacementConfirmed = (go) => confirmed = true;

            tool.OnEnter(context);
            tool.SetupPlacement(_ghost, _prefab);
            tool.ConfirmPlacement();

            Assert.IsFalse(confirmed);
            Assert.IsFalse(_ghost == null || _ghost.Equals(null)); // Ghost remains
        }

        [Test]
        public void Tick_UpdatesGhostPositionAndRotation()
        {
            var tool = new PlacementTool();
            
            var strategy = ScriptableObject.CreateInstance<MockStrategy>();
            strategy.ReturnPos = new Vector3(1, 0, 1);
            strategy.ReturnRot = Quaternion.Euler(0, 90, 0);

            var context = new PlacementToolContext(
                null, strategy, new MockValidator(), null, null, _camera, 100, 90, 90, false, false, false, -1, -1, -1, new PlacementSelectionState(), null, new ToolStateRegistry()
            );

            // Add safe box collider for reliable raycast
            var floor = new GameObject("Floor");
            var coll = floor.AddComponent<BoxCollider>();
            coll.size = new Vector3(100, 1, 100);
            floor.transform.position = new Vector3(0, -0.5f, 0);

            // Important: sync transforms
            Physics.SyncTransforms();

            var input = new MockInput(pointerPos: new Vector2(_camera.pixelWidth / 2f, _camera.pixelHeight / 2f));
            
            // Need to recreate context with the assigned input
            context = new PlacementToolContext(
                input, strategy, new MockValidator(), null, null, _camera, 100, 90, 90, false, false, false, -1, -1, -1, new PlacementSelectionState(), null, new ToolStateRegistry()
            );

            tool.OnEnter(context);
            tool.SetupPlacement(_ghost, _prefab);

            // Trigger raycast state
            tool.HandleInput();
            // Update position
            tool.Tick();

            Assert.AreEqual(new Vector3(1, 0, 1), _ghost.transform.position);
            Assert.AreEqual(Quaternion.Euler(0, 90, 0).eulerAngles, _ghost.transform.rotation.eulerAngles);

            Object.DestroyImmediate(floor);
        }
    }
}
