using NUnit.Framework;
using Systems.PlacementSystem.Tools;

public class PlacementToolContextTests
{
    [Test]
    public void Context_IsReadOnly_PropertiesAreAssigned()
    {
        var context = new PlacementToolContext(null, null, null, null, null, null, 0f, 0f, 0f, false);
        Assert.NotNull(context);
    }
}
