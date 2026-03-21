using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Validation;
using Systems.PlacementSystem.Core;
using Systems.PlacementSystem.Core.Components;

namespace Systems.PlacementSystem.Tests
{
    public class DefaultPlacementValidatorTests
    {
        [Test]
        public void IsPlacementValid_NullGhostObject_ReturnsFalse()
        {
            var validator = new DefaultPlacementValidator();
            var result = validator.IsPlacementValid(Vector3.zero, Quaternion.identity, null);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void IsPlacementValid_GhostWithoutPart_ReturnsTrue()
        {
            var validator = new DefaultPlacementValidator();
            var ghost = new GameObject("Ghost");
            
            var result = validator.IsPlacementValid(Vector3.zero, Quaternion.identity, ghost);
            
            Assert.IsTrue(result.IsValid);
            Object.DestroyImmediate(ghost);
        }

        [Test]
        public void IsPlacementValid_StrictModeWithoutPlacementPart_ReturnsFalse()
        {
            var validator = new DefaultPlacementValidator(allowPlacementWithoutPart: false, searchInChildren: true);
            var ghost = new GameObject("Ghost");

            var result = validator.IsPlacementValid(Vector3.zero, Quaternion.identity, ghost);

            Assert.IsFalse(result.IsValid);
            Object.DestroyImmediate(ghost);
        }

        [Test]
        public void IsPlacementValid_StrictModeWithChildPlacementPart_ReturnsTrue()
        {
            var validator = new DefaultPlacementValidator(allowPlacementWithoutPart: false, searchInChildren: true);
            var ghost = new GameObject("Ghost");
            var child = new GameObject("Child");
            child.transform.SetParent(ghost.transform);
            child.AddComponent<PlacementPart>();

            var result = validator.IsPlacementValid(Vector3.zero, Quaternion.identity, ghost);

            Assert.IsTrue(result.IsValid);
            Object.DestroyImmediate(ghost);
        }
    }
}
