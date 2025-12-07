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
        /// Update an existing placed object's data
        /// </summary>
        public bool UpdatePlacedObject(string objectId, PlacedObjectData updatedData)
        {
            var existingObject = GetPlacedObject(objectId);
            if (existingObject == null)
            {
                return false;
            }
            
            // Update the object's properties
            existingObject.objectDataId = updatedData.objectDataId;
            existingObject.position = updatedData.position;
            existingObject.rotation = updatedData.rotation;
            existingObject.scale = updatedData.scale;
            existingObject.customName = updatedData.customName;
            
            // Update properties if provided
            if (updatedData.properties != null)
            {
                existingObject.properties = updatedData.properties;
            }
            
            lastModified = DateTime.Now;
            return true;
        }
        
        /// <summary>
        /// Create a new placed object or update an existing one if it already exists
        /// </summary>
        public void CreateOrUpdatePlacedObject(PlacedObjectData placedObject)
        {
            var existingObject = GetPlacedObject(placedObject.id);
            if (existingObject != null)
            {
                // Update existing object
                UpdatePlacedObject(placedObject.id, placedObject);
            }
            else
            {
                // Add new object
                AddPlacedObject(placedObject);
            }
        }
        
        /// <summary>
        /// Update only the transform (position, rotation, scale) of a placed object
        /// </summary>
        public bool UpdatePlacedObjectTransform(string objectId, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var placedObject = GetPlacedObject(objectId);
            if (placedObject == null)
            {
                return false;
            }
            
            placedObject.position = position;
            placedObject.rotation = rotation;
            placedObject.scale = scale;
            lastModified = DateTime.Now;
            return true;
        }
        
        /// <summary>
        /// Update only the position of a placed object
        /// </summary>
        public bool UpdatePlacedObjectPosition(string objectId, Vector3 position)
        {
            var placedObject = GetPlacedObject(objectId);
            if (placedObject == null)
            {
                return false;
            }
            
            placedObject.position = position;
            lastModified = DateTime.Now;
            return true;
        }
        
        /// <summary>
        /// Update only the rotation of a placed object
        /// </summary>
        public bool UpdatePlacedObjectRotation(string objectId, Vector3 rotation)
        {
            var placedObject = GetPlacedObject(objectId);
            if (placedObject == null)
            {
                return false;
            }
            
            placedObject.rotation = rotation;
            lastModified = DateTime.Now;
            return true;
        }
        
        /// <summary>
        /// Update only the scale of a placed object
        /// </summary>
        public bool UpdatePlacedObjectScale(string objectId, Vector3 scale)
        {
            var placedObject = GetPlacedObject(objectId);
            if (placedObject == null)
            {
                return false;
            }
            
            placedObject.scale = scale;
            lastModified = DateTime.Now;
            return true;
        }
        
        /// <summary>
        /// Update the custom name of a placed object
        /// </summary>
        public bool UpdatePlacedObjectName(string objectId, string customName)
        {
            var placedObject = GetPlacedObject(objectId);
            if (placedObject == null)
            {
                return false;
            }
            
            placedObject.customName = customName;
            lastModified = DateTime.Now;
            return true;
        }

        public bool HasObjectId(string objectId)
        {
            return placedObjects.Exists(obj => obj.id == objectId);
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