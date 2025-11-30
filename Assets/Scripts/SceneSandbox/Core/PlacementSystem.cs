using System;
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
        [SerializeField] private SceneSandbox.Data.SceneObjectLibrary _objectLibrary;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private CameraRaycaster _cameraRaycaster;
        [SerializeField] private Transform _stageArea;

        [Header("Settings")]
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private float _rotationSnapDegrees = 15f;
        [SerializeField] private LayerMask _placementLayers = -1;
        [SerializeField] private LayerMask _selectionLayers = -1;
        [SerializeField] private float _maxRaycastDistance = 100f;

        [Header("Events")]
        public UnityEvent<string> OnPlacementStarted = new UnityEvent<string>();
        public UnityEvent<Vector3> OnPlacementUpdated = new UnityEvent<Vector3>();
        public UnityEvent<GameObject> OnPlacementConfirmed = new UnityEvent<GameObject>();
        public UnityEvent OnPlacementCancelled = new UnityEvent();
        public UnityEvent<Vector3> OnDropIndicatorShown = new UnityEvent<Vector3>();
        public UnityEvent<Vector3> OnDropIndicatorUpdated = new UnityEvent<Vector3>();
        public UnityEvent OnDropIndicatorHidden = new UnityEvent();

        // Internal state (kept minimal)
        private string _currentObjectId;
        private GameObject _currentObject;
        private Vector2 _lastScreenPosition;
        private PlacementState _state = PlacementState.Idle;

        public void Initialize(SceneSandbox.Data.SceneObjectLibrary library, GridManager gridManager, CameraRaycaster cameraRaycaster, Transform stageArea,
            bool snapToGrid, float rotationSnapDegrees, LayerMask placementLayers, float maxRaycastDistance, LayerMask selectionLayers)
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
        }

        public void StartPlacement(string objectDataId, Vector2 screenPosition)
        {
            _currentObjectId = objectDataId;
            _lastScreenPosition = screenPosition;
            _state = PlacementState.Active;
            OnPlacementStarted.Invoke(objectDataId);
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

            // Emit events for listeners
            OnPlacementStarted.Invoke(objectDataId);
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
            OnPlacementStarted.Invoke(objectPrefab.name);
        }

        public void UpdatePlacement(Vector2 screenPosition)
        {
            _lastScreenPosition = screenPosition;
            if (_state != PlacementState.Active) return;

            // Calculate world position from screen position
            Vector3 worldPosition = ComputeWorldPositionFromScreen(screenPosition, _maxRaycastDistance, _placementLayers);
            
            // Apply grid snapping if enabled
            worldPosition = ApplyGridSnap(worldPosition);
            
            if (_debugLogs) Debug.Log($"OnPlacementUpdated called with screenPosition: {screenPosition}, worldPosition: {worldPosition}");

            OnPlacementUpdated.Invoke(worldPosition);
        }

        public void ConfirmPlacement(GameObject placedObject)
        {
            _currentObject = null;
            _currentObjectId = null;
            _state = PlacementState.Idle;
            OnPlacementConfirmed.Invoke(placedObject);
            // Hide indicator on confirm
            OnDropIndicatorHidden.Invoke();
        }

        public GameObject CancelPlacement()
        {
            GameObject obj = _currentObject;
            _state = PlacementState.Cancelling;
            OnPlacementCancelled.Invoke();
            OnDropIndicatorHidden.Invoke();
            _currentObjectId = null;
            _currentObject = null;
            _state = PlacementState.Idle;
            return obj;
        }

        public bool IsActive => _state == PlacementState.Active;
        public PlacementState State => _state;

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
        /// Non-breaking: returns the created GameObject, builder handles scene config.
        /// </summary>
        public GameObject PlaceObject(string objectDataId, Vector3 position)
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
            GameObject newObject = GameObject.Instantiate(objectData.prefab, _stageArea);
            newObject.name = objectData.displayName;

            // Ensure TransformableItem component
            var transformable = newObject.GetComponent<TransformableItem>();
            if (transformable == null)
            {
                transformable = newObject.AddComponent<TransformableItem>();
            }

            // Apply grid snapping if enabled
            SyncGridManagerSettingsInternal();
            Vector3 targetPosition = ApplyGridSnap(position);
            newObject.transform.position = targetPosition;

            // Apply scale (preserve prefab scale if defaultScale is zero)
            Vector3 targetScale = objectData.defaultScale == Vector3.zero ? objectData.prefab.transform.localScale : objectData.defaultScale;
            newObject.transform.localScale = targetScale;

            // Assign object data and generate placed ID
            string placedObjectId = System.Guid.NewGuid().ToString();
            transformable.SetObjectData(objectDataId, placedObjectId);

            return newObject;
        }

        private void SyncGridManagerSettingsInternal()
        {
            if (_gridManager == null) return;
            _gridManager.Enabled = _snapToGrid;
            // Note: cell size/offset managed by builder; this keeps snap toggle in sync.
        }

        public Vector3 ComputeWorldPositionFromScreen(Vector2 screenPosition, float maxDistance, LayerMask placementLayers)
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

        public Vector3 ApplyGridSnap(Vector3 position)
        {
            if (_gridManager != null && _snapToGrid)
            {
                return _gridManager.GetSnappedPosition(position);
            }
            return position;
        }

        public Quaternion ApplyRotationSnap(Quaternion rotation)
        {
            if (_gridManager != null && _snapToGrid)
            {
                return _gridManager.GetSnappedRotation(rotation, _rotationSnapDegrees);
            }
            return rotation;
        }

        public bool ValidatePlacementPosition(Vector3 position, Vector3 sizeExtents, Quaternion rotation, LayerMask collisionLayers, LayerMask groundLayers, bool ignoreStatic, bool requireSurfaceBelow, LayerMask placementLayers)
        {
            // Scene bounds validation is deferred to builder for now

            // Collision check using OverlapBox
            Collider[] overlapping = Physics.OverlapBox(position, sizeExtents, rotation, collisionLayers, QueryTriggerInteraction.Ignore);
            foreach (var col in overlapping)
            {
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
                if (!Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, 0.2f, placementLayers))
                {
                    return false;
                }
            }
            return true;
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
                if (_cameraRaycaster.TryRaycast(screenPosition, out RaycastHit hit))
                {
                    resultItem = hit.collider.GetComponentInParent<TransformableItem>();
                }

                // restore
                _cameraRaycaster.RaycastMask = originalMask;
                _cameraRaycaster.MaxDistance = originalMax;
                return resultItem;
            }

            return null;
        }
    }
}
