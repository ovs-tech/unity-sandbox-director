using NUnit.Framework;
using Systems.PlacementSystem.Tools;
using Systems.CommandSystem;
using Systems.PlacementSystem.Core;
using UnityEngine;

public class PlacementToolContextTests
{
    private class TestCommandExecutor : ICommandExecutor
    {
        public int ExecuteCount { get; private set; }
        public ICommand LastCommand { get; private set; }
        public bool LastMerge { get; private set; }
        public string LastNamespace { get; private set; }

        public void Execute(ICommand command, bool merge, string commandNamespace)
        {
            ExecuteCount++;
            LastCommand = command;
            LastMerge = merge;
            LastNamespace = commandNamespace;
        }
    }

    [Test]
    public void Context_IsReadOnly_PropertiesAreAssigned()
    {
        var cameraObject = new GameObject("TestCamera");
        var camera = cameraObject.AddComponent<Camera>();

        try
        {
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
            Assert.NotNull(context.CommandExecutor);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void Context_UsesProvidedCommandExecutor()
    {
        var cameraObject = new GameObject("TestCamera");
        var camera = cameraObject.AddComponent<Camera>();

        try
        {
            var executor = new TestCommandExecutor();
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
                true,
                false,
                true,
                -1,
                -1,
                -1,
                null,
                null,
                new ToolStateRegistry(),
                executor);

            Assert.AreSame(executor, context.CommandExecutor);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }
}
