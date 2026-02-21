using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit.Fields;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Slider form field for UI Toolkit
    /// </summary>
    public class SliderFormFieldUIToolkit : IFormFieldUIToolkit
    {
        private FormFieldDefinition definition;
        private Slider slider;

        public void Initialize(FormFieldDefinition definition, VisualElement element)
        {
            this.definition = definition;
            this.slider = element as Slider;

            if (slider != null)
            {
                // Set min/max values from options
                if (definition.options != null)
                {
                    if (definition.options.ContainsKey("min") && 
                        float.TryParse(definition.options["min"].ToString(), out float min))
                    {
                        slider.lowValue = min;
                    }
                    
                    if (definition.options.ContainsKey("max") && 
                        float.TryParse(definition.options["max"].ToString(), out float max))
                    {
                        slider.highValue = max;
                    }
                }

                // Set default value
                if (definition.defaultValue != null && 
                    float.TryParse(definition.defaultValue.ToString(), out float defaultVal))
                {
                    slider.value = defaultVal;
                }

                // Show value label
                slider.showInputField = true;
            }
        }

        public object GetValue()
        {
            return slider?.value ?? 0f;
        }

        public void SetValue(object value)
        {
            if (slider != null && value != null)
            {
                if (float.TryParse(value.ToString(), out float floatValue))
                {
                    slider.value = floatValue;
                }
            }
        }

        public string GetName()
        {
            return definition?.name ?? "unknown";
        }

        public bool IsValid()
        {
            // Sliders are always valid within their range
            return true;
        }

        public void Focus()
        {
            slider?.Focus();
        }
    }
}