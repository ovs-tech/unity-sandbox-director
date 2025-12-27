using UnityEngine.UIElements;
using Core.UI.FormSubmit.Fields;

namespace Core.UI.FormSubmit
{
    /// <summary>
    /// Toggle/checkbox form field for UI Toolkit
    /// </summary>
    public class ToggleFormFieldUIToolkit : IFormFieldUIToolkit
    {
        private FormFieldDefinition definition;
        private Toggle toggle;

        public void Initialize(FormFieldDefinition definition, VisualElement element)
        {
            this.definition = definition;
            this.toggle = element as Toggle;

            if (toggle != null)
            {
                if (definition.defaultValue is bool boolValue)
                {
                    toggle.value = boolValue;
                }
                else if (definition.defaultValue != null)
                {
                    if (bool.TryParse(definition.defaultValue.ToString(), out bool parsedValue))
                    {
                        toggle.value = parsedValue;
                    }
                }
            }
        }

        public object GetValue()
        {
            return toggle?.value ?? false;
        }

        public void SetValue(object value)
        {
            if (toggle != null && value != null)
            {
                if (value is bool boolValue)
                {
                    toggle.value = boolValue;
                }
                else if (bool.TryParse(value.ToString(), out bool parsedValue))
                {
                    toggle.value = parsedValue;
                }
            }
        }

        public string GetName()
        {
            return definition?.name ?? "unknown";
        }

        public bool IsValid()
        {
            // Toggles are always valid
            return true;
        }

        public void Focus()
        {
            toggle?.Focus();
        }
    }
}