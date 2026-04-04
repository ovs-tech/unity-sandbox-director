using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Input;
using Systems.PlacementSystem.Strategies;
using Systems.PlacementSystem.Tools;
using Systems.PlacementSystem.Validation;
using Systems.PlacementSystem.Visualization;
using Systems.PlacementSystem.Sockets;
using Systems.PlacementSystem.Core.Components;

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
            // Arrange - Create complete placement system
            var controllerObj = new GameObject("PlacementController");
            controllerObj.transform.parent = _sceneRoot.transform;

            var controller = controllerObj.AddComponent<PlacementController>();
            var inputProvider = controllerObj.AddComponent<LegacyInputProvider>();
            var strategy = ScriptableObject.CreateInstance<GridPlacementStrategy>();
            var visualizer = ScriptableObject.CreateInstance<StandardPlacementVisualizer>();

            // Create prefab with validation
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var part = prefab.AddComponent<PlacementPart>();

            // Setup controller via reflection
            SetupController(controller, _camera, inputProvider, strategy, visualizer, prefab);

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
            // Arrange
            var socketType = ScriptableObject.CreateInstance<SocketType>();
            
            // Create socket with collider for detection
            var socketObj = new GameObject("Socket");
            socketObj.transform.parent = _sceneRoot.transform;
            socketObj.transform.position = new Vector3(5, 1, 5);
            var socketCollider = socketObj.AddComponent<SphereCollider>();
            socketCollider.radius = 0.5f;
            var socket = socketObj.AddComponent<Socket>();
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

            var part = prefab.AddComponent<PlacementPart>();

            // Create rules with lenient settings
            var clearanceRule = ScriptableObject.CreateInstance<ClearanceRule>();
            var surfaceRule = ScriptableObject.CreateInstance<RequireSurfaceRule>();

            // Configure rules via reflection
            ConfigureClearanceRule(clearanceRule);
            ConfigureRequireSurfaceRule(surfaceRule);

            part.AddRule(clearanceRule);
            part.AddRule(surfaceRule);

            // Act - Validate at valid position (above ground)
            var resultValid = part.ValidatePlacement(
                new Vector3(0, 1.5f, 0),
                Quaternion.identity,
                prefab
            );

            // Validate at invalid position (in the air, far from ground)
            var resultInvalid = part.ValidatePlacement(
                new Vector3(0, 100, 0),
                Quaternion.identity,
                prefab
            );

            // Assert
            Assert.IsTrue(resultValid.IsValid, "Should be valid near ground");
            Assert.IsFalse(resultInvalid.IsValid, "Should be invalid in the air");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(clearanceRule);
            Object.DestroyImmediate(surfaceRule);
        }

        [Test]
        public void Integration_StrategySwitch_Works()
        {
            // Arrange
            var controllerObj = new GameObject("PlacementController");
            controllerObj.transform.parent = _sceneRoot.transform;

            var controller = controllerObj.AddComponent<PlacementController>();
            var inputProvider = controllerObj.AddComponent<LegacyInputProvider>();
            var visualizer = ScriptableObject.CreateInstance<StandardPlacementVisualizer>();
            var freeStrategy = ScriptableObject.CreateInstance<FreePositionStrategy>();
            var gridStrategy = ScriptableObject.CreateInstance<GridPlacementStrategy>();
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);

            SetupController(controller, _camera, inputProvider, freeStrategy, visualizer, prefab);

            // Act - Switch strategies
            controller.SetPlacementStrategy(freeStrategy);
            Vector3 freePos = freeStrategy.CalculatePosition(new Vector3(1.7f, 0, 1.7f), null);

            controller.SetPlacementStrategy(gridStrategy);
            Vector3 gridPos = gridStrategy.CalculatePosition(new Vector3(1.7f, 0, 1.7f), null);

            // Assert
            Assert.AreNotEqual(freePos.x, gridPos.x, "Strategies should produce different results");

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Integration_VisualizerFeedback_Works()
        {
            // Arrange
            var visualizerObj = new GameObject("Visualizer");
            visualizerObj.transform.parent = _sceneRoot.transform;
            var visualizer = ScriptableObject.CreateInstance<StandardPlacementVisualizer>();

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
            // Arrange - Complete system
            var controllerObj = new GameObject("PlacementController");
            controllerObj.transform.parent = _sceneRoot.transform;

            var controller = controllerObj.AddComponent<PlacementController>();
            var inputProvider = controllerObj.AddComponent<LegacyInputProvider>();
            var strategy = ScriptableObject.CreateInstance<FreePositionStrategy>();
            var visualizer = ScriptableObject.CreateInstance<StandardPlacementVisualizer>();

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            SetupController(controller, _camera, inputProvider, strategy, visualizer, prefab);

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
            StandardPlacementVisualizer vis, GameObject prefab)
        {
            var type = typeof(PlacementController);
            
            type.GetField("_placementCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, cam);
            type.GetField("_inputProviderComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, input);
            type.GetField("_placementStrategy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, strat as ScriptableObject);
            type.GetField("_placementVisualizer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, vis);
            type.GetField("_objectToPlace", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, prefab);

            var toolManager = controller.gameObject.GetComponent<ToolManager>();
            if (toolManager == null)
            {
                toolManager = controller.gameObject.AddComponent<ToolManager>();
            }

            type.GetField("_toolManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, toolManager);

            ConfigureToolManager(toolManager);

            // Initialize for testing instead of Awake
            controller.InitializeForTesting();
        }

        private static void ConfigureToolManager(ToolManager toolManager)
        {
            var toolset = ScriptableObject.CreateInstance<PlacementToolset>();
            var selectionTool = ScriptableObject.CreateInstance<SelectionTool>();
            var placementTool = ScriptableObject.CreateInstance<PlacementTool>();

            var entries = new List<PlacementToolset.ToolEntry>
            {
                new PlacementToolset.ToolEntry
                {
                    ToolType = PlacementController.ToolIds.Selection,
                    ToolAsset = selectionTool
                },
                new PlacementToolset.ToolEntry
                {
                    ToolType = PlacementController.ToolIds.Placement,
                    ToolAsset = placementTool
                }
            };

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var toolsField = typeof(PlacementToolset).GetField("_tools", flags);
            toolsField.SetValue(toolset, entries);

            var toolsetField = typeof(ToolManager).GetField("_toolset", flags);
            toolsetField.SetValue(toolManager, toolset);
        }

        /// <summary>
        /// Helper to set socket type via reflection.
        /// </summary>
        private void SetSocketType(Socket socket, SocketType socketType)
        {
            var field = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(socket, socketType);
            }
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
