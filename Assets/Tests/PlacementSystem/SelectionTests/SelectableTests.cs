using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Selection;

namespace PlacementSystem.Tests
{
    /// <summary>
    /// Unit tests for the Selectable component.
    /// </summary>
    public class SelectableTests
    {
        private GameObject _testObject;
        private Selectable _selectable;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestSelectable");
            _selectable = _testObject.AddComponent<Selectable>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
                Object.DestroyImmediate(_testObject);
        }

        [Test]
        public void Selectable_InitialState_IsNotSelected()
        {
            // Assert
            Assert.IsFalse(_selectable.IsSelected, "Selectable should not be selected initially");
        }

        [Test]
        public void Selectable_InitialState_IsSelectable()
        {
            // Assert
            Assert.IsTrue(_selectable.IsSelectable, "Selectable should be selectable by default");
        }

        [Test]
        public void Selectable_CanSetIsSelectable()
        {
            // Act
            _selectable.IsSelectable = false;

            // Assert
            Assert.IsFalse(_selectable.IsSelectable, "IsSelectable should be false after setting");
        }

        [Test]
        public void Selectable_SelectionPriority_ReturnsDefaultZero()
        {
            // Assert
            Assert.AreEqual(0, _selectable.SelectionPriority, "Default selection priority should be 0");
        }

        [Test]
        public void Selectable_NotifySelected_MarksAsSelected()
        {
            // Act
            CallNotifySelected(_selectable);

            // Assert
            Assert.IsTrue(_selectable.IsSelected, "Should be marked as selected after notification");
        }

        [Test]
        public void Selectable_NotifyDeselected_MarksAsNotSelected()
        {
            // Arrange
            CallNotifySelected(_selectable);

            // Act
            CallNotifyDeselected(_selectable);

            // Assert
            Assert.IsFalse(_selectable.IsSelected, "Should be marked as not selected after deselection");
        }

        [Test]
        public void Selectable_NotifySelected_TriggersEvent()
        {
            // Arrange
            bool eventTriggered = false;
            _selectable.OnSelected.AddListener(() => eventTriggered = true);

            // Act
            CallNotifySelected(_selectable);

            // Assert
            Assert.IsTrue(eventTriggered, "OnSelected event should be triggered");
        }

        [Test]
        public void Selectable_NotifyDeselected_TriggersEvent()
        {
            // Arrange
            bool eventTriggered = false;
            CallNotifySelected(_selectable); // First select it
            _selectable.OnDeselected.AddListener(() => eventTriggered = true);

            // Act
            CallNotifyDeselected(_selectable);

            // Assert
            Assert.IsTrue(eventTriggered, "OnDeselected event should be triggered");
        }

        [Test]
        public void Selectable_NotifySelectedTwice_DoesNotTriggerEventTwice()
        {
            // Arrange
            int eventCount = 0;
            _selectable.OnSelected.AddListener(() => eventCount++);

            // Act
            CallNotifySelected(_selectable);
            CallNotifySelected(_selectable);

            // Assert
            Assert.AreEqual(1, eventCount, "Event should only trigger once");
        }

        [Test]
        public void Selectable_NotifyDeselectedTwice_DoesNotTriggerEventTwice()
        {
            // Arrange
            int eventCount = 0;
            CallNotifySelected(_selectable);
            _selectable.OnDeselected.AddListener(() => eventCount++);

            // Act
            CallNotifyDeselected(_selectable);
            CallNotifyDeselected(_selectable);

            // Assert
            Assert.AreEqual(1, eventCount, "Event should only trigger once");
        }

        // Helper methods to call internal methods via reflection
        private void CallNotifySelected(Selectable selectable)
        {
            var method = typeof(Selectable).GetMethod("NotifySelected",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(selectable, null);
        }

        private void CallNotifyDeselected(Selectable selectable)
        {
            var method = typeof(Selectable).GetMethod("NotifyDeselected",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(selectable, null);
        }
    }
}
