using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;
using Systems.SceneSandbox.Data;

namespace Systems.SceneSandbox.UI.SceneObjectLibrary
{
    /// <summary>
    /// View for the Scene Object Library UI.
    /// Renders UI elements and binds them to the ViewModel.
    /// Emits events when user interactions occur.
    /// </summary>
    public class SceneObjectLibraryView : MonoBehaviour
    {
        /// <summary>
        /// Fired when a card is clicked (object selected).
        /// </summary>
        public event Action<SceneObjectData> OnCardClicked;
        
        /// <summary>
        /// Fired when type filter changes.
        /// </summary>
        public event Action<SceneObjectType?> OnTypeFilterChanged;
        
        /// <summary>
        /// Fired when category filter changes.
        /// </summary>
        public event Action<string> OnCategoryFilterChanged;
        
        /// <summary>
        /// Fired when search query changes.
        /// </summary>
        public event Action<string> OnSearchQueryChanged;
        
        /// <summary>
        /// Fired when tag filter changes.
        /// </summary>
        public event Action<string> OnTagFilterChanged;
        
        private UIDocument _document;
        private VisualElement _root;
        private SceneObjectLibraryViewModel _viewModel;
        
        // UI Element References
        private Button _typeFilterAllButton;
        private Button _typeFilterActorButton;
        private Button _typeFilterPropButton;
        private Button _typeFilterCameraButton;
        private Button _typeFilterLightButton;
        
        private DropdownField _categoryFilterDropdown;
        private DropdownField _tagFilterDropdown;
        private TextField _searchField;
        private ScrollView _objectGridScrollView;
        private VisualElement _objectGridContainer;
        private Label _emptyStateLabel;
        
        // Cached grid container
        private VisualElement _grid;

        private VisualTreeAsset _objectCardTemplate;
        
        // Card pooling
        private Queue<VisualElement> _cardPool = new Queue<VisualElement>();
        private Dictionary<SceneObjectData, VisualElement> _visibleCards = new Dictionary<SceneObjectData, VisualElement>();
        
        // Search debouncing
        private Coroutine _searchDebounceCoroutine;
        
        private SceneObjectType? _currentTypeFilter;
        
        /// <summary>
        /// Initialize the View with a ViewModel and UIDocument.
        /// </summary>
        public IEnumerator InitializeView(SceneObjectLibraryViewModel viewModel, UIDocument document)
        {
            _viewModel = viewModel;
            _document = document;
            
            if (_document == null)
            {
                Debug.LogError("UIDocument is null in SceneObjectLibraryView!");
                yield break;
            }
            
            _root = _document.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("Root VisualElement is null!");
                yield break;
            }
            
            // Create UI structure (only if not already created)
            CreateUIStructure();
            
            // Bind to ViewModel
            BindToViewModel();
            
            // Initial render
            RenderObjectGrid(_viewModel.FilteredObjects.Value);
            
            yield return null;
        }
        
