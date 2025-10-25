using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneSandbox.Data
{
    /// <summary>
    /// Data for a placed object in the scene
    /// </summary>
    [Serializable]
    public class PlacedObjectData
    {
        public string id;
        public string objectDataId;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public string customName;
        public Dictionary<string, object> properties;
        
        public PlacedObjectData()
        {
            id = Guid.NewGuid().ToString();
            position = Vector3.zero;
            rotation = Vector3.zero;
            scale = Vector3.one;
            properties = new Dictionary<string, object>();
        }
        
        public PlacedObjectData(string objectDataId, Vector3 position) : this()
        {
            this.objectDataId = objectDataId;
            this.position = position;
        }
    }

    /// <summary>
    /// Scene configuration that can be saved and loaded
    /// Contains all placed objects and scene settings
    /// </summary>
    [Serializable]
    public class SceneConfiguration
    {
        [Header("Scene Info")]
        public string sceneId; // Unique identifier for the scene
        public string sceneName;
        public string description;
        public DateTime createdDate;
        public DateTime lastModified;
        
        [Header("Scene Objects")]
        public List<PlacedObjectData> placedObjects;
        
        [Header("Scene Settings")]
        public Vector3 defaultCameraPosition = new Vector3(0, 2, -5);
        public Vector3 defaultCameraRotation = Vector3.zero;
        public Color environmentColor = Color.gray;
        public string environmentLighting = "Default";
        
        public SceneConfiguration()
        {
            sceneId = Guid.NewGuid().ToString();
            sceneName = "New Scene";
            description = "";
            createdDate = DateTime.Now;
            lastModified = DateTime.Now;
            placedObjects = new List<PlacedObjectData>();
        }
        
        public SceneConfiguration(string sceneName) : this()
        {
            this.sceneName = sceneName;
        }
        
        /// <summary>
        /// Add a placed object to the scene
        /// </summary>
        public void AddPlacedObject(PlacedObjectData placedObject)
        {
            placedObjects.Add(placedObject);
            lastModified = DateTime.Now;
        }
        
        /// <summary>
        /// Remove a placed object from the scene
        /// </summary>
        public bool RemovePlacedObject(string objectId)
        {
            var removed = placedObjects.RemoveAll(obj => obj.id == objectId) > 0;
            if (removed)
            {
                lastModified = DateTime.Now;
            }
            return removed;
        }
        
        /// <summary>
        /// Find a placed object by ID
        /// </summary>
        public PlacedObjectData GetPlacedObject(string objectId)
        {
            return placedObjects.Find(obj => obj.id == objectId);
        }
        
        /// <summary>
        /// Clear all placed objects
        /// </summary>
        public void ClearPlacedObjects()
        {
            placedObjects.Clear();
            lastModified = DateTime.Now;
        }
    }
}