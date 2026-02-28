using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.PlacementSystem.Persistence
{
    [Serializable]
    public class PlacedObjectRecord
    {
        public string id;
        public string prefabName;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public string customName;
        public Dictionary<string, object> properties;

        public PlacedObjectRecord()
        {
            id = Guid.NewGuid().ToString();
            prefabName = string.Empty;
            position = Vector3.zero;
            rotation = Vector3.zero;
            scale = Vector3.one;
            customName = string.Empty;
            properties = new Dictionary<string, object>();
        }
    }

    [Serializable]
    public class PlacementSceneData
    {
        public string sceneName;
        public List<PlacedObjectRecord> placedObjects = new List<PlacedObjectRecord>();

        public PlacementSceneData()
        {
            sceneName = string.Empty;
            placedObjects = new List<PlacedObjectRecord>();
        }
    }
}
