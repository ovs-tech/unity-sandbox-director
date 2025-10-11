using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using MiniTimeline.UI;
using Core.UI.Core.Helpers;

namespace Core.UI.FormSubmit
{
    /// <summary>
    /// Dynamic form panel that can build UI forms from JSON field definitions
    /// Singleton pattern ensures only one form panel exists at a time
    /// Supports various field types: text, textarea, selectbox, number, toggle, etc.
    /// </summary>
    public class FormSubmitPanel : MonoBehaviour
    {
        #region Singleton
        
        private static FormSubmitPanel instance;
        public static FormSubmitPanel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<FormSubmitPanel>();
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
            // Create Canvas if none exists
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("FormSubmitCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // Ensure it's on top
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create the form panel
            GameObject formPanelObj = new GameObject("FormSubmitPanel");
            formPanelObj.transform.SetParent(canvas.transform, false);
            
            instance = formPanelObj.AddComponent<FormSubmitPanel>();
            instance.Initialize();
        }
        
        #endregion
        
        #region UI Components
        
        [Header("Panel Settings")]
        private GameObject backgroundPanel;
        private GameObject formContainer;
        private ScrollRect scrollRect;
        
        [Header("Header")]
        private TextMeshProUGUI titleText;
        private Button closeButton;
        
        [Header("Footer")]
        private Button submitButton;
        private Button cancelButton;
        
        [Header("Field Prefabs")]
        [SerializeField] private GameObject textFieldPrefab;
        [SerializeField] private GameObject textAreaFieldPrefab;
        [SerializeField] private GameObject numberFieldPrefab;
        [SerializeField] private GameObject selectFieldPrefab;
        [SerializeField] private GameObject toggleFieldPrefab;
        [SerializeField] private GameObject sliderFieldPrefab;
        [SerializeField] private GameObject colorFieldPrefab;
        
        // Dynamic field instances
        private List<IFormField> dynamicFields = new List<IFormField>();
        
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
        /// Check if fieldsParent is properly initialized
        /// </summary>
        public bool IsFieldsParentReady => GetFieldsParent() != null;
        
        /// <summary>
        /// Dynamically find the fields parent transform
        /// </summary>
        private Transform GetFieldsParent()
        {
            if (scrollRect != null && scrollRect.content != null)
            {
                return scrollRect.content;
            }
            return null;
        }
        
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
            // Check if we need to initialize (more comprehensive check)
            bool needsInitialization = !isInitialized || 
                                      GetFieldsParent() == null || 
                                      titleText == null || 
                                      backgroundPanel == null ||
                                      formContainer == null;
                                      
            if (!needsInitialization)
            {
                return;
            }
            
            
            
            // Reset flag if we're re-initializing
            isInitialized = false;
            
            CreateFormUI();
            SetupEventListeners();
            CloseForm();
            isInitialized = true;
            
        }
        
        private void CreateFormUI()
        {
            // Create main background panel
            backgroundPanel = new GameObject("Background");
            backgroundPanel.transform.SetParent(transform, false);
            
            var backgroundRect = backgroundPanel.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;
            backgroundRect.anchoredPosition = Vector2.zero;
            
            var backgroundImage = backgroundPanel.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.7f); // Semi-transparent black
            
            // Create form container
            formContainer = new GameObject("FormContainer");
            formContainer.transform.SetParent(backgroundPanel.transform, false);
            
            var containerRect = formContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(240f, 320f);
            containerRect.anchoredPosition = Vector2.zero;
            
