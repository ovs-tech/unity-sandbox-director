using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit.Fields;
using System.Collections.Generic;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Select dropdown form field for UI Toolkit
    /// </summary>
    public class SelectFormFieldUIToolkit : IFormFieldUIToolkit
    {
        private FormFieldDefinition definition;
        private DropdownField dropdownField;

        public void Initialize(FormFieldDefinition definition, VisualElement element)
        {
            this.definition = definition;
            this.dropdownField = element as DropdownField;

            if (dropdownField != null)
            {
                // Set up choices from options
                if (definition.options != null && definition.options.ContainsKey("items"))
                {
                    if (definition.options["items"] is List<string> items)
                    {
                        dropdownField.choices = new List<string>(items);
                        
                        // Set default value
                        string defaultValue = definition.defaultValue?.ToString();
                        if (!string.IsNullOrEmpty(defaultValue) && items.Contains(defaultValue))
                        {
                            dropdownField.value = defaultValue;
                        }
                        else if (items.Count > 0)
                        {
                            dropdownField.value = items[0];
                        }
                    }
                }
            }
        }

        public object GetValue()
        {
            return dropdownField?.value ?? "";
        }

        public void SetValue(object value)
        {
            if (dropdownField != null && value != null)
            {
                string stringValue = value.ToString();
                if (dropdownField.choices != null && dropdownField.choices.Contains(stringValue))
                {
                    dropdownField.value = stringValue;
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
                string value = GetValue()?.ToString();
                return !string.IsNullOrEmpty(value);
            }
            return true;
        }

        public void Focus()
        {
            dropdownField?.Focus();
        }
    }
}