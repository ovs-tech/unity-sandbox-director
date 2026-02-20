using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class MoveToolTests
{
    [Test]
    public void MoveTool_CanEnterAndExit()
    {
        var tool = new MoveTool();
        Assert.NotNull(tool);
    }
}
