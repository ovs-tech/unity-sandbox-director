using UnityEngine;
using UnityEngine.EventSystems;
using SceneSandbox.Core;

namespace SceneSandbox.Input
{
    /// <summary>
    /// Handles background clicks to clear selection when clicking on empty space
    /// Attach this to the main camera or a dedicated UI management object
    /// </summary>
    public class BackgroundClickHandler : MonoBehaviour, IPointerClickHandler
    {
        [Header("Background Click Settings")]
        [SerializeField] private bool _clearSelectionOnBackgroundClick = true;
        [SerializeField] private LayerMask _backgroundLayers = -1;

        private Camera _camera;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main ?? FindFirstObjectByType<Camera>();
            }

            // Ensure we have a way to receive clicks
            EnsureClickDetection();
        }

        private void Update()
        {
            // Handle mouse clicks when not using EventSystem
            if (UnityEngine.Input.GetMouseButtonDown(0) && _clearSelectionOnBackgroundClick)
            {
                HandleBackgroundClick(UnityEngine.Input.mousePosition);
            }
        }

        private void EnsureClickDetection()
        {
            // For 3D world clicks, we need a GraphicRaycaster or similar
            // For now, we'll use the Update method to check for clicks
            if (_camera != null)
            {
                // Ensure camera has PhysicsRaycaster for 3D interaction
                if (_camera.GetComponent<PhysicsRaycaster>() == null)
                {
                    _camera.gameObject.AddComponent<PhysicsRaycaster>();
                }
            }
        }

        private void HandleBackgroundClick(Vector2 screenPosition)
        {
            if (_camera == null) return;

            // Cast a ray to see what we hit
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            
            // Check if we hit any draggable items
            bool hitDraggableItem = false;
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _backgroundLayers))
            {
                var draggableItem = hit.collider.GetComponent<TransformableItem>();
                if (draggableItem != null)
                {
                    hitDraggableItem = true;
                }
            }

            // If we didn't hit a draggable item, clear selection
            if (!hitDraggableItem && TransformableSelectionManager.Instance != null)
            {
                TransformableSelectionManager.Instance.ClearSelection();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // This will be called by the EventSystem for UI clicks
            if (_clearSelectionOnBackgroundClick)
            {
                HandleBackgroundClick(eventData.position);
            }
        }
    }
}