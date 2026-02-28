using UnityEngine;
using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit.Fields;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Color picker form field for UI Toolkit
    /// </summary>
    public class ColorFormFieldUIToolkit : IFormFieldUIToolkit
    {
        private FormFieldDefinition definition;
        private Button colorButton;
        private Color currentColor = Color.white;

        public void Initialize(FormFieldDefinition definition, VisualElement element)
        {
            this.definition = definition;
            this.colorButton = element as Button;

            if (colorButton != null)
            {
                // Set default color
                if (definition.defaultValue != null)
                {
                    if (definition.defaultValue is Color colorValue)
                    {
                        currentColor = colorValue;
                    }
                    else if (ColorUtility.TryParseHtmlString(definition.defaultValue.ToString(), out Color parsedColor))
                    {
                        currentColor = parsedColor;
                    }
                }

                UpdateButtonAppearance();

                // Set up click handler to open color picker
                colorButton.clicked += OnColorButtonClicked;
            }
        }

        private void OnColorButtonClicked()
        {
            // Note: UI Toolkit doesn't have a built-in color picker
            // This would need to be implemented as a custom popup or use a third-party solution
            // For now, we'll cycle through some preset colors as a demonstration
            CycleColor();
        }

        private void CycleColor()
        {
            // Simple color cycling for demonstration
            Color[] presetColors = {
                Color.white, Color.red, Color.green, Color.blue, 
                Color.yellow, Color.magenta, Color.cyan, Color.black
            };

            int currentIndex = System.Array.IndexOf(presetColors, currentColor);
            currentIndex = (currentIndex + 1) % presetColors.Length;
            currentColor = presetColors[currentIndex];
            
            UpdateButtonAppearance();
        }

        private void UpdateButtonAppearance()
        {
            if (colorButton != null)
            {
                colorButton.style.backgroundColor = currentColor;
                colorButton.text = $"Color: {ColorUtility.ToHtmlStringRGBA(currentColor)}";
                
                // Adjust text color for better visibility
                float brightness = (currentColor.r + currentColor.g + currentColor.b) / 3f;
                colorButton.style.color = brightness > 0.5f ? Color.black : Color.white;
            }
        }

        public object GetValue()
        {
            return currentColor;
        }

        public void SetValue(object value)
        {
            if (value != null)
            {
                if (value is Color colorValue)
                {
                    currentColor = colorValue;
                }
                else if (ColorUtility.TryParseHtmlString(value.ToString(), out Color parsedColor))
                {
                    currentColor = parsedColor;
                }
                
                UpdateButtonAppearance();
            }
        }

        public string GetName()
        {
            return definition?.name ?? "unknown";
        }

        public bool IsValid()
        {
            // Color fields are always valid
            return true;
        }

        public void Focus()
        {
            colorButton?.Focus();
        }
    }
}