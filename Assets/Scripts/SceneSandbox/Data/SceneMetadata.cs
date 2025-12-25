using System;
using UnityEngine;

namespace Systems.SceneSandbox.Data
{
    /// <summary>
    /// Lightweight metadata for scenes in a project (for UI display)
    /// </summary>
    [Serializable]
    public class SceneMetadata
    {
        public string sceneId;
        public string sceneName;
        public string description;
        public DateTime created;
        public DateTime lastModified;
        public int objectCount;
        public bool isActive;
        
        public SceneMetadata(SceneConfiguration scene, bool isActive = false)
        {
            this.sceneId = scene.sceneId;
            this.sceneName = scene.sceneName;
            this.description = scene.description;
            this.created = scene.createdDate;
            this.lastModified = scene.lastModified;
            this.objectCount = scene.placedObjects?.Count ?? 0;
            this.isActive = isActive;
        }
    }
}
