using UnityEngine;
using UnityEngine.UIElements;
using SceneSandbox.Core;
using SceneSandbox.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.UI.FormSubmit;
using Core.UI.FormSubmit.Fields;

namespace SceneSandbox.UI
{
    public class SandboxBuilderUIToolkit : MonoBehaviour
    {
        private VisualElement _root;
        private SceneSandboxBuilder _sandboxBuilder;
        private GameObject _selectedObject;

        // Bottom Bar Elements
        private Button _buildModeButton;
        private Button _inspectButton;
        private Button _projectButton;
        private Button _sceneButton;
        private Button _previewButton;
        private Button _stopPreviewButton;
        private Label _currentProjectText;
        private Label _sceneNameText;
        private Label _objectCountText;
        private Label _previewStatusText;

        // Floating Panels
        private VisualElement _objectPalette;
        private VisualElement _propertiesPanel;

        // Palette Controls
        private ScrollView _objectPaletteScrollView;
        private DropdownField _typeFilter;
        private DropdownField _categoryFilter;
        private TextField _searchField;

        // Properties Controls
        private Button _deleteObjectButton;
        private Toggle _snapToGridToggle;
        private Slider _gridSizeSlider;
        private Label _gridSizeText;

        // Dialog Container
        private VisualElement _dialogContainer;

        private void OnEnable()
        {
            _sandboxBuilder = FindObjectOfType<SceneSandboxBuilder>();
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;

            QueryUIElements();
            RegisterCallbacks();

            if (_sandboxBuilder != null)
            {
                BindSandboxEvents();
                UpdateUI();
                PopulateFilterDropdowns();
                PopulateObjectPalette();
            }
        }

        private void QueryUIElements()
        {
            // Bottom Bar
            _buildModeButton = _root.Q<Button>("build-mode-button");
            _inspectButton = _root.Q<Button>("inspect-button");
            _projectButton = _root.Q<Button>("project-button");
            _sceneButton = _root.Q<Button>("scene-button");
            _previewButton = _root.Q<Button>("preview-button");
            _stopPreviewButton = _root.Q<Button>("stop-preview-button");
            _currentProjectText = _root.Q<Label>("current-project-text");
            _sceneNameText = _root.Q<Label>("scene-name-text");
            _objectCountText = _root.Q<Label>("object-count-text");
            _previewStatusText = _root.Q<Label>("preview-status-text");

            // Panels
            _objectPalette = _root.Q<VisualElement>("object-palette");
            _propertiesPanel = _root.Q<VisualElement>("properties-panel");

            // Palette Content
            _objectPaletteScrollView = _root.Q<ScrollView>("object-palette-scroll-view");
            _typeFilter = _root.Q<DropdownField>("type-filter");
            _categoryFilter = _root.Q<DropdownField>("category-filter");
            _searchField = _root.Q<TextField>("search-field");

            // Properties Content
            _deleteObjectButton = _root.Q<Button>("delete-object-button");
            _snapToGridToggle = _root.Q<Toggle>("snap-to-grid-toggle");
            _gridSizeSlider = _root.Q<Slider>("grid-size-slider");
            _gridSizeText = _root.Q<Label>("grid-size-text");

            // Dialogs
            _dialogContainer = _root.Q<VisualElement>("dialog-container");
        }

