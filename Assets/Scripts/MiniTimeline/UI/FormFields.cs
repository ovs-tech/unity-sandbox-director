using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace MiniTimeline.UI
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
            rectTransform.sizeDelta = new Vector2(0, 100);
            
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
            
            inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            
            // Text component for input field
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            
            var textMesh = textObj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "";
            textMesh.fontSize = 14f;
            textMesh.color = Color.white;
            textMesh.alignment = TextAlignmentOptions.TopLeft;
            
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
                placeholderRect.offsetMin = new Vector2(10, 5);
                placeholderRect.offsetMax = new Vector2(-10, -5);
                
                var placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
                placeholderText.text = definition.placeholder;
                placeholderText.fontSize = 14f;
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                placeholderText.alignment = TextAlignmentOptions.TopLeft;
                
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
}