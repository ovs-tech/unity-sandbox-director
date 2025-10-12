using UnityEngine.UIElements;
using Core.UI.FormSubmit.Fields;

namespace Core.UI.FormSubmit
{
    /// <summary>
    /// Hidden form field for UI Toolkit (stores data without display)
    /// </summary>
    public class HiddenFormFieldUIToolkit : IFormFieldUIToolkit
    {
        private FormFieldDefinition definition;
        private object hiddenValue;

        public void Initialize(FormFieldDefinition definition, VisualElement element)
        {
            this.definition = definition;
            
            // Set the hidden value from the definition's default value
            hiddenValue = definition.defaultValue;
            
            // Hide the element
            if (element != null)
            {
                element.style.display = DisplayStyle.None;
            }
        }

        public object GetValue()
        {
            return hiddenValue;
        }

        public void SetValue(object value)
        {
            hiddenValue = value;
        }

        public string GetName()
        {
            return definition?.name ?? "unknown";
        }

        public bool IsValid()
        {
            // Hidden fields are always valid
            return true;
        }

        public void Focus()
        {
            // Hidden fields can't be focused
        }
    }
}