        private void RegisterCallbacks()
        {
            // Bottom Bar
            _buildModeButton?.clicked += ToggleObjectPalette;
            _inspectButton?.clicked += TogglePropertiesPanel;
            _projectButton?.clicked += ShowProjectDialog;
            _sceneButton?.clicked += ShowSceneDialog;
            _previewButton?.clicked += OnPreviewClicked;
            _stopPreviewButton?.clicked += OnStopPreviewClicked;

            // Palette
            _typeFilter?.RegisterValueChangedCallback(evt => PopulateObjectPalette());
            _categoryFilter?.RegisterValueChangedCallback(evt => PopulateObjectPalette());
            _searchField?.RegisterValueChangedCallback(evt => PopulateObjectPalette());

            // Properties
            _deleteObjectButton?.clicked += OnDeleteObjectClicked;
            _snapToGridToggle?.RegisterValueChangedCallback(evt => OnSnapToGridChanged(evt.newValue));
            _gridSizeSlider?.RegisterValueChangedCallback(evt => OnGridSizeChanged(evt.newValue));
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

        #region Panel Toggling

        private void ToggleObjectPalette()
        {
            var isVisible = _objectPalette.style.display == DisplayStyle.Flex;
            _objectPalette.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void TogglePropertiesPanel()
        {
            if (_selectedObject == null)
            {
                 ShowMessageDialog("Inspector", "Select an object to view its properties.");
                 _propertiesPanel.style.display = DisplayStyle.None;
                 return;
            }
            var isVisible = _propertiesPanel.style.display == DisplayStyle.Flex;
            _propertiesPanel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        #endregion

        #region Dialogs

        private void ShowProjectDialog()
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("new", "New Project", "button"),
                new FormFieldDefinition("save", "Save Project", "button") { options = new Dictionary<string, object> { ["enabled"] = _sandboxBuilder?.CurrentProject != null } },
                new FormFieldDefinition("load", "Load Project", "button"),
                new FormFieldDefinition("clear", "Close Project", "button") { options = new Dictionary<string, object> { ["enabled"] = _sandboxBuilder?.CurrentProject != null } }
            };

            ShowDialog("Project Management", fields, (formData) =>
            {
                if (!formData.ContainsKey("clickedButton")) return;
                switch (formData["clickedButton"].ToString())
                {
                    case "new": ShowNewProjectForm(); break;
                    case "save": ShowSaveProjectForm(); break;
                    case "load": ShowLoadProjectForm(); break;
                    case "clear": OnClearProjectClicked(); break;
                }
            });
        }

        private void ShowSceneDialog()
        {
             var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("new", "New Scene", "button"),
                new FormFieldDefinition("save", "Save Scene", "button") { options = new Dictionary<string, object> { ["enabled"] = _sandboxBuilder?.CurrentScene != null } },
                new FormFieldDefinition("load", "Load Scene", "button"),
                new FormFieldDefinition("clear", "Clear Scene", "button") { options = new Dictionary<string, object> { ["enabled"] = _sandboxBuilder?.CurrentScene != null && _sandboxBuilder.CurrentScene.placedObjects.Any() } }
            };

            ShowDialog("Scene Management", fields, (formData) =>
            {
                if (!formData.ContainsKey("clickedButton")) return;
                switch (formData["clickedButton"].ToString())
                {
                    case "new": ShowNewSceneForm(); break;
                    case "save": OnSaveSceneClicked(); break;
                    case "load": ShowLoadSceneForm(); break;
                    case "clear": OnClearSceneClicked(); break;
                }
            });
        }

