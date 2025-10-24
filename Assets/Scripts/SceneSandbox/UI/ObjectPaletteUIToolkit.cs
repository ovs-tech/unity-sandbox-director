using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
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
        [SerializeField] private InputActionAsset _inputActions;
        
        // Visual Elements
        private VisualElement _rootElement;
        private VisualElement _paletteContainer;
        private DropdownField _typeFilter;
        private DropdownField _categoryFilter;
        private TextField _searchField;
        private Button _refreshButton;
        private ScrollView _itemsScrollView;
        private VisualElement _itemsContainer;
        
        // Input Actions
        private InputAction _pointAction;
        
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
            Debug.Log("ObjectPaletteUIToolkit: Awake() called");
            InitializeInputActions();
            InitializeUI();
            SetupUI();
            Debug.Log("ObjectPaletteUIToolkit: Awake() completed");
        }
        
        private void InitializeInputActions()
        {
            if (_inputActions != null)
            {
                var uiMap = _inputActions.FindActionMap("UI");
                if (uiMap != null)
                {
                    _pointAction = uiMap.FindAction("Point");
                    if (_pointAction != null)
                    {
                        Debug.Log("ObjectPaletteUIToolkit: Found UI/Point action");
                    }
                    else
                    {
                        Debug.LogWarning("ObjectPaletteUIToolkit: UI/Point action not found");
                    }
                }
            }
            else
            {
                Debug.LogWarning("ObjectPaletteUIToolkit: InputActions not assigned, items will use fallback");
            }
        }
        
        private void Start()
        {
            if (_objectLibrary != null)
            {
                RefreshPalette();
            }
            else
            {
                Debug.LogWarning("ObjectPaletteUIToolkit: No object library assigned. Palette will be empty until library is set.");
            }
        }
        
        private void OnDestroy()
        {
            // Clean up all palette items
            ClearPalette();
            
            // Unregister event callbacks
            if (_typeFilter != null)
            {
                _typeFilter.UnregisterValueChangedCallback(evt => OnTypeFilterChanged(evt.newValue));
            }
            
            if (_categoryFilter != null)
            {
                _categoryFilter.UnregisterValueChangedCallback(evt => OnCategoryFilterChanged(evt.newValue));
            }
            
            if (_searchField != null)
            {
                _searchField.UnregisterValueChangedCallback(evt => OnSearchTextChanged(evt.newValue));
            }
            
            if (_refreshButton != null)
            {
                _refreshButton.clicked -= RefreshPalette;
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
                    Debug.Log("ObjectPaletteUIToolkit: UIDocument component was missing, created new one.");
                }
            }
            
            // Create root element
            if (_paletteTemplate != null)
            {
                _paletteTemplate.CloneTree(_uiDocument.rootVisualElement);
                Debug.Log("ObjectPaletteUIToolkit: UI created from template.");
            }
            else
            {
                CreatePaletteFromCode();
                Debug.Log("ObjectPaletteUIToolkit: UI created from code (no template assigned).");
            }
            
            _rootElement = _uiDocument.rootVisualElement;
            
            if (_rootElement == null)
            {
                Debug.LogError("ObjectPaletteUIToolkit: Failed to create root visual element!");
                return;
            }
            
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
            Debug.Log("[FILTER] QueryUIElements() - Searching for UI elements...");
            
            _paletteContainer = _rootElement.Q<VisualElement>("palette-container");
            Debug.Log($"[FILTER] palette-container: {(_paletteContainer != null ? "Found" : "NOT FOUND")}");
            
            _typeFilter = _rootElement.Q<DropdownField>("type-filter");
            Debug.Log($"[FILTER] type-filter: {(_typeFilter != null ? "Found" : "NOT FOUND")}");
            
            _categoryFilter = _rootElement.Q<DropdownField>("category-filter");
            Debug.Log($"[FILTER] category-filter: {(_categoryFilter != null ? "Found" : "NOT FOUND")}");
            
            _searchField = _rootElement.Q<TextField>("search-field");
            Debug.Log($"[FILTER] search-field: {(_searchField != null ? "Found" : "NOT FOUND")}");
            
            _refreshButton = _rootElement.Q<Button>("refresh-button");
            Debug.Log($"[FILTER] refresh-button: {(_refreshButton != null ? "Found" : "NOT FOUND")}");
            
            _itemsScrollView = _rootElement.Q<ScrollView>("items-scroll-view");
            Debug.Log($"[FILTER] items-scroll-view: {(_itemsScrollView != null ? "Found" : "NOT FOUND")}");
            
            _itemsContainer = _rootElement.Q<VisualElement>("items-container");
            Debug.Log($"[FILTER] items-container: {(_itemsContainer != null ? "Found" : "NOT FOUND")}");
        }
        
        private void SetupUI()
        {
            Debug.Log("[FILTER] SetupUI() - Registering event callbacks...");
            
            // Setup type filter
            if (_typeFilter != null)
            {
                _typeFilter.RegisterValueChangedCallback(evt => OnTypeFilterChanged(evt.newValue));
                Debug.Log("[FILTER] ✅ Type filter callback registered");
            }
            else
            {
                Debug.LogWarning("[FILTER] ❌ Type filter not found in UI - cannot register callback!");
            }
            
            // Setup category filter
            if (_categoryFilter != null)
            {
                _categoryFilter.RegisterValueChangedCallback(evt => OnCategoryFilterChanged(evt.newValue));
                Debug.Log("[FILTER] ✅ Category filter callback registered");
            }
            else
            {
                Debug.LogWarning("[FILTER] ❌ Category filter not found in UI - cannot register callback!");
            }
            
            // Setup search field
            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt => OnSearchTextChanged(evt.newValue));
                Debug.Log("[FILTER] ✅ Search field callback registered");
            }
            else
            {
                Debug.LogWarning("[FILTER] ❌ Search field not found in UI - cannot register callback!");
            }
            
            // Setup refresh button (for UXML-defined buttons)
            if (_refreshButton != null)
            {
                _refreshButton.clicked += RefreshPalette;
                Debug.Log("[FILTER] ✅ Refresh button callback registered");
            }
            else
            {
                Debug.LogWarning("[FILTER] ❌ Refresh button not found in UI - cannot register callback!");
            }
            
            Debug.Log("[FILTER] SetupUI() completed");
        }
        
        /// <summary>
        /// Refresh the palette with objects from the library
        /// </summary>
        public void RefreshPalette()
        {
            Debug.Log($"[FILTER] RefreshPalette() called - Type: {_currentTypeFilter}, Category: '{_currentCategoryFilter}', Search: '{_currentSearchText}'");
            
            if (_objectLibrary == null)
            {
                Debug.LogWarning("ObjectPaletteUIToolkit: Cannot refresh palette - object library is null.");
                return;
            }
            
            ClearPalette();
            UpdateCategoryFilter();
            
            var filteredObjects = GetFilteredObjects();
            
            Debug.Log($"[FILTER] Found {filteredObjects.Count} objects after filtering");
            
            if (filteredObjects.Count == 0)
            {
                Debug.LogWarning($"[FILTER] No objects match current filters (Type: {_currentTypeFilter}, Category: {_currentCategoryFilter}, Search: '{_currentSearchText}')");
            }
            else
            {
                Debug.Log($"[FILTER] Objects to display: {string.Join(", ", filteredObjects.Select(o => o.displayName))}");
            }
            
            CreatePaletteItems(filteredObjects);
            Debug.Log($"[FILTER] RefreshPalette() completed - Created {_paletteItems.Count} palette items");
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
            if (_categoryFilter == null)
            {
                Debug.LogWarning("ObjectPaletteUIToolkit: Category filter is null, cannot update categories.");
                return;
            }
            
            if (_objectLibrary == null)
            {
                Debug.LogWarning("ObjectPaletteUIToolkit: Object library is null, cannot update categories.");
                return;
            }
            
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
            Debug.Log($"[FILTER] Total objects in library: {allObjects.Count}");
            
            var filteredObjects = allObjects.AsEnumerable();
            
            // Filter by type
            if (_currentTypeFilter != (SceneObjectType)(-1))
            {
                int beforeCount = filteredObjects.Count();
                filteredObjects = filteredObjects.Where(obj => obj.objectType == _currentTypeFilter);
                Debug.Log($"[FILTER] After type filter ({_currentTypeFilter}): {beforeCount} -> {filteredObjects.Count()}");
            }
            
            // Filter by category
            if (_currentCategoryFilter != "All")
            {
                int beforeCount = filteredObjects.Count();
                filteredObjects = filteredObjects.Where(obj => obj.category == _currentCategoryFilter);
                Debug.Log($"[FILTER] After category filter ('{_currentCategoryFilter}'): {beforeCount} -> {filteredObjects.Count()}");
            }
            
            // Filter by search text
            if (!string.IsNullOrEmpty(_currentSearchText))
            {
                int beforeCount = filteredObjects.Count();
                string searchLower = _currentSearchText.ToLower();
                filteredObjects = filteredObjects.Where(obj => 
                    obj.displayName.ToLower().Contains(searchLower) ||
                    (obj.tags != null && obj.tags.Any(tag => tag.ToLower().Contains(searchLower))));
                Debug.Log($"[FILTER] After search filter ('{_currentSearchText}'): {beforeCount} -> {filteredObjects.Count()}");
            }
            
            return filteredObjects.ToList();
        }
        
        private void CreatePaletteItems(List<SceneObjectData> objects)
        {
            if (_itemsContainer == null) 
            {
                Debug.LogWarning("ObjectPaletteUIToolkit: Items container is null, cannot create palette items.");
                return;
            }
            
            foreach (var objectData in objects)
            {
                CreatePaletteItem(objectData);
            }
            
            // Update layout after all items are created
            UpdateLayout();
        }
        
        private void CreatePaletteItem(SceneObjectData objectData)
        {
            if (objectData == null)
            {
                Debug.LogWarning("ObjectPaletteUIToolkit: Attempted to create palette item with null object data.");
                return;
            }
            
            // Create palette item
            var paletteItem = new ObjectPaletteItemUIToolkit();
            
            try
            {
                paletteItem.Initialize(objectData, this, _itemTemplate, _itemSize);
                
                // Pass InputAction if available
                if (_pointAction != null)
                {
                    paletteItem.SetPointAction(_pointAction);
                }
                
                // Verify initialization succeeded
                if (paletteItem.RootElement == null)
                {
                    Debug.LogError($"ObjectPaletteUIToolkit: Failed to initialize palette item for '{objectData.displayName}' - RootElement is null.");
                    return;
                }
                
                // Bind events
                paletteItem.OnItemSelected += SelectItem;
                paletteItem.OnItemDragStarted += OnItemDragStarted;
                paletteItem.OnItemDragMoved += OnItemDragMoved;
                paletteItem.OnItemDragEnded += OnItemDragEnded;
                
                // Add to container
                _itemsContainer.Add(paletteItem.RootElement);
                _paletteItems.Add(paletteItem);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ObjectPaletteUIToolkit: Exception while creating palette item for '{objectData.displayName}': {ex.Message}");
                paletteItem?.Dispose();
            }
        }
        
        private void UpdateLayout()
        {
            if (_itemsContainer == null) return;
            
            // Configure flexbox layout for items container
            _itemsContainer.style.flexDirection = FlexDirection.Row;
            _itemsContainer.style.flexWrap = Wrap.Wrap;
            _itemsContainer.style.justifyContent = Justify.FlexStart;
            _itemsContainer.style.alignItems = Align.FlexStart;
            _itemsContainer.style.paddingLeft = 4;
            _itemsContainer.style.paddingRight = 4;
            _itemsContainer.style.paddingTop = 4;
            _itemsContainer.style.paddingBottom = 4;
            
            // Force layout update
            _itemsContainer.MarkDirtyRepaint();
        }
        
        #region Event Handlers
        
        private void OnTypeFilterChanged(string value)
        {
            Debug.Log($"[FILTER] Type filter changed to: '{value}'");
            
            if (value == "All Types")
            {
                _currentTypeFilter = (SceneObjectType)(-1);
            }
            else if (System.Enum.TryParse<SceneObjectType>(value, out var type))
            {
                _currentTypeFilter = type;
                Debug.Log($"[FILTER] Parsed type: {type}");
            }
            else
            {
                Debug.LogWarning($"[FILTER] Failed to parse type: '{value}'");
            }
            
            RefreshPalette();
        }
        
        private void OnCategoryFilterChanged(string value)
        {
            Debug.Log($"[FILTER] Category filter changed to: '{value}'");
            _currentCategoryFilter = value;
            RefreshPalette();
        }
        
        private void OnSearchTextChanged(string searchText)
        {
            Debug.Log($"[FILTER] Search text changed to: '{searchText}'");
            _currentSearchText = searchText;
            RefreshPalette();
        }
        
        private void OnItemDragStarted(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
        {
            Debug.Log($"[DRAG] Drag started for '{item.ObjectData.displayName}' at screen position {screenPosition}");
            
            // Check if sandbox builder is assigned
            if (_sandboxBuilder == null)
            {
                Debug.LogError("[DRAG] ❌ SandboxBuilder is NULL! Cannot start drag.");
                return;
            }
            
            // Delegate to SceneSandboxBuilder to handle indicator and world position conversion
            _sandboxBuilder.HandleDragFromUI(item.ObjectData.id, screenPosition);
        }
        
        private void OnItemDragMoved(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
        {
            // Check if sandbox builder is assigned
            if (_sandboxBuilder == null) return;
            
            // Delegate to SceneSandboxBuilder to update indicator position
            _sandboxBuilder.HandleDragMoveFromUI(screenPosition);
        }
        
        private void OnItemDragEnded(ObjectPaletteItemUIToolkit item, Vector2 screenPosition)
        {
            Debug.Log($"[DRAG] ========== DRAG ENDED ==========");
            Debug.Log($"[DRAG] Object: '{item.ObjectData.displayName}' (ID: {item.ObjectData.id})");
            Debug.Log($"[DRAG] Screen position: {screenPosition}");
            
            // Check if sandbox builder is assigned
            if (_sandboxBuilder == null)
            {
                Debug.LogError("[DRAG] ❌ SandboxBuilder is NULL! Cannot place object.");
                return;
            }
            
            // Check if we're over the scene (not over UI)
            bool isOverScene = IsScreenPositionOverScene(screenPosition);
            
            Debug.Log($"[DRAG] Is over scene viewport? {isOverScene}");
            
            // Only place if over scene area
            if (!isOverScene)
            {
                Debug.LogWarning($"[DRAG] ❌ Cannot place - not over scene area");
                _sandboxBuilder.CancelDragFromUI();
                return;
            }
            
            // Delegate to SceneSandboxBuilder to handle placement
            // SceneSandboxBuilder will convert screen → world and place the object
            Debug.Log($"[DRAG] ✅ Requesting placement from SceneSandboxBuilder...");
            try
            {
                GameObject placedObject = _sandboxBuilder.HandleDropFromUI(item.ObjectData.id, screenPosition);
                
                if (placedObject != null)
                {
                    Debug.Log($"[DRAG] ✅ Successfully placed '{item.ObjectData.displayName}' at {placedObject.transform.position}");
                    OnObjectDraggedToScene?.Invoke(item.ObjectData, screenPosition);
                }
                else
                {
                    Debug.LogWarning($"[DRAG] ⚠️ Placement returned null (possibly out of bounds)");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DRAG] ❌ Exception during placement: {ex.Message}");
                Debug.LogError($"[DRAG] Stack trace: {ex.StackTrace}");
            }
            
            Debug.Log($"[DRAG] ========== END DRAG ==========");
        }
        
        #endregion
        
        #region Helper Methods
        
        private Vector2 PanelToScreenPosition(Vector2 panelPosition)
        {
            // UI Toolkit panel coordinates need to be converted to screen coordinates
            // Panel origin (0,0) is typically top-left of the panel
            // Screen origin (0,0) is bottom-left of the screen
            
            if (_rootElement == null || _rootElement.panel == null)
            {
                Debug.LogWarning("[DRAG] Cannot convert position - root element or panel is null");
                return panelPosition;
            }
            
            Debug.Log($"[DRAG] Input panel position: {panelPosition}");
            
            // IMPORTANT: Panel position can be negative for elements in left/top panels
            // We need to convert from panel-space to absolute screen-space
            
            // Get screen dimensions
            float screenHeight = Screen.height;
            
            // Panel coordinates are already in pixel space, just need Y-flip for Unity screen coords
            // Unity screen space: (0,0) = bottom-left, (width, height) = top-right
            // Panel space: (0,0) = top-left, (width, height) = bottom-right
            
            // Handle negative panel positions (elements outside main viewport)
            // For left panels, panelPosition.x will be negative
            // We need to convert this to positive screen coordinates
            
            Vector2 screenPos;
            if (panelPosition.x < 0 || panelPosition.y < 0)
            {
                // Element is in a side panel or outside main area
                // Need to calculate absolute position from panel root
                if (_rootElement.panel.visualTree != null)
                {
                    var panelRoot = _rootElement.panel.visualTree;
                    // panelPosition is already in absolute coordinates from the panel root
                    screenPos = new Vector2(
                        panelPosition.x,  // Keep X as-is (absolute from left)
                        screenHeight - panelPosition.y  // Flip Y
                    );
                }
                else
                {
                    screenPos = new Vector2(panelPosition.x, screenHeight - panelPosition.y);
                }
            }
            else
            {
                // Standard conversion for positive coordinates
                screenPos = new Vector2(panelPosition.x, screenHeight - panelPosition.y);
            }
            
            Debug.Log($"[DRAG] Coordinate conversion: Panel({panelPosition.x}, {panelPosition.y}) -> Screen({screenPos.x}, {screenPos.y}) [Screen: {Screen.width}x{screenHeight}]");
            
            return screenPos;
        }
        
        private bool IsPositionOverUI(Vector2 panelPosition)
        {
            if (_rootElement == null || _rootElement.panel == null)
            {
                return false;
            }
            
            // Check if position is over any UI element (except the palette items themselves)
            var pickedElement = _rootElement.panel.Pick(panelPosition);
            
            if (pickedElement == null)
            {
                Debug.Log($"[DRAG] Position {panelPosition} is NOT over any UI element");
                return false;
            }
            
            // Check if picked element is part of the palette items container
            // If it's a palette item, we consider it "not over UI" for placement purposes
            bool isOverPaletteItems = IsElementInHierarchy(pickedElement, _itemsContainer);
            
            if (isOverPaletteItems)
            {
                Debug.Log($"[DRAG] Position is over palette items (allowed for dragging)");
                return false; // Allow drag from palette
            }
            
            // Position is over some other UI element
            Debug.Log($"[DRAG] Position is over UI element: {pickedElement.name} (blocking placement)");
            return true;
        }
        
        private bool IsElementInHierarchy(VisualElement element, VisualElement container)
        {
            if (element == null || container == null)
                return false;
            
            var current = element;
            while (current != null)
            {
                if (current == container)
                    return true;
                current = current.parent;
            }
            
            return false;
        }
        
        private bool IsScreenPositionOverScene(Vector2 screenPosition)
        {
            Camera sceneCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (sceneCamera == null)
            {
                Debug.LogWarning("[DRAG] No camera found for scene detection");
                return false;
            }
            
            Debug.Log($"[DRAG] Using camera: {sceneCamera.name}");
            
            Vector2 viewportPosition = sceneCamera.ScreenToViewportPoint(screenPosition);
            
            Debug.Log($"[DRAG] Viewport position: {viewportPosition} (valid range: 0-1)");
            
            bool isOverScene = viewportPosition.x >= 0 && viewportPosition.x <= 1 &&
                               viewportPosition.y >= 0 && viewportPosition.y <= 1;
            
            return isOverScene;
        }
        
        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            Camera sceneCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (sceneCamera == null)
            {
                Debug.LogWarning("[DRAG] No camera found for world position conversion");
                return Vector3.zero;
            }
            
            Ray ray = sceneCamera.ScreenPointToRay(screenPosition);
            Debug.Log($"[DRAG] Ray origin: {ray.origin}, direction: {ray.direction}");
            
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                Debug.Log($"[DRAG] ✅ Raycast hit ground at distance {distance}, world position: {worldPos}");
                return worldPos;
            }
            
            // Fallback to a position in front of the camera
            Vector3 fallbackPos = ray.GetPoint(10f);
            Debug.LogWarning($"[DRAG] ⚠️ Ground plane raycast MISSED, using fallback position {fallbackPos}");
            return fallbackPos;
        }
        
        #endregion
    }
}
