using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Component for areas where draggable items can be dropped
    /// Provides visual feedback and validates drop operations
    /// </summary>
    public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Drop Zone Settings")]
        [SerializeField] private bool _acceptAllTypes = true;
        [SerializeField] private List<string> _acceptedObjectTypes = new List<string>();
        [SerializeField] private bool _snapToCenter = false;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private float _gridSize = 1f;
        
        [Header("Visual Feedback")]
        [SerializeField] private Material _highlightMaterial;
        [SerializeField] private Color _validDropColor = Color.green;
        [SerializeField] private Color _invalidDropColor = Color.red;
        [SerializeField] private GameObject _dropIndicator;
        
        [Header("Zone Bounds")]
        [SerializeField] private Vector3 _zoneSize = Vector3.one;
        [SerializeField] private bool _showGizmos = true;
        
        // Components
        private Renderer _renderer;
        private Material _originalMaterial;
        private Collider _collider;
        
        // State
        private bool _isHighlighted;
        private bool _hasValidDragOver;
        private TransformableItem _hoveredItem;
        
        // Events
        public System.Action<TransformableItem, Vector3> OnItemDropped;
        public System.Action<TransformableItem> OnValidItemEnter;
        public System.Action<TransformableItem> OnValidItemExit;
        public System.Action<TransformableItem> OnInvalidItemEnter;
        public System.Action<TransformableItem> OnInvalidItemExit;
        
        // Properties
        public Vector3 ZoneSize 
        { 
            get => _zoneSize; 
            set => _zoneSize = value; 
        }
        
        public bool AcceptAllTypes 
        { 
            get => _acceptAllTypes; 
            set => _acceptAllTypes = value; 
        }
        
        public List<string> AcceptedObjectTypes => _acceptedObjectTypes;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            
            // Store original material
            if (_renderer != null)
            {
                _originalMaterial = _renderer.material;
            }
            
            // Ensure we have a collider
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
                _collider.isTrigger = true;
            }
            
            // Set up drop indicator
            if (_dropIndicator != null)
            {
                _dropIndicator.SetActive(false);
            }
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            var draggableItem = eventData.pointerDrag?.GetComponent<TransformableItem>();
            if (draggableItem == null) return;
            
            if (CanAcceptItem(draggableItem))
            {
                Vector3 dropPosition = CalculateDropPosition(eventData.position, draggableItem);
                OnItemDropped?.Invoke(draggableItem, dropPosition);
            }
            
            ClearHighlight();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            var draggableItem = eventData.pointerDrag?.GetComponent<TransformableItem>();
            if (draggableItem == null) return;
            
            _hoveredItem = draggableItem;
            
            if (CanAcceptItem(draggableItem))
            {
                SetHighlight(true, true);
                OnValidItemEnter?.Invoke(draggableItem);
            }
            else
            {
                SetHighlight(true, false);
                OnInvalidItemEnter?.Invoke(draggableItem);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hoveredItem != null)
            {
                if (_hasValidDragOver)
                {
                    OnValidItemExit?.Invoke(_hoveredItem);
                }
                else
                {
                    OnInvalidItemExit?.Invoke(_hoveredItem);
                }
            }
            
            _hoveredItem = null;
            ClearHighlight();
        }
        
        /// <summary>
        /// Check if this drop zone can accept the given item
        /// </summary>
        public bool CanAcceptItem(TransformableItem item)
        {
            if (item == null) return false;
            
            if (_acceptAllTypes) return true;
            
            // Check if the item's type is in the accepted types list
            return _acceptedObjectTypes.Contains(item.ObjectDataId);
        }
        
        /// <summary>
        /// Calculate the final position where the item should be dropped
        /// </summary>
        public Vector3 CalculateDropPosition(Vector2 screenPosition, TransformableItem item)
        {
            Vector3 worldPosition;
            
            if (_snapToCenter)
            {
                // Snap to center of drop zone
                worldPosition = transform.position;
            }
            else
            {
                // Use the screen position to calculate world position
                Camera camera = Camera.main ?? FindFirstObjectByType<Camera>();
                if (camera != null)
                {
                    Ray ray = camera.ScreenPointToRay(screenPosition);
                    Plane plane = new Plane(Vector3.up, transform.position);
                    
                    if (plane.Raycast(ray, out float distance))
                    {
                        worldPosition = ray.GetPoint(distance);
                    }
                    else
                    {
                        worldPosition = transform.position;
                    }
                }
                else
                {
                    worldPosition = transform.position;
                }
            }
            
            // Apply grid snapping
            if (_snapToGrid)
            {
                worldPosition = SnapToGrid(worldPosition);
            }
            
            // Ensure the position is within the drop zone bounds
            worldPosition = ClampToZoneBounds(worldPosition);
            
            return worldPosition;
        }
        
        /// <summary>
        /// Add an accepted object type
        /// </summary>
        public void AddAcceptedType(string objectType)
        {
            if (!_acceptedObjectTypes.Contains(objectType))
            {
                _acceptedObjectTypes.Add(objectType);
            }
        }
        
        /// <summary>
        /// Remove an accepted object type
        /// </summary>
        public void RemoveAcceptedType(string objectType)
        {
            _acceptedObjectTypes.Remove(objectType);
        }
        
        /// <summary>
        /// Check if a world position is within this drop zone
        /// </summary>
        public bool IsPositionInZone(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            Vector3 halfSize = _zoneSize * 0.5f;
            
            return localPosition.x >= -halfSize.x && localPosition.x <= halfSize.x &&
                   localPosition.y >= -halfSize.y && localPosition.y <= halfSize.y &&
                   localPosition.z >= -halfSize.z && localPosition.z <= halfSize.z;
        }
        
        private void SetHighlight(bool highlight, bool isValid)
        {
            _isHighlighted = highlight;
            _hasValidDragOver = isValid;
            
            if (highlight && _highlightMaterial != null && _renderer != null)
            {
                _renderer.material = _highlightMaterial;
                
                // Tint the material based on validity
                Color targetColor = isValid ? _validDropColor : _invalidDropColor;
                _renderer.material.color = targetColor;
            }
            else if (_renderer != null && _originalMaterial != null)
            {
                _renderer.material = _originalMaterial;
            }
            
            // Show/hide drop indicator
            if (_dropIndicator != null)
            {
                _dropIndicator.SetActive(highlight);
            }
        }
        
        private void ClearHighlight()
        {
            SetHighlight(false, false);
        }
        
        private Vector3 SnapToGrid(Vector3 position)
        {
            float snappedX = Mathf.Round(position.x / _gridSize) * _gridSize;
            float snappedZ = Mathf.Round(position.z / _gridSize) * _gridSize;
            return new Vector3(snappedX, position.y, snappedZ);
        }
        
        private Vector3 ClampToZoneBounds(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            Vector3 halfSize = _zoneSize * 0.5f;
            
            localPosition.x = Mathf.Clamp(localPosition.x, -halfSize.x, halfSize.x);
            localPosition.y = Mathf.Clamp(localPosition.y, -halfSize.y, halfSize.y);
            localPosition.z = Mathf.Clamp(localPosition.z, -halfSize.z, halfSize.z);
            
            return transform.TransformPoint(localPosition);
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            Gizmos.matrix = transform.localToWorldMatrix;
            
            // Draw zone bounds
            Color gizmoColor = _isHighlighted ? 
                (_hasValidDragOver ? _validDropColor : _invalidDropColor) : 
                Color.yellow;
            
            gizmoColor.a = 0.3f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(Vector3.zero, _zoneSize);
            
            // Draw wireframe
            gizmoColor.a = 1f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(Vector3.zero, _zoneSize);
            
            // Draw grid if enabled
            if (_snapToGrid)
            {
                DrawGrid();
            }
        }
        
        private void DrawGrid()
        {
            Vector3 halfSize = _zoneSize * 0.5f;
            int gridCountX = Mathf.FloorToInt(_zoneSize.x / _gridSize);
            int gridCountZ = Mathf.FloorToInt(_zoneSize.z / _gridSize);
            
            Gizmos.color = Color.white * 0.5f;
            
            // Draw grid lines along X axis
            for (int i = 0; i <= gridCountX; i++)
            {
                float x = -halfSize.x + (i * _gridSize);
                Vector3 start = new Vector3(x, 0, -halfSize.z);
                Vector3 end = new Vector3(x, 0, halfSize.z);
                Gizmos.DrawLine(start, end);
            }
            
            // Draw grid lines along Z axis
            for (int i = 0; i <= gridCountZ; i++)
            {
                float z = -halfSize.z + (i * _gridSize);
                Vector3 start = new Vector3(-halfSize.x, 0, z);
                Vector3 end = new Vector3(halfSize.x, 0, z);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}