        private void ShowNewProjectForm()
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("projectName", "Project Name", "text") { required = true, defaultValue = $"Project_{DateTime.Now:yyyyMMdd_HHmmss}" }
            };
            ShowDialog("Create New Project", fields, (formData) =>
            {
                string name = formData["projectName"].ToString().Trim();
                if (string.IsNullOrEmpty(name))
                {
                    ShowMessageDialog("Error", "Project name cannot be empty.");
                    return;
                }
                _sandboxBuilder?.CreateNewProject(name);
                ShowMessageDialog("Success", $"Project '{name}' created.");
            });
        }

        private void ShowNewSceneForm()
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("sceneName", "Scene Name", "text") { required = true, defaultValue = $"Scene_{DateTime.Now:yyyyMMdd_HHmmss}" }
            };
            ShowDialog("Create New Scene", fields, (formData) =>
            {
                string name = formData["sceneName"].ToString().Trim();
                if (string.IsNullOrEmpty(name))
                {
                    ShowMessageDialog("Error", "Scene name cannot be empty.");
                    return;
                }
                _sandboxBuilder?.CreateNewScene(name);
            });
        }

        private void ShowSaveProjectForm()
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("info", "Save Project", "info") { defaultValue = $"Saving: {_sandboxBuilder.CurrentProject.projectName}"},
                new FormFieldDefinition("newProjectName", "New Project Name (for Save As)", "text"),
                new FormFieldDefinition("confirm", "Save", "button")
            };
            ShowDialog("Save Project", fields, (formData) => {
                string newProjectName = formData.ContainsKey("newProjectName") ? formData["newProjectName"].ToString().Trim() : null;
                string savePath = null;
                if (!string.IsNullOrEmpty(newProjectName))
                {
                    savePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_sandboxBuilder.CurrentProject.filePath), $"{newProjectName}.sbproj");
                }

                if (_sandboxBuilder.SaveProject(savePath))
                    ShowMessageDialog("Success", "Project saved successfully!");
                else
                    ShowMessageDialog("Error", "Failed to save project.");
            });
        }

        private void ShowLoadProjectForm()
        {
            var projects = _sandboxBuilder.GetAvailableProjects();
            if (projects == null || projects.Count == 0)
            {
                ShowMessageDialog("Load Project", "No saved projects found.");
                return;
            }

            var projectNames = projects.Select(p => p.displayName).ToList();
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("project", "Select Project", "select") { options = new Dictionary<string, object> {{"items", projectNames}} }
            };

            ShowDialog("Load Project", fields, (formData) => {
                var selectedName = formData["project"].ToString();
                var projectInfo = projects.FirstOrDefault(p => p.displayName == selectedName);
                if (projectInfo != null)
                {
                    _sandboxBuilder.LoadProject(projectInfo.filePath);
                }
            });
        }

        private void ShowDialog(string title, List<FormFieldDefinition> fields, Action<Dictionary<string, object>> onSubmit)
        {
            FormSubmitPanelUIToolkit.Instance.Show(title, fields, onSubmit, null, _dialogContainer);
        }

        private void ShowMessageDialog(string title, string message)
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("message", "", "info") { defaultValue = message }
            };
            ShowDialog(title, fields, data => {});
        }

        private void ShowConfirmationDialogAsync(string title, string message, Action onConfirm, Action onCancel = null)
        {
            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("message", "", "info") { defaultValue = message },
                new FormFieldDefinition("confirm", "Yes", "button"),
                new FormFieldDefinition("cancel", "No", "button")
            };

            ShowDialog(title, fields, (formData) => {
                if (formData.ContainsKey("clickedButton") && formData["clickedButton"].ToString() == "confirm")
                    onConfirm?.Invoke();
                else
                    onCancel?.Invoke();
            });
        }

        #endregion

        #region Event Handlers

        private void OnPreviewClicked() => _sandboxBuilder?.StartPreview();
        private void OnStopPreviewClicked() => _sandboxBuilder?.StopPreview();
        private void OnSaveSceneClicked() => _sandboxBuilder?.SaveScene();

        private void ShowLoadSceneForm()
        {
            var scenes = _sandboxBuilder.GetAvailableScenes();
            if (scenes == null || scenes.Count == 0)
            {
                ShowMessageDialog("Load Scene", "No saved scenes found for the current project.");
                return;
            }

            var fields = new List<FormFieldDefinition>
            {
                new FormFieldDefinition("scene", "Select Scene", "select") { options = new Dictionary<string, object> {{"items", scenes}} }
            };

            ShowDialog("Load Scene", fields, (formData) => {
                var selectedScene = formData["scene"].ToString();
                if (!string.IsNullOrEmpty(selectedScene))
                {
                    // This assumes the scene name is the file name without extension
                    var scenePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_sandboxBuilder.CurrentProject.filePath), "Scenes", $"{selectedScene}.json");
                    _sandboxBuilder.LoadScene(scenePath);
                }
            });
        }

        private void OnLoadSceneClicked() => ShowLoadSceneForm();
        private void OnClearSceneClicked() => ShowConfirmationDialogAsync("Clear Scene", "Are you sure you want to clear the scene?", () => _sandboxBuilder?.ClearScene());
        private void OnClearProjectClicked() => ShowConfirmationDialogAsync("Close Project", "Are you sure? Unsaved changes will be lost.", () => _sandboxBuilder?.CreateNewProject("New Project"));
        private void OnDeleteObjectClicked() => _sandboxBuilder?.RemoveObject(_selectedObject);
        private void OnSnapToGridChanged(bool enabled) => Debug.Log($"Snap to grid: {enabled}");
        private void OnGridSizeChanged(float size) => _gridSizeText.text = size.ToString("F1");
        private void OnSceneLoaded(SceneConfiguration scene) => UpdateUI();
        private void OnSceneSaved(SceneConfiguration scene) => ShowMessageDialog("Success", $"Scene '{scene.sceneName}' saved successfully");
        private void OnObjectPlaced(GameObject obj) => UpdateUI();
        private void OnObjectRemoved(GameObject obj)
        {
            if (_selectedObject == obj)
            {
                _selectedObject = null;
                _propertiesPanel.style.display = DisplayStyle.None;
            }
            UpdateUI();
        }
        private void OnObjectSelected(GameObject obj)
        {
            _selectedObject = obj;
            _propertiesPanel.style.display = DisplayStyle.Flex;
            UpdatePropertiesPanel();
            UpdateUI();
        }
        private void OnSceneCleared()
        {
            _selectedObject = null;
            UpdateUI();
        }
        private void OnPreviewStateChanged(bool isInPreview) => UpdateUI();

        #endregion

        #region UI Updates

        private void PopulateObjectPalette()
        {
            if (_sandboxBuilder?.ObjectLibrary == null || _objectPaletteScrollView == null) return;

            _objectPaletteScrollView.Clear();
            var items = GetFilteredObjects();

            foreach (var itemData in items)
            {
                var button = new Button(() => _sandboxBuilder.PlaceObject(itemData.id, Vector3.zero))
                {
                    text = itemData.displayName
                };
                _objectPaletteScrollView.Add(button);
            }
        }

        private List<SceneObjectData> GetFilteredObjects()
        {
            var allObjects = _sandboxBuilder.ObjectLibrary.GetAllObjects();
            if (allObjects == null) return new List<SceneObjectData>();

            var filteredObjects = allObjects.AsEnumerable();

            if (_typeFilter.value != "All Types")
                filteredObjects = filteredObjects.Where(obj => obj.objectType.ToString() == _typeFilter.value);

            if (_categoryFilter.value != "All")
                filteredObjects = filteredObjects.Where(obj => obj.category == _categoryFilter.value);

            if (!string.IsNullOrEmpty(_searchField.value))
            {
                string searchLower = _searchField.value.ToLower();
                filteredObjects = filteredObjects.Where(obj =>
                    obj.displayName.ToLower().Contains(searchLower) ||
                    (obj.tags != null && obj.tags.Any(tag => tag.ToLower().Contains(searchLower))));
            }

            return filteredObjects.ToList();
        }

        private void PopulateFilterDropdowns()
        {
            if (_sandboxBuilder?.ObjectLibrary == null) return;

            var typeOptions = new List<string> { "All Types" };
            typeOptions.AddRange(Enum.GetNames(typeof(SceneObjectType)));
            _typeFilter.choices = typeOptions;
            _typeFilter.value = "All Types";

            var categoryOptions = new List<string> { "All" };
            categoryOptions.AddRange(_sandboxBuilder.ObjectLibrary.GetCategories());
            _categoryFilter.choices = categoryOptions;
            _categoryFilter.value = "All";
        }

        private void UpdatePropertiesPanel()
        {
            var transformControls = _root.Q<VisualElement>("transform-controls");
            transformControls.Clear();

            if (_selectedObject == null) return;

            // Name
            var nameField = new TextField("Object Name");
            nameField.value = _selectedObject.name;
            nameField.RegisterValueChangedCallback(evt => {
                _selectedObject.name = evt.newValue;
            });
            transformControls.Add(nameField);

            // Position
            var pos = _selectedObject.transform.position;
            var posX = new FloatField("X") { value = pos.x };
            var posY = new FloatField("Y") { value = pos.y };
            var posZ = new FloatField("Z") { value = pos.z };
            posX.RegisterValueChangedCallback(evt => { var p = _selectedObject.transform.position; p.x = evt.newValue; _selectedObject.transform.position = p; });
            posY.RegisterValueChangedCallback(evt => { var p = _selectedObject.transform.position; p.y = evt.newValue; _selectedObject.transform.position = p; });
            posZ.RegisterValueChangedCallback(evt => { var p = _selectedObject.transform.position; p.z = evt.newValue; _selectedObject.transform.position = p; });
            var posGroup = new VisualElement() { name = "pos-group" };
            posGroup.Add(new Label("Position"));
            posGroup.Add(posX);
            posGroup.Add(posY);
            posGroup.Add(posZ);
            transformControls.Add(posGroup);

            // Rotation
            var rot = _selectedObject.transform.eulerAngles;
            var rotX = new FloatField("X") { value = rot.x };
            var rotY = new FloatField("Y") { value = rot.y };
            var rotZ = new FloatField("Z") { value = rot.z };
            rotX.RegisterValueChangedCallback(evt => { var r = _selectedObject.transform.eulerAngles; r.x = evt.newValue; _selectedObject.transform.eulerAngles = r; });
            rotY.RegisterValueChangedCallback(evt => { var r = _selectedObject.transform.eulerAngles; r.y = evt.newValue; _selectedObject.transform.eulerAngles = r; });
            rotZ.RegisterValueChangedCallback(evt => { var r = _selectedObject.transform.eulerAngles; r.z = evt.newValue; _selectedObject.transform.eulerAngles = r; });
            var rotGroup = new VisualElement() { name = "rot-group" };
            rotGroup.Add(new Label("Rotation"));
            rotGroup.Add(rotX);
            rotGroup.Add(rotY);
            rotGroup.Add(rotZ);
            transformControls.Add(rotGroup);

            // Scale
            var scl = _selectedObject.transform.localScale;
            var sclX = new FloatField("X") { value = scl.x };
            var sclY = new FloatField("Y") { value = scl.y };
            var sclZ = new FloatField("Z") { value = scl.z };
            sclX.RegisterValueChangedCallback(evt => { var s = _selectedObject.transform.localScale; s.x = evt.newValue; _selectedObject.transform.localScale = s; });
            sclY.RegisterValueChangedCallback(evt => { var s = _selectedObject.transform.localScale; s.y = evt.newValue; _selectedObject.transform.localScale = s; });
            sclZ.RegisterValueChangedCallback(evt => { var s = _selectedObject.transform.localScale; s.z = evt.newValue; _selectedObject.transform.localScale = s; });
            var sclGroup = new VisualElement() { name = "scl-group" };
            sclGroup.Add(new Label("Scale"));
            sclGroup.Add(sclX);
            sclGroup.Add(sclY);
            sclGroup.Add(sclZ);
            transformControls.Add(sclGroup);
        }

        private void UpdateUI()
        {
            var scene = _sandboxBuilder?.CurrentScene;
            var project = _sandboxBuilder?.CurrentProject;
            bool isInPreview = _sandboxBuilder?.IsInPreviewMode ?? false;

            // Info Bar
            _currentProjectText.text = $"Project: {project?.projectName ?? "None"}";
            _sceneNameText.text = $"Scene: {scene?.sceneName ?? "None"}";
            _objectCountText.text = $"Objects: {scene?.placedObjects?.Count ?? 0}";
            _previewStatusText.text = isInPreview ? "PREVIEW" : "BUILD";
            _previewStatusText.style.color = isInPreview ? Color.cyan : Color.white;

            // Button States
            _previewButton.style.display = isInPreview ? DisplayStyle.None : DisplayStyle.Flex;
            _stopPreviewButton.style.display = isInPreview ? DisplayStyle.Flex : DisplayStyle.None;

            _deleteObjectButton?.SetEnabled(_selectedObject != null && !isInPreview);

            var bottomBar = _root.Q<VisualElement>("bottom-bar");
            bottomBar?.SetEnabled(!isInPreview);

            if(isInPreview)
            {
                _objectPalette.style.display = DisplayStyle.None;
                _propertiesPanel.style.display = DisplayStyle.None;
            }
        }
        #endregion
    }
}