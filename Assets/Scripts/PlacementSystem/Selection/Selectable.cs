using UnityEngine;
using UnityEngine.Events;

namespace Systems.PlacementSystem.Selection
{
    /// <summary>
    /// Component that marks a GameObject as selectable.
    /// Attach to placed objects to make them selectable by the SelectionController.
    /// Provides events for selection/deselection and optional metadata.
    /// </summary>
    public class Selectable : MonoBehaviour
    {
        [Header("Selection Settings")]
        [SerializeField, Tooltip("Can this object be selected?")]
        private bool _isSelectable = true;

        [SerializeField, Tooltip("Priority for selection (higher = preferred when overlapping)")]
        private int _selectionPriority = 0;

        [Header("Events")]
        [SerializeField]
        private UnityEvent _onSelected = new UnityEvent();

        [SerializeField]
        private UnityEvent _onDeselected = new UnityEvent();

        // Runtime state
        private bool _isCurrentlySelected;

        /// <summary>
        /// Gets or sets whether this object can be selected.
        /// </summary>
        public bool IsSelectable
        {
            get => _isSelectable;
            set => _isSelectable = value;
        }

        /// <summary>
        /// Gets the selection priority of this object.
        /// </summary>
        public int SelectionPriority => _selectionPriority;

        /// <summary>
        /// Gets whether this object is currently selected.
        /// </summary>
        public bool IsSelected => _isCurrentlySelected;

        /// <summary>
        /// Event triggered when this object is selected.
        /// </summary>
        public UnityEvent OnSelected => _onSelected;

        /// <summary>
        /// Event triggered when this object is deselected.
        /// </summary>
        public UnityEvent OnDeselected => _onDeselected;

        /// <summary>
        /// Called by SelectionController when this object is selected.
        /// </summary>
        internal void NotifySelected()
        {
            if (_isCurrentlySelected)
                return;

            _isCurrentlySelected = true;
            _onSelected?.Invoke();
        }

        /// <summary>
        /// Called by SelectionController when this object is deselected.
        /// </summary>
        internal void NotifyDeselected()
        {
            if (!_isCurrentlySelected)
                return;

            _isCurrentlySelected = false;
            _onDeselected?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw selection bounds for visualization in editor
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
            }
        }
    }
}
