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
        
        [Header("Palette Resources")]
        [SerializeField] private SceneObjectLibrary _objectLibrary;
        [SerializeField] private VisualTreeAsset _paletteTemplate;
        [SerializeField] private VisualTreeAsset _itemTemplate;
        [SerializeField] private StyleSheet _paletteStyleSheet;
        
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
            
            // Initialize object palette if not assigned
            if (_objectPalette == null)
            {
                _objectPalette = FindFirstObjectByType<ObjectPaletteUIToolkit>();
                
                if (_objectPalette == null)
                {
                    Debug.LogWarning("SandboxBuilderUIToolkit: ObjectPaletteUIToolkit not found in scene. Creating new instance.");
                    CreateObjectPalette();
                }
            }
            
            // Setup palette integration
            if (_objectPalette != null)
            {
                SetupPaletteIntegration();
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
            if (_uiDocument.rootVisualElement == null && _mainUITemplate != null)
            {
                _mainUITemplate.CloneTree(_uiDocument.rootVisualElement);
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
        
        private void CreateObjectPalette()
        {
            // Create a new GameObject for the palette
            GameObject paletteGO = new GameObject("ObjectPaletteUIToolkit");
            paletteGO.transform.SetParent(transform);
            
            // Add the ObjectPaletteUIToolkit component
            _objectPalette = paletteGO.AddComponent<ObjectPaletteUIToolkit>();
            
            // Assign palette resources
            AssignPaletteResources();
            
            Debug.Log("SandboxBuilderUIToolkit: Created new ObjectPaletteUIToolkit instance.");
        }
        
        private void AssignPaletteResources()
        {
            if (_objectPalette == null) return;
            
            // Use reflection to set private serialized fields
            var paletteType = typeof(ObjectPaletteUIToolkit);
            
            // Set object library
            if (_objectLibrary != null)
            {
                var objectLibraryField = paletteType.GetField("_objectLibrary", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (objectLibraryField != null)
                {
                    objectLibraryField.SetValue(_objectPalette, _objectLibrary);
                    Debug.Log("SandboxBuilderUIToolkit: Assigned ObjectLibrary to palette.");
                }
            }
            
            // Set palette template
            if (_paletteTemplate != null)
            {
                var paletteTemplateField = paletteType.GetField("_paletteTemplate", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (paletteTemplateField != null)
                {
                    paletteTemplateField.SetValue(_objectPalette, _paletteTemplate);
                    Debug.Log("SandboxBuilderUIToolkit: Assigned PaletteTemplate to palette.");
                }
            }
            
            // Set item template
            if (_itemTemplate != null)
            {
                var itemTemplateField = paletteType.GetField("_itemTemplate", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (itemTemplateField != null)
                {
                    itemTemplateField.SetValue(_objectPalette, _itemTemplate);
                    Debug.Log("SandboxBuilderUIToolkit: Assigned ItemTemplate to palette.");
                }
            }
            
            // Set palette stylesheet
            if (_paletteStyleSheet != null)
            {
                var paletteStyleSheetField = paletteType.GetField("_paletteStyleSheet", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (paletteStyleSheetField != null)
                {
                    paletteStyleSheetField.SetValue(_objectPalette, _paletteStyleSheet);
                    Debug.Log("SandboxBuilderUIToolkit: Assigned StyleSheet to palette.");
                }
            }
            
            // Set sandbox builder reference
            if (_sandboxBuilder != null)
            {
                var sandboxBuilderField = paletteType.GetField("_sandboxBuilder", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (sandboxBuilderField != null)
                {
                    sandboxBuilderField.SetValue(_objectPalette, _sandboxBuilder);
                    Debug.Log("SandboxBuilderUIToolkit: Assigned SandboxBuilder to palette.");
                }
            }
        }
        
        private void SetupPaletteIntegration()
        {
            if (_objectPalette == null) return;
            
            // Ensure palette has all required resources
            AssignPaletteResources();
            
            // Subscribe to palette events
            _objectPalette.OnObjectSelected += OnPaletteObjectSelected;
            _objectPalette.OnObjectDraggedToScene += OnPaletteObjectDraggedToScene;
            
            // Attach palette UI to the palette-container in the main UI
            AttachPaletteToContainer();
            
            Debug.Log("SandboxBuilderUIToolkit: Palette integration setup complete.");
        }
        
        private void AttachPaletteToContainer()
        {
            if (_objectPalette == null || _rootElement == null) return;
            
            // Find the palette container in the main UI
            var paletteContainer = _rootElement.Q<VisualElement>("palette-container");
            
            if (paletteContainer == null)
            {
                Debug.LogWarning("SandboxBuilderUIToolkit: palette-container not found in UXML. Cannot attach palette UI.");
                return;
            }
            
            // Get the palette's UI root element
            // The palette manages its own UIDocument, so we need to get its root and move it
            var paletteUIDocument = _objectPalette.GetComponent<UIDocument>();
            
            if (paletteUIDocument == null)
            {
                Debug.LogWarning("SandboxBuilderUIToolkit: ObjectPalette has no UIDocument. Cannot attach UI.");
                return;
            }
            
            // Wait for palette to initialize, then move its root element to our container
            // We need to do this after the palette has created its UI
            StartCoroutine(AttachPaletteUICoroutine(paletteContainer, paletteUIDocument));
        }
        
        private System.Collections.IEnumerator AttachPaletteUICoroutine(VisualElement paletteContainer, UIDocument paletteUIDocument)
        {
            // Wait for one frame to ensure palette UI is initialized
            yield return null;
            
            // Get the palette's root container
            var paletteRoot = paletteUIDocument.rootVisualElement.Q<VisualElement>("palette-container");
            
            if (paletteRoot == null)
            {
                Debug.LogWarning("SandboxBuilderUIToolkit: Could not find palette-container in ObjectPalette UI.");
                yield break;
            }
            
            // Clear the palette container in our main UI
            paletteContainer.Clear();
            
            // Remove palette root from its current parent
            paletteRoot.RemoveFromHierarchy();
            
            // Add it to our main UI's palette container
            paletteContainer.Add(paletteRoot);
            
            // Ensure it takes up full space
            paletteRoot.style.flexGrow = 1;
            
            // Disable the palette's own UIDocument to avoid rendering conflicts
            paletteUIDocument.enabled = false;
            
            Debug.Log("SandboxBuilderUIToolkit: Successfully attached palette UI to palette-container.");
        }
        
        private void OnPaletteObjectSelected(SceneObjectData objectData)
        {
            Debug.Log($"SandboxBuilderUIToolkit: Object selected from palette: {objectData.displayName}");
            // Additional logic when an object is selected from palette
        }
        
        private void OnPaletteObjectDraggedToScene(SceneObjectData objectData, Vector2 screenPosition)
        {
            Debug.Log($"SandboxBuilderUIToolkit: Object dragged to scene: {objectData.displayName}");
            // The ObjectPalette already handles placement, but we can add additional logic here
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
        /// Set the object palette reference
        /// </summary>
        public void SetObjectPalette(ObjectPaletteUIToolkit palette)
        {
            // Unsubscribe from old palette if exists
            if (_objectPalette != null)
            {
                _objectPalette.OnObjectSelected -= OnPaletteObjectSelected;
                _objectPalette.OnObjectDraggedToScene -= OnPaletteObjectDraggedToScene;
            }
            
            _objectPalette = palette;
            
            // Subscribe to new palette
            if (_objectPalette != null)
            {
                SetupPaletteIntegration();
            }
        }
        
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
            
            // The palette UI is now integrated into our UIDocument, so we don't need to
            // control the palette GameObject's active state separately
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
            // Unsubscribe from sandbox builder events
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
            
            // Unsubscribe from palette events
            if (_objectPalette != null)
            {
                _objectPalette.OnObjectSelected -= OnPaletteObjectSelected;
                _objectPalette.OnObjectDraggedToScene -= OnPaletteObjectDraggedToScene;
            }
        }
    }
}
