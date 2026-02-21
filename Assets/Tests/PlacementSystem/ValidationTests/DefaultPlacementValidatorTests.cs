using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Validation;
using Systems.PlacementSystem.Core;

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
        public void IsPlacementValid_GhostWithoutPlaceableObject_ReturnsTrue()
        {
            var validator = new DefaultPlacementValidator();
            var ghost = new GameObject("Ghost");
            
            var result = validator.IsPlacementValid(Vector3.zero, Quaternion.identity, ghost);
            
            Assert.IsTrue(result.IsValid);
            Object.DestroyImmediate(ghost);
        }
    }
}
