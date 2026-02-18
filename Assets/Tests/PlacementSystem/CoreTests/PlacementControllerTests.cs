using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Input;
using Systems.PlacementSystem.Strategies;
using Systems.PlacementSystem.Validation;
using Systems.PlacementSystem.Visualization;

namespace PlacementSystem.Tests
{
    /// <summary>
    /// Tests for the PlacementController orchestrator.
    /// </summary>
    public class PlacementControllerTests
    {
        private GameObject _controllerGameObject;
        private GameObject _prefab;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            // Create camera
            var cameraObj = new GameObject("TestCamera");
            _camera = cameraObj.AddComponent<Camera>();
            _camera.transform.position = new Vector3(0, 10, 0);
            _camera.transform.LookAt(Vector3.zero);

            // Create controller
            _controllerGameObject = new GameObject("PlacementController");

            // Create test prefab
            _prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _prefab.name = "TestPrefab";
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerGameObject != null)
                Object.DestroyImmediate(_controllerGameObject);
            if (_prefab != null)
                Object.DestroyImmediate(_prefab);
            if (_camera != null)
                Object.DestroyImmediate(_camera.gameObject);
        }

        [Test]
        public void PlacementController_CanBeCreated()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Act
            var controller = _controllerGameObject.AddComponent<PlacementController>();

            // Assert
            Assert.IsNotNull(controller);
        }

        [Test]
        public void PlacementController_SetObjectToPlace_Works()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();

            // Act & Assert
            Assert.DoesNotThrow(() => controller.SetObjectToPlace(_prefab));
        }

        [Test]
        public void PlacementController_StartPlacement_WithoutDependencies_DoesNotThrow()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();
            SetupControllerDependencies(controller);
            controller.SetObjectToPlace(_prefab);

            // Act & Assert
            Assert.DoesNotThrow(() => controller.StartPlacement());
        }

        [Test]
        public void PlacementController_CancelPlacement_DoesNotThrow()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();

            // Act & Assert
            Assert.DoesNotThrow(() => controller.CancelPlacement());
        }

        [Test]
        public void PlacementController_StartAndCancelPlacement_Works()
        {
            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();
            SetupControllerDependencies(controller);
            controller.SetObjectToPlace(_prefab);

            // Act
            controller.StartPlacement();
            controller.CancelPlacement();

            // Assert - Should not throw
            Assert.Pass();
        }

        [Test]
        public void PlacementController_SetPlacementStrategy_Works()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();
            var strategy = _controllerGameObject.AddComponent<FreePositionStrategy>();

            // Act & Assert
            Assert.DoesNotThrow(() => controller.SetPlacementStrategy(strategy));
        }

        [Test]
        public void PlacementController_SetPlacementStrategy_CanSwitchStrategies()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();
            var freeStrategy = _controllerGameObject.AddComponent<FreePositionStrategy>();
            var gridStrategy = _controllerGameObject.AddComponent<GridPlacementStrategy>();

            // Act
            controller.SetPlacementStrategy(freeStrategy);
            controller.SetPlacementStrategy(gridStrategy);
            controller.SetPlacementStrategy(freeStrategy);

            // Assert - Should not throw
            Assert.Pass();
        }

        [Test]
        public void PlacementController_ConfirmPlacement_WithoutStarting_DoesNotThrow()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();
        
            // Act & Assert
            Assert.DoesNotThrow(() => controller.ConfirmPlacement());
        }

        [Test]
        public void PlacementController_MultipleStartCalls_OnlyCreatesOneGhost()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();
            controller.SetObjectToPlace(_prefab);

            // Act
            controller.StartPlacement();
            int countAfterFirst = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
            
            controller.StartPlacement(); // Second call should be ignored
            int countAfterSecond = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        
            // Assert
            Assert.AreEqual(countAfterFirst, countAfterSecond, "Multiple start calls should not create multiple ghosts");

            // Cleanup
            controller.CancelPlacement();
        }

        [Test]
        public void PlacementController_WithNullPrefab_StartPlacementDoesNothing()
        {
            // Expect dependency error logs
            LogAssert.Expect(LogType.Error, "PlacementController: Input provider must implement IInputProvider");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement strategy must implement IPlacementStrategy");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement validator must implement IPlacementValidator");
            LogAssert.Expect(LogType.Error, "PlacementController: Placement visualizer must implement IPlacementVisualizer");

            // Arrange
            var controller = _controllerGameObject.AddComponent<PlacementController>();
            controller.SetObjectToPlace(null);

            // Act & Assert
            Assert.DoesNotThrow(() => controller.StartPlacement());
        }

        /// <summary>
        /// Helper method to setup basic dependencies for the controller.
        /// </summary>
        private void SetupControllerDependencies(PlacementController controller)
        {
            // Add required components
            var inputProvider = _controllerGameObject.AddComponent<LegacyInputProvider>();
            var strategy = _controllerGameObject.AddComponent<FreePositionStrategy>();
            var validator = _controllerGameObject.AddComponent<PlacementValidation>();
            var visualizer = _controllerGameObject.AddComponent<StandardPlacementVisualizer>();

            // Use reflection to set private fields
            var cameraField = typeof(PlacementController).GetField("_placementCamera", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cameraField.SetValue(controller, _camera);

            var inputField = typeof(PlacementController).GetField("_inputProviderComponent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            inputField.SetValue(controller, inputProvider);

            var strategyField = typeof(PlacementController).GetField("_placementStrategyComponent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            strategyField.SetValue(controller, strategy);

            var validatorField = typeof(PlacementController).GetField("_placementValidatorComponent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            validatorField.SetValue(controller, validator);

            var visualizerField = typeof(PlacementController).GetField("_placementVisualizerComponent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            visualizerField.SetValue(controller, visualizer);

            // Trigger Awake manually
            var awakeMethod = typeof(PlacementController).GetMethod("Awake", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (awakeMethod != null)
            {
                awakeMethod.Invoke(controller, null);
            }
        }
    }
}
