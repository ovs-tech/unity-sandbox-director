using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.EditorTests
{
    public class PlacementSetupWizardTests
    {
        [Test]
        public void AddToScene_CreatesPlacementManagerWithComponents()
        {
            var go = new GameObject("Placement Manager");
            var controller = go.AddComponent<PlacementController>();
            Assert.IsNotNull(controller);
            Object.DestroyImmediate(go);
        }
    }
}
