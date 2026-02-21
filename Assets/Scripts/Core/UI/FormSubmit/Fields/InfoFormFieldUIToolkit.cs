using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit.Fields;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Info display form field for UI Toolkit (readonly text)
    /// </summary>
    public class InfoFormFieldUIToolkit : IFormFieldUIToolkit
    {
        private FormFieldDefinition definition;
        private TextField textField;

        public void Initialize(FormFieldDefinition definition, VisualElement element)
        {
            this.definition = definition;
            this.textField = element as TextField;

            if (textField != null)
            {
                textField.value = definition.defaultValue?.ToString() ?? "";
                textField.SetEnabled(false); // Make it readonly
            }
        }

        public object GetValue()
        {
            return textField?.value ?? "";
        }

        public void SetValue(object value)
        {
            if (textField != null)
            {
                textField.value = value?.ToString() ?? "";
            }
        }

        public string GetName()
        {
            return definition?.name ?? "unknown";
        }

        public bool IsValid()
        {
            // Info fields are always valid
            return true;
        }

        public void Focus()
        {
            // Info fields can't be focused since they're readonly
        }
    }
}