using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Systems.SceneSandbox.Data;
using Systems.SceneSandbox.Serialization;

namespace Systems.SceneSandbox.Core
{
    /// <summary>
    /// Handles scene and project serialization/deserialization for the Scene Sandbox Builder system
    /// </summary>
    public class SceneSerializer : MonoBehaviour
    {
        [Header("Save/Load Configuration")]
        private string _defaultSavePath = "";
        private string _defaultProjectSavePath = "";
        [Tooltip("Name for the current scene configuration")]
        [SerializeField] private string _currentSceneName = "Untitled Scene";
        [Tooltip("Automatically load the first available project on start (Play mode only)")]
        [SerializeField] private bool _autoLoadFirstProject = false;

        [Header("Debug Settings")]
        [SerializeField] private bool _debugLogs = false;

        [Header("Events")]
        [SerializeField] private SceneConfigurationEvent _onSceneLoaded = new SceneConfigurationEvent();
        [SerializeField] private SceneConfigurationEvent _onSceneSaved = new SceneConfigurationEvent();
        [SerializeField] private UnityEvent _onSceneCleared = new UnityEvent();

        // Current state
        private SceneConfiguration _currentScene;
        private SandboxProjectData _currentProject;

        // Component dependencies
        private PlacementSystem _placementSystem;
        private SelectionManager _selectionManager;
        private SceneSandboxBuilder _builder;

        // Events
        public event Action<SceneConfiguration> OnSceneLoaded;
        public event Action<SceneConfiguration> OnSceneSaved;
        public event Action OnSceneCleared;

        // Properties
        public SceneConfiguration CurrentScene => _currentScene;
        public SandboxProjectData CurrentProject => _currentProject;
        public string CurrentSceneName => _currentSceneName;
        public SceneConfigurationEvent OnSceneLoadedEvent => _onSceneLoaded;
        public SceneConfigurationEvent OnSceneSavedEvent => _onSceneSaved;
        public UnityEvent OnSceneClearedEvent => _onSceneCleared;

        /// <summary>
        /// Initialize the serializer with required dependencies
        /// </summary>
        public void Initialize(PlacementSystem placementSystem, SelectionManager selectionManager, SceneSandboxBuilder builder)
        {
            _placementSystem = placementSystem;
            _selectionManager = selectionManager;
            _builder = builder;

            // Initialize save paths
            InitializeSavePaths();

            // Auto-load first project if enabled and in play mode
            if (_autoLoadFirstProject && Application.isPlaying)
            {
                TryAutoLoadFirstProject();
            }

            // Register placement persistence with the central SaveLoadSystem if available
            try {
                var saveSystem = Systems.Persistence.SaveLoadSystem.Instance;
                if (saveSystem != null) {
                    var placementPersistence = new PlacementPersistence(_placementSystem, _currentScene, _currentSceneName);
                    saveSystem.RegisterSubsystem(placementPersistence);
                }
            } catch (Exception) {
                // Ignore if SaveLoadSystem not present in some contexts
            }

            // Create default scene/project if needed
            if (_currentProject == null)
            {
                CreateDefaultProject();
            }
        }

