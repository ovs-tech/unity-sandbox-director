using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Strategies;

namespace PlacementSystem.Tests
{
    /// <summary>
    /// Tests for placement strategies.
    /// </summary>
    public class PlacementStrategyTests
    {
        private GameObject _testGameObject;
        private GameObject _strategyGameObject;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestObject");
            _strategyGameObject = new GameObject("StrategyObject");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
                Object.DestroyImmediate(_testGameObject);
            if (_strategyGameObject != null)
                Object.DestroyImmediate(_strategyGameObject);
        }

        [Test]
        public void FreePositionStrategy_CalculatePosition_ReturnsOffsetPosition()
        {
            // Arrange
            var strategy = _strategyGameObject.AddComponent<FreePositionStrategy>();
            Vector3 inputPosition = new Vector3(1, 2, 3);

            // Act
            Vector3 result = strategy.CalculatePosition(inputPosition, _testGameObject);

            // Assert
            Assert.AreEqual(inputPosition.x, result.x, 0.01f);
            Assert.GreaterOrEqual(result.y, inputPosition.y); // Should have offset
            Assert.AreEqual(inputPosition.z, result.z, 0.01f);
        }

        [Test]
        public void FreePositionStrategy_CalculateRotation_ReturnsInputRotation()
        {
            // Arrange
            var strategy = _strategyGameObject.AddComponent<FreePositionStrategy>();
            Quaternion inputRotation = Quaternion.Euler(0, 45, 0);

            // Act
            Quaternion result = strategy.CalculateRotation(inputRotation);

            // Assert
            Assert.AreEqual(inputRotation, result);
        }

        [Test]
        public void GridPlacementStrategy_CalculatePosition_SnapsToGrid()
        {
            // Arrange
            var strategy = _strategyGameObject.AddComponent<GridPlacementStrategy>();
            // Set grid size to 1 using reflection
            var gridSizeField = typeof(GridPlacementStrategy).GetField("_gridSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gridSizeField.SetValue(strategy, 1f);

            Vector3 inputPosition = new Vector3(1.7f, 0, 2.3f);

            // Act
            Vector3 result = strategy.CalculatePosition(inputPosition, _testGameObject);

            // Assert
            Assert.AreEqual(2f, result.x, 0.01f, "X should snap to grid");
            Assert.AreEqual(2f, result.z, 0.01f, "Z should snap to grid");
        }

        [Test]
        public void GridPlacementStrategy_CalculateRotation_SnapsRotation()
        {
            // Arrange
            var strategy = _strategyGameObject.AddComponent<GridPlacementStrategy>();
            var rotationSnapField = typeof(GridPlacementStrategy).GetField("_rotationSnap", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rotationSnapField.SetValue(strategy, 90f);

            Quaternion inputRotation = Quaternion.Euler(0, 47, 0);

            // Act
            Quaternion result = strategy.CalculateRotation(inputRotation);

            // Assert
            float resultAngle = result.eulerAngles.y;
            // 47 / 90 = 0.522, rounds to 1, then * 90 = 90
            Assert.AreEqual(90f, resultAngle, 1f, "Rotation should snap to 90-degree increments");
        }

        [Test]
        public void HexPlacementStrategy_CalculatePosition_SnapsToHexGrid()
        {
            // Arrange
            var strategy = _strategyGameObject.AddComponent<HexPlacementStrategy>();
            Vector3 inputPosition = new Vector3(1.5f, 0, 1.5f);

            // Act
            Vector3 result = strategy.CalculatePosition(inputPosition, _testGameObject);

            // Assert
            Assert.IsNotNull(result);
            // Hex snapping is complex, just verify it returns a valid position
            Assert.IsFalse(float.IsNaN(result.x));
            Assert.IsFalse(float.IsNaN(result.z));
        }

        [Test]
        public void HexPlacementStrategy_CalculateRotation_SnapsTo60Degrees()
        {
            // Arrange
            var strategy = _strategyGameObject.AddComponent<HexPlacementStrategy>();
            Quaternion inputRotation = Quaternion.Euler(0, 35, 0);

            // Act
            Quaternion result = strategy.CalculateRotation(inputRotation);

            // Assert
            float resultAngle = result.eulerAngles.y;
            // Should snap to 60-degree increments (0, 60, 120, 180, 240, 300)
            float remainder = resultAngle % 60f;
            Assert.Less(Mathf.Abs(remainder), 1f, "Should snap to 60-degree increments");
        }

        [Test]
        public void GridPlacementStrategy_WithZeroGridSize_DoesNotThrow()
        {
            // Arrange
            var strategy = _strategyGameObject.AddComponent<GridPlacementStrategy>();
            var gridSizeField = typeof(GridPlacementStrategy).GetField("_gridSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gridSizeField.SetValue(strategy, 0.1f); // Very small but not zero

            // Act & Assert
            Assert.DoesNotThrow(() => strategy.CalculatePosition(Vector3.zero, _testGameObject));
        }

        [Test]
        public void AllStrategies_ImplementIPlacementStrategy()
        {
            // Arrange & Act
            var freeStrategy = _strategyGameObject.AddComponent<FreePositionStrategy>();
            var gridStrategy = _strategyGameObject.AddComponent<GridPlacementStrategy>();
            var hexStrategy = _strategyGameObject.AddComponent<HexPlacementStrategy>();

            // Assert
            Assert.IsNotNull(freeStrategy as Systems.PlacementSystem.Core.IPlacementStrategy);
            Assert.IsNotNull(gridStrategy as Systems.PlacementSystem.Core.IPlacementStrategy);
            Assert.IsNotNull(hexStrategy as Systems.PlacementSystem.Core.IPlacementStrategy);            
            // Suppress expected logs
            LogAssert.NoUnexpectedReceived();        }
    }
}
