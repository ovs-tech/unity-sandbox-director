using UnityEngine;
using UnityEngine.UI;
using SceneSandbox.Core;
using SceneSandbox.Data;

namespace SceneSandbox.UI
{
    /// <summary>
    /// Main UI controller for the Scene Sandbox Builder
    /// Manages all UI panels and interactions
    /// </summary>
    public class SandboxBuilderUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private ObjectPalette _objectPalette;
        [SerializeField] private GameObject _propertiesPanel;
        [SerializeField] private GameObject _sceneControlsPanel;
        [SerializeField] private GameObject _previewPanel;
        
        [Header("Scene Controls")]
        [SerializeField] private Button _newSceneButton;
        [SerializeField] private Button _saveSceneButton;
        [SerializeField] private Button _loadSceneButton;
        [SerializeField] private Button _clearSceneButton;
        [SerializeField] private Button _previewButton;
        [SerializeField] private Button _stopPreviewButton;
        
        [Header("Object Controls")]
        [SerializeField] private Button _deleteObjectButton;
        [SerializeField] private Toggle _snapToGridToggle;
        [SerializeField] private Slider _gridSizeSlider;
        [SerializeField] private Text _gridSizeText;
        
        [Header("Properties Panel")]
        [SerializeField] private Text _selectedObjectName;
        [SerializeField] private InputField _objectNameInput;
        [SerializeField] private Transform _positionInputs;
        [SerializeField] private Transform _rotationInputs;
        [SerializeField] private Transform _scaleInputs;
        
        [Header("Scene Info")]
        [SerializeField] private Text _sceneNameText;
        [SerializeField] private Text _objectCountText;
        [SerializeField] private Text _previewStatusText;
        
        [Header("Mobile-Specific")]
        [SerializeField] private GameObject _mobileControlsPanel;
        [SerializeField] private Button _mobilePaletteToggle;
        [SerializeField] private Button _mobilePropertiesToggle;
        
        // Core References
        private Core.SceneSandboxBuilder _sandboxBuilder;
        private GameObject _selectedObject;
        private bool _isPaletteVisible = true;
        private bool _arePropertiesVisible = false;
        
        // Input fields for transform editing
        private InputField[] _positionFields;
        private InputField[] _rotationFields;
        private InputField[] _scaleFields;
        
        // Properties
        public bool IsPaletteVisible => _isPaletteVisible;
        public bool ArePropertiesVisible => _arePropertiesVisible;
        
        private void Awake()
        {
            CreateUIIfMissing();
            SetupUI();
            SetupMobileControls();
        }
        
        private void Start()
        {
            // Find sandbox builder if not assigned
            if (_sandboxBuilder == null)
            {
                _sandboxBuilder = FindFirstObjectByType<Core.SceneSandboxBuilder>();
            }
            
            if (_sandboxBuilder != null)
            {
                BindSandboxEvents();
                UpdateUI();
            }
            
            // Set initial panel visibility
            UpdatePanelVisibility();
        }
        
        private void SetupUI()
        {
            // Scene control buttons
            if (_newSceneButton != null)
                _newSceneButton.onClick.AddListener(OnNewSceneClicked);
            
            if (_saveSceneButton != null)
                _saveSceneButton.onClick.AddListener(OnSaveSceneClicked);
            
            if (_loadSceneButton != null)
                _loadSceneButton.onClick.AddListener(OnLoadSceneClicked);
            
            if (_clearSceneButton != null)
                _clearSceneButton.onClick.AddListener(OnClearSceneClicked);
            
            if (_previewButton != null)
                _previewButton.onClick.AddListener(OnPreviewClicked);
            
            if (_stopPreviewButton != null)
                _stopPreviewButton.onClick.AddListener(OnStopPreviewClicked);
            
            // Object control buttons
            if (_deleteObjectButton != null)
                _deleteObjectButton.onClick.AddListener(OnDeleteObjectClicked);
            
            // Settings controls
            if (_snapToGridToggle != null)
                _snapToGridToggle.onValueChanged.AddListener(OnSnapToGridChanged);
            
            if (_gridSizeSlider != null)
                _gridSizeSlider.onValueChanged.AddListener(OnGridSizeChanged);
            
            // Properties panel
            if (_objectNameInput != null)
                _objectNameInput.onEndEdit.AddListener(OnObjectNameChanged);
            
            SetupTransformInputs();
        }
        
