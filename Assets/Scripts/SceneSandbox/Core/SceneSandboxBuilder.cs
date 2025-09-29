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
        
        [Header("Preview Settings")]
        [SerializeField] private MiniTimelineDirector _timelineDirector;
        [SerializeField] private bool _autoPreview = false;
        [SerializeField] private float _previewDuration = 10f;
        
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
        
        /// <summary>
        /// Set the object library for this sandbox builder
        /// </summary>
        public void SetObjectLibrary(SceneObjectLibrary library)
        {
            _objectLibrary = library;
        }
        
        private void Awake()
        {
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
                
                _inputHandler.Enable();
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
            newObject.transform.localScale = objectData.defaultScale;
            
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
            
            _currentScene.AddPlacedObject(placedObjectData);
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
            var draggable = obj.GetComponent<DraggableItem>();
            if (draggable == null) return false;
            
            string objectId = draggable.ObjectId;
            
            // Remove from scene configuration
            if (_currentScene.RemovePlacedObject(objectId))
            {
                _placedObjects.Remove(objectId);
                
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
            var objectsToRemove = new List<GameObject>(_placedObjects.Values);
            foreach (var obj in objectsToRemove)
            {
                RemoveObject(obj);
            }
            
            _placedObjects.Clear();
            _currentScene.ClearPlacedObjects();
            
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
        
        #endregion
        
        #region Input Event Handlers
        
        private void HandleClick(Vector2 screenPosition)
        {
            // Handle empty space clicks (deselect)
            if (GetObjectAtScreenPosition(screenPosition) == null)
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
        
        private void HandleDragStart(GameObject obj, Vector2 screenPosition)
        {
            var draggable = obj.GetComponent<DraggableItem>();
            if (draggable != null)
            {
                SelectObject(obj);
                draggable.StartDrag(screenPosition);
            }
        }
        
        private void HandleDragMove(GameObject obj, Vector2 screenPosition)
        {
            var draggable = obj.GetComponent<DraggableItem>();
            draggable?.ContinueDrag(screenPosition);
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
            Vector3 rayStart = position + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, _placementLayers))
            {
                return hit.point;
            }
            
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
        
        private string GetDefaultSavePath()
        {
            return $"{_defaultSavePath}{_currentSceneName}.json";
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
                _inputHandler.Cleanup();
            }
            
            StopPreview();
        }
    }
}