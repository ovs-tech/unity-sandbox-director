using Core.UI.FormSubmit.Fields;
using UnityEngine.UIElements;

namespace Core.UI.FormSubmit
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