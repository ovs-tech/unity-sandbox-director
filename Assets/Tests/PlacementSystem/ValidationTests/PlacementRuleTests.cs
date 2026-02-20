using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Validation;

namespace PlacementSystem.Tests
{
    /// <summary>
    /// Tests for the validation rule system.
    /// </summary>
    public class PlacementRuleTests
    {
        private GameObject _testGameObject;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestObject");
            _testGameObject.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }

        [Test]
        public void ClearanceRule_NoObstacles_ReturnsTrue()
        {
            // Arrange
            var rule = ScriptableObject.CreateInstance<ClearanceRule>();
            // Configure rule via reflection
            SetMaxAllowedOverlaps(rule, 0);
            SetCheckBoxSize(rule, new Vector3(1f, 2f, 1f));
            SetObstacleLayer(rule, 0); // Only check Default layer
            
            Vector3 position = new Vector3(10, 10, 10); // Far away from test object
            Quaternion rotation = Quaternion.identity;

            // Act
            bool result = rule.CheckRule(position, rotation, _testGameObject);

            // Assert
            Assert.IsTrue(result, "ClearanceRule should pass when no obstacles present");

            // Cleanup
            Object.DestroyImmediate(rule);
        }

        [Test]
        public void RequireSurfaceRule_NoSurface_ReturnsFalse()
        {
            // Arrange
            var rule = ScriptableObject.CreateInstance<RequireSurfaceRule>();
            Vector3 position = new Vector3(0, 100, 0); // High in the air
            Quaternion rotation = Quaternion.identity;

            // Act
            bool result = rule.CheckRule(position, rotation, _testGameObject);

            // Assert
            Assert.IsFalse(result, "RequireSurfaceRule should fail when no surface below");

            // Cleanup
            Object.DestroyImmediate(rule);
        }

        [Test]
        public void RequireSurfaceRule_WithValidSurface_ReturnsTrue()
        {
            // Arrange
            var rule = ScriptableObject.CreateInstance<RequireSurfaceRule>();
            SetRequiredSurfaceLayer(rule, 0); // Check Default layer
            
            // Create a ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = Vector3.zero;
            ground.layer = LayerMask.NameToLayer("Default");

            Vector3 position = new Vector3(0, 1, 0);
            Quaternion rotation = Quaternion.identity;

            // Act
            bool result = rule.CheckRule(position, rotation, _testGameObject);

            // Assert
            Assert.IsTrue(result, "RequireSurfaceRule should pass when valid surface exists");

            // Cleanup
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(rule);
            
            // Suppress warnings about primitive colliders
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PlaceableObject_NoRules_ReturnsTrue()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();

            // Act
            bool result = placeableObject.ValidatePlacement(Vector3.zero, Quaternion.identity, _testGameObject);

            // Assert
            Assert.IsTrue(result, "PlaceableObject with no rules should always validate");
        }

        [Test]
        public void PlaceableObject_AddAndRemoveRule_Works()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            var rule = ScriptableObject.CreateInstance<ClearanceRule>();

            // Act
            placeableObject.AddRule(rule);
            var rules = placeableObject.GetRules();
            int countAfterAdd = rules.Count;

            placeableObject.RemoveRule(rule);
            int countAfterRemove = placeableObject.GetRules().Count;

            // Assert
            Assert.AreEqual(1, countAfterAdd, "Rule should be added");
            Assert.AreEqual(0, countAfterRemove, "Rule should be removed");

