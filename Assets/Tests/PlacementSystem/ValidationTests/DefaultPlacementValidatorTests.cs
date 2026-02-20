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
            bool result = validator.IsPlacementValid(Vector3.zero, Quaternion.identity, null);
            Assert.IsFalse(result);
        }

        [Test]
        public void IsPlacementValid_GhostWithoutPlaceableObject_ReturnsTrue()
        {
            var validator = new DefaultPlacementValidator();
            var ghost = new GameObject("Ghost");
            
            bool result = validator.IsPlacementValid(Vector3.zero, Quaternion.identity, ghost);
            
            Assert.IsTrue(result);
            Object.DestroyImmediate(ghost);
        }
    }
}
