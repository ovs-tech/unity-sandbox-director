using UnityEngine.UIElements;
using Core.UI.FormSubmit.Fields;

namespace Core.UI.FormSubmit
{
    /// <summary>
    /// Text input form field for UI Toolkit
    /// </summary>
    public class TextFormFieldUIToolkit : IFormFieldUIToolkit
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
                
                if (!string.IsNullOrEmpty(definition.placeholder))
                {
                    // Add placeholder functionality
                    textField.RegisterCallback<FocusInEvent>(OnFocusIn);
                    textField.RegisterCallback<FocusOutEvent>(OnFocusOut);
                    UpdatePlaceholder();
                }
            }
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            if (textField.value == definition.placeholder)
            {
                textField.value = "";
                textField.style.color = UnityEngine.Color.white;
            }
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            UpdatePlaceholder();
        }

        private void UpdatePlaceholder()
        {
            if (string.IsNullOrEmpty(textField.value) && !string.IsNullOrEmpty(definition.placeholder))
            {
                textField.value = definition.placeholder;
                textField.style.color = new UnityEngine.Color(1f, 1f, 1f, 0.5f);
            }
        }

        public object GetValue()
        {
            if (textField != null)
            {
                string value = textField.value;
                return value == definition.placeholder ? "" : value;
            }
            return "";
        }

        public void SetValue(object value)
        {
            if (textField != null)
            {
                textField.value = value?.ToString() ?? "";
                textField.style.color = UnityEngine.Color.white;
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
            textField?.Focus();
        }
    }
}