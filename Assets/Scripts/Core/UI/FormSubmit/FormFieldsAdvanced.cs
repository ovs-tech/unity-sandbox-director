using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace Core.UI.FormSubmit
{
    /// <summary>
    /// Select/Dropdown form field
    /// </summary>
    public class SelectFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private TMP_Dropdown dropdown;
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
            
            // Dropdown
            var dropdownObj = new GameObject("Dropdown");
            dropdownObj.transform.SetParent(transform, false);
            var dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0, 0);
            dropdownRect.anchorMax = new Vector2(1, 0.5f);
            dropdownRect.sizeDelta = Vector2.zero;
            dropdownRect.anchoredPosition = Vector2.zero;
            
            var dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = dropdownImage;
            
            // Label for dropdown
            var dropdownLabelObj = new GameObject("Label");
            dropdownLabelObj.transform.SetParent(dropdownObj.transform, false);
            var dropdownLabelRect = dropdownLabelObj.AddComponent<RectTransform>();
            dropdownLabelRect.anchorMin = new Vector2(0, 0);
            dropdownLabelRect.anchorMax = new Vector2(1, 1);
            dropdownLabelRect.sizeDelta = Vector2.zero;
            dropdownLabelRect.offsetMin = new Vector2(10, 0);
            dropdownLabelRect.offsetMax = new Vector2(-25, 0);
            
            var dropdownLabelText = dropdownLabelObj.AddComponent<TextMeshProUGUI>();
            dropdownLabelText.text = "";
            dropdownLabelText.fontSize = 14f;
            dropdownLabelText.color = Color.white;
            dropdownLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            
            dropdown.captionText = dropdownLabelText;
            
            // Arrow
            var arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            var arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-12.5f, 0);
            arrowRect.sizeDelta = new Vector2(20, 20);
            
            var arrowImage = arrowObj.AddComponent<Image>();
            arrowImage.color = Color.white;
            arrowImage.sprite = Resources.Load<Sprite>("UI/Skin/DropdownArrow");
            
            // Template (for dropdown list)
            var templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            var templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.anchoredPosition = new Vector2(0, -25);
            templateRect.sizeDelta = new Vector2(0, 50);
            
            templateObj.SetActive(false);
            
            var templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            var templateScrollRect = templateObj.AddComponent<ScrollRect>();
            
            // Viewport
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            viewportRect.offsetMin = new Vector2(5, 5);
            viewportRect.offsetMax = new Vector2(-5, -5);
            
            var viewportMask = viewportObj.AddComponent<Mask>();
            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f);
            
            // Content
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            
            // Item
            var itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            var itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0, 40);
            
            var itemToggle = itemObj.AddComponent<Toggle>();
            
            // Item Background
            var itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            var itemBgRect = itemBgObj.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            itemBgRect.anchoredPosition = Vector2.zero;
            
            var itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Item Label
            var itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.sizeDelta = Vector2.zero;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);
            
            var itemLabelText = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabelText.text = "";
            itemLabelText.fontSize = 14f;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            
            itemToggle.targetGraphic = itemBgImage;
            
            // Setup dropdown references
            dropdown.template = templateRect;
            dropdown.captionText = dropdownLabelText;
            dropdown.itemText = itemLabelText;
            
            templateScrollRect.content = contentRect;
            templateScrollRect.viewport = viewportRect;
            templateScrollRect.horizontal = false;
            templateScrollRect.vertical = true;
            
            // Populate options
            PopulateOptions();
        }
        
        private void PopulateOptions()
        {
            dropdown.options.Clear();
            
            if (definition.options != null && definition.options.ContainsKey("items"))
            {
                if (definition.options["items"] is List<string> stringOptions)
                {
                    foreach (var option in stringOptions)
                    {
                        dropdown.options.Add(new TMP_Dropdown.OptionData(option));
                    }
                }
                else if (definition.options["items"] is List<object> objectOptions)
                {
                    foreach (var option in objectOptions)
                    {
                        dropdown.options.Add(new TMP_Dropdown.OptionData(option.ToString()));
                    }
                }
            }
            
            dropdown.RefreshShownValue();
            
            // Set default value
            if (definition.defaultValue != null)
            {
                var defaultStr = definition.defaultValue.ToString();
                var index = dropdown.options.FindIndex(o => o.text == defaultStr);
                if (index >= 0)
                {
                    dropdown.value = index;
                }
            }
        }
        
        public object GetValue()
        {
            if (dropdown.value >= 0 && dropdown.value < dropdown.options.Count)
                return dropdown.options[dropdown.value].text;
            return "";
        }
        
        public void SetValue(object value)
        {
            var valueStr = value?.ToString() ?? "";
            var index = dropdown.options.FindIndex(o => o.text == valueStr);
            if (index >= 0)
            {
                dropdown.value = index;
            }
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            if (definition.required && dropdown.value < 0)
                return false;
            
            return true;
        }
        
        public void Focus()
        {
            dropdown.Select();
        }
    }
    
    /// <summary>
    /// Slider form field
    /// </summary>
    public class SliderFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private Slider slider;
        private TextMeshProUGUI label;
        private TextMeshProUGUI valueLabel;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            CreateUI();
        }
        
        private void CreateUI()
        {
            var rectTransform = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 80);
            
            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.75f);
            labelRect.anchorMax = new Vector2(0.7f, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            
            label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = definition.label;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.BottomLeft;
            
            // Value label
            var valueLabelObj = new GameObject("ValueLabel");
            valueLabelObj.transform.SetParent(transform, false);
            var valueLabelRect = valueLabelObj.AddComponent<RectTransform>();
            valueLabelRect.anchorMin = new Vector2(0.7f, 0.75f);
            valueLabelRect.anchorMax = new Vector2(1, 1);
            valueLabelRect.sizeDelta = Vector2.zero;
            valueLabelRect.anchoredPosition = Vector2.zero;
            
            valueLabel = valueLabelObj.AddComponent<TextMeshProUGUI>();
            valueLabel.text = "0";
            valueLabel.fontSize = 14f;
            valueLabel.color = Color.white;
            valueLabel.alignment = TextAlignmentOptions.BottomRight;
            
            // Slider
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(transform, false);
            var sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 0.6f);
            sliderRect.sizeDelta = Vector2.zero;
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.localScale = new Vector3(1, 0.3f, 1);
            
            slider = sliderObj.AddComponent<Slider>();
            
            // Background
            var backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(sliderObj.transform, false);
            var backgroundRect = backgroundObj.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 0.25f);
            backgroundRect.anchorMax = new Vector2(1, 0.75f);
            backgroundRect.sizeDelta = Vector2.zero;
            backgroundRect.anchoredPosition = Vector2.zero;
            
            var backgroundImage = backgroundObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Fill Area
            var fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.anchoredPosition = Vector2.zero;
            
            // Fill
            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            
            var fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 0.8f, 0.8f);
            
            // Handle Slide Area
            var handleSlideAreaObj = new GameObject("Handle Slide Area");
            handleSlideAreaObj.transform.SetParent(sliderObj.transform, false);
            var handleSlideAreaRect = handleSlideAreaObj.AddComponent<RectTransform>();
            handleSlideAreaRect.anchorMin = new Vector2(0, 0);
            handleSlideAreaRect.anchorMax = new Vector2(1, 1);
            handleSlideAreaRect.sizeDelta = Vector2.zero;
            handleSlideAreaRect.offsetMin = new Vector2(10, 0);
            handleSlideAreaRect.offsetMax = new Vector2(-10, 0);
            
            // Handle
            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleSlideAreaObj.transform, false);
            var handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            
            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;
            
            // Setup slider
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            
            // Set min/max values
            if (definition.options != null)
            {
                if (definition.options.ContainsKey("min"))
                    slider.minValue = System.Convert.ToSingle(definition.options["min"]);
                
                if (definition.options.ContainsKey("max"))
                    slider.maxValue = System.Convert.ToSingle(definition.options["max"]);
                
                if (definition.options.ContainsKey("step"))
                {
                    // For discrete steps
                    slider.wholeNumbers = true;
                }
            }
            
            // Set default value
            if (definition.defaultValue != null)
            {
                slider.value = System.Convert.ToSingle(definition.defaultValue);
            }
            
            // Update value label when slider changes
            slider.onValueChanged.AddListener(UpdateValueLabel);
            UpdateValueLabel(slider.value);
        }
        
        private void UpdateValueLabel(float value)
        {
            valueLabel.text = value.ToString("F2");
        }
        
        public object GetValue()
        {
            return slider.value;
        }
        
        public void SetValue(object value)
        {
            slider.value = System.Convert.ToSingle(value);
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            return true; // Sliders are always valid within their range
        }
        
        public void Focus()
        {
            slider.Select();
        }
    }
    
    /// <summary>
    /// Color picker form field
    /// </summary>
    public class ColorFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private Button colorButton;
        private Image colorDisplay;
        private TextMeshProUGUI label;
        private Color currentColor = Color.white;
        
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
            labelRect.anchorMax = new Vector2(0.7f, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            
            label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = definition.label;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.BottomLeft;
            
            // Color button
            var colorButtonObj = new GameObject("ColorButton");
            colorButtonObj.transform.SetParent(transform, false);
            var colorButtonRect = colorButtonObj.AddComponent<RectTransform>();
            colorButtonRect.anchorMin = new Vector2(0.7f, 0);
            colorButtonRect.anchorMax = new Vector2(1, 0.5f);
            colorButtonRect.sizeDelta = Vector2.zero;
            colorButtonRect.anchoredPosition = Vector2.zero;
            
            colorButton = colorButtonObj.AddComponent<Button>();
            colorDisplay = colorButtonObj.AddComponent<Image>();
            colorDisplay.color = currentColor;
            
            colorButton.targetGraphic = colorDisplay;
            colorButton.onClick.AddListener(ShowColorPicker);
            
            // Set default value
            if (definition.defaultValue is Color colorValue)
            {
                currentColor = colorValue;
                colorDisplay.color = currentColor;
            }
            else if (definition.defaultValue is string hexValue)
            {
                if (ColorUtility.TryParseHtmlString(hexValue, out Color parsedColor))
                {
                    currentColor = parsedColor;
                    colorDisplay.color = currentColor;
                }
            }
        }
        
        private void ShowColorPicker()
        {
            // Simple color picker - in a real implementation, you'd want a proper color picker UI
            // For now, we'll cycle through some preset colors
            Color[] presetColors = {
                Color.red, Color.green, Color.blue, Color.yellow,
                Color.cyan, Color.magenta, Color.white, Color.black
            };
            
            int currentIndex = System.Array.IndexOf(presetColors, currentColor);
            currentIndex = (currentIndex + 1) % presetColors.Length;
            currentColor = presetColors[currentIndex];
            colorDisplay.color = currentColor;
        }
        
        public object GetValue()
        {
            return currentColor;
        }
        
        public void SetValue(object value)
        {
            if (value is Color colorValue)
            {
                currentColor = colorValue;
                colorDisplay.color = currentColor;
            }
            else if (value is string hexValue)
            {
                if (ColorUtility.TryParseHtmlString(hexValue, out Color parsedColor))
                {
                    currentColor = parsedColor;
                    colorDisplay.color = currentColor;
                }
            }
        }
        
        public string GetName()
        {
            return definition.name;
        }
        
        public bool IsValid()
        {
            return true; // Colors are always valid
        }
        
        public void Focus()
        {
            colorButton.Select();
        }
    }
}