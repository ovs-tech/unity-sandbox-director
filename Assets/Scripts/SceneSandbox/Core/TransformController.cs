using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SceneSandbox.Core
{
    /// <summary>
    /// TransformController - Phase 2.3
    /// Manages transform modes (Position/Rotation/Scale) and axis constraints.
    /// Non-breaking: provides APIs and events, delegates to existing builder behavior initially.
    /// </summary>
    public class TransformController : MonoBehaviour
    {
        [Header("Debugging")]
        [SerializeField] private bool _debugLogs = false;
        [Header("Dependencies")]
        [SerializeField] private SelectionManager _selectionManager;
        [SerializeField] private GridManager _gridManager;

        [Header("Transform Sensitivity")]
        [SerializeField] private float _rotationSensitivity = 1.0f; // Degrees per pixel
        [SerializeField] private float _scaleSensitivity = 0.01f; // Scale units per pixel
        [SerializeField] private float _scrollScaleSensitivity = 0.1f; // Scale units per scroll notch

        [Header("Scale Constraints")]
        [SerializeField] private Vector3 _minScale = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 _maxScale = new Vector3(10f, 10f, 10f);

        [Header("Transform Mode Colors")]
        [SerializeField] private Color _positionModeColor = new Color(0f, 1f, 1f, 0.5f); // Cyan
        [SerializeField] private Color _rotationModeColor = new Color(1f, 1f, 0f, 0.5f); // Yellow
        [SerializeField] private Color _scaleModeColor = new Color(1f, 0f, 1f, 0.5f); // Magenta

        [Header("Events")]
        public UnityEvent<TransformModeType> OnTransformModeChanged = new UnityEvent<TransformModeType>();
        public UnityEvent<TransformAxis> OnTransformAxisChanged = new UnityEvent<TransformAxis>();

        // Internal state
        private TransformModeType _currentMode = TransformModeType.Position;
        private TransformAxis _currentAxis = TransformAxis.All;
        private List<TransformableItem> _activeTransformItems = new List<TransformableItem>();

        // Properties
        public TransformModeType CurrentMode => _currentMode;
        public TransformAxis CurrentAxis => _currentAxis;
        public Color CurrentModeColor => GetModeColor(_currentMode);
        public bool IsTransformActive => _activeTransformItems.Count > 0;

        public void Initialize(SelectionManager selectionManager, GridManager gridManager)
        {
            _selectionManager = selectionManager;
            _gridManager = gridManager;

            // Subscribe to selection changes
            if (_selectionManager != null)
            {
                // Subscribe without clearing other listeners
                _selectionManager.OnSelectionChanged.AddListener(OnSelectionChanged);

                // Also register per-object select/deselect to keep active items in sync
                _selectionManager.OnObjectSelected.AddListener((GameObject obj) =>
                {
                    if (_debugLogs)
                    {
                        Debug.Log($"[TransformController] OnObjectSelected => {obj?.name ?? "<null>"}");
                    }
                    var item = obj != null ? obj.GetComponent<TransformableItem>() : null;
                    if (item != null)
                    {
                        RegisterItem(item);
                    }
                });

                _selectionManager.OnObjectDeselected.AddListener((GameObject obj) =>
                {
                    if (_debugLogs)
                    {
                        Debug.Log($"[TransformController] OnObjectDeselected => {obj?.name ?? "<null>"}");
                    }
                    var item = obj != null ? obj.GetComponent<TransformableItem>() : null;
                    if (item != null)
                    {
                        UnregisterItem(item);
                    }
                });
            }
        }

        #region Transform Mode Management

        /// <summary>
        /// Set the current transform mode (Position/Rotation/Scale)
        /// </summary>
        public void SetTransformMode(TransformModeType mode)
        {
            if (_currentMode == mode) return;

            _currentMode = mode;

            // Reset axis to All when changing mode or entering Position mode
            if (mode == TransformModeType.Position || mode == TransformModeType.None)
            {
                SetTransformAxis(TransformAxis.All);
            }

            // Update active transform items with new mode
            UpdateActiveTransformItemsMode();

            OnTransformModeChanged?.Invoke(_currentMode);
        }

        /// <summary>
        /// Toggle the current transform axis (X, Y, Z, All)
        /// Only applicable for Rotation and Scale modes
        /// </summary>
        public void ToggleTransformAxis()
        {
            // Cycle through axes: All -> X -> Y -> Z -> All
            TransformAxis newAxis = _currentAxis switch
            {
                TransformAxis.All => TransformAxis.X,
                TransformAxis.X => TransformAxis.Y,
                TransformAxis.Y => TransformAxis.Z,
                TransformAxis.Z => TransformAxis.All,
                _ => TransformAxis.All
            };

            SetTransformAxis(newAxis);
        }

        /// <summary>
        /// Set the current transform axis
        /// </summary>
        public void SetTransformAxis(TransformAxis axis)
        {
            if (_currentAxis == axis) return;

            _currentAxis = axis;

            // Update active transform items with new axis
            UpdateActiveTransformItemsAxis();

            OnTransformAxisChanged?.Invoke(_currentAxis);
        }

        /// <summary>
        /// Get color for a specific transform mode
        /// </summary>
        public Color GetModeColor(TransformModeType mode)
        {
            return mode switch
            {
                TransformModeType.Position => _positionModeColor,
                TransformModeType.Rotation => _rotationModeColor,
                TransformModeType.Scale => _scaleModeColor,
                _ => Color.white
            };
        }

        #endregion

        #region Transform Application

        /// <summary>
        /// Apply transform delta from drag input
        /// </summary>
        public void ApplyTransformDelta(Vector2 dragDelta, List<GameObject> targets = null)
        {
            if (targets == null || targets.Count == 0)
            {
                // Use selected items
                targets = _selectionManager?.GetSelectedObjects();
                if (targets == null || targets.Count == 0) return;
            }

            switch (_currentMode)
            {
                case TransformModeType.Position:
                    ApplyPositionDelta(dragDelta, targets);
                    break;

                case TransformModeType.Rotation:
                    ApplyRotationDelta(dragDelta, targets);
                    break;

                case TransformModeType.Scale:
                    ApplyScaleDelta(dragDelta, targets);
                    break;
            }
        }

        /// <summary>
        /// Apply scroll-based transform (typically for scaling)
        /// </summary>
        public void ApplyScrollTransform(float scrollDelta, List<GameObject> targets = null)
        {
            if (targets == null || targets.Count == 0)
            {
                // Use selected items
                targets = _selectionManager?.GetSelectedObjects();
                if (targets == null || targets.Count == 0) return;
            }

            // Scroll typically used for scaling
            if (_currentMode == TransformModeType.Scale)
            {
                ApplyScrollScale(scrollDelta, targets);
            }
        }

        private void ApplyPositionDelta(Vector2 delta, List<GameObject> targets)
        {
            Vector3 worldDelta = CalculateWorldDelta(delta);

            foreach (var obj in targets)
            {
                if (obj == null) continue;

                Vector3 newPosition = obj.transform.position + ApplyAxisConstraint(worldDelta, _currentAxis);

                // Apply grid snapping if enabled
                if (_gridManager != null && _gridManager.Enabled)
                {
                    newPosition = _gridManager.GetSnappedPosition(newPosition);
                }

                obj.transform.position = newPosition;
            }
        }

        private void ApplyRotationDelta(Vector2 delta, List<GameObject> targets)
        {
            // Use horizontal drag delta for rotation
            float rotationDelta = delta.x * _rotationSensitivity;

            foreach (var obj in targets)
            {
                if (obj == null) continue;

                Vector3 rotationAxis = GetAxisVector(_currentAxis);
                obj.transform.Rotate(rotationAxis, rotationDelta, Space.World);
            }
        }

        private void ApplyScaleDelta(Vector2 delta, List<GameObject> targets)
        {
            // Use vertical drag delta for scaling (up = larger, down = smaller)
            float scaleDelta = -delta.y * _scaleSensitivity;

            foreach (var obj in targets)
            {
                if (obj == null) continue;

                Vector3 scaleChange = ApplyAxisConstraint(Vector3.one * scaleDelta, _currentAxis);
                Vector3 newScale = obj.transform.localScale + scaleChange;

                // Clamp scale to min/max constraints
                newScale = Vector3.Max(newScale, _minScale);
                newScale = Vector3.Min(newScale, _maxScale);

                obj.transform.localScale = newScale;
            }
        }

        private void ApplyScrollScale(float scrollDelta, List<GameObject> targets)
        {
            float scaleMultiplier = scrollDelta * _scrollScaleSensitivity;

            foreach (var obj in targets)
            {
                if (obj == null) continue;

                Vector3 scaleChange = ApplyAxisConstraint(Vector3.one * scaleMultiplier, _currentAxis);
                Vector3 newScale = obj.transform.localScale + scaleChange;

                // Clamp scale to min/max constraints
                newScale = Vector3.Max(newScale, _minScale);
                newScale = Vector3.Min(newScale, _maxScale);

                obj.transform.localScale = newScale;
            }
        }

        #endregion

        #region Transform Item Management

        /// <summary>
        /// Register a TransformableItem for transform control
        /// </summary>
        public void RegisterItem(TransformableItem item)
        {
            if (item == null || _activeTransformItems.Contains(item)) return;
            if (_debugLogs)
            {
                Debug.Log($"[TransformController] Registering TransformableItem: {item.gameObject.name}");
            }
            _activeTransformItems.Add(item);
            item.SetTransformModeType(_currentMode);
            item.SetTransformAxis(_currentAxis);
        }

        /// <summary>
        /// Unregister a TransformableItem from transform control
        /// </summary>
        public void UnregisterItem(TransformableItem item)
        {
            if (item == null) return;
            if (_debugLogs)
            {
                Debug.Log($"[TransformController] Unregistering TransformableItem: {item.gameObject.name}");
            }
            _activeTransformItems.Remove(item);
        }

        /// <summary>
        /// Clear all registered transform items
        /// </summary>
        public void ClearTransformItems()
        {
            foreach (var item in _activeTransformItems)
            {
                if (item != null)
                {
                    item.SetTransformModeType(TransformModeType.None);
                }
            }
            _activeTransformItems.Clear();
        }

        private void UpdateActiveTransformItemsMode()
        {
            // If no active items, sync from current selection to avoid no-op updates
            if (_activeTransformItems.Count == 0 && _selectionManager != null)
            {
                SyncActiveItemsFromSelection();
            }
            foreach (var item in _activeTransformItems)
            {
                if (item != null)
                {
                    item.SetTransformModeType(_currentMode);
                }
            }
        }

        private void UpdateActiveTransformItemsAxis()
        {
            // Ensure we have items to update; if empty, sync from selection first
            if (_activeTransformItems.Count == 0 && _selectionManager != null)
            {
                SyncActiveItemsFromSelection();
            }
            if (_debugLogs)
            {
                Debug.Log("[TransformController] Updating Active Transform Items to Axis: " + _currentAxis + " for Mode: " + _currentMode + " with count: " + _activeTransformItems.Count);
            }
            foreach (var item in _activeTransformItems)
            {
                if (item != null)
                {
                    item.SetTransformAxis(_currentAxis);
                }
            }
        }

        private void OnSelectionChanged(List<GameObject> selectedObjects)
        {
            if (_debugLogs)
            {
                Debug.Log($"[TransformController] OnSelectionChanged => count: {selectedObjects?.Count ?? 0}");
            }
            SyncActiveItemsFromSelection(selectedObjects);
        }

        /// <summary>
        /// Sync active transform items list from selection manager
        /// </summary>
        private void SyncActiveItemsFromSelection(List<GameObject> selectedObjects = null)
        {
            ClearTransformItems();
            var objects = selectedObjects ?? _selectionManager?.GetSelectedObjects();
            if (objects == null) return;

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                var item = obj.GetComponent<TransformableItem>();
                if (item != null)
                {
                    RegisterItem(item);
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Calculate world-space delta from screen-space drag
        /// </summary>
        private Vector3 CalculateWorldDelta(Vector2 screenDelta)
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;

            // Simple screen-to-world mapping (can be enhanced)
            Vector3 worldDelta = cam.transform.right * screenDelta.x * 0.01f +
                                cam.transform.up * screenDelta.y * 0.01f;

            return worldDelta;
        }

        /// <summary>
        /// Apply axis constraint to a vector
        /// </summary>
        private Vector3 ApplyAxisConstraint(Vector3 value, TransformAxis axis)
        {
            return axis switch
            {
                TransformAxis.X => new Vector3(value.x, 0, 0),
                TransformAxis.Y => new Vector3(0, value.y, 0),
                TransformAxis.Z => new Vector3(0, 0, value.z),
                TransformAxis.All => value,
                _ => value
            };
        }

        /// <summary>
        /// Get axis vector for rotation
        /// </summary>
        private Vector3 GetAxisVector(TransformAxis axis)
        {
            return axis switch
            {
                TransformAxis.X => Vector3.right,
                TransformAxis.Y => Vector3.up,
                TransformAxis.Z => Vector3.forward,
                TransformAxis.All => Vector3.up, // Default to Y for "All"
                _ => Vector3.up
            };
        }

        #endregion
    }
}
