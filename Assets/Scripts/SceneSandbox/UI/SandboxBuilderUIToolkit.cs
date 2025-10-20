using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SceneSandbox.Core;
using SceneSandbox.Data;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;

namespace SceneSandbox.UI
{
    /// <summary>
    /// Main UI controller for the Scene Sandbox Builder using UI Toolkit
    /// Manages all UI panels and interactions
    /// </summary>
    public class SandboxBuilderUIToolkit : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Visual Assets")]
        [SerializeField] private VisualTreeAsset _mainUITemplate;
        [SerializeField] private StyleSheet _mainUIStyleSheet;
        
        [Header("References")]
        [SerializeField] private ObjectPaletteUIToolkit _objectPalette;
        [SerializeField] private Transform _panelPos;
        
        // Core References
        private SceneSandboxBuilder _sandboxBuilder;
        private GameObject _selectedObject;
        
        // Visual Elements - Main Layout
        private VisualElement _rootElement;
        private VisualElement _mainContainer;
        private VisualElement _leftPanel;
        private VisualElement _centerPanel;
        private VisualElement _rightPanel;
        
        // Scene Controls Panel
        private VisualElement _sceneControlsPanel;
        private Button _newSceneButton;
        private Button _saveSceneButton;
        private Button _loadSceneButton;
        private Button _clearSceneButton;
        private Button _previewButton;
        private Button _stopPreviewButton;
        
        // Project Controls
        private VisualElement _projectControlsPanel;
        private Button _newProjectButton;
        private Button _saveProjectButton;
        private Button _loadProjectButton;
        private Button _clearProjectButton;
        private TextField _projectNameInput;
        private Label _currentProjectText;
        
        // Object Controls
        private VisualElement _objectControlsPanel;
        private Button _deleteObjectButton;
        private Toggle _snapToGridToggle;
        private Slider _gridSizeSlider;
        private Label _gridSizeText;
        
        // Scene Info
        private Label _sceneNameText;
        private Label _objectCountText;
        private Label _previewStatusText;
        
        // Mobile Controls
        private VisualElement _mobileControlsPanel;
        private Button _mobilePaletteToggle;
        private Button _mobilePropertiesToggle;
        
        // Properties panel state
        private bool _isPropertiesPanelShowing = false;
        private bool _isPaletteVisible = true;
        
        // Properties
        public bool IsPaletteVisible => _isPaletteVisible;
        
        private void Awake()
        {
            InitializeUI();
            SetupUI();
            SetupMobileControls();
        }
        
        private void Start()
        {
            // Find sandbox builder if not assigned
            if (_sandboxBuilder == null)
            {
                _sandboxBuilder = FindFirstObjectByType<SceneSandboxBuilder>();
            }
            
            if (_sandboxBuilder != null)
            {
                BindSandboxEvents();
                UpdateUI();
            }
            
            // Set initial panel visibility
            UpdatePanelVisibility();
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
            if (_uiDocument.rootVisualElement == null || _mainUITemplate != null)
            {
                if (_mainUITemplate != null)
                {
                    _mainUITemplate.CloneTree(_uiDocument.rootVisualElement);
                }
                else
                {
                    CreateUIFromCode();
                }
            }
            
            _rootElement = _uiDocument.rootVisualElement;
            
            // Apply stylesheet
            if (_mainUIStyleSheet != null && !_rootElement.styleSheets.Contains(_mainUIStyleSheet))
            {
                _rootElement.styleSheets.Add(_mainUIStyleSheet);
            }
            
            // Query all UI elements
            QueryUIElements();
        }
        
        private void CreateUIFromCode()
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();
            
            // Main container
            _mainContainer = new VisualElement();
            _mainContainer.name = "main-container";
            _mainContainer.AddToClassList("main-container");
            _mainContainer.style.flexDirection = FlexDirection.Row;
            _mainContainer.style.flexGrow = 1;
            root.Add(_mainContainer);
            
            // Create three-panel layout
            CreateLeftPanel();
            CreateCenterPanel();
            CreateRightPanel();
            
