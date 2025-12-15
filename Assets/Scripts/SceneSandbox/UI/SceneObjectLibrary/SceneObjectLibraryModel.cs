using System.Collections.Generic;
using UnityEngine;
using Systems.SceneSandbox.Data;
using SceneObjectLibrarySO = Systems.SceneSandbox.Data.SceneObjectLibrary;

namespace Systems.SceneSandbox.UI.SceneObjectLibrary
{
    /// <summary>
    /// Model for Scene Object Library.
    /// Wraps the SceneObjectLibrary ScriptableObject and provides data access methods.
    /// </summary>
    public class SceneObjectLibraryModel
    {
        private readonly SceneObjectLibrarySO _sceneObjectLibrary;
        
        public SceneObjectLibraryModel(SceneObjectLibrarySO library)
        {
            _sceneObjectLibrary = library;
            
            if (_sceneObjectLibrary == null)
            {
                Debug.LogError("SceneObjectLibrary not assigned to model!");
            }
        }
        
        /// <summary>
        /// Get all objects from the library.
        /// </summary>
        public List<SceneObjectData> GetAllObjects()
        {
            if (_sceneObjectLibrary == null)
                return new List<SceneObjectData>();
            
            return _sceneObjectLibrary.GetAllObjects();
        }
        
        /// <summary>
        /// Get objects filtered by type.
        /// </summary>
        public List<SceneObjectData> GetObjectsByType(SceneObjectType type)
        {
            if (_sceneObjectLibrary == null)
                return new List<SceneObjectData>();
            
            return _sceneObjectLibrary.GetObjectsByType(type);
        }
        
        /// <summary>
        /// Get objects filtered by category.
        /// </summary>
        public List<SceneObjectData> GetObjectsByCategory(string category)
        {
            if (_sceneObjectLibrary == null)
                return new List<SceneObjectData>();
            
            return _sceneObjectLibrary.GetObjectsByCategory(category);
        }
        
        /// <summary>
        /// Get a specific object by ID.
        /// </summary>
        public SceneObjectData GetObjectById(string id)
        {
            if (_sceneObjectLibrary == null)
                return null;
            
            return _sceneObjectLibrary.GetObjectById(id);
        }
        
        /// <summary>
        /// Get all available categories in the library.
        /// </summary>
        public List<string> GetAvailableCategories()
        {
            if (_sceneObjectLibrary == null)
                return new List<string>();
            
            return _sceneObjectLibrary.GetCategories();
        }
        
        /// <summary>
        /// Check if the library is valid and has objects.
        /// </summary>
        public bool IsValid
        {
            get => _sceneObjectLibrary != null && _sceneObjectLibrary.GetAllObjects().Count > 0;
        }
        
        /// <summary>
        /// Get the object count.
        /// </summary>
        public int ObjectCount
        {
            get => _sceneObjectLibrary?.GetAllObjects().Count ?? 0;
        }
    }
}
