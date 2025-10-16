using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.FormSubmit.Fields;

namespace Core.UI.FormSubmit
{
    /// <summary>
    /// Dynamic form panel using UI Toolkit that can build UI forms from JSON field definitions
    /// Singleton pattern ensures only one form panel exists at a time
    /// Supports various field types: text, textarea, selectbox, number, toggle, etc.
    /// </summary>
    public class FormSubmitPanelUIToolkit : MonoBehaviour
    {
        #region Singleton

        private static FormSubmitPanelUIToolkit instance;
        public static FormSubmitPanelUIToolkit Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<FormSubmitPanelUIToolkit>();
                    if (instance == null)
                    {
                        CreateSingleton();
                    }
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        private static void CreateSingleton()
        {
            // Create UIDocument if none exists
            UIDocument uiDocument = FindFirstObjectByType<UIDocument>();
            if (uiDocument == null)
            {
                GameObject uiDocumentObj = new GameObject("FormSubmitUIDocument");
                uiDocument = uiDocumentObj.AddComponent<UIDocument>();

                // Create a basic panel setup
                var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.targetTexture = null; // Screen space
                panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.sortingOrder = 1000;

                uiDocument.panelSettings = panelSettings;
            }

            // Create the form panel
            GameObject formPanelObj = new GameObject("FormSubmitPanelUIToolkit");
            instance = formPanelObj.AddComponent<FormSubmitPanelUIToolkit>();
            instance.uiDocument = uiDocument;
            instance.Initialize();
        }

        #endregion

        #region UI Components

        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Visual Assets")]
        [SerializeField] private VisualTreeAsset formPanelTemplate;
        [SerializeField] private StyleSheet formPanelStyleSheet;

        // Visual Elements
        private VisualElement rootElement;
        private VisualElement backgroundPanel;
        private VisualElement formContainer;
        private ScrollView scrollView;
        private VisualElement fieldsContainer;

        // Header elements
        private Label titleText;
        private Button closeButton;

        // Footer elements
        private Button submitButton;
        private Button cancelButton;

        // Dynamic field instances
        private List<IFormFieldUIToolkit> dynamicFields = new List<IFormFieldUIToolkit>();

        // Initialization flag
        private bool isInitialized = false;

        #endregion

        #region Events

        public event Action<Dictionary<string, object>> OnFormSubmitted;
        public event Action OnFormCancelled;

        #endregion

        #region Properties

        public bool IsVisible { get; private set; }

