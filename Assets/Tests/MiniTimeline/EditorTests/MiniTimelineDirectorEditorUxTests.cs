using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.MiniTimeline.Core;

namespace Systems.MiniTimeline.EditorTests
{
    public class MiniTimelineDirectorEditorUxTests
    {
        private GameObject _gameObject;
        private MiniTimelineDirector _director;
        private UnityEditor.Editor _editor;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("MiniTimelineDirectorEditorUxTests");
            _director = _gameObject.AddComponent<MiniTimelineDirector>();
            _editor = UnityEditor.Editor.CreateEditor(_director);
        }

        [TearDown]
        public void TearDown()
        {
            if (_editor != null)
            {
                Object.DestroyImmediate(_editor);
            }

            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        [Test]
        public void CreateBindingContextComponent_IsUndoable()
        {
            Assert.IsNull(_gameObject.GetComponent<BindableObjectManager>());

            InvokePrivateMethod("CreateBindingContextComponent");

            Assert.IsNotNull(_gameObject.GetComponent<BindableObjectManager>());

            Undo.PerformUndo();

            Assert.IsNull(_gameObject.GetComponent<BindableObjectManager>());
        }

        [Test]
        public void UpdateClipFromForm_UpdatesClipTimingAndId()
        {
            _director.CreateNewProject("EditorUxProject", 10f, 30f);

            var track = new Tracks.MovementTrack
            {
                Id = "track_editor_ux",
                Name = "Track",
                Enabled = true
            };

            LogAssert.Expect(LogType.Error, new Regex(@"\[MovementTrack\] Target object for track 'track_editor_ux' is not a Transform, GameObject, or Component"));
            _director.AddTrack(track);

            var clip = new Tracks.MovementClip
            {
                Id = "clip_old",
                Start = 0f,
                Duration = 1f
            };
            _director.AddClip(clip, track.Id);

            SetPrivateField("editingTrack", track);
            SetPrivateField("editingClip", clip);
            SetPrivateField("isCreateMode", false);
            SetPrivateField("formClipId", "clip_updated");
            SetPrivateField("formStartTime", 2f);
            SetPrivateField("formDuration", 3f);

            InvokePrivateMethod("UpdateClipFromForm");

            Assert.AreEqual("clip_updated", clip.Id);
            Assert.AreEqual(2f, clip.Start);
            Assert.AreEqual(3f, clip.Duration);
        }

        [Test]
        public void SessionState_RoundTripsFoldoutsAndPaths()
        {
            SetPrivateField("showProjectManagement", false);
            SetPrivateField("showTrackInfo", true);
            SetPrivateField("lastSavedPath", "saved-path");
            SetPrivateField("lastLoadedPath", "loaded-path");

            InvokePrivateMethod("SaveSessionState");

            SetPrivateField("showProjectManagement", true);
            SetPrivateField("showTrackInfo", false);
            SetPrivateField("lastSavedPath", string.Empty);
            SetPrivateField("lastLoadedPath", string.Empty);

            InvokePrivateMethod("LoadSessionState");

            Assert.AreEqual(false, GetPrivateField<bool>("showProjectManagement"));
            Assert.AreEqual(true, GetPrivateField<bool>("showTrackInfo"));
            Assert.AreEqual("saved-path", GetPrivateField<string>("lastSavedPath"));
            Assert.AreEqual("loaded-path", GetPrivateField<string>("lastLoadedPath"));
        }

        private void InvokePrivateMethod(string methodName)
        {
            var method = _editor.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Could not find private method '{methodName}'");
            method.Invoke(_editor, null);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            var field = _editor.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Could not find private field '{fieldName}'");
            field.SetValue(_editor, value);
        }

        private T GetPrivateField<T>(string fieldName)
        {
            var field = _editor.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Could not find private field '{fieldName}'");
            return (T)field.GetValue(_editor);
        }
    }
}
