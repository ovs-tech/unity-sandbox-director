using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using SceneSandbox.Data;

namespace SceneSandbox.UI
{
    /// <summary>
    /// UI panel that displays available objects for placement in the scene using UI Toolkit
    /// Supports filtering by type and category
    /// </summary>
    public class ObjectPaletteUIToolkit : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Visual Assets")]
        [SerializeField] private VisualTreeAsset _paletteTemplate;
        [SerializeField] private VisualTreeAsset _itemTemplate;
        [SerializeField] private StyleSheet _paletteStyleSheet;
        
        [Header("Layout Settings")]
        [SerializeField] private int _itemsPerRow = 4;
        [SerializeField] private Vector2 _itemSize = new Vector2(100, 120);
        
        [Header("References")]
        [SerializeField] private Core.SceneSandboxBuilder _sandboxBuilder;
        [SerializeField] private SceneObjectLibrary _objectLibrary;
        
        // Visual Elements
        private VisualElement _rootElement;
        private VisualElement _paletteContainer;
        private DropdownField _typeFilter;
        private DropdownField _categoryFilter;
        private TextField _searchField;
        private Button _refreshButton;
        private ScrollView _itemsScrollView;
        private VisualElement _itemsContainer;
        
        // State
        private List<ObjectPaletteItemUIToolkit> _paletteItems = new List<ObjectPaletteItemUIToolkit>();
        private ObjectPaletteItemUIToolkit _selectedItem;
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
        
        public ObjectPaletteItemUIToolkit SelectedItem => _selectedItem;
        
        private void Awake()
        {
            InitializeUI();
            SetupUI();
        }
        
        private void Start()
        {
            if (_objectLibrary != null)
            {
                RefreshPalette();
            }
        }
        
        private void InitializeUI()
        {
            // Get or create UIDocument
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
                if (_uiDocument == null)
                {
                    _uiDocument = gameObject.AddComponent<UIDocument>();
                }
            }
            
            // Create root element
            if (_paletteTemplate != null)
            {
                _paletteTemplate.CloneTree(_uiDocument.rootVisualElement);
            }
            else
            {
                CreatePaletteFromCode();
            }
            
            _rootElement = _uiDocument.rootVisualElement;
            
            // Apply stylesheet
            if (_paletteStyleSheet != null && !_rootElement.styleSheets.Contains(_paletteStyleSheet))
            {
                _rootElement.styleSheets.Add(_paletteStyleSheet);
            }
            
