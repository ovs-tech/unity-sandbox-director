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
        public enum PlacementState
        {
            Idle,
            Active,
            Confirming,
            Cancelling
        }
        [Header("Dependencies")]
        [SerializeField] private SceneSandbox.Data.SceneObjectLibrary _objectLibrary;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private CameraRaycaster _cameraRaycaster;
        [SerializeField] private Transform _stageArea;

        [Header("Settings")]
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private float _rotationSnapDegrees = 15f;

        [Header("Events")]
        public UnityEvent<string> OnPlacementStarted = new UnityEvent<string>();
        public UnityEvent<Vector2> OnPlacementUpdated = new UnityEvent<Vector2>();
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
            bool snapToGrid, float rotationSnapDegrees)
        {
            _objectLibrary = library;
            _gridManager = gridManager;
            _cameraRaycaster = cameraRaycaster;
            _stageArea = stageArea;
            _snapToGrid = snapToGrid;
            _rotationSnapDegrees = rotationSnapDegrees;
        }

        public void StartPlacement(string objectDataId, Vector2 screenPosition)
        {
            _currentObjectId = objectDataId;
            _lastScreenPosition = screenPosition;
            _state = PlacementState.Active;
            OnPlacementStarted.Invoke(objectDataId);
        }

        public void UpdatePlacement(Vector2 screenPosition)
        {
            _lastScreenPosition = screenPosition;
            if (_state != PlacementState.Active) return;
            OnPlacementUpdated.Invoke(screenPosition);
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

        public void CancelPlacement()
        {
            _currentObject = null;
            _currentObjectId = null;
            _state = PlacementState.Idle;
            OnPlacementCancelled.Invoke();
            OnDropIndicatorHidden.Invoke();
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

        public Vector3 ComputeWorldPositionFromScreen(Vector2 screenPosition, float maxDistance, LayerMask placementLayers, Camera sceneCamera)
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

            // Fallback using provided camera
            if (sceneCamera != null)
            {
                Ray ray = sceneCamera.ScreenPointToRay(screenPosition);
                if (Physics.Raycast(ray, out RaycastHit hitFallback, maxDistance, placementLayers))
                {
                    return hitFallback.point;
                }
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
    }
}
