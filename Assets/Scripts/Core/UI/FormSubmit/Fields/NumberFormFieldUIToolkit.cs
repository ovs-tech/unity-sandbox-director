using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit.Fields;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Number input form field for UI Toolkit
    /// </summary>
    public class NumberFormFieldUIToolkit : IFormFieldUIToolkit
    {
        private FormFieldDefinition definition;
        private FloatField floatField;

        public void Initialize(FormFieldDefinition definition, VisualElement element)
        {
            this.definition = definition;
            this.floatField = element as FloatField;

            if (floatField != null)
            {
                if (definition.defaultValue != null)
                {
                    if (float.TryParse(definition.defaultValue.ToString(), out float defaultVal))
                    {
                        floatField.value = defaultVal;
                    }
                }

                // Set validation constraints if defined in options
                if (definition.options != null)
                {
                    // Register value change callback for validation
                    floatField.RegisterValueChangedCallback(OnValueChanged);
                }
            }
        }

        private void OnValueChanged(ChangeEvent<float> evt)
        {
            float newValue = evt.newValue;
            
            // Apply min/max constraints
            if (definition.options != null)
            {
                if (definition.options.ContainsKey("min") && 
                    float.TryParse(definition.options["min"].ToString(), out float min))
                {
                    if (newValue < min)
                    {
                        floatField.value = min;
                        return;
                    }
                }
                
                if (definition.options.ContainsKey("max") && 
                    float.TryParse(definition.options["max"].ToString(), out float max))
                {
                    if (newValue > max)
                    {
                        floatField.value = max;
                        return;
                    }
                }
            }
        }

        public object GetValue()
        {
            return floatField?.value ?? 0f;
        }

        public void SetValue(object value)
        {
            if (floatField != null && value != null)
            {
                if (float.TryParse(value.ToString(), out float floatValue))
                {
                    floatField.value = floatValue;
                }
            }
        }

        public string GetName()
        {
            return definition?.name ?? "unknown";
        }

        public bool IsValid()
        {
            if (definition.required)
            {
                return floatField != null;
            }

            // Check min/max constraints
            if (definition.options != null && floatField != null)
            {
                float value = floatField.value;
                
                if (definition.options.ContainsKey("min") && 
                    float.TryParse(definition.options["min"].ToString(), out float min))
                {
                    if (value < min) return false;
                }
                
                if (definition.options.ContainsKey("max") && 
                    float.TryParse(definition.options["max"].ToString(), out float max))
                {
                    if (value > max) return false;
                }
            }

            return true;
        }

        public void Focus()
        {
            floatField?.Focus();
        }
    }
}