        private void InitializeSavePaths()
        {
            string rootPath = Application.isEditor ? System.IO.Path.Combine(Application.dataPath, "Data") : Application.persistentDataPath;
            _defaultSavePath = System.IO.Path.Combine(rootPath, "SceneSandboxBuilder", "SavedScenes");
            _defaultProjectSavePath = System.IO.Path.Combine(rootPath, "SceneSandboxBuilder", "SavedProjects");
            try
            {
                if (!System.IO.Directory.Exists(_defaultSavePath))
                {
                    System.IO.Directory.CreateDirectory(_defaultSavePath);
                }

                if (!System.IO.Directory.Exists(_defaultProjectSavePath))
                {
                    System.IO.Directory.CreateDirectory(_defaultProjectSavePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create save directories: {ex.Message}");
            }
        }

        #region Scene Save/Load

        /// <summary>
        /// Save the current scene to a file
        /// </summary>
        public bool SaveScene(string filePath = null)
        {
            if (_currentScene == null) return false;

            try
            {
                UpdateSceneConfiguration();

                string savePath = filePath ?? GetDefaultSavePath();

                if (!EnsureDirectoryExists(savePath))
                {
                    return false;
                }

                string json = JsonUtility.ToJson(_currentScene, true);
                System.IO.File.WriteAllText(savePath, json);

                _onSceneSaved?.Invoke(_currentScene);
                OnSceneSaved?.Invoke(_currentScene);

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save scene: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a scene from a file
        /// </summary>
        public bool LoadScene(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return false;
                }

                string json = System.IO.File.ReadAllText(filePath);
                var sceneConfig = JsonUtility.FromJson<SceneConfiguration>(json);

                if (sceneConfig != null)
                {
                    LoadSceneConfiguration(sceneConfig);
                    return true;
                }
            }
            catch (System.Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// Clear the current scene
        /// </summary>
        public void ClearScene()
        {
            // Clear placed objects via PlacementSystem
            _placementSystem?.ClearPlacedObjects();

            // Clear selection
            _selectionManager?.ClearSelection();

            // Clear scene configuration if it exists
            if (_currentScene != null)
            {
                _currentScene.ClearPlacedObjects();
            }

            _onSceneCleared?.Invoke();
            OnSceneCleared?.Invoke();
        }

        private void UpdateSceneConfiguration()
        {
            if (_currentScene == null || _placementSystem == null) return;

            // PlacementSystem already maintains scene configuration in real-time
            // This method is kept for future extensions or manual updates
            _currentScene.lastModified = System.DateTime.Now;
        }

        private void LoadSceneConfiguration(SceneConfiguration sceneConfig)
        {
            // Clear current scene GameObjects and runtime state first
            _placementSystem?.ClearPlacedObjects();
            _selectionManager?.ClearSelection();

            // Now set the new scene (after clearing runtime state)
            _currentScene = sceneConfig;
            _currentSceneName = sceneConfig.sceneName;

            // Recreate all placed objects
            foreach (var placedObjectData in sceneConfig.placedObjects)
            {
                _placementSystem?.PlaceObject(placedObjectData);
            }

            _onSceneLoaded?.Invoke(_currentScene);
            OnSceneLoaded?.Invoke(_currentScene);
        }

        /// <summary>
        /// Public wrapper to load a scene configuration from external callers (e.g., builder).
        /// This ensures the same internal loading logic and events are used.
        /// </summary>
        /// <param name="sceneConfig">Scene configuration to apply</param>
        public void SetCurrentSceneConfiguration(SceneConfiguration sceneConfig)
        {
            if (sceneConfig == null) return;
            LoadSceneConfiguration(sceneConfig);
        }

        #endregion

        #region Project Save/Load

        /// <summary>
        /// Create a new project
        /// </summary>
        public void CreateNewProject(string name)
        {
            _currentProject = new SandboxProjectData(name);
            _currentSceneName = name;

            _currentScene = _currentProject.GetActiveScene();

            _onSceneLoaded?.Invoke(_currentScene);
            OnSceneLoaded?.Invoke(_currentScene);
        }

        /// <summary>
        /// Save the current project
        /// </summary>
        public bool SaveProject(string filePath = null)
        {
            if (_currentProject == null)
            {
                CreateDefaultProject();
            }

            try
            {
                // Update current scene configuration with current object states before saving
                if (_currentScene != null)
                {
                    UpdateSceneConfiguration();

                    // Update scene in project's scene list
                    var sceneInProject = _currentProject.GetScene(_currentScene.sceneId);
                    if (sceneInProject != null)
                    {
                        // Update the scene in the list
                        int index = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
                        if (index >= 0)
                        {
                            _currentProject.scenes[index] = _currentScene;
                        }
                    }
                }

                // Update project settings from current builder state
                UpdateProjectSettingsFromBuilder();

                string savePath = filePath ?? GetDefaultProjectSavePath();

                // Ensure directory exists
                if (!EnsureDirectoryExists(savePath))
                {
                    return false;
                }

                bool success = SandboxProjectSerializer.SaveToFile(_currentProject, _builder, savePath);

                if (success)
                {
                    _onSceneSaved?.Invoke(_currentScene);
                    OnSceneSaved?.Invoke(_currentScene);
                }

                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save project: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a project from a file
        /// </summary>
        public bool LoadProject(string filePath)
        {
            try
            {
                var project = SandboxProjectSerializer.LoadFromFile(filePath);
                if (project != null)
                {
                    LoadProjectData(project);
                    return true;
                }
            }
            catch (System.Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// Delete a project file
        /// </summary>
        public bool DeleteProject(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    // If deleting current project, clear it
                    if (_currentProject != null && filePath.Contains(_currentProject.projectName))
                    {
                        _currentProject = null;
                    }

                    System.IO.File.Delete(filePath);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to delete project: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Get list of available projects
        /// </summary>
        public List<SandboxProjectMetadata> GetAvailableProjects()
        {
            if (_debugLogs) Debug.Log($"[SceneSerializer] Getting available projects from path: {_defaultProjectSavePath}");
            return SandboxProjectSerializer.GetProjectsInDirectory(_defaultProjectSavePath, "*.sbproj");
        }

        private void TryAutoLoadFirstProject()
        {
            var availableProjects = GetAvailableProjects();

            if (availableProjects != null && availableProjects.Count > 0)
            {
                var firstProject = availableProjects[0];
                if (_debugLogs) Debug.Log($"[SceneSerializer] Auto-loading first project: {firstProject.projectName}");

                if (LoadProject(firstProject.filePath))
                {
                    if (_debugLogs) Debug.Log($"[SceneSerializer] Successfully auto-loaded project: {firstProject.projectName}");
                }
                else
                {
                    Debug.LogWarning($"[SceneSerializer] Failed to auto-load project: {firstProject.projectName}");
                }
            }
            else
            {
                if (_debugLogs) Debug.Log("[SceneSerializer] No projects available to auto-load");
            }
        }

        private void LoadProjectData(SandboxProjectData project)
        {
            // Clear current state
            ClearScene();

            // Set project
            _currentProject = project;

            // Apply project settings to builder
            ApplyProjectSettingsToBuilder();

            // Load active scene or first scene
            var activeScene = project.GetActiveScene();
            if (activeScene != null)
            {
                LoadSceneConfiguration(activeScene);
            }
            else if (project.scenes.Count > 0)
            {
                LoadSceneConfiguration(project.scenes[0]);
                project.activeSceneId = project.scenes[0].sceneId;
            }
            else
            {
                // Create default scene if none exists
                _currentScene = new SceneConfiguration($"{project.projectName} Scene");
                project.scenes.Add(_currentScene);
                project.activeSceneId = _currentScene.sceneId;
            }

            _currentSceneName = _currentScene.sceneName;
        }

        private void CreateDefaultProject()
        {
            if (_currentProject == null)
            {
                _currentProject = new SandboxProjectData(_currentSceneName ?? "Untitled Project");
            }

            if (_currentScene == null)
            {
                _currentScene = new SceneConfiguration(_currentSceneName ?? "New Scene");
            }

            // Ensure current scene is in project
            if (!_currentProject.scenes.Contains(_currentScene))
            {
                _currentProject.scenes.Add(_currentScene);
            }

            _currentProject.activeSceneId = _currentScene.sceneId;
            UpdateProjectSettingsFromBuilder();
        }

        private void UpdateProjectSettingsFromBuilder()
        {
            if (_currentProject?.settings == null || _builder == null)
                return;

            // Settings are updated from the builder's current state
            // This will be refactored in future phases to use a settings object
            _currentProject.lastModified = System.DateTime.Now;
        }

        private void ApplyProjectSettingsToBuilder()
        {
            if (_currentProject?.settings == null || _builder == null)
                return;

            // Settings are applied to the builder
            // This will be refactored in future phases to use a settings object
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Update an object's transform in the current scene configuration
        /// </summary>
        public void UpdateObjectInScene(string objectId, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (_currentScene == null) return;

            var placedObjectData = _currentScene.GetPlacedObject(objectId);
            if (placedObjectData != null)
            {
                placedObjectData.position = position;
                placedObjectData.rotation = rotation;
                placedObjectData.scale = scale;
                _currentScene.lastModified = System.DateTime.Now;
            }
        }

        /// <summary>
        /// Register a placed object in the current scene configuration
        /// </summary>
        public void RegisterPlacedObject(SceneSandbox.Data.PlacedObjectData placedObjectData)
        {
            if (_currentScene != null)
            {
                _currentScene.CreateOrUpdatePlacedObject(placedObjectData);
            }
        }

        /// <summary>
        /// Unregister a placed object from the current scene configuration
        /// </summary>
        public void UnregisterPlacedObject(string objectId)
        {
            if (_currentScene != null)
            {
                _currentScene.RemovePlacedObject(objectId);
            }
        }

        private string GetDefaultSavePath()
        {
            string safeName = SanitizeFileName(_currentSceneName ?? "Untitled");
            return System.IO.Path.Combine(_defaultSavePath, $"{safeName}.json");
        }

        private string GetDefaultProjectSavePath()
        {
            string projectName = _currentProject?.projectName ?? _currentSceneName ?? "Untitled";
            string safeName = SanitizeFileName(projectName);
            return System.IO.Path.Combine(_defaultProjectSavePath, $"{safeName}.sbproj");
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Untitled";

            // Remove invalid filename characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string sanitized = string.Join("_", fileName.Split(invalidChars, System.StringSplitOptions.RemoveEmptyEntries));

            // Trim and ensure not empty
            sanitized = sanitized.Trim();
            return string.IsNullOrEmpty(sanitized) ? "Untitled" : sanitized;
        }

        private bool EnsureDirectoryExists(string filePath)
        {
            try
            {
                string directory = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create directory: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
