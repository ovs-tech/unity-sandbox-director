using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Core.UI.FormSubmit.Fields
{
    /// <summary>
    /// Hidden form field - stores data without UI display
    /// </summary>
    public class HiddenFormField : MonoBehaviour, IFormField
    {
        private FormFieldDefinition definition;
        private object hiddenValue;
        
        public void Initialize(FormFieldDefinition definition)
        {
            this.definition = definition;
            
            // Set the hidden value from the definition's default value
            hiddenValue = definition.defaultValue;
            
            // Hide this GameObject since it's a hidden field
            gameObject.SetActive(false);
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
            return definition.name;
        }
        
        public bool IsValid()
        {
            return true; // Hidden fields are always valid
        }
        
        public void Focus()
        {
            // Hidden fields can't be focused
        }
    }
}