        private void SetupMobileControls()
        {
            // Check if running on mobile
            bool isMobile = Application.isMobilePlatform;
            
            if (_mobileControlsPanel != null)
            {
                _mobileControlsPanel.SetActive(isMobile);
            }
            
            if (isMobile)
            {
                // Setup mobile-specific controls
                if (_mobilePaletteToggle != null)
                    _mobilePaletteToggle.onClick.AddListener(TogglePalette);
                
                if (_mobilePropertiesToggle != null)
                    _mobilePropertiesToggle.onClick.AddListener(ToggleProperties);
                
                // Start with palette visible on mobile
                _isPaletteVisible = true;
                _arePropertiesVisible = false;
            }
        }
        
        private void SetupTransformInputs()
        {
            // Setup position input fields
            if (_positionInputs != null)
            {
                _positionFields = _positionInputs.GetComponentsInChildren<InputField>();
                for (int i = 0; i < _positionFields.Length && i < 3; i++)
                {
                    int index = i; // Capture for closure
                    _positionFields[i].onEndEdit.AddListener(value => OnPositionChanged(index, value));
                }
            }
            
            // Setup rotation input fields
            if (_rotationInputs != null)
            {
                _rotationFields = _rotationInputs.GetComponentsInChildren<InputField>();
                for (int i = 0; i < _rotationFields.Length && i < 3; i++)
                {
                    int index = i; // Capture for closure
                    _rotationFields[i].onEndEdit.AddListener(value => OnRotationChanged(index, value));
                }
            }
            
            // Setup scale input fields
            if (_scaleInputs != null)
            {
                _scaleFields = _scaleInputs.GetComponentsInChildren<InputField>();
                for (int i = 0; i < _scaleFields.Length && i < 3; i++)
                {
                    int index = i; // Capture for closure
                    _scaleFields[i].onEndEdit.AddListener(value => OnScaleChanged(index, value));
                }
            }
        }
        
        private void BindSandboxEvents()
        {
            if (_sandboxBuilder == null) return;
            
            _sandboxBuilder.OnSceneLoaded += OnSceneLoaded;
            _sandboxBuilder.OnSceneSaved += OnSceneSaved;
            _sandboxBuilder.OnObjectPlaced += OnObjectPlaced;
            _sandboxBuilder.OnObjectRemoved += OnObjectRemoved;
            _sandboxBuilder.OnObjectSelected += OnObjectSelected;
            _sandboxBuilder.OnSceneCleared += OnSceneCleared;
            _sandboxBuilder.OnPreviewStateChanged += OnPreviewStateChanged;
        }
        
        #region Public Interface
        
        /// <summary>
        /// Toggle the visibility of the object palette
        /// </summary>
        public void TogglePalette()
        {
            _isPaletteVisible = !_isPaletteVisible;
            UpdatePanelVisibility();
        }
        
        /// <summary>
        /// Toggle the visibility of the properties panel
        /// </summary>
        public void ToggleProperties()
        {
            _arePropertiesVisible = !_arePropertiesVisible;
            UpdatePanelVisibility();
        }
        
        /// <summary>
        /// Show or hide the object palette
        /// </summary>
        public void SetPaletteVisible(bool visible)
        {
            _isPaletteVisible = visible;
            UpdatePanelVisibility();
        }
        
        /// <summary>
        /// Show or hide the properties panel
        /// </summary>
        public void SetPropertiesVisible(bool visible)
        {
            _arePropertiesVisible = visible;
            UpdatePanelVisibility();
        }
        
