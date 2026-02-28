using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Systems.SceneSandbox.Data;
using Systems.SceneSandbox.Serialization;
using Systems.Persistence;
using Systems.Persistence.Core;

namespace Systems.SceneSandbox.Core
{
    /// <summary>
    /// Handles scene and project serialization/deserialization for the Scene Sandbox Builder system
    /// </summary>
    public class SceneSerializer : MonoBehaviour, ISubsystemPersistence
    {
        [Header("Save/Load Configuration")]
        private string _defaultSavePath = "";
        private string _defaultProjectSavePath = "";
        private string _rootPath = "";
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
        // Root folder used for save/load (editor can read this)
        public string RootPath => _rootPath;
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

            // Register placement persistence and project persistence with the central SaveLoadSystem if available
            try {
                var saveSystem = Systems.Persistence.GamePersistenceManager.Instance;
                if (saveSystem != null) {
                    var placementPersistence = new PlacementPersistence(_placementSystem, _currentScene, _currentSceneName);
                    saveSystem.RegisterSubsystem(placementPersistence);

                    // Register this serializer as a subsystem to persist project data
                    saveSystem.RegisterSubsystem(this);
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
            // Require persistence manager's data-service root path (no fallback to Application paths)
            string rootPath = null;
            try
            {
                var pm = Systems.Persistence.GamePersistenceManager.Instance;
                if (pm != null)
                {
                    // Prefer namespace-aware root; fall back to generic data service root only within persistence manager
                    rootPath = pm.GetDataServiceRootPath(Namespace) ?? pm.GetDataServiceRootPath();
                }
            }
            catch { /* ignore */ }

            if (string.IsNullOrEmpty(rootPath))
            {
                Debug.LogWarning("[SceneSerializer] Persistence data-service root not available. Save/load paths will be disabled until persistence is available.");
                _rootPath = string.Empty;
                _defaultSavePath = string.Empty;
                _defaultProjectSavePath = string.Empty;
                return;
            }

            _rootPath = rootPath;
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

        #region ISubsystemPersistence Implementation

        // Namespace used by the persistence system for project files
        public string Namespace => "SceneSandboxProject";

        public string PersistentName => _currentProject?.projectName ?? _currentSceneName ?? "scene_project";

        public PersistenceTarget Target => PersistenceTarget.External;

        // The concrete data type we provide/expect
        public Type DataType => typeof(SandboxProjectData);

        // Provide current project as save data (ensure it's up-to-date)
        public object GetSaveData()
        {
            if (_currentProject == null)
            {
                CreateDefaultProject();
            }

            // Ensure current scene state is reflected in project
            if (_currentScene != null)
            {
                int idx = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
                if (idx >= 0)
                {
                    _currentProject.scenes[idx] = _currentScene;
                }
                else
                {
                    _currentProject.scenes.Add(_currentScene);
                    _currentProject.activeSceneId = _currentScene.sceneId;
                }
            }

            UpdateProjectSettingsFromBuilder();
            _currentProject.lastModified = DateTime.Now;

            return _currentProject;
        }

        // Load project data provided by persistence manager
        public void LoadData(object data)
        {
            if (data == null) return;
            var proj = data as SandboxProjectData;
            if (proj == null) return;

            // Defer to existing loader logic
            LoadProjectData(proj);
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

                // Ensure directory exists for fallback file save
                EnsureDirectoryExists(savePath);

                bool success = false;

                try
                {
                    var pm = GamePersistenceManager.Instance;
                    if (pm != null)
                    {
                        // Use filename (without extension) as save name and a dedicated namespace
                        string saveName = System.IO.Path.GetFileNameWithoutExtension(savePath);
                        pm.SaveFile(this, saveName, null, true);
                        success = true;
                    }
                    else
                    {
                        // Fallback: write raw JSON to provided path
                        string json = JsonUtility.ToJson(_currentProject, true);
                        System.IO.File.WriteAllText(savePath, json);
                        success = true;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to save project via persistence: {ex.Message}");
                    success = false;
                }

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
                SandboxProjectData project = null;

                var pm = GamePersistenceManager.Instance;
                if (pm != null)
                {
                    // Attempt to load via persistence manager using filename as save name
                    string saveName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    try
                    {
                        project = pm.LoadFile<SandboxProjectData>(this, saveName);
                    }
                    catch { project = null; }
                }

                // Fallback to raw file load if persistence manager not available or load failed
                if (project == null)
                {
                    if (!System.IO.File.Exists(filePath)) return false;
                    string json = System.IO.File.ReadAllText(filePath);
                    project = JsonUtility.FromJson<SandboxProjectData>(json);
                }

                if (project != null)
                {
                    LoadProjectData(project);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load project: {ex.Message}");
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
                // Try using persistence manager first
                var pm = GamePersistenceManager.Instance;
                string saveName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                if (pm != null)
                {
                    try
                    {
                        pm.DeleteGame(saveName);
                    }
                    catch { }
                }

                // Also remove raw file if present
                if (System.IO.File.Exists(filePath))
                {
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
            if (_debugLogs) Debug.Log($"[SceneSerializer] Getting available projects from persistence or path: {_defaultProjectSavePath}");

            var results = new List<SandboxProjectMetadata>();

            var pm = GamePersistenceManager.Instance;
            if (pm != null)
            {
                try
                {
                    var saves = pm.ListSaves(Namespace);
                    foreach (var s in saves)
                    {
                        results.Add(new SandboxProjectMetadata
                        {
                            projectName = s,
                            filePath = s,
                            created = System.DateTime.MinValue,
                            lastModified = System.DateTime.MinValue,
                            description = "",
                            objectCount = 0,
                            hasTimelineIntegration = false,
                            sceneBounds = new UnityEngine.Vector3(20f, 10f, 20f)
                        });
                    }
                }
                catch { }
            }

            // Fallback: scan directory for .sbproj files
            try
            {
                if (System.IO.Directory.Exists(_defaultProjectSavePath))
                {
                    var files = System.IO.Directory.GetFiles(_defaultProjectSavePath, "*.sbproj");
                    foreach (var f in files)
                    {
                        try
                        {
                            string json = System.IO.File.ReadAllText(f);
                            var proj = JsonUtility.FromJson<SandboxProjectData>(json);
                            if (proj != null)
                            {
                                results.Add(new SandboxProjectMetadata
                                {
                                    projectName = proj.projectName,
                                    filePath = f,
                                    created = proj.created,
                                    lastModified = proj.lastModified,
                                    description = proj.description,
                                    objectCount = proj.scenes?.Sum(s => s.placedObjects?.Count ?? 0) ?? 0,
                                    hasTimelineIntegration = proj.hasTimelineIntegration,
                                    sceneBounds = proj.settings?.sceneBounds ?? new UnityEngine.Vector3(20f, 10f, 20f)
                                });
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return results.OrderByDescending(p => p.lastModified).ToList();
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
