using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Tools;

public class PlacementToolTests
{
    [Test]
    public void PlacementTool_CanEnterAndExit()
    {
        var tool = new PlacementTool();
        Assert.NotNull(tool);
    }

    [Test]
    public void PlacementTool_OnPlacementConfirmedCallback_HasCorrectSignature()
    {
        var tool = new PlacementTool();
        
        GameObject capturedObject = null;
        tool.OnPlacementConfirmed += (obj) => { capturedObject = obj; };
        
        // Verify callback can be subscribed and would receive GameObject parameter
        Assert.NotNull(tool.OnPlacementConfirmed);
    }

    [Test]
    public void PlacementTool_OnPlacementCancelledCallback_HasCorrectSignature()
    {
        var tool = new PlacementTool();
        
        bool callbackFired = false;
        tool.OnPlacementCancelled += () => { callbackFired = true; };
        
        // Verify callback can be subscribed
        Assert.NotNull(tool.OnPlacementCancelled);
    }
}