        /// <summary>
        /// Update all UI elements
        /// </summary>
        public void UpdateUI()
        {
            UpdateSceneInfo();
            UpdateObjectInfo();
            UpdateButtonStates();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnNewSceneClicked()
        {
            _sandboxBuilder?.CreateNewScene();
        }
        
        private void OnSaveSceneClicked()
        {
            _sandboxBuilder?.SaveScene();
        }
        
        private void OnLoadSceneClicked()
        {
            // TODO: Show file picker or scene selection dialog
            Debug.Log("Load scene dialog not implemented yet");
        }
        
        private void OnClearSceneClicked()
        {
            // TODO: Show confirmation dialog
            _sandboxBuilder?.ClearScene();
        }
        
        private void OnPreviewClicked()
        {
            _sandboxBuilder?.StartPreview();
        }
        
        private void OnStopPreviewClicked()
        {
            _sandboxBuilder?.StopPreview();
        }
        
        private void OnDeleteObjectClicked()
        {
            if (_selectedObject != null)
            {
                _sandboxBuilder?.RemoveObject(_selectedObject);
            }
        }
        
        private void OnSnapToGridChanged(bool enabled)
        {
            // TODO: Implement snap to grid setting
            Debug.Log($"Snap to grid: {enabled}");
        }
        
        private void OnGridSizeChanged(float size)
        {
            if (_gridSizeText != null)
            {
                _gridSizeText.text = size.ToString("F1");
            }
            
            // TODO: Update grid size in sandbox builder
        }
        
        private void OnObjectNameChanged(string newName)
        {
            if (_selectedObject != null)
            {
                _selectedObject.name = newName;
            }
        }
        
        private void OnPositionChanged(int axis, string value)
        {
            if (_selectedObject == null || !float.TryParse(value, out float floatValue)) return;
            
            Vector3 position = _selectedObject.transform.position;
            
            switch (axis)
            {
                case 0: position.x = floatValue; break;
                case 1: position.y = floatValue; break;
                case 2: position.z = floatValue; break;
            }
            
            _selectedObject.transform.position = position;
        }
        
        private void OnRotationChanged(int axis, string value)
        {
            if (_selectedObject == null || !float.TryParse(value, out float floatValue)) return;
            
            Vector3 rotation = _selectedObject.transform.eulerAngles;
            
            switch (axis)
            {
                case 0: rotation.x = floatValue; break;
                case 1: rotation.y = floatValue; break;
                case 2: rotation.z = floatValue; break;
            }
            
            _selectedObject.transform.eulerAngles = rotation;
        }
        
        private void OnScaleChanged(int axis, string value)
        {
            if (_selectedObject == null || !float.TryParse(value, out float floatValue)) return;
            
            Vector3 scale = _selectedObject.transform.localScale;
            
            switch (axis)
            {
                case 0: scale.x = floatValue; break;
                case 1: scale.y = floatValue; break;
                case 2: scale.z = floatValue; break;
            }
            
            _selectedObject.transform.localScale = scale;
        }
        
        // Sandbox events
        private void OnSceneLoaded(SceneConfiguration scene)
        {
            UpdateUI();
        }
        
        private void OnSceneSaved(SceneConfiguration scene)
        {
            Debug.Log($"Scene '{scene.sceneName}' saved successfully");
        }
        
        private void OnObjectPlaced(GameObject obj)
        {
            UpdateUI();
        }
        
        private void OnObjectRemoved(GameObject obj)
        {
            if (_selectedObject == obj)
            {
                _selectedObject = null;
            }
            UpdateUI();
        }
        
        private void OnObjectSelected(GameObject obj)
        {
            _selectedObject = obj;
            UpdateObjectInfo();
            UpdateButtonStates();
        }
        
        private void OnSceneCleared()
        {
            _selectedObject = null;
            UpdateUI();
        }
        
        private void OnPreviewStateChanged(bool isInPreview)
        {
            UpdateButtonStates();
            
            if (_previewStatusText != null)
            {
                _previewStatusText.text = isInPreview ? "PREVIEW MODE" : "EDIT MODE";
            }
        }
        
        #endregion
        
        #region UI Updates
        
        private void UpdatePanelVisibility()
        {
            if (_objectPalette != null)
            {
                _objectPalette.gameObject.SetActive(_isPaletteVisible);
            }
            
            if (_propertiesPanel != null)
            {
                _propertiesPanel.SetActive(_arePropertiesVisible);
            }
        }
        
        private void UpdateSceneInfo()
        {
            if (_sandboxBuilder?.CurrentScene == null) return;
            
            var scene = _sandboxBuilder.CurrentScene;
            
            if (_sceneNameText != null)
            {
                _sceneNameText.text = scene.sceneName;
            }
            
            if (_objectCountText != null)
            {
                _objectCountText.text = $"Objects: {scene.placedObjects.Count}";
            }
        }
        
        private void UpdateObjectInfo()
        {
            bool hasSelection = _selectedObject != null;
            
            // Update object name
            if (_selectedObjectName != null)
            {
                _selectedObjectName.text = hasSelection ? _selectedObject.name : "No Selection";
            }
            
            if (_objectNameInput != null)
            {
                _objectNameInput.text = hasSelection ? _selectedObject.name : "";
                _objectNameInput.interactable = hasSelection;
            }
            
            // Update transform inputs
            UpdateTransformInputs();
        }
        
        private void UpdateTransformInputs()
        {
            bool hasSelection = _selectedObject != null;
            
            // Position inputs
            if (_positionFields != null)
            {
                Vector3 position = hasSelection ? _selectedObject.transform.position : Vector3.zero;
                for (int i = 0; i < _positionFields.Length && i < 3; i++)
                {
                    _positionFields[i].text = position[i].ToString("F2");
                    _positionFields[i].interactable = hasSelection;
                }
            }
            
            // Rotation inputs
            if (_rotationFields != null)
            {
                Vector3 rotation = hasSelection ? _selectedObject.transform.eulerAngles : Vector3.zero;
                for (int i = 0; i < _rotationFields.Length && i < 3; i++)
                {
                    _rotationFields[i].text = rotation[i].ToString("F2");
                    _rotationFields[i].interactable = hasSelection;
                }
            }
            
            // Scale inputs
            if (_scaleFields != null)
            {
                Vector3 scale = hasSelection ? _selectedObject.transform.localScale : Vector3.one;
                for (int i = 0; i < _scaleFields.Length && i < 3; i++)
                {
                    _scaleFields[i].text = scale[i].ToString("F2");
                    _scaleFields[i].interactable = hasSelection;
                }
            }
        }
        
        private void UpdateButtonStates()
        {
            bool hasSelection = _selectedObject != null;
            bool isInPreview = _sandboxBuilder?.IsInPreviewMode ?? false;
            
            // Enable/disable buttons based on state
            if (_deleteObjectButton != null)
                _deleteObjectButton.interactable = hasSelection && !isInPreview;
            
            if (_previewButton != null)
                _previewButton.interactable = !isInPreview;
            
            if (_stopPreviewButton != null)
                _stopPreviewButton.interactable = isInPreview;
            
            // Disable editing controls during preview
            if (_objectPalette != null)
                _objectPalette.gameObject.SetActive(!isInPreview);
        }
        
        #endregion
        
        #region UI Creation
        
        /// <summary>
        /// Creates UI elements programmatically if they're not assigned in the inspector
        /// </summary>
        private void CreateUIIfMissing()
        {
            // Create main UI structure if nothing is assigned
            bool needsCompleteUI = _sceneControlsPanel == null && _propertiesPanel == null;
            
            if (needsCompleteUI)
            {
                CreateCompleteUI();
            }
            else
            {
                // Create individual missing components
                CreateMissingComponents();
            }
        }
        
        private void CreateCompleteUI()
        {
            // Create main canvas if we're not on one
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("SandboxUI Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasGO.transform, false);
            }
            
            // Setup main layout
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            // Fill the canvas
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // Create main panels
            CreateSceneControlsPanel();
            CreatePropertiesPanel();
            CreateMobileControlsPanel();
        }
        
        private void CreateSceneControlsPanel()
        {
            // Create scene controls panel
            var controlsGO = new GameObject("Scene Controls Panel");
            controlsGO.transform.SetParent(transform, false);
            _sceneControlsPanel = controlsGO;
            
            var controlsRect = controlsGO.AddComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0, 1);
            controlsRect.anchorMax = new Vector2(1, 1);
            controlsRect.pivot = new Vector2(0.5f, 1);
            controlsRect.sizeDelta = new Vector2(0, 80);
            controlsRect.anchoredPosition = Vector2.zero;
            
            var controlsImage = controlsGO.AddComponent<Image>();
            controlsImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Add horizontal layout
            var layoutGroup = controlsGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            
            // Create scene control buttons
            _newSceneButton = CreateButton(controlsGO.transform, "New Scene");
            _saveSceneButton = CreateButton(controlsGO.transform, "Save Scene");
            _loadSceneButton = CreateButton(controlsGO.transform, "Load Scene");
            _clearSceneButton = CreateButton(controlsGO.transform, "Clear Scene");
            _previewButton = CreateButton(controlsGO.transform, "Preview");
            _stopPreviewButton = CreateButton(controlsGO.transform, "Stop Preview");
            
            // Scene info
            _sceneNameText = CreateText(controlsGO.transform, "New Scene");
            _objectCountText = CreateText(controlsGO.transform, "Objects: 0");
            _previewStatusText = CreateText(controlsGO.transform, "EDIT MODE");
        }
        
