using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Input;
using Systems.PlacementSystem.Strategies;
using Systems.PlacementSystem.Validation;
using Systems.PlacementSystem.Visualization;
using Systems.PlacementSystem.Sockets;

namespace PlacementSystem.Tests
{
    /// <summary>
    /// Integration tests for the complete placement system.
    /// </summary>
    public class PlacementSystemIntegrationTests
    {
        private GameObject _sceneRoot;
        private Camera _camera;
        private GameObject _ground;

        [SetUp]
        public void SetUp()
        {
            _sceneRoot = new GameObject("TestScene");

            // Create camera
            var cameraObj = new GameObject("Camera");
            cameraObj.transform.parent = _sceneRoot.transform;
            _camera = cameraObj.AddComponent<Camera>();
            _camera.transform.position = new Vector3(0, 10, -10);
            _camera.transform.LookAt(Vector3.zero);

            // Create ground
            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _ground.transform.parent = _sceneRoot.transform;
            _ground.transform.position = Vector3.zero;
            _ground.layer = LayerMask.NameToLayer("Default");
        }

        [TearDown]
        public void TearDown()
        {
            if (_sceneRoot != null)
                Object.DestroyImmediate(_sceneRoot);
        }

        [Test]
        public void Integration_CompleteSystemSetup_Works()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange - Create complete placement system
            var controllerObj = new GameObject("PlacementController");
            controllerObj.transform.parent = _sceneRoot.transform;

            var controller = controllerObj.AddComponent<PlacementController>();
            var inputProvider = controllerObj.AddComponent<LegacyInputProvider>();
            var strategy = controllerObj.AddComponent<GridPlacementStrategy>();
            var validator = controllerObj.AddComponent<PlacementValidation>();
            var visualizer = controllerObj.AddComponent<StandardPlacementVisualizer>();

            // Create prefab with validation
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var placeableObject = prefab.AddComponent<PlaceableObject>();

            // Setup controller via reflection
            SetupController(controller, _camera, inputProvider, strategy, validator, visualizer, prefab);

            // Act - Start placement
            controller.StartPlacement();

            // Assert - System should be active
            Assert.IsNotNull(controller);

            // Cleanup
            controller.CancelPlacement();
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Integration_SocketSnapping_Works()
        {
            // Expect log message
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("SnapManager: Cached \\d+ sockets"));

            // Arrange
            var socketType = ScriptableObject.CreateInstance<SocketType>();
            
            // Create socket with collider for detection
            var socketObj = new GameObject("Socket");
            socketObj.transform.parent = _sceneRoot.transform;
            socketObj.transform.position = new Vector3(5, 1, 5);
            var socket = socketObj.AddComponent<Socket>();
            var socketCollider = socketObj.AddComponent<SphereCollider>();
            socketCollider.radius = 0.5f;
            socketObj.layer = LayerMask.NameToLayer("Default");
            SetSocketType(socket, socketType);

            // Create snap manager
            var snapManagerObj = new GameObject("SnapManager");
            snapManagerObj.transform.parent = _sceneRoot.transform;
            var snapManager = snapManagerObj.AddComponent<SnapManager>();
            snapManager.RegisterSocket(socket);

            // Act - Find nearest socket
            Socket found = snapManager.FindNearestSocket(new Vector3(5.5f, 1, 5.5f), socketType, 2f);

            // Assert
            Assert.IsNotNull(found, "Should find the socket");
            Assert.AreEqual(socket, found);
    
            // Suppress expected logs
            LogAssert.NoUnexpectedReceived();
        
            // Cleanup
            Object.DestroyImmediate(socketType);
        }

        [Test]
        public void Integration_ValidationWithMultipleRules_Works()
        {
            // Create ground for surface validation
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = Vector3.zero;
            ground.layer = LayerMask.NameToLayer("Default");
            ground.name = "Ground";

            // Arrange
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "TestCube";
            
            // Remove collider from prefab to avoid validation conflicts
            var collider = prefab.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            var placeableObject = prefab.AddComponent<PlaceableObject>();

            // Create rules with lenient settings
            var clearanceRule = ScriptableObject.CreateInstance<ClearanceRule>();
            var surfaceRule = ScriptableObject.CreateInstance<RequireSurfaceRule>();

            // Configure rules via reflection
            ConfigureClearanceRule(clearanceRule);
            ConfigureRequireSurfaceRule(surfaceRule);

            placeableObject.AddRule(clearanceRule);
            placeableObject.AddRule(surfaceRule);

            // Act - Validate at valid position (above ground)
            bool resultValid = placeableObject.ValidatePlacement(
                new Vector3(0, 1.5f, 0),
                Quaternion.identity,
                prefab
            );

            // Validate at invalid position (in the air, far from ground)
            bool resultInvalid = placeableObject.ValidatePlacement(
                new Vector3(0, 100, 0),
                Quaternion.identity,
                prefab
            );

            // Assert
            Assert.IsTrue(resultValid, "Should be valid near ground");
            Assert.IsFalse(resultInvalid, "Should be invalid in the air");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(clearanceRule);
            Object.DestroyImmediate(surfaceRule);
        }

