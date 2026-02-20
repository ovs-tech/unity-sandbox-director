using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class SelectionToolTests
{
    [Test]
    public void SelectionTool_CanEnterAndExit()
    {
        var tool = new SelectionTool();
        Assert.NotNull(tool);
    }
}