        private void CreatePropertiesPanel()
        {
            // Create properties panel
            var propertiesGO = new GameObject("Properties Panel");
            propertiesGO.transform.SetParent(transform, false);
            _propertiesPanel = propertiesGO;
            
            var propertiesRect = propertiesGO.AddComponent<RectTransform>();
            propertiesRect.anchorMin = new Vector2(1, 0);
            propertiesRect.anchorMax = new Vector2(1, 1);
            propertiesRect.pivot = new Vector2(1, 0.5f);
            propertiesRect.sizeDelta = new Vector2(300, 0);
            propertiesRect.anchoredPosition = Vector2.zero;
            
            var propertiesImage = propertiesGO.AddComponent<Image>();
            propertiesImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            
            // Add scroll view for properties
            var scrollView = CreateScrollView(propertiesGO.transform, "Properties Content");
            var contentTransform = scrollView.content;
            
            // Add vertical layout to content
            var contentLayout = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.spacing = 5;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlHeight = false;
            
            // Create property sections
            CreateObjectInfoSection(contentTransform);
            CreateTransformSection(contentTransform);
            CreateObjectControlsSection(contentTransform);
        }
        
        private void CreateObjectInfoSection(Transform parent)
        {
            var sectionGO = new GameObject("Object Info Section");
            sectionGO.transform.SetParent(parent, false);
            
            var sectionLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 5;
            sectionLayout.childControlHeight = false;
            sectionLayout.childForceExpandWidth = true;
            
            CreateText(sectionGO.transform, "OBJECT INFO", 14, FontStyle.Bold);
            
            _selectedObjectName = CreateText(sectionGO.transform, "No Selection");
            _objectNameInput = CreateInputField(sectionGO.transform, "Object Name");
        }
        
