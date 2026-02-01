using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.SceneSandbox.Core;
using Systems.SceneSandbox.Data;

namespace Systems.SceneSandbox.Core.Tests
{
    public class SceneSandboxBuilderTests
    {
        private GameObject _gameObject;
        private SceneSandboxBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("SceneSandboxBuilderTest");
            _builder = _gameObject.AddComponent<SceneSandboxBuilder>();

            // Add required components that might not be auto-added or need configuration
            _gameObject.AddComponent<SandboxInputManager>();
            _gameObject.AddComponent<CameraRaycaster>();
            _gameObject.AddComponent<GridManager>();
            _gameObject.AddComponent<PlacementSystem>();
            _gameObject.AddComponent<SelectionManager>();
            _gameObject.AddComponent<TransformController>();
            _gameObject.AddComponent<SceneSerializer>();
            _gameObject.AddComponent<PreviewController>();
            _gameObject.AddComponent<SandboxGizmoRenderer>();

            // Ensure camera exists for raycasting
            if (Camera.main == null)
            {
                var cameraGO = new GameObject("MainCamera");
                cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }

            var camera = GameObject.FindGameObjectWithTag("MainCamera");
            if (camera != null && camera.name == "MainCamera")
            {
                Object.DestroyImmediate(camera);
            }
        }

        [Test]
        public void Initialization_SetsDefaultValues()
        {
            // Assert
            Assert.IsNotNull(_builder);
            Assert.IsNotNull(_builder.PlacementSystem);
            Assert.IsNotNull(_builder.SceneSerializer);
            Assert.AreEqual(SandboxMode.Build, _builder.CurrentMode);
        }

        [Test]
        public void ToggleMode_SwitchesBetweenBuildAndPlay()
        {
            // Arrange
            _builder.SetMode(SandboxMode.Build);

            // Act
            _builder.ToggleMode();

            // Assert
            Assert.AreEqual(SandboxMode.Play, _builder.CurrentMode);

            // Act
            _builder.ToggleMode();

            // Assert
            Assert.AreEqual(SandboxMode.Build, _builder.CurrentMode);
        }

        [Test]
        public void SetMode_SetsSpecificMode()
        {
            // Act
            _builder.SetMode(SandboxMode.Play);

            // Assert
            Assert.AreEqual(SandboxMode.Play, _builder.CurrentMode);
            Assert.IsFalse(_builder.IsInBuildMode);

            // Act
            _builder.SetMode(SandboxMode.Build);

            // Assert
            Assert.AreEqual(SandboxMode.Build, _builder.CurrentMode);
            Assert.IsTrue(_builder.IsInBuildMode);
        }

        [Test]
        public void CreateNewScene_ResetsSceneState()
        {
            // Act
            _builder.CreateNewScene("TestScene");

            // Assert
            Assert.AreEqual("TestScene", _builder.SceneSerializer.CurrentScene.sceneName);
            Assert.AreEqual(0, _builder.SceneSerializer.CurrentScene.placedObjects.Count);
        }

        [Test]
        public void CreateNewProject_CreatesProject()
        {
            // Act
            _builder.CreateNewProject("TestProject");

            // Assert
            Assert.IsNotNull(_builder.SceneSerializer.CurrentProject);
            Assert.AreEqual("TestProject", _builder.SceneSerializer.CurrentProject.projectName);
        }

        [Test]
        public void ClearScene_RemovesAllObjects()
        {
            // Arrange
            _builder.CreateNewScene("TestScene");
            // We can't easily place an object without a library and valid IDs,
            // but we can check if calling ClearScene doesn't crash and potentially resets state.
            // Ideally we would mock the PlacementSystem or SceneSerializer to inject objects.

            // Act
            _builder.ClearScene();

            // Assert
            Assert.AreEqual(0, _builder.SceneSerializer.CurrentScene.placedObjects.Count);
        }

        [Test]
        public void SetTransformMode_UpdatesMode()
        {
            // Act
            _builder.SetTransformMode(TransformModeType.Rotation);

            // Assert
            // We need to wait for the event to propagate if it's async, but here it seems synchronous via direct call
            // However, the builder updates its internal state via event callback from TransformController.
            // Since we are adding components in SetUp, and they are initialized in Awake/Start,
            // we might need to simulate a frame or call Initialize manually if Start hasn't run.
            // Components added via AddComponent run Awake immediately, but Start runs on next frame.

            // Force initialization if needed, or rely on TransformController

            // We check if the controller updated
            var controller = _gameObject.GetComponent<TransformController>();
            Assert.AreEqual(TransformModeType.Rotation, controller.CurrentMode);
        }
    }
}
