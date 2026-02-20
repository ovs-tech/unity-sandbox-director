using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void RemoveObject_WhenNullOrWithoutTransformable_ReturnsFalse()
        {
            // Null object
            Assert.IsFalse(_builder.RemoveObject((GameObject)null));

            // Object without TransformableItem
            var temp = new GameObject("Temp_NoDraggable");
            try
            {
                Assert.IsFalse(_builder.RemoveObject(temp));
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
        }

        [Test]
        public void RemoveObject_ById_NotFound_ReturnsFalse()
        {
            Assert.IsFalse(_builder.RemoveObject("this-id-does-not-exist-123"));
        }

        [Test]
        public void SetSceneBounds_And_Offset_UpdateProperties()
        {
            var bounds = new Vector3(10f, 8f, 6f);
            var offset = new Vector3(1f, 2f, 3f);

            _builder.SetSceneBounds(bounds);
            _builder.SetSceneBoundsOffset(offset);

            Assert.AreEqual(bounds, _builder.SceneBounds);
            Assert.AreEqual(offset, _builder.SceneBoundsOffset);
        }

        [Test]
        public void DropIndicator_DefaultPosition_WhenNotCreated_ReturnsZero()
        {
            // When no drop indicator exists, method should return Vector3.zero
            Assert.AreEqual(Vector3.zero, _builder.GetDropIndicatorPosition());
        }

        [Test]
        public void SetDropIndicatorEnabled_TogglesFlag()
        {
            _builder.SetDropIndicatorEnabled(false);
            Assert.IsFalse(_builder.DropIndicatorEnabled);

            _builder.SetDropIndicatorEnabled(true);
            Assert.IsTrue(_builder.DropIndicatorEnabled);
        }

        [Test]
        public void IsValidPlacementPosition_RespectsSceneBounds()
        {
            // Make bounds small around origin
            _builder.SetSceneBounds(new Vector3(2f, 2f, 2f));
            _builder.SetSceneBoundsOffset(Vector3.zero);

            // Position inside bounds
            Assert.IsTrue(_builder.IsValidPlacementPosition(Vector3.zero));

            // Position outside bounds
            Assert.IsFalse(_builder.IsValidPlacementPosition(new Vector3(10f, 0f, 0f)));
        }

        [Test]
        public void StartPlacement_And_ConfirmRegistersPlacedObject()
        {
            // Create a simple prefab and add to a library
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "TestPrefab";

            var lib = ScriptableObject.CreateInstance<Systems.SceneSandbox.Data.SceneObjectLibrary>();
            var objData = new Systems.SceneSandbox.Data.SceneObjectData("Cube", prefab, Systems.SceneSandbox.Data.SceneObjectType.Prop);
            lib.AddObject(objData);

            // Assign library to builder and initialize placement
            _builder.SetObjectLibrary(lib);

            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            // Start placement using builder (delegates to PlacementSystem)
            _builder.StartPlacement(objData.id, screenCenter);

            // PlacementSystem should have an active current object
            Assert.IsTrue(_builder.PlacementSystem.IsActive);
            Assert.IsNotNull(_builder.PlacementSystem.CurrentObject);

            // Confirm placement via builder (this will invoke serializer registration)
            _builder.ConfirmPlacement();

            // After confirmation, PlacementSystem.CurrentObject should be null (reset)
            Assert.IsNull(_builder.PlacementSystem.CurrentObject);

            // SceneSerializer should have recorded the placed object
            Assert.IsNotNull(_builder.SceneSerializer.CurrentScene);
            Assert.GreaterOrEqual(_builder.SceneSerializer.CurrentScene.placedObjects.Count, 1);

            // Clean up created prefab
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void CancelPlacement_DestroysNewlyCreatedObject()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.name = "TempPrefab";
            var lib = ScriptableObject.CreateInstance<Systems.SceneSandbox.Data.SceneObjectLibrary>();
            var objData = new Systems.SceneSandbox.Data.SceneObjectData("Sphere", prefab, Systems.SceneSandbox.Data.SceneObjectType.Prop);
            lib.AddObject(objData);
            _builder.SetObjectLibrary(lib);

            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            _builder.StartPlacement(objData.id, screenCenter);

            // Ensure created
            var created = _builder.PlacementSystem.CurrentObject;
            Assert.IsNotNull(created);

            // Cancel placement
            _builder.CancelPlacement();

            // Placement system should be idle and current object null
            Assert.IsFalse(_builder.PlacementSystem.IsActive);
            Assert.IsNull(_builder.PlacementSystem.CurrentObject);

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void ProjectAndSceneManagement_CreateDuplicateRenameCount()
        {
            // Create project
            _builder.CreateNewProject("MyTestProject");
            Assert.IsNotNull(_builder.SceneSerializer.CurrentProject);
            Assert.AreEqual("MyTestProject", _builder.SceneSerializer.CurrentProject.projectName);

            // Create new scene in project
            var newScene = _builder.CreateNewSceneInProject("Level 2");
            Assert.IsNotNull(newScene);
            Assert.AreEqual(2, _builder.GetSceneCount());

            // Duplicate current scene
            var duplicate = _builder.DuplicateCurrentScene();
            Assert.IsNotNull(duplicate);
            Assert.AreEqual(3, _builder.GetSceneCount());

            // Rename current scene
            string nameBefore = _builder.SceneSerializer.CurrentScene.sceneName;
            _builder.RenameCurrentScene("RenamedScene");
            Assert.AreEqual("RenamedScene", _builder.SceneSerializer.CurrentScene.sceneName);

            // Get metadata list
            var metadata = _builder.GetAllSceneMetadata();
            Assert.IsNotNull(metadata);
            Assert.GreaterOrEqual(metadata.Count, 1);
        }

        [Test]
        public void RemoveObject_AfterPlacement_RemovesFromScene()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "RemovePrefab";
            var lib = ScriptableObject.CreateInstance<Systems.SceneSandbox.Data.SceneObjectLibrary>();
            var objData = new Systems.SceneSandbox.Data.SceneObjectData("Cube2", prefab, Systems.SceneSandbox.Data.SceneObjectType.Prop);
            lib.AddObject(objData);
            _builder.SetObjectLibrary(lib);

            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            _builder.StartPlacement(objData.id, screenCenter);
            // Confirm placement and ensure scene has object
            _builder.ConfirmPlacement();
            Assert.GreaterOrEqual(_builder.SceneSerializer.CurrentScene.placedObjects.Count, 1);

            // Find the placed object in the scene (PlacementSystem tracks placed objects)
            var placedEntries = _builder.SceneSerializer.CurrentScene.placedObjects;
            var first = placedEntries[0];

            // Try to find the actual GameObject by placed id via PlacementSystem
            var placedObj = _builder.PlacementSystem.GetPlacedObject(first.id);
            Assert.IsNotNull(placedObj);

            // Remove via builder
            bool removed = _builder.RemoveObject(placedObj);
            Assert.IsTrue(removed);
            Assert.IsFalse(_builder.SceneSerializer.CurrentScene.placedObjects.Exists(p => p.id == first.id));

            Object.DestroyImmediate(prefab);
        }
    }
}
