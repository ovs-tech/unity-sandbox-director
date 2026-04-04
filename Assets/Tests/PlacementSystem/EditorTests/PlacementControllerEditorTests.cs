using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Systems.PlacementSystem.Core;

namespace Systems.PlacementSystem.EditorTests
{
    public class PlacementControllerEditorTests
    {
        [Test]
        public void CustomEditorExistsForPlacementController()
        {
            var go = new GameObject("TestObj");
            var controller = go.AddComponent<PlacementController>();
            var editor = UnityEditor.Editor.CreateEditor(controller);
            Assert.IsNotNull(editor);
            StringAssert.Contains("PlacementController", editor.GetType().Name);
            StringAssert.Contains("Editor", editor.GetType().Name);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(editor);
        }
    }
}
