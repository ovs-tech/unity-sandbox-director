using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class PlacementToolTests
{
    [Test]
    public void PlacementTool_CanEnterAndExit()
    {
        var tool = new PlacementTool();
        Assert.NotNull(tool);
    }
}
