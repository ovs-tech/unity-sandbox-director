using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SceneSandbox.Data;
using SceneSandbox.Core;

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
        [SerializeField] private Dropdown _typeFilter;
        [SerializeField] private Dropdown _categoryFilter;
        [SerializeField] private InputField _searchField;
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
            CreateUIIfMissing();
            SetupUI();
        }
        
        private void Start()
        {
            if (_objectLibrary != null)
            {
                RefreshPalette();
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
                Debug.LogWarning("[ObjectPalette] No item container found, items cannot be created");
                return;
            }
            
            // Ensure we have an item prefab (optional - we can create without it)
            if (_itemPrefab == null)
            {
                Debug.Log("[ObjectPalette] No item prefab found, creating items directly");
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
            Debug.Log($"Creating palette item for: {objectData.displayName}");
            
            ObjectPaletteItem paletteItem = null;
            
            if (_itemPrefab != null)
            {
                // Use prefab if available
                GameObject itemGO = Instantiate(_itemPrefab, _itemContainer);
                itemGO.SetActive(true); // Ensure it's active
                
                paletteItem = itemGO.GetComponent<ObjectPaletteItem>();
                
                // If no ObjectPaletteItem component exists, add one
                if (paletteItem == null)
                {
                    paletteItem = itemGO.AddComponent<ObjectPaletteItem>();
                }
            }
            else
            {
                // Create directly using factory method
                paletteItem = ObjectPaletteItem.CreatePaletteItem(_itemContainer, objectData, this);
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
                    _sandboxBuilder.PlaceObject(item.ObjectData.id, worldPosition);
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
        
        #region UI Creation
        
        /// <summary>
        /// Creates UI elements programmatically if they're not assigned in the inspector
        /// </summary>
        private void CreateUIIfMissing()
        {
            if (_itemContainer == null || _typeFilter == null || _categoryFilter == null)
            {
                CreatePaletteUI();
            }
        }
        
        private void CreatePaletteUI()
        {
            // Setup the main container as a panel
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            // Add background
            var backgroundImage = GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            }
            
            // Set default size and position (left side of screen)
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.sizeDelta = new Vector2(250, 0);
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Add main layout
            var mainLayout = gameObject.GetComponent<VerticalLayoutGroup>();
            if (mainLayout == null)
            {
                mainLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            }
            mainLayout.padding = new RectOffset(10, 10, 10, 10);
            mainLayout.spacing = 10;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childControlHeight = false;
            
            // Create header section
            CreateHeaderSection();
            
            // Create item container
            CreateItemContainer();
        }
        
        private void CreateHeaderSection()
        {
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(transform, false);
            
            var headerLayout = headerGO.AddComponent<VerticalLayoutGroup>();
            headerLayout.spacing = 5;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childControlHeight = false;
            
            // Title
            CreateText(headerGO.transform, "OBJECT PALETTE", 14, FontStyle.Bold);
            
            // Type filter
            var typeFilterRow = new GameObject("Type Filter Row");
            typeFilterRow.transform.SetParent(headerGO.transform, false);
            var typeLayout = typeFilterRow.AddComponent<HorizontalLayoutGroup>();
            typeLayout.spacing = 5;
            
            CreateText(typeFilterRow.transform, "Type:", 10);
            _typeFilter = CreateDropdown(typeFilterRow.transform, new string[] { "All Types", "Actor", "Prop", "Camera", "Light" });
            
            // Category filter
            var categoryFilterRow = new GameObject("Category Filter Row");
            categoryFilterRow.transform.SetParent(headerGO.transform, false);
            var categoryLayout = categoryFilterRow.AddComponent<HorizontalLayoutGroup>();
            categoryLayout.spacing = 5;
            
            CreateText(categoryFilterRow.transform, "Category:", 10);
            _categoryFilter = CreateDropdown(categoryFilterRow.transform, new string[] { "All" });
            
            // Search field
            var searchRow = new GameObject("Search Row");
            searchRow.transform.SetParent(headerGO.transform, false);
            var searchLayout = searchRow.AddComponent<HorizontalLayoutGroup>();
            searchLayout.spacing = 5;
            
            CreateText(searchRow.transform, "Search:", 10);
            _searchField = CreateInputField(searchRow.transform, "Search objects...");
            
            // Refresh button
            _refreshButton = CreateButton(headerGO.transform, "Refresh");
        }
        
        private void CreateItemContainer()
        {
            var containerGO = new GameObject("Item Container");
            containerGO.transform.SetParent(transform, false);
            
            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 300); // Fixed height for scrolling
            
            // Add scroll view
            var scrollRect = containerGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            // Create viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(containerGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            var viewportMask = viewportGO.AddComponent<Mask>();
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = Color.clear;
            
            // Create content area (this will be our item container)
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            // Add grid layout for items
            var gridLayout = contentGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = _itemSize;
            gridLayout.spacing = _itemSpacing;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = _itemsPerRow;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            
            // Add content size fitter for scrolling
            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            
            // Assign references
            _itemContainer = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            
            // Create default item prefab if missing
            if (_itemPrefab == null)
            {
                _itemPrefab = CreateDefaultItemPrefab();
            }
        }
        
        private GameObject CreateDefaultItemPrefab()
        {
            var prefabGO = new GameObject("DefaultObjectPaletteItem");
            prefabGO.SetActive(false); // Keep as prefab
            
            var rectTransform = prefabGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = _itemSize;
            
            // Add background
            var backgroundImage = prefabGO.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Add ObjectPaletteItem component
            var paletteItem = prefabGO.AddComponent<ObjectPaletteItem>();
            
            // Create icon area
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(prefabGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.3f);
            iconRect.anchorMax = new Vector2(1, 1);
            iconRect.offsetMin = new Vector2(5, 5);
            iconRect.offsetMax = new Vector2(-5, -5);
            
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white;
            
            // Create label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(prefabGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0.3f);
            labelRect.offsetMin = new Vector2(2, 2);
            labelRect.offsetMax = new Vector2(-2, -2);
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = "Item";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 10;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            
            // Assign references to ObjectPaletteItem via reflection or public setters
            // This is a simplified approach - in a real implementation you'd want proper initialization
            
            return prefabGO;
        }
        
        #endregion
        
        #region UI Helpers
        
        private Text CreateText(Transform parent, string text, int fontSize = 12, FontStyle style = FontStyle.Normal)
        {
            var textGO = new GameObject($"Text_{text.Replace(" ", "").Substring(0, Mathf.Min(10, text.Length))}");
            textGO.transform.SetParent(parent, false);
            
            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;
            
            var layoutElement = textGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = fontSize + 4;
            layoutElement.flexibleWidth = 0;
            
            return textComponent;
        }
        
        private Dropdown CreateDropdown(Transform parent, string[] options)
        {
            var dropdownGO = new GameObject("Dropdown");
            dropdownGO.transform.SetParent(parent, false);
            
            var dropdownRect = dropdownGO.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(120, 25);
            
            var dropdownImage = dropdownGO.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            var dropdown = dropdownGO.AddComponent<Dropdown>();
            
            // Create label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(dropdownGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-25, 0);
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            dropdown.captionText = labelText;
            
            // Add options
            dropdown.options.Clear();
            foreach (var option in options)
            {
                dropdown.options.Add(new Dropdown.OptionData(option));
            }
            
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            
            return dropdown;
        }
        
        private InputField CreateInputField(Transform parent, string placeholder)
        {
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(parent, false);
            
            var inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(120, 25);
            
            var inputImage = inputGO.AddComponent<Image>();
            inputImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            var inputField = inputGO.AddComponent<InputField>();
            
            // Create text component
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            
            var textComponent = textGO.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;
            
            inputField.textComponent = textComponent;
            
            // Create placeholder
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputGO.transform, false);
            var placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(5, 0);
            placeholderRect.offsetMax = new Vector2(-5, 0);
            
            var placeholderText = placeholderGO.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            
            inputField.placeholder = placeholderText;
            
            return inputField;
        }
        
        private Button CreateButton(Transform parent, string text)
        {
            var buttonGO = new GameObject($"Button_{text.Replace(" ", "")}");
            buttonGO.transform.SetParent(parent, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(100, 25);
            
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            var button = buttonGO.AddComponent<Button>();
            
            // Add text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 10;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            return button;
        }
        
        #endregion
    }
}