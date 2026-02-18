using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Visualization;
using Systems.PlacementSystem.Core;

namespace PlacementSystem.Tests
{
    /// <summary>
    /// Tests for placement visualizers.
    /// </summary>
    public class VisualizerTests
    {
        private GameObject _visualizerGameObject;
        private GameObject _ghostObject;

        [SetUp]
        public void SetUp()
        {
            _visualizerGameObject = new GameObject("Visualizer");
            _ghostObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ghostObject.name = "GhostObject";
        }

        [TearDown]
        public void TearDown()
        {
            if (_visualizerGameObject != null)
                Object.DestroyImmediate(_visualizerGameObject);
            if (_ghostObject != null)
                Object.DestroyImmediate(_ghostObject);
        }

        [Test]
        public void StandardPlacementVisualizer_ImplementsInterface()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<StandardPlacementVisualizer>();

            // Assert
            Assert.IsNotNull(visualizer as IPlacementVisualizer);
        }

        [Test]
        public void StandardPlacementVisualizer_Initialize_DoesNotThrow()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<StandardPlacementVisualizer>();

            // Act & Assert
            Assert.DoesNotThrow(() => visualizer.Initialize(_ghostObject));
        }

        [Test]
        public void StandardPlacementVisualizer_UpdateVisual_DoesNotThrow()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<StandardPlacementVisualizer>();
            visualizer.Initialize(_ghostObject);

            // Act & Assert
            Assert.DoesNotThrow(() => visualizer.UpdateVisual(true));
            Assert.DoesNotThrow(() => visualizer.UpdateVisual(false));
        }

        [Test]
        public void StandardPlacementVisualizer_Cleanup_DoesNotThrow()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<StandardPlacementVisualizer>();
            visualizer.Initialize(_ghostObject);

            // Act & Assert
            Assert.DoesNotThrow(() => visualizer.Cleanup());
        }

        [Test]
        public void StandardPlacementVisualizer_InitializeWithNull_DoesNotThrow()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<StandardPlacementVisualizer>();

            // Act & Assert
            Assert.DoesNotThrow(() => visualizer.Initialize(null));
        }

        [Test]
        public void StandardPlacementVisualizer_UpdateBeforeInitialize_DoesNotThrow()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<StandardPlacementVisualizer>();

            // Act & Assert
            Assert.DoesNotThrow(() => visualizer.UpdateVisual(true));
        }

        [Test]
        public void OutlinePlacementVisualizer_ImplementsInterface()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<OutlinePlacementVisualizer>();

            // Assert
            Assert.IsNotNull(visualizer as IPlacementVisualizer);
        }

        [Test]
        public void OutlinePlacementVisualizer_Initialize_DoesNotThrow()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<OutlinePlacementVisualizer>();

            // Act & Assert
            Assert.DoesNotThrow(() => visualizer.Initialize(_ghostObject));
        }

        [Test]
        public void OutlinePlacementVisualizer_UpdateVisual_DoesNotThrow()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<OutlinePlacementVisualizer>();
            visualizer.Initialize(_ghostObject);

            // Act & Assert
            Assert.DoesNotThrow(() => visualizer.UpdateVisual(true));
            Assert.DoesNotThrow(() => visualizer.UpdateVisual(false));
        }

        [Test]
        public void OutlinePlacementVisualizer_Cleanup_DoesNotThrow()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<OutlinePlacementVisualizer>();
            visualizer.Initialize(_ghostObject);

            // Act & Assert
            Assert.DoesNotThrow(() => visualizer.Cleanup());
        }

        [Test]
        public void Visualizers_CanBeUsedInterchangeably()
        {
            // Arrange
            var standardVisualizer = _visualizerGameObject.AddComponent<StandardPlacementVisualizer>();
            var outlineVisualizer = _visualizerGameObject.AddComponent<OutlinePlacementVisualizer>();

            // Act - Cast to interface
            IPlacementVisualizer visualizer1 = standardVisualizer;
            IPlacementVisualizer visualizer2 = outlineVisualizer;

            // Assert
            Assert.IsNotNull(visualizer1);
            Assert.IsNotNull(visualizer2);
            Assert.DoesNotThrow(() => visualizer1.Initialize(_ghostObject));
            Assert.DoesNotThrow(() => visualizer2.Initialize(_ghostObject));
        }

        [Test]
        public void StandardPlacementVisualizer_MultipleInitializeCalls_DoesNotLeak()
        {
            // Arrange
            var visualizer = _visualizerGameObject.AddComponent<StandardPlacementVisualizer>();

            // Act
            visualizer.Initialize(_ghostObject);
            visualizer.Initialize(_ghostObject);
            visualizer.Initialize(_ghostObject);

            // Assert & Cleanup
            Assert.DoesNotThrow(() => visualizer.Cleanup());
            
            // Suppress expected logs
            LogAssert.NoUnexpectedReceived();
        }
    }
}
