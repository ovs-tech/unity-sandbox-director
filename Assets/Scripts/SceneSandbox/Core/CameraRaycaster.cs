using UnityEngine;

namespace Systems.SceneSandbox.Core
{
    /// Minimal, non-breaking camera raycaster component.
    /// Provides safe raycasts from a camera to the scene with layer filtering.
    public class CameraRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _raycastMask = ~0; // default: everything
        [SerializeField] private float _maxDistance = 1000f;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        public Camera TargetCamera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = Camera.main;
                }
                return _camera;
            }
            set => _camera = value;
        }

        public LayerMask RaycastMask
        {
            get => _raycastMask;
            set => _raycastMask = value;
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

        public bool TryRaycast(Vector2 screenPoint, out RaycastHit hit)
        {
            var cam = TargetCamera;
            if (cam == null)
            {
                hit = default;
                return false;
            }

            var ray = cam.ScreenPointToRay(screenPoint);
            return Physics.Raycast(ray, out hit, _maxDistance, _raycastMask, QueryTriggerInteraction.Collide);
        }

        public bool TryRaycastFromCenter(out RaycastHit hit)
        {
            var cam = TargetCamera;
            if (cam == null)
            {
                hit = default;
                return false;
            }

            var center = new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f);
            return TryRaycast(center, out hit);
        }
    }
}
