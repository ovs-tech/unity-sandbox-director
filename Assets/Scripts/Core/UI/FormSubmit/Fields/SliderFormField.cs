using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.FormSubmit.Fields
{
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
}