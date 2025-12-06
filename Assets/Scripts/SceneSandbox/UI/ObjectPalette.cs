using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SceneSandbox.Data;

namespace SceneSandbox.UI
{
    /// <summary>
    /// UI panel that displays available objects for placement in the scene
    /// Supports filtering by type and category
    /// </summary>
    public class ObjectPalette : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private TMP_Dropdown _typeFilter;
        [SerializeField] private TMP_Dropdown _categoryFilter;
        [SerializeField] private TMP_InputField _searchField;
        [SerializeField] private Button _refreshButton;
        
        [Header("Layout Settings")]
        [SerializeField] private int _itemsPerRow = 4;
        [SerializeField] private Vector2 _itemSize = new Vector2(100, 120);
        [SerializeField] private Vector2 _itemSpacing = new Vector2(10, 10);
        
        [Header("References")]
        [SerializeField] private Core.SceneSandboxBuilder _sandboxBuilder;
        [SerializeField] private SceneObjectLibrary _objectLibrary;
        
        // State
        private List<ObjectPaletteItem> _paletteItems = new List<ObjectPaletteItem>();
        private ObjectPaletteItem _selectedItem;
        private SceneObjectType _currentTypeFilter = (SceneObjectType)(-1); // All types
        private string _currentCategoryFilter = "All";
        private string _currentSearchText = "";
        
        // Events
        public System.Action<SceneObjectData> OnObjectSelected;
        public System.Action<SceneObjectData, Vector2> OnObjectDraggedToScene;
        
        // Properties
        public SceneObjectLibrary ObjectLibrary 
        { 
            get => _objectLibrary; 
            set => _objectLibrary = value; 
        }
        
        public ObjectPaletteItem SelectedItem => _selectedItem;
        
        private void Awake()
        {
            SetupUI();
        }
        
        private void Start()
        {
            if (_objectLibrary != null)
            {
                RefreshPalette();
            }
            else
            {
                // No object library assigned
            }
        }
        
        private void SetupUI()
        {
            // Setup type filter dropdown
            if (_typeFilter != null)
            {
                _typeFilter.ClearOptions();
                var typeOptions = new List<string> { "All Types" };
                typeOptions.AddRange(System.Enum.GetNames(typeof(SceneObjectType)));
                _typeFilter.AddOptions(typeOptions);
                _typeFilter.onValueChanged.AddListener(OnTypeFilterChanged);
            }
            
            // Setup search field
            if (_searchField != null)
            {
                _searchField.onValueChanged.AddListener(OnSearchTextChanged);
            }
            
            // Setup refresh button
            if (_refreshButton != null)
            {
                _refreshButton.onClick.AddListener(RefreshPalette);
            }
        }
        
        /// <summary>
        /// Refresh the palette with objects from the library
        /// </summary>
        public void RefreshPalette()
        {
            if (_objectLibrary == null) return;
            
            ClearPalette();
            UpdateCategoryFilter();
            
            var filteredObjects = GetFilteredObjects();
            CreatePaletteItems(filteredObjects);
        }
        
