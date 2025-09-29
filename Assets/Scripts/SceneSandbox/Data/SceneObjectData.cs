using System;
using UnityEngine;

namespace SceneSandbox.Data
{
    /// <summary>
    /// Types of objects that can be placed in the scene
    /// </summary>
    public enum SceneObjectType
    {
        Actor,
        Prop,
        Camera,
        Light
    }

    /// <summary>
    /// Data structure for scene objects that can be placed
    /// </summary>
    [Serializable]
    public class SceneObjectData
    {
        [Header("Object Info")]
        public string id;
        public string displayName;
        public SceneObjectType objectType;
        public GameObject prefab;
        
        [Header("UI Representation")]
        public Sprite icon;
        public Color iconColor = Color.white;
        
        [Header("Placement Settings")]
        public bool canRotate = true;
        public bool canScale = true;
        public Vector3 defaultScale = Vector3.one;
        public Vector3 snapOffset = Vector3.zero;
        
        [Header("Categories")]
        public string category = "Default";
        public string[] tags;
        
        public SceneObjectData()
        {
            id = System.Guid.NewGuid().ToString();
        }
        
        public SceneObjectData(string displayName, GameObject prefab, SceneObjectType type)
        {
            this.id = System.Guid.NewGuid().ToString();
            this.displayName = displayName;
            this.prefab = prefab;
            this.objectType = type;
        }
    }
}