            var containerImage = formContainer.AddComponent<Image>();
            containerImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f); // Dark panel
            
            // Create header
            CreateHeader();
            
            // Create scrollable content area
            CreateScrollableContent();
            
            // Create footer
            CreateFooter();
            
            // Load and create field prefabs
            CreateFieldPrefabs();
            
            // Verify all critical components were created
            
            if (GetFieldsParent() == null)
            {
                // fieldsParent is unexpectedly null after CreateFormUI
            }
        }
        
        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(formContainer.transform, false);
            
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 50f);
            headerRect.anchoredPosition = new Vector2(0f, -25f);
            
            // Title text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0.8f, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;
            
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Create Clip";
            titleText.fontSize = 18f;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.margin = new Vector4(15, 0, 0, 0);
            
            // Close button
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(header.transform, false);
            
            var closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.8f, 0f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.sizeDelta = Vector2.zero;
            closeRect.anchoredPosition = Vector2.zero;
            
            closeButton = closeObj.AddComponent<Button>();
            var closeImage = closeObj.AddComponent<Image>();
            closeImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            
            // Close button text
            GameObject closeTextObj = new GameObject("Text");
            closeTextObj.transform.SetParent(closeObj.transform, false);
            var closeTextRect = closeTextObj.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;
            closeTextRect.anchoredPosition = Vector2.zero;
            
            var closeTextMesh = closeTextObj.AddComponent<TextMeshProUGUI>();
            closeTextMesh.text = "×";
            closeTextMesh.fontSize = 24f;
            closeTextMesh.color = Color.white;
            closeTextMesh.alignment = TextAlignmentOptions.Center;
        }
        
        private void CreateScrollableContent()
        {
            // Create scroll rect container
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(formContainer.transform, false);
            
            var scrollRectTransform = scrollObj.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.sizeDelta = Vector2.zero;
            scrollRectTransform.anchoredPosition = Vector2.zero;
            scrollRectTransform.offsetMin = new Vector2(10f, 60f); // Bottom margin for footer
            scrollRectTransform.offsetMax = new Vector2(-10f, -60f); // Top margin for header
            
            this.scrollRect = scrollObj.AddComponent<ScrollRect>();
            
            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            var viewportMask = viewport.AddComponent<Mask>();
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f); // Nearly transparent for masking
            
            // Create content container
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            
            
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            contentRect.anchoredPosition = Vector2.zero;
            
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlHeight = true;
            contentLayout.spacing = 10f;
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            
            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Setup scroll rect
            this.scrollRect.content = contentRect;
            this.scrollRect.viewport = viewportRect;
            this.scrollRect.horizontal = false;
            this.scrollRect.vertical = true;
            
            
        }
        
        private void CreateFooter()
        {
            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(formContainer.transform, false);
            
            var footerRect = footer.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.sizeDelta = new Vector2(0f, 50f);
            footerRect.anchoredPosition = new Vector2(0f, 25f);
            
            var footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = true;
            footerLayout.spacing = 10f;
            footerLayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Cancel button
            GameObject cancelObj = new GameObject("CancelButton");
            cancelObj.transform.SetParent(footer.transform, false);
            
            cancelButton = cancelObj.AddComponent<Button>();
            var cancelImage = cancelObj.AddComponent<Image>();
            cancelImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            
            var cancelTextObj = new GameObject("Text");
            cancelTextObj.transform.SetParent(cancelObj.transform, false);
            var cancelTextRect = cancelTextObj.AddComponent<RectTransform>();
            cancelTextRect.anchorMin = Vector2.zero;
            cancelTextRect.anchorMax = Vector2.one;
            cancelTextRect.sizeDelta = Vector2.zero;
            
            var cancelTextMesh = UICreationHelper.CreateStandardTextMesh(cancelTextObj, "Cancel", 14f, Color.white);
            cancelTextMesh.alignment = TextAlignmentOptions.Center;
            
            // Submit button
            GameObject submitObj = new GameObject("SubmitButton");
            submitObj.transform.SetParent(footer.transform, false);
            
            submitButton = submitObj.AddComponent<Button>();
            var submitImage = submitObj.AddComponent<Image>();
            submitImage.color = new Color(0.2f, 0.6f, 0.8f, 0.8f);
            
            var submitTextObj = new GameObject("Text");
            submitTextObj.transform.SetParent(submitObj.transform, false);
            var submitTextRect = submitTextObj.AddComponent<RectTransform>();
            submitTextRect.anchorMin = Vector2.zero;
            submitTextRect.anchorMax = Vector2.one;
            submitTextRect.sizeDelta = Vector2.zero;
            
            var submitTextMesh = UICreationHelper.CreateStandardTextMesh(submitTextObj, "OK", 14f, Color.white);
            submitTextMesh.alignment = TextAlignmentOptions.Center;
        }
        
        private void CreateFieldPrefabs()
        {
            // For now, we'll create these dynamically
            // In a production environment, these would be proper prefabs
            // created in the Unity Editor
        }
        
        private void SetupEventListeners()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseForm);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancel);
            
            if (submitButton != null)
                submitButton.onClick.AddListener(HandleSubmit);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Show the form panel with specified form definition
        /// </summary>
        public void Show(string title, List<FormFieldDefinition> fieldDefinitions, Action<Dictionary<string, object>> onSubmit = null, Action onCancel = null, Transform parent = null)
        {
            
            // If a parent is specified, ensure we're parented to it
            if (parent != null && transform.parent != parent)
            {
                transform.SetParent(parent, false);

                // Ensure proper anchoring when parent changes
                var rectTransform = GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }
            }
            
            // Check initialization state
            bool fieldsParentExists = GetFieldsParent() != null;
            bool titleTextExists = titleText != null;
            bool backgroundPanelExists = backgroundPanel != null;
            
            // Only initialize if not already initialized
            if (!fieldsParentExists || !titleTextExists || !backgroundPanelExists)
            {
                Initialize();

                // Re-check after initialization
                fieldsParentExists = GetFieldsParent() != null;
                titleTextExists = titleText != null;
                backgroundPanelExists = backgroundPanel != null;
            }
            
            // Verify fieldsParent is still valid after potential re-initialization
            if (GetFieldsParent() == null)
            {
                return;
            }
            
            if (titleText != null)
            {
                titleText.text = title;
            }
            
            // Clear existing fields
            ClearFields();
            
            // Verify fieldsParent is still valid after clearing
            if (GetFieldsParent() == null)
            {
                return;
            }
            
            // Create new fields
            CreateFields(fieldDefinitions);
            
            // Set callbacks
            OnFormSubmitted = onSubmit;
            OnFormCancelled = onCancel;
            
            // Show panel
            if (backgroundPanel != null)
            {
                backgroundPanel.SetActive(true);
                IsVisible = true;

                // Additional validation
                if (backgroundPanel.activeSelf)
                {
                    var fieldsParentAfterShow = GetFieldsParent();
                    if (fieldsParentAfterShow == null)
                    {
                        // fieldsParent became null after showing panel
                    }
                }
            }
            else
            {
                return;
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
                backgroundPanel.SetActive(false);

            IsVisible = false;

            // Only clear fields if we have fieldsParent
            if (GetFieldsParent() != null)
            {
                ClearFields();
            }
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
            if (customData != null)
            {
                // no-op: customData inspection removed
            }

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
            var fieldsParent = GetFieldsParent();
            
            if (fieldsParent != null)
            {
                int childCountBefore = fieldsParent.childCount;
                
                // Only destroy the children, not the fieldsParent itself
                // Use DestroyImmediate for UI cleanup to ensure objects are removed immediately
                // Note: We need to iterate backwards because DestroyImmediate modifies the collection
                for (int i = fieldsParent.childCount - 1; i >= 0; i--)
                {
                    Transform child = fieldsParent.GetChild(i);
                    if (child != null)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
                
                int childCountAfter = fieldsParent.childCount;
                if (childCountAfter != 0)
                {
                    // some children remain after clearing
                }
            }
            else
            {
                
            }
            
            dynamicFields.Clear();
            
        }
        
        private void CreateFields(List<FormFieldDefinition> fieldDefinitions)
        {
            
            int createdCount = 0;
            
            foreach (var fieldDef in fieldDefinitions)
            {
                
                CreateField(fieldDef);
                createdCount++;
            }
            
        }
        
        private void CreateField(FormFieldDefinition fieldDef)
        {
            var fieldsParent = GetFieldsParent();
            
            if (fieldsParent == null)
            {
                return;
            }
            
            GameObject fieldObj = null;
            IFormField formField = null;
            
            switch (fieldDef.type.ToLower())
            {
                case "text":
                    fieldObj = CreateTextField(fieldDef);
                    break;
                case "textarea":
                    fieldObj = CreateTextAreaField(fieldDef);
                    break;
                case "number":
                    fieldObj = CreateNumberField(fieldDef);
                    break;
                case "select":
                case "selectbox":
                    fieldObj = CreateSelectField(fieldDef);
                    break;
                case "toggle":
                case "checkbox":
                    fieldObj = CreateToggleField(fieldDef);
                    break;
                case "slider":
                    fieldObj = CreateSliderField(fieldDef);
                    break;
                case "color":
                    fieldObj = CreateColorField(fieldDef);
                    break;
                case "button":
                    fieldObj = CreateButtonField(fieldDef);
                    break;
                case "info":
                    fieldObj = CreateInfoField(fieldDef);
                    break;
                case "hidden":
                    fieldObj = CreateHiddenField(fieldDef);
                    break;
                default:
                    Debug.LogWarning($"FormSubmitPanel.CreateField: Unsupported field type: {fieldDef.type}, falling back to text");
                    fieldObj = CreateTextField(fieldDef); // Fallback to text
                    break;
            }
            
            if (fieldObj != null)
            {
                fieldObj.transform.SetParent(fieldsParent, false);
                formField = fieldObj.GetComponent<IFormField>();

                if (formField != null)
                {
                    dynamicFields.Add(formField);
                }
            }
        }
        
        private GameObject CreateTextField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"TextField_{fieldDef.name}");
            
            // Add layout element
            var layoutElement = fieldObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60f;
            
            // Create field UI
            var textField = fieldObj.AddComponent<TextFormField>();
            textField.Initialize(fieldDef);
            
            return fieldObj;
        }
        
        private GameObject CreateTextAreaField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"TextAreaField_{fieldDef.name}");
            
            var layoutElement = fieldObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 150f; // Increased for better scrollable experience
            
            var textAreaField = fieldObj.AddComponent<TextAreaFormField>();
            textAreaField.Initialize(fieldDef);
            
            return fieldObj;
        }
        
        private GameObject CreateNumberField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"NumberField_{fieldDef.name}");
            
            var layoutElement = fieldObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60f;
            
            var numberField = fieldObj.AddComponent<NumberFormField>();
            numberField.Initialize(fieldDef);
            
            return fieldObj;
        }
        
        private GameObject CreateSelectField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"SelectField_{fieldDef.name}");
            
            var layoutElement = fieldObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60f;
            
            var selectField = fieldObj.AddComponent<SelectFormField>();
            selectField.Initialize(fieldDef);
            
            return fieldObj;
        }
        
        private GameObject CreateToggleField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"ToggleField_{fieldDef.name}");
            
            var layoutElement = fieldObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 40f;
            
            var toggleField = fieldObj.AddComponent<ToggleFormField>();
            toggleField.Initialize(fieldDef);
            
            return fieldObj;
        }
        
        private GameObject CreateSliderField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"SliderField_{fieldDef.name}");
            
            var layoutElement = fieldObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 80f;
            
            var sliderField = fieldObj.AddComponent<SliderFormField>();
            sliderField.Initialize(fieldDef);
            
            return fieldObj;
        }
        
        private GameObject CreateColorField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"ColorField_{fieldDef.name}");
            
            var layoutElement = fieldObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60f;
            
            var colorField = fieldObj.AddComponent<ColorFormField>();
            colorField.Initialize(fieldDef);
            
            return fieldObj;
        }
        
        private GameObject CreateButtonField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"ButtonField_{fieldDef.name}");
            
            var layoutElement = fieldObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 50f;
            
            var buttonField = fieldObj.AddComponent<ButtonFormField>();
            buttonField.Initialize(fieldDef);
            
            return fieldObj;
        }
        
        private GameObject CreateInfoField(FormFieldDefinition fieldDef)
        {
            // Convert info field to readonly textarea
            var textAreaDef = new FormFieldDefinition
            {
                name = fieldDef.name,
                label = fieldDef.label,
                type = "textarea",
                defaultValue = fieldDef.defaultValue,
                required = fieldDef.required,
                placeholder = fieldDef.placeholder,
                tooltip = fieldDef.tooltip,
                options = new Dictionary<string, object>(fieldDef.options ?? new Dictionary<string, object>())
            };
            
            // Mark as readonly
            textAreaDef.options["readonly"] = true;
            
            return CreateTextAreaField(textAreaDef);
        }
        
        private GameObject CreateHiddenField(FormFieldDefinition fieldDef)
        {
            GameObject fieldObj = new GameObject($"HiddenField_{fieldDef.name}");
            
            // Hidden fields don't need layout elements since they're not visible
            var hiddenField = fieldObj.AddComponent<HiddenFormField>();
            hiddenField.Initialize(fieldDef);
            
            return fieldObj;
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
                    // invalid field - abort submit
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
    
    #region Data Classes
    
    /// <summary>
    /// Definition for a form field
    /// </summary>
    [Serializable]
    public class FormFieldDefinition
    {
        public string name;
        public string label;
        public string type;
        public object defaultValue;
        public bool required;
        public string placeholder;
        public string tooltip;
        public Dictionary<string, object> options; // For select fields, validation rules, etc.
        
        public FormFieldDefinition()
        {
            options = new Dictionary<string, object>();
        }
        
        public FormFieldDefinition(string name, string label, string type, object defaultValue = null)
        {
            this.name = name;
            this.label = label;
            this.type = type;
            this.defaultValue = defaultValue;
            this.options = new Dictionary<string, object>();
        }
    }
    
    #endregion
    
    #region Form Field Interface
    
    /// <summary>
    /// Interface for form field components
    /// </summary>
    public interface IFormField
    {
        void Initialize(FormFieldDefinition definition);
        object GetValue();
        void SetValue(object value);
        string GetName();
        bool IsValid();
        void Focus();
    }
    
    #endregion
    
    #region Form Field Components
    
    /// <summary>
    /// Button form field that triggers actions
    /// </summary>
    public class ButtonFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition fieldDefinition;
        private Button button;
        private TextMeshProUGUI buttonText;
        private string actionValue;
        
        public void Initialize(FormFieldDefinition definition)
        {
            fieldDefinition = definition;
            CreateUI();
            
            // Get action from options
            if (definition.options != null && definition.options.ContainsKey("action"))
            {
                actionValue = definition.options["action"].ToString();
            }
        }
        
        private void CreateUI()
        {
            // Safety check for fieldDefinition
            if (fieldDefinition == null)
            {
                // Debug.LogError("ButtonFormField.CreateUI: fieldDefinition is null");
                return;
            }
            
            // Get or add RectTransform (don't add if it already exists)
            var rect = GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }
            
            // Create button
            button = gameObject.AddComponent<Button>();
            var buttonImage = gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.6f, 0.9f, 0.8f);
            
            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = fieldDefinition.defaultValue?.ToString() ?? fieldDefinition.label;
            buttonText.fontSize = 14f;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            // Handle button click
            button.onClick.AddListener(() =>
            {
                // For button fields, we set a special action value that the form handler can detect
                if (!string.IsNullOrEmpty(actionValue))
                {
                    // Create a special form data entry that indicates this was a button click
                    var formPanel = GetComponentInParent<FormSubmitPanel>();
                    if (formPanel != null)
                    {
                        var formData = new Dictionary<string, object>
                        {
                            ["action"] = actionValue,
                            [fieldDefinition.name] = actionValue
                        };
                        
                        // Check if this button should close the form after action
                        bool shouldCloseForm = true;
                        if (fieldDefinition.options != null && fieldDefinition.options.ContainsKey("closeForm"))
                        {
                            bool.TryParse(fieldDefinition.options["closeForm"].ToString(), out shouldCloseForm);
                        }
                        
                        if (shouldCloseForm)
                        {
                            // Use the custom submission method which closes the form
                            formPanel.HandleCustomSubmission(formData);
                        }
                        else
                        {
                            // Trigger the event without closing the form
                            formPanel.TriggerFormSubmission(formData);
                        }
                    }
                }
            });
        }
        
        public object GetValue()
        {
            return actionValue ?? fieldDefinition.name;
        }
        
        public void SetValue(object value)
        {
            // Buttons don't have settable values
        }
        
        public string GetName()
        {
            return fieldDefinition?.name ?? "unknown";
        }
        
        public bool IsValid()
        {
            return true; // Buttons are always valid
        }
        
        public void Focus()
        {
            button?.Select();
        }
    }
    
    #endregion
}