        /// <summary>
        /// Create the UI structure programmatically.
        /// Only creates elements if they don't already exist.
        /// </summary>
        private void CreateUIStructure()
        {
            // Check if main container already exists
            var mainContainer = _root.Q<VisualElement>("MainContainer");
            if (mainContainer == null)
            {
                // Main container doesn't exist, create it
                mainContainer = new VisualElement();
                mainContainer.name = "MainContainer";
                mainContainer.style.flexDirection = FlexDirection.Column;
                mainContainer.style.flexGrow = 1;
                _root.Add(mainContainer);
            }
            
            // Check if filter bar already exists
            var filterBar = mainContainer.Q<VisualElement>("FilterBar");
            if (filterBar == null)
            {
                // Filter bar doesn't exist, create it
                filterBar = CreateFilterBar();
                mainContainer.Add(filterBar);
            }
            else
            {
                // Filter bar exists, cache references to its elements
                CacheFilterBarReferences(filterBar);
            }
            
            // Check if object grid scroll view already exists
            _objectGridScrollView = mainContainer.Q<ScrollView>("ObjectGridScrollView");
            if (_objectGridScrollView == null)
            {
                // Object grid doesn't exist, create it
                _objectGridScrollView = new ScrollView(ScrollViewMode.Vertical);
                _objectGridScrollView.name = "ObjectGridScrollView";
                _objectGridScrollView.style.flexGrow = 1;
                mainContainer.Add(_objectGridScrollView);
            }
            
            // Check if object grid container already exists
            _objectGridContainer = _objectGridScrollView.Q<VisualElement>("ObjectGridContainer");
            if (_objectGridContainer == null)
            {
                // Container doesn't exist, create it
                _objectGridContainer = new VisualElement();
                _objectGridContainer.name = "ObjectGridContainer";
                _objectGridContainer.style.display = DisplayStyle.Flex;
                _objectGridScrollView.Add(_objectGridContainer);
            }
            
            // Check if empty state label already exists
            _emptyStateLabel = _objectGridContainer.Q<Label>("EmptyStateLabel");
            if (_emptyStateLabel == null)
            {
                // Empty state label doesn't exist, create it
                _emptyStateLabel = new Label("No objects available");
                _emptyStateLabel.name = "EmptyStateLabel";
                _emptyStateLabel.style.display = DisplayStyle.None;
                _objectGridContainer.Add(_emptyStateLabel);
            }

            // Initialize Grid
            _grid = _objectGridContainer.Q<VisualElement>("Grid");
            if (_grid == null)
            {
                _grid = new VisualElement();
                _grid.name = "Grid";
                _grid.style.display = DisplayStyle.Flex;
                _grid.style.flexWrap = Wrap.Wrap;
                _grid.style.paddingLeft = 5;
                _grid.style.paddingRight = 5;
                _grid.style.paddingTop = 5;
                _grid.style.paddingBottom = 5;
                _objectGridContainer.Add(_grid);
            }
        }
        