        private void CreateTransformSection(Transform parent)
        {
            var sectionGO = new GameObject("Transform Section");
            sectionGO.transform.SetParent(parent, false);
            
            var sectionLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 5;
            sectionLayout.childControlHeight = false;
            sectionLayout.childForceExpandWidth = true;
            
            CreateText(sectionGO.transform, "TRANSFORM", 14, FontStyle.Bold);
            
            // Position inputs
            _positionInputs = CreateVector3InputGroup(sectionGO.transform, "Position");
            
            // Rotation inputs
            _rotationInputs = CreateVector3InputGroup(sectionGO.transform, "Rotation");
            
            // Scale inputs
            _scaleInputs = CreateVector3InputGroup(sectionGO.transform, "Scale");
        }
        
        private void CreateObjectControlsSection(Transform parent)
        {
            var sectionGO = new GameObject("Object Controls Section");
            sectionGO.transform.SetParent(parent, false);
            
            var sectionLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 5;
            sectionLayout.childControlHeight = false;
            sectionLayout.childForceExpandWidth = true;
            
            CreateText(sectionGO.transform, "CONTROLS", 14, FontStyle.Bold);
            
            _deleteObjectButton = CreateButton(sectionGO.transform, "Delete Object");
            
            // Grid controls
            var gridControlsGO = new GameObject("Grid Controls");
            gridControlsGO.transform.SetParent(sectionGO.transform, false);
            var gridLayout = gridControlsGO.AddComponent<VerticalLayoutGroup>();
            gridLayout.spacing = 5;
            
            _snapToGridToggle = CreateToggle(gridControlsGO.transform, "Snap to Grid");
            
            var gridSizeRow = new GameObject("Grid Size Row");
            gridSizeRow.transform.SetParent(gridControlsGO.transform, false);
            var gridSizeLayout = gridSizeRow.AddComponent<HorizontalLayoutGroup>();
            gridSizeLayout.spacing = 5;
            
            CreateText(gridSizeRow.transform, "Grid Size:");
            _gridSizeSlider = CreateSlider(gridSizeRow.transform, 0.5f, 5f, 1f);
            _gridSizeText = CreateText(gridSizeRow.transform, "1.0");
        }
        
