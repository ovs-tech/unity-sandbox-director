using UnityEngine;
using Systems.SceneSandbox.Core;

namespace Systems.SceneSandbox.Core.Tools
{
    /// <summary>
    /// Tool for placing objects into the scene.
    /// Handles spawning a "ghost" object, updating its position via strategy, and confirming placement.
    /// </summary>
    public class PlacementTool : ISandboxTool
    {
        public string ToolName => "Placement";

        private ToolContext _context;
        private IPlacementStrategy _strategy;
        private string _objectIdToPlace;

        // State
        private bool _isActive;
        private GameObject _ghostObject;
        private TransformableItem _ghostItem;

        public PlacementTool(string objectId, IPlacementStrategy strategy)
        {
            _objectIdToPlace = objectId;
            _strategy = strategy;
        }

        public void OnEnter(ToolContext context)
        {
            _context = context;
            _isActive = true;

            // Subscribe to input via InputManager events
            _context.InputManager.OnPointerMoved += OnPointerMove;
            _context.InputManager.OnPointerDown += OnPointerDown;
            _context.InputManager.OnCancelPlacement += OnCancel;

            // Start placement immediately
            StartPlacement();
        }

        public void OnExit()
        {
            _isActive = false;

            if (_context != null)
            {
                _context.InputManager.OnPointerMoved -= OnPointerMove;
                _context.InputManager.OnPointerDown -= OnPointerDown;
                _context.InputManager.OnCancelPlacement -= OnCancel;
            }

            // Cleanup
            CleanupGhost();

            // Notify system we're done (if needed)
            if (_context != null && _context.PlacementSystem != null && _context.PlacementSystem.IsActive)
            {
                _context.PlacementSystem.CancelPlacement();
            }
        }

        public void OnUpdate()
        {
            // Optional per-frame logic
        }

        public void OnDrawGizmos()
        {
            // Visualize placement ray or grid if needed
        }

        private void StartPlacement()
        {
            if (_context == null || _context.PlacementSystem == null) return;

            // Ask system to create the object but let us control it
            // We use StartPlacementAndCreate which sets system state to Active
            // We pass Vector2.zero as initial screen pos, will update immediately
            _ghostObject = _context.PlacementSystem.StartPlacementAndCreate(_objectIdToPlace, Vector2.zero);

            if (_ghostObject != null)
            {
                _ghostItem = _ghostObject.GetComponent<TransformableItem>();
                // Initially disable colliders on ghost to prevent self-raycast
                var colliders = _ghostObject.GetComponentsInChildren<Collider>();
                foreach (var c in colliders) c.enabled = false;
            }
        }

        private void OnPointerMove(Vector2 screenPos)
        {
            if (!_isActive || _ghostObject == null || _context.CameraRaycaster == null) return;

            Ray ray = _context.CameraRaycaster.ScreenPointToRay(screenPos);

            if (_strategy.GetPlacement(ray, _context, out Vector3 pos, out Quaternion rot))
            {
                _ghostObject.transform.position = pos;
                _ghostObject.transform.rotation = rot;

                // Notify system for any listeners (e.g. UI updates)
                _context.PlacementSystem.UpdatePlacement(screenPos);
            }
        }

        private void OnPointerDown(Vector2 screenPos)
        {
            if (!_isActive || _ghostObject == null) return;

            // Re-enable colliders before confirming so it's a valid object in scene
            var colliders = _ghostObject.GetComponentsInChildren<Collider>();
            foreach (var c in colliders) c.enabled = true;

            // Confirm via system
            _context.PlacementSystem.ConfirmPlacement();

            // Placement confirmed.
            // Often we want continuous placement (place multiple).
            // If so, spawn next ghost.
            _ghostObject = null; // System has taken ownership or destroyed it if failed

            // Loop: Start next placement
            StartPlacement();
        }

        private void OnCancel()
        {
            if (_context != null && _context.PlacementSystem != null)
            {
                _context.PlacementSystem.CancelPlacement();
            }
            // Request switch to default tool (Selection)
            // Accessing manager via Builder in context
            if (_context.Builder != null)
            {
                 _context.Builder.SetTool("Selection");
            }
        }

        private void CleanupGhost()
        {
            if (_ghostObject != null)
            {
                Object.Destroy(_ghostObject);
                _ghostObject = null;
            }
        }
    }
}
