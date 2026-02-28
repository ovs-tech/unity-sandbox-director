using NUnit.Framework;
using UnityEngine;
using Systems.PlacementSystem.Core.Components;

namespace Systems.PlacementSystem.EditorTests
{
    public class PartEditorTests
    {
        [Test]
        public void CustomEditorExistsForPart()
        {
            var go = new GameObject("TestObj");
            var placeable = go.AddComponent<PlacementPart>();
            var editor = UnityEditor.Editor.CreateEditor(placeable);
            Assert.AreEqual("PartEditor", editor.GetType().Name);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(editor);
        }
    }
}