        private void CreateMobileControlsPanel()
        {
            if (!Application.isMobilePlatform) return;
            
            var mobileGO = new GameObject("Mobile Controls Panel");
            mobileGO.transform.SetParent(transform, false);
            _mobileControlsPanel = mobileGO;
            
            var mobileRect = mobileGO.AddComponent<RectTransform>();
            mobileRect.anchorMin = new Vector2(0, 0);
            mobileRect.anchorMax = new Vector2(1, 0);
            mobileRect.pivot = new Vector2(0.5f, 0);
            mobileRect.sizeDelta = new Vector2(0, 60);
            mobileRect.anchoredPosition = Vector2.zero;
            
            var mobileImage = mobileGO.AddComponent<Image>();
            mobileImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            var mobileLayout = mobileGO.AddComponent<HorizontalLayoutGroup>();
            mobileLayout.padding = new RectOffset(10, 10, 10, 10);
            mobileLayout.spacing = 10;
            mobileLayout.childAlignment = TextAnchor.MiddleCenter;
            
            _mobilePaletteToggle = CreateButton(mobileGO.transform, "Palette");
            _mobilePropertiesToggle = CreateButton(mobileGO.transform, "Properties");
        }
        
        private void CreateMissingComponents()
        {
            // Create individual missing components as needed
            if (_newSceneButton == null && _sceneControlsPanel != null)
            {
                _newSceneButton = CreateButton(_sceneControlsPanel.transform, "New Scene");
            }
            
            // Add more individual component creation as needed
        }
        
        #endregion
        
        #region UI Helpers
        
        private Button CreateButton(Transform parent, string text)
        {
            var buttonGO = new GameObject($"Button_{text.Replace(" ", "")}");
            buttonGO.transform.SetParent(parent, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(120, 30);
            
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
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            return button;
        }
        
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
            
            return textComponent;
        }
        
        private InputField CreateInputField(Transform parent, string placeholder)
        {
            var inputGO = new GameObject($"InputField_{placeholder.Replace(" ", "")}");
            inputGO.transform.SetParent(parent, false);
            
            var inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(200, 25);
            
            var inputImage = inputGO.AddComponent<Image>();
            inputImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            var inputField = inputGO.AddComponent<InputField>();
            
            // Create text component for input field
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
            placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            
            inputField.placeholder = placeholderText;
            
            return inputField;
        }
        