        /// <summary>
        /// Create the filter bar UI.
        /// </summary>
        private VisualElement CreateFilterBar()
        {
            var filterBar = new VisualElement();
            filterBar.name = "FilterBar";
            filterBar.style.flexDirection = FlexDirection.Row;
            filterBar.style.marginBottom = 10;
            filterBar.style.paddingLeft = 5;
            filterBar.style.paddingRight = 5;
            filterBar.style.paddingTop = 5;
            filterBar.style.paddingBottom = 5;
            filterBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Type filter buttons
            var typeFilterContainer = new VisualElement();
            typeFilterContainer.name = "TypeFilterContainer";
            typeFilterContainer.style.flexDirection = FlexDirection.Row;
            typeFilterContainer.style.marginRight = 10;
            
            _typeFilterAllButton = CreateTypeFilterButton("All", null);
            _typeFilterAllButton.name = "TypeFilterAllButton";
            _typeFilterAllButton.style.marginRight = 5;
            typeFilterContainer.Add(_typeFilterAllButton);
            
            _typeFilterActorButton = CreateTypeFilterButton("Actor", SceneObjectType.Actor);
            _typeFilterActorButton.name = "TypeFilterActorButton";
            _typeFilterActorButton.style.marginRight = 5;
            typeFilterContainer.Add(_typeFilterActorButton);
            
            _typeFilterPropButton = CreateTypeFilterButton("Prop", SceneObjectType.Prop);
            _typeFilterPropButton.name = "TypeFilterPropButton";
            _typeFilterPropButton.style.marginRight = 5;
            typeFilterContainer.Add(_typeFilterPropButton);
            
            _typeFilterCameraButton = CreateTypeFilterButton("Camera", SceneObjectType.Camera);
            _typeFilterCameraButton.name = "TypeFilterCameraButton";
            _typeFilterCameraButton.style.marginRight = 5;
            typeFilterContainer.Add(_typeFilterCameraButton);
            
            _typeFilterLightButton = CreateTypeFilterButton("Light", SceneObjectType.Light);
            _typeFilterLightButton.name = "TypeFilterLightButton";
            typeFilterContainer.Add(_typeFilterLightButton);
            
            filterBar.Add(typeFilterContainer);
            
            // Category dropdown
            _categoryFilterDropdown = new DropdownField("Category");
            _categoryFilterDropdown.name = "CategoryFilter";
            _categoryFilterDropdown.style.width = 150;
            _categoryFilterDropdown.style.marginRight = 10;
            _categoryFilterDropdown.RegisterValueChangedCallback(evt =>
            {
                OnCategoryFilterChanged?.Invoke(evt.newValue);
            });
            filterBar.Add(_categoryFilterDropdown);
            
            // Tag dropdown
            _tagFilterDropdown = new DropdownField("Tag");
            _tagFilterDropdown.name = "TagFilter";
            _tagFilterDropdown.style.width = 150;
            _tagFilterDropdown.style.marginRight = 10;
            _tagFilterDropdown.RegisterValueChangedCallback(evt =>
            {
                OnTagFilterChanged?.Invoke(evt.newValue);
            });
            filterBar.Add(_tagFilterDropdown);
            
            // Search field
            _searchField = new TextField("Search");
            _searchField.name = "SearchField";
            _searchField.style.flexGrow = 1;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                // Debounce search
                if (_searchDebounceCoroutine != null)
                    StopCoroutine(_searchDebounceCoroutine);
                
                _searchDebounceCoroutine = StartCoroutine(DebounceSearch(evt.newValue));
            });
            filterBar.Add(_searchField);
            
            return filterBar;
        }
        
        /// <summary>
        /// Cache references to existing filter bar elements.
        /// Called when filter bar already exists and we need to reconnect to it.
        /// </summary>
        private void CacheFilterBarReferences(VisualElement filterBar)
        {
            // Cache type filter buttons
            var typeFilterContainer = filterBar.Q<VisualElement>("TypeFilterContainer");
            if (typeFilterContainer != null)
            {
                _typeFilterAllButton = typeFilterContainer.Q<Button>("TypeFilterAllButton");
                _typeFilterActorButton = typeFilterContainer.Q<Button>("TypeFilterActorButton");
                _typeFilterPropButton = typeFilterContainer.Q<Button>("TypeFilterPropButton");
                _typeFilterCameraButton = typeFilterContainer.Q<Button>("TypeFilterCameraButton");
                _typeFilterLightButton = typeFilterContainer.Q<Button>("TypeFilterLightButton");
                
                // Re-register click handlers if buttons exist
                if (_typeFilterAllButton != null)
                {
                    _typeFilterAllButton.clicked += () => { _currentTypeFilter = null; UpdateTypeFilterButtons(); OnTypeFilterChanged?.Invoke(null); };
                }
                if (_typeFilterActorButton != null)
                {
                    _typeFilterActorButton.clicked += () => { _currentTypeFilter = SceneObjectType.Actor; UpdateTypeFilterButtons(); OnTypeFilterChanged?.Invoke(SceneObjectType.Actor); Debug.Log("Actor filter clicked"); };
                }
                if (_typeFilterPropButton != null)
                {
                    _typeFilterPropButton.clicked += () => { _currentTypeFilter = SceneObjectType.Prop; UpdateTypeFilterButtons(); OnTypeFilterChanged?.Invoke(SceneObjectType.Prop); };
                }
                if (_typeFilterCameraButton != null)
                {
                    _typeFilterCameraButton.clicked += () => { _currentTypeFilter = SceneObjectType.Camera; UpdateTypeFilterButtons(); OnTypeFilterChanged?.Invoke(SceneObjectType.Camera); };
                }
                if (_typeFilterLightButton != null)
                {
                    _typeFilterLightButton.clicked += () => { _currentTypeFilter = SceneObjectType.Light; UpdateTypeFilterButtons(); OnTypeFilterChanged?.Invoke(SceneObjectType.Light); };
                }
            }
            
            // Cache category dropdown
            _categoryFilterDropdown = filterBar.Q<DropdownField>("CategoryFilter");
            if (_categoryFilterDropdown != null)
            {
                _categoryFilterDropdown.RegisterValueChangedCallback(evt => { OnCategoryFilterChanged?.Invoke(evt.newValue); });
            }
            
            // Cache tag dropdown
            _tagFilterDropdown = filterBar.Q<DropdownField>("TagFilter");
            if (_tagFilterDropdown != null)
            {
                _tagFilterDropdown.RegisterValueChangedCallback(evt => { OnTagFilterChanged?.Invoke(evt.newValue); });
            }
            
            // Cache search field
            _searchField = filterBar.Q<TextField>("SearchField");
            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt =>
                {
                    if (_searchDebounceCoroutine != null)
                        StopCoroutine(_searchDebounceCoroutine);
                    _searchDebounceCoroutine = StartCoroutine(DebounceSearch(evt.newValue));
                });
            }
        }
        
        /// <summary>
        /// Create a type filter button.
        /// </summary>
        private Button CreateTypeFilterButton(string label, SceneObjectType? type)
        {
            var button = new Button(() =>
            {
                _currentTypeFilter = type;
                UpdateTypeFilterButtons();
                OnTypeFilterChanged?.Invoke(type);
            });
            button.text = label;
            button.style.minWidth = 60;
            button.style.paddingLeft = 5;
            button.style.paddingRight = 5;
            button.style.paddingTop = 5;
            button.style.paddingBottom = 5;
            return button;
        }
        
        /// <summary>
        /// Update visual state of type filter buttons.
        /// </summary>
        private void UpdateTypeFilterButtons()
        {
            _typeFilterAllButton.style.backgroundColor = _currentTypeFilter == null ? new Color(0.4f, 0.6f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
            _typeFilterActorButton.style.backgroundColor = _currentTypeFilter == SceneObjectType.Actor ? new Color(0.4f, 0.6f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
            _typeFilterPropButton.style.backgroundColor = _currentTypeFilter == SceneObjectType.Prop ? new Color(0.4f, 0.6f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
            _typeFilterCameraButton.style.backgroundColor = _currentTypeFilter == SceneObjectType.Camera ? new Color(0.4f, 0.6f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
            _typeFilterLightButton.style.backgroundColor = _currentTypeFilter == SceneObjectType.Light ? new Color(0.4f, 0.6f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
        }
        
        /// <summary>
        /// Debounce search input by 300ms.
        /// </summary>
        private IEnumerator DebounceSearch(string query)
        {
            yield return new WaitForSeconds(0.3f);
            OnSearchQueryChanged?.Invoke(query);
            _searchDebounceCoroutine = null;
        }
        
        /// <summary>
        /// Bind UI elements to ViewModel properties.
        /// </summary>
        private void BindToViewModel()
        {
            // Bind category dropdown to available categories
            UpdateCategoryDropdown();
            
            // Bind tag dropdown to available tags
            UpdateTagDropdown();
            
            // We'll need to update the grid when filters change
            // For now, we'll handle this through event callbacks
        }
        
        /// <summary>
        /// Update the category dropdown with available categories.
        /// </summary>
        private void UpdateCategoryDropdown()
        {
            var categories = _viewModel.AvailableCategories.Value;
            _categoryFilterDropdown.choices = new List<string>(categories);
            
            if (categories.Count > 0)
                _categoryFilterDropdown.value = categories[0]; // Default to "All"
        }
        
        /// <summary>
        /// Update the tag dropdown with available tags.
        /// </summary>
        private void UpdateTagDropdown()
        {
            var tags = _viewModel.AvailableTags.Value;
            _tagFilterDropdown.choices = new List<string>(tags);
            
            if (tags.Count > 0)
                _tagFilterDropdown.value = tags[0]; // Default to "All"
        }
        
        /// <summary>
        /// Render the object grid with filtered objects.
        /// </summary>
        public void RenderObjectGrid(List<SceneObjectData> filteredObjects)
        {
            // Recycle all existing cards in the grid
            while (_grid.childCount > 0)
            {
                var card = _grid.ElementAt(0);
                card.RemoveFromHierarchy();
                _cardPool.Enqueue(card);
            }
            _visibleCards.Clear();
            
            if (filteredObjects == null || filteredObjects.Count == 0)
            {
                // Show empty state
                _emptyStateLabel.text = "No objects available";
                _emptyStateLabel.style.display = DisplayStyle.Flex;

                // Ensure label is in container
                if (!_objectGridContainer.Contains(_emptyStateLabel))
                    _objectGridContainer.Add(_emptyStateLabel);

                return;
            }
            
            _emptyStateLabel.style.display = DisplayStyle.None;

            // Ensure grid is in container
            if (!_objectGridContainer.Contains(_grid))
            {
                _objectGridContainer.Add(_grid);
            }
            
            // Render cards
            foreach (var obj in filteredObjects)
            {
                VisualElement card;
                if (_cardPool.Count > 0)
                {
                    card = _cardPool.Dequeue();
                }
                else
                {
                    card = CreateObjectCardView();
                }

                BindObjectCard(card, obj);
                _grid.Add(card);
                _visibleCards[obj] = card;
            }
        }
        
        /// <summary>
        /// Create the basic structure of a card for displaying an object.
        /// </summary>
        private VisualElement CreateObjectCardView()
        {
            var card = new VisualElement();
            card.name = "ObjectCard";
            card.style.width = 100;
            card.style.height = 120;
            card.style.marginLeft = 5;
            card.style.marginRight = 5;
            card.style.marginTop = 5;
            card.style.marginBottom = 5;
            card.style.paddingLeft = 5;
            card.style.paddingRight = 5;
            card.style.paddingTop = 5;
            card.style.paddingBottom = 5;
            card.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            card.style.borderBottomLeftRadius = 4;
            card.style.borderBottomRightRadius = 4;
            card.style.borderTopLeftRadius = 4;
            card.style.borderTopRightRadius = 4;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopWidth = 1;
            card.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            card.style.borderLeftColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            card.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            card.style.borderTopColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            // Cursor styling via MouseCursor is editor-only; omit for runtime compatibility.
            
            // Icon
            var icon = new Image();
            icon.name = "Icon";
            icon.style.width = 64;
            icon.style.height = 64;
            icon.style.marginBottom = 5;
            card.Add(icon);
            
            // Name label
            var nameLabel = new Label();
            nameLabel.name = "NameLabel";
            nameLabel.style.fontSize = 10;
            nameLabel.style.whiteSpace = WhiteSpace.NoWrap;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;
            nameLabel.style.marginBottom = 2;
            card.Add(nameLabel);
            
            // Category badge
            var categoryLabel = new Label();
            categoryLabel.name = "CategoryLabel";
            categoryLabel.style.fontSize = 8;
            categoryLabel.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            categoryLabel.style.paddingLeft = 3;
            categoryLabel.style.paddingRight = 3;
            categoryLabel.style.paddingTop = 2;
            categoryLabel.style.paddingBottom = 2;
            categoryLabel.style.borderBottomLeftRadius = 2;
            categoryLabel.style.borderBottomRightRadius = 2;
            categoryLabel.style.borderTopLeftRadius = 2;
            categoryLabel.style.borderTopRightRadius = 2;
            card.Add(categoryLabel);
            
            // Click handler - uses stored user data
            card.RegisterCallback<ClickEvent>(evt =>
            {
                if (card.userData is SceneObjectData data)
                {
                    OnCardClicked?.Invoke(data);
                }
            });
            
            // Hover effect
            card.RegisterCallback<PointerEnterEvent>(evt =>
            {
                card.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            });
            
            card.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                card.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            });
            
            return card;
        }

        /// <summary>
        /// Update an existing card with new object data.
        /// </summary>
        private void BindObjectCard(VisualElement card, SceneObjectData obj)
        {
            card.userData = obj;

            // Icon
            var icon = card.Q<Image>("Icon");
            if (icon != null)
            {
                if (obj.icon != null)
                {
                    icon.image = obj.icon.texture;
                    icon.style.backgroundColor = StyleKeyword.Null; // Clear background color
                }
                else
                {
                    icon.image = null;
                    // Use type-specific fallback color
                    var fallbackColor = ObjectIconFallback.GetTypeColor(obj.objectType);
                    icon.style.backgroundColor = fallbackColor;
                }
            }

            // Name label
            var nameLabel = card.Q<Label>("NameLabel");
            if (nameLabel != null)
            {
                nameLabel.text = obj.displayName;
            }

            // Category badge
            var categoryLabel = card.Q<Label>("CategoryLabel");
            if (categoryLabel != null)
            {
                categoryLabel.text = obj.category ?? "Default";
            }
        }

        // Kept for compatibility if referenced elsewhere, but calls new methods
        private VisualElement CreateObjectCard(SceneObjectData obj)
        {
            var card = CreateObjectCardView();
            BindObjectCard(card, obj);
            return card;
        }
    }
}