        /// <summary>
        /// Clear all items from the palette
        /// </summary>
        public void ClearPalette()
        {
            foreach (var item in _paletteItems)
            {
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _paletteItems.Clear();
            _selectedItem = null;
        }
        
        /// <summary>
        /// Set the selected object type filter
        /// </summary>
        public void SetTypeFilter(SceneObjectType objectType)
        {
            _currentTypeFilter = objectType;
            
            if (_typeFilter != null)
            {
                int index = objectType == (SceneObjectType)(-1) ? 0 : (int)objectType + 1;
                _typeFilter.SetValueWithoutNotify(index);
            }
            
            RefreshPalette();
        }
        
        /// <summary>
        /// Set the selected category filter
        /// </summary>
        public void SetCategoryFilter(string category)
        {
            _currentCategoryFilter = category;
            
            if (_categoryFilter != null)
            {
                var options = _categoryFilter.options;
                int index = options.FindIndex(opt => opt.text == category);
                if (index >= 0)
                {
                    _categoryFilter.SetValueWithoutNotify(index);
                }
            }
            
            RefreshPalette();
        }
        
        /// <summary>
        /// Set the search text filter
        /// </summary>
        public void SetSearchFilter(string searchText)
        {
            _currentSearchText = searchText;
            
            if (_searchField != null)
            {
                _searchField.SetTextWithoutNotify(searchText);
            }
            
            RefreshPalette();
        }
        
        /// <summary>
        /// Select a specific palette item
        /// </summary>
        public void SelectItem(ObjectPaletteItem item)
        {
            // Deselect previous item
            if (_selectedItem != null)
            {
                _selectedItem.SetSelected(false);
            }
            
            _selectedItem = item;
            
            // Select new item
            if (_selectedItem != null)
            {
                _selectedItem.SetSelected(true);
                OnObjectSelected?.Invoke(_selectedItem.ObjectData);
            }
        }
        
        private void UpdateCategoryFilter()
        {
            if (_categoryFilter == null || _objectLibrary == null) return;
            
            _categoryFilter.ClearOptions();
            var categoryOptions = new List<string> { "All" };
            categoryOptions.AddRange(_objectLibrary.GetCategories());
            _categoryFilter.AddOptions(categoryOptions);
            _categoryFilter.onValueChanged.RemoveAllListeners();
            _categoryFilter.onValueChanged.AddListener(OnCategoryFilterChanged);
        }
        
        private List<SceneObjectData> GetFilteredObjects()
        {
            if (_objectLibrary == null) return new List<SceneObjectData>();
            
            var allObjects = _objectLibrary.GetAllObjects();
            var filteredObjects = allObjects.AsEnumerable();
            
            // Filter by type
            if (_currentTypeFilter != (SceneObjectType)(-1))
            {
                filteredObjects = filteredObjects.Where(obj => obj.objectType == _currentTypeFilter);
            }
            
            // Filter by category
            if (_currentCategoryFilter != "All")
            {
                filteredObjects = filteredObjects.Where(obj => obj.category == _currentCategoryFilter);
            }
            
            // Filter by search text
            if (!string.IsNullOrEmpty(_currentSearchText))
            {
                string searchLower = _currentSearchText.ToLower();
                filteredObjects = filteredObjects.Where(obj => 
                    obj.displayName.ToLower().Contains(searchLower) ||
                    (obj.tags != null && obj.tags.Any(tag => tag.ToLower().Contains(searchLower))));
            }
            
            return filteredObjects.ToList();
        }
        
        private void CreatePaletteItems(List<SceneObjectData> objects)
        {
            if (_itemContainer == null) 
            {
                return;
            }
            
            if (_itemPrefab == null)
            {
                return;
            }
            
            foreach (var objectData in objects)
            {
                CreatePaletteItem(objectData);
            }
            
            // Update layout
            UpdateLayout();
        }
        
        private void CreatePaletteItem(SceneObjectData objectData)
        {
            
            // Use prefab to create item
            GameObject itemGO = Instantiate(_itemPrefab, _itemContainer);
            itemGO.SetActive(true); // Ensure it's active
            
            var paletteItem = itemGO.GetComponent<ObjectPaletteItem>();
            
            // If no ObjectPaletteItem component exists, add one
            if (paletteItem == null)
            {
                paletteItem = itemGO.AddComponent<ObjectPaletteItem>();
            }
            
            if (paletteItem != null)
            {
                paletteItem.Initialize(objectData, this);
                
                // Bind events
                paletteItem.OnItemSelected += SelectItem;
                paletteItem.OnItemDragStarted += OnItemDragStarted;
                paletteItem.OnItemDragMoved += OnItemDragMoved;
                paletteItem.OnItemDragEnded += OnItemDragEnded;
                
                _paletteItems.Add(paletteItem);
            }
        }
        
        private void UpdateLayout()
        {
            if (_itemContainer == null) return;
            
            var gridLayout = _itemContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.cellSize = _itemSize;
                gridLayout.spacing = _itemSpacing;
                gridLayout.constraintCount = _itemsPerRow;
            }
        }
        
        #region Event Handlers
        
        private void OnTypeFilterChanged(int index)
        {
            if (index == 0)
            {
                _currentTypeFilter = (SceneObjectType)(-1); // All types
            }
            else
            {
                _currentTypeFilter = (SceneObjectType)(index - 1);
            }
            
            RefreshPalette();
        }
        
        private void OnCategoryFilterChanged(int index)
        {
            if (_categoryFilter != null && index < _categoryFilter.options.Count)
            {
                _currentCategoryFilter = _categoryFilter.options[index].text;
                RefreshPalette();
            }
        }
        
        private void OnSearchTextChanged(string searchText)
        {
            _currentSearchText = searchText;
            RefreshPalette();
        }
        
        private void OnItemDragStarted(ObjectPaletteItem item, Vector2 screenPosition)
        {
            // Optional: Provide feedback when dragging starts
        }
        
        private void OnItemDragMoved(ObjectPaletteItem item, Vector2 screenPosition)
        {
            // Optional: Update drag feedback
        }
        
        private void OnItemDragEnded(ObjectPaletteItem item, Vector2 screenPosition)
        {
            // Check if dropped on scene area
            if (IsScreenPositionOverScene(screenPosition))
            {
                Vector3 worldPosition = ScreenToWorldPosition(screenPosition);
                
                if (_sandboxBuilder != null)
                {
                    _sandboxBuilder.StartPlacement(item.ObjectData.id, worldPosition);
                    _sandboxBuilder.ConfirmPlacement();
                }
                
                OnObjectDraggedToScene?.Invoke(item.ObjectData, screenPosition);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool IsScreenPositionOverScene(Vector2 screenPosition)
        {
            // Check if the screen position is over the 3D scene area
            // This is a simple implementation - you might want to make this more sophisticated
            Camera sceneCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (sceneCamera == null) return false;
            
            // Convert screen position to viewport coordinates
            Vector2 viewportPosition = sceneCamera.ScreenToViewportPoint(screenPosition);
            
            // Check if within camera viewport
            return viewportPosition.x >= 0 && viewportPosition.x <= 1 &&
                   viewportPosition.y >= 0 && viewportPosition.y <= 1;
        }
        
        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            Camera sceneCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (sceneCamera == null) return Vector3.zero;
            
            // Cast a ray from screen position
            Ray ray = sceneCamera.ScreenPointToRay(screenPosition);
            
            // Use a ground plane for placement
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            // Fallback to a position in front of the camera
            return ray.GetPoint(10f);
        }
        
        #endregion
        

    }
}