using NUnit.Framework;
using UnityEditor;

namespace Systems.PlacementSystem.EditorTests
{
    public class PrefabCreatorWindowTests
    {
        [Test]
        public void WindowCanBeOpened()
        {
            var window = EditorWindow.GetWindow(typeof(Systems.PlacementSystem.Editor.PrefabCreatorWindow));
            Assert.IsNotNull(window);
            window.Close();
        }
    }
}