        /// <summary>
        /// Check if fields container is properly initialized
        /// </summary>
        public bool IsFieldsContainerReady => fieldsContainer != null;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                Initialize();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (isInitialized && IsFieldsContainerReady)
            {
                return;
            }

            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null)
                {
                    uiDocument = gameObject.AddComponent<UIDocument>();
                }
            }

            CreateFormUI();
            SetupEventListeners();
            CloseForm();
            isInitialized = true;
        }

        private void CreateFormUI()
        {
            // Get or create root element
            rootElement = uiDocument.rootVisualElement;
            uiDocument.position = Position.Absolute;
            uiDocument.worldSpaceSizeMode = UIDocument.WorldSpaceSizeMode.Dynamic;
            if (rootElement == null)
            {
                rootElement = new VisualElement();
                rootElement.name = "FormSubmitRoot";
            }

            // Apply stylesheet if available
            if (formPanelStyleSheet != null)
            {
                rootElement.styleSheets.Add(formPanelStyleSheet);
            }

            // Create or load from template
            if (formPanelTemplate != null)
            {
                formPanelTemplate.CloneTree(rootElement);
                SetupUIReferences();
            }

            // Ensure we have all required elements
            ValidateUIElements();
        }

        private void SetupUIReferences()
        {
            // Get references from template
            backgroundPanel = rootElement.Q("background-panel");
            formContainer = rootElement.Q("form-container");
            scrollView = rootElement.Q<ScrollView>("scroll-view");
            fieldsContainer = rootElement.Q("fields-container");
            titleText = rootElement.Q<Label>("title-text");
            closeButton = rootElement.Q<Button>("close-button");
            submitButton = rootElement.Q<Button>("submit-button");
            cancelButton = rootElement.Q<Button>("cancel-button");
        }

        private void ValidateUIElements()
        {
            if (backgroundPanel == null ||
                formContainer == null ||
                scrollView == null ||
                fieldsContainer == null ||
                titleText == null ||
                closeButton == null ||
                submitButton == null ||
                cancelButton == null)
            {
                Debug.LogError("FormSubmitPanelUIToolkit: Some UI elements are missing after initialization");
            }
        }

        private void SetupEventListeners()
        {
            if (closeButton != null)
                closeButton.clicked += CloseForm;

            if (cancelButton != null)
                cancelButton.clicked += HandleCancel;

            if (submitButton != null)
                submitButton.clicked += HandleSubmit;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the form panel with specified form definition
        /// </summary>
        public void Show(string title, List<FormFieldDefinition> fieldDefinitions, Action<Dictionary<string, object>> onSubmit = null, Action onCancel = null, Transform parent = null)
        {
            // Handle parent transform (for compatibility with uGUI version)
            if (parent != null)
            {
                this.transform.SetParent(parent, false);
                // UI Toolkit does not use Transform hierarchy for UI, but we can check for UIDocument
                var parentUIDocument = parent.GetComponent<UIDocument>();
                if (parentUIDocument == null)
                {
                    parentUIDocument = parent.GetComponentInChildren<UIDocument>();
                }
                if (parentUIDocument != null)
                {
                    uiDocument = parentUIDocument;
                }
                else
                {
                    Debug.LogWarning("FormSubmitPanelUIToolkit: Parent transform specified but no UIDocument found. UI Toolkit does not use Transform hierarchy. Consider using UIDocument setup instead.");
                }
            }

            // Ensure initialization
            if (!isInitialized || !IsFieldsContainerReady)
            {
                Initialize();
            }

            if (!IsFieldsContainerReady)
            {
                Debug.LogError("FormSubmitPanelUIToolkit: Fields container is not ready");
                return;
            }

            if (titleText != null)
            {
                titleText.text = title;
            }

            // Clear existing fields
            ClearFields();

            // Create new fields
            CreateFields(fieldDefinitions);

            // Set callbacks
            OnFormSubmitted = onSubmit;
            OnFormCancelled = onCancel;

            // Show panel
            if (backgroundPanel != null)
            {
                backgroundPanel.style.display = DisplayStyle.Flex;
                IsVisible = true;
            }

            // Focus first field if available
            FocusFirstField();
        }

        /// <summary>
        /// Close the form panel
        /// </summary>
        public void CloseForm()
        {
            if (backgroundPanel != null)
                backgroundPanel.style.display = DisplayStyle.None;

            IsVisible = false;
            ClearFields();
        }

        /// <summary>
        /// Force re-initialize the form UI if needed
        /// </summary>
        public void ForceReinitialize()
        {
            isInitialized = false;
            Initialize();
        }

        /// <summary>
        /// Handle form submission with custom data (used by button fields)
        /// </summary>
        public void HandleCustomSubmission(Dictionary<string, object> customData)
        {
            OnFormSubmitted?.Invoke(customData);
            CloseForm();
        }

        /// <summary>
        /// Trigger form submission without closing the form (used by action buttons)
        /// </summary>
        public void TriggerFormSubmission(Dictionary<string, object> customData)
        {
            OnFormSubmitted?.Invoke(customData);
        }

        #endregion

        #region Form Management

        private void ClearFields()
        {
            if (fieldsContainer != null)
            {
                fieldsContainer.Clear();
            }
            dynamicFields.Clear();
        }

        private void CreateFields(List<FormFieldDefinition> fieldDefinitions)
        {
            foreach (var fieldDef in fieldDefinitions)
            {
                CreateField(fieldDef);
            }
        }

        private void CreateField(FormFieldDefinition fieldDef)
        {
            if (fieldsContainer == null)
            {
                return;
            }

            VisualElement fieldElement = null;
            IFormFieldUIToolkit formField = null;

            switch (fieldDef.type.ToLower())
            {
                case "text":
                    fieldElement = CreateTextField(fieldDef);
                    break;
                case "textarea":
                    fieldElement = CreateTextAreaField(fieldDef);
                    break;
                case "number":
                    fieldElement = CreateNumberField(fieldDef);
                    break;
                case "select":
                case "selectbox":
                    fieldElement = CreateSelectField(fieldDef);
                    break;
                case "toggle":
                case "checkbox":
                    fieldElement = CreateToggleField(fieldDef);
                    break;
                case "slider":
                    fieldElement = CreateSliderField(fieldDef);
                    break;
                case "color":
                    fieldElement = CreateColorField(fieldDef);
                    break;
                case "button":
                    fieldElement = CreateButtonField(fieldDef);
                    break;
                case "info":
                    fieldElement = CreateInfoField(fieldDef);
                    break;
                case "hidden":
                    fieldElement = CreateHiddenField(fieldDef);
                    break;
                default:
                    Debug.LogWarning($"FormSubmitPanelUIToolkit.CreateField: Unsupported field type: {fieldDef.type}, falling back to text");
                    fieldElement = CreateTextField(fieldDef);
                    break;
            }

            if (fieldElement != null)
            {
                fieldsContainer.Add(fieldElement);
                formField = fieldElement.userData as IFormFieldUIToolkit;

                if (formField != null)
                {
                    dynamicFields.Add(formField);
                }
            }
        }

        private VisualElement CreateTextField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var textField = new TextField();
            textField.value = fieldDef.defaultValue?.ToString() ?? "";
            textField.AddToClassList("field-input");

            if (!string.IsNullOrEmpty(fieldDef.placeholder))
            {
                // Note: UI Toolkit doesn't have built-in placeholder support
                // We can implement it using a visual hint
            }

            var formField = new TextFormFieldUIToolkit();
            formField.Initialize(fieldDef, textField);
            container.userData = formField;

            container.Add(label);
            container.Add(textField);

            return container;
        }

        private VisualElement CreateTextAreaField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var textField = new TextField();
            textField.multiline = true;
            textField.value = fieldDef.defaultValue?.ToString() ?? "";
            textField.AddToClassList("field-textarea");
            textField.style.height = 100;

            var formField = new TextAreaFormFieldUIToolkit();
            formField.Initialize(fieldDef, textField);
            container.userData = formField;

            container.Add(label);
            container.Add(textField);

            return container;
        }

        private VisualElement CreateNumberField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var floatField = new FloatField();
            if (fieldDef.defaultValue != null)
            {
                if (float.TryParse(fieldDef.defaultValue.ToString(), out float defaultVal))
                {
                    floatField.value = defaultVal;
                }
            }
            floatField.AddToClassList("field-input");

            var formField = new NumberFormFieldUIToolkit();
            formField.Initialize(fieldDef, floatField);
            container.userData = formField;

            container.Add(label);
            container.Add(floatField);

            return container;
        }

        private VisualElement CreateSelectField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var dropdownField = new DropdownField();

            // Get options from field definition
            if (fieldDef.options != null && fieldDef.options.ContainsKey("items"))
            {
                if (fieldDef.options["items"] is List<string> items)
                {
                    dropdownField.choices = items;
                    if (items.Count > 0)
                    {
                        dropdownField.value = fieldDef.defaultValue?.ToString() ?? items[0];
                    }
                }
            }

            dropdownField.AddToClassList("field-input");

            var formField = new SelectFormFieldUIToolkit();
            formField.Initialize(fieldDef, dropdownField);
            container.userData = formField;

            container.Add(label);
            container.Add(dropdownField);

            return container;
        }

        private VisualElement CreateToggleField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;

            var toggle = new Toggle();
            if (fieldDef.defaultValue is bool boolVal)
            {
                toggle.value = boolVal;
            }
            toggle.AddToClassList("field-toggle");
            toggle.style.marginRight = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");

            var formField = new ToggleFormFieldUIToolkit();
            formField.Initialize(fieldDef, toggle);
            container.userData = formField;

            container.Add(toggle);
            container.Add(label);

            return container;
        }

        private VisualElement CreateSliderField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var slider = new Slider();

            // Set min/max from options
            if (fieldDef.options != null)
            {
                if (fieldDef.options.ContainsKey("min") && float.TryParse(fieldDef.options["min"].ToString(), out float min))
                {
                    slider.lowValue = min;
                }
                if (fieldDef.options.ContainsKey("max") && float.TryParse(fieldDef.options["max"].ToString(), out float max))
                {
                    slider.highValue = max;
                }
            }

            if (fieldDef.defaultValue != null && float.TryParse(fieldDef.defaultValue.ToString(), out float defaultVal))
            {
                slider.value = defaultVal;
            }

            slider.AddToClassList("field-slider");

            var formField = new SliderFormFieldUIToolkit();
            formField.Initialize(fieldDef, slider);
            container.userData = formField;

            container.Add(label);
            container.Add(slider);

            return container;
        }

        private VisualElement CreateColorField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            // UI Toolkit doesn't have a built-in color picker, so we'll use a button to simulate one
            var colorButton = new Button();
            colorButton.text = "Select Color";
            colorButton.AddToClassList("field-color-button");
            colorButton.style.height = 30;

            var formField = new ColorFormFieldUIToolkit();
            formField.Initialize(fieldDef, colorButton);
            container.userData = formField;

            container.Add(label);
            container.Add(colorButton);

            return container;
        }

        private VisualElement CreateButtonField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var button = new Button();
            button.text = fieldDef.label;
            button.AddToClassList("field-button");

            var formField = new ButtonFormFieldUIToolkit();
            formField.Initialize(fieldDef, button);
            container.userData = formField;

            // Set up button click to trigger form submission if it's an action button
            button.clicked += () =>
            {
                var actionData = new Dictionary<string, object>
                {
                    [fieldDef.name] = formField.GetValue()
                };
                TriggerFormSubmission(actionData);
            };

            container.Add(button);

            return container;
        }

        private VisualElement CreateInfoField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.AddToClassList("field-container");
            container.style.marginBottom = 10;

            var label = new Label(fieldDef.label);
            label.AddToClassList("field-label");
            label.style.marginBottom = 5;

            var textField = new TextField();
            textField.value = fieldDef.defaultValue?.ToString() ?? "";
            textField.SetEnabled(false); // Make it readonly
            textField.AddToClassList("field-info");

            var formField = new InfoFormFieldUIToolkit();
            formField.Initialize(fieldDef, textField);
            container.userData = formField;

            container.Add(label);
            container.Add(textField);

            return container;
        }

        private VisualElement CreateHiddenField(FormFieldDefinition fieldDef)
        {
            var container = new VisualElement();
            container.style.display = DisplayStyle.None; // Hidden field

            var formField = new HiddenFormFieldUIToolkit();
            formField.Initialize(fieldDef, container);
            container.userData = formField;

            return container;
        }

        private void FocusFirstField()
        {
            if (dynamicFields.Count > 0)
            {
                dynamicFields[0].Focus();
            }
        }

        #endregion

        #region Event Handlers

        private void HandleSubmit()
        {
            var formData = new Dictionary<string, object>();

            foreach (var field in dynamicFields)
            {
                if (field.IsValid())
                {
                    formData[field.GetName()] = field.GetValue();
                }
                else
                {
                    return; // Don't submit if any field is invalid
                }
            }

            OnFormSubmitted?.Invoke(formData);
            CloseForm();
        }

        private void HandleCancel()
        {
            OnFormCancelled?.Invoke();
            CloseForm();
        }

        #endregion
    }
}