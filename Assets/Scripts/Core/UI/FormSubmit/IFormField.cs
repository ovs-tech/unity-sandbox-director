using Core.UI.FormSubmit.Fields;

namespace Core.UI.FormSubmit
{
        /// <summary>
    /// Interface for form field components
    /// </summary>
    public interface IFormField
    {
        void Initialize(FormFieldDefinition definition);
        object GetValue();
        void SetValue(object value);
        string GetName();
        bool IsValid();
        void Focus();
    }
}