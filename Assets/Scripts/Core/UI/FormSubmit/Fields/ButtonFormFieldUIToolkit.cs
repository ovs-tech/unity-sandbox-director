using UnityEngine.UIElements;
using Systems.Core.UI.FormSubmit.Fields;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Button form field for UI Toolkit
    /// </summary>
    public class ButtonFormFieldUIToolkit : IFormFieldUIToolkit
    {
        private FormFieldDefinition definition;
        private Button button;
        private string actionValue;

        public void Initialize(FormFieldDefinition definition, VisualElement element)
        {
            this.definition = definition;
            this.button = element as Button;

            if (button != null)
            {
                button.text = definition.label;
                
                // Get action from options
                if (definition.options != null && definition.options.ContainsKey("action"))
                {
                    actionValue = definition.options["action"].ToString();
                }
                else
                {
                    actionValue = definition.name;
                }
            }
        }

        public object GetValue()
        {
            return actionValue ?? definition?.name ?? "unknown";
        }

        public void SetValue(object value)
        {
            // Buttons don't have settable values in the traditional sense
            if (value != null)
            {
                actionValue = value.ToString();
            }
        }

        public string GetName()
        {
            return definition?.name ?? "unknown";
        }

        public bool IsValid()
        {
            // Buttons are always valid
            return true;
        }

        public void Focus()
        {
            button?.Focus();
        }
    }
}