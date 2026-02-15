using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.SceneSandbox.Core;
using Systems.SceneSandbox.Core.Tools;

namespace Systems.SceneSandbox.Core.Tools.Tests
{
    public class ToolSystemTests
    {
        private GameObject _builderGO;
        private SceneSandboxBuilder _builder;
        private SandboxToolManager _toolManager;
        private SandboxInputManager _inputManager;
        private SelectionManager _selectionManager;
        private PlacementSystem _placementSystem;
        private TransformController _transformController;
        private GridManager _gridManager;
        private CameraRaycaster _cameraRaycaster;

        [SetUp]
        public void Setup()
        {
            _builderGO = new GameObject("Builder");
            _builder = _builderGO.AddComponent<SceneSandboxBuilder>();

            // Managers
            _toolManager = _builderGO.AddComponent<SandboxToolManager>();
            _inputManager = _builderGO.AddComponent<SandboxInputManager>();
            _selectionManager = _builderGO.AddComponent<SelectionManager>();
            _placementSystem = _builderGO.AddComponent<PlacementSystem>();
            _transformController = _builderGO.AddComponent<TransformController>();
            _gridManager = _builderGO.AddComponent<GridManager>();
            _cameraRaycaster = _builderGO.AddComponent<CameraRaycaster>();

            // Init context manually for test isolation
            var context = new ToolContext(
                _inputManager,
                _placementSystem,
                _selectionManager,
                _transformController,
                _gridManager,
                _cameraRaycaster,
                _builderGO.transform,
                _builder
            );

            _toolManager.Initialize(context);
        }

        [TearDown]
        public void Teardown()
        {
            if (_builderGO != null)
                Object.DestroyImmediate(_builderGO);
        }

        [Test]
        public void ToolManager_RegistersAndSwitchesTools()
        {
            var selectionTool = new SelectionTool();
            _toolManager.RegisterTool(selectionTool);

            _toolManager.ActivateTool("Selection");
            Assert.AreEqual(selectionTool, _toolManager.ActiveTool);
            Assert.AreEqual("Selection", _toolManager.ActiveTool.ToolName);
        }

        [Test]
        public void SelectionTool_EntersAndExits_WithoutErrors()
        {
            var selectionTool = new SelectionTool();
            _toolManager.RegisterTool(selectionTool);

            // Test enter
            _toolManager.ActivateTool("Selection");
            Assert.AreEqual(selectionTool, _toolManager.ActiveTool);

            // Test exit (by switching to null or another tool)
            _toolManager.SetActiveTool(null);
            Assert.IsNull(_toolManager.ActiveTool);
        }

        [Test]
        public void PlacementTool_Strategy_DefaultPlacement()
        {
            // Test the strategy logic in isolation
            var strategy = new DefaultPlacementStrategy(100f, -1, false, 1f);
            var context = new ToolContext(
                _inputManager, _placementSystem, _selectionManager,
                _transformController, _gridManager, _cameraRaycaster,
                _builderGO.transform, _builder
            );

            // Mock ray pointing down at origin
            Ray ray = new Ray(new Vector3(0, 10, 0), Vector3.down);

            bool result = strategy.GetPlacement(ray, context, out Vector3 pos, out Quaternion rot);

            Assert.IsTrue(result);
            // Default placement falls back to plane at y=0 if no hit, or raycast.
            // With empty scene, physics raycast fails. Fallback logic should catch it.
            // DefaultStrategy fallback is Plane(up, zero).
            Assert.AreEqual(Vector3.zero.y, pos.y, 0.01f);
        }

         [Test]
        public void PlacementTool_Strategy_SurfaceNormal()
        {
            // Setup a floor for raycast
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.position = Vector3.zero;
            floor.transform.up = Vector3.up;

            // Wait for physics update? Or direct raycast?
            // In editor tests, physics works immediately usually.
            // Let's manually sync physics if needed or rely on NUnit defaults.
            Physics.SyncTransforms();

            var strategy = new SurfaceNormalStrategy(100f, -1);
            var context = new ToolContext(
                _inputManager, _placementSystem, _selectionManager,
                _transformController, _gridManager, _cameraRaycaster,
                _builderGO.transform, _builder
            );

            Ray ray = new Ray(new Vector3(0, 10, 0), Vector3.down);
            bool result = strategy.GetPlacement(ray, context, out Vector3 pos, out Quaternion rot);

            Assert.IsTrue(result);
            Assert.AreEqual(Vector3.zero.x, pos.x, 0.1f);
            Assert.AreEqual(Vector3.zero.z, pos.z, 0.1f);
            // Normal is up, so rotation should align up to up (identity)
            Assert.AreEqual(Vector3.up, rot * Vector3.up); // Check alignment

            Object.DestroyImmediate(floor);
        }
    }
}