            // Create mobile controls if needed
            if (Application.isMobilePlatform)
            {
                CreateMobileControlsPanel();
            }
        }
        
        private void CreateLeftPanel()
        {
            _leftPanel = new VisualElement();
            _leftPanel.name = "left-panel";
            _leftPanel.AddToClassList("panel");
            _leftPanel.AddToClassList("left-panel");
            _leftPanel.style.width = 300;
            _mainContainer.Add(_leftPanel);
            
            // Object palette will be added here by ObjectPaletteUIToolkit
            var paletteContainer = new VisualElement();
            paletteContainer.name = "palette-container";
            paletteContainer.style.flexGrow = 1;
            _leftPanel.Add(paletteContainer);
        }
        
        private void CreateCenterPanel()
        {
            _centerPanel = new VisualElement();
            _centerPanel.name = "center-panel";
            _centerPanel.AddToClassList("center-panel");
            _centerPanel.style.flexGrow = 1;
            _mainContainer.Add(_centerPanel);
            
            // Scene controls at top
            CreateSceneControlsPanel();
            
            // Scene info
            CreateSceneInfoPanel();
            
            // Main viewport area (3D scene shows through here)
            var viewportArea = new VisualElement();
            viewportArea.name = "viewport-area";
            viewportArea.style.flexGrow = 1;
            viewportArea.pickingMode = PickingMode.Ignore; // Let scene camera handle input
            _centerPanel.Add(viewportArea);
        }
        
        private void CreateRightPanel()
        {
            _rightPanel = new VisualElement();
            _rightPanel.name = "right-panel";
            _rightPanel.AddToClassList("panel");
            _rightPanel.AddToClassList("right-panel");
            _rightPanel.style.width = 300;
            _mainContainer.Add(_rightPanel);
            
            // Object controls
            CreateObjectControlsPanel();
        }
        
        private void CreateSceneControlsPanel()
        {
            _sceneControlsPanel = new VisualElement();
            _sceneControlsPanel.name = "scene-controls-panel";
            _sceneControlsPanel.AddToClassList("panel");
            _sceneControlsPanel.AddToClassList("controls-panel");
            _centerPanel.Add(_sceneControlsPanel);
            
            // Title
            var title = new Label("Scene Builder");
            title.AddToClassList("panel__header");
            _sceneControlsPanel.Add(title);
            
            // Project controls row
            var projectRow = CreateHorizontalGroup("project-controls-row");
            _sceneControlsPanel.Add(projectRow);
            
            _projectNameInput = new TextField("Project Name");
            _projectNameInput.style.flexGrow = 1;
            projectRow.Add(_projectNameInput);
            
            _newProjectButton = CreateButton("New", "button--secondary");
            projectRow.Add(_newProjectButton);
            
            _saveProjectButton = CreateButton("Save", "button--primary");
            projectRow.Add(_saveProjectButton);
            
            _loadProjectButton = CreateButton("Load", "button--secondary");
            projectRow.Add(_loadProjectButton);
            
            // Scene controls row
            var sceneRow = CreateHorizontalGroup("scene-controls-row");
            _sceneControlsPanel.Add(sceneRow);
            
            _newSceneButton = CreateButton("New Scene", "button--secondary");
            sceneRow.Add(_newSceneButton);
            
            _saveSceneButton = CreateButton("Save Scene", "button--primary");
            sceneRow.Add(_saveSceneButton);
            
            _loadSceneButton = CreateButton("Load Scene", "button--secondary");
            sceneRow.Add(_loadSceneButton);
            
            _clearSceneButton = CreateButton("Clear", "button--danger");
            sceneRow.Add(_clearSceneButton);
            
            // Preview controls
            var previewRow = CreateHorizontalGroup("preview-controls-row");
            _sceneControlsPanel.Add(previewRow);
            
            _previewButton = CreateButton("Preview", "button--success");
            previewRow.Add(_previewButton);
            
            _stopPreviewButton = CreateButton("Stop Preview", "button--warning");
            previewRow.Add(_stopPreviewButton);
            
            _currentProjectText = new Label("No project loaded");
            _currentProjectText.style.marginTop = 8;
            _sceneControlsPanel.Add(_currentProjectText);
        }
        
        private void CreateSceneInfoPanel()
        {
            var infoPanel = new VisualElement();
            infoPanel.name = "scene-info-panel";
            infoPanel.AddToClassList("panel");
            infoPanel.style.marginTop = 8;
            _centerPanel.Add(infoPanel);
            
            var infoRow = CreateHorizontalGroup("scene-info-row");
            infoPanel.Add(infoRow);
            
            _sceneNameText = new Label("Scene: None");
            infoRow.Add(_sceneNameText);
            
            _objectCountText = new Label("Objects: 0");
            infoRow.Add(_objectCountText);
            
            _previewStatusText = new Label("Edit Mode");
            infoRow.Add(_previewStatusText);
        }
        
        private void CreateObjectControlsPanel()
        {
            _objectControlsPanel = new VisualElement();
            _objectControlsPanel.name = "object-controls-panel";
            _objectControlsPanel.AddToClassList("panel");
            _rightPanel.Add(_objectControlsPanel);
            
            var title = new Label("Object Controls");
            title.AddToClassList("panel__header");
            _objectControlsPanel.Add(title);
            
            _deleteObjectButton = CreateButton("Delete Selected", "button--danger");
            _objectControlsPanel.Add(_deleteObjectButton);
            
            // Grid settings
            var gridTitle = new Label("Grid Settings");
            gridTitle.style.marginTop = 16;
            gridTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _objectControlsPanel.Add(gridTitle);
            
            _snapToGridToggle = new Toggle("Snap to Grid");
            _objectControlsPanel.Add(_snapToGridToggle);
            
            var sliderContainer = new VisualElement();
            sliderContainer.style.marginTop = 8;
            _objectControlsPanel.Add(sliderContainer);
            
            _gridSizeText = new Label("Grid Size: 1.0");
            sliderContainer.Add(_gridSizeText);
            
            _gridSizeSlider = new Slider("Grid Size", 0.1f, 5.0f);
            _gridSizeSlider.value = 1.0f;
            sliderContainer.Add(_gridSizeSlider);
        }
        
        private void CreateMobileControlsPanel()
        {
            _mobileControlsPanel = new VisualElement();
            _mobileControlsPanel.name = "mobile-controls-panel";
            _mobileControlsPanel.AddToClassList("mobile-controls");
            _mobileControlsPanel.style.position = Position.Absolute;
            _mobileControlsPanel.style.bottom = 0;
            _mobileControlsPanel.style.left = 0;
            _mobileControlsPanel.style.right = 0;
            _mobileControlsPanel.style.height = 60;
            _rootElement.Add(_mobileControlsPanel);
            
            var buttonRow = CreateHorizontalGroup("mobile-button-row");
            buttonRow.style.justifyContent = Justify.SpaceAround;
            _mobileControlsPanel.Add(buttonRow);
            
            _mobilePaletteToggle = CreateButton("Palette", "button--secondary");
            buttonRow.Add(_mobilePaletteToggle);
            
            _mobilePropertiesToggle = CreateButton("Properties", "button--secondary");
            buttonRow.Add(_mobilePropertiesToggle);
        }
        
        private void QueryUIElements()
        {
            // Query elements created from UXML
            _mainContainer = _rootElement.Q<VisualElement>("main-container");
            _leftPanel = _rootElement.Q<VisualElement>("left-panel");
            _centerPanel = _rootElement.Q<VisualElement>("center-panel");
            _rightPanel = _rootElement.Q<VisualElement>("right-panel");
            
            // Scene controls
            _sceneControlsPanel = _rootElement.Q<VisualElement>("scene-controls-panel");
            _newSceneButton = _rootElement.Q<Button>("new-scene-button");
            _saveSceneButton = _rootElement.Q<Button>("save-scene-button");
            _loadSceneButton = _rootElement.Q<Button>("load-scene-button");
            _clearSceneButton = _rootElement.Q<Button>("clear-scene-button");
            _previewButton = _rootElement.Q<Button>("preview-button");
            _stopPreviewButton = _rootElement.Q<Button>("stop-preview-button");
            
            // Project controls
            _newProjectButton = _rootElement.Q<Button>("new-project-button");
            _saveProjectButton = _rootElement.Q<Button>("save-project-button");
            _loadProjectButton = _rootElement.Q<Button>("load-project-button");
            _clearProjectButton = _rootElement.Q<Button>("clear-project-button");
            _projectNameInput = _rootElement.Q<TextField>("project-name-input");
            _currentProjectText = _rootElement.Q<Label>("current-project-text");
            
            // Object controls
            _objectControlsPanel = _rootElement.Q<VisualElement>("object-controls-panel");
            _deleteObjectButton = _rootElement.Q<Button>("delete-object-button");
            _snapToGridToggle = _rootElement.Q<Toggle>("snap-to-grid-toggle");
            _gridSizeSlider = _rootElement.Q<Slider>("grid-size-slider");
            _gridSizeText = _rootElement.Q<Label>("grid-size-text");
            
            // Scene info
            _sceneNameText = _rootElement.Q<Label>("scene-name-text");
            _objectCountText = _rootElement.Q<Label>("object-count-text");
            _previewStatusText = _rootElement.Q<Label>("preview-status-text");
            
            // Mobile controls
            _mobileControlsPanel = _rootElement.Q<VisualElement>("mobile-controls-panel");
            _mobilePaletteToggle = _rootElement.Q<Button>("mobile-palette-toggle");
            _mobilePropertiesToggle = _rootElement.Q<Button>("mobile-properties-toggle");
        }
        
        private void SetupUI()
        {
            // Scene control buttons
            if (_newSceneButton != null)
                _newSceneButton.clicked += OnNewSceneClicked;
            
            if (_saveSceneButton != null)
                _saveSceneButton.clicked += OnSaveSceneClicked;
            
            if (_loadSceneButton != null)
                _loadSceneButton.clicked += OnLoadSceneClicked;
            
            if (_clearSceneButton != null)
                _clearSceneButton.clicked += OnClearSceneClicked;
            
            if (_previewButton != null)
                _previewButton.clicked += OnPreviewClicked;
            
            if (_stopPreviewButton != null)
                _stopPreviewButton.clicked += OnStopPreviewClicked;
            
            // Project control buttons
            if (_newProjectButton != null)
                _newProjectButton.clicked += OnNewProjectClicked;
            
            if (_saveProjectButton != null)
                _saveProjectButton.clicked += OnSaveProjectClicked;
            
            if (_loadProjectButton != null)
                _loadProjectButton.clicked += OnLoadProjectClicked;
            
            if (_clearProjectButton != null)
                _clearProjectButton.clicked += OnClearProjectClicked;
            
            // Object control buttons
            if (_deleteObjectButton != null)
                _deleteObjectButton.clicked += OnDeleteObjectClicked;
            
            // Settings controls
            if (_snapToGridToggle != null)
                _snapToGridToggle.RegisterValueChangedCallback(evt => OnSnapToGridChanged(evt.newValue));
            
            if (_gridSizeSlider != null)
                _gridSizeSlider.RegisterValueChangedCallback(evt => OnGridSizeChanged(evt.newValue));
        }
        
        private void SetupMobileControls()
        {
            bool isMobile = Application.isMobilePlatform;
            
            if (_mobileControlsPanel != null)
            {
                _mobileControlsPanel.style.display = isMobile ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (isMobile)
            {
                if (_mobilePaletteToggle != null)
                    _mobilePaletteToggle.clicked += TogglePalette;
                
                if (_mobilePropertiesToggle != null)
                    _mobilePropertiesToggle.clicked += ToggleProperties;
                
                _isPaletteVisible = true;
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
        
        public void TogglePalette()
        {
            _isPaletteVisible = !_isPaletteVisible;
            UpdatePanelVisibility();
        }
        
        public void ToggleProperties()
        {
            if (_selectedObject != null)
            {
                if (!_isPropertiesPanelShowing)
                {
                    ShowObjectPropertiesPanel();
                }
                else
                {
                    FormSubmitPanelUIToolkit.Instance.CloseForm();
                    _isPropertiesPanelShowing = false;
                }
            }
            else
            {
                ShowMessageDialog("No Object Selected", "Please select an object to edit its properties.");
            }
        }
        
        public void SetPaletteVisible(bool visible)
        {
            _isPaletteVisible = visible;
            UpdatePanelVisibility();
        }
        
        public void UpdateUI()
        {
            UpdateSceneInfo();
            UpdateObjectInfo();
            UpdateButtonStates();
            UpdateProjectInfo();
        }
        
        private void ShowObjectPropertiesPanel()
        {
            if (_selectedObject == null) return;
            
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            // Object name
            fieldDefinitions.Add(new FormFieldDefinition("objectName", "Object Name", "text")
            {
                required = true,
                defaultValue = _selectedObject.name,
                placeholder = "Enter object name..."
            });
            
            // Transform - Position
            var position = _selectedObject.transform.position;
            fieldDefinitions.Add(new FormFieldDefinition("positionX", "Position X", "number")
            {
                defaultValue = position.x
            });
            fieldDefinitions.Add(new FormFieldDefinition("positionY", "Position Y", "number")
            {
                defaultValue = position.y
            });
            fieldDefinitions.Add(new FormFieldDefinition("positionZ", "Position Z", "number")
            {
                defaultValue = position.z
            });
            
            // Transform - Rotation
            var rotation = _selectedObject.transform.eulerAngles;
            fieldDefinitions.Add(new FormFieldDefinition("rotationX", "Rotation X", "number")
            {
                defaultValue = rotation.x
            });
            fieldDefinitions.Add(new FormFieldDefinition("rotationY", "Rotation Y", "number")
            {
                defaultValue = rotation.y
            });
            fieldDefinitions.Add(new FormFieldDefinition("rotationZ", "Rotation Z", "number")
            {
                defaultValue = rotation.z
            });
            
            // Transform - Scale
            var scale = _selectedObject.transform.localScale;
            fieldDefinitions.Add(new FormFieldDefinition("scaleX", "Scale X", "number")
            {
                defaultValue = scale.x
            });
            fieldDefinitions.Add(new FormFieldDefinition("scaleY", "Scale Y", "number")
            {
                defaultValue = scale.y
            });
            fieldDefinitions.Add(new FormFieldDefinition("scaleZ", "Scale Z", "number")
            {
                defaultValue = scale.z
            });
            
            // Object active state
            fieldDefinitions.Add(new FormFieldDefinition("activeState", "Active", "toggle")
            {
                defaultValue = _selectedObject.activeSelf
            });
            
            _isPropertiesPanelShowing = true;
            
            FormSubmitPanelUIToolkit.Instance.Show(
                $"Properties: {_selectedObject.name}",
                fieldDefinitions,
                (formData) => ApplyObjectProperties(formData),
                () => { _isPropertiesPanelShowing = false; },
                _panelPos
            );
        }
        
        private void ApplyObjectProperties(Dictionary<string, object> formData)
        {
            if (_selectedObject == null) return;
            
            // Apply name
            if (formData.ContainsKey("objectName"))
            {
                _selectedObject.name = formData["objectName"].ToString();
            }
            
            // Apply position
            if (formData.ContainsKey("positionX") && formData.ContainsKey("positionY") && formData.ContainsKey("positionZ"))
            {
                var newPosition = new Vector3(
                    Convert.ToSingle(formData["positionX"]),
                    Convert.ToSingle(formData["positionY"]),
                    Convert.ToSingle(formData["positionZ"])
                );
                _selectedObject.transform.position = newPosition;
            }
            
            // Apply rotation
            if (formData.ContainsKey("rotationX") && formData.ContainsKey("rotationY") && formData.ContainsKey("rotationZ"))
            {
                var newRotation = new Vector3(
                    Convert.ToSingle(formData["rotationX"]),
                    Convert.ToSingle(formData["rotationY"]),
                    Convert.ToSingle(formData["rotationZ"])
                );
                _selectedObject.transform.eulerAngles = newRotation;
            }
            
            // Apply scale
            if (formData.ContainsKey("scaleX") && formData.ContainsKey("scaleY") && formData.ContainsKey("scaleZ"))
            {
                var newScale = new Vector3(
                    Convert.ToSingle(formData["scaleX"]),
                    Convert.ToSingle(formData["scaleY"]),
                    Convert.ToSingle(formData["scaleZ"])
                );
                _selectedObject.transform.localScale = newScale;
            }
            
            // Apply active state
            if (formData.ContainsKey("activeState"))
            {
                _selectedObject.SetActive(Convert.ToBoolean(formData["activeState"]));
            }
            
            UpdateObjectInfo();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnNewSceneClicked()
        {
            ShowNewSceneDialog();
        }
        
        private void ShowNewSceneDialog()
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            fieldDefinitions.Add(new FormFieldDefinition("sceneName", "Scene Name", "text")
            {
                required = true,
                placeholder = "Enter scene name...",
                defaultValue = $"Scene_{DateTime.Now:yyyyMMdd_HHmmss}"
            });
            
            fieldDefinitions.Add(new FormFieldDefinition("description", "Description (Optional)", "textarea")
            {
                required = false,
                placeholder = "Enter scene description..."
            });

            FormSubmitPanelUIToolkit.Instance.Show(
                "Create New Scene",
                fieldDefinitions,
                (formData) =>
                {
                    string sceneName = formData["sceneName"].ToString();
                    if (string.IsNullOrEmpty(sceneName))
                    {
                        ShowMessageDialog("Error", "Scene name cannot be empty.");
                        return;
                    }
                    
                    _sandboxBuilder?.CreateNewScene(sceneName);
                    
                    if (formData.ContainsKey("description"))
                    {
                        // Store description if needed
                    }
                },
                () => { /* Cancelled */ },
                _panelPos
            );
        }
        
        private void OnSaveSceneClicked()
        {
            _sandboxBuilder?.SaveScene();
        }
        
        private void OnLoadSceneClicked()
        {
            Debug.Log("Load scene dialog not implemented yet");
        }
        
        private void OnClearSceneClicked()
        {
            ShowConfirmationDialogAsync("Clear Scene", 
                "Are you sure you want to clear all objects from the current scene?\n\nThis action cannot be undone.",
                () => _sandboxBuilder?.ClearScene(),
                () => { /* Cancelled */ }
            );
        }
        
        private void OnNewProjectClicked()
        {
            if (_sandboxBuilder?.CurrentProject != null)
            {
                ShowConfirmationDialogAsync("New Project", 
                    "Creating a new project will close the current project.\n\nDo you want to continue?",
                    () => ShowNewProjectDialog(),
                    () => { /* Cancelled */ }
                );
            }
            else
            {
                ShowNewProjectDialog();
            }
        }
        
        private void ShowNewProjectDialog()
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            fieldDefinitions.Add(new FormFieldDefinition("projectName", "Project Name", "text")
            {
                required = true,
                placeholder = "Enter project name...",
                defaultValue = $"Project_{DateTime.Now:yyyyMMdd_HHmmss}"
            });
            
            fieldDefinitions.Add(new FormFieldDefinition("description", "Description (Optional)", "textarea")
            {
                required = false,
                placeholder = "Enter project description..."
            });

            FormSubmitPanelUIToolkit.Instance.Show(
                "Create New Project",
                fieldDefinitions,
                (formData) =>
                {
                    string projectName = formData["projectName"].ToString();
                    if (string.IsNullOrEmpty(projectName))
                    {
                        ShowMessageDialog("Error", "Project name cannot be empty.");
                        return;
                    }
                    
                    _sandboxBuilder?.CreateNewProject(projectName);
                    UpdateProjectInfo();
                },
                () => { /* Cancelled */ },
                _panelPos
            );
        }
        
        private void OnSaveProjectClicked()
        {
            if (_sandboxBuilder?.CurrentProject == null)
            {
                ShowMessageDialog("No Project", "No project is currently loaded.");
                return;
            }
            
            _sandboxBuilder?.SaveProject();
        }
        
        private void OnLoadProjectClicked()
        {
            Debug.Log("Load project dialog not implemented yet");
        }
        
        private void OnClearProjectClicked()
        {
            if (_sandboxBuilder?.CurrentProject == null)
            {
                ShowMessageDialog("No Project", "No project is currently loaded.");
                return;
            }
            
            ShowConfirmationDialogAsync("Clear Project", 
                "Are you sure you want to close the current project?\n\nUnsaved changes will be lost.",
                () => {
                    _sandboxBuilder?.ClearScene();
                    if (_sandboxBuilder != null)
                    {
                        _sandboxBuilder.CurrentProject?.sceneConfiguration?.ClearPlacedObjects();
                    }
                },
                () => { /* Cancelled */ }
            );
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
            // Grid snapping is handled internally by SceneSandboxBuilder
            // We just store the preference here
            if (_snapToGridToggle != null)
            {
                Debug.Log($"Snap to grid: {enabled}");
            }
        }
        
        private void OnGridSizeChanged(float size)
        {
            if (_gridSizeText != null)
            {
                _gridSizeText.text = $"Grid Size: {size:F1}";
            }
            
            // Grid size would need to be exposed in SceneSandboxBuilder
            Debug.Log($"Grid size changed to: {size}");
        }
        
        // Sandbox events
        private void OnSceneLoaded(SceneConfiguration scene)
        {
            UpdateUI();
        }
        
        private void OnSceneSaved(SceneConfiguration scene)
        {
            UpdateUI();
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
                _previewStatusText.text = isInPreview ? "Preview Mode" : "Edit Mode";
            }
        }
        
        #endregion
        
        #region UI Updates
        
        private void UpdatePanelVisibility()
        {
            if (_leftPanel != null)
            {
                _leftPanel.style.display = _isPaletteVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        private void UpdateSceneInfo()
        {
            if (_sandboxBuilder?.CurrentScene == null) return;
            
            if (_sceneNameText != null)
            {
                _sceneNameText.text = $"Scene: {_sandboxBuilder.CurrentScene.sceneName}";
            }
            
            if (_objectCountText != null)
            {
                _objectCountText.text = $"Objects: {_sandboxBuilder.CurrentScene.placedObjects?.Count ?? 0}";
            }
        }
        
        private void UpdateProjectInfo()
        {
            if (_currentProjectText != null)
            {
                if (_sandboxBuilder?.CurrentProject != null)
                {
                    _currentProjectText.text = $"Project: {_sandboxBuilder.CurrentProject.projectName}";
                }
                else
                {
                    _currentProjectText.text = "No project loaded";
                }
            }
            
            if (_projectNameInput != null && _sandboxBuilder?.CurrentProject != null)
            {
                if (string.IsNullOrEmpty(_projectNameInput.value))
                {
                    _projectNameInput.value = _sandboxBuilder.CurrentProject.projectName;
                }
            }
        }
        
        private void UpdateObjectInfo()
        {
            // Object info can be displayed in properties panel
        }
        
        private void UpdateButtonStates()
        {
            bool hasScene = _sandboxBuilder?.CurrentScene != null;
            bool hasProject = _sandboxBuilder?.CurrentProject != null;
            bool isInPreview = _sandboxBuilder?.IsInPreviewMode ?? false;
            bool hasSelection = _selectedObject != null;
            
            SetButtonEnabled(_deleteObjectButton, hasSelection && !isInPreview);
            SetButtonEnabled(_previewButton, hasScene && !isInPreview);
            SetButtonEnabled(_stopPreviewButton, isInPreview);
            SetButtonEnabled(_saveSceneButton, hasScene && !isInPreview);
            SetButtonEnabled(_clearSceneButton, hasScene && !isInPreview);
            SetButtonEnabled(_saveProjectButton, hasProject);
            SetButtonEnabled(_clearProjectButton, hasProject);
        }
        
        #endregion
        
        #region Helper Methods
        
        private void ShowConfirmationDialogAsync(string title, string message, Action onConfirm, Action onCancel)
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            fieldDefinitions.Add(new FormFieldDefinition("message", "", "label")
            {
                defaultValue = message
            });
            
            FormSubmitPanelUIToolkit.Instance.Show(
                title,
                fieldDefinitions,
                (formData) => onConfirm?.Invoke(),
                () => onCancel?.Invoke(),
                _panelPos
            );
        }
        
        private void ShowMessageDialog(string title, string message)
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            fieldDefinitions.Add(new FormFieldDefinition("message", "", "label")
            {
                defaultValue = message
            });
            
            FormSubmitPanelUIToolkit.Instance.Show(
                title,
                fieldDefinitions,
                (formData) => { /* OK clicked */ },
                null,
                _panelPos
            );
        }
        
        private Button CreateButton(string text, string styleClass = null)
        {
            var button = new Button { text = text };
            button.AddToClassList("button");
            if (!string.IsNullOrEmpty(styleClass))
            {
                button.AddToClassList(styleClass);
            }
            return button;
        }
        
        private VisualElement CreateHorizontalGroup(string name)
        {
            var group = new VisualElement { name = name };
            group.style.flexDirection = FlexDirection.Row;
            group.style.marginTop = 8;
            return group;
        }
        
        private void SetButtonEnabled(Button button, bool enabled)
        {
            if (button != null)
            {
                button.SetEnabled(enabled);
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
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
