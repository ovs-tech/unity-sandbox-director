using System.Collections.Generic;
using UnityEngine;

namespace Systems.SceneSandbox.Data
{
    /// <summary>
    /// ScriptableObject library containing all available scene objects
    /// </summary>
    [CreateAssetMenu(fileName = "SceneObjectLibrary", menuName = "Scene Sandbox Builder/Scene Object Library")]
    public class SceneObjectLibrary : ScriptableObject
    {
        [Header("Actor Objects")]
        [SerializeField] private List<SceneObjectData> actors = new List<SceneObjectData>();
        
        [Header("Prop Objects")]
        [SerializeField] private List<SceneObjectData> props = new List<SceneObjectData>();
        
        [Header("Camera Objects")]
        [SerializeField] private List<SceneObjectData> cameras = new List<SceneObjectData>();
        
        [Header("Light Objects")]
        [SerializeField] private List<SceneObjectData> lights = new List<SceneObjectData>();
        
        /// <summary>
        /// Get all objects of a specific type
        /// </summary>
        public List<SceneObjectData> GetObjectsByType(SceneObjectType type)
        {
            return type switch
            {
                SceneObjectType.Actor => new List<SceneObjectData>(actors),
                SceneObjectType.Prop => new List<SceneObjectData>(props),
                SceneObjectType.Camera => new List<SceneObjectData>(cameras),
                SceneObjectType.Light => new List<SceneObjectData>(lights),
                _ => new List<SceneObjectData>()
            };
        }
        
        /// <summary>
        /// Get all objects regardless of type
        /// </summary>
        public List<SceneObjectData> GetAllObjects()
        {
            var allObjects = new List<SceneObjectData>();
            allObjects.AddRange(actors);
            allObjects.AddRange(props);
            allObjects.AddRange(cameras);
            allObjects.AddRange(lights);
            return allObjects;
        }
        
        /// <summary>
        /// Get objects by category
        /// </summary>
        public List<SceneObjectData> GetObjectsByCategory(string category)
        {
            var filteredObjects = new List<SceneObjectData>();
            var allObjects = GetAllObjects();
            
            foreach (var obj in allObjects)
            {
                if (obj.category == category)
                {
                    filteredObjects.Add(obj);
                }
            }
            
            return filteredObjects;
        }
        
        /// <summary>
        /// Find object by ID
        /// </summary>
        public SceneObjectData GetObjectById(string id)
        {
            var allObjects = GetAllObjects();
            return allObjects.Find(obj => obj.id == id);
        }
        
        /// <summary>
        /// Add a new object to the library
        /// </summary>
        public void AddObject(SceneObjectData objectData)
        {
            List<SceneObjectData> targetList = objectData.objectType switch
            {
                SceneObjectType.Actor => actors,
                SceneObjectType.Prop => props,
                SceneObjectType.Camera => cameras,
                SceneObjectType.Light => lights,
                _ => props
            };
            
            if (!targetList.Contains(objectData))
            {
                targetList.Add(objectData);
            }
        }
        
        /// <summary>
        /// Remove an object from the library
        /// </summary>
        public bool RemoveObject(string id)
        {
            var removed = actors.RemoveAll(obj => obj.id == id) > 0;
            removed |= props.RemoveAll(obj => obj.id == id) > 0;
            removed |= cameras.RemoveAll(obj => obj.id == id) > 0;
            removed |= lights.RemoveAll(obj => obj.id == id) > 0;
            
            return removed;
        }
        
        /// <summary>
        /// Get all available categories (dynamically built from objects)
        /// </summary>
        public List<string> GetCategories()
        {
            var categorySet = new HashSet<string>();
            var allObjects = GetAllObjects();
            
            foreach (var obj in allObjects)
            {
                if (!string.IsNullOrEmpty(obj.category))
                {
                    categorySet.Add(obj.category);
                }
            }
            
            var categoriesList = new List<string>(categorySet);
            categoriesList.Sort(); // Sort alphabetically
            
            // Always ensure "Default" is first if it exists
            if (categoriesList.Contains("Default"))
            {
                categoriesList.Remove("Default");
                categoriesList.Insert(0, "Default");
            }
            
            return categoriesList;
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to refresh the library
        /// </summary>
        public void RefreshLibrary()
        {
            // Ensure all objects have unique IDs
            var allObjects = GetAllObjects();
            var usedIds = new HashSet<string>();
            
            foreach (var obj in allObjects)
            {
                if (string.IsNullOrEmpty(obj.id) || usedIds.Contains(obj.id))
                {
                    obj.id = System.Guid.NewGuid().ToString();
                }
                usedIds.Add(obj.id);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        #endif
    }
}