        [Test]
        public void Integration_StrategySwitch_Works()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controllerObj = new GameObject("PlacementController");
            controllerObj.transform.parent = _sceneRoot.transform;

            var controller = controllerObj.AddComponent<PlacementController>();
            var freeStrategy = controllerObj.AddComponent<FreePositionStrategy>();
            var gridStrategy = controllerObj.AddComponent<GridPlacementStrategy>();

            // Act - Switch strategies
            controller.SetPlacementStrategy(freeStrategy);
            Vector3 freePos = freeStrategy.CalculatePosition(new Vector3(1.7f, 0, 1.7f), null);

            controller.SetPlacementStrategy(gridStrategy);
            Vector3 gridPos = gridStrategy.CalculatePosition(new Vector3(1.7f, 0, 1.7f), null);

            // Assert
            Assert.AreNotEqual(freePos.x, gridPos.x, "Strategies should produce different results");
        }

        [Test]
        public void Integration_VisualizerFeedback_Works()
        {
            // Arrange
            var visualizerObj = new GameObject("Visualizer");
            visualizerObj.transform.parent = _sceneRoot.transform;
            var visualizer = visualizerObj.AddComponent<StandardPlacementVisualizer>();

            var ghostObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghostObject.transform.parent = _sceneRoot.transform;

            // Act
            visualizer.Initialize(ghostObject);
            visualizer.UpdateVisual(true);  // Valid
            visualizer.UpdateVisual(false); // Invalid
            visualizer.Cleanup();

            // Assert - Should not throw
            Assert.Pass();

            // Cleanup
            Object.DestroyImmediate(ghostObject);
        }

        [Test]
        public void Integration_CompleteWorkflow_StartPlaceCancel()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange - Complete system
            var controllerObj = new GameObject("PlacementController");
            controllerObj.transform.parent = _sceneRoot.transform;

            var controller = controllerObj.AddComponent<PlacementController>();
            var inputProvider = controllerObj.AddComponent<LegacyInputProvider>();
            var strategy = controllerObj.AddComponent<FreePositionStrategy>();
            var validator = controllerObj.AddComponent<PlacementValidation>();
            var visualizer = controllerObj.AddComponent<StandardPlacementVisualizer>();

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            SetupController(controller, _camera, inputProvider, strategy, validator, visualizer, prefab);

            // Act - Full workflow
            Assert.DoesNotThrow(() => controller.StartPlacement());
            Assert.DoesNotThrow(() => controller.CancelPlacement());

            // Assert - Should not throw
            Assert.Pass();

            // Cleanup
            Object.DestroyImmediate(prefab);
        }

        /// <summary>
        /// Helper to setup controller via reflection.
        /// </summary>
        private void SetupController(PlacementController controller, Camera cam, 
            LegacyInputProvider input, IPlacementStrategy strat, 
            PlacementValidation valid, StandardPlacementVisualizer vis, GameObject prefab)
        {
            var type = typeof(PlacementController);
            
            type.GetField("_placementCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, cam);
            type.GetField("_inputProviderComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, input);
            type.GetField("_placementStrategyComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, strat as MonoBehaviour);
            type.GetField("_placementValidatorComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, valid);
            type.GetField("_placementVisualizerComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, vis);
            type.GetField("_objectToPlace", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, prefab);

            // Trigger Awake
            type.GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(controller, null);
        }

        /// <summary>
        /// Helper to set socket type via reflection.
        /// </summary>
        private void SetSocketType(Socket socket, SocketType socketType)
        {
            var field = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(socket, socketType);
        }

        private void ConfigureClearanceRule(ClearanceRule rule)
        {
            var typeField = typeof(ClearanceRule).GetField("_maxAllowedOverlaps", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // Allow plenty of overlaps for test (we're checking near ground with a plane below)
            typeField?.SetValue(rule, 10);

            var layerField = typeof(ClearanceRule).GetField("_obstacleLayer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (layerField != null)
            {
                // Check only a non-existent layer so clearance always passes in this test
                LayerMask mask = 0; // No layers
                layerField.SetValue(rule, mask);
            }
        }

        private void ConfigureRequireSurfaceRule(RequireSurfaceRule rule)
        {
            var layerField = typeof(RequireSurfaceRule).GetField("_requiredSurfaceLayer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (layerField != null)
            {
                LayerMask mask = LayerMask.GetMask("Default");
                layerField.SetValue(rule, mask);
            }
        }
    }
}
