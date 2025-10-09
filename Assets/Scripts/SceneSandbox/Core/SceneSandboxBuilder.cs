using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SceneSandbox.Data;
using SceneSandbox.Input;
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
        [SerializeField] private InputActionAsset _inputActions;
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
        [SerializeField] private string _defaultSavePath = "Assets/SceneSandboxBuilder/SavedScenes/";
        [SerializeField] private string _currentSceneName = "Untitled Scene";
        [SerializeField] private string _defaultProjectSavePath = "Assets/SceneSandboxBuilder/SavedProjects/";

        // Components
        private SandboxInputHandler _inputHandler;
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
        /// Automatically find and assign the InputActions asset if not already set
        /// </summary>
        private void AutoAssignInputActions()
        {
            if (_inputActions != null) return;

            // Try to find the UIAndGameplay InputActions asset
            string[] assetPaths = {
                "Assets/Settings/InputActions/UIAndGameplay.inputactions",
                "Assets/InputActions/UIAndGameplay.inputactions",
                "Assets/Settings/UIAndGameplay.inputactions"
            };

            foreach (string path in assetPaths)
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset != null)
                {
                    _inputActions = asset;
                    return;
                }
            }

        }

        private void Awake()
        {
            // Auto-assign InputActions if not set
            if (_inputActions == null)
            {
                AutoAssignInputActions();
            }

            InitializeComponents();
            InitializeState();

        }

        private void Start()
        {
            SetupInputHandler();
            CreateNewScene();

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

            // Get or create input handler
            _inputHandler = GetComponent<SandboxInputHandler>();
            if (_inputHandler == null)
            {
                _inputHandler = gameObject.AddComponent<SandboxInputHandler>();
            }
        }

        private void InitializeState()
        {
            _placedObjects = new Dictionary<string, GameObject>();
            _previewObjects = new List<GameObject>();

            // Don't create drop indicator here - create it when first needed
            // This avoids potential issues with early initialization
        }

        private void SetupInputHandler()
        {

            if (_inputHandler != null && _inputActions != null)
            {
                _inputHandler.Initialize(_inputActions);

                // Bind input events
                _inputHandler.OnClick += HandleClick;
                _inputHandler.OnRightClick += HandleRightClick;
                _inputHandler.OnDragStart += HandleDragStart;
                _inputHandler.OnDragMove += HandleDragMove;
                _inputHandler.OnDragEnd += HandleDragEnd;
                _inputHandler.OnObjectSelect += HandleObjectSelect;
                _inputHandler.OnObjectDelete += HandleObjectDelete;
                _inputHandler.OnPointMove += HandlePointMove;
                _inputHandler.Enable();

            }
            else
            {
                // Input handler or actions not available; nothing to initialize.
            }
        }

        #region Public Interface

        /// <summary>
        /// Place an object from the library at the specified position
        /// </summary>
        public GameObject PlaceObject(string objectDataId, Vector3 position, bool autoSelect = true)
        {
            if (_objectLibrary == null) return null;

            var objectData = _objectLibrary.GetObjectById(objectDataId);
            if (objectData == null || objectData.prefab == null) return null;

            // Instantiate the object
            GameObject newObject = Instantiate(objectData.prefab, _stageArea);
            newObject.name = objectData.displayName;

            // Apply placement settings
            Vector3 finalPosition = _snapToGrid ? SnapToGrid(position) : position;
            finalPosition = GetSurfacePosition(finalPosition);
            newObject.transform.position = finalPosition;

            // Apply scale - preserve prefab scale if defaultScale is zero
            Vector3 targetScale = objectData.defaultScale;
            if (targetScale == Vector3.zero)
            {
                targetScale = objectData.prefab.transform.localScale;
            }
            newObject.transform.localScale = targetScale;

            // Add draggable component if not present
            var draggable = newObject.GetComponent<DraggableItem>();
            if (draggable == null)
            {
                draggable = newObject.AddComponent<DraggableItem>();
            }

            // Set up draggable item
            string placedObjectId = System.Guid.NewGuid().ToString();
            draggable.SetObjectData(objectDataId, placedObjectId);

            // Bind draggable events
            BindDraggableEvents(draggable);

            // Add to scene configuration
            var placedObjectData = new Data.PlacedObjectData(objectDataId, finalPosition)
            {
                id = placedObjectId,
                rotation = newObject.transform.eulerAngles,
                scale = newObject.transform.localScale,
                customName = objectData.displayName
            };

            if (_currentScene != null)
            {
                _currentScene.AddPlacedObject(placedObjectData);
            }
            _placedObjects[placedObjectId] = newObject;

            // Select the object if requested
            if (autoSelect)
            {
                SelectObject(newObject);
            }

            OnObjectPlaced?.Invoke(newObject);

            return newObject;
        }

        /// <summary>
        /// Remove an object from the scene
        /// </summary>
        public bool RemoveObject(GameObject obj)
        {
            if (obj == null) return false;

            var draggable = obj.GetComponent<DraggableItem>();
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
        /// </summary>
        public void SelectObject(GameObject obj)
        {
            // Deselect previous object
            if (_selectedObject != null)
            {
                var prevDraggable = _selectedObject.GetComponent<DraggableItem>();
                prevDraggable?.SetSelectedState(false);
            }

            _selectedObject = obj;

            // Select new object
            if (_selectedObject != null)
            {
                var draggable = _selectedObject.GetComponent<DraggableItem>();
                draggable?.SetSelectedState(true);

                // Show indicator for draggable objects when selected
                if (draggable != null && draggable.CanDrag && _enableDropIndicator)
                {
                    ShowDropIndicator(_selectedObject.transform.position);
                }
            }
            else
            {
                // Hide indicator when nothing is selected
                HideDropIndicator();
            }

            OnObjectSelected?.Invoke(_selectedObject);
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
                string json = JsonUtility.ToJson(_currentScene, true);

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(savePath));
                System.IO.File.WriteAllText(savePath, json);

                OnSceneSaved?.Invoke(_currentScene);

                return true;
            }
            catch (System.Exception)
            {
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

            // Create new scene as part of the project
            _currentScene = new SceneConfiguration($"{name} Scene");
            _currentProject.sceneConfiguration = _currentScene;
            
            _currentSceneName = _currentScene.sceneName;

            OnSceneLoaded?.Invoke(_currentScene);
            Debug.Log($"[SceneSandboxBuilder] Created new project: {name}");
        }

        /// <summary>
        /// Save the current project to JSON file
        /// </summary>
        public bool SaveProject(string filePath = null)
        {
            if (_currentProject == null)
            {
                Debug.LogWarning("[SceneSandboxBuilder] No project to save. Creating default project.");
                CreateDefaultProject();
            }

            try
            {
                // Update scene configuration with current object states before saving
                if (_currentScene != null)
                {
                    UpdateSceneConfiguration();
                    _currentProject.sceneConfiguration = _currentScene;
                }

                // Update project settings from current builder state
                UpdateProjectSettingsFromBuilder();

                string savePath = filePath ?? GetDefaultProjectSavePath();
                bool success = SandboxProjectSerializer.SaveToFile(_currentProject, this, savePath);
                
                if (success)
                {
                    Debug.Log($"[SceneSandboxBuilder] Saved project to: {savePath}");
                    OnSceneSaved?.Invoke(_currentScene);
                }
                
                return success;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneSandboxBuilder] Error saving project: {e.Message}");
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
                    Debug.Log($"[SceneSandboxBuilder] Loaded project from: {filePath}");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneSandboxBuilder] Error loading project: {e.Message}");
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
                    Debug.Log($"[SceneSandboxBuilder] Deleted project: {filePath}");
                    
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
                    Debug.LogWarning($"[SceneSandboxBuilder] Project file not found: {filePath}");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneSandboxBuilder] Error deleting project: {e.Message}");
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

            // Load scene configuration
            if (project.sceneConfiguration != null)
            {
                LoadSceneConfiguration(project.sceneConfiguration);
            }
            else
            {
                // Create default scene if none exists
                _currentScene = new SceneConfiguration($"{project.projectName} Scene");
                _currentProject.sceneConfiguration = _currentScene;
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

            _currentProject.sceneConfiguration = _currentScene;
            UpdateProjectSettingsFromBuilder();
        }

        /// <summary>
        /// Get default save path for projects
        /// </summary>
        private string GetDefaultProjectSavePath()
        {
            string projectName = _currentProject?.projectName ?? _currentSceneName ?? "Untitled";
            return $"{_defaultProjectSavePath}{projectName}.sbproj";
        }

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
                Debug.LogError($"[SceneSandboxBuilder] Failed to create object with factory: {objectDataId}");
                return null;
            }

            // Add draggable component for interaction
            var draggable = newObject.GetComponent<DraggableItem>();
            if (draggable == null)
            {
                draggable = newObject.AddComponent<DraggableItem>();
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
            Debug.Log($"[SceneSandboxBuilder] Placed object using factory: {objectData.displayName} ({factoryData.objectType})");

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

        #endregion

        #region Input Event Handlers

        private void HandleClick(Vector2 screenPosition)
        {
            var hitObject = GetObjectAtScreenPosition(screenPosition);

            // Handle empty space clicks (deselect)
            if (hitObject == null)
            {
                SelectObject(null);
            }
        }

        private void HandleRightClick(Vector2 screenPosition)
        {
            var hitObject = GetObjectAtScreenPosition(screenPosition);
            if (hitObject != null)
            {
                // Show context menu for object
                ShowContextMenu(hitObject, screenPosition);
            }
            else
            {
                // Show general context menu
                ShowGeneralContextMenu(screenPosition);
            }
        }

        private void HandlePointMove(Vector2 screenPosition)
        {
            // Update drop indicator position during drag operations
            if (_isDraggingObject && _enableDropIndicator)
            {
                Vector3 worldPos = GetWorldPositionFromScreen(screenPosition);
                UpdateDropIndicator(worldPos);
            }
            // Show indicator following mouse when a draggable object is selected (but not being dragged)
            else if (_selectedObject != null && !_isDraggingObject && _enableDropIndicator)
            {
                var draggable = _selectedObject.GetComponent<DraggableItem>();
                if (draggable != null && draggable.CanDrag)
                {
                    Vector3 worldPos = GetWorldPositionFromScreen(screenPosition);
                    if (worldPos != Vector3.zero) // Valid world position
                    {
                        UpdateDropIndicator(worldPos);
                    }
                }
            }
        }

        private void HandleDragStart(GameObject obj, Vector2 screenPosition)
        {
            var draggable = obj.GetComponent<DraggableItem>();
            if (draggable != null)
            {
                SelectObject(obj);
                draggable.StartDrag(screenPosition);

                // Start drop indicator system
                _isDraggingObject = true;
                Vector3 worldPos = GetWorldPositionFromScreen(screenPosition);

                ShowDropIndicator(worldPos);
            }
            else
            {

            }
        }

        private void HandleDragMove(GameObject obj, Vector2 screenPosition)
        {
            var draggable = obj.GetComponent<DraggableItem>();
            draggable?.ContinueDrag(screenPosition);

            // Drop indicator updates are now handled by HandlePointMove
        }

        private void HandleDragEnd(GameObject obj, Vector2 screenPosition)
        {
            var draggable = obj.GetComponent<DraggableItem>();
            if (draggable != null)
            {
                draggable.EndDrag(screenPosition);

                // Update scene configuration
                UpdateObjectInScene(draggable);
            }

            // End drop indicator system
            _isDraggingObject = false;
            HideDropIndicator();
        }

        private void HandleObjectSelect(GameObject obj)
        {
            SelectObject(obj);
        }

        private void HandleObjectDelete(GameObject obj)
        {
            RemoveObject(obj);
        }

        #endregion

        #region Helper Methods

        private void BindDraggableEvents(DraggableItem draggable)
        {
            draggable.OnDragStarted += (item, pos) => SelectObject(item.gameObject);
            draggable.OnDragEnded += (item, pos) => UpdateObjectInScene(item);
            draggable.OnItemClicked += (item) => SelectObject(item.gameObject);
            draggable.OnItemSelected += (item) => SelectObject(item.gameObject);
        }

        private void UpdateObjectInScene(DraggableItem draggable)
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
                var draggable = obj.GetComponent<DraggableItem>();
                if (draggable != null)
                {
                    UpdateObjectInScene(draggable);
                }
            }
        }

        private void LoadSceneConfiguration(SceneConfiguration sceneConfig)
        {
            ClearScene();

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

                    var draggable = obj.GetComponent<DraggableItem>();
                    if (draggable == null)
                    {
                        draggable = obj.AddComponent<DraggableItem>();
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

            return Mathf.Abs(localPos.x) <= _sceneBounds.x / 2f &&
                   Mathf.Abs(localPos.y) <= _sceneBounds.y / 2f &&
                   Mathf.Abs(localPos.z) <= _sceneBounds.z / 2f;
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
            return $"{_defaultSavePath}{_currentSceneName}.json";
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
                var draggable = preview.GetComponent<DraggableItem>();
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
            if (_inputHandler != null)
            {
                // Unbind input events
                _inputHandler.OnClick -= HandleClick;
                _inputHandler.OnRightClick -= HandleRightClick;
                _inputHandler.OnDragStart -= HandleDragStart;
                _inputHandler.OnDragMove -= HandleDragMove;
                _inputHandler.OnDragEnd -= HandleDragEnd;
                _inputHandler.OnObjectSelect -= HandleObjectSelect;
                _inputHandler.OnObjectDelete -= HandleObjectDelete;
                _inputHandler.OnPointMove -= HandlePointMove;

                _inputHandler.Cleanup();
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