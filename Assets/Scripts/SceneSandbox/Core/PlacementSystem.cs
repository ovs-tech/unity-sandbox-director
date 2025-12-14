using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Minimal PlacementSystem scaffold (Phase 2).
    /// Non-breaking: provides APIs and events, does not alter existing builder behavior yet.
    /// </summary>
    public class PlacementSystem : MonoBehaviour
    {
        [Header("Debugging")]
        [SerializeField] private bool _debugLogs = false;

        [Header("Dependencies")]
        [SerializeField] private Data.SceneObjectLibrary _objectLibrary;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private CameraRaycaster _cameraRaycaster;
        [SerializeField] private Transform _stageArea;

        [Header("Settings")]
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private float _rotationSnapDegrees = 15f;
        [SerializeField] private LayerMask _placementLayers = -1;
        [SerializeField] private LayerMask _selectionLayers = -1;
        [SerializeField] private float _maxRaycastDistance = 100f;

        [Header("Validation Settings")]
        [SerializeField] private bool _checkCollisions = true;
        [SerializeField] private LayerMask _collisionLayers = -1;
        [SerializeField] private LayerMask _groundLayers = 0;
        [SerializeField] private bool _ignoreStaticObjects = true;
        [SerializeField] private bool _requireSurfaceBelow = false;

        [Header("Events")]
        public UnityEvent<string, GameObject> OnPlacementStarted = new UnityEvent<string, GameObject>();
        public UnityEvent<Vector3> OnPlacementUpdated = new UnityEvent<Vector3>();
        public UnityEvent<string, GameObject> OnPlacementConfirmed = new UnityEvent<string, GameObject>();
        public UnityEvent<GameObject, bool> OnPlacementCancelled = new UnityEvent<GameObject, bool>(); // GameObject, isNewlyCreated
        public UnityEvent<Vector3> OnDropIndicatorShown = new UnityEvent<Vector3>();
        public UnityEvent<Vector3> OnDropIndicatorUpdated = new UnityEvent<Vector3>();
        public UnityEvent OnDropIndicatorHidden = new UnityEvent();

        // Internal state (kept minimal)
        // Internal state (kept minimal)
        private string _currentObjectId;
        private GameObject _currentObject;
        private Vector3 _currentObjectLastPosition; // Track position when placement started
        private bool _currentObjectIsNewlyCreated; // Track if object was newly created during placement
        private Vector2 _lastScreenPosition;
        private PlacementState _state = PlacementState.Idle;
        private bool _isValidPlacementCurrentPosition = true;
        
        // Placed objects tracking
        private Dictionary<string, GameObject> _placedObjects = new Dictionary<string, GameObject>();

        public void Initialize(Data.SceneObjectLibrary library, GridManager gridManager, CameraRaycaster cameraRaycaster, Transform stageArea,
            bool snapToGrid, float rotationSnapDegrees, LayerMask placementLayers, float maxRaycastDistance, LayerMask selectionLayers,
            bool checkCollisions = true, LayerMask collisionLayers = default, LayerMask groundLayers = default, bool ignoreStaticObjects = true, bool requireSurfaceBelow = false)
        {
            _objectLibrary = library;
            _gridManager = gridManager;
            _cameraRaycaster = cameraRaycaster;
            _stageArea = stageArea;
            _snapToGrid = snapToGrid;
            _rotationSnapDegrees = rotationSnapDegrees;
            _placementLayers = placementLayers;
            _maxRaycastDistance = maxRaycastDistance;
            _selectionLayers = selectionLayers;
            
            // Validation settings
            _checkCollisions = checkCollisions;
            _collisionLayers = collisionLayers;
            _groundLayers = groundLayers;
            _ignoreStaticObjects = ignoreStaticObjects;
            _requireSurfaceBelow = requireSurfaceBelow;
        }

        public void StartPlacement(string objectDataId, Vector2 screenPosition)
        {
            _currentObjectId = objectDataId;
            _lastScreenPosition = screenPosition;
            _state = PlacementState.Active;
            _currentObjectIsNewlyCreated = false; // Not creating object here
            OnPlacementStarted.Invoke(objectDataId, _currentObject);
        }

        /// <summary>
        /// Start placement using the currently stored objectId (_currentObjectId).
        /// If _currentObjectId is null, automatically fetches the first item via SwitchNextPlacement.
        /// </summary>
        public void StartPlacement(Vector2 screenPosition)
        {
            if (string.IsNullOrEmpty(_currentObjectId))
            {
                string firstObjectId = SwitchNextPlacement();

                if (string.IsNullOrEmpty(firstObjectId))
                {
                    return;
                }
            }

            StartPlacementAndCreate(_currentObjectId, screenPosition);
        }

        /// <summary>
        /// Start placement and create the actual object at computed world position.
        /// Returns the created GameObject or null on failure. Scene config is handled by caller.
        /// </summary>
        public GameObject StartPlacementAndCreate(string objectDataId, Vector2 screenPosition)
        {
            _currentObjectId = objectDataId;
            _lastScreenPosition = screenPosition;
            _state = PlacementState.Active;

            // Compute world position from screen
            Vector3 worldPos = ComputeWorldPositionFromScreen(screenPosition, _maxRaycastDistance, _placementLayers);
            worldPos = ApplyGridSnap(worldPos);

            // Instantiate via system PlaceObject (no scene config changes here)
            GameObject created = PlaceObject(objectDataId, worldPos);
            _currentObject = created;
            _currentObjectIsNewlyCreated = true; // Mark as newly created
            if (created != null)
            {
                _currentObjectLastPosition = created.transform.position; // Store creation position
            }

            // Emit events for listeners
            OnPlacementStarted.Invoke(objectDataId, _currentObject);
            if (created != null)
            {
                OnPlacementUpdated.Invoke(worldPos);
            }

            return created;
        }

        public void StartPlacement(GameObject objectPrefab, Vector2? screenPosition = null)
        {
            _currentObject = objectPrefab;
            _lastScreenPosition = screenPosition ?? Vector2.zero;
            _state = PlacementState.Active;
            _currentObjectIsNewlyCreated = false; // Existing object being manipulated
            if (_currentObject != null)
            {
                _currentObjectLastPosition = _currentObject.transform.position; // Store current position
            }
            OnPlacementStarted.Invoke(null, _currentObject);
        }

        public void UpdatePlacement(Vector2 screenPosition)
        {
            _lastScreenPosition = screenPosition;
            if (_state != PlacementState.Active) return;

            // Calculate world position from screen position
            Vector3 worldPosition = ComputeWorldPositionFromScreen(screenPosition, _maxRaycastDistance, _placementLayers);

            // Apply grid snapping if enabled
            worldPosition = ApplyGridSnap(worldPosition);

            // Validate placement position
            if (_currentObject != null && _checkCollisions)
            {
                Bounds bounds = GetObjectBounds(_currentObject);
                Vector3 sizeExtents = bounds.extents;
                Quaternion rotation = _currentObject.transform.rotation;
                
                bool isValid = ValidatePlacementPosition(
                    worldPosition,
                    sizeExtents,
                    rotation,
                    _collisionLayers,
                    _groundLayers,
                    _ignoreStaticObjects,
                    _requireSurfaceBelow,
                    _placementLayers
                );
                
                SetPlacementPositionValidity(isValid);
            }

            if (_debugLogs) Debug.Log($"OnPlacementUpdated called with screenPosition: {screenPosition}, worldPosition: {worldPosition}");

            OnPlacementUpdated.Invoke(worldPosition);
        }

        /// <summary>
        /// Set whether the current placement position is valid
        /// </summary>
        private void SetPlacementPositionValidity(bool isValid)
        {
            _isValidPlacementCurrentPosition = isValid;
        }

        public void ConfirmPlacement()
        {
            if(_state != PlacementState.Active) return;

            if(_isValidPlacementCurrentPosition == false)
            {
                Debug.LogWarning("[PlacementSystem] Cannot confirm placement: current position is invalid.");
                return;
            }

            OnPlacementConfirmed.Invoke(_currentObjectId, _currentObject);
            OnDropIndicatorHidden.Invoke();
            
            _currentObject = null;
            _currentObjectId = null;
            _state = PlacementState.Idle;
        }

        public GameObject CancelPlacement()
        {
            GameObject obj = _currentObject;
            
            if (obj != null)
            {
                if (_currentObjectIsNewlyCreated)
                {
                    // If newly created during this placement session, destroy it
                    var transformable = obj.GetComponent<TransformableItem>();
                    if (transformable != null)
                    {
                        string placedId = transformable.ObjectId;
                        if (!string.IsNullOrEmpty(placedId) && _placedObjects.ContainsKey(placedId))
                        {
                            _placedObjects.Remove(placedId);
                        }
                    }
                    
                    Destroy(obj);
                }
                else
                {
                    // If existing object, just reset its position to where placement started
                    obj.transform.position = _currentObjectLastPosition;
                }
            }
            
            _state = PlacementState.Cancelling;
            OnPlacementCancelled.Invoke(obj, _currentObjectIsNewlyCreated);
            OnDropIndicatorHidden.Invoke();
            _currentObjectId = null;
            _currentObject = null;
            _currentObjectIsNewlyCreated = false;
            _state = PlacementState.Idle;
            _isValidPlacementCurrentPosition = true;
            return obj;
        }

        public bool IsActive => _state == PlacementState.Active;
        public PlacementState State => _state;
        public string CurrentObjectId => _currentObjectId;
        public GameObject CurrentObject => _currentObject;
        public bool IsValidPlacementCurrentPosition => _isValidPlacementCurrentPosition;
        
        /// <summary>
        /// Get a read-only view of all currently placed objects
        /// </summary>
        public IReadOnlyDictionary<string, GameObject> PlacedObjects => _placedObjects;

        /// <summary>
        /// Switch to the next object in the library and store it in _currentObjectId.
        /// Returns the next object ID or null if library is empty.
        /// </summary>
        public string SwitchNextPlacement()
        {
            if (_objectLibrary == null)
            {
                Debug.LogError("[PlacementSystem] ObjectLibrary is not set.");
                return null;
            }

            var allObjects = _objectLibrary.GetAllObjects();
            if (allObjects == null || allObjects.Count == 0)
            {
                Debug.LogWarning("[PlacementSystem] ObjectLibrary is empty.");
                return null;
            }

            // Find current index
            int currentIndex = -1;
            if (!string.IsNullOrEmpty(_currentObjectId))
            {
                for (int i = 0; i < allObjects.Count; i++)
                {
                    if (allObjects[i].id == _currentObjectId)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            // Get next index (wrap around)
            int nextIndex = (currentIndex + 1) % allObjects.Count;
            _currentObjectId = allObjects[nextIndex].id;

            if (_debugLogs)
            {
                Debug.Log($"[PlacementSystem] Switched to next object: {_currentObjectId} ({allObjects[nextIndex].displayName})");
            }

            return _currentObjectId;
        }

        // Drop indicator hooks (non-breaking): only emit events, no visuals created here
        public void ShowDropIndicator(Vector3 worldPosition)
        {
            OnDropIndicatorShown.Invoke(worldPosition);
        }

        public void UpdateDropIndicator(Vector3 worldPosition)
        {
            OnDropIndicatorUpdated.Invoke(worldPosition);
        }

        public void HideDropIndicator()
        {
            OnDropIndicatorHidden.Invoke();
        }

        // --- Phase 2: Minimal helpers (non-breaking, internal use) ---

        /// <summary>
        /// Instantiate and place an object from the library under the stage area.
        /// Supports optional scale and rotation overrides.
        /// Non-breaking: returns the created GameObject, builder handles scene config.
        /// </summary>
        public GameObject PlaceObject(string objectDataId, Vector3 worldPos, Vector3? overrideScale = null, Vector3? overrideRulerAngles = null)
        {
            if (_objectLibrary == null || _stageArea == null)
            {
                Debug.LogError("[PlacementSystem] ObjectLibrary or StageArea is not set.");
                return null;
            }

            var objectData = _objectLibrary.GetObjectById(objectDataId);
            if (objectData == null || objectData.prefab == null)
            {
                Debug.LogError($"[PlacementSystem] Invalid object data for ID: {objectDataId}");
                return null;
            }

            // Instantiate under stage
            GameObject newObject = Instantiate(objectData.prefab, _stageArea);
            newObject.name = objectData.displayName;

            // Ensure TransformableItem component
            var transformable = newObject.GetComponent<TransformableItem>();
            if (transformable == null)
            {
                transformable = newObject.AddComponent<TransformableItem>();
            }

            // Apply grid snapping if enabled
            SyncGridManagerSettingsInternal();
            Vector3 targetPosition = ApplyGridSnap(worldPos);
            newObject.transform.position = targetPosition;

            // Apply rotation (override or use prefab rotation)
            if (overrideRulerAngles.HasValue)
            {
                newObject.transform.eulerAngles = overrideRulerAngles.Value;
            }
            else
            {
                newObject.transform.eulerAngles = objectData.prefab.transform.eulerAngles;
            }

            // Apply scale (override, or preserve prefab scale if defaultScale is zero)
            Vector3 targetScale;
            if (overrideScale.HasValue)
            {
                targetScale = overrideScale.Value;
            }
            else if (objectData.defaultScale != Vector3.zero)
            {
                targetScale = objectData.defaultScale;
            }
            else
            {
                targetScale = objectData.prefab.transform.localScale;
            }
            newObject.transform.localScale = targetScale;

            // Assign object data and generate placed ID
            string placedObjectId = System.Guid.NewGuid().ToString();
            transformable.SetObjectData(objectDataId, placedObjectId);
            
            // Track the placed object
            _placedObjects[placedObjectId] = newObject;

            return newObject;
        }

        /// <summary>
        /// Place an object using PlacedObjectData
        /// </summary>
        public GameObject PlaceObject(Data.PlacedObjectData placedObjectData)
        {
            if (_objectLibrary == null || _stageArea == null)
            {
                Debug.LogError("[PlacementSystem] ObjectLibrary or StageArea is not set.");
                return null;
            }

            var objectData = _objectLibrary.GetObjectById(placedObjectData.objectDataId);
            if (objectData == null || objectData.prefab == null)
            {
                Debug.LogError($"[PlacementSystem] Invalid object data for ID: {placedObjectData.objectDataId}");
                return null;
            }

            // Instantiate under stage
            GameObject newObject = Instantiate(objectData.prefab, _stageArea);
            newObject.name = placedObjectData.customName ?? objectData.displayName;

            // Ensure TransformableItem component
            var transformable = newObject.GetComponent<TransformableItem>();
            if (transformable == null)
            {
                transformable = newObject.AddComponent<TransformableItem>();
            }

            // Apply transforms from PlacedObjectData
            newObject.transform.position = placedObjectData.position;
            newObject.transform.eulerAngles = placedObjectData.rotation;
            newObject.transform.localScale = placedObjectData.scale;

            // Assign object data using the placed object ID
            transformable.SetObjectData(placedObjectData.objectDataId, placedObjectData.id);

            // Track the placed object
            _placedObjects[placedObjectData.id] = newObject;

            return newObject;
        }

        private void SyncGridManagerSettingsInternal()
        {
            if (_gridManager == null) return;
            _gridManager.Enabled = _snapToGrid;
            // Note: cell size/offset managed by builder; this keeps snap toggle in sync.
        }

        private Vector3 ComputeWorldPositionFromScreen(Vector2 screenPosition, float maxDistance, LayerMask placementLayers)
        {
            if (_cameraRaycaster != null)
            {
                var originalMask = _cameraRaycaster.RaycastMask;
                var originalMax = _cameraRaycaster.MaxDistance;
                _cameraRaycaster.RaycastMask = placementLayers;
                _cameraRaycaster.MaxDistance = maxDistance;
                if (_cameraRaycaster.TryRaycast(screenPosition, out RaycastHit hit))
                {
                    _cameraRaycaster.RaycastMask = originalMask;
                    _cameraRaycaster.MaxDistance = originalMax;
                    return hit.point;
                }
                _cameraRaycaster.RaycastMask = originalMask;
                _cameraRaycaster.MaxDistance = originalMax;
            }

            return Vector3.zero;
        }

        private Vector3 ApplyGridSnap(Vector3 position)
        {
            if (_gridManager != null && _snapToGrid)
            {
                return _gridManager.GetSnappedPosition(position);
            }
            return position;
        }

        private bool ValidatePlacementPosition(Vector3 position, Vector3 sizeExtents, Quaternion rotation, LayerMask collisionLayers, LayerMask groundLayers, bool ignoreStatic, bool requireSurfaceBelow, LayerMask placementLayers)
        {
            // Scene bounds validation is deferred to builder for now

            // Collision check using OverlapBox
            Collider[] overlapping = Physics.OverlapBox(position, sizeExtents, rotation, collisionLayers, QueryTriggerInteraction.Ignore);
            
            foreach (var col in overlapping)
            {
                // Ignore collisions with self (the current object being placed)
                if (_currentObject != null && col.transform.IsChildOf(_currentObject.transform))
                {
                    continue;
                }
                
                if (_currentObject != null && col.gameObject == _currentObject)
                {
                    continue;
                }
                
                int objLayer = col.gameObject.layer;
                
                if ((groundLayers.value & (1 << objLayer)) != 0)
                {
                    continue;
                }
                
                if (ignoreStatic && col.gameObject.isStatic)
                {
                    continue;
                }
                
                return false;
            }

            // Optional surface requirement
            if (requireSurfaceBelow)
            {
                bool surfaceFound = Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, 0.2f, placementLayers);
                
                if (!surfaceFound)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get the bounds of a game object including all renderers
        /// </summary>
        private Bounds GetObjectBounds(GameObject obj)
        {
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            var renderers = obj.GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        /// <summary>
        /// Perform raycast to detect TransformableItem
        /// </summary>
        public TransformableItem RaycastForItem(Vector2 screenPosition)
        {
            // Use selection layers when not in placement mode, otherwise use placement layers
            LayerMask maskToUse = IsActive ? _placementLayers : _selectionLayers;

            if (_cameraRaycaster != null)
            {
                // Temporarily override mask and distance
                var originalMask = _cameraRaycaster.RaycastMask;
                var originalMax = _cameraRaycaster.MaxDistance;
                _cameraRaycaster.RaycastMask = maskToUse;
                _cameraRaycaster.MaxDistance = _maxRaycastDistance;
                TransformableItem resultItem = null;
                if (_debugLogs)
                {
                    Debug.Log($"[PlacementSystem] RaycastForItem - Screen: {screenPosition}, Mask: {maskToUse.value}, MaxDist: {_maxRaycastDistance}, Using {(IsActive ? "placement" : "selection")} layers");
                }
                if (_cameraRaycaster.TryRaycast(screenPosition, out RaycastHit hit))
                {
                    if (_debugLogs)
                    {
                        Debug.Log($"[PlacementSystem] RaycastForItem - Hit: {hit.collider.gameObject.name} at {hit.point} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                    }
                    resultItem = hit.collider.GetComponentInParent<TransformableItem>();
                    if (_debugLogs)
                    {
                        Debug.Log($"[PlacementSystem] RaycastForItem - Result item: {(resultItem != null ? resultItem.gameObject.name : "null")}");
                    }
                }
                else if (_debugLogs)
                {
                    Debug.Log("[PlacementSystem] RaycastForItem - No hit");
                }

                // restore
                _cameraRaycaster.RaycastMask = originalMask;
                _cameraRaycaster.MaxDistance = originalMax;
                return resultItem;
            }

            if (_debugLogs)
            {
                Debug.LogWarning("[PlacementSystem] RaycastForItem - CameraRaycaster is null");
            }

            return null;
        }

        /// <summary>
        /// Remove an object from the placed objects tracking
        /// </summary>
        public bool RemovePlacedObject(string placedObjectId)
        {
            if (_placedObjects.ContainsKey(placedObjectId))
            {
                _placedObjects.Remove(placedObjectId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove an object from the placed objects tracking by GameObject
        /// </summary>
        public bool RemovePlacedObject(GameObject obj)
        {
            if (obj == null) return false;

            var transformable = obj.GetComponent<TransformableItem>();
            if (transformable != null)
            {
                string placedId = transformable.ObjectId;
                return RemovePlacedObject(placedId);
            }

            // Fallback: search by value
            var kvp = _placedObjects.FirstOrDefault(x => x.Value == obj);
            if (kvp.Key != null)
            {
                _placedObjects.Remove(kvp.Key);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get a placed object by its ID
        /// </summary>
        public GameObject GetPlacedObject(string placedObjectId)
        {
            if (_placedObjects.TryGetValue(placedObjectId, out GameObject obj))
            {
                return obj;
            }
            return null;
        }

        /// <summary>
        /// Clear all tracked placed objects (optionally destroying them)
        /// </summary>
        public void ClearPlacedObjects(bool destroyGameObjects = true)
        {
            if (destroyGameObjects)
            {
                foreach (var obj in _placedObjects.Values)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }
            _placedObjects.Clear();
        }

        /// <summary>
        /// Get the count of all placed objects
        /// </summary>
        public int GetPlacedObjectCount()
        {
            return _placedObjects.Count;
        }
    }
}
