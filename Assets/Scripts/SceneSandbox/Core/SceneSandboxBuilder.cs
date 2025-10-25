using System.Collections.Generic;
using UnityEngine;
using SceneSandbox.Data;
using SceneSandbox.Serialization;
using MiniTimeline.Core;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Main controller for the Scene Sandbox Builder system
    /// Manages the placement, interaction, and organization of scene objects
    /// </summary>
    public class SceneSandboxBuilder : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SceneObjectLibrary _objectLibrary;
        [SerializeField] private Transform _sceneRoot;
        [SerializeField] private Transform _stageArea;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask _placementLayers = -1;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private float _gridSize = 1f;
        [SerializeField] private float _defaultPlacementHeight = 0f;
        [SerializeField] private float _minimumPlacementHeight = 0f; // Minimum Y position for placement
        [SerializeField] private bool _useRaycastForPlacement = true;
        [SerializeField] private bool _useConsistentHeight = false; // Force consistent Y height for indicator

        [Header("Preview Settings")]
        [SerializeField] private MiniTimelineDirector _timelineDirector;
        [SerializeField] private bool _autoPreview = false;
        [SerializeField] private float _previewDuration = 10f;

        [Header("Gizmo Settings")]
        [SerializeField] private bool _enableGizmos = true;
        [SerializeField] private bool _showBoundsGizmo = true;
        [SerializeField] private bool _showAxesGizmo = true;
        [SerializeField] private bool _showHandlesGizmo = true;
        [SerializeField] private Color _gizmoBoundsColor = Color.yellow;
        [SerializeField] private float _gizmoAxisLength = 1f;

        [Header("Scene Gizmo Settings")]
        [SerializeField] private bool _enableSceneGizmos = true;
        [SerializeField] private bool _showSceneGrid = true;
        [SerializeField] private bool _showSceneBounds = true;
        [SerializeField] private bool _showStageAreaGizmo = true;
        [SerializeField] private bool _showPlacementHeightGizmo = true;
        [SerializeField] private Vector3 _sceneBounds = new Vector3(20f, 10f, 20f);
        [SerializeField] private Color _sceneBoundsColor = Color.cyan;
        [SerializeField] private Color _placementHeightColor = Color.yellow;

        [Header("Drop Indicator Settings")]
        [SerializeField] private bool _enableDropIndicator = true;
        [SerializeField] private GameObject _dropIndicatorPrefab;
        [SerializeField] private Color _validDropColor = Color.green;
        [SerializeField] private Color _invalidDropColor = Color.red;
        [SerializeField] private float _dropIndicatorSize = 1f;

        [Header("Save/Load")]
        [SerializeField] private string _defaultSavePath = ""; // Will be initialized to Application.persistentDataPath/SavedScenes
        [SerializeField] private string _currentSceneName = "Untitled Scene";
        [SerializeField] private string _defaultProjectSavePath = ""; // Will be initialized to Application.persistentDataPath/SavedProjects

        // Components
        private Camera _sceneCamera;

        // State
        private SceneConfiguration _currentScene;
        private SandboxProjectData _currentProject;
        private Dictionary<string, GameObject> _placedObjects;
        private GameObject _selectedObject;
        private List<GameObject> _previewObjects;

        // Drop Indicator State
        private GameObject _currentDropIndicator;
        private bool _isDraggingObject = false;
        private Vector3 _dragPreviewPosition;

        // Events
        public System.Action<SceneConfiguration> OnSceneLoaded;
        public System.Action<SceneConfiguration> OnSceneSaved;
        public System.Action<GameObject> OnObjectPlaced;
        public System.Action<GameObject> OnObjectRemoved;
        public System.Action<GameObject> OnObjectSelected;
        public System.Action OnSceneCleared;
        public System.Action<bool> OnPreviewStateChanged;

        // Properties
        public SceneConfiguration CurrentScene => _currentScene;
        public SandboxProjectData CurrentProject => _currentProject;
        public SceneObjectLibrary ObjectLibrary => _objectLibrary;
        public bool IsInPreviewMode => _previewObjects?.Count > 0;
        public GameObject SelectedObject => _selectedObject;
        public bool GizmosEnabled => _enableGizmos;
        public bool SceneGizmosEnabled => _enableSceneGizmos;
        public Vector3 SceneBounds => _sceneBounds;
        public bool DropIndicatorEnabled => _enableDropIndicator;

        /// <summary>
        /// Set the object library for this sandbox builder
        /// </summary>
        public void SetObjectLibrary(SceneObjectLibrary library)
        {
            _objectLibrary = library;
        }

        /// <summary>
        /// Initialize default save paths using Application.persistentDataPath for multi-platform support
        /// </summary>
        private void InitializeDefaultPaths()
        {
            // Initialize scene save path if not set or still using old Assets/ path
            if (string.IsNullOrEmpty(_defaultSavePath) || _defaultSavePath.StartsWith("Assets/"))
            {
                _defaultSavePath = System.IO.Path.Combine(Application.persistentDataPath, "SceneSandboxBuilder", "SavedScenes");
                Debug.Log($"[SceneSandboxBuilder] Initialized scene save path: {_defaultSavePath}");
            }

            // Initialize project save path if not set or still using old Assets/ path
            if (string.IsNullOrEmpty(_defaultProjectSavePath) || _defaultProjectSavePath.StartsWith("Assets/"))
            {
                _defaultProjectSavePath = System.IO.Path.Combine(Application.persistentDataPath, "SceneSandboxBuilder", "SavedProjects");
                Debug.Log($"[SceneSandboxBuilder] Initialized project save path: {_defaultProjectSavePath}");
            }

            // Ensure directories exist
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
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneSandboxBuilder] Failed to create default directories: {ex.Message}");
            }
        }

        private void Awake()
        {
            // Initialize default paths for multi-platform support
            InitializeDefaultPaths();

            InitializeComponents();
            InitializeState();

        }

        private void Start()
        {
            CreateNewScene();
            SetupSelectionManagerEvents();
        }

        private void SetupSelectionManagerEvents()
        {
            if (TransformableSelectionManager.Instance != null)
            {
                TransformableSelectionManager.Instance.OnItemSelected += HandleItemSelected;
                TransformableSelectionManager.Instance.OnItemDeselected += HandleItemDeselected;
            }
        }



        private void InitializeComponents()
        {
            // Get or create scene camera
            _sceneCamera = Camera.main ?? FindFirstObjectByType<Camera>();

            // Create scene root if not assigned
            if (_sceneRoot == null)
            {
                _sceneRoot = new GameObject("Scene Root").transform;
            }

            // Create stage area if not assigned
            if (_stageArea == null)
            {
                var stageGO = new GameObject("Stage Area");
                stageGO.transform.parent = _sceneRoot;
                _stageArea = stageGO.transform;

                // Add a drop zone to the stage area
                var dropZone = stageGO.AddComponent<DropZone>();
                dropZone.ZoneSize = new Vector3(20f, 5f, 20f);
                dropZone.AcceptAllTypes = true;
            }
        }

        private void InitializeState()
        {
            _placedObjects = new Dictionary<string, GameObject>();
            _previewObjects = new List<GameObject>();

            // Don't create drop indicator here - create it when first needed
            // This avoids potential issues with early initialization
        }

        #region Public Interface

        /// <summary>
        /// Place an object from the library at the specified position
        /// </summary>
        public GameObject PlaceObject(string objectDataId, Vector3 position, bool autoSelect = true)
        {
            Debug.Log($"[PLACE] PlaceObject called - ID: {objectDataId}, Position: {position}");
            
            if (_objectLibrary == null)
            {
                Debug.LogError("[PLACE] ❌ ObjectLibrary is NULL!");
                return null;
            }

            Debug.Log($"[PLACE] Getting object data from library...");
            var objectData = _objectLibrary.GetObjectById(objectDataId);
            
            if (objectData == null)
            {
                Debug.LogError($"[PLACE] ❌ Object data not found for ID: {objectDataId}");
                return null;
            }
            
            if (objectData.prefab == null)
            {
                Debug.LogError($"[PLACE] ❌ Prefab is NULL for object: {objectData.displayName}");
                return null;
            }
            
            Debug.Log($"[PLACE] Found object: {objectData.displayName}, Prefab: {objectData.prefab.name}");

            // Instantiate the object
            Debug.Log($"[PLACE] Instantiating prefab... StageArea: {(_stageArea != null ? _stageArea.name : "NULL")}");
            GameObject newObject = Instantiate(objectData.prefab, _stageArea);
            newObject.name = objectData.displayName;
            Debug.Log($"[PLACE] ✅ Instantiated: {newObject.name}");

            // Apply placement settings
            Vector3 finalPosition = _snapToGrid ? SnapToGrid(position) : position;
            Debug.Log($"[PLACE] Getting surface position for: {finalPosition}");
            finalPosition = GetSurfacePosition(finalPosition);
            newObject.transform.position = finalPosition;
            Debug.Log($"[PLACE] Final position: {finalPosition}");

            // Apply scale - preserve prefab scale if defaultScale is zero
            Vector3 targetScale = objectData.defaultScale;
            if (targetScale == Vector3.zero)
            {
                targetScale = objectData.prefab.transform.localScale;
            }
            newObject.transform.localScale = targetScale;
            Debug.Log($"[PLACE] Scale applied: {targetScale}");

            // Add draggable component if not present
            Debug.Log($"[PLACE] Adding TransformableItem component...");
            var draggable = newObject.GetComponent<TransformableItem>();
            if (draggable == null)
            {
                draggable = newObject.AddComponent<TransformableItem>();
            }
            Debug.Log($"[PLACE] TransformableItem ready");

            // Set up draggable item
            Debug.Log($"[PLACE] Setting up draggable data...");
            string placedObjectId = System.Guid.NewGuid().ToString();
            draggable.SetObjectData(objectDataId, placedObjectId);
            Debug.Log($"[PLACE] PlacedObjectId: {placedObjectId}");

            // Bind draggable events
            Debug.Log($"[PLACE] Binding draggable events...");
            BindDraggableEvents(draggable);

            // Add to scene configuration
            Debug.Log($"[PLACE] Creating PlacedObjectData...");
            var placedObjectData = new Data.PlacedObjectData(objectDataId, finalPosition)
            {
                id = placedObjectId,
                rotation = newObject.transform.eulerAngles,
                scale = newObject.transform.localScale,
                customName = objectData.displayName
            };

            Debug.Log($"[PLACE] Adding to scene configuration... CurrentScene: {(_currentScene != null ? _currentScene.sceneName : "NULL")}");
            if (_currentScene != null)
            {
                _currentScene.AddPlacedObject(placedObjectData);
                Debug.Log($"[PLACE] Added to scene configuration");
            }
            
            Debug.Log($"[PLACE] Adding to _placedObjects dictionary...");
            _placedObjects[placedObjectId] = newObject;
            Debug.Log($"[PLACE] Dictionary now has {_placedObjects.Count} objects");

            // Select the object if requested
            if (autoSelect)
            {
                Debug.Log($"[PLACE] Selecting object...");
                SelectObject(newObject);
            }

            Debug.Log($"[PLACE] Invoking OnObjectPlaced event...");
            OnObjectPlaced?.Invoke(newObject);

            Debug.Log($"[PLACE] ✅✅✅ PlaceObject completed successfully!");
            return newObject;
        }

        /// <summary>
        /// Remove an object from the scene
        /// </summary>
        public bool RemoveObject(GameObject obj)
        {
            if (obj == null) return false;

            var draggable = obj.GetComponent<TransformableItem>();
            if (draggable == null) return false;

            string objectId = draggable.ObjectId;

            // Remove from scene configuration
            if (_currentScene != null && _currentScene.RemovePlacedObject(objectId))
            {
                if (_placedObjects != null)
                {
                    _placedObjects.Remove(objectId);
                }

                if (_selectedObject == obj)
                {
                    SelectObject(null);
                }

                Destroy(obj);
                OnObjectRemoved?.Invoke(obj);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove an object by its ID
        /// </summary>
        public bool RemoveObject(string objectId)
        {
            if (_placedObjects.TryGetValue(objectId, out GameObject obj))
            {
                return RemoveObject(obj);
            }
            return false;
        }

        /// <summary>
        /// Select an object in the scene
        /// Delegates to TransformableSelectionManager for actual selection
        /// </summary>
        public void SelectObject(GameObject obj)
        {
            if (obj != null)
            {
                var item = obj.GetComponent<TransformableItem>();
                if (item != null && TransformableSelectionManager.Instance != null)
                {
                    TransformableSelectionManager.Instance.SelectItem(item);
                }
            }
            else
            {
                // Deselect all
                if (TransformableSelectionManager.Instance != null)
                {
                    TransformableSelectionManager.Instance.ClearSelection();
                }
            }
        }

        /// <summary>
        /// Clear all objects from the scene
        /// </summary>
        public void ClearScene()
        {
            // Remove all placed objects
            if (_placedObjects != null)
            {
                var objectsToRemove = new List<GameObject>(_placedObjects.Values);
                foreach (var obj in objectsToRemove)
                {
                    if (obj != null)
                    {
                        RemoveObject(obj);
                    }
                }

                _placedObjects.Clear();
            }

            // Clear scene configuration if it exists
            if (_currentScene != null)
            {
                _currentScene.ClearPlacedObjects();
            }

            SelectObject(null);
            OnSceneCleared?.Invoke();
        }

        /// <summary>
        /// Create a new empty scene
        /// </summary>
        public void CreateNewScene(string sceneName = null)
        {
            ClearScene();

            _currentScene = new SceneConfiguration(sceneName ?? "New Scene");
            _currentSceneName = _currentScene.sceneName;

            OnSceneLoaded?.Invoke(_currentScene);
        }

        /// <summary>
        /// Save the current scene configuration
        /// </summary>
        public bool SaveScene(string filePath = null)
        {
            if (_currentScene == null) return false;

            try
            {
                // Update scene configuration with current object states
                UpdateSceneConfiguration();

                string savePath = filePath ?? GetDefaultSavePath();
                
                // Ensure directory exists
                if (!EnsureDirectoryExists(savePath))
                {
                    Debug.LogError($"Cannot create directory for: {savePath}");
                    return false;
                }

                string json = JsonUtility.ToJson(_currentScene, true);
                System.IO.File.WriteAllText(savePath, json);

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
        /// Load a scene configuration from file
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
        /// Handle drag start from UI - shows drop indicator at screen position
        /// </summary>
        public void HandleDragFromUI(string objectDataId, Vector2 screenPosition)
        {
            if (_objectLibrary == null) return;

            var objectData = _objectLibrary.GetObjectById(objectDataId);
            if (objectData == null || objectData.prefab == null) return;

            // Convert screen position to world position
            Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);

            // Show drop indicator at the calculated position
            _isDraggingObject = true;
            _dragPreviewPosition = worldPosition;
            ShowDropIndicator(worldPosition);
        }

        /// <summary>
        /// Handle drag move from UI - updates drop indicator position
        /// </summary>
        public void HandleDragMoveFromUI(Vector2 screenPosition)
        {
            if (!_isDraggingObject) return;

            // Convert screen position to world position
            Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);

            // Update drop indicator position
            _dragPreviewPosition = worldPosition;
            UpdateDropIndicator(worldPosition);
        }

        /// <summary>
        /// Handle drag end from UI - places object at screen position
        /// </summary>
        public GameObject HandleDropFromUI(string objectDataId, Vector2 screenPosition)
        {
            Debug.Log($"[BUILDER] HandleDropFromUI called - ID: {objectDataId}, ScreenPos: {screenPosition}");
            
            // Hide drop indicator
            HideDropIndicator();
            _isDraggingObject = false;

            // Convert screen position to world position
            Debug.Log($"[BUILDER] Converting screen to world position...");
            Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);
            Debug.Log($"[BUILDER] World position: {worldPosition}");

            // Check if position is within scene bounds
            Debug.Log($"[BUILDER] Checking if position is in scene bounds...");
            if (!IsPositionInSceneBounds(worldPosition))
            {
                Debug.LogWarning($"[BUILDER] ❌ Position {worldPosition} is outside scene bounds!");
                return null;
            }
            Debug.Log($"[BUILDER] ✅ Position is within scene bounds");

            // Place the object at the calculated world position
            Debug.Log($"[BUILDER] Calling PlaceObject...");
            GameObject result = PlaceObject(objectDataId, worldPosition, autoSelect: true);
            Debug.Log($"[BUILDER] PlaceObject returned: {(result != null ? result.name : "NULL")}");
            return result;
        }

        /// <summary>
        /// Cancel drag operation from UI
        /// </summary>
        public void CancelDragFromUI()
        {
            HideDropIndicator();
            _isDraggingObject = false;
        }

        #endregion

        #region Project Management (New Serialization System)

        /// <summary>
        /// Create a new sandbox project
        /// </summary>
        public void CreateNewProject(string projectName = null)
        {
            ClearScene();

            string name = projectName ?? "New Sandbox Project";
            _currentProject = new SandboxProjectData(name);

            // Set default configuration from current settings
            UpdateProjectSettingsFromBuilder();

            // Get the default scene created by the constructor
            _currentScene = _currentProject.GetActiveScene();
            _currentSceneName = _currentScene.sceneName;

            OnSceneLoaded?.Invoke(_currentScene);
        }

        /// <summary>
        /// Save the current project to JSON file
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
                    Debug.LogError($"Cannot create directory for: {savePath}");
                    return false;
                }

                bool success = SandboxProjectSerializer.SaveToFile(_currentProject, this, savePath);
                
                if (success)
                {
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
        /// Load a project from JSON file
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
        /// Delete a project file from disk
        /// </summary>
        public bool DeleteProject(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    
                    // If the deleted project is currently loaded, clear the current project
                    if (_currentProject != null && filePath.Contains(_currentProject.projectName))
                    {
                        _currentProject = null;
                        CreateNewScene();
                    }
                    
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get metadata for all projects in the default directory
        /// </summary>
        public List<SandboxProjectMetadata> GetAvailableProjects()
        {
            return SandboxProjectSerializer.GetProjectsInDirectory(_defaultProjectSavePath, "*.sbproj");
        }

        /// <summary>
        /// Get project information for display in UI
        /// </summary>
        public SandboxProjectMetadata GetCurrentProjectInfo()
        {
            if (_currentProject == null)
                return null;

            // Update project from runtime state before getting info
            SandboxProjectSerializer.UpdateProjectFromRuntimeState(_currentProject, this);

            return new SandboxProjectMetadata
            {
                projectName = _currentProject.projectName,
                filePath = GetDefaultProjectSavePath(),
                created = _currentProject.created,
                lastModified = _currentProject.lastModified,
                description = _currentProject.description,
                objectCount = _placedObjects?.Count ?? 0,
                hasTimelineIntegration = _currentProject.hasTimelineIntegration,
                sceneBounds = _currentProject.settings.sceneBounds
            };
        }

        /// <summary>
        /// Update project settings from current builder configuration
        /// </summary>
        private void UpdateProjectSettingsFromBuilder()
        {
            if (_currentProject?.settings == null)
                return;

            var settings = _currentProject.settings;
            
            // Update placement settings
            settings.snapToGrid = _snapToGrid;
            settings.gridSize = _gridSize;
            settings.defaultPlacementHeight = _defaultPlacementHeight;
            settings.minimumPlacementHeight = _minimumPlacementHeight;
            settings.useRaycastForPlacement = _useRaycastForPlacement;
            settings.useConsistentHeight = _useConsistentHeight;
            settings.placementLayers = _placementLayers;

            // Update visual settings
            settings.sceneBounds = _sceneBounds;
            settings.sceneBoundsColor = _sceneBoundsColor;
            settings.enableGizmos = _enableGizmos;
            settings.showBoundsGizmo = _showBoundsGizmo;
            settings.showAxesGizmo = _showAxesGizmo;
            settings.showHandlesGizmo = _showHandlesGizmo;
            settings.gizmoBoundsColor = _gizmoBoundsColor;
            settings.gizmoAxisLength = _gizmoAxisLength;
            settings.enableSceneGizmos = _enableSceneGizmos;
            settings.showSceneGrid = _showSceneGrid;
            settings.showSceneBounds = _showSceneBounds;
            settings.showStageAreaGizmo = _showStageAreaGizmo;
            settings.showPlacementHeightGizmo = _showPlacementHeightGizmo;
            settings.placementHeightColor = _placementHeightColor;

            // Update drop indicator settings
            settings.enableDropIndicator = _enableDropIndicator;
            settings.validDropColor = _validDropColor;
            settings.invalidDropColor = _invalidDropColor;
            settings.dropIndicatorSize = _dropIndicatorSize;

            // Update preview settings
            settings.autoPreview = _autoPreview;
            settings.previewDuration = _previewDuration;

            _currentProject.lastModified = System.DateTime.Now;
        }

        /// <summary>
        /// Apply project settings to builder configuration
        /// </summary>
        private void ApplyProjectSettingsToBuilder()
        {
            if (_currentProject?.settings == null)
                return;

            var settings = _currentProject.settings;

            // Apply placement settings
            _snapToGrid = settings.snapToGrid;
            _gridSize = settings.gridSize;
            _defaultPlacementHeight = settings.defaultPlacementHeight;
            _minimumPlacementHeight = settings.minimumPlacementHeight;
            _useRaycastForPlacement = settings.useRaycastForPlacement;
            _useConsistentHeight = settings.useConsistentHeight;
            _placementLayers = settings.placementLayers;

            // Apply visual settings
            _sceneBounds = settings.sceneBounds;
            _sceneBoundsColor = settings.sceneBoundsColor;
            _enableGizmos = settings.enableGizmos;
            _showBoundsGizmo = settings.showBoundsGizmo;
            _showAxesGizmo = settings.showAxesGizmo;
            _showHandlesGizmo = settings.showHandlesGizmo;
            _gizmoBoundsColor = settings.gizmoBoundsColor;
            _gizmoAxisLength = settings.gizmoAxisLength;
            _enableSceneGizmos = settings.enableSceneGizmos;
            _showSceneGrid = settings.showSceneGrid;
            _showSceneBounds = settings.showSceneBounds;
            _showStageAreaGizmo = settings.showStageAreaGizmo;
            _showPlacementHeightGizmo = settings.showPlacementHeightGizmo;
            _placementHeightColor = settings.placementHeightColor;

            // Apply drop indicator settings
            _enableDropIndicator = settings.enableDropIndicator;
            _validDropColor = settings.validDropColor;
            _invalidDropColor = settings.invalidDropColor;
            _dropIndicatorSize = settings.dropIndicatorSize;

            // Apply preview settings
            _autoPreview = settings.autoPreview;
            _previewDuration = settings.previewDuration;
        }

        /// <summary>
        /// Load project data and apply to current builder state
        /// </summary>
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

        /// <summary>
        /// Create a default project from current state
        /// </summary>
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

        /// <summary>
        /// Get default save path for projects
        /// </summary>
        private string GetDefaultProjectSavePath()
        {
            string projectName = _currentProject?.projectName ?? _currentSceneName ?? "Untitled";
            string safeName = SanitizeFileName(projectName);
            return System.IO.Path.Combine(_defaultProjectSavePath, $"{safeName}.sbproj");
        }

        #endregion

        #region Multi-Scene Management

        /// <summary>
        /// Create a new scene in the current project
        /// </summary>
        public SceneConfiguration CreateNewSceneInProject(string sceneName = null)
        {
            if (_currentProject == null)
            {
                CreateDefaultProject();
            }

            // Save current scene before switching and update it in the project's scene list
            if (_currentScene != null)
            {
                UpdateSceneConfiguration();
                
                // Update the scene in the project's scenes list
                int currentSceneIndex = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
                if (currentSceneIndex >= 0)
                {
                    _currentProject.scenes[currentSceneIndex] = _currentScene;
                    Debug.Log($"[SceneSandboxBuilder] Saved current scene '{_currentScene.sceneName}' with {_currentScene.placedObjects.Count} objects before creating new scene");
                }
            }

            string name = sceneName ?? $"Scene {_currentProject.scenes.Count + 1}";
            var newScene = _currentProject.AddScene(name);
            
            // Switch to new scene (this will clear and load the empty new scene)
            SwitchToScene(newScene.sceneId);
            
            Debug.Log($"[SceneSandboxBuilder] Created new scene: {name}");
            return newScene;
        }

        /// <summary>
        /// Switch to a different scene in the project
        /// </summary>
        public bool SwitchToScene(string sceneId)
        {
            if (_currentProject == null)
            {
                Debug.LogError("No project loaded");
                return false;
            }

            var targetScene = _currentProject.GetScene(sceneId);
            if (targetScene == null)
            {
                Debug.LogError($"Scene not found: {sceneId}");
                return false;
            }

            // Save current scene state and update it in the project's scene list
            if (_currentScene != null)
            {
                UpdateSceneConfiguration();
                
                // Update the scene in the project's scenes list
                int currentSceneIndex = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
                if (currentSceneIndex >= 0)
                {
                    _currentProject.scenes[currentSceneIndex] = _currentScene;
                    Debug.Log($"[SceneSandboxBuilder] Saved scene '{_currentScene.sceneName}' with {_currentScene.placedObjects.Count} objects");
                }
            }

            // Load target scene (LoadSceneConfiguration handles clearing internally)
            LoadSceneConfiguration(targetScene);
            _currentProject.SetActiveScene(sceneId);
            
            Debug.Log($"[SceneSandboxBuilder] Switched to scene: {targetScene.sceneName} with {targetScene.placedObjects.Count} objects");
            return true;
        }

        /// <summary>
        /// Delete a scene from the project
        /// </summary>
        public bool DeleteSceneFromProject(string sceneId)
        {
            if (_currentProject == null)
            {
                Debug.LogError("No project loaded");
                return false;
            }

            if (_currentProject.scenes.Count <= 1)
            {
                Debug.LogError("Cannot delete the last scene in project");
                return false;
            }

            // If deleting current scene, switch to another first
            if (_currentScene != null && _currentScene.sceneId == sceneId)
            {
                var otherScene = _currentProject.scenes.Find(s => s.sceneId != sceneId);
                if (otherScene != null)
                {
                    SwitchToScene(otherScene.sceneId);
                }
            }

            bool removed = _currentProject.RemoveScene(sceneId);
            if (removed)
            {
                Debug.Log($"[SceneSandboxBuilder] Deleted scene: {sceneId}");
            }
            
            return removed;
        }

        /// <summary>
        /// Get all scenes in the current project
        /// </summary>
        public List<SceneConfiguration> GetAllScenesInProject()
        {
            return _currentProject?.scenes ?? new List<SceneConfiguration>();
        }

        /// <summary>
        /// Duplicate the current scene
        /// </summary>
        public SceneConfiguration DuplicateCurrentScene()
        {
            if (_currentScene == null || _currentProject == null)
            {
                Debug.LogError("No scene or project loaded");
                return null;
            }

            // Save current state and update it in the project's scene list
            UpdateSceneConfiguration();
            
            // Update the scene in the project's scenes list
            int currentSceneIndex = _currentProject.scenes.FindIndex(s => s.sceneId == _currentScene.sceneId);
            if (currentSceneIndex >= 0)
            {
                _currentProject.scenes[currentSceneIndex] = _currentScene;
            }

            // Create duplicate
            var duplicate = new SceneConfiguration($"{_currentScene.sceneName} (Copy)");
            duplicate.description = _currentScene.description;
            duplicate.defaultCameraPosition = _currentScene.defaultCameraPosition;
            duplicate.defaultCameraRotation = _currentScene.defaultCameraRotation;
            duplicate.environmentColor = _currentScene.environmentColor;
            duplicate.environmentLighting = _currentScene.environmentLighting;

            // Copy all placed objects
            foreach (var obj in _currentScene.placedObjects)
            {
                var objCopy = new Data.PlacedObjectData(obj.objectDataId, obj.position)
                {
                    id = System.Guid.NewGuid().ToString(), // New ID for copy
                    rotation = obj.rotation,
                    scale = obj.scale,
                    customName = obj.customName,
                    properties = new Dictionary<string, object>(obj.properties)
                };
                duplicate.AddPlacedObject(objCopy);
            }

            _currentProject.scenes.Add(duplicate);
            
            Debug.Log($"[SceneSandboxBuilder] Duplicated scene: {duplicate.sceneName} with {duplicate.placedObjects.Count} objects");
            return duplicate;
        }

        /// <summary>
        /// Rename the current scene
        /// </summary>
        public void RenameCurrentScene(string newName)
        {
            if (_currentScene != null)
            {
                _currentScene.sceneName = newName;
                _currentSceneName = newName;
                _currentScene.lastModified = System.DateTime.Now;
                Debug.Log($"[SceneSandboxBuilder] Renamed scene to: {newName}");
            }
        }

        /// <summary>
        /// Get scene count in current project
        /// </summary>
        public int GetSceneCount()
        {
            return _currentProject?.scenes.Count ?? 0;
        }

        /// <summary>
        /// Get metadata for all scenes in project (for UI)
        /// </summary>
        public System.Collections.Generic.List<Data.SceneMetadata> GetAllSceneMetadata()
        {
            var metadataList = new System.Collections.Generic.List<Data.SceneMetadata>();
            
            if (_currentProject == null)
                return metadataList;

            string activeSceneId = _currentProject.activeSceneId;
            
            foreach (var scene in _currentProject.scenes)
            {
                bool isActive = scene.sceneId == activeSceneId;
                metadataList.Add(new Data.SceneMetadata(scene, isActive));
            }
            
            return metadataList;
        }

        #endregion

        /// <summary>
        /// Place an object using the new factory system
        /// </summary>
        public GameObject PlaceObjectWithFactory(string objectDataId, Vector3 position, bool autoSelect = true)
        {
            if (_objectLibrary == null) return null;

            var objectData = _objectLibrary.GetObjectById(objectDataId);
            if (objectData == null || objectData.prefab == null) return null;

            // Create placed object data using existing API
            string placedObjectId = System.Guid.NewGuid().ToString();
            var placedObjectData = new Data.PlacedObjectData(objectDataId, position)
            {
                id = placedObjectId,
                rotation = objectData.prefab.transform.eulerAngles,
                scale = objectData.defaultScale != Vector3.zero ? objectData.defaultScale : objectData.prefab.transform.localScale,
                customName = objectData.displayName
            };

            // Apply positioning
            placedObjectData.position = _snapToGrid ? SnapToGrid(position) : position;
            placedObjectData.position = GetSurfacePosition(placedObjectData.position);

            // Store factory-related properties in the existing properties dictionary
            placedObjectData.properties["objectType"] = SandboxObjectFactory.DetermineObjectType(objectData.prefab);
            placedObjectData.properties["libraryName"] = objectData.displayName;
            placedObjectData.properties["isActive"] = true;

            // Create new factory data for runtime object creation
            var factoryData = new Serialization.PlacedObjectData
            {
                instanceId = placedObjectId,
                objectType = placedObjectData.properties["objectType"].ToString(),
                position = placedObjectData.position,
                rotation = Quaternion.Euler(placedObjectData.rotation),
                scale = placedObjectData.scale,
                customName = placedObjectData.customName,
                isActive = true,
                customProperties = new Dictionary<string, object>(placedObjectData.properties)
            };

            // Create runtime object using factory
            GameObject newObject = SandboxObjectFactory.CreateRuntimeObjectFromData(factoryData, _stageArea);
            
            if (newObject == null)
            {
                return null;
            }

            // Add draggable component for interaction
            var draggable = newObject.GetComponent<TransformableItem>();
            if (draggable == null)
            {
                draggable = newObject.AddComponent<TransformableItem>();
            }

            draggable.SetObjectData(objectDataId, placedObjectId);
            BindDraggableEvents(draggable);

            // Add to scene and project
            if (_currentScene != null)
            {
                _currentScene.AddPlacedObject(placedObjectData);
            }
            
            _placedObjects[placedObjectId] = newObject;

            // Ensure project exists
            if (_currentProject == null)
            {
                CreateDefaultProject();
            }

            // Select if requested
            if (autoSelect)
            {
                SelectObject(newObject);
            }

            OnObjectPlaced?.Invoke(newObject);

            return newObject;
        }

        /// <summary>
        /// Start preview mode using the timeline director
        /// </summary>
        public void StartPreview()
        {
            if (_timelineDirector == null || IsInPreviewMode) return;

            StopPreview(); // Ensure clean state

            // Create preview objects (snapshots of current scene)
            CreatePreviewObjects();

            // Set up timeline director with current scene
            SetupTimelineForPreview();

            // Start playback
            _timelineDirector.Play();

            OnPreviewStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Stop preview mode and return to editing
        /// </summary>
        public void StopPreview()
        {
            if (_timelineDirector != null)
            {
                _timelineDirector.Stop();
            }

            // Clean up preview objects
            CleanupPreviewObjects();

            OnPreviewStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Toggle gizmo visibility for all objects
        /// </summary>
        public void SetGizmoVisibility(bool visible)
        {
            _enableGizmos = visible;
        }

        /// <summary>
        /// Toggle scene gizmo visibility
        /// </summary>
        public void SetSceneGizmoVisibility(bool visible)
        {
            _enableSceneGizmos = visible;
        }

        /// <summary>
        /// Toggle specific scene gizmo features
        /// </summary>
        public void SetSceneGridVisibility(bool visible)
        {
            _showSceneGrid = visible;
        }

        public void SetSceneBoundsVisibility(bool visible)
        {
            _showSceneBounds = visible;
        }

        public void SetStageAreaGizmoVisibility(bool visible)
        {
            _showStageAreaGizmo = visible;
        }

        /// <summary>
        /// Toggle placement height gizmo visibility
        /// </summary>
        public void SetPlacementHeightGizmoVisibility(bool visible)
        {
            _showPlacementHeightGizmo = visible;
        }

        /// <summary>
        /// Set the default placement height
        /// </summary>
        public void SetDefaultPlacementHeight(float height)
        {
            _defaultPlacementHeight = height;
        }

        /// <summary>
        /// Toggle raycast-based placement vs fixed height placement
        /// </summary>
        public void SetUseRaycastForPlacement(bool useRaycast)
        {
            _useRaycastForPlacement = useRaycast;
        }

        /// <summary>
        /// Set the scene bounds for visualization and validation
        /// </summary>
        public void SetSceneBounds(Vector3 bounds)
        {
            _sceneBounds = bounds;
        }

        /// <summary>
        /// Set the scene bounds color
        /// </summary>
        public void SetSceneBoundsColor(Color color)
        {
            _sceneBoundsColor = color;
        }

        /// <summary>
        /// Toggle drop indicator system
        /// </summary>
        public void SetDropIndicatorEnabled(bool enabled)
        {
            _enableDropIndicator = enabled;
            if (!enabled)
            {
                HideDropIndicator();
            }
        }

        /// <summary>
        /// Set drop indicator colors
        /// </summary>
        public void SetDropIndicatorColors(Color validColor, Color invalidColor)
        {
            _validDropColor = validColor;
            _invalidDropColor = invalidColor;
        }

        /// <summary>
        /// Check if a position is valid for object placement
        /// </summary>
        public bool IsValidPlacementPosition(Vector3 position)
        {
            return IsPositionInSceneBounds(position);
        }

        /// <summary>
        /// Get the current drop indicator position during drag operations
        /// </summary>
        public Vector3 GetDropIndicatorPosition()
        {
            return _dragPreviewPosition;
        }

        #region Selection Manager Event Handlers

        private void HandleItemSelected(TransformableItem item)
        {
            if (item != null)
            {
                _selectedObject = item.gameObject;
                OnObjectSelected?.Invoke(_selectedObject);
            }
        }

        private void HandleItemDeselected(TransformableItem item)
        {
            if (item != null && _selectedObject == item.gameObject)
            {
                _selectedObject = null;
                OnObjectSelected?.Invoke(null);
            }
        }

        #endregion

        #region Helper Methods

        private void BindDraggableEvents(TransformableItem draggable)
        {
            draggable.OnDragStarted += (item, pos) => SelectObject(item.gameObject);
            draggable.OnDragEnded += (item, pos) => UpdateObjectInScene(item);
            draggable.OnItemClicked += (item) => SelectObject(item.gameObject);
            draggable.OnItemSelected += (item) => SelectObject(item.gameObject);
        }

        private void UpdateObjectInScene(TransformableItem draggable)
        {
            if (_currentScene == null) return;

            var placedObjectData = _currentScene.GetPlacedObject(draggable.ObjectId);
            if (placedObjectData != null)
            {
                placedObjectData.position = draggable.transform.position;
                placedObjectData.rotation = draggable.transform.eulerAngles;
                placedObjectData.scale = draggable.transform.localScale;
                _currentScene.lastModified = System.DateTime.Now;
            }
        }

        private void UpdateSceneConfiguration()
        {
            foreach (var kvp in _placedObjects)
            {
                var obj = kvp.Value;
                var draggable = obj.GetComponent<TransformableItem>();
                if (draggable != null)
                {
                    UpdateObjectInScene(draggable);
                }
            }
        }

        private void LoadSceneConfiguration(SceneConfiguration sceneConfig)
        {
            // Clear current scene GameObjects and runtime state first
            // Important: Clear before setting _currentScene to avoid clearing the new scene's data
            if (_placedObjects != null)
            {
                var objectsToRemove = new List<GameObject>(_placedObjects.Values);
                foreach (var obj in objectsToRemove)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                _placedObjects.Clear();
            }
            
            SelectObject(null);

            // Now set the new scene (after clearing runtime state)
            _currentScene = sceneConfig;
            _currentSceneName = sceneConfig.sceneName;

            // Recreate all placed objects
            foreach (var placedObjectData in sceneConfig.placedObjects)
            {
                var objectData = _objectLibrary.GetObjectById(placedObjectData.objectDataId);
                if (objectData?.prefab != null)
                {
                    GameObject obj = Instantiate(objectData.prefab, _stageArea);
                    obj.transform.position = placedObjectData.position;
                    obj.transform.eulerAngles = placedObjectData.rotation;
                    obj.transform.localScale = placedObjectData.scale;
                    obj.name = placedObjectData.customName ?? objectData.displayName;

                    var draggable = obj.GetComponent<TransformableItem>();
                    if (draggable == null)
                    {
                        draggable = obj.AddComponent<TransformableItem>();
                    }

                    draggable.SetObjectData(placedObjectData.objectDataId, placedObjectData.id);
                    BindDraggableEvents(draggable);

                    _placedObjects[placedObjectData.id] = obj;
                }
            }

            OnSceneLoaded?.Invoke(_currentScene);
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            if (!_snapToGrid) return position;

            float snappedX = Mathf.Round(position.x / _gridSize) * _gridSize;
            float snappedZ = Mathf.Round(position.z / _gridSize) * _gridSize;
            return new Vector3(snappedX, position.y, snappedZ);
        }

        private Vector3 GetSurfacePosition(Vector3 position)
        {
            // If raycast placement is disabled, always use default placement height
            if (!_useRaycastForPlacement)
            {
                Vector3 result = new Vector3(position.x, _defaultPlacementHeight, position.z);
                return result;
            }

            // Try to find surface through raycast
            Vector3 rayStart = position + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, _placementLayers))
            {
                // Found surface, use the hit point's Y position but respect minimum height
                float finalY = Mathf.Max(hit.point.y, _minimumPlacementHeight);
                Vector3 result = new Vector3(position.x, finalY, position.z);
                return result;
            }

            // No surface found, use default placement height (respecting minimum)
            float finalDefaultHeight = Mathf.Max(_defaultPlacementHeight, _minimumPlacementHeight);
            Vector3 defaultResult = new Vector3(position.x, finalDefaultHeight, position.z);
            return defaultResult;
        }

        private GameObject GetObjectAtScreenPosition(Vector2 screenPosition)
        {
            if (_sceneCamera == null) return null;

            Ray ray = _sceneCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.collider.gameObject;
            }

            return null;
        }

        /// <summary>
        /// Convert screen position to world position for drop indicator
        /// </summary>
        private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
        {
            if (_sceneCamera == null)
            {
                return Vector3.zero;
            }

            Ray ray = _sceneCamera.ScreenPointToRay(screenPosition);
            Vector3 worldPos;

            // If raycast placement is disabled or consistent height is enabled, always project to default height
            if (!_useRaycastForPlacement || _useConsistentHeight)
            {
                Vector3 rayDirection = ray.direction.normalized;
                if (Mathf.Abs(rayDirection.y) < 0.001f)
                {
                    return new Vector3(0, _defaultPlacementHeight, 0);
                }

                float t = (_defaultPlacementHeight - ray.origin.y) / rayDirection.y;
                worldPos = ray.origin + rayDirection * t;
                return worldPos;
            }

            // Try to hit the ground or existing objects
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _placementLayers))
            {
                float finalY = Mathf.Max(hit.point.y, _minimumPlacementHeight);
                worldPos = new Vector3(hit.point.x, finalY, hit.point.z);
            }
            else
            {
                Vector3 rayDirection = ray.direction.normalized;
                if (Mathf.Abs(rayDirection.y) < 0.001f)
                {
                    return new Vector3(0, _defaultPlacementHeight, 0);
                }
                float t = (_defaultPlacementHeight - ray.origin.y) / rayDirection.y;
                Vector3 projectedPos = ray.origin + rayDirection * t;
                float finalY = Mathf.Max(projectedPos.y, _minimumPlacementHeight);
                worldPos = new Vector3(projectedPos.x, finalY, projectedPos.z);
            }

            return worldPos;
        }

        /// <summary>
        /// Check if a position is within the scene bounds
        /// </summary>
        private bool IsPositionInSceneBounds(Vector3 position)
        {
            Vector3 center = _stageArea != null ? _stageArea.position : transform.position;
            Vector3 localPos = position - center;

            bool result = Mathf.Abs(localPos.x) <= _sceneBounds.x / 2f &&
                   Mathf.Abs(localPos.y) <= _sceneBounds.y / 2f &&
                   Mathf.Abs(localPos.z) <= _sceneBounds.z / 2f;
                   
            return result;
        }

        /// <summary>
        /// Show drop indicator at specified position
        /// </summary>
        private void ShowDropIndicator(Vector3 position)
        {
            if (!_enableDropIndicator)
            {
                return;
            }

            // Create drop indicator if it doesn't exist
            if (_currentDropIndicator == null)
            {
                CreateDropIndicator();
            }

            if (_currentDropIndicator != null)
            {
                _currentDropIndicator.SetActive(true);

                UpdateDropIndicator(position);

            }
        }

        /// <summary>
        /// Update drop indicator position and appearance
        /// </summary>
        private void UpdateDropIndicator(Vector3 position)
        {
            if (!_enableDropIndicator)
            {
                return;
            }

            // Create drop indicator if it doesn't exist
            if (_currentDropIndicator == null)
            {
                CreateDropIndicator();

                if (_currentDropIndicator != null)
                {
                    _currentDropIndicator.SetActive(true);
                }
                else
                {
                    return;
                }
            }

            // Ensure indicator is active
            if (!_currentDropIndicator.activeInHierarchy)
            {
                _currentDropIndicator.SetActive(true);
            }

            // Snap to grid if enabled
            Vector3 finalPosition = _snapToGrid ? SnapToGrid(position) : position;
            // Note: GetWorldPositionFromScreen already handles surface detection, 
            // so we don't need to call GetSurfacePosition again here


            _dragPreviewPosition = finalPosition;
            _currentDropIndicator.transform.position = finalPosition;

            // Update color based on validity
            bool isValidPosition = IsPositionInSceneBounds(finalPosition);
            Color indicatorColor = isValidPosition ? _validDropColor : _invalidDropColor;


            // Apply color to the indicator
            var renderer = _currentDropIndicator.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = indicatorColor;
            }
        }

        /// <summary>
        /// Hide drop indicator
        /// </summary>
        private void HideDropIndicator()
        {
            if (_currentDropIndicator != null)
            {
                _currentDropIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Create drop indicator GameObject
        /// </summary>
        private void CreateDropIndicator()
        {
            if (_dropIndicatorPrefab != null)
            {
                try
                {
                    _currentDropIndicator = Instantiate(_dropIndicatorPrefab);
                    _currentDropIndicator.name = "Drop Indicator (Prefab)";
                }
                catch (System.Exception)
                {
                    _currentDropIndicator = null;
                }
            }

            // If prefab failed or wasn't assigned, create default
            if (_currentDropIndicator == null)
            {
                try
                {
                    // Create default drop indicator
                    _currentDropIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    _currentDropIndicator.name = "Drop Indicator (Default)";

                    // Remove collider to prevent interference
                    var collider = _currentDropIndicator.GetComponent<Collider>();
                    if (collider != null)
                    {
                        DestroyImmediate(collider);
                    }

                    // Scale to indicator size
                    _currentDropIndicator.transform.localScale = new Vector3(_dropIndicatorSize, 0.1f, _dropIndicatorSize);

                    // Create a more visible material
                    var renderer = _currentDropIndicator.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material indicatorMaterial = new Material(Shader.Find("Standard"));
                        indicatorMaterial.color = _validDropColor;
                        indicatorMaterial.SetFloat("_Metallic", 0f);
                        indicatorMaterial.SetFloat("_Glossiness", 0.5f);
                        renderer.material = indicatorMaterial;
                    }
                }
                catch (System.Exception)
                {
                    return;
                }
            }

            if (_currentDropIndicator != null)
            {
                _currentDropIndicator.SetActive(false);
            }
        }

        private string GetDefaultSavePath()
        {
            string safeName = SanitizeFileName(_currentSceneName ?? "Untitled");
            return System.IO.Path.Combine(_defaultSavePath, $"{safeName}.json");
        }

        /// <summary>
        /// Sanitize filename to remove invalid characters
        /// </summary>
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

        /// <summary>
        /// Ensure directory exists before saving
        /// </summary>
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

        /// <summary>
        /// Draw gizmos for the scene and selected objects
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_enableGizmos && !_enableSceneGizmos) return;

            // Draw scene gizmos
            if (_enableSceneGizmos)
            {
                DrawSceneGizmos();
            }

            // Draw object gizmos
            if (_enableGizmos && _selectedObject != null)
            {
                DrawObjectGizmos(_selectedObject);
            }

            // Draw drop indicator gizmos when dragging
            if (_enableDropIndicator && _isDraggingObject)
            {
                DrawDropIndicatorGizmos();
            }
        }

        /// <summary>
        /// Draw scene-level gizmos (grid, bounds, stage area)
        /// </summary>
        private void DrawSceneGizmos()
        {
            // Draw scene grid
            if (_showSceneGrid)
            {
                DrawSceneGrid();
            }

            // Draw scene bounds
            if (_showSceneBounds)
            {
                DrawSceneBounds();
            }

            // Draw stage area
            if (_showStageAreaGizmo && _stageArea != null)
            {
                DrawStageArea();
            }

            // Draw placement height level
            if (_showPlacementHeightGizmo)
            {
                DrawPlacementHeight();
            }
        }

        /// <summary>
        /// Draw gizmos for a selected object
        /// </summary>
        private void DrawObjectGizmos(GameObject obj)
        {
            if (obj == null) return;

            Bounds bounds = CalculateObjectBounds(obj);

            // Draw bounding box
            if (_showBoundsGizmo)
            {
                Gizmos.color = _gizmoBoundsColor;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            // Draw coordinate axes
            if (_showAxesGizmo)
            {
                DrawObjectAxes(obj.transform, bounds.center);
            }

            // Draw manipulation handles
            if (_showHandlesGizmo)
            {
                DrawManipulationHandles(bounds);
            }
        }

        /// <summary>
        /// Draw the scene grid
        /// </summary>
        private void DrawSceneGrid()
        {
            Vector3 center = _stageArea != null ? _stageArea.position : transform.position;
            float gridExtent = 20f; // Grid extends 20 units from center
            int gridLines = Mathf.RoundToInt(gridExtent * 2 / _gridSize);

            Gizmos.color = Color.gray;

            // Draw vertical lines (along Z-axis)
            for (int i = 0; i <= gridLines; i++)
            {
                float x = center.x - gridExtent + (i * _gridSize);
                Vector3 start = new Vector3(x, center.y, center.z - gridExtent);
                Vector3 end = new Vector3(x, center.y, center.z + gridExtent);

                // Center line in different color
                if (Mathf.Approximately(x, center.x))
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(start, end);
                    Gizmos.color = Color.gray;
                }
                else
                {
                    Gizmos.DrawLine(start, end);
                }
            }

            // Draw horizontal lines (along X-axis)
            for (int i = 0; i <= gridLines; i++)
            {
                float z = center.z - gridExtent + (i * _gridSize);
                Vector3 start = new Vector3(center.x - gridExtent, center.y, z);
                Vector3 end = new Vector3(center.x + gridExtent, center.y, z);

                // Center line in different color
                if (Mathf.Approximately(z, center.z))
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(start, end);
                    Gizmos.color = Color.gray;
                }
                else
                {
                    Gizmos.DrawLine(start, end);
                }
            }

            // Draw origin marker
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(center, 0.1f);
        }

        /// <summary>
        /// Draw scene boundaries
        /// </summary>
        private void DrawSceneBounds()
        {
            Vector3 center = _stageArea != null ? _stageArea.position : transform.position;
            Vector3 size = _sceneBounds; // Use configurable scene bounds

            Gizmos.color = _sceneBoundsColor;
            Gizmos.DrawWireCube(center, size);

            // Draw corner markers
            float markerSize = 0.2f;
            Vector3[] corners = {
                center + new Vector3(-size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, -size.y/2, size.z/2),
                center + new Vector3(size.x/2, -size.y/2, size.z/2)
            };

            foreach (var corner in corners)
            {
                Gizmos.DrawWireCube(corner, Vector3.one * markerSize);
            }
        }

        /// <summary>
        /// Draw stage area gizmo
        /// </summary>
        private void DrawStageArea()
        {
            if (_stageArea == null) return;

            Vector3 stagePos = _stageArea.position;
            Vector3 stageScale = _stageArea.localScale;

            // Draw stage area bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(stagePos, stageScale);

            // Draw floor
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 floorPos = new Vector3(stagePos.x, stagePos.y - stageScale.y / 2f, stagePos.z);
            Vector3 floorSize = new Vector3(stageScale.x, 0.01f, stageScale.z);
            Gizmos.DrawCube(floorPos, floorSize);
        }

        /// <summary>
        /// Draw default placement height level
        /// </summary>
        private void DrawPlacementHeight()
        {
            Vector3 center = _stageArea != null ? _stageArea.position : transform.position;
            Vector3 placementPos = new Vector3(center.x, _defaultPlacementHeight, center.z);

            // Draw placement height plane
            Gizmos.color = _placementHeightColor;

            // Draw a grid at the placement height
            float gridExtent = 15f; // Smaller than scene grid
            int gridLines = Mathf.RoundToInt(gridExtent * 2 / _gridSize);

            // Draw horizontal grid lines at placement height
            for (int i = 0; i <= gridLines; i++)
            {
                float x = center.x - gridExtent + (i * _gridSize);
                Vector3 start = new Vector3(x, _defaultPlacementHeight, center.z - gridExtent);
                Vector3 end = new Vector3(x, _defaultPlacementHeight, center.z + gridExtent);
                Gizmos.DrawLine(start, end);
            }

            for (int i = 0; i <= gridLines; i++)
            {
                float z = center.z - gridExtent + (i * _gridSize);
                Vector3 start = new Vector3(center.x - gridExtent, _defaultPlacementHeight, z);
                Vector3 end = new Vector3(center.x + gridExtent, _defaultPlacementHeight, z);
                Gizmos.DrawLine(start, end);
            }

            // Draw placement height indicator at center
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(placementPos, 0.2f);

            // Draw vertical line from ground to placement height
            Vector3 groundPos = new Vector3(center.x, center.y, center.z);
            Gizmos.color = _placementHeightColor;
            Gizmos.DrawLine(groundPos, placementPos);

            // Draw height text indicator (visual marker)
            Vector3[] heightMarkers = {
                placementPos + Vector3.right * 2f,
                placementPos + Vector3.left * 2f,
                placementPos + Vector3.forward * 2f,
                placementPos + Vector3.back * 2f
            };

            foreach (var marker in heightMarkers)
            {
                Gizmos.DrawWireCube(marker, Vector3.one * 0.1f);
            }
        }

        /// <summary>
        /// Draw coordinate axes for an object
        /// </summary>
        private void DrawObjectAxes(Transform objTransform, Vector3 center)
        {
            // X axis (Red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, center + objTransform.right * _gizmoAxisLength);

            // Y axis (Green)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + objTransform.up * _gizmoAxisLength);

            // Z axis (Blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(center, center + objTransform.forward * _gizmoAxisLength);
        }

        /// <summary>
        /// Draw manipulation handles
        /// </summary>
        private void DrawManipulationHandles(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            float handleSize = 0.1f;

            Gizmos.color = Color.white;

            // Corner handles
            Vector3[] corners = {
                center + new Vector3(-size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(size.x/2, -size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, -size.y/2, size.z/2),
                center + new Vector3(size.x/2, -size.y/2, size.z/2),
                center + new Vector3(-size.x/2, size.y/2, -size.z/2),
                center + new Vector3(size.x/2, size.y/2, -size.z/2),
                center + new Vector3(-size.x/2, size.y/2, size.z/2),
                center + new Vector3(size.x/2, size.y/2, size.z/2)
            };

            foreach (var corner in corners)
            {
                Gizmos.DrawWireCube(corner, Vector3.one * handleSize);
            }

            // Face center handles
            Gizmos.color = _gizmoBoundsColor;
            Vector3[] faces = {
                center + new Vector3(-size.x/2, 0, 0), // Left
                center + new Vector3(size.x/2, 0, 0),  // Right
                center + new Vector3(0, -size.y/2, 0), // Bottom
                center + new Vector3(0, size.y/2, 0),  // Top
                center + new Vector3(0, 0, -size.z/2), // Back
                center + new Vector3(0, 0, size.z/2)   // Front
            };

            foreach (var face in faces)
            {
                Gizmos.DrawWireCube(face, Vector3.one * handleSize * 0.7f);
            }
        }

        /// <summary>
        /// Calculate bounds for an object
        /// </summary>
        private Bounds CalculateObjectBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();

            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return bounds;
            }
            else if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
                return bounds;
            }
            else
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }
        }

        /// <summary>
        /// Draw drop indicator gizmos during drag operations
        /// </summary>
        private void DrawDropIndicatorGizmos()
        {
            if (!_isDraggingObject) return;

            Vector3 position = _dragPreviewPosition;
            bool isValidPosition = IsPositionInSceneBounds(position);

            // Draw drop zone circle
            Gizmos.color = isValidPosition ? _validDropColor : _invalidDropColor;

            // Draw a circle on the ground
            float radius = _dropIndicatorSize * 0.5f;
            int segments = 32;
            float angleStep = 360f / segments;

            Vector3 prevPoint = position + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }

            // Draw vertical indicator line
            Gizmos.DrawLine(position, position + Vector3.up * 2f);

            // Draw grid snap indicator if snapping is enabled
            if (_snapToGrid)
            {
                Vector3 snappedPos = SnapToGrid(position);
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(snappedPos + Vector3.up * 0.05f, Vector3.one * _gridSize * 0.1f);
            }
        }

        private void ShowContextMenu(GameObject obj, Vector2 screenPosition)
        {
            // TODO: Implement context menu system

        }

        private void ShowGeneralContextMenu(Vector2 screenPosition)
        {
            // TODO: Implement general context menu

        }

        private void CreatePreviewObjects()
        {
            _previewObjects.Clear();

            foreach (var kvp in _placedObjects)
            {
                var original = kvp.Value;
                var preview = Instantiate(original);

                // Disable draggable component during preview
                var draggable = preview.GetComponent<TransformableItem>();
                if (draggable != null)
                {
                    draggable.enabled = false;
                }

                _previewObjects.Add(preview);
            }
        }

        private void CleanupPreviewObjects()
        {
            foreach (var previewObj in _previewObjects)
            {
                if (previewObj != null)
                {
                    Destroy(previewObj);
                }
            }
            _previewObjects.Clear();
        }

        private void SetupTimelineForPreview()
        {
            if (_timelineDirector?.Project == null) return;

            // TODO: Set up timeline tracks for preview objects
            // This would integrate with the existing MiniTimeline system
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from selection manager events
            if (TransformableSelectionManager.Instance != null)
            {
                TransformableSelectionManager.Instance.OnItemSelected -= HandleItemSelected;
                TransformableSelectionManager.Instance.OnItemDeselected -= HandleItemDeselected;
            }

            StopPreview();

            // Cleanup drop indicator
            if (_currentDropIndicator != null)
            {
                DestroyImmediate(_currentDropIndicator);
            }
        }
    }
}