            // Cleanup
            Object.DestroyImmediate(rule);
        }

        [Test]
        public void PlaceableObject_NullRule_DoesNotThrow()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();

            // Act & Assert
            Assert.DoesNotThrow(() => placeableObject.AddRule(null));
            Assert.DoesNotThrow(() => placeableObject.RemoveRule(null));
        }

        [Test]
        public void PlaceableObject_ValidatePlacement_WithPassingRule_ReturnsTrue()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            var rule = ScriptableObject.CreateInstance<ClearanceRule>();
            SetMaxAllowedOverlaps(rule, 0);
            SetCheckBoxSize(rule, new Vector3(1f, 1f, 1f));
            SetObstacleLayer(rule, 0);
            
            placeableObject.AddRule(rule);
            Vector3 position = new Vector3(100, 100, 100); // Far from any obstacles

            // Act
            bool result = placeableObject.ValidatePlacement(position, Quaternion.identity, _testGameObject);

            // Assert
            Assert.IsTrue(result, "Validation should pass with passing rule");

            // Cleanup
            Object.DestroyImmediate(rule);
        }

        [Test]
        public void PlaceableObject_ValidatePlacement_WithFailingRule_ReturnsFalse()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            var rule = ScriptableObject.CreateInstance<RequireSurfaceRule>();
            SetRequiredSurfaceLayer(rule, 0);
            
            placeableObject.AddRule(rule);
            Vector3 position = new Vector3(0, 1000, 0); // High in the air, no surface

            // Expect log about rule failure
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Placement rule failed.*"));

            // Act
            bool result = placeableObject.ValidatePlacement(position, Quaternion.identity, _testGameObject);

            // Assert
            Assert.IsFalse(result, "Validation should fail with failing rule");

            // Cleanup
            Object.DestroyImmediate(rule);
        }

        [Test]
        public void PlaceableObject_ValidatePlacement_WithMultipleRules_AllMustPass()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            
            var passingRule = ScriptableObject.CreateInstance<ClearanceRule>();
            SetMaxAllowedOverlaps(passingRule, 0);
            SetCheckBoxSize(passingRule, new Vector3(1f, 1f, 1f));
            SetObstacleLayer(passingRule, 0);
            
            var failingRule = ScriptableObject.CreateInstance<RequireSurfaceRule>();
            SetRequiredSurfaceLayer(failingRule, 0);
            
            placeableObject.AddRule(passingRule);
            placeableObject.AddRule(failingRule);
            
            Vector3 position = new Vector3(100, 1000, 100); // Far from obstacles but no surface

            // Expect log about rule failure
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Placement rule failed.*"));

            // Act
            bool result = placeableObject.ValidatePlacement(position, Quaternion.identity, _testGameObject);

            // Assert
            Assert.IsFalse(result, "Validation should fail when any rule fails");

            // Cleanup
            Object.DestroyImmediate(passingRule);
            Object.DestroyImmediate(failingRule);
        }

        [Test]
        public void PlaceableObject_RequiredSocketType_ReturnsCorrectValue()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            var socketType = ScriptableObject.CreateInstance<Systems.PlacementSystem.Sockets.SocketType>();
            
            // Set via reflection
            var field = typeof(Systems.PlacementSystem.Validation.PlaceableObject).GetField("_requiredSocketType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(placeableObject, socketType);

            // Act
            var result = placeableObject.RequiredSocketType;

            // Assert
            Assert.AreEqual(socketType, result, "RequiredSocketType should return assigned value");

            // Cleanup
            Object.DestroyImmediate(socketType);
        }

        [Test]
        public void PlaceableObject_SnapRange_ReturnsCorrectValue()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            float expectedRange = 5.5f;
            
            // Set via reflection
            var field = typeof(Systems.PlacementSystem.Validation.PlaceableObject).GetField("_snapRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(placeableObject, expectedRange);

            // Act
            var result = placeableObject.SnapRange;

            // Assert
            Assert.AreEqual(expectedRange, result, "SnapRange should return assigned value");
        }

        [Test]
        public void PlaceableObject_GetRules_ReturnsReadOnlyList()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            var rule = ScriptableObject.CreateInstance<ClearanceRule>();
            placeableObject.AddRule(rule);

            // Act
            var rules = placeableObject.GetRules();

            // Assert
            Assert.IsNotNull(rules, "GetRules should return a list");
            Assert.AreEqual(1, rules.Count, "List should contain the added rule");
            Assert.AreEqual(rule, rules[0], "Rule should match the added rule");

            // Cleanup
            Object.DestroyImmediate(rule);
        }

        [Test]
        public void PlaceableObject_AddDuplicateRule_DoesNotAddTwice()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            var rule = ScriptableObject.CreateInstance<ClearanceRule>();

            // Act
            placeableObject.AddRule(rule);
            placeableObject.AddRule(rule); // Add same rule again
            var rules = placeableObject.GetRules();

            // Assert
            Assert.AreEqual(1, rules.Count, "Duplicate rule should not be added");

            // Cleanup
            Object.DestroyImmediate(rule);
        }

        [Test]
        public void PlaceableObject_ValidatePlacement_WithNullRule_SkipsRule()
        {
            // Arrange
            var placeableObject = _testGameObject.AddComponent<PlaceableObject>();
            
            // Add null rule via reflection to simulate corrupted data
            var rulesField = typeof(Systems.PlacementSystem.Validation.PlaceableObject).GetField("_placementRules",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rulesList = (System.Collections.Generic.List<PlacementRule>)rulesField.GetValue(placeableObject);
            rulesList.Add(null);

            // Expect warning about null rule
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Null placement rule found.*"));

            // Act
            bool result = placeableObject.ValidatePlacement(Vector3.zero, Quaternion.identity, _testGameObject);

            // Assert
            Assert.IsTrue(result, "Validation should pass when null rule is skipped");
        }

        /// <summary>
        /// Helper method to set private field values via reflection for testing.
        /// </summary>
        private void SetMaxAllowedOverlaps(ClearanceRule rule, int value)
        {
            var field = typeof(ClearanceRule).GetField("_maxAllowedOverlaps", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(rule, value);
        }

        private void SetCheckBoxSize(ClearanceRule rule, Vector3 value)
        {
            var field = typeof(ClearanceRule).GetField("_checkBoxSize", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(rule, value);
        }

        private void SetObstacleLayer(ClearanceRule rule, int layerMask)
        {
            var field = typeof(ClearanceRule).GetField("_obstacleLayer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                // LayerMask is a struct with internal m_Mask field. Use GetMask to create properly.
                LayerMask mask = LayerMask.GetMask("Default");
                field.SetValue(rule, mask);
            }
        }

        private void SetRequiredSurfaceLayer(RequireSurfaceRule rule, int layerMask)
        {
            var field = typeof(RequireSurfaceRule).GetField("_requiredSurfaceLayer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                // LayerMask is a struct with internal m_Mask field. Use GetMask to create properly.
                LayerMask mask = LayerMask.GetMask("Default");
                field.SetValue(rule, mask);
            }
        }
    }
}
