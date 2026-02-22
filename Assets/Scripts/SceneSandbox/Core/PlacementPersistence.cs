using System;
using Systems.Persistence.Core;
using Systems.SceneSandbox.Data;

namespace Systems.SceneSandbox.Core {
    // Adapter that exposes PlacementSystem's scene-placed objects as a subsystem persistence
    public class PlacementPersistence : ISubsystemPersistence {
        readonly PlacementSystem _placementSystem;
        readonly SceneConfiguration _sceneConfiguration;
        readonly string _sceneName;

        public PlacementPersistence(PlacementSystem placementSystem, SceneConfiguration sceneConfiguration, string sceneName) {
            _placementSystem = placementSystem;
            _sceneConfiguration = sceneConfiguration;
            _sceneName = sceneName ?? "scene";
        }

        public string Namespace => $"placed_objects_{_sceneName}";

        public Type DataType => typeof(SceneConfiguration);

        public object GetSaveData() {
            // Save the scene configuration which contains placed objects
            return _sceneConfiguration;
        }

        public void LoadData(object data) {
            if (data == null) return;
            var sceneConfig = data as SceneConfiguration;
            if (sceneConfig == null) return;

            // Clear existing placed objects and recreate from sceneConfig
            _placementSystem?.ClearPlacedObjects();
            if (sceneConfig.placedObjects != null) {
                foreach (var placed in sceneConfig.placedObjects) {
                    _placementSystem?.PlaceObject(placed);
                }
            }
        }
    }
}
