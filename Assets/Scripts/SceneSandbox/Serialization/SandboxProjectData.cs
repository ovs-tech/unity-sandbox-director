using System;
using System.Collections.Generic;
using UnityEngine;
using Systems.SceneSandbox.Data;
using Systems.SceneSandbox.Core;

namespace Systems.SceneSandbox.Serialization
{
    /// <summary>
    /// Represents a placed object in the scene for serialization
    /// This is the enhanced version with more properties for the factory system
    /// </summary>
    [Serializable]
    public class PlacedObjectData
    {
        public string instanceId;
        public string objectType;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string customName;
        public string tag;
        public int layer;
        public bool isActive;
        public Dictionary<string, object> customProperties;

        public PlacedObjectData()
        {
            instanceId = Guid.NewGuid().ToString();
            objectType = "GameObject";
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
            customName = "";
            tag = "Untagged";
            layer = 0;
            isActive = true;
            customProperties = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Complete project data for Scene Sandbox Builder
    /// Contains scene configuration, settings, and metadata
    /// </summary>
    [Serializable]
    public class SandboxProjectData
    {
        [Header("Project Info")]
        public int version = 1;
        public string projectName = "Untitled Sandbox Project";
        public string description = "";
        public DateTime created = DateTime.Now;
        public DateTime lastModified = DateTime.Now;
        
        [Header("Project Settings")]
        public SandboxProjectSettings settings = new SandboxProjectSettings();
        
        [Header("Scene Data")]
        public List<SceneConfiguration> scenes = new List<SceneConfiguration>();
        public string activeSceneId = ""; // ID of currently active scene
        
        [Header("Object Library Reference")]
        public string objectLibraryPath = "";
        
        [Header("Timeline Integration")]
        public string timelineProjectPath = "";
        public bool hasTimelineIntegration = false;
        
        [Header("Export Settings")]
        public SandboxExportSettings exportSettings = new SandboxExportSettings();

        public SandboxProjectData()
        {
            var defaultScene = new SceneConfiguration("Main Scene");
            scenes.Add(defaultScene);
            activeSceneId = defaultScene.sceneId;
        }

        public SandboxProjectData(string name) : this()
        {
            projectName = name;
            scenes[0].sceneName = $"{name} - Main Scene";
        }
        
        /// <summary>
        /// Get the currently active scene
        /// </summary>
        public SceneConfiguration GetActiveScene()
        {
            return scenes.Find(s => s.sceneId == activeSceneId) ?? (scenes.Count > 0 ? scenes[0] : null);
        }
        
        /// <summary>
        /// Add a new scene to the project
        /// </summary>
        public SceneConfiguration AddScene(string sceneName)
        {
            var newScene = new SceneConfiguration(sceneName);
            scenes.Add(newScene);
            return newScene;
        }
        
        /// <summary>
        /// Remove a scene from the project
        /// </summary>
        public bool RemoveScene(string sceneId)
        {
            // Don't allow removing the last scene
            if (scenes.Count <= 1)
                return false;
                
            var removed = scenes.RemoveAll(s => s.sceneId == sceneId) > 0;
            
            // If removed scene was active, switch to first scene
            if (removed && activeSceneId == sceneId)
            {
                activeSceneId = scenes.Count > 0 ? scenes[0].sceneId : "";
            }
            
            return removed;
        }
        
        /// <summary>
        /// Get a scene by ID
        /// </summary>
        public SceneConfiguration GetScene(string sceneId)
        {
            return scenes.Find(s => s.sceneId == sceneId);
        }
        
        /// <summary>
        /// Get a scene by name
        /// </summary>
        public SceneConfiguration GetSceneByName(string sceneName)
        {
            return scenes.Find(s => s.sceneName == sceneName);
        }
        
        /// <summary>
        /// Set the active scene
        /// </summary>
        public bool SetActiveScene(string sceneId)
        {
            var scene = GetScene(sceneId);
            if (scene != null)
            {
                activeSceneId = sceneId;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Project-wide settings for Scene Sandbox Builder
    /// </summary>
    [Serializable]
    public class SandboxProjectSettings
    {
        [Header("Placement Settings")]
        public bool snapToGrid = true;
        public float gridSize = 1f;
        public float defaultPlacementHeight = 0f;
        public float minimumPlacementHeight = 0f;
        public bool useRaycastForPlacement = true;
        public bool useConsistentHeight = false;
        public LayerMask placementLayers = -1;
        
        [Header("Grid Settings")]
        public Vector3 gridOffset = Vector3.zero;
        public PivotPoint gridPivotOffset = PivotPoint.Center; // Offset relative to scene bounds pivot
        
        [Header("Scene Bounds")]
        public Vector3 sceneBounds = new Vector3(20f, 10f, 20f);
        public Vector3 sceneBoundsOffset = Vector3.zero;
        public PivotPoint sceneBoundsPivot = PivotPoint.Center;
        public Color sceneBoundsColor = Color.cyan;
        
        [Header("Gizmo Settings")]
        public bool enableGizmos = true;
        public bool showBoundsGizmo = true;
        public bool showAxesGizmo = true;
        public bool showHandlesGizmo = true;
        public Color gizmoBoundsColor = Color.yellow;
        public float gizmoAxisLength = 1f;
        
        [Header("Scene Gizmo Settings")]
        public bool enableSceneGizmos = true;
        public bool showSceneGrid = true;
        public bool showSceneBounds = true;
        public bool showStageAreaGizmo = true;
        public bool showPlacementHeightGizmo = true;
        public Color placementHeightColor = Color.yellow;
        
        [Header("Drop Indicator Settings")]
        public bool enableDropIndicator = true;
        public Color validDropColor = Color.green;
        public Color invalidDropColor = Color.red;
        public float dropIndicatorSize = 1f;
        
        [Header("Preview Settings")]
        public bool autoPreview = false;
        public float previewDuration = 10f;
    }

    /// <summary>
    /// Export settings for sharing or building sandbox projects
    /// </summary>
    [Serializable]
    public class SandboxExportSettings
    {
        [Header("Export Options")]
        public bool includeObjectLibrary = true;
        public bool includeTimelineData = false;
        public bool compressTextures = true;
        public bool optimizeMeshes = true;
        
        [Header("Platform Settings")]
        public List<string> targetPlatforms = new List<string> { "StandaloneWindows64", "Android" };
        
        [Header("Package Settings")]
        public string packageName = "";
        public string packageVersion = "1.0.0";
        public string packageAuthor = "";
        
        [Header("Build Settings")]
        public bool createExecutable = false;
        public bool createPackage = true;
        public string outputDirectory = "Builds/";
    }

    /// <summary>
    /// Runtime sandbox state data that doesn't get serialized to file
    /// </summary>
    public class SandboxRuntimeData
    {
        public Dictionary<string, GameObject> runtimeObjects = new Dictionary<string, GameObject>();
        public GameObject selectedObject;
        public List<GameObject> previewObjects = new List<GameObject>();
        public bool isInPreviewMode = false;
        public Vector3 lastDropIndicatorPosition = Vector3.zero;
        public bool isDragging = false;
        
        public void Clear()
        {
            runtimeObjects.Clear();
            selectedObject = null;
            previewObjects.Clear();
            isInPreviewMode = false;
            isDragging = false;
        }
    }

    /// <summary>
    /// Project metadata for listing and management
    /// </summary>
    [Serializable]
    public class SandboxProjectMetadata
    {
        public string projectName;
        public string filePath;
        public DateTime created;
        public DateTime lastModified;
        public string description;
        public int objectCount;
        public bool hasTimelineIntegration;
        public Vector3 sceneBounds;
        public string thumbnailPath;
    }
}