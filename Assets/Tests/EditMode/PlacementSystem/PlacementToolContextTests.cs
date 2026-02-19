using NUnit.Framework;
using Systems.PlacementSystem.Tools;
using UnityEngine;

public class PlacementToolContextTests
{
    [Test]
    public void Context_IsReadOnly_PropertiesAreAssigned()
    {
        var cameraObject = new GameObject("TestCamera");
        var camera = cameraObject.AddComponent<Camera>();

        var context = new PlacementToolContext(
            null,
            null,
            null,
            null,
            null,
            camera,
            12.5f,
            45f,
            90f,
            true);

        Assert.NotNull(context);
        Assert.AreEqual(camera, context.PlacementCamera);
        Assert.AreEqual(12.5f, context.MaxRaycastDistance);
        Assert.AreEqual(45f, context.RotationIncrementDegrees);
        Assert.AreEqual(90f, context.RotationSnapDegrees);
        Assert.IsTrue(context.AllowMultiSelection);
        Assert.NotNull(context.SelectionState);

        Object.DestroyImmediate(cameraObject);
    }
}
