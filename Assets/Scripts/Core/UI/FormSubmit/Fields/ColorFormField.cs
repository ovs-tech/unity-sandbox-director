using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Core.UI.FormSubmit.Fields
{
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