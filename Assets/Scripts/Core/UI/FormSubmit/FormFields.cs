using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.UI.Core.Helpers;

namespace Core.UI.FormSubmit
{
    /// <summary>
    /// Text input form field
    /// </summary>
    public class TextFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private TMP_InputField inputField;
        private TextMeshProUGUI label;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            CreateUI();
        }
        
        private void CreateUI()
        {
            var rectTransform = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 60);
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            
            label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = definition.label;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.BottomLeft;
            
            // Input field
            var inputObj = new GameObject("Input");
            inputObj.transform.SetParent(transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.5f);
            inputRect.sizeDelta = Vector2.zero;
            inputRect.anchoredPosition = Vector2.zero;
            
            var inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            inputField = inputObj.AddComponent<TMP_InputField>();
            
            // Text component for input field
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            var textMesh = textObj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "";
            textMesh.fontSize = 14f;
            textMesh.color = Color.white;
            
            inputField.textComponent = textMesh;
            inputField.targetGraphic = inputImage;
            
            // Set placeholder
            if (!string.IsNullOrEmpty(definition.placeholder))
            {
                var placeholderObj = new GameObject("Placeholder");
                placeholderObj.transform.SetParent(inputObj.transform, false);
                var placeholderRect = placeholderObj.AddComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero;
                placeholderRect.offsetMin = new Vector2(10, 0);
                placeholderRect.offsetMax = new Vector2(-10, 0);
                
                var placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
                placeholderText.text = definition.placeholder;
                placeholderText.fontSize = 14f;
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                
                inputField.placeholder = placeholderText;
            }
            
            // Set default value
            if (definition.defaultValue != null)
            {
                inputField.text = definition.defaultValue.ToString();
            }
        }
        
        public object GetValue()
        {
            return inputField.text;
        }
        
        public void SetValue(object value)
        {
            inputField.text = value?.ToString() ?? "";
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            if (definition.required && string.IsNullOrEmpty(inputField.text))
                return false;
            
            return true;
        }
        
        public void Focus()
        {
            inputField.Select();
        }
    }
    
    /// <summary>
    /// Multi-line text input form field
    /// </summary>
    public class TextAreaFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private TMP_InputField inputField;
        private TextMeshProUGUI label;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            CreateUI();
        }
        
        private void CreateUI()
        {
            var rectTransform = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 150); // Increased height for better scrolling experience
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.8f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            
            label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = definition.label;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.BottomLeft;
            
            // Input field
            var inputObj = new GameObject("Input");
            inputObj.transform.SetParent(transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.75f);
            inputRect.sizeDelta = Vector2.zero;
            inputRect.anchoredPosition = Vector2.zero;
            
            var inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Add ScrollRect for scrollable content
            var scrollRect = inputObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            
            // Create scrollbar
            var scrollbarObj = new GameObject("Scrollbar Vertical");
            scrollbarObj.transform.SetParent(inputObj.transform, false);
            var scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            scrollbarRect.anchoredPosition = Vector2.zero;
            
            var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            
            // Scrollbar background
            var scrollbarBg = scrollbarObj.AddComponent<Image>();
            scrollbarBg.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            scrollbar.targetGraphic = scrollbarBg;
            
            // Scrollbar handle area
            var handleAreaObj = new GameObject("Sliding Area");
            handleAreaObj.transform.SetParent(scrollbarObj.transform, false);
            var handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-20, -20);
            handleAreaRect.anchoredPosition = Vector2.zero;
            
            // Scrollbar handle
            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            var handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = new Vector2(0, 20);
            handleRect.anchoredPosition = Vector2.zero;
            
            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            
            // Connect scrollbar to scrollrect
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = -3;
            
            // Create viewport for masking (adjusted for scrollbar)
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(inputObj.transform, false);
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = new Vector2(1, 1);
            viewportRect.sizeDelta = new Vector2(-20, 0); // Make room for scrollbar
            viewportRect.anchoredPosition = Vector2.zero;
            
            var mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f); // Nearly transparent for masking
            
            // Create content container
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;
            
            // Content Size Fitter for auto-sizing based on text
            var contentSizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            inputField = contentObj.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            
            // Setup ScrollRect references
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            
            // Text component for input field
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(contentObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            
            var textMesh = UICreationHelper.CreateStandardTextMesh(textObj, "", 14f, Color.white);
            textMesh.alignment = TextAlignmentOptions.TopLeft;
            textMesh.overflowMode = TextOverflowModes.Overflow; // Allow overflow for scrolling
            
            inputField.textComponent = textMesh;
            inputField.targetGraphic = inputImage;
            
            // Set placeholder
            if (!string.IsNullOrEmpty(definition.placeholder))
            {
                var placeholderObj = new GameObject("Placeholder");
                placeholderObj.transform.SetParent(contentObj.transform, false);
                var placeholderRect = placeholderObj.AddComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero;
                placeholderRect.offsetMin = new Vector2(10, 5);
                placeholderRect.offsetMax = new Vector2(-10, -5);
                
                var placeholderText = UICreationHelper.CreateStandardTextMesh(placeholderObj, definition.placeholder, 14f, new Color(0.5f, 0.5f, 0.5f, 0.8f));
                placeholderText.alignment = TextAlignmentOptions.TopLeft;
                
                inputField.placeholder = placeholderText;
            }
            
            // Set default value
            if (definition.defaultValue != null)
            {
                inputField.text = definition.defaultValue.ToString();
            }
            
            // Check for readonly option
            if (definition.options != null && definition.options.ContainsKey("readonly") && System.Convert.ToBoolean(definition.options["readonly"]))
            {
                inputField.readOnly = true;
                inputField.interactable = false;
                // Change background color for readonly fields
                inputImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            }
        }
        
        public object GetValue()
        {
            return inputField.text;
        }
        
        public void SetValue(object value)
        {
            inputField.text = value?.ToString() ?? "";
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            if (definition.required && string.IsNullOrEmpty(inputField.text))
                return false;
            
            return true;
        }
        
        public void Focus()
        {
            inputField.Select();
        }
    }
    
    /// <summary>
    /// Number input form field
    /// </summary>
    public class NumberFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private TMP_InputField inputField;
        private TextMeshProUGUI label;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            CreateUI();
        }
        
        private void CreateUI()
        {
            var rectTransform = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 60);
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            
            label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = definition.label;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.BottomLeft;
            
            // Input field
            var inputObj = new GameObject("Input");
            inputObj.transform.SetParent(transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.5f);
            inputRect.sizeDelta = Vector2.zero;
            inputRect.anchoredPosition = Vector2.zero;
            
            var inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            
            // Text component for input field
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            var textMesh = textObj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "";
            textMesh.fontSize = 14f;
            textMesh.color = Color.white;
            
            inputField.textComponent = textMesh;
            inputField.targetGraphic = inputImage;
            
            // Set placeholder
            if (!string.IsNullOrEmpty(definition.placeholder))
            {
                var placeholderObj = new GameObject("Placeholder");
                placeholderObj.transform.SetParent(inputObj.transform, false);
                var placeholderRect = placeholderObj.AddComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.sizeDelta = Vector2.zero;
                placeholderRect.offsetMin = new Vector2(10, 0);
                placeholderRect.offsetMax = new Vector2(-10, 0);
                
                var placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
                placeholderText.text = definition.placeholder;
                placeholderText.fontSize = 14f;
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                
                inputField.placeholder = placeholderText;
            }
            
            // Set default value
            if (definition.defaultValue != null)
            {
                inputField.text = definition.defaultValue.ToString();
            }
        }
        
        public object GetValue()
        {
            if (float.TryParse(inputField.text, out float result))
                return result;
            return 0f;
        }
        
        public void SetValue(object value)
        {
            inputField.text = value?.ToString() ?? "0";
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            if (definition.required && string.IsNullOrEmpty(inputField.text))
                return false;
            
            return float.TryParse(inputField.text, out _);
        }
        
        public void Focus()
        {
            inputField.Select();
        }
    }
    
    /// <summary>
    /// Toggle/Checkbox form field
    /// </summary>
    public class ToggleFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private Toggle toggle;
        private TextMeshProUGUI label;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            CreateUI();
        }
        
        private void CreateUI()
        {
            var rectTransform = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 40);
            
            // Toggle
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(transform, false);
            var toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 0);
            toggleRect.anchorMax = new Vector2(1, 1);
            toggleRect.sizeDelta = Vector2.zero;
            toggleRect.anchoredPosition = Vector2.zero;
            
            toggle = toggleObj.AddComponent<Toggle>();
            
            // Background
            var backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(toggleObj.transform, false);
            var backgroundRect = backgroundObj.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 0.5f);
            backgroundRect.anchorMax = new Vector2(0, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(20, 20);
            
            var backgroundImage = backgroundObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Checkmark
            var checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(backgroundObj.transform, false);
            var checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.sizeDelta = Vector2.zero;
            checkmarkRect.anchoredPosition = Vector2.zero;
            
            var checkmarkImage = checkmarkObj.AddComponent<Image>();
            checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            checkmarkImage.sprite = Resources.Load<Sprite>("UI/Skin/Checkmark");
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.offsetMin = new Vector2(30, 0);
            labelRect.anchoredPosition = Vector2.zero;
            
            label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = definition.label;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Setup toggle
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            
            // Set default value
            if (definition.defaultValue is bool boolValue)
            {
                toggle.isOn = boolValue;
            }
        }
        
        public object GetValue()
        {
            return toggle.isOn;
        }
        
        public void SetValue(object value)
        {
            if (value is bool boolValue)
                toggle.isOn = boolValue;
            else
                toggle.isOn = false;
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            return true; // Toggles are always valid
        }
        
        public void Focus()
        {
            toggle.Select();
        }
    }
    
    /// <summary>
    /// Hidden form field - stores data without UI display
    /// </summary>
    public class HiddenFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private object hiddenValue;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            
            // Set the hidden value from the definition's default value
            hiddenValue = definition.defaultValue;
            
            // Hide this GameObject since it's a hidden field
            gameObject.SetActive(false);
        }
        
        public object GetValue()
        {
            return hiddenValue;
        }
        
        public void SetValue(object value)
        {
            hiddenValue = value;
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            return true; // Hidden fields are always valid
        }
        
        public void Focus()
        {
            // Hidden fields can't be focused
        }
    }
}