using Systems.Core.UI.FormSubmit.Fields;
using UnityEngine.UIElements;

namespace Systems.Core.UI.FormSubmit
{
    /// <summary>
    /// Interface for UI Toolkit form field components
    /// </summary>
    public interface IFormFieldUIToolkit
    {
        void Initialize(FormFieldDefinition definition, VisualElement element);
        object GetValue();
        void SetValue(object value);
        string GetName();
        bool IsValid();
        void Focus();
    }
}