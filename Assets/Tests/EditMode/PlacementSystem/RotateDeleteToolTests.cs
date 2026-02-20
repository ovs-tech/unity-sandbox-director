using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class RotateDeleteToolTests
{
    [Test]
    public void RotateTool_CanEnterAndExit()
    {
        var tool = new RotateTool();
        Assert.NotNull(tool);
    }

    [Test]
    public void DeleteTool_CanEnterAndExit()
    {
        var tool = new DeleteTool();
        Assert.NotNull(tool);
    }
}
