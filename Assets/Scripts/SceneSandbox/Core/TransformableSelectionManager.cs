using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSandbox.Core
{
    /// <summary>
    /// Manages selection state for draggable items in the scene sandbox
    /// Ensures only one item is selected at a time and provides selection events
    /// </summary>
    public class TransformableSelectionManager : MonoBehaviour
    {
        private static TransformableSelectionManager _instance;
        public static TransformableSelectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TransformableSelectionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SelectionManager");
                        _instance = go.AddComponent<TransformableSelectionManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Selection Settings")]
        [SerializeField] private bool _allowMultipleSelection = false;

        // Current selection
        [SerializeField]private readonly List<TransformableItem> _selectedItems = new List<TransformableItem>();
        [SerializeField] private TransformableItem _lastSelectedItem;

        // Events
        public System.Action<TransformableItem> OnItemSelected;
        public System.Action<TransformableItem> OnItemDeselected;
        public System.Action<List<TransformableItem>> OnSelectionChanged;

        // Properties
        public TransformableItem SelectedItem => _selectedItems.Count > 0 ? _selectedItems[0] : null;
        public List<TransformableItem> SelectedItems => new List<TransformableItem>(_selectedItems);
        public bool HasSelection => _selectedItems.Count > 0;
        public int SelectionCount => _selectedItems.Count;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Subscribe to scene change events for cleanup
                SceneManager.sceneUnloaded += OnSceneUnloaded;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Find all existing draggable items and register for their events
            RegisterExistingDraggableItems();
        }

        /// <summary>
        /// Register for events on all existing draggable items in the scene
        /// </summary>
        private void RegisterExistingDraggableItems()
        {
            var existingItems = FindObjectsByType<TransformableItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var item in existingItems)
            {
                RegisterDraggableItem(item);
            }
        }

        /// <summary>
        /// Register a draggable item for selection management
        /// </summary>
        /// <param name="item">The draggable item to register</param>
        public void RegisterDraggableItem(TransformableItem item)
        {
            if (item == null) return;

            // Subscribe to selection events
            item.OnSelectionChanged -= OnDraggableItemSelectionChanged;
            item.OnSelectionChanged += OnDraggableItemSelectionChanged;
        }

        /// <summary>
        /// Unregister a draggable item from selection management
        /// </summary>
        /// <param name="item">The draggable item to unregister</param>
        public void UnregisterDraggableItem(TransformableItem item)
        {
            if (item == null) return;

            // Unsubscribe from events
            item.OnSelectionChanged -= OnDraggableItemSelectionChanged;

            // Remove from selection if it was selected
            if (_selectedItems.Contains(item))
            {
                _selectedItems.Remove(item);
                OnItemDeselected?.Invoke(item);
                OnSelectionChanged?.Invoke(SelectedItems);
            }
        }

        /// <summary>
        /// Handle selection state change from a draggable item
        /// </summary>
        /// <param name="item">The item whose selection changed</param>
        /// <param name="isSelected">Whether the item is now selected</param>
        private void OnDraggableItemSelectionChanged(TransformableItem item, bool isSelected)
        {
            if (isSelected)
            {
                SelectItem(item);
            }
            else
            {
                DeselectItem(item);
            }
        }

        /// <summary>
        /// Select an item
        /// </summary>
        /// <param name="item">The item to select</param>
        public void SelectItem(TransformableItem item)
        {
            if (item == null || _selectedItems.Contains(item)) return;

            // If multiple selection is not allowed, deselect all other items
            if (!_allowMultipleSelection && _selectedItems.Count > 0)
            {
                ClearSelection();
            }

            _selectedItems.Add(item);
            _lastSelectedItem = item;

            // Ensure the item's visual state is updated
            item.SetSelectedState(true);

            OnItemSelected?.Invoke(item);
            OnSelectionChanged?.Invoke(SelectedItems);

            Debug.Log($"Selected item: {item.name}");
        }

        /// <summary>
        /// Deselect an item
        /// </summary>
        /// <param name="item">The item to deselect</param>
        public void DeselectItem(TransformableItem item)
        {
            if (item == null || !_selectedItems.Contains(item)) return;

            _selectedItems.Remove(item);

            // Ensure the item's visual state is updated
            item.SetSelectedState(false);

            OnItemDeselected?.Invoke(item);
            OnSelectionChanged?.Invoke(SelectedItems);

            if (_lastSelectedItem == item)
            {
                _lastSelectedItem = _selectedItems.Count > 0 ? _selectedItems[_selectedItems.Count - 1] : null;
            }

            Debug.Log($"Deselected item: {item.name}");
        }

        /// <summary>
        /// Clear all selection
        /// </summary>
        public void ClearSelection()
        {
            var itemsToDeselect = new List<TransformableItem>(_selectedItems);
            
            foreach (var item in itemsToDeselect)
            {
                if (item != null)
                {
                    item.SetSelectedState(false);
                }
            }

            _selectedItems.Clear();
            _lastSelectedItem = null;

            foreach (var item in itemsToDeselect)
            {
                if (item != null)
                {
                    OnItemDeselected?.Invoke(item);
                }
            }

            OnSelectionChanged?.Invoke(SelectedItems);

            Debug.Log("Cleared all selection");
        }

        /// <summary>
        /// Toggle selection state of an item
        /// </summary>
        /// <param name="item">The item to toggle</param>
        public void ToggleSelection(TransformableItem item)
        {
            if (item == null) return;

            if (_selectedItems.Contains(item))
            {
                DeselectItem(item);
            }
            else
            {
                SelectItem(item);
            }
        }

        /// <summary>
        /// Check if an item is selected
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if the item is selected</returns>
        public bool IsSelected(TransformableItem item)
        {
            return item != null && _selectedItems.Contains(item);
        }

        /// <summary>
        /// Delete all selected items
        /// </summary>
        public void DeleteSelectedItems()
        {
            if (_selectedItems.Count == 0) return;

            var itemsToDelete = new List<TransformableItem>(_selectedItems);
            ClearSelection();

            foreach (var item in itemsToDelete)
            {
                if (item != null && item.gameObject != null)
                {
                    Debug.Log($"Deleting selected item: {item.name}");
                    
                    if (Application.isPlaying)
                    {
                        Destroy(item.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(item.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Called when a scene is unloaded - clean up all scene-specific data
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"TransformableSelectionManager: Cleaning up for unloaded scene '{scene.name}'");
            ClearSelection();
        }

        /// <summary>
        /// Called when a scene is loaded - register existing draggable items
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"TransformableSelectionManager: Registering items in loaded scene '{scene.name}'");
            // Re-register items in the new scene
            RegisterExistingDraggableItems();
        }

        private void OnDestroy()
        {
            // Unsubscribe from scene events
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            // Show selection info in the corner for debugging
            GUI.Box(new Rect(10, 10, 200, 60), "");
            GUI.Label(new Rect(15, 15, 190, 20), $"Selected Items: {_selectedItems.Count}");
            
            if (_lastSelectedItem != null)
            {
                GUI.Label(new Rect(15, 35, 190, 20), $"Last: {_lastSelectedItem.name}");
            }
        }
        #endif
    }
}