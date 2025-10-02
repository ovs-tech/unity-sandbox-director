using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SceneSandbox.Data;
using SceneSandbox.Input;
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
        [SerializeField] private bool _useRaycastForPlacement = true;
        
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
        
        // Components
        private SandboxInputHandler _inputHandler;
        private Camera _sceneCamera;
        
        // State
        private SceneConfiguration _currentScene;
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
            
            Debug.Log("Attempting to auto-assign InputActions asset...");
            
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
                    Debug.Log($"Auto-assigned InputActions asset from: {path}");
                    return;
                }
            }
            
            Debug.LogWarning("Could not auto-assign InputActions asset. Please assign it manually in the Inspector.");
        }
        
        private void Awake()
        {
            Debug.Log("SceneSandboxBuilder Awake starting...");
            
            // Auto-assign InputActions if not set
            if (_inputActions == null)
            {
                AutoAssignInputActions();
            }
            
            InitializeComponents();
            InitializeState();
            Debug.Log("SceneSandboxBuilder Awake complete.");
        }
        
        private void Start()
        {
            Debug.Log("SceneSandboxBuilder Start beginning...");
            SetupInputHandler();
            CreateNewScene();
            
            // Test drop indicator system at startup
            Debug.Log("=== Drop Indicator Startup Test ===");
            Debug.Log($"Drop indicator enabled: {_enableDropIndicator}");
            Debug.Log($"Drop indicator size: {_dropIndicatorSize}");
            Debug.Log($"Valid drop color: {_validDropColor}");
            Debug.Log($"Invalid drop color: {_invalidDropColor}");
            Debug.Log($"Scene camera: {(_sceneCamera != null ? _sceneCamera.name : "null")}");
            
            Debug.Log("SceneSandboxBuilder Start complete.");
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
            Debug.Log("Setting up input handler...");
            
            if (_inputHandler != null && _inputActions != null)
            {
                Debug.Log($"Input handler exists: {_inputHandler != null}, Input actions exists: {_inputActions != null}");
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
                
                Debug.Log("All input events bound successfully");
                
                _inputHandler.Enable();
                
                Debug.Log("Input handler setup complete. Events bound and enabled.");
            }
            else
            {
                Debug.LogWarning($"Input handler setup failed. InputHandler: {_inputHandler != null}, InputActions: {_inputActions != null}");
                
                if (_inputHandler == null)
                    Debug.LogError("Input handler is null!");
                if (_inputActions == null)
                    Debug.LogError("Input actions asset is null!");
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
            var placedObjectData = new PlacedObjectData(objectDataId, finalPosition)
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
                Debug.Log($"Scene saved to: {savePath}");
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save scene: {e.Message}");
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
                    Debug.LogError($"Scene file not found: {filePath}");
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
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene: {e.Message}");
            }
            
            return false;
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
            Debug.Log($"HandleClick called at screen position: {screenPosition}");
            
            var hitObject = GetObjectAtScreenPosition(screenPosition);
            Debug.Log($"HandleClick hit object: {(hitObject != null ? hitObject.name : "null")}");
            
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
                Debug.Log($"HandlePointMove updating drop indicator - screen: {screenPosition}, dragging: {_isDraggingObject}, enabled: {_enableDropIndicator}");
                Vector3 worldPos = GetWorldPositionFromScreen(screenPosition);
                Debug.Log($"HandlePointMove world position: {worldPos}");
                UpdateDropIndicator(worldPos);
            }
        }
        
        private void HandleDragStart(GameObject obj, Vector2 screenPosition)
        {
            Debug.Log($"HandleDragStart called for object: {obj?.name} at screen position: {screenPosition}");
            
            var draggable = obj.GetComponent<DraggableItem>();
            if (draggable != null)
            {
                SelectObject(obj);
                draggable.StartDrag(screenPosition);
                
                // Start drop indicator system
                _isDraggingObject = true;
                Debug.Log($"Starting drop indicator system - _isDraggingObject: {_isDraggingObject}, _enableDropIndicator: {_enableDropIndicator}");
                
                Vector3 worldPos = GetWorldPositionFromScreen(screenPosition);
                Debug.Log($"World position from screen: {worldPos}");
                
                ShowDropIndicator(worldPos);
            }
            else
            {
                Debug.LogWarning($"No DraggableItem component found on {obj?.name}");
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
                return new Vector3(position.x, _defaultPlacementHeight, position.z);
            }
            
            // Try to find surface through raycast
            Vector3 rayStart = position + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, _placementLayers))
            {
                // Found surface, but check if we should use default height instead
                float surfaceHeight = hit.point.y;
                
                // Use the higher of the two: surface height or default placement height
                float finalHeight = Mathf.Max(surfaceHeight, _defaultPlacementHeight);
                
                return new Vector3(position.x, finalHeight, position.z);
            }
            
            // No surface found, use default placement height
            return new Vector3(position.x, _defaultPlacementHeight, position.z);
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
            if (_sceneCamera == null) return Vector3.zero;
            
            Ray ray = _sceneCamera.ScreenPointToRay(screenPosition);
            Vector3 worldPos;
            
            // If raycast placement is disabled, always project to default height
            if (!_useRaycastForPlacement)
            {
                Vector3 rayDirection = ray.direction.normalized;
                float t = (_defaultPlacementHeight - ray.origin.y) / rayDirection.y;
                worldPos = ray.origin + rayDirection * t;
                return worldPos;
            }
            
            // Try to hit the ground or existing objects
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _placementLayers))
            {
                // Use the higher of surface height or default placement height
                float finalHeight = Mathf.Max(hit.point.y, _defaultPlacementHeight);
                worldPos = new Vector3(hit.point.x, finalHeight, hit.point.z);
            }
            else
            {
                // Project ray onto the default placement height
                Vector3 rayDirection = ray.direction.normalized;
                float t = (_defaultPlacementHeight - ray.origin.y) / rayDirection.y;
                worldPos = ray.origin + rayDirection * t;
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
            Debug.Log($"ShowDropIndicator called with position: {position}, enabled: {_enableDropIndicator}");
            
            if (!_enableDropIndicator) 
            {
                Debug.Log("Drop indicator is disabled, returning");
                return;
            }
            
            // Create drop indicator if it doesn't exist
            if (_currentDropIndicator == null)
            {
                Debug.Log("Drop indicator doesn't exist, creating new one...");
                CreateDropIndicator();
            }
            
            if (_currentDropIndicator != null)
            {
                Debug.Log($"Activating drop indicator: {_currentDropIndicator.name}");
                _currentDropIndicator.SetActive(true);
                
                Debug.Log($"Drop indicator active state: {_currentDropIndicator.activeInHierarchy}");
                Debug.Log($"Drop indicator world position before update: {_currentDropIndicator.transform.position}");
                
                UpdateDropIndicator(position);
                
                Debug.Log($"Drop indicator world position after update: {_currentDropIndicator.transform.position}");
            }
            else
            {
                Debug.LogError("Failed to create or access drop indicator!");
            }
        }
        
        /// <summary>
        /// Update drop indicator position and appearance
        /// </summary>
        private void UpdateDropIndicator(Vector3 position)
        {
            if (!_enableDropIndicator) 
            {
                Debug.Log($"UpdateDropIndicator early return - drop indicator disabled");
                return;
            }
            
            // Create drop indicator if it doesn't exist
            if (_currentDropIndicator == null) 
            {
                Debug.Log("UpdateDropIndicator: Drop indicator is null, creating new one...");
                CreateDropIndicator();
                
                if (_currentDropIndicator != null)
                {
                    Debug.Log("UpdateDropIndicator: Successfully created drop indicator");
                    _currentDropIndicator.SetActive(true);
                }
                else
                {
                    Debug.LogError("UpdateDropIndicator: Failed to create drop indicator!");
                    return;
                }
            }
            
            // Ensure indicator is active
            if (!_currentDropIndicator.activeInHierarchy)
            {
                Debug.Log("UpdateDropIndicator: Activating drop indicator");
                _currentDropIndicator.SetActive(true);
            }
            
            // Snap to grid if enabled
            Vector3 finalPosition = _snapToGrid ? SnapToGrid(position) : position;
            finalPosition = GetSurfacePosition(finalPosition);
            
            Debug.Log($"UpdateDropIndicator - input: {position}, final: {finalPosition}, snap: {_snapToGrid}");
            
            _dragPreviewPosition = finalPosition;
            _currentDropIndicator.transform.position = finalPosition;
            
            // Update color based on validity
            bool isValidPosition = IsPositionInSceneBounds(finalPosition);
            Color indicatorColor = isValidPosition ? _validDropColor : _invalidDropColor;
            
            Debug.Log($"UpdateDropIndicator - valid position: {isValidPosition}, color: {indicatorColor}");
            
            // Apply color to the indicator
            var renderer = _currentDropIndicator.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = indicatorColor;
                Debug.Log($"Applied color to drop indicator material: {indicatorColor}");
            }
            else
            {
                Debug.LogWarning("Drop indicator renderer or material is null!");
            }
        }
        
        /// <summary>
        /// Hide drop indicator
        /// </summary>
        private void HideDropIndicator()
        {
            Debug.Log($"HideDropIndicator called - indicator exists: {_currentDropIndicator != null}");
            
            if (_currentDropIndicator != null)
            {
                Debug.Log($"Hiding drop indicator: {_currentDropIndicator.name}");
                _currentDropIndicator.SetActive(false);
                Debug.Log($"Drop indicator hidden - active state: {_currentDropIndicator.activeInHierarchy}");
            }
        }
        
        /// <summary>
        /// Create drop indicator GameObject
        /// </summary>
        private void CreateDropIndicator()
        {
            Debug.Log("CreateDropIndicator called");
            
            if (_dropIndicatorPrefab != null)
            {
                Debug.Log($"Creating drop indicator from prefab: {_dropIndicatorPrefab.name}");
                _currentDropIndicator = Instantiate(_dropIndicatorPrefab);
                _currentDropIndicator.name = "Drop Indicator (Prefab)";
            }
            else
            {
                Debug.Log("Creating default drop indicator (Cylinder)");
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
                    // Create a new material to ensure visibility
                    Material indicatorMaterial = new Material(Shader.Find("Standard"));
                    indicatorMaterial.color = _validDropColor;
                    indicatorMaterial.SetFloat("_Metallic", 0f);
                    indicatorMaterial.SetFloat("_Glossiness", 0.5f);
                    renderer.material = indicatorMaterial;
                    
                    Debug.Log($"Drop indicator material created with color: {_validDropColor}");
                }
            }
            
            if (_currentDropIndicator != null)
            {
                Debug.Log($"Drop indicator created successfully: {_currentDropIndicator.name} at position {_currentDropIndicator.transform.position}");
                
                // Initially hide the indicator
                _currentDropIndicator.SetActive(false);
                Debug.Log("Drop indicator initially hidden");
            }
            else
            {
                Debug.LogError("Failed to create drop indicator!");
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
            Vector3 floorPos = new Vector3(stagePos.x, stagePos.y - stageScale.y/2f, stagePos.z);
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
            Debug.Log($"Context menu for {obj.name}");
        }
        
        private void ShowGeneralContextMenu(Vector2 screenPosition)
        {
            // TODO: Implement general context menu
            Debug.Log("General context menu");
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