using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SceneSandbox.Core;
using SceneSandbox.Data;
using Core.UI.FormSubmit;
using System;
using System.Collections.Generic;

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
        [SerializeField] private GameObject _sceneControlsPanel;
        [SerializeField] private GameObject _previewPanel;
        [SerializeField] private Transform _panelPos;
        
        // Properties panel will be handled by FormSubmitPanel dynamically
        
        [Header("Scene Controls")]
        [SerializeField] private Button _newSceneButton;
        [SerializeField] private Button _saveSceneButton;
        [SerializeField] private Button _loadSceneButton;
        [SerializeField] private Button _clearSceneButton;
        [SerializeField] private Button _previewButton;
        [SerializeField] private Button _stopPreviewButton;
        
        [Header("Project Controls")]
        [SerializeField] private Button _newProjectButton;
        [SerializeField] private Button _saveProjectButton;
        [SerializeField] private Button _loadProjectButton;
        [SerializeField] private Button _clearProjectButton;
        [SerializeField] private TMP_InputField _projectNameInput;
        [SerializeField] private TextMeshProUGUI _currentProjectText;
        
        [Header("Object Controls")]
        [SerializeField] private Button _deleteObjectButton;
        [SerializeField] private Toggle _snapToGridToggle;
        [SerializeField] private Slider _gridSizeSlider;
        [SerializeField] private TextMeshProUGUI _gridSizeText;
        
        [Header("Properties Panel")]
        // Properties panel fields removed - now handled dynamically by FormSubmitPanel
        [SerializeField] private Transform _positionInputs;
        [SerializeField] private Transform _rotationInputs;
        [SerializeField] private Transform _scaleInputs;
        
        [Header("Scene Info")]
        [SerializeField] private TextMeshProUGUI _sceneNameText;
        [SerializeField] private TextMeshProUGUI _objectCountText;
        [SerializeField] private TextMeshProUGUI _previewStatusText;
        
        [Header("Mobile-Specific")]
        [SerializeField] private GameObject _mobileControlsPanel;
        [SerializeField] private Button _mobilePaletteToggle;
        [SerializeField] private Button _mobilePropertiesToggle;
        
        // Core References
        private Core.SceneSandboxBuilder _sandboxBuilder;
        private GameObject _selectedObject;
        private bool _isPaletteVisible = true;
        private bool _arePropertiesVisible = false;
        
        // Properties panel state
        private bool _isPropertiesPanelShowing = false;
        
        // Input fields for transform editing
        private TMP_InputField[] _positionFields;
        private TMP_InputField[] _rotationFields;
        private TMP_InputField[] _scaleFields;
        
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
            
            // Project control buttons
            if (_newProjectButton != null)
                _newProjectButton.onClick.AddListener(OnNewProjectClicked);
            
            if (_saveProjectButton != null)
                _saveProjectButton.onClick.AddListener(OnSaveProjectClicked);
            
            if (_loadProjectButton != null)
                _loadProjectButton.onClick.AddListener(OnLoadProjectClicked);
            
            if (_clearProjectButton != null)
                _clearProjectButton.onClick.AddListener(OnClearProjectClicked);
            
            // Object control buttons
            if (_deleteObjectButton != null)
                _deleteObjectButton.onClick.AddListener(OnDeleteObjectClicked);
            
            // Settings controls
            if (_snapToGridToggle != null)
                _snapToGridToggle.onValueChanged.AddListener(OnSnapToGridChanged);
            
            if (_gridSizeSlider != null)
                _gridSizeSlider.onValueChanged.AddListener(OnGridSizeChanged);
            
            // Properties panel now handled by FormSubmitPanel
            
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
                _positionFields = _positionInputs.GetComponentsInChildren<TMP_InputField>();
                for (int i = 0; i < _positionFields.Length && i < 3; i++)
                {
                    int index = i; // Capture for closure
                    _positionFields[i].onEndEdit.AddListener(value => OnPositionChanged(index, value));
                }
            }
            
            // Setup rotation input fields
            if (_rotationInputs != null)
            {
                _rotationFields = _rotationInputs.GetComponentsInChildren<TMP_InputField>();
                for (int i = 0; i < _rotationFields.Length && i < 3; i++)
                {
                    int index = i; // Capture for closure
                    _rotationFields[i].onEndEdit.AddListener(value => OnRotationChanged(index, value));
                }
            }
            
            // Setup scale input fields
            if (_scaleInputs != null)
            {
                _scaleFields = _scaleInputs.GetComponentsInChildren<TMP_InputField>();
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
            if (_selectedObject != null)
            {
                if (!_isPropertiesPanelShowing)
                {
                    ShowObjectPropertiesPanel();
                }
                else
                {
                    FormSubmitPanel.Instance.CloseForm();
                    _isPropertiesPanelShowing = false;
                }
            }
            else
            {
                ShowMessageDialog("No Object Selected", "Please select an object to edit its properties.");
            }
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
        /// Show or hide the object properties using FormSubmitPanel
        /// </summary>
        public void SetPropertiesVisible(bool visible)
        {
            if (visible && _selectedObject != null)
            {
                ShowObjectPropertiesPanel();
            }
            else if (!visible && _isPropertiesPanelShowing)
            {
                FormSubmitPanel.Instance.CloseForm();
                _isPropertiesPanelShowing = false;
            }
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
        
        /// <summary>
        /// Show object properties panel using FormSubmitPanel
        /// </summary>
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
            
            FormSubmitPanel.Instance.Show(
                $"Properties: {_selectedObject.name}",
                fieldDefinitions,
                (formData) => {
                    ApplyObjectProperties(formData);
                    _isPropertiesPanelShowing = false;
                },
                () => {
                    _isPropertiesPanelShowing = false;
                },
                _panelPos
            );
        }
        
        /// <summary>
        /// Apply properties from form data to the selected object
        /// </summary>
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
            
            // Update UI to reflect changes
            UpdateObjectInfo();
        }
        
        #endregion
        
        #region Event Handlers
        
        // Scene control event handlers
        private void OnNewSceneClicked()
        {
            ShowNewSceneDialog();
        }
        
        /// <summary>
        /// Show new scene creation dialog using FormSubmitPanel
        /// </summary>
        private void ShowNewSceneDialog()
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            // Scene name input
            fieldDefinitions.Add(new FormFieldDefinition("sceneName", "Scene Name", "text")
            {
                required = true,
                placeholder = "Enter scene name...",
                defaultValue = $"Scene_{System.DateTime.Now:yyyyMMdd_HHmmss}"
            });
            
            // Optional description
            fieldDefinitions.Add(new FormFieldDefinition("description", "Description (Optional)", "textarea")
            {
                required = false,
                placeholder = "Enter scene description..."
            });

            FormSubmitPanel.Instance.Show(
                "Create New Scene",
                fieldDefinitions,
                (formData) =>
                {
                    string sceneName = formData["sceneName"].ToString().Trim();

                    if (string.IsNullOrEmpty(sceneName))
                    {
                        ShowMessageDialog("Error", "Please enter a valid scene name.");
                        return;
                    }

                    // Create the scene
                    _sandboxBuilder?.CreateNewScene(sceneName);
                    Debug.Log($"Created new scene: {sceneName}");

                    ShowMessageDialog("Success", $"Scene '{sceneName}' created successfully!");
                },
                () =>
                {
                    // Cancel callback - do nothing
                },
                _panelPos
            );
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
            ShowConfirmationDialogAsync("Clear Scene", 
                "Are you sure you want to clear all objects from the current scene?\n\nThis action cannot be undone.",
                () => _sandboxBuilder?.ClearScene(),
                () => { /* Cancel - do nothing */ }
            );
        }
        
        // Project control event handlers
        private void OnNewProjectClicked()
        {
            // Check if there's an existing project with unsaved changes
            if (_sandboxBuilder?.CurrentProject != null)
            {
                ShowConfirmationDialogAsync("New Project", 
                    "Creating a new project will close the current project.\n\nDo you want to continue?",
                    () => ShowNewProjectDialog(),
                    () => { /* Cancel - do nothing */ }
                );
            }
            else
            {
                ShowNewProjectDialog();
            }
        }
        
        /// <summary>
        /// Show new project creation dialog using FormSubmitPanel
        /// </summary>
        private void ShowNewProjectDialog()
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            // Project name input
            fieldDefinitions.Add(new FormFieldDefinition("projectName", "Project Name", "text")
            {
                required = true,
                placeholder = "Enter project name...",
                defaultValue = GetProjectNameFromInput()
            });
            
            // Optional description
            fieldDefinitions.Add(new FormFieldDefinition("description", "Description (Optional)", "textarea")
            {
                required = false,
                placeholder = "Enter project description..."
            });
            
            // Project settings section
            fieldDefinitions.Add(new FormFieldDefinition("settings_header", "Project Settings", "info")
            {
                defaultValue = "Configure initial project settings:"
            });
            
            // Grid settings
            fieldDefinitions.Add(new FormFieldDefinition("snapToGrid", "Snap to Grid", "toggle")
            {
                defaultValue = true
            });
            
            fieldDefinitions.Add(new FormFieldDefinition("gridSize", "Grid Size", "slider")
            {
                defaultValue = 1.0f,
                options = new Dictionary<string, object>
                {
                    ["min"] = 0.5f,
                    ["max"] = 5.0f,
                    ["step"] = 0.1f
                }
            });
            
            FormSubmitPanel.Instance.Show(
                "Create New Project",
                fieldDefinitions,
                (formData) => {
                    string projectName = formData["projectName"].ToString().Trim();

                    if (string.IsNullOrEmpty(projectName))
                    {
                        ShowMessageDialog("Error", "Please enter a valid project name.");
                        return;
                    }

                    // Create the project
                    _sandboxBuilder?.CreateNewProject(projectName);
                    Debug.Log($"Created new project: {projectName}");

                    // Update UI
                    if (_projectNameInput != null)
                    {
                        _projectNameInput.text = projectName;
                    }

                    ShowMessageDialog("Success", $"Project '{projectName}' created successfully!");
                },
                () => {
                    // Cancel callback - do nothing
                },
                _panelPos
            );
        }
        
        private void OnSaveProjectClicked()
        {
            if (_sandboxBuilder?.CurrentProject == null)
            {
                ShowMessageDialog("Error", "No project is currently loaded to save.");
                return;
            }
            
            ShowSaveProjectDialog();
        }
        
        /// <summary>
        /// Show save project dialog using FormSubmitPanel
        /// </summary>
        private void ShowSaveProjectDialog()
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            // Current project info
            var currentProject = _sandboxBuilder.CurrentProject;
            fieldDefinitions.Add(new FormFieldDefinition("projectInfo", "Current Project", "info")
            {
                defaultValue = $"Project: {currentProject.projectName}\nObjects: {_sandboxBuilder.CurrentScene?.placedObjects?.Count ?? 0}"
            });
            
            // Save options
            fieldDefinitions.Add(new FormFieldDefinition("saveOption", "Save Option", "select")
            {
                required = true,
                defaultValue = "quickSave",
                options = new Dictionary<string, object>
                {
                    ["options"] = new List<string> { "Quick Save", "Save As..." }
                }
            });
            
            // Custom file name (shown conditionally)
            fieldDefinitions.Add(new FormFieldDefinition("customFileName", "File Name (Optional)", "text")
            {
                required = false,
                placeholder = "Leave empty for default name",
                tooltip = "Custom file name for the project"
            });
            
            FormSubmitPanel.Instance.Show(
                "Save Project",
                fieldDefinitions,
                (formData) => {
                    string saveOption = formData["saveOption"].ToString();
                    string customFileName = formData.ContainsKey("customFileName") ? 
                        formData["customFileName"].ToString().Trim() : "";
                    
                    string savePath = null;
                    if (!string.IsNullOrEmpty(customFileName))
                    {
                        savePath = System.IO.Path.Combine(
                            _sandboxBuilder.CurrentProject.projectName, 
                            customFileName + ".sbproj"
                        );
                    }
                    
                    if (_sandboxBuilder.SaveProject(savePath))
                    {
                        ShowMessageDialog("Success", "Project saved successfully!");
                    }
                    else
                    {
                        ShowMessageDialog("Error", "Failed to save project. Please check the console for details.");
                    }
                },
                () => {
                    // Cancel callback - do nothing
                },
                _panelPos
            );
        }
        
        private void OnLoadProjectClicked()
        {
            ShowProjectLoadDialog();
        }
        
        private void OnClearProjectClicked()
        {
            if (_sandboxBuilder?.CurrentProject == null)
            {
                ShowMessageDialog("Info", "No project is currently loaded.");
                return;
            }
            
            ShowConfirmationDialogAsync("Clear Project", 
                $"Are you sure you want to close the current project '{_sandboxBuilder.CurrentProject.projectName}'?\n\nUnsaved changes will be lost.",
                () => ClearCurrentProject(),
                () => { /* Cancel - do nothing */ }
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
            if (_sandboxBuilder != null)
            {
                // TODO: Add SetSnapToGrid method to SceneSandboxBuilder
                Debug.Log($"Snap to grid: {enabled}");
            }
        }
        
        private void OnGridSizeChanged(float size)
        {
            if (_gridSizeText != null)
            {
                _gridSizeText.text = size.ToString("F1");
            }
            
            if (_sandboxBuilder != null)
            {
                // TODO: Add SetGridSize method to SceneSandboxBuilder
                Debug.Log($"Grid size changed to: {size}");
            }
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
            
            // Properties panel is now handled by FormSubmitPanel dynamically
            // No static panel to toggle
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
            
            // Update project info
            UpdateProjectInfo();
        }
        
        /// <summary>
        /// Update project information display
        /// </summary>
        private void UpdateProjectInfo()
        {
            if (_currentProjectText != null)
            {
                if (_sandboxBuilder?.CurrentProject != null)
                {
                    var project = _sandboxBuilder.CurrentProject;
                    _currentProjectText.text = $"Project: {project.projectName}";
                }
                else
                {
                    _currentProjectText.text = "No Project Loaded";
                }
            }
            
            // Update project name input field
            if (_projectNameInput != null && _sandboxBuilder?.CurrentProject != null)
            {
                if (string.IsNullOrEmpty(_projectNameInput.text))
                {
                    _projectNameInput.text = _sandboxBuilder.CurrentProject.projectName;
                }
            }
        }
        
        private void UpdateObjectInfo()
        {
            bool hasSelection = _selectedObject != null;
            
            // Object info is now displayed in FormSubmitPanel when properties are shown
            // Static UI elements removed in favor of dynamic form-based editing
            
            // Update transform inputs if they exist
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
            bool hasProject = _sandboxBuilder?.CurrentProject != null;
            bool hasScene = _sandboxBuilder?.CurrentScene != null;
            
            // Object control buttons
            if (_deleteObjectButton != null)
                _deleteObjectButton.interactable = hasSelection && !isInPreview;
            
            // Scene control buttons
            if (_previewButton != null)
                _previewButton.interactable = !isInPreview && hasScene;
            
            if (_stopPreviewButton != null)
                _stopPreviewButton.interactable = isInPreview;
            
            if (_saveSceneButton != null)
                _saveSceneButton.interactable = hasScene && !isInPreview;
            
            if (_clearSceneButton != null)
                _clearSceneButton.interactable = hasScene && !isInPreview;
            
            // Project control buttons
            if (_newProjectButton != null)
                _newProjectButton.interactable = !isInPreview;
            
            if (_saveProjectButton != null)
                _saveProjectButton.interactable = hasProject && !isInPreview;
            
            if (_loadProjectButton != null)
                _loadProjectButton.interactable = !isInPreview;
            
            if (_clearProjectButton != null)
                _clearProjectButton.interactable = hasProject && !isInPreview;
            
            // Disable editing controls during preview
            if (_objectPalette != null)
                _objectPalette.gameObject.SetActive(!isInPreview);
            
            // Update project name input
            if (_projectNameInput != null)
                _projectNameInput.interactable = !isInPreview;
        }
        
        #endregion
        
        #region Project Management Helpers
        
        /// <summary>
        /// Get project name from input field or generate default name
        /// </summary>
        private string GetProjectNameFromInput()
        {
            if (_projectNameInput != null && !string.IsNullOrEmpty(_projectNameInput.text))
            {
                return _projectNameInput.text.Trim();
            }
            
            // Generate default name with timestamp
            return $"Project_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        }
        
        /// <summary>
        /// Clear the current project and reset UI
        /// </summary>
        private void ClearCurrentProject()
        {
            // Clear the current project reference
            // Note: SceneSandboxBuilder doesn't have a direct "clear project" method,
            // so we'll create a new empty scene which effectively clears the project state
            _sandboxBuilder?.CreateNewScene();
            
            // Reset project name input
            if (_projectNameInput != null)
            {
                _projectNameInput.text = "";
            }
            
            UpdateUI();
            Debug.Log("Project cleared successfully.");
        }
        
        /// <summary>
        /// Show a simple project load dialog with available projects
        /// </summary>
        private void ShowProjectLoadDialog()
        {
            if (_sandboxBuilder == null)
            {
                ShowMessageDialog("Error", "Sandbox Builder not found.");
                return;
            }
            
            var availableProjects = _sandboxBuilder.GetAvailableProjects();
            
            if (availableProjects == null || availableProjects.Count == 0)
            {
                ShowMessageDialog("Info", "No saved projects found in the default directory.");
                return;
            }
            
            // Create form fields for project selection
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            // Project selection dropdown
            var projectOptions = new Dictionary<string, object>();
            var projectNames = new List<string>();
            var projectPaths = new Dictionary<string, string>();
            
            foreach (var project in availableProjects)
            {
                var displayName = $"{project.projectName} ({project.lastModified:yyyy-MM-dd HH:mm})";
                projectNames.Add(displayName);
                projectPaths[displayName] = project.filePath;
                projectOptions.Add(displayName, project.filePath);
            }
            
            projectOptions["items"] = projectNames;
            
            fieldDefinitions.Add(new FormFieldDefinition("selectedProject", "Select Project", "select")
            {
                required = true,
                options = projectOptions,
                defaultValue = projectNames.Count > 0 ? projectNames[0] : ""
            });
            
            // Show info about selected project
            if (availableProjects.Count > 0)
            {
                var firstProject = availableProjects[0];
                fieldDefinitions.Add(new FormFieldDefinition("projectInfo", "Project Information", "info")
                {
                    defaultValue = $"Created: {firstProject.created:yyyy-MM-dd HH:mm}\nLast Modified: {firstProject.lastModified:yyyy-MM-dd HH:mm}\nObjects: {firstProject.objectCount}"
                });
            }
            
            FormSubmitPanel.Instance.Show(
                "Load Project",
                fieldDefinitions,
                (formData) => {
                    if (formData.ContainsKey("selectedProject"))
                    {
                        var selectedProjectName = formData["selectedProject"].ToString();
                        if (projectPaths.ContainsKey(selectedProjectName))
                        {
                            var projectPath = projectPaths[selectedProjectName];
                            if (_sandboxBuilder.LoadProject(projectPath))
                            {
                                var projectName = selectedProjectName.Split('(')[0].Trim();
                                ShowMessageDialog("Success", $"Project '{projectName}' loaded successfully!");
                                
                                // Update project name input
                                if (_projectNameInput != null)
                                {
                                    _projectNameInput.text = projectName;
                                }
                            }
                            else
                            {
                                ShowMessageDialog("Error", "Failed to load project. Please check the console for details.");
                            }
                        }
                    }
                },
                () => {
                    // Cancel callback - do nothing
                },
                _panelPos
            );
        }
        
        /// <summary>
        /// Show a confirmation dialog using FormSubmitPanel
        /// </summary>
        private bool ShowConfirmationDialog(string title, string message)
        {
            // For confirmation dialogs, we need to handle the async nature differently
            // We'll use a callback-based approach instead
            ShowConfirmationDialogAsync(title, message, null, null);
            
            // For editor mode, fall back to Unity's dialog
            #if UNITY_EDITOR
            return UnityEditor.EditorUtility.DisplayDialog(title, message, "Yes", "No");
            #else
            // In runtime, we return true and handle the actual confirmation via callback
            return true;
            #endif
        }
        
        /// <summary>
        /// Show a confirmation dialog with callbacks using FormSubmitPanel
        /// </summary>
        private void ShowConfirmationDialogAsync(string title, string message, System.Action onConfirm, System.Action onCancel)
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            // Add info field with the message
            fieldDefinitions.Add(new FormFieldDefinition("message", "", "info")
            {
                defaultValue = message
            });
            
            // Add confirm and cancel buttons
            fieldDefinitions.Add(new FormFieldDefinition("confirm", "Yes", "button")
            {
                options = new Dictionary<string, object> { ["action"] = "confirm" }
            });
            
            fieldDefinitions.Add(new FormFieldDefinition("cancel", "No", "button")
            {
                options = new Dictionary<string, object> { ["action"] = "cancel" }
            });
            
            FormSubmitPanel.Instance.Show(
                title,
                fieldDefinitions,
                (formData) => {
                    if (formData.ContainsKey("action"))
                    {
                        string action = formData["action"].ToString();
                        if (action == "confirm")
                        {
                            onConfirm?.Invoke();
                        }
                        else if (action == "cancel")
                        {
                            onCancel?.Invoke();
                        }
                    }
                },
                () => {
                    onCancel?.Invoke();
                },
                _panelPos
            );
        }
        
        /// <summary>
        /// Show a message dialog using FormSubmitPanel
        /// </summary>
        private void ShowMessageDialog(string title, string message)
        {
            var fieldDefinitions = new List<FormFieldDefinition>();
            
            // Add info field with the message
            fieldDefinitions.Add(new FormFieldDefinition("message", "", "info")
            {
                defaultValue = message
            });
            
            FormSubmitPanel.Instance.Show(
                title,
                fieldDefinitions,
                (formData) => {
                    // OK button pressed - just close
                },
                () => {
                    // Cancel/close - same as OK for message dialogs
                },
                _panelPos
            );
        }
        
        #endregion
        
        #region UI Creation
        
        /// <summary>
        /// Creates UI elements programmatically if they're not assigned in the inspector
        /// </summary>
        private void CreateUIIfMissing()
        {
            // Create main UI structure if nothing is assigned
            bool needsCompleteUI = _sceneControlsPanel == null;
            
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
            CreateMobileControlsPanel();
            
            // Properties panel is now handled dynamically by FormSubmitPanel
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
            controlsRect.sizeDelta = new Vector2(0, 120); // Increased height for project controls
            controlsRect.anchoredPosition = Vector2.zero;
            
            var controlsImage = controlsGO.AddComponent<Image>();
            controlsImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Add vertical layout for multiple rows
            var layoutGroup = controlsGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.spacing = 5;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            
            // Create project controls row
            CreateProjectControlsRow(controlsGO.transform);
            
            // Create scene controls row
            CreateSceneControlsRow(controlsGO.transform);
            
            // Create info row
            CreateInfoRow(controlsGO.transform);
        }
        
        private void CreateProjectControlsRow(Transform parent)
        {
            var projectRowGO = new GameObject("Project Controls Row");
            projectRowGO.transform.SetParent(parent, false);
            
            var projectLayout = projectRowGO.AddComponent<HorizontalLayoutGroup>();
            projectLayout.spacing = 10;
            projectLayout.childAlignment = TextAnchor.MiddleLeft;
            
            // Project name input
            _projectNameInput = CreateInputField(projectRowGO.transform, "Enter project name...");
            var projectInputLayout = _projectNameInput.GetComponent<LayoutElement>();
            if (projectInputLayout == null)
                projectInputLayout = _projectNameInput.gameObject.AddComponent<LayoutElement>();
            projectInputLayout.preferredWidth = 200;
            
            // Project control buttons
            _newProjectButton = CreateButton(projectRowGO.transform, "New Project");
            _saveProjectButton = CreateButton(projectRowGO.transform, "Save Project");
            _loadProjectButton = CreateButton(projectRowGO.transform, "Load Project");
            _clearProjectButton = CreateButton(projectRowGO.transform, "Clear Project");
        }
        
        private void CreateSceneControlsRow(Transform parent)
        {
            var sceneRowGO = new GameObject("Scene Controls Row");
            sceneRowGO.transform.SetParent(parent, false);
            
            var sceneLayout = sceneRowGO.AddComponent<HorizontalLayoutGroup>();
            sceneLayout.spacing = 10;
            sceneLayout.childAlignment = TextAnchor.MiddleLeft;
            
            // Create scene control buttons
            _newSceneButton = CreateButton(sceneRowGO.transform, "New Scene");
            _saveSceneButton = CreateButton(sceneRowGO.transform, "Save Scene");
            _loadSceneButton = CreateButton(sceneRowGO.transform, "Load Scene");
            _clearSceneButton = CreateButton(sceneRowGO.transform, "Clear Scene");
            _previewButton = CreateButton(sceneRowGO.transform, "Preview");
            _stopPreviewButton = CreateButton(sceneRowGO.transform, "Stop Preview");
        }
        
        private void CreateInfoRow(Transform parent)
        {
            var infoRowGO = new GameObject("Info Row");
            infoRowGO.transform.SetParent(parent, false);
            
            var infoLayout = infoRowGO.AddComponent<HorizontalLayoutGroup>();
            infoLayout.spacing = 20;
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            // Scene and project info
            _currentProjectText = CreateText(infoRowGO.transform, "No Project Loaded");
            _sceneNameText = CreateText(infoRowGO.transform, "New Scene");
            _objectCountText = CreateText(infoRowGO.transform, "Objects: 0");
            _previewStatusText = CreateText(infoRowGO.transform, "EDIT MODE");
        }
        
        // Properties panel functionality moved to FormSubmitPanel-based ShowObjectPropertiesPanel method
        
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
            
            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            return button;
        }
        
        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize = 12, FontStyle style = FontStyle.Normal)
        {
            var textGO = new GameObject($"Text_{text.Replace(" ", "").Substring(0, Mathf.Min(10, text.Length))}");
            textGO.transform.SetParent(parent, false);
            
            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            
            // Convert Unity FontStyle to TextMeshPro FontStyles
            switch (style)
            {
                case FontStyle.Bold:
                    textComponent.fontStyle = FontStyles.Bold;
                    break;
                case FontStyle.Italic:
                    textComponent.fontStyle = FontStyles.Italic;
                    break;
                case FontStyle.BoldAndItalic:
                    textComponent.fontStyle = FontStyles.Bold | FontStyles.Italic;
                    break;
                default:
                    textComponent.fontStyle = FontStyles.Normal;
                    break;
            }
            
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            
            var layoutElement = textGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = fontSize + 4;
            
            return textComponent;
        }
        
        private TMP_InputField CreateInputField(Transform parent, string placeholder)
        {
            var inputGO = new GameObject($"InputField_{placeholder.Replace(" ", "")}");
            inputGO.transform.SetParent(parent, false);
            
            var inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(200, 25);
            
            var inputImage = inputGO.AddComponent<Image>();
            inputImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            var inputField = inputGO.AddComponent<TMP_InputField>();
            
            // Create text component for input field
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            
            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            
            inputField.textComponent = textComponent;
            
            // Create placeholder
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputGO.transform, false);
            var placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(5, 0);
            placeholderRect.offsetMax = new Vector2(-5, 0);
            
            var placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            
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
            
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            
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