        private Toggle CreateToggle(Transform parent, string label)
        {
            var toggleGO = new GameObject($"Toggle_{label.Replace(" ", "")}");
            toggleGO.transform.SetParent(parent, false);
            
            var toggleRect = toggleGO.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(200, 20);
            
            var toggle = toggleGO.AddComponent<Toggle>();
            
            // Create background
            var backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(toggleGO.transform, false);
            var backgroundRect = backgroundGO.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 0.5f);
            backgroundRect.anchorMax = new Vector2(0, 0.5f);
            backgroundRect.pivot = new Vector2(0, 0.5f);
            backgroundRect.sizeDelta = new Vector2(20, 20);
            
            var backgroundImage = backgroundGO.AddComponent<Image>();
            backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // Create checkmark
            var checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(backgroundGO.transform, false);
            var checkmarkRect = checkmarkGO.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            
            var checkmarkImage = checkmarkGO.AddComponent<Image>();
            checkmarkImage.color = Color.green;
            
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            
            // Create label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(toggleGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(25, 0);
            labelRect.offsetMax = Vector2.zero;
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            return toggle;
        }
        
        private Slider CreateSlider(Transform parent, float minValue, float maxValue, float defaultValue)
        {
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(parent, false);
            
            var sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(100, 20);
            
            var slider = sliderGO.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = defaultValue;
            
            // Create background
            var backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(sliderGO.transform, false);
            var backgroundRect = backgroundGO.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            
            var backgroundImage = backgroundGO.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Create fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = Color.blue;
            
            // Create handle
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;
            
            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            
            var handleImage = handleGO.AddComponent<Image>();
            handleImage.color = Color.white;
            
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            
            return slider;
        }
        
        private Transform CreateVector3InputGroup(Transform parent, string label)
        {
            var groupGO = new GameObject($"{label} Inputs");
            groupGO.transform.SetParent(parent, false);
            
            var groupLayout = groupGO.AddComponent<VerticalLayoutGroup>();
            groupLayout.spacing = 2;
            groupLayout.childControlHeight = false;
            groupLayout.childForceExpandWidth = true;
            
            CreateText(groupGO.transform, label, 10, FontStyle.Bold);
            
            var inputsRowGO = new GameObject($"{label} Row");
            inputsRowGO.transform.SetParent(groupGO.transform, false);
            var inputsLayout = inputsRowGO.AddComponent<HorizontalLayoutGroup>();
            inputsLayout.spacing = 5;
            
            CreateInputField(inputsRowGO.transform, "X");
            CreateInputField(inputsRowGO.transform, "Y");
            CreateInputField(inputsRowGO.transform, "Z");
            
            return groupGO.transform;
        }
        
        private ScrollRect CreateScrollView(Transform parent, string contentName)
        {
            var scrollViewGO = new GameObject("Scroll View");
            scrollViewGO.transform.SetParent(parent, false);
            
            var scrollRect = scrollViewGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            
            var scrollImage = scrollViewGO.AddComponent<Image>();
            scrollImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            
            var scrollComponent = scrollViewGO.AddComponent<ScrollRect>();
            
            // Create viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            var viewportMask = viewportGO.AddComponent<Mask>();
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = Color.clear;
            
            // Create content
            var contentGO = new GameObject(contentName);
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollComponent.viewport = viewportRect;
            scrollComponent.content = contentRect;
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            
            return scrollComponent;
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Unbind events to prevent memory leaks
            if (_sandboxBuilder != null)
            {
                _sandboxBuilder.OnSceneLoaded -= OnSceneLoaded;
                _sandboxBuilder.OnSceneSaved -= OnSceneSaved;
                _sandboxBuilder.OnObjectPlaced -= OnObjectPlaced;
                _sandboxBuilder.OnObjectRemoved -= OnObjectRemoved;
                _sandboxBuilder.OnObjectSelected -= OnObjectSelected;
                _sandboxBuilder.OnSceneCleared -= OnSceneCleared;
                _sandboxBuilder.OnPreviewStateChanged -= OnPreviewStateChanged;
            }
        }
    }
}