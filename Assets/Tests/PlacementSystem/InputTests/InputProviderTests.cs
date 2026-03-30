using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Input;
using Systems.PlacementSystem.Core;

namespace PlacementSystem.Tests
{
    /// <summary>
    /// Tests for input providers.
    /// </summary>
    public class InputProviderTests
    {
        private GameObject _inputProviderGameObject;

        [SetUp]
        public void SetUp()
        {
            _inputProviderGameObject = new GameObject("InputProvider");
        }

        [TearDown]
        public void TearDown()
        {
            if (_inputProviderGameObject != null)
                Object.DestroyImmediate(_inputProviderGameObject);
        }

        [Test]
        public void LegacyInputProvider_ImplementsInterface()
        {
            // Arrange
            var inputProvider = _inputProviderGameObject.AddComponent<LegacyInputProvider>();

            // Assert
            Assert.IsNotNull(inputProvider as IInputProvider, "LegacyInputProvider should implement IInputProvider");
        }

        [Test]
        public void LegacyInputProvider_GetPointerPosition_ReturnsValidVector()
        {
            // Arrange
            var inputProvider = _inputProviderGameObject.AddComponent<LegacyInputProvider>();

            // Act
            Vector2 position = inputProvider.GetPointerPosition();

            // Assert
            Assert.IsFalse(float.IsNaN(position.x));
            Assert.IsFalse(float.IsNaN(position.y));
        }

        [Test]
        public void LegacyInputProvider_IsPlaceActionTriggered_ReturnsBool()
        {
            // Arrange
            var inputProvider = _inputProviderGameObject.AddComponent<LegacyInputProvider>();

            // Act
            bool result = inputProvider.IsPlaceActionTriggered();

            // Assert
            Assert.IsFalse(result); // Should be false when not clicking
        }

        [Test]
        public void LegacyInputProvider_IsCancelActionTriggered_ReturnsBool()
        {
            // Arrange
            var inputProvider = _inputProviderGameObject.AddComponent<LegacyInputProvider>();

            // Act
            bool result = inputProvider.IsCancelActionTriggered();

            // Assert
            Assert.IsFalse(result); // Should be false when not pressing cancel
        }

        [Test]
        public void LegacyInputProvider_IsRotateActionTriggered_ReturnsBool()
        {
            // Arrange
            var inputProvider = _inputProviderGameObject.AddComponent<LegacyInputProvider>();

            // Act
            bool result = inputProvider.IsRotateActionTriggered();

            // Assert
            Assert.IsFalse(result); // Should be false when not pressing rotate
        }

        [Test]
        public void NewInputSystemProvider_ImplementsInterface()
        {
            // Arrange
            var inputProvider = _inputProviderGameObject.AddComponent<NewInputSystemProvider>();

            // Assert
            Assert.IsNotNull(inputProvider as IInputProvider, "NewInputSystemProvider should implement IInputProvider");
        }

        [Test]
        public void NewInputSystemProvider_GetPointerPosition_ReturnsValidVector()
        {
            // Arrange
            var inputProvider = _inputProviderGameObject.AddComponent<NewInputSystemProvider>();

            // Act
            Vector2 position = inputProvider.GetPointerPosition();

            // Assert
            Assert.IsFalse(float.IsNaN(position.x));
            Assert.IsFalse(float.IsNaN(position.y));
        }

        [Test]
        public void NewInputSystemProvider_AllMethodsReturnBool()
        {
            // Arrange
            var inputProvider = _inputProviderGameObject.AddComponent<NewInputSystemProvider>();

            // Act & Assert
            Assert.DoesNotThrow(() => inputProvider.IsPlaceActionTriggered());
            Assert.DoesNotThrow(() => inputProvider.IsCancelActionTriggered());
            Assert.DoesNotThrow(() => inputProvider.IsRotateActionTriggered());
            Assert.DoesNotThrow(() => inputProvider.IsMultiSelectModifierHeld());
        }

        [Test]
        public void LegacyInputProvider_MultiSelectModifierMethod_ReturnsBool()
        {
            var inputProvider = _inputProviderGameObject.AddComponent<LegacyInputProvider>();
            Assert.DoesNotThrow(() => inputProvider.IsMultiSelectModifierHeld());
        }

        [Test]
        public void InputProviders_CanBeUsedInterchangeably()
        {
            // Arrange
            var legacyProvider = _inputProviderGameObject.AddComponent<LegacyInputProvider>();
            var newProvider = _inputProviderGameObject.AddComponent<NewInputSystemProvider>();

            // Act - Cast to interface
            IInputProvider provider1 = legacyProvider;
            IInputProvider provider2 = newProvider;

            // Assert - Both should provide the same interface methods
            Assert.IsNotNull(provider1);
            Assert.IsNotNull(provider2);
            Assert.DoesNotThrow(() => provider1.GetPointerPosition());
            Assert.DoesNotThrow(() => provider2.GetPointerPosition());
            
            // Suppress expected logs
            LogAssert.NoUnexpectedReceived();
        }
    }
}
