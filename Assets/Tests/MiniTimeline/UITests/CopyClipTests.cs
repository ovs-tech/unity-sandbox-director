using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.UI;
using Systems.MiniTimeline.Core;
using System.Reflection;
using System;

namespace Systems.MiniTimeline.UI.Tests
{
    public class CopyClipTests
    {
        [System.Serializable]
        private class TestClip : MiniClipBase
        {
            public int testValue;
        }

        [Test]
        public void CopyClip_StoresSerializedClipInClipboard()
        {
            // Arrange
            var editorGO = new GameObject("Editor");
            var uiDoc = editorGO.AddComponent<UnityEngine.UIElements.UIDocument>();
            uiDoc.rootVisualElement.Add(new UnityEngine.UIElements.VisualElement { name = "timeline-editor" });
            var editor = editorGO.AddComponent<TimelineEditorUIToolkit>();

            var clip = new TestClip { Id = "TestClip1", Start = 1.0f, Duration = 2.0f, testValue = 42 };

            var clipUI = new ClipUIToolkit();
            // Use reflection to set private clip field
            var clipField = typeof(ClipUIToolkit).GetField("clip", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(clipField, "Could not find 'clip' field in ClipUIToolkit");
            clipField.SetValue(clipUI, clip);

            // Act
            // Invoke CopyClip
            var copyMethod = typeof(TimelineEditorUIToolkit).GetMethod("CopyClip", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(copyMethod, "Could not find 'CopyClip' method");
            copyMethod.Invoke(editor, new object[] { clipUI });

            // Assert
            // Check clipboard
            var clipboardField = typeof(TimelineEditorUIToolkit).GetField("clipClipboard", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(clipboardField, "Could not find 'clipClipboard' field");

            var clipboard = clipboardField.GetValue(null);
            Assert.IsNotNull(clipboard, "Clipboard should not be null");

            // Clipboard is a private nested class, so use reflection to read fields
            var clipboardType = clipboard.GetType();
            var typeField = clipboardType.GetField("type");
            var dataField = clipboardType.GetField("data");

            Assert.IsNotNull(typeField, "Could not find 'type' field in clipboard data");
            Assert.IsNotNull(dataField, "Could not find 'data' field in clipboard data");

            string typeName = (string)typeField.GetValue(clipboard);
            string jsonData = (string)dataField.GetValue(clipboard);

            Assert.AreEqual(typeof(TestClip).AssemblyQualifiedName, typeName, "Type mismatch");

            var deserializedClip = (TestClip)JsonUtility.FromJson(jsonData, typeof(TestClip));
            Assert.IsNotNull(deserializedClip, "Deserialized clip is null");
            Assert.AreEqual(clip.Id, deserializedClip.Id);
            Assert.AreEqual(clip.Start, deserializedClip.Start);
            Assert.AreEqual(clip.testValue, deserializedClip.testValue);

            // Cleanup
            GameObject.DestroyImmediate(editorGO);
        }
    }
}