            // Query UI elements
            QueryUIElements();
        }
        
        private void CreatePaletteFromCode()
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();
            
            // Main palette container
            _paletteContainer = new VisualElement();
            _paletteContainer.name = "palette-container";
            _paletteContainer.AddToClassList("panel");
            _paletteContainer.AddToClassList("palette-panel");
            _paletteContainer.style.flexGrow = 1;
            root.Add(_paletteContainer);
            
            // Title
            var title = new Label("Object Palette");
            title.AddToClassList("panel__header");
            _paletteContainer.Add(title);
            
            // Filters section
            var filtersContainer = new VisualElement();
            filtersContainer.name = "filters-container";
            filtersContainer.style.marginBottom = 8;
            _paletteContainer.Add(filtersContainer);
            
            // Type filter
            _typeFilter = new DropdownField("Type");
            _typeFilter.choices = new List<string> { "All Types" };
            _typeFilter.choices.AddRange(System.Enum.GetNames(typeof(SceneObjectType)));
            _typeFilter.value = "All Types";
            filtersContainer.Add(_typeFilter);
            
            // Category filter
            _categoryFilter = new DropdownField("Category");
            _categoryFilter.choices = new List<string> { "All" };
            _categoryFilter.value = "All";
            filtersContainer.Add(_categoryFilter);
            
            // Search field
            _searchField = new TextField("Search");
            _searchField.style.marginTop = 4;
            filtersContainer.Add(_searchField);
            
            // Refresh button
            _refreshButton = new Button(() => RefreshPalette()) { text = "Refresh" };
            _refreshButton.AddToClassList("button");
            _refreshButton.AddToClassList("button--secondary");
            _refreshButton.style.marginTop = 4;
            filtersContainer.Add(_refreshButton);
            
            // Items scroll view
            _itemsScrollView = new ScrollView(ScrollViewMode.Vertical);
            _itemsScrollView.style.flexGrow = 1;
            _paletteContainer.Add(_itemsScrollView);
            
            // Items container (grid layout)
            _itemsContainer = new VisualElement();
            _itemsContainer.name = "items-container";
            _itemsContainer.style.flexDirection = FlexDirection.Row;
            _itemsContainer.style.flexWrap = Wrap.Wrap;
            _itemsScrollView.Add(_itemsContainer);
        }
        
        private void QueryUIElements()
        {
            _paletteContainer = _rootElement.Q<VisualElement>("palette-container");
            _typeFilter = _rootElement.Q<DropdownField>("type-filter");
            _categoryFilter = _rootElement.Q<DropdownField>("category-filter");
            _searchField = _rootElement.Q<TextField>("search-field");
            _refreshButton = _rootElement.Q<Button>("refresh-button");
            _itemsScrollView = _rootElement.Q<ScrollView>("items-scroll-view");
            _itemsContainer = _rootElement.Q<VisualElement>("items-container");
        }
        
        private void SetupUI()
        {
            // Setup type filter
            if (_typeFilter != null)
            {
                _typeFilter.RegisterValueChangedCallback(evt => OnTypeFilterChanged(evt.newValue));
            }
            
            // Setup category filter
            if (_categoryFilter != null)
            {
                _categoryFilter.RegisterValueChangedCallback(evt => OnCategoryFilterChanged(evt.newValue));
            }
            
            // Setup search field
            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt => OnSearchTextChanged(evt.newValue));
            }
            
            // Refresh button already set up in CreatePaletteFromCode
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
                item?.Dispose();
            }
            _paletteItems.Clear();
            _selectedItem = null;
            
            if (_itemsContainer != null)
            {
                _itemsContainer.Clear();
            }
        }
        
        /// <summary>
        /// Set the selected object type filter
        /// </summary>
        public void SetTypeFilter(SceneObjectType objectType)
        {
            _currentTypeFilter = objectType;
            
            if (_typeFilter != null)
            {
                string filterValue = objectType == (SceneObjectType)(-1) ? "All Types" : objectType.ToString();
                _typeFilter.SetValueWithoutNotify(filterValue);
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
                _categoryFilter.SetValueWithoutNotify(category);
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
                _searchField.SetValueWithoutNotify(searchText);
            }
            
            RefreshPalette();
        }
        
        /// <summary>
        /// Select a specific palette item
        /// </summary>
        public void SelectItem(ObjectPaletteItemUIToolkit item)
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
            
            var categories = new List<string> { "All" };
            categories.AddRange(_objectLibrary.GetCategories());
            
            _categoryFilter.choices = categories;
            if (!categories.Contains(_currentCategoryFilter))
            {
                _currentCategoryFilter = "All";
                _categoryFilter.value = "All";
            }
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
            if (_itemsContainer == null) return;
            
            foreach (var objectData in objects)
            {
                CreatePaletteItem(objectData);
            }
        }
        
        private void CreatePaletteItem(SceneObjectData objectData)
        {
            var paletteItem = new ObjectPaletteItemUIToolkit();
            paletteItem.Initialize(objectData, this, _itemTemplate, _itemSize);
            
            // Bind events
            paletteItem.OnItemSelected += SelectItem;
            paletteItem.OnItemDragStarted += OnItemDragStarted;
            paletteItem.OnItemDragMoved += OnItemDragMoved;
            paletteItem.OnItemDragEnded += OnItemDragEnded;
            
            // Add to container
            _itemsContainer.Add(paletteItem.RootElement);
            _paletteItems.Add(paletteItem);
        }
        
        #region Event Handlers
        
        private void OnTypeFilterChanged(string value)
        {
            if (value == "All Types")
            {
                _currentTypeFilter = (SceneObjectType)(-1);
            }
            else if (System.Enum.TryParse<SceneObjectType>(value, out var type))
            {
                _currentTypeFilter = type;
            }
            
            RefreshPalette();
        }
        
        private void OnCategoryFilterChanged(string value)
        {
            _currentCategoryFilter = value;
            RefreshPalette();
        }
        
        private void OnSearchTextChanged(string searchText)
        {
            _currentSearchText = searchText;
            RefreshPalette();
        }
        
        private void OnItemDragStarted(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
        {
            // Optional: Provide feedback when dragging starts
        }
        
        private void OnItemDragMoved(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
        {
            // Optional: Update drag feedback
        }
        
        private void OnItemDragEnded(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
        {
            // Check if dropped on scene area
            if (IsScreenPositionOverScene(screenPosition))
            {
                Vector3 worldPosition = ScreenToWorldPosition(screenPosition);
                
                if (_sandboxBuilder != null)
                {
                    _sandboxBuilder.PlaceObject(item.ObjectData.id, worldPosition);
                }
                
                OnObjectDraggedToScene?.Invoke(item.ObjectData, screenPosition);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool IsScreenPositionOverScene(Vector2 screenPosition)
        {
            Camera sceneCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (sceneCamera == null) return false;
            
            Vector2 viewportPosition = sceneCamera.ScreenToViewportPoint(screenPosition);
            
            return viewportPosition.x >= 0 && viewportPosition.x <= 1 &&
                   viewportPosition.y >= 0 && viewportPosition.y <= 1;
        }
        
        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            Camera sceneCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (sceneCamera == null) return Vector3.zero;
            
            Ray ray = sceneCamera.ScreenPointToRay(screenPosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            return ray.GetPoint(10f);
        }
        
        #endregion
    }
}
