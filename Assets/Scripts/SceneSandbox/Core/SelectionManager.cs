using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SceneSandbox.Core
{
    /// <summary>
    /// SelectionManager - Phase 2.2
    /// Manages object selection, multi-select, and hover state.
    /// Non-breaking: provides APIs and events, delegates to existing builder behavior initially.
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        [Header("Debugging")]
        [SerializeField] private bool _debugLogs = false;
        [Header("Dependencies")]
        [SerializeField] private CameraRaycaster _cameraRaycaster;

        [Header("Selection Settings")]
        [SerializeField] private LayerMask _selectionLayers = -1;
        [SerializeField] private bool _autoEditOnSelect = true;

        [Header("Events")]
        public UnityEvent<GameObject> OnObjectSelected = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnObjectDeselected = new UnityEvent<GameObject>();
        public UnityEvent<List<GameObject>> OnSelectionChanged = new UnityEvent<List<GameObject>>();
        public UnityEvent<GameObject> OnObjectHoverEnter = new UnityEvent<GameObject>();
        public UnityEvent<GameObject> OnObjectHoverExit = new UnityEvent<GameObject>();

        // Internal state
        private readonly List<TransformableItem> _selectedItems = new List<TransformableItem>();
        private TransformableItem _lastSelectedItem;
        private GameObject _currentHoverItem;
        private bool? _tempAutoEditOverride; // Temporary override for next selection

        // Properties
        public bool AutoEditOnSelect => _autoEditOnSelect;
        public bool GetEffectiveAutoEdit(bool overrideValue)
        {
            if (_tempAutoEditOverride.HasValue)
            {
                bool result = _tempAutoEditOverride.Value;
                _tempAutoEditOverride = null; // Reset after use
                return result;
            }
            return overrideValue;
        }
        public List<TransformableItem> SelectedItems => new List<TransformableItem>(_selectedItems);
        public TransformableItem LastSelectedItem => _lastSelectedItem;
        public int SelectionCount => _selectedItems.Count;
        public bool HasSelection => _selectedItems.Count > 0;

        public void Initialize(CameraRaycaster cameraRaycaster, LayerMask selectionLayers, bool autoEditOnSelect)
        {
            _cameraRaycaster = cameraRaycaster;
            _selectionLayers = selectionLayers;
            _autoEditOnSelect = autoEditOnSelect;

            if (_debugLogs)
            {
                Debug.Log("[SelectionManager] Initialized with layers: " + _selectionLayers.value + ", autoEdit: " + _autoEditOnSelect);
            }
        }

        #region Public API

        /// <summary>
        /// Select a GameObject (converts to TransformableItem)
        /// </summary>
        public void SelectObject(GameObject obj, bool additive = false, bool overrideAutoEdit = false)
        {
            if (_debugLogs)
            {
                Debug.Log($"[SelectionManager] SelectObject => {obj?.name ?? "<null>"}, additive: {additive}, overrideAutoEdit: {overrideAutoEdit}");
            }
            // Store temporary override if different from default
            if (overrideAutoEdit != _autoEditOnSelect)
            {
                _tempAutoEditOverride = overrideAutoEdit;
            }

            if (obj == null)
            {
                ClearSelection();
                return;
            }

            var item = obj.GetComponent<TransformableItem>();
            if (item != null)
            {
                SelectItem(item, additive, overrideAutoEdit);
            }
        }

        /// <summary>
        /// Select a TransformableItem directly
        /// </summary>
        public void SelectItem(TransformableItem item, bool additive = false, bool overrideAutoEdit = false)
        {
            if (item == null) return;

            if (_debugLogs)
            {
                Debug.Log($"[SelectionManager] SelectItem => {item.gameObject.name}, additive: {additive}");
            }

            // Check if already selected
            if (_selectedItems.Contains(item))
            {
                // If not additive, and this is the only item, do nothing
                if (!additive && _selectedItems.Count == 1) return;
                
                // If additive, deselect this item (toggle)
                if (additive)
                {
                    DeselectItem(item);
                    return;
                }
            }

            // Single selection: clear others first
            if (!additive)
            {
                ClearSelection();
            }

            _selectedItems.Add(item);
            _lastSelectedItem = item;
            item.SetSelectedState(true);
            if (item.EnableTransformControls)
            {
                bool effectiveAutoEdit = GetEffectiveAutoEdit(overrideAutoEdit);
                if (effectiveAutoEdit)
                {
                    item.SetTransformModeType(TransformModeType.Position);
                }
            }

            OnObjectSelected?.Invoke(item.gameObject);
            OnSelectionChanged?.Invoke(GetSelectedObjects());
        }

        /// <summary>
        /// Deselect a GameObject
        /// </summary>
        public void DeselectObject(GameObject obj)
        {
            if (obj == null) return;

            var item = obj.GetComponent<TransformableItem>();
            if (item != null)
            {
                DeselectItem(item);
            }
        }

        /// <summary>
        /// Deselect a TransformableItem
        /// </summary>
        public void DeselectItem(TransformableItem item)
        {
            if (item == null || !_selectedItems.Contains(item)) return;

            if (_debugLogs)
            {
                Debug.Log($"[SelectionManager] DeselectItem => {item.gameObject.name}");
            }

            _selectedItems.Remove(item);
            item.SetSelectedState(false);

            if (_lastSelectedItem == item)
            {
                _lastSelectedItem = _selectedItems.Count > 0 ? _selectedItems[_selectedItems.Count - 1] : null;
            }

            OnObjectDeselected?.Invoke(item.gameObject);
            OnSelectionChanged?.Invoke(GetSelectedObjects());
        }

        /// <summary>
        /// Clear all selections
        /// </summary>
        public void ClearSelection()
        {
            if (_debugLogs)
            {
                Debug.Log($"[SelectionManager] ClearSelection (prev count: {_selectedItems.Count})");
            }
            if (_selectedItems.Count == 0) return;

            var itemsToDeselect = new List<TransformableItem>(_selectedItems);
            _selectedItems.Clear();
            _lastSelectedItem = null;

            foreach (var item in itemsToDeselect)
            {
                if (item != null)
                {
                    item.SetSelectedState(false);
                    if(item.EnableTransformControls)
                        item.SetTransformModeType(TransformModeType.None);
                    OnObjectDeselected?.Invoke(item.gameObject);
                }
            }

            OnSelectionChanged?.Invoke(GetSelectedObjects());
        }

        /// <summary>
        /// Select multiple objects at once
        /// </summary>
        public void SelectMultiple(List<GameObject> objects)
        {
            if (_debugLogs)
            {
                Debug.Log($"[SelectionManager] SelectMultiple => {objects?.Count ?? 0}");
            }
            if (objects == null || objects.Count == 0)
            {
                ClearSelection();
                return;
            }

            ClearSelection();

            foreach (var obj in objects)
            {
                if (obj == null) continue;

                var item = obj.GetComponent<TransformableItem>();
                if (item != null)
                {
                    _selectedItems.Add(item);
                    item.SetSelectedState(true);
                    _lastSelectedItem = item;
                }
            }

            OnSelectionChanged?.Invoke(GetSelectedObjects());
        }

        /// <summary>
        /// Get list of selected GameObjects
        /// </summary>
        public List<GameObject> GetSelectedObjects()
        {
            var objects = new List<GameObject>();
            foreach (var item in _selectedItems)
            {
                if (item != null)
                {
                    objects.Add(item.gameObject);
                }
            }
            return objects;
        }

        /// <summary>
        /// Get primary selected object (last selected)
        /// </summary>
        public GameObject GetPrimarySelection()
        {
            return _lastSelectedItem != null ? _lastSelectedItem.gameObject : null;
        }

        /// <summary>
        /// Check if an object is selected
        /// </summary>
        public bool IsObjectSelected(GameObject obj)
        {
            if (obj == null) return false;

            var item = obj.GetComponent<TransformableItem>();
            return item != null && _selectedItems.Contains(item);
        }

        #endregion

        #region Hover Detection

        /// <summary>
        /// Process raycast input for hover detection and interaction.
        /// Called per-frame to update hover state and detect clickable objects.
        /// </summary>
        /// <param name="screenPosition">Current screen position to raycast from</param>
        /// <param name="onObjectFound">Callback when an object is found under the cursor</param>
        /// <returns>The TransformableItem hit, or null if nothing was hit</returns>
        public TransformableItem ProcessRaycastInput(Vector2 screenPosition, System.Action<TransformableItem> onObjectFound = null)
        {
            if (_cameraRaycaster == null) return null;

            TransformableItem hitItem = null;

            if (_cameraRaycaster.TryRaycast(screenPosition, out RaycastHit hit))
            {
                // Check if hit object is on selection layers
                if (((1 << hit.collider.gameObject.layer) & _selectionLayers) != 0)
                {
                    hitItem = hit.collider.GetComponent<TransformableItem>();
                    if (hitItem == null)
                    {
                        hitItem = hit.collider.GetComponentInParent<TransformableItem>();
                    }

                    // Update hover state
                    UpdateHoverState(hitItem?.gameObject);

                    // Notify callback if object found
                    onObjectFound?.Invoke(hitItem);
                }
                else
                {
                    // Not on selection layer, clear hover
                    UpdateHoverState(null);
                }
            }
            else
            {
                // No hit, clear hover
                UpdateHoverState(null);
            }

            return hitItem;
        }

        /// <summary>
        /// Update hover detection from screen position
        /// </summary>
        public void UpdateHoverDetection(Vector2 screenPosition)
        {
            if (_cameraRaycaster == null) return;

            GameObject hitObject = null;

            if (_cameraRaycaster.TryRaycast(screenPosition, out RaycastHit hit))
            {
                // Check if hit object is on selection layers
                if (((1 << hit.collider.gameObject.layer) & _selectionLayers) != 0)
                {
                    hitObject = hit.collider.gameObject;
                }
            }

            UpdateHoverState(hitObject);
        }

        /// <summary>
        /// Manually set hover object (for custom raycast logic)
        /// </summary>
        public void UpdateHoverState(GameObject obj)
        {
            if (obj == _currentHoverItem) return;

            // Exit previous hover
            if (_currentHoverItem != null)
            {
                if (_debugLogs)
                {
                    Debug.Log($"[SelectionManager] HoverExit => {_currentHoverItem.name}");
                }
                OnObjectHoverExit?.Invoke(_currentHoverItem);
            }

            _currentHoverItem = obj;

            // Enter new hover
            if (_currentHoverItem != null)
            {
                if (_debugLogs)
                {
                    Debug.Log($"[SelectionManager] HoverEnter => {_currentHoverItem.name}");
                }
                OnObjectHoverEnter?.Invoke(_currentHoverItem);
            }
        }

        /// <summary>
        /// Get currently hovered object
        /// </summary>
        public GameObject GetHoveredObject()
        {
            return _currentHoverItem;
        }

        /// <summary>
        /// Clear hover state
        /// </summary>
        public void ClearHover()
        {
            UpdateHoverState(null);
        }

        #